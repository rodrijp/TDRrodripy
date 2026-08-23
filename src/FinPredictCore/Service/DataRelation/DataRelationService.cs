using FinPredictData.Context;
using Microsoft.EntityFrameworkCore;
using DataRelationModel = FinPredictData.Models.DataRelation;

namespace FinPredictCore.Service.DataRelation;

public class DataRelationService : IDataRelationService
{
    private readonly TDRMercatContext _context;

    public DataRelationService(TDRMercatContext context)
    {
        _context = context;
    }

    public async Task<DataRelationModel> CreateOrUpdate(DataRelationModel dataRelation)
    {
        if (dataRelation is null)
            throw new ArgumentNullException(nameof(dataRelation));

        var existing = await _context.DataRelations
            .FirstOrDefaultAsync(x =>
                x.DataIdSource == dataRelation.DataIdSource &&
                x.DataIdTarget == dataRelation.DataIdTarget);

        if (existing is not null)
        {
            existing.Correlation = dataRelation.Correlation;
            existing.Covariance = dataRelation.Covariance;
            existing.CorrelationLog = dataRelation.CorrelationLog;
            await _context.SaveChangesAsync();
            return existing;
        }

        _context.DataRelations.Add(dataRelation);
        await _context.SaveChangesAsync();
        return dataRelation;
    }

    public async Task<DataRelationModel?> GetByDataIdSourceAndTarget(short dataIdSource, short dataIdTarget) => await _context.DataRelations
            .FirstOrDefaultAsync(x =>
                x.DataIdSource == dataIdSource &&
                x.DataIdTarget == dataIdTarget);
}
