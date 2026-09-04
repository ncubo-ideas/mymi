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
        case "garantia":  await Garantia(); break;
        case "taller":    await Taller(); break;
        case "costos":    await Costos(); break;
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

        "VehicleDelivered" =>
            $"entregado el {Dia(e, "deliveredAt")} con {Numero(e, "reading")} "
            + $"{Texto(e, "unit")} de odometro",
        "WarrantyEnvelopePublished" =>
            $"sobre de {Numero(e, "months")} meses o {Numero(e, "distance")} "
            + $"{Texto(e, "unit")}, publicado el {Dia(e, "publishedAt")}",

        "ShopEntryRecorded" =>
            $"entro a {Texto(e, "branch")} el {Dia(e, "enteredAt")} por {Texto(e, "subject")}",
        "ShopCollectionRecorded" =>
            $"entregado el {Dia(e, "collectedAt")}, cubrio {Dias(e, "coveredDays")}",
        "ShopStayRecorded" =>
            $"estadia en {Texto(e, "branch")} por {Texto(e, "subject")}, del "
            + $"{Dia(e, "enteredAt")} al {Dia(e, "collectedAt")}, cubrio {Dias(e, "coveredDays")}",

        "RecurringCostRecorded" =>
            $"gasto recurrente de {Numero(e, "amount")} {Texto(e, "currency")} en "
            + $"{Texto(e, "subject")}, incurrido el {Dia(e, "incurredAt")}",
        "DurableCostRecorded" =>
            $"gasto duradero de {Numero(e, "amount")} {Texto(e, "currency")} en "
            + $"{Texto(e, "subject")}, incurrido el {Dia(e, "incurredAt")}, con "
            + $"{Numero(e, "expectedLifeMonths")} meses de vida util",

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

        deliveredAt = Arg(args, "--deliveredAt", "2026-09-01"),
        reading = Entero(Arg(args, "--reading", "0")),
        months = Entero(Arg(args, "--months", "0")),
        distance = Entero(Arg(args, "--distance", "0")),

        branch = Arg(args, "--branch", ""),
        subject = Arg(args, "--subject", ""),
        enteredAt = Arg(args, "--enteredAt", "2026-09-01"),
        collectedAt = Arg(args, "--collectedAt", "2026-09-01"),
        coveredDays = ListaDeDias(Arg(args, "--coveredDays", "")),

        incurredAt = Arg(args, "--incurredAt", "2026-09-01"),
        expectedLifeMonths = Entero(Arg(args, "--expectedLifeMonths", "0")),
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
        (Reclamo("W-9912", "2025-11-15"),
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
    bool sustituye = !args.Contains("--sin-sustituir");

    Console.WriteLine($"publicando la ficha del fabricante en {recurso}/{id}...");
    Console.WriteLine($"  revisiones: {primera} y {segunda}");
    Console.WriteLine(sustituye
        ? $"  y al final SUSTITUYE {primera}: despues de eso solo contesta el aceite"
        : $"  SIN sustituir: {primera} sigue contestando, asi que la hoja queda entera");
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

    foreach ((object cuerpo, string porque) in sustituye ? guion : guion[..^1])
    {
        using var r = await http.PostAsJsonAsync($"{recurso}/{id}/events/hecho", cuerpo);
        Console.WriteLine($"  HTTP {(int)r.StatusCode}   {porque}");
    }

    Console.WriteLine();
    if (!sustituye)
    {
        Console.WriteLine("NO se posteo la sustitucion. Una revision que corrige un item no borra los");
        Console.WriteLine($"que no republico, asi que el aceite sale de {segunda} y todo lo demas sigue");
        Console.WriteLine($"saliendo de {primera} -- que es lo que la columna de revision esta ahi para");
        Console.WriteLine("mostrar. Con la sustitucion, en cambio, se retira lo que la sucesora no");
        Console.WriteLine("volvio a publicar y la hoja queda en una sola linea.");
        Console.WriteLine();
    }
    Console.WriteLine("UN CODIGO DE REVISION SE TOMA UNA SOLA VEZ Y PARA SIEMPRE. Correr esto dos");
    Console.WriteLine("veces contra el mismo journal hace que el segundo OpenRevision se rechace:");
    Console.WriteLine("la app pierde ese hecho, sigue con el siguiente, y deja una marca. Para una");
    Console.WriteLine("segunda corrida hay que pasar --revision y --revision2 con codigos nuevos.");
}

async Task Garantia()
{
    const string unidadDeLaNarrativa = "UnitA";
    string unidad = Arg(args, "--unit", "UnitB");

    Console.WriteLine($"publicando la entrega y el sobre de garantia en {recurso}/{id}...");
    Console.WriteLine($"  todo en {unidad}, que es la unidad en que la agencia leyo este carro");
    Console.WriteLine();

    (object cuerpo, string porque)[] guion =
    {
        (Entrega("2025-03-01", 120, unidad),
            "la ENTREGA: cuando se entrego y en que odometro estaba ese dia. Es el ancla desde "
            + "donde se miden las dos mitades del sobre"),

        (Sobre(36, 60000, "2025-03-01", unidad),
            "el SOBRE: 36 meses o 60000 de distancia, publicado el dia de la entrega"),

        (LecturaEn(6000, "2025-10-01", unidad),
            "una lectura del odometro EN LA MISMA UNIDAD que las dos de arriba, que es lo que hace "
            + "falta para que la mitad de distancia se pueda medir"),

        (Sobre(48, 80000, "2026-11-01", unidad),
            "y el sobre RE-PUBLICADO, mas largo: rige desde su instante de publicacion hacia "
            + "adelante, y la frase nombra cual de los dos contesto"),

        (Sobre(24, 40000, "2024-01-01", unidad),
            "un sobre publicado ANTES que el que rige, llegando DESPUES: entra y NO gobierna. El "
            + "orden de llegada no es el de vigencia"),

        (Entrega("2025-02-15", 100, unidad),
            "y la entrega CORREGIDA: gana la ultima que llego, aunque sea la mas vieja de las dos. "
            + "Un hecho historico tiene una sola version"),
    };

    foreach ((object cuerpo, string porque) in guion)
    {
        using var r = await http.PostAsJsonAsync($"{recurso}/{id}/events/hecho", cuerpo);
        Console.WriteLine($"  HTTP {(int)r.StatusCode}   {porque}");
    }

    Console.WriteLine();
    Console.WriteLine("LOS DOS DESORDENES ESTAN A PROPOSITO Y NO SE ORDENAN IGUAL. El sobre aplica por");
    Console.WriteLine("instante de PUBLICACION, asi que el de 2024 entra y no desplaza a nada; la");
    Console.WriteLine("entrega es CORRECTIVA, asi que la del 15 de febrero si reemplaza a la del 1 de");
    Console.WriteLine("marzo. Dos ordenamientos sobre un solo cable, y los dos son correctos.");
    Console.WriteLine();
    Console.WriteLine("ESTO SE PUEDE VOLVER A CORRER contra el mismo journal: ninguno de los dos hechos");
    Console.WriteLine("toma una clave que no se pueda volver a tomar, asi que no hay nada que se gaste.");
    Console.WriteLine("Lo unico que hace falta es que los numeros de evento sigan avanzando, y de eso se");
    Console.WriteLine("encarga el mock.");
    Console.WriteLine();
    if (unidad == unidadDeLaNarrativa)
    {
        Console.WriteLine("Y SOBRE LA NARRATIVA QUE EL MOCK YA TRAE, LAS TRES UNIDADES CALZAN: la del limite");
        Console.WriteLine("del sobre, la de la lectura del dia de la entrega y la del odometro de hoy, las tres");
        Console.WriteLine($"en {unidad}. Asi que la respuesta contesta las DOS mitades, el plazo y la distancia,");
        Console.WriteLine("y no queda nada suspendido.");
        Console.WriteLine();
        Console.WriteLine("SON TRES Y NO DOS, y por eso este guion las manda todas juntas. Corrido en la otra");
        Console.WriteLine("unidad la respuesta contesta el plazo, dice con palabras que la distancia no se pudo");
        Console.WriteLine("medir y nombra las dos que no calzaron; cambiar solo la del sobre mueve el desajuste");
        Console.WriteLine("de una pareja a la otra en vez de resolverlo. Para verlo:");
        Console.WriteLine("  garantia --unit UnitB");
    }
    else
    {
        Console.WriteLine("Y SOBRE LA NARRATIVA QUE EL MOCK YA TRAE, LA MITAD DE DISTANCIA QUEDA SUSPENDIDA,");
        Console.WriteLine($"que es la ley y no un defecto. Esa narrativa mide en {unidadDeLaNarrativa}, este guion");
        Console.WriteLine($"en {unidad}, y las unidades no se convierten -- asi que la respuesta contesta el plazo,");
        Console.WriteLine("dice con palabras que la distancia no se pudo medir, y nombra las dos unidades que no");
        Console.WriteLine("calzaron.");
        Console.WriteLine();
        Console.WriteLine("SON TRES UNIDADES LAS QUE TIENEN QUE CALZAR, no dos: la del limite del sobre, la");
        Console.WriteLine("de la lectura del dia de la entrega, y la del odometro de hoy. Cambiar solo la del");
        Console.WriteLine("sobre mueve el desajuste de una pareja a la otra en vez de resolverlo. Para verlo");
        Console.WriteLine("entero hay que correr TODO este guion en la unidad de la narrativa:");
        Console.WriteLine($"  garantia --unit {unidadDeLaNarrativa}");
    }
}

async Task Taller()
{
    bool dejaUnaAbierta = !args.Contains("--sin-abrir");

    Console.WriteLine($"publicando las estadias en el taller de {recurso}/{id}...");
    Console.WriteLine(dejaUnaAbierta
        ? "  y la ultima estadia queda ABIERTA: el carro esta adentro y todavia no lo entregan"
        : "  SIN dejar ninguna abierta, que es lo que hace este guion re-corrible");
    Console.WriteLine();

    (object cuerpo, string porque)[] guion =
    {
        (EstadiaCompleta("Taller Central", "Cambio de frenos",
                "2026-05-11T08:30:00Z", "2026-05-13T16:45:00Z",
                ["2026-05-11", "2026-05-12", "2026-05-13"]),
            "una estadia entera DE UN SOLO ACTO: los dos extremos y los TRES dias que cubrio, "
            + "todo en un hecho. Es la forma correctiva, para lo que se registra despues"),

        (EntradaAlTaller("Taller Escazu", "Revision de los 20 mil", "2026-07-06T07:15:00Z"),
            "la ENTRADA: el carro entro y desde aca la estadia queda abierta. Es lo unico que "
            + "las dos lecturas por instante alcanzan"),
        (EntregaDelTaller("2026-07-07T17:00:00Z", ["2026-07-06", "2026-07-07"]),
            "y la ENTREGA, que la cierra con los DOS dias que la sucursal dice que cubrio. Los "
            + "dias los DICE la sucursal: no salen de los dos instantes"),
    };

    foreach ((object cuerpo, string porque) in guion)
    {
        using var r = await http.PostAsJsonAsync($"{recurso}/{id}/events/hecho", cuerpo);
        Console.WriteLine($"  HTTP {(int)r.StatusCode}   {porque}");
    }

    if (dejaUnaAbierta)
    {
        using var r = await http.PostAsJsonAsync($"{recurso}/{id}/events/hecho",
            EntradaAlTaller("Taller Curridabat", "Ruido en la suspension", "2026-10-05T09:00:00Z"));
        Console.WriteLine($"  HTTP {(int)r.StatusCode}   "
            + "una SEGUNDA entrada que nadie cierra: el carro queda adentro");
    }

    Console.WriteLine();
    Console.WriteLine("LA RESPUESTA SON LOS DIAS Y NO UNA DURACION. Cinco dias en dos estadias, y los");
    Console.WriteLine("cinco se pueden nombrar uno por uno: 11, 12 y 13 de mayo, 6 y 7 de julio. Una");
    Console.WriteLine("duracion no se puede cruzar con el calendario de nadie; una lista de fechas si.");
    Console.WriteLine();
    Console.WriteLine("LOS DIAS LLEGAN CON EL HECHO Y NO SE CALCULAN. Entro el 11 a las 08:30 y salio el");
    Console.WriteLine("13 a las 16:45, y los dias que cubrio son tres porque la sucursal dijo tres.");
    Console.WriteLine("Sacarlos de los dos instantes pediria una zona horaria que este registro no");
    Console.WriteLine("lleva, y con la zona equivocada el 11 podria salir siendo el 10.");
    Console.WriteLine();
    if (dejaUnaAbierta)
    {
        Console.WriteLine("LA ULTIMA QUEDO ABIERTA A PROPOSITO, y es el unico estado que separa las dos");
        Console.WriteLine("lecturas por INSTANTE de las seis por DIAS: una estadia abierta todavia no tiene");
        Console.WriteLine("ningun dia -- los dias llegan con la entrega -- asi que ningun periodo dicho en");
        Console.WriteLine("dias la alcanza, y preguntar si el carro esta adentro AHORA si.");
        Console.WriteLine();
        Console.WriteLine("Y POR ESO ESTO NO SE PUEDE VOLVER A CORRER TAL CUAL: un carro no esta en dos");
        Console.WriteLine("sucursales a la vez, asi que la primera entrada de una segunda corrida se rechaza");
        Console.WriteLine("mientras la de octubre siga abierta, y ese rechazo marca el journal. Para una");
        Console.WriteLine("segunda corrida:");
        Console.WriteLine("  taller --sin-abrir");
    }
    else
    {
        Console.WriteLine("NO SE DEJO NINGUNA ABIERTA, asi que esto SI se puede volver a correr contra el");
        Console.WriteLine("mismo journal: ninguno de los tres hechos toma una clave que se gaste, y sin una");
        Console.WriteLine("estadia abierta la entrada de la corrida siguiente no choca con nada. Lo que se");
        Console.WriteLine("pierde es la estadia abierta, que es lo unico que las dos lecturas por instante");
        Console.WriteLine("contestan distinto de las seis por dias.");
    }
}

async Task Costos()
{
    Console.WriteLine($"publicando los gastos de {recurso}/{id}...");
    Console.WriteLine("  UNA duradera y DOS recurrentes, en DOS monedas, y en un orden que NO es el");
    Console.WriteLine("  orden en que se incurrieron");
    Console.WriteLine();

    (object cuerpo, string porque)[] guion =
    {
        (GastoDuradero("2026-03-12T09:00:00Z", 240000.00m, "CRC", "Llantas nuevas", 48),
            "un gasto DURADERO: sigue dando valor despues del periodo en que se pago, y la vida "
            + "util en meses la dijo quien lo vendio. Es el que hace que las dos vistas difieran"),

        (GastoRecurrente("2026-08-05T14:15:00Z", 120.50m, "USD", "Lavado premium"),
            "un gasto RECURRENTE en OTRA moneda, y en AGOSTO: llega antes que el de mayo a "
            + "proposito, y las filas salen igual en orden del instante en que se incurrieron"),

        (GastoRecurrente("2026-05-20T10:30:00Z", 45000.00m, "CRC", "Cambio de aceite"),
            "y otro RECURRENTE, de mayo: se consume en su propio periodo y no se reparte"),
    };

    foreach ((object cuerpo, string porque) in guion)
    {
        using var r = await http.PostAsJsonAsync($"{recurso}/{id}/events/hecho", cuerpo);
        Console.WriteLine($"  HTTP {(int)r.StatusCode}   {porque}");
    }

    Console.WriteLine();
    Console.WriteLine("LA RESPUESTA SON LAS DOS VISTAS EN UNA SOLA FRASE. Lo que salio del banco en el");
    Console.WriteLine("periodo y lo que el periodo consumio, siempre juntas: sin separar la duradera de");
    Console.WriteLine("las recurrentes, un total dice 'el carro se puso caro' cuando la verdad es");
    Console.WriteLine("'compro llantas'. Ninguna lectura contesta una vista sola.");
    Console.WriteLine();
    Console.WriteLine("Y LAS DOS MONEDAS SE DICEN CADA UNA POR SU LADO. No hay total entre ellas y no");
    Console.WriteLine("hay factor con que sumarlas: reconvertir un historico es falsear el pasado.");
    Console.WriteLine();
    Console.WriteLine("ESTO SE PUEDE VOLVER A CORRER contra el mismo journal, SIN NINGUNA BANDERA, y es");
    Console.WriteLine("la diferencia con 'taller' y con 'ficha'. Una entrada del libro no tiene clave:");
    Console.WriteLine("dos gastos del mismo monto en el mismo asunto y el mismo dia son DOS gastos, no");
    Console.WriteLine("una correccion, asi que no hay nada que se gaste ni ningun estado con que");
    Console.WriteLine("chocar. Correrlo dos veces deja seis entradas, que es lo correcto.");
    Console.WriteLine();
    Console.WriteLine("UNA MONEDA QUE ESTE MODULO NO NOMBRA SE DESCARTA EN EL BORDE y no deja marca.");
    Console.WriteLine("Para verlo:");
    Console.WriteLine("  inyectar --tipo RecurringCostRecorded --currency XYZ --amount 1000");
    Console.WriteLine("           --incurredAt 2026-05-20T10:30:00Z --subject Prueba");
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

static object LecturaEn(int valor, string observado, string unidad) => new
{
    tipo = "OdometerReported", value = valor, unit = unidad, observedAt = observado,
    source = "Telemetry",
};

static object Entrega(string entregado, int lectura, string unidad) => new
{
    tipo = "VehicleDelivered", deliveredAt = entregado, reading = lectura, unit = unidad,
};

static object Sobre(int meses, int distancia, string publicado, string unidad) => new
{
    tipo = "WarrantyEnvelopePublished", months = meses, distance = distancia, unit = unidad,
    publishedAt = publicado,
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

static object EntradaAlTaller(string sucursal, string asunto, string entro) => new
{
    tipo = "ShopEntryRecorded", branch = sucursal, subject = asunto, enteredAt = entro,
};

static object EntregaDelTaller(string entregado, string[] dias) => new
{
    tipo = "ShopCollectionRecorded", collectedAt = entregado, coveredDays = dias,
};

static object EstadiaCompleta(
    string sucursal, string asunto, string entro, string entregado, string[] dias) => new
{
    tipo = "ShopStayRecorded", branch = sucursal, subject = asunto,
    enteredAt = entro, collectedAt = entregado, coveredDays = dias,
};

/// <summary>Un gasto que se consume en el periodo en que se incurrio. No lleva vida util.</summary>
static object GastoRecurrente(string incurrido, decimal monto, string moneda, string asunto) => new
{
    tipo = "RecurringCostRecorded", incurredAt = incurrido,
    amount = monto, currency = moneda, subject = asunto,
};

/// <summary>Un gasto que sigue dando valor despues, con su vida util en meses.</summary>
static object GastoDuradero(
    string incurrido, decimal monto, string moneda, string asunto, int vidaUtilEnMeses) => new
{
    tipo = "DurableCostRecorded", incurredAt = incurrido,
    amount = monto, currency = moneda, subject = asunto, expectedLifeMonths = vidaUtilEnMeses,
};

static object Reclamo(string reclamo, string observado) => new
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

// Los dias de una estadia llegan como LISTA y se imprimen como llegaron. No se cuentan ni se
// resumen: la respuesta de esta rebanada son los dias, no cuantos son.
static string Dias(JsonElement e, string campo)
{
    if (!e.TryGetProperty(campo, out JsonElement v) || v.ValueKind != JsonValueKind.Array)
        return "";

    List<string> dichos = [];

    foreach (JsonElement dia in v.EnumerateArray())
        if (dia.ValueKind == JsonValueKind.String) dichos.Add(dia.GetString());

    return string.Join(" ", dichos);
}

// Los dias que se le pasan a `inyectar`, separados por coma en la linea de comandos. Vacio es una
// lista vacia y no una lista con una cadena vacia adentro: un dia en blanco no es un dia.
static string[] ListaDeDias(string texto) =>
    texto.Length == 0
        ? []
        : texto.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

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
    Console.WriteLine("                                --sin-sustituir salta la sustitucion final, y esa");
    Console.WriteLine("                                es la unica forma de ver la hoja entera en pantalla");
    Console.WriteLine("  garantia                      publica la entrega y el sobre de garantia de este");
    Console.WriteLine("                                vehiculo, con los DOS desordenes a proposito: un sobre");
    Console.WriteLine("                                viejo que llega tarde no gobierna, y una entrega vieja");
    Console.WriteLine("                                que llega tarde SI reemplaza. Se puede repetir");
    Console.WriteLine("                                --unit (default UnitB) manda las TRES unidades del");
    Console.WriteLine("                                guion; con UnitA calza con la narrativa del mock y la");
    Console.WriteLine("                                mitad de distancia se puede medir");
    Console.WriteLine("  taller                        publica las estadias en el taller de este vehiculo:");
    Console.WriteLine("                                una entera de un solo acto, y una entrada con su");
    Console.WriteLine("                                entrega. La respuesta son los DIAS y no una duracion");
    Console.WriteLine("                                --sin-abrir salta la ultima entrada; sin esa bandera");
    Console.WriteLine("                                queda una estadia ABIERTA y no se puede repetir");
    Console.WriteLine("  costos                        publica los gastos de este vehiculo: una duradera con");
    Console.WriteLine("                                su vida util y dos recurrentes, en dos monedas. Se");
    Console.WriteLine("                                puede repetir SIN ninguna bandera -- una entrada del");
    Console.WriteLine("                                libro no tiene clave que se gaste");
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
    Console.WriteLine("  y los DOS del perfil de ESTE vehiculo, uno por comando de la agencia. No llevan");
    Console.WriteLine("  --revisionCode: no son del catalogo del modelo, son de este carro. Ninguno de los");
    Console.WriteLine("  dos toma una clave que se gaste, asi que los dos se pueden repetir.");
    Console.WriteLine();
    Console.WriteLine("    VehicleDelivered                 --deliveredAt --reading --unit");
    Console.WriteLine("    WarrantyEnvelopePublished        --months --distance --unit --publishedAt");
    Console.WriteLine();
    Console.WriteLine("  y los TRES de las estadias en el taller, uno por comando de la sucursal. Los");
    Console.WriteLine("  dias van SEPARADOS POR COMA y cada uno en yyyy-MM-dd: los dice la sucursal, no");
    Console.WriteLine("  salen de los dos instantes. Una entrada deja la estadia ABIERTA hasta que llegue");
    Console.WriteLine("  su entrega, y un carro no esta en dos sucursales a la vez.");
    Console.WriteLine();
    Console.WriteLine("    ShopEntryRecorded                --branch --subject --enteredAt");
    Console.WriteLine("    ShopCollectionRecorded           --collectedAt --coveredDays");
    Console.WriteLine("    ShopStayRecorded                 --branch --subject --enteredAt");
    Console.WriteLine("                                     --collectedAt --coveredDays");
    Console.WriteLine();
    Console.WriteLine("  y los DOS del libro de costos, uno por comando de la sucursal. Ninguno de los dos");
    Console.WriteLine("  trae la clasificacion: la dice CUAL de los dos se manda, y solo el duradero lleva");
    Console.WriteLine("  --expectedLifeMonths. Una moneda que este modulo no nombra se DESCARTA en el borde");
    Console.WriteLine("  y no deja marca; las que hay son CRC y USD. Los dos se pueden repetir.");
    Console.WriteLine();
    Console.WriteLine("    RecurringCostRecorded            --incurredAt --amount --currency --subject");
    Console.WriteLine("    DurableCostRecorded              --incurredAt --amount --currency --subject");
    Console.WriteLine("                                     --expectedLifeMonths");
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
    Console.WriteLine("    inyectar --tipo VehicleDelivered --deliveredAt 2025-03-01 --reading 120 --unit UnitB");
    Console.WriteLine("    inyectar --tipo WarrantyEnvelopePublished --months 36 --distance 60000");
    Console.WriteLine("             --unit UnitB --publishedAt 2025-03-01");
    Console.WriteLine("    inyectar --tipo ShopEntryRecorded --branch \"Taller Central\" --subject Frenos");
    Console.WriteLine("             --enteredAt 2026-05-11T08:30:00Z");
    Console.WriteLine("    inyectar --tipo ShopCollectionRecorded --collectedAt 2026-05-13T16:45:00Z");
    Console.WriteLine("             --coveredDays 2026-05-11,2026-05-12,2026-05-13");
    Console.WriteLine("    inyectar --tipo RecurringCostRecorded --incurredAt 2026-05-20T10:30:00Z");
    Console.WriteLine("             --amount 45000.00 --currency CRC --subject \"Cambio de aceite\"");
    Console.WriteLine("    inyectar --tipo DurableCostRecorded --incurredAt 2026-03-12T09:00:00Z");
    Console.WriteLine("             --amount 240000.00 --currency CRC --subject \"Llantas nuevas\"");
    Console.WriteLine("             --expectedLifeMonths 48");
    Console.WriteLine();
    Console.WriteLine("  --url      default http://localhost:8092/");
    Console.WriteLine("  --recurso  default vehicle");
    Console.WriteLine("  --id       default 1HGCM82633A004352  (el VIN)");
}
