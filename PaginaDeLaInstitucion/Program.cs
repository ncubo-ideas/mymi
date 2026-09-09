// LA PRESENCIA WEB DE UNA INSTITUCION, en chiquito. Son las tres cosas que publica, y las
// tres viven en el MISMO origen porque asi es como funciona de verdad:
//
//   /              su pagina, con el boton "Agregar a Gnothi"
//   /app/          su PWA, la app suelta que la persona usa hoy
//   /app/eventos/  su narrativa, relevada al publicador
//   /<modulo>.gmod el bundle que Gnothi baja al dar de alta el modulo (un zip, con su mime propio)
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
    ?? Path.Combine(AppContext.BaseDirectory, "examenes.gmod");

// THE PACKAGE'S OWN TYPE. Served as this, and not as application/zip, so a phone that receives
// the file -a download, a share- knows what it is without opening it: Gnothi's intent filters
// match on the mime, because for content:// files the path is opaque and the extension is not
// there to match. A file that went through chat as plain application/zip is caught too, and the
// inspection tells a module from any other zip.
const string ModuleMime = "application/vnd.gnothi.module";

// La carpeta `web/` del bundle ya desempaquetado: se sirve la MISMA app que va adentro del
// zip, no otra copia. Si lo suelto y lo hospedado fueran binarios distintos, la comparacion
// entre los dos modos no probaria nada.
string appSuelta = app.Configuration["AppSuelta"]
    ?? Path.Combine(Path.GetDirectoryName(bundle) ?? ".", "examenes", "web");

string publicador = app.Configuration["Publicador"] ?? "http://127.0.0.1:8095/";
string suPropiaUrl = app.Configuration["UrlPublica"] ?? "http://127.0.0.1:8096";

// THE INSTITUTION'S SIGNING KEY, the file armar-bundle.ps1 / Gnothi.Host --sign keep. Only its
// public half is published here; the private half never leaves that file. Optional: without it
// the origin publishes no keys, and Gnothi's sheet says the package's key is unknown to it.
string? llave = app.Configuration["Llave"];

// THE KEYS THE INSTITUTION RETIRED, the list Gnothi.Host --revoke keeps (public halves and dates).
// Published apart from the active keys: "retired" is a stronger thing to say than "absent".
string? llavesRetiradas = app.Configuration["LlavesRetiradas"];

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

// THE BUNDLE, with its length announced -Gnothi draws a progress bar from it- and, for rehearsing
// a slow network on a fast machine, an optional throttle: --LentoKBps 600 serves it at ~600 KB/s,
// which is what lets a person actually see the download happen in the emulator. Development only.
int lentoKBps = int.TryParse(app.Configuration["LentoKBps"], out int kbps) ? kbps : 0;
app.MapGet($"/{nombreDelZip}", async (HttpContext ctx) =>
{
    if (!File.Exists(bundle))
    {
        ctx.Response.StatusCode = StatusCodes.Status404NotFound;
        await ctx.Response.WriteAsync($"no hay bundle en {bundle}. Corre armar-bundle.ps1 primero.");
        return;
    }

    ctx.Response.ContentType = ModuleMime;
    ctx.Response.ContentLength = new FileInfo(bundle).Length;
    await using FileStream file = File.OpenRead(bundle);
    if (lentoKBps <= 0)
    {
        await file.CopyToAsync(ctx.Response.Body, ctx.RequestAborted);
        return;
    }

    var chunk = new byte[64 * 1024];
    int read;
    while ((read = await file.ReadAsync(chunk, ctx.RequestAborted)) > 0)
    {
        await ctx.Response.Body.WriteAsync(chunk.AsMemory(0, read), ctx.RequestAborted);
        await ctx.Response.Body.FlushAsync(ctx.RequestAborted);
        await Task.Delay(TimeSpan.FromMilliseconds(read / 1024.0 * 1000 / lentoKBps), ctx.RequestAborted);
    }
});

// WHAT THIS ORIGIN PUBLISHES. Gnothi asks the origin that served a bundle whether that bundle is
// its own -here- and compares with the origin the manifest declares: that is the backing of
// origin. TLS is what makes the answer worth anything; over http, as in development, it only
// proves that the three pieces are consistent, and Gnothi's sheet says so.
app.MapGet("/.well-known/gnothi/modules.json", () =>
{
    string? module = ModuleNameOf(bundle);
    object[] modules = module is null ? [] : [new { module, bundle = $"{suPropiaUrl}/{nombreDelZip}", seal = SealOf(bundle) }];
    object[] keys = PublishedKey(llave) is { } key ? [key] : [];
    object[] revoked = RevokedKeys(llavesRetiradas);
    return Results.Json(new { gnothi = 1, institution = titulo, keys, revoked, modules });
});

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
    // ONE LINK FOR THE BUTTON AND FOR THE CODE. It carries the bundle's address and, when the
    // bundle is there to be read, the seal of its manifest (&h=): Gnothi refuses a package whose
    // manifest no longer hashes to it, so whoever made this link or printed this code fixed
    // WHICH package it points at, and a swap on the server afterwards is caught. Integrity, not
    // authenticity -- the one who prints the code chooses both the address and the seal -- and
    // Gnothi's sheet says exactly that.
    string? seal = SealOf(bundle);
    string alta = "https://go.gnothi.app/alta?bundle="
        + Uri.EscapeDataString($"{suPropiaUrl}/{nombreDelZip}")
        + (seal is null ? "" : "&h=" + seal);

    // THE CODE, drawn here. The same link as the button, for the person who is reading this
    // page on a laptop and has Gnothi on her phone: no link crosses that gap, a code does.
    string qr;
    using (var generator = new QRCoder.QRCodeGenerator())
    using (QRCoder.QRCodeData data = generator.CreateQrCode(alta, QRCoder.QRCodeGenerator.ECCLevel.M))
        qr = new QRCoder.SvgQRCode(data).GetGraphic(6, "#16202c", "#ffffff", drawQuietZones: true);

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
          .qr { display: flex; align-items: center; gap: 20px; flex-wrap: wrap; margin-top: 28px; }
          .qr svg { width: 176px; height: 176px; display: block; border-radius: 8px; background: #fff;
                    box-shadow: 0 1px 4px rgba(0,0,0,.12); }
          .qr p { margin: 0; font-size: 14px; max-width: 22em; }
        </style>
        <h1>{{titulo}}</h1>
        <p>{{subtitulo}}</p>
        <p>Llevate tu registro a Gnothi. Se instala en tu telefono y a partir de ahi
           tus datos viven ahi, no en nuestro servidor.</p>
        <a class="cta" href="{{alta}}">Agregar a Gnothi</a>
        <br><a class="otra" href="/app/">o abrir la app en el navegador</a>
        <div class="qr">
          <div>{{qr}}</div>
          <p>Si estas leyendo esto en una computadora, escanea el codigo desde Gnothi en tu
             telefono: <b>Agregar modulo &rarr; Escanear el codigo de la institucion</b>.
             Es el mismo enlace que el boton.</p>
        </div>
        <p style="margin-top:32px;font-size:13px">El bundle esta en <code>{{suPropiaUrl}}/{{nombreDelZip}}</code>
           y este origen publica sus modulos en <code>{{suPropiaUrl}}/.well-known/gnothi/modules.json</code></p>
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
    llave,
    hayLlave = llave is not null && File.Exists(llave),
}));

app.Run();

// THE SEAL OF A BUNDLE: the first 16 bytes of the SHA-256 of manifest.json exactly as it sits
// in the zip, base64url without padding. Gnothi computes the same thing over the same bytes
// (ModuleEnrollment.SealOf) and the two have to agree byte for byte, so nothing is normalised
// on either side. Null when there is no bundle to read: the link then carries no seal, which is
// what it did before, not an error.
static string? SealOf(string bundlePath)
{
    if (!File.Exists(bundlePath)) return null;

    using var zip = System.IO.Compression.ZipFile.OpenRead(bundlePath);
    System.IO.Compression.ZipArchiveEntry? manifest = zip.Entries.FirstOrDefault(e =>
        e.FullName.Replace('\\', '/').Equals("manifest.json", StringComparison.OrdinalIgnoreCase));
    if (manifest is null) return null;

    using Stream bytes = manifest.Open();
    byte[] hash = System.Security.Cryptography.SHA256.HashData(bytes);
    return Convert.ToBase64String(hash, 0, 16).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}

// The public half of the institution's key, as the well-known publishes it: id, algorithm, key.
// Read from the key file --sign writes, and NOTHING else is read from it.
static object? PublishedKey(string? keyPath)
{
    if (keyPath is null || !File.Exists(keyPath)) return null;

    using var key = System.Text.Json.JsonDocument.Parse(File.ReadAllText(keyPath));
    System.Text.Json.JsonElement root = key.RootElement;
    return new
    {
        id = root.TryGetProperty("id", out System.Text.Json.JsonElement i) ? i.GetString() : "",
        alg = root.TryGetProperty("alg", out System.Text.Json.JsonElement a) ? a.GetString() : "",
        @public = root.TryGetProperty("public", out System.Text.Json.JsonElement p) ? p.GetString() : "",
    };
}

// The retired keys, as --revoke wrote them: id, public half, since, reason. Empty when the file
// does not exist -nothing was ever retired- or cannot be read.
static object[] RevokedKeys(string? revokedPath)
{
    if (revokedPath is null || !File.Exists(revokedPath)) return [];

    try
    {
        using var list = System.Text.Json.JsonDocument.Parse(File.ReadAllText(revokedPath));
        if (list.RootElement.ValueKind != System.Text.Json.JsonValueKind.Array) return [];
        return [.. list.RootElement.EnumerateArray().Select(r => (object)new
        {
            id = r.TryGetProperty("id", out System.Text.Json.JsonElement i) ? i.GetString() : "",
            @public = r.TryGetProperty("public", out System.Text.Json.JsonElement p) ? p.GetString() : "",
            since = r.TryGetProperty("since", out System.Text.Json.JsonElement s) ? s.GetString() : "",
            reason = r.TryGetProperty("reason", out System.Text.Json.JsonElement z) ? z.GetString() : null,
        })];
    }
    catch (System.Text.Json.JsonException)
    {
        return [];
    }
}

// The module's name as its manifest declares it, for the list this origin publishes. Null when
// there is no bundle to read.
static string? ModuleNameOf(string bundlePath)
{
    if (!File.Exists(bundlePath)) return null;

    using var zip = System.IO.Compression.ZipFile.OpenRead(bundlePath);
    System.IO.Compression.ZipArchiveEntry? manifest = zip.Entries.FirstOrDefault(e =>
        e.FullName.Replace('\\', '/').Equals("manifest.json", StringComparison.OrdinalIgnoreCase));
    if (manifest is null) return null;

    using Stream bytes = manifest.Open();
    using var declared = System.Text.Json.JsonDocument.Parse(bytes);
    return declared.RootElement.TryGetProperty("module", out System.Text.Json.JsonElement m) ? m.GetString() : null;
}
