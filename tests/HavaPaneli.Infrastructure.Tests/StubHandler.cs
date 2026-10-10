using System.Net;
using System.Text;

namespace HavaPaneli.Infrastructure.Tests;

/// <summary>Gerçek ağa çıkmadan sabit bir HTTP yanıtı döndüren sahte işleyici; son isteğin adresini saklar.</summary>
internal sealed class StubHandler(HttpStatusCode status, string body) : HttpMessageHandler
{
    public Uri? LastRequest { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        LastRequest = request.RequestUri;
        return Task.FromResult(new HttpResponseMessage(status)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        });
    }
}
