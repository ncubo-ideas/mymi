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
//   inyectar --fecha 2026-08-30 --litros 38 --precio 668 --total 25384 \
//            --surtidor A-3 --corte automatico --transaccion T-0229
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
        placa = Arg(args, "--placa", "BQR-482"),
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
        ("2025-12-15", 37m, "A-3", 615m, 22755m, "automatico", "T-0201", "linea base, sin rendimiento aun"),
        ("2026-01-01", 36m, "A-3", 620m, 22320m, "automatico", "T-0202", "primer tramo: 12.6 km/L"),
        ("2026-01-17", 38m, "A-3", 620m, 23560m, "automatico", "T-0203", "confirma la base"),
        ("2026-01-27", 37m, "A-3", 620m, 22940m, "automatico", "T-0204", "la serie del canon: 51 249 km"),
        ("2026-02-06", 36m, "A-3", 628m, 22608m, "automatico", "T-0205", "la serie del canon: 51 703 km"),
        ("2026-02-16", 36m, "A-3", 628m, 22608m, "automatico", "T-0206", "la serie del canon: 52 157 km"),
        ("2026-02-21", 20m, "A-3", 628m, 12560m, "preseteado", "T-0207", "PRESETEADA: la inferencia dice 'no fue lleno'"),
        ("2026-03-02", 34m, "A-3", 634m, 21556m, "automatico", "T-0208", "la parcial no rompe el tramo: 12.6"),
        ("2026-03-12", 37m, "A-3", 634m, 23458m, "automatico", "T-0209", "la serie del canon: 53 303 km"),
        ("2026-03-22", 36m, "A-3", 634m, 22824m, "automatico", "T-0210", "la serie del canon: 53 757 km"),
        ("2026-04-01", 38m, "A-3", 640m, 24320m, "automatico", "T-0211", "la serie del canon: 54 236 km"),
        ("2026-04-11", 36m, "A-3", 640m, 23040m, "automatico", "T-0212", "la serie del canon: 54 690 km"),
        ("2026-04-21", 37m, "A-3", 640m, 23680m, "automatico", "T-0213", "la serie del canon: 55 156 km"),
        ("2026-05-02", 38m, "A-3", 645m, 24510m, "automatico", "T-0214", "empieza la caida: 11.7"),
        ("2026-05-12", 36m, "A-3", 645m, 23220m, "automatico", "T-0215", "la serie del canon: 56 089 km"),
        ("2026-05-21", 38m, "A-3", 645m, 24510m, "automatico", "T-0216", "la serie del canon: 56 534 km"),
        ("2026-05-30", 36m, "A-3", 645m, 23220m, "automatico", "T-0217", "la serie del canon: 56 955 km"),
        ("2026-06-02", 10m, "B-1", 652m, 6520m, "automatico", "T-0218", "OTRO SURTIDOR: tramo no comparable, y por 6520 no da boleto"),
        ("2026-06-12", 39m, "A-3", 652m, 25428m, "automatico", "T-0219", "el escalon sigue bajando"),
        ("2026-06-21", 37m, "A-3", 652m, 24124m, "automatico", "T-0220", "la serie del canon: 57 972 km"),
        ("2026-06-30", 38m, "A-3", 652m, 24776m, "automatico", "T-0221", "la serie del canon: 58 390 km"),
        ("2026-07-09", 37m, "A-3", 660m, 24420m, "automatico", "T-0222", "la serie del canon: 58 775 km"),
        ("2026-07-17", 39m, "A-3", 660m, 25740m, "automatico", "T-0223", "la serie del canon: 59 157 km"),
        ("2026-07-25", 38m, "A-3", 660m, 25080m, "automatico", "T-0224", "la serie del canon: 59 514 km"),
        ("2026-08-01", 36m, "A-3", 668m, 24048m, "automatico", "T-0225", "la serie del canon: 59 852 km"),
        ("2026-08-09", 39m, "A-3", 668m, 26052m, "automatico", "T-0226", "la serie del canon: 60 219 km"),
        ("2026-08-17", 39m, "A-3", 668m, 26052m, "automatico", "T-0227", "a esta le va a faltar el kilometraje"),
        ("2026-08-25", 40m, "A-3", 668m, 26720m, "automatico", "T-0228", "el tramo largo: 9.4, la anomalia"),
    };

    foreach (var (fecha, litros, surtidor, precio, total, corte, transaccion, porque) in guion)
    {
        var cuerpo = new
        {
            fecha, placa = "BQR-482", tipoDeCombustible = "regular",
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
    Console.WriteLine("  inyectar --litros 38 --precio 668 [--total 25384] [--fecha 2026-08-30]");
    Console.WriteLine("           [--surtidor A-3] [--corte automatico|preseteado|indeterminado]");
    Console.WriteLine("           [--tipo regular|super|diesel] [--unidad L|galUS|galImp] [--moneda CRC|PAB|USD]");
    Console.WriteLine("           [--placa BQR-482] [--transaccion T-0229]");
    Console.WriteLine("                                agrega UNA carga concreta");
    Console.WriteLine();
    Console.WriteLine("  Los kilometrajes y el tanque NO se inyectan: son aportes de Sonia y entran");
    Console.WriteLine("  por su app (o por 'Cargas.Consola sembrar-aportes' sobre el journal).");
    Console.WriteLine();
    Console.WriteLine("  --url      default http://localhost:8111/");
    Console.WriteLine("  --recurso  default cargas");
    Console.WriteLine("  --id       default 1");
}
