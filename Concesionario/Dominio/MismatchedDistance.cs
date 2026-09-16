using System;

namespace Concesionario.Dominio
{
	/// <summary>
	/// CUAL DE LAS DOS DISTANCIAS DEL REGISTRO no calzo con la unidad del limite publicado, por su
	/// codigo. Son dos y no una: decir que no se pudo medir sin decir cual contra cual no le deja a
	/// nadie corregirlo.
	/// </summary>
	internal static class MismatchedDistance
	{
		/// <summary>La lectura del odometro que se registro el dia de la entrega.</summary>
		internal const string TheDeliveryReading = "TheDeliveryReading";

		/// <summary>El odometro de este vehiculo al instante preguntado.</summary>
		internal const string TheOdometer = "TheOdometer";
	}
}
