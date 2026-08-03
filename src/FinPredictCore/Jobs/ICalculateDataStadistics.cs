using System.Threading.Tasks;

namespace FinPredictCore.Jobs
{
	/// <summary>
	/// Interfaz para ejecutar los cálculos de estadísticas de datos.
	/// </summary>
	public interface ICalculateDataStadistics
	{
		/// <summary>
		/// Ejecuta los cálculos de estadísticas de datos.
		/// </summary>
		Task Do();
	}
}

