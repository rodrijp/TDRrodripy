using Microsoft.Extensions.Configuration;

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

        // Por ahora no hacemos nada con workPath (no-op)
    }
}
