using System;
using System.Text.Json;
using Cargas.Dominio;
using Puppets.Portador;

namespace Cargas.App.Eventos
{
	// EL EVENTO PRINCIPAL de este negocio: un surtidor despacho una carga a nombre de
	// la placa. Lo que se simula es "un surtidor despacha", no "la estacion registra
	// una venta" (§6.3.5 del brief): por eso el evento trae el surtidor con su precio.
	//
	// Este tipo es la frontera: de aca para adentro no entra nada del payload del
	// proveedor. Solo se usa como ENTRADA de un PerformCommand; lo que la pantalla ve
	// sale siempre de un PerformQuery sobre el actor.
	//
	// EL WIRE ES PLANO A PROPOSITO: precioMonto/precioMoneda y no un objeto anidado.
	// La leccion de examenes aplica igual: el serializador emite TODOS los campos de un
	// objeto anidado aunque esten en cero, asi que la presencia no dice nada. Con campos
	// planos, cada uno se valida por su VALOR.
	public sealed class CargaRegistrada : IMensajeDeEvento
	{
		public const string TipoDeEvento = "CargaRegistrada";

		public long Evento { get; private set; }
		public string Tipo => TipoDeEvento;

		public DateTime Fecha { get; private set; }
		public string Placa { get; private set; }
		public string TipoDeCombustible { get; private set; }
		public string SurtidorIdentificacion { get; private set; }
		public string SurtidorEstacion { get; private set; }
		public decimal PrecioMonto { get; private set; }
		public string PrecioMoneda { get; private set; }
		public decimal VolumenValor { get; private set; }
		public string VolumenUnidad { get; private set; }
		public decimal TotalMonto { get; private set; }
		public string TotalMoneda { get; private set; }
		public string FormaDeCorte { get; private set; }
		public string NumeroDeTransaccion { get; private set; }

		private CargaRegistrada() { }

		// Devuelve null si la senal no es una CargaRegistrada bien formada. El bucle
		// DESCARTA un null; no inventa un comando para tragarse la senal. Una estacion
		// puede emitir basura -una unidad desconocida, un monto corrido- y eso NO es
		// motivo para que la app deje de ponerse al dia con el resto.
		public static CargaRegistrada Interpretar(SenalDeEvento senal)
		{
			if (senal.Tipo != TipoDeEvento) return null;
			if (senal.Contenido.Length == 0) return null;

			try
			{
				using JsonDocument documento = JsonDocument.Parse(senal.Contenido);
				JsonElement raiz = documento.RootElement;

				// Sin fecha no se puede ordenar en la serie, que es para lo que existe
				// este dominio.
				if (!raiz.TryGetProperty("fecha", out JsonElement fechaElemento)
					|| !fechaElemento.TryGetDateTime(out DateTime fecha)) return null;

				string placa = Texto(raiz, "placa");
				if (placa.Length == 0) return null;

				// Sin transaccion los aportes de Sonia no tienen a que adjuntarse.
				string transaccion = Texto(raiz, "numeroDeTransaccion");
				if (transaccion.Length == 0) return null;

				// Los conjuntos cerrados se RECONOCEN en la frontera y se descarta lo
				// desconocido: EsConocido decide, PorNombre revienta, y aca corresponde
				// decidir. Un "gal" pelado cae aca: no dice cual galon es.
				string tipo = Texto(raiz, "tipoDeCombustible");
				if (!Dominio.TipoDeCombustible.EsConocido(tipo)) return null;

				string unidad = Texto(raiz, "volumenUnidad");
				if (!UnidadDeVolumen.EsConocida(unidad)) return null;

				string monedaDelPrecio = Texto(raiz, "precioMoneda");
				string monedaDelTotal = Texto(raiz, "totalMoneda");
				if (!Moneda.EsConocida(monedaDelPrecio)) return null;
				if (!Moneda.EsConocida(monedaDelTotal)) return null;

				string corte = Texto(raiz, "formaDeCorte");
				if (!Dominio.FormaDeCorte.EsConocida(corte)) return null;

				string estacion = Texto(raiz, "surtidorEstacion");
				string surtidor = Texto(raiz, "surtidorIdentificacion");
				if (estacion.Length == 0 || surtidor.Length == 0) return null;

				// Se valida el VALOR, no la presencia: el serializador emite los campos
				// numericos siempre, aunque esten en cero.
				decimal volumen = Decimal(raiz, "volumenValor");
				decimal precio = Decimal(raiz, "precioMonto");
				decimal total = Decimal(raiz, "totalMonto");
				if (volumen <= 0m || precio <= 0m || total <= 0m) return null;

				return new CargaRegistrada
				{
					Evento = senal.Numero,
					Fecha = fecha,
					Placa = placa,
					TipoDeCombustible = tipo,
					SurtidorIdentificacion = surtidor,
					SurtidorEstacion = estacion,
					PrecioMonto = precio,
					PrecioMoneda = monedaDelPrecio,
					VolumenValor = volumen,
					VolumenUnidad = unidad,
					TotalMonto = total,
					TotalMoneda = monedaDelTotal,
					FormaDeCorte = corte,
					NumeroDeTransaccion = transaccion,
				};
			}
			catch (JsonException)
			{
				return null;
			}
		}

		private static string Texto(JsonElement raiz, string nombre) =>
			raiz.TryGetProperty(nombre, out JsonElement e) && e.ValueKind == JsonValueKind.String
				? e.GetString() : "";

		private static decimal Decimal(JsonElement raiz, string nombre) =>
			raiz.TryGetProperty(nombre, out JsonElement e) && e.TryGetDecimal(out decimal valor)
				? valor : 0m;
	}
}
