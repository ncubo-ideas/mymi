using System.Text.Json;


namespace Gnothi.Host;

// LA SALA: el modulo es cargo -sus .dll y nada mas- pero no corre en el proceso de Gnothi.
// Corre adentro del motor web, en UN SOLO documento que es de Gnothi y no de ninguna
// institucion, con el runtime .NET y el motor compilados a wasm.
//
// POR QUE EXISTE. En un telefono, cargar el .dll de un tercero en el proceso de la app es lo
// que las dos tiendas no permiten publicar -Google lo dice de los .dex, .jar y .so, y Apple en
// la 3.3(b) del acuerdo-, y las dos exceptuan el codigo que interpreta un motor web. La sala es
// esa excepcion, sin perder lo que la forma cargo daba: el modulo sigue pesando 200 KB porque
// el sustrato se lo presta Gnothi, y se presta UNA vez para todos.
//
// LAS DOS COSAS QUE ESTA CLASE SOSTIENE, y no son opcionales:
//
//   1 · UNA LLAMADA EN VUELO. La identidad del modulo no viene en el mensaje: viene del marco
//       de la llamada que el Host abrio. Eso solo identifica mientras haya una sola llamada a
//       la vez, asi que se serializa aca y no se confia en que nadie la cruce.
//
//   2 · UN JOURNAL MONTADO POR VEZ. Medido: con varias carpetas montadas a la vez, cualquier
//       codigo adentro del sandbox lee la del vecino. Con una sola, el intento falla solo. Asi
//       que antes de cargar un modulo se desaloja el anterior -- y en ese orden, porque borrar
//       la carpeta con el actor vivo mata el runtime desde un finalizador.
internal sealed class SalaShell : IShell
{
	// EL ANFITRION DE LA SALA, que lo pone la plataforma igual que con WebViewShell.Host. El
	// Host no sabe que hay un WebView del otro lado: le pide texto y recibe texto.
	internal static ISalaHost? Host;

	// Compartido por TODAS las instancias, porque la sala es una sola: el Dispatcher crea un
	// Module -y por lo tanto una cascara- por modulo, pero del otro lado hay un unico documento.
	private static readonly SemaphoreSlim UnaPorVez = new(1, 1);
	private static string cargado = "";

	private readonly string name;
	private readonly string entrada;
	private readonly string granted;

	public SalaShell(string name, Shell shell, string grantedFolder)
	{
		this.name = name;

		// DONDE ESTA EL DLL, SEGUN COMO SE INSTALO EL MODULO. En la forma hibrida el alta
		// escribe [web, endpoint, dll] -- el web va primero porque lo lee todo el lado que abre
		// la pantalla, y poner el dll ahi haria que abrir el modulo intentara servir un .dll
		// como si fuera la carpeta del bundle. En la forma cargo el dll es lo unico y va en [0].
		//
		// La sala toma EL MISMO paquete en los dos casos: solo cambia de que casillero sacar la
		// ruta.
		entrada = shell.Arguments.Count > 2 ? shell.Arguments[2]
			: shell.Arguments.Count > 0 ? shell.Arguments[0]
			: "";

		granted = grantedFolder;
	}

	public bool IsRunning => string.Equals(cargado, name, StringComparison.Ordinal);

	public async Task<object> Invoke(string tool, IReadOnlyDictionary<string, object?>? arguments, Accepts accepts, CancellationToken ct)
	{
		await UnaPorVez.WaitAsync(ct);
		try
		{
			await Asegurar(ct);

			string args = JsonSerializer.Serialize(arguments ?? new Dictionary<string, object?>());
			string crudo = await Anfitrion().Pedir("consultar", name, tool, args, ct);

			using JsonDocument doc = JsonDocument.Parse(crudo);
			if (!doc.RootElement.GetProperty("ok").GetBoolean())
				throw new InvalidOperationException(doc.RootElement.GetProperty("motivo").GetString());

			// EL CONTENIDO VIAJA OPACO, igual que en las otras tres cascaras: el Host no lo mira,
			// lo reenvia. Aca llega ya aplanado a diccionarios y primitivos desde adentro de la
			// sala, que es donde vive el Type del modulo.
			return JsonSerializer.Deserialize<object>(
				doc.RootElement.GetProperty("contenido").GetRawText())!;
		}
		finally { UnaPorVez.Release(); }
	}

	public async Task<IReadOnlyList<string>> ActualTools(CancellationToken ct)
	{
		await UnaPorVez.WaitAsync(ct);
		try
		{
			await Asegurar(ct);

			string crudo = await Anfitrion().Pedir("repertorio", name, "", "", ct);
			using JsonDocument doc = JsonDocument.Parse(crudo);
			if (!doc.RootElement.GetProperty("ok").GetBoolean()) return [];

			return [.. doc.RootElement.GetProperty("consultas").EnumerateArray()
				.Select(x => x.GetString() ?? "")];
		}
		finally { UnaPorVez.Release(); }
	}

	// LO QUE PIDE LA CARA DE LA INSTITUCION, y es otro conjunto que el del asistente.
	//
	// SIN ESTO LA PANTALLA NO ABRE. El valor por omision de IShell.View tira
	// NotSupportedException -- "only the hybrid form has a face that asks" -- y la cara del
	// modulo se queda en esa excepcion. La sala SI tiene con que contestarle: la superficie que
	// corre adentro trae sus [View] igual que sus [Query], y el sustrato ya las descubre.
	//
	// Siguen siendo dos caminos y no un parametro, igual que en el contrato: con uno solo, una
	// consulta de pantalla podria entrar por la puerta del asistente y al reves.
	public async Task<object> View(string name, IReadOnlyDictionary<string, object?>? arguments, Accepts accepts, CancellationToken ct)
	{
		await UnaPorVez.WaitAsync(ct);
		try
		{
			await Asegurar(ct);

			string args = JsonSerializer.Serialize(arguments ?? new Dictionary<string, object?>());
			return Contenido(await Anfitrion().Pedir("vista", this.name, name, args, ct));
		}
		finally { UnaPorVez.Release(); }
	}

	// LO QUE EL CARTERO DEJA. Gnothi hizo la red con SU credencial y le entrega al modulo un
	// sobre ya leido; en la forma hibrida la cara no tiene motor, asi que la superficie es la
	// unica que puede asentarlo. En la sala pasa lo mismo, un piso mas adentro.
	public async Task<string> Deliver(string envelopeJson, CancellationToken ct)
	{
		await UnaPorVez.WaitAsync(ct);
		try
		{
			await Asegurar(ct);

			object? r = Contenido(await Anfitrion().Pedir("entregar", name, envelopeJson, "", ct));
			return r as string ?? JsonSerializer.Serialize(r);
		}
		finally { UnaPorVez.Release(); }
	}

	// HASTA DONDE YA LEYO, para que el cartero pida de ahi en adelante y no se baje el historial
	// entero en cada refresco.
	public async Task<long> Cursor(CancellationToken ct)
	{
		await UnaPorVez.WaitAsync(ct);
		try
		{
			await Asegurar(ct);

			object? r = Contenido(await Anfitrion().Pedir("cursor", name, "", "", ct));
			return r is JsonElement j && j.TryGetInt64(out long v) ? v : 0L;
		}
		catch (Exception) { return 0L; }
		finally { UnaPorVez.Release(); }
	}

	// EL DESALOJO ES DEL LADO DE ADENTRO. Soltar esta cascara no tiene por que tirar el
	// documento: la sala sigue viva para el proximo modulo, que es toda la ganancia.
	public ValueTask DisposeAsync() => ValueTask.CompletedTask;

	// La respuesta de la sala es siempre {ok, contenido} o {ok:false, motivo}. Un solo lugar que
	// lo abra, para que los cuatro caminos fallen igual.
	private static object? Contenido(string crudo)
	{
		using JsonDocument doc = JsonDocument.Parse(crudo);
		if (!doc.RootElement.GetProperty("ok").GetBoolean())
			throw new InvalidOperationException(doc.RootElement.GetProperty("motivo").GetString());

		return doc.RootElement.TryGetProperty("contenido", out JsonElement c)
			? JsonSerializer.Deserialize<object>(c.GetRawText())
			: null;
	}

	private ISalaHost Anfitrion() => Host
		?? throw new InvalidOperationException(
			"no hay sala registrada: la app tiene que poner SalaShell.Host antes de despachar.");

	// UN MODULO POR VEZ. El anterior se desaloja ANTES de que el siguiente monte su carpeta, y
	// por eso la frontera entre journals existe: en el sandbox no queda nada del vecino que
	// leer.
	private async Task Asegurar(CancellationToken ct)
	{
		if (string.Equals(cargado, name, StringComparison.Ordinal)) return;

		if (cargado.Length > 0)
		{
			await Anfitrion().Pedir("desalojar", cargado, "", "", ct);
			cargado = "";
		}

		await Anfitrion().Preparar(name, entrada, granted, ct);

		string crudo = await Anfitrion().Pedir("cargar", name, "", "", ct);
		using JsonDocument doc = JsonDocument.Parse(crudo);
		if (!doc.RootElement.GetProperty("ok").GetBoolean())
			throw new InvalidOperationException(doc.RootElement.GetProperty("motivo").GetString());

		cargado = name;
	}
}

// LO QUE LA PLATAFORMA LLENA. Dos cosas y nada mas: decirle a la sala de donde sacar los bytes
// de un modulo, y hacerle un pedido.
//
// NO RECIBE UN WEBVIEW NI NADA DEL MOTOR WEB: el Host habla con texto, que es la misma costura
// que ya tiene con la cascara hospedada.
internal interface ISalaHost
{
	// Donde viven los .dll de este modulo y donde esta su journal en el aparato. La sala los
	// pide por su propio origen y esta implementacion los sirve; el modulo nunca abre un archivo.
	Task Preparar(string modulo, string entrada, string grantedFolder, CancellationToken ct);

	Task<string> Pedir(string verbo, string modulo, string a, string b, CancellationToken ct);
}
