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

			assert("cambiar de modulo desaloja al anterior, GUARDA lo que escribio, y recien despues monta al siguiente",
				anfitrion.Secuencia is ["desalojar:examenes", "guardar:examenes", "preparar:compras", "cargar:compras", "consultar:compras"],
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
		}
		finally { SalaShell.Host = null; }
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

		public Task Guardar(string modulo, string ruta, string contenidoBase64, CancellationToken ct)
		{
			secuencia.Add($"guardar:{modulo}");
			guardados.Add($"{modulo}/{ruta}");
			return Task.CompletedTask;
		}

		public Task<string> Pedir(string verbo, string modulo, string a, string b, CancellationToken ct)
		{
			secuencia.Add($"{verbo}:{modulo}");

			switch (verbo)
			{
				case "cargar":
					cargados.Add(modulo);
					MaximoCargadosALaVez = Math.Max(MaximoCargadosALaVez, cargados.Count);
					return Task.FromResult("""{"ok":true,"consultas":["resumen"]}""");

				case "desalojar":
					cargados.Remove(modulo);
					// El desalojo trae lo que el modulo escribio mientras estuvo adentro.
					return Task.FromResult(
						$$"""{"ok":true,"escrituras":[{"ruta":"{{modulo}}/meta.bin","contenido":"AAEC"}]}""");

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
