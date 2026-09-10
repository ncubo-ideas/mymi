using System.Collections.Generic;
using System.Text.Json;

namespace Puppets.Portador
{
	// EN QUE ESTADO ESTA LA CUENTA de la persona ante el proveedor. Viaja en el sobre porque
	// es lo unico que la app suelta puede leer sin nada mas: si su registro ya se mudo a
	// Gnothi, se entera en su proximo pull.
	//
	// POR QUE ACA Y NO EN EL DISPOSITIVO (especificacion 13 §4): ninguna marca del lado web
	// sobrevive a "borrar datos del sitio", ni a que la persona quite el icono. Y una marca
	// que se escribe la app a si misma no prueba que Gnothi haya recibido nada. La unica que
	// sobrevive y que ademas CERTIFICA la recepcion es la que escribe el proveedor cuando le
	// emitio credencial a Gnothi.
	public sealed class EstadoDeCuenta
	{
		// "active" o "migrated". Un valor que no se conoce se trata como activo: ante la duda
		// la app sigue funcionando, no se apaga sola.
		public string Estado { get; init; } = "active";

		public string Desde { get; init; } = "";

		// Donde vive ahora el registro. Se dice en pantalla: "tu historia ahora vive en X"
		// sin decir donde es un mensaje que no se puede accionar.
		public string Host { get; init; } = "";

		public bool Migrada => string.Equals(Estado, "migrated", System.StringComparison.OrdinalIgnoreCase);
	}

	// EL SOBRE YA LEIDO: el estado de la cuenta y las senales. El sobre es del MECANISMO; los
	// campos de adentro de cada evento son de la app, y el Portador no los mira.
	public sealed class SobreLeido
	{
		public EstadoDeCuenta? Cuenta { get; init; }

		public IReadOnlyList<SenalDeEvento> Senales { get; init; } = new List<SenalDeEvento>();
	}

	// EL SOBRE en el que viaja la narrativa del proveedor:
	//
	//     { "account": { "state": "...", ... }, "events": [ { "id": N, "tipo": "...", ... } ], "last": N }
	//
	// Vive aparte de la fuente HTTP porque el sobre es el mismo venga por donde venga: lo trae
	// la fuente HTTP cuando la app hace su propio pull, y lo trae Gnothi por el puente cuando
	// la app corre hospedada. Una sola interpretacion para los dos.
	//
	// `account` es OPCIONAL: un proveedor que todavia no lo manda deja la cuenta en null y la
	// app se comporta como siempre. Es un cambio aditivo.
	public static class SobreDeEventos
	{
		// Un evento sin id o sin tipo se descarta aca mismo: no se le puede poner checkpoint
		// ni rutear, y tragarlo con un numero inventado desalinearia la bitacora.
		public static SobreLeido Interpretar(string cuerpo)
		{
			var senales = new List<SenalDeEvento>();
			if (string.IsNullOrWhiteSpace(cuerpo)) return new SobreLeido { Senales = senales };

			using JsonDocument documento = JsonDocument.Parse(cuerpo);
			JsonElement raiz = documento.RootElement;

			EstadoDeCuenta cuenta = LeerCuenta(raiz);

			if (raiz.TryGetProperty("events", out JsonElement eventos) && eventos.ValueKind == JsonValueKind.Array)
			{
				foreach (JsonElement evento in eventos.EnumerateArray())
				{
					if (!evento.TryGetProperty("id", out JsonElement idEvento)
						|| !idEvento.TryGetInt64(out long numero)) continue;
					if (!evento.TryGetProperty("tipo", out JsonElement tipo)
						|| tipo.ValueKind != JsonValueKind.String) continue;

					senales.Add(new SenalDeEvento(numero, tipo.GetString(), evento.GetRawText()));
				}
			}

			// El orden es del proveedor, pero el bucle depende de aplicarlos en orden
			// creciente: la bitacora solo avanza, asi que un bloque desordenado haria que el
			// Check rechace los que vengan atras. Ordenar aca lo hace robusto sin acoplar el
			// bucle al medio.
			senales.Sort((a, b) => a.Numero.CompareTo(b.Numero));
			return new SobreLeido { Cuenta = cuenta, Senales = senales };
		}

		private static EstadoDeCuenta LeerCuenta(JsonElement raiz)
		{
			if (!raiz.TryGetProperty("account", out JsonElement cuenta)
				|| cuenta.ValueKind != JsonValueKind.Object) return null;

			return new EstadoDeCuenta
			{
				Estado = Texto(cuenta, "state", "active"),
				Desde = Texto(cuenta, "migratedAt", ""),
				Host = Texto(cuenta, "host", ""),
			};
		}

		private static string Texto(JsonElement objeto, string campo, string porDefecto) =>
			objeto.TryGetProperty(campo, out JsonElement valor) && valor.ValueKind == JsonValueKind.String
				? valor.GetString() ?? porDefecto
				: porDefecto;
	}
}
