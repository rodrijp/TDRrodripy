using FinPredictCore.Fuentes;
using Microsoft.Extensions.Configuration;

var config = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();

var connectionString = config.GetConnectionString("TDRMercatDB");
Console.ReadKey();

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