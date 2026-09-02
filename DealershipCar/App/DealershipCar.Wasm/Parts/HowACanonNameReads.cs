namespace DealershipCar.Wasm.Parts
{
	/// <summary>
	/// Como se lee en la pantalla cada nombre del canon. El nombre lo dice el dominio y cruza la
	/// puerta tal cual; aca solo se elige la etiqueta, y un nombre que ninguna lista nombre se
	/// separa en cada mayuscula en vez de desaparecer.
	/// </summary>
	public static class HowACanonNameReads
	{
		/// <summary>Una de las nueve clases de operacion de mantenimiento.</summary>
		public static string AnOperation(string kind) => kind switch
		{
			"Inspection" => "Revisión",
			"FluidChange" => "Cambio de fluido",
			"FilterChange" => "Cambio de filtro",
			"WearPartReplacement" => "Cambio de pieza de desgaste",
			"TimedComponent" => "Componente por tiempo",
			"SoftwareUpdate" => "Actualización de software",
			"RecallRemedy" => "Llamado a revisión",
			"Diagnostic" => "Diagnóstico",
			"Repair" => "Reparación",
			_ => AppState.Spaced(kind),
		};

		/// <summary>Donde esta parada una operacion respecto de su vencimiento.</summary>
		public static string ADueState(string dueState) => dueState switch
		{
			"NotDue" => "Sin vencer",
			"DueSoon" => "Por vencer",
			"Due" => "Vencida",
			"Overdue" => "Atrasada",
			"Deferred" => "Aplazada",
			"Completed" => "Hecha",
			"Superseded" => "Sustituida",
			_ => AppState.Spaced(dueState),
		};

		/// <summary>Una de las seis clases de fluido.</summary>
		public static string AFluid(string kind) => kind switch
		{
			"EngineOil" => "Aceite de motor",
			"BrakeFluid" => "Líquido de frenos",
			"Coolant" => "Refrigerante",
			"TransmissionFluid" => "Líquido de transmisión",
			"PowerSteeringFluid" => "Líquido de dirección asistida",
			"WasherFluid" => "Líquido de limpiaparabrisas",
			_ => AppState.Spaced(kind),
		};

		/// <summary>Una de las tres posiciones de llanta.</summary>
		public static string ATyre(string position) => position switch
		{
			"Front" => "Delantera",
			"Rear" => "Trasera",
			"Spare" => "De repuesto",
			_ => AppState.Spaced(position),
		};

		/// <summary>
		/// Donde esta parado el vehiculo frente a su sobre o frente a un sistema del alcance. Las
		/// dos posturas se leen aca porque comparten <c>NotPublished</c>, que se dice igual en las
		/// dos.
		/// </summary>
		public static string AStanding(string standing) => standing switch
		{
			"Inside" => "Adentro",
			"Reached" => "Alcanzado",
			"NotPublished" => "Sin publicar",
			"Covered" => "Cubierto",
			"CoveredWithExclusion" => "Cubierto con exclusión",
			"NotCovered" => "No cubierto",
			"OutsideEnvelope" => "Fuera de los límites",
			_ => AppState.Spaced(standing),
		};
	}
}
