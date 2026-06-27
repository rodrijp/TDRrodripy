using System.Collections.Generic;
using FinPredictData.Models;

namespace FinPredictCore.Service.Data
{
    public interface IDataService
    {
        IEnumerable<Datum> GetAllDatums();
        IEnumerable<Datum> GetAllDatumsBySource(int sourceId);
  
    }
}
