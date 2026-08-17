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
	public class CalculateDataStadistics : ICalculateDataStadistics
	{
		private readonly IHistoricalDataService _historicalDataService;
		private readonly IDataService _dataService;
		private readonly IDataStadisticService _compoundService;

		public CalculateDataStadistics(
			IHistoricalDataService historicalDataService,
			IDataService dataService,
			IDataStadisticService compoundService)
		{
			_historicalDataService = historicalDataService;
			_dataService = dataService;
			_compoundService = compoundService;
		}

		public async Task Do()
		{
			await CalculateCompoundAnualGrowthRate();
			await CalculateVolatibility();
			await CalculateSharpe();
			await CalculateSortino();
		}

		#region CAGR

		private async Task CalculateCompoundAnualGrowthRate()
		{
			var datums = _dataService.GetAllDatums().ToList();
			Console.WriteLine($"Iniciando cálculo de CAGR para {datums.Count} activos...");

			foreach (var datum in datums)
			{
				try
				{
					if (datum.DataId == 14 || datum.DataId == 15)
					{
						Console.WriteLine($"  -> {datum.DataId} {datum.DataName}: No se calculará CAGR.");
						continue;
					}


					var historical = (await _historicalDataService.GetHistoricalDataByData(datum.DataId))
						.OrderBy(h => h.Date)
						.ToList();

					if (historical.Count < 2)
					{
						Console.WriteLine($"  -> {datum.DataId} {datum.DataName}: no hay datos suficientes.");
						await _compoundService.CreateOrUpdate(new DataStadistic { DataId = datum.DataId, Cagr = null });
						continue;
					}

					double? cagr = null;

					if (datum.IsValue)
					{
						var first = historical.First();
						var last = historical.Last();
						var firstDate = first.Date.ToDateTime(TimeOnly.MinValue);
						var lastDate = last.Date.ToDateTime(TimeOnly.MinValue);
						var years = (lastDate - firstDate).TotalDays / 365.25;

						if (first.Value > 0 && last.Value > 0)
						{
							cagr = Math.Pow((double)(last.Value / first.Value), 1.0 / years) - 1.0;
						}
					}
					else
					{
						var annualHistorical = historical
							.GroupBy(h => h.Date.Year)
							.Select(g => g.Last())
							.OrderBy(h => h.Date)
							.ToList();

						if (annualHistorical.Count >= 2)
						{
							var first = annualHistorical.First();
							var last = annualHistorical.Last();
							var firstDate = first.Date.ToDateTime(TimeOnly.MinValue);
							var lastDate = last.Date.ToDateTime(TimeOnly.MinValue);
							var years = (lastDate - firstDate).TotalDays / 365.25;

							if (years > 0)
							{
								const double initialValue = 100.0;
								var accumulatedValue = initialValue;

								foreach (var yearlyRate in annualHistorical)
								{
									var factor = ToFactor(yearlyRate.Value);
									if (factor <= 0)
									{
										accumulatedValue = double.NaN;
										break;
									}

									accumulatedValue *= factor;
								}

								if (!double.IsNaN(accumulatedValue) && accumulatedValue > 0)
								{
									cagr = Math.Pow(accumulatedValue / initialValue, 1.0 / years) - 1.0;
								}
							}
						}
					}

					var cagrFloat = cagr.HasValue ? (float?)cagr.Value : null;

					var saved = await _compoundService.CreateOrUpdate(new DataStadistic
					{
						DataId = datum.DataId,
						Cagr = cagrFloat
					});

					Console.WriteLine($"    → Calculada CAGR para {datum.DataId} {datum.DataName}: {(cagr.HasValue ? cagr.Value.ToString("P6") : "(n/a)")}");
					Console.WriteLine($"    → Guardado: DataId={saved.DataId}, Cagr={(saved.Cagr.HasValue ? saved.Cagr.Value.ToString("P6") : "(n/a)")}");
				}
				catch (Exception ex)
				{
					Console.WriteLine($"  -> Error calculando CAGR para {datum.DataId}: {ex.Message}");
				}
			}

			Console.WriteLine("Cálculo de CAGR finalizado.");
		}

		#endregion

		#region Volatility
		private async Task CalculateVolatibility()
		{
			var datums = _dataService.GetAllDatums().ToList();
			Console.WriteLine($"Iniciando cálculo de volatilidad para {datums.Count} activos...");

			foreach (var datum in datums)
			{
				try
				{
					var historical = (await _historicalDataService.GetHistoricalDataByData(datum.DataId))
						.OrderBy(h => h.Date)
						.ToList();

					var annualHistorical = GetAnnualHistoricalValues(historical);
					if (annualHistorical.Count < 2)
					{
						continue;
					}

					var logReturns = BuildLogReturns(datum, annualHistorical);
					if (logReturns.Count < 2)
					{
						continue;
					}

					var crudeVolatility = CalculateHistoricalVolatility(logReturns);
					var detrendedVolatility = CalculateDetrendedVolatility(logReturns);
					if (double.IsNaN(crudeVolatility) || double.IsInfinity(crudeVolatility))
					{
						continue;
					}

					var saved = await _compoundService.CreateOrUpdate(new DataStadistic
					{
						DataId = datum.DataId,
						Volatilidadcruda = (float?)crudeVolatility,
						Volatilidaddetendenciada = (float?)detrendedVolatility
					});

					Console.WriteLine($"    → Volatilidad para {datum.DataId} {datum.DataName}: Cruda={crudeVolatility}, Detendenciada={detrendedVolatility}");
					Console.WriteLine($"    → Guardado: DataId={saved.DataId}, Volatilidadcruda={(saved.Volatilidadcruda.HasValue ? saved.Volatilidadcruda.Value.ToString("F6") : "(n/a)" )}, Volatilidaddetendenciada={(saved.Volatilidaddetendenciada.HasValue ? saved.Volatilidaddetendenciada.Value.ToString("F6") : "(n/a)")}");
				}
				catch (Exception ex)
				{
					Console.WriteLine($"  -> Error calculando volatilidad para {datum.DataId}: {ex.Message}");
				}
			}

			Console.WriteLine("Cálculo de volatilidad finalizado.");
		}

		#endregion

		#region Sharpe
		private async Task CalculateSharpe()
		{
			var datums = _dataService.GetAllDatums().ToList();
			Console.WriteLine($"Iniciando cálculo de Sharpe para {datums.Count} activos...");

			var riskFreeStat = await _compoundService.GetByDataId(11);
			var riskFreeRate = riskFreeStat?.Cagr;
			if (!riskFreeRate.HasValue)
			{
				Console.WriteLine("  -> No se encontró el rendimiento libre de riesgo para DataId=11.");
				return;
			}

			foreach (var datum in datums)
			{
				if (datum.DataId == 13 || datum.DataId == 14 || datum.DataId == 15 || datum.DataId == 17)
				{
					Console.WriteLine($"  -> {datum.DataId} {datum.DataName}: no se calculará Sharpe.");
					continue;
				}

				try
				{
					var datumStats = await _compoundService.GetByDataId(datum.DataId);
					var rendimiento = datumStats?.Cagr;
					var volatilidad = datumStats?.Volatilidaddetendenciada;	

					if (!rendimiento.HasValue)
					{
						Console.WriteLine($"  -> {datum.DataId} {datum.DataName}: no hay CAGR disponible para Sharpe.");
						continue;
					}

					if (!volatilidad.HasValue || volatilidad.Value <= 0 || double.IsNaN(volatilidad.Value) || double.IsInfinity(volatilidad.Value))
					{
						Console.WriteLine($"  -> {datum.DataId} {datum.DataName}: no hay volatilidad cruda válida para Sharpe.");
						continue;
					}

					var sharpe = (rendimiento.Value - riskFreeRate.Value) / volatilidad.Value;

					var saved = await _compoundService.CreateOrUpdate(new DataStadistic
					{
						DataId = datum.DataId,
						Sharpe = double.IsNaN(sharpe) || double.IsInfinity(sharpe) ? null : (float?)sharpe
					});

					Console.WriteLine($"    → Sharpe para {datum.DataId} {datum.DataName}: {(saved.Sharpe.HasValue ? saved.Sharpe.Value.ToString("F6") : "(n/a)")}");
				}
				catch (Exception ex)
				{
					Console.WriteLine($"  -> Error calculando Sharpe para {datum.DataId}: {ex.Message}");
				}
			}

			Console.WriteLine("Cálculo de Sharpe finalizado.");
		}

		#endregion

		private async Task CalculateSortino()
		{
			var datums = _dataService.GetAllDatums().ToList();
			Console.WriteLine($"Iniciando cálculo de Sortino para {datums.Count} activos...");

			foreach (var datum in datums)
			{
				if (datum.DataId == 14 || datum.DataId == 15 || datum.DataId == 17)
				{
					Console.WriteLine($"  -> {datum.DataId} {datum.DataName}: No se calculará Sortino.");
					continue;
				}

				try
				{
					var historical = (await _historicalDataService.GetHistoricalDataByData(datum.DataId))
						.OrderBy(h => h.Date)
						.ToList();
					var annualHistorical = GetAnnualHistoricalValues(historical);
					var returns = BuildLogReturns(datum, annualHistorical);

					var sortino = CalculateSortinoRatio(returns);
					var saved = await _compoundService.CreateOrUpdate(new DataStadistic
					{ 
						DataId = datum.DataId,
						Sortino = double.IsNaN(sortino) || double.IsInfinity(sortino) ? null : (float?)sortino
					});

					Console.WriteLine($"    → Sortino para {datum.DataId} {datum.DataName}: {(saved.Sortino.HasValue ? saved.Sortino.Value.ToString("F6") : "(n/a)")}");
				}
				catch (Exception ex)
				{
					Console.WriteLine($"  -> Error calculando Sortino para {datum.DataId}: {ex.Message}");
				}
			}

			Console.WriteLine("Cálculo de Sortino finalizado.");
		}

		private static double CalculateSortinoRatio(IReadOnlyList<double> returns, double targetReturn = 0)
		{
			if (returns.Count == 0)
			{
				return double.NaN;
			}

			var excessReturns = returns.Select(value => value - targetReturn).ToList();
			var downsideReturns = excessReturns.Where(value => value < 0).ToList();
			if (downsideReturns.Count == 0)
			{
				return double.NaN;
			}

			var downsideDeviation = Math.Sqrt(downsideReturns.Sum(value => value * value) / returns.Count);
			var averageExcessReturn = excessReturns.Average();
			return downsideDeviation > 0 ? averageExcessReturn / downsideDeviation : double.NaN;
		}

		private static List<HistoricalDatum> GetAnnualHistoricalValues(List<HistoricalDatum> historical)
		{
			return historical
				.GroupBy(h => h.Date.Year)
				.Select(group => group.OrderByDescending(h => h.Date).First())
				.OrderBy(h => h.Date)
				.ToList();
		}

		private static List<double> BuildLogReturns(Datum datum, List<HistoricalDatum> annualHistorical)
		{
			var values = annualHistorical.Select(h => (double)h.Value).ToList();

			if (datum.IsValue)
			{
				return LogReturnsFromIndex(values);
			}

			return LogReturnsFromRate(values);
		}

		private static List<double> LogReturnsFromIndex(IReadOnlyList<double> values)
		{
			var results = new List<double>();

			for (var i = 1; i < values.Count; i++)
			{
				if (values[i - 1] <= 0 || values[i] <= 0)
				{
					continue;
				}

				results.Add(Math.Log(values[i] / values[i - 1]));
			}

			return results;
		}

		private static List<double> LogReturnsFromAnnualReturn(IReadOnlyList<double> values)
		{
			var results = new List<double>();

			foreach (var value in values)
			{
				if (value <= -1)
				{
					continue;
				}

				results.Add(Math.Log(1 + value));
			}

			return results;
		}

		private static List<double> LogReturnsFromRate(IReadOnlyList<double> values)
		{
			var results = new List<double>();

			for (var i = 1; i < values.Count; i++)
			{
				var factorPrev = ToFactor(values[i - 1]);
				var factorCurr = ToFactor(values[i]);
				if (factorPrev <= 0 || factorCurr <= 0)
				{
					continue;  //Tasa mas de 100% negativa.
				}
				results.Add(Math.Log(factorCurr / factorPrev));
			}

			return results;
		}

		private static double CalculateHistoricalVolatility(IReadOnlyList<double> values)
		{
			if (values.Count < 2)
			{
				return double.NaN;
			}

			var average = values.Average();
			var variance = values.Sum(value => Math.Pow(value - average, 2)) / (values.Count - 1);
			return Math.Sqrt(variance);
		}

		private static double CalculateDetrendedVolatility(IReadOnlyList<double> values, int movingAverageWindow = 5)
		{
			if (values.Count < movingAverageWindow)
			{
				return double.NaN;
			}

			var residuals = new List<double>();

			for (var i = movingAverageWindow - 1; i < values.Count; i++)
			{
				var windowValues = values.Skip(i - (movingAverageWindow - 1)).Take(movingAverageWindow).ToList();
				var movingAverage = windowValues.Average();
				residuals.Add(values[i] - movingAverage);
			}

			return CalculateHistoricalVolatility(residuals);
		}

		private static double ToFactor(double v)
		{
			// - Si el valor está en formato decimal (ej. 0.20 = 20%), usar 1 + v.
			// - Si el valor está en formato porcentaje (ej. 20 = 20%), usar 1 + v/100.
			// Esto evita convertir 1 (1%) a 100% y obtener factor cero.
			// if (Math.Abs(v) < 1.0)
			// {
			// 	return 1.0 + v;
			// }

			return 1.0 + v / 100.0;
		}
	}
}

