using System;

namespace Compras.Dominio
{
	/// <summary>
	/// Hasta donde leyo la app la narrativa del supermercado, y con que credencial la
	/// sigue leyendo. Vive en el journal, no en memoria: es lo que hace exacto el resume
	/// tras una caida, porque avanza DENTRO del mismo comando que aplica la compra.
	/// </summary>
	// DEUDA CONOCIDA Y DELIBERADA (heredada de Facturas/Examenes): las tres piezas quedan
	// en claro en una carpeta del usuario que tambien lee el proceso MCP. El access token
	// expira pronto, pero el refresh es DURABLE: quien lea esa carpeta puede seguir
	// leyendo los eventos hasta que el proveedor lo revoque. Para el MVP se acepta.
	public class Bitacora
	{
		public long UltimoEvento { get; private set; }

		public string DeviceKey { get; private set; } = "";

		public string Token { get; private set; } = "";

		public string RefreshToken { get; private set; } = "";

		public bool TieneDevice => DeviceKey.Length > 0;

		public bool TieneToken => Token.Length > 0;

		public bool PuedeRotar => RefreshToken.Length > 0;

		/// <summary>Solo hacia adelante. La guarda Check del comando ya rechaza el repetido; esta es la segunda linea.</summary>
		public void Avanzar(long evento)
		{
			if (evento <= UltimoEvento)
				throw new InvalidOperationException(
					$"El evento {evento} no avanza la bitacora: ya se habia procesado hasta {UltimoEvento}.");

			UltimoEvento = evento;
		}

		/// <summary>Una sola vez por instalacion: re-registrar dejaria huerfana la credencial vieja.</summary>
		public void RegistrarDevice(string deviceKey)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(deviceKey);
			if (DeviceKey.Length > 0 && DeviceKey != deviceKey)
				throw new InvalidOperationException(
					$"Esta instalacion ya esta registrada como device '{DeviceKey}'; no se puede cambiar a '{deviceKey}'.");

			DeviceKey = deviceKey;
		}

		public void GuardarCredencial(string token, string refreshToken)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(token);
			// Vacio es legitimo: hay proveedores que solo dan el access token.
			ArgumentNullException.ThrowIfNull(refreshToken);

			Token = token;
			RefreshToken = refreshToken;
		}
	}
}
