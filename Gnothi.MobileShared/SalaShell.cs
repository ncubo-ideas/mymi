using System.Text.Json;


namespace Gnothi.Host;

// LA SALA: el modulo es cargo -sus .dll y nada mas- pero no corre en el proceso de Gnothi.
// Corre adentro del motor web, en UN SOLO documento que es de Gnothi y no de ninguna
// institucion, con el runtime .NET y el motor compilados a wasm.
//
// POR QUE EXISTE. En un telefono, cargar el .dll de un tercero en el proceso de la app deja al
// codigo de la institucion con acceso DIRECTO a las APIs del sistema. La sala se lo quita sin
// perder lo que la forma cargo daba: el modulo sigue pesando 200 KB porque el sustrato se lo
// presta Gnothi, y se presta UNA vez para todos.
//
// ── QUE SE PUEDE AFIRMAR DE LAS TIENDAS Y QUE NO ────────────────────────────────────────────
//
// ESTE COMENTARIO DECIA MAS DE LO QUE SE SABIA. Decia que "las dos tiendas exceptuan el codigo
// que interpreta un motor web" y que "la sala es esa excepcion". Eso venia del viejo §3.3.2 del
// acuerdo de Apple, QUE YA NO EXISTE CON ESA REDACCION. Leidos los tres textos el 2026-09-16,
// ver `Documentacion/Jonathan/B-arquitectura/B11-lo-que-dicen-las-tiendas.md`:
//
//   - GOOGLE PLAY es el texto claro, y la palabra que decide es "indirect". Aca el codigo de la
//     institucion lo ejecuta el runtime .NET compilado a wasm, dentro del documento del motor
//     web, y su acceso al sistema es indirecto: MEDIDO el 2026-09-17 con una sonda que corre
//     como modulo -- no ve el disco del aparato, no sale del origen, y no tiene como llamar al
//     puente. Por ese texto, la sala pasa.
//
//   - APPLE ES UNA PREGUNTA ABIERTA, no un si. Hoy dice "interpreted code" con tres
//     condiciones, sin nombrar motor. Y B11 deja sin saber DOS cosas que son exactamente el
//     caso de la sala: si wasm cuenta como codigo interpretado, y si un .dll de IL corrido por
//     el interprete de Mono cuenta. No hay como contestarlo leyendo: se contesta enviando.
//
//   - EL 4.7.2 -no exponer APIs nativas a software que no viene en el binario- le pega a la
//     forma hospedada por su puente. A la sala le pega menos, porque el dominio de adentro NO
//     tiene puente: no puede alcanzar `DotNet.invokeMethodAsync`. Menos no es nada.
//
//   - Y el ESCAPARATE (3.3.1(B)(c)) le pega a Gnothi entero, no a una forma.
//
// O sea: la sala esta MEJOR POSICIONADA que la hibrida y, frente al texto de Google, al menos
// tan bien como la hospedada. No es "la excepcion que las tiendas nombran". Nada de esto es un
// veredicto: son textos leidos, no una revision pasada.
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

	// EL TOPE DE UN PEDIDO A LA SALA. Generoso a proposito: lo medido en un A54 y en el emulador
	// es 36-120 ms para cargar y para desalojar, asi que diez segundos no se alcanzan trabajando,
	// solo trabado.
	//
	// No es `const` para que la prueba pueda bajarlo: sin eso, afirmar que un modulo trabado no
	// cuelga la sala costaria diez segundos de suite, y una prueba cara termina sin correrse.
	internal static TimeSpan Tope = TimeSpan.FromSeconds(10);

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
			string crudo = await Pedir("consultar", name, tool, args, ct);

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

			string crudo = await Pedir("repertorio", name, "", "", ct);
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
			return Contenido(await Pedir("vista", this.name, name, args, ct));
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

			string crudo = await Pedir("entregar", name, envelopeJson, "", ct);
			await Persistir(name, crudo, ct);

			object? r = Contenido(crudo);
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

			object? r = Contenido(await Pedir("cursor", name, "", "", ct));
			return r is JsonElement j && j.TryGetInt64(out long v) ? v : 0L;
		}
		catch (Exception) { return 0L; }
		finally { UnaPorVez.Release(); }
	}

	// EL DESALOJO ES DEL LADO DE ADENTRO. Soltar esta cascara no tiene por que tirar el
	// documento: la sala sigue viva para el proximo modulo, que es toda la ganancia.
	public ValueTask DisposeAsync() => ValueTask.CompletedTask;

	// EL VIAJE DE VUELTA DEL JOURNAL.
	//
	// Adentro de la sala no hay disco: el modulo escribe en memoria y no sabe que hay una carpeta
	// del otro lado. Lo que escribio viaja en la respuesta, y Gnothi lo guarda. El modulo NO elige
	// donde aterriza: da una ruta relativa y el anfitrion la ancla a SU carpeta, con el mismo
	// cerco que el puente de la forma hospedada ya tiene.
	//
	// Falla RUIDOSA a proposito: si esto se cayera en silencio, la persona veria su refresco
	// aplicado y al volver ya no estaria, que es la peor forma de perder un dato.
	private async Task Persistir(string modulo, string crudo, CancellationToken ct)
	{
		using JsonDocument doc = JsonDocument.Parse(crudo);
		if (!doc.RootElement.TryGetProperty("escrituras", out JsonElement escrituras)) return;
		if (escrituras.ValueKind != JsonValueKind.Array || escrituras.GetArrayLength() == 0) return;

		foreach (JsonElement e in escrituras.EnumerateArray())
		{
			string ruta = e.GetProperty("ruta").GetString() ?? "";
			string contenido = e.GetProperty("contenido").GetString() ?? "";
			if (ruta.Length == 0) continue;

			await Anfitrion().Guardar(modulo, ruta, contenido, ct);
		}
	}

	// QUE EL DESALOJO HAYA OCURRIDO DE VERDAD. Dos cosas, y las dos tienen que ser ciertas: que la
	// operacion no fallara, y que la carpeta quedara vaciada. La segunda es la que importa,
	// porque es la frontera.
	private static void Confirmar(string despedida, string modulo)
	{
		using JsonDocument doc = JsonDocument.Parse(despedida);

		if (!doc.RootElement.TryGetProperty("ok", out JsonElement ok) || !ok.GetBoolean())
			throw new InvalidOperationException(
				$"no se pudo desalojar '{modulo}', asi que no se carga otro encima: "
				+ (doc.RootElement.TryGetProperty("motivo", out JsonElement m) ? m.GetString() : "sin motivo"));

		if (!doc.RootElement.TryGetProperty("vaciada", out JsonElement vaciada) || !vaciada.GetBoolean())
			throw new InvalidOperationException(
				$"el journal de '{modulo}' no quedo vaciado, asi que no se carga otro encima: "
				+ (doc.RootElement.TryGetProperty("desmontada", out JsonElement d) ? d.GetString() : "sin detalle"));
	}

	// QUE QUEDO DE LAS CARGAS ANTERIORES, Y QUE PASA SI QUEDO ALGO.
	//
	// El sustrato ya reportaba esto al cargar, porque la contencion se afirma intentandola. Pero
	// era una LINEA DE LOG: si alguna vez hubiera dicho mas de cero bytes, el modulo entraba
	// igual y nadie se enteraba hasta leer el logcat de una corrida vieja.
	//
	// Ahora es una condicion. Cero bytes es la frontera de la sala, y una frontera que no se
	// chequea no es una frontera: es una expectativa.
	//
	// LOS ARCHIVOS SI PUEDEN QUEDAR, y la diferencia importa. El desmontaje VACIA en vez de
	// borrar -- borrar mata el runtime desde un finalizador --, asi que quedan carpetas de cero
	// bytes. Desde el cambio a nombres neutros esas carpetas se llaman `c1`, `c2`: dicen cuantas
	// cargas hubo, no de quien. Eso es el residuo conocido y acotado; bytes, no.
	private static void Vecindario(JsonElement respuesta, string modulo)
	{
		if (!respuesta.TryGetProperty("vecinos", out JsonElement vecinos)
			|| vecinos.ValueKind != JsonValueKind.Array) return;

		long bytes = 0;
		foreach (JsonElement v in vecinos.EnumerateArray())
			if (v.TryGetProperty("bytes", out JsonElement b) && b.TryGetInt64(out long n)) bytes += n;

		if (bytes > 0)
			throw new InvalidOperationException(
				$"no se carga '{modulo}': quedaron {bytes} bytes de cargas anteriores montados en la sala, "
				+ $"y eso es la frontera rota -- {vecinos.GetRawText()}");
	}

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

	// NINGUN PEDIDO A LA SALA ES ILIMITADO, Y ESE ERA EL AGUJERO.
	//
	// Adentro del desalojo corre codigo QUE ELIGE EL MODULO: el sustrato le llama a su Dispose(),
	// su Cerrar(), su Close(). Un ciclo infinito ahi no se interrumpe desde adentro -- el motor
	// web es de un solo hilo, no hay otro que pueda apropiarse -- y el dano no se queda en el
	// modulo: se queda tambien con `UnaPorVez` tomado, asi que TODOS dejan de contestar hasta que
	// alguien mate la app.
	//
	// Son dos modos de falla distintos y hubo que arreglarlos por separado: el `finally` del
	// sustrato cubre el que TIRA, este cubre el que NO VUELVE.
	//
	// DESDE AFUERA SI SE PUEDE, y la forma correcta no es un timeout comun sino TIRAR EL
	// DOCUMENTO. El motor web corre en su propio proceso, asi que destruirlo se lleva el ciclo
	// puesto; y lo que el modulo tenia montado vive ADENTRO del documento, no en el disco, asi
	// que al descartarlo no queda nada que desmontar. La frontera la cierra la plataforma, y no
	// la cooperacion del modulo -- que es lo que se le estaba pidiendo.
	//
	// LO QUE SE PIERDE es lo que ese modulo no habia devuelto todavia, y es SUYO: el que traba su
	// propio desalojo paga su propio dato, y no el del vecino. La consulta en curso falla ruidosa
	// -- mismo criterio que `Confirmar` -- y la siguiente arranca una sala nueva.
	private async Task<string> Pedir(string verbo, string modulo, string a, string b, CancellationToken ct)
	{
		try { return await Anfitrion().Pedir(verbo, modulo, a, b, ct).WaitAsync(Tope, ct); }
		catch (TimeoutException)
		{
			// Primero la variable y despues el documento: si descartar tirara, el proximo intento
			// tiene que creer que no hay nadie cargado en vez de reintentar un desalojo imposible.
			cargado = "";

			// Sin `ct`: el que se cancelo puede ser justamente el pedido que quedo trabado, y
			// dejar el documento muerto en pie es peor que cualquier cancelacion.
			await Anfitrion().Descartar($"'{modulo}' no contesto a '{verbo}'", CancellationToken.None);

			throw new InvalidOperationException(
				$"la sala no contesto a '{verbo}' de '{modulo}' en {Tope.TotalSeconds:0} s; se tiro el "
				+ "documento, y con el todo lo que ese modulo tenia montado");
		}
	}

	// UN MODULO POR VEZ. El anterior se desaloja ANTES de que el siguiente monte su carpeta, y
	// por eso la frontera entre journals existe: en el sandbox no queda nada del vecino que
	// leer.
	//
	// ── LA DECISION DE NO RESERVAR LA RANURA ────────────────────────────────────────────────
	//
	// La cara de un modulo y el asistente entran los dos por aca, asi que se desalojan
	// mutuamente. MEDIDO en el emulador: 42 consultas encadenadas al mismo modulo cuestan 728 ms;
	// las mismas 42 con una consulta de otro modulo intercalada entre cada una cuestan 8.264 ms.
	// Once veces mas.
	//
	// Se evaluo que la pantalla abierta RETUVIERA la ranura y que una consulta por otro modulo
	// fallara mientras tanto. SE DESCARTO, y por la razon de fondo del producto: preguntar por
	// cualquier modulo -- y CRUZAR dos -- es para lo que existe Gnothi. Una pregunta que cruza
	// compras con examenes toca los dos en el mismo turno; con la ranura reservada por una
	// pantalla abierta, esa pregunta no se podria contestar. Se estaria apagando el producto para
	// proteger un dibujo.
	//
	// Y el peor caso medido no es el caso real: exige una consulta entre CADA una de las 42 idas
	// y vueltas. Una consulta suelta durante una apertura cuesta ~450 ms, que no se ve. El costo
	// que si se paga siempre es el del cruce: un cambio de modulo por cada modulo que la pregunta
	// toque, ~200 a 450 ms cada uno, contra los segundos que tarda el modelo en contestar.
	//
	// Si algun dia una pantalla se ve lenta, el numero ya esta medido y el lugar a tocar es este.
	private async Task Asegurar(CancellationToken ct)
	{
		if (string.Equals(cargado, name, StringComparison.Ordinal)) return;

		if (cargado.Length > 0)
		{
			// EL DESALOJO TRAE LO QUE EL MODULO ESCRIBIO, y hay que guardarlo ANTES de cargar al
			// siguiente. Si no, lo que asento se evapora al vaciarle la carpeta: el refresco
			// parecia funcionar y despues volvia al estado anterior.
			string despedida = await Pedir("desalojar", cargado, "", "", ct);
			await Persistir(cargado, despedida, ct);

			// Y RECIEN AHORA DEJA DE SER SERVIBLE. Entre el desalojo y la carga del siguiente no
			// hay ningun modulo servible por el origen de la sala.
			await Anfitrion().Soltar(cargado, ct);

			// FALLAR CERRADO, Y ES UN CAMBIO DE CRITERIO.
			//
			// Antes esto no miraba la respuesta: desalojaba, borraba su variable y cargaba al
			// siguiente aunque el desalojo hubiera fallado. Como la frontera de la sala ES el
			// vaciado, un desalojo fallido la dejaba abierta EN SILENCIO.
			//
			// Ahora una consulta falla ruidosa antes que contestar bien con la frontera rota. Un
			// modulo trabado bloquea al resto, y eso es lo correcto para una propiedad de
			// contencion: la alternativa es contestar como si nada mientras el vecino queda
			// expuesto.
			Confirmar(despedida, cargado);
			cargado = "";
		}

		await Anfitrion().Preparar(name, entrada, granted, ct);

		string crudo = await Pedir("cargar", name, "", "", ct);
		using JsonDocument doc = JsonDocument.Parse(crudo);
		if (!doc.RootElement.GetProperty("ok").GetBoolean())
			throw new InvalidOperationException(doc.RootElement.GetProperty("motivo").GetString());

		Vecindario(doc.RootElement, name);
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

	// DEJA DE SER SERVIBLE. El anfitrion sirve los bytes de UN modulo por su propio origen, y
	// esto le dice cual deja de serlo. Entre un modulo y el siguiente no hay ninguno: si algo
	// pidiera un journal en esa ventana, la respuesta es que no existe.
	Task Soltar(string modulo, CancellationToken ct);

	// LO QUE EL MODULO ESCRIBIO, de vuelta a la carpeta de la persona. La ruta es RELATIVA y el
	// que la ancla es el anfitrion: el modulo no puede nombrar donde aterriza, que es la misma
	// regla que `WebModule.InJournal` sostiene del otro lado.
	Task Guardar(string modulo, string ruta, string contenidoBase64, CancellationToken ct);

	// TIRAR EL DOCUMENTO ENTERO, y es lo unico que recupera una sala que no contesta. Lo pide la
	// cascara cuando un pedido paso del tope; el anfitrion suelta la ranura -- un modulo sin
	// documento no es servible por el origen --, falla las esperas pendientes, y el proximo
	// pedido crea un documento nuevo.
	//
	// No tiene que confirmar nada ni desmontar nada: lo que el modulo tenia montado vive en la
	// memoria del documento, asi que se va con el.
	Task Descartar(string motivo, CancellationToken ct);
}
