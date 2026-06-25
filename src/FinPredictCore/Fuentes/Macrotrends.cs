using System.Globalization;
using Microsoft.Playwright;

namespace FinPredictCore.Fuentes;

public class Macrotrends
{

    

    public void LimpiarCSV(string rutaOrigen, string rutaDestino)
    {
        var lineas = File.ReadAllLines(rutaOrigen);
        if (lineas.Length < 2) return;

        var cabecera = lineas[0];
        var datos = new List<(DateTime fecha, decimal valor)>();

        for (int i = 1; i < lineas.Length; i++)
        {
            var partes = lineas[i].Split(';');
            if (partes.Length < 2) continue;

            if (DateTime.TryParse(partes[0].Trim().Replace("\"", ""), CultureInfo.InvariantCulture, out var fecha) &&
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

        var lineasSalida = new List<string> { cabecera };
        lineasSalida.AddRange(
            seleccionados.Select(d => $"{d.fecha:MM/dd/yyyy},{d.valor.ToString(CultureInfo.InvariantCulture)}"));

        File.WriteAllText(rutaDestino, string.Join(Environment.NewLine, lineasSalida));
    }

    public async Task DownloadCSVAsync(string url, string rutaDestino)
    {
        var playwright = await Playwright.CreateAsync();
        var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        
        // Configurar contexto para aceptar descargas
        var context = await browser.NewContextAsync(new BrowserNewContextOptions 
        { 
            AcceptDownloads = true
        });
        
        var page = await context.NewPageAsync();

        try
        {
            await page.GotoAsync(url);
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Hacer click en el icono de exportar para descargar
            var exportIcon = page.Locator("g:has-text(\"Download Data\")").First;
            var iconCount = await page.Locator("g:has-text(\"Download Data\")").CountAsync();
            Console.WriteLine($"INFO: Found {iconCount} export icons");
            
            await exportIcon.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 30000 });
            await exportIcon.ScrollIntoViewIfNeededAsync();

            // Iniciar espera de descarga ANTES del click
            var downloadTask = page.WaitForDownloadAsync();
            
            // Hacer click para descargar
            await exportIcon.ClickAsync(new LocatorClickOptions { Force = true });
            
            // Esperar a que se complete la descarga
            var download = await downloadTask;

            // Guardar el archivo en la ruta destino
            await download.SaveAsAsync(rutaDestino);
            Console.WriteLine($"INFO: Archivo descargado exitosamente a {rutaDestino}");
        }
        catch (Exception ex)
        {
            Console.WriteLine("ERROR en DownloadCSVAsync:");
            Console.WriteLine(ex);
            throw;
        }
        finally
        {
            await page.CloseAsync();
            await context.CloseAsync();
            await browser.CloseAsync();
        }
    }
}
