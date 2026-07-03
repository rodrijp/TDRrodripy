using FinPredictCore.Service.Data;
using FinPredictData.Models;
using Microsoft.AspNetCore.Mvc;

namespace FinPredictWebApi.Controllers;

[ApiController]
[Route("api/data")]
public class DataControler : ControllerBase
{
    private readonly IDataService _dataService;

    public DataControler(IDataService dataService)
    {
        _dataService = dataService;
    }

    [HttpGet]
    public ActionResult<IEnumerable<Datum>> GetAllDatums()
    {
        var result = _dataService.GetAllDatums().ToList();
        return Ok(result);
    }
}
