using FinPredictData.Context;
using Microsoft.EntityFrameworkCore;
using DataStadisticModel = FinPredictData.Models.DataStadistic;

namespace FinPredictCore.Service.DataStadistic;

public class DataStadisticService : IDataStadisticService
{
    private readonly TDRMercatContext _context;

    public DataStadisticService(TDRMercatContext context)
    {
        _context = context;
    }

    public async Task<DataStadisticModel> CreateOrUpdate(DataStadisticModel model)
    {
        if (model is null)
            throw new ArgumentNullException(nameof(model));
        try
        {
            var existing = await _context.DataStadistics
                .FirstOrDefaultAsync(x => x.DataId == model.DataId);

            if (existing is not null)
            {
                if (model.Cagr.HasValue)
                {
                    existing.Cagr = model.Cagr;
                }

                if (model.Cagr20y.HasValue)
                {
                    existing.Cagr20y = model.Cagr20y;
                }

                if (model.Volatilidadneg30y.HasValue)
                {
                    existing.Volatilidadneg30y = model.Volatilidadneg30y;
                }

                if (model.Volatilidadcruda.HasValue)
                {
                    existing.Volatilidadcruda = model.Volatilidadcruda;
                }

                if (model.Volatilidaddetendenciada.HasValue)
                {
                    existing.Volatilidaddetendenciada = model.Volatilidaddetendenciada;
                }

                if (model.Sortino.HasValue)
                {
                    existing.Sortino = model.Sortino;
                }

                if (model.Sharpe.HasValue)
                {
                    existing.Sharpe = model.Sharpe;
                }

                if (model.Sortino20y.HasValue)
                {
                    existing.Sortino20y = model.Sortino20y;
                }

                if (model.CorrelationGen30y.HasValue)
                {
                    existing.CorrelationGen30y = model.CorrelationGen30y;
                }

                if (model.Rscore.HasValue)
                {
                    existing.Rscore = model.Rscore;
                }

                if (model.Cagr10y.HasValue)
                {
                    existing.Cagr10y = model.Cagr10y;
                }

                if (model.Rasignation.HasValue)
                {
                    existing.Rasignation = model.Rasignation;
                }

                await _context.SaveChangesAsync();
                return existing;
            }

            _context.DataStadistics.Add(model);
            await _context.SaveChangesAsync();
            return model;
        }
        catch (DbUpdateException dbEx)
        {
            throw new InvalidOperationException($"Error guardando CompoundAnualGrowthRate para DataId={model.DataId}: {dbEx.Message}", dbEx);
        }
    }

    public async Task<DataStadisticModel?> GetByDataId(short dataId) => await _context.DataStadistics
            .FirstOrDefaultAsync(x => x.DataId == dataId);
}
