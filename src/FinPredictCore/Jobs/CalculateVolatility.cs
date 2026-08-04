using System;
using System.Linq;
using System.Threading.Tasks;
using FinPredictCore.Service.Data;
using FinPredictCore.Service.DataStadistic;
using FinPredictCore.Service.HistoricalData;
using FinPredictData.Models;

namespace FinPredictCore.Jobs
{
	public class CalculateVolatility : ICalculateVolatility
	{
		private readonly IHistoricalDataService _historicalDataService;
		private readonly IDataService _dataService;
		private readonly IDataStadisticService _dataStadisticService;

		public CalculateVolatility(
			IHistoricalDataService historicalDataService,
			IDataService dataService,
			IDataStadisticService dataStadisticService)
		{
			_historicalDataService = historicalDataService;
			_dataService = dataService;
			_dataStadisticService = dataStadisticService;
		}

		public async Task Do()
		{
			var datums = _dataService.GetAllDatums().ToList();
			Console.WriteLine($"Iniciando cálculo de volatilidad para {datums.Count} activos...");

			foreach (var datum in datums)
			{
				try
				{
					if (!datum.IsValue)
					{
						continue;
					}

					var historical = (await _historicalDataService.GetHistoricalDataByData(datum.DataId))
						.OrderBy(h => h.Date)
						.ToList();

					if (historical.Count < 2)
					{
						continue;
					}

					var increases = historical
						.Zip(historical.Skip(1), (previous, current) =>
						{
							if (previous.Value <= 0 || current.Value <= 0)
							{
								return double.NaN;
							}

							return Math.Log((double)current.Value / previous.Value);
						})
						.Where(increase => !double.IsNaN(increase) && !double.IsInfinity(increase))
						.ToList();

					if (increases.Count < 2)
					{
						continue;
					}

					var median = increases.Sum() / increases.Count;

					var variance = increases
						.Sum(increase => Math.Pow(increase - median, 2)) / (increases.Count - 1);

					var volatility = Math.Sqrt(variance);

					var saved = await _dataStadisticService.CreateOrUpdate(new DataStadistic
					{
						DataId = datum.DataId,
						Volatilidad = (float?)volatility
					});

					Console.WriteLine($"    → Volatilidad para {datum.DataId} {datum.DataName}: {volatility}");
					Console.WriteLine($"    → Guardado: DataId={saved.DataId}, Volatilidad={(saved.Volatilidad.HasValue ? saved.Volatilidad.Value.ToString("F6") : "(n/a)")}");
				}
				catch (Exception ex)
				{
					Console.WriteLine($"  -> Error calculando volatilidad para {datum.DataId}: {ex.Message}");
				}
			}
		}
	}
}

