using Choreography.Theater;

namespace Municipalidad.App
{
	/// <summary>
	/// El anfitrion del asistente: el mismo actor y el mismo journal que la app, abiertos para leer.
	/// Un actor tiene UN escritor, y este no lo es.
	/// </summary>
	public sealed class AppDeMunicipalidadSoloLectura : PerformanceV2
	{
		public AppDeMunicipalidadSoloLectura()
			: base(MunicipalidadApp.ElActor, ElDominio.Assembly)
		{
		}

		/// <summary>
		/// VACIO A PROPOSITO: la semilla dejaria una entrada en el journal de la persona en cada
		/// pregunta del asistente y habria dos escritores sobre un mismo actor. Es lo unico que
		/// separa a este anfitrion del de la app.
		/// </summary>
		/// <remarks>
		/// Como no corre ningun upgrade, un journal al que le falte una global de raiz sigue
		/// faltandole: una lectura que la nombre se niega mientras la app no lo abra.
		/// </remarks>
		protected override void OnHydrated()
		{
		}
	}
}
