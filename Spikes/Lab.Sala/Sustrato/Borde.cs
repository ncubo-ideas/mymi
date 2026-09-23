using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

namespace Sala.Sustrato;

// EL BORDE ENTRE LA SALA Y EL MODULO, DECLARADO.
//
// POR QUE EXISTE, y es una objecion de Jonathan que era justa. La nota de la sala decia que esta
// forma es el escalon anterior a la caja de wasm porque «deja el borde declarado como el trabajo
// que permite mudarse despues». Y no lo dejaba: el sustrato reflexionaba sobre el Type de la
// institucion -- buscaba sus metodos, los guardaba en un diccionario y los invocaba --, que es
// exactamente la reflexion de la forma hibrida, un piso mas adentro. El trabajo que habilitaba la
// mudanza era justo el que no se habia hecho.
//
// LA REFLEXION NO DESAPARECE: CAMBIA DE LADO Y SE VUELVE UN PROTOCOLO.
//
// Antes: el sustrato mira adentro del ensamblado ajeno y se queda con los MethodInfo.
// Ahora: alguien reflexiona UNA vez sobre la superficie, PUBLICA una lista de nombres, y de ahi
// en adelante todo pasa por `Atender(verbo, nombre, carga)` con texto de un lado y texto del
// otro. El sustrato no guarda un solo MethodInfo del modulo.
//
// ES EL MISMO BORDE QUE LA CAJA, A PROPOSITO. Los verbos, la forma del sobre y la forma de la
// respuesta son los de `Spikes/Lab.WasiCaja/ElBorde.cs`, de la rama de la caja. Eso es lo que
// hace que mudar un modulo de la sala a la caja sea cambiar el TRANSPORTE -- de una llamada en el
// mismo heap a una llamada por wasm -- y no el contrato. Si las dos ramas se juntan, estos dos
// archivos tienen que converger en uno; hoy no se puede porque viven en ramas distintas.
//
// ── OJO CON LOS VERBOS, QUE ESTAN CRUZADOS ENTRE LAS DOS CAPAS ──────────────────────────────
//
// En este borde, igual que en la caja:  `preguntar` = [Query] (el asistente)
//                                        `consultar` = [View]  (la pantalla)
// En la capa de afuera (Sala.Consultar / Sala.Vista, que es la que habla con el Host) es al
// reves: `consultar` son las [Query]. Los dos contratos ya existian y cada uno es consistente
// consigo mismo, pero la palabra `consultar` significa cosas distintas a un lado y al otro del
// borde. Es una trampa de mudanza y hay que reconciliarla en algun momento: vale la pena
// renombrar UNO de los dos antes de que alguien los mezcle sin darse cuenta.
internal sealed class Borde
{
	private readonly object superficie;
	private readonly Dictionary<string, MethodInfo> porNombre = new(StringComparer.Ordinal);
	private readonly List<string> consultas = [];
	private readonly List<string> vistas = [];
	private readonly MethodInfo? entregar;
	private readonly MethodInfo? cursor;

	// EL DE AFUERA: si el modulo trae SU PROPIO borde, es el que manda y este objeto no
	// reflexiona nada. Ver `Suyo`.
	private readonly Func<string, string, string, object?>? propio;

	// LOS MARCADORES SE BUSCAN POR NOMBRE DE TIPO, y no con `typeof`. Si se usara el tipo, el
	// modulo tendria que cargar EL MISMO ensamblado del contrato que el sustrato, y eso es justo
	// el acoplamiento que la forma cargo evita prestandole la copia. Por nombre, las dos copias
	// sirven -- y un modulo viejo que lleva los marcadores de MCP tambien entra.
	private Borde(object superficie)
	{
		this.superficie = superficie;

		const BindingFlags Todo = BindingFlags.Public | BindingFlags.NonPublic
			| BindingFlags.Instance | BindingFlags.Static;

		foreach (MethodInfo m in superficie.GetType().GetMethods(Todo))
		{
			foreach (object a in m.GetCustomAttributes(false))
			{
				switch (a.GetType().Name)
				{
					case "QueryAttribute":
					case "McpServerToolAttribute":
						Registrar(consultas, Nombre(a, m), m);
						break;

					case "ViewAttribute":
						Registrar(vistas, Nombre(a, m), m);
						break;

					case "DeliverAttribute": entregar = m; break;
					case "CursorAttribute": cursor = m; break;
				}
			}
		}
	}

	// EL MODULO QUE TRAE SU PROPIO BORDE. El sustrato NO mira adentro de su ensamblado: le pide
	// que declare y le cree la lista. Es el reparto de autoridad que la caja ya tiene, y el unico
	// estado en el que la palabra «declarado» es literal.
	private Borde(object superficie, Func<string, string, string, object?> propio)
	{
		this.superficie = superficie;
		this.propio = propio;

		// LA DECLARACION SE PIDE UNA VEZ, AL ARMARSE, y de ahi salen los nombres. Sin esto el
		// sustrato no tendria que ofrecerle al Host -- y peor: aceptaria cualquier nombre sin
		// mirar, que es soltar la puerta por conjunto.
		//
		// `declarar` es SINCRONO a proposito: se pide aca, en un constructor, y una declaracion
		// que hubiera que esperar obligaria a armar el borde en dos tiempos.
		declaracion = Desenvolver(propio("declarar", "", "")) ?? default;

		if (declaracion.ValueKind == JsonValueKind.Object)
		{
			if (declaracion.TryGetProperty("consultas", out JsonElement c))
				foreach (JsonElement x in c.EnumerateArray()) consultas.Add(x.GetString() ?? "");

			if (declaracion.TryGetProperty("vistas", out JsonElement v))
				foreach (JsonElement x in v.EnumerateArray()) vistas.Add(x.GetString() ?? "");
		}
	}

	private readonly JsonElement declaracion;

	// EL SOBRE DE VUELTA DE LA CAJA: `{"ok":true,"dice":…}`. Un borde propio contesta asi, porque
	// esa es la forma que cruza a wasm: TEXTO, no objetos. Aca se abre y se devuelve lo de adentro
	// -- o se tira ruidoso, si dijo que no.
	//
	// Vuelve un `JsonElement` y no un string, y la diferencia importa: devolver el texto haria que
	// el contenido viajara al Host como una CADENA con json adentro, y quien pregunta recibiria
	// comillas en vez de hechos.
	private static JsonElement? Desenvolver(object? crudo)
	{
		if (crudo is not string texto || texto.Length == 0) return null;

		using JsonDocument d = JsonDocument.Parse(texto);
		if (!d.RootElement.TryGetProperty("dice", out JsonElement dice)) return d.RootElement.Clone();

		if (d.RootElement.TryGetProperty("ok", out JsonElement ok) && !ok.GetBoolean())
			throw new InvalidOperationException(dice.ValueKind == JsonValueKind.String ? dice.GetString() : dice.GetRawText());

		return dice.Clone();
	}

	// COMO SE ARMA, Y EL ORDEN ES LA COSTURA DE LA MUDANZA.
	//
	// Primero se le pregunta al modulo si trae su propio borde. Si lo trae, el sustrato no mira
	// adentro de su ensamblado nunca mas: le pasa los verbos y le cree la declaracion. Si no lo
	// trae -- que es el caso de todos los modulos de hoy --, el sustrato le PRESTA este, igual
	// que le presta el motor y el runtime.
	//
	// Asi la migracion es de a un modulo y sin tocar el Host: una institucion que quiera cerrar
	// su propio borde lo empaqueta y a partir de ese dia el sustrato deja de reflexionar sobre
	// ella. Las otras siguen andando.
	internal static Borde Para(object superficie) =>
		Suyo(superficie) is Func<string, string, string, object?> f
			? new Borde(superficie, f)
			: new Borde(superficie);

	// EL BORDE QUE TRAE EL MODULO, si lo trae. Se busca por nombre de tipo y de metodo, que es la
	// misma regla que los marcadores: un tipo publico llamado `Borde` o `ElBorde` con un
	// `Atender(string, string, string)`.
	private static Func<string, string, string, object?>? Suyo(object superficie)
	{
		Type? tipo = superficie.GetType().Assembly.GetTypes()
			.FirstOrDefault(t => t.Name is "Borde" or "ElBorde");

		MethodInfo? atender = tipo?.GetMethod("Atender",
			BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static,
			null, [typeof(string), typeof(string), typeof(string)], null);

		if (tipo is null || atender is null) return null;

		object? instancia = atender.IsStatic ? null : Activator.CreateInstance(tipo, superficie);
		return (verbo, nombre, carga) => atender.Invoke(instancia, [verbo, nombre, carga]);
	}

	private void Registrar(List<string> donde, string nombre, MethodInfo m)
	{
		donde.Add(nombre);
		porNombre[nombre] = m;
	}

	// El nombre publico sale del marcador cuando lo trae; si no, el metodo se llama como se llama.
	private static string Nombre(object marca, MethodInfo m) =>
		(string?)marca.GetType().GetProperty("Name")?.GetValue(marca) is { Length: > 0 } n ? n : m.Name;

	// LO QUE EL MODULO DECLARA, y es lo unico que el sustrato va a saber de el: NOMBRES, no tipos.
	internal string Declaracion()
	{
		if (propio is not null) return declaracion.ValueKind == JsonValueKind.Undefined ? "{}" : declaracion.GetRawText();

		return JsonSerializer.Serialize(new
		{
			consultas,
			vistas,
			entrega = entregar is not null,
			cursor = cursor is not null,
		});
	}

	// DE QUIEN ES EL BORDE QUE SE USO. No es una curiosidad: mientras diga «prestado», el que
	// reflexiona sobre el ensamblado de la institucion sigue siendo Gnothi. Cuando una institucion
	// empaqueta el suyo, esta linea lo dice y se puede auditar sin leer codigo.
	internal string Dueno => propio is not null ? "propio" : "prestado";

	internal IReadOnlyList<string> Consultas => consultas;

	internal IReadOnlyList<string> Vistas => vistas;

	// LA PUERTA, Y SIGUE SIENDO UNA PUERTA POR CONJUNTO. `preguntar` solo llega a las [Query];
	// `consultar` solo llega a las [View]; `entregar` es el unico que escribe. Que el borde sea
	// declarado no afloja eso: lo hace MAS explicito, porque el conjunto se elige ANTES de buscar
	// el nombre -- y buscar el nombre en el conjunto equivocado ya no encuentra nada.
	//
	// Antes esta separacion vivia en `Invocar`, que ante una vista fallada caia a buscar entre las
	// consultas. Aca no hay a donde caer.
	internal async Task<object?> Atender(string verbo, string nombre, string carga)
	{
		// EL BORDE PROPIO CONTESTA, PERO LA PUERTA POR CONJUNTO LA SIGUE SOSTENIENDO EL SUSTRATO.
		// Se comprueba el nombre contra lo que el modulo DECLARO antes de pasarle el verbo: que el
		// modulo sea dueño de su borde no lo hace dueño de a que conjunto pertenece un nombre.
		if (propio is not null)
		{
			Vigilar(verbo, nombre);
			return Desenvolver(await Esperar(propio(verbo, nombre, carga)));
		}

		switch (verbo)
		{
			case "preguntar":
				if (!consultas.Contains(nombre)) throw new ArgumentException($"'{nombre}' no es una consulta de este modulo");
				return await Llamar(porNombre[nombre], carga);

			case "consultar":
				if (!vistas.Contains(nombre)) throw new ArgumentException($"'{nombre}' no es una vista de este modulo");
				return await Llamar(porNombre[nombre], carga);

			case "cursor":
				if (cursor is null) return 0L;
				return await Llamar(cursor, "");

			case "entregar":
				if (entregar is null) return "";
				return await Llamar(entregar, carga);

			default:
				throw new ArgumentException($"verbo desconocido en el borde: {verbo}");
		}
	}

	private void Vigilar(string verbo, string nombre)
	{
		if (verbo == "preguntar" && !consultas.Contains(nombre))
			throw new ArgumentException($"'{nombre}' no es una consulta de este modulo");

		if (verbo == "consultar" && !vistas.Contains(nombre))
			throw new ArgumentException($"'{nombre}' no es una vista de este modulo");
	}

	// EL SOBRE LO ARMA EL SUSTRATO Y TIENE DOS CAMPOS, igual que en la caja:
	//
	//     { "args": { … lo que pregunto quien pregunta … }, "accepts": "…" }
	//
	// `accepts` NO puede venir de adentro de `args`, y ese orden ES el punto: si se leyera del
	// diccionario de quien pregunta, una consulta podria elegir su propio envoltorio, y el formato
	// no es de la pregunta.
	private async Task<object?> Llamar(MethodInfo metodo, string sobre)
	{
		ParameterInfo[] piden = metodo.GetParameters();
		object? crudo;

		if (piden.Length == 0)
		{
			crudo = metodo.Invoke(metodo.IsStatic ? null : superficie, null);
		}
		else
		{
			JsonElement args = default;
			string acepta = "";

			if (sobre.Length > 0)
			{
				using JsonDocument d = JsonDocument.Parse(sobre);
				if (d.RootElement.TryGetProperty("args", out JsonElement a)) args = a.Clone();
				if (d.RootElement.TryGetProperty("accepts", out JsonElement ac)) acepta = ac.GetString() ?? "";
			}

			// El sobre entero para un metodo que pide un solo string y no nombra un argumento: es
			// el caso de `[Deliver]`, que recibe el sobre del cartero tal cual.
			bool sobreEntero = piden.Length == 1 && piden[0].ParameterType == typeof(string)
				&& args.ValueKind != JsonValueKind.Object;

			object?[] valores = sobreEntero
				? [sobre]
				: [.. piden.Select(p => Ligar(p, args, acepta))];

			crudo = metodo.Invoke(metodo.IsStatic ? null : superficie, valores);
		}

		return await Esperar(crudo);
	}

	// UNA CONSULTA PUEDE SER ASINCRONA. En el motor web no es un lujo: el runtime es de un solo
	// hilo, asi que un modulo NO PUEDE bloquear para esperar lo suyo -- `GetResult()` tira
	// «Cannot wait on monitors on this runtime». Devolver la tarea es su unico camino.
	private static async Task<object?> Esperar(object? crudo)
	{
		if (crudo is not Task tarea) return crudo;

		await tarea;
		return tarea.GetType().IsGenericType
			? tarea.GetType().GetProperty("Result")?.GetValue(tarea)
			: null;
	}

	private static object? Ligar(ParameterInfo p, JsonElement args, string acepta)
	{
		// EL PARAMETRO RESERVADO, Y SE MIRA PRIMERO. Una superficie que quiere elegir su formato
		// declara `accepts` y lo llena el SUSTRATO, nunca quien pregunta. Una que no lo declara ni
		// se entera de que existe.
		if (string.Equals(p.Name, "accepts", StringComparison.Ordinal)) return acepta;

		if (args.ValueKind == JsonValueKind.Object
			&& args.TryGetProperty(p.Name ?? "", out JsonElement v)
			&& v.ValueKind != JsonValueKind.Null)
		{
			string texto = v.ValueKind == JsonValueKind.String ? v.GetString() ?? "" : v.GetRawText();
			return p.ParameterType.IsEnum
				? Enum.Parse(p.ParameterType, texto, ignoreCase: true)
				: Convert.ChangeType(texto, p.ParameterType);
		}

		// Falta y tiene omision: se usa. Falta y no tiene: se dice CUAL, que es lo unico que le
		// sirve a quien lee el log.
		return p.HasDefaultValue
			? p.DefaultValue
			: throw new ArgumentException($"falta el parametro '{p.Name}'");
	}
}
