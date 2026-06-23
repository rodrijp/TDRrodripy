using System.Globalization;

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

foreach (var file in csvFiles)
{
    ProcesarArchivo(file);
    Console.WriteLine($"Procesado: {Path.GetFileName(file)}");
}

static string PedirCarpeta()
{
    Console.Write("Introduce la ruta de la carpeta: ");
    return Console.ReadLine() ?? "";
}

static void ProcesarArchivo(string ruta)
{
    var lineas = File.ReadAllLines(ruta);
    if (lineas.Length < 2) return;

    var cabecera = lineas[0];
    var datos = new List<(DateTime fecha, decimal valor)>();

    for (int i = 1; i < lineas.Length; i++)
    {
        var partes = lineas[i].Split(';');
        if (partes.Length < 2) continue;

        if (DateTime.TryParse(partes[0].Trim().Replace("\"",""),  CultureInfo.InvariantCulture, out var fecha) &&
            decimal.TryParse(partes[1].Trim(), new CultureInfo("es-ES"), out var valor))
        {
            datos.Add((fecha, valor));
        }
    }

    var seleccionados = datos
        .GroupBy(d => new { d.fecha.Year, d.fecha.Month })
        .Select(g => g.OrderBy(d => d.fecha).First())
        .OrderBy(d => d.fecha)
        .ToList();

    var dir = Path.GetDirectoryName(ruta) ?? ".";
    var sinExt = Path.GetFileNameWithoutExtension(ruta);
    var salida = Path.Combine(dir, $"{sinExt}(arreglado).csv");

    var lineasSalida = new List<string> { cabecera };
    lineasSalida.AddRange(
        seleccionados.Select(d => $"{d.fecha:MM/dd/yyyy},{d.valor}"));

    File.WriteAllText(salida, string.Join(Environment.NewLine, lineasSalida));
    Console.WriteLine($"Creado: {Path.GetFileName(salida)}");
}
