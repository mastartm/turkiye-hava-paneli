using HavaPaneli.Application;
using Microsoft.AspNetCore.Mvc;

namespace HavaPaneli.Api.Controllers;

[ApiController]
[Route("api/cities")]
public sealed class CitiesController(ForecastQueryService forecasts, IReadOnlyList<Domain.City> cities) : ControllerBase
{
    /// <summary>Haritadaki tüm şehirler (81 il + 6 KKTC ilçesi).</summary>
    [HttpGet]
    public ActionResult<IReadOnlyList<CityDto>> GetAll() =>
        Ok(cities.Select(CityDto.From).ToList());

    /// <summary>Bir şehrin 7 günlük tahmini; her gün için iklim normali ve anomali ile.</summary>
    [HttpGet("{id}/forecast")]
    public async Task<ActionResult<CityForecastDto>> GetForecast(string id, CancellationToken ct)
    {
        var view = await forecasts.GetCityForecastAsync(id, ct);
        if (view is null)
            return NotFound(new ProblemDetails { Title = "Şehir bulunamadı", Detail = $"'{id}' kimliğinde bir şehir yok.", Status = 404 });

        return Ok(new CityForecastDto(
            CityDto.From(view.City),
            view.Days.Select(DayDto.From).ToList(),
            view.Current is null ? null : CurrentDto.From(view.Current.Observation),
            view.Current?.FetchedAt));
    }
}
