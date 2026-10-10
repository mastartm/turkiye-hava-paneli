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

    /// <summary>
    /// URL'de kullanılan kimlik: küçük harf, ASCII ("İstanbul" → "istanbul", "Şanlıurfa" → "sanliurfa").
    /// Türkçe karakterli adresler tarayıcıda ve elle yazarken sorun çıkarır; ASCII kimlik güvenli ve tahmin edilebilir.
    /// </summary>
    public string Slug => ToSlug(Name);

    public static string ToSlug(string name)
    {
        var sb = new System.Text.StringBuilder(name.Length);
        foreach (var ch in name.Trim())
        {
            // Büyük/küçük harf dönüşümüne güvenmiyoruz (İ/I/ı kültüre göre farklı çalışır): her Türkçe harfi açıkça eşleriz.
            sb.Append(ch switch
            {
                'ç' or 'Ç' => 'c',
                'ğ' or 'Ğ' => 'g',
                'ı' or 'İ' or 'I' or 'i' => 'i',
                'ö' or 'Ö' => 'o',
                'ş' or 'Ş' => 's',
                'ü' or 'Ü' => 'u',
                'â' or 'Â' => 'a',
                'î' or 'Î' => 'i',
                'û' or 'Û' => 'u',
                ' ' => '-',
                _ => char.ToLowerInvariant(ch)
            });
        }
        return sb.ToString();
    }

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
