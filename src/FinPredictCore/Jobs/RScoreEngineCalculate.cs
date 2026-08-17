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
            Console.WriteLine("Iniciando cálculo de Sortino 20Y (ajustado por inflación)...");

            // Obtener datos de inflación (DataID = 13)
            var inflationHistorical = (await _historicalDataService.GetHistoricalDataByData(13))
                .OrderBy(h => h.Date)
                .ToList();

            if (inflationHistorical.Count < 2)
            {
                Console.WriteLine("Error: No hay datos de inflación disponibles para calcular Sortino 20Y.");
                return;
            }

            Console.WriteLine($"  Datos de inflación encontrados: {inflationHistorical.Count}");

            // Agrupar inflación por año (último de cada año)
            var inflationYearlySeries = inflationHistorical
                .GroupBy(h => h.Date.Year)
                .Select(g => g.OrderByDescending(h => h.Date).First())
                .OrderBy(h => h.Date)
                .ToList();

            Console.WriteLine($"  Series anuales de inflación: {inflationYearlySeries.Count} años (desde {inflationYearlySeries.First().Date.Year} hasta {inflationYearlySeries.Last().Date.Year})");

            if (inflationYearlySeries.Count < 2)
            {
                Console.WriteLine("Error: Insuficientes años de inflación.");
                return;
            }

            // Crear un diccionario de inflación por año para búsquedas rápidas
            var inflationByYear = new Dictionary<int, float>();
            for (var i = 0; i < inflationYearlySeries.Count; i++)
            {
                inflationByYear[inflationYearlySeries[i].Date.Year] = inflationYearlySeries[i].Value;
            }

            Console.WriteLine($"  Diccionario de inflación creado con {inflationByYear.Count} años");

            var datums = _dataService
                .GetAllDatums()
                .Where(d => d.DataId != 13 && d.DataId != 14 && d.DataId != 15 && d.DataId != 17)
                .ToList();

            Console.WriteLine($"Calculando Sortino 20Y para {datums.Count} activos...");
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
                        Console.WriteLine($"  DataId={datum.DataId}: No hay datos históricos suficientes");
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
                        Console.WriteLine($"  DataId={datum.DataId}: Insuficientes series anuales ({yearlySeries.Count})");
                        continue;
                    }

                    Console.WriteLine($"  DataId={datum.DataId}: Años de datos {yearlySeries.First().Year} - {yearlySeries.Last().Year}");

                    // Calcular retornos reales ajustados por inflación
                    var realReturns = new List<double>();

                    for (var i = 1; i < yearlySeries.Count; i++)
                    {
                        var yearCurrent = yearlySeries[i].Year;
                        var yearPrevious = yearlySeries[i - 1].Year;
                        var valueCurrent = (double)yearlySeries[i].Data.Value;
                        var valuePrevious = (double)yearlySeries[i - 1].Data.Value;

                        if (valuePrevious <= 0 || valueCurrent <= 0)
                        {
                            Console.WriteLine($"    Año {yearCurrent}: Valores no válidos");
                            continue;
                        }

                        // Verificar que tenemos inflación para ese año
                        if (!inflationByYear.ContainsKey(yearCurrent) || !inflationByYear.ContainsKey(yearPrevious))
                        {
                            Console.WriteLine($"    Año {yearCurrent}: No hay datos de inflación");
                            continue;
                        }

                        // Calcular retorno logarítmico del activo
                        var R = Math.Log(valueCurrent / valuePrevious);

                        // Calcular retorno logarítmico de la inflación
                        var inflationCurrent = (double)inflationByYear[yearCurrent];
                        var inflationPrevious = (double)inflationByYear[yearPrevious];

                        if (inflationPrevious <= 0 || inflationCurrent <= 0)
                        {
                            Console.WriteLine($"    Año {yearCurrent}: Valores de inflación no válidos");
                            continue;
                        }

                        var I = Math.Log(inflationCurrent / inflationPrevious);

                        // Retorno real: (1+R)/(1+I) - 1
                        // R e I ya son log returns, se usan directamente
                        var realReturn = (1.0 + R) / (1.0 + I) - 1.0;
                        realReturns.Add(realReturn);
                    }

                    if (realReturns.Count < 2)
                    {
                        Console.WriteLine($"  DataId={datum.DataId}: Insuficientes retornos reales ({realReturns.Count})");
                        continue;
                    }

                    Console.WriteLine($"  DataId={datum.DataId}: Retornos reales calculados: {realReturns.Count}");

                    // Calcular Sortino Ratio
                    // Numerador: media aritmética de retornos reales
                    var numerator = realReturns.Average();

                    // Denominador: desviación a la baja
                    // Elevar al cuadrado solo retornos negativos, promediar dividiendo por N (no N-1)
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
                        Console.WriteLine($"  DataId={datum.DataId}: Sortino inválido (NaN o Infinity)");
                        continue;
                    }

                    var saved = await _dataStadisticService.CreateOrUpdate(new DataStadistic
                    {
                        DataId = datum.DataId,
                        Sortino20y = (float?)sortino
                    });

                    Console.WriteLine($"  ✓ DataId={saved.DataId}, {datum.DataName}, Sortino 20Y={saved.Sortino20y:F6}");
                    successCount++;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  ✗ Error calculando Sortino20Y para DataId={datum.DataId}: {ex.Message}\n{ex.StackTrace}");
                }
            }

            Console.WriteLine($"Cálculo de Sortino 20Y finalizado. Activos procesados exitosamente: {successCount}/{datums.Count}");
        }
    }
}
