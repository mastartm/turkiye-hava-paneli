using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HavaPaneli.Domain;
using HavaPaneli.Infrastructure.Persistence;

namespace HavaPaneli.Api.Tests;

[Collection("postgres")]
public class ApiEndpointsTests(PostgresFixture pg)
{
    // 10 Ekim 2026 12:00 UTC = Türkiye saatiyle 15:00, aynı gün.
    private static readonly DateTimeOffset Now = new(2026, 10, 10, 12, 0, 0, TimeSpan.Zero);
    private static readonly City Ankara = CityCatalog.All.Single(c => c.Name == "Ankara");
    private static readonly City Girne = CityCatalog.All.Single(c => c.Name == "Girne");

    private static DailyForecast Day(int d, double mean) => new(new DateOnly(2026, 10, d), 5, 15, mean, 0.2, 3);

    private static ClimateNormal Constant(double v) => new(Enumerable.Repeat(v, 366).ToArray());

    private static async Task<JsonElement> Json(HttpResponseMessage r) =>
        (await r.Content.ReadFromJsonAsync<JsonElement>());

    // ---- Sağlık ve kaynaklar ----

    [Fact]
    public async Task Health_200_Doner()
    {
        await using var api = await pg.CreateApiAsync(Now);

        var r = await api.Client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
    }

    [Fact]
    public async Task Kaynaklar_Atif_Bilgisini_Icerir()
    {
        await using var api = await pg.CreateApiAsync(Now);

        var json = await Json(await api.Client.GetAsync("/api/sources"));

        var names = json.EnumerateArray().Select(s => s.GetProperty("name").GetString()).ToList();
        Assert.Contains("Open-Meteo", names);
        Assert.Contains("CC BY 4.0", json.EnumerateArray().Select(s => s.GetProperty("license").GetString()));
    }

    // ---- Şehirler ----

    [Fact]
    public async Task Sehir_Listesi_87_Sehir_Ve_ASCII_Kimlik_Doner()
    {
        await using var api = await pg.CreateApiAsync(Now);

        var json = await Json(await api.Client.GetAsync("/api/cities"));

        Assert.Equal(87, json.GetArrayLength());
        var istanbul = json.EnumerateArray().Single(c => c.GetProperty("id").GetString() == "istanbul");
        Assert.Equal("İstanbul", istanbul.GetProperty("name").GetString());
        Assert.Equal("Turkiye", istanbul.GetProperty("country").GetString());
        Assert.Equal(6, json.EnumerateArray().Count(c => c.GetProperty("country").GetString() == "Kktc"));
    }

    [Fact]
    public async Task Tahmin_Gunler_Siralanir_Ve_Anomali_Doner()
    {
        await using var api = await pg.CreateApiAsync(Now);
        await new EfWeatherStore(api.Db).UpsertForecastsAsync(Ankara, [Day(11, 14), Day(10, 12), Day(9, 99)], Now);
        await new EfClimateNormalStore(api.Db).UpsertAsync(Ankara, Constant(10));

        var r = await api.Client.GetAsync("/api/cities/ankara/forecast");
        var json = await Json(r);

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Equal("ankara", json.GetProperty("city").GetProperty("id").GetString());
        var days = json.GetProperty("days").EnumerateArray().ToList();
        Assert.Equal(2, days.Count); // 9 Ekim (dün) hariç
        Assert.Equal("2026-10-10", days[0].GetProperty("date").GetString()); // DateOnly → "yyyy-MM-dd"
        Assert.Equal(2.0, days[0].GetProperty("anomalyC").GetDouble(), 6);
        Assert.Equal(10.0, days[0].GetProperty("normalC").GetDouble(), 6);
        Assert.Equal(4.0, days[1].GetProperty("anomalyC").GetDouble(), 6);
    }

    [Fact]
    public async Task Normal_Yoksa_Anomali_Null_Doner_Ve_Tahmin_Yine_Gelir()
    {
        await using var api = await pg.CreateApiAsync(Now);
        await new EfWeatherStore(api.Db).UpsertForecastsAsync(Ankara, [Day(10, 12)], Now);

        var json = await Json(await api.Client.GetAsync("/api/cities/ankara/forecast"));

        var day = json.GetProperty("days")[0];
        Assert.Equal(12.0, day.GetProperty("meanC").GetDouble());
        Assert.Equal(JsonValueKind.Null, day.GetProperty("anomalyC").ValueKind);
    }

    [Fact]
    public async Task Sicakliklar_Bir_Ondalik_Yuvarlanir_Hesap_Tam_Hassasiyetle_Yapilir()
    {
        await using var api = await pg.CreateApiAsync(Now);
        await new EfWeatherStore(api.Db).UpsertForecastsAsync(Ankara, [Day(10, 12.34)], Now);
        await new EfClimateNormalStore(api.Db).UpsertAsync(Ankara, Constant(10.0));

        var day = (await Json(await api.Client.GetAsync("/api/cities/ankara/forecast"))).GetProperty("days")[0];

        Assert.Equal(12.3, day.GetProperty("meanC").GetDouble());    // 12.34 → 12.3
        Assert.Equal(2.3, day.GetProperty("anomalyC").GetDouble());  // 12.34 - 10 = 2.34 → 2.3
    }

    [Theory]
    [InlineData("ANKARA")]
    [InlineData("Ankara")]
    public async Task Sehir_Kimligi_Buyuk_Kucuk_Harfe_Duyarsiz(string id)
    {
        await using var api = await pg.CreateApiAsync(Now);

        var r = await api.Client.GetAsync($"/api/cities/{id}/forecast");

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
    }

    [Fact]
    public async Task Turkce_Karakterli_Kimlik_Url_Kodlamasiyla_Da_Calisir()
    {
        await using var api = await pg.CreateApiAsync(Now);

        var r = await api.Client.GetAsync("/api/cities/" + Uri.EscapeDataString("İstanbul") + "/forecast");

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
    }

    [Fact]
    public async Task Bilinmeyen_Sehir_404_ProblemDetails_Doner()
    {
        await using var api = await pg.CreateApiAsync(Now);

        var r = await api.Client.GetAsync("/api/cities/atlantis/forecast");
        var json = await Json(r);

        Assert.Equal(HttpStatusCode.NotFound, r.StatusCode);
        Assert.Equal(404, json.GetProperty("status").GetInt32());
        Assert.Equal("Şehir bulunamadı", json.GetProperty("title").GetString());
    }

    // ---- Harita ----

    [Fact]
    public async Task Harita_Tum_Noktalari_Listeler_Verisi_Olanlarda_Anomali_Vardir()
    {
        await using var api = await pg.CreateApiAsync(Now);
        await new EfWeatherStore(api.Db).UpsertForecastsAsync(Ankara, [Day(10, 12)], Now);
        await new EfWeatherStore(api.Db).UpsertForecastsAsync(Girne, [Day(10, 25)], Now);
        await new EfClimateNormalStore(api.Db).UpsertAsync(Ankara, Constant(10));
        await new EfClimateNormalStore(api.Db).UpsertAsync(Girne, Constant(20));

        var json = await Json(await api.Client.GetAsync("/api/map"));

        Assert.Equal("2026-10-10", json.GetProperty("date").GetString());
        var points = json.GetProperty("points");
        Assert.Equal(87, points.GetArrayLength());
        var ankara = points.EnumerateArray().Single(p => p.GetProperty("city").GetProperty("id").GetString() == "ankara");
        Assert.Equal(2.0, ankara.GetProperty("anomalyC").GetDouble(), 6);
        var girne = points.EnumerateArray().Single(p => p.GetProperty("city").GetProperty("id").GetString() == "girne");
        Assert.Equal(5.0, girne.GetProperty("anomalyC").GetDouble(), 6);
        var izmir = points.EnumerateArray().Single(p => p.GetProperty("city").GetProperty("id").GetString() == "izmir");
        Assert.Equal(JsonValueKind.Null, izmir.GetProperty("meanC").ValueKind); // verisiz şehir listede, boş değerlerle
    }

    // ---- Depremler ----

    private static Earthquake Quake(string id, double mag, DateTimeOffset at) => new(id, at, mag, "Yer", 38.5, 27.1, 8.5);

    [Fact]
    public async Task Depremler_Filtrelenir_Ve_Yeniden_Eskiye_Doner()
    {
        await using var api = await pg.CreateApiAsync(Now);
        await new EfEarthquakeStore(api.Db).UpsertAsync(
        [
            Quake("old", 5.0, Now.AddDays(-10)),
            Quake("small", 1.0, Now.AddHours(-1)),
            Quake("a", 3.0, Now.AddHours(-5)),
            Quake("b", 4.0, Now.AddHours(-2)),
        ]);

        var json = await Json(await api.Client.GetAsync("/api/earthquakes?days=7&minMagnitude=2.5"));

        Assert.Equal(["b", "a"], json.EnumerateArray().Select(e => e.GetProperty("id").GetString()));
    }

    [Fact]
    public async Task Depremde_Varsayilan_Parametreler_Calisir()
    {
        await using var api = await pg.CreateApiAsync(Now);

        var r = await api.Client.GetAsync("/api/earthquakes");

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
    }

    [Theory]
    [InlineData("days=0")]
    [InlineData("days=31")]
    [InlineData("minMagnitude=-1")]
    [InlineData("minMagnitude=11")]
    [InlineData("days=abc")]
    public async Task Gecersiz_Deprem_Parametresi_400_Doner(string query)
    {
        await using var api = await pg.CreateApiAsync(Now);

        var r = await api.Client.GetAsync($"/api/earthquakes?{query}");

        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
    }

    // ---- Genel ----

    [Fact]
    public async Task Var_Olmayan_Yol_404_Doner()
    {
        await using var api = await pg.CreateApiAsync(Now);

        Assert.Equal(HttpStatusCode.NotFound, (await api.Client.GetAsync("/api/yok")).StatusCode);
    }

    [Fact]
    public async Task Yalnizca_GET_Desteklenir()
    {
        await using var api = await pg.CreateApiAsync(Now);

        var r = await api.Client.PostAsync("/api/map", null);

        Assert.Equal(HttpStatusCode.MethodNotAllowed, r.StatusCode);
    }

    [Fact]
    public async Task JSON_camelCase_Ve_Enum_Metin_Doner()
    {
        await using var api = await pg.CreateApiAsync(Now);

        var json = await Json(await api.Client.GetAsync("/api/cities"));

        var first = json[0];
        Assert.True(first.TryGetProperty("latitude", out _)); // "Latitude" değil
        Assert.Equal(JsonValueKind.String, first.GetProperty("country").ValueKind);
    }
}
