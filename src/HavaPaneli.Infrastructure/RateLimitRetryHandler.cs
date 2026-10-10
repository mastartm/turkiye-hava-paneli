using System.Net;

namespace HavaPaneli.Infrastructure;

/// <summary>
/// 429 (çok fazla istek) yanıtında, limit "dakikalık" ise bekleyip yeniden dener.
/// Open-Meteo limiti istek sayısına değil istek "ağırlığına" bakar: 30 yıllık günlük veri istemek
/// kısa bir tahminden çok daha pahalıdır, bu yüzden ardışık şehirlerde dakikalık limite takılırız.
/// Saatlik/günlük limitte beklemek işe yaramaz (saatler sürer), o durumda yanıt olduğu gibi döner ve çağıran hata görür.
/// </summary>
public sealed class RateLimitRetryHandler : DelegatingHandler
{
    private readonly int _maxRetries;
    private readonly TimeSpan _wait;
    private readonly Func<TimeSpan, CancellationToken, Task> _delay;

    public RateLimitRetryHandler(
        HttpMessageHandler inner,
        int maxRetries = 4,
        TimeSpan? wait = null,
        Func<TimeSpan, CancellationToken, Task>? delay = null) : base(inner)
    {
        _maxRetries = maxRetries;
        // Sunucu "bir dakika sonra dene" diyor; sınırda kalmamak için biraz fazla bekleriz.
        _wait = wait ?? TimeSpan.FromSeconds(65);
        _delay = delay ?? Task.Delay;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        for (var attempt = 0; ; attempt++)
        {
            var response = await base.SendAsync(request, cancellationToken);
            if (response.StatusCode != HttpStatusCode.TooManyRequests || attempt >= _maxRetries)
                return response;

            // ReadAsString içeriği belleğe alır; yanıt sonradan döndürülürse çağıran yine okuyabilir.
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!body.Contains("Minutely", StringComparison.OrdinalIgnoreCase))
                return response; // saatlik/günlük limit: beklemek çözmez

            response.Dispose();
            await _delay(_wait, cancellationToken);
        }
    }
}
