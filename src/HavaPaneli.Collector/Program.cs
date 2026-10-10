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
    // Timeout tüm çağrıyı kapsar, yeniden deneme beklemeleri dahil: en kötü durumda 4 x 65 sn bekleme + istek süreleri sığsın.
    var http = new HttpClient(new RateLimitRetryHandler(new HttpClientHandler()))
    {
        BaseAddress = new Uri(baseAddress),
        Timeout = TimeSpan.FromMinutes(10)
    };
    http.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
    return http;
}

// --normals: 1991-2020 iklim normallerini hesaplayıp saklar (tek seferlik iş; hesaplanmış şehirler atlanır).
// --force ile hepsini yeniden hesaplar. Normal tahminler gibi sık değişmez, bu yüzden ayrı bir mod.
if (args.Contains("--normals"))
{
    using var archiveHttp = NewClient("https://archive-api.open-meteo.com/");
    var normals = new ClimateNormalService(
        CityCatalog.All,
        new OpenMeteoClimateProvider(archiveHttp),
        new EfClimateNormalStore(dbFactory),
        delayBetweenCities: TimeSpan.FromSeconds(1));

    Console.WriteLine($"İklim normali hesabı başladı ({ClimateNormalService.PeriodStart.Year}-{ClimateNormalService.PeriodEnd.Year}).");
    var nr = await normals.RunAsync(force: args.Contains("--force"), cts.Token);
    Console.WriteLine($"Normal: {nr.Computed} hesaplandı, {nr.Skipped} atlandı (zaten vardı).");
    foreach (var f in nr.Failed)
        Console.Error.WriteLine($"  HATA şehir -> {f}");
    return nr.HasFailures ? 1 : 0;
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
