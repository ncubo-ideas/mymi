namespace Gnothi.Host;

// EL DESPACHO: el nucleo del Host. Catalogo y Atender, y nada mas.
//
// NO SABE que existe MCP, ni un asistente, ni un bucle de agente. Recibe una Peticion y
// devuelve una Respuesta. Quien lo llama es asunto de quien lo llama.
//
// Por que importa tanto esta forma: el dia que exista una segunda entrada -en este
// proyecto sera el bucle de agente propio para movil- tiene que hacer LO MISMO. Si la
// logica de catalogo, permisos y cuotas viviera adentro de la clase que habla MCP, la
// segunda entrada obliga a duplicarla o a extraer a mano la mitad de algo que ya esta en
// produccion. Y peor: no se podria BORRAR la primera entrada el dia que sobre, porque se
// llevaria los modulos con ella.
//
// La direccion de las flechas es la propiedad, y es verificable: frente -> despacho ->
// modulos. Nunca al revés, nunca de costado.
internal sealed class Despacho
{
	private readonly CatalogoDeModulos catalogo;
	private readonly Medidor medidor;

	public Despacho(CatalogoDeModulos catalogo, Medidor medidor)
	{
		this.catalogo = catalogo;
		this.medidor = medidor;
	}

	public CatalogoDeModulos Modulos => catalogo;
	public Medidor Medidor => medidor;

	// EL CATALOGO. La misma lista para cualquier llamador; no la arma el frente.
	// Sale del MANIFIESTO, no de preguntarle al modulo: armar el catalogo no despierta a
	// nadie.
	public IEnumerable<(Modulo modulo, ToolDeclarada tool)> Catalogo()
		=> catalogo.Modulos.SelectMany(m => m.ToolsHabilitadas().Select(t => (m, t)));

	// ATENDER. El unico camino de ejecucion, para cualquier entrada.
	public async Task<Respuesta> Atender(Peticion p, CancellationToken ct)
	{
		Modulo? m = catalogo.Buscar(p.Modulo);
		if (m is null) return Respuesta.Denegada($"no hay un modulo instalado llamado '{p.Modulo}'");

		// El permiso se chequea ACA, no en el frente. Un frente nuevo no puede olvidarse
		// de chequearlo porque no es el que invoca.
		if (!m.Permitida(p.Tool))
			return Respuesta.Denegada(
				$"'{p.Tool}' no esta disponible: la persona apago esa consulta o el modulo entero");

		medidor.Contar(p.Modulo, p.Tool);

		try
		{
			object contenido = await m.Invocar(p.Tool, p.Argumentos, ct);
			return Respuesta.Ok(contenido);
		}
		catch (Exception e)
		{
			// El error de un modulo es un error del modulo, no una caida del Host. Veinte
			// modulos instalados significa veinte formas de que algo ajeno falle.
			return Respuesta.Denegada($"el modulo '{p.Modulo}' fallo: {e.GetType().Name}: {e.Message}");
		}
	}
}

internal sealed record Peticion(string Modulo, string Tool, IReadOnlyDictionary<string, object?>? Argumentos);

// El contenido viaja como OPACO a proposito. El despacho no lo mira, no lo parsea y no lo
// reformatea: si lo hiciera tendria que entender el dominio, y ahi la capa se contamina.
// El frente lo devuelve tal cual llego del modulo.
internal sealed record Respuesta(bool Autorizada, object? Contenido, string Motivo)
{
	public static Respuesta Ok(object contenido) => new(true, contenido, "");
	public static Respuesta Denegada(string motivo) => new(false, null, motivo);
}

// MEDIR es uno de los cuatro verbos del Host, y lo que se mide es al ASISTENTE: cuantas
// veces pregunto y por que. No es cuota para proteger el dispositivo -es que la persona
// pueda ver cuanto le sacaron.
internal sealed class Medidor
{
	private readonly Dictionary<string, int> porTool = [];
	private readonly Lock candado = new();

	public void Contar(string modulo, string tool)
	{
		lock (candado) { string k = $"{modulo}/{tool}"; porTool[k] = porTool.GetValueOrDefault(k) + 1; }
	}

	public IReadOnlyDictionary<string, int> Consultas()
	{
		lock (candado) { return new Dictionary<string, int>(porTool); }
	}
}
