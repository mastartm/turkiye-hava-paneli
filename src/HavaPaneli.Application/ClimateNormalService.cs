using HavaPaneli.Domain;

namespace HavaPaneli.Application;

/// <summary>Geçmiş günlük ortalama sıcaklıkları getiren kaynak (iklim normali için).</summary>
public interface IClimateProvider
{
    Task<IReadOnlyList<DailyMean>> GetDailyMeansAsync(
        City city, DateOnly from, DateOnly to, CancellationToken cancellationToken = default);
}

public interface IClimateNormalStore
{
    Task UpsertAsync(City city, ClimateNormal normal, CancellationToken cancellationToken = default);

    /// <returns>Şehrin normali; hiç hesaplanmadıysa null.</returns>
    Task<ClimateNormal?> GetAsync(string cityName, CancellationToken cancellationToken = default);

    /// <summary>Normali hesaplanmış tüm şehirler (anahtar: şehir adı). Harita özeti için tek sorguda alınır.</summary>
    Task<IReadOnlyDictionary<string, ClimateNormal>> GetAllAsync(CancellationToken cancellationToken = default);
}

public sealed record NormalsResult(int Computed, int Skipped, IReadOnlyList<string> Failed)
{
    public bool HasFailures => Failed.Count > 0;
}

/// <summary>
/// Her şehir için 1991-2020 iklim normalini hesaplayıp saklar. Normal değişmeyen bir veridir:
/// bir kez hesaplanır, sonra atlanır (zorlanmadıkça). Bir şehir hata verirse diğerleri devam eder.
/// </summary>
public sealed class ClimateNormalService
{
    // Dünya Meteoroloji Örgütü'nün standart iklim normali dönemi.
    public static readonly DateOnly PeriodStart = new(1991, 1, 1);
    public static readonly DateOnly PeriodEnd = new(2020, 12, 31);

    private readonly IReadOnlyList<City> _cities;
    private readonly IClimateProvider _provider;
    private readonly IClimateNormalStore _store;
    private readonly TimeSpan _delayBetweenCities;

    public ClimateNormalService(
        IReadOnlyList<City> cities, IClimateProvider provider, IClimateNormalStore store, TimeSpan delayBetweenCities)
    {
        _cities = cities;
        _provider = provider;
        _store = store;
        _delayBetweenCities = delayBetweenCities;
    }

    public async Task<NormalsResult> RunAsync(bool force = false, CancellationToken cancellationToken = default)
    {
        var computed = 0;
        var skipped = 0;
        var failed = new List<string>();

        foreach (var city in _cities)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                if (!force && await _store.GetAsync(city.Name, cancellationToken) is not null)
                {
                    skipped++;
                    continue; // zaten var: dış API'ye gitmeden atla
                }

                var history = await _provider.GetDailyMeansAsync(city, PeriodStart, PeriodEnd, cancellationToken);
                var normal = ClimateNormalCalculator.Compute(history);
                await _store.UpsertAsync(city, normal, cancellationToken);
                computed++;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                failed.Add($"{city.Name}: {ex.Message}");
            }

            if (_delayBetweenCities > TimeSpan.Zero)
                await Task.Delay(_delayBetweenCities, cancellationToken);
        }

        return new NormalsResult(computed, skipped, failed);
    }
}
