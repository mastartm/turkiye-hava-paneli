namespace HavaPaneli.Domain;

public enum Country
{
    Turkiye,
    Kktc
}

/// <summary>
/// Haritada gösterilen bir il (Türkiye) ya da ilçe (KKTC). Hava durumu koordinatla sorgulandığı için
/// koordinat geçersizse nesne hiç oluşmaz: hatalı veri daha içeri girerken durdurulur.
/// </summary>
public sealed record City
{
    public string Name { get; }
    public Country Country { get; }
    public double Latitude { get; }
    public double Longitude { get; }

    public City(string name, Country country, double latitude, double longitude)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Şehir adı boş olamaz.", nameof(name));
        if (latitude is < -90 or > 90 || double.IsNaN(latitude))
            throw new ArgumentOutOfRangeException(nameof(latitude), "Enlem -90 ile 90 arasında olmalı.");
        if (longitude is < -180 or > 180 || double.IsNaN(longitude))
            throw new ArgumentOutOfRangeException(nameof(longitude), "Boylam -180 ile 180 arasında olmalı.");

        Name = name.Trim();
        Country = country;
        Latitude = latitude;
        Longitude = longitude;
    }
}
