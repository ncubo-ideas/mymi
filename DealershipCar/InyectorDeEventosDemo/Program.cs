using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;

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
        case "ficha":     await Ficha(); break;
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

        "SpecificationRevisionOpened" =>
            $"abre {Texto(e, "revisionCode")}, publicada el {Dia(e, "publishedAt")}",
        "SpecificationRevisionSealed" =>
            $"sella {Texto(e, "revisionCode")}",
        "SpecificationRevisionSuperseded" =>
            $"{Texto(e, "revisionCode")} sustituida por {Texto(e, "supersededByCode")}",
        "FluidSpecificationPublished" =>
            $"{Texto(e, "revisionCode")}: {Texto(e, "kind")} grado {Texto(e, "grade")}"
            + $", {Numero(e, "capacity")} {Texto(e, "capacityUnit")}"
            + $", cada {Numero(e, "intervalDistance")} {Texto(e, "intervalUnit")}",
        "TyreSpecificationPublished" =>
            $"{Texto(e, "revisionCode")}: llanta {Texto(e, "position")} {Texto(e, "size")}"
            + $", {Numero(e, "coldPressure")} {Texto(e, "pressureUnit")}, {Texto(e, "ratings")}",
        "FuelGradePublished" =>
            $"{Texto(e, "revisionCode")}: combustible {Texto(e, "grade")} ({Texto(e, "standing")})",
        "ServiceIntervalPublished" =>
            $"{Texto(e, "revisionCode")}: {Texto(e, "kind")} cada "
            + $"{Numero(e, "intervalDistance")} {Texto(e, "intervalUnit")}"
            + $" o {Numero(e, "intervalDays")} dias",
        "ServicePricePublished" =>
            $"{Texto(e, "revisionCode")}: {Texto(e, "kind")} en "
            + $"{Numero(e, "milestoneDistance")} {Texto(e, "milestoneUnit")} cuesta "
            + $"{Numero(e, "amount")} {Texto(e, "currency")}"
            + $", vigente {Dia(e, "validFrom")} a {Dia(e, "validUntil")}",
        "CoveredSystemPublished" =>
            $"{Texto(e, "revisionCode")}: cubre '{Texto(e, "systemCode")}', limites propios "
            + $"{Numero(e, "ownLimitMonths")} meses / {Numero(e, "ownLimitDistance")} "
            + $"{Texto(e, "ownLimitUnit")}",
        "CoverageExclusionPublished" =>
            $"{Texto(e, "revisionCode")}: excluye '{Texto(e, "exclusion")}' de "
            + $"'{Texto(e, "systemCode")}' cuando {Texto(e, "condition")}",
        "ServicingPolicyPublished" =>
            $"{Texto(e, "revisionCode")}: servicio en otro taller -- {Texto(e, "rule")}",

        _ => "(un tipo que la app NO consume: lo descarta)",
    };
}

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

        publishedAt = Arg(args, "--publishedAt", "2026-09-01"),
        supersededByCode = Arg(args, "--supersededByCode", ""),
        grade = Arg(args, "--grade", ""),
        capacity = Decimal(Arg(args, "--capacity", "0")),
        capacityUnit = Arg(args, "--capacityUnit", "UnitA"),
        intervalDistance = Entero(Arg(args, "--intervalDistance", "0")),
        intervalUnit = Arg(args, "--intervalUnit", "UnitA"),
        intervalDays = Entero(Arg(args, "--intervalDays", "0")),
        position = Arg(args, "--position", ""),
        size = Arg(args, "--size", ""),
        coldPressure = Decimal(Arg(args, "--coldPressure", "0")),
        pressureUnit = Arg(args, "--pressureUnit", "UnitA"),
        ratings = Arg(args, "--ratings", ""),
        standing = Arg(args, "--standing", ""),
        milestoneDistance = Entero(Arg(args, "--milestoneDistance", "0")),
        milestoneUnit = Arg(args, "--milestoneUnit", "UnitA"),
        amount = Decimal(Arg(args, "--amount", "0")),
        currency = Arg(args, "--currency", ""),
        validFrom = Arg(args, "--validFrom", "2026-09-01"),
        validUntil = Arg(args, "--validUntil", "2027-09-01"),
        systemCode = Arg(args, "--systemCode", ""),
        ownLimitMonths = Entero(Arg(args, "--ownLimitMonths", "0")),
        ownLimitDistance = Entero(Arg(args, "--ownLimitDistance", "0")),
        ownLimitUnit = Arg(args, "--ownLimitUnit", "UnitA"),
        exclusion = Arg(args, "--exclusion", ""),
        condition = Arg(args, "--condition", ""),
        rule = Arg(args, "--rule", ""),
        whatPreservesCover = Arg(args, "--whatPreservesCover", ""),
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

async Task Historia()
{
    Console.WriteLine($"sembrando la historia en {recurso}/{id}...");

    (object cuerpo, string porque)[] guion =
    {
        (Revision("R1", "2025-09-01"),
            "el plan con que se entrega el vehiculo"),

        (PlanOp("FluidChange", "TimeOrDistance", 365, 10000, true, false, "2025-09-01", 12000),
            "tiempo O distancia: vence con la primera de las dos que llegue"),
        (PlanOp("FilterChange", "DistanceOnly", 0, 15000, true, false, "2025-09-01", 12000),
            "solo distancia: sin lecturas no se mueve, y eso se ve"),
        (PlanOp("TimedComponent", "TimeOnly", 550, 0, true, true, "2025-09-01", 12000),
            "solo tiempo, y DE SEGURIDAD: la ley no la deja aplazar"),
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
            "el taller informa el cruce: es lo unico que mueve una ConditionOnly"),

        (Completacion("FluidChange", "2026-03-05", 20000),
            "la operacion se hizo DE VERDAD: su sucesora se ancla ahi y no donde el plan decia"),

        (Revision("R2", "2026-06-01"),
            "el fabricante emite una revision del plan"),
        (PlanOp("FluidChange", "TimeOrDistance", 300, 8000, true, false, "2026-06-01", 20000),
            "y su reemplazo SUSTITUYE al activo, no duplica: intervalo mas corto"),

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

async Task Ficha()
{
    string primera = Arg(args, "--revision", "MY2025-R1");
    string segunda = Arg(args, "--revision2", "MY2025-R2");

    Console.WriteLine($"publicando la ficha del fabricante en {recurso}/{id}...");
    Console.WriteLine($"  revisiones: {primera} y {segunda}");
    Console.WriteLine();

    (object cuerpo, string porque)[] guion =
    {
        (RevisionAbierta(primera, "2025-08-15"),
            "el fabricante ABRE la revision: nace borrador y no contesta nada"),

        (Fluido(primera, "EngineOil", "5W-30 synthetic", 4.2m, 10000, 365),
            "el aceite: grado, capacidad con su unidad, y cada cuanto se cambia"),
        (Llanta(primera, "Front", "205/55R16", 2.4m, "94V"),
            "la llanta delantera: la presion viaja en la unidad en que se publico"),

        (Combustible(primera, "95 RON Premium", "Permitted"),
            "el combustible se arma grado a grado, y el minimo NO va primero"),
        (Combustible(primera, "91 RON regular", "Minimum"), "el minimo"),
        (Combustible(primera, "E85 Flex", "Prohibited"),
            "y el prohibido, que es la mitad que importa"),

        (Intervalo(primera, "FluidChange", 10000, 365),
            "cada cuanto dice el fabricante que se hace: es el numero PUBLICADO"),
        (Precio(primera, "FluidChange", 10000, 89.90m, "USD", "2025-08-15", "2026-12-31"),
            "el precio en un hito, con su vigencia"),

        (Sistema(primera, "POWERTRAIN", 0, 0),
            "un sistema adentro del alcance; CERO EN LAS DOS MITADES es como se dice que "
            + "no publico limites propios"),
        (Exclusion(primera, "POWERTRAIN", "wear parts", "when the part is a consumable"),
            "y la exclusion, con la condicion bajo la cual muerde"),

        (Regla(primera, "service may be done at any qualified workshop",
                "keep the invoices and the parts records"),
            "la regla de servicio en otro taller y que conserva la cobertura"),

        (RevisionSellada(primera),
            "SELLA la revision: es lo unico que la hace contestar"),

        (RevisionAbierta(segunda, "2026-02-01"),
            "abre una segunda revision"),
        (Fluido(segunda, "EngineOil", "0W-20 synthetic", 4.2m, 10000, 365),
            "que republica SOLO el aceite"),
        (RevisionSellada(segunda),
            "y la sella"),

        (RevisionSustituida(primera, segunda),
            "SUSTITUYE la primera: lo que la sucesora no republico se retira con ella"),
    };

    foreach ((object cuerpo, string porque) in guion)
    {
        using var r = await http.PostAsJsonAsync($"{recurso}/{id}/events/hecho", cuerpo);
        Console.WriteLine($"  HTTP {(int)r.StatusCode}   {porque}");
    }

    Console.WriteLine();
    Console.WriteLine("UN CODIGO DE REVISION SE TOMA UNA SOLA VEZ Y PARA SIEMPRE. Correr esto dos");
    Console.WriteLine("veces contra el mismo journal hace que el segundo OpenRevision se rechace:");
    Console.WriteLine("la app pierde ese hecho, sigue con el siguiente, y deja una marca. Para una");
    Console.WriteLine("segunda corrida hay que pasar --revision y --revision2 con codigos nuevos.");
}

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

static object RevisionAbierta(string codigo, string publicada) => new
{
    tipo = "SpecificationRevisionOpened", revisionCode = codigo, publishedAt = publicada,
};

static object RevisionSellada(string codigo) => new
{
    tipo = "SpecificationRevisionSealed", revisionCode = codigo,
};

static object RevisionSustituida(string codigo, string sucesora) => new
{
    tipo = "SpecificationRevisionSuperseded", revisionCode = codigo, supersededByCode = sucesora,
};

static object Fluido(string codigo, string kind, string grado,
    decimal capacidad, int distancia, int dias) => new
{
    tipo = "FluidSpecificationPublished", revisionCode = codigo, kind, grade = grado,
    capacity = capacidad, capacityUnit = "UnitA",
    intervalDistance = distancia, intervalUnit = "UnitA", intervalDays = dias,
};

static object Llanta(string codigo, string posicion, string medida,
    decimal presion, string clasificaciones) => new
{
    tipo = "TyreSpecificationPublished", revisionCode = codigo, position = posicion, size = medida,
    coldPressure = presion, pressureUnit = "UnitA", ratings = clasificaciones,
};

static object Combustible(string codigo, string grado, string postura) => new
{
    tipo = "FuelGradePublished", revisionCode = codigo, grade = grado, standing = postura,
};

static object Intervalo(string codigo, string kind, int distancia, int dias) => new
{
    tipo = "ServiceIntervalPublished", revisionCode = codigo, kind,
    intervalDistance = distancia, intervalUnit = "UnitA", intervalDays = dias,
};

static object Precio(string codigo, string kind, int hito,
    decimal monto, string moneda, string desde, string hasta) => new
{
    tipo = "ServicePricePublished", revisionCode = codigo, kind,
    milestoneDistance = hito, milestoneUnit = "UnitA",
    amount = monto, currency = moneda, validFrom = desde, validUntil = hasta,
};

static object Sistema(string codigo, string sistema, int meses, int distancia) => new
{
    tipo = "CoveredSystemPublished", revisionCode = codigo, systemCode = sistema,
    ownLimitMonths = meses, ownLimitDistance = distancia, ownLimitUnit = "UnitA",
};

static object Exclusion(string codigo, string sistema, string excluye, string condicion) => new
{
    tipo = "CoverageExclusionPublished", revisionCode = codigo, systemCode = sistema,
    exclusion = excluye, condition = condicion,
};

static object Regla(string codigo, string regla, string conserva) => new
{
    tipo = "ServicingPolicyPublished", revisionCode = codigo,
    rule = regla, whatPreservesCover = conserva,
};

static object Garantia(string reclamo, string observado) => new
{
    tipo = "WarrantyClaimFiled", observedAt = observado, revisionCode = reclamo,
};

static object Cita(string observado) => new
{
    tipo = "AppointmentScheduled", observedAt = observado,
};

static string Texto(JsonElement e, string campo) =>
    e.TryGetProperty(campo, out JsonElement v) && v.ValueKind == JsonValueKind.String ? v.GetString() : "";

static string Numero(JsonElement e, string campo) =>
    e.TryGetProperty(campo, out JsonElement v) && v.ValueKind == JsonValueKind.Number
        ? v.GetRawText()
        : "0";

static string Dia(JsonElement e, string campo) =>
    e.TryGetProperty(campo, out JsonElement v) && v.TryGetDateTime(out DateTime d)
        ? d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
        : "";

static int Entero(string texto) =>
    int.TryParse(texto, NumberStyles.Any, CultureInfo.InvariantCulture, out int valor) ? valor : 0;

static decimal Decimal(string texto) =>
    decimal.TryParse(texto, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal valor)
        ? valor
        : 0m;

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
    Console.WriteLine("  ficha                         publica la ficha del fabricante en dos revisiones");
    Console.WriteLine("                                --revision --revision2 (default MY2025-R1 y -R2)");
    Console.WriteLine("  senuelo                       demuestra el 403 de /customer con el token de la app");
    Console.WriteLine("  inyectar --tipo <tipo> ...    agrega UN hecho concreto");
    Console.WriteLine();
    Console.WriteLine("  tipos que la app consume -- los cinco de la agenda:");
    Console.WriteLine("    OdometerReported           --value --unit --observedAt --source");
    Console.WriteLine("    ConditionThresholdCrossed  --kind --observedAt");
    Console.WriteLine("    OperationCompleted         --kind --completedAt --completionReading --unit");
    Console.WriteLine("    PlanOperationSeeded        --kind --basis --timeLimitDays --distanceLimit --unit");
    Console.WriteLine("                               --isMandatory --isSafety --anchoredAt --anchorReading --anchorUnit");
    Console.WriteLine("    PlanRevisionAdopted        --revisionCode --adoptedAt");
    Console.WriteLine();
    Console.WriteLine("  y los ONCE de la especificacion del modelo, uno por comando de publicacion.");
    Console.WriteLine("  Todos llevan --revisionCode, y ninguno trae una revision entera: una hoja");
    Console.WriteLine("  llega hecho por hecho, y el orden importa -- publicar necesita un borrador");
    Console.WriteLine("  abierto, sellar necesita contenido, y sustituir necesita una sucesora sellada.");
    Console.WriteLine();
    Console.WriteLine("    SpecificationRevisionOpened      --publishedAt");
    Console.WriteLine("    FluidSpecificationPublished      --kind --grade --capacity --capacityUnit");
    Console.WriteLine("                                     --intervalDistance --intervalUnit --intervalDays");
    Console.WriteLine("    TyreSpecificationPublished       --position --size --coldPressure --pressureUnit --ratings");
    Console.WriteLine("    FuelGradePublished               --grade --standing");
    Console.WriteLine("    ServiceIntervalPublished         --kind --intervalDistance --intervalUnit --intervalDays");
    Console.WriteLine("    ServicePricePublished            --kind --milestoneDistance --milestoneUnit");
    Console.WriteLine("                                     --amount --currency --validFrom --validUntil");
    Console.WriteLine("    CoveredSystemPublished           --systemCode --ownLimitMonths --ownLimitDistance --ownLimitUnit");
    Console.WriteLine("    CoverageExclusionPublished       --systemCode --exclusion --condition");
    Console.WriteLine("    ServicingPolicyPublished         --rule --whatPreservesCover");
    Console.WriteLine("    SpecificationRevisionSealed      (solo --revisionCode)");
    Console.WriteLine("    SpecificationRevisionSuperseded  --supersededByCode");
    Console.WriteLine();
    Console.WriteLine("  cualquier otro --tipo se acepta y la app lo DESCARTA, que es lo normal:");
    Console.WriteLine("  el concesionario publica su narrativa entera y este modulo modela una rebanada.");
    Console.WriteLine();
    Console.WriteLine("  ejemplos:");
    Console.WriteLine("    inyectar --tipo OdometerReported --value 31000 --observedAt 2026-10-01 --source Telemetry");
    Console.WriteLine("    inyectar --tipo OperationCompleted --kind FilterChange --completedAt 2026-10-02 --completionReading 31000");
    Console.WriteLine("    inyectar --tipo SpecificationRevisionOpened --revisionCode MY2026-R1 --publishedAt 2026-10-01");
    Console.WriteLine("    inyectar --tipo FluidSpecificationPublished --revisionCode MY2026-R1 --kind BrakeFluid");
    Console.WriteLine("             --grade \"DOT 4\" --capacity 0.5 --intervalDistance 20000 --intervalDays 730");
    Console.WriteLine("    inyectar --tipo SpecificationRevisionSealed --revisionCode MY2026-R1");
    Console.WriteLine();
    Console.WriteLine("  --url      default http://localhost:8092/");
    Console.WriteLine("  --recurso  default vehicle");
    Console.WriteLine("  --id       default 1HGCM82633A004352  (el VIN)");
}
