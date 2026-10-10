using HavaPaneli.Domain;

namespace HavaPaneli.Domain.Tests;

public class CityCatalogTests
{
    [Fact]
    public void Turkiye_81_Il_Icerir()
    {
        Assert.Equal(81, CityCatalog.All.Count(c => c.Country == Country.Turkiye));
    }

    [Fact]
    public void KKTC_6_Ilce_Icerir()
    {
        Assert.Equal(6, CityCatalog.All.Count(c => c.Country == Country.Kktc));
    }

    [Fact]
    public void Sehir_Adlari_Benzersiz()
    {
        var names = CityCatalog.All.Select(c => c.Name).ToList();

        Assert.Equal(names.Count, names.Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    // Kaba kutu kontrolü: yanlış işaretli ya da yer değiştirmiş (enlem/boylam ters) bir koordinatı yakalar.
    [Fact]
    public void Turkiye_Koordinatlari_Ulke_Kutusunda()
    {
        foreach (var c in CityCatalog.All.Where(c => c.Country == Country.Turkiye))
        {
            Assert.InRange(c.Latitude, 35.8, 42.2);
            Assert.InRange(c.Longitude, 25.6, 44.9);
        }
    }

    [Fact]
    public void KKTC_Koordinatlari_Ada_Kutusunda()
    {
        foreach (var c in CityCatalog.All.Where(c => c.Country == Country.Kktc))
        {
            Assert.InRange(c.Latitude, 35.0, 35.7);
            Assert.InRange(c.Longitude, 32.2, 34.7);
        }
    }

    [Fact]
    public void Turkce_Karakterler_Korunur()
    {
        var names = CityCatalog.All.Select(c => c.Name).ToHashSet();

        Assert.Contains("İstanbul", names);
        Assert.Contains("Iğdır", names);
        Assert.Contains("Şanlıurfa", names);
    }
}
