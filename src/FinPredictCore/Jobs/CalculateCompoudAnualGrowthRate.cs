using System;
using System.Linq;
using System.Threading.Tasks;
using FinPredictCore.Service.Data;
using FinPredictCore.Service.DataStadistic;
using FinPredictCore.Service.HistoricalData;
using FinPredictData.Models;

namespace FinPredictCore.Jobs
{
	public class CalculateCompoudAnualGrowthRate : ICalculateCompoudAnualGrowthRate
	{
		private readonly IHistoricalDataService _historicalDataService;
		private readonly IDataService _dataService;
		private readonly IDataStadisticService _compoundService;

		public CalculateCompoudAnualGrowthRate(
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

					}

					var cagrFloat = cagr.HasValue ? (float?)cagr.Value : null;

					Console.WriteLine($"    → Calculada CAGR para {datum.DataId} {datum.DataName}: {(cagr.HasValue ? cagr.Value.ToString("P6") : "(n/a)")}");

					var saved = await _compoundService.CreateOrUpdate(new DataStadistic
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

		private static double ToFactor(double v)
		{
			// Para valores de porcentaje:
			// - Si el valor está en formato decimal (ej. 0.20 = 20%), usar 1 + v.
			// - Si el valor está en formato porcentaje (ej. 20 = 20%), usar 1 + v/100.
			// Esto evita convertir 1 (1%) a 100% y obtener factor cero.
			if (Math.Abs(v) < 1.0)
			{
				return 1.0 + v;
			}

			return 1.0 + v / 100.0;
		}
	}
}

