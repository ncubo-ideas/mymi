using System;
using System.Collections.Generic;
using System.Text;

namespace Examenes.Dominio
{
	// QUE se midio. Es una identidad, no una cadena.
	//
	// POR QUE DEJO DE SER UN STRING. Un expediente real no viene de un laboratorio: viene
	// de todos los laboratorios por los que paso la persona, y cada uno nombra distinto lo
	// mismo. Medido sobre tres reportes reales del mismo paciente, tres instituciones y
	// cuatro anos:
	//
	//     G. BLANCOS   (2022)  |  WBC   (2026-06)  |  LEUCOCITOS TOTALES  (2026-07)
	//     HEMOGLOBINA  (2022)  |  HGB   (2026-06)  |  HEMOGLOBINA         (2026-07)
	//     ACETONA      (2022)  |   -               |  CETONAS             (2026-07)
	//
	// Con el analito como cadena y un diccionario por nombre, esos cuatro anos no forman
	// UNA serie de tres puntos: forman TRES series de un punto, y la app contesta
	// "indeterminada" en las tres. O sea que la capacidad que justifica todo el dominio
	// -leer la serie- se pierde exactamente en el caso para el que existe.
	//
	// La identidad arregla eso sin pedirle nada al emisor: el analito responde a varios
	// nombres y sigue siendo uno solo.
	//
	// Conjunto CERRADO, comparado por referencia, igual que UnidadDeMedida. Un nombre que
	// no resuelve no es un analito nuevo: es un evento que este dominio no modela.
	//
	// DOS NOMBRES, y la distincion importa:
	//
	//   Nombre       la llave canonica. Es la que ya viaja en las proyecciones y la que
	//                usa el enum del MCP. NO se toca: cambiarla obliga a mover cuatro
	//                capas a la vez y no compra nada.
	//   ParaMostrar  el nombre con acentos, que es el que ve la persona. Vive aca y no en
	//                la pantalla porque es dominio: "Antigeno prostatico" sin tilde no es
	//                una decision de presentacion, es un nombre mal escrito.
	//
	// La UNIDAD tambien vive aca. Antes cada subclase de ExamenDeSangre repetia su
	// constante al llamar a Medida(); ahora la dice el analito, que es de quien es. Es
	// §5.2 del brief: la unidad gobierna la conversion, el analito gobierna el significado.
	public sealed class Analito
	{
		// Los sinonimos NO son adornos ni traducciones libres: son los encabezados con los
		// que estos analitos aparecen impresos en reportes reales.
		public static readonly Analito AntigenoProstatico = new Analito(
			"Antigeno prostatico",
			"Antígeno prostático",
			UnidadDeMedida.NanogramosPorMililitro,
			0m, 5000m,
			"PSA", "APE", "antigeno prostatico especifico");

		public static readonly Analito ColesterolTotal = new Analito(
			"Colesterol total",
			"Colesterol total",
			UnidadDeMedida.MiligramosPorDecilitro,
			20m, 1200m,
			"colesterol", "CHOL", "colesterol serico");

		public static readonly Analito ColesterolLdl = new Analito(
			"Colesterol LDL",
			"Colesterol LDL",
			UnidadDeMedida.MiligramosPorDecilitro,
			10m, 900m,
			"LDL", "LDL-C", "lipoproteina de baja densidad");

		public static readonly Analito ColesterolHdl = new Analito(
			"Colesterol HDL",
			"Colesterol HDL",
			UnidadDeMedida.MiligramosPorDecilitro,
			5m, 200m,
			"HDL", "HDL-C", "lipoproteina de alta densidad");

		public static readonly Analito Hemoglobina = new Analito(
			"Hemoglobina",
			"Hemoglobina",
			UnidadDeMedida.GramosPorDecilitro,
			2m, 25m,
			"HGB", "HB");

		private static readonly Analito[] todos =
		{
			AntigenoProstatico, ColesterolTotal, ColesterolLdl, ColesterolHdl, Hemoglobina,
		};

		// Indice de TODOS los nombres -el canonico y los sinonimos- ya normalizados. Se
		// arma una vez.
		private static readonly Dictionary<string, Analito> porNombre = Indexar();

		public string Nombre { get; }

		public string ParaMostrar { get; }

		public UnidadDeMedida Unidad { get; }

		// EL RANGO DE LO FISIOLOGICAMENTE POSIBLE, que NO es el rango de referencia.
		//
		// El de referencia dice que es NORMAL; este dice que es POSIBLE en una persona
		// viva. Entre los dos hay lugar para valores altos y reales -una hemoglobina de 9
		// es anemia, no un error- y afuera de este ya no hay hallazgo que interpretar: hay
		// un dato malformado.
		//
		// Alvaro: "aqui me estan mandando una vaina de un cadaver". Una hemoglobina de 40
		// no es una hemoglobina alta, es un numero que nadie midio en un paciente.
		//
		// Vive en el ANALITO y no en el examen porque hace falta ANTES de construirlo: la
		// ruta necesita poder descartar el evento sin que reviente el comando.
		//
		// ⚠️ Los limites son deliberadamente ANCHOS y orientativos. La guarda existe para
		// atajar basura, no para opinar de clinica: apretarlos de mas descartaria hallazgos
		// reales, que es peor que dejar pasar uno raro. Antes de mostrarse fuera del equipo
		// conviene que alguien de laboratorio los revise.
		public decimal ValorMinimoPosible { get; }

		public decimal ValorMaximoPosible { get; }

		public bool EsValorPosible(decimal valor) =>
			valor >= ValorMinimoPosible && valor <= ValorMaximoPosible;

		private readonly string[] sinonimos;

		private Analito(string nombre, string paraMostrar, UnidadDeMedida unidad,
			decimal minimoPosible, decimal maximoPosible, params string[] sinonimos)
		{
			Nombre = nombre;
			ParaMostrar = paraMostrar;
			Unidad = unidad;
			ValorMinimoPosible = minimoPosible;
			ValorMaximoPosible = maximoPosible;
			this.sinonimos = sinonimos ?? new string[0];
		}

		public static IReadOnlyList<Analito> Todos => todos;

		// Los otros nombres con los que este analito llega, para que la superficie pueda
		// explicarle a alguien por que dos hojas distintas terminaron en la misma serie.
		public IReadOnlyList<string> Sinonimos => sinonimos;

		// El armado del indice es tambien donde se hace cumplir la invariante que hace
		// segura la tabla de sinonimos: NINGUN nombre puede resolver a dos analitos. Sin
		// esto, agregar manana un sinonimo que ya pertenece a otro se lo roba en silencio
		// y una serie entera se muda de analito sin que nada falle.
		//
		// Revienta al inicializar el tipo, que es lo mas temprano y ruidoso posible: la
		// primera comprobacion de consola lo descubre.
		private static Dictionary<string, Analito> Indexar()
		{
			var indice = new Dictionary<string, Analito>(StringComparer.Ordinal);

			foreach (Analito analito in todos)
			{
				Registrar(indice, analito.Nombre, analito);
				Registrar(indice, analito.ParaMostrar, analito);
				foreach (string sinonimo in analito.sinonimos)
					Registrar(indice, sinonimo, analito);
			}

			return indice;
		}

		private static void Registrar(Dictionary<string, Analito> indice, string nombre,
			Analito analito)
		{
			string llave = Normalizar(nombre);

			// El canonico y el ParaMostrar de un analito sin acentos son el mismo texto
			// normalizado. Que un analito se registre dos veces con la misma llave no es
			// un choque; que se registren DOS analitos distintos, si.
			if (indice.TryGetValue(llave, out Analito ya))
			{
				if (ReferenceEquals(ya, analito)) return;

				throw new InvalidOperationException(
					$"'{nombre}' resuelve a '{ya.Nombre}' y a '{analito.Nombre}' a la vez. "
					+ "Dos analitos no pueden responder al mismo nombre.");
			}

			indice[llave] = analito;
		}

		// Funcion total con excepcion, igual que UnidadDeMedida.PorSimbolo: el nombre
		// llega de afuera -de una consulta de la IA, de la pantalla, de un evento- y solo
		// hay cinco analitos validos. Que no resuelva ninguno es un defecto de quien
		// pregunta, no un analito nuevo que haya que inventar.
		public static Analito PorNombre(string nombre)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(nombre);

			if (porNombre.TryGetValue(Normalizar(nombre), out Analito analito)) return analito;

			throw new ArgumentException(
				$"'{nombre}' no es un analito que este expediente conozca. Los que conoce son: "
				+ NombresConocidos() + ".", nameof(nombre));
		}

		// Para quien tiene que DECIDIR en vez de fallar. La ruta de eventos descarta lo que
		// este dominio no modela devolviendo null, y descartar no es un error: un
		// laboratorio manda el perfil completo y esta app modela cinco analitos.
		public static bool EsConocido(string nombre)
		{
			if (string.IsNullOrWhiteSpace(nombre)) return false;
			return porNombre.ContainsKey(Normalizar(nombre));
		}

		// Si ESTE analito atiende por ese nombre. Es la pregunta desde el otro lado y
		// evita que quien la haga tenga que resolver primero y comparar despues.
		public bool RespondeA(string nombre)
		{
			if (string.IsNullOrWhiteSpace(nombre)) return false;
			return porNombre.TryGetValue(Normalizar(nombre), out Analito hallado)
				&& ReferenceEquals(hallado, this);
		}

		// Operacion con nombre en vez de sobrecargar ==, igual que UnidadDeMedida. La
		// pregunta del dominio es "son el mismo analito", o sea "forman serie".
		public bool EsElMismo(Analito otro)
		{
			ArgumentNullException.ThrowIfNull(otro);
			return ReferenceEquals(this, otro);
		}

		public override string ToString() => Nombre;

		private static string NombresConocidos()
		{
			var texto = new StringBuilder();
			for (int i = 0; i < todos.Length; i++)
			{
				if (i > 0) texto.Append(", ");
				texto.Append(todos[i].Nombre);
			}
			return texto.ToString();
		}

		// El plegado vive en NombreNormalizado: la misma operacion hace falta tambien en
		// los colores, y es mecanica, no significado. Que nombres acepta este conjunto y
		// que significan sigue siendo de aca.
		private static string Normalizar(string nombre) => NombreNormalizado.De(nombre);
	}
}
