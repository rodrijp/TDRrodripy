using System.Threading.Tasks;

namespace FinPredictCore.Jobs
{
	/// <summary>
	/// Interfaz para calcular la tasa de crecimiento compuesto anual.
	/// </summary>
	public interface ICalculateCompoudAnualGrowthRate
	{
		/// <summary>
		/// Ejecuta el cálculo de la tasa de crecimiento compuesto anual.
		/// </summary>
		Task Do();
	}
}

