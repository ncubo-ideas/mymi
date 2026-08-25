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
	public Instalacion Instalacion { get; }
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
