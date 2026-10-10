using HavaPaneli.Application;
using HavaPaneli.Domain;
using HavaPaneli.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Bağlantı dizesi parola içerebilir: yalnızca yapılandırmadan (ortam değişkeni) gelir, değeri asla yazdırılmaz.
var connectionString = builder.Configuration.GetConnectionString("Default");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException("'ConnectionStrings__Default' ortam değişkeni (veya ConnectionStrings:Default ayarı) tanımlı değil.");

builder.Services.AddDbContextFactory<AppDbContext>(o => o.UseNpgsql(connectionString));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IReadOnlyList<City>>(CityCatalog.All);
builder.Services.AddSingleton<IClimateNormalStore, EfClimateNormalStore>();
builder.Services.AddSingleton<IForecastReader, EfForecastReader>();
builder.Services.AddSingleton<IEarthquakeReader, EfEarthquakeReader>();
builder.Services.AddSingleton<ForecastQueryService>();
builder.Services.AddSingleton<EarthquakeQueryService>();

builder.Services.AddControllers();
// Hatalar standart "problem details" JSON'u olarak döner; istisna ayrıntısı dışarı sızmaz.
builder.Services.AddProblemDetails();

// CORS: tarayıcıdaki arayüz farklı bir adresten (örn. localhost:5173) çağırırsa izin gerekir.
// İzinli adresler yapılandırmadan gelir; hiçbiri tanımlı değilse CORS kapalıdır (varsayılan güvenli).
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
if (allowedOrigins.Length > 0)
{
    builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.WithOrigins(allowedOrigins).WithMethods("GET").AllowAnyHeader()));
}

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();
if (allowedOrigins.Length > 0) app.UseCors();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapControllers();

app.Run();

// Testlerde WebApplicationFactory'nin Program sınıfını görebilmesi için.
public partial class Program;
