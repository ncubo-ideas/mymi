using System;

namespace Compras.Dominio
{
	/// <summary>
	/// Que tiene que HACER la persona con un hallazgo de este negocio. Conjunto cerrado
	/// y ordenado: la accion sale de aca, no de la pantalla.
	/// </summary>
	// Las acciones de un supermercado son administrativas -una garantia que se vence, un
	// abastecimiento que se corto-, nunca consejos de salud: que la canasta se haya
	// vuelto ocasional es un HECHO que este modulo reporta, y lo que se hace con eso no
	// lo decide el supermercado.
	//
	// Tiene peso porque hay que poder decir "lo mas urgente de todo el historial", y
	// comparar por peso es lo unico que permite ordenar cosas que no son numeros.
	public sealed class Severidad
	{
		/// <summary>Un dato. Decir que hay algo que hacer cuando no lo hay entrena a ignorar los avisos.</summary>
		public static readonly Severidad Informativo = new Severidad("informativo", 0, "");

		/// <summary>Hay tiempo, pero conviene no dejarlo pasar.</summary>
		public static readonly Severidad ConvieneRevisar =
			new Severidad("conviene revisar", 1, "tenelo a mano por si acaso");

		/// <summary>La ventana se esta cerrando y despues no se puede reclamar.</summary>
		public static readonly Severidad AntesDeQueSeCierre =
			new Severidad("antes de que se cierre", 2, "hacé el reclamo antes de que venza");

		public string Nombre { get; private set; }

		/// <summary>Que hacer, ya escrito como se le muestra a la persona. Vacio en Informativo, y eso es una respuesta.</summary>
		public string QueHacer { get; private set; }

		private readonly int peso;

		private Severidad(string nombre, int peso, string queHacer)
		{
			Nombre = nombre;
			QueHacer = queHacer;
			this.peso = peso;
		}

		public bool PideAlgo => peso > 0;

		public bool EsMasUrgenteQue(Severidad otra)
		{
			ArgumentNullException.ThrowIfNull(otra);
			return peso > otra.peso;
		}

		/// <summary>La mas urgente de dos, para poder resumir sin que la superficie conozca el orden.</summary>
		public static Severidad LaMayor(Severidad una, Severidad otra)
		{
			ArgumentNullException.ThrowIfNull(una);
			ArgumentNullException.ThrowIfNull(otra);
			return una.EsMasUrgenteQue(otra) ? una : otra;
		}
	}
}
