using System;
using System.Globalization;

namespace Municipalidad.Mcp.Tools
{
	/// <summary>
	/// El unico reloj de este proceso. Toda pregunta del asistente se contesta respecto de un instante,
	/// y ese instante se lee aca y en ningun otro lado.
	/// </summary>
	internal static class ElRelojQueLeeElAsistente
	{
		/// <summary>
		/// El instante en que se hizo la pregunta, en UTC y truncado al segundo, que es la resolucion con
		/// la que el journal guarda un instante. Es argumento de una CONSULTA y por eso NO es monotono:
		/// una consulta no escribe nada, asi que exigir que avance rechazaria una segunda pregunta
		/// legitima un segundo despues.
		/// </summary>
		internal static DateTime ElInstanteEnQueSePregunto()
		{
			DateTime ahora = DateTime.UtcNow;

			return new DateTime(
				ahora.Ticks - ahora.Ticks % TimeSpan.TicksPerSecond, DateTimeKind.Utc);
		}

		/// <summary>
		/// El dia de un instante, en el formato con que el dominio escribe un dia. La cultura es
		/// invariante y no la de la maquina: anio-mes-dia es una llave de calendario, y un calendario
		/// que no fuera el gregoriano la escribiria con otro anio.
		/// </summary>
		internal static string ElDiaDe(DateTime instante)
		{
			return instante.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
		}
	}
}
