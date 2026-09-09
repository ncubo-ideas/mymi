using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
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
	// LA CREDENCIAL ES DE ESTA CLASE, no del bucle. Quien lee por HTTP necesita un
	// token; quien lee por el puente de Gnothi no. Poner eso en el bucle lo obligaba
	// a conocer un detalle que solo el transporte tiene, y dejaba a la fuente
	// hospedada teniendo que inventarse un token que nadie usa.
	public sealed class FuenteHttpDeEventos : IFuenteDeEventos
	{
		private readonly HttpClient http;
		private readonly string recurso;
		private readonly string id;
		private readonly Func<CancellationToken, Task<string>> credencial;
		private readonly Func<CancellationToken, Task<string>> renovar;

		public string Nombre => $"{recurso}/{id}/events";

		// credencial: de donde sale el access token. Es del HOST de la app -el browser, la
		// consola-, NO del journal: un token es contexto de sesion, no historia de la
		// persona. Puede conseguirlo si todavia no lo tiene.
		// renovar: que hacer cuando el proveedor lo rechaza. Devuelve el token nuevo. Es
		// opcional porque no todo host puede rotar -la consola de medicion no lo
		// necesita-, pero la app instalada SI lo pasa: el access token es corto a
		// proposito, asi que sin rotacion la app se rompe sola al rato.
		public FuenteHttpDeEventos(HttpClient http, string recurso, string id,
			Func<CancellationToken, Task<string>> credencial,
			Func<CancellationToken, Task<string>> renovar = null)
		{
			ArgumentNullException.ThrowIfNull(http);
			ArgumentNullException.ThrowIfNullOrWhiteSpace(recurso);
			ArgumentNullException.ThrowIfNullOrWhiteSpace(id);
			ArgumentNullException.ThrowIfNull(credencial);

			this.http = http;
			this.recurso = recurso;
			this.id = id;
			this.credencial = credencial;
			this.renovar = renovar;
		}

		public async Task<SobreLeido> LeerDesde(long despuesDe, CancellationToken ct)
		{
			string token = await credencial(ct).ConfigureAwait(false);
			if (string.IsNullOrWhiteSpace(token))
				throw new InvalidOperationException(
					$"No hay credencial para leer {Nombre}. El host tiene que conseguirla antes de refrescar.");

			try
			{
				return await Leer(despuesDe, token, ct).ConfigureAwait(false);
			}
			catch (CredencialRechazadaException) when (renovar != null)
			{
				// Un solo reintento, y solo ante un rechazo de credencial. Si el token
				// rotado tambien es rechazado, el problema no es la expiracion y
				// reintentar seria castigar al proveedor con pedidos que ya sabemos que
				// fallan.
				string renovado = await renovar(ct).ConfigureAwait(false);
				if (string.IsNullOrWhiteSpace(renovado)) throw;

				return await Leer(despuesDe, renovado, ct).ConfigureAwait(false);
			}
		}

		private async Task<SobreLeido> Leer(long despuesDe, string token, CancellationToken ct)
		{
			using var pedido = new HttpRequestMessage(HttpMethod.Get,
				$"{recurso}/{Uri.EscapeDataString(id)}/events?after={despuesDe}");
			pedido.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

			using HttpResponseMessage respuesta = await http.SendAsync(pedido, ct).ConfigureAwait(false);

			// Se distingue del resto de los errores con su propio tipo, porque ante este
			// SI se puede hacer algo: rotar la credencial y reintentar. Un 500 o un
			// timeout no tienen esa salida.
			if (respuesta.StatusCode == HttpStatusCode.Unauthorized || respuesta.StatusCode == HttpStatusCode.Forbidden)
				throw new CredencialRechazadaException(
					$"El proveedor rechazo la credencial ({(int)respuesta.StatusCode}) al leer {Nombre}.");

			respuesta.EnsureSuccessStatusCode();

			string cuerpo = await respuesta.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
			return SobreDeEventos.Interpretar(cuerpo);
		}
	}
}
