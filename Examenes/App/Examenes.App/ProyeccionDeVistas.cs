using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using Examenes.App.Vistas;

namespace Examenes.App
{
	// Convierte la emision del print en los records de vista.
	//
	// Esto NO es el antipatron de "parsear el print para sacar un valor tipado". Ese
	// antipatron es re-derivar en C# un ESCALAR que el actor ya calculo, y para eso la
	// app usa parametros Out tipados (CantidadDeExamenes, AnalitosFueraDeRango,
	// EdadDelPaciente). Lo de aca es otra cosa: una VISTA, y el print con formatter JSON
	// es su canal declarado - la guia dice devolver ese string tal cual porque "el print
	// ya hizo todo el trabajo de serializacion". Un cliente REST hace exactamente esto.
	// La diferencia es que aca el cliente esta en el mismo proceso.
	//
	// El formato de fecha es el del formatter: MM/dd/yyyy HH:mm:ss en cultura
	// invariante.
	internal static class ProyeccionDeVistas
	{
		private const string FormatoDeFecha = "MM/dd/yyyy HH:mm:ss";
		private const string FormatoDeFechaSinHora = "MM/dd/yyyy";

		internal static IReadOnlyList<ExamenEnLista> Lista(string json)
		{
			var examenes = new List<ExamenEnLista>();
			if (string.IsNullOrWhiteSpace(json)) return examenes;

			using JsonDocument documento = JsonDocument.Parse(json);

			// Sin examenes el foreach no emite nada y la propiedad no existe: eso es una
			// lista vacia, no un error.
			if (!documento.RootElement.TryGetProperty("examen", out JsonElement arreglo)) return examenes;

			foreach (JsonElement elemento in Elementos(arreglo))
				examenes.Add(new ExamenEnLista
				{
					Nombre = Texto(elemento, "nombre"),
					Fecha = Fecha(elemento, "fecha"),
					Resultado = Texto(elemento, "resultado"),
					AportaSerie = Booleano(elemento, "aportaSerie"),
					Severidad = Texto(elemento, "severidad"),
					ParaSonia = Texto(elemento, "paraSonia"),
				});

			return examenes;
		}

		internal static TendenciaDeAnalito Tendencia(string json)
		{
			if (string.IsNullOrWhiteSpace(json)) return new TendenciaDeAnalito();

			using JsonDocument documento = JsonDocument.Parse(json);
			JsonElement raiz = documento.RootElement;

			return new TendenciaDeAnalito
			{
				Analito = Texto(raiz, "analito"),
				Tendencia = Texto(raiz, "tendencia"),
				ParaMostrar = Texto(raiz, "paraMostrar"),
				Mediciones = Entero(raiz, "mediciones"),
				ValorInicial = Decimal(raiz, "valorInicial"),
				ValorFinal = Decimal(raiz, "valorFinal"),
				Unidad = Texto(raiz, "unidad"),
				Dias = Entero(raiz, "dias"),
				Cambio = Decimal(raiz, "cambio"),
				SugiereRepetirElExamen = Booleano(raiz, "sugiereRepetirElExamen"),
				HaySerie = Booleano(raiz, "haySerie"),
			};
		}

		internal static PresenciaDeAnalito Presencia(string json)
		{
			if (string.IsNullOrWhiteSpace(json)) return new PresenciaDeAnalito();

			using JsonDocument documento = JsonDocument.Parse(json);
			JsonElement raiz = documento.RootElement;

			return new PresenciaDeAnalito
			{
				Analito = Texto(raiz, "analito"),
				ParaMostrar = Texto(raiz, "paraMostrar"),
				Presencia = Texto(raiz, "presencia"),
				Mediciones = Entero(raiz, "mediciones"),
				Positivas = Entero(raiz, "positivas"),
				PositivasSeguidas = Entero(raiz, "positivasSeguidas"),
				Dias = Entero(raiz, "dias"),
				UltimoResultado = Texto(raiz, "ultimoResultado"),
				EstaPresente = Booleano(raiz, "estaPresente"),
				HaySerie = Booleano(raiz, "haySerie"),
			};
		}

		internal static IReadOnlyList<PruebaEnSerie> Pruebas(string json)
		{
			var pruebas = new List<PruebaEnSerie>();
			if (string.IsNullOrWhiteSpace(json)) return pruebas;

			using JsonDocument documento = JsonDocument.Parse(json);
			if (!documento.RootElement.TryGetProperty("prueba", out JsonElement arreglo)) return pruebas;

			foreach (JsonElement elemento in Elementos(arreglo))
				pruebas.Add(new PruebaEnSerie
				{
					Fecha = Fecha(elemento, "fecha"),
					Expresado = Texto(elemento, "expresado"),
					EsPositivo = Booleano(elemento, "esPositivo"),
					EsHallazgo = Booleano(elemento, "esHallazgo"),
				});

			return pruebas;
		}

		internal static LoMasUrgente Urgente(string json)
		{
			if (string.IsNullOrWhiteSpace(json)) return new LoMasUrgente();

			using JsonDocument documento = JsonDocument.Parse(json);
			JsonElement raiz = documento.RootElement;

			return new LoMasUrgente
			{
				Severidad = Texto(raiz, "severidad"),
				Accion = Texto(raiz, "accion"),
				PideAlgo = Booleano(raiz, "pideAlgo"),
			};
		}

		internal static IReadOnlyList<CeldaEnCuadro> Cuadro(string json)
		{
			var celdas = new List<CeldaEnCuadro>();
			if (string.IsNullOrWhiteSpace(json)) return celdas;

			using JsonDocument documento = JsonDocument.Parse(json);
			// La llave del arreglo es el nombre de la variable del foreach del DSL -"celda"-,
			// igual que "examen" en Lista. No es el nombre de la primera columna.
			if (!documento.RootElement.TryGetProperty("celda", out JsonElement arreglo)) return celdas;

			foreach (JsonElement elemento in Elementos(arreglo))
				celdas.Add(new CeldaEnCuadro
				{
					Fila = Entero(elemento, "fila"),
					Columna = Entero(elemento, "columna"),
					Analito = Texto(elemento, "analito"),
					ParaMostrar = Texto(elemento, "paraMostrar"),
					Fecha = Fecha(elemento, "fecha"),
					Expresado = Texto(elemento, "expresado"),
					AportaSerie = Booleano(elemento, "aportaSerie"),
					Severidad = Texto(elemento, "severidad"),
					ParaSonia = Texto(elemento, "paraSonia"),
				});

			return celdas;
		}

		internal static IReadOnlyList<PuntoDeSerie> Puntos(string json)
		{
			var puntos = new List<PuntoDeSerie>();
			if (string.IsNullOrWhiteSpace(json)) return puntos;

			using JsonDocument documento = JsonDocument.Parse(json);
			if (!documento.RootElement.TryGetProperty("punto", out JsonElement arreglo)) return puntos;

			foreach (JsonElement elemento in Elementos(arreglo))
				puntos.Add(new PuntoDeSerie
				{
					Fecha = Fecha(elemento, "fecha"),
					Valor = Decimal(elemento, "valor"),
					Unidad = Texto(elemento, "unidad"),
				});

			return puntos;
		}

		internal static IReadOnlyList<LineaDeReferencia> Referencias(string json)
		{
			var lineas = new List<LineaDeReferencia>();
			if (string.IsNullOrWhiteSpace(json)) return lineas;

			using JsonDocument documento = JsonDocument.Parse(json);
			if (!documento.RootElement.TryGetProperty("referencia", out JsonElement arreglo)) return lineas;

			foreach (JsonElement elemento in Elementos(arreglo))
				lineas.Add(new LineaDeReferencia
				{
					Nombre = Texto(elemento, "nombre"),
					Valor = Decimal(elemento, "valor"),
					Unidad = Texto(elemento, "unidad"),
					ParaSonia = Texto(elemento, "paraSonia"),
				});

			return lineas;
		}

		internal static IReadOnlyList<ControlEnEspera> Controles(string json)
		{
			var controles = new List<ControlEnEspera>();
			if (string.IsNullOrWhiteSpace(json)) return controles;

			using JsonDocument documento = JsonDocument.Parse(json);
			if (!documento.RootElement.TryGetProperty("control", out JsonElement arreglo)) return controles;

			foreach (JsonElement elemento in Elementos(arreglo))
				controles.Add(new ControlEnEspera
				{
					Analito = Texto(elemento, "analito"),
					ParaMostrar = Texto(elemento, "paraMostrar"),
					UltimaMedicion = Fecha(elemento, "ultimaMedicion"),
					DiasDesdeLaUltima = Entero(elemento, "diasDesdeLaUltima"),
					MesesRecomendados = Entero(elemento, "mesesRecomendados"),
					DiasQueFaltan = Entero(elemento, "diasQueFaltan"),
					EstaVencido = Booleano(elemento, "estaVencido"),
					Severidad = Texto(elemento, "severidad"),
					Accion = Texto(elemento, "accion"),
					ParaSonia = Texto(elemento, "paraSonia"),
				});

			return controles;
		}

		internal static InterpretacionDeAnalito Interpretacion(string json)
		{
			if (string.IsNullOrWhiteSpace(json)) return new InterpretacionDeAnalito();

			using JsonDocument documento = JsonDocument.Parse(json);
			JsonElement raiz = documento.RootElement;

			return new InterpretacionDeAnalito
			{
				Analito = Texto(raiz, "analito"),
				ParaMostrar = Texto(raiz, "paraMostrar"),
				Veredicto = Texto(raiz, "veredicto"),
				Severidad = Texto(raiz, "severidad"),
				Forma = Texto(raiz, "forma"),
				ParaSonia = Texto(raiz, "paraSonia"),
				Accion = Texto(raiz, "accion"),
				ReferenciaLegible = Texto(raiz, "referencia"),
				PideAlgo = Booleano(raiz, "pideAlgo"),
				SugiereRepetirElExamen = Booleano(raiz, "sugiereRepetirElExamen"),
			};
		}

		internal static VelocidadDeAnalito Velocidad(string json)
		{
			if (string.IsNullOrWhiteSpace(json)) return new VelocidadDeAnalito();

			using JsonDocument documento = JsonDocument.Parse(json);
			JsonElement raiz = documento.RootElement;

			return new VelocidadDeAnalito
			{
				Analito = Texto(raiz, "analito"),
				ParaMostrar = Texto(raiz, "paraMostrar"),
				PorAno = Decimal(raiz, "porAno"),
				Unidad = Texto(raiz, "unidad"),
				Dias = Entero(raiz, "dias"),
				Umbral = Decimal(raiz, "umbral"),
				EsAplicable = Booleano(raiz, "esAplicable"),
				ExigeAtencion = Booleano(raiz, "exigeAtencion"),
				Expresada = Texto(raiz, "expresada"),
			};
		}

		internal static IReadOnlyList<string> Analitos(string json)
		{
			var nombres = new List<string>();
			if (string.IsNullOrWhiteSpace(json)) return nombres;

			using JsonDocument documento = JsonDocument.Parse(json);
			if (!documento.RootElement.TryGetProperty("analito", out JsonElement arreglo)) return nombres;

			foreach (JsonElement elemento in Elementos(arreglo))
			{
				string nombre = Texto(elemento, "nombre");
				if (nombre.Length > 0) nombres.Add(nombre);
			}
			return nombres;
		}

		// Un solo print emite un objeto; varios emiten un arreglo. Tratar los dos casos
		// evita que la vista se rompa justo cuando hay un solo examen.
		private static IEnumerable<JsonElement> Elementos(JsonElement elemento)
		{
			if (elemento.ValueKind == JsonValueKind.Array)
			{
				foreach (JsonElement hijo in elemento.EnumerateArray()) yield return hijo;
				yield break;
			}
			if (elemento.ValueKind == JsonValueKind.Object) yield return elemento;
		}

		private static string Texto(JsonElement padre, string nombre) =>
			padre.TryGetProperty(nombre, out JsonElement e) && e.ValueKind == JsonValueKind.String
				? e.GetString() : "";

		private static int Entero(JsonElement padre, string nombre) =>
			padre.TryGetProperty(nombre, out JsonElement e) && e.TryGetInt32(out int valor) ? valor : 0;

		private static decimal Decimal(JsonElement padre, string nombre) =>
			padre.TryGetProperty(nombre, out JsonElement e) && e.TryGetDecimal(out decimal valor) ? valor : 0m;

		private static bool Booleano(JsonElement padre, string nombre)
		{
			if (!padre.TryGetProperty(nombre, out JsonElement e)) return false;
			if (e.ValueKind == JsonValueKind.True) return true;
			if (e.ValueKind == JsonValueKind.False) return false;

			// El formatter puede emitir el booleano como texto; se aceptan las dos formas
			// para que la vista no dependa de ese detalle.
			return e.ValueKind == JsonValueKind.String
				&& bool.TryParse(e.GetString(), out bool valor) && valor;
		}

		private static DateTime Fecha(JsonElement padre, string nombre)
		{
			string texto = Texto(padre, nombre);
			if (texto.Length == 0) return default;

			if (DateTime.TryParseExact(texto, FormatoDeFecha, CultureInfo.InvariantCulture,
					DateTimeStyles.None, out DateTime conHora)) return conHora;
			if (DateTime.TryParseExact(texto, FormatoDeFechaSinHora, CultureInfo.InvariantCulture,
					DateTimeStyles.None, out DateTime soloFecha)) return soloFecha;

			return DateTime.TryParse(texto, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime suelta)
				? suelta : default;
		}
	}
}
