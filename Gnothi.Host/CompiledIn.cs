using System.Collections.Concurrent;

namespace Gnothi.Host;

// ⚠ CONGELADA. ESCRITA, NO ENGANCHADA, Y A PROPOSITO.
//
// Module.ChooseShell NO la nombra, asi que hoy este archivo no corre nunca. Se deja porque el
// diseño esta pensado y vale, pero engancharla ahora seria construir sobre una apuesta.
//
// POR QUE. Existia para un solo escenario: si en iOS hubiera que ir por NativeAOT -- que
// prohibe cargar ensamblados en runtime -- para poder tener App Intents nativos. Y ese
// escenario dejo de ser el unico camino: el Host y la caratula de Swift ya se pasan los datos
// por una FOTO en un contenedor compartido (Gnothi.Movil/Instantanea.cs), sin NativeAOT, sin
// puente Swift-.NET y sin perder la carga dinamica de modulos.
//
// SE DESCONGELA SI, y solo si, pasa alguna de estas dos:
//
//   1. La sonda de Gnothi.iOS dice que el interprete de Mono NO esta puesto. Ahi
//      InProcessShell no puede existir en iOS y esta pasa a ser obligatoria.
//
//      MEDIDO el 2026-09-01 en un iPhone 13 Pro (Renan): con MtouchInterpreter=-all en las
//      dos configuraciones, los cinco pasos pasan y las banderas dan Supported=True /
//      Compiled=False -- el interprete trabajando bajo el AOT obligatorio de Apple. Sin la
//      bandera en Debug moria con SIGABRT en load_aot_module. LA CONDICION 1 NO SE DIO:
//      InProcessShell existe en iOS, y esta cascara sigue congelada.
//   2. Se decide meter el Host DENTRO de la app de Swift como framework. Ahi el unico camino
//      documentado es NativeAOT, y con el la reflexion no sobrevive.
//
// El cuadro completo, con las fuentes: NotasGnothiHost/33.
//
// ────────────────────────────────────────────────────────────────────────────────────────
//
// LA TERCERA CASCARA: el modulo YA VIENE COMPILADO ADENTRO DE LA APP.
//
// DE DONDE SALE. En iOS los App Intents solo existen en Swift, asi que la unica forma de tener
// Puerta 3 nativa es que la app principal sea Swift y el Host entre como framework -- y el
// unico camino documentado para eso es NativeAOT, que dice textual «it isn't possible to load
// and execute assemblies at runtime». O sea: InProcessShell no puede existir ahi.
// NotasGnothiHost/33 tiene el cuadro completo.
//
// LO QUE ESTA CASCARA NO HACE, y es todo el punto: NO DESCUBRE NADA.
//
// InProcessShell busca la clase marcada con GetTypes(), el metodo con GetMethods(), arma el
// objeto con GetConstructors() y proyecta el resultado con GetProperties(). Son los 9 warnings
// que quedan en Lab.Aot, y no se arreglan anotando: los tipos son desconocidos al compilar,
// que es su razon de ser. Aca no hay nada que descubrir porque el modulo SE REGISTRA.
//
// QUIEN REGISTRA. La app que lo embarca, al arrancar. Y eso NO es una perdida disfrazada de
// diseño: en una plataforma donde un modulo no puede llegar despues, listarlos al compilar es
// el MISMO hecho dicho en voz alta. Lo que se pierde -- que la institucion entregue su modulo
// sin que republiquemos -- se pierde por Apple, no por esta clase.
//
// EL ADAPTADOR ES EL PROTOTIPO DEL CODEGEN. La R13 de Alvaro dice que para Android hace falta
// «un shim Android diminuto por modulo, generado por codegen». Esto es lo mismo del otro lado:
// hoy el adaptador se escribe a mano, y el dia que se genere, se genera exactamente esto.
public static class CompiledIn
{
	// Registro al arrancar, lecturas despues. Concurrente por si una pantalla pregunta el
	// catalogo mientras otra todavia registra -- barato, y evita una carrera que se
	// manifestaria como "el modulo no existe" una vez de cada cien arranques.
	private static readonly ConcurrentDictionary<string, Func<string, CompiledSurface>> registro =
		new(StringComparer.OrdinalIgnoreCase);

	/// <param name="module">El nombre del negocio, el mismo del manifiesto y de la carpeta.</param>
	/// <param name="surface">
	/// Recibe la carpeta OTORGADA y devuelve la superficie. La carpeta llega por parametro y no
	/// la elige el modulo, igual que en las otras dos cascaras: otorgar es acto del que otorga.
	/// </param>
	public static void Register(string module, Func<string, CompiledSurface> surface) =>
		registro[module] = surface;

	/// Los que estan registrados. Sirve para diagnosticar en el aparato, que es donde se va a
	/// notar que faltaba uno.
	public static IReadOnlyCollection<string> Registered => (IReadOnlyCollection<string>)registro.Keys;

	internal static CompiledSurface? Build(string module, string grantedFolder) =>
		registro.TryGetValue(module, out Func<string, CompiledSurface>? f) ? f(grantedFolder) : null;
}

// LO QUE UN MODULO COMPILADO ADENTRO DECLARA DE SI MISMO. Dos cosas y nada mas.
//
// SOBRE LO QUE DEVUELVE `Answer`: tiene que ser algo que Gnothi.Assistant/Json.cs sepa
// escribir -- diccionarios, listas y primitivos. NO un POCO.
//
// Eso parece una carga para el adaptador y en realidad es la consecuencia honesta: sin
// reflexion, alguien tiene que proyectar, y el unico que puede hacerlo sin reflexionar es el
// codigo que YA CONOCE el tipo. InProcessShell.Project() hace esa misma proyeccion, pero
// mirando propiedades en runtime -- que es justo lo que aca no se puede.
public sealed record CompiledSurface(
	IReadOnlyList<string> Tools,
	Func<string, IReadOnlyDictionary<string, object?>?, Accepts, object> Answer);

internal sealed class CompiledInShell : IShell
{
	private readonly string name;
	private readonly string grantedFolder;
	private CompiledSurface? surface;

	public CompiledInShell(string name, string grantedFolder)
	{
		this.name = name;
		this.grantedFolder = grantedFolder;
	}

	// Registrado no es lo mismo que construido. Se construye al primer uso, igual que las
	// otras dos, para que instalar un modulo no cueste abrir su journal.
	public bool IsRunning => surface is not null;

	// FALLA CERRADO Y RUIDOSO, y esto es deliberado.
	//
	// Un installation.json que dice "compiled-in" para un modulo que la app NO embarco es un
	// error de quien armo la app, y el sintoma natural seria un catalogo con un modulo de cero
	// tools -- indistinguible de un modulo apagado por la persona. Esa confusion es exactamente
	// la que no queremos: el consentimiento apagado tiene que seguir siendo indistinguible de
	// "nadie pregunto", y un error de empaquetado NO.
	private CompiledSurface Start() =>
		surface ??= CompiledIn.Build(name, grantedFolder)
			?? throw new InvalidOperationException(
				$"'{name}' declara la cascara 'compiled-in' pero no esta registrado. " +
				$"Registrados: [{string.Join(", ", CompiledIn.Registered)}]. " +
				"Lo registra la app al arrancar, con CompiledIn.Register.");

	public Task<IReadOnlyList<string>> ActualTools(CancellationToken ct) =>
		Task.FromResult(Start().Tools);

	public Task<object> Invoke(string tool, IReadOnlyDictionary<string, object?>? arguments, Accepts accepts, CancellationToken ct)
	{
		CompiledSurface s = Start();

		// El nombre se verifica ACA y no adentro del adaptador. Asi el mensaje es el mismo que
		// dan las otras dos cascaras, y un adaptador que se olvide de validar no termina
		// devolviendo algo raro para una tool que no existe.
		if (!s.Tools.Contains(tool, StringComparer.Ordinal))
			throw new InvalidOperationException($"module '{name}' does not expose '{tool}'");

		// `accepts` va por su propio carril, NUNCA por `arguments`. Es la misma regla que
		// InProcessShell.Bind aplica leyendo el parametro reservado antes que el diccionario:
		// el formato no es del que pregunta.
		return Task.FromResult(s.Answer(tool, arguments, accepts));
	}

	// No hay proceso que matar ni contexto que descargar: el modulo es parte de la app y vive
	// lo que vive la app. La contracara es que NO se puede desalojar para soltar el journal
	// -- las otras dos si pueden -- y por eso un adaptador no deberia dejar archivos abiertos
	// entre llamadas.
	public ValueTask DisposeAsync()
	{
		surface = null;
		return ValueTask.CompletedTask;
	}
}
