using System;

namespace Concesionario.Dominio
{
	/// <summary>
	/// COMO SE DICE EL TIEMPO TRANSCURRIDO entre los dos extremos de un tramo, por su codigo. El
	/// dia es la unidad por la que el ritmo se dice, asi que "mas corto que la unidad" es el unico
	/// corte que hay aqui: decir "en 0 dias" seria falso.
	/// <para>
	/// El segundo entero y el dia entero son sus propios codigos porque en los dos idiomas se dicen
	/// en singular, y quien redacta no puede elegir entre singular y plural mirando unos digitos.
	/// </para>
	/// </summary>
	internal static class SpanShape
	{
		/// <summary>El tramo es exactamente un segundo.</summary>
		internal const string AWholeSecond = "AWholeSecond";

		/// <summary>El tramo es mas corto que un dia y se dice en segundos enteros.</summary>
		internal const string InSeconds = "InSeconds";

		/// <summary>El tramo es exactamente un dia.</summary>
		internal const string AWholeDay = "AWholeDay";

		/// <summary>El tramo es de mas de un dia y se dice en dias, con sus decimales.</summary>
		internal const string InDays = "InDays";
	}
}
