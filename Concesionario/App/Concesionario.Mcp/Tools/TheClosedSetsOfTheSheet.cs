namespace Concesionario.Mcp.Tools
{
	/// <summary>
	/// Los nombres de los conjuntos cerrados que la ficha del fabricante habla, repetidos en esta
	/// frontera para poder recorrerlos. El asistente nunca elige uno: solo se enumeran.
	/// </summary>
	///
	/// Los nombres son los del dominio letra por letra, porque el NOMBRE es lo que cruza la puerta.
	/// TheClosedSetAtTheAssistantsEdgeIsTheDomainsOwn los fija miembro por miembro contra los enums
	/// del dominio.
	internal static class TheClosedSetsOfTheSheet
	{
		/// <summary>Las seis clases de fluido, en el orden en que el canon las escribe.</summary>
		internal static string[] Fluids =>
			new[]
			{
				"EngineOil", "BrakeFluid", "Coolant", "TransmissionFluid", "PowerSteeringFluid",
				"WasherFluid",
			};

		/// <summary>Las tres posiciones de llanta, en el orden en que el canon las escribe.</summary>
		internal static string[] TyrePositions => new[] { "Front", "Rear", "Spare" };

		/// <summary>
		/// Las dos unidades de distancia. No se convierten entre si en ningun punto: se pregunta por
		/// cada una y se contesta con la que tenga respuesta, en la unidad en que se publico.
		/// </summary>
		internal static string[] DistanceUnits => new[] { "UnitA", "UnitB" };
	}
}
