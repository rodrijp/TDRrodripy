using FinPredictData.Models;

namespace FinPredictCore.Service.HistoricalData;

public interface IHistoricalDataService
{
    Task<long> CreateOrUpdate(HistoricalDatum historicalDatum);

    Task<List<HistoricalDatum>> GetHistoricalDataByData(short dataId);
}
