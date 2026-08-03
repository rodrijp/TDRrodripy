using System.Threading.Tasks;

namespace FinPredictCore.Jobs
{
	/// <summary>
	/// Interfaz para calcular la volatilidad.
	/// </summary>
	public interface ICalculateVolatility
	{
		/// <summary>
		/// Ejecuta el cálculo de la volatilidad.
		/// </summary>
		Task Do();
	}
}
