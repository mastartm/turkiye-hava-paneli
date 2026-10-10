using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace HavaPaneli.Infrastructure.Persistence;

/// <summary>
/// Sadece `dotnet ef migrations add` komutu için. Migration üretmek veritabanına bağlanmaz,
/// bu yüzden bağlantı dizesi gerçek olmak zorunda değil; ortam değişkeni varsa onu kullanırız.
/// Repoda gerçek parola bulunmaz.
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var cs = Environment.GetEnvironmentVariable("ConnectionStrings__Default")
                 ?? "Host=localhost;Database=design_time_only";
        var options = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(cs).Options;
        return new AppDbContext(options);
    }
}
