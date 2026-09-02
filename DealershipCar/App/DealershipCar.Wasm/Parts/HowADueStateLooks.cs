namespace DealershipCar.Wasm.Parts
{
	/// <summary>
	/// Con que color y con que dibujo se pinta cada estado del calendario. El nombre del estado lo
	/// dice el dominio; aca solo se elige como se ve, y un estado que la lista no nombre se pinta
	/// neutro en vez de desaparecer.
	/// </summary>
	public static class HowADueStateLooks
	{
		/// <summary>El tono, que es el sufijo de la clase de css que lo pinta.</summary>
		public static string ToneOf(string dueState) => dueState switch
		{
			"Overdue" => "danger",
			"Due" => "warn",
			"DueSoon" => "info",
			"Deferred" => "neutral",
			"Completed" => "ok",
			_ => "neutral",
		};

		/// <summary>El nombre del dibujo que acompana al estado.</summary>
		public static string GlyphOf(string dueState) => dueState switch
		{
			"Overdue" => "alert",
			"Due" => "clock",
			"DueSoon" => "clock",
			"Deferred" => "calendar",
			"Completed" => "check",
			"Superseded" => "history",
			_ => "check",
		};
	}
}
