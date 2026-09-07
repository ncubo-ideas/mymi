using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text.Json.Serialization;

// Mock del servidor de la estacion de servicio. Happy path, estado en memoria.
//
// Lo que se simula no es "la estacion registra una venta" sino "UN SURTIDOR DESPACHA"
// (§6.3.5 del brief): cada CargaRegistrada trae el surtidor con su precio de ese
// momento y la forma de corte, que es la huella con la que la app puede inferir el
// tanque lleno.
//
// El diseno central: un token de device declara QUE puede hacer y SOBRE QUE recurso, y
// cada endpoint declara que scope exige. La app solo consigue "{recurso}.events.read",
// asi que cualquier otro endpoint la rechaza porque el token NO LO TIENE - no por un if
// contra una ruta. Eso es lo que vuelve falsable el invariante "la app no es cliente de
// los servicios generales de la estacion": el senuelo /perfil devuelve 403
// estructuralmente, no por cortesia.
//
// Para este negocio el juego de URLs es:
//
//   GET  /cargas/{id}/events?after=N     scope cargas.events.read
//   GET  /cargas/{id}/perfil             scope cargas.profile.read  -> 403, el senuelo

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
// La estacion le da al DEVICE del usuario un access token corto y un refresh token
// durable y rotatorio. El scope queda acotado a leer los eventos de UN recurso concreto:
// nada de cargas de otros clientes, ni perfil, ni facturacion.
app.MapPost("/device/token", (PedidoDeToken pedido) =>
{
    if (string.IsNullOrWhiteSpace(pedido?.DeviceKey)) return Results.BadRequest(new { error = "deviceKey requerido" });

    string recurso = Vacio(pedido.Recurso) ? "cargas" : pedido.Recurso;
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
        events = almacen.Posteriores(recurso, id, desde),
        last = almacen.UltimoNumero(recurso, id),
    });
});

// EL SENUELO. Existe para demostrar el invariante, no para servir a nadie: exige un
// scope de perfil que ningun token de device tiene, asi que responde 403 con la
// credencial de la app. Es la demostracion falsable de que la app no es cliente de los
// servicios generales de la estacion.
app.MapGet("/{recurso}/{id}/perfil", (string recurso, string id, HttpRequest pedido) =>
{
    IResult negado = concesiones.Exigir(pedido, $"{recurso}.profile.read", recurso, id);
    if (negado != null) return negado;

    return Results.Ok(new { nombre = "nunca deberia verse", recurso, id });
});

// ---------------------------------------------------------------- control del mock

// Un surtidor despacha una carga CONCRETA. Es lo que usa el inyector de demo: una demo
// se cuenta con valores que uno eligio, porque los tramos dependen de los numeros.
// Accion DE LA ESTACION, no de la app: sin token.
app.MapPost("/{recurso}/{id}/events/carga", (string recurso, string id, CargaAInyectar pedido) =>
{
    if (pedido is null) return Results.BadRequest(new { error = "cuerpo requerido" });
    if (Vacio(pedido.Placa)) return Results.BadRequest(new { error = "placa requerida" });
    if (Vacio(pedido.NumeroDeTransaccion)) return Results.BadRequest(new { error = "numeroDeTransaccion requerido" });
    if (pedido.VolumenValor <= 0) return Results.BadRequest(new { error = "volumenValor debe ser positivo" });
    if (pedido.PrecioMonto <= 0 || pedido.TotalMonto <= 0)
        return Results.BadRequest(new { error = "precioMonto y totalMonto deben ser positivos" });

    EventoDeCarga nuevo = almacen.AgregarCarga(recurso, id, pedido);
    return Results.Ok(new { agregado = nuevo.Id, transaccion = nuevo.NumeroDeTransaccion, resultado = nuevo.Resultado() });
});

// La lealtad tambien es narrativa de la estacion: los puntos LLEGAN, la app no los
// calcula. Si el programa cambia la tasa, el historico no se recalcula.
app.MapPost("/{recurso}/{id}/events/puntos", (string recurso, string id, PuntosAInyectar pedido) =>
{
    if (pedido is null || pedido.Puntos <= 0)
        return Results.BadRequest(new { error = "puntos debe ser positivo" });

    EventoDePuntos nuevo = almacen.AgregarPuntos(recurso, id, pedido);
    return Results.Ok(new { agregado = nuevo.Id, puntos = nuevo.Puntos });
});

app.MapPost("/{recurso}/{id}/events/nivel", (string recurso, string id, NivelAInyectar pedido) =>
{
    if (pedido is null || Vacio(pedido.Nivel))
        return Results.BadRequest(new { error = "nivel requerido" });

    EventoDeNivel nuevo = almacen.AgregarNivel(recurso, id, pedido);
    return Results.Ok(new { agregado = nuevo.Id, nivel = nuevo.Nivel });
});

// Expira SOLO los access tokens y deja los refresh vivos. Es el caso que el diseno
// declara normal - "access token corto, refresh token durable/rotatorio" - y sin una
// forma de provocarlo no hay manera de demostrar que la app rota sola en vez de pedir
// una credencial nueva. Control del mock, no de la estacion.
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

// ---------------------------------------------------------------- los eventos

// Tres tipos de evento con un campo `tipo` que discrimina, y el WIRE PLANO a proposito:
// precioMonto/precioMoneda y no un objeto anidado, porque el serializador emite todos
// los campos de un objeto aunque esten en cero y la app aprendio (en examenes, medido)
// a validar por VALOR y no por presencia.
abstract class EventoDeLaEstacion
{
    [JsonPropertyName("id")]    public long Id { get; init; }
    [JsonPropertyName("tipo")]  public abstract string Tipo { get; }
    [JsonPropertyName("fecha")] public DateTime Fecha { get; init; }
}

// Un surtidor despacho. Lo que la estacion SABE: el instrumento con su precio, el
// volumen, el total autoritativo, la forma de corte y la placa que capturo. Lo que NO
// viaja, porque no lo sabe: el kilometraje, si el tanque quedo lleno, que carro es.
sealed class EventoDeCarga : EventoDeLaEstacion
{
    public override string Tipo => "CargaRegistrada";

    [JsonPropertyName("placa")]                  public string Placa { get; init; } = "";
    [JsonPropertyName("tipoDeCombustible")]      public string TipoDeCombustible { get; init; } = "";
    [JsonPropertyName("surtidorIdentificacion")] public string SurtidorIdentificacion { get; init; } = "";
    [JsonPropertyName("surtidorEstacion")]       public string SurtidorEstacion { get; init; } = "";
    [JsonPropertyName("precioMonto")]            public decimal PrecioMonto { get; init; }
    [JsonPropertyName("precioMoneda")]           public string PrecioMoneda { get; init; } = "";
    [JsonPropertyName("volumenValor")]           public decimal VolumenValor { get; init; }
    [JsonPropertyName("volumenUnidad")]          public string VolumenUnidad { get; init; } = "";
    [JsonPropertyName("totalMonto")]             public decimal TotalMonto { get; init; }
    [JsonPropertyName("totalMoneda")]            public string TotalMoneda { get; init; } = "";
    [JsonPropertyName("formaDeCorte")]           public string FormaDeCorte { get; init; } = "";
    [JsonPropertyName("numeroDeTransaccion")]    public string NumeroDeTransaccion { get; init; } = "";

    internal string Resultado() =>
        $"{VolumenValor} {VolumenUnidad} de {TipoDeCombustible} en {SurtidorIdentificacion} "
        + $"({FormaDeCorte}) = {TotalMonto} {TotalMoneda}";
}

sealed class EventoDePuntos : EventoDeLaEstacion
{
    public override string Tipo => "PuntosAcreditados";

    [JsonPropertyName("puntos")] public int Puntos { get; init; }
}

sealed class EventoDeNivel : EventoDeLaEstacion
{
    public override string Tipo => "NivelActualizado";

    [JsonPropertyName("nivel")] public string Nivel { get; init; } = "";
}

sealed class AlmacenDeEventos
{
    private readonly ConcurrentDictionary<string, List<EventoDeLaEstacion>> porRecurso = new();
    private readonly Lock candado = new();

    private List<EventoDeLaEstacion> Lista(string recurso, string id) =>
        porRecurso.GetOrAdd($"{recurso}/{id}", _ => Sembrar());

    // EL GUION sembrado, el mismo de Muestras.cs de la consola (§13 del brief, v2). Las
    // fechas son literales a proposito: una demo con fechas relativas al dia de hoy deja
    // de ser reproducible, y los tramos dependen de los dias transcurridos.
    //
    // La historia: tres tramos parejos de 12.6 km/L (la linea base), la carga PRESETEADA
    // de 20.000 L exactos (la inferencia dice "no fue lleno" y acierta), la caida que
    // empieza en 11.7, la carga en OTRO SURTIDOR (el tramo no comparable), la carga a la
    // que Sonia no le va a poner kilometraje, y el cierre de hoy que da 9.4.
    //
    // Los totales cuadran EXACTO con precio por volumen: el caso que no cuadra se
    // demuestra inyectando, no de fabrica.
    private static List<EventoDeLaEstacion> Sembrar() => new()
    {
        Carga(201, new DateTime(2025, 12, 15, 8, 10, 0), 40m, "A-3", 615m, 24600m, "automatico", "T-0201"),
        Carga(202, new DateTime(2026, 1, 15, 9, 20, 0), 38m, "A-3", 620m, 23560m, "automatico", "T-0202"),
        Carga(203, new DateTime(2026, 2, 15, 8, 45, 0), 39m, "A-3", 628m, 24492m, "automatico", "T-0203"),
        Carga(204, new DateTime(2026, 3, 20, 17, 30, 0), 20m, "A-3", 634m, 12680m, "preseteado", "T-0204"),
        Carga(205, new DateTime(2026, 3, 28, 8, 15, 0), 19m, "A-3", 634m, 12046m, "automatico", "T-0205"),
        Carga(206, new DateTime(2026, 5, 20, 8, 50, 0), 41m, "A-3", 645m, 26445m, "automatico", "T-0206"),
        Carga(207, new DateTime(2026, 6, 20, 9, 5, 0), 42m, "B-1", 652m, 27384m, "automatico", "T-0207"),
        Carga(208, new DateTime(2026, 7, 25, 8, 30, 0), 43m, "A-3", 660m, 28380m, "automatico", "T-0208"),
        Carga(209, new DateTime(2026, 8, 25, 8, 40, 0), 44m, "A-3", 668m, 29392m, "automatico", "T-0209"),

        // La lealtad, discreta: un punto por litro entero (tasa del demo), y el nivel
        // que la estacion decidio subir a mitad de la serie.
        Puntos(210, new DateTime(2025, 12, 15, 8, 10, 5), 40),
        Puntos(211, new DateTime(2026, 1, 15, 9, 20, 5), 38),
        Puntos(212, new DateTime(2026, 2, 15, 8, 45, 5), 39),
        Puntos(213, new DateTime(2026, 3, 20, 17, 30, 5), 20),
        Puntos(214, new DateTime(2026, 3, 28, 8, 15, 5), 19),
        Puntos(215, new DateTime(2026, 5, 20, 8, 50, 5), 41),
        Puntos(216, new DateTime(2026, 6, 20, 9, 5, 5), 42),
        new EventoDeNivel { Id = 217, Fecha = new DateTime(2026, 6, 20, 9, 5, 10), Nivel = "plata" },
        Puntos(218, new DateTime(2026, 7, 25, 8, 30, 5), 43),
        Puntos(219, new DateTime(2026, 8, 25, 8, 40, 5), 44),
    };

    private static EventoDeCarga Carga(long id, DateTime fecha, decimal litros, string surtidor,
        decimal precio, decimal total, string corte, string transaccion) =>
        new EventoDeCarga
        {
            Id = id,
            Fecha = fecha,
            Placa = "ABC-123",
            TipoDeCombustible = "regular",
            SurtidorIdentificacion = surtidor,
            SurtidorEstacion = "La Sabana",
            PrecioMonto = precio,
            PrecioMoneda = "CRC",
            VolumenValor = litros,
            VolumenUnidad = "L",
            TotalMonto = total,
            TotalMoneda = "CRC",
            FormaDeCorte = corte,
            NumeroDeTransaccion = transaccion,
        };

    private static EventoDePuntos Puntos(long id, DateTime fecha, int puntos) =>
        new EventoDePuntos { Id = id, Fecha = fecha, Puntos = puntos };

    // La lista viaja como object para que el serializador emita el tipo CONCRETO de cada
    // evento: declarada como base, un EventoDeCarga saldria sin sus campos.
    internal List<object> Posteriores(string recurso, string id, long after)
    {
        List<EventoDeLaEstacion> lista = Lista(recurso, id);
        lock (candado)
            return lista.Where(e => e.Id > after).OrderBy(e => e.Id).Cast<object>().ToList();
    }

    internal long UltimoNumero(string recurso, string id)
    {
        List<EventoDeLaEstacion> lista = Lista(recurso, id);
        lock (candado)
            return lista.Count == 0 ? 0 : lista.Max(e => e.Id);
    }

    internal EventoDeCarga AgregarCarga(string recurso, string id, CargaAInyectar pedido)
    {
        List<EventoDeLaEstacion> lista = Lista(recurso, id);

        lock (candado)
        {
            var evento = new EventoDeCarga
            {
                Id = Siguiente(lista),
                Fecha = pedido.Fecha == default ? DateTime.Now : pedido.Fecha,
                Placa = pedido.Placa,
                TipoDeCombustible = ODefecto(pedido.TipoDeCombustible, "regular"),
                SurtidorIdentificacion = ODefecto(pedido.Surtidor, "A-3"),
                SurtidorEstacion = ODefecto(pedido.Estacion, "La Sabana"),
                PrecioMonto = pedido.PrecioMonto,
                PrecioMoneda = ODefecto(pedido.PrecioMoneda, "CRC"),
                VolumenValor = pedido.VolumenValor,
                VolumenUnidad = ODefecto(pedido.VolumenUnidad, "L"),
                TotalMonto = pedido.TotalMonto,
                TotalMoneda = ODefecto(pedido.TotalMoneda, "CRC"),
                FormaDeCorte = ODefecto(pedido.FormaDeCorte, "automatico"),
                NumeroDeTransaccion = pedido.NumeroDeTransaccion,
            };

            lista.Add(evento);
            return evento;
        }
    }

    private static string ODefecto(string valor, string porDefecto) =>
        string.IsNullOrWhiteSpace(valor) ? porDefecto : valor;

    internal EventoDePuntos AgregarPuntos(string recurso, string id, PuntosAInyectar pedido)
    {
        List<EventoDeLaEstacion> lista = Lista(recurso, id);

        lock (candado)
        {
            var evento = new EventoDePuntos
            {
                Id = Siguiente(lista),
                Fecha = pedido.Fecha == default ? DateTime.Now : pedido.Fecha,
                Puntos = pedido.Puntos,
            };
            lista.Add(evento);
            return evento;
        }
    }

    internal EventoDeNivel AgregarNivel(string recurso, string id, NivelAInyectar pedido)
    {
        List<EventoDeLaEstacion> lista = Lista(recurso, id);

        lock (candado)
        {
            var evento = new EventoDeNivel
            {
                Id = Siguiente(lista),
                Fecha = pedido.Fecha == default ? DateTime.Now : pedido.Fecha,
                Nivel = pedido.Nivel,
            };
            lista.Add(evento);
            return evento;
        }
    }

    private static long Siguiente(List<EventoDeLaEstacion> lista) =>
        (lista.Count == 0 ? 200 : lista.Max(e => e.Id)) + 1;
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

// Los cuerpos que manda el inyector de eventos de demo.
sealed class CargaAInyectar
{
    [JsonPropertyName("fecha")]               public DateTime Fecha { get; set; }
    [JsonPropertyName("placa")]               public string Placa { get; set; }
    [JsonPropertyName("tipoDeCombustible")]   public string TipoDeCombustible { get; set; }
    [JsonPropertyName("surtidor")]            public string Surtidor { get; set; }
    [JsonPropertyName("estacion")]            public string Estacion { get; set; }
    [JsonPropertyName("precioMonto")]         public decimal PrecioMonto { get; set; }
    [JsonPropertyName("precioMoneda")]        public string PrecioMoneda { get; set; }
    [JsonPropertyName("volumenValor")]        public decimal VolumenValor { get; set; }
    [JsonPropertyName("volumenUnidad")]       public string VolumenUnidad { get; set; }
    [JsonPropertyName("totalMonto")]          public decimal TotalMonto { get; set; }
    [JsonPropertyName("totalMoneda")]         public string TotalMoneda { get; set; }
    [JsonPropertyName("formaDeCorte")]        public string FormaDeCorte { get; set; }
    [JsonPropertyName("numeroDeTransaccion")] public string NumeroDeTransaccion { get; set; }
}

sealed class PuntosAInyectar
{
    [JsonPropertyName("fecha")]  public DateTime Fecha { get; set; }
    [JsonPropertyName("puntos")] public int Puntos { get; set; }
}

sealed class NivelAInyectar
{
    [JsonPropertyName("fecha")] public DateTime Fecha { get; set; }
    [JsonPropertyName("nivel")] public string Nivel { get; set; }
}
