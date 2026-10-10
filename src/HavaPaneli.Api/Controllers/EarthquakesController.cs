using HavaPaneli.Application;
using Microsoft.AspNetCore.Mvc;

namespace HavaPaneli.Api.Controllers;

[ApiController]
[Route("api/earthquakes")]
public sealed class EarthquakesController(EarthquakeQueryService earthquakes) : ControllerBase
{
    /// <summary>Son depremler, en yeniden eskiye. days: 1-30, minMagnitude: 0-10.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<EarthquakeDto>>> Get(
        [FromQuery] int days = 7, [FromQuery] double minMagnitude = 2.5, CancellationToken ct = default)
    {
        try
        {
            var result = await earthquakes.GetRecentAsync(days, minMagnitude, ct);
            return Ok(result.Select(EarthquakeDto.From).ToList());
        }
        catch (ArgumentOutOfRangeException ex)
        {
            // Geçersiz parametre istemcinin hatası: 500 değil 400 dönmeli.
            ModelState.AddModelError(ex.ParamName ?? "query", ex.Message);
            return ValidationProblem(ModelState);
        }
    }
}
