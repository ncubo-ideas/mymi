namespace DealershipCar.Mcp.Tools
{
	/// <summary>
	/// El unico reloj de este proceso. Toda pregunta del asistente se contesta respecto de un
	/// instante, y ese instante se lee aca y en ningun otro lado, para que truncar no dependa de que
	/// cada familia de tools se acuerde.
	/// </summary>
	internal static class TheClockTheAssistantReads
	{
		/// <summary>
		/// El instante en que se hizo la pregunta, en UTC y truncado al segundo, que es la resolucion
		/// con la que el journal guarda un instante. Es argumento de una CONSULTA y por eso NO es
		/// monotono: una consulta no escribe nada, asi que no hay nada que proteger, y exigir que
		/// avance rechazaria una segunda pregunta legitima un segundo despues.
		/// </summary>
		internal static DateTime TheInstantThisWasAsked()
		{
			DateTime now = DateTime.UtcNow;

			return new DateTime(
				now.Ticks - now.Ticks % TimeSpan.TicksPerSecond, DateTimeKind.Utc);
		}
	}
}
