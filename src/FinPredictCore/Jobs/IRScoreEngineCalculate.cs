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
    }
}
