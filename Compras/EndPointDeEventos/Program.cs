using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text.Json.Serialization;

// Mock del servidor del supermercado. Happy path, estado en memoria.
//
// El diseno central: un token de device declara QUE puede hacer y SOBRE QUE recurso, y
// cada endpoint declara que scope exige. La app solo consigue "{recurso}.events.read",
// asi que cualquier otro endpoint la rechaza porque el token NO LO TIENE - no por un if
// contra una ruta. Eso es lo que vuelve falsable el invariante "la app no es cliente de
// los servicios generales del supermercado": el senuelo /perfil devuelve 403
// estructuralmente, no por cortesia.
//
// Para este negocio el juego de URLs es:
//
//   GET  /cliente/{id}/events?after=N     scope cliente.events.read
//   GET  /cliente/{id}/perfil             scope cliente.profile.read  -> 403, el senuelo
//
// LA DATA SEMBRADA SE PRESTA PARA SER ANALIZADA: cada linea lleva su categoria (y la
// categoria, en el dominio de la app, lleva su perfil de consumo). Cuando el Host este
// listo y el asistente pueda ver varios negocios, esta narrativa ya cuenta la historia
// del cruce — la canasta de agosto 2025 era 9% ocasional, la de agosto 2026 es 97% —
// sin tocar un byte del mock.

var builder = WebApplication.CreateBuilder(args);

// Mock en localhost: cualquier origen. No usa cookies (el token viaja en el header
// Authorization), asi que AllowAnyOrigin es valido y evita que el primer intento desde
// el browser muera en un preflight.
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
    .AllowAnyOrigin()
    .AllowAnyHeader()
    .AllowAnyMethod()));

var app = builder.Build();

app.UseCors();

var almacen = new AlmacenDeEventos();
var concesiones = new Concesiones();

// ---------------------------------------------------------------- credenciales

// El supermercado le da al DEVICE del usuario un access token corto y un refresh token
// durable y rotatorio. El scope queda acotado a leer los eventos de UN recurso concreto:
// nada de compras de otros clientes, ni perfil, ni puntos.
app.MapPost("/device/token", (PedidoDeToken pedido) =>
{
    if (string.IsNullOrWhiteSpace(pedido?.DeviceKey)) return Results.BadRequest(new { error = "deviceKey requerido" });

    string recurso = Vacio(pedido.Recurso) ? "cliente" : pedido.Recurso;
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

// EL UNICO verbo de lectura que la app usa. Devuelve solo los eventos posteriores a
// `after`, que es el checkpoint que la app tiene journalizado.
app.MapGet("/{recurso}/{id}/events", (string recurso, string id, long? after, HttpRequest pedido) =>
{
    IResult negado = concesiones.Exigir(pedido, $"{recurso}.events.read", recurso, id);
    if (negado != null) return negado;

    long desde = after ?? 0;
    List<EventoDeCompra> eventos = almacen.Posteriores(recurso, id, desde);

    return Results.Ok(new
    {
        events = eventos,
        last = almacen.UltimoNumero(recurso, id),
    });
});

// EL SENUELO. Existe para demostrar el invariante, no para servir a nadie: exige un
// scope de perfil que ningun token de device tiene, asi que responde 403 con la
// credencial de la app. Es la demostracion falsable de que la app no es cliente de los
// servicios generales del supermercado.
app.MapGet("/{recurso}/{id}/perfil", (string recurso, string id, HttpRequest pedido) =>
{
    IResult negado = concesiones.Exigir(pedido, $"{recurso}.profile.read", recurso, id);
    if (negado != null) return negado;

    return Results.Ok(new { nombre = "nunca deberia verse", recurso, id });
});

// ---------------------------------------------------------------- control del mock

// Registra una compra CONCRETA, con sus lineas. Es lo que usa el inyector de eventos de
// demo: una demo se cuenta con numeros que uno eligio, porque la historia (la canasta
// que se voltea, la garantia que vence) depende de los valores y de las fechas.
// Accion DEL SUPERMERCADO, no de la app: sin token.
app.MapPost("/{recurso}/{id}/events/compra", (string recurso, string id, CompraAInyectar pedido) =>
{
    if (pedido is null || Vacio(pedido.Numero))
        return Results.BadRequest(new { error = "numero requerido" });
    if (pedido.Skus is null || pedido.Skus.Length == 0)
        return Results.BadRequest(new { error = "hace falta al menos una linea" });
    if (pedido.Descripciones?.Length != pedido.Skus.Length
        || pedido.Cantidades?.Length != pedido.Skus.Length
        || pedido.PreciosUnitarios?.Length != pedido.Skus.Length
        || pedido.Categorias?.Length != pedido.Skus.Length
        || pedido.MesesDeGarantia?.Length != pedido.Skus.Length)
        return Results.BadRequest(new { error = "las seis listas deben tener el mismo largo" });

    foreach (string categoria in pedido.Categorias)
        if (!AlmacenDeEventos.EsCategoriaConocida(categoria))
            return Results.BadRequest(new
            {
                error = $"categoria desconocida: {categoria}",
                conocidas = AlmacenDeEventos.CategoriasConocidas,
            });

    EventoDeCompra nuevo = almacen.Agregar(recurso, id, pedido);
    return Results.Ok(new { agregado = nuevo.Id, numero = nuevo.Numero, total = nuevo.Total() });
});

// Expira SOLO los access tokens y deja los refresh vivos. Es el caso que el diseno
// declara normal - "access token corto, refresh token durable/rotatorio" - y sin una
// forma de provocarlo no hay manera de demostrar que la app rota sola en vez de pedir
// una credencial nueva. Control del mock, no del supermercado.
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
    // concesion en el indice para que Exigir la encuentre y responda "expirado" en vez
    // de "desconocido": son dos rechazos distintos y la app reacciona distinto a cada uno.
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

// Un evento de la narrativa del supermercado. Un solo tipo de evento con las lineas del
// ticket en listas paralelas: el supermercado dice "te registre una compra", y que trae
// cada linea (incluida su categoria y su garantia) es un dato. El que traduce esas
// listas a clases del dominio es la app, en su frontera.
sealed class EventoDeCompra
{
    [JsonPropertyName("id")]               public long Id { get; init; }
    [JsonPropertyName("tipo")]             public string Tipo { get; init; } = "CompraRegistrada";
    [JsonPropertyName("numero")]           public string Numero { get; init; }
    [JsonPropertyName("fecha")]            public DateTime Fecha { get; init; }
    [JsonPropertyName("skus")]             public string[] Skus { get; init; }
    [JsonPropertyName("descripciones")]    public string[] Descripciones { get; init; }
    [JsonPropertyName("cantidades")]       public int[] Cantidades { get; init; }
    [JsonPropertyName("preciosUnitarios")] public decimal[] PreciosUnitarios { get; init; }

    // La categoria por linea es lo que vuelve la narrativa ANALIZABLE: sin ella,
    // "HELADO-1L x3" es una linea muda; con ella, la app puede sumar la canasta por
    // perfil y el asistente puede cruzarla con lo que digan otros negocios.
    [JsonPropertyName("categorias")]       public string[] Categorias { get; init; }

    // 0 = sin garantia, que es el caso normal de un supermercado. Solo lo que de
    // verdad trae cobertura (un electrodomestico) lleva meses.
    [JsonPropertyName("mesesDeGarantia")]  public int[] MesesDeGarantia { get; init; }

    internal decimal Total()
    {
        decimal total = 0m;
        for (int i = 0; i < Skus.Length; i++) total += Cantidades[i] * PreciosUnitarios[i];
        return total;
    }
}

sealed class AlmacenDeEventos
{
    private readonly ConcurrentDictionary<string, List<EventoDeCompra>> porRecurso = new();
    private readonly Lock candado = new();

    internal static readonly string[] CategoriasConocidas =
    {
        "Proteinas", "FrutasYVerduras", "LacteosYHuevos", "GranosYAbarrotes",
        "AceitesYGrasas", "Azucarados", "Snacks", "Limpieza", "Electrohogar",
    };

    internal static bool EsCategoriaConocida(string categoria) =>
        CategoriasConocidas.Any(c => string.Equals(c, categoria, StringComparison.OrdinalIgnoreCase));

    private List<EventoDeCompra> Lista(string recurso, string id) =>
        porRecurso.GetOrAdd($"{recurso}/{id}", _ => Sembrar());

    // LA HISTORIA sembrada, la misma que usa la consola para medir. Las fechas son
    // literales a proposito: una demo con fechas relativas al dia de hoy deja de ser
    // reproducible, y la comparacion de canastas depende de en que mes cae cada compra.
    //
    // El guion en una linea: en agosto 2025 la canasta es sana (9% ocasional); de
    // febrero a agosto 2026 entran helado, sodas y snacks hasta voltearla (97%);
    // en mayo 2026 entra la olla arrocera con su garantia de 12 meses. Es la mitad
    // "compras" del cruce con el colesterol del laboratorio, preplanificada.
    private static List<EventoDeCompra> Sembrar() => new()
    {
        new EventoDeCompra
        {
            Id = 301, Numero = "T-88101", Fecha = new DateTime(2025, 8, 5, 17, 20, 0),
            Skus = new[] { "POLLO-KG", "FRIJOL-900", "LECHE-1L", "BROCOLI" },
            Descripciones = new[] { "Pechuga de pollo kg", "Frijoles negros 900g", "Leche semidescremada 1L", "Brocoli unidad" },
            Cantidades = new[] { 2, 2, 6, 3 },
            PreciosUnitarios = new[] { 4200m, 1150m, 950m, 890m },
            Categorias = new[] { "Proteinas", "GranosYAbarrotes", "LacteosYHuevos", "FrutasYVerduras" },
            MesesDeGarantia = new[] { 0, 0, 0, 0 },
        },
        new EventoDeCompra
        {
            Id = 302, Numero = "T-88240", Fecha = new DateTime(2025, 8, 20, 18, 5, 0),
            Skus = new[] { "ACEITE-GIR-1L", "ARROZ-2KG", "HUEVOS-30", "TILAPIA-KG" },
            Descripciones = new[] { "Aceite de girasol 1L", "Arroz grano entero 2kg", "Huevos carton 30", "Filet de tilapia kg" },
            Cantidades = new[] { 1, 1, 1, 1 },
            PreciosUnitarios = new[] { 2890m, 2450m, 3300m, 5200m },
            Categorias = new[] { "AceitesYGrasas", "GranosYAbarrotes", "LacteosYHuevos", "Proteinas" },
            MesesDeGarantia = new[] { 0, 0, 0, 0 },
        },
        new EventoDeCompra
        {
            Id = 303, Numero = "T-91515", Fecha = new DateTime(2026, 2, 14, 19, 40, 0),
            Skus = new[] { "HELADO-1L", "SODA-2L", "GALLETA-CHOC", "POLLO-KG" },
            Descripciones = new[] { "Helado de vainilla 1L", "Soda cola 2L", "Galletas de chocolate", "Pechuga de pollo kg" },
            Cantidades = new[] { 2, 4, 3, 1 },
            PreciosUnitarios = new[] { 3450m, 1750m, 1250m, 4350m },
            Categorias = new[] { "Azucarados", "Azucarados", "Azucarados", "Proteinas" },
            MesesDeGarantia = new[] { 0, 0, 0, 0 },
        },
        new EventoDeCompra
        {
            Id = 304, Numero = "T-93872", Fecha = new DateTime(2026, 5, 10, 10, 15, 0),
            Skus = new[] { "OLLA-ARR-18", "ARROZ-2KG", "MANTECA-500" },
            Descripciones = new[] { "Olla arrocera 1.8L", "Arroz grano entero 2kg", "Manteca de palma 500g" },
            Cantidades = new[] { 1, 2, 1 },
            PreciosUnitarios = new[] { 45900m, 2520m, 1980m },
            Categorias = new[] { "Electrohogar", "GranosYAbarrotes", "AceitesYGrasas" },
            MesesDeGarantia = new[] { 12, 0, 0 },
        },
        new EventoDeCompra
        {
            Id = 305, Numero = "T-95104", Fecha = new DateTime(2026, 7, 3, 20, 10, 0),
            Skus = new[] { "SODA-12PK", "HELADO-1L", "PAPAS-FAM", "DETERG-5L" },
            Descripciones = new[] { "Soda cola lata 12-pack", "Helado de vainilla 1L", "Papas fritas familiar", "Detergente liquido 5L" },
            Cantidades = new[] { 2, 2, 3, 1 },
            PreciosUnitarios = new[] { 5990m, 3560m, 2100m, 6800m },
            Categorias = new[] { "Azucarados", "Azucarados", "Snacks", "Limpieza" },
            MesesDeGarantia = new[] { 0, 0, 0, 0 },
        },
        new EventoDeCompra
        {
            Id = 306, Numero = "T-96470", Fecha = new DateTime(2026, 8, 15, 18, 50, 0),
            Skus = new[] { "HELADO-1L", "SODA-2L", "CHOC-BARRA", "MANTECA-500", "BROCOLI" },
            Descripciones = new[] { "Helado de vainilla 1L", "Soda cola 2L", "Chocolate de leche barra", "Manteca de palma 500g", "Brocoli unidad" },
            Cantidades = new[] { 3, 6, 4, 2, 1 },
            PreciosUnitarios = new[] { 3680m, 1790m, 1450m, 2050m, 920m },
            Categorias = new[] { "Azucarados", "Azucarados", "Azucarados", "AceitesYGrasas", "FrutasYVerduras" },
            MesesDeGarantia = new[] { 0, 0, 0, 0, 0 },
        },
    };

    internal List<EventoDeCompra> Posteriores(string recurso, string id, long after)
    {
        List<EventoDeCompra> lista = Lista(recurso, id);
        lock (candado)
            return lista.Where(e => e.Id > after).OrderBy(e => e.Id).ToList();
    }

    internal long UltimoNumero(string recurso, string id)
    {
        List<EventoDeCompra> lista = Lista(recurso, id);
        lock (candado)
            return lista.Count == 0 ? 0 : lista.Max(e => e.Id);
    }

    // La compra que el operador de la demo eligio, con sus lineas exactas.
    internal EventoDeCompra Agregar(string recurso, string id, CompraAInyectar pedido)
    {
        List<EventoDeCompra> lista = Lista(recurso, id);

        lock (candado)
        {
            long siguiente = (lista.Count == 0 ? 300 : lista.Max(e => e.Id)) + 1;
            var evento = new EventoDeCompra
            {
                Id = siguiente,
                Numero = pedido.Numero,
                Fecha = pedido.Fecha == default ? DateTime.Now : pedido.Fecha,
                Skus = pedido.Skus,
                Descripciones = pedido.Descripciones,
                Cantidades = pedido.Cantidades,
                PreciosUnitarios = pedido.PreciosUnitarios,
                Categorias = pedido.Categorias,
                MesesDeGarantia = pedido.MesesDeGarantia,
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
sealed class CompraAInyectar
{
    [JsonPropertyName("numero")]           public string Numero { get; set; }
    [JsonPropertyName("fecha")]            public DateTime Fecha { get; set; }
    [JsonPropertyName("skus")]             public string[] Skus { get; set; }
    [JsonPropertyName("descripciones")]    public string[] Descripciones { get; set; }
    [JsonPropertyName("cantidades")]       public int[] Cantidades { get; set; }
    [JsonPropertyName("preciosUnitarios")] public decimal[] PreciosUnitarios { get; set; }
    [JsonPropertyName("categorias")]       public string[] Categorias { get; set; }
    [JsonPropertyName("mesesDeGarantia")]  public int[] MesesDeGarantia { get; set; }
}
