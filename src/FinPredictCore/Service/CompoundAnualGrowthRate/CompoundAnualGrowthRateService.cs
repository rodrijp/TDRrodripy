using FinPredictData.Context;
using Microsoft.EntityFrameworkCore;
using CompoundModel = FinPredictData.Models.CompoundAnualGrowthRate;

namespace FinPredictCore.Service.CompoundAnualGrowthRate;

public class CompoundAnualGrowthRateService : ICompoundAnualGrowthRateService
{
    private readonly TDRMercatContext _context;

    public CompoundAnualGrowthRateService(TDRMercatContext context)
    {
        _context = context;
    }

    public async Task<CompoundModel> CreateOrUpdate(CompoundModel model)
    {
        if (model is null)
            throw new ArgumentNullException(nameof(model));
        try
        {
            var existing = await _context.CompoundAnualGrowthRates
                .FirstOrDefaultAsync(x => x.DataId == model.DataId);

            if (existing is not null)
            {
                existing.Cagr = model.Cagr;
                await _context.SaveChangesAsync();
                return existing;
            }

            _context.CompoundAnualGrowthRates.Add(model);
            await _context.SaveChangesAsync();
            return model;
        }
        catch (DbUpdateException dbEx)
        {
            throw new InvalidOperationException($"Error guardando CompoundAnualGrowthRate para DataId={model.DataId}: {dbEx.Message}", dbEx);
        }
    }
}
