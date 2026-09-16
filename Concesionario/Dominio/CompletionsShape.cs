using System;

namespace Concesionario.Dominio
{
	/// <summary>
	/// QUE TIENE QUE DECIR EL HISTORIAL DE UNA CLASE, por su codigo: hay completaciones que contar, o
	/// cual de las dos ausencias le toca.
	/// <para>
	/// Las dos ausencias son sus propios codigos porque no son la misma: una la cura una visita al
	/// taller y la otra no la cura ninguna hasta que alguien siembre la operacion. Un solo codigo para
	/// las dos afirmaria que una operacion que no esta en el plan del vehiculo nunca se hizo.
	/// </para>
	/// </summary>
	internal static class CompletionsShape
	{
		/// <summary>La clase esta agendada y su cadena dejo al menos una completacion.</summary>
		internal const string OnRecord = "OnRecord";

		/// <summary>La clase esta agendada y su cadena no dejo ninguna.</summary>
		internal const string NoneOnRecord = "NoneOnRecord";

		/// <summary>La clase no esta agendada, asi que no hay cadena que caminar.</summary>
		internal const string NotOnTheSchedule = "NotOnTheSchedule";
	}
}
