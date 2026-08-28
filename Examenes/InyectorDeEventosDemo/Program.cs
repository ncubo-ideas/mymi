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
// el colesterol de Sonia viene subiendo, que es toda la demo.
//
// Regla de la casa: se publica SOLO lo que la app va a mostrar. Si la historia son once
// examenes, son once eventos, no sesenta.

string baseUrl = Arg(args, "--url", "http://localhost:8091/");
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
        case "sexo":      await Sexo(); break;
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
        string fecha = e.GetProperty("fecha").GetDateTime().ToString("yyyy-MM-dd");

        // DOS TIPOS DE EVENTO desde que el laboratorio publica datos del paciente. Un
        // evento de paciente no tiene `examen` ni `valor`, asi que pedirselos daria
        // blancos: se pregunta por el tipo, que es lo que discrimina.
        string tipo = e.TryGetProperty("tipo", out JsonElement t) ? t.GetString() : "ExamenAgregado";
        if (tipo == "DatosDePacienteActualizados")
        {
            Console.WriteLine($"  #{e.GetProperty("id")}  {fecha}  {"(paciente)",-20} "
                + $"sexo {e.GetProperty("sexo").GetString()}");
            continue;
        }

        string examen = e.GetProperty("examen").GetString();
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
//   inyectar --examen Colesterol --fecha 2026-04-28 --valor 212 --unidad mg/dL
//   inyectar --examen Orina --fecha 2026-04-28 --color ambar --nitritos negativo
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

// EL LABORATORIO PUBLICA EL SEXO DEL PACIENTE.
//   sexo --sexo mujer
//
// Va aparte del guion sembrado a proposito. La demo tiene que poder mostrar los TRES
// estados, y el orden importa:
//
//   1. se abre la app sin este evento    -> el sexo es DESCONOCIDO y el bloque insiste
//   2. Sonia aprieta "Soy mujer"         -> la hemoglobina y el HDL consiguen su rango y
//                                           el expediente entero queda interpretado
//   3. se corre esto con --sexo hombre   -> el laboratorio publica lo contrario, y como
//                                           gana lo que dijo ella, NO cambia nada: eso es
//                                           el "tú eres la autoridad final" en vivo
//
// El guion de Sonia no trae antigeno prostatico porque a ella no se lo hicieron. Para ver
// el caso "este analito no te corresponde" con una serie de verdad, se inyecta uno a mano
// y se declara mujer desde la app: la tarjeta desaparece. Este comando es la otra fuente,
// y esta para poder mostrar el conflicto.
async Task Sexo()
{
    string sexo = Arg(args, "--sexo", "");
    if (sexo.Length == 0) { Console.Error.WriteLine("Falta --sexo (hombre | mujer)"); return; }

    using var respuesta = await http.PostAsJsonAsync($"{recurso}/{id}/events/paciente",
        new { sexo, fecha = Arg(args, "--fecha", DateTime.Today.ToString("yyyy-MM-dd")) });

    if (!respuesta.IsSuccessStatusCode)
    {
        Console.Error.WriteLine($"URL2 respondio {(int)respuesta.StatusCode}: "
            + await respuesta.Content.ReadAsStringAsync());
        return;
    }

    Console.WriteLine($"publicado: {await respuesta.Content.ReadAsStringAsync()}");
}

// EL GUION COMPLETO de la demo, sembrado de una vez. Aca se edita LA HISTORIA.
//
// URL2 ya arranca con esta misma historia sembrada, asi que esto sirve para re-sembrarla
// sobre otro expediente o para agregarla despues de haber inyectado a mano.
//
// Las fechas son literales. Una demo con "hoy menos tres anos" deja de ser reproducible:
// las tendencias dependen de los dias transcurridos, y el mismo journal reproducido el
// ano que viene contaria otra historia.
//
// ES EL EXPEDIENTE DE SONIA: 34 anos, y sus resultados del 28 de abril de 2026. Los tres
// colesteroles son la serie -182 y 188 de linea base, 212 de hallazgo- y son lo que la
// portada de la app titula.
//
// EL VALOR SECUNDARIO VIAJA EN EL GUION porque el perfil lipidico son DOS analitos: el
// LDL y su HDL. Mandarlo en cero hacia que la app descartara el evento —cero mg/dL de HDL
// no es un valor que una persona viva pueda tener— y el primer punto de la historia se
// perdia en silencio.
//
// LO QUE ESTE COMANDO NO PUEDE SEMBRAR: el examen de heces y la sangre de la orina. El
// endpoint de inyeccion no tiene campo `sangre`, asi que las tres pruebas de sangre oculta
// del guion viven solo en la semilla de URL2. Con esto se re-siembra la parte cuantitativa
// del expediente, no el expediente entero.
async Task Historia()
{
    Console.WriteLine($"sembrando la historia en {recurso}/{id}...");

    (string examen, string fecha, decimal valor, decimal secundario, string unidad, string color, string nitritos, string porque)[] guion =
    {
        ("ColesterolDetallado", "2021-04-22", 118.0m, 58.0m, "mg/dL", "",      "", "el perfil lipidico viejo: es lo unico que sale VENCIDO"),
        ("Colesterol",          "2024-04-24", 182.0m, 0m,    "mg/dL", "",      "", "linea base: de aca sale el 'venia bien'"),
        ("Hemoglobina",         "2024-04-24", 13.1m,  0m,    "g/dL",  "",      "", "primer punto del analito cuyo rango depende del sexo"),
        ("Colesterol",          "2025-04-26", 188.0m, 0m,    "mg/dL", "",      "", "subio 6 mg/dL, que NO llega a ser un cambio"),
        ("Colesterol",          "2026-04-28", 212.0m, 0m,    "mg/dL", "",      "", "el ascenso que cuenta la historia, y sigue en rango a los 34"),
        ("Hemoglobina",         "2026-04-28", 13.4m,  0m,    "g/dL",  "",      "", "estable"),
        ("Orina",               "2026-04-28", 0m,     0m,    "",      "ambar", "negativo",
            "un examen con DOS formas de magnitud: un color y un binario"),
        ("Orina",               "2025-04-26", 0m,     0m,    "",      "amarillo palido", "negativo",
            "la segunda tira: con dos, la serie puede decir que nunca aparecio"),
    };

    foreach (var (examen, fecha, valor, secundario, unidad, color, nitritos, porque) in guion)
    {
        var cuerpo = new { examen, fecha, valor, valorSecundario = secundario, unidad, color, nitritos };
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
    Console.WriteLine("  sexo --sexo mujer             el laboratorio publica el sexo del paciente");
    Console.WriteLine("     (la app arranca en 'desconocido' a proposito: es lo que hace que el");
    Console.WriteLine("      bloque que insiste se vea. Si la persona ya lo declaro, gana ella.)");
    Console.WriteLine("  inyectar --examen Colesterol --fecha 2026-04-28 --valor 212 --unidad mg/dL");
    Console.WriteLine("  inyectar --examen Orina --fecha 2026-04-28 --color ambar --nitritos negativo");
    Console.WriteLine("     nitritos: positivo | negativo | (vacio = no se corrio la prueba, que NO es negativo)");
    Console.WriteLine("                                agrega UN examen concreto");
    Console.WriteLine();
    Console.WriteLine("  examenes: Colesterol, ColesterolDetallado, AntigenoProstatico, Hemoglobina, Orina, Heces");
    Console.WriteLine("  --url      default http://localhost:8091/");
    Console.WriteLine("  --recurso  default expediente");
    Console.WriteLine("  --id       default 1");
}
