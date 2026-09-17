using System.Text.Json;
using Gnothi.Host;

namespace Gnothi.Tests;

// LA SALA, Y LAS DOS COSAS QUE NO PUEDE PERDER.
//
// La sala corre a TODOS los modulos en un solo documento del motor web. Eso le da lo que la
// forma hospedada no tiene -- un sustrato compartido -- y le quita la muleta que hacia gratis la
// separacion: alli hay un puente por WebView, atado a un modulo, y el llamante no tiene donde
// escribir una mentira. Aca hay un puente y varios modulos.
//
// Entonces la separacion pasa a ser un INVARIANTE que sostiene esta clase, y son dos:
//
//   1 · UNA LLAMADA EN VUELO, y la identidad del modulo sale del MARCO de la llamada que el Host
//       abrio, nunca de lo que venga en el mensaje. Si un argumento pudiera elegir de quien se
//       habla, el Host veria una pregunta legitima de un modulo legitimo -- que es exactamente
//       lo que el comentario de `WebModule.Serve` prohibe del otro lado.
//
//   2 · UN MODULO CARGADO POR VEZ, y el anterior se DESALOJA ANTES de que el siguiente monte su
//       carpeta. Medido en un A54: con dos carpetas montadas a la vez, cualquier codigo adentro
//       del sandbox lee la del vecino; desalojando primero, no queda nada que leer.
//
// POR QUE SE PUEDE PROBAR ACA, sin telefono y sin motor web: `ISalaHost` es la unica costura con
// la plataforma, y todo lo que decide vive de este lado. El doble de abajo anota la SECUENCIA de
// verbos, que es justamente donde viven los dos invariantes.
internal static class Sala
{
	public static void Run(Action<string, bool, string> assert)
	{
		var anfitrion = new SalaDePrueba();
		SalaShell.Host = anfitrion;

		try
		{
			var examenes = new SalaShell("examenes", Instalacion("examenes"), "/journals/examenes");
			var compras = new SalaShell("compras", Instalacion("compras"), "/journals/compras");

			// ── 1 · el primero se prepara y se carga antes de contestar ─────────────────────
			examenes.Invoke("resumen", null, Accepts.JsonOnly, CancellationToken.None).GetAwaiter().GetResult();

			assert("el primer modulo se prepara y se carga antes de contestar",
				anfitrion.Secuencia is ["preparar:examenes", "cargar:examenes", "consultar:examenes"],
				anfitrion.Como());

			// ── 2 · el mismo modulo otra vez NO se recarga ──────────────────────────────────
			anfitrion.Olvidar();
			examenes.Invoke("lista_de_examenes", null, Accepts.JsonOnly, CancellationToken.None).GetAwaiter().GetResult();

			assert("preguntarle otra vez al mismo modulo no lo vuelve a cargar",
				anfitrion.Secuencia is ["consultar:examenes"],
				anfitrion.Como());

			// ── 3 · EL INVARIANTE: desalojar al anterior ANTES de montar al siguiente ───────
			anfitrion.Olvidar();
			compras.Invoke("resumen", null, Accepts.JsonOnly, CancellationToken.None).GetAwaiter().GetResult();

			assert("cambiar de modulo desaloja, GUARDA lo que escribio, lo SUELTA, y recien despues monta al siguiente",
				anfitrion.Secuencia is ["desalojar:examenes", "guardar:examenes", "soltar:examenes", "preparar:compras", "cargar:compras", "consultar:compras"],
				anfitrion.Como());

			// SOLTAR ANTES DE PREPARAR, Y NO ES UN DETALLE DE ORDEN. El anfitrion sirve los bytes
			// de UN modulo por el origen de la sala; si el anterior siguiera servible, un dominio
			// adentro podria pedir su journal por HTTP y saltarse el desmontaje entero. Entre un
			// modulo y el siguiente no hay ninguno servible.
			assert("y suelta al anterior ANTES de preparar al siguiente",
				Donde(anfitrion.Secuencia, "soltar:examenes") < Donde(anfitrion.Secuencia, "preparar:compras"),
				anfitrion.Como());

			assert("y lo guardado es del modulo que se fue, no del que entra",
				anfitrion.Guardados is ["examenes/examenes/meta.bin"],
				string.Join(", ", anfitrion.Guardados));

			assert("y nunca hay dos modulos cargados a la vez",
				anfitrion.MaximoCargadosALaVez == 1,
				$"llego a tener {anfitrion.MaximoCargadosALaVez} cargados");

			// ── 4 · EL INVARIANTE: el llamante no elige de quien se habla ───────────────────
			//
			// Esta es la que se rompe sin que nadie lo note. Un argumento llamado `modulo` o
			// `module` compila, queda verde, y recien se ve el dia que un modulo lee el journal
			// del vecino.
			anfitrion.Olvidar();
			var mentira = new Dictionary<string, object?>
			{
				["modulo"] = "examenes",
				["module"] = "examenes",
				["grantedFolder"] = "/journals/examenes",
			};
			compras.Invoke("resumen", mentira, Accepts.JsonOnly, CancellationToken.None).GetAwaiter().GetResult();

			assert("un argumento que nombra otro modulo NO cambia de quien se habla",
				anfitrion.Secuencia is ["consultar:compras"],
				anfitrion.Como());

			assert("y ese argumento igual llega al modulo, que es su dueño",
				anfitrion.UltimosArgumentos.Contains("\"modulo\"", StringComparison.Ordinal),
				anfitrion.UltimosArgumentos);

			// ── 5 · las tres puertas van por su propio verbo ────────────────────────────────
			anfitrion.Olvidar();
			compras.View("serie", null, Accepts.JsonOnly, CancellationToken.None).GetAwaiter().GetResult();
			compras.Deliver("{}", CancellationToken.None).GetAwaiter().GetResult();
			compras.Cursor(CancellationToken.None).GetAwaiter().GetResult();

			assert("la pantalla, la entrega y el cursor no entran por la puerta del asistente",
				anfitrion.Secuencia is ["vista:compras", "entregar:compras", "guardar:compras", "cursor:compras"],
				anfitrion.Como());

			// EL REFRESCO TIENE QUE QUEDAR, y esta es la afirmacion que lo dice: entregar un sobre
			// no termina cuando el modulo lo aplica en memoria, termina cuando lo que escribio
			// aterrizo en la carpeta de la persona. Sin esto el refresco se veia aplicado y
			// desaparecia al cambiar de modulo.
			assert("lo que el modulo asienta al entregar un sobre se guarda en el acto",
				anfitrion.Guardados is [_, "compras/compras/journal/journal_000001.bin"],
				string.Join(", ", anfitrion.Guardados));

			// ── 6 · soltar la cascara NO tira el documento ──────────────────────────────────
			//
			// Es toda la ganancia de la sala: el Dispatcher crea y suelta una cascara por
			// consulta, y si eso tirara el sustrato se pagaria el arranque cada vez. Medido: 3
			// segundos contra 14 milisegundos.
			anfitrion.Olvidar();
			compras.DisposeAsync().AsTask().GetAwaiter().GetResult();

			assert("soltar la cascara no desaloja ni tira el sustrato",
				anfitrion.Secuencia.Count == 0,
				anfitrion.Como());

			// ── 7 · FALLAR CERRADO: si el journal no quedo vaciado, NO se carga otro encima ──
			//
			// Este es el modo de falla que estaba vivo y que nadie veia. La frontera de la sala ES
			// el vaciado; un desalojo que no vacia la deja abierta. Antes el Host seguia adelante
			// y montaba al siguiente igual. Ahora la consulta falla, que es lo correcto para una
			// propiedad de contencion: contestar bien con la frontera rota es peor que no
			// contestar.
			anfitrion.Olvidar();
			anfitrion.NoVacia = true;

			bool fallo = false;
			try { examenes.Invoke("resumen", null, Accepts.JsonOnly, CancellationToken.None).GetAwaiter().GetResult(); }
			catch (InvalidOperationException) { fallo = true; }

			assert("un desalojo que no vacio el journal FALLA la consulta en vez de seguir",
				fallo, anfitrion.Como());

			assert("y no llega a preparar ni a cargar al siguiente",
				!anfitrion.Secuencia.Any(v => v.StartsWith("preparar:", StringComparison.Ordinal)
					|| v.StartsWith("cargar:", StringComparison.Ordinal)),
				anfitrion.Como());

			// ── 8 · EL MODULO QUE NO VUELVE DE SU PROPIO DESALOJO ───────────────────────────
			//
			// El 7 cubre el desalojo que TIRA. Este cubre el que NO VUELVE, que es otro modo de
			// falla y no se arregla igual: adentro del desalojo corre el Dispose() del modulo --
			// codigo que el modulo eligio -- y un ciclo infinito ahi no se interrumpe desde
			// adentro, porque el motor web es de un solo hilo. Sin tope, ese modulo se queda con
			// el semaforo tomado y NINGUN modulo vuelve a contestar hasta que maten la app: un
			// modulo cualquiera apagando el Host entero.
			//
			// La salida es tirar el documento. Y no es solo recuperarse: la memoria del modulo
			// vive adentro del documento, asi que tirarlo CIERRA la frontera en vez de dejarla
			// dependiendo de que el modulo cooperara.
			anfitrion.Olvidar();
			anfitrion.NoVacia = false;
			anfitrion.SeTraba = true;

			TimeSpan tope = SalaShell.Tope;
			SalaShell.Tope = TimeSpan.FromMilliseconds(200);

			bool trabo = false;
			try { examenes.Invoke("resumen", null, Accepts.JsonOnly, CancellationToken.None).GetAwaiter().GetResult(); }
			catch (InvalidOperationException) { trabo = true; }

			assert("un modulo que no vuelve de su desalojo no cuelga la sala: la consulta falla",
				trabo, anfitrion.Como());

			assert("y se tira el documento, nombrando al modulo que no contesto",
				anfitrion.Descartado.Contains("compras", StringComparison.Ordinal),
				anfitrion.Descartado.Length == 0 ? "(no se descarto nada)" : anfitrion.Descartado);

			assert("y no se monta al siguiente encima del que quedo trabado",
				!anfitrion.Secuencia.Any(v => v.StartsWith("preparar:", StringComparison.Ordinal)
					|| v.StartsWith("cargar:", StringComparison.Ordinal)),
				anfitrion.Como());

			// LA AFIRMACION QUE HACE QUE ESTO SEA UNA RECUPERACION Y NO UN ERROR MAS PROLIJO: la
			// consulta siguiente tiene que andar, en una sala nueva. Si se quedara trabada, el
			// tope no habria servido de nada.
			anfitrion.Olvidar();
			anfitrion.SeTraba = false;
			examenes.Invoke("resumen", null, Accepts.JsonOnly, CancellationToken.None).GetAwaiter().GetResult();

			assert("y la consulta siguiente contesta, en una sala nueva y sin rastro del anterior",
				anfitrion.Secuencia is ["preparar:examenes", "cargar:examenes", "consultar:examenes"],
				anfitrion.Como());

			SalaShell.Tope = tope;

			// ── 9 · LO QUE QUEDA DE LA CARGA ANTERIOR NO ES UNA LINEA DE LOG ────────────────
			//
			// El sustrato reporta al cargar cuanto quedo de las cargas anteriores, porque el
			// desmontaje VACIA en vez de borrar -- borrar mata el runtime desde un finalizador.
			// Eso se escribia en el log y el modulo entraba igual.
			//
			// Cero bytes ES la frontera de la sala. Una frontera que no se chequea es una
			// expectativa, asi que ahora la carga falla.
			anfitrion.Olvidar();
			anfitrion.BytesDeAntes = 48;

			bool quedoAlgo = false;
			try { compras.Invoke("resumen", null, Accepts.JsonOnly, CancellationToken.None).GetAwaiter().GetResult(); }
			catch (InvalidOperationException) { quedoAlgo = true; }

			assert("si quedaron bytes de una carga anterior, NO se carga el siguiente encima",
				quedoAlgo, anfitrion.Como());

			// Y LOS ARCHIVOS SI PUEDEN QUEDAR: son carpetas de cero bytes, con nombres neutros
			// (`c1`, `c2`) desde que la carpeta dejo de llamarse como el modulo. Dicen cuantas
			// cargas hubo, no de quien. Ese es el residuo conocido, y no tiene que fallar nada.
			anfitrion.Olvidar();
			anfitrion.BytesDeAntes = 0;
			compras.Invoke("resumen", null, Accepts.JsonOnly, CancellationToken.None).GetAwaiter().GetResult();

			assert("pero carpetas vacias de cargas anteriores no impiden cargar",
				anfitrion.Secuencia is ["preparar:compras", "cargar:compras", "consultar:compras"],
				anfitrion.Como());
		}
		finally { SalaShell.Host = null; }
	}

	// En que lugar de la secuencia aparecio un verbo. -1 si no aparecio, que en una comparacion de
	// orden ya dice que algo falta.
	private static int Donde(IReadOnlyList<string> secuencia, string verbo)
	{
		for (int i = 0; i < secuencia.Count; i++)
			if (string.Equals(secuencia[i], verbo, StringComparison.Ordinal)) return i;

		return -1;
	}

	private static Shell Instalacion(string modulo) => new()
	{
		Type = "sala",
		Command = "dotnet",
		Arguments = [$"/Modulos/{modulo}/web", "", $"/Modulos/{modulo}/dominio/{modulo}.App.dll"],
	};

	// EL DOBLE DE LA SALA. No hay motor web ni WebView: lo que se prueba es la SECUENCIA de
	// verbos, que es donde viven los dos invariantes.
	private sealed class SalaDePrueba : ISalaHost
	{
		private readonly List<string> secuencia = [];
		private readonly HashSet<string> cargados = [];

		public IReadOnlyList<string> Secuencia => secuencia;

		public int MaximoCargadosALaVez { get; private set; }

		public string UltimosArgumentos { get; private set; } = "";

		public void Olvidar() => secuencia.Clear();

		public string Como() => secuencia.Count == 0 ? "(no pidio nada)" : string.Join(" -> ", secuencia);

		public IReadOnlyList<string> Guardados => guardados;

		private readonly List<string> guardados = [];

		public Task Preparar(string modulo, string entrada, string grantedFolder, CancellationToken ct)
		{
			secuencia.Add($"preparar:{modulo}");
			return Task.CompletedTask;
		}

		public bool NoVacia { get; set; }

		// EL MODULO QUE NO VUELVE. El doble no simula un ciclo infinito con un ciclo infinito:
		// devuelve una tarea que nunca se completa, que es lo que la cascara ve de este lado.
		public bool SeTraba { get; set; }

		public string Descartado { get; private set; } = "";

		// CUANTO PESA LO QUE QUEDO DE LAS CARGAS ANTERIORES. Cero es lo normal: carpetas vaciadas.
		// Mas que cero es la frontera rota.
		public long BytesDeAntes { get; set; }

		public Task Descartar(string motivo, CancellationToken ct)
		{
			secuencia.Add("descartar");
			Descartado = motivo;

			// Tirar el documento se lleva lo que hubiera cargado adentro: no queda nada montado
			// que el siguiente pueda leer, que es justamente por lo que se tira.
			cargados.Clear();
			return Task.CompletedTask;
		}

		public Task Soltar(string modulo, CancellationToken ct)
		{
			secuencia.Add($"soltar:{modulo}");
			return Task.CompletedTask;
		}

		public Task Guardar(string modulo, string ruta, string contenidoBase64, CancellationToken ct)
		{
			secuencia.Add($"guardar:{modulo}");
			guardados.Add($"{modulo}/{ruta}");
			return Task.CompletedTask;
		}

		public Task<string> Pedir(string verbo, string modulo, string a, string b, CancellationToken ct)
		{
			secuencia.Add($"{verbo}:{modulo}");

			// Una tarea que no se completa nunca: para la cascara es indistinguible de un
			// Dispose() que se colgo adentro del motor web, que es lo que se quiere probar.
			if (SeTraba && verbo == "desalojar") return new TaskCompletionSource<string>().Task;

			switch (verbo)
			{
				case "cargar":
					cargados.Add(modulo);
					MaximoCargadosALaVez = Math.Max(MaximoCargadosALaVez, cargados.Count);

					// `vecinos` es lo que quedo de las cargas anteriores. Los nombres son neutros
					// -- `c1`, `c2` -- porque la carpeta ya no se llama como el modulo, asi que
					// lo unico que se puede afirmar es el peso.
					return Task.FromResult(
						$$"""{"ok":true,"consultas":["resumen"],"vecinos":[{"carga":"c1","archivos":3,"bytes":{{BytesDeAntes}}}]}""");

				case "desalojar":
					cargados.Remove(modulo);

					// LA SALA QUE NO PUDO VACIAR. Es el caso que antes pasaba en silencio: el Host
					// seguia adelante y montaba al siguiente encima de lo que quedo.
					if (NoVacia)
						return Task.FromResult("""{"ok":true,"vaciada":false,"desmontada":"QUEDARON 2 archivos sin vaciar de 3"}""");

					// El desalojo trae lo que el modulo escribio mientras estuvo adentro.
					return Task.FromResult(
						$$"""{"ok":true,"vaciada":true,"escrituras":[{"ruta":"{{modulo}}/meta.bin","contenido":"AAEC"}]}""");

				case "consultar":
				case "vista":
					UltimosArgumentos = b;
					return Task.FromResult("""{"ok":true,"contenido":{"hayDatos":true}}""");

				case "entregar":
					// Con una escritura adentro: es lo que el modulo asento al aplicar el sobre.
					return Task.FromResult(
						"""{"ok":true,"contenido":"","escrituras":[{"ruta":"compras/journal/journal_000001.bin","contenido":"AAEC"}]}""");

				case "cursor":
					return Task.FromResult("""{"ok":true,"contenido":0}""");

				default:
					return Task.FromResult("""{"ok":true,"contenido":null}""");
			}
		}
	}
}
