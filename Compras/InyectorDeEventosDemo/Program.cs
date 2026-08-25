using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;

// Inyector de eventos de demo para URL2.
//
// Herramienta de operacion: la usa quien monta la demo, no la persona usuaria. Le habla
// a URL2 por HTTP igual que cualquier operador, sin referenciar ni el dominio ni la app.
//
// Por que existe: los datos del demo se ORQUESTAN como test cases. Un endpoint que
// agregara compras aleatorias no sirve para contar una historia, porque la CANASTA
// depende de las categorias y de las fechas, y la garantia de su fecha exacta. Con
// valores al azar no se puede demostrar que la canasta se volteo, que es media demo.
//
// Regla de la casa: se publica SOLO lo que la app va a mostrar. Si la historia son seis
// compras, son seis eventos, no sesenta.

string baseUrl = Arg(args, "--url", "http://localhost:8101/");
string recurso = Arg(args, "--recurso", "cliente");
string id = Arg(args, "--id", "1");

using var http = new HttpClient { BaseAddress = new Uri(baseUrl) };

string comando = args.Length > 0 && !args[0].StartsWith("--") ? args[0] : "ayuda";

try
{
    switch (comando)
    {
        case "listar":    await Listar(); break;
        case "inyectar":  await Inyectar(); break;
        case "senuelo":   await Senuelo(); break;
        default:          Ayuda(); break;
    }
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error: {ex.GetType().Name}: {ex.Message}");
    Console.Error.WriteLine($"  (¿esta URL2 arriba en {baseUrl}?)");
    return 2;
}

// Que ve hoy la app: sirve para saber desde donde sigue la numeracion.
async Task Listar()
{
    string credencial = await Credencial();
    using var pedido = new HttpRequestMessage(HttpMethod.Get, $"{recurso}/{id}/events?after=0");
    pedido.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", credencial);

    using var respuesta = await http.SendAsync(pedido);
    respuesta.EnsureSuccessStatusCode();

    using JsonDocument doc = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
    JsonElement eventos = doc.RootElement.GetProperty("events");

    Console.WriteLine($"{eventos.GetArrayLength()} eventos en {recurso}/{id}:");
    foreach (JsonElement e in eventos.EnumerateArray())
    {
        string fecha = e.GetProperty("fecha").GetDateTime().ToString("yyyy-MM-dd");
        decimal total = Total(e);
        int lineas = e.GetProperty("skus").GetArrayLength();
        Console.WriteLine($"  #{e.GetProperty("id")}  {fecha}  {e.GetProperty("numero"),-10} "
            + $"{lineas} lineas  {total.ToString("N0", CultureInfo.InvariantCulture)}");
    }

    static decimal Total(JsonElement e)
    {
        JsonElement cantidades = e.GetProperty("cantidades");
        JsonElement precios = e.GetProperty("preciosUnitarios");
        decimal total = 0m;
        for (int i = 0; i < cantidades.GetArrayLength(); i++)
            total += cantidades[i].GetInt32() * precios[i].GetDecimal();
        return total;
    }
}

// Una compra CONCRETA, con sus lineas. Cada --linea va como
// sku,descripcion,cantidad,precio,categoria,mesesDeGarantia — la categoria es lo que
// vuelve la narrativa analizable, y los meses (0 = sin garantia) lo que respalda la
// pregunta de la garantia.
//
//   inyectar --numero T-97001 --fecha 2026-08-25 \
//     --linea "HELADO-1L,Helado de vainilla 1L,2,3680,Azucarados,0" \
//     --linea "LICUA-600,Licuadora 600W,1,28900,Electrohogar,24"
async Task Inyectar()
{
    string numero = Arg(args, "--numero", "");
    if (numero.Length == 0) { Console.Error.WriteLine("Falta --numero"); return; }

    string[] lineas = Lineas(args);
    if (lineas.Length == 0)
    {
        Console.Error.WriteLine("Hace falta al menos una --linea sku,descripcion,cantidad,precio,categoria,mesesDeGarantia");
        return;
    }

    var partes = lineas.Select(l => l.Split(',')).ToArray();
    if (partes.Any(p => p.Length != 6))
    {
        Console.Error.WriteLine("Cada --linea va como: sku,descripcion,cantidad,precio,categoria,mesesDeGarantia");
        return;
    }

    var cuerpo = new
    {
        numero,
        fecha = Arg(args, "--fecha", DateTime.Today.ToString("yyyy-MM-dd")),
        skus = partes.Select(p => p[0].Trim()).ToArray(),
        descripciones = partes.Select(p => p[1].Trim()).ToArray(),
        cantidades = partes.Select(p => int.Parse(p[2], CultureInfo.InvariantCulture)).ToArray(),
        preciosUnitarios = partes.Select(p => decimal.Parse(p[3], CultureInfo.InvariantCulture)).ToArray(),
        categorias = partes.Select(p => p[4].Trim()).ToArray(),
        mesesDeGarantia = partes.Select(p => int.Parse(p[5], CultureInfo.InvariantCulture)).ToArray(),
    };

    using var respuesta = await http.PostAsJsonAsync($"{recurso}/{id}/events/compra", cuerpo);
    string contenido = await respuesta.Content.ReadAsStringAsync();
    if (!respuesta.IsSuccessStatusCode)
    {
        Console.Error.WriteLine($"URL2 rechazo la compra ({(int)respuesta.StatusCode}): {contenido}");
        return;
    }

    Console.WriteLine($"agregada: {contenido}");
}

// El invariante, demostrado en vivo: el MISMO token que lee los eventos NO alcanza para
// el perfil. 200 en /events y 403 en /perfil, con la credencial valida de la app.
async Task Senuelo()
{
    string credencial = await Credencial();

    using var eventos = new HttpRequestMessage(HttpMethod.Get, $"{recurso}/{id}/events?after=0");
    eventos.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", credencial);
    using var respuestaEventos = await http.SendAsync(eventos);

    using var perfil = new HttpRequestMessage(HttpMethod.Get, $"{recurso}/{id}/perfil");
    perfil.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", credencial);
    using var respuestaPerfil = await http.SendAsync(perfil);

    Console.WriteLine($"GET {recurso}/{id}/events?after=0    -> {(int)respuestaEventos.StatusCode}");
    Console.WriteLine($"GET {recurso}/{id}/perfil            -> {(int)respuestaPerfil.StatusCode}");
    Console.WriteLine($"     {await respuestaPerfil.Content.ReadAsStringAsync()}");
}

// El inyector consigue su propia credencial de lectura, como cualquier device.
async Task<string> Credencial()
{
    using var respuesta = await http.PostAsJsonAsync("device/token",
        new { deviceKey = "device-inyector-de-demo", recurso, id });
    respuesta.EnsureSuccessStatusCode();

    using JsonDocument doc = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
    return doc.RootElement.GetProperty("accessToken").GetString();
}

static string Arg(string[] args, string nombre, string porDefecto)
{
    for (int i = 0; i < args.Length - 1; i++)
        if (args[i] == nombre) return args[i + 1];
    return porDefecto;
}

static string[] Lineas(string[] args)
{
    var lineas = new List<string>();
    for (int i = 0; i < args.Length - 1; i++)
        if (args[i] == "--linea") lineas.Add(args[i + 1]);
    return lineas.ToArray();
}

static void Ayuda()
{
    Console.WriteLine("Inyector de eventos de demo para URL2 (Compras)");
    Console.WriteLine();
    Console.WriteLine("  listar                             que ve hoy la app (y desde donde sigue la numeracion)");
    Console.WriteLine("  inyectar --numero T-97001 [--fecha yyyy-MM-dd] --linea \"sku,descripcion,cantidad,precio,categoria,meses\" ...");
    Console.WriteLine("  senuelo                            demuestra el 403 estructural del token de la app");
    Console.WriteLine();
    Console.WriteLine("  opciones: --url http://localhost:8101/  --recurso cliente  --id 1");
    Console.WriteLine();
    Console.WriteLine("  categorias: Proteinas FrutasYVerduras LacteosYHuevos GranosYAbarrotes");
    Console.WriteLine("              AceitesYGrasas Azucarados Snacks Limpieza Electrohogar");
}
