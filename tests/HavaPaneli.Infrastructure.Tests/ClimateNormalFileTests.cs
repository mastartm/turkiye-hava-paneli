using System.Globalization;
using System.Text;
using HavaPaneli.Domain;
using HavaPaneli.Infrastructure;

namespace HavaPaneli.Infrastructure.Tests;

public class ClimateNormalFileTests
{
    private static ClimateNormal Normal(Func<int, double> f) => new(Enumerable.Range(0, 366).Select(f).ToArray());

    private static string WriteToString(IReadOnlyDictionary<string, ClimateNormal> data)
    {
        using var ms = new MemoryStream();
        ClimateNormalFile.Write(ms, data);
        return Encoding.UTF8.GetString(ms.ToArray());
    }

    private static IReadOnlyDictionary<string, ClimateNormal> ReadFromString(string json)
    {
        using var ms = new MemoryStream(Encoding.UTF8.GetBytes(json));
        return ClimateNormalFile.Read(ms);
    }

    [Fact]
    public void Yazip_Okuyunca_Degerler_Yarim_Yuzde_Birlik_Hassasiyetle_Korunur()
    {
        var original = new Dictionary<string, ClimateNormal> { ["ankara"] = Normal(i => -5 + i * 0.0731) };

        var back = ReadFromString(WriteToString(original));

        Assert.Equal(366, back["ankara"].DailyMeansC.Count);
        for (var i = 0; i < 366; i++)
            Assert.Equal(original["ankara"].DailyMeansC[i], back["ankara"].DailyMeansC[i], 2); // 0,01 hassasiyet (yuvarlama)
    }

    [Fact]
    public void Sehirler_Kimlige_Gore_Siralanir_Ayni_Veri_Ayni_Dosyayi_Uretir()
    {
        var a = new Dictionary<string, ClimateNormal> { ["van"] = Normal(_ => 1), ["adana"] = Normal(_ => 2) };
        var b = new Dictionary<string, ClimateNormal> { ["adana"] = Normal(_ => 2), ["van"] = Normal(_ => 1) };

        var text = WriteToString(a);

        Assert.Equal(text, WriteToString(b));
        Assert.True(text.IndexOf("\"adana\"", StringComparison.Ordinal) < text.IndexOf("\"van\"", StringComparison.Ordinal));
    }

    [Fact]
    public void Her_Sehir_Kendi_Satirinda()
    {
        var text = WriteToString(new Dictionary<string, ClimateNormal> { ["a"] = Normal(_ => 1), ["b"] = Normal(_ => 2) });

        Assert.Equal(2, text.Split('\n').Count(l => l.StartsWith("\"a\":[") || l.StartsWith("\"b\":[")));
    }

    [Fact]
    public void Eksi_Sifir_Yazilmaz()
    {
        // -0.001 iki ondalığa yuvarlanınca negatif sıfır olur; "-0" yerine "0" yazılmalı.
        var text = WriteToString(new Dictionary<string, ClimateNormal> { ["a"] = Normal(_ => -0.001) });

        Assert.DoesNotContain("-0,", text);
        Assert.DoesNotContain("-0]", text);
        Assert.Contains("[0,0,0", text);
    }

    [Fact]
    public void Turkce_Kulturde_Bile_Noktali_Ondalik_Yazilir_Ve_Okunur()
    {
        var old = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("tr-TR");
        try
        {
            var text = WriteToString(new Dictionary<string, ClimateNormal> { ["a"] = Normal(_ => 12.34) });

            Assert.Contains("12.34", text);
            Assert.DoesNotContain("12,34", text);
            Assert.Equal(12.34, ReadFromString(text)["a"].DailyMeansC[0], 6);
        }
        finally { CultureInfo.CurrentCulture = old; }
    }

    [Fact]
    public void Bos_Liste_Yazilip_Okunabilir()
    {
        Assert.Empty(ReadFromString(WriteToString(new Dictionary<string, ClimateNormal>())));
    }

    [Fact]
    public void Gecersiz_JSON_Anlasilir_Hata_Verir()
    {
        Assert.Throws<InvalidDataException>(() => ReadFromString("{ bozuk"));
    }

    [Theory]
    [InlineData("""{"version":2,"cities":{}}""")]
    [InlineData("""{"cities":{}}""")]
    [InlineData("""{"version":"1","cities":{}}""")]
    [InlineData("""[]""")]
    public void Desteklenmeyen_Surum_Ya_Da_Yapi_Reddedilir(string json)
    {
        Assert.Throws<InvalidDataException>(() => ReadFromString(json));
    }

    [Fact]
    public void Cities_Alani_Yoksa_Reddedilir()
    {
        Assert.Throws<InvalidDataException>(() => ReadFromString("""{"version":1}"""));
    }

    [Fact]
    public void Yanlis_Uzunluktaki_Dizi_Reddedilir()
    {
        var json = "{\"version\":1,\"cities\":{\"ankara\":[" + string.Join(",", Enumerable.Repeat("1", 365)) + "]}}";

        var ex = Assert.Throws<InvalidDataException>(() => ReadFromString(json));
        Assert.Contains("ankara", ex.Message);
    }

    [Fact]
    public void Sayi_Olmayan_Deger_Reddedilir()
    {
        var values = string.Join(",", Enumerable.Repeat("1", 365)) + ",\"x\"";
        var json = "{\"version\":1,\"cities\":{\"ankara\":[" + values + "]}}";

        Assert.Throws<InvalidDataException>(() => ReadFromString(json));
    }
}
