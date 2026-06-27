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
        Console.WriteLine("[ImportFuentesToDB] Iniciant importació de fonts de Macrotrends...");
        var dataPath = _workingDirectories.Data;
        var tempPath = _workingDirectories.Temporary;
        if (string.IsNullOrWhiteSpace(dataPath) || string.IsNullOrWhiteSpace(tempPath))
        {
            Console.WriteLine("[ImportFuentesToDB] Error: camins no configurats correctament.");
            return;
        }

        var datos = _dataService.GetAllDatumsBySource(SourceUtil.SourceMacrotrends);
        Console.WriteLine($"[ImportFuentesToDB] S'han trobat {datos.Count()} fonts per importar.");

        foreach (var dato in datos)
        {
            if (string.IsNullOrWhiteSpace(dato.DataName)) continue;

            var source = Path.Combine(dataPath, $"{dato.DataName}.csv");
            Console.WriteLine($"[ImportFuentesToDB] Processant: {dato.DataName}");
            ImportarArchivo(source, dato.DataId, tempPath);
        }

        Console.WriteLine("[ImportFuentesToDB] Importació completada. Netejant arxius temporals...");
        // Limpiar los archivos procesados de Temporary
        CleanupTempFiles(tempPath);
        Console.WriteLine("[ImportFuentesToDB] Procés finalitzat.");
    }

    private void CleanupTempFiles(string tempPath)
    {
        var cleanedFiles = Directory.GetFiles(tempPath, "*.cleaned.csv");
        Console.WriteLine($"[ImportFuentesToDB] Suprimint {cleanedFiles.Length} arxius temporals...");
        foreach (var file in cleanedFiles)
        {
            File.Delete(file);
            Console.WriteLine($"[ImportFuentesToDB] Suprimit: {Path.GetFileName(file)}");
        }
    }

    private void ImportarArchivo(string source, short dataId, string tempPath)
    {
        Console.WriteLine($"[ImportarArchivo] Netejant arxiu: {Path.GetFileName(source)}");
        var dest = Path.Combine(tempPath, Path.GetFileNameWithoutExtension(source) + ".cleaned.csv");

        var cleaner = new Macrotrends();
        cleaner.LimpiarCSV(source, dest);

        if (!File.Exists(dest))
        {
            Console.WriteLine($"[ImportarArchivo] Error: arxiu net no generat per a {Path.GetFileName(source)}");
            return;
        }

        var lineas = File.ReadAllLines(dest);
        if (lineas.Length < 2)
        {
            Console.WriteLine($"[ImportarArchivo] Error: arxiu buit o amb format invàlid a {Path.GetFileName(dest)}");
            return;
        }

        var registrosValidos = 0;
        var registrosInvalidos = 0;

        for (var i = 1; i < lineas.Length; i++)
        {
            var partes = lineas[i].Split(',');
            if (partes.Length < 2)
            {
                registrosInvalidos++;
                continue;
            }

            if (!DateOnly.TryParseExact(partes[0].Trim(), "MM/dd/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var fecha))
            {
                registrosInvalidos++;
                continue;
            }

            if (!decimal.TryParse(partes[1].Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var valorDecimal))
            {
                registrosInvalidos++;
                continue;
            }

            var historicalDatum = new HistoricalDatum
            {
                Date = fecha,
                DataId = dataId,
                Value = (float)valorDecimal
            };

            _historicalDataService.CreateOrUpdate(historicalDatum).GetAwaiter().GetResult();
            registrosValidos++;
        }

        Console.WriteLine($"[ImportarArchivo] Completat: {registrosValidos} registres importats, {registrosInvalidos} registres descartats.");
    }
}
