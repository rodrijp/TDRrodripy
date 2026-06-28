using System.Threading.Tasks;

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
    }
}
