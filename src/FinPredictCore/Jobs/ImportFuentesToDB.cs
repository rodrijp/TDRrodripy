using System.Globalization;
using System.IO;
using FinPredictCore.Fuentes;
using FinPredictCore.Service.HistoricalData;
using FinPredictData.Models;
using Microsoft.Extensions.Configuration;

namespace FinPredictCore.Jobs;

public class ImportFuentesToDB : IImportFuentesToDB
{
    private readonly IConfiguration _configuration;
    private readonly IHistoricalDataService _historicalDataService;

    public ImportFuentesToDB(IConfiguration configuration, IHistoricalDataService historicalDataService)
    {
        _configuration = configuration;
        _historicalDataService = historicalDataService;
    }

    public void Do()
    {
        // Obtener el path de trabajo desde appsettings.json
        var workPath = _configuration["WorkingDirectories:Temporary"];
        if (string.IsNullOrWhiteSpace(workPath)) return;

        var source = Path.Combine(workPath, "ORO.csv");
        if (!File.Exists(source)) return;

        var dest = Path.Combine(workPath, "ORO.cleaned.csv");

        var cleaner = new Macrotrends();
        cleaner.LimpiarCSV(source, dest);

        if (!File.Exists(dest)) return;

        var lineas = File.ReadAllLines(dest);
        if (lineas.Length < 2) return;

        for (var i = 1; i < lineas.Length; i++)
        {
            var partes = lineas[i].Split(',');
            if (partes.Length < 2) continue;

            if (!DateOnly.TryParseExact(partes[0].Trim(), "MM/dd/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var fecha))
            {
                continue;
            }

            if (!decimal.TryParse(partes[1].Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var valorDecimal))
            {
                continue;
            }

            var historicalDatum = new HistoricalDatum
            {
                Date = fecha,
                DataId = 1,
                Value = (float)valorDecimal
            };

            _historicalDataService.CreateOrUpdate(historicalDatum).GetAwaiter().GetResult();
        }
    }
}
