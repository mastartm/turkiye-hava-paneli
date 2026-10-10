using System.Globalization;
using System.Net;
using HavaPaneli.Domain;
using HavaPaneli.Infrastructure;

namespace HavaPaneli.Infrastructure.Tests;

public class UsgsEarthquakeProviderTests
{
    // Gerçek yanıttan kısaltılmış; ikinci kayıtta büyüklük null.
    private const string SampleJson = """
        {"type":"FeatureCollection","features":[
        {"type":"Feature","properties":{"mag":4.2,"place":"27 km WSW of Fry, Greece","time":1791330636679},
         "geometry":{"type":"Point","coordinates":[26.6477,35.3207,10]},"id":"us6000u0mf"},
        {"type":"Feature","properties":{"mag":null,"place":"x","time":1791330238171},
         "geometry":{"type":"Point","coordinates":[27.0,36.0,5]},"id":"us-null"},
        {"type":"Feature","properties":{"mag":3.1,"place":null,"time":1791330000000},
         "geometry":{"type":"Point","coordinates":[30.0,39.0]},"id":"us-nodepth"}
        ]}
        """;

    private static (UsgsEarthquakeProvider provider, StubHandler handler) Create(
        string body = SampleJson, HttpStatusCode status = HttpStatusCode.OK)
    {
        var handler = new StubHandler(status, body);
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://fake.test/") };
        return (new UsgsEarthquakeProvider(http), handler);
    }

    private static readonly DateTimeOffset Since = new(2026, 10, 3, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Yaniti_Depreme_Cevirir_Ve_Enlem_Boylami_Dogru_Okur()
    {
        var (provider, _) = Create();

        var result = await provider.GetRecentAsync(GeoBounds.TurkiyeAndKktc, Since, 2.5);

        var first = result[0];
        Assert.Equal("us6000u0mf", first.Id);
        Assert.Equal(4.2, first.Magnitude);
        Assert.Equal(35.3207, first.Latitude);   // GeoJSON'da ikinci eleman enlemdir
        Assert.Equal(26.6477, first.Longitude);  // birinci eleman boylamdır
        Assert.Equal(10, first.DepthKm);
        Assert.Equal(DateTimeOffset.FromUnixTimeMilliseconds(1791330636679), first.OccurredAt);
    }

    [Fact]
    public async Task Buyuklugu_Olmayan_Kayit_Atlanir()
    {
        var (provider, _) = Create();

        var result = await provider.GetRecentAsync(GeoBounds.TurkiyeAndKktc, Since, 2.5);

        Assert.DoesNotContain(result, e => e.Id == "us-null");
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task Derinlik_Ve_Yer_Adi_Yoksa_Null_Ve_Varsayilan_Metin()
    {
        var (provider, _) = Create();

        var result = await provider.GetRecentAsync(GeoBounds.TurkiyeAndKktc, Since, 2.5);

        var e = result.Single(x => x.Id == "us-nodepth");
        Assert.Null(e.DepthKm);
        Assert.Equal("Bilinmeyen yer", e.Place);
    }

    [Fact]
    public async Task Istek_Turkce_Kulturde_Bile_Noktali_Sayi_Ve_UTC_Zaman_Kullanir()
    {
        var old = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("tr-TR");
        try
        {
            var (provider, handler) = Create();
            await provider.GetRecentAsync(GeoBounds.TurkiyeAndKktc, Since, 2.5);

            var q = handler.LastRequest!.Query;
            Assert.Contains("minmagnitude=2.5", q);
            Assert.Contains("minlatitude=34.5", q);
            Assert.Contains("starttime=2026-10-03T00:00:00", q);
        }
        finally { CultureInfo.CurrentCulture = old; }
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(11)]
    public async Task Gecersiz_Buyukluk_Reddedilir(double mag)
    {
        var (provider, _) = Create();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => provider.GetRecentAsync(GeoBounds.TurkiyeAndKktc, Since, mag));
    }

    [Fact]
    public async Task Hata_Kodu_Istisna_Firlatir()
    {
        var (provider, _) = Create("{}", HttpStatusCode.ServiceUnavailable);

        await Assert.ThrowsAsync<HttpRequestException>(
            () => provider.GetRecentAsync(GeoBounds.TurkiyeAndKktc, Since, 2.5));
    }

    [Fact]
    public async Task Beklenmeyen_Yanit_Anlasilir_Hata_Verir()
    {
        var (provider, _) = Create("""{"foo":1}""");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => provider.GetRecentAsync(GeoBounds.TurkiyeAndKktc, Since, 2.5));
    }
}
