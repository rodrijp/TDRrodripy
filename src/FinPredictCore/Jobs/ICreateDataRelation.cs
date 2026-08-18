using System.Threading.Tasks;
using FinPredictData.Models;
using static FinPredictCore.Jobs.CreateDataRelation;

namespace FinPredictCore.Jobs
{
    /// <summary>
    /// Interfaz para crear relaciones de datos.
    /// </summary>
    public interface ICreateDataRelation
    {
        /// <summary>
        /// Ejecuta la operación de creación de la relación de datos.
        /// </summary>
        Task Do();
        Task<double> CalculaCorrelación(Datum datum1, Datum datum2, TypeDatum type, int year = 0);
    }
}
