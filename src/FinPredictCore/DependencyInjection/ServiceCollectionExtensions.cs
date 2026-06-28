// MiSolucion.Compartido/DependencyInjection/ServiceCollectionExtensions.cs
using FinPredictCore.Configuration;
using FinPredictCore.Jobs;
using FinPredictCore.Service.Data;
using FinPredictCore.Service.DataRelation;
using FinPredictCore.Service.HistoricalData;
using FinPredictData.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FinPredictCore.DependencyInjection;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Configura todos los servicios comunes para todas las aplicaciones de consola
    /// </summary>
    public static IServiceCollection AddSharedServices(this IServiceCollection services)
    {
        services.AddDbContext<TDRMercatContext>((serviceProvider, options) =>
        {
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            var connectionString = configuration.GetConnectionString("TDRMercatDB");
            options.UseNpgsql(connectionString);
        });

        services.AddScoped<IHistoricalDataService, HistoricalDataService>();
        services.AddScoped<IDataService, DataService>();
        services.AddScoped<IDataRelationService, DataRelationService>();
        services.AddScoped<IImportFuentesToDB, ImportFuentesToDB>();
        services.AddScoped<ICreateDataRelation, CreateDataRelation>();

        return services;
    }

    public static IServiceCollection AddSharedServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<WorkingDirectoriesOptions>(configuration.GetSection("WorkingDirectories"));
        return services.AddSharedServices();
    }

    /// <summary>
    /// Configura el logging común para todas las aplicaciones
    /// </summary>
    public static IServiceCollection AddSharedLogging(this IServiceCollection services)
    {
        services.AddLogging(logging =>
        {
            logging.AddConsole();
            logging.AddDebug();
            // Puedes agregar más providers aquí (Application Insights, etc.)
        });
        
        return services;
    }

    /// <summary>
    /// Método de extensión para crear un HostBuilder con configuración compartida
    /// </summary>
    public static IHostBuilder CreateSharedHostBuilder(string[] args)
    {
        return Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                // Agregar servicios compartidos
                services.AddSharedServices(context.Configuration);
                services.AddSharedLogging();
                
                // Aquí puedes agregar más configuraciones compartidas
                // como configuración de validación, AutoMapper, etc.
            });
    }
}