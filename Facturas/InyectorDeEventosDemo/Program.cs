using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;

// Inyector de eventos de demo para URL2.
//
// Herramienta de operacion: la usa quien monta la demo, no la persona usuaria. Le habla
// a URL2 por HTTP igual que cualquier operador, sin referenciar ni el dominio ni la app.
//
// Por que existe: los datos del demo se ORQUESTAN como test cases. Un endpoint que
// agrega una factura aleatoria no sirve para contar una historia, porque el total no se
// puede predecir. Con esto se inyecta la factura que la historia necesita.

string baseUrl = Arg(args, "--url", "http://localhost:8081/");
string recurso = Arg(args, "--recurso", "loan");
string id = Arg(args, "--id", "78341");

using var http = new HttpClient { BaseAddress = new Uri(baseUrl) };

string comando = args.Length > 0 && !args[0].StartsWith("--") ? args[0] : "ayuda";

try
{
    switch (comando)
    {
        case "listar":     await Listar(); break;
        case "aleatoria":  await Aleatoria(); break;
        case "inyectar":   await Inyectar(); break;
        case "historia":   await Historia(); break;
        default:           Ayuda(); break;
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
    var credencial = await Credencial();
    using var pedido = new HttpRequestMessage(HttpMethod.Get, $"{recurso}/{id}/events?after=0");
    pedido.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", credencial);

    using var respuesta = await http.SendAsync(pedido);
    respuesta.EnsureSuccessStatusCode();

    using JsonDocument doc = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
    JsonElement eventos = doc.RootElement.GetProperty("events");

    Console.WriteLine($"{eventos.GetArrayLength()} eventos en {recurso}/{id}:");
    foreach (JsonElement e in eventos.EnumerateArray())
        Console.WriteLine($"  #{e.GetProperty("id")}  {e.GetProperty("numero")}  {Total(e).ToString("N2", CultureInfo.InvariantCulture)}");
}

// El atajo de siempre: una factura cualquiera, para probar que el refresh trae lo nuevo.
async Task Aleatoria()
{
    using var respuesta = await http.PostAsync($"{recurso}/{id}/events/factura-aleatoria", null);
    respuesta.EnsureSuccessStatusCode();
    Console.WriteLine($"agregada: {await respuesta.Content.ReadAsStringAsync()}");
}

// Una factura CONCRETA, con sus lineas, para que el total sea predecible.
//   inyectar --numero TQ-9001 --fecha 2026-08-20 --linea "ACE-1L,Aceite de girasol 1L,3,2890.00"
async Task Inyectar()
{
    string numero = Arg(args, "--numero", "");
    if (numero.Length == 0) { Console.Error.WriteLine("Falta --numero"); return; }

    string[] lineas = Lineas(args);
    if (lineas.Length == 0) { Console.Error.WriteLine("Hace falta al menos una --linea sku,descripcion,cantidad,precio"); return; }

    var partes = lineas.Select(l => l.Split(',')).ToArray();
    if (partes.Any(p => p.Length != 4))
    {
        Console.Error.WriteLine("Cada --linea va como: sku,descripcion,cantidad,precio");
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
    };

    decimal total = 0m;
    for (int i = 0; i < cuerpo.skus.Length; i++) total += cuerpo.cantidades[i] * cuerpo.preciosUnitarios[i];

    using var respuesta = await http.PostAsJsonAsync($"{recurso}/{id}/events/factura", cuerpo);
    if (!respuesta.IsSuccessStatusCode)
    {
        Console.Error.WriteLine($"URL2 respondio {(int)respuesta.StatusCode}. "
            + "Si es 404, a URL2 le falta el endpoint POST .../events/factura.");
        return;
    }

    Console.WriteLine($"inyectada {numero}, {cuerpo.skus.Length} lineas, total {total.ToString("N2", CultureInfo.InvariantCulture)}");
    Console.WriteLine($"  respuesta: {await respuesta.Content.ReadAsStringAsync()}");
}

// El guion completo de la demo, sembrado de una vez. Aca se edita LA HISTORIA.
async Task Historia()
{
    Console.WriteLine($"sembrando la historia en {recurso}/{id}...");
    // Cada linea es sku,descripcion,cantidad,precio. Los montos estan elegidos para que
    // el total sea el que la historia necesita contar, no para ser realistas.
    (string numero, string fecha, string[] lineas)[] guion =
    {
        ("TQ-9001", "2026-08-01", new[] { "ACE-1L,Aceite de girasol 1L,3,2890.00", "MAN-500,Manteca 500g,2,1750.00" }),
        ("TQ-9002", "2026-08-08", new[] { "GAR-400,Garbanzos en lata 400g,4,1190.00" }),
        ("TQ-9003", "2026-08-15", new[] { "FRI-1KG,Frijol negro 1kg,2,2450.00", "HUE-12,Huevos x12,1,2350.00" }),
    };

    foreach (var (numero, fecha, lineas) in guion)
    {
        var partes = lineas.Select(l => l.Split(',')).ToArray();
        var cuerpo = new
        {
            numero,
            fecha,
            skus = partes.Select(p => p[0]).ToArray(),
            descripciones = partes.Select(p => p[1]).ToArray(),
            cantidades = partes.Select(p => int.Parse(p[2], CultureInfo.InvariantCulture)).ToArray(),
            preciosUnitarios = partes.Select(p => decimal.Parse(p[3], CultureInfo.InvariantCulture)).ToArray(),
        };
        using var r = await http.PostAsJsonAsync($"{recurso}/{id}/events/factura", cuerpo);
        Console.WriteLine($"  {numero}: HTTP {(int)r.StatusCode}");
    }
}

async Task<string> Credencial()
{
    using var respuesta = await http.PostAsJsonAsync("device/token",
        new { deviceKey = "inyector-de-demo", recurso, id });
    respuesta.EnsureSuccessStatusCode();

    using JsonDocument doc = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
    return doc.RootElement.GetProperty("accessToken").GetString();
}

static decimal Total(JsonElement evento)
{
    decimal total = 0m;
    JsonElement cantidades = evento.GetProperty("cantidades");
    JsonElement precios = evento.GetProperty("preciosUnitarios");
    for (int i = 0; i < cantidades.GetArrayLength(); i++)
        total += cantidades[i].GetInt32() * precios[i].GetDecimal();
    return total;
}

static string[] Lineas(string[] args)
{
    var salida = new List<string>();
    for (int i = 0; i < args.Length - 1; i++)
        if (args[i] == "--linea") salida.Add(args[i + 1]);
    return salida.ToArray();
}

static string Arg(string[] args, string nombre, string porDefecto)
{
    for (int i = 0; i < args.Length - 1; i++)
        if (args[i] == nombre) return args[i + 1];
    return porDefecto;
}

static void Ayuda()
{
    Console.WriteLine("Inyector de eventos de demo para URL2");
    Console.WriteLine();
    Console.WriteLine("  listar                        que eventos ve la app hoy");
    Console.WriteLine("  aleatoria                     agrega una factura cualquiera");
    Console.WriteLine("  inyectar --numero TQ-9001 --linea \"SKU,descripcion,cantidad,precio\" [--linea ...]");
    Console.WriteLine("                                agrega UNA factura concreta, con total predecible");
    Console.WriteLine("  historia                      siembra el guion completo de la demo");
    Console.WriteLine();
    Console.WriteLine("  --url      default http://localhost:8081/");
    Console.WriteLine("  --recurso  default loan");
    Console.WriteLine("  --id       default 78341");
}
