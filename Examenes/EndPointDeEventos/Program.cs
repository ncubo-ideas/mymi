using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text.Json.Serialization;

// Mock del servidor del laboratorio. Happy path, estado en memoria.
//
// El diseno central: un token de device declara QUE puede hacer y SOBRE QUE recurso, y
// cada endpoint declara que scope exige. La app solo consigue "{recurso}.events.read",
// asi que cualquier otro endpoint la rechaza porque el token NO LO TIENE - no por un if
// contra una ruta. Eso es lo que vuelve falsable el invariante "la app no es cliente de
// los servicios generales del laboratorio": el senuelo /perfil devuelve 403
// estructuralmente, no por cortesia.
//
// Las rutas estan parametrizadas por recurso (/{recurso}/{id}/events), asi que este
// mismo mecanismo sirve para el expediente de un paciente igual que servia para las
// facturas de un cliente. Para este negocio el juego de URLs es:
//
//   GET  /expediente/{id}/events?after=N     scope expediente.events.read
//   GET  /expediente/{id}/perfil             scope expediente.profile.read  -> 403, el senuelo

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

// El laboratorio le da al DEVICE del usuario un access token corto y un refresh token
// durable y rotatorio. El scope queda acotado a leer los eventos de UN recurso concreto:
// nada de resultados de otros pacientes, ni perfil, ni agenda.
app.MapPost("/device/token", (PedidoDeToken pedido) =>
{
    if (string.IsNullOrWhiteSpace(pedido?.DeviceKey)) return Results.BadRequest(new { error = "deviceKey requerido" });

    string recurso = Vacio(pedido.Recurso) ? "expediente" : pedido.Recurso;
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
    List<EventoDeExamen> eventos = almacen.Posteriores(recurso, id, desde);

    return Results.Ok(new
    {
        events = eventos,
        last = almacen.UltimoNumero(recurso, id),
    });
});

// EL SENUELO. Existe para demostrar el invariante, no para servir a nadie: exige un
// scope de perfil que ningun token de device tiene, asi que responde 403 con la
// credencial de la app. Es la demostracion falsable de que la app no es cliente de los
// servicios generales del laboratorio.
app.MapGet("/{recurso}/{id}/perfil", (string recurso, string id, HttpRequest pedido) =>
{
    IResult negado = concesiones.Exigir(pedido, $"{recurso}.profile.read", recurso, id);
    if (negado != null) return negado;

    return Results.Ok(new { nombre = "nunca deberia verse", recurso, id });
});

// ---------------------------------------------------------------- control del mock

// Registra un examen CONCRETO. Es lo que usa el inyector de eventos de demo: una demo
// se cuenta con valores que uno eligio, porque la historia depende de los numeros.
// Accion DEL LABORATORIO, no de la app: sin token.
app.MapPost("/{recurso}/{id}/events/examen", (string recurso, string id, ExamenAInyectar pedido) =>
{
    if (pedido is null || Vacio(pedido.Examen))
        return Results.BadRequest(new { error = "examen requerido" });
    if (!AlmacenDeEventos.EsExamenConocido(pedido.Examen))
        return Results.BadRequest(new
        {
            error = "examen desconocido",
            conocidos = AlmacenDeEventos.ExamenesConocidos,
        });
    if (pedido.Examen == "Orina" && Vacio(pedido.Color))
        return Results.BadRequest(new { error = "un examen de orina necesita color" });
    if (pedido.Examen != "Orina" && Vacio(pedido.Unidad))
        return Results.BadRequest(new { error = "un examen de sangre necesita unidad" });

    EventoDeExamen nuevo = almacen.Agregar(recurso, id, pedido);
    return Results.Ok(new { agregado = nuevo.Id, examen = nuevo.Examen, resultado = nuevo.Resultado() });
});

// Expira SOLO los access tokens y deja los refresh vivos. Es el caso que el diseno
// declara normal - "access token corto, refresh token durable/rotatorio" - y sin una
// forma de provocarlo no hay manera de demostrar que la app rota sola en vez de pedir
// una credencial nueva. Control del mock, no del laboratorio.
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

// Un evento de la narrativa del laboratorio. Un solo tipo de evento con un campo
// `examen` que discrimina: el laboratorio dice "te registre un examen", y cual es un
// dato. El que traduce ese campo a una clase del dominio es la app, en su frontera.
sealed class EventoDeExamen
{
    [JsonPropertyName("id")]              public long Id { get; init; }
    [JsonPropertyName("tipo")]            public string Tipo { get; init; } = "ExamenAgregado";
    [JsonPropertyName("examen")]          public string Examen { get; init; }
    [JsonPropertyName("fecha")]           public DateTime Fecha { get; init; }
    [JsonPropertyName("valor")]           public decimal Valor { get; init; }
    [JsonPropertyName("valorSecundario")] public decimal ValorSecundario { get; init; }
    [JsonPropertyName("unidad")]          public string Unidad { get; init; } = "";
    [JsonPropertyName("color")]           public string Color { get; init; } = "";

    // El urianalisis informa DOS cosas de forma distinta: un color (categoria sin orden)
    // y unos nitritos (presencia, sin escala). Que el mock las emita separadas es lo que
    // le permite al dominio modelarlas como dos magnitudes distintas en vez de aplanar
    // las dos a un string. Vacio significa que la prueba no se corrio, que NO es lo mismo
    // que negativo.
    [JsonPropertyName("nitritos")]        public string Nitritos { get; init; } = "";

    // La sangre oculta. El mismo campo sirve para orina y para heces: en el wire es un
    // binario con las mismas tres palabras. Lo que cambia -que una espere a la proxima
    // cita y la otra no- lo decide la app, no este mock.
    [JsonPropertyName("sangre")]          public string Sangre { get; init; } = "";

    internal string Resultado()
    {
        if (Examen != "Orina" && Examen != "Heces") return $"{Valor} {Unidad}";

        string texto = Color;
        if (!string.IsNullOrWhiteSpace(Nitritos)) texto += $", nitritos {Nitritos}";
        if (!string.IsNullOrWhiteSpace(Sangre)) texto += $", sangre {Sangre}";
        return texto;
    }
}

sealed class AlmacenDeEventos
{
    private readonly ConcurrentDictionary<string, List<EventoDeExamen>> porRecurso = new();
    private readonly Lock candado = new();

    internal static readonly string[] ExamenesConocidos =
        { "Colesterol", "ColesterolDetallado", "AntigenoProstatico", "Hemoglobina", "Orina", "Heces" };

    internal static bool EsExamenConocido(string examen) =>
        Array.IndexOf(ExamenesConocidos, examen) >= 0;

    private List<EventoDeExamen> Lista(string recurso, string id) =>
        porRecurso.GetOrAdd($"{recurso}/{id}", _ => Sembrar());

    // LA HISTORIA sembrada, la misma que usa la consola para medir. Las fechas son
    // literales a proposito: una demo con fechas relativas al dia de hoy deja de ser
    // reproducible, y las tendencias dependen de los dias transcurridos.
    //
    // Los tres primeros antigenos son la LINEA BASE. Sin ellos el 5.2 del final es un
    // numero cualquiera apenas arriba del techo; con ellos es un salto de 2.2 sobre un
    // paciente que venia planchado tres anos, y eso es lo que la app puede decir y una
    // pantalla de resultados no.
    private static List<EventoDeExamen> Sembrar() => new()
    {
        Sangre(201, "AntigenoProstatico", new DateTime(2023, 8, 21, 9, 10, 0), 3.0m, "ng/mL"),
        Sangre(202, "AntigenoProstatico", new DateTime(2024, 8, 21, 9, 5, 0), 3.1m, "ng/mL"),
        Sangre(203, "Colesterol", new DateTime(2025, 8, 21, 8, 40, 0), 200.0m, "mg/dL"),
        Sangre(204, "AntigenoProstatico", new DateTime(2025, 8, 21, 8, 41, 0), 3.0m, "ng/mL"),
        Sangre(205, "Colesterol", new DateTime(2026, 8, 21, 8, 30, 0), 280.0m, "mg/dL"),
        Sangre(206, "AntigenoProstatico", new DateTime(2026, 8, 21, 8, 31, 0), 5.2m, "ng/mL"),
        Sangre(207, "Hemoglobina", new DateTime(2026, 8, 21, 8, 32, 0), 14.2m, "g/dL"),
        new EventoDeExamen
        {
            Id = 208, Examen = "Orina", Fecha = new DateTime(2026, 8, 21, 8, 33, 0),
            Color = "ambar", Nitritos = "negativo", Sangre = "negativo",
        },

        // EL CONTRASTE DE ALVARO, en el guion. Una sangre oculta POSITIVA, y la app tiene
        // que decir "conviene revisarlo" y no alarmar: la causa mas frecuente es local.
        //
        // El mismo positivo en el examen de ARRIBA -la orina- diria que hay que verlo
        // pronto. Cambiar el "negativo" del 208 por "positivo" es la forma mas corta de
        // ver la diferencia en pantalla, y no esta asi de fabrica porque el hallazgo que
        // manda en esta demo es la velocidad del antigeno.
        new EventoDeExamen
        {
            Id = 209, Examen = "Heces", Fecha = new DateTime(2026, 8, 21, 8, 34, 0),
            Color = "chocolate", Sangre = "positivo",
        },
    };

    private static EventoDeExamen Sangre(long id, string examen, DateTime fecha, decimal valor, string unidad) =>
        new EventoDeExamen { Id = id, Examen = examen, Fecha = fecha, Valor = valor, Unidad = unidad };

    internal List<EventoDeExamen> Posteriores(string recurso, string id, long after)
    {
        List<EventoDeExamen> lista = Lista(recurso, id);
        lock (candado)
            return lista.Where(e => e.Id > after).OrderBy(e => e.Id).ToList();
    }

    internal long UltimoNumero(string recurso, string id)
    {
        List<EventoDeExamen> lista = Lista(recurso, id);
        lock (candado)
            return lista.Count == 0 ? 0 : lista.Max(e => e.Id);
    }

    // El examen que el operador de la demo eligio, con sus valores exactos.
    internal EventoDeExamen Agregar(string recurso, string id, ExamenAInyectar pedido)
    {
        List<EventoDeExamen> lista = Lista(recurso, id);

        lock (candado)
        {
            long siguiente = (lista.Count == 0 ? 200 : lista.Max(e => e.Id)) + 1;
            var evento = new EventoDeExamen
            {
                Id = siguiente,
                Examen = pedido.Examen,
                Fecha = pedido.Fecha == default ? DateTime.Now : pedido.Fecha,
                Valor = pedido.Valor,
                ValorSecundario = pedido.ValorSecundario,
                Unidad = pedido.Unidad ?? "",
                Color = pedido.Color ?? "",
                Nitritos = pedido.Nitritos ?? "",
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
sealed class ExamenAInyectar
{
    [JsonPropertyName("examen")]          public string Examen { get; set; }
    [JsonPropertyName("nitritos")]        public string Nitritos { get; set; }
    [JsonPropertyName("fecha")]           public DateTime Fecha { get; set; }
    [JsonPropertyName("valor")]           public decimal Valor { get; set; }
    [JsonPropertyName("valorSecundario")] public decimal ValorSecundario { get; set; }
    [JsonPropertyName("unidad")]          public string Unidad { get; set; }
    [JsonPropertyName("color")]           public string Color { get; set; }
}
