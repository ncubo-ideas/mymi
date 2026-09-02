using System;
using System.Text.Json;

namespace DealershipCar.App.Events
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
