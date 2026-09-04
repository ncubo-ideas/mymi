// LA PRESENCIA WEB DE UNA INSTITUCION, en chiquito. Son las tres cosas que publica, y las
// tres viven en el MISMO origen porque asi es como funciona de verdad:
//
//   /              su pagina, con el boton "Agregar a Gnothi"
//   /app/          su PWA, la app suelta que la persona usa hoy
//   /app/eventos/  su narrativa, relevada al publicador
//   /<modulo>.zip  el bundle que Gnothi baja al dar de alta el modulo
//
// ES LA MISMA PLANTILLA PARA LAS CUATRO instituciones del POC -- laboratorio, supermercado,
// estacion, concesionario -- y todo lo que cambia entra por configuracion: el bundle, la app
// suelta, el publicador al que releva, y como se llama. Por eso no vive dentro de ningun
// modulo: no es de ninguno.
//
// EXISTE PARA QUE LA PRUEBA SEA REPRODUCIBLE. Hasta el 2026-09-04 esto vivia en un scratchpad
// y se perdia con la sesion, asi que nadie mas podia correr el alta desde un enlace -- que es
// justamente el paso que separa "empujamos archivos con adb" de "una persona toca un boton".
//
// POR QUE LOS EVENTOS PASAN POR ACA y no van directo al publicador: la app suelta los pide en
// una URL RELATIVA a su propio origen (`appsettings.json` dice `"BaseUrl": "eventos/"`). Eso
// no es un atajo del POC, es el diseno: mismo origen significa sin CORS, sin contenido mixto,
// y sin que la app tenga que saber donde vive su publicador. La institucion releva.
//
// POR QUE EL BOTON APUNTA AL DOMINIO DE GNOTHI y no al propio: Chromium mantiene DENTRO del
// navegador los enlaces que van al mismo host de la pagina actual, asi que un enlace a la
// propia institucion nunca abriria la app. Tiene que ser cross-origin.

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Todo se pasa por configuracion porque el puerto del tunel (`adb reverse`) lo elige quien
// corre la prueba, no este archivo.
string bundle = app.Configuration["Bundle"]
    ?? Path.Combine(AppContext.BaseDirectory, "examenes.zip");

// La carpeta `web/` del bundle ya desempaquetado: se sirve la MISMA app que va adentro del
// zip, no otra copia. Si lo suelto y lo hospedado fueran binarios distintos, la comparacion
// entre los dos modos no probaria nada.
string appSuelta = app.Configuration["AppSuelta"]
    ?? Path.Combine(Path.GetDirectoryName(bundle) ?? ".", "examenes", "web");

string publicador = app.Configuration["Publicador"] ?? "http://127.0.0.1:8095/";
string suPropiaUrl = app.Configuration["UrlPublica"] ?? "http://127.0.0.1:8096";

string titulo = app.Configuration["Titulo"] ?? "Laboratorio Clinico Delfos";
string subtitulo = app.Configuration["Subtitulo"] ?? "Mis examenes";

var alPublicador = new HttpClient { BaseAddress = new Uri(publicador) };

// ---------------------------------------------------------------- la app suelta

// ANTES DEL RUTEO, y no es cosmetico: StaticFileMiddleware SE SALTA la peticion si el ruteo
// ya eligio un endpoint (mira `context.GetEndpoint() != null` y se va). Con el fallback de
// /app/** registrado, el ruteo elige SIEMPRE, asi que puesto despues no sirve NADA: cada
// archivo del runtime de wasm contestaba 200 con el index.html adentro, y la app moria con
// "Unexpected token '<'". Medido el 2026-09-04, y el sintoma no menciona archivos estaticos
// por ningun lado.
if (Directory.Exists(appSuelta))
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(appSuelta),
        RequestPath = "/app",

        // El runtime de wasm viaja en .dat, .blat y .wasm, que no estan en la tabla de tipos
        // conocidos. Sin esto contestan 404 y la app no arranca.
        ServeUnknownFileTypes = true,
        DefaultContentType = "application/octet-stream",
    });
}

app.UseRouting();

// ---------------------------------------------------------------- el bundle

// El zip se sirve con SU nombre, el del archivo: la pagina es la misma plantilla para
// cualquier institucion y el nombre del bundle es lo unico que cambia.
string nombreDelZip = Path.GetFileName(bundle);

app.MapGet($"/{nombreDelZip}", () => File.Exists(bundle)
    ? Results.File(bundle, "application/zip")
    : Results.NotFound($"no hay bundle en {bundle}. Corre armar-bundle.ps1 primero."));

// ---------------------------------------------------------------- la narrativa, relevada

// ANTES que los archivos estaticos, o el catch-all de /app/ se comeria esta ruta y la app
// suelta recibiria un 404 en vez de sus eventos.
app.Map("/app/eventos/{**ruta}", async (HttpContext ctx, string ruta) =>
{
    var pedido = new HttpRequestMessage(
        new HttpMethod(ctx.Request.Method),
        ruta + ctx.Request.QueryString);

    // El cuerpo PRIMERO, porque el Content-Type es una cabecera DE CONTENIDO y no de pedido:
    // puesta en `pedido.Headers` se descarta EN SILENCIO -- TryAddWithoutValidation devuelve
    // false y nadie lo mira -- y el publicador recibe un POST sin tipo y contesta
    // 415 Unsupported Media Type. Medido el 2026-09-04 pidiendo la credencial.
    if (ctx.Request.ContentLength > 0)
    {
        pedido.Content = new StreamContent(ctx.Request.Body);
        if (!string.IsNullOrEmpty(ctx.Request.ContentType))
            pedido.Content.Headers.TryAddWithoutValidation("Content-Type", ctx.Request.ContentType);
    }

    // El Authorization viaja tal cual: la credencial es de la app contra el publicador, y la
    // institucion no la mira ni la guarda. Releva el sobre, no lo abre.
    foreach (var cabecera in ctx.Request.Headers)
    {
        if (cabecera.Key.StartsWith("Content-", StringComparison.OrdinalIgnoreCase)) continue;
        if (string.Equals(cabecera.Key, "Host", StringComparison.OrdinalIgnoreCase)) continue;

        pedido.Headers.TryAddWithoutValidation(cabecera.Key, (IEnumerable<string>)cabecera.Value);
    }

    using HttpResponseMessage respuesta = await alPublicador.SendAsync(pedido, ctx.RequestAborted);

    ctx.Response.StatusCode = (int)respuesta.StatusCode;
    ctx.Response.ContentType = respuesta.Content.Headers.ContentType?.ToString() ?? "application/json";
    await respuesta.Content.CopyToAsync(ctx.Response.Body, ctx.RequestAborted);
});

// Una PWA es una sola pagina: cualquier ruta de adentro que NO sea un archivo devuelve su
// index.html y el ruteo lo hace ella. Sin esto, recargar en /app/mis-examenes da 404. Llega
// aca solo lo que los archivos estaticos de arriba no sirvieron.
if (Directory.Exists(appSuelta))
{
    app.MapFallback("/app/{**ruta}", async ctx =>
    {
        ctx.Response.ContentType = "text/html";
        await ctx.Response.SendFileAsync(Path.Combine(appSuelta, "index.html"));
    });
}

// ---------------------------------------------------------------- la pagina

app.MapGet("/", () =>
{
    string alta = "https://go.gnothi.app/alta?bundle="
        + Uri.EscapeDataString($"{suPropiaUrl}/{nombreDelZip}");

    return Results.Content($$"""
        <!doctype html><meta charset=utf-8>
        <meta name=viewport content="width=device-width,initial-scale=1">
        <title>{{titulo}}</title>
        <style>
          body { font: 16px/1.5 system-ui, sans-serif; margin: 0; padding: 32px 20px;
                 color: #16202c; background: #f6f7fa; }
          h1 { font-size: 24px; margin: 0 0 4px; }
          p  { color: #4a5568; max-width: 34em; }
          a.cta { display: inline-block; margin-top: 20px; padding: 14px 22px; border-radius: 10px;
                  background: #3f34a8; color: #fff; text-decoration: none; font-weight: 600; }
          a.otra { display: inline-block; margin-top: 14px; color: #3f34a8; }
          code { background: #e9ecf2; padding: 2px 6px; border-radius: 4px; font-size: 13px; }
        </style>
        <h1>{{titulo}}</h1>
        <p>{{subtitulo}}</p>
        <p>Llevate tu registro a Gnothi. Se instala en tu telefono y a partir de ahi
           tus datos viven ahi, no en nuestro servidor.</p>
        <a class="cta" href="{{alta}}">Agregar a Gnothi</a>
        <br><a class="otra" href="/app/">o abrir la app en el navegador</a>
        <p style="margin-top:32px;font-size:13px">El bundle esta en <code>{{suPropiaUrl}}/{{nombreDelZip}}</code></p>
        """, "text/html");
});

app.MapGet("/salud", () => Results.Ok(new
{
    estado = "arriba",
    bundle,
    hayBundle = File.Exists(bundle),
    appSuelta,
    hayAppSuelta = Directory.Exists(appSuelta),
    publicador,
}));

app.Run();
