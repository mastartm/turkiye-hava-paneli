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
    public void Slug_Kimlikleri_Benzersiz_Ve_Sadece_ASCII_Kucuk_Harf()
    {
        var slugs = CityCatalog.All.Select(c => c.Slug).ToList();

        Assert.Equal(slugs.Count, slugs.Distinct().Count());
        Assert.All(slugs, s => Assert.Matches("^[a-z0-9-]+$", s));
    }

    [Theory]
    [InlineData("İstanbul", "istanbul")]
    [InlineData("Iğdır", "igdir")]
    [InlineData("Şanlıurfa", "sanliurfa")]
    [InlineData("Hakkâri", "hakkari")]
    [InlineData("Güzelyurt", "guzelyurt")]
    [InlineData("Çanakkale", "canakkale")]
    [InlineData("Kahramanmaraş", "kahramanmaras")]
    public void Slug_Turkce_Karakterleri_ASCII_Yapar(string name, string expected)
    {
        Assert.Equal(expected, new City(name, Country.Turkiye, 39, 35).Slug);
    }

    [Fact]
    public void Slug_Kultur_Bagimsiz_Calisir()
    {
        var old = System.Globalization.CultureInfo.CurrentCulture;
        System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("tr-TR");
        try
        {
            Assert.Equal("istanbul", new City("İstanbul", Country.Turkiye, 41, 29).Slug);
            Assert.Equal("isparta", new City("Isparta", Country.Turkiye, 37.76, 30.55).Slug);
        }
        finally { System.Globalization.CultureInfo.CurrentCulture = old; }
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
