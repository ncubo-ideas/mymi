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

// La estacion le da al DEVICE del usuario un access token corto y un refresh token
// durable y rotatorio. El scope queda acotado a leer los eventos de UN recurso concreto:
// nada de cargas de otros clientes, ni perfil, ni facturacion.
app.MapPost("/device/token", (PedidoDeToken pedido) =>
{
    if (string.IsNullOrWhiteSpace(pedido?.DeviceKey)) return Results.BadRequest(new { error = "deviceKey requerido" });

    string recurso = Vacio(pedido.Recurso) ? "cargas" : pedido.Recurso;
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

    return Results.Ok(new
    {
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

// El carnet de socia. El numero es DE CARA AL CLIENTE: se dicta por telefono y se
// muestra en la bomba. La llave con la que la app se baja el historial es otra cosa y no
// viaja por aca.
sealed class EventoDeMembresia : EventoDeLaEstacion
{
    public override string Tipo => "MembresiaEmitida";

    [JsonPropertyName("numeroDeSocio")] public string NumeroDeSocio { get; init; } = "";
    [JsonPropertyName("nombre")]        public string Nombre { get; init; } = "";
    [JsonPropertyName("desde")]         public DateTime Desde { get; init; }
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

sealed class EventoDeSorteo : EventoDeLaEstacion
{
    public override string Tipo => "SorteoAnunciado";

    [JsonPropertyName("codigo")]         public string Codigo { get; init; } = "";
    [JsonPropertyName("nombre")]         public string Nombre { get; init; } = "";
    [JsonPropertyName("premio")]         public string Premio { get; init; } = "";
    [JsonPropertyName("fechaDelSorteo")] public DateTime FechaDelSorteo { get; init; }
    [JsonPropertyName("montoPorBoleto")] public decimal MontoPorBoleto { get; init; }
    [JsonPropertyName("moneda")]         public string Moneda { get; init; } = "CRC";
}

sealed class EventoDeBoleto : EventoDeLaEstacion
{
    public override string Tipo => "BoletoEmitido";

    [JsonPropertyName("numeroDeBoleto")]      public string NumeroDeBoleto { get; init; } = "";
    [JsonPropertyName("codigoDelSorteo")]     public string CodigoDelSorteo { get; init; } = "";
    [JsonPropertyName("numeroDeTransaccion")] public string NumeroDeTransaccion { get; init; } = "";
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
    // La historia, realineada al CANON de Sonia: el odometro va de 49.850 a 60.962 con el
    // quiebre de enero -850 km/mes antes del ascenso, 1.400 despues-, y por eso las cargas
    // se aprietan de una cada 17 dias a una cada 8. Adentro van los trece tramos de 12.6
    // km/L (la linea base), la carga PRESETEADA de 20.000 L exactos (la inferencia dice
    // "no fue lleno" y acierta), la caida que empieza en 11.7, la carga en OTRO SURTIDOR
    // (el tramo no comparable), la carga a la que Sonia no le va a poner kilometraje, y el
    // cierre de hoy que da 9.4.
    //
    // ESTA TABLA LA GENERA EL CANON, no el gusto: los litros salen de dividir la distancia
    // entre el rendimiento del tramo, y la fecha de invertir la curva del odometro. Editar
    // un numero suelto aca descuadra la serie sin que nada se queje. La copia que mide el
    // dominio esta en Muestras.cs y las dos tienen que moverse juntas.
    //
    // Los totales cuadran EXACTO con precio por volumen: el caso que no cuadra se
    // demuestra inyectando, no de fabrica.
    private static List<EventoDeLaEstacion> Sembrar() => new()
    {
        Carga(201, new DateTime(2025, 12, 15, 8, 10, 0), 37m, "A-3", 615m, 22755m, "automatico", "T-0201"),
        Carga(202, new DateTime(2026, 1, 1, 9, 20, 0), 36m, "A-3", 620m, 22320m, "automatico", "T-0202"),
        Carga(203, new DateTime(2026, 1, 17, 8, 45, 0), 38m, "A-3", 620m, 23560m, "automatico", "T-0203"),
        Carga(204, new DateTime(2026, 1, 27, 7, 55, 0), 37m, "A-3", 620m, 22940m, "automatico", "T-0204"),
        Carga(205, new DateTime(2026, 2, 6, 8, 15, 0), 36m, "A-3", 628m, 22608m, "automatico", "T-0205"),
        Carga(206, new DateTime(2026, 2, 16, 9, 5, 0), 36m, "A-3", 628m, 22608m, "automatico", "T-0206"),
        Carga(207, new DateTime(2026, 2, 21, 17, 30, 0), 20m, "A-3", 628m, 12560m, "preseteado", "T-0207"),
        Carga(208, new DateTime(2026, 3, 2, 8, 40, 0), 34m, "A-3", 634m, 21556m, "automatico", "T-0208"),
        Carga(209, new DateTime(2026, 3, 12, 7, 40, 0), 37m, "A-3", 634m, 23458m, "automatico", "T-0209"),
        Carga(210, new DateTime(2026, 3, 22, 9, 30, 0), 36m, "A-3", 634m, 22824m, "automatico", "T-0210"),
        Carga(211, new DateTime(2026, 4, 1, 8, 10, 0), 38m, "A-3", 640m, 24320m, "automatico", "T-0211"),
        Carga(212, new DateTime(2026, 4, 11, 9, 20, 0), 36m, "A-3", 640m, 23040m, "automatico", "T-0212"),
        Carga(213, new DateTime(2026, 4, 21, 8, 45, 0), 37m, "A-3", 640m, 23680m, "automatico", "T-0213"),
        Carga(214, new DateTime(2026, 5, 2, 7, 55, 0), 38m, "A-3", 645m, 24510m, "automatico", "T-0214"),
        Carga(215, new DateTime(2026, 5, 12, 8, 15, 0), 36m, "A-3", 645m, 23220m, "automatico", "T-0215"),
        Carga(216, new DateTime(2026, 5, 21, 9, 5, 0), 38m, "A-3", 645m, 24510m, "automatico", "T-0216"),
        Carga(217, new DateTime(2026, 5, 30, 8, 30, 0), 36m, "A-3", 645m, 23220m, "automatico", "T-0217"),
        Carga(218, new DateTime(2026, 6, 2, 8, 40, 0), 10m, "B-1", 652m, 6520m, "automatico", "T-0218"),
        Carga(219, new DateTime(2026, 6, 12, 7, 40, 0), 39m, "A-3", 652m, 25428m, "automatico", "T-0219"),
        Carga(220, new DateTime(2026, 6, 21, 9, 30, 0), 37m, "A-3", 652m, 24124m, "automatico", "T-0220"),
        Carga(221, new DateTime(2026, 6, 30, 8, 10, 0), 38m, "A-3", 652m, 24776m, "automatico", "T-0221"),
        Carga(222, new DateTime(2026, 7, 9, 9, 20, 0), 37m, "A-3", 660m, 24420m, "automatico", "T-0222"),
        Carga(223, new DateTime(2026, 7, 17, 8, 45, 0), 39m, "A-3", 660m, 25740m, "automatico", "T-0223"),
        Carga(224, new DateTime(2026, 7, 25, 7, 55, 0), 38m, "A-3", 660m, 25080m, "automatico", "T-0224"),
        Carga(225, new DateTime(2026, 8, 1, 8, 15, 0), 36m, "A-3", 668m, 24048m, "automatico", "T-0225"),
        Carga(226, new DateTime(2026, 8, 9, 9, 5, 0), 39m, "A-3", 668m, 26052m, "automatico", "T-0226"),
        Carga(227, new DateTime(2026, 8, 17, 8, 30, 0), 39m, "A-3", 668m, 26052m, "automatico", "T-0227"),
        Carga(228, new DateTime(2026, 8, 25, 8, 40, 0), 40m, "A-3", 668m, 26720m, "automatico", "T-0228"),

        // La lealtad, discreta: un punto por litro entero (tasa del demo). Con 998 puntos
        // la serie cruza LOS DOS umbrales del programa -200 y 800-, asi que la estacion
        // promueve dos veces: plata en febrero y oro en julio.
        Puntos(229, new DateTime(2025, 12, 15, 8, 10, 5), 37),
        Puntos(230, new DateTime(2026, 1, 1, 9, 20, 5), 36),
        Puntos(231, new DateTime(2026, 1, 17, 8, 45, 5), 38),
        Puntos(232, new DateTime(2026, 1, 27, 7, 55, 5), 37),
        Puntos(233, new DateTime(2026, 2, 6, 8, 15, 5), 36),
        Puntos(234, new DateTime(2026, 2, 16, 9, 5, 5), 36),
        new EventoDeNivel { Id = 235, Fecha = new DateTime(2026, 2, 16, 9, 5, 7), Nivel = "plata" },
        Puntos(236, new DateTime(2026, 2, 21, 17, 30, 5), 20),
        Puntos(237, new DateTime(2026, 3, 2, 8, 40, 5), 34),
        Puntos(238, new DateTime(2026, 3, 12, 7, 40, 5), 37),
        Puntos(239, new DateTime(2026, 3, 22, 9, 30, 5), 36),
        Puntos(240, new DateTime(2026, 4, 1, 8, 10, 5), 38),
        Puntos(241, new DateTime(2026, 4, 11, 9, 20, 5), 36),
        Puntos(242, new DateTime(2026, 4, 21, 8, 45, 5), 37),
        Puntos(243, new DateTime(2026, 5, 2, 7, 55, 5), 38),
        Puntos(244, new DateTime(2026, 5, 12, 8, 15, 5), 36),
        Puntos(245, new DateTime(2026, 5, 21, 9, 5, 5), 38),
        Puntos(246, new DateTime(2026, 5, 30, 8, 30, 5), 36),
        Puntos(247, new DateTime(2026, 6, 2, 8, 40, 5), 10),
        Puntos(248, new DateTime(2026, 6, 12, 7, 40, 5), 39),
        Puntos(249, new DateTime(2026, 6, 21, 9, 30, 5), 37),
        Puntos(250, new DateTime(2026, 6, 30, 8, 10, 5), 38),
        Puntos(251, new DateTime(2026, 7, 9, 9, 20, 5), 37),
        Puntos(252, new DateTime(2026, 7, 17, 8, 45, 5), 39),
        new EventoDeNivel { Id = 253, Fecha = new DateTime(2026, 7, 17, 8, 45, 7), Nivel = "oro" },
        Puntos(254, new DateTime(2026, 7, 25, 7, 55, 5), 38),
        Puntos(255, new DateTime(2026, 8, 1, 8, 15, 5), 36),
        Puntos(256, new DateTime(2026, 8, 9, 9, 5, 5), 39),
        Puntos(257, new DateTime(2026, 8, 17, 8, 30, 5), 39),
        Puntos(258, new DateTime(2026, 8, 25, 8, 40, 5), 40),

        // LA TOMBOLA. Un boleto por cada 10 000 colones de una carga, y el resto NO se
        // acumula para la siguiente. Los boletos los EMITE la estacion: el sorteo se rifa
        // sobre numeros, asi que uno fabricado por la app no serviria para reclamar nada.
        //
        // SOLO SE AGREGA AL FINAL, del 259 en adelante, y 201-258 no se renumeran nunca:
        // una bitacora existente pide after=258 y unos boletos intercalados no le llegarian
        // jamas — sin error, porque el Check los rechazaria en silencio.
        //
        // DOS SORTEOS para que el vencimiento se pueda enseñar: al "hoy" del arnes
        // (2026-08-25) el de junio ya se rifo y el de diciembre sigue abierto.
        Sorteo(259, new DateTime(2025, 12, 1), "SORTEO-2026-06", "Tómbola de medio año",
            "un Hyundai Grand i10", new DateTime(2026, 6, 15), 10000m),

        // Treinta y cinco boletos, dos por carga salvo la del surtidor B-1: esos 6.520
        // colones no llegan al corte y NO dan ninguno. Es el unico caso del guion donde
        // una carga se queda sin boleto, y ensena que el resto no se acumula.
        Boleto(260, new DateTime(2025, 12, 15, 8, 10, 10), "B-000101", "SORTEO-2026-06", "T-0201"),
        Boleto(261, new DateTime(2025, 12, 15, 8, 10, 11), "B-000102", "SORTEO-2026-06", "T-0201"),
        Boleto(262, new DateTime(2026, 1, 1, 9, 20, 10), "B-000103", "SORTEO-2026-06", "T-0202"),
        Boleto(263, new DateTime(2026, 1, 1, 9, 20, 11), "B-000104", "SORTEO-2026-06", "T-0202"),
        Boleto(264, new DateTime(2026, 1, 17, 8, 45, 10), "B-000105", "SORTEO-2026-06", "T-0203"),
        Boleto(265, new DateTime(2026, 1, 17, 8, 45, 11), "B-000106", "SORTEO-2026-06", "T-0203"),
        Boleto(266, new DateTime(2026, 1, 27, 7, 55, 10), "B-000107", "SORTEO-2026-06", "T-0204"),
        Boleto(267, new DateTime(2026, 1, 27, 7, 55, 11), "B-000108", "SORTEO-2026-06", "T-0204"),
        Boleto(268, new DateTime(2026, 2, 6, 8, 15, 10), "B-000109", "SORTEO-2026-06", "T-0205"),
        Boleto(269, new DateTime(2026, 2, 6, 8, 15, 11), "B-000110", "SORTEO-2026-06", "T-0205"),
        Boleto(270, new DateTime(2026, 2, 16, 9, 5, 10), "B-000111", "SORTEO-2026-06", "T-0206"),
        Boleto(271, new DateTime(2026, 2, 16, 9, 5, 11), "B-000112", "SORTEO-2026-06", "T-0206"),
        Boleto(272, new DateTime(2026, 2, 21, 17, 30, 10), "B-000113", "SORTEO-2026-06", "T-0207"),
        Boleto(273, new DateTime(2026, 3, 2, 8, 40, 10), "B-000114", "SORTEO-2026-06", "T-0208"),
        Boleto(274, new DateTime(2026, 3, 2, 8, 40, 11), "B-000115", "SORTEO-2026-06", "T-0208"),
        Boleto(275, new DateTime(2026, 3, 12, 7, 40, 10), "B-000116", "SORTEO-2026-06", "T-0209"),
        Boleto(276, new DateTime(2026, 3, 12, 7, 40, 11), "B-000117", "SORTEO-2026-06", "T-0209"),
        Boleto(277, new DateTime(2026, 3, 22, 9, 30, 10), "B-000118", "SORTEO-2026-06", "T-0210"),
        Boleto(278, new DateTime(2026, 3, 22, 9, 30, 11), "B-000119", "SORTEO-2026-06", "T-0210"),
        Boleto(279, new DateTime(2026, 4, 1, 8, 10, 10), "B-000120", "SORTEO-2026-06", "T-0211"),
        Boleto(280, new DateTime(2026, 4, 1, 8, 10, 11), "B-000121", "SORTEO-2026-06", "T-0211"),
        Boleto(281, new DateTime(2026, 4, 11, 9, 20, 10), "B-000122", "SORTEO-2026-06", "T-0212"),
        Boleto(282, new DateTime(2026, 4, 11, 9, 20, 11), "B-000123", "SORTEO-2026-06", "T-0212"),
        Boleto(283, new DateTime(2026, 4, 21, 8, 45, 10), "B-000124", "SORTEO-2026-06", "T-0213"),
        Boleto(284, new DateTime(2026, 4, 21, 8, 45, 11), "B-000125", "SORTEO-2026-06", "T-0213"),
        Boleto(285, new DateTime(2026, 5, 2, 7, 55, 10), "B-000126", "SORTEO-2026-06", "T-0214"),
        Boleto(286, new DateTime(2026, 5, 2, 7, 55, 11), "B-000127", "SORTEO-2026-06", "T-0214"),
        Boleto(287, new DateTime(2026, 5, 12, 8, 15, 10), "B-000128", "SORTEO-2026-06", "T-0215"),
        Boleto(288, new DateTime(2026, 5, 12, 8, 15, 11), "B-000129", "SORTEO-2026-06", "T-0215"),
        Boleto(289, new DateTime(2026, 5, 21, 9, 5, 10), "B-000130", "SORTEO-2026-06", "T-0216"),
        Boleto(290, new DateTime(2026, 5, 21, 9, 5, 11), "B-000131", "SORTEO-2026-06", "T-0216"),
        Boleto(291, new DateTime(2026, 5, 30, 8, 30, 10), "B-000132", "SORTEO-2026-06", "T-0217"),
        Boleto(292, new DateTime(2026, 5, 30, 8, 30, 11), "B-000133", "SORTEO-2026-06", "T-0217"),
        Boleto(293, new DateTime(2026, 6, 12, 7, 40, 10), "B-000134", "SORTEO-2026-06", "T-0219"),
        Boleto(294, new DateTime(2026, 6, 12, 7, 40, 11), "B-000135", "SORTEO-2026-06", "T-0219"),

        // LOS DIECIOCHO DE DICIEMBRE LLEGAN ANTES QUE SU ANUNCIO, y es a proposito: el
        // desorden de llegada es real -los eventos vienen en el orden en que la estacion
        // los publico- y asi el guion de fabrica lo ejercita en CADA corrida, en vez de
        // quedar escondido en un test que nadie mira.
        Boleto(295, new DateTime(2026, 6, 21, 9, 30, 10), "B-000201", "SORTEO-2026-12", "T-0220"),
        Boleto(296, new DateTime(2026, 6, 21, 9, 30, 11), "B-000202", "SORTEO-2026-12", "T-0220"),
        Boleto(297, new DateTime(2026, 6, 30, 8, 10, 10), "B-000203", "SORTEO-2026-12", "T-0221"),
        Boleto(298, new DateTime(2026, 6, 30, 8, 10, 11), "B-000204", "SORTEO-2026-12", "T-0221"),
        Boleto(299, new DateTime(2026, 7, 9, 9, 20, 10), "B-000205", "SORTEO-2026-12", "T-0222"),
        Boleto(300, new DateTime(2026, 7, 9, 9, 20, 11), "B-000206", "SORTEO-2026-12", "T-0222"),
        Boleto(301, new DateTime(2026, 7, 17, 8, 45, 10), "B-000207", "SORTEO-2026-12", "T-0223"),
        Boleto(302, new DateTime(2026, 7, 17, 8, 45, 11), "B-000208", "SORTEO-2026-12", "T-0223"),
        Boleto(303, new DateTime(2026, 7, 25, 7, 55, 10), "B-000209", "SORTEO-2026-12", "T-0224"),
        Boleto(304, new DateTime(2026, 7, 25, 7, 55, 11), "B-000210", "SORTEO-2026-12", "T-0224"),
        Boleto(305, new DateTime(2026, 8, 1, 8, 15, 10), "B-000211", "SORTEO-2026-12", "T-0225"),
        Boleto(306, new DateTime(2026, 8, 1, 8, 15, 11), "B-000212", "SORTEO-2026-12", "T-0225"),
        Boleto(307, new DateTime(2026, 8, 9, 9, 5, 10), "B-000213", "SORTEO-2026-12", "T-0226"),
        Boleto(308, new DateTime(2026, 8, 9, 9, 5, 11), "B-000214", "SORTEO-2026-12", "T-0226"),
        Boleto(309, new DateTime(2026, 8, 17, 8, 30, 10), "B-000215", "SORTEO-2026-12", "T-0227"),
        Boleto(310, new DateTime(2026, 8, 17, 8, 30, 11), "B-000216", "SORTEO-2026-12", "T-0227"),
        Boleto(311, new DateTime(2026, 8, 25, 8, 40, 10), "B-000217", "SORTEO-2026-12", "T-0228"),
        Boleto(312, new DateTime(2026, 8, 25, 8, 40, 11), "B-000218", "SORTEO-2026-12", "T-0228"),

        Sorteo(313, new DateTime(2026, 8, 26), "SORTEO-2026-12", "Tómbola de fin de año",
            "un Toyota Yaris", new DateTime(2026, 12, 15), 10000m),

        // El carnet, que en la vida real llega el dia que uno se afilia y aca llega
        // ultimo por la misma razon que los boletos: solo se agrega al final.
        new EventoDeMembresia
        {
            Id = 314,
            Fecha = new DateTime(2026, 8, 26, 9, 0, 0),
            NumeroDeSocio = "SOCIO-000142",
            Nombre = "Sonia Vargas",
            Desde = new DateTime(2025, 11, 3),
        },
    };

    private static EventoDeCarga Carga(long id, DateTime fecha, decimal litros, string surtidor,
        decimal precio, decimal total, string corte, string transaccion) =>
        new EventoDeCarga
        {
            Id = id,
            Fecha = fecha,
            Placa = "BQR-482",
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

    private static EventoDeSorteo Sorteo(long id, DateTime fecha, string codigo, string nombre,
        string premio, DateTime fechaDelSorteo, decimal montoPorBoleto) =>
        new EventoDeSorteo
        {
            Id = id, Fecha = fecha, Codigo = codigo, Nombre = nombre, Premio = premio,
            FechaDelSorteo = fechaDelSorteo, MontoPorBoleto = montoPorBoleto, Moneda = "CRC",
        };

    private static EventoDeBoleto Boleto(long id, DateTime fecha, string numero,
        string sorteo, string transaccion) =>
        new EventoDeBoleto
        {
            Id = id, Fecha = fecha, NumeroDeBoleto = numero,
            CodigoDelSorteo = sorteo, NumeroDeTransaccion = transaccion,
        };

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
