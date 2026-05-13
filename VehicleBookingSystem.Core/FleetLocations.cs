namespace VehicleBookingSystem.Core;

/// <summary>
/// Canonical Healthwise office locations.
/// Source: https://healthwise.org.au/contact/
/// ? Verify and update this list whenever the contact page changes.
/// </summary>
public static class FleetLocations
{
    // NSW locations
    public static readonly string[] All =
    [
        "Tamworth",
        "Armidale",
        "Inverell",
        "Moree",
        "Narrabri",
        "Gunnedah"
    ];

    // Coordinates for the Leaflet map (lat, lng)
    public static readonly Dictionary<string, (double Lat, double Lng)> Coordinates = new()
    {
        ["Tamworth"]  = (-31.0927, 150.9320),
        ["Armidale"]  = (-30.5131, 151.6672),
        ["Inverell"]  = (-29.7699, 151.1147),
        ["Moree"]     = (-29.4667, 149.8333),
        ["Narrabri"]  = (-30.3272, 149.7843),
        ["Gunnedah"]  = (-30.9853, 150.2572)
    };

    // Map centre and default zoom
    public const double MapCentreLatitude  = -30.3;
    public const double MapCentreLongitude = 150.7;
    public const int    MapZoom            = 8;
}

