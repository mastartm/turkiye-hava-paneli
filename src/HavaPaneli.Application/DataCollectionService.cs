using HavaPaneli.Domain;

namespace HavaPaneli.Application;

/// <param name="CitiesSucceeded">Tahmini çekilip saklanan şehir sayısı.</param>
/// <param name="FailedCities">Hata veren şehirlerin adları ve kısa hata metni.</param>
/// <param name="EarthquakesStored">Saklanan deprem sayısı; deprem çekimi hata verdiyse null.</param>
/// <param name="EarthquakeError">Deprem hatası varsa metni.</param>
public sealed record CollectionResult(
    int CitiesSucceeded,
    IReadOnlyList<string> FailedCities,
    int? EarthquakesStored,
    string? EarthquakeError)
{
    public bool HasFailures => FailedCities.Count > 0 || EarthquakeError is not null;
}

/// <summary>
/// "Tüm şehirlerin tahminini ve son depremleri çek, sakla" işini yürütür.
/// Bir şehir ya da deprem kaynağı hata verirse diğerleri devam eder: kısmi veri, hiç veri olmamasından iyidir.
/// </summary>
public sealed class DataCollectionService
{
    public const int ForecastDays = 7;
    public const double MinEarthquakeMagnitude = 2.5;
    public static readonly TimeSpan EarthquakeWindow = TimeSpan.FromDays(7);

    private readonly IReadOnlyList<City> _cities;
    private readonly IWeatherProvider _weather;
    private readonly IEarthquakeProvider _earthquakes;
    private readonly IWeatherStore _weatherStore;
    private readonly IEarthquakeStore _earthquakeStore;
    private readonly TimeProvider _time;
    private readonly TimeSpan _delayBetweenCities;

    public DataCollectionService(
        IReadOnlyList<City> cities,
        IWeatherProvider weather,
        IEarthquakeProvider earthquakes,
        IWeatherStore weatherStore,
        IEarthquakeStore earthquakeStore,
        TimeProvider time,
        TimeSpan delayBetweenCities)
    {
        _cities = cities;
        _weather = weather;
        _earthquakes = earthquakes;
        _weatherStore = weatherStore;
        _earthquakeStore = earthquakeStore;
        _time = time;
        _delayBetweenCities = delayBetweenCities;
    }

    public async Task<CollectionResult> RunAsync(CancellationToken cancellationToken = default)
    {
        var now = _time.GetUtcNow();
        var failed = new List<string>();
        var succeeded = 0;

        foreach (var city in _cities)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var forecasts = await _weather.GetForecastAsync(city, ForecastDays, cancellationToken);
                await _weatherStore.UpsertForecastsAsync(city, forecasts, now, cancellationToken);
                succeeded++;
            }
            // İptal istisnasını yutmamak için filtre: sadece gerçek hataları kaydedip devam ederiz.
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                failed.Add($"{city.Name}: {ex.Message}");
            }

            // Ücretsiz API'ye kibar davranmak için istekler arasında kısa bekleme.
            if (_delayBetweenCities > TimeSpan.Zero)
                await Task.Delay(_delayBetweenCities, _time, cancellationToken);
        }

        int? stored = null;
        string? quakeError = null;
        try
        {
            var quakes = await _earthquakes.GetRecentAsync(
                GeoBounds.TurkiyeAndKktc, now - EarthquakeWindow, MinEarthquakeMagnitude, cancellationToken);
            stored = await _earthquakeStore.UpsertAsync(quakes, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            quakeError = ex.Message;
        }

        return new CollectionResult(succeeded, failed, stored, quakeError);
    }
}
