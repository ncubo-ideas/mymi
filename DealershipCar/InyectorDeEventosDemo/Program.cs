using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;

// Inyector de eventos de demo para URL2, el mock del concesionario.
//
// Herramienta de operacion: la usa quien monta la demo, no la persona usuaria. Le habla a URL2 por
// HTTP igual que cualquier operador, sin referenciar ni el dominio ni la app.
//
// POR QUE EXISTE: los datos del demo se ORQUESTAN COMO TEST CASES. Un endpoint que agrega una
// lectura de odometro aleatoria no sirve para contar una historia, porque CUANDO ALGO VENCE depende
// de los valores y de los instantes. Con lecturas al azar no se puede demostrar que el filtro vence
// justo despues de la revision del fabricante, que es toda la demo.
//
// LAS FECHAS SON LITERALES. Una demo con "hoy menos seis meses" deja de ser reproducible: los
// vencimientos dependen de los dias transcurridos, y el mismo journal reproducido el ano que viene
// contaria otra historia. Es la regla de C27 y aca vale igual que en el guion de la consola.
//
// Regla de la casa: se publica SOLO lo que la app va a mostrar. Si la historia son diecinueve
// eventos, son diecinueve, no doscientos.

string baseUrl = Arg(args, "--url", "http://localhost:8092/");
string recurso = Arg(args, "--recurso", "vehicle");
string id = Arg(args, "--id", "1HGCM82633A004352");

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
        Console.WriteLine($"  #{e.GetProperty("id")}  {e.GetProperty("tipo").GetString(),-26} {Resumen(e)}");
}

// Lo que se ve de cada evento en una linea. Los cinco tipos que la app consume dicen su hecho; los
// que no consume se muestran igual, porque VERLOS es la mitad del punto: el concesionario publica
// su narrativa entera y la app se queda con su rebanada.
static string Resumen(JsonElement e)
{
    string tipo = e.GetProperty("tipo").GetString();

    return tipo switch
    {
        "OdometerReported" =>
            $"{Numero(e, "value")} {Texto(e, "unit")} observado el {Dia(e, "observedAt")} ({Texto(e, "source")})",
        "ConditionThresholdCrossed" =>
            $"{Texto(e, "kind")} cruzo su umbral el {Dia(e, "observedAt")}",
        "OperationCompleted" =>
            $"{Texto(e, "kind")} hecho el {Dia(e, "completedAt")} con {Numero(e, "completionReading")} {Texto(e, "unit")}",
        "PlanOperationSeeded" =>
            $"{Texto(e, "kind")} {Texto(e, "basis")} {Numero(e, "timeLimitDays")}d/{Numero(e, "distanceLimit")}{Texto(e, "unit")}"
            + $" anclada el {Dia(e, "anchoredAt")} en {Numero(e, "anchorReading")}",
        "PlanRevisionAdopted" =>
            $"revision {Texto(e, "revisionCode")} adoptada el {Dia(e, "adoptedAt")}",
        _ => "(un tipo que la app NO consume: lo descarta)",
    };
}

// Un hecho CONCRETO, con sus valores exactos.
//   inyectar --tipo OdometerReported --value 31000 --observedAt 2026-10-01 --source Telemetry
//   inyectar --tipo OperationCompleted --kind FilterChange --completedAt 2026-10-02 --completionReading 31000
async Task Inyectar()
{
    string tipo = Arg(args, "--tipo", "");
    if (tipo.Length == 0) { Console.Error.WriteLine("Falta --tipo"); return; }

    var cuerpo = new
    {
        tipo,
        value = Entero(Arg(args, "--value", "0")),
        unit = Arg(args, "--unit", "UnitA"),
        observedAt = Arg(args, "--observedAt", "2026-09-01"),
        source = Arg(args, "--source", "Telemetry"),
        kind = Arg(args, "--kind", ""),
        completedAt = Arg(args, "--completedAt", "2026-09-01"),
        completionReading = Entero(Arg(args, "--completionReading", "0")),
        basis = Arg(args, "--basis", ""),
        timeLimitDays = Entero(Arg(args, "--timeLimitDays", "0")),
        distanceLimit = Entero(Arg(args, "--distanceLimit", "0")),
        isMandatory = Arg(args, "--isMandatory", "false") == "true",
        isSafety = Arg(args, "--isSafety", "false") == "true",
        anchoredAt = Arg(args, "--anchoredAt", "2026-09-01"),
        adoptedAt = Arg(args, "--adoptedAt", "2026-09-01"),
        anchorReading = Entero(Arg(args, "--anchorReading", "0")),
        anchorUnit = Arg(args, "--anchorUnit", "UnitA"),
        revisionCode = Arg(args, "--revisionCode", ""),
    };

    using var respuesta = await http.PostAsJsonAsync($"{recurso}/{id}/events/hecho", cuerpo);
    if (!respuesta.IsSuccessStatusCode)
    {
        Console.Error.WriteLine($"URL2 respondio {(int)respuesta.StatusCode}: "
            + await respuesta.Content.ReadAsStringAsync());
        return;
    }

    Console.WriteLine($"inyectado: {await respuesta.Content.ReadAsStringAsync()}");
}

// EL GUION COMPLETO de la demo, sembrado de una vez. ACA SE EDITA LA HISTORIA.
//
// URL2 ya arranca con esta misma historia sembrada, asi que esto sirve para re-sembrarla sobre otro
// vehiculo o para agregarla despues de haber inyectado a mano.
//
// El PORQUE de cada linea viaja al lado: es lo que permite editar la historia sin volver a deducir
// para que estaba cada hecho.
async Task Historia()
{
    Console.WriteLine($"sembrando la historia en {recurso}/{id}...");

    (object cuerpo, string porque)[] guion =
    {
        (Revision("R1", "2025-09-01"),
            "el plan con que se entrega el vehiculo"),

        (PlanOp("FluidChange", "TimeOrDistance", 365, 10000, true, false, "2025-09-01", 12000),
            "tiempo O distancia: vence con la primera de las dos que llegue (ley L1)"),
        (PlanOp("FilterChange", "DistanceOnly", 0, 15000, true, false, "2025-09-01", 12000),
            "solo distancia: sin lecturas no se mueve, y eso se ve"),
        (PlanOp("TimedComponent", "TimeOnly", 550, 0, true, true, "2025-09-01", 12000),
            "solo tiempo, y DE SEGURIDAD: la ley L6 no la deja aplazar"),
        (PlanOp("Inspection", "ConditionOnly", 0, 0, true, false, "2025-09-01", 12000),
            "solo condicion: no la mueve el tiempo ni la distancia, solo un cruce observado"),

        (Lectura(13200, "2025-10-01", "Telemetry"), "primer mes"),
        (Garantia("W-9912", "2025-11-15"),
            "UN TIPO QUE LA APP NO CONSUME: se descarta, no se inventa un comando para tragarlo"),
        (Lectura(16000, "2025-12-01", "Telemetry"),
            "llega ANTES que la de noviembre: el orden de llegada no es el de observacion"),
        (Lectura(14500, "2025-11-01", "HumanEntry"), "el relleno tardio que si calza en la linea"),
        (Lectura(17300, "2026-01-01", "Telemetry"), "enero"),
        (Lectura(20000, "2026-03-01", "Telemetry"), "marzo llega antes que febrero"),
        (Lectura(18600, "2026-02-01", "CustomerDeclared"), "y febrero calza entre las dos"),

        (Cruce("Inspection", "2026-04-01"),
            "el taller informa el cruce: es lo unico que mueve una ConditionOnly (ley L3)"),

        (Completacion("FluidChange", "2026-03-05", 20000),
            "la operacion se hizo DE VERDAD: su sucesora se ancla ahi y no donde el plan decia (ley L4)"),

        (Revision("R2", "2026-06-01"),
            "el fabricante emite una revision del plan"),
        (PlanOp("FluidChange", "TimeOrDistance", 300, 8000, true, false, "2026-06-01", 20000),
            "y su reemplazo SUSTITUYE al activo, no duplica (ley L5): intervalo mas corto"),

        (Lectura(27000, "2026-07-01", "Telemetry"), "julio: el filtro llega a su limite"),
        (Lectura(29500, "2026-08-15", "Telemetry"), "agosto: y el fluido pasa el suyo, por distancia"),

        (Cita("2026-09-10"),
            "OTRO TIPO QUE LA APP NO CONSUME: la cita es del concesionario, no de la agenda"),
    };

    foreach ((object cuerpo, string porque) in guion)
    {
        using var r = await http.PostAsJsonAsync($"{recurso}/{id}/events/hecho", cuerpo);
        Console.WriteLine($"  HTTP {(int)r.StatusCode}   {porque}");
    }
}

// El senuelo, para demostrarlo en vivo: con la credencial de la app, /customer da 403. No es
// cortesia del mock - el scope del token es {recurso}.events.read y nada mas.
async Task Senuelo()
{
    string credencial = await Credencial();

    foreach (string ruta in new[] { $"{recurso}/{id}/events?after=0", $"{recurso}/{id}/customer" })
    {
        using var pedido = new HttpRequestMessage(HttpMethod.Get, ruta);
        pedido.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", credencial);

        using var respuesta = await http.SendAsync(pedido);
        Console.WriteLine($"  GET {ruta,-52} -> {(int)respuesta.StatusCode}");
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

// ---------------------------------------------------------------- los cuerpos del guion

static object Lectura(int valor, string observado, string fuente) => new
{
    tipo = "OdometerReported", value = valor, unit = "UnitA", observedAt = observado, source = fuente,
};

static object Cruce(string kind, string observado) => new
{
    tipo = "ConditionThresholdCrossed", kind, observedAt = observado,
};

static object Completacion(string kind, string hecho, int lectura) => new
{
    tipo = "OperationCompleted", kind, completedAt = hecho, completionReading = lectura, unit = "UnitA",
};

static object Revision(string codigo, string adoptada) => new
{
    tipo = "PlanRevisionAdopted", revisionCode = codigo, adoptedAt = adoptada,
};

static object PlanOp(string kind, string basis, int dias, int distancia,
    bool obligatoria, bool seguridad, string anclada, int lecturaDelAncla) => new
{
    tipo = "PlanOperationSeeded", kind, basis,
    timeLimitDays = dias, distanceLimit = distancia, unit = "UnitA",
    isMandatory = obligatoria, isSafety = seguridad,
    anchoredAt = anclada, anchorReading = lecturaDelAncla, anchorUnit = "UnitA",
};

static object Garantia(string reclamo, string observado) => new
{
    tipo = "WarrantyClaimFiled", observedAt = observado, revisionCode = reclamo,
};

static object Cita(string observado) => new
{
    tipo = "AppointmentScheduled", observedAt = observado,
};

// ---------------------------------------------------------------- utilidades

static string Texto(JsonElement e, string campo) =>
    e.TryGetProperty(campo, out JsonElement v) && v.ValueKind == JsonValueKind.String ? v.GetString() : "";

static int Numero(JsonElement e, string campo) =>
    e.TryGetProperty(campo, out JsonElement v) && v.TryGetInt32(out int n) ? n : 0;

static string Dia(JsonElement e, string campo) =>
    e.TryGetProperty(campo, out JsonElement v) && v.TryGetDateTime(out DateTime d)
        ? d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
        : "";

static int Entero(string texto) =>
    int.TryParse(texto, NumberStyles.Any, CultureInfo.InvariantCulture, out int valor) ? valor : 0;

static string Arg(string[] args, string nombre, string porDefecto)
{
    for (int i = 0; i < args.Length - 1; i++)
        if (args[i] == nombre) return args[i + 1];
    return porDefecto;
}

static void Ayuda()
{
    Console.WriteLine("Inyector de eventos de demo para URL2 (concesionario)");
    Console.WriteLine();
    Console.WriteLine("  listar                        que eventos ve la app hoy");
    Console.WriteLine("  historia                      siembra el guion completo de la demo");
    Console.WriteLine("  senuelo                       demuestra el 403 de /customer con el token de la app");
    Console.WriteLine("  inyectar --tipo <tipo> ...    agrega UN hecho concreto");
    Console.WriteLine();
    Console.WriteLine("  tipos que la app consume:");
    Console.WriteLine("    OdometerReported           --value --unit --observedAt --source");
    Console.WriteLine("    ConditionThresholdCrossed  --kind --observedAt");
    Console.WriteLine("    OperationCompleted         --kind --completedAt --completionReading --unit");
    Console.WriteLine("    PlanOperationSeeded        --kind --basis --timeLimitDays --distanceLimit --unit");
    Console.WriteLine("                               --isMandatory --isSafety --anchoredAt --anchorReading --anchorUnit");
    Console.WriteLine("    PlanRevisionAdopted        --revisionCode --adoptedAt");
    Console.WriteLine();
    Console.WriteLine("  cualquier otro --tipo se acepta y la app lo DESCARTA, que es lo normal:");
    Console.WriteLine("  el concesionario publica su narrativa entera y este modulo modela una rebanada.");
    Console.WriteLine();
    Console.WriteLine("  ejemplos:");
    Console.WriteLine("    inyectar --tipo OdometerReported --value 31000 --observedAt 2026-10-01 --source Telemetry");
    Console.WriteLine("    inyectar --tipo OperationCompleted --kind FilterChange --completedAt 2026-10-02 --completionReading 31000");
    Console.WriteLine();
    Console.WriteLine("  --url      default http://localhost:8092/");
    Console.WriteLine("  --recurso  default vehicle");
    Console.WriteLine("  --id       default 1HGCM82633A004352  (el VIN)");
}
