using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FinPredictCore.Service.Data;
using FinPredictCore.Service.DataStadistic;
using FinPredictCore.Service.HistoricalData;
using FinPredictCore.Service.Source;
using FinPredictData.Models;
using static FinPredictCore.Jobs.CreateDataRelation;

namespace FinPredictCore.Jobs
{
    public class RScoreEngineCalculate : IRScoreEngineCalculate
    {
        private readonly IDataService _dataService;
        private readonly IHistoricalDataService _historicalDataService;
        private readonly IDataStadisticService _dataStadisticService;
        private readonly ICreateDataRelation _createDataRelation;

        public RScoreEngineCalculate(
            IDataService dataService,
            IHistoricalDataService historicalDataService,
            IDataStadisticService dataStadisticService,
            ICreateDataRelation createDataRelation)
        {
            _dataService = dataService;
            _historicalDataService = historicalDataService;
            _dataStadisticService = dataStadisticService;
            _createDataRelation = createDataRelation;
        }

        public async Task Do()
        {
            await CalculateCAGR20Y();
            await CalculateNegVol30Y();
            await CalculateSortino20Y();
            await CalculateCorrelationGen30Y();
        }

        private HashSet<int> getExcludeDatumIds()
        {
            return new HashSet<int> { DataUtil.INFLATION, DataUtil.UNEMPLOYMENT, DataUtil.DEBT_GDP, DataUtil.M2, DataUtil.DOW_JONES, DataUtil.TREASURY_30Y, DataUtil.TREASURY_10Y};
        }

        public async Task CalculateCAGR20Y()
        {
            var datums = _dataService
                .GetAllDatums()
                .Where(d => !getExcludeDatumIds().Contains(d.DataId))
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

                    double? cagr = CalculateDataStadistics.CalculaCagr(datum, last20YearsData);

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

        public async Task CalculateNegVol30Y()
        {
            var datums = _dataService
                .GetAllDatums()
                .Where(d => !getExcludeDatumIds().Contains(d.DataId))
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
                    var startDate = lastDate.AddYears(-30);

                    var yearlySeries = historical
                        .Where(h => h.Date >= startDate && h.Date <= lastDate)
                        .GroupBy(h => h.Date.Year)
                        .Select(g => g.OrderByDescending(h => h.Date).First())
                        .OrderBy(h => h.Date)
                        .ToList();

                    if (yearlySeries.Count < 2)
                    {
                        continue;
                    }

                    var allReturns = new List<double>();
                    for (var i = 1; i < yearlySeries.Count; i++)
                    {
                        var previousValue = yearlySeries[i - 1].Value;
                        var currentValue = yearlySeries[i].Value;

                        if (previousValue <= 0 || currentValue <= 0)
                        {
                            continue;
                        }

                        var returnValue = (double)currentValue / previousValue - 1.0;
                        allReturns.Add(returnValue);
                    }

                    if (allReturns.Count < 2)
                    {
                        continue;
                    }

                    var mean = allReturns.Average();
                    var negativeSquaredDiffs = allReturns
                        .Where(r => r < 0)
                        .Sum(r => Math.Pow(r - mean, 2));
                    var variance = negativeSquaredDiffs / allReturns.Count;
                    var negVol = Math.Sqrt(variance);

                    if (double.IsNaN(negVol) || double.IsInfinity(negVol))
                    {
                        continue;
                    }

                    var saved = await _dataStadisticService.CreateOrUpdate(new DataStadistic
                    {
                        DataId = datum.DataId,
                        Volatilidadneg30y = (float?)negVol
                    });

                    Console.WriteLine($"DataId={saved.DataId}, DataName={datum.DataName}, VOLATILIDADNeg 30Y={saved.Volatilidadneg30y:F6}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error calculando VolatilidadNeg30Y para DataId={datum.DataId}: {ex.Message}");
                }
            }
        }

        public async Task CalculateSortino20Y()
        {
            Console.WriteLine("\n" + new string('=', 60));
            Console.WriteLine(">>> INICIANDO CalculateSortino20Y <<<");
            Console.WriteLine(new string('=', 60));

            try
            {
                // Obtener datos de inflación (DataID = 13)
                Console.WriteLine("[1] Obteniendo datos de inflación (DataID=13)...");
                var inflationHistorical = (await _historicalDataService.GetHistoricalDataByData(13))
                    .OrderBy(h => h.Date)
                    .ToList();

                var lastInflationDate = inflationHistorical.Max(h => h.Date);
                var inflationStartDate = lastInflationDate.AddYears(-20);
                var last20YearsInflation = inflationHistorical
                    .Where(h => h.Date >= inflationStartDate && h.Date <= lastInflationDate)
                    .ToList();
                var inflactionAvg20Y = last20YearsInflation.Any()
                    ? last20YearsInflation.Average(h => (double)h.Value)
                    : (double?)null;


                Console.WriteLine($"Inflación mitja darrers 20 anys: {inflactionAvg20Y}");




                Console.WriteLine("[4] Obteniendo lista de activos...");
                var datums = _dataService
                    .GetAllDatums()
                    .Where(d => !getExcludeDatumIds().Contains(d.DataId))
                    .ToList();

                Console.WriteLine($"[4] ✓ Total de activos a procesar: {datums.Count}");
                int successCount = 0;

                foreach (var datum in datums)
                {
                    try
                    {
                        var historical = (await _historicalDataService.GetHistoricalDataByData(datum.DataId))
                            .OrderBy(h => h.Date)
                            .ToList();

                        if (historical.Count < 2)
                        {
                            Console.WriteLine($"  ⚠ DataId={datum.DataId}: No hay datos históricos suficientes");
                            continue;
                        }

                        var lastDate = historical.Max(h => h.Date);
                        var startDate = lastDate.AddYears(-20);

                        // Filtrar datos últimos 20 años y agrupar por año
                        var yearlySeries = historical
                            .Where(h => h.Date >= startDate && h.Date <= lastDate)
                            .ToList();

                        var sortino = CalculateDataStadistics.CalculaSortino(datum, yearlySeries, inflactionAvg20Y ?? 0);

                        var saved = await _dataStadisticService.CreateOrUpdate(new DataStadistic
                        {
                            DataId = datum.DataId,
                            Sortino20y = (float?)sortino
                        });

                        Console.WriteLine($"  {datum.DataName }   ✓ Sortino 20Y = {saved.Sortino20y:F6}");
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"  ✗ ERROR en DataId={datum.DataId}: {ex.Message}");
                    }
                }

                Console.WriteLine(new string('=', 60));
                Console.WriteLine($"✓ Cálculo de Sortino 20Y finalizado: {successCount}/{datums.Count} activos procesados");
                Console.WriteLine(new string('=', 60) + "\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERROR CRÍTICO en CalculateSortino20Y: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }

        public async Task CalculateCorrelationGen30Y()
        {
            Console.WriteLine("\n" + new string('=', 60));
            Console.WriteLine(">>> INICIANDO CalculateCorrelationGen30Y <<<");
            Console.WriteLine(new string('=', 60));

            try
            {
                var datums = _dataService
                    .GetAllDatums()
                    .Where(d => !getExcludeDatumIds().Contains(d.DataId))
                    .ToList();

                Console.WriteLine($"[1] Total de activos a procesar: {datums.Count}");
                int successCount = 0;

                foreach (var datum in datums)
                {
                    try
                    {
                        var correlations = new List<double>();

                        foreach (var other in datums)
                        {
                            if (other.DataId == datum.DataId)
                            {
                                continue;
                            }

                            try
                            {
                                var correlation = await _createDataRelation.CalculaCorrelación(datum, other, TypeDatum.Arithmetic, year: 30);
                                if (!double.IsNaN(correlation))
                                {
                                    correlations.Add(correlation);
                                }
                            }
                            catch (Exception)
                            {
                            }
                        }

                        float? meanCorrelation = correlations.Count > 0
                            ? (float?)correlations.Average()
                            : null;

                        await _dataStadisticService.CreateOrUpdate(new DataStadistic
                        {
                            DataId = datum.DataId,
                            CorrelationGen30y = meanCorrelation
                        });

                        Console.WriteLine($"  {datum.DataName}   ✓ CorrelationGen30Y = {meanCorrelation:F6} ({correlations.Count} correlaciones)");
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"  ✗ ERROR en DataId={datum.DataId}: {ex.Message}");
                    }
                }

                Console.WriteLine(new string('=', 60));
                Console.WriteLine($"✓ Cálculo de CorrelationGen30Y finalizado: {successCount}/{datums.Count} activos procesados");
                Console.WriteLine(new string('=', 60) + "\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERROR CRÍTICO en CalculateCorrelationGen30Y: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }
    }
}
