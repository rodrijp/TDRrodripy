using FinPredictData.Context;
using FinPredictData.Models;
using Microsoft.EntityFrameworkCore;

namespace FinPredictCore.Service.Data;

public class DataService : IDataService
{
    private readonly TDRMercatContext _context;

    public DataService(TDRMercatContext context)
    {
        _context = context;
    }

    public IEnumerable<Datum> GetAllDatums()
    {
        return _context.Data
            .AsNoTracking()
            .ToList();
    }
    public IEnumerable<Datum> GetAllDatumsBySource(int sourceId)
    {
        return _context.Data
            .AsNoTracking()
            .Where(d => d.SourceId == sourceId)
            .ToList();
    }


}
