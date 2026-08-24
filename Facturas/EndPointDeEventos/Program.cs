using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text.Json.Serialization;

// Mock del servidor del proveedor. Happy path, estado en memoria.
//
// El diseno central: un token de device declara QUE puede hacer y SOBRE QUE
// recurso, y cada endpoint declara que scope exige. La app solo consigue
// "{recurso}.events.read", asi que cualquier otro endpoint la rechaza porque el
// token NO LO TIENE - no por un if contra una ruta. Eso es lo que vuelve falsable
// el invariante "la app no es cliente de los servicios generales del proveedor":
// el enduelo /perfil devuelve 403 estructuralmente, no por cortesia.
//
// Las rutas estan parametrizadas por recurso (/{recurso}/{id}/events) para que
// APP2 (hipotecas) sea otro juego de URLs sobre el mismo mecanismo, tal como en el
// diagrama: GET /loans/78341/events?after=800.

var builder = WebApplication.CreateBuilder(args);

// Mock en localhost: cualquier origen. No usa cookies (el token viaja en el header
// Authorization), asi que AllowAnyOrigin es valido y evita que el primer intento
// desde el browser muera en un preflight.
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
    .AllowAnyOrigin()
    .AllowAnyHeader()
    .AllowAnyMethod()));

var app = builder.Build();
app.UseCors();

var almacen = new AlmacenDeEventos();
var concesiones = new Concesiones();

// ---------------------------------------------------------------- credenciales

// El proveedor le da al DEVICE del usuario un access token corto y un refresh
// token durable y rotatorio. El scope queda acotado a leer los eventos de UN
// recurso concreto: nada de transfer, payment, profile.write ni account.read.
app.MapPost("/device/token", (PedidoDeToken pedido) =>
{
    if (string.IsNullOrWhiteSpace(pedido?.DeviceKey)) return Results.BadRequest(new { error = "deviceKey requerido" });

    string recurso = Vacio(pedido.Recurso) ? "clientes" : pedido.Recurso;
    string id = Vacio(pedido.Id) ? "1" : pedido.Id;

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

// EL UNICO verbo de lectura que la app usa. Devuelve solo los eventos posteriores
// a `after`, que es el checkpoint que la app tiene journalizado.
app.MapGet("/{recurso}/{id}/events", (string recurso, string id, long? after, HttpRequest pedido) =>
{
    IResult negado = concesiones.Exigir(pedido, $"{recurso}.events.read", recurso, id);
    if (negado != null) return negado;

    long desde = after ?? 0;
    List<EventoDeFactura> eventos = almacen.Posteriores(recurso, id, desde);

    return Results.Ok(new
    {
        events = eventos,
        last = almacen.UltimoNumero(recurso, id),
    });
});

// EL SENUELO. Existe para demostrar el invariante, no para servir a nadie: exige
// un scope de perfil que ningun token de device tiene, asi que responde 403 con la
// credencial de la app. Si algun dia alguien agregara una llamada a esto desde el
// Portador, el proveedor la rechazaria.
app.MapGet("/{recurso}/{id}/perfil", (string recurso, string id, HttpRequest pedido) =>
{
    IResult negado = concesiones.Exigir(pedido, $"{recurso}.profile.read", recurso, id);
    if (negado != null) return negado;

    return Results.Ok(new { nombre = "nunca deberia verse", recurso, id });
});

// ---------------------------------------------------------------- control del mock

// Simula que el proveedor le registro una factura nueva al usuario. Es una accion
// DEL PROVEEDOR, no de la app, asi que no pide el token de la app: lo dispara quien
// opera el mock para probar que el refresh trae registros nuevos.
app.MapPost("/{recurso}/{id}/events/factura-aleatoria", (string recurso, string id) =>
{
    EventoDeFactura nuevo = almacen.AgregarAleatoria(recurso, id);
    return Results.Ok(new { agregado = nuevo.Id, numero = nuevo.Numero, total = nuevo.Total() });
});

// Registra una factura CONCRETA, con sus lineas. Es lo que usa el inyector de eventos de
// demo: con la aleatoria no se puede predecir el total, y una demo se cuenta con numeros
// que uno eligio. Tambien accion del proveedor, no de la app: sin token.
app.MapPost("/{recurso}/{id}/events/factura", (string recurso, string id, FacturaAInyectar pedido) =>
{
    if (pedido is null || Vacio(pedido.Numero))
        return Results.BadRequest(new { error = "numero requerido" });
    if (pedido.Skus is null || pedido.Skus.Length == 0)
        return Results.BadRequest(new { error = "hace falta al menos una linea" });
    if (pedido.Descripciones?.Length != pedido.Skus.Length
        || pedido.Cantidades?.Length != pedido.Skus.Length
        || pedido.PreciosUnitarios?.Length != pedido.Skus.Length)
        return Results.BadRequest(new { error = "las cuatro listas deben tener el mismo largo" });

    EventoDeFactura nuevo = almacen.Agregar(recurso, id, pedido);
    return Results.Ok(new { agregado = nuevo.Id, numero = nuevo.Numero, total = nuevo.Total() });
});

// Expira SOLO los access tokens y deja los refresh vivos. Es el caso que el diseno
// declara normal - "access token corto, refresh token durable/rotatorio" - y sin una
// forma de provocarlo no hay manera de demostrar que la app rota sola en vez de pedir
// una credencial nueva. Control del mock, no del proveedor.
app.MapPost("/device/expirar-access", () =>
{
    int expirados = concesiones.ExpirarAccessTokens();
    return Results.Ok(new { expirados, refreshSiguenVivos = true });
});

app.MapGet("/salud", () => Results.Ok(new { estado = "arriba" }));

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

    // Adelanta el vencimiento de todos los access tokens sin tocar los refresh. Deja la
    // concesion en el indice para que Exigir la encuentre y responda "expirado" en vez de
    // "desconocido": son dos rechazos distintos y la app reacciona distinto a cada uno.
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
    // Tres razones de rechazo, y la tercera es la que importa: el token puede ser
    // perfectamente valido y aun asi no servir, porque su scope no cubre lo que el
    // endpoint pide.
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

sealed class EventoDeFactura
{
    [JsonPropertyName("id")]               public long Id { get; init; }
    [JsonPropertyName("tipo")]             public string Tipo { get; init; } = "FacturaAgregada";
    [JsonPropertyName("numero")]           public string Numero { get; init; }
    [JsonPropertyName("fecha")]            public DateTime Fecha { get; init; }
    [JsonPropertyName("skus")]             public string[] Skus { get; init; }
    [JsonPropertyName("descripciones")]    public string[] Descripciones { get; init; }
    [JsonPropertyName("cantidades")]       public int[] Cantidades { get; init; }
    [JsonPropertyName("preciosUnitarios")] public decimal[] PreciosUnitarios { get; init; }

    internal decimal Total()
    {
        decimal total = 0m;
        for (int i = 0; i < Skus.Length; i++) total += Cantidades[i] * PreciosUnitarios[i];
        return total;
    }
}

sealed class AlmacenDeEventos
{
    private readonly ConcurrentDictionary<string, List<EventoDeFactura>> porRecurso = new();
    private readonly Lock candado = new();

    // Las tres facturas sembradas son las mismas que usa la consola para medir, asi
    // que el total esperado tras un refresh limpio es 19.321,50.
    private static readonly (string sku, string desc, decimal precio)[] Catalogo =
    {
        ("ARR-1KG",   "Arroz 1kg",          1450.00m),
        ("LEC-1L",    "Leche 1L",            890.50m),
        ("CAF-500G",  "Cafe molido 500g",   4750.00m),
        ("PAN-INT",   "Pan integral",       1200.00m),
        ("HUE-12",    "Huevos x12",         2350.00m),
        ("ACE-900ML", "Aceite 900ml",       3100.00m),
        ("AZU-2KG",   "Azucar 2kg",         1980.00m),
        ("JAB-3UN",   "Jabon x3",           1675.00m),
    };

    private List<EventoDeFactura> Lista(string recurso, string id) =>
        porRecurso.GetOrAdd($"{recurso}/{id}", _ => Sembrar());

    private static List<EventoDeFactura> Sembrar() => new()
    {
        new EventoDeFactura
        {
            Id = 101, Numero = "TQ-0001", Fecha = new DateTime(2026, 8, 3, 10, 14, 0),
            Skus = new[] { "ARR-1KG", "LEC-1L" },
            Descripciones = new[] { "Arroz 1kg", "Leche 1L" },
            Cantidades = new[] { 2, 3 },
            PreciosUnitarios = new[] { 1450.00m, 890.50m },
        },
        new EventoDeFactura
        {
            Id = 102, Numero = "TQ-0002", Fecha = new DateTime(2026, 8, 7, 18, 32, 0),
            Skus = new[] { "CAF-500G" },
            Descripciones = new[] { "Cafe molido 500g" },
            Cantidades = new[] { 1 },
            PreciosUnitarios = new[] { 4750.00m },
        },
        new EventoDeFactura
        {
            Id = 103, Numero = "TQ-0003", Fecha = new DateTime(2026, 8, 12, 9, 5, 0),
            Skus = new[] { "PAN-INT", "HUE-12", "ACE-900ML" },
            Descripciones = new[] { "Pan integral", "Huevos x12", "Aceite 900ml" },
            Cantidades = new[] { 1, 2, 1 },
            PreciosUnitarios = new[] { 1200.00m, 2350.00m, 3100.00m },
        },
    };

    internal List<EventoDeFactura> Posteriores(string recurso, string id, long after)
    {
        List<EventoDeFactura> lista = Lista(recurso, id);
        lock (candado)
            return lista.Where(e => e.Id > after).OrderBy(e => e.Id).ToList();
    }

    internal long UltimoNumero(string recurso, string id)
    {
        List<EventoDeFactura> lista = Lista(recurso, id);
        lock (candado)
            return lista.Count == 0 ? 0 : lista.Max(e => e.Id);
    }

    // La factura que el operador de la demo eligio, con sus lineas exactas.
    internal EventoDeFactura Agregar(string recurso, string id, FacturaAInyectar pedido)
    {
        List<EventoDeFactura> lista = Lista(recurso, id);

        lock (candado)
        {
            long siguiente = (lista.Count == 0 ? 100 : lista.Max(e => e.Id)) + 1;
            var evento = new EventoDeFactura
            {
                Id = siguiente,
                Numero = pedido.Numero,
                Fecha = pedido.Fecha == default ? DateTime.Now : pedido.Fecha,
                Skus = pedido.Skus,
                Descripciones = pedido.Descripciones,
                Cantidades = pedido.Cantidades,
                PreciosUnitarios = pedido.PreciosUnitarios,
            };

            lista.Add(evento);
            return evento;
        }
    }

    internal EventoDeFactura AgregarAleatoria(string recurso, string id)
    {
        List<EventoDeFactura> lista = Lista(recurso, id);

        lock (candado)
        {
            long siguiente = (lista.Count == 0 ? 100 : lista.Max(e => e.Id)) + 1;
            int lineas = Random.Shared.Next(1, 4);

            var elegidos = Catalogo.OrderBy(_ => Random.Shared.Next()).Take(lineas).ToArray();
            var evento = new EventoDeFactura
            {
                Id = siguiente,
                Numero = $"TQ-{siguiente:0000}",
                Fecha = DateTime.Now,
                Skus = elegidos.Select(c => c.sku).ToArray(),
                Descripciones = elegidos.Select(c => c.desc).ToArray(),
                Cantidades = elegidos.Select(_ => Random.Shared.Next(1, 5)).ToArray(),
                PreciosUnitarios = elegidos.Select(c => c.precio).ToArray(),
            };

            lista.Add(evento);
            return evento;
        }
    }
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
sealed class FacturaAInyectar
{
    [JsonPropertyName("numero")]           public string Numero { get; set; }
    [JsonPropertyName("fecha")]            public DateTime Fecha { get; set; }
    [JsonPropertyName("skus")]             public string[] Skus { get; set; }
    [JsonPropertyName("descripciones")]    public string[] Descripciones { get; set; }
    [JsonPropertyName("cantidades")]       public int[] Cantidades { get; set; }
    [JsonPropertyName("preciosUnitarios")] public decimal[] PreciosUnitarios { get; set; }
}
