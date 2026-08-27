using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;

// Inyector de eventos de demo para URL2.
//
// Herramienta de operacion: la usa quien monta la demo, no la persona usuaria. Le habla
// a URL2 por HTTP igual que cualquier operador, sin referenciar ni el dominio ni la app.
//
// Por que existe: los datos del demo se ORQUESTAN como test cases. Un endpoint que
// agrega una carga aleatoria no sirve para contar una historia, porque los TRAMOS
// dependen de los litros y de las fechas. Con valores al azar no se puede demostrar que
// el rendimiento viene cayendo, que es toda la demo.
//
// Regla de la casa: se publica SOLO lo que la app va a mostrar. Si la historia son
// nueve cargas, son nueve eventos, no sesenta.

string baseUrl = Arg(args, "--url", "http://localhost:8111/");
string recurso = Arg(args, "--recurso", "cargas");
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
        string tipo = e.GetProperty("tipo").GetString();
        string fecha = e.GetProperty("fecha").GetDateTime().ToString("yyyy-MM-dd");
        Console.WriteLine($"  #{e.GetProperty("id")}  {fecha}  {tipo,-18} {Resumen(e, tipo)}");
    }

    static string Resumen(JsonElement e, string tipo) => tipo switch
    {
        "CargaRegistrada" =>
            $"{Numero(e, "volumenValor")} {Texto(e, "volumenUnidad")} en {Texto(e, "surtidorIdentificacion")} "
            + $"({Texto(e, "formaDeCorte")}) = {Numero(e, "totalMonto")} {Texto(e, "totalMoneda")} "
            + $"[{Texto(e, "numeroDeTransaccion")}]",
        "PuntosAcreditados" => $"+{e.GetProperty("puntos").GetInt32()} puntos",
        "NivelActualizado" => $"nivel {Texto(e, "nivel")}",
        _ => "",
    };

    static string Texto(JsonElement e, string nombre) =>
        e.TryGetProperty(nombre, out JsonElement p) ? p.GetString() : "";

    static string Numero(JsonElement e, string nombre) =>
        e.TryGetProperty(nombre, out JsonElement p)
            ? p.GetDecimal().ToString("0.##", CultureInfo.InvariantCulture) : "";
}

// Una carga CONCRETA, con su fecha y sus numeros.
//   inyectar --fecha 2026-08-26 --litros 42 --precio 668 --total 28056 \
//            --surtidor A-3 --corte automatico --transaccion T-0210
async Task Inyectar()
{
    decimal litros = Decimal(Arg(args, "--litros", "0"));
    decimal precio = Decimal(Arg(args, "--precio", "0"));
    if (litros <= 0 || precio <= 0)
    {
        Console.Error.WriteLine("Faltan --litros y --precio (y opcionalmente --total)");
        return;
    }

    decimal total = Decimal(Arg(args, "--total", "0"));
    if (total <= 0) total = litros * precio;

    var cuerpo = new
    {
        fecha = Arg(args, "--fecha", DateTime.Today.ToString("yyyy-MM-dd")),
        placa = Arg(args, "--placa", "ABC-123"),
        tipoDeCombustible = Arg(args, "--tipo", "regular"),
        surtidor = Arg(args, "--surtidor", "A-3"),
        estacion = Arg(args, "--estacion", "La Sabana"),
        precioMonto = precio,
        precioMoneda = Arg(args, "--moneda", "CRC"),
        volumenValor = litros,
        volumenUnidad = Arg(args, "--unidad", "L"),
        totalMonto = total,
        totalMoneda = Arg(args, "--moneda", "CRC"),
        formaDeCorte = Arg(args, "--corte", "automatico"),
        numeroDeTransaccion = Arg(args, "--transaccion", $"T-{DateTime.Now:HHmmss}"),
    };

    using var respuesta = await http.PostAsJsonAsync($"{recurso}/{id}/events/carga", cuerpo);
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
// URL2 ya arranca con esta misma historia sembrada, asi que esto sirve para
// re-sembrarla sobre otra bitacora o para agregarla despues de inyectar a mano.
//
// Las fechas son literales. Una demo con "hoy menos ocho meses" deja de ser
// reproducible: los tramos dependen de los dias transcurridos, y el mismo journal
// reproducido el ano que viene contaria otra historia.
async Task Historia()
{
    Console.WriteLine($"sembrando la historia en {recurso}/{id}...");

    (string fecha, decimal litros, string surtidor, decimal precio, decimal total,
        string corte, string transaccion, string porque)[] guion =
    {
        ("2025-12-15", 40m, "A-3", 615m, 24600m, "automatico", "T-0201", "linea base, sin rendimiento aun"),
        ("2026-01-15", 38m, "A-3", 620m, 23560m, "automatico", "T-0202", "primer tramo: 12.6 km/L"),
        ("2026-02-15", 39m, "A-3", 628m, 24492m, "automatico", "T-0203", "confirma la base"),
        ("2026-03-20", 20m, "A-3", 634m, 12680m, "preseteado", "T-0204", "PRESETEADA: la inferencia dice 'no fue lleno'"),
        ("2026-03-28", 19m, "A-3", 634m, 12046m, "automatico", "T-0205", "la parcial no rompe el tramo: 12.6"),
        ("2026-05-20", 41m, "A-3", 645m, 26445m, "automatico", "T-0206", "empieza la caida: 11.7"),
        ("2026-06-20", 42m, "B-1", 652m, 27384m, "automatico", "T-0207", "OTRO SURTIDOR: tramo no comparable"),
        ("2026-07-25", 43m, "A-3", 660m, 28380m, "automatico", "T-0208", "a esta le va a faltar el kilometraje"),
        ("2026-08-25", 44m, "A-3", 668m, 29392m, "automatico", "T-0209", "el tramo largo: 9.4, la anomalia"),
    };

    foreach (var (fecha, litros, surtidor, precio, total, corte, transaccion, porque) in guion)
    {
        var cuerpo = new
        {
            fecha, placa = "ABC-123", tipoDeCombustible = "regular",
            surtidor, estacion = "La Sabana",
            precioMonto = precio, precioMoneda = "CRC",
            volumenValor = litros, volumenUnidad = "L",
            totalMonto = total, totalMoneda = "CRC",
            formaDeCorte = corte, numeroDeTransaccion = transaccion,
        };
        using var r = await http.PostAsJsonAsync($"{recurso}/{id}/events/carga", cuerpo);
        Console.WriteLine($"  {fecha}  {litros,5} L {surtidor,-4} HTTP {(int)r.StatusCode}   {porque}");

        var puntos = new { fecha, puntos = (int)litros };
        using var rp = await http.PostAsJsonAsync($"{recurso}/{id}/events/puntos", puntos);
        Console.WriteLine($"  {fecha}  +{(int)litros} puntos       HTTP {(int)rp.StatusCode}");
    }

    var nivel = new { fecha = "2026-06-20", nivel = "plata" };
    using var rn = await http.PostAsJsonAsync($"{recurso}/{id}/events/nivel", nivel);
    Console.WriteLine($"  2026-06-20  nivel plata      HTTP {(int)rn.StatusCode}");
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
    Console.WriteLine("Inyector de eventos de demo para URL2 (la estacion)");
    Console.WriteLine();
    Console.WriteLine("  listar                        que eventos ve la app hoy");
    Console.WriteLine("  historia                      siembra el guion completo de la demo (cargas + lealtad)");
    Console.WriteLine("  senuelo                       demuestra el 403 de /perfil con el token de la app");
    Console.WriteLine("  inyectar --litros 42 --precio 668 [--total 28056] [--fecha 2026-08-26]");
    Console.WriteLine("           [--surtidor A-3] [--corte automatico|preseteado|indeterminado]");
    Console.WriteLine("           [--tipo regular|super|diesel] [--unidad L|galUS|galImp] [--moneda CRC|PAB|USD]");
    Console.WriteLine("           [--placa ABC-123] [--transaccion T-0210]");
    Console.WriteLine("                                agrega UNA carga concreta");
    Console.WriteLine();
    Console.WriteLine("  Los kilometrajes y el tanque NO se inyectan: son aportes de Sonia y entran");
    Console.WriteLine("  por su app (o por 'Cargas.Consola sembrar-aportes' sobre el journal).");
    Console.WriteLine();
    Console.WriteLine("  --url      default http://localhost:8111/");
    Console.WriteLine("  --recurso  default cargas");
    Console.WriteLine("  --id       default 1");
}
