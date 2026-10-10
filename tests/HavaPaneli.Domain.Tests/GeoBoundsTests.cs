using HavaPaneli.Domain;

namespace HavaPaneli.Domain.Tests;

public class GeoBoundsTests
{
    [Fact]
    public void Gecerli_Kutu_Olusur()
    {
        var b = new GeoBounds(34.5, 42.5, 25, 45);

        Assert.Equal(34.5, b.MinLatitude);
        Assert.Equal(45, b.MaxLongitude);
    }

    [Fact]
    public void Hazir_Turkiye_Kutusu_Tum_Sehirleri_Kapsar()
    {
        var b = GeoBounds.TurkiyeAndKktc;

        Assert.All(CityCatalog.All, c =>
        {
            Assert.InRange(c.Latitude, b.MinLatitude, b.MaxLatitude);
            Assert.InRange(c.Longitude, b.MinLongitude, b.MaxLongitude);
        });
    }

    [Theory]
    [InlineData(42, 34, 25, 45)]   // min enlem > max enlem
    [InlineData(34, 42, 45, 25)]   // min boylam > max boylam
    [InlineData(-91, 42, 25, 45)]
    [InlineData(34, 42, 25, 181)]
    public void Gecersiz_Kutu_Reddedilir(double minLat, double maxLat, double minLon, double maxLon)
    {
        Assert.Throws<ArgumentException>(() => new GeoBounds(minLat, maxLat, minLon, maxLon));
    }
}
