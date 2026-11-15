using GBV_Shield.Services;
using System.Net;
using System.Text.Json;

void Assert(bool value, string message) { if (!value) throw new Exception(message); Console.WriteLine("PASS: " + message); }
bool Relevant(object tags) { using var d = JsonDocument.Parse(JsonSerializer.Serialize(tags)); return OSMService.IsRelevantSupport(d.RootElement); }
Assert(!Relevant(new { name="1st Benoni Sea Scouts", amenity="community_centre" }), "Exclude Scouts");
Assert(!Relevant(new { name="The Magic Company", amenity="community_centre" }), "Exclude entertainment");
Assert(!Relevant(new { name="Jupiter's", office="ngo" }), "NGO tag alone is insufficient");
Assert(Relevant(new { name="Kidz Clinic Boksburg", amenity="clinic" }), "Include Kidz Clinic");
Assert(Relevant(new { name="Childline Gauteng" }), "Include Childline");
Assert(Relevant(new { name="Boksburg Child Welfare" }), "Include Child Welfare");
Assert(Relevant(new Dictionary<string,string> { ["name"]="Hope Centre", ["amenity"]="social_facility", ["social_facility:for"]="abused;women" }), "Include tagged abuse support");
var handler = new FixtureHandler();
var service = new OSMService(new HttpClient(handler));
var police = await service.GetNearbySupportAsync(0,0,SupportCategory.Police,10000);
Assert(police.Count==2, "Keep stations, exclude academy and outside-radius station, deduplicate objects");
Assert(police[0].Name=="Near Police" && police[1].Name=="Far Police", "Sort by distance");
Assert(police.All(x=>x.Address!="Address not supplied"), "Recover missing street address");
Assert(handler.Calls==2, "Retry incomplete Overpass response rather than treating it as empty success");
var local = await service.GetNearbySupportAsync(-26.192579,28.256326,SupportCategory.Police,10000);
Assert(local.Any(x=>x.Name=="Boksburg North SAPS"), "Official station survives missing OSM address");
Assert(local.All(x=>x.Distance<=10000) && local.SequenceEqual(local.OrderBy(x=>x.Distance)), "Merged records respect radius and ordering");
Console.WriteLine("All directory checks passed.");

sealed class FixtureHandler : HttpMessageHandler
{
    public int Calls;
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
    {
        Calls++;
        var json = Calls==1 ? "{\"remark\":\"runtime error: timeout\",\"elements\":[]}" : """
        {"elements":[
        {"lat":0.02,"lon":0,"tags":{"name":"Far Police","amenity":"police","addr:street":"Second Street"}},
        {"lat":0.01,"lon":0,"tags":{"name":"Near Police","amenity":"police"}},
        {"lat":0.01,"lon":0,"tags":{"name":"Near Police","amenity":"police"}},
        {"lat":0.01,"lon":0,"tags":{"name":"Police Academy","amenity":"police"}},
        {"lat":0.5,"lon":0,"tags":{"name":"Outside Police","amenity":"police","addr:street":"Third Street"}}
        ]}
        """;
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) {Content=new StringContent(json)});
    }
}
