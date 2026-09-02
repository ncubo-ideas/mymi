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
	// YA NO ES SOLO DE SANGRE, y esa es la correccion. Hasta ahora Analito exigia una
	// UnidadDeMedida -un conjunto cerrado de tres concentraciones masicas- asi que los
	// unicos que podian existir eran los cuantitativos de sangre. El color de la orina es
	// algo que se midio, y no podia ser un analito; la sangre oculta tampoco.
	//
	// O sea que "Analito" era, en realidad, "analito cuantitativo de sangre" con un nombre
	// general puesto encima. La misma clase de cosa que el feedback viene senalando, en un
	// lugar donde no la habiamos buscado.
	//
	// LA UNIDAD BAJA A DONDE VIVE. No es de todo analito: es de la FORMA CONTINUA, igual
	// que en la taxonomia de Magnitud. Un color es un analito sin unidad, del mismo modo
	// que es una magnitud sin unidad.
	//
	// La identidad, los sinonimos y la resolucion por nombre se quedan aca: son de TODO
	// analito, y son la razon de que esta clase exista.
	internal abstract class Analito
	{
		// Los sinonimos NO son adornos ni traducciones libres: son los encabezados con los
		// que estos analitos aparecen impresos en reportes reales.
		// EL UNICO CON REQUISITO ANATOMICO, y por eso el requisito entra por constructor:
		// no es "si es hombre", es SI TIENE PROSTATA. Ver RequisitoAnatomico para por que
		// el predicado correcto no es el sexo.
		internal static readonly AnalitoContinuo AntigenoProstatico = new AnalitoContinuo(
			"Antigeno prostatico",
			"Antígeno prostático",
			UnidadDeMedida.NanogramosPorMililitro,
			0m, 5000m,
			RequisitoAnatomico.Prostata,
			"PSA", "APE", "antigeno prostatico especifico");

		internal static readonly AnalitoContinuo ColesterolTotal = new AnalitoContinuo(
			"Colesterol total",
			"Colesterol total",
			UnidadDeMedida.MiligramosPorDecilitro,
			20m, 1200m,
			"colesterol", "CHOL", "colesterol serico");

		internal static readonly AnalitoContinuo ColesterolLdl = new AnalitoContinuo(
			"Colesterol LDL",
			"Colesterol LDL",
			UnidadDeMedida.MiligramosPorDecilitro,
			10m, 900m,
			"LDL", "LDL-C", "lipoproteina de baja densidad");

		internal static readonly AnalitoContinuo ColesterolHdl = new AnalitoContinuo(
			"Colesterol HDL",
			"Colesterol HDL",
			UnidadDeMedida.MiligramosPorDecilitro,
			5m, 200m,
			"HDL", "HDL-C", "lipoproteina de alta densidad");

		internal static readonly AnalitoContinuo Hemoglobina = new AnalitoContinuo(
			"Hemoglobina",
			"Hemoglobina",
			UnidadDeMedida.GramosPorDecilitro,
			2m, 25m,
			"HGB", "HB");

		// ---------------------------------------------------------- los que faltaban
		//
		// El urianalisis informa TRES cosas medidas y el de heces DOS. Hasta ahora ninguna
		// era un analito, asi que las tres colapsaban en una fila del historial y en un
		// string: "ambar, nitritos negativo, sangre negativo". Es la misma aplanadora que
		// sacamos del dominio, sobreviviendo en la identidad.
		//
		// Los nombres NO chocan con las clases de magnitud -ColorDeOrina, Nitritos,
		// SangreEnOrina- a proposito: aquellas son el RESULTADO, estas son QUE se midio.

		internal static readonly AnalitoNominal ColorDeLaOrina = new AnalitoNominal(
			"Color de la orina", "Color de la orina", "color de orina", "aspecto de la orina");

		internal static readonly AnalitoBinario NitritosEnOrina = new AnalitoBinario(
			"Nitritos", "Nitritos", "nitrito", "nitritos en orina");

		internal static readonly AnalitoBinario SangreEnLaOrina = new AnalitoBinario(
			"Sangre en orina", "Sangre en orina", "hematuria", "sangre oculta en orina");

		internal static readonly AnalitoNominal ColorDeLasHeces = new AnalitoNominal(
			"Color de las heces", "Color de las heces", "color de heces", "consistencia");

		internal static readonly AnalitoBinario SangreEnLasHeces = new AnalitoBinario(
			"Sangre en heces", "Sangre en heces", "sangre oculta en heces");

		// SI ESTE ANALITO FORMA SERIE. Es la fila `EvaluarTendencia` de la tabla del brief
		// §6.5: todas las formas menos la nominal.
		//
		// Un color NO la forma, y no es una limitacion pendiente: preguntar si el color de
		// la orina "viene subiendo" no es una pregunta. Un binario SI, y esa es la
		// correccion de este cambio — "negativo, negativo, positivo" es una serie legitima,
		// y hasta ahora este dominio no la podia tener.
		internal abstract bool FormaSerie { get; }

		// QUE TIENE QUE TENER LA PERSONA para que este analito signifique algo. Ninguno
		// para casi todos; el antigeno prostatico exige prostata.
		//
		// VIVE EN LA IDENTIDAD y no en el examen, y esa es la decision que hace que la
		// correccion no sea un filtro de pantalla: la aplicabilidad es una propiedad de QUE
		// SE MIDIO, independiente de que exista una medicion. Por eso la tool MCP puede
		// contestar "este analito no te corresponde" de un antigeno del que ni siquiera hay
		// serie, y por eso ninguna superficie puede olvidarse de preguntarlo.
		internal RequisitoAnatomico Requisito { get; private set; }

		// SI ESTE ANALITO EXISTE PARA ESTA PERSONA. Tres respuestas y no dos: ver
		// Aplicabilidad.
		//
		// El analito NO PREGUNTA POR EL SEXO —pregunta por su requisito, y el requisito
		// pregunta por la anatomia—. Es lo que deja el modelo abierto a que la anatomia
		// deje de derivarse del sexo sin tocar ningun analito.
		internal Aplicabilidad AplicaA(Paciente paciente)
		{
			ArgumentNullException.ThrowIfNull(paciente);
			return Requisito.En(paciente);
		}

		private static readonly Analito[] todos =
		{
			AntigenoProstatico, ColesterolTotal, ColesterolLdl, ColesterolHdl, Hemoglobina,
			ColorDeLaOrina, NitritosEnOrina, SangreEnLaOrina,
			ColorDeLasHeces, SangreEnLasHeces,
		};

		// Indice de TODOS los nombres -el canonico y los sinonimos- ya normalizados. Se
		// arma una vez.
		private static readonly Dictionary<string, Analito> porNombre = Indexar();

		internal string Nombre { get; }

		internal string ParaMostrar { get; }

		private readonly string[] sinonimos;

		protected Analito(string nombre, string paraMostrar, RequisitoAnatomico requisito,
			string[] sinonimos)
		{
			ArgumentNullException.ThrowIfNull(requisito);

			Nombre = nombre;
			ParaMostrar = paraMostrar;
			Requisito = requisito;
			this.sinonimos = sinonimos ?? new string[0];
		}

		internal static IReadOnlyList<Analito> Todos => todos;

		// Los otros nombres con los que este analito llega, para que la superficie pueda
		// explicarle a alguien por que dos hojas distintas terminaron en la misma serie.
		internal IReadOnlyList<string> Sinonimos => sinonimos;

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
		internal static Analito PorNombre(string nombre)
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
		internal static bool EsConocido(string nombre)
		{
			if (string.IsNullOrWhiteSpace(nombre)) return false;
			return porNombre.ContainsKey(Normalizar(nombre));
		}

		// Si ESTE analito atiende por ese nombre. Es la pregunta desde el otro lado y
		// evita que quien la haga tenga que resolver primero y comparar despues.
		internal bool RespondeA(string nombre)
		{
			if (string.IsNullOrWhiteSpace(nombre)) return false;
			return porNombre.TryGetValue(Normalizar(nombre), out Analito hallado)
				&& ReferenceEquals(hallado, this);
		}

		// Operacion con nombre en vez de sobrecargar ==, igual que UnidadDeMedida. La
		// pregunta del dominio es "son el mismo analito", o sea "forman serie".
		internal bool EsElMismo(Analito otro)
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
