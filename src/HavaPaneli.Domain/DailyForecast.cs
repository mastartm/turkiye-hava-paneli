namespace HavaPaneli.Domain;

/// <summary>
/// Bir şehir için tek günlük tahmin. Değerler nullable: kaynak bazı günler için veri vermeyebilir
/// ve "0 derece" ile "veri yok" birbirine karışmamalı.
/// </summary>
public sealed record DailyForecast(
    DateOnly Date,
    double? MinTemperatureC,
    double? MaxTemperatureC,
    double? MeanTemperatureC,
    double? PrecipitationMm,
    int? WeatherCode);
