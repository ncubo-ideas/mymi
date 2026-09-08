using Concesionario.App;
using Puppeteer;

namespace Concesionario.Mcp.Tools
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
			"La app todavía no se ha abierto en esta máquina, así que no hay nada registrado en esta "
			+ "carpeta. Pídale a la persona que abra su app Concesionario una vez; no es un error, y "
			+ "no es algo que usted ni el concesionario puedan arreglar desde aquí.";

		/// <summary>
		/// Que este journal lo escribio una app anterior a la cuarta global de raiz, asi que nada de
		/// lo que el concesionario registra sobre ESTE carro esta en el.
		/// </summary>
		internal const string TheAppIsOlderThanTheVehiclesOwnRecord =
			"Este registro lo escribió una versión de la app que todavía no guardaba lo que "
			+ "el concesionario registra sobre ESTE vehículo -- su sobre de garantía, la fecha en que se "
			+ "entregó y la lectura del odómetro tomada ese día --, así que dónde está parado este "
			+ "carro frente a su garantía no está en él en absoluto. Eso no es lo mismo que el carro "
			+ "no tenga garantía, y no es un hueco que usted pueda llenar. Su plan de mantenimiento y "
			+ "todo lo que el fabricante publicó están completos y siguen contestando. Pídale a la "
			+ "persona que abra su app una vez y eso lo traerá.";

		/// <summary>
		/// Que este journal lo escribio una app anterior a las estadias en el taller, asi que no hay
		/// ninguna en el. No es lo mismo que el carro nunca haya estado en un taller.
		/// </summary>
		internal const string TheAppIsOlderThanTheShopStays =
			"Este registro lo escribió una versión de la app que todavía no guardaba las estadías en "
			+ "el taller de este vehículo, así que aquí no hay ninguna en absoluto. Eso NO es lo mismo "
			+ "que el carro nunca haya estado en un taller, y no se puede reportar como cero días: es "
			+ "que este registro no lleva ese dato todavía. Su plan de mantenimiento y todo lo que el "
			+ "fabricante publicó están completos y siguen contestando. Pídale a la persona que abra su "
			+ "app una vez y eso lo traerá; este servidor no lo puede arreglar, porque arreglarlo "
			+ "querría decir escribir en su registro.";

		/// <summary>
		/// Que dos hechos que el concesionario registro se contradicen entre si, y que quien pregunta no
		/// puso ninguno de los dos.
		/// </summary>
		internal const string TheRecordSaysTwoThingsThatCannotBothBeTrue =
			"Dos hechos que el concesionario registró sobre este vehículo se contradicen entre sí al "
			+ "instante preguntado, así que el registro no se pudo medir y esta respuesta no lleva "
			+ "ninguna posición. Ninguno de los dos hechos vino de la persona ni vino de usted: los "
			+ "dos están en el registro propio del concesionario, y solo el concesionario puede corregirlos. "
			+ "Dígale a la persona que su registro tiene un conflicto adentro y que lo plantee con su "
			+ "concesionario. No estime la posición, y no adivine cuál de los dos hechos es el equivocado.";

		/// <summary>
		/// Que hay mas de un vehiculo en la carpeta y por cual se contesta. Vacio cuando hay uno solo.
		/// </summary>
		internal static string MoreThanOneVehicle(JournalReader reader)
		{
			ArgumentNullException.ThrowIfNull(reader);

			return reader.HowManyVehicles > 1
				? $"Hay {reader.HowManyVehicles} vehículos en esta carpeta. Esta respuesta es sobre "
					+ "el primero; diga de qué VIN se trata para que la persona pueda avisarle si se "
					+ "refería a otro."
				: "";
		}

		/// <summary>
		/// Si este journal lleva lo que el concesionario registra sobre este vehiculo. Un journal que
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
		/// Si este journal lleva las estadias en el taller de este vehiculo. Un journal que escribio
		/// una app anterior no tiene la global de raiz, y este proceso NO la puede crear: crearla seria
		/// escribir. Se pregunta con la lectura por instante, que es la que la tool necesita igual.
		/// </summary>
		internal static bool TheVehiclesStaysAreThere(DealershipCarAPI api, DateTime asOf)
		{
			ArgumentNullException.ThrowIfNull(api);

			try
			{
				api.IsInsideTheShopAt(asOf);
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
