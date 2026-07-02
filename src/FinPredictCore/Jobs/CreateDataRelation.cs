using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FinPredictCore.Service.Data;
using FinPredictCore.Service.DataRelation;
using FinPredictCore.Service.HistoricalData;
using MathNet.Numerics.Statistics;
using FinPredictData.Models;

namespace FinPredictCore.Jobs
{


    public class CreateDataRelation : ICreateDataRelation
    {
        private readonly IHistoricalDataService _historicalDataService;
        private readonly IDataService _dataService;
        private readonly IDataRelationService _dataRelationService;

        private enum TypeDatum
        {
            Arithmetic,
            Logarithmic
        }

        public CreateDataRelation(
            IHistoricalDataService historicalDataService,
            IDataService dataService,
            IDataRelationService dataRelationService)
        {
            _historicalDataService = historicalDataService;
            _dataService = dataService;
            _dataRelationService = dataRelationService;
        }

        public async Task Do()
        {
            var datums = _dataService.GetAllDatums().ToList();
            Console.WriteLine($"Iniciando cálculo de correlaciones para {datums.Count} variables...");

            foreach(var datum1 in datums)
            {

                foreach(var datum2 in datums)
                {

                    Console.WriteLine($"  -> Comparando {datum1.DataId}/{datum1.DataName} con {datum2.DataId}/{datum2.DataName}");

                    try
                    {
                        var correlation = await CalculaCorrelación(datum1, datum2, TypeDatum.Arithmetic);
                        var correlationValue = double.IsNaN(correlation) ? null : (float?)correlation;

                        var dataRelation = new DataRelation
                        {
                            DataIdSource = datum1.DataId,
                            DataIdTarget = datum2.DataId,
                            Correlation = correlationValue,
                            Covariance = null
                        };

                        await _dataRelationService.CreateOrUpdate(dataRelation);
                        Console.WriteLine($"    ✅ Correlación guardada: {datum1.DataId} <-> {datum2.DataId} = {correlationValue:F6}");
                    }
                    catch (ArgumentException ex)
                    {
                        Console.WriteLine($"    ⚠️ Se omite comparación por argumento inválido: {ex.Message}");
                    }
                    catch (InvalidOperationException ex)
                    {
                        Console.WriteLine($"    ⚠️ Se omite comparación por falta de datos comunes: {ex.Message}");
                    }
                }
            }

            Console.WriteLine("Finalizado el cálculo de correlaciones.");
        }

        private async Task<IEnumerable<(DateOnly Date, float Value)>> GetSerie(Datum datum,  TypeDatum type)
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



        private async Task<double> CalculaCorrelación(Datum datum1, Datum datum2, TypeDatum type)
        {
//            if (datum1.DataId == datum2.DataId)
//                throw new ArgumentException("Los DataId no deben ser iguales.", nameof(datum2));

            var s1 = await GetSerie(datum1, type);
            var s2 = await GetSerie(datum2, type);

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
