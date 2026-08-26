using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text.Json.Serialization;

// Mock del servidor del concesionario. Happy path, estado en memoria.
//
// El diseno central: un token de device declara QUE puede hacer y SOBRE QUE recurso, y cada
// endpoint declara que scope exige. La app solo consigue "{recurso}.events.read", asi que
// cualquier otro endpoint la rechaza porque el token NO LO TIENE - no por un if contra una ruta.
// Eso es lo que vuelve FALSABLE el invariante "la app no es cliente de los servicios generales del
// concesionario": el senuelo /customer devuelve 403 estructuralmente, no por cortesia.
//
// Las rutas estan parametrizadas por recurso (/{recurso}/{id}/events), asi que este mismo mecanismo
// sirve para el vehiculo de una persona igual que servia para el expediente de un paciente. Para
// este negocio el juego de URLs es:
//
//   GET  /vehicle/{vin}/events?after=N      scope vehicle.events.read
//   GET  /vehicle/{vin}/customer            scope vehicle.customer.read  -> 403, el senuelo
//
// EL SENUELO ES EL EXPEDIENTE DEL CLIENTE, y la eleccion no es decorativa: es exactamente el dato
// que un concesionario tiene y que a esta app no le corresponde. El concesionario sirve EVENTOS del
// vehiculo; quien es la persona, que compro y cuanto debe no entra por ningun lado.

var builder = WebApplication.CreateBuilder(args);

// Mock en localhost: cualquier origen. No usa cookies (el token viaja en el header Authorization),
// asi que AllowAnyOrigin es valido y evita que el primer intento desde el browser muera en un
// preflight.
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
    .AllowAnyOrigin()
    .AllowAnyHeader()
    .AllowAnyMethod()));

var app = builder.Build();

app.UseCors();

var almacen = new AlmacenDeEventos();
var concesiones = new Concesiones();

// ---------------------------------------------------------------- credenciales

// El concesionario le da al DEVICE de la persona un access token corto y un refresh token durable y
// rotatorio. El scope queda acotado a leer los eventos de UN vehiculo concreto: nada de otros
// vehiculos, ni del expediente del cliente, ni de la agenda del taller.
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

// ---------------------------------------------------------------- la narrativa

// EL UNICO verbo de lectura que la app usa. Devuelve solo los eventos posteriores a `after`, que es
// el punto de control que la app tiene journalizado.
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

// EL SENUELO. Existe para demostrar el invariante, no para servir a nadie: exige un scope que
// ningun token de device tiene, asi que responde 403 con la credencial de la app. Es la
// demostracion falsable de que la app no es cliente de los servicios generales del concesionario.
app.MapGet("/{recurso}/{id}/customer", (string recurso, string id, HttpRequest pedido) =>
{
    IResult negado = concesiones.Exigir(pedido, $"{recurso}.customer.read", recurso, id);
    if (negado != null) return negado;

    return Results.Ok(new { nombre = "nunca deberia verse", recurso, id });
});

// ---------------------------------------------------------------- control del mock

// Registra un hecho CONCRETO. Es lo que usa el inyector de eventos de demo: una demo se cuenta con
// valores que uno eligio, porque las fechas de vencimiento dependen de los numeros y de los
// instantes. Accion DEL CONCESIONARIO, no de la app: sin token.
app.MapPost("/{recurso}/{id}/events/hecho", (string recurso, string id, HechoAInyectar pedido) =>
{
    if (pedido is null || Vacio(pedido.Tipo))
        return Results.BadRequest(new { error = "tipo requerido", conocidos = AlmacenDeEventos.TiposConocidos });

    EventoDelConcesionario nuevo = almacen.Agregar(recurso, id, pedido);
    return Results.Ok(new { agregado = nuevo.Id, tipo = nuevo.Tipo, resumen = nuevo.Resumen() });
});

// Expira SOLO los access tokens y deja los refresh vivos. Es el caso que el diseno declara normal
// - "access token corto, refresh token durable/rotatorio" - y sin una forma de provocarlo no hay
// manera de demostrar que la app rota sola en vez de pedir una credencial nueva. Control del mock,
// no del concesionario.
app.MapPost("/device/expirar-access", () =>
{
    int expirados = concesiones.ExpirarAccessTokens();
    return Results.Ok(new { expirados, refreshSiguenVivos = true });
});

app.MapGet("/salud", () => Results.Ok(new { estado = "arriba", vin = AlmacenDeEventos.VinDelDemo }));

app.Run();

static bool Vacio(string s) => string.IsNullOrWhiteSpace(s);

// ---------------------------------------------------------------- credenciales

sealed record Concesion(string DeviceKey, string Recurso, string Id, string Scope,
    string AccessToken, string RefreshToken, DateTime Expira)
{
    internal const int SegundosDeVida = 900;   // access token CORTO, y se hace cumplir

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

    // Adelanta el vencimiento de todos los access tokens sin tocar los refresh. Deja la concesion en
    // el indice para que Exigir la encuentre y responda "expirado" en vez de "desconocido": son dos
    // rechazos distintos y la app reacciona distinto a cada uno.
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

    // Durable y ROTATORIO: el refresh viejo se invalida y sale uno nuevo.
    internal Concesion Rotar(string refreshToken)
    {
        if (!porRefreshToken.TryRemove(refreshToken, out Concesion vieja)) return null;

        porAccessToken.TryRemove(vieja.AccessToken, out _);
        return Otorgar(vieja.DeviceKey, vieja.Recurso, vieja.Id);
    }

    // Devuelve null si la credencial alcanza; si no, el resultado a responder.
    //
    // Tres razones de rechazo, y la tercera es la que importa: el token puede ser perfectamente
    // valido y aun asi no servir, porque su scope no cubre lo que el endpoint pide.
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

// ---------------------------------------------------------------- los eventos

// Un evento de la narrativa del concesionario. `id` y `tipo` son el SOBRE y los lee el mecanismo
// compartido; todo lo demas es el payload de este negocio y lo interpreta el modulo.
//
// UN SOLO TIPO C# PARA LOS CINCO EVENTOS, y eso es del mock y no del contrato: el mock es un
// diccionario en memoria, y darle cinco clases seria darle un modelo que no tiene. Los campos que
// no aplican viajan en su valor neutro y el Interpretar de cada mensaje ya sabe cuales exige.
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
    // El VIN del vehiculo de la demo. Es el mismo que cuenta la consola en su historia de seis
    // meses, para que las dos caratulas hablen del mismo auto.
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

    // LA HISTORIA SEMBRADA, la misma que siembra el inyector. Las fechas son literales a proposito:
    // una demo con fechas relativas al dia de hoy deja de ser reproducible, y los vencimientos
    // dependen de los dias transcurridos. Es la regla de C27, y vale para todo lo que tenga que
    // reproducir una secuencia recibida.
    //
    // QUE CUENTA. El vehiculo se entrega el 1/9/2025 con 12000 unidades y el plan trae cuatro
    // operaciones. Seis meses de odometro, que llegan DESORDENADAS respecto del instante observado.
    // El taller informa un cruce de umbral y una operacion hecha de verdad, que re-ancla su
    // sucesora. El fabricante emite una revision con un intervalo mas corto. Y dos lecturas mas al
    // final, que son las que hacen vencer cosas.
    //
    // Y DOS EVENTOS QUE ESTA APP NO CONSUME, a proposito: un reclamo de garantia y una cita de
    // taller. El concesionario publica su narrativa entera; este modulo modela una rebanada de ella
    // y descarta el resto. Que se vean descartados en la consola es la mitad visible de
    // "la interpretacion es del modulo".
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

    // El hecho que el operador de la demo eligio, con sus valores exactos.
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

// ---------------------------------------------------------------- cuerpos de pedido

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

// El cuerpo que manda el inyector de eventos de demo.
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
