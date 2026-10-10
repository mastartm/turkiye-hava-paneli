using System.Net;
using System.Text;
using HavaPaneli.Infrastructure;

namespace HavaPaneli.Infrastructure.Tests;

public class RateLimitRetryHandlerTests
{
    private const string Minutely = """{"error":true,"reason":"Minutely API request limit exceeded. Please try again in one minute."}""";
    private const string Daily = """{"error":true,"reason":"Daily API request limit exceeded. Please try again tomorrow."}""";

    /// <summary>Sırayla verilen yanıtları döndüren sahte iç işleyici; kaç istek geldiğini sayar.</summary>
    private sealed class SequenceHandler(params (HttpStatusCode Status, string Body)[] responses) : HttpMessageHandler
    {
        public int Calls { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var (status, body) = responses[Math.Min(Calls, responses.Length - 1)];
            Calls++;
            return Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") });
        }
    }

    private static (HttpClient client, SequenceHandler inner, List<TimeSpan> waits) Create(
        int maxRetries, params (HttpStatusCode, string)[] responses)
    {
        var inner = new SequenceHandler(responses);
        var waits = new List<TimeSpan>();
        // Gerçek beklemek yerine süreyi kaydeden sahte bekleme: test anında biter.
        var handler = new RateLimitRetryHandler(inner, maxRetries, TimeSpan.FromSeconds(65),
            (t, _) => { waits.Add(t); return Task.CompletedTask; });
        return (new HttpClient(handler) { BaseAddress = new Uri("https://fake.test/") }, inner, waits);
    }

    [Fact]
    public async Task Basarili_Istekte_Beklemez()
    {
        var (client, inner, waits) = Create(4, (HttpStatusCode.OK, "{}"));

        var r = await client.GetAsync("x");

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Equal(1, inner.Calls);
        Assert.Empty(waits);
    }

    [Fact]
    public async Task Dakikalik_Limitte_Bekler_Ve_Yeniden_Dener()
    {
        var (client, inner, waits) = Create(4,
            (HttpStatusCode.TooManyRequests, Minutely), (HttpStatusCode.OK, """{"ok":1}"""));

        var r = await client.GetAsync("x");

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Equal("""{"ok":1}""", await r.Content.ReadAsStringAsync());
        Assert.Equal(2, inner.Calls);
        Assert.Equal([TimeSpan.FromSeconds(65)], waits);
    }

    [Fact]
    public async Task Gunluk_Limitte_Beklemez_Yaniti_Aynen_Doner()
    {
        var (client, inner, waits) = Create(4, (HttpStatusCode.TooManyRequests, Daily));

        var r = await client.GetAsync("x");

        Assert.Equal(HttpStatusCode.TooManyRequests, r.StatusCode);
        Assert.Contains("Daily", await r.Content.ReadAsStringAsync()); // içerik hâlâ okunabilir
        Assert.Equal(1, inner.Calls);
        Assert.Empty(waits);
    }

    [Fact]
    public async Task Surekli_429_Verirse_En_Fazla_Deneme_Sayisinda_Vazgecer()
    {
        var (client, inner, waits) = Create(2, (HttpStatusCode.TooManyRequests, Minutely));

        var r = await client.GetAsync("x");

        Assert.Equal(HttpStatusCode.TooManyRequests, r.StatusCode);
        Assert.Equal(3, inner.Calls); // ilk istek + 2 yeniden deneme
        Assert.Equal(2, waits.Count);
    }

    [Fact]
    public async Task Diger_Hata_Kodlarinda_Yeniden_Denemez()
    {
        var (client, inner, _) = Create(4, (HttpStatusCode.InternalServerError, "{}"));

        var r = await client.GetAsync("x");

        Assert.Equal(HttpStatusCode.InternalServerError, r.StatusCode);
        Assert.Equal(1, inner.Calls);
    }
}
