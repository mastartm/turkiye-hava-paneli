using Microsoft.AspNetCore.Mvc;

namespace HavaPaneli.Api.Controllers;

[ApiController]
[Route("api/sources")]
public sealed class SourcesController : ControllerBase
{
    // Lisans gereği atıf zorunlu (Open-Meteo CC BY 4.0). Arayüz bu listeyi her sayfada gösterir.
    private static readonly IReadOnlyList<SourceDto> Sources =
    [
        new("Open-Meteo", "https://open-meteo.com/", "CC BY 4.0",
            "Hava tahmini ve geçmiş veri (ERA5-Land). Lisans: https://creativecommons.org/licenses/by/4.0/"),
        new("USGS Earthquake Hazards Program", "https://earthquake.usgs.gov/", "Kamu verisi",
            "Deprem kayıtları. Komşu bölgelerdeki depremler de görünebilir."),
    ];

    /// <summary>Verinin kaynakları ve lisansları (atıf için).</summary>
    [HttpGet]
    public ActionResult<IReadOnlyList<SourceDto>> Get() => Ok(Sources);
}
