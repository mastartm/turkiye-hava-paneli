using HavaPaneli.Application;
using HavaPaneli.Domain;

namespace HavaPaneli.Application.Tests;

public class DataCollectionServiceTests
{
    private static readonly City Ankara = new("Ankara", Country.Turkiye, 39.93, 32.86);
    private static readonly City Izmir = new("İzmir", Country.Turkiye, 38.42, 27.14);
    private static readonly City Girne = new("Girne", Country.Kktc, 35.34, 33.32);

    private sealed class FakeWeather(Func<City, IReadOnlyList<DailyForecast>> onCall) : IWeatherProvider
    {
        public List<string> Calls { get; } = [];
        public CurrentObservation? Current { get; set; }

        public Task<WeatherSnapshot> GetSnapshotAsync(City city, int days, CancellationToken ct = default)
        {
            Calls.Add($"{city.Name}:{days}");
            return Task.FromResult(new WeatherSnapshot(onCall(city), Current));
        }
    }

    private sealed class FakeQuakes(Func<IReadOnlyList<Earthquake>> onCall) : IEarthquakeProvider
    {
        public GeoBounds? LastBounds { get; private set; }
        public DateTimeOffset? LastSince { get; private set; }
        public double? LastMinMagnitude { get; private set; }

        public Task<IReadOnlyList<Earthquake>> GetRecentAsync(
            GeoBounds bounds, DateTimeOffset since, double minMagnitude, CancellationToken ct = default)
        {
            LastBounds = bounds; LastSince = since; LastMinMagnitude = minMagnitude;
            return Task.FromResult(onCall());
        }
    }

    private sealed class FakeWeatherStore : IWeatherStore
    {
        public List<(string City, int Count, DateTimeOffset At)> Saved { get; } = [];

        public Task UpsertForecastsAsync(City city, IReadOnlyList<DailyForecast> f, DateTimeOffset at, CancellationToken ct = default)
        {
            Saved.Add((city.Name, f.Count, at));
            return Task.CompletedTask;
        }
    }

    private sealed class FakeCurrentStore : ICurrentWeatherStore
    {
        public List<(string City, CurrentObservation Current, DateTimeOffset At)> Saved { get; } = [];

        public Task UpsertCurrentAsync(City city, CurrentObservation current, DateTimeOffset fetchedAt, CancellationToken ct = default)
        {
            Saved.Add((city.Name, current, fetchedAt));
            return Task.CompletedTask;
        }
    }

    private sealed class FakeQuakeStore : IEarthquakeStore
    {
        public int Received { get; private set; }

        public Task<int> UpsertAsync(IReadOnlyList<Earthquake> e, CancellationToken ct = default)
        {
            Received = e.Count;
            return Task.FromResult(e.Count);
        }
    }

    private static readonly DateTimeOffset Now = new(2026, 10, 10, 12, 0, 0, TimeSpan.Zero);

    private sealed class FixedTime(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private static DailyForecast Day(int d) => new(new DateOnly(2026, 10, d), 1, 2, 1.5, 0, 0);

    private static Earthquake Quake(string id) => new(id, Now, 3.0, "x", 39, 30, 10);

    private static DataCollectionService Create(
        IReadOnlyList<City> cities, FakeWeather weather, FakeQuakes quakes,
        FakeWeatherStore ws, FakeQuakeStore qs, FakeCurrentStore? cs = null) =>
        new(cities, weather, quakes, ws, cs ?? new FakeCurrentStore(), qs, new FixedTime(Now), TimeSpan.Zero);

    [Fact]
    public async Task Tum_Sehirler_Ve_Depremler_Saklanir()
    {
        var weather = new FakeWeather(_ => [Day(10), Day(11)]);
        var quakes = new FakeQuakes(() => [Quake("a"), Quake("b")]);
        var ws = new FakeWeatherStore();
        var qs = new FakeQuakeStore();

        var result = await Create([Ankara, Izmir, Girne], weather, quakes, ws, qs).RunAsync();

        Assert.Equal(3, result.CitiesSucceeded);
        Assert.Equal(2, result.EarthquakesStored);
        Assert.False(result.HasFailures);
        Assert.Equal(3, ws.Saved.Count);
        Assert.All(ws.Saved, s => Assert.Equal(Now, s.At));
        Assert.All(weather.Calls, c => Assert.EndsWith($":{DataCollectionService.ForecastDays}", c));
    }

    [Fact]
    public async Task Bir_Sehir_Hata_Verirse_Digerleri_Devam_Eder()
    {
        var weather = new FakeWeather(c => c.Name == "İzmir"
            ? throw new HttpRequestException("429")
            : [Day(10)]);
        var ws = new FakeWeatherStore();

        var result = await Create([Ankara, Izmir, Girne], weather, new FakeQuakes(() => []), ws, new FakeQuakeStore())
            .RunAsync();

        Assert.Equal(2, result.CitiesSucceeded);
        Assert.Single(result.FailedCities);
        Assert.Contains("İzmir", result.FailedCities[0]);
        Assert.True(result.HasFailures);
        Assert.DoesNotContain(ws.Saved, s => s.City == "İzmir");
    }

    [Fact]
    public async Task Deprem_Hatasi_Hava_Durumunu_Etkilemez()
    {
        var quakes = new FakeQuakes(() => throw new HttpRequestException("503"));
        var ws = new FakeWeatherStore();

        var result = await Create([Ankara], new FakeWeather(_ => [Day(10)]), quakes, ws, new FakeQuakeStore())
            .RunAsync();

        Assert.Equal(1, result.CitiesSucceeded);
        Assert.Null(result.EarthquakesStored);
        Assert.Equal("503", result.EarthquakeError);
        Assert.True(result.HasFailures);
    }

    [Fact]
    public async Task Deprem_Sorgusu_Dogru_Kutu_Sure_Ve_Buyuklukle_Yapilir()
    {
        var quakes = new FakeQuakes(() => []);

        await Create([Ankara], new FakeWeather(_ => []), quakes, new FakeWeatherStore(), new FakeQuakeStore())
            .RunAsync();

        Assert.Equal(GeoBounds.TurkiyeAndKktc, quakes.LastBounds);
        Assert.Equal(Now - DataCollectionService.EarthquakeWindow, quakes.LastSince);
        Assert.Equal(DataCollectionService.MinEarthquakeMagnitude, quakes.LastMinMagnitude);
    }

    private static readonly CurrentObservation SampleCurrent = new(new DateTimeOffset(2026, 10, 10, 15, 0, 0, TimeSpan.FromHours(3)), 28.4, 2, 0);

    [Fact]
    public async Task Su_Anki_Durum_Saklanir_Ve_Cekilme_Zamani_Yazilir()
    {
        var weather = new FakeWeather(_ => [Day(10)]) { Current = SampleCurrent };
        var cs = new FakeCurrentStore();

        await Create([Ankara, Izmir], weather, new FakeQuakes(() => []), new FakeWeatherStore(), new FakeQuakeStore(), cs).RunAsync();

        Assert.Equal(2, cs.Saved.Count);
        Assert.All(cs.Saved, s => Assert.Equal(Now, s.At)); // verinin ne zaman çekildiği (bayatlık bilgisi)
        Assert.Equal(28.4, cs.Saved[0].Current.TemperatureC);
    }

    [Fact]
    public async Task Su_Anki_Durum_Yoksa_Gunluk_Tahmin_Yine_Saklanir_Ve_Hata_Sayilmaz()
    {
        var weather = new FakeWeather(_ => [Day(10)]) { Current = null };
        var ws = new FakeWeatherStore();
        var cs = new FakeCurrentStore();

        var result = await Create([Ankara], weather, new FakeQuakes(() => []), ws, new FakeQuakeStore(), cs).RunAsync();

        Assert.Equal(1, result.CitiesSucceeded);
        Assert.False(result.HasFailures);
        Assert.Single(ws.Saved);
        Assert.Empty(cs.Saved);
    }

    [Fact]
    public async Task Iptal_Istegi_Hata_Olarak_Yutulmaz()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            Create([Ankara], new FakeWeather(_ => []), new FakeQuakes(() => []),
                new FakeWeatherStore(), new FakeQuakeStore()).RunAsync(cts.Token));
    }
}
