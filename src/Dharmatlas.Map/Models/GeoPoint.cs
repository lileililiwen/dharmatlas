namespace Dharmatlas.Map.Models;

/// <summary>A geographic coordinate (WGS84).</summary>
/// <param name="Latitude">Decimal degrees, north positive.</param>
/// <param name="Longitude">Decimal degrees, east positive.</param>
public sealed record GeoPoint(double Latitude, double Longitude);
