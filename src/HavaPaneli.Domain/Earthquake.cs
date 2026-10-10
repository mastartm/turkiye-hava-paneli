namespace HavaPaneli.Domain;

/// <summary>Tek bir deprem kaydı. Id kaynağın kendi kimliğidir; aynı depremi iki kez saklamamak için kullanılır.</summary>
public sealed record Earthquake(
    string Id,
    DateTimeOffset OccurredAt,
    double Magnitude,
    string Place,
    double Latitude,
    double Longitude,
    double? DepthKm);

/// <summary>
/// Dikdörtgen bir bölge (koordinat kutusu). Deprem kaynağına "şu kutunun içindekileri ver" demek için kullanılır.
/// </summary>
public sealed record GeoBounds
{
    public double MinLatitude { get; }
    public double MaxLatitude { get; }
    public double MinLongitude { get; }
    public double MaxLongitude { get; }

    public GeoBounds(double minLatitude, double maxLatitude, double minLongitude, double maxLongitude)
    {
        if (minLatitude is < -90 or > 90 || maxLatitude is < -90 or > 90 || minLatitude >= maxLatitude)
            throw new ArgumentException("Enlem aralığı geçersiz: -90..90 içinde ve min < max olmalı.");
        if (minLongitude is < -180 or > 180 || maxLongitude is < -180 or > 180 || minLongitude >= maxLongitude)
            throw new ArgumentException("Boylam aralığı geçersiz: -180..180 içinde ve min < max olmalı.");

        MinLatitude = minLatitude;
        MaxLatitude = maxLatitude;
        MinLongitude = minLongitude;
        MaxLongitude = maxLongitude;
    }

    /// <summary>Türkiye, KKTC ve kıyıya yakın komşu bölgeleri kapsayan kutu.</summary>
    public static GeoBounds TurkiyeAndKktc { get; } = new(34.5, 42.5, 25.0, 45.0);
}
