using System;
using System.Text.Json;
using Puppets.Portador;

namespace Cargas.App.Eventos
{
	// Un boleto de tombola, emitido por la estacion. UNO POR BOLETO y no uno por carga: el
	// numero ES la identidad del boleto, asi que es tambien la guarda de idempotencia
	// natural, igual que el numero de transaccion lo es para una carga.
	//
	// La app NO los deriva. El sorteo se rifa sobre numeros, y un "boleto #7" fabricado
	// localmente no sirve para reclamar nada: lo unico que la app deriva es la EXPLICACION
	// de cuantos dio cada carga.
	public sealed class BoletoEmitido : IMensajeDeEvento
	{
		public const string TipoDeEvento = "BoletoEmitido";

		public long Evento { get; private set; }
		public string Tipo => TipoDeEvento;

		public string NumeroDeBoleto { get; private set; }
		public string CodigoDelSorteo { get; private set; }
		public DateTime Fecha { get; private set; }
		public string NumeroDeTransaccion { get; private set; }

		private BoletoEmitido() { }

		public static BoletoEmitido Interpretar(SenalDeEvento senal)
		{
			if (senal.Tipo != TipoDeEvento) return null;
			if (senal.Contenido.Length == 0) return null;

			try
			{
				using JsonDocument documento = JsonDocument.Parse(senal.Contenido);
				JsonElement raiz = documento.RootElement;

				// Sin numero no es un boleto: es el que se rifa y es la guarda de
				// idempotencia. Nada que hacer con uno anonimo.
				string numero = Texto(raiz, "numeroDeBoleto");
				if (numero.Length == 0) return null;

				// Un boleto que no puede vencer no es un boleto de tombola.
				string sorteo = Texto(raiz, "codigoDelSorteo");
				if (sorteo.Length == 0) return null;

				if (!raiz.TryGetProperty("fecha", out JsonElement fechaElemento)
					|| !fechaElemento.TryGetDateTime(out DateTime fecha)) return null;

				// Sin transaccion no se puede explicar de que carga salio, que es la razon
				// de existir de la funcion. Se descarta acá y no se acepta a medias.
				string transaccion = Texto(raiz, "numeroDeTransaccion");
				if (transaccion.Length == 0) return null;

				return new BoletoEmitido
				{
					Evento = senal.Numero,
					NumeroDeBoleto = numero,
					CodigoDelSorteo = sorteo,
					Fecha = fecha,
					NumeroDeTransaccion = transaccion,
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
