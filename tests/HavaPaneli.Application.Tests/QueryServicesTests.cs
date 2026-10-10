using HavaPaneli.Application;
using HavaPaneli.Domain;

namespace HavaPaneli.Application.Tests;

public class QueryServicesTests
{
    private static readonly City Ankara = new("Ankara", Country.Turkiye, 39.93, 32.86);
    private static readonly City Istanbul = new("İstanbul", Country.Turkiye, 41.01, 28.98);
    private static readonly City Girne = new("Girne", Country.Kktc, 35.34, 33.32);

    // 10 Ekim 2026 12:00 UTC = Türkiye saatiyle 15:00, aynı gün.
    private static readonly DateTimeOffset Now = new(2026, 10, 10, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = new(2026, 10, 10);

    private sealed class FixedTime(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class FakeForecasts(List<(string City, DailyForecast F)> data) : IForecastReader
    {
        public DateOnly? LastFrom { get; private set; }
        public int? LastDays { get; private set; }

        public Task<IReadOnlyList<DailyForecast>> GetForCityAsync(string cityName, DateOnly from, int days, CancellationToken ct = default)
        {
            LastFrom = from; LastDays = days;
            return Task.FromResult<IReadOnlyList<DailyForecast>>(
                data.Where(x => x.City == cityName && x.F.Date >= from).Select(x => x.F).ToList());
        }

        public Task<IReadOnlyDictionary<string, DailyForecast>> GetForDateAsync(DateOnly date, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyDictionary<string, DailyForecast>>(
                data.Where(x => x.F.Date == date).ToDictionary(x => x.City, x => x.F));
    }

    private sealed class FakeNormals(Dictionary<string, ClimateNormal> data) : IClimateNormalStore
    {
        public Task UpsertAsync(City city, ClimateNormal normal, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<ClimateNormal?> GetAsync(string cityName, CancellationToken ct = default) =>
            Task.FromResult(data.GetValueOrDefault(cityName));
        public Task<IReadOnlyDictionary<string, ClimateNormal>> GetAllAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyDictionary<string, ClimateNormal>>(data);
    }

    private static ClimateNormal ConstantNormal(double v) => new(Enumerable.Repeat(v, 366).ToArray());

    private static DailyForecast Day(DateOnly d, double? mean) => new(d, 5, 15, mean, 0.2, 3);

    private static ForecastQueryService Create(
        List<(string, DailyForecast)> forecasts, Dictionary<string, ClimateNormal> normals, FakeForecasts? reader = null) =>
        new([Ankara, Istanbul, Girne], reader ?? new FakeForecasts(forecasts), new FakeNormals(normals), new FixedTime(Now));

    // ---- Şehir bulma ----

    [Theory]
    [InlineData("istanbul")]
    [InlineData("İstanbul")]
    [InlineData("ISTANBUL")]
    public void Sehir_Kimlikten_Buyuk_Kucuk_Ve_Turkce_Fark_Etmeden_Bulunur(string input)
    {
        Assert.Equal("İstanbul", Create([], []).FindCity(input)?.Name);
    }

    [Fact]
    public void Bilinmeyen_Sehir_Null_Doner()
    {
        Assert.Null(Create([], []).FindCity("atlantis"));
    }

    // ---- Şehir tahmini ----

    [Fact]
    public async Task Bilinmeyen_Sehir_Icin_Null()
    {
        Assert.Null(await Create([], []).GetCityForecastAsync("atlantis"));
    }

    [Fact]
    public async Task Tahmin_Gunleri_Tarihe_Gore_Sirali_Ve_Anomali_Hesapli()
    {
        var data = new List<(string, DailyForecast)>
        {
            ("Ankara", Day(Today.AddDays(1), 14)),
            ("Ankara", Day(Today, 12)),
        };

        var view = await Create(data, new() { ["Ankara"] = ConstantNormal(10) }).GetCityForecastAsync("ankara");

        Assert.NotNull(view);
        Assert.Equal([Today, Today.AddDays(1)], view!.Days.Select(d => d.Date));
        Assert.Equal(10, view.Days[0].NormalC);
        Assert.Equal(2, view.Days[0].AnomalyC!.Value, 6);   // 12 - 10
        Assert.Equal(4, view.Days[1].AnomalyC!.Value, 6);   // 14 - 10
    }

    [Fact]
    public async Task Normal_Yoksa_Anomali_Null_Ama_Tahmin_Doner()
    {
        var data = new List<(string, DailyForecast)> { ("Ankara", Day(Today, 12)) };

        var view = await Create(data, []).GetCityForecastAsync("ankara");

        Assert.Single(view!.Days);
        Assert.Null(view.Days[0].NormalC);
        Assert.Null(view.Days[0].AnomalyC);
        Assert.Equal(12, view.Days[0].MeanC);
    }

    [Fact]
    public async Task Ortalama_Yoksa_Anomali_Null()
    {
        var data = new List<(string, DailyForecast)> { ("Ankara", Day(Today, null)) };

        var view = await Create(data, new() { ["Ankara"] = ConstantNormal(10) }).GetCityForecastAsync("ankara");

        Assert.Null(view!.Days[0].AnomalyC);
    }

    [Fact]
    public async Task Sehir_Var_Ama_Veri_Yoksa_Bos_Liste()
    {
        var view = await Create([], []).GetCityForecastAsync("ankara");

        Assert.NotNull(view);
        Assert.Empty(view!.Days);
    }

    [Fact]
    public async Task Sorgu_Bugunden_Baslar_Ve_7_Gun_Ister()
    {
        var reader = new FakeForecasts([]);

        await Create([], [], reader).GetCityForecastAsync("ankara");

        Assert.Equal(Today, reader.LastFrom);
        Assert.Equal(7, reader.LastDays);
    }

    [Fact]
    public async Task Bugun_Turkiye_Saatine_Gore_Hesaplanir()
    {
        // 22:30 UTC = Türkiye'de ertesi gün 01:30. "Bugün" 11 Ekim olmalı.
        var late = new DateTimeOffset(2026, 10, 10, 22, 30, 0, TimeSpan.Zero);
        var reader = new FakeForecasts([]);
        var svc = new ForecastQueryService([Ankara], reader, new FakeNormals([]), new FixedTime(late));

        await svc.GetCityForecastAsync("ankara");

        Assert.Equal(new DateOnly(2026, 10, 11), reader.LastFrom);
    }

    // ---- Harita özeti ----

    [Fact]
    public async Task Harita_Tum_Sehirleri_Listeler_Verisiz_Olanlar_Bos_Degerli()
    {
        var data = new List<(string, DailyForecast)> { ("Ankara", Day(Today, 12)) };

        var map = await Create(data, new() { ["Ankara"] = ConstantNormal(10) }).GetMapAsync();

        Assert.Equal(Today, map.Date);
        Assert.Equal(3, map.Points.Count);
        var ankara = map.Points.Single(p => p.City.Name == "Ankara");
        Assert.Equal(2, ankara.AnomalyC!.Value, 6);
        var girne = map.Points.Single(p => p.City.Name == "Girne");
        Assert.Null(girne.MeanC);
        Assert.Null(girne.AnomalyC);
    }

    [Fact]
    public async Task Harita_Sadece_Bugunun_Verisini_Kullanir()
    {
        var data = new List<(string, DailyForecast)>
        {
            ("Ankara", Day(Today.AddDays(1), 30)), // yarın: haritada görünmemeli
        };

        var map = await Create(data, []).GetMapAsync();

        Assert.Null(map.Points.Single(p => p.City.Name == "Ankara").MeanC);
    }

    // ---- Deprem sorgusu ----

    private sealed class FakeQuakeReader : IEarthquakeReader
    {
        public DateTimeOffset? LastSince { get; private set; }
        public double? LastMin { get; private set; }
        public int? LastLimit { get; private set; }

        public Task<IReadOnlyList<Earthquake>> GetRecentAsync(DateTimeOffset since, double minMagnitude, int limit, CancellationToken ct = default)
        {
            LastSince = since; LastMin = minMagnitude; LastLimit = limit;
            return Task.FromResult<IReadOnlyList<Earthquake>>([]);
        }
    }

    [Fact]
    public async Task Deprem_Sorgusu_Dogru_Sure_Buyukluk_Ve_Limit_Iletir()
    {
        var reader = new FakeQuakeReader();

        await new EarthquakeQueryService(reader, new FixedTime(Now)).GetRecentAsync(7, 3.0);

        Assert.Equal(Now.AddDays(-7), reader.LastSince);
        Assert.Equal(3.0, reader.LastMin);
        Assert.Equal(EarthquakeQueryService.MaxResults, reader.LastLimit);
    }

    [Theory]
    [InlineData(0, 2.5)]
    [InlineData(31, 2.5)]
    [InlineData(7, -1)]
    [InlineData(7, 11)]
    [InlineData(7, double.NaN)]
    public async Task Deprem_Sorgusu_Gecersiz_Parametreyi_Reddeder(int days, double mag)
    {
        var svc = new EarthquakeQueryService(new FakeQuakeReader(), new FixedTime(Now));

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => svc.GetRecentAsync(days, mag));
    }
}
