using ModelContextProtocol.Protocol;

namespace Gnothi.Host;

// UNA INSTANCIA DE MODULO: lo que la institucion declaro + lo que el Host le concedio +
// la cascara con la que se lo ejecuta.
//
// Es la unidad de aislamiento. En este POC el aislamiento real que hay es el que da el
// sistema operativo a un proceso hijo -que es poco- mas la carpeta unica. El aislamiento
// duro (memoria lineal propia, --dir) llega con la Etapa 2. Conviene no decir que este
// POC demuestra confinamiento: demuestra el eslabon de agregacion, que es otra cosa.
internal sealed class Modulo : IAsyncDisposable
{
	private readonly CascaraStdio cascara;

	public Modulo(Manifiesto manifiesto, Instalacion instalacion, string directorio)
	{
		Manifiesto = manifiesto;
		Instalacion = instalacion;
		Directorio = directorio;
		cascara = new CascaraStdio(manifiesto.Modulo, instalacion.Cascara, instalacion.CarpetaConcedida);
	}

	public Manifiesto Manifiesto { get; }
	// El consentimiento es lo UNICO mutable de un modulo en caliente: setter privado y un
	// solo camino de escritura, Reconsentir. La cascara y la carpeta concedida no se
	// recargan, porque cambiarlas es reinstalar y no reconsentir.
	public Instalacion Instalacion { get; private set; }
	public string Directorio { get; }
	public string Nombre => Manifiesto.Modulo;
	public bool Levantado => cascara.Levantado;

	// El nombre con el que la tool viaja hacia el asistente. Prefijado por modulo porque
	// dos instituciones van a llamar 'total' a cosas distintas, y el asistente elige por
	// nombre y descripcion.
	public static string NombrePublico(string modulo, string tool) => $"{modulo}_{tool}";

	// Las tools que este modulo APORTA al catalogo, ya filtradas por consentimiento.
	//
	// Apagado devuelve vacio: el modulo no se entera, y no puede distinguir "me apagaron"
	// de "nadie pregunto". Esa indistinguibilidad es la propiedad, no un efecto lateral.
	public IEnumerable<ToolDeclarada> ToolsHabilitadas()
	{
		if (!Instalacion.Encendido) return [];
		return Manifiesto.Tools.Where(t => !Instalacion.ToolsApagadas.Contains(t.Nombre));
	}

	// RECONSENTIR: la persona movio un interruptor y el Host se enteró leyendo su archivo.
	//
	// Devuelve si algo cambió de verdad, porque un editor de texto escribe el mismo archivo
	// tres veces y no hay que avisarle al asistente tres veces que su catalogo cambió.
	//
	// Solo entran los dos campos del consentimiento. Que la firma no acepte una Instalacion
	// entera es a proposito: hace IMPOSIBLE que un cambio de cascara se cuele por acá y le
	// mueva el piso a un modulo que ya está corriendo.
	public bool Reconsentir(bool encendido, IReadOnlyList<string> toolsApagadas)
	{
		bool mismoInterruptor = Instalacion.Encendido == encendido;
		bool mismasApagadas = Instalacion.ToolsApagadas.Count == toolsApagadas.Count
			&& !Instalacion.ToolsApagadas.Except(toolsApagadas, StringComparer.Ordinal).Any();

		if (mismoInterruptor && mismasApagadas) return false;

		Instalacion = Instalacion with { Encendido = encendido, ToolsApagadas = toolsApagadas };
		return true;
	}

	public bool Permitida(string tool)
		=> Instalacion.Encendido && !Instalacion.ToolsApagadas.Contains(tool);

	public Task<CallToolResult> Invocar(
		string tool, IReadOnlyDictionary<string, object?>? args, CancellationToken ct)
		=> cascara.Invocar(tool, args, ct);

	// Para la verificacion declarado-contra-real.
	public async Task<IReadOnlyList<string>> ToolsRealesDelModulo(CancellationToken ct)
		=> [.. (await cascara.ToolsReales(ct)).Select(t => t.Name)];

	// Medido DESDE AFUERA, con stat sobre la carpeta que el Host concedio. No se le
	// pregunta al modulo cuanto ocupa: un modulo que reporta 4 MB mientras ocupa 400 no
	// puede existir si nadie le pregunta.
	public long BytesEnDisco()
	{
		if (!Directory.Exists(Instalacion.CarpetaConcedida)) return 0;
		return new DirectoryInfo(Instalacion.CarpetaConcedida)
			.EnumerateFiles("*", SearchOption.AllDirectories)
			.Sum(f => f.Length);
	}

	public ValueTask DisposeAsync() => cascara.DisposeAsync();
}
