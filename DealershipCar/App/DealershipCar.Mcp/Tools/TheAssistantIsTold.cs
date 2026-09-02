using DealershipCar.App;
using Puppeteer;

namespace DealershipCar.Mcp.Tools
{
	/// <summary>
	/// Lo que se le dice al asistente cuando la respuesta no puede ser la que pidio, y las dos
	/// preguntas que las familias de tools se hacen antes de contestar. Vive en un solo sitio porque
	/// todas contestan lo mismo ante lo mismo.
	/// </summary>
	internal static class TheAssistantIsTold
	{
		/// <summary>Que la app nunca se abrio en esta maquina, que es un estado y no un error.</summary>
		internal const string TheAppHasNotBeenOpened =
			"The app has not been opened on this machine yet, so there is nothing recorded in this "
			+ "folder. Ask the person to open their DealershipCar app once; it is not an error, and "
			+ "it is not something you or the dealership can fix from here.";

		/// <summary>
		/// Que este journal lo escribio una app anterior a la cuarta global de raiz, asi que nada de
		/// lo que la agencia registra sobre ESTE carro esta en el.
		/// </summary>
		internal const string TheAppIsOlderThanTheVehiclesOwnRecord =
			"This record was written by a version of the app that did not yet keep what the "
			+ "dealership records about THIS vehicle - its warranty envelope, the date it was handed "
			+ "over and the odometer reading taken that day - so where this car stands against its "
			+ "warranty is not in it at all. That is not the same as the car having no warranty, and "
			+ "it is not a gap you can fill. Their maintenance schedule and everything the "
			+ "manufacturer published are complete and still answer. Ask the person to open their "
			+ "app once and it will bring this in.";

		/// <summary>
		/// Que dos hechos que la agencia registro se contradicen entre si, y que quien pregunta no
		/// puso ninguno de los dos.
		/// </summary>
		internal const string TheRecordSaysTwoThingsThatCannotBothBeTrue =
			"Two facts the dealership recorded about this vehicle contradict each other at the "
			+ "instant asked, so the record cannot be measured and this answer carries no standing. "
			+ "Neither fact came from the person and neither came from you: they are both in the "
			+ "dealership's own record, and only the dealership can correct them. Tell the person "
			+ "their record has a conflict in it and to raise it with their dealership. Do not "
			+ "estimate the standing, and do not guess which of the two facts is the wrong one.";

		/// <summary>
		/// Que hay mas de un vehiculo en la carpeta y por cual se contesta. Vacio cuando hay uno solo.
		/// </summary>
		internal static string MoreThanOneVehicle(JournalReader reader)
		{
			ArgumentNullException.ThrowIfNull(reader);

			return reader.HowManyVehicles > 1
				? $"There are {reader.HowManyVehicles} vehicles in this folder. This answer is about "
					+ "the first one; say which VIN it is so the person can tell you if they meant "
					+ "another."
				: "";
		}

		/// <summary>
		/// Si este journal lleva lo que la agencia registra sobre este vehiculo. Un journal que
		/// escribio una app anterior no tiene la global de raiz, y este proceso NO la puede crear:
		/// crearla seria escribir.
		/// </summary>
		internal static bool TheVehiclesOwnRecordIsThere(DealershipCarAPI api)
		{
			ArgumentNullException.ThrowIfNull(api);

			try
			{
				api.IsWarrantyEnvelopePublished();
				return true;
			}
			catch (LanguageException)
			{
				return false;
			}
		}

		/// <summary>
		/// Si lo que reviento es el dominio rechazando un registro que se contradice consigo mismo.
		/// Se reconoce POR EL NOMBRE DEL TIPO y no por el tipo: la excepcion del dominio es interna a
		/// su ensamblado y ninguna caratula la puede nombrar. Cualquier otra cosa sigue de largo, que
		/// es lo que separa un registro contradictorio de un disco que fallo.
		/// </summary>
		internal static bool TheRecordContradictsItself(Exception thrown)
		{
			ArgumentNullException.ThrowIfNull(thrown);

			return thrown.GetType().Name == "DomainException";
		}

		/// <summary>Une las frases que haya, sin dejar espacios de mas cuando falta alguna.</summary>
		internal static string EverythingWorthSaying(params string[] sentences)
		{
			var says = new List<string>();

			foreach (string each in sentences)
				if (!string.IsNullOrEmpty(each)) says.Add(each);

			return string.Join(" ", says);
		}
	}
}
