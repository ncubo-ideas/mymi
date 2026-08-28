namespace DealershipCar.Mcp.Tools
{
	/// <summary>
	/// Lo que se le dice al asistente cuando la respuesta no puede ser la que pidio. Vive en un solo
	/// sitio porque las dos familias de tools contestan lo mismo ante lo mismo.
	/// </summary>
	internal static class TheAssistantIsTold
	{
		/// <summary>Que la app nunca se abrio en esta maquina, que es un estado y no un error.</summary>
		internal const string TheAppHasNotBeenOpened =
			"The app has not been opened on this machine yet, so there is nothing recorded in this "
			+ "folder. Ask the person to open their DealershipCar app once; it is not an error, and "
			+ "it is not something you or the dealership can fix from here.";

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
