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

        public async Task Do()
        {
            await CalculateCAGR20Y();
            await CalculateNegVol30Y();
            await CalculateSortino20Y();
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

        public async Task CalculateNegVol30Y()
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

                    var negativeReturns = new List<double>();
                    for (var i = 1; i < yearlySeries.Count; i++)
                    {
                        var previousValue = yearlySeries[i - 1].Value;
                        var currentValue = yearlySeries[i].Value;

                        if (previousValue <= 0 || currentValue <= 0)
                        {
                            continue;
                        }

                        var returnValue = (double)currentValue / previousValue - 1.0;
                        if (returnValue < 0)
                        {
                            negativeReturns.Add(returnValue);
                        }
                    }

                    if (negativeReturns.Count < 2)
                    {
                        continue;
                    }

                    var mean = negativeReturns.Average();
                    var variance = negativeReturns.Sum(r => Math.Pow(r - mean, 2)) / (negativeReturns.Count - 1);
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

                Console.WriteLine($"[1] Datos de inflación encontrados: {inflationHistorical.Count}");

                if (inflationHistorical.Count < 2)
                {
                    Console.WriteLine("❌ ERROR: No hay datos de inflación disponibles para calcular Sortino 20Y.");
                    return;
                }

                // Agrupar inflación por año (último de cada año)
                Console.WriteLine("[2] Agrupando inflación por año...");
                var inflationYearlySeries = inflationHistorical
                    .GroupBy(h => h.Date.Year)
                    .Select(g => g.OrderByDescending(h => h.Date).First())
                    .OrderBy(h => h.Date)
                    .ToList();

                Console.WriteLine($"[2] Series anuales de inflación: {inflationYearlySeries.Count} años (desde {inflationYearlySeries.First().Date.Year} hasta {inflationYearlySeries.Last().Date.Year})");

                if (inflationYearlySeries.Count < 2)
                {
                    Console.WriteLine("❌ ERROR: Insuficientes años de inflación.");
                    return;
                }

                // Crear un diccionario de inflación por año para búsquedas rápidas
                Console.WriteLine("[3] Creando diccionario de inflación...");
                var inflationByYear = new Dictionary<int, float>();
                for (var i = 0; i < inflationYearlySeries.Count; i++)
                {
                    inflationByYear[inflationYearlySeries[i].Date.Year] = inflationYearlySeries[i].Value;
                }

                Console.WriteLine($"[3] ✓ Diccionario de inflación creado con {inflationByYear.Count} años");

                Console.WriteLine("[4] Obteniendo lista de activos...");
                var datums = _dataService
                    .GetAllDatums()
                    .Where(d => d.DataId != 13 && d.DataId != 14 && d.DataId != 15 && d.DataId != 17)
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
                            .GroupBy(h => h.Date.Year)
                            .Select(g => new { Year = g.Key, Data = g.OrderByDescending(h => h.Date).First() })
                            .OrderBy(x => x.Year)
                            .ToList();

                        if (yearlySeries.Count < 2)
                        {
                            Console.WriteLine($"  ⚠ DataId={datum.DataId}: Insuficientes series anuales ({yearlySeries.Count})");
                            continue;
                        }

                        Console.WriteLine($"  → DataId={datum.DataId} ({datum.DataName}): {yearlySeries.First().Year}-{yearlySeries.Last().Year}");

                        // Obtener primer y último año de la serie anual del activo
                        var firstYear = yearlySeries.First().Year;
                        var lastYear = yearlySeries.Last().Year;
                        var numYears = yearlySeries.Count - 1;

                        if (numYears < 1)
                        {
                            Console.WriteLine($"     ⚠ Insuficientes periodos anuales ({numYears})");
                            continue;
                        }

                        var firstAssetValue = (double)yearlySeries.First().Data.Value;
                        var lastAssetValue = (double)yearlySeries.Last().Data.Value;

                        if (firstAssetValue <= 0 || lastAssetValue <= 0)
                        {
                            Console.WriteLine($"     ⚠ Valores de activo inválidos en rango {firstYear}-{lastYear}");
                            continue;
                        }

                        if (!inflationByYear.ContainsKey(firstYear) || !inflationByYear.ContainsKey(lastYear))
                        {
                            Console.WriteLine($"     ⚠ Sin datos de inflación para el rango {firstYear}-{lastYear}");
                            continue;
                        }

                        var firstInflationValue = (double)inflationByYear[firstYear];
                        var lastInflationValue = (double)inflationByYear[lastYear];

                        if (firstInflationValue <= 0 || lastInflationValue <= 0)
                        {
                            Console.WriteLine($"     ⚠ Valores de inflación inválidos en rango {firstYear}-{lastYear}");
                            continue;
                        }

                        // R: retorno logarítmico anualizado del activo
                        var R = Math.Log(lastAssetValue / firstAssetValue) / numYears;

                        // I: retorno logarítmico anualizado de la inflación (DataId=13)
                        var I = Math.Log(lastInflationValue / firstInflationValue) / numYears;

                        // Numerador: (1+R)/(1+I) - 1
                        var numerator = (1.0 + R) / (1.0 + I) - 1.0;

                        // Calcular retornos reales año a año (para el denominador)
                        var realReturns = new List<double>();

                        for (var i = 1; i < yearlySeries.Count; i++)
                        {
                            var yearCurrent = yearlySeries[i].Year;
                            var yearPrevious = yearlySeries[i - 1].Year;
                            var valueCurrent = (double)yearlySeries[i].Data.Value;
                            var valuePrevious = (double)yearlySeries[i - 1].Data.Value;

                            if (valuePrevious <= 0 || valueCurrent <= 0)
                            {
                                continue;
                            }

                            if (!inflationByYear.ContainsKey(yearCurrent) || !inflationByYear.ContainsKey(yearPrevious))
                            {
                                continue;
                            }

                            var assetLogReturn = Math.Log(valueCurrent / valuePrevious);

                            var inflationCurrent = (double)inflationByYear[yearCurrent];
                            var inflationPrevious = (double)inflationByYear[yearPrevious];

                            if (inflationPrevious <= 0 || inflationCurrent <= 0)
                            {
                                continue;
                            }

                            var inflLogReturn = Math.Log(inflationCurrent / inflationPrevious);

                            var realReturn = (1.0 + assetLogReturn) / (1.0 + inflLogReturn) - 1.0;
                            realReturns.Add(realReturn);
                        }

                        if (realReturns.Count < 2)
                        {
                            Console.WriteLine($"     ⚠ Insuficientes retornos reales ({realReturns.Count})");
                            continue;
                        }

                        // Denominador: desviación a la baja
                        // Elevar al cuadrado solo retornos negativos (activos con menor crecimiento que inflación)
                        var downsideSquares = realReturns
                            .Select(r => r < 0 ? Math.Pow(r, 2) : 0)
                            .ToList();

                        var downsideVariance = downsideSquares.Sum() / realReturns.Count;
                        var denominator = Math.Sqrt(downsideVariance);

                        double sortino;
                        if (denominator > 0)
                        {
                            sortino = numerator / denominator;
                        }
                        else
                        {
                            sortino = double.NaN;
                        }

                        if (double.IsNaN(sortino) || double.IsInfinity(sortino))
                        {
                            Console.WriteLine($"     ⚠ Sortino inválido (NaN o Infinity)");
                            continue;
                        }

                        var saved = await _dataStadisticService.CreateOrUpdate(new DataStadistic
                        {
                            DataId = datum.DataId,
                            Sortino20y = (float?)sortino
                        });

                        Console.WriteLine($"     ✓ Sortino 20Y = {saved.Sortino20y:F6}");
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
    }
}
