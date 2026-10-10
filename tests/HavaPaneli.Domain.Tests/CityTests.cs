using HavaPaneli.Domain;

namespace HavaPaneli.Domain.Tests;

public class CityTests
{
    [Fact]
    public void Gecerli_Koordinatla_Sehir_Olusur_Ve_Ad_Kirpilir()
    {
        var city = new City("  Ankara ", Country.Turkiye, 39.93, 32.86);

        Assert.Equal("Ankara", city.Name);
        Assert.Equal(Country.Turkiye, city.Country);
    }

    [Fact]
    public void KKTC_Ilcesi_Olusturulabilir()
    {
        var city = new City("Girne", Country.Kktc, 35.34, 33.32);

        Assert.Equal(Country.Kktc, city.Country);
    }

    [Theory]
    [InlineData(91, 30)]
    [InlineData(-91, 30)]
    [InlineData(40, 181)]
    [InlineData(40, -181)]
    [InlineData(double.NaN, 30)]
    public void Gecersiz_Koordinat_Reddedilir(double lat, double lon)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new City("X", Country.Turkiye, lat, lon));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Bos_Ad_Reddedilir(string name)
    {
        Assert.Throws<ArgumentException>(() => new City(name, Country.Turkiye, 40, 30));
    }
}
