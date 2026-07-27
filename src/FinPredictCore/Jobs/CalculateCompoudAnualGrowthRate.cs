using System;
using System.Linq;
using System.Threading.Tasks;
using FinPredictCore.Service.CompoundAnualGrowthRate;
using FinPredictCore.Service.Data;
using FinPredictCore.Service.HistoricalData;
using FinPredictData.Models;

namespace FinPredictCore.Jobs
{
	public class CalculateCompoudAnualGrowthRate : ICalculateCompoudAnualGrowthRate
	{
		private readonly IHistoricalDataService _historicalDataService;
		private readonly IDataService _dataService;
		private readonly ICompoundAnualGrowthRateService _compoundService;

		public CalculateCompoudAnualGrowthRate(
			IHistoricalDataService historicalDataService,
			IDataService dataService,
			ICompoundAnualGrowthRateService compoundService)
		{
			_historicalDataService = historicalDataService;
			_dataService = dataService;
			_compoundService = compoundService;
		}

		public async Task Do()
		{
			var datums = _dataService.GetAllDatums().ToList();
			Console.WriteLine($"Iniciando cálculo de CAGR para {datums.Count} activos...");

			foreach (var datum in datums)
			{
				try
				{
					var historical = (await _historicalDataService.GetHistoricalDataByData(datum.DataId))
						.OrderBy(h => h.Date)
						.ToList();

					if (historical.Count < 2)
					{
						Console.WriteLine($"  -> {datum.DataId} {datum.DataName}: no hay datos suficientes.");
						await _compoundService.CreateOrUpdate(new CompoundAnualGrowthRate { DataId = datum.DataId, Cagr = null });
						continue;
					}

					// Primero, construir primer valor de cada año (primer registro encontrado en ese año)
					var firstValueByYear = historical
						.GroupBy(h => h.Date.Year)
						.ToDictionary(g => g.Key, g => g.First().Value);

					var years = firstValueByYear.Keys.OrderBy(y => y).ToList();

					var annualCagrs = new System.Collections.Generic.List<double>();

					// Para cada año que tenga el año siguiente, calcular CAGR con n = 1 (valor_next / valor_this)^(1/1)-1
					for (int i = 0; i < years.Count - 1; i++)
					{
						var y = years[i];
						var next = years[i + 1];
						if (!firstValueByYear.TryGetValue(y, out var v1) || !firstValueByYear.TryGetValue(next, out var v2))
							continue;

						if (v1 <= 0 || v2 <= 0)
							continue;

						var r = Math.Pow((double)(v2 / v1), 1.0 / (next - y)) - 1.0;
						if (!double.IsNaN(r) && !double.IsInfinity(r))
							annualCagrs.Add(r);
					}

					double? meanCagr = null;

					if (annualCagrs.Count > 0)
					{
						meanCagr = annualCagrs.Average();
					}
					else
					{
						// Fallback: usar periodo completo
						var first = historical.First();
						var last = historical.Last();
						var n = last.Date.Year - first.Date.Year;
						if (n > 0 && first.Value > 0 && last.Value > 0)
						{
							var r = Math.Pow((double)(last.Value / first.Value), 1.0 / n) - 1.0;
							if (!double.IsNaN(r) && !double.IsInfinity(r))
								meanCagr = r;
						}
					}

					var cagrFloat = meanCagr.HasValue ? (float?)meanCagr.Value : null;

					Console.WriteLine($"    → Calculada media CAGR para {datum.DataId} {datum.DataName}: {(meanCagr.HasValue ? meanCagr.Value.ToString("P6") : "(n/a)")}");
					Console.WriteLine($"    → Guardando en BD: DataId={datum.DataId}, Cagr={(cagrFloat.HasValue ? cagrFloat.Value.ToString("G10") : "null")}");

					var saved = await _compoundService.CreateOrUpdate(new CompoundAnualGrowthRate
					{
						DataId = datum.DataId,
						Cagr = cagrFloat
					});

					Console.WriteLine($"    → Guardado: DataId={saved.DataId}, Cagr={(saved.Cagr.HasValue ? saved.Cagr.Value.ToString("P6") : "(n/a)")}");
				}
				catch (Exception ex)
				{
					Console.WriteLine($"  -> Error calculando CAGR para {datum.DataId}: {ex.Message}");
				}
			}

			Console.WriteLine("Cálculo de CAGR finalizado.");
		}
	}
}

