using System;
using System.Text.Json;

namespace DealershipCar.App.Events
{
	// COMO SE LEE EL PAYLOAD DE UN EVENTO, en un solo lugar. El sobre -- id y tipo -- es del
	// mecanismo y lo parsea FuenteHttpDeEventos; de la llave para adentro el layout es de este
	// negocio, y estos cinco lectores son todo lo que hace falta para entenderlo.
	//
	// NINGUNO REVIENTA. Un campo que falta, que viene de otro tipo o que viene vacio contesta la
	// ausencia y el Interpretar de cada mensaje decide si eso lo descarta. Esa es la mitad que
	// hace que un evento mal formado NO llegue nunca a un comando: llegar cuesta una entrada
	// permanente en el journal de la persona, porque el motor escribe antes de ejecutar (C22).
	internal static class Payload
	{
		internal static string Text(JsonElement root, string field)
		{
			return root.TryGetProperty(field, out JsonElement found)
				&& found.ValueKind == JsonValueKind.String
					? found.GetString()
					: "";
		}

		// Se pide EXPLICITAMENTE si el numero estaba, en vez de devolver cero: cero es una lectura
		// legal del odometro y un limite legal de un intervalo, asi que confundirlo con "no vino"
		// dejaria pasar un evento incompleto.
		internal static bool Whole(JsonElement root, string field, out int value)
		{
			value = 0;

			return root.TryGetProperty(field, out JsonElement found)
				&& found.ValueKind == JsonValueKind.Number
				&& found.TryGetInt32(out value);
		}

		internal static bool Instant(JsonElement root, string field, out DateTime value)
		{
			value = default(DateTime);

			return root.TryGetProperty(field, out JsonElement found)
				&& found.ValueKind == JsonValueKind.String
				&& found.TryGetDateTime(out value);
		}

		// Una bandera AUSENTE es falsa, y eso no es adivinar: en el plan del fabricante
		// "obligatoria" y "de seguridad" son afirmaciones, asi que no afirmarlas es negarlas. Un
		// campo presente con cualquier otra forma -- una cadena, un numero -- si es malformado.
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
