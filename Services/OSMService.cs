using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.ApplicationModel.Communication;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Devices.Sensors;
using System.Text.RegularExpressions;

namespace GBV_Shield.Services
{
    public enum SupportCategory
    {
        Hospital,
        Police,
        NgoNpo
    }

    public sealed class OSMService
    {
        private static readonly Uri[] OverpassEndpoints =
        {
            new("https://maps.mail.ru/osm/tools/overpass/api/interpreter"),
            new("https://overpass.private.coffee/api/interpreter"),
            new("https://overpass-api.de/api/interpreter")
        };
        private readonly HttpClient _httpClient;
        private readonly Dictionary<string, Location> _officeLocations = new();
        public string SearchNotice { get; private set; } = string.Empty;

        private const string SupportNames = @"\b(child\s*line|child\s*(and\s*family\s*)?welfare|kidz\s*clinic|victim\s*(support|empowerment)|rape\s*crisis|domestic\s*(abuse|violence)|gender.based\s*violence|gbv|women.?s?\s*shelter|safe\s*house|people\s*opposing\s*women\s*abuse|powa)\b";

        internal static bool IsRelevantSupport(JsonElement tags)
        {
            var name = GetTag(tags, "name") ?? "";
            if (Regex.IsMatch(name, @"\b(scouts?|casino|arcade|cultural\s*precinct)\b", RegexOptions.IgnoreCase)) return false;
            if (Regex.IsMatch(name, SupportNames, RegexOptions.IgnoreCase)) return true;
            var purpose = GetTag(tags, "social_facility:for") ?? "";
            return Regex.IsMatch(purpose, @"(^|;)(abused|victims|domestic_violence|women|children|juvenile)(;|$)", RegexOptions.IgnoreCase)
                && (GetTag(tags, "amenity") == "social_facility" || GetTag(tags, "office") == "ngo");
        }

        public OSMService()
        {
            // Use the managed transport so Android socket failures have consistent
            // HttpRequestException semantics and can fall through to the next server.
            _httpClient = new HttpClient(new SocketsHttpHandler
            {
                ConnectTimeout = TimeSpan.FromSeconds(10),
                PooledConnectionLifetime = TimeSpan.FromMinutes(2)
            }) { Timeout = TimeSpan.FromSeconds(15) };
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("GBV-Shield/1.0 support-location-search");
        }

        internal OSMService(HttpClient httpClient) => _httpClient = httpClient;

        public async Task<List<PoiResult>> GetNearbySupportAsync(
            double latitude,
            double longitude,
            SupportCategory category = SupportCategory.Hospital,
            int? radiusMeters = null,
            CancellationToken cancellationToken = default,
            Action<List<PoiResult>>? onDirectoryReady = null)
        {
            SearchNotice = string.Empty;
            var radius = Math.Clamp(radiusMeters ?? (category == SupportCategory.NgoNpo ? 50_000 : 10_000), 500, 50_000);
            var saved = await GetDirectoryAsync(latitude, longitude, category, radius, cancellationToken);
            saved = saved.OrderBy(x => x.Distance).ToList();
            if (saved.Count > 0) onDirectoryReady?.Invoke(saved);
            using var liveTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            liveTimeout.CancelAfter(TimeSpan.FromSeconds(35));
            List<PoiResult> mapped;
            try { mapped = await GetMappedAsync(latitude, longitude, category, radius, liveTimeout.Token); }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested &&
                ex is HttpRequestException or JsonException or OperationCanceledException)
            {
                mapped = new();
                SearchNotice = "Live search unavailable. Showing saved directory contacts; coverage may be incomplete.";
            }
            if (mapped.Count == 0 && saved.Count == 0 && SearchNotice.Length > 0)
                throw new HttpRequestException("Live search is unavailable and no saved contacts could be located nearby.");
            var combined = new List<PoiResult>(saved);
            foreach (var item in mapped.OrderBy(x => x.Distance))
            {
                if (combined.Any(x => SamePlace(x, item))) continue;
                combined.Add(item);
            }
            return combined.Where(x => x.Distance <= radius).OrderBy(x => x.Distance).ToList();
        }

        private static bool SamePlace(PoiResult a, PoiResult b)
        {
            static string Key(string name) => Regex.Replace(name.ToLowerInvariant(), @"\b(saps|police|station|south african|suid.afrikaanse|polisiediens)\b|[^a-z0-9]", "");
            return Key(a.Name) == Key(b.Name) && DistanceMeters(a.Latitude, a.Longitude, b.Latitude, b.Longitude) < 1000
                || a.Type == "Police Station" && b.Type == a.Type && DistanceMeters(a.Latitude, a.Longitude, b.Latitude, b.Longitude) < 100;
        }

        private async Task<List<PoiResult>> GetDirectoryAsync(double latitude, double longitude, SupportCategory category, int radius, CancellationToken cancellationToken)
        {
            var results = new List<PoiResult>();
            foreach (var entry in SupportDirectory.Entries.Where(x => x.Category == category))
            {
                cancellationToken.ThrowIfCancellationRequested();
                Location? location = entry.Latitude is double lat && entry.Longitude is double lon ? new Location(lat, lon) : null;
                if (location is null && !_officeLocations.TryGetValue(entry.Address, out location))
                {
                    try
                    {
                        location = (await Geocoding.Default.GetLocationsAsync(entry.Address + ", South Africa")
                            .WaitAsync(TimeSpan.FromSeconds(5), cancellationToken)).FirstOrDefault();
                        if (location != null) _officeLocations[entry.Address] = location;
                    }
                    catch (Exception) when (!cancellationToken.IsCancellationRequested) { }
                }
                if (location is null) { SearchNotice = "Some directory offices could not be located. Call Childline 116 for child-protection referrals."; continue; }
                var distance = DistanceMeters(latitude, longitude, location.Latitude, location.Longitude);
                if (distance > radius) continue;
                results.Add(new PoiResult { Name = entry.Name, Address = entry.Address, Phone = entry.Phone,
                    Type = category == SupportCategory.Police ? "Police Station" : "NGO / NPO",
                    Latitude = location.Latitude, Longitude = location.Longitude, Distance = distance,
                    WebsiteUrl = entry.Source, SourceLabel = "Directory checked 14 Sep 2026 • call before visiting",
                    MapsUrl = "https://www.google.com/maps/search/?api=1&query=" + Uri.EscapeDataString(entry.Name + ", " + entry.Address) });
            }
            return results;
        }

        private async Task<List<PoiResult>> GetMappedAsync(double latitude, double longitude, SupportCategory category, int? radiusMeters, CancellationToken cancellationToken)
        {
            var radius = Math.Clamp(radiusMeters ?? Constants.ShelterSearchRadius, 500, 50_000);
            var lat = latitude.ToString(CultureInfo.InvariantCulture);
            var lon = longitude.ToString(CultureInfo.InvariantCulture);
            // Bounding-box lookups avoid expensive around geometry scans at 50 km.
            // Enforce the circular radius on the returned coordinates below.
            var latSpan = radius / 111_000d;
            var lonSpan = latSpan / Math.Max(0.01, Math.Cos(latitude * Math.PI / 180));
            var bounds = FormattableString.Invariant($"{latitude - latSpan},{longitude - lonSpan},{latitude + latSpan},{longitude + lonSpan}");
            var categoryQuery = category switch
            {
                SupportCategory.Hospital =>
                    $"nwr(around:{radius},{lat},{lon})[\"amenity\"=\"hospital\"];",
                SupportCategory.Police =>
                    $"nwr(around:{radius},{lat},{lon})[\"amenity\"=\"police\"]; nwr(around:{radius},{lat},{lon})[\"police\"=\"station\"];",
                SupportCategory.NgoNpo => $$"""
                    nwr(around:{{radius}},{{lat}},{{lon}})["office"~"^(ngo|association|charity)$"];
                    nwr(around:{{radius}},{{lat}},{{lon}})["amenity"="social_facility"];
                    nwr(around:{{radius}},{{lat}},{{lon}})["social_facility"~"^(shelter|group_home|assisted_living|outreach|care_home)$"];
                    nwr(around:{{radius}},{{lat}},{{lon}})["social_work"~"^(yes|child_protection|family_support|crisis_centre)$"];
                    nwr(around:{{radius}},{{lat}},{{lon}})["amenity"~"^(clinic|doctors)$"]["name"~"(child|kidz|welfare|childline|gbv|abuse|victim|women|family|crisis|safe.?house|shelter)",i];
                    nwr(around:{{radius}},{{lat}},{{lon}})["name"~"(childline|child.?welfare|kidz.?clinic|safe.?house|abuse.?shelter|victim.?support|women.?shelter|gbv)",i];
                    nwr(around:{{radius}},{{lat}},{{lon}})["emergency"="social_facility"];
                    """,
                _ => throw new ArgumentOutOfRangeException(nameof(category))
            };
            categoryQuery = categoryQuery.Replace($"around:{radius},{lat},{lon}", bounds);
            var query = $$"""
                [out:json][timeout:12];
                (
                  {{categoryQuery}}
                );
                out center tags;
                """;

            HttpResponseMessage? response = null;
            Exception? lastError = null;
            foreach (var endpoint in OverpassEndpoints)
            {
                try
                {
                    using var content = new FormUrlEncodedContent(
                        new Dictionary<string, string> { ["data"] = query });
                    var attempt = await _httpClient.PostAsync(endpoint, content, cancellationToken);
                    if (attempt.IsSuccessStatusCode)
                    {
                        try
                        {
                            using var check = JsonDocument.Parse(await attempt.Content.ReadAsStringAsync(cancellationToken));
                            if (check.RootElement.TryGetProperty("remark", out _) ||
                                !check.RootElement.TryGetProperty("elements", out var list) || list.ValueKind != JsonValueKind.Array)
                                throw new JsonException("Incomplete map response");
                        }
                        catch (JsonException)
                        {
                            attempt.Dispose();
                            lastError = new HttpRequestException("Location directory returned incomplete results.");
                            continue;
                        }
                        response = attempt;
                        break;
                    }

                    lastError = new HttpRequestException(
                        $"Location directory returned {(int)attempt.StatusCode} ({attempt.ReasonPhrase}).");
                    attempt.Dispose();
                }
                catch (Exception ex) when (!cancellationToken.IsCancellationRequested &&
                    (ex is HttpRequestException or OperationCanceledException or System.IO.IOException))
                {
                    lastError = ex;
                }
            }

            if (response is null)
                throw new HttpRequestException("The location directory is temporarily unavailable.", lastError);

            using (response)
            {
                await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
                if (!document.RootElement.TryGetProperty("elements", out var elements))
                    return new List<PoiResult>();

                var results = new List<PoiResult>();
                foreach (var element in elements.EnumerateArray())
                {
                    if (!TryGetCoordinates(element, out var resultLatitude, out var resultLongitude)) continue;

                    var tags = element.TryGetProperty("tags", out var tagElement) ? tagElement : default;
                    var name = GetTag(tags, "name");
                    if (string.IsNullOrWhiteSpace(name)) continue;
                    var distance = DistanceMeters(latitude, longitude, resultLatitude, resultLongitude);
                    if (distance > radius) continue;
                    if (category == SupportCategory.NgoNpo && !IsRelevantSupport(tags)) continue;
                    if (GetTag(tags, "access") is "private" or "no" ||
                        GetTag(tags, "disused") == "yes" || GetTag(tags, "abandoned") == "yes") continue;

                    // Do not infer round-the-clock availability from the word hospital.
                    if (category == SupportCategory.Hospital &&
                        GetTag(tags, "opening_hours")?.Trim() != "24/7") continue;
                    if (category == SupportCategory.Police &&
                        (GetTag(tags, "police") is string policeType && policeType != "station" ||
                         name.Contains("Academy", StringComparison.OrdinalIgnoreCase) ||
                         name.Contains("Flying Squad", StringComparison.OrdinalIgnoreCase) ||
                         name.Contains("Immigration", StringComparison.OrdinalIgnoreCase))) continue;

                    var address = FormatAddress(tags);
                    if (address == "Address not supplied")
                    {
                        try
                        {
                            var place = (await Geocoding.Default.GetPlacemarksAsync(resultLatitude, resultLongitude)
                                .WaitAsync(TimeSpan.FromSeconds(3), cancellationToken)).FirstOrDefault();
                            if (!string.IsNullOrWhiteSpace(place?.Thoroughfare))
                                address = "Near " + string.Join(", ", new[] { place.Thoroughfare, place.SubLocality, place.Locality }
                                    .Where(x => !string.IsNullOrWhiteSpace(x)).Distinct());
                        }
                        catch (Exception) when (!cancellationToken.IsCancellationRequested) { }
                    }
                    if (address == "Address not supplied") continue;

                    var website = GetTag(tags, "website") ?? GetTag(tags, "contact:website");
                    results.Add(new PoiResult
                    {
                        Name = name,
                        Address = address,
                        Type = GetTypeLabel(tags, category),
                        Phone = GetTag(tags, "phone") ?? GetTag(tags, "contact:phone"),
                        WebsiteUrl = NormalizeWebsite(website),
                        MapsUrl = $"https://www.openstreetmap.org/?mlat={resultLatitude.ToString(CultureInfo.InvariantCulture)}&mlon={resultLongitude.ToString(CultureInfo.InvariantCulture)}#map=17/{resultLatitude.ToString(CultureInfo.InvariantCulture)}/{resultLongitude.ToString(CultureInfo.InvariantCulture)}",
                        Distance = distance,
                        Latitude = resultLatitude, Longitude = resultLongitude,
                        SourceLabel = "OpenStreetMap • address and availability may need confirmation"
                    });
                }

                var ordered = results.Where(result => result.Distance <= radius).GroupBy(result => (result.Name, result.Address))
                    .Select(group => group.First()).OrderBy(result => result.Distance);
                return ordered.ToList();
            }
        }

        private static bool TryGetCoordinates(JsonElement element, out double latitude, out double longitude)
        {
            if (element.TryGetProperty("lat", out var lat) && element.TryGetProperty("lon", out var lon) ||
                element.TryGetProperty("center", out var center) &&
                center.TryGetProperty("lat", out lat) && center.TryGetProperty("lon", out lon))
            {
                latitude = lat.GetDouble();
                longitude = lon.GetDouble();
                return true;
            }
            latitude = longitude = 0;
            return false;
        }

        private static string? GetTag(JsonElement tags, string name) =>
            tags.ValueKind == JsonValueKind.Object && tags.TryGetProperty(name, out var value) ? value.GetString() : null;

        private static string FormatAddress(JsonElement tags)
        {
            var full = GetTag(tags, "addr:full");
            if (!string.IsNullOrWhiteSpace(full)) return full;
            var street = GetTag(tags, "addr:street") ?? GetTag(tags, "contact:street") ?? GetTag(tags, "addr:place");
            if (string.IsNullOrWhiteSpace(street)) return "Address not supplied";
            var address = string.Join(", ", new[] { GetTag(tags, "addr:housenumber") ?? GetTag(tags, "contact:housenumber"),
                street, GetTag(tags, "addr:suburb"), GetTag(tags, "addr:city") ?? GetTag(tags, "contact:city") }
                .Where(part => !string.IsNullOrWhiteSpace(part)));
            return string.IsNullOrWhiteSpace(address) ? "Address not supplied" : address;
        }

        private static string GetTypeLabel(JsonElement tags, SupportCategory category) => category switch
        {
            SupportCategory.Police => "Police Station",
            SupportCategory.NgoNpo => "NGO / NPO",
            _ => "Hospital • listed as 24 hours"
        };

        private static string? NormalizeWebsite(string? website)
        {
            if (string.IsNullOrWhiteSpace(website)) return null;
            var candidate = website.Contains("://") ? website : $"https://{website}";
            return Uri.TryCreate(candidate, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https" ? uri.AbsoluteUri : null;
        }

        private static double DistanceMeters(double lat1, double lon1, double lat2, double lon2)
        {
            const double earthRadius = 6_371_000;
            var latDelta = DegreesToRadians(lat2 - lat1);
            var lonDelta = DegreesToRadians(lon2 - lon1);
            var a = Math.Sin(latDelta / 2) * Math.Sin(latDelta / 2) +
                    Math.Cos(DegreesToRadians(lat1)) * Math.Cos(DegreesToRadians(lat2)) *
                    Math.Sin(lonDelta / 2) * Math.Sin(lonDelta / 2);
            return earthRadius * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        }

        private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180;
    }

    public sealed class PoiResult
    {
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string MapsUrl { get; set; } = string.Empty;
        public string? WebsiteUrl { get; set; }
        public string? Phone { get; set; }
        public double Distance { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string SourceLabel { get; set; } = string.Empty;
        public double DistanceKm => Math.Round(Distance / 1000, 1);
        public string WebsiteDisplay => WebsiteUrl ?? string.Empty;
        public bool HasPhone => !string.IsNullOrWhiteSpace(Phone);
        public bool HasWebsite => !string.IsNullOrWhiteSpace(WebsiteUrl);
        public string TagColor => Type switch
        {
            "Police Station" => "#1565C0",
            "Hospital" => "#C62828",
            "Clinic" => "#D84315",
            "Shelter / Support" => "#2E7D32",
            _ => "#455A64"
        };

        public ICommand OpenMapsCommand => new Command(async () => await Launcher.Default.OpenAsync(new Uri(MapsUrl)));
        public ICommand OpenWebsiteCommand => new Command(async () =>
        {
            if (Uri.TryCreate(WebsiteUrl, UriKind.Absolute, out var uri)) await Launcher.Default.OpenAsync(uri);
        });
        public ICommand CallCommand => new Command(() =>
        {
            var first = Phone?.Split(new[] { ';', ',', '/' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            var number = Regex.Replace(first ?? "", @"[^+0-9]", "");
            if (number.Length > 0 && PhoneDialer.Default.IsSupported) PhoneDialer.Default.Open(number);
        });
    }
}
