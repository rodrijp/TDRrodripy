using DataRelationModel = FinPredictData.Models.DataRelation;

namespace FinPredictCore.Service.DataRelation;

public interface IDataRelationService
{
    Task<DataRelationModel> CreateOrUpdate(DataRelationModel dataRelation);
    Task<DataRelationModel?> GetByDataIdSourceAndTarget(short dataIdSource, short dataIdTarget);
}
