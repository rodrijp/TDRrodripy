using FinPredictData.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

var config = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();

var optionsBuilder = new DbContextOptionsBuilder<TDRMercatContext>();
optionsBuilder.UseNpgsql(config.GetConnectionString("TDRMercatDB"));

using var context = new TDRMercatContext(optionsBuilder.Options);

Console.WriteLine("=== Tabla Source ===");
foreach (var source in context.Sources)
{
    Console.WriteLine($"{source.SourceId} - {source.SourceName}");
}

Console.WriteLine("\n=== Tabla Data ===");
foreach (var datum in context.Data)
{
    Console.WriteLine($"{datum.DataId} - {datum.DataName}");
}
/*
var folderPath = @"c:\tdr\trabajo";
//var folderPath = args.Length > 0 ? args[0] : PedirCarpeta();

if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath))
{
    Console.WriteLine("La carpeta no existe.");
    return;
}

var csvFiles = Directory.GetFiles(folderPath, "*.csv");
if (csvFiles.Length == 0)
{
    Console.WriteLine("No se encontraron archivos .csv.");
    return;
}

var procesador = new Macrotrends();

foreach (var file in csvFiles)
{
    var dir = Path.GetDirectoryName(file) ?? ".";
    var sinExt = Path.GetFileNameWithoutExtension(file);
    var salida = Path.Combine(dir, $"{sinExt}(arreglado).csv");

    procesador.LimpiarCSV(file, salida);
    Console.WriteLine($"Procesado: {Path.GetFileName(file)}");
}

static string PedirCarpeta()
{
    Console.Write("Introduce la ruta de la carpeta: ");
    return Console.ReadLine() ?? "";
}
*/