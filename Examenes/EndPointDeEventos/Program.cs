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
    // Las dos muestras que no tienen numero necesitan color; las demas, unidad. Estaban
    // preguntadas solo por "Orina", asi que a un examen de heces se le exigia una unidad que
    // no tiene y no se le exigia el color que si necesita.
    bool sinNumero = pedido.Examen == "Orina" || pedido.Examen == "Heces";
    if (sinNumero && Vacio(pedido.Color))
        return Results.BadRequest(new { error = "un examen de orina o de heces necesita color" });
    if (!sinNumero && Vacio(pedido.Unidad))
        return Results.BadRequest(new { error = "un examen de sangre necesita unidad" });

    EventoDeExamen nuevo = almacen.Agregar(recurso, id, pedido);
    return Results.Ok(new { agregado = nuevo.Id, examen = nuevo.Examen, resultado = nuevo.Resultado() });
});

// PUBLICA UN DATO DEL PACIENTE. Accion DEL LABORATORIO, igual que registrar un examen:
// sin token, porque no es la app la que lo hace.
//
// POR QUE EXISTE ESTE ENDPOINT. Un laboratorio real SABE el sexo del paciente —los rangos
// de referencia van impresos en el reporte— asi que modelar solo la puerta por la que lo
// declara la persona seria fingir que no lo sabe. Con este endpoint las dos fuentes
// existen, y el caso interesante aparece solo: si la institucion publica una cosa y la
// persona declara otra, la app guarda las dos con su procedencia y para interpretar gana
// lo que dijo ella.
//
// NO ESTA EN LA HISTORIA SEMBRADA a proposito: el guion de la demo arranca con el sexo
// DESCONOCIDO para que el bloque que insiste se vea, y eso ocurre porque el dato de verdad
// falta, no porque se haya escondido.
app.MapPost("/{recurso}/{id}/events/paciente", (string recurso, string id, DatoDePacienteAInyectar pedido) =>
{
    if (pedido is null || Vacio(pedido.Sexo))
        return Results.BadRequest(new { error = "sexo requerido" });
    if (!AlmacenDeEventos.EsSexoConocido(pedido.Sexo))
        return Results.BadRequest(new
        {
            error = "sexo desconocido",
            conocidos = AlmacenDeEventos.SexosConocidos,
        });

    EventoDeExamen nuevo = almacen.AgregarDatoDePaciente(recurso, id, pedido);
    return Results.Ok(new { agregado = nuevo.Id, tipo = nuevo.Tipo, resultado = nuevo.Resultado() });
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

    // EL TIPO YA NO ES UNA CONSTANTE, y ese es el cambio: este laboratorio publica DOS
    // narrativas. La de siempre —"te registre un examen"— y la de los datos del paciente,
    // que un laboratorio real tiene porque los rangos de referencia van impresos en el
    // reporte y para imprimirlos hay que saber el sexo.
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

    // Las otras tres casillas de la tira reactiva. Vacio es "el laboratorio no corrio esa
    // prueba", que es un estado legitimo del urianalisis y distinto de un negativo: un
    // reporte viejo no trae ninguna de las tres.
    [JsonPropertyName("proteina")]        public string Proteina { get; init; } = "";
    [JsonPropertyName("cetonas")]         public string Cetonas { get; init; } = "";
    [JsonPropertyName("glucosa")]         public string Glucosa { get; init; } = "";

    // SOLO EN LOS EVENTOS DE PACIENTE. Viaja en el mismo sobre porque este mock guarda una
    // sola lista ordenada por id —que es lo que hace que el checkpoint de la app sea un
    // solo numero— y no porque un examen tenga sexo.
    [JsonPropertyName("sexo")]            public string Sexo { get; init; } = "";

    internal string Resultado()
    {
        if (Tipo == "DatosDePacienteActualizados") return $"sexo {Sexo}";
        if (Examen != "Orina" && Examen != "Heces") return $"{Valor} {Unidad}";

        string texto = Color;
        if (!string.IsNullOrWhiteSpace(Nitritos)) texto += $", nitritos {Nitritos}";
        if (!string.IsNullOrWhiteSpace(Sangre)) texto += $", sangre {Sangre}";
        if (!string.IsNullOrWhiteSpace(Proteina)) texto += $", proteina {Proteina}";
        if (!string.IsNullOrWhiteSpace(Cetonas)) texto += $", cetonas {Cetonas}";
        if (!string.IsNullOrWhiteSpace(Glucosa)) texto += $", glucosa {Glucosa}";
        return texto;
    }
}

sealed class AlmacenDeEventos
{
    private readonly ConcurrentDictionary<string, List<EventoDeExamen>> porRecurso = new();
    private readonly Lock candado = new();

    internal static readonly string[] ExamenesConocidos =
    {
        "Colesterol", "ColesterolDetallado", "AntigenoProstatico", "Hemoglobina",
        "Orina", "Heces",

        // La quimica general. Los nombres son los que la app resuelve en su frontera:
        // ademas de estos acepta las abreviaturas del reporte -TG, GLU, BUN, NA, K, CL-
        // porque el analito lleva sus sinonimos.
        "Trigliceridos", "Glucosa en ayunas", "Nitrogeno ureico",
        "Sodio", "Potasio", "Cloro", "Proteinas totales",
    };

    internal static bool EsExamenConocido(string examen) =>
        Array.IndexOf(ExamenesConocidos, examen) >= 0;

    // Lo que este laboratorio sabe publicar. Es su vocabulario, no el del dominio de la
    // app: la app lo resuelve en su frontera y acepta ademas las abreviaturas de un HL7.
    internal static readonly string[] SexosConocidos = { "hombre", "mujer" };

    internal static bool EsSexoConocido(string sexo) =>
        Array.IndexOf(SexosConocidos, (sexo ?? "").Trim().ToLowerInvariant()) >= 0;

    private List<EventoDeExamen> Lista(string recurso, string id) =>
        porRecurso.GetOrAdd($"{recurso}/{id}", _ => Sembrar());

    // LA HISTORIA sembrada, la misma que usa la consola para medir. Las fechas son
    // literales a proposito: una demo con fechas relativas al dia de hoy deja de ser
    // reproducible, y las tendencias dependen de los dias transcurridos.
    //
    // ES EL EXPEDIENTE DE SONIA, la persona de la demo: 34 anos, y sus resultados del 28
    // de abril de 2026. La landing de URL1 la saluda por su nombre y muestra dos
    // veredictos -el conteo de lo que esta en rango, y el colesterol que subio un
    // poquito-, asi que el laboratorio tiene que publicar los numeros con los que la app
    // puede decir esas dos cosas.
    //
    // Los tres colesteroles son la SERIE: 182 y 188 son la linea base y el 212 es el
    // hallazgo. Sin los dos primeros el 212 es un numero cualquiera bien adentro del
    // rango; con ellos es un ascenso real que todavia no se sale, y eso es lo que la app
    // puede decir y una hoja de resultados no.
    //
    // NO HAY ANTIGENO PROSTATICO en el guion, y esa ausencia es un dato: a Sonia no se lo
    // hicieron. El endpoint lo sigue aceptando -esta en ExamenesConocidos- para poder
    // inyectarlo en vivo y mostrar que la app lo esconde cuando la persona declara que es
    // mujer.
    private static List<EventoDeExamen> Sembrar() => new()
    {
        // EL PERFIL LIPIDICO DE 2021, y no es relleno: es lo UNICO que la app va a mostrar
        // VENCIDO. El LDL salio en rango, asi que su control es cada 60 meses, y abril de
        // 2021 los pasa. Todo el resto del guion cae entre 2024 y 2026, asi que sin esto
        // nada aparece atrasado nunca y la consulta que mira el hueco no muestra nada.
        //
        // Ademas ejercita ColesterolDetallado, que estaba modelado -con su HDL que se lee
        // al reves que el LDL, y con su piso que depende del sexo- y no aparecia en la
        // historia.
        new EventoDeExamen
        {
            Id = 200, Examen = "ColesterolDetallado", Fecha = new DateTime(2021, 4, 22, 8, 15, 0),
            Valor = 118m, ValorSecundario = 58m, Unidad = "mg/dL",
        },

        // Hace dos anos: la linea base del colesterol y la primera hemoglobina.
        Sangre(201, "Colesterol", new DateTime(2024, 4, 24, 8, 20, 0), 182.0m, "mg/dL"),
        Sangre(202, "Hemoglobina", new DateTime(2024, 4, 24, 8, 21, 0), 13.1m, "g/dL"),

        // Hace un ano: subio 6 mg/dL, que para este analito NO llega a ser un cambio.
        Sangre(203, "Colesterol", new DateTime(2025, 4, 26, 8, 30, 0), 188.0m, "mg/dL"),

        // El 28 de abril, el dia que la landing muestra.
        Sangre(204, "Colesterol", new DateTime(2026, 4, 28, 8, 40, 0), 212.0m, "mg/dL"),
        Sangre(205, "Hemoglobina", new DateTime(2026, 4, 28, 8, 41, 0), 13.4m, "g/dL"),
        new EventoDeExamen
        {
            Id = 206, Examen = "Orina", Fecha = new DateTime(2026, 4, 28, 8, 42, 0),
            Color = "ambar", Nitritos = "negativo", Sangre = "negativo",

            // LA TIRA ENTERA, que es lo que el reporte del CSS trae. Las tres casillas
            // nuevas no agregan eventos: agregan tres analitos al mismo urianalisis, y eso
            // es lo que las lleva a las 18 del conteo.
            Proteina = "negativo", Cetonas = "negativo", Glucosa = "negativo",
        },

        // EL EXPEDIENTE DE SONIA ESTA LIMPIO A PROPOSITO. La demo se abre con "todo esta
        // bien" y un solo ambar, y un positivo de fabrica taparia justo ese veredicto.
        //
        // Para ver el otro lado alcanza cambiar UN "negativo" por "positivo" aca: en las
        // heces la app dice "conviene revisarlo" y no alarma -la causa mas frecuente es
        // local-, y el MISMO positivo en la orina no espera a la proxima cita. Misma forma
        // de magnitud, distinta conducta, y la diferencia vive en el analito.
        new EventoDeExamen
        {
            Id = 207, Examen = "Heces", Fecha = new DateTime(2026, 4, 28, 8, 43, 0),
            Color = "marrón", Sangre = "negativo",
        },

        // LA HISTORIA DE LOS BINARIOS, y va con ids POSTERIORES aunque las fechas sean
        // anteriores. No es un descuido: los ids son el orden del journal y no se
        // reescriben, y ademas es el caso real de una carga atrasada, que el dominio
        // ordena por fecha al armar la serie.
        //
        // POR QUE HACEN FALTA. Sin ellos, el negativo del 207 es un dato suelto y de
        // "sangre en heces" no se puede decir mas que "una sola prueba, todavia no hay
        // serie". Con las tres, la serie contesta que en las tres veces nunca aparecio —y
        // eso es informacion que un examen solo no puede dar.
        new EventoDeExamen
        {
            Id = 208, Examen = "Heces", Fecha = new DateTime(2024, 4, 24, 8, 22, 0),
            Color = "marrón", Sangre = "negativo",
        },
        new EventoDeExamen
        {
            Id = 209, Examen = "Heces", Fecha = new DateTime(2025, 4, 26, 8, 31, 0),
            Color = "marrón", Sangre = "negativo",
        },

        // Y la orina, con su segunda tira: dos pruebas y ninguna aparicion. El color cambia
        // -amarillo palido y despues ambar- y las dos categorias son esperables, que es lo
        // que la serie del color sabe leer y una prueba binaria no sabria nombrar.
        new EventoDeExamen
        {
            Id = 210, Examen = "Orina", Fecha = new DateTime(2025, 4, 26, 8, 32, 0),
            Color = "amarillo palido", Nitritos = "negativo", Sangre = "negativo",
            Proteina = "negativo", Cetonas = "negativo", Glucosa = "negativo",
        },

        // LA QUIMICA GENERAL DEL MISMO REPORTE del 28 de abril: los siete, todos en rango.
        //
        // SIETE EVENTOS Y NO UNO, que no es como un laboratorio la emite. Es una limitacion
        // conocida y anotada: un ExamenDeSangre mide UN analito, asi que un panel llega como
        // N eventos. Abrir el camino continuo por lectura es el follow-on, y es lo mismo que
        // haria falta para que un hemograma de 32 parametros no sean 32 eventos.
        //
        // UNA MEDICION CADA UNO, como en el reporte real: sin serie no hay tendencia, y sin
        // embargo el analito cuenta en el conteo de la portada porque su valor esta en su
        // rango. Los siete tienen rango independiente del sexo a proposito.
        Sangre(211, "Trigliceridos", new DateTime(2026, 4, 28, 8, 44, 0), 92m, "mg/dL"),
        Sangre(212, "Glucosa en ayunas", new DateTime(2026, 4, 28, 8, 45, 0), 88m, "mg/dL"),
        Sangre(213, "Nitrogeno ureico", new DateTime(2026, 4, 28, 8, 46, 0), 13m, "mg/dL"),
        Sangre(214, "Sodio", new DateTime(2026, 4, 28, 8, 47, 0), 140m, "mEq/L"),
        Sangre(215, "Potasio", new DateTime(2026, 4, 28, 8, 48, 0), 4.2m, "mEq/L"),
        Sangre(216, "Cloro", new DateTime(2026, 4, 28, 8, 49, 0), 103m, "mEq/L"),
        Sangre(217, "Proteinas totales", new DateTime(2026, 4, 28, 8, 50, 0), 7.2m, "g/dL"),
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

    // El dato del paciente que publica el laboratorio. Entra en la MISMA lista y con el
    // mismo contador: es un evento mas de la narrativa, y por eso el checkpoint de la app
    // sigue siendo un solo numero.
    internal EventoDeExamen AgregarDatoDePaciente(string recurso, string id,
        DatoDePacienteAInyectar pedido)
    {
        List<EventoDeExamen> lista = Lista(recurso, id);

        lock (candado)
        {
            long siguiente = (lista.Count == 0 ? 200 : lista.Max(e => e.Id)) + 1;
            var evento = new EventoDeExamen
            {
                Id = siguiente,
                Tipo = "DatosDePacienteActualizados",
                Fecha = pedido.Fecha == default ? DateTime.Now : pedido.Fecha,
                Sexo = pedido.Sexo.Trim().ToLowerInvariant(),
            };
            lista.Add(evento);
            return evento;
        }
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

                // La sangre oculta se copiaba en el guion sembrado y NO en la inyeccion en
                // vivo, asi que el unico camino por el que no se podia demostrar la serie
                // binaria era justamente el de la demo.
                Sangre = pedido.Sangre ?? "",
                Proteina = pedido.Proteina ?? "",
                Cetonas = pedido.Cetonas ?? "",
                Glucosa = pedido.Glucosa ?? "",
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
sealed class DatoDePacienteAInyectar
{
    [JsonPropertyName("sexo")]  public string Sexo { get; set; }
    [JsonPropertyName("fecha")] public DateTime Fecha { get; set; }
}

sealed class ExamenAInyectar
{
    [JsonPropertyName("examen")]          public string Examen { get; set; }
    [JsonPropertyName("nitritos")]        public string Nitritos { get; set; }
    [JsonPropertyName("sangre")]          public string Sangre { get; set; }
    [JsonPropertyName("proteina")]        public string Proteina { get; set; }
    [JsonPropertyName("cetonas")]         public string Cetonas { get; set; }
    [JsonPropertyName("glucosa")]         public string Glucosa { get; set; }
    [JsonPropertyName("fecha")]           public DateTime Fecha { get; set; }
    [JsonPropertyName("valor")]           public decimal Valor { get; set; }
    [JsonPropertyName("valorSecundario")] public decimal ValorSecundario { get; set; }
    [JsonPropertyName("unidad")]          public string Unidad { get; set; }
    [JsonPropertyName("color")]           public string Color { get; set; }
}
