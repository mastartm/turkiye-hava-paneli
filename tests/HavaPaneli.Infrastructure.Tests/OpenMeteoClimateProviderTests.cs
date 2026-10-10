using System.Globalization;
using System.Net;
using HavaPaneli.Domain;
using HavaPaneli.Infrastructure;

namespace HavaPaneli.Infrastructure.Tests;

public class OpenMeteoClimateProviderTests
{
    // Gerçek yanıt biçiminden kısaltılmış; ikinci günde null var.
    private const string SampleJson = """
        {"daily":{"time":["1991-01-01","1991-01-02","1991-01-03"],"temperature_2m_mean":[-1.5,null,0.25]}}
        """;

    private static readonly City Ankara = new("Ankara", Country.Turkiye, 39.93, 32.86);
    private static readonly DateOnly From = new(1991, 1, 1);
    private static readonly DateOnly To = new(2020, 12, 31);

    private static (OpenMeteoClimateProvider provider, StubHandler handler) Create(
        string body = SampleJson, HttpStatusCode status = HttpStatusCode.OK)
    {
        var handler = new StubHandler(status, body);
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://fake.test/") };
        return (new OpenMeteoClimateProvider(http), handler);
    }

    [Fact]
    public async Task Yaniti_Gunluk_Ortalamalara_Cevirir_Null_Korunur()
    {
        var (provider, _) = Create();

        var result = await provider.GetDailyMeansAsync(Ankara, From, To);

        Assert.Equal(3, result.Count);
        Assert.Equal(new DateOnly(1991, 1, 1), result[0].Date);
        Assert.Equal(-1.5, result[0].MeanTemperatureC);
        Assert.Null(result[1].MeanTemperatureC);
    }

    [Fact]
    public async Task Istek_Model_Ve_Donemi_Acikca_Icerir_Turkce_Kulturde_Bile()
    {
        var old = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("tr-TR");
        try
        {
            var (provider, handler) = Create();
            await provider.GetDailyMeansAsync(Ankara, From, To);

            var q = handler.LastRequest!.Query;
            Assert.Contains("start_date=1991-01-01", q);
            Assert.Contains("end_date=2020-12-31", q);
            Assert.Contains("models=era5_land", q);
            Assert.Contains("latitude=39.93", q);
        }
        finally { CultureInfo.CurrentCulture = old; }
    }

    [Fact]
    public async Task Ters_Tarih_Araligi_Reddedilir()
    {
        var (provider, _) = Create();

        await Assert.ThrowsAsync<ArgumentException>(() => provider.GetDailyMeansAsync(Ankara, To, From));
    }

    [Fact]
    public async Task Hata_Kodu_Istisna_Firlatir()
    {
        var (provider, _) = Create("{}", HttpStatusCode.TooManyRequests);

        await Assert.ThrowsAsync<HttpRequestException>(() => provider.GetDailyMeansAsync(Ankara, From, To));
    }

    [Fact]
    public async Task Beklenmeyen_Yanit_Anlasilir_Hata_Verir()
    {
        var (provider, _) = Create("""{"foo":1}""");

        await Assert.ThrowsAsync<InvalidOperationException>(() => provider.GetDailyMeansAsync(Ankara, From, To));
    }
}
