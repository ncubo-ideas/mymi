using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Concesionario.App.Views
{
	/// <summary>
	/// Donde esta parado ESTE vehiculo contra el sobre que la agencia le publico, leido entero en un
	/// solo instante y por la unica puerta. Ninguna frase se compone aca: cada una la compuso el
	/// dominio, y esta clase solo la acomoda al lado de los hechos del registro contra los que se
	/// midio. Va aparte de la ficha del fabricante porque la ficha es de un MODELO y esto es de un
	/// CARRO.
	/// </summary>
	public sealed class WhereThisVehicleStands
	{
		/// <summary>El instante en que la pantalla pregunto. Todo lo de abajo es a ese instante.</summary>
		public string AskedAt { get; init; } = "";
		/// <summary>
		/// Si dos hechos que la agencia registro no pueden ser ciertos los dos a ese instante. Cuando
		/// es cierto, la mitad del vehiculo sale vacia ENTERA: una frase que mide algo al lado de un
		/// aviso que dice que no se pudo medir nada es peor que cualquiera de las dos sola.
		/// </summary>
		public bool RecordContradictsItself { get; init; }
		/// <summary>Si la agencia publico un sobre para este vehiculo.</summary>
		public bool EnvelopePublished { get; init; }
		/// <summary>
		/// El sobre publicado en palabras, sin ningun hecho de ningun carro. Contesta siempre, tambien
		/// sin sobre: es la mitad que puede citar quien no tiene el odometro de nadie.
		/// </summary>
		public string PublishedSays { get; init; } = "";
		/// <summary>El dia en que se publico el sobre que rige, o vacio si no hay ninguno.</summary>
		public string PublishedAt { get; init; } = "";
		/// <summary>Si la agencia registro la entrega, que es el ancla de las dos mitades.</summary>
		public bool DeliveryRecorded { get; init; }
		/// <summary>El dia en que se entrego el carro, o vacio si no esta registrado.</summary>
		public string DeliveredAt { get; init; } = "";
		/// <summary>
		/// El odometro del dia de la entrega. Cero con la entrega sin registrar NO es una lectura de
		/// cero, y por eso va al lado de su presencia y nunca solo.
		/// </summary>
		public int DeliveryReading { get; init; }
		/// <summary>La unidad de esa lectura, dicha como la lee una persona. Aca no se convierte.</summary>
		public string DeliveryUnit { get; init; } = "";
		/// <summary>Si el feed trajo una lectura observada a ese instante o antes.</summary>
		public bool OdometerKnown { get; init; }
		/// <summary>La lectura contra la que se midio la mitad de distancia.</summary>
		public int Odometer { get; init; }
		/// <summary>La unidad de esa lectura. Nunca se convierte a la del sobre.</summary>
		public string OdometerUnit { get; init; } = "";
		/// <summary>
		/// El dia en que esa lectura se OBSERVO, que no es el dia en que se pregunta. Es la linea de
		/// honestidad de esta seccion: sin ella, una lectura de marzo se lee como la de hoy.
		/// </summary>
		public string OdometerObservedAt { get; init; } = "";
		/// <summary>Si el registro pudo decidir donde esta parado el vehiculo a ese instante.</summary>
		public bool CanBeEvaluated { get; init; }
		/// <summary>
		/// Donde esta parado, por el nombre de la respuesta. Vacio cuando no se pudo decidir: ninguno
		/// de los nombres quiere decir "no se pudo medir".
		/// </summary>
		public string Standing { get; init; } = "";
		/// <summary>
		/// LA RESPUESTA, en la frase que compuso el dominio, con las dos mitades adentro y con el
		/// sobre y el odometro contra los que se midio. Vacia solo cuando el registro se contradice.
		/// </summary>
		public string Says { get; init; } = "";
		/// <summary>
		/// Donde esta parado este vehiculo en cada sistema que el alcance publicado declara adentro.
		/// Vacia cuando no hay alcance publicado, cuando al registro le falta un hecho o cuando se
		/// contradice.
		/// </summary>
		public IReadOnlyList<CoverageStandingForThisVehicle> Systems { get; init; } =
			new List<CoverageStandingForThisVehicle>();
		/// <summary>
		/// Si al registro le falta alguno de los tres hechos con que se mide. Es una rama de pantalla
		/// y no una respuesta: cual falta lo dice la frase del dominio con sus propias palabras.
		/// </summary>
		public bool TheRecordIsMissingAFact =>
			!EnvelopePublished || !DeliveryRecorded || !OdometerKnown;
		/// <summary>
		/// Lee la mitad del vehiculo por la puerta, en un solo instante, para que la postura, las dos
		/// mitades y los hechos contra los que se midieron sean todos del mismo momento.
		/// </summary>
		public static WhereThisVehicleStands ReadThrough(DealershipCarAPI api, DateTime asOf)
		{
			ArgumentNullException.ThrowIfNull(api);
			bool published = api.IsWarrantyEnvelopePublished();
			bool delivered = api.HasDeliveryBeenRecorded();
			bool measured = api.HasAnOdometerReading(asOf);
			bool contradicts = false;
			bool canBeEvaluated = false;
			string standing = "";
			string says = "";
			IReadOnlyList<CoverageStandingForThisVehicle> systems =
				new List<CoverageStandingForThisVehicle>();
			try
			{
				canBeEvaluated = api.CanTheEnvelopeBeEvaluated(asOf);
				standing = canBeEvaluated ? api.ReadEnvelopeStanding(asOf) : "";
				says = api.DescribeWarrantyEnvelope(asOf);
				if (published && delivered && measured)
					systems = TheStandingOnEverySystemTheScopeDeclares(api, asOf);
			}
			catch (Exception thrown) when (TheRecordContradictsItself(thrown))
			{
				contradicts = true;
				canBeEvaluated = false;
				standing = "";
				says = "";
			}
			return new WhereThisVehicleStands
			{
				AskedAt = TheMoment(asOf),
				RecordContradictsItself = contradicts,
				EnvelopePublished = published,
				PublishedSays = api.DescribePublishedWarrantyEnvelope(),
				PublishedAt = published ? TheDay(api.ReadEnvelopePublishedAt()) : "",
				DeliveryRecorded = delivered,
				DeliveredAt = delivered ? TheDay(api.ReadDeliveredAt()) : "",
				DeliveryReading = delivered ? api.ReadDeliveryReading() : 0,
				DeliveryUnit = delivered ? api.ReadDeliveryUnit() : "",
				OdometerKnown = measured,
				Odometer = measured ? api.ReadOdometer(asOf) : 0,
				OdometerUnit = measured ? api.ReadOdometerUnit(asOf) : "",
				OdometerObservedAt = measured ? TheDay(api.ReadOdometerObservedAt(asOf)) : "",
				CanBeEvaluated = canBeEvaluated,
				Standing = standing,
				Says = says,
				Systems = systems,
			};
		}
		private static List<CoverageStandingForThisVehicle> TheStandingOnEverySystemTheScopeDeclares(
			DealershipCarAPI api, DateTime asOf)
		{
			var systems = new List<CoverageStandingForThisVehicle>();
			foreach (string code in api.CoveredSystems())
			{
				bool can = api.CanCoverageBeEvaluatedForThisVehicle(code, asOf);
				systems.Add(new CoverageStandingForThisVehicle
				{
					SystemCode = code,
					CanBeEvaluated = can,
					Standing = can ? api.ReadCoverageStandingForThisVehicle(code, asOf) : "",
					Says = api.DescribeCoverageForThisVehicle(code, asOf),
				});
			}
			return systems;
		}
		/// <summary>
		/// Si lo que reviento es el dominio rechazando un registro que se contradice consigo mismo. Se
		/// reconoce POR EL NOMBRE DEL TIPO y no por el tipo: la excepcion del dominio es interna a su
		/// ensamblado y esta caratula no es amiga de el. Cualquier otra cosa sigue de largo, que es lo
		/// que separa un registro contradictorio de un disco que fallo.
		/// </summary>
		private static bool TheRecordContradictsItself(Exception thrown)
		{
			return thrown.GetType().Name == "DomainException";
		}
		private static string TheDay(DateTime instant)
		{
			return instant.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
		}
		private static string TheMoment(DateTime instant)
		{
			return instant.ToString("yyyy-MM-dd HH:mm:ss'Z'", CultureInfo.InvariantCulture);
		}
	}
	/// <summary>
	/// Donde esta parado este vehiculo en UN sistema que el alcance publicado declara adentro, con la
	/// frase que compuso el catalogo del fabricante contra los hechos de este carro.
	/// </summary>
	public sealed class CoverageStandingForThisVehicle
	{
		/// <summary>El codigo del sistema, en el vocabulario del fabricante.</summary>
		public string SystemCode { get; init; } = "";
		/// <summary>Si la cobertura de ese sistema se pudo decidir para este vehiculo.</summary>
		public bool CanBeEvaluated { get; init; }
		/// <summary>
		/// La postura por su nombre, o vacio cuando no se pudo decidir -- que pasa, por ejemplo,
		/// cuando el limite que rige y el odometro llegaron en unidades distintas.
		/// </summary>
		public string Standing { get; init; } = "";
		/// <summary>
		/// La frase del dominio. Contesta tambien lo que no se pudo evaluar, nombrando las dos
		/// unidades que no calzaron, asi que siempre hay algo que mostrar.
		/// </summary>
		public string Says { get; init; } = "";
	}
}
