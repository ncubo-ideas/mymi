using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;

namespace Concesionario.App.Events
{
	internal static class Payload
	{
		internal static string Text(JsonElement root, string field)
		{
			return root.TryGetProperty(field, out JsonElement found)
				&& found.ValueKind == JsonValueKind.String
					? found.GetString()
					: "";
		}

		internal static bool Whole(JsonElement root, string field, out int value)
		{
			value = 0;

			return root.TryGetProperty(field, out JsonElement found)
				&& found.ValueKind == JsonValueKind.Number
				&& found.TryGetInt32(out value);
		}

		/// <summary>
		/// Una cantidad con fraccion, tal como la publico quien la manda. Un campo ausente o que no
		/// es un numero contesta que no: un cero fabricado aca seria una capacidad, una presion o un
		/// monto que nadie dijo.
		/// </summary>
		internal static bool Amount(JsonElement root, string field, out decimal value)
		{
			value = 0m;

			return root.TryGetProperty(field, out JsonElement found)
				&& found.ValueKind == JsonValueKind.Number
				&& found.TryGetDecimal(out value);
		}

		internal static bool Instant(JsonElement root, string field, out DateTime value)
		{
			value = default(DateTime);

			return root.TryGetProperty(field, out JsonElement found)
				&& found.ValueKind == JsonValueKind.String
				&& found.TryGetDateTime(out value);
		}

		/// <summary>
		/// Los dias de calendario que una estadia cubrio, tal como la sucursal los dijo y en el mismo
		/// orden. Contesta que NO cuando el campo falta, cuando no es una lista, cuando la lista esta
		/// vacia, cuando un elemento no es texto y cuando un elemento no dice un dia en la forma que
		/// este modulo lee: los dias SON el hecho, asi que una lista que no nombra ninguno no esta
		/// diciendo cero dias, no esta diciendo. Cuantos son, en que orden vienen y si son contiguos
		/// es ley del dominio y no se copia aca.
		/// </summary>
		internal static bool Days(JsonElement root, string field, out string[] value)
		{
			value = null;

			if (!root.TryGetProperty(field, out JsonElement found)) return false;
			if (found.ValueKind != JsonValueKind.Array) return false;
			if (found.GetArrayLength() == 0) return false;

			List<string> said = new List<string>();

			foreach (JsonElement each in found.EnumerateArray())
			{
				if (each.ValueKind != JsonValueKind.String) return false;

				string day = each.GetString();

				if (!DateOnly.TryParseExact(
						day, DealershipCarAPI.TheShapeADayIsSaidIn, CultureInfo.InvariantCulture,
						DateTimeStyles.None, out _))
					return false;

				said.Add(day);
			}

			value = said.ToArray();

			return true;
		}

		internal static bool Truth(JsonElement root, string field, out bool value)
		{
			value = false;

			if (!root.TryGetProperty(field, out JsonElement found)) return true;

			if (found.ValueKind == JsonValueKind.True) { value = true; return true; }
			if (found.ValueKind == JsonValueKind.False) return true;

			return false;
		}
	}
}
