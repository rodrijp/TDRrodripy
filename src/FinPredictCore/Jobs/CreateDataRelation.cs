using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FinPredictCore.Service.HistoricalData;
using MathNet.Numerics.Statistics;
using FinPredictData.Context;
using FinPredictData.Models;

namespace FinPredictCore.Jobs
{


    public class CreateDataRelation : ICreateDataRelation
    {
        private readonly IHistoricalDataService _historicalDataService;

        public CreateDataRelation(IHistoricalDataService historicalDataService)
        {
            _historicalDataService = historicalDataService;
        }

        public async Task Do()
        {
            var correlation = await CalculaCorrelación(1,2);
        }

        private async Task<IEnumerable<(DateOnly Date, float Value)>> GetSerie(Datum datum)
        {
            var data = await _historicalDataService.GetHistoricalDataByData(datum.DataId);
            var orderedData = data.OrderBy(h => h.Date).ToList();

            return orderedData.Select((historicalDatum, index) =>
            {
                if (!datum.IsValue || index == 0)
                    return (NormalizeToFirstDayOfMonth(historicalDatum.Date), historicalDatum.Value);

                var previousDatum = orderedData[index - 1];
                if (previousDatum.Value == 0)
                    return (NormalizeToFirstDayOfMonth(historicalDatum.Date), 0f);

                var percentageGrowth = ((historicalDatum.Value - previousDatum.Value) / previousDatum.Value) * 100f;
                return (NormalizeToFirstDayOfMonth(historicalDatum.Date), percentageGrowth);
            })
            .ToList();
        }

        private static DateOnly NormalizeToFirstDayOfMonth(DateOnly date)
        {
            return new DateOnly(date.Year, date.Month, 1);
        }

        public async Task<double> CalculaCorrelación(short dataId1, short dataId2)
        {
            if (dataId1 == dataId2)
                throw new ArgumentException("Los DataId no deben ser iguales.", nameof(dataId2));

            return await CalculaCorrelación(new Datum { DataId = dataId1, IsValue = true }, new Datum { DataId = dataId2, IsValue = true });
        }

        public async Task<double> CalculaCorrelación(Datum datum1, Datum datum2)
        {
            if (datum1.DataId == datum2.DataId)
                throw new ArgumentException("Los DataId no deben ser iguales.", nameof(datum2));

            var s1 = await GetSerie(datum1);
            var s2 = await GetSerie(datum2);

            var (start, end, commonDates, x, y) = AlignAndExtractCommon(s1, s2);

            // Usar MathNet.Numerics para calcular la correlación de Pearson
            var correlation = Correlation.Pearson(x, y);
            return correlation;
        }

        private static (DateOnly start, DateOnly end, List<DateOnly> commonDates, double[] x, double[] y)
            AlignAndExtractCommon(IEnumerable<(DateOnly Date, float Value)> s1,
                                   IEnumerable<(DateOnly Date, float Value)> s2)
        {
            var dict1 = s1.ToDictionary(x => x.Date, x => (double)x.Value);
            var dict2 = s2.ToDictionary(x => x.Date, x => (double)x.Value);

            var start1 = s1.Min(x => x.Date);
            var end1 = s1.Max(x => x.Date);
            var start2 = s2.Min(x => x.Date);
            var end2 = s2.Max(x => x.Date);

            var start = start1 > start2 ? start1 : start2;
            var end = end1 < end2 ? end1 : end2;

            if (end.CompareTo(start) < 0)
                throw new InvalidOperationException("No existe periodo común entre las dos series.");

            dict1 = dict1.Where(kv => kv.Key.CompareTo(start) >= 0 && kv.Key.CompareTo(end) <= 0)
                         .ToDictionary(kv => kv.Key, kv => kv.Value);
            dict2 = dict2.Where(kv => kv.Key.CompareTo(start) >= 0 && kv.Key.CompareTo(end) <= 0)
                         .ToDictionary(kv => kv.Key, kv => kv.Value);

            var commonDates = dict1.Keys.Intersect(dict2.Keys).OrderBy(d => d).ToList();

            if (commonDates.Count < 2)
                throw new InvalidOperationException("No hay suficientes puntos comunes en el periodo compartido para calcular la correlación.");

            var x = commonDates.Select(d => dict1[d]).ToArray();
            var y = commonDates.Select(d => dict2[d]).ToArray();

            return (start, end, commonDates, x, y);
        }





    }
}
