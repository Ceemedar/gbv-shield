namespace GBV_Shield.Services;

// Public office contacts checked 2026-09-14. No confidential shelter addresses.
// Keep provenance with each record so maintenance does not depend on name matching.
internal sealed record SupportEntry(string Name, SupportCategory Category, string Address,
    string Phone, string Source, double? Latitude = null, double? Longitude = null);

internal static class SupportDirectory
{
    internal static readonly SupportEntry[] Entries =
    [
        new("Boksburg North SAPS", SupportCategory.Police, "37 Fourth Street, Boksburg North, Boksburg, 1459", "0119600100", "https://www.saps.gov.za/contacts/stationdetails.php?sid=233&sname=Boksburg+North+%28Gauteng%29", -26.20587, 28.25301),
        new("Boksburg SAPS", SupportCategory.Police, "112 Commissioner Street, Boksburg, 1459", "0104979900", "https://www.saps.gov.za/contacts/stationdetails.php?sid=225&sname=Boksburg+%28Gauteng%29", -26.22025, 28.23787),
        new("Benoni SAPS", SupportCategory.Police, "117 Harpur Avenue, Benoni, 1501", "0761566327", "https://www.saps.gov.za/contacts/stationdetails.php?sid=1080", -26.19655, 28.31337),
        new("Actonville SAPS", SupportCategory.Police, "73 Singh Street, Actonville, Benoni, 1502", "0119646000", "https://www.saps.gov.za/contacts/stationdetails.php?sid=456", -26.21198, 28.29975),
        new("Germiston SAPS", SupportCategory.Police, "10 Long Street, Germiston, 1400", "0118715003", "https://www.saps.gov.za/contacts/stationdetails.php?sid=252", -26.21722, 28.16174),
        new("Primrose SAPS", SupportCategory.Police, "27 Poppy Street, Primrose, Germiston, 1401", "0118420522", "https://www.saps.gov.za/contacts/stationdetails.php?sid=262&sname=Primrose+%28Gauteng%29", -26.18914, 28.15795),
        new("Elsburg SAPS", SupportCategory.Police, "100 Nagington Road, Wadeville, Germiston, 1407", "0113838200", "https://www.saps.gov.za/contacts/stationdetails.php?sid=249&sname=Elsburg+%28Gauteng%29", -26.260441, 28.188860),
        new("Kidz Clinic Boksburg (WMACA)", SupportCategory.NgoNpo, "99 Market Street, Boksburg, 1459", "0118920404", "https://www.wmaca.org", -26.22236, 28.25799),
        new("Boksburg Child Welfare", SupportCategory.NgoNpo, "Corner Trichardt Road and Commissioner Street, Boksburg, 1459", "0119179544", "https://www.childwelfaresagauteng.org/team/boksburg/"),
        new("Childline Gauteng", SupportCategory.NgoNpo, "Children's Memorial Institute, 13 Joubert Street Extension, Braampark, Johannesburg, 2017", "0116452000", "https://childlinegauteng.co.za/contact-us/"),
        new("Frida Hartley Shelter for Women", SupportCategory.NgoNpo, "97 Regent Street, Bellevue, Johannesburg, 2198", "0116486005", "https://www.fridahartley.org/contact")
    ];
}
