using HavaPaneli.Application;
using HavaPaneli.Domain;
using Microsoft.EntityFrameworkCore;

namespace HavaPaneli.Infrastructure.Persistence;

public sealed class EfClimateNormalStore(IDbContextFactory<AppDbContext> factory) : IClimateNormalStore
{
    public async Task UpsertAsync(City city, ClimateNormal normal, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);

        var cityRow = await CityRows.GetOrCreateAsync(db, city, ct);

        var row = await db.ClimateNormals.SingleOrDefaultAsync(n => n.CityId == cityRow.Id, ct);
        if (row is null)
        {
            row = new ClimateNormalRow { CityId = cityRow.Id };
            db.ClimateNormals.Add(row);
        }
        row.DailyMeansC = normal.DailyMeansC.ToArray();
        await db.SaveChangesAsync(ct);
    }

    public async Task<ClimateNormal?> GetAsync(string cityName, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);

        var values = await db.ClimateNormals
            .Where(n => db.Cities.Any(c => c.Id == n.CityId && c.Name == cityName))
            .Select(n => n.DailyMeansC)
            .SingleOrDefaultAsync(ct);

        return values is null ? null : new ClimateNormal(values);
    }
}

/// <summary>Şehri adından bulur, yoksa oluşturur; koordinatı güncel tutar. Birden fazla saklama sınıfı kullanır.</summary>
internal static class CityRows
{
    public static async Task<CityRow> GetOrCreateAsync(AppDbContext db, City city, CancellationToken ct)
    {
        var row = await db.Cities.SingleOrDefaultAsync(c => c.Name == city.Name, ct);
        if (row is null)
        {
            row = new CityRow { Name = city.Name };
            db.Cities.Add(row);
        }
        row.Country = city.Country.ToString();
        row.Latitude = city.Latitude;
        row.Longitude = city.Longitude;
        await db.SaveChangesAsync(ct); // Yeni şehrin Id'si oluşsun diye hemen kaydet.
        return row;
    }
}
