using System.Globalization;
using System.Net;
using HavaPaneli.Domain;
using HavaPaneli.Infrastructure;

namespace HavaPaneli.Infrastructure.Tests;

public class OpenMeteoWeatherProviderTests
{
    // Gerçek API'den alınmış yanıtın kısaltılmışı; bir günde null var.
    private const string SampleJson = """
        {"daily":{"time":["2026-10-10","2026-10-11"],
        "temperature_2m_max":[24.1,25.7],"temperature_2m_min":[7.7,null],
        "temperature_2m_mean":[15.1,16.0],"precipitation_sum":[0.0,1.5],"weather_code":[0,61]}}
        """;

    private static readonly City Ankara = new("Ankara", Country.Turkiye, 39.93, 32.86);

    private static (OpenMeteoWeatherProvider provider, StubHandler handler) Create(
        string body = SampleJson, HttpStatusCode status = HttpStatusCode.OK)
    {
        var handler = new StubHandler(status, body);
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://fake.test/") };
        return (new OpenMeteoWeatherProvider(http), handler);
    }

    [Fact]
    public async Task Yaniti_Gunluk_Tahmine_Cevirir()
    {
        var (provider, _) = Create();

        var result = await provider.GetForecastAsync(Ankara, 2);

        Assert.Equal(2, result.Count);
        Assert.Equal(new DateOnly(2026, 10, 10), result[0].Date);
        Assert.Equal(24.1, result[0].MaxTemperatureC);
        Assert.Equal(61, result[1].WeatherCode);
    }

    [Fact]
    public async Task Null_Deger_Sifira_Cevrilmez()
    {
        var (provider, _) = Create();

        var result = await provider.GetForecastAsync(Ankara, 2);

        Assert.Null(result[1].MinTemperatureC);
    }

    [Fact]
    public async Task Istek_Turkce_Kulturde_Bile_Noktali_Koordinat_Kullanir()
    {
        var old = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("tr-TR");
        try
        {
            var (provider, handler) = Create();
            await provider.GetForecastAsync(Ankara, 2);

            Assert.Contains("latitude=39.93", handler.LastRequest!.Query);
            Assert.Contains("longitude=32.86", handler.LastRequest.Query);
        }
        finally { CultureInfo.CurrentCulture = old; }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(17)]
    public async Task Gecersiz_Gun_Sayisi_Reddedilir(int days)
    {
        var (provider, _) = Create();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => provider.GetForecastAsync(Ankara, days));
    }

    [Fact]
    public async Task Hata_Kodu_Istisna_Firlatir()
    {
        var (provider, _) = Create("{}", HttpStatusCode.TooManyRequests);

        await Assert.ThrowsAsync<HttpRequestException>(() => provider.GetForecastAsync(Ankara, 2));
    }

    [Fact]
    public async Task Beklenmeyen_Yanit_Anlasilir_Hata_Verir()
    {
        var (provider, _) = Create("""{"foo":1}""");

        await Assert.ThrowsAsync<InvalidOperationException>(() => provider.GetForecastAsync(Ankara, 2));
    }
}
