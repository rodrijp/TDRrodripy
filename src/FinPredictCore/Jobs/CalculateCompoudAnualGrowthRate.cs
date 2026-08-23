using System;
using System.Linq;
using System.Threading.Tasks;
using FinPredictCore.Service.Data;
using FinPredictCore.Service.DataStadistic;
using FinPredictCore.Service.HistoricalData;
using FinPredictData.Models;

namespace FinPredictCore.Jobs
{
	public class CalculateCompoudAnualGrowthRate : ICalculateDataStadistics
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

        private static double ToFactor(double v) =>
            // - Si el valor está en formato decimal (ej. 0.20 = 20%), usar 1 + v.
            // - Si el valor está en formato porcentaje (ej. 20 = 20%), usar 1 + v/100.
            // Esto evita convertir 1 (1%) a 100% y obtener factor cero.
            //			if (Math.Abs(v) < 1.0)
            //			{
            //				return 1.0 + v;
            //			}

            1.0 + v / 100.0;
    }
}

