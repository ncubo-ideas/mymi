using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Puppets.Portador
{
	// La UNICA lectura que la app le hace al proveedor: su propia narrativa a
	// partir de un punto.
	//
	//     GET {base}/{recurso}/{id}/events?after=N     Authorization: Bearer <token>
	//
	// Esta clase tiene un solo metodo y arma una sola forma de URL. Eso es
	// deliberado y es el invariante del diseno: la app NO es un cliente de los
	// servicios generales del proveedor. No hay endpoint de vistas, ni de reportes,
	// ni de consultas. El proveedor sirve eventos; la interpretacion es del puppet.
	// El token esta acotado al scope {recurso}.events.read sobre ESE recurso, asi
	// que aunque alguien agregara aca otra llamada, el proveedor la rechazaria.
	//
	// El sobre (events / last) es del mecanismo y se parsea aca. Los campos de
	// adentro son de la app: cada evento viaja como su JSON crudo en Contenido y
	// el Portador no los mira.
	public sealed class FuenteHttpDeEventos : IFuenteDeEventos
	{
		private readonly HttpClient http;
		private readonly string recurso;
		private readonly string id;

		public string Nombre => $"{recurso}/{id}/events";

		public FuenteHttpDeEventos(HttpClient http, string recurso, string id)
		{
			ArgumentNullException.ThrowIfNull(http);
			ArgumentNullException.ThrowIfNullOrWhiteSpace(recurso);
			ArgumentNullException.ThrowIfNullOrWhiteSpace(id);

			this.http = http;
			this.recurso = recurso;
			this.id = id;
		}

		public async Task<IReadOnlyList<SenalDeEvento>> LeerDesde(long despuesDe, string token, CancellationToken ct)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(token);

			using var pedido = new HttpRequestMessage(HttpMethod.Get,
				$"{recurso}/{Uri.EscapeDataString(id)}/events?after={despuesDe}");
			pedido.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

			using HttpResponseMessage respuesta = await http.SendAsync(pedido, ct).ConfigureAwait(false);

			// Se distingue del resto de los errores con su propio tipo, porque el bucle
			// puede HACER algo con este: rotar la credencial y reintentar. Un 500 o un
			// timeout no tienen esa salida.
			if (respuesta.StatusCode == HttpStatusCode.Unauthorized || respuesta.StatusCode == HttpStatusCode.Forbidden)
				throw new CredencialRechazadaException(
					$"El proveedor rechazo la credencial ({(int)respuesta.StatusCode}) al leer {Nombre}.");

			respuesta.EnsureSuccessStatusCode();

			string cuerpo = await respuesta.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
			return Interpretar(cuerpo);
		}

		// Solo el sobre. Un evento sin id o sin tipo se descarta aca mismo: no se le
		// puede poner checkpoint ni rutear, y tragarlo con un numero inventado
		// desalinearia la bitacora.
		private static List<SenalDeEvento> Interpretar(string cuerpo)
		{
			var senales = new List<SenalDeEvento>();
			if (string.IsNullOrWhiteSpace(cuerpo)) return senales;

			using JsonDocument documento = JsonDocument.Parse(cuerpo);

			if (!documento.RootElement.TryGetProperty("events", out JsonElement eventos)
				|| eventos.ValueKind != JsonValueKind.Array)
				return senales;

			foreach (JsonElement evento in eventos.EnumerateArray())
			{
				if (!evento.TryGetProperty("id", out JsonElement idEvento)
					|| !idEvento.TryGetInt64(out long numero)) continue;
				if (!evento.TryGetProperty("tipo", out JsonElement tipo)
					|| tipo.ValueKind != JsonValueKind.String) continue;

				senales.Add(new SenalDeEvento(numero, tipo.GetString(), evento.GetRawText()));
			}

			// El orden es del proveedor, pero el bucle depende de aplicarlos en orden
			// creciente: la bitacora solo avanza, asi que un bloque desordenado haria
			// que el Check rechace los que vengan atras. Ordenar aca lo hace robusto
			// sin acoplar el bucle al medio.
			senales.Sort((a, b) => a.Numero.CompareTo(b.Numero));
			return senales;
		}
	}
}
