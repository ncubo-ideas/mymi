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
	// Y ES DONDE SE ESCRIBE LA FRASE. El dominio manda la clave y sus valores; aca se le
	// pide al renderizador del idioma que la diga. Los codigos siguen viajando al lado:
	// una pantalla decide con el codigo y muestra el dicho.
	//
	// El formato de fecha es el del formatter: MM/dd/yyyy HH:mm:ss en cultura
	// invariante.
	internal static class ProyeccionDeVistas
	{
		private const string FormatoDeFecha = "MM/dd/yyyy HH:mm:ss";
		private const string FormatoDeFechaSinHora = "MM/dd/yyyy";

		internal static IReadOnlyList<ExamenEnLista> Lista(string json, Frases.Frases frases)
		{
			var examenes = new List<ExamenEnLista>();
			if (string.IsNullOrWhiteSpace(json)) return examenes;

			using JsonDocument documento = JsonDocument.Parse(json);

			// Sin examenes el foreach no emite nada y la propiedad no existe: eso es una
			// lista vacia, no un error.
			if (!documento.RootElement.TryGetProperty("examen", out JsonElement arreglo)) return examenes;

			// LAS LECTURAS LLEGAN PLANAS Y CON EL INDICE DE SU EXAMEN, que es lo que deja
			// rearmar el resultado de cada fila sin volver a decidir que va donde. Ver
			// LecturaEnLaLista: el print proyecta un solo nivel.
			var porFila = new Dictionary<int, List<(string, string, decimal, string, bool)>>();
			if (documento.RootElement.TryGetProperty("lectura", out JsonElement lecturas))
				foreach (JsonElement elemento in Elementos(lecturas))
				{
					int fila = Entero(elemento, "fila");
					if (!porFila.TryGetValue(fila, out var suyas))
					{
						suyas = new List<(string, string, decimal, string, bool)>();
						porFila[fila] = suyas;
					}

					suyas.Add((Texto(elemento, "analito"), Texto(elemento, "resultado"),
						Decimal(elemento, "valor"), Texto(elemento, "unidad"),
						Booleano(elemento, "esNumero")));
				}

			int cual = 0;
			foreach (JsonElement elemento in Elementos(arreglo))
			{
				string clave = Texto(elemento, "nombre");
				porFila.TryGetValue(cual, out var suyas);

				examenes.Add(new ExamenEnLista
				{
					Nombre = Frases.LaRedaccion.Nombre(frases, clave),
					Fecha = Fecha(elemento, "fecha"),
					Resultado = Frases.LaRedaccion.Resultado(frases, clave,
						suyas ?? new List<(string, string, decimal, string, bool)>()),
					AportaSerieContinua = Booleano(elemento, "aportaSerieContinua"),
				});

				cual++;
			}

			return examenes;
		}

		internal static TendenciaDeAnalito Tendencia(string json, Frases.Frases frases)
		{
			if (string.IsNullOrWhiteSpace(json)) return new TendenciaDeAnalito();

			using JsonDocument documento = JsonDocument.Parse(json);
			JsonElement raiz = documento.RootElement;

			string tendencia = Texto(raiz, "tendencia");

			return new TendenciaDeAnalito
			{
				Analito = Texto(raiz, "analito"),
				Tendencia = tendencia,
				TendenciaDicha = Frases.LaRedaccion.Tendencia(frases, tendencia),
				ParaMostrar = Frases.LaRedaccion.Nombre(frases, Texto(raiz, "claveDelAnalito")),
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

		internal static PresenciaDeAnalito Presencia(string json, Frases.Frases frases)
		{
			if (string.IsNullOrWhiteSpace(json)) return new PresenciaDeAnalito();

			using JsonDocument documento = JsonDocument.Parse(json);
			JsonElement raiz = documento.RootElement;

			string presencia = Texto(raiz, "presencia");

			return new PresenciaDeAnalito
			{
				Analito = Texto(raiz, "analito"),
				ParaMostrar = Frases.LaRedaccion.Nombre(frases, Texto(raiz, "claveDelAnalito")),
				Presencia = presencia,
				PresenciaDicha = Frases.LaRedaccion.Presencia(frases, presencia),
				Mediciones = Entero(raiz, "mediciones"),
				Positivas = Entero(raiz, "positivas"),
				PositivasSeguidas = Entero(raiz, "positivasSeguidas"),
				Dias = Entero(raiz, "dias"),
				UltimoResultado = Frases.LaRedaccion.Categoria(frases, Texto(raiz, "ultimoResultado")),
				EstaPresente = Booleano(raiz, "estaPresente"),
				HaySerie = Booleano(raiz, "haySerie"),
			};
		}

		internal static IReadOnlyList<PruebaEnSerie> Pruebas(string json, Frases.Frases frases)
		{
			var pruebas = new List<PruebaEnSerie>();
			if (string.IsNullOrWhiteSpace(json)) return pruebas;

			using JsonDocument documento = JsonDocument.Parse(json);
			if (!documento.RootElement.TryGetProperty("prueba", out JsonElement arreglo)) return pruebas;

			foreach (JsonElement elemento in Elementos(arreglo))
				pruebas.Add(new PruebaEnSerie
				{
					Fecha = Fecha(elemento, "fecha"),
					Expresado = Frases.LaRedaccion.Categoria(frases, Texto(elemento, "resultado")),
					EsPositivo = Booleano(elemento, "esPositivo"),
					EsHallazgo = Booleano(elemento, "esHallazgo"),
				});

			return pruebas;
		}

		internal static ConstanciaDeAnalito Constancia(string json, Frases.Frases frases)
		{
			if (string.IsNullOrWhiteSpace(json)) return new ConstanciaDeAnalito();

			using JsonDocument documento = JsonDocument.Parse(json);
			JsonElement raiz = documento.RootElement;

			string constancia = Texto(raiz, "constancia");

			return new ConstanciaDeAnalito
			{
				Analito = Texto(raiz, "analito"),
				ParaMostrar = Frases.LaRedaccion.Nombre(frases, Texto(raiz, "claveDelAnalito")),
				Constancia = constancia,
				ConstanciaDicha = Frases.LaRedaccion.Constancia(frases, constancia),
				Mediciones = Entero(raiz, "mediciones"),
				UltimaCategoria = Frases.LaRedaccion.Categoria(frases, Texto(raiz, "ultimaCategoria")),
				UltimaEsEsperable = Booleano(raiz, "ultimaEsEsperable"),
				NoEsperables = Entero(raiz, "noEsperables"),
				NoEsperablesSeguidas = Entero(raiz, "noEsperablesSeguidas"),
				LaUltimaSeRepitio = Booleano(raiz, "laUltimaSeRepitio"),
				Dias = Entero(raiz, "dias"),
				EstaFueraDeLoEsperable = Booleano(raiz, "estaFueraDeLoEsperable"),
				HaySerie = Booleano(raiz, "haySerie"),
			};
		}

		internal static IReadOnlyList<ObservacionEnSerie> Observaciones(string json, Frases.Frases frases)
		{
			var observaciones = new List<ObservacionEnSerie>();
			if (string.IsNullOrWhiteSpace(json)) return observaciones;

			using JsonDocument documento = JsonDocument.Parse(json);
			if (!documento.RootElement.TryGetProperty("observacion", out JsonElement arreglo))
				return observaciones;

			foreach (JsonElement elemento in Elementos(arreglo))
				observaciones.Add(new ObservacionEnSerie
				{
					Fecha = Fecha(elemento, "fecha"),
					Expresado = Frases.LaRedaccion.Categoria(frases, Texto(elemento, "resultado")),
					EsEsperable = Booleano(elemento, "esEsperable"),
					EsHallazgo = Booleano(elemento, "esHallazgo"),
				});

			return observaciones;
		}

		internal static LoMasUrgente Urgente(string json, Frases.Frases frases)
		{
			if (string.IsNullOrWhiteSpace(json)) return new LoMasUrgente();

			using JsonDocument documento = JsonDocument.Parse(json);
			JsonElement raiz = documento.RootElement;

			string severidad = Texto(raiz, "severidad");

			return new LoMasUrgente
			{
				Severidad = severidad,
				SeveridadDicha = Frases.LaRedaccion.Severidad(frases, severidad),
				Accion = Frases.LaRedaccion.Accion(frases, Texto(raiz, "accion"), Texto(raiz, "plazo")),
				PideAlgo = Booleano(raiz, "pideAlgo"),
			};
		}

		internal static IReadOnlyList<CeldaEnCuadro> Cuadro(string json, Frases.Frases frases)
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
					ParaMostrar = Frases.LaRedaccion.Nombre(frases, Texto(elemento, "claveDelAnalito")),
					Fecha = Fecha(elemento, "fecha"),
					Expresado = Booleano(elemento, "esNumero")
						? frases.Medida(Decimal(elemento, "valor"), Texto(elemento, "unidad"))
						: Frases.LaRedaccion.Categoria(frases, Texto(elemento, "resultado")),
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

		internal static IReadOnlyList<LineaDeReferencia> Referencias(string json, Frases.Frases frases)
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
					ParaSonia = Frases.LaRedaccion.Linea(frases, Texto(elemento, "clave"),
						Decimal(elemento, "valor"), Texto(elemento, "unidad")),
				});

			return lineas;
		}

		internal static IReadOnlyList<ControlEnEspera> Controles(string json, Frases.Frases frases)
		{
			var controles = new List<ControlEnEspera>();
			if (string.IsNullOrWhiteSpace(json)) return controles;

			using JsonDocument documento = JsonDocument.Parse(json);
			if (!documento.RootElement.TryGetProperty("control", out JsonElement arreglo)) return controles;

			foreach (JsonElement elemento in Elementos(arreglo))
			{
				string severidad = Texto(elemento, "severidad");

				controles.Add(new ControlEnEspera
				{
					Analito = Texto(elemento, "analito"),
					ParaMostrar = Frases.LaRedaccion.Nombre(frases, Texto(elemento, "claveDelAnalito")),
					UltimaMedicion = Fecha(elemento, "ultimaMedicion"),
					DiasDesdeLaUltima = Entero(elemento, "diasDesdeLaUltima"),
					MesesRecomendados = Entero(elemento, "mesesRecomendados"),
					DiasQueFaltan = Entero(elemento, "diasQueFaltan"),
					EstaVencido = Booleano(elemento, "estaVencido"),
					Severidad = severidad,
					SeveridadDicha = Frases.LaRedaccion.Severidad(frases, severidad),
					Accion = Frases.LaRedaccion.Accion(frases, Texto(elemento, "accion"),
						Texto(elemento, "plazo")),
					ParaSonia = Frases.LaRedaccion.Control(frases,
						Texto(elemento, "clave"), Texto(elemento, "claveDelAnalito"),
						Texto(elemento, "claveDeLoQueHace"), Entero(elemento, "anosDesdeLaUltima"),
						Entero(elemento, "diasDesdeLaUltima"), Texto(elemento, "claveDeCadaCuanto"),
						Entero(elemento, "anosRecomendados"), Entero(elemento, "mesesRecomendados")),
				});
			}

			return controles;
		}

		internal static InterpretacionDeAnalito Interpretacion(string json, Frases.Frases frases)
		{
			if (string.IsNullOrWhiteSpace(json)) return new InterpretacionDeAnalito();

			using JsonDocument documento = JsonDocument.Parse(json);
			JsonElement raiz = documento.RootElement;

			string veredicto = Texto(raiz, "veredicto");
			string severidad = Texto(raiz, "severidad");

			return new InterpretacionDeAnalito
			{
				Analito = Texto(raiz, "analito"),
				ParaMostrar = Frases.LaRedaccion.Nombre(frases, Texto(raiz, "claveDelAnalito")),
				Veredicto = veredicto,
				VeredictoDicho = Frases.LaRedaccion.Veredicto(frases, veredicto),
				Severidad = severidad,
				SeveridadDicha = Frases.LaRedaccion.Severidad(frases, severidad),
				Forma = Texto(raiz, "forma"),
				ParaSonia = Frases.LaRedaccion.Lectura(frases,
					Texto(raiz, "clave"), Texto(raiz, "claveDelAnalito"),
					Decimal(raiz, "valorInicial"), Decimal(raiz, "valorFinal"), Texto(raiz, "unidad"),
					Entero(raiz, "dias"), Decimal(raiz, "cambio"),
					Entero(raiz, "cuantas"), Entero(raiz, "cuantasSeguidas"),
					Texto(raiz, "claveDeLaCategoria"),
					Decimal(raiz, "nivelPropio"), Decimal(raiz, "limiteCitado"),
					Decimal(raiz, "ritmoPorAno"), Texto(raiz, "claveDeLaColetilla")),
				Accion = Frases.LaRedaccion.Accion(frases, Texto(raiz, "accion"), Texto(raiz, "plazo")),
				ReferenciaLegible = Frases.LaRedaccion.Referencia(frases,
					Texto(raiz, "claveDeLaReferencia"), Decimal(raiz, "minimoDeLaReferencia"),
					Decimal(raiz, "maximoDeLaReferencia"), Texto(raiz, "unidadDeLaReferencia")),
				MinimoDeLaReferencia = Decimal(raiz, "minimoDeLaReferencia"),
				MaximoDeLaReferencia = Decimal(raiz, "maximoDeLaReferencia"),
				UnidadDeLaReferencia = Texto(raiz, "unidadDeLaReferencia"),
				PideAlgo = Booleano(raiz, "pideAlgo"),
				SugiereRepetirElExamen = Booleano(raiz, "sugiereRepetirElExamen"),
				NoLeCorresponde = Booleano(raiz, "noLeCorresponde"),
				RequiereElSexo = Booleano(raiz, "requiereElSexo"),
			};
		}

		internal static LoQueEstaBien EstaEnRango(string json, Frases.Frases frases)
		{
			if (string.IsNullOrWhiteSpace(json)) return new LoQueEstaBien();

			using JsonDocument documento = JsonDocument.Parse(json);
			JsonElement raiz = documento.RootElement;

			return new LoQueEstaBien
			{
				EnSuRango = Entero(raiz, "enSuRango"),
				Analitos = Entero(raiz, "analitos"),
				HayAlgo = Booleano(raiz, "hayAlgo"),
				ParaSonia = Frases.LaRedaccion.EnRango(frases, Texto(raiz, "clave"),
					Entero(raiz, "enSuRango"), Entero(raiz, "analitos")),
			};
		}

		internal static FaltaElSexoDelPaciente PedidoDeSexo(string json, Frases.Frases frases)
		{
			if (string.IsNullOrWhiteSpace(json)) return new FaltaElSexoDelPaciente();

			using JsonDocument documento = JsonDocument.Parse(json);
			JsonElement raiz = documento.RootElement;

			string severidad = Texto(raiz, "severidad");

			return new FaltaElSexoDelPaciente
			{
				HaceFalta = Booleano(raiz, "haceFalta"),
				SinInterpretar = Entero(raiz, "sinInterpretar"),
				Analitos = Entero(raiz, "analitos"),
				Severidad = severidad,
				SeveridadDicha = Frases.LaRedaccion.Severidad(frases, severidad),
				ParaSonia = Frases.LaRedaccion.PedidoDeSexo(frases, Texto(raiz, "clave"),
					Entero(raiz, "sinInterpretar"), Entero(raiz, "analitos"),
					Codigos(raiz, "porRango"), Codigos(raiz, "porAplicabilidad")),
				PorQue = frases.Diga(Texto(raiz, "porQue")),
			};
		}

		internal static VelocidadDeAnalito Velocidad(string json, Frases.Frases frases)
		{
			if (string.IsNullOrWhiteSpace(json)) return new VelocidadDeAnalito();

			using JsonDocument documento = JsonDocument.Parse(json);
			JsonElement raiz = documento.RootElement;

			return new VelocidadDeAnalito
			{
				Analito = Texto(raiz, "analito"),
				ParaMostrar = Frases.LaRedaccion.Nombre(frases, Texto(raiz, "claveDelAnalito")),
				PorAno = Decimal(raiz, "porAno"),
				Unidad = Texto(raiz, "unidad"),
				Dias = Entero(raiz, "dias"),
				Umbral = Decimal(raiz, "umbral"),
				EsAplicable = Booleano(raiz, "esAplicable"),
				ExigeAtencion = Booleano(raiz, "exigeAtencion"),
				Expresada = Frases.LaRedaccion.Velocidad(frases, Texto(raiz, "clave"),
					Decimal(raiz, "porAno"), Texto(raiz, "unidad")),
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

		// Los codigos que emitio un foreach sobre una lista de strings: la llave del
		// arreglo es el nombre de la variable del foreach y la columna se llama "analito".
		private static IReadOnlyList<string> Codigos(JsonElement raiz, string variable)
		{
			var codigos = new List<string>();
			if (!raiz.TryGetProperty(variable, out JsonElement arreglo)) return codigos;

			foreach (JsonElement elemento in Elementos(arreglo))
			{
				string codigo = Texto(elemento, "analito");
				if (codigo.Length > 0) codigos.Add(codigo);
			}
			return codigos;
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
