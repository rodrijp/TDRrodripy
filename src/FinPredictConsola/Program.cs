// AppConsola1/Program.cs
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using FinPredictCore.DependencyInjection;
using FinPredictCore.Jobs;

// ✅ USAR EL BUILDER COMPARTIDO - Así de simple!
var builder = ServiceCollectionExtensions.CreateSharedHostBuilder(args);


builder.ConfigureLogging(logging =>
{
    logging.ClearProviders();
    logging.AddSimpleConsole(options =>
    {
        options.SingleLine = true;
        options.TimestampFormat = "HH:mm:ss ";
    });
    logging.SetMinimumLevel(LogLevel.Warning); // Minimo porque sino el ef muestra demasiada información
});

using var host = builder.Build();

using (var scope = host.Services.CreateScope())
{
    Console.WriteLine("=== Iniciando la aplicación ===");

  // Ejecutar importación de fuentes a la base de datos
    var importer = scope.ServiceProvider.GetRequiredService<IImportFuentesToDB>();
 importer.Do();
  // Ejecutar cálculo de correlaciones y guardado en BD
     var createRelation = scope.ServiceProvider.GetRequiredService<ICreateDataRelation>();
     await createRelation.Do();
  // Ejecutar cálculo de CAGR y guardado en BD
 var cagrJob = scope.ServiceProvider.GetRequiredService<ICalculateDataStadistics>();
   await cagrJob.Do();

  // Ejecutar cálculo del R-score y CAGR 20Y
   var rScoreJob = scope.ServiceProvider.GetRequiredService<IRScoreEngineCalculate>();
   await rScoreJob.Do();

  // Ejecutar cálculo del RAsignation y guardado en BD
  var rAsignationJob = scope.ServiceProvider.GetRequiredService<IRAsignationCalculate>();
  await rAsignationJob.Do();

  // CalculateNegVol30Y ya se ejecuta dentro de rScoreJob.Do()
}

await host.RunAsync();
/*using FinPredictData.Context;
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
}*/
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