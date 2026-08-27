using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;

// Inyector de eventos de demo para URL2.
//
// Herramienta de operacion: la usa quien monta la demo, no la persona usuaria. Le habla
// a URL2 por HTTP igual que cualquier operador, sin referenciar ni el dominio ni la app.
//
// Por que existe: los datos del demo se ORQUESTAN como test cases. Un endpoint que
// agrega un examen aleatorio no sirve para contar una historia, porque la TENDENCIA
// depende de los valores y de las fechas. Con valores al azar no se puede demostrar que
// el antigeno viene subiendo, que es toda la demo.
//
// Regla de la casa: se publica SOLO lo que la app va a mostrar. Si la historia son ocho
// cargas, son ocho eventos, no sesenta.

string baseUrl = Arg(args, "--url", "http://localhost:8111/");
string recurso = Arg(args, "--recurso", "expediente");
string id = Arg(args, "--id", "1");

using var http = new HttpClient { BaseAddress = new Uri(baseUrl) };

string comando = args.Length > 0 && !args[0].StartsWith("--") ? args[0] : "ayuda";

try
{
    switch (comando)
    {
        case "listar":    await Listar(); break;
        case "inyectar":  await Inyectar(); break;
        case "historia":  await Historia(); break;
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
    var credencial = await Credencial();
    using var pedido = new HttpRequestMessage(HttpMethod.Get, $"{recurso}/{id}/events?after=0");
    pedido.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", credencial);

    using var respuesta = await http.SendAsync(pedido);
    respuesta.EnsureSuccessStatusCode();

    using JsonDocument doc = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
    JsonElement eventos = doc.RootElement.GetProperty("events");

    Console.WriteLine($"{eventos.GetArrayLength()} eventos en {recurso}/{id}:");
    foreach (JsonElement e in eventos.EnumerateArray())
    {
        string examen = e.GetProperty("examen").GetString();
        string fecha = e.GetProperty("fecha").GetDateTime().ToString("yyyy-MM-dd");
        string resultado = examen == "Orina"
            ? Orina(e)
            : $"{e.GetProperty("valor").GetDecimal().ToString("0.##", CultureInfo.InvariantCulture)} "
              + e.GetProperty("unidad").GetString();
        Console.WriteLine($"  #{e.GetProperty("id")}  {fecha}  {examen,-20} {resultado}");
    }

    // El urianalisis informa DOS cosas. Un evento sin el campo es una prueba que no se
    // corrio, que no es lo mismo que un negativo, asi que no se dice nada de ella.
    static string Orina(JsonElement e)
    {
        string color = e.GetProperty("color").GetString();
        string nitritos = e.TryGetProperty("nitritos", out JsonElement n) ? n.GetString() : "";
        return string.IsNullOrWhiteSpace(nitritos) ? color : $"{color}, nitritos {nitritos}";
    }
}

// Un examen CONCRETO, con su fecha y su valor.
//   inyectar --examen AntigenoProstatico --fecha 2026-08-21 --valor 5.2 --unidad ng/mL
//   inyectar --examen Orina --fecha 2026-08-21 --color ambar --nitritos negativo
async Task Inyectar()
{
    string examen = Arg(args, "--examen", "");
    if (examen.Length == 0) { Console.Error.WriteLine("Falta --examen"); return; }

    var cuerpo = new
    {
        examen,
        fecha = Arg(args, "--fecha", DateTime.Today.ToString("yyyy-MM-dd")),
        valor = Decimal(Arg(args, "--valor", "0")),
        valorSecundario = Decimal(Arg(args, "--valor-secundario", "0")),
        unidad = Arg(args, "--unidad", ""),
        color = Arg(args, "--color", ""),
        nitritos = Arg(args, "--nitritos", ""),
    };

    using var respuesta = await http.PostAsJsonAsync($"{recurso}/{id}/events/examen", cuerpo);
    if (!respuesta.IsSuccessStatusCode)
    {
        Console.Error.WriteLine($"URL2 respondio {(int)respuesta.StatusCode}: "
            + await respuesta.Content.ReadAsStringAsync());
        return;
    }

    Console.WriteLine($"inyectado: {await respuesta.Content.ReadAsStringAsync()}");
}

// EL GUION COMPLETO de la demo, sembrado de una vez. Aca se edita LA HISTORIA.
//
// URL2 ya arranca con esta misma historia sembrada, asi que esto sirve para re-sembrarla
// sobre otro expediente o para agregarla despues de haber inyectado a mano.
//
// Las fechas son literales. Una demo con "hoy menos tres anos" deja de ser reproducible:
// las tendencias dependen de los dias transcurridos, y el mismo journal reproducido el
// ano que viene contaria otra historia.
async Task Historia()
{
    Console.WriteLine($"sembrando la historia en {recurso}/{id}...");

    (string examen, string fecha, decimal valor, string unidad, string color, string nitritos, string porque)[] guion =
    {
        ("AntigenoProstatico", "2023-08-21", 3.0m,   "ng/mL", "",      "", "linea base: de aca sale el 'venia estable'"),
        ("AntigenoProstatico", "2024-08-21", 3.1m,   "ng/mL", "",      "", "sigue estable un ano despues"),
        ("Colesterol",         "2025-08-21", 200.0m, "mg/dL", "",      "", "primer punto de la serie de colesterol"),
        ("AntigenoProstatico", "2025-08-21", 3.0m,   "ng/mL", "",      "", "tercer ano planchado"),
        ("Colesterol",         "2026-08-21", 280.0m, "mg/dL", "",      "", "el salto que cuenta la historia"),
        ("AntigenoProstatico", "2026-08-21", 5.2m,   "ng/mL", "",      "", "el salto que exige atencion"),
        ("Hemoglobina",        "2026-08-21", 14.2m,  "g/dL",  "",      "", "para que la lista tenga cuerpo"),
        ("Orina",              "2026-08-21", 0m,     "",      "ambar", "negativo",
            "un examen SIN serie, y con DOS formas de magnitud: un color y un binario"),
    };

    foreach (var (examen, fecha, valor, unidad, color, nitritos, porque) in guion)
    {
        var cuerpo = new { examen, fecha, valor, valorSecundario = 0m, unidad, color, nitritos };
        using var r = await http.PostAsJsonAsync($"{recurso}/{id}/events/examen", cuerpo);
        Console.WriteLine($"  {fecha}  {examen,-20} HTTP {(int)r.StatusCode}   {porque}");
    }
}

// El senuelo, para demostrarlo en vivo: con la credencial de la app, /perfil da 403.
// No es cortesia del mock - el scope del token es {recurso}.events.read y nada mas.
async Task Senuelo()
{
    string credencial = await Credencial();

    foreach (string ruta in new[] { $"{recurso}/{id}/events?after=0", $"{recurso}/{id}/perfil" })
    {
        using var pedido = new HttpRequestMessage(HttpMethod.Get, ruta);
        pedido.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", credencial);

        using var respuesta = await http.SendAsync(pedido);
        Console.WriteLine($"  GET {ruta,-40} -> {(int)respuesta.StatusCode}");
        if (!respuesta.IsSuccessStatusCode)
            Console.WriteLine($"       {await respuesta.Content.ReadAsStringAsync()}");
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

static decimal Decimal(string texto) =>
    decimal.TryParse(texto, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal valor) ? valor : 0m;

static string Arg(string[] args, string nombre, string porDefecto)
{
    for (int i = 0; i < args.Length - 1; i++)
        if (args[i] == nombre) return args[i + 1];
    return porDefecto;
}

static void Ayuda()
{
    Console.WriteLine("Inyector de eventos de demo para URL2 (laboratorio)");
    Console.WriteLine();
    Console.WriteLine("  listar                        que eventos ve la app hoy");
    Console.WriteLine("  historia                      siembra el guion completo de la demo");
    Console.WriteLine("  senuelo                       demuestra el 403 de /perfil con el token de la app");
    Console.WriteLine("  inyectar --examen AntigenoProstatico --fecha 2026-08-21 --valor 5.2 --unidad ng/mL");
    Console.WriteLine("  inyectar --examen Orina --fecha 2026-08-21 --color ambar --nitritos negativo");
    Console.WriteLine("     nitritos: positivo | negativo | (vacio = no se corrio la prueba, que NO es negativo)");
    Console.WriteLine("                                agrega UN examen concreto");
    Console.WriteLine();
    Console.WriteLine("  cargas: Colesterol, ColesterolDetallado, AntigenoProstatico, Hemoglobina, Orina, Heces");
    Console.WriteLine("  --url      default http://localhost:8111/");
    Console.WriteLine("  --recurso  default expediente");
    Console.WriteLine("  --id       default 1");
}
