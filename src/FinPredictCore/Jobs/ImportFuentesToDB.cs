using System.IO;
using Microsoft.Extensions.Configuration;
using FinPredictCore.Fuentes;

namespace FinPredictCore.Jobs;

public class ImportFuentesToDB : IImportFuentesToDB
{
    private readonly IConfiguration _configuration;

    public ImportFuentesToDB(IConfiguration configuration)
    {
        _configuration = configuration;
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
    }
}
