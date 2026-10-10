using System.Globalization;
using System.Text;
using System.Text.Json;
using HavaPaneli.Domain;

namespace HavaPaneli.Infrastructure;

/// <summary>
/// Hesaplanmış iklim normallerinin JSON dosya biçimi (repoda `data/climate-normals.json`).
/// Biçim bilerek sade: şehir başına tek satır, git farkları okunabilsin; değerler 0,01 °C'ye yuvarlanır.
/// </summary>
public static class ClimateNormalFile
{
    public const int FormatVersion = 1;
    public const string Period = "1991-2020";

    private const string Source = "Open-Meteo, ERA5-Land yeniden analizi (CC BY 4.0), 1991-2020 gunluk ortalama sicaklik, +-7 gun penceresi";

    /// <summary>Anahtar: şehir kimliği (slug). Şehirler kimliğe göre sıralı yazılır: aynı veri her zaman aynı dosyayı üretir.</summary>
    public static void Write(Stream stream, IReadOnlyDictionary<string, ClimateNormal> normals)
    {
        using var writer = new StreamWriter(stream, new UTF8Encoding(false), leaveOpen: true) { NewLine = "\n" };

        writer.WriteLine("{");
        writer.WriteLine($"\"version\":{FormatVersion},\"period\":\"{Period}\",\"unit\":\"C\",\"source\":{JsonSerializer.Serialize(Source)},");
        writer.WriteLine("\"cities\":{");

        var ordered = normals.OrderBy(kv => kv.Key, StringComparer.Ordinal).ToList();
        for (var i = 0; i < ordered.Count; i++)
        {
            var values = string.Join(",", ordered[i].Value.DailyMeansC.Select(Format));
            writer.WriteLine($"{JsonSerializer.Serialize(ordered[i].Key)}:[{values}]{(i < ordered.Count - 1 ? "," : "")}");
        }

        writer.WriteLine("}}");
    }

    // InvariantCulture: Türkçe kültürde ondalık ayraç virgüldür ve geçersiz JSON üretirdi.
    private static string Format(double value)
    {
        var rounded = Math.Round(value, 2);
        // Sıfıra yuvarlanan negatif sayı .NET'te "-0" yazılabilir; "-0" geçerli ama gereksiz gürültü.
        if (rounded == 0) rounded = 0;
        return rounded.ToString("0.##", CultureInfo.InvariantCulture);
    }

    /// <exception cref="InvalidDataException">Dosya biçimi, sürümü ya da içeriği geçersizse (anlaşılır mesajla).</exception>
    public static IReadOnlyDictionary<string, ClimateNormal> Read(Stream stream)
    {
        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(stream);
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException("Normaller dosyası geçerli JSON değil.", ex);
        }

        using (doc)
        {
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object
                || !root.TryGetProperty("version", out var version) || version.ValueKind != JsonValueKind.Number
                || version.GetInt32() != FormatVersion)
                throw new InvalidDataException($"Normaller dosyasının sürümü desteklenmiyor (beklenen: {FormatVersion}).");

            if (!root.TryGetProperty("cities", out var cities) || cities.ValueKind != JsonValueKind.Object)
                throw new InvalidDataException("Normaller dosyasında 'cities' alanı yok.");

            var result = new Dictionary<string, ClimateNormal>();
            foreach (var city in cities.EnumerateObject())
            {
                if (city.Value.ValueKind != JsonValueKind.Array || city.Value.GetArrayLength() != ClimateNormal.DaysInYear)
                    throw new InvalidDataException($"'{city.Name}' için tam {ClimateNormal.DaysInYear} değer bekleniyordu.");

                var values = new double[ClimateNormal.DaysInYear];
                var i = 0;
                foreach (var v in city.Value.EnumerateArray())
                {
                    if (v.ValueKind != JsonValueKind.Number)
                        throw new InvalidDataException($"'{city.Name}' içinde sayı olmayan bir değer var.");
                    values[i++] = v.GetDouble();
                }
                result[city.Name] = new ClimateNormal(values); // NaN'i ClimateNormal reddeder
            }
            return result;
        }
    }
}
