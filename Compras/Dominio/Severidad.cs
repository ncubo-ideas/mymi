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
	internal sealed class Severidad
	{
		/// <summary>Un dato. Decir que hay algo que hacer cuando no lo hay entrena a ignorar los avisos.</summary>
		internal static readonly Severidad Informativo = new Severidad("informativo", 0);

		/// <summary>Hay tiempo, pero conviene no dejarlo pasar.</summary>
		internal static readonly Severidad ConvieneRevisar = new Severidad("conviene-revisar", 1);

		/// <summary>La ventana se esta cerrando y despues no se puede reclamar.</summary>
		internal static readonly Severidad AntesDeQueSeCierre = new Severidad("antes-de-que-se-cierre", 2);

		/// <summary>
		/// EL CODIGO de esta severidad: identidad, y lo unico que cruza el wire. Que hacer con un
		/// hallazgo de esta severidad es texto de presentacion y lo busca con este codigo la capa que
		/// redacta, en el idioma que le pidieron.
		/// </summary>
		internal string Nombre { get; private set; }

		private readonly int peso;

		private Severidad(string nombre, int peso)
		{
			Nombre = nombre;
			this.peso = peso;
		}

		internal bool PideAlgo => peso > 0;

		internal bool EsMasUrgenteQue(Severidad otra)
		{
			ArgumentNullException.ThrowIfNull(otra);
			return peso > otra.peso;
		}

		/// <summary>La mas urgente de dos, para poder resumir sin que la superficie conozca el orden.</summary>
		internal static Severidad LaMayor(Severidad una, Severidad otra)
		{
			ArgumentNullException.ThrowIfNull(una);
			ArgumentNullException.ThrowIfNull(otra);
			return una.EsMasUrgenteQue(otra) ? una : otra;
		}
	}
}
