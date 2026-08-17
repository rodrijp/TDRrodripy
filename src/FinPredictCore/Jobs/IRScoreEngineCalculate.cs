using System.Threading.Tasks;

namespace FinPredictCore.Jobs
{
    /// <summary>
    /// Interfaz para ejecutar el cálculo del engine R-Score.
    /// </summary>
    public interface IRScoreEngineCalculate
    {
        /// <summary>
        /// Ejecuta el cálculo del engine R-Score.
        /// </summary>
        Task Do();

        /// <summary>
        /// Calcula la volatilidad negativa de los últimos 30 años para cada activo.
        /// </summary>
        Task CalculateNegVol30Y();

        /// <summary>
        /// Calcula el Sortino Ratio ajustado por inflación para los últimos 20 años.
        /// </summary>
        Task CalculateSortino20Y();
    }
}
