using System;
using System.Text.Json;
using Puppets.Portador;

namespace Examenes.App.Eventos
{
	// El unico evento de este negocio: el laboratorio registro un examen a nombre del
	// usuario.
	//
	// Forma de IDispatchMessage: un tag de routing compacto y un Deserialize que
	// entiende SU propio wire. El tag es "ExamenAgregado" y el layout es el JSON del
	// proveedor; ninguno de los dos aparece en el texto de un comando.
	//
	// Este tipo es la frontera: de aca para adentro no entra nada del payload del
	// proveedor. Solo se usa como ENTRADA de un PerformCommand, nunca se le muestra a la
	// UI - lo que la pantalla ve sale siempre de un PerformQuery sobre el actor.
	//
	// Un solo tipo de evento con un DISCRIMINADOR de examen, en vez de cinco tipos de
	// evento: el proveedor emite "te registre un examen" y que examen es un campo. El
	// que traduce ese campo a una clase del dominio es ExamenesAPI, en la frontera, con
	// un switch. El dominio nunca ve el string.
	public sealed class ExamenAgregado : IMensajeDeEvento
	{
		public const string TipoDeEvento = "ExamenAgregado";

		// Los examenes que ESTA app sabe aplicar. Un examen que no este aca se descarta
		// en la ruta: el proveedor puede emitir mas de los que la app modela.
		public const string Colesterol = "Colesterol";
		public const string ColesterolDetallado = "ColesterolDetallado";
		public const string AntigenoProstatico = "AntigenoProstatico";
		public const string Hemoglobina = "Hemoglobina";
		public const string Orina = "Orina";

		public long Evento { get; private set; }
		public string Tipo => TipoDeEvento;

		// Cual de los cinco. Se valida en Interpretar, asi que para cuando este mensaje
		// existe ya se sabe que la app tiene comando para el.
		public string Examen { get; private set; }

		public DateTime Fecha { get; private set; }
		public decimal Valor { get; private set; }
		public decimal ValorSecundario { get; private set; }
		public string Unidad { get; private set; }
		public string Color { get; private set; }

		// Vacio significa que el laboratorio NO corrio la prueba de nitritos, que es un
		// estado legitimo del urianalisis y distinto de un negativo. El dominio lo
		// resuelve a Nitritos.NoRealizado; aca no se decide nada, solo se transporta.
		public string Nitritos { get; private set; }

		private ExamenAgregado() { }

		// Devuelve null si la senal no es un ExamenAgregado bien formado, o si es de un
		// examen que esta app no modela. El bucle DESCARTA un null; no inventa un
		// comando para tragarse la senal.
		public static ExamenAgregado Interpretar(SenalDeEvento senal)
		{
			if (senal.Tipo != TipoDeEvento) return null;
			if (senal.Contenido.Length == 0) return null;

			try
			{
				using JsonDocument documento = JsonDocument.Parse(senal.Contenido);
				JsonElement raiz = documento.RootElement;

				string examen = Texto(raiz, "examen");
				if (!EsConocido(examen)) return null;

				if (!raiz.TryGetProperty("fecha", out JsonElement fechaElemento)
					|| !fechaElemento.TryGetDateTime(out DateTime fecha)) return null;

				string color = Texto(raiz, "color");
				string unidad = Texto(raiz, "unidad");

				// La orina no trae numero ni unidad; los de sangre no traen color. Se
				// exige aca lo que cada uno necesita: un evento incompleto se descarta
				// en la frontera, no revienta adentro del comando.
				if (examen == Orina)
				{
					if (color.Length == 0) return null;
				}
				else
				{
					if (unidad.Length == 0) return null;
					if (!raiz.TryGetProperty("valor", out JsonElement valorElemento)
						|| !valorElemento.TryGetDecimal(out _)) return null;
				}

				return new ExamenAgregado
				{
					Evento = senal.Numero,
					Examen = examen,
					Fecha = fecha,
					Valor = Decimal(raiz, "valor"),
					ValorSecundario = Decimal(raiz, "valorSecundario"),
					Unidad = unidad,
					Color = color,
					Nitritos = Texto(raiz, "nitritos"),
				};
			}
			catch (JsonException)
			{
				return null;
			}
		}

		private static bool EsConocido(string examen) =>
			examen == Colesterol || examen == ColesterolDetallado || examen == AntigenoProstatico
			|| examen == Hemoglobina || examen == Orina;

		private static string Texto(JsonElement raiz, string nombre) =>
			raiz.TryGetProperty(nombre, out JsonElement e) && e.ValueKind == JsonValueKind.String
				? e.GetString() : "";

		private static decimal Decimal(JsonElement raiz, string nombre) =>
			raiz.TryGetProperty(nombre, out JsonElement e) && e.TryGetDecimal(out decimal valor) ? valor : 0m;
	}
}
