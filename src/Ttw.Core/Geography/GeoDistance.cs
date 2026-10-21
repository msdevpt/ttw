namespace Ttw.Core.Geography;

/// <summary>
/// Distance calculations between geographic coordinates.
/// </summary>
public static class GeoDistance
{
    /// <summary>Approximate earth radius in kilometres.</summary>
    private const double EarthRadiusKm = 6372.795477598;

    /// <summary>
    /// Calculates the great-circle distance between two coordinates, in kilometres.
    /// </summary>
    public static double HaversineKm(double lat1, double lon1, double lat2, double lon2)
    {
        double dLat = ToRadians(lat2 - lat1);
        double dLon = ToRadians(lon2 - lon1);

        double radLat1 = ToRadians(lat1);
        double radLat2 = ToRadians(lat2);

        double a = Haversin(dLat) + Math.Cos(radLat1) * Math.Cos(radLat2) * Haversin(dLon);
        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return EarthRadiusKm * c;
    }

    /// <summary>
    /// Converts degrees to radians the same way the Java implementation does,
    /// so the distances come out bit for bit identical.
    /// </summary>
    private static double ToRadians(double degrees) => degrees / 180.0 * Math.PI;

    private static double Haversin(double value) => Math.Pow(Math.Sin(value / 2), 2);
}
