using System;
using System.Text.Json;
using Examenes.Dominio;
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
	// EL CAMPO `examen` SE RESUELVE A UNA IDENTIDAD, NO SE COMPARA CONTRA UNA LISTA.
	//
	// Antes habia cinco constantes y un EsConocido que las comparaba con ==. Eso obliga al
	// laboratorio a escribir el token exacto que esta app eligio, y ningun laboratorio
	// real hace eso: medido sobre tres reportes del mismo paciente, la hemoglobina llega
	// como "HEMOGLOBINA" en uno y como "HGB" en otro. Con == el segundo se descarta en
	// silencio, que es la peor forma de perder un dato: sin error y sin rastro.
	//
	// Ahora el nombre entra por Analito.PorNombre, que es la misma frontera que ya usaba
	// UnidadDeMedida.PorSimbolo. El emisor escribe como escribe y el mensaje sale con la
	// identidad ya resuelta, asi que de aca para adentro nadie vuelve a mirar una cadena.
	public sealed class ExamenAgregado : IMensajeDeEvento
	{
		public const string TipoDeEvento = "ExamenAgregado";

		// El unico token que NO es un analito y por eso sigue siendo una constante: la
		// orina es otro tipo de muestra, no otra sustancia medida. No da un numero, da un
		// color y unos nitritos, y en el dominio no es un ExamenDeSangre.
		public const string Orina = "Orina";

		// Token de FORMA, no de analito, y el unico que queda. Dice "colesterol
		// fraccionado", o sea LDL con su HDL al lado en el mismo examen. Se sigue
		// aceptando porque es lo que emite URL2 hoy; un emisor que mande "LDL" con
		// valorSecundario entra por el mismo lugar y produce el mismo mensaje.
		private const string TokenDeColesterolFraccionado = "ColesterolDetallado";

		public long Evento { get; private set; }
		public string Tipo => TipoDeEvento;

		// QUE se midio, ya resuelto a identidad. Null cuando el examen es de orina, que es
		// el unico caso en que no hay un analito detras.
		public Analito Analito { get; private set; }

		public bool EsDeOrina => Analito == null;

		// Si el examen trajo DOS valores: el del analito y su fraccion acompanante. Es la
		// forma del payload y no la identidad de lo medido, y por eso viaja aparte en vez
		// de estar codificada en el nombre.
		public bool TraeFraccionamiento { get; private set; }

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

				string queSeMidio = Texto(raiz, "examen");
				if (queSeMidio.Length == 0) return null;

				// Sin fecha no se puede ordenar en una serie, que es para lo que existe
				// este dominio. Se exige antes que nada.
				if (!raiz.TryGetProperty("fecha", out JsonElement fechaElemento)
					|| !fechaElemento.TryGetDateTime(out DateTime fecha)) return null;

				string color = Texto(raiz, "color");
				string unidad = Texto(raiz, "unidad");
				bool traeFraccionamiento = TieneNumero(raiz, "valorSecundario");

				// La orina no trae numero ni unidad; los de sangre no traen color. Se
				// exige aca lo que cada uno necesita: un evento incompleto se descarta
				// en la frontera, no revienta adentro del comando.
				if (EsOrina(queSeMidio))
				{
					if (color.Length == 0) return null;

					return Armar(senal, null, false, raiz, fecha, unidad, color);
				}

				Analito analito = Resolver(queSeMidio, ref traeFraccionamiento);

				// Descarta, no revienta. Un laboratorio publica el perfil que se le pidio y
				// esta app modela cinco analitos: recibir uno que no modela es lo normal,
				// no un defecto. Es la misma razon por la que la ruta devuelve null.
				if (analito == null) return null;

				if (unidad.Length == 0) return null;
				if (!TieneNumero(raiz, "valor")) return null;

				// El LDL solo entra fraccionado, porque el dominio lo modela junto con su
				// HDL en un mismo examen. Un LDL suelto no tiene donde aplicarse, asi que
				// se descarta en vez de construir un examen a medias.
				if (analito.EsElMismo(Analito.ColesterolLdl) && !traeFraccionamiento) return null;

				// Y el HDL no forma serie por su cuenta: viaja como acompanante del LDL.
				// Llegar solo es un evento que esta app no sabe aplicar.
				if (analito.EsElMismo(Analito.ColesterolHdl)) return null;

				return Armar(senal, analito, traeFraccionamiento, raiz, fecha, unidad, color);
			}
			catch (JsonException)
			{
				return null;
			}
		}

		// La traduccion del campo `examen` a identidad, con el unico token de forma que
		// queda. Devuelve null para lo que esta app no modela.
		private static Analito Resolver(string queSeMidio, ref bool traeFraccionamiento)
		{
			if (string.Equals(queSeMidio, TokenDeColesterolFraccionado,
				StringComparison.OrdinalIgnoreCase))
			{
				traeFraccionamiento = true;
				return Analito.ColesterolLdl;
			}

			// EsConocido y no PorNombre: aca un nombre que no resuelve NO es un defecto.
			// PorNombre revienta, que es lo correcto cuando alguien PREGUNTA por un analito
			// -es un error de quien pregunta- y es lo incorrecto cuando un laboratorio
			// PUBLICA uno que esta app no modela, que es lo esperable.
			return Analito.EsConocido(queSeMidio) ? Analito.PorNombre(queSeMidio) : null;
		}

		private static ExamenAgregado Armar(SenalDeEvento senal, Analito analito,
			bool traeFraccionamiento, JsonElement raiz, DateTime fecha, string unidad, string color) =>
			new ExamenAgregado
			{
				Evento = senal.Numero,
				Analito = analito,
				TraeFraccionamiento = traeFraccionamiento,
				Fecha = fecha,
				Valor = Decimal(raiz, "valor"),
				ValorSecundario = Decimal(raiz, "valorSecundario"),
				Unidad = unidad,
				Color = color,
				Nitritos = Texto(raiz, "nitritos"),
			};

		private static bool EsOrina(string examen) =>
			string.Equals(examen, Orina, StringComparison.OrdinalIgnoreCase);

		private static bool TieneNumero(JsonElement raiz, string nombre) =>
			raiz.TryGetProperty(nombre, out JsonElement e) && e.TryGetDecimal(out _);

		private static string Texto(JsonElement raiz, string nombre) =>
			raiz.TryGetProperty(nombre, out JsonElement e) && e.ValueKind == JsonValueKind.String
				? e.GetString() : "";

		private static decimal Decimal(JsonElement raiz, string nombre) =>
			raiz.TryGetProperty(nombre, out JsonElement e) && e.TryGetDecimal(out decimal valor) ? valor : 0m;
	}
}
