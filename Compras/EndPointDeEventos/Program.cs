using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text.Json;
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


// LAS CUENTAS QUE YA SE MUDARON A GNOTHI. La marca de migracion la pone EL PROVEEDOR y no el
// aparato: ninguna marca del lado web sobrevive a "borrar datos del sitio", y una que se
// escribe la app a si misma no prueba que Gnothi haya recibido nada. Esta si, porque la
// escribe quien emitio la credencial.
//
// Se escribe SOLA cuando un device de Gnothi pide su primera concesion, que es el instante
// que el documento describe. En un proveedor de verdad ese instante lleva una pantalla de
// consentimiento -"mover el registro de esta cuenta a Gnothi en este telefono"- para que la
// marca sea un acto de la persona y no un efecto secundario.
// LA GRACIA, y por que la marca sola no alcanzaba. Hasta ahora la app suelta dejaba de
// sincronizar porque DECIDIA obedecer la marca; el proveedor no le quitaba nada. El
// razonamiento completo esta en Examenes/EndPointDeEventos/Program.cs, escrito una sola vez a
// proposito. En corto: vencida la gracia la cuenta ES de Gnothi, y el proveedor se lo hace
// cumplir en las TRES puertas -- emitir, rotar y leer. Cerrar una sola seria teatro (13 §4).
const int DiasDeGracia = 30;

var vinculadas = new ConcurrentDictionary<string, Vinculo>();

static bool EsDeGnothi(string deviceKey) =>
    deviceKey.StartsWith("device-gnothi", StringComparison.Ordinal);
// El supermercado le da al DEVICE del usuario un access token corto y un refresh token
// durable y rotatorio. El scope queda acotado a leer los eventos de UN recurso concreto:
// nada de compras de otros clientes, ni perfil, ni puntos.
app.MapPost("/device/token", (PedidoDeToken pedido) =>
{
    if (string.IsNullOrWhiteSpace(pedido?.DeviceKey)) return Results.BadRequest(new { error = "deviceKey requerido" });

    string recurso = Vacio(pedido.Recurso) ? "cliente" : pedido.Recurso;
    string id = Vacio(pedido.Id) ? "1" : pedido.Id;

    // PUERTA 1 DE 3: no se le emite credencial nueva a un device ajeno sobre una cuenta que ya
    // es de Gnothi. Sin esto, la app a la que se le revoco el token vuelve a pedir una.
    if (!EsDeGnothi(pedido.DeviceKey) && SinGracia(vinculadas, recurso, id))
        return Revocado($"la cuenta {recurso}/{id} ya no se sirve a este cliente");
    Concesion concesion = concesiones.Otorgar(pedido.DeviceKey, recurso, id);

    // EL INSTANTE DE LA MIGRACION: el proveedor le acaba de emitir credencial a Gnothi para
    // esta cuenta. Ahi queda vinculada, y la app suelta se entera en su proximo pull porque
    // el sobre pasa a decir "migrated". Un device de Gnothi se reconoce por su prefijo, que
    // en un proveedor de verdad seria un client_id registrado.
    if (EsDeGnothi(pedido.DeviceKey))
    {
        Vinculo vinculo = vinculadas.GetOrAdd($"{recurso}/{id}", _ => new Vinculo(
            "Gnothi", DateTime.UtcNow.ToString("O"), DateTime.UtcNow.AddDays(DiasDeGracia)));
        Console.WriteLine($"cuenta {recurso}/{id} vinculada a Gnothi ({pedido.DeviceKey}); hasta {vinculo.GraciaHasta:O}");
    }

    return Results.Ok(concesion.ComoRespuesta());
});

app.MapPost("/device/refresh", (PedidoDeRefresh pedido) =>
{
    if (string.IsNullOrWhiteSpace(pedido?.RefreshToken)) return Results.BadRequest(new { error = "refreshToken requerido" });

    // PUERTA 2 DE 3. Se mira ANTES de rotar: rotar y despues negar dejaria la concesion vieja
    // invalidada y la nueva sin entregar.
    Concesion actual = concesiones.PorRefresh(pedido.RefreshToken);
    if (actual != null && !EsDeGnothi(actual.DeviceKey) && SinGracia(vinculadas, actual.Recurso, actual.Id))
    {
        concesiones.Revocar(actual.Recurso, actual.Id, salvoGnothi: true);
        return Revocado($"la cuenta {actual.Recurso}/{actual.Id} ya no se sirve a este cliente");
    }
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

    // PUERTA 3 DE 3, la que nombra el documento: vencida la gracia, el proximo pull de la app
    // suelta recibe 401.
    if (!concesiones.EsDeGnothi(pedido) && SinGracia(vinculadas, recurso, id))
    {
        concesiones.Revocar(recurso, id, salvoGnothi: true);
        return Revocado($"la cuenta {recurso}/{id} ya no se sirve a este cliente");
    }
    long desde = after ?? 0;
    List<EventoDeCompra> eventos = almacen.Posteriores(recurso, id, desde);

    return Results.Ok(new
    {
        // EL ESTADO DE LA CUENTA VIAJA EN EL SOBRE, y viaja SIEMPRE, aunque no haya un solo
        // evento nuevo: es justamente lo que la app suelta necesita para enterarse de que su
        // registro ya se mudo. Campo aditivo: un cliente viejo lo ignora.
        account = vinculadas.TryGetValue($"{recurso}/{id}", out Vinculo vinculo)
            ? new
            {
                state = "migrated",
                migratedAt = vinculo.Desde,
                host = vinculo.Host,
                graceUntil = vinculo.GraciaHasta.ToString("O"),
            }
            : new { state = "active", migratedAt = "", host = "", graceUntil = "" },
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

// ---------------------------------------------------------------- el catalogo

// EL CATALOGO ES DEL SUPERMERCADO, no de la persona: son los productos que vende, con
// su precio de gondola, su categoria y su garantia si la trae. No pide token porque no
// es data de nadie — cualquiera que entre a la tienda ve los precios.
//
// Existe para que la caja (la UI de operacion de la demo) no lleve el catalogo
// hardcodeado adentro: el POS le pregunta a su propio sistema que vende, que es lo que
// haria uno de verdad.
app.MapGet("/catalogo", () => Results.Ok(new { productos = Catalogo.Productos }));

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

// Adelanta los 30 dias. Mismo espiritu que /device/expirar-access. Solo mueve el reloj: las
// tres puertas revocan al pasar, asi que el rechazo lleva el codigo invalid_token.
app.MapPost("/device/vencer-gracia", () =>
{
    var vencidas = new List<string>();
    foreach (string cuenta in vinculadas.Keys)
    {
        if (!vinculadas.TryGetValue(cuenta, out Vinculo v)) continue;
        vinculadas[cuenta] = v with { GraciaHasta = DateTime.UtcNow.AddSeconds(-1) };
        vencidas.Add(cuenta);
    }
    return Results.Ok(new { cuentas = vencidas, revoca = false });
});

// DESVINCULAR, paso 5 de 13 §4: la migracion es REVERSIBLE. Una decision de la que no se puede
// volver no es una decision de la persona: es una trampa.
app.MapDelete("/{recurso}/{id}/vinculo", (string recurso, string id) =>
{
    if (!vinculadas.TryRemove($"{recurso}/{id}", out Vinculo _))
        return Results.NotFound(new { error = "esa cuenta no estaba vinculada" });

    int revocadas = concesiones.Revocar(recurso, id, salvoGnothi: false, soloGnothi: true);
    return Results.Ok(new { cuenta = $"{recurso}/{id}", concesionesRevocadas = revocadas });
});

app.MapGet("/salud", () => Results.Ok(new { estado = "arriba" }));

app.Run();

static bool Vacio(string s) => string.IsNullOrWhiteSpace(s);
// Vinculada a Gnothi Y la gracia ya vencio. Mientras dura la gracia esto es false y la app
// suelta lee igual, sabiendo que se muda.
static bool SinGracia(System.Collections.Concurrent.ConcurrentDictionary<string, Vinculo> vinculadas,
    string recurso, string id) =>
    vinculadas.TryGetValue($"{recurso}/{id}", out Vinculo v) && v.GraciaHasta <= DateTime.UtcNow;

// 401 con el codigo de RFC 6750, que es el que el documento nombra. El cliente mira el ESTADO y
// no el cuerpo.
static IResult Revocado(string detalle) => Results.Json(
    new { error = "invalid_token", error_description = detalle }, statusCode: 401);


// ---------------------------------------------------------------- credenciales

// A que host esta vinculada una cuenta, y hasta cuando la sigue leyendo quien estaba antes.
sealed record Vinculo(string Host, string Desde, DateTime GraciaHasta);

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


    // Quitarle el acceso a un cliente de verdad: se van los DOS tokens. Sacar solo el access no
    // revoca nada -- el cliente rota con su refresh y sigue leyendo.
    internal int Revocar(string recurso, string id, bool salvoGnothi, bool soloGnothi = false)
    {
        int revocadas = 0;
        foreach (Concesion c in porAccessToken.Values.ToArray())
        {
            if (c.Recurso != recurso || c.Id != id) continue;

            bool esGnothi = c.DeviceKey.StartsWith("device-gnothi", StringComparison.Ordinal);
            if (salvoGnothi && esGnothi) continue;
            if (soloGnothi && !esGnothi) continue;

            porAccessToken.TryRemove(c.AccessToken, out _);
            porRefreshToken.TryRemove(c.RefreshToken, out _);
            revocadas++;
        }
        return revocadas;
    }

    // La concesion detras de un refresh token, SIN rotarla.
    internal Concesion PorRefresh(string refreshToken) =>
        porRefreshToken.TryGetValue(refreshToken, out Concesion c) ? c : null;

    // De quien es el token que viene en el pedido. Solo se llama despues de que Exigir dejo pasar.
    internal bool EsDeGnothi(HttpRequest pedido)
    {
        string presentado = Bearer(pedido);
        return presentado != null
            && porAccessToken.TryGetValue(presentado, out Concesion c)
            && c.DeviceKey.StartsWith("device-gnothi", StringComparison.Ordinal);
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

// ---------------------------------------------------------------- el catalogo

sealed class ProductoDelCatalogo
{
    [JsonPropertyName("sku")]             public string Sku { get; init; }
    [JsonPropertyName("descripcion")]     public string Descripcion { get; init; }
    [JsonPropertyName("precio")]          public decimal Precio { get; init; }
    [JsonPropertyName("categoria")]       public string Categoria { get; init; }
    [JsonPropertyName("mesesDeGarantia")] public int MesesDeGarantia { get; init; }
}

static class Catalogo
{
    // Los precios estan en colones y son de gondola, no inventados al azar: la demo se
    // cuenta con numeros que uno eligio. Incluye los SKUs de la narrativa sembrada, para
    // que la caja pueda repetir una compra que ya paso, y bastante mas para que la
    // historia se pueda seguir escribiendo en vivo.
    internal static readonly ProductoDelCatalogo[] Productos =
    {
        // -- proteinas
        P("POLLO-KG", "Pechuga de pollo kg", 4350, "Proteinas"),
        P("TILAPIA-KG", "Filet de tilapia kg", 5200, "Proteinas"),
        P("SALMON-KG", "Filet de salmon kg", 12800, "Proteinas"),
        P("CARNE-MOL", "Carne molida kg", 5600, "Proteinas"),
        P("ATUN-LATA", "Atun en agua lata", 1250, "Proteinas"),

        // -- frutas y verduras
        P("BROCOLI", "Brocoli unidad", 920, "FrutasYVerduras"),
        P("ESPINACA", "Espinaca fresca 250g", 1450, "FrutasYVerduras"),
        P("BANANO-KG", "Banano kg", 890, "FrutasYVerduras"),
        P("TOMATE-KG", "Tomate kg", 1150, "FrutasYVerduras"),
        P("MANZANA-KG", "Manzana kg", 2100, "FrutasYVerduras"),

        // -- lacteos y huevos
        P("LECHE-1L", "Leche semidescremada 1L", 950, "LacteosYHuevos"),
        P("HUEVOS-30", "Huevos carton 30", 3300, "LacteosYHuevos"),
        P("QUESO-500", "Queso turrialba 500g", 3450, "LacteosYHuevos"),
        P("YOGURT-1L", "Yogurt natural 1L", 2200, "LacteosYHuevos"),

        // -- granos y abarrotes
        P("ARROZ-2KG", "Arroz grano entero 2kg", 2520, "GranosYAbarrotes"),
        P("FRIJOL-900", "Frijoles negros 900g", 1150, "GranosYAbarrotes"),
        P("PASTA-500", "Pasta espagueti 500g", 890, "GranosYAbarrotes"),
        P("AVENA-1KG", "Avena en hojuelas 1kg", 1980, "GranosYAbarrotes"),

        // -- aceites y grasas
        P("ACEITE-GIR-1L", "Aceite de girasol 1L", 2890, "AceitesYGrasas"),
        P("MANTECA-500", "Manteca de palma 500g", 2050, "AceitesYGrasas"),

        // -- lacteos que SUBEN en 2026. El cruce del asistente los nombra: "en su carrito
        //    tambien subieron el queso crema y la mantequilla".
        P("QUESO-CREMA", "Queso crema 250g", 1980, "LacteosYHuevos"),
        P("MANTEQUILLA-250", "Mantequilla 250g", 2650, "LacteosYHuevos"),

        // -- embutidos. Departamento propio y no proteinas: la nutricionista le pide
        //    "cambiar embutidos por proteina fresca", asi que para quien la aconseja son
        //    dos cosas distintas.
        P("JAMON-200", "Jamon de pierna 200g", 2350, "Embutidos"),
        P("SALCHICHON-300", "Salchichon 300g", 2890, "Embutidos"),
        P("SALCHICHA-PK", "Salchichas paquete 8u", 2100, "Embutidos"),
        P("MORTADELA-250", "Mortadela 250g", 1650, "Embutidos"),

        // -- comidas preparadas
        P("POLLO-ROST", "Pollo rostizado entero", 6900, "ComidasPreparadas"),
        P("LASAGNA-CONG", "Lasagna congelada familiar", 5450, "ComidasPreparadas"),
        P("PIZZA-CONG", "Pizza congelada grande", 4200, "ComidasPreparadas"),
        P("ENSALADA-PREP", "Ensalada preparada 400g", 2450, "ComidasPreparadas"),

        // -- mascotas. El invariante 14 pone la comida de Luna en ₡25.000 al mes, y va en
        //    una bolsa mensual: es un gasto del hogar que se repone, no un antojo.
        P("PERRO-CROQ-10KG", "Alimento para perro 10kg", 25000, "Mascotas"),

        // -- azucarados
        P("HELADO-1L", "Helado de vainilla 1L", 3680, "Azucarados"),
        P("SODA-2L", "Soda cola 2L", 1790, "Azucarados"),
        P("SODA-12PK", "Soda cola lata 12-pack", 5990, "Azucarados"),
        P("GALLETA-CHOC", "Galletas de chocolate", 1250, "Azucarados"),
        P("CHOC-BARRA", "Chocolate de leche barra", 1450, "Azucarados"),

        // -- snacks
        P("PAPAS-FAM", "Papas fritas familiar", 2100, "Snacks"),
        P("MANI-300", "Mani salado 300g", 1680, "Snacks"),

        // -- limpieza
        P("DETERG-5L", "Detergente liquido 5L", 6800, "Limpieza"),
        P("JABON-3PK", "Jabon de bano 3-pack", 2450, "Limpieza"),
        P("CLORO-1L", "Cloro 1L", 1100, "Limpieza"),

        // -- electrohogar: LO UNICO QUE TRAE GARANTIA, y por eso la caja tiene que poder
        //    registrar los meses. Es lo que hace posible la pregunta de la garantia.
        P("OLLA-ARR-18", "Olla arrocera 1.8L", 45900, "Electrohogar", mesesDeGarantia: 12),
        P("LICUA-600", "Licuadora 600W", 28900, "Electrohogar", mesesDeGarantia: 24),
        P("MICROON-20", "Microondas 20L", 62000, "Electrohogar", mesesDeGarantia: 24),
        P("PLANCHA-V", "Plancha a vapor", 18500, "Electrohogar", mesesDeGarantia: 12),
    };

    private static ProductoDelCatalogo P(string sku, string descripcion, decimal precio,
        string categoria, int mesesDeGarantia = 0) =>
        new ProductoDelCatalogo
        {
            Sku = sku,
            Descripcion = descripcion,
            Precio = precio,
            Categoria = categoria,
            MesesDeGarantia = mesesDeGarantia,
        };
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
        "AceitesYGrasas", "Azucarados", "Snacks", "Embutidos", "ComidasPreparadas",
        "Mascotas", "Limpieza", "Electrohogar",
    };

    internal static bool EsCategoriaConocida(string categoria) =>
        CategoriasConocidas.Any(c => string.Equals(c, categoria, StringComparison.OrdinalIgnoreCase));

    private List<EventoDeCompra> Lista(string recurso, string id) =>
        porRecurso.GetOrAdd($"{recurso}/{id}", _ => Sembrar());

    // LA HISTORIA sembrada, la misma que usa la consola para medir. Las fechas son
    // literales a proposito: una demo con fechas relativas al dia de hoy deja de ser
    // reproducible, y la comparacion de canastas depende de en que mes cae cada compra.
    //
    // YA NO ESTA ESCRITA ACA. Vive en guion.json, y la genera generar-guion.py, que ADEMAS
    // verifica los invariantes del ecosistema y se cae si uno no cierra. La razon es de
    // tamaño y de responsabilidad: los invariantes piden compras SEMANALES durante veinte
    // meses -87 tickets- con dos totales de agosto exactos al colon y cuentas de tickets
    // que hay que poder comprobar. Eso a mano no se sostiene, y peor: nadie podria decir
    // si los numeros cierran sin volver a sumarlos.
    //
    // El guion en una linea: en 2025 la canasta es de cocinar en casa -frescos todas las
    // semanas, proteina fresca, helado una vez al mes-; en enero de 2026 entran embutidos
    // y comidas preparadas, el helado pasa a semanal y los frescos bajan a quincenales. El
    // total sube 11%, no 50%: lo que entra DESPLAZA a lo que sale. Es la mitad "compras"
    // del cruce con el colesterol del laboratorio.
    private static List<EventoDeCompra> Sembrar()
    {
        // Al lado del ejecutable: el csproj lo copia a la salida.
        string ruta = Path.Combine(AppContext.BaseDirectory, "guion.json");
        if (!File.Exists(ruta))
            throw new FileNotFoundException(
                "No esta guion.json al lado del ejecutable. Se genera con "
                + "'python generar-guion.py' en Compras/EndPointDeEventos.", ruta);

        var opciones = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        List<EventoDeCompra> guion =
            JsonSerializer.Deserialize<List<EventoDeCompra>>(File.ReadAllText(ruta), opciones);

        if (guion is null || guion.Count == 0)
            throw new InvalidOperationException("guion.json no trae ninguna compra.");

        return guion;
    }

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
