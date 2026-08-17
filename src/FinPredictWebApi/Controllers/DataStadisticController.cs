using FinPredictCore.Service.DataStadistic;
using FinPredictData.Models;
using Microsoft.AspNetCore.Mvc;

namespace FinPredictWebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DataStadisticController : ControllerBase
{
    private readonly IDataStadisticService _dataStadisticService;

    public DataStadisticController(IDataStadisticService dataStadisticService)
    {
        _dataStadisticService = dataStadisticService;
    }

    /// <summary>
    /// Obtiene las estadísticas (DataStadistic) para un dato específico.
    /// </summary>
    /// <param name="dataId">Id del dato financiero</param>
    [HttpGet("{dataId}")]
    public async Task<ActionResult<DataStadistic>> GetDataStadistic(short dataId)
    {
        var result = await _dataStadisticService.GetByDataId(dataId);
        if (result is null)
            return NotFound();

        return Ok(result);
    }
}
