using System;
using System.IO;
using System.Threading.Tasks;

namespace FinPredictCore.Jobs
{
	public class CalculateVolatility : ICalculateVolatility
	{
		public Task Do()
		{
			var filePath = @"C:\TDR\TDRrodripy\data\S&P 500 TR.csv";
			var values = 0.0;
			var count = 0;
			var percentages = new System.Collections.Generic.List<double>();

			if (File.Exists(filePath))
			{
				var lines = File.ReadAllLines(filePath);

				foreach (var line in lines)
				{
					if (string.IsNullOrWhiteSpace(line))
						continue;

					var parts = line.Split(',');
					if (parts.Length < 2)
						continue;

					if (double.TryParse(parts[1], out var percentageValue))
					{
						values += percentageValue;
						percentages.Add(percentageValue);
						count++;
					}
				}
			}

			var median = count > 0 ? values / count : 0.0;
			var squaredDifferencesSum = 0.0;

			foreach (var percentage in percentages)
			{
				var difference = percentage - median;
				squaredDifferencesSum += difference * difference;
			}

			var variance = count > 1 ? squaredDifferencesSum / (count - 1) : 0.0;
			var volatility = Math.Sqrt(variance);

			Console.WriteLine($"Media de valores porcentuales: {median}");
			Console.WriteLine($"Volatilidad: {volatility}");

			return Task.CompletedTask;
		}
	}
}
