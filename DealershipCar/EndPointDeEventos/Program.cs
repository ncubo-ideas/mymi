using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
    .AllowAnyOrigin()
    .AllowAnyHeader()
    .AllowAnyMethod()));

var app = builder.Build();

app.UseCors();

var almacen = new AlmacenDeEventos();
var concesiones = new Concesiones();

app.MapPost("/device/token", (PedidoDeToken pedido) =>
{
    if (string.IsNullOrWhiteSpace(pedido?.DeviceKey)) return Results.BadRequest(new { error = "deviceKey requerido" });

    string recurso = Vacio(pedido.Recurso) ? "vehicle" : pedido.Recurso;
    string id = Vacio(pedido.Id) ? AlmacenDeEventos.VinDelDemo : pedido.Id;

    Concesion concesion = concesiones.Otorgar(pedido.DeviceKey, recurso, id);
    return Results.Ok(concesion.ComoRespuesta());
});

app.MapPost("/device/refresh", (PedidoDeRefresh pedido) =>
{
    if (string.IsNullOrWhiteSpace(pedido?.RefreshToken)) return Results.BadRequest(new { error = "refreshToken requerido" });

    Concesion rotada = concesiones.Rotar(pedido.RefreshToken);
    return rotada == null
        ? Results.Unauthorized()
        : Results.Ok(rotada.ComoRespuesta());
});

app.MapGet("/{recurso}/{id}/events", (string recurso, string id, long? after, HttpRequest pedido) =>
{
    IResult negado = concesiones.Exigir(pedido, $"{recurso}.events.read", recurso, id);
    if (negado != null) return negado;

    long desde = after ?? 0;
    List<EventoDelConcesionario> eventos = almacen.Posteriores(recurso, id, desde);

    return Results.Ok(new
    {
        events = eventos,
        last = almacen.UltimoNumero(recurso, id),
    });
});

app.MapGet("/{recurso}/{id}/customer", (string recurso, string id, HttpRequest pedido) =>
{
    IResult negado = concesiones.Exigir(pedido, $"{recurso}.customer.read", recurso, id);
    if (negado != null) return negado;

    return Results.Ok(new { nombre = "nunca deberia verse", recurso, id });
});

app.MapPost("/{recurso}/{id}/events/hecho", (string recurso, string id, HechoAInyectar pedido) =>
{
    if (pedido is null || Vacio(pedido.Tipo))
        return Results.BadRequest(new { error = "tipo requerido", conocidos = AlmacenDeEventos.TiposConocidos });

    EventoDelConcesionario nuevo = almacen.Agregar(recurso, id, pedido);
    return Results.Ok(new { agregado = nuevo.Id, tipo = nuevo.Tipo, resumen = nuevo.Resumen() });
});

app.MapPost("/device/expirar-access", () =>
{
    int expirados = concesiones.ExpirarAccessTokens();
    return Results.Ok(new { expirados, refreshSiguenVivos = true });
});

app.MapGet("/salud", () => Results.Ok(new { estado = "arriba", vin = AlmacenDeEventos.VinDelDemo }));

app.Run();

static bool Vacio(string s) => string.IsNullOrWhiteSpace(s);

sealed record Concesion(string DeviceKey, string Recurso, string Id, string Scope,
    string AccessToken, string RefreshToken, DateTime Expira)
{
    internal const int SegundosDeVida = 900;

    internal object ComoRespuesta() => new
    {
        accessToken = AccessToken,
        refreshToken = RefreshToken,
        expiresIn = (int)Math.Max(0, (Expira - DateTime.UtcNow).TotalSeconds),
        scope = Scope,
        subject = DeviceKey,
        resource = $"{Recurso}-{Id}",
    };
}

sealed class Concesiones
{
    private readonly ConcurrentDictionary<string, Concesion> porAccessToken = new();
    private readonly ConcurrentDictionary<string, Concesion> porRefreshToken = new();

    internal Concesion Otorgar(string deviceKey, string recurso, string id)
    {
        var concesion = new Concesion(deviceKey, recurso, id, $"{recurso}.events.read",
            Aleatorio("at"), Aleatorio("rt"),
            DateTime.UtcNow.AddSeconds(Concesion.SegundosDeVida));

        porAccessToken[concesion.AccessToken] = concesion;
        porRefreshToken[concesion.RefreshToken] = concesion;
        return concesion;
    }

    internal int ExpirarAccessTokens()
    {
        int expirados = 0;
        foreach (string token in porAccessToken.Keys)
        {
            if (!porAccessToken.TryGetValue(token, out Concesion vieja)) continue;

            var vencida = vieja with { Expira = DateTime.UtcNow.AddSeconds(-1) };
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
            return Results.Json(new { error = "access token expirado; rotalo con /device/refresh" }, statusCode: 401);

        if (concesion.Recurso != recurso || concesion.Id != id)
            return Results.Json(new
            {
                error = "el token esta acotado a otro recurso",
                otorgado = $"{concesion.Recurso}-{concesion.Id}",
                pedido = $"{recurso}-{id}",
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

    private static string Aleatorio(string prefijo) =>
        prefijo + "_" + Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();
}

sealed class EventoDelConcesionario
{
    [JsonPropertyName("id")]                public long Id { get; init; }
    [JsonPropertyName("tipo")]              public string Tipo { get; init; }

    [JsonPropertyName("value")]             public int Value { get; init; }
    [JsonPropertyName("unit")]              public string Unit { get; init; } = "";
    [JsonPropertyName("observedAt")]        public DateTime ObservedAt { get; init; }
    [JsonPropertyName("source")]            public string Source { get; init; } = "";

    [JsonPropertyName("kind")]              public string Kind { get; init; } = "";

    [JsonPropertyName("completedAt")]       public DateTime CompletedAt { get; init; }
    [JsonPropertyName("completionReading")] public int CompletionReading { get; init; }

    [JsonPropertyName("basis")]             public string Basis { get; init; } = "";
    [JsonPropertyName("timeLimitDays")]     public int TimeLimitDays { get; init; }
    [JsonPropertyName("distanceLimit")]     public int DistanceLimit { get; init; }
    [JsonPropertyName("isMandatory")]       public bool IsMandatory { get; init; }
    [JsonPropertyName("isSafety")]          public bool IsSafety { get; init; }
    [JsonPropertyName("anchoredAt")]        public DateTime AnchoredAt { get; init; }
    [JsonPropertyName("anchorReading")]     public int AnchorReading { get; init; }
    [JsonPropertyName("anchorUnit")]        public string AnchorUnit { get; init; } = "";

    [JsonPropertyName("revisionCode")]      public string RevisionCode { get; init; } = "";
    [JsonPropertyName("adoptedAt")]         public DateTime AdoptedAt { get; init; }

    [JsonPropertyName("claim")]             public string Claim { get; init; } = "";

    internal string Resumen() => Tipo switch
    {
        "OdometerReported" => $"{Value} {Unit} observado el {ObservedAt:yyyy-MM-dd} ({Source})",
        "ConditionThresholdCrossed" => $"{Kind} cruzo su umbral el {ObservedAt:yyyy-MM-dd}",
        "OperationCompleted" => $"{Kind} hecho el {CompletedAt:yyyy-MM-dd} con {CompletionReading} {Unit}",
        "PlanOperationSeeded" => $"{Kind} {Basis} {TimeLimitDays}d/{DistanceLimit}{Unit}",
        "PlanRevisionAdopted" => $"revision {RevisionCode} del {AdoptedAt:yyyy-MM-dd}",
        _ => Tipo,
    };
}

sealed class AlmacenDeEventos
{
    internal const string VinDelDemo = "1HGCM82633A004352";

    private const string UnitA = "UnitA";

    private readonly ConcurrentDictionary<string, List<EventoDelConcesionario>> porRecurso = new();
    private readonly Lock candado = new();

    internal static readonly string[] TiposConocidos =
    {
        "OdometerReported", "ConditionThresholdCrossed", "OperationCompleted",
        "PlanOperationSeeded", "PlanRevisionAdopted",
    };

    private List<EventoDelConcesionario> Lista(string recurso, string id) =>
        porRecurso.GetOrAdd($"{recurso}/{id}", _ => Sembrar());

    private static List<EventoDelConcesionario> Sembrar() => new()
    {
        Revision(101, "R1", "2025-09-01"),

        PlanOp(102, "FluidChange",    "TimeOrDistance", 365, 10000, true,  false),
        PlanOp(103, "FilterChange",   "DistanceOnly",     0, 15000, true,  false),
        PlanOp(104, "TimedComponent", "TimeOnly",       550,     0, true,  true),
        PlanOp(105, "Inspection",     "ConditionOnly",    0,     0, true,  false),

        Lectura(106, 13200, "2025-10-01", "Telemetry"),

        new EventoDelConcesionario
        {
            Id = 107, Tipo = "WarrantyClaimFiled", Claim = "W-9912",
            ObservedAt = new DateTime(2025, 11, 15),
        },

        Lectura(108, 16000, "2025-12-01", "Telemetry"),
        Lectura(109, 14500, "2025-11-01", "HumanEntry"),
        Lectura(110, 17300, "2026-01-01", "Telemetry"),
        Lectura(111, 20000, "2026-03-01", "Telemetry"),
        Lectura(112, 18600, "2026-02-01", "CustomerDeclared"),

        Cruce(113, "Inspection", "2026-04-01"),

        Completacion(114, "FluidChange", "2026-03-05", 20000),

        Revision(115, "R2", "2026-06-01"),
        PlanOpAnclada(116, "FluidChange", "TimeOrDistance", 300, 8000, true, false, "2026-06-01", 20000),

        Lectura(117, 27000, "2026-07-01", "Telemetry"),
        Lectura(118, 29500, "2026-08-15", "Telemetry"),

        new EventoDelConcesionario
        {
            Id = 119, Tipo = "AppointmentScheduled",
            ObservedAt = new DateTime(2026, 9, 10),
        },
    };

    private static EventoDelConcesionario Lectura(long id, int valor, string observado, string fuente) =>
        new()
        {
            Id = id, Tipo = "OdometerReported",
            Value = valor, Unit = UnitA, ObservedAt = DateTime.Parse(observado), Source = fuente,
        };

    private static EventoDelConcesionario Cruce(long id, string kind, string observado) =>
        new()
        {
            Id = id, Tipo = "ConditionThresholdCrossed",
            Kind = kind, ObservedAt = DateTime.Parse(observado),
        };

    private static EventoDelConcesionario Completacion(long id, string kind, string hecho, int lectura) =>
        new()
        {
            Id = id, Tipo = "OperationCompleted",
            Kind = kind, CompletedAt = DateTime.Parse(hecho), CompletionReading = lectura, Unit = UnitA,
        };

    private static EventoDelConcesionario Revision(long id, string codigo, string adoptada) =>
        new()
        {
            Id = id, Tipo = "PlanRevisionAdopted",
            RevisionCode = codigo, AdoptedAt = DateTime.Parse(adoptada),
        };

    private static EventoDelConcesionario PlanOp(
        long id, string kind, string basis, int dias, int distancia, bool obligatoria, bool seguridad) =>
        PlanOpAnclada(id, kind, basis, dias, distancia, obligatoria, seguridad, "2025-09-01", 12000);

    private static EventoDelConcesionario PlanOpAnclada(
        long id, string kind, string basis, int dias, int distancia, bool obligatoria, bool seguridad,
        string anclada, int lecturaDelAncla) =>
        new()
        {
            Id = id, Tipo = "PlanOperationSeeded",
            Kind = kind, Basis = basis, TimeLimitDays = dias, DistanceLimit = distancia, Unit = UnitA,
            IsMandatory = obligatoria, IsSafety = seguridad,
            AnchoredAt = DateTime.Parse(anclada), AnchorReading = lecturaDelAncla, AnchorUnit = UnitA,
        };

    internal List<EventoDelConcesionario> Posteriores(string recurso, string id, long after)
    {
        List<EventoDelConcesionario> lista = Lista(recurso, id);
        lock (candado)
            return lista.Where(e => e.Id > after).OrderBy(e => e.Id).ToList();
    }

    internal long UltimoNumero(string recurso, string id)
    {
        List<EventoDelConcesionario> lista = Lista(recurso, id);
        lock (candado)
            return lista.Count == 0 ? 0 : lista.Max(e => e.Id);
    }

    internal EventoDelConcesionario Agregar(string recurso, string id, HechoAInyectar pedido)
    {
        List<EventoDelConcesionario> lista = Lista(recurso, id);

        lock (candado)
        {
            long siguiente = (lista.Count == 0 ? 100 : lista.Max(e => e.Id)) + 1;
            var evento = new EventoDelConcesionario
            {
                Id = siguiente,
                Tipo = pedido.Tipo,
                Value = pedido.Value,
                Unit = Vacio(pedido.Unit) ? UnitA : pedido.Unit,
                ObservedAt = pedido.ObservedAt,
                Source = pedido.Source ?? "",
                Kind = pedido.Kind ?? "",
                CompletedAt = pedido.CompletedAt,
                CompletionReading = pedido.CompletionReading,
                Basis = pedido.Basis ?? "",
                TimeLimitDays = pedido.TimeLimitDays,
                DistanceLimit = pedido.DistanceLimit,
                IsMandatory = pedido.IsMandatory,
                IsSafety = pedido.IsSafety,
                AnchoredAt = pedido.AnchoredAt,
                AnchorReading = pedido.AnchorReading,
                AnchorUnit = Vacio(pedido.AnchorUnit) ? UnitA : pedido.AnchorUnit,
                RevisionCode = pedido.RevisionCode ?? "",
                AdoptedAt = pedido.AdoptedAt,
            };

            lista.Add(evento);
            return evento;
        }
    }

    private static bool Vacio(string s) => string.IsNullOrWhiteSpace(s);
}

sealed class PedidoDeToken
{
    [JsonPropertyName("deviceKey")] public string DeviceKey { get; set; }
    [JsonPropertyName("recurso")]   public string Recurso { get; set; }
    [JsonPropertyName("id")]        public string Id { get; set; }
}

sealed class PedidoDeRefresh
{
    [JsonPropertyName("refreshToken")] public string RefreshToken { get; set; }
}

sealed class HechoAInyectar
{
    [JsonPropertyName("tipo")]              public string Tipo { get; set; }

    [JsonPropertyName("value")]             public int Value { get; set; }
    [JsonPropertyName("unit")]              public string Unit { get; set; }
    [JsonPropertyName("observedAt")]        public DateTime ObservedAt { get; set; }
    [JsonPropertyName("source")]            public string Source { get; set; }

    [JsonPropertyName("kind")]              public string Kind { get; set; }

    [JsonPropertyName("completedAt")]       public DateTime CompletedAt { get; set; }
    [JsonPropertyName("completionReading")] public int CompletionReading { get; set; }

    [JsonPropertyName("basis")]             public string Basis { get; set; }
    [JsonPropertyName("timeLimitDays")]     public int TimeLimitDays { get; set; }
    [JsonPropertyName("distanceLimit")]     public int DistanceLimit { get; set; }
    [JsonPropertyName("isMandatory")]       public bool IsMandatory { get; set; }
    [JsonPropertyName("isSafety")]          public bool IsSafety { get; set; }
    [JsonPropertyName("anchoredAt")]        public DateTime AnchoredAt { get; set; }
    [JsonPropertyName("anchorReading")]     public int AnchorReading { get; set; }
    [JsonPropertyName("anchorUnit")]        public string AnchorUnit { get; set; }

    [JsonPropertyName("revisionCode")]      public string RevisionCode { get; set; }
    [JsonPropertyName("adoptedAt")]         public DateTime AdoptedAt { get; set; }
}
