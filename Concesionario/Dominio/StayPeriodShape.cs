using System;

namespace Concesionario.Dominio
{
	/// <summary>
	/// QUE TIENE QUE DECIR UN PERIODO DE ESTADIAS, por su codigo: hay dias que contar, o no hay
	/// ninguna estadia con dias registrados.
	/// <para>
	/// La ausencia es su propio codigo y no un conteo en cero: un cero no diria entre cuando y cuando
	/// se pregunto, y tampoco distingue no haber estado adentro de una estadia todavia abierta, que
	/// aun no tiene dias.
	/// </para>
	/// </summary>
	internal static class StayPeriodShape
	{
		/// <summary>El periodo tiene al menos una estadia con dias registrados.</summary>
		internal const string WithStaysOnRecord = "WithStaysOnRecord";

		/// <summary>El periodo no tiene ninguna.</summary>
		internal const string NoStayOnRecord = "NoStayOnRecord";
	}
}
