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

// EL GUION COMPLETO, de un tiro. Es lo que la caja no puede dar: armar seis tickets a
// mano son veinte clicks, y antes de un demo lo que hace falta es dejar el escenario
// puesto. Tambien sirve para sembrar OTRO cliente (--id 2) o para rehacer la historia en
// un recurso limpio.
//
// Las fechas son LITERALES, igual que en el mock y en las muestras de la consola: con
// fechas relativas al dia de hoy la canasta cambiaria de mes y la comparacion anual
// dejaria de dar lo que la demo promete.
async Task Historia()
{
    Console.WriteLine($"sembrando la historia en {recurso}/{id}...");

    // Cuantos hay YA. El mock siembra su narrativa al arrancar -por clave recurso/id-,
    // asi que lo normal es que esto no este vacio: avisarlo evita que alguien duplique
    // los numeros de ticket sin darse cuenta. La app los rechazaria por numero repetido
    // y el refresh saldria con rechazos, que es un ruido feo en medio de un demo.
    int existentes = await CuantosEventos();
    if (existentes > 0)
        Console.WriteLine($"  OJO: {recurso}/{id} ya tiene {existentes} eventos. Los numeros de ticket "
            + "que se repitan los va a rechazar la app, no el mock.");

    (string numero, string fecha, string porque, string[] lineas)[] guion =
    {
        ("T-88101", "2025-08-05", "la canasta base: proteina, granos, lacteos y verdura",
            new[] { "POLLO-KG,Pechuga de pollo kg,2,4200,Proteinas,0",
                    "FRIJOL-900,Frijoles negros 900g,2,1150,GranosYAbarrotes,0",
                    "LECHE-1L,Leche semidescremada 1L,6,950,LacteosYHuevos,0",
                    "BROCOLI,Brocoli unidad,3,890,FrutasYVerduras,0" }),

        ("T-88240", "2025-08-20", "el unico aceite del mes: de aca sale el 8,8% ocasional",
            new[] { "ACEITE-GIR-1L,Aceite de girasol 1L,1,2890,AceitesYGrasas,0",
                    "ARROZ-2KG,Arroz grano entero 2kg,1,2450,GranosYAbarrotes,0",
                    "HUEVOS-30,Huevos carton 30,1,3300,LacteosYHuevos,0",
                    "TILAPIA-KG,Filet de tilapia kg,1,5200,Proteinas,0" }),

        ("T-91515", "2026-02-14", "empieza el deslizamiento: helado, sodas y galletas",
            new[] { "HELADO-1L,Helado de vainilla 1L,2,3450,Azucarados,0",
                    "SODA-2L,Soda cola 2L,4,1750,Azucarados,0",
                    "GALLETA-CHOC,Galletas de chocolate,3,1250,Azucarados,0",
                    "POLLO-KG,Pechuga de pollo kg,1,4350,Proteinas,0" }),

        ("T-93872", "2026-05-10", "LA COMPRA DE LA GARANTIA: la olla arrocera, 12 meses",
            new[] { "OLLA-ARR-18,Olla arrocera 1.8L,1,45900,Electrohogar,12",
                    "ARROZ-2KG,Arroz grano entero 2kg,2,2520,GranosYAbarrotes,0",
                    "MANTECA-500,Manteca de palma 500g,1,1980,AceitesYGrasas,0" }),

        ("T-95104", "2026-07-03", "el ultimo mes con algo de limpieza: de aca sale el hueco",
            new[] { "SODA-12PK,Soda cola lata 12-pack,2,5990,Azucarados,0",
                    "HELADO-1L,Helado de vainilla 1L,2,3560,Azucarados,0",
                    "PAPAS-FAM,Papas fritas familiar,3,2100,Snacks,0",
                    "DETERG-5L,Detergente liquido 5L,1,6800,Limpieza,0" }),

        ("T-96470", "2026-08-15", "la canasta ya se volteo: 97% ocasional",
            new[] { "HELADO-1L,Helado de vainilla 1L,3,3680,Azucarados,0",
                    "SODA-2L,Soda cola 2L,6,1790,Azucarados,0",
                    "CHOC-BARRA,Chocolate de leche barra,4,1450,Azucarados,0",
                    "MANTECA-500,Manteca de palma 500g,2,2050,AceitesYGrasas,0",
                    "BROCOLI,Brocoli unidad,1,920,FrutasYVerduras,0" }),
    };

    foreach (var (numero, fecha, porque, lineas) in guion)
    {
        var partes = lineas.Select(l => l.Split(',')).ToArray();
        var cuerpo = new
        {
            numero,
            fecha,
            skus = partes.Select(p => p[0].Trim()).ToArray(),
            descripciones = partes.Select(p => p[1].Trim()).ToArray(),
            cantidades = partes.Select(p => int.Parse(p[2], CultureInfo.InvariantCulture)).ToArray(),
            preciosUnitarios = partes.Select(p => decimal.Parse(p[3], CultureInfo.InvariantCulture)).ToArray(),
            categorias = partes.Select(p => p[4].Trim()).ToArray(),
            mesesDeGarantia = partes.Select(p => int.Parse(p[5], CultureInfo.InvariantCulture)).ToArray(),
        };

        using var r = await http.PostAsJsonAsync($"{recurso}/{id}/events/compra", cuerpo);
        Console.WriteLine($"  {fecha}  {numero,-10} {lineas.Length} lineas  HTTP {(int)r.StatusCode}   {porque}");
    }

    Console.WriteLine("listo. La app la trae con Refresh.");
}

// Cuantos eventos ve hoy la app. Se pide con su propio token, como cualquier device.
async Task<int> CuantosEventos()
{
    try
    {
        string credencial = await Credencial();
        using var pedido = new HttpRequestMessage(HttpMethod.Get, $"{recurso}/{id}/events?after=0");
        pedido.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", credencial);

        using var respuesta = await http.SendAsync(pedido);
        if (!respuesta.IsSuccessStatusCode) return 0;

        using JsonDocument doc = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("events").GetArrayLength();
    }
    catch
    {
        return 0;
    }
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
    Console.WriteLine("  historia                           siembra el guion completo del demo, de un tiro");
    Console.WriteLine("  senuelo                            demuestra el 403 estructural del token de la app");
    Console.WriteLine();
    Console.WriteLine("  opciones: --url http://localhost:8101/  --recurso cliente  --id 1");
    Console.WriteLine();
    Console.WriteLine("  categorias: Proteinas FrutasYVerduras LacteosYHuevos GranosYAbarrotes");
    Console.WriteLine("              AceitesYGrasas Azucarados Snacks Limpieza Electrohogar");
}
