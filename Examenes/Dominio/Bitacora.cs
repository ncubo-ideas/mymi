using System;

namespace Examenes.Dominio
{
	// Hasta donde leyo la app la narrativa del proveedor, y con que credencial la
	// sigue leyendo.
	//
	// UltimoEvento es lo que hace exacto el resume: vive en el journal, no en una
	// variable en memoria, y avanza DENTRO del mismo comando que aplica el examen.
	// Si la app se cae, se vuelve a pedir a partir de aca y no hay estado intermedio
	// posible - o el examen y su checkpoint entraron juntos, o no entro ninguno.
	//
	// Avanzar solo acepta ir hacia adelante. La guarda Check del comando ya rechaza
	// un evento repetido antes de escribir nada; esta validacion es la segunda linea,
	// para que el invariante viva en el dominio y no solo en el llamador.
	//
	// La credencial son tres piezas y las tres viven aca porque las tres son "con que
	// sigo leyendo": el DeviceKey identifica a ESTA instalacion ante el proveedor, el
	// access token es corto, y el refresh token es durable y rotatorio. Guardarlos en
	// el journal es lo que le permite a la app abrir y ponerse al dia sola, sin que la
	// persona pegue nada.
	//
	// DEUDA CONOCIDA Y DELIBERADA: quedan en claro en una carpeta del usuario que
	// tambien lee el proceso MCP. El access token expira pronto, pero el refresh token
	// es DURABLE - quien lea esa carpeta puede seguir leyendo los eventos del usuario
	// hasta que el proveedor lo revoque. Para el MVP se acepta; documentarlo es parte
	// de aceptarlo.
	public class Bitacora
	{
		public long UltimoEvento { get; private set; }

		public string DeviceKey { get; private set; } = "";

		public string Token { get; private set; } = "";

		public string RefreshToken { get; private set; } = "";

		public bool TieneDevice => DeviceKey.Length > 0;

		public bool TieneToken => Token.Length > 0;

		public bool PuedeRotar => RefreshToken.Length > 0;

		public void Avanzar(long evento)
		{
			if (evento <= UltimoEvento)
				throw new InvalidOperationException(
					$"El evento {evento} no avanza la bitacora: ya se habia procesado hasta {UltimoEvento}.");

			UltimoEvento = evento;
		}

		// Una sola vez por instalacion. Si se re-registrara con otra clave, el
		// proveedor veria otro device y la credencial vieja quedaria huerfana.
		public void RegistrarDevice(string deviceKey)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(deviceKey);
			if (DeviceKey.Length > 0 && DeviceKey != deviceKey)
				throw new InvalidOperationException(
					$"Esta instalacion ya esta registrada como device '{DeviceKey}'; no se puede cambiar a '{deviceKey}'.");

			DeviceKey = deviceKey;
		}

		// El refresh token es opcional: hay proveedores que solo dan el access token.
		public void GuardarCredencial(string token, string refreshToken)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(token);
			ArgumentNullException.ThrowIfNull(refreshToken);

			Token = token;
			RefreshToken = refreshToken;
		}
	}
}
