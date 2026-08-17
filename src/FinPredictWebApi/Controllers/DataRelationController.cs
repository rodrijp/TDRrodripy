using FinPredictCore.Service.DataRelation;
using FinPredictData.Models;
using Microsoft.AspNetCore.Mvc;

namespace FinPredictWebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DataRelationController : ControllerBase
{
    private readonly IDataRelationService _dataRelationService;

    public DataRelationController(IDataRelationService dataRelationService)
    {
        _dataRelationService = dataRelationService;
    }

    [HttpGet("{dataIdSource}/{dataIdTarget}")]
    public async Task<ActionResult<DataRelation>> GetDataRelation(short dataIdSource, short dataIdTarget)
    {
        var result = await _dataRelationService.GetByDataIdSourceAndTarget(dataIdSource, dataIdTarget);

        if (result is null)
            return NotFound();

        return Ok(result);
    }
}
