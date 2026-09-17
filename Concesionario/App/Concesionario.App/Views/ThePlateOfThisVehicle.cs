using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Concesionario.App.Views
{
	/// <summary>
	/// La placa de ESTE vehiculo al instante en que la pantalla pregunta, leida por la unica puerta.
	/// Ninguna frase se compone aca y a ningun texto se le toca un caracter: los dos salen de la puerta
	/// ya decididos.
	/// <para>
	/// Va aparte de la mitad del vehiculo porque esa mide este carro contra su sobre de garantia y la
	/// placa no es eso: es el nombre PUBLICO del carro, el que usa cualquier registro que no sea el de
	/// este concesionario. Fusionarlas volveria falso el nombre de aquel tipo.
	/// </para>
	/// <para>
	/// La llave de cruce NO esta aca a proposito. Es la mitad que se COMPARA contra otro registro, y
	/// una persona lee una sola columna: la misma placa escrita de dos formas en la misma pantalla se
	/// lee como un defecto. La puerta las contesta las dos, y el asistente si se lleva las dos.
	/// </para>
	/// </summary>
	public sealed class ThePlateOfThisVehicle
	{
		/// <summary>El instante en que la pantalla pregunto. Cual placa rige es a ese momento.</summary>
		public string AskedAt { get; init; } = "";
		/// <summary>
		/// Si el concesionario le publico una placa a este vehiculo a ese instante. Decide la rama, y
		/// es la pregunta que hay que hacer antes de pedir el texto publicado.
		/// </summary>
		public bool PlatePublished { get; init; }
		/// <summary>
		/// LA PLACA TAL COMO LA PUBLICO EL CONCESIONARIO, o vacia si no hay ninguna. Esta en la
		/// superficie siempre y la pantalla no la dibuja: la frase de abajo la lleva adentro, asi que
		/// pintar las dos diria la misma placa dos veces en una columna que se lee de un vistazo.
		/// </summary>
		public string AsPublished { get; init; } = "";
		/// <summary>
		/// LA RESPUESTA, en la frase que compuso el dominio: cual es la placa y desde cuando rige, o la
		/// constancia con nombre de que no hay ninguna. Contesta en los dos estados, y es la que la
		/// pantalla pinta.
		/// </summary>
		public string Says { get; init; } = "";
		/// <summary>
		/// Lee la placa por la puerta con UN solo instante, y pregunta si la hay ANTES de pedir el
		/// texto publicado: sin placa esa lectura revienta, y una excepcion aqui se lleva por delante
		/// la pagina entera en vez de dejar una frase.
		/// </summary>
		public static ThePlateOfThisVehicle ReadThrough(
			DealershipCarAPI api, DateTime asOf, string language)
		{
			ArgumentNullException.ThrowIfNull(api);
			bool published = api.IsAPlatePublished(asOf);
			return new ThePlateOfThisVehicle
			{
				AskedAt = TheMoment(asOf),
				PlatePublished = published,
				AsPublished = published ? api.ReadPlateAsPublished(asOf) : "",
				Says = api.DescribePlate(asOf, language),
			};
		}
		private static string TheMoment(DateTime instant)
		{
			return instant.ToString("yyyy-MM-dd HH:mm:ss'Z'", CultureInfo.InvariantCulture);
		}
	}
}
