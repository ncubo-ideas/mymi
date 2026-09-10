using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text.Json;

namespace Municipalidad.EventosMock
{
	/// <summary>
	/// El endpoint de eventos de la municipalidad, simulado. Sirve la narrativa de UN contribuyente a
	/// partir de un punto, y nada mas: no hay endpoint de vistas ni de reportes, porque la app no es un
	/// cliente de los servicios generales del portal.
	/// </summary>
	public static class Program
	{
		public static void Main(string[] args)
		{
			WebApplicationBuilder constructor = WebApplication.CreateBuilder(args);

			constructor.Services.AddCors(o => o.AddDefaultPolicy(p => p
				.AllowAnyOrigin()
				.AllowAnyHeader()
				.AllowAnyMethod()));

			WebApplication app = constructor.Build();

			app.UseCors();

			AlmacenDeEventos almacen = new AlmacenDeEventos();
			Concesiones concesiones = new Concesiones();

			app.MapPost("/device/token", (PedidoDeToken pedido) =>
			{
				if (string.IsNullOrWhiteSpace(pedido?.DeviceKey))
					return Results.BadRequest(new { error = "deviceKey requerido" });

				string recurso = Vacio(pedido.Recurso) ? AlmacenDeEventos.ElRecurso : pedido.Recurso;
				string id = Vacio(pedido.Id) ? AlmacenDeEventos.LaCedulaDelDemo : pedido.Id;

				return Results.Ok(concesiones.Otorgar(pedido.DeviceKey, recurso, id).ComoRespuesta());
			});

			app.MapPost("/device/refresh", (PedidoDeRefresh pedido) =>
			{
				if (string.IsNullOrWhiteSpace(pedido?.RefreshToken))
					return Results.BadRequest(new { error = "refreshToken requerido" });

				Concesion rotada = concesiones.Rotar(pedido.RefreshToken);

				return rotada == null ? Results.Unauthorized() : Results.Ok(rotada.ComoRespuesta());
			});

			app.MapGet("/{recurso}/{id}/events", (string recurso, string id, long? after, HttpRequest pedido) =>
			{
				IResult negado = concesiones.Exigir(pedido, recurso + ".events.read", recurso, id);
				if (negado != null) return negado;

				return Results.Ok(new
				{
					events = almacen.Posteriores(recurso, id, after ?? 0),
					last = almacen.UltimoNumero(recurso, id),
				});
			});

			app.MapGet("/{recurso}/{id}/perfil", (string recurso, string id, HttpRequest pedido) =>
			{
				IResult negado = concesiones.Exigir(pedido, recurso + ".perfil.read", recurso, id);
				if (negado != null) return negado;

				return Results.Ok(new { nombre = "nunca deberia verse", recurso, id });
			});

			app.MapPost("/{recurso}/{id}/events/hecho", (string recurso, string id, HechoAInyectar pedido) =>
			{
				if (pedido == null || Vacio(pedido.Tipo))
					return Results.BadRequest(new
					{
						error = "tipo requerido",
						conocidos = AlmacenDeEventos.LosTiposQueLaMunicipalidadPublica,
					});

				if (Array.IndexOf(AlmacenDeEventos.LosTiposQueLaMunicipalidadPublica, pedido.Tipo) < 0)
					return Results.BadRequest(new
					{
						error = "la municipalidad no publica ese tipo de hecho",
						pedido = pedido.Tipo,
						conocidos = AlmacenDeEventos.LosTiposQueLaMunicipalidadPublica,
					});

				return Results.Ok(almacen.Agregar(recurso, id, pedido));
			});

			app.MapPost("/device/expirar-access", () =>
				Results.Ok(new { expirados = concesiones.ExpirarAccessTokens(), refreshSiguenVivos = true }));

			app.MapGet("/salud", () => Results.Ok(new
			{
				estado = "arriba",
				recurso = AlmacenDeEventos.ElRecurso,
				cedula = AlmacenDeEventos.LaCedulaDelDemo,
				eventos = almacen.UltimoNumero(AlmacenDeEventos.ElRecurso, AlmacenDeEventos.LaCedulaDelDemo),
				narrativas = almacen.LasNarrativasSembradas(),
			}));

			app.Run();
		}

		private static bool Vacio(string texto)
		{
			return string.IsNullOrWhiteSpace(texto);
		}
	}

	/// <summary>
	/// Un token acotado a leer los eventos de UN contribuyente. Access token corto y refresh durable y
	/// rotatorio, que es lo que le da al bucle de refresh algo que hacer cuando el proveedor lo rechaza.
	/// </summary>
	public sealed record Concesion(string DeviceKey, string Recurso, string Id, string Scope,
		string AccessToken, string RefreshToken, DateTime Expira)
	{
		internal const int SegundosDeVida = 900;

		internal object ComoRespuesta()
		{
			return new
			{
				accessToken = AccessToken,
				refreshToken = RefreshToken,
				expiresIn = (int)Math.Max(0, (Expira - DateTime.UtcNow).TotalSeconds),
				scope = Scope,
				subject = DeviceKey,
				resource = Recurso + "-" + Id,
			};
		}
	}

	/// <summary>
	/// Las concesiones vivas. Exige el bearer, el recurso y el scope en cada lectura: un token de otro
	/// contribuyente no sirve para leer este, y eso es lo que hace del alcance una propiedad y no una
	/// promesa.
	/// </summary>
	public sealed class Concesiones
	{
		private readonly ConcurrentDictionary<string, Concesion> porAccessToken = new();
		private readonly ConcurrentDictionary<string, Concesion> porRefreshToken = new();

		internal Concesion Otorgar(string deviceKey, string recurso, string id)
		{
			Concesion concesion = new Concesion(
				deviceKey, recurso, id, recurso + ".events.read",
				Aleatorio("at"), Aleatorio("rt"),
				DateTime.UtcNow.AddSeconds(Concesion.SegundosDeVida));

			porAccessToken[concesion.AccessToken] = concesion;
			porRefreshToken[concesion.RefreshToken] = concesion;

			return concesion;
		}

		/// <summary>
		/// Vence todos los access token dejando vivos los refresh. Es el unico modo de provocar a mano lo
		/// que en produccion pasa solo, y sin el la rotacion del bucle no se puede ejercitar.
		/// </summary>
		internal int ExpirarAccessTokens()
		{
			int expirados = 0;

			foreach (string token in porAccessToken.Keys)
			{
				if (!porAccessToken.TryGetValue(token, out Concesion vieja)) continue;

				Concesion vencida = vieja with { Expira = DateTime.UtcNow.AddSeconds(-1) };
				porAccessToken[token] = vencida;
				porRefreshToken[vencida.RefreshToken] = vencida;
				expirados++;
			}

			return expirados;
		}

		internal Concesion Rotar(string refreshToken)
		{
			if (!porRefreshToken.TryRemove(refreshToken, out Concesion vieja)) return null;

			porAccessToken.TryRemove(vieja.AccessToken, out _);

			return Otorgar(vieja.DeviceKey, vieja.Recurso, vieja.Id);
		}

		internal IResult Exigir(HttpRequest pedido, string scopeExigido, string recurso, string id)
		{
			string presentado = Bearer(pedido);

			if (presentado == null)
				return Results.Json(new { error = "falta el header Authorization: Bearer" }, statusCode: 401);

			if (!porAccessToken.TryGetValue(presentado, out Concesion concesion))
				return Results.Json(new { error = "access token desconocido" }, statusCode: 401);

			if (concesion.Expira <= DateTime.UtcNow)
				return Results.Json(
					new { error = "access token expirado; rotalo con /device/refresh" }, statusCode: 401);

			if (concesion.Recurso != recurso || concesion.Id != id)
				return Results.Json(new
				{
					error = "el token esta acotado a otro contribuyente",
					otorgado = concesion.Recurso + "-" + concesion.Id,
					pedido = recurso + "-" + id,
				}, statusCode: 403);

			if (concesion.Scope != scopeExigido)
				return Results.Json(new
				{
					error = "el scope del token no cubre esta operacion",
					scopeDelToken = concesion.Scope,
					scopeExigido,
				}, statusCode: 403);

			return null;
		}

		private static string Bearer(HttpRequest pedido)
		{
			string encabezado = pedido.Headers.Authorization.ToString();

			return encabezado.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
				? encabezado["Bearer ".Length..].Trim()
				: null;
		}

		private static string Aleatorio(string prefijo)
		{
			return prefijo + "_"
				+ Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();
		}
	}

	/// <summary>
	/// La narrativa de cada contribuyente, en memoria. Cada hecho es un objeto plano con su numero y su
	/// tipo al lado de sus campos, que es la forma que el sobre del mecanismo lee.
	/// </summary>
	public sealed class AlmacenDeEventos
	{
		internal const string ElRecurso = "contribuyente";

		/// <summary>
		/// La cedula del demo. Es un marcador y no una cedula: el portal consolida por cedula, asi que la
		/// llave del feed existe, pero inventar una con forma de cedula real le pondria el nombre de
		/// alguien a estos datos.
		/// </summary>
		internal const string LaCedulaDelDemo = "CEDULA-DEMO-1";

		/// <summary>
		/// La cedula de la narrativa alterna cuyo cotejo sale limpio. Es una segunda cedula y no un
		/// segundo momento de la primera: el cotejo tiene UN estado por cuenta, asi que la misma cuenta
		/// no puede a la vez dejar periodos sin comprobante y no dejar ninguno.
		/// </summary>
		internal const string LaCedulaConTodoComprobado = "CEDULA-DEMO-2";

		/// <summary>
		/// La cedula de la narrativa alterna en que la app nunca registro una consulta al portal, asi
		/// que no hay con que cotejar. No poder cotejar no es estar en orden, y esa distincion no se
		/// puede ver en una cuenta que si tiene consulta.
		/// </summary>
		internal const string LaCedulaSinConsulta = "CEDULA-DEMO-3";

		/// <summary>
		/// Lo que el portal deja pendiente de basura en la narrativa principal: un mes a la tarifa
		/// nueva, que no alcanza hasta el ultimo periodo pagado y deja julio y agosto de 2026 pagados
		/// sin comprobante.
		/// </summary>
		private const decimal ElPendienteDeBasuraQueDejaDosMesesSinComprobante = 8000m;

		/// <summary>
		/// Lo mismo en la alterna: tres meses a la tarifa nueva, que llegan justo hasta el ultimo
		/// periodo pagado y no dejan ninguno sin comprobante.
		/// </summary>
		private const decimal ElPendienteDeBasuraQueNoDejaNinguno = 24000m;

		/// <summary>
		/// Los siete tipos que la municipalidad publica, uno por comando de la puerta que ella manda. El
		/// AVALUO esta porque lo fija la institucion y es el valor con que cobra; la reserva y el valor
		/// DECLARADO no estan y no faltan, porque los aporta la persona.
		/// </summary>
		internal static readonly string[] LosTiposQueLaMunicipalidadPublica =
		{
			"FincaIncorporada", "AvaluoFijado", "CobroPuesto", "PagoRegistrado",
			"LineaDePagoRegistrada", "ConsultaTomada", "PendienteRegistrado",
		};

		private readonly ConcurrentDictionary<string, List<Dictionary<string, object>>> porContribuyente =
			new();

		private readonly List<string> cedulasSembradas = new List<string>();

		public AlmacenDeEventos()
		{
			Sembrar(LaCedulaDelDemo, LaNarrativaSembrada());
			Sembrar(LaCedulaConTodoComprobado, LaNarrativaConTodoComprobado());
			Sembrar(LaCedulaSinConsulta, LaNarrativaSinConsulta());
		}

		/// <summary>
		/// Una fila por narrativa sembrada, con su cedula y cuantos hechos tiene. Sale de la misma lista
		/// que el constructor lleno, asi que no puede quedar desalineada con lo que el feed sirve.
		/// </summary>
		internal IReadOnlyList<object> LasNarrativasSembradas()
		{
			List<object> filas = new List<object>();

			foreach (string cedula in cedulasSembradas)
				filas.Add(new { cedula, eventos = UltimoNumero(ElRecurso, cedula) });

			return filas;
		}

		/// <summary>Deja una narrativa bajo su cedula y anota la cedula, que es lo que /salud enumera.</summary>
		private void Sembrar(string cedula, List<Dictionary<string, object>> narrativa)
		{
			porContribuyente[Llave(ElRecurso, cedula)] = narrativa;
			cedulasSembradas.Add(cedula);
		}

		internal IReadOnlyList<Dictionary<string, object>> Posteriores(
			string recurso, string id, long despuesDe)
		{
			List<Dictionary<string, object>> narrativa = De(recurso, id);
			List<Dictionary<string, object>> posteriores = new();

			lock (narrativa)
				foreach (Dictionary<string, object> hecho in narrativa)
					if (Convert.ToInt64(hecho["id"]) > despuesDe) posteriores.Add(hecho);

			return posteriores;
		}

		internal long UltimoNumero(string recurso, string id)
		{
			List<Dictionary<string, object>> narrativa = De(recurso, id);

			lock (narrativa)
				return narrativa.Count == 0 ? 0 : Convert.ToInt64(narrativa[^1]["id"]);
		}

		internal object Agregar(string recurso, string id, HechoAInyectar pedido)
		{
			List<Dictionary<string, object>> narrativa = De(recurso, id);

			lock (narrativa)
			{
				Dictionary<string, object> hecho = new()
				{
					["id"] = narrativa.Count == 0 ? 1L : Convert.ToInt64(narrativa[^1]["id"]) + 1,
					["tipo"] = pedido.Tipo,
				};

				if (pedido.Campos.ValueKind == JsonValueKind.Object)
					foreach (JsonProperty campo in pedido.Campos.EnumerateObject())
						if (campo.Name != "id" && campo.Name != "tipo")
							hecho[campo.Name] = ComoValor(campo.Value);

				narrativa.Add(hecho);

				return new { agregado = hecho["id"], tipo = pedido.Tipo };
			}
		}

		private static object ComoValor(JsonElement valor)
		{
			switch (valor.ValueKind)
			{
				case JsonValueKind.String: return valor.GetString();
				case JsonValueKind.Number: return valor.GetDecimal();
				case JsonValueKind.True: return true;
				case JsonValueKind.False: return false;
				default: return valor.GetRawText();
			}
		}

		private List<Dictionary<string, object>> De(string recurso, string id)
		{
			return porContribuyente.GetOrAdd(Llave(recurso, id), _ => new List<Dictionary<string, object>>());
		}

		private static string Llave(string recurso, string id)
		{
			return recurso + "/" + id;
		}

		/// <summary>
		/// La historia del demo, que es la misma que `medir` corre por la puerta: los dos alfabetos del
		/// periodo, el salto de 2026 por tarifa en basura y por avaluo en bienes inmuebles, la mora que
		/// la municipalidad cobro, el pendiente de basura que cubre un solo mes y deja julio y agosto
		/// de 2026 pagados sin comprobante, el cambio de limpieza de calles que queda encima del ruido y
		/// debajo del umbral, y el mes de limpieza que la municipalidad puso al cobro, nadie pago y ya
		/// vencio.
		/// <para>
		/// LAS TRES NARRATIVAS SE COMPONEN DE LAS MISMAS ESCENAS Y NINGUN LITERAL ESTA ESCRITO DOS
		/// VECES. Lo que una alterna cambia se lee en la escena que le falta o en el monto que le pasa.
		/// </para>
		/// <para>
		/// NINGUN INSTANTE SALE DE UN RELOJ. Lo que guiona una secuencia recibida no lee la hora: cada
		/// fecha de aca es un literal, para que dos corridas sirvan la misma narrativa.
		/// </para>
		/// </summary>
		private static List<Dictionary<string, object>> LaNarrativaSembrada()
		{
			List<Dictionary<string, object>> narrativa = new();

			LaFincaConSusAvaluos(narrativa);
			LosTresRecibos(narrativa);
			ElMesDeLimpiezaQueVencio(narrativa);
			LaConsultaConSusPendientes(narrativa, ElPendienteDeBasuraQueDejaDosMesesSinComprobante);
			ElMesDeBasuraPuestoAlCobro(narrativa);

			return narrativa;
		}

		/// <summary>
		/// LA ALTERNA EN QUE EL COTEJO SALE LIMPIO. La misma historia con un pendiente de basura de tres
		/// meses en vez de uno: llega hasta el ultimo periodo pagado, asi que todo lo que el portal da
		/// por pagado tiene comprobante en la app. Es la unica diferencia con la principal.
		/// </summary>
		private static List<Dictionary<string, object>> LaNarrativaConTodoComprobado()
		{
			List<Dictionary<string, object>> narrativa = new();

			LaFincaConSusAvaluos(narrativa);
			LosTresRecibos(narrativa);
			ElMesDeLimpiezaQueVencio(narrativa);
			LaConsultaConSusPendientes(narrativa, ElPendienteDeBasuraQueNoDejaNinguno);
			ElMesDeBasuraPuestoAlCobro(narrativa);

			return narrativa;
		}

		/// <summary>
		/// LA ALTERNA SIN CONSULTA. La misma historia sin la escena de la consulta, asi que la app tiene
		/// recibos y cobros de la municipalidad y ninguna foto del portal contra la cual cotejarlos. Los
		/// pendientes se van con ella: un pendiente cuya consulta no esta se rechaza.
		/// </summary>
		private static List<Dictionary<string, object>> LaNarrativaSinConsulta()
		{
			List<Dictionary<string, object>> narrativa = new();

			LaFincaConSusAvaluos(narrativa);
			LosTresRecibos(narrativa);
			ElMesDeLimpiezaQueVencio(narrativa);
			ElMesDeBasuraPuestoAlCobro(narrativa);

			return narrativa;
		}

		/// <summary>La finca y los dos avaluos con que la municipalidad la cobro, el segundo un 80 % mas
		/// alto que el primero.</summary>
		private static void LaFincaConSusAvaluos(List<Dictionary<string, object>> narrativa)
		{
			Finca(narrativa, "F-1");
			Avaluo(narrativa, "F-1", "2024-01-15T00:00:00Z", 20000000m);
			Avaluo(narrativa, "F-1", "2026-01-20T00:00:00Z", 36000000m);
		}

		/// <summary>
		/// Los tres comprobantes con sus lineas. EL ENCABEZADO DE CADA RECIBO VA ANTES DE SUS LINEAS:
		/// una linea sin encabezado se rechaza y no se reintenta.
		/// </summary>
		private static void LosTresRecibos(List<Dictionary<string, object>> narrativa)
		{
			Recibo(narrativa, "R-1", "2025-03-31T00:00:00Z", 0m);
			Linea(narrativa, "R-1", "recoleccion-de-basura", "2025.03", 5000m, 0m);
			Linea(narrativa, "R-1", "limpieza-de-calles", "2025.03", 2500m, 0m);
			Linea(narrativa, "R-1", "servicio-policia", "2025.03", 6000m, 0m);
			Linea(narrativa, "R-1", "bienes-inmuebles", "2025.01", 20000m, 0m);

			Recibo(narrativa, "R-2", "2025-06-30T00:00:00Z", 0m);
			Linea(narrativa, "R-2", "recoleccion-de-basura", "2025.06", 15000m, 0m);
			Linea(narrativa, "R-2", "limpieza-de-calles", "2025.06", 8100m, 0m);
			Linea(narrativa, "R-2", "servicio-policia", "2025.06", 18000m, 0m);
			Linea(narrativa, "R-2", "bienes-inmuebles", "2025.02", 20000m, 0m);

			Recibo(narrativa, "R-3", "2026-06-30T00:00:00Z", 0m);
			Linea(narrativa, "R-3", "recoleccion-de-basura", "2026.06", 96000m, 310m);
			Linea(narrativa, "R-3", "servicio-policia", "2026.06", 73200m, 240m);
			Linea(narrativa, "R-3", "bienes-inmuebles", "2026.02", 144000m, 470m);
		}

		/// <summary>El mes de limpieza que la municipalidad puso al cobro, nadie pago y ya vencio.</summary>
		private static void ElMesDeLimpiezaQueVencio(List<Dictionary<string, object>> narrativa)
		{
			Cobro(narrativa, "limpieza-de-calles", "2026.07", 2700m, "2026-07-01T00:00:00Z");
		}

		/// <summary>
		/// La foto del portal con lo que dejo pendiente. LA CONSULTA VA ANTES DE SUS PENDIENTES: un
		/// pendiente cuya consulta no esta se rechaza y no se reintenta. Lo unico que cambia entre la
		/// narrativa principal y la alterna es el monto de basura.
		/// </summary>
		private static void LaConsultaConSusPendientes(
			List<Dictionary<string, object>> narrativa, decimal pendienteDeBasura)
		{
			Consulta(narrativa, "2026-09-03T00:00:00Z");
			Pendiente(narrativa, "2026-09-03T00:00:00Z", "recoleccion-de-basura", "2026.09",
				pendienteDeBasura, 45m);
			Pendiente(narrativa, "2026-09-03T00:00:00Z", "servicio-policia", "2026.09", 18300m, 34m);
			Pendiente(narrativa, "2026-09-03T00:00:00Z", "bienes-inmuebles", "2026.03", 36000m, 0m);
		}

		/// <summary>El mes de basura que la municipalidad puso al cobro despues de la consulta.</summary>
		private static void ElMesDeBasuraPuestoAlCobro(List<Dictionary<string, object>> narrativa)
		{
			Cobro(narrativa, "recoleccion-de-basura", "2026.10", 8000m, "2026-10-01T00:00:00Z");
		}

		private static void Finca(List<Dictionary<string, object>> narrativa, string numeroDeFinca)
		{
			Poner(narrativa, "FincaIncorporada", new Dictionary<string, object>
			{
				["numeroDeFinca"] = numeroDeFinca,
			});
		}

		private static void Avaluo(List<Dictionary<string, object>> narrativa, string numeroDeFinca,
			string fechaDelAvaluo, decimal valor)
		{
			Poner(narrativa, "AvaluoFijado", new Dictionary<string, object>
			{
				["numeroDeFinca"] = numeroDeFinca,
				["fechaDelAvaluo"] = fechaDelAvaluo,
				["valor"] = valor,
			});
		}

		private static void Recibo(List<Dictionary<string, object>> narrativa, string numeroDeRecibo,
			string fechaDelPago, decimal descuento)
		{
			Poner(narrativa, "PagoRegistrado", new Dictionary<string, object>
			{
				["numeroDeRecibo"] = numeroDeRecibo,
				["fechaDelPago"] = fechaDelPago,
				["descuento"] = descuento,
			});
		}

		private static void Linea(List<Dictionary<string, object>> narrativa, string numeroDeRecibo,
			string nombreDelRubro, string periodo, decimal monto, decimal intereses)
		{
			Poner(narrativa, "LineaDePagoRegistrada", new Dictionary<string, object>
			{
				["numeroDeRecibo"] = numeroDeRecibo,
				["nombreDelRubro"] = nombreDelRubro,
				["periodo"] = periodo,
				["monto"] = monto,
				["intereses"] = intereses,
			});
		}

		private static void Consulta(List<Dictionary<string, object>> narrativa, string fechaDeLaConsulta)
		{
			Poner(narrativa, "ConsultaTomada", new Dictionary<string, object>
			{
				["fechaDeLaConsulta"] = fechaDeLaConsulta,
			});
		}

		private static void Pendiente(List<Dictionary<string, object>> narrativa, string fechaDeLaConsulta,
			string nombreDelRubro, string periodo, decimal monto, decimal intereses)
		{
			Poner(narrativa, "PendienteRegistrado", new Dictionary<string, object>
			{
				["fechaDeLaConsulta"] = fechaDeLaConsulta,
				["nombreDelRubro"] = nombreDelRubro,
				["periodo"] = periodo,
				["monto"] = monto,
				["intereses"] = intereses,
			});
		}

		private static void Cobro(List<Dictionary<string, object>> narrativa, string nombreDelRubro,
			string periodo, decimal monto, string puestoEl)
		{
			Poner(narrativa, "CobroPuesto", new Dictionary<string, object>
			{
				["nombreDelRubro"] = nombreDelRubro,
				["periodo"] = periodo,
				["monto"] = monto,
				["puestoEl"] = puestoEl,
			});
		}

		private static void Poner(List<Dictionary<string, object>> narrativa, string tipo,
			Dictionary<string, object> campos)
		{
			Dictionary<string, object> hecho = new()
			{
				["id"] = (long)(narrativa.Count + 1),
				["tipo"] = tipo,
			};

			foreach (KeyValuePair<string, object> campo in campos) hecho[campo.Key] = campo.Value;

			narrativa.Add(hecho);
		}
	}

	/// <summary>Lo que un dispositivo manda para que le den credencial.</summary>
	public sealed class PedidoDeToken
	{
		public string DeviceKey { get; set; }

		public string Recurso { get; set; }

		public string Id { get; set; }
	}

	/// <summary>Lo que manda para rotarla.</summary>
	public sealed class PedidoDeRefresh
	{
		public string RefreshToken { get; set; }
	}

	/// <summary>
	/// Un hecho que el operador inyecta. Los campos viajan sin interpretar: el mock no sabe que significa
	/// ninguno, y esa ignorancia es a proposito -- el que decide si un hecho se puede decir es el
	/// interprete de la app, y un mock que validara campos taparia justo lo que hay que ver.
	/// </summary>
	public sealed class HechoAInyectar
	{
		public string Tipo { get; set; }

		public JsonElement Campos { get; set; }
	}
}
