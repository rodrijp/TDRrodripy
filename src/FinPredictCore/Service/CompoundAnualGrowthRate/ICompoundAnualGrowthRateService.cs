using CompoundModel = FinPredictData.Models.CompoundAnualGrowthRate;

namespace FinPredictCore.Service.CompoundAnualGrowthRate;

public interface ICompoundAnualGrowthRateService
{
    Task<CompoundModel> CreateOrUpdate(CompoundModel model);
}
