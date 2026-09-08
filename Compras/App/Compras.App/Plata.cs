using System.Globalization;

namespace Compras.App
{
	/// <summary>
	/// Como se escribe un monto en colones, para la PRESENTACION: la consola y la caratula.
	/// </summary>
	// POR QUE HAY DOS. El dominio tiene su propio `Plata`, `internal`, con esta misma regla,
	// porque el dominio escribe prosa que ya trae montos adentro ("gastaste ₡172.630 en...").
	// Esta copia existe porque la UI tambien recibe decimales CRUDOS de una consulta y tiene
	// que escribirlos igual -- y desde que el dominio es enteramente `internal`, ni la
	// caratula ni la consola pueden nombrar el `Plata` de adentro.
	//
	// LA DUPLICACION ES DELIBERADA Y ESTA AMARRADA: Comprobaciones compara las dos
	// implementaciones monto por monto, asi que si una cambia y la otra no, el fixture lo
	// dice. Es la leccion de la lista espejo de Categoria, resuelta al reves: cuando no se
	// puede tener una sola copia, se tiene una prueba que las obliga a coincidir.
	//
	// EL PUNTO SEPARA LOS MILES, no la coma: es como se escribe un monto en Costa Rica.
	// El formato se arma A MANO en vez de pedir la cultura "es-CR" porque la caratula corre
	// en wasm, y si el publish recorta los datos de globalizacion la cultura no existe y el
	// monto se escribiria distinto que en la consola.
	public static class Plata
	{
		public const string Simbolo = "₡";

		private static readonly NumberFormatInfo Colon = new NumberFormatInfo
		{
			NumberGroupSeparator = ".",
			NumberDecimalSeparator = ",",
			NumberGroupSizes = new[] { 3 },
		};

		/// <summary>Un monto redondeado al colon, con su simbolo: ₡172.630.</summary>
		public static string Colones(decimal monto) => Simbolo + monto.ToString("N0", Colon);
	}
}
