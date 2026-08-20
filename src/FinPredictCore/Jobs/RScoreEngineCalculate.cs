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
            await RScoreCalculator();
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

        public async Task RScoreCalculator()
        {
            Console.WriteLine("\n" + new string('=', 60));
            Console.WriteLine(">>> INICIANDO RScoreCalculator <<<");
            Console.WriteLine(new string('=', 60));

            try
            {
                var datums = _dataService
                    .GetAllDatums()
                    .Where(d => !getExcludeDatumIds().Contains(d.DataId))
                    .ToList();

                Console.WriteLine($"[1] Total de activos a procesar: {datums.Count}");

                var assetsData = new Dictionary<int, (string Name, float? Cagr20y, float? Volatilidadneg30y, float? Sortino20y, float? CorrelationGen30y)>();

                foreach (var datum in datums)
                {
                    try
                    {
                        var stadistic = await _dataStadisticService.GetByDataId(datum.DataId);

                        if (stadistic == null)
                        {
                            Console.WriteLine($"  ⚠ DataId={datum.DataId}: No tiene DataStadistic calculado, se omite");
                            continue;
                        }

                        if (stadistic.Cagr20y == null || stadistic.Volatilidadneg30y == null || stadistic.Sortino20y == null || stadistic.CorrelationGen30y == null)
                        {
                            Console.WriteLine($"  ⚠ DataId={datum.DataId}: Faltan campos necesarios (Cagr20y={stadistic.Cagr20y}, VolNeg30y={stadistic.Volatilidadneg30y}, Sortino20y={stadistic.Sortino20y}, Corr30y={stadistic.CorrelationGen30y}), se omite");
                            continue;
                        }

                        assetsData[datum.DataId] = (datum.DataName, stadistic.Cagr20y, stadistic.Volatilidadneg30y, stadistic.Sortino20y, stadistic.CorrelationGen30y);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"  ✗ ERROR leyendo DataStadistic para DataId={datum.DataId}: {ex.Message}");
                    }
                }

                if (assetsData.Count == 0)
                {
                    Console.WriteLine("❌ No hay datos suficientes para calcular RScore. Asegúrese de ejecutar los cálculos previos.");
                    return;
                }

                Console.WriteLine($"\n[2] Activos válidos para RScore: {assetsData.Count}");

                // --- GradeCagr20Y ---
                var cagrValues = assetsData.Values.Where(v => v.Cagr20y.HasValue).Select(v => (double)v.Cagr20y!.Value).ToList();
                double bestCagr = cagrValues.Max();
                double worstCagr = cagrValues.Min();
                Console.WriteLine($"\n[3] CAGR 20Y - Best={bestCagr:F6}, Worst={worstCagr:F6}");

                // --- GradeVolatility30y ---
                var volValues = assetsData.Values.Where(v => v.Volatilidadneg30y.HasValue).Select(v => (double)v.Volatilidadneg30y!.Value).ToList();
                double bestVolatility = volValues.Max();
                double worstVolatility = volValues.Min();
                Console.WriteLine($"[4] VOLATILIDADNeg 30Y - Best={bestVolatility:F6}, Worst={worstVolatility:F6}");

                // --- GradeSortino20y ---
                var sortinoValues = assetsData.Values.Where(v => v.Sortino20y.HasValue).Select(v => (double)v.Sortino20y!.Value).ToList();
                double bestSortino = sortinoValues.Max();
                double worstSortino = sortinoValues.Min();
                Console.WriteLine($"[5] Sortino 20Y - Best={bestSortino:F6}, Worst={worstSortino:F6}");

                // --- GradeCorrelationGen30y ---
                var corrValues = assetsData.Values.Where(v => v.CorrelationGen30y.HasValue).Select(v => (double)v.CorrelationGen30y!.Value).ToList();
                double bestCorrelation = corrValues.Max();
                double worstCorrelation = corrValues.Min();
                Console.WriteLine($"[6] CorrelationGen 30Y - Best={bestCorrelation:F6}, Worst={worstCorrelation:F6}");

                Console.WriteLine("\n[7] Calculando RScore para cada activo...\n");

                int successCount = 0;

                foreach (var kvp in assetsData)
                {
                    try
                    {
                        int dataId = kvp.Key;
                        var (name, cagr20y, volNeg30y, sortino20y, corr30y) = kvp.Value;

                        // GradeCagr20Y: Best=10, Worst=0
                        double gradeCagr = 0;
                        if (bestCagr != worstCagr)
                        {
                            gradeCagr = ((double)cagr20y! - worstCagr) / (bestCagr - worstCagr) * 10.0;
                        }
                        else
                        {
                            gradeCagr = 5.0;
                        }

                        // GradeVolatility30y: Best(max vol)=0, Worst(min vol)=10
                        double gradeVol = 0;
                        if (bestVolatility != worstVolatility)
                        {
                            gradeVol = (bestVolatility - (double)volNeg30y!) / (bestVolatility - worstVolatility) * 10.0;
                        }
                        else
                        {
                            gradeVol = 5.0;
                        }

                        // GradeSortino20y: Best=10, Worst=0
                        double gradeSortino = 0;
                        if (bestSortino != worstSortino)
                        {
                            gradeSortino = ((double)sortino20y! - worstSortino) / (bestSortino - worstSortino) * 10.0;
                        }
                        else
                        {
                            gradeSortino = 5.0;
                        }

                        // GradeCorrelationGen30y: Best(max corr)=0, Worst(min corr)=10
                        double gradeCorr = 0;
                        if (bestCorrelation != worstCorrelation)
                        {
                            gradeCorr = (bestCorrelation - (double)corr30y!) / (bestCorrelation - worstCorrelation) * 10.0;
                        }
                        else
                        {
                            gradeCorr = 5.0;
                        }

                        // RScore final
                        double rScore = (gradeCagr * 0.4) + (gradeVol * 0.3) + (gradeSortino * 0.2) + (gradeCorr * 0.1);

                        await _dataStadisticService.CreateOrUpdate(new DataStadistic
                        {
                            DataId = (short)dataId,
                            Rscore = (float?)rScore
                        });

                        Console.WriteLine($"  {name,-20} | Cagr20Y={cagr20y:F4} Vol30Y={volNeg30y:F4} Sort20Y={sortino20y:F4} Corr30Y={corr30y:F4} | GradeCagr={gradeCagr:F2} GradeVol={gradeVol:F2} GradeSort={gradeSortino:F2} GradeCorr={gradeCorr:F2} | RScore={rScore:F4}");
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"  ✗ ERROR en DataId={kvp.Key}: {ex.Message}");
                    }
                }

                Console.WriteLine(new string('=', 60));
                Console.WriteLine($"✓ Cálculo de RScore finalizado: {successCount}/{assetsData.Count} activos procesados");
                Console.WriteLine(new string('=', 60) + "\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERROR CRÍTICO en RScoreCalculator: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }
    }
}
