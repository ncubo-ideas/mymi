using System;

namespace Compras.Dominio
{
	/// <summary>
	/// Que SIGNIFICA el cambio de la canasta, que no es lo mismo que el cambio.
	/// </summary>
	// El cambio es un hecho neutro: "la proporcion ocasional bajo 29 puntos". El
	// veredicto dice hacia donde: en lo ocasional, bajar es hacia donde conviene, asi
	// que ese mismo hecho es "mejorando".
	//
	// Existe porque sin el la pantalla tenia que decidirlo, y decidia mal: con un solo
	// booleano (CrecioLoOcasional) una canasta que MEJORA 29 puntos se mostraba igual
	// que una que no se movio -"estable", en verde-, porque lo unico que el booleano
	// sabia contestar era "crecio o no crecio".
	public sealed class Veredicto
	{
		/// <summary>Falta uno de los dos meses: no se afirma nada. No hay lectura que dar.</summary>
		public static readonly Veredicto Indeterminado = new Veredicto("indeterminado");

		/// <summary>Se movio menos de lo que cuenta como cambio real.</summary>
		public static readonly Veredicto Estable = new Veredicto("estable");

		/// <summary>Lo ocasional bajo: la canasta va hacia donde conviene.</summary>
		public static readonly Veredicto Mejorando = new Veredicto("mejorando");

		/// <summary>Lo ocasional subio.</summary>
		public static readonly Veredicto Empeorando = new Veredicto("empeorando");

		public string Nombre { get; private set; }

		private Veredicto(string nombre)
		{
			Nombre = nombre;
		}

		public bool HayLectura => !ReferenceEquals(this, Indeterminado);

		public bool EsMejorando => ReferenceEquals(this, Mejorando);

		public bool EsEmpeorando => ReferenceEquals(this, Empeorando);
	}
}
