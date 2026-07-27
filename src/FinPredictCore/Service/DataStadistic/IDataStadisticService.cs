using CompoundModel = FinPredictData.Models.DataStadistic;
using DataStadisticModel = FinPredictData.Models.DataStadistic;

namespace FinPredictCore.Service.DataStadistic;

public interface IDataStadisticService
{
    Task<DataStadisticModel> CreateOrUpdate(DataStadisticModel model);
}
