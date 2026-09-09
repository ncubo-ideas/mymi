using System;

namespace Compras.Dominio
{
	/// <summary>
	/// MAÑANA, TARDE O NOCHE. En qué momento del día cae una compra.
	/// </summary>
	// EXISTE PORQUE LOS CORTES SON UN JUICIO Y NO UN HECHO. "A las 6 de la tarde" es un
	// dato; que eso sea "tarde" y no "noche" lo decidimos nosotros, y por eso la decision
	// vive en un conjunto cerrado del dominio y no en un if de una pantalla — donde cada
	// pantalla la resolveria distinto y la app se contradiria consigo misma.
	//
	// El diseño de la app lo pide asi: "es cuando mas suelen aparecer tus compras", con
	// mañana, tarde y noche contadas aparte.
	//
	// LOS CORTES, y quedan a la vista para que alguien los discuta: la mañana termina a la
	// una, porque el almuerzo parte el dia en dos para cualquiera; y la noche arranca a las
	// siete, que es cuando una ida al super deja de ser un mandado del dia y pasa a ser
	// despues del trabajo. Son de Costa Rica y de una familia que cocina en casa; en otro
	// pais o para otra vida serian otros.
	internal sealed class MomentoDelDia
	{
		/// <summary>La hora en que la mañana se termina.</summary>
		internal const int HoraEnQueEmpiezaLaTarde = 13;

		/// <summary>La hora en que la tarde se termina.</summary>
		internal const int HoraEnQueEmpiezaLaNoche = 19;

		internal static readonly MomentoDelDia Manana = new MomentoDelDia("manana", "mañana", 1);
		internal static readonly MomentoDelDia Tarde = new MomentoDelDia("tarde", "tarde", 2);
		internal static readonly MomentoDelDia Noche = new MomentoDelDia("noche", "noche", 3);

		internal static readonly MomentoDelDia[] Todos = { Manana, Tarde, Noche };

		/// <summary>La etiqueta interna, sin acentos: para comparar y serializar.</summary>
		internal string Nombre { get; private set; }

		/// <summary>Como se le dice a la persona: "en la mañana".</summary>
		internal string ParaMostrar { get; private set; }

		private readonly int orden;

		private MomentoDelDia(string nombre, string paraMostrar, int orden)
		{
			Nombre = nombre;
			ParaMostrar = paraMostrar;
			this.orden = orden;
		}

		/// <summary>
		/// El momento de una hora del día. TOTAL: cualquier hora válida cae en uno.
		/// </summary>
		internal static MomentoDelDia De(DateTime cuando)
		{
			if (cuando.Hour < HoraEnQueEmpiezaLaTarde) return Manana;
			if (cuando.Hour < HoraEnQueEmpiezaLaNoche) return Tarde;

			return Noche;
		}

		/// <summary>Si este momento va antes que el otro en el día.</summary>
		internal bool EsAntesQue(MomentoDelDia otro)
		{
			ArgumentNullException.ThrowIfNull(otro);
			return orden < otro.orden;
		}

		public override string ToString() => Nombre;
	}
}
