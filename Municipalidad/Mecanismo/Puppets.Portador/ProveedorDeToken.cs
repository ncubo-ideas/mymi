using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Puppets.Portador
{
	// El token que el proveedor le da al DEVICE del usuario, acotado a leer los
	// eventos de un recurso. Access token corto, refresh token durable y rotatorio.
	//
	// Esta pieza existe para que la consola y las pruebas puedan correr solas. En
	// la app instalada el usuario consigue el token por fuera y lo pega. Que el
	// usuario lo pegue no es una limitacion del MVP - es que el token es del
	// usuario, no de la app.
	//
	// DONDE QUEDA lo decide el HOST de la app, y NO el journal: en el browser es
	// localStorage, hospedada en Gnothi es el Keystore del aparato, y en la consola
	// es una variable en memoria que muere con el proceso. Hasta el 2026-09-03 se
	// journalizaba, que era darle a un contexto de sesion la durabilidad y la
	// portabilidad de la historia de la persona: el journal se copia a otra maquina
	// y se le entrega a Gnothi, y el token viajaba adentro.
	public sealed class ProveedorDeToken
	{
		private readonly HttpClient http;

		public ProveedorDeToken(HttpClient http)
		{
			ArgumentNullException.ThrowIfNull(http);
			this.http = http;
		}

		public async Task<Credencial> Pedir(string deviceKey, string recurso, string id, CancellationToken ct = default)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(deviceKey);
			ArgumentNullException.ThrowIfNullOrWhiteSpace(recurso);
			ArgumentNullException.ThrowIfNullOrWhiteSpace(id);

			using HttpResponseMessage respuesta = await http
				.PostAsJsonAsync("device/token", new { deviceKey, recurso, id }, ct)
				.ConfigureAwait(false);
			respuesta.EnsureSuccessStatusCode();

			Credencial credencial = await respuesta.Content
				.ReadFromJsonAsync<Credencial>(ct)
				.ConfigureAwait(false);

			if (credencial == null || string.IsNullOrWhiteSpace(credencial.AccessToken))
				throw new InvalidOperationException("El proveedor no devolvio un access token.");

			return credencial;
		}

		// Devuelve null si el proveedor NO acepta el refresh token. Que un refresh haya
		// caducado, sido rotado por otra instancia o revocado es un resultado esperado
		// del flujo, no una excepcion: el llamador simplemente pide una credencial nueva
		// con su device key. Antes esto tiraba HttpRequestException y obligaba a cada
		// llamador a envolverlo en un try/catch para descubrir algo que la firma podia
		// decir sola.
		public async Task<Credencial> Rotar(string refreshToken, CancellationToken ct = default)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(refreshToken);

			using HttpResponseMessage respuesta = await http
				.PostAsJsonAsync("device/refresh", new { refreshToken }, ct)
				.ConfigureAwait(false);

			if (respuesta.StatusCode == HttpStatusCode.Unauthorized
				|| respuesta.StatusCode == HttpStatusCode.Forbidden
				|| respuesta.StatusCode == HttpStatusCode.NotFound) return null;

			respuesta.EnsureSuccessStatusCode();

			Credencial rotada = await respuesta.Content.ReadFromJsonAsync<Credencial>(ct).ConfigureAwait(false);
			return string.IsNullOrWhiteSpace(rotada?.AccessToken) ? null : rotada;
		}
	}

	public sealed class Credencial
	{
		[JsonPropertyName("accessToken")]
		public string AccessToken { get; set; } = "";

		[JsonPropertyName("refreshToken")]
		public string RefreshToken { get; set; } = "";

		[JsonPropertyName("expiresIn")]
		public int ExpiresIn { get; set; }

		[JsonPropertyName("scope")]
		public string Scope { get; set; } = "";
	}
}
