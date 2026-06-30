using FinPredictCore.Service.HistoricalData;
using FinPredictData.Models;
using Microsoft.AspNetCore.Mvc;

namespace FinPredictWebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HistoricalDataController : ControllerBase
{
    private readonly IHistoricalDataService _historicalDataService;

    public HistoricalDataController(IHistoricalDataService historicalDataService)
    {
        _historicalDataService = historicalDataService;
    }

    /// <summary>
    /// Obtiene los datos históricos de un dato específico.
    /// </summary>
    /// <param name="dataId">Id del dato financiero</param>
    /// <returns></returns>
    [HttpGet("{dataId}")]
    public async Task<ActionResult<List<HistoricalDatum>>> GetHistoricalDataByData(short dataId)
    {
        var result = await _historicalDataService.GetHistoricalDataByData(dataId);
        return Ok(result);
    }
}
