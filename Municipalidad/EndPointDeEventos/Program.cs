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

		public AlmacenDeEventos()
		{
			porContribuyente[Llave(ElRecurso, LaCedulaDelDemo)] = LaNarrativaSembrada();
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
		/// periodo, el salto de 2025 por tarifa en basura y por avaluo en bienes inmuebles, y la mora que
		/// la municipalidad cobro. El encabezado de cada recibo va ANTES de sus lineas y la consulta antes
		/// de sus pendientes, porque una linea sin encabezado se rechaza y no se reintenta.
		/// <para>
		/// NINGUN INSTANTE SALE DE UN RELOJ. Lo que guiona una secuencia recibida no lee la hora: cada
		/// fecha de aca es un literal, para que dos corridas sirvan la misma narrativa.
		/// </para>
		/// </summary>
		private static List<Dictionary<string, object>> LaNarrativaSembrada()
		{
			List<Dictionary<string, object>> narrativa = new();

			Finca(narrativa, "F-1");
			Avaluo(narrativa, "F-1", "2023-01-15T00:00:00Z", 20000000m);
			Avaluo(narrativa, "F-1", "2025-01-20T00:00:00Z", 36000000m);

			Recibo(narrativa, "R-1", "2024-03-31T00:00:00Z", 0m);
			Linea(narrativa, "R-1", "recoleccion-de-basura", "2024.03", 5000m, 0m);
			Linea(narrativa, "R-1", "servicio-policia", "2024.03", 6000m, 0m);
			Linea(narrativa, "R-1", "bienes-inmuebles", "2024.01", 20000m, 0m);

			Recibo(narrativa, "R-2", "2024-06-30T00:00:00Z", 0m);
			Linea(narrativa, "R-2", "recoleccion-de-basura", "2024.06", 15000m, 0m);
			Linea(narrativa, "R-2", "servicio-policia", "2024.06", 18000m, 0m);
			Linea(narrativa, "R-2", "bienes-inmuebles", "2024.02", 20000m, 0m);

			Recibo(narrativa, "R-3", "2025-06-30T00:00:00Z", 0m);
			Linea(narrativa, "R-3", "recoleccion-de-basura", "2025.06", 96000m, 310m);
			Linea(narrativa, "R-3", "servicio-policia", "2025.06", 73200m, 240m);
			Linea(narrativa, "R-3", "bienes-inmuebles", "2025.02", 144000m, 470m);

			Consulta(narrativa, "2025-09-03T00:00:00Z");
			Pendiente(narrativa, "2025-09-03T00:00:00Z", "recoleccion-de-basura", "2025.09", 24000m, 45m);
			Pendiente(narrativa, "2025-09-03T00:00:00Z", "servicio-policia", "2025.09", 18300m, 34m);
			Pendiente(narrativa, "2025-09-03T00:00:00Z", "bienes-inmuebles", "2025.03", 36000m, 0m);

			Cobro(narrativa, "recoleccion-de-basura", "2025.10", 8000m, "2025-10-01T00:00:00Z");

			return narrativa;
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
