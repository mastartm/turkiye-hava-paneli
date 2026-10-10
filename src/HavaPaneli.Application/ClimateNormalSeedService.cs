using HavaPaneli.Domain;

namespace HavaPaneli.Application;

/// <param name="Imported">Veritabanına yazılan şehir sayısı.</param>
/// <param name="MissingCities">Katalogda olup dosyada normali bulunmayan şehirler (yüklenemedi).</param>
/// <param name="UnknownIds">Dosyada olup katalogda karşılığı bulunmayan kimlikler (yok sayıldı).</param>
public sealed record SeedImportResult(int Imported, IReadOnlyList<string> MissingCities, IReadOnlyList<string> UnknownIds)
{
    public bool HasProblems => MissingCities.Count > 0;
}

/// <summary>
/// İklim normallerini dosyaya aktarmak ve dosyadan yüklemek için mantık. Normaller 30 yıllık veriden hesaplanır ve
/// Open-Meteo'nun ağırlık limitine takılır; hesaplanmış halini repoda tutup yüklemek, her kurulumda API'ye gitmeyi önler.
/// Dosyada şehirler adla değil <see cref="City.Slug"/> ile anahtarlanır: ASCII ve sabit, ad yazımındaki farklardan etkilenmez.
/// </summary>
public sealed class ClimateNormalSeedService
{
    private readonly IReadOnlyList<City> _cities;
    private readonly IClimateNormalStore _store;

    public ClimateNormalSeedService(IReadOnlyList<City> cities, IClimateNormalStore store)
    {
        _cities = cities;
        _store = store;
    }

    /// <returns>Veritabanında normali olan şehirler (anahtar: slug). Normali olmayan şehir dahil edilmez.</returns>
    public async Task<IReadOnlyDictionary<string, ClimateNormal>> ExportAsync(CancellationToken ct = default)
    {
        var all = await _store.GetAllAsync(ct);
        return _cities
            .Where(c => all.ContainsKey(c.Name))
            .ToDictionary(c => c.Slug, c => all[c.Name]);
    }

    /// <summary>
    /// Dosyadaki normalleri veritabanına yazar (varsa üzerine yazar: tekrar çalıştırmak zararsızdır).
    /// Katalogda olup dosyada olmayan şehirler hata olarak raporlanır; yüklenebilenler yine de yüklenir.
    /// </summary>
    public async Task<SeedImportResult> ImportAsync(IReadOnlyDictionary<string, ClimateNormal> data, CancellationToken ct = default)
    {
        var imported = 0;
        var missing = new List<string>();

        foreach (var city in _cities)
        {
            if (!data.TryGetValue(city.Slug, out var normal))
            {
                missing.Add(city.Name);
                continue;
            }
            await _store.UpsertAsync(city, normal, ct);
            imported++;
        }

        var known = _cities.Select(c => c.Slug).ToHashSet();
        var unknown = data.Keys.Where(k => !known.Contains(k)).OrderBy(k => k, StringComparer.Ordinal).ToList();
        return new SeedImportResult(imported, missing, unknown);
    }
}
