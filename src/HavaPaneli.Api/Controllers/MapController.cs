using HavaPaneli.Application;
using Microsoft.AspNetCore.Mvc;

namespace HavaPaneli.Api.Controllers;

[ApiController]
[Route("api/map")]
public sealed class MapController(ForecastQueryService forecasts) : ControllerBase
{
    /// <summary>Haritadaki tüm noktaların bugünkü özeti: sıcaklık, hava kodu, normal ve anomali.</summary>
    [HttpGet]
    public async Task<ActionResult<MapDto>> Get(CancellationToken ct)
    {
        var map = await forecasts.GetMapAsync(ct);
        return Ok(new MapDto(
            map.Date,
            map.Points.Select(p => new MapPointDto(
                CityDto.From(p.City), Round.C(p.MinC), Round.C(p.MaxC), Round.C(p.MeanC), p.WeatherCode, Round.C(p.NormalC), Round.C(p.AnomalyC))).ToList()));
    }
}
