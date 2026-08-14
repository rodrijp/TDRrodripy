using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FinPredictCore.Service.Data;
using FinPredictCore.Service.DataStadistic;
using FinPredictCore.Service.HistoricalData;
using FinPredictData.Models;

namespace FinPredictCore.Jobs
{
    public class RScoreEngineCalculate : IRScoreEngineCalculate
    {
        private readonly IDataService _dataService;
        private readonly IHistoricalDataService _historicalDataService;
        private readonly IDataStadisticService _dataStadisticService;

        public RScoreEngineCalculate(
            IDataService dataService,
            IHistoricalDataService historicalDataService,
            IDataStadisticService dataStadisticService)
        {
            _dataService = dataService;
            _historicalDataService = historicalDataService;
            _dataStadisticService = dataStadisticService;
        }

        public Task Do()
        {
            return CalculateCAGR20Y();
        }

        public async Task CalculateCAGR20Y()
        {
            var datums = _dataService
                .GetAllDatums()
                .Where(d => d.DataId != 13 && d.DataId != 14 && d.DataId != 15 && d.DataId != 17)
                .ToList();

            foreach (var datum in datums)
            {
                try
                {
                    var historical = (await _historicalDataService.GetHistoricalDataByData(datum.DataId))
                        .OrderBy(h => h.Date)
                        .ToList();

                    if (historical.Count < 2)
                    {
                        continue;
                    }

                    var lastDate = historical.Max(h => h.Date);
                    var startDate = lastDate.AddYears(-20);

                    var last20YearsData = historical
                        .Where(h => h.Date >= startDate && h.Date <= lastDate)
                        .OrderBy(h => h.Date)
                        .ToList();

                    if (last20YearsData.Count < 2)
                    {
                        continue;
                    }

                    var first = last20YearsData.First();
                    var last = last20YearsData.Last();

                    if (first.Value <= 0 || last.Value <= 0)
                    {
                        continue;
                    }

                    var years = (last.Date.ToDateTime(TimeOnly.MinValue) - first.Date.ToDateTime(TimeOnly.MinValue)).TotalDays / 365.25;
                    if (years <= 0)
                    {
                        continue;
                    }

                    var cagr = Math.Pow(last.Value / first.Value, 1.0 / years) - 1.0;

                    await _dataStadisticService.CreateOrUpdate(new DataStadistic
                    {
                        DataId = datum.DataId,
                        Cagr20y = (float?)cagr
                    });
                }
                catch (Exception)
                {
                    // Intencionalmente se omite el activo si no puede calcularse el CAGR de 20 años.
                }
            }
        }
    }
}
