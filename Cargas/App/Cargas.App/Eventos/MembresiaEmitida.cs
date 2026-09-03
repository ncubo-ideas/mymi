using System;
using System.Text.Json;
using Puppets.Portador;

namespace Cargas.App.Eventos
{
	// EL CARNET DE SOCIA, emitido por la estacion. El numero es el que Sonia le muestra a
	// la bomba para que le acrediten; NO es la llave con la que esta app pide sus eventos,
	// y esa distincion es deliberada: esa llave se cambia por un token, asi que un carnet
	// que la llevara seria una credencial fotografiable.
	//
	// Reemitir es legitimo -reimprimir un carnet, cambiar de numero- y por eso el comando
	// no tiene guarda de "ya afiliada": la unica guarda es la del checkpoint.
	public sealed class MembresiaEmitida : IMensajeDeEvento
	{
		public const string TipoDeEvento = "MembresiaEmitida";

		public long Evento { get; private set; }
		public string Tipo => TipoDeEvento;

		public string NumeroDeSocio { get; private set; }
		public string Nombre { get; private set; }
		public DateTime Desde { get; private set; }

		private MembresiaEmitida() { }

		public static MembresiaEmitida Interpretar(SenalDeEvento senal)
		{
			if (senal.Tipo != TipoDeEvento) return null;
			if (senal.Contenido.Length == 0) return null;

			try
			{
				using JsonDocument documento = JsonDocument.Parse(senal.Contenido);
				JsonElement raiz = documento.RootElement;

				// Sin numero no hay carnet: es lo unico que el QR tiene que llevar.
				string socio = Texto(raiz, "numeroDeSocio");
				if (socio.Length == 0) return null;

				if (!raiz.TryGetProperty("desde", out JsonElement desdeElemento)
					|| !desdeElemento.TryGetDateTime(out DateTime desde)) return null;

				// El nombre SI puede venir vacio: un carnet sin nombre sigue acreditando,
				// y pedirlo obligatorio seria exigir un dato personal para nada.
				return new MembresiaEmitida
				{
					Evento = senal.Numero,
					NumeroDeSocio = socio,
					Nombre = Texto(raiz, "nombre"),
					Desde = desde,
				};
			}
			catch (JsonException)
			{
				return null;
			}
		}

		private static string Texto(JsonElement raiz, string nombre) =>
			raiz.TryGetProperty(nombre, out JsonElement elemento)
			&& elemento.ValueKind == JsonValueKind.String
				? elemento.GetString().Trim()
				: "";
	}
}
