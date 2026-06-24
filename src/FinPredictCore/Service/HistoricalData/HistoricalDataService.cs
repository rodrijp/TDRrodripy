using FinPredictData.Context;
using FinPredictData.Models;
using Microsoft.EntityFrameworkCore;

namespace FinPredictCore.Service.HistoricalData;

public class HistoricalDataService : IHistoricalDataService
{
    private readonly TDRMercatContext _context;

    public HistoricalDataService(TDRMercatContext context)
    {
        _context = context;
    }

    public async Task<long> CreateOrUpdate(HistoricalDatum historicalDatum)
    {
        ArgumentNullException.ThrowIfNull(historicalDatum);

        var existing = await _context.HistoricalData
            .FirstOrDefaultAsync(h => h.Date == historicalDatum.Date && h.DataId == historicalDatum.DataId);

        if (existing is null)
        {
            _context.HistoricalData.Add(historicalDatum);
        }
        else
        {
            existing.Date = historicalDatum.Date;
            existing.DataId = historicalDatum.DataId;
            existing.Value = historicalDatum.Value;
        }

        await _context.SaveChangesAsync();

        return historicalDatum.HistoricalDataId;
    }

    public async Task<List<HistoricalDatum>> GetHistoricalDataByData(short dataId)
    {
        return await _context.HistoricalData
            .Where(x => x.DataId == dataId)
            .ToListAsync();
    }
}
