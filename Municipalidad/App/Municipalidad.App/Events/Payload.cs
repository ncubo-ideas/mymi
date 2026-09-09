using System;
using System.Text.Json;

namespace Municipalidad.App.Events
{
	/// <summary>
	/// Los lectores del payload de una senal. Cada uno contesta si el campo ESTA y si dice lo que dice
	/// decir; ninguno fabrica un valor para un campo ausente.
	/// </summary>
	internal static class Payload
	{
		/// <summary>
		/// El texto de un campo, o vacio cuando el campo no esta o no es texto. Quien lo llama decide si
		/// un vacio se puede decir: un descuento vacio no existe, pero un nombre vacio no nombra nada.
		/// </summary>
		internal static string Texto(JsonElement raiz, string campo)
		{
			return raiz.TryGetProperty(campo, out JsonElement hallado)
				&& hallado.ValueKind == JsonValueKind.String
					? hallado.GetString()
					: "";
		}

		/// <summary>
		/// Una cantidad con fraccion, tal como la publico quien la manda. Un campo ausente o que no es un
		/// numero contesta que no: un cero fabricado aca seria un monto, una mora o un descuento que
		/// nadie dijo.
		/// </summary>
		internal static bool Monto(JsonElement raiz, string campo, out decimal valor)
		{
			valor = 0m;

			return raiz.TryGetProperty(campo, out JsonElement hallado)
				&& hallado.ValueKind == JsonValueKind.Number
				&& hallado.TryGetDecimal(out valor);
		}

		/// <summary>
		/// Un instante publicado. Un campo ausente o que no dice un instante contesta que no: sin el, una
		/// fecha se leeria como el ano cero.
		/// </summary>
		internal static bool Instante(JsonElement raiz, string campo, out DateTime valor)
		{
			valor = default(DateTime);

			return raiz.TryGetProperty(campo, out JsonElement hallado)
				&& hallado.ValueKind == JsonValueKind.String
				&& hallado.TryGetDateTime(out valor);
		}
	}
}
