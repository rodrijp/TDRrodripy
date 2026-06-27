using System.Globalization;
using System.IO;
using FinPredictCore.Configuration;
using FinPredictCore.Fuentes;
using FinPredictCore.Service.Data;
using FinPredictCore.Service.HistoricalData;
using FinPredictCore.Service.Source;
using FinPredictData.Models;
using Microsoft.Extensions.Options;

namespace FinPredictCore.Jobs;

public class ImportFuentesToDB : IImportFuentesToDB
{
    private readonly WorkingDirectoriesOptions _workingDirectories;
    private readonly IHistoricalDataService _historicalDataService;
    private readonly IDataService _dataService;

    public ImportFuentesToDB(IOptions<WorkingDirectoriesOptions> workingDirectoriesOptions, IHistoricalDataService historicalDataService, IDataService dataService)
    {
        _workingDirectories = workingDirectoriesOptions.Value;
        _historicalDataService = historicalDataService;
        _dataService = dataService;
    }

    public void Do()
    {
        ImportFuenteMacrotrendsToDB();
    }

    private void ImportFuenteMacrotrendsToDB()
    {
        var workPath = _workingDirectories.Temporary;
        if (string.IsNullOrWhiteSpace(workPath)) return;


        var datos = _dataService.GetAllDatumsBySource(SourceUtil.SourceMacrotrends);

        foreach (var dato in datos)
        {
            if (string.IsNullOrWhiteSpace(dato.DataName)) continue;



            var source = Path.Combine(workPath, $"{dato.DataName}.csv");
            ImportarArchivo(source, dato.DataId);
        }
    }

    private void ImportarArchivo(string source, short dataId)
    {

#pragma warning disable CS8604 // Possible null reference argument.
        var dest = Path.Combine(Path.GetDirectoryName(source), Path.GetFileNameWithoutExtension(source) + ".cleaned.csv");
#pragma warning restore CS8604 // Possible null reference argument.

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
                DataId = dataId,
                Value = (float)valorDecimal
            };

            _historicalDataService.CreateOrUpdate(historicalDatum).GetAwaiter().GetResult();
        }
    }
}
