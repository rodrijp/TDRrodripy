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

    [HttpGet("{dataId}")]
    public async Task<ActionResult<List<HistoricalDatum>>> GetHistoricalDataByData(short dataId)
    {
        var result = await _historicalDataService.GetHistoricalDataByData(dataId);
        return Ok(result);
    }
}
