using System.Net;
using System.Text;
using System.Text.Json;

namespace Gnothi.Mobile;

// EL CARTERO. Lo único del sistema que sale a la red, y la razón por la que Gnothi tiene
// permiso de internet mientras el módulo no puede usarlo (especificación 11 §6, 13 §8).
//
//     módulo ── gnothi.pull(after=N) ──► CARTERO ── HTTPS ──► publicador
//     módulo ◄──────── el sobre ────────  CARTERO ◄────────────┘
//
// TRES INVARIANTES, y cada uno está acá por algo:
//
//   EL CURSOR ES DEL ACTOR. El `after` llega del módulo, salido del replay de su propio
//   journal. El cartero lo valida como entero y lo releva; NO lo guarda. No hay dos verdades
//   sobre hasta dónde leyó, y por lo tanto no hay una que pueda quedar vieja.
//
//   EL ENDPOINT ES FIJO. Se registra al instalar el módulo y el módulo no lo elige ni lo
//   puede cambiar. Pedir "traeme de tal otro lado" no es una operación que exista.
//
//   LA CREDENCIAL ES DE GNOTHI. Se consigue contra su propio device key, se guarda cifrada
//   con una llave del Keystore, y el módulo nunca la ve.
//
// EL SOBRE VIAJA TAL CUAL. El cartero no lo abre, no lo reordena y no le agrega nada: es el
// brazo del módulo, no un segundo autor.
internal sealed class CarteroDeGnothi
{
	private readonly HttpClient http;
	private readonly string recurso;
	private readonly string id;
	private readonly CredencialDeGnothi credencial;
	private readonly string modulo;

	// El endpoint viene como `https://host/base|recurso|id` en el manifiesto de instalación.
	// Tres partes porque son tres cosas distintas: dónde está el publicador, qué recurso de la
	// persona, y cuál.
	internal CarteroDeGnothi(string modulo, string endpoint, CredencialDeGnothi credencial)
	{
		this.modulo = modulo;
		this.credencial = credencial;

		string[] partes = endpoint.Split('|');
		string baseUrl = partes.Length > 0 && partes[0].Length > 0 ? partes[0] : "";
		recurso = partes.Length > 1 && partes[1].Length > 0 ? partes[1] : "expediente";
		id = partes.Length > 2 && partes[2].Length > 0 ? partes[2] : "1";

		if (!baseUrl.EndsWith('/')) baseUrl += "/";
		http = new HttpClient { BaseAddress = new Uri(baseUrl), Timeout = TimeSpan.FromSeconds(20) };
	}

	// LO QUE EL MÓDULO PIDE. Devuelve el sobre del publicador, tal cual vino.
	//
	// Si el publicador rechaza la credencial se pide una nueva UNA vez y se reintenta. Si el
	// segundo intento también falla, el problema no es la expiración y reintentar sería
	// castigar al publicador con pedidos que ya sabemos que fallan.
	internal async Task<string> Pull(long desde, CancellationToken ct = default)
	{
		if (desde < 0) throw new ArgumentException($"cursor inválido: {desde}");

		try
		{
			string token = await Credencial(ct);

			HttpResponseMessage respuesta = await Leer(desde, token, ct);
			if (respuesta.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
			{
				respuesta.Dispose();
				Android.Util.Log.Info("gnothi", $"{modulo}: el publicador rechazó la credencial; pidiendo otra");
				respuesta = await Leer(desde, await Renovar(ct), ct);
			}

			using (respuesta)
			{
				respuesta.EnsureSuccessStatusCode();
				string sobre = await respuesta.Content.ReadAsStringAsync(ct);
				Android.Util.Log.Info("gnothi", $"{modulo}: pull after={desde} -> {sobre.Length} bytes");
				return sobre;
			}
		}
		catch (Exception ex)
		{
			// UN CARTERO QUE FALLA NO PUEDE FALLAR EN SILENCIO. El módulo se abre igual
			// -mostrando lo que ya tiene, que es de lo que sirve tener el dato propio- y sin
			// esto nadie se entera de por qué dejó de llegar lo nuevo. Es la misma disciplina
			// que el flush del journal: una degradación muda es peor que un error.
			Android.Util.Log.Warn("gnothi",
				$"{modulo}: el cartero no pudo traer after={desde}: {ex.GetType().Name}: {ex.Message}");
			throw;
		}
	}

	private Task<HttpResponseMessage> Leer(long desde, string token, CancellationToken ct)
	{
		var pedido = new HttpRequestMessage(HttpMethod.Get,
			$"{recurso}/{Uri.EscapeDataString(id)}/events?after={desde}");
		pedido.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

		return http.SendAsync(pedido, ct);
	}

	private async Task<string> Credencial(CancellationToken ct)
	{
		string guardado = credencial.Token;
		return guardado.Length > 0 ? guardado : await Renovar(ct);
	}

	// GNOTHI SE PRESENTA COMO OTRO CLIENTE, con su propio device key. El publicador emite una
	// concesión acotada a leer los eventos de ESE recurso: nada de perfil, nada de otros
	// pacientes. Y en ese mismo instante puede marcar la cuenta como migrada, que es lo que la
	// instalación suelta lee en su próximo pull (13 §4).
	//
	// [SUPUESTO] Este es el mecanismo de "enrolamiento del publicador" del 13 §3.2, que es el
	// que el mock implementa. El otro —OAuth con PKCE por el browser del sistema, con Auth Tab
	// en Android— exige que el publicador tenga authorization server, y ninguno lo tiene
	// todavía. Cuando lo tenga, ESTA es la única función que cambia.
	private async Task<string> Renovar(CancellationToken ct)
	{
		string device = credencial.AsegurarDeviceKey();

		string cuerpo = JsonSerializer.Serialize(new { deviceKey = device, recurso, id });
		using var contenido = new StringContent(cuerpo, Encoding.UTF8, "application/json");

		using HttpResponseMessage respuesta = await http.PostAsync("device/token", contenido, ct);
		respuesta.EnsureSuccessStatusCode();

		using JsonDocument documento = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync(ct));
		string token = Texto(documento.RootElement, "accessToken");
		credencial.Guardar(token, Texto(documento.RootElement, "refreshToken"));

		Android.Util.Log.Info("gnothi", $"{modulo}: Gnothi consiguió su credencial ante el publicador");
		return token;
	}

	private static string Texto(JsonElement objeto, string campo) =>
		objeto.TryGetProperty(campo, out JsonElement valor) && valor.ValueKind == JsonValueKind.String
			? valor.GetString() ?? ""
			: "";
}
