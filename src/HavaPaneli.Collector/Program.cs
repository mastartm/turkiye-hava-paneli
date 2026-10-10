using HavaPaneli.Application;
using HavaPaneli.Domain;
using HavaPaneli.Infrastructure;
using HavaPaneli.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

// Toplayıcı: bir kez çalışır, tüm şehirlerin tahminini ve son depremleri çekip veritabanına yazar, çıkar.
// "Çalış ve bitir" modeli bilerek seçildi: zamanlanmış bir iş (GitHub Actions) bunu saatte bir çağırır;
// API uyusa bile veri akmaya devam eder (docs/ARCHITECTURE.md K-015).
//
// Çıkış kodları: 0 = hepsi tamam, 1 = bazı kaynaklar hata verdi (kısmi veri yazıldı), 2 = yapılandırma hatası.

var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Default");
if (string.IsNullOrWhiteSpace(connectionString))
{
    // Bağlantı dizesi parola içerebilir: değerini asla yazdırmıyoruz, sadece eksik olduğunu söylüyoruz.
    Console.Error.WriteLine("HATA: 'ConnectionStrings__Default' ortam değişkeni tanımlı değil.");
    return 2;
}

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

var dbOptions = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(connectionString).Options;
var dbFactory = new SimpleDbFactory(dbOptions);

// Toplayıcı şema sahibi: tablolar yoksa oluşturur, eksik migration varsa uygular.
await using (var db = dbFactory.CreateDbContext())
    await db.Database.MigrateAsync(cts.Token);

const string UserAgent = "turkiye-hava-paneli/0.1 (portfolio project)";
HttpClient NewClient(string baseAddress)
{
    var http = new HttpClient { BaseAddress = new Uri(baseAddress), Timeout = TimeSpan.FromSeconds(30) };
    http.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
    return http;
}

using var weatherHttp = NewClient("https://api.open-meteo.com/");
using var quakeHttp = NewClient("https://earthquake.usgs.gov/");

var service = new DataCollectionService(
    CityCatalog.All,
    new OpenMeteoWeatherProvider(weatherHttp),
    new UsgsEarthquakeProvider(quakeHttp),
    new EfWeatherStore(dbFactory),
    new EfEarthquakeStore(dbFactory),
    TimeProvider.System,
    delayBetweenCities: TimeSpan.FromMilliseconds(250));

Console.WriteLine($"Toplama başladı: {CityCatalog.All.Count} şehir + son {DataCollectionService.EarthquakeWindow.TotalDays:0} günün depremleri.");
var result = await service.RunAsync(cts.Token);

Console.WriteLine($"Hava durumu: {result.CitiesSucceeded}/{CityCatalog.All.Count} şehir saklandı.");
foreach (var f in result.FailedCities)
    Console.Error.WriteLine($"  HATA şehir -> {f}");
if (result.EarthquakesStored is { } stored)
    Console.WriteLine($"Deprem: {stored} kayıt saklandı.");
else
    Console.Error.WriteLine($"  HATA deprem -> {result.EarthquakeError}");

return result.HasFailures ? 1 : 0;

internal sealed class SimpleDbFactory(DbContextOptions<AppDbContext> options) : IDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext() => new(options);
}
