# Capítulo 3 — Construir la app

> Capítulo 3 de las [guías](README.md). El 0 dice dónde corre todo; el 4, qué modelar; éste, cómo se escribe.

Receta de trabajo, derivada de construir una app completa de punta a punta: dominio puro, API REST y
CLI, sobre un journal de file system.

**Qué es esta guía y qué no.** No reemplaza a las guías de Puppeteer (`basics.md`, `parameters.md`,
`control-flow.md`, `conventions.md`, `output-and-controller.md`, `hosting-and-environments.md`) ni a
la guía de dominio (`domain-guide.md`). Aquéllas dicen **cómo funciona el framework**; ésta dice **en
qué orden armar una aplicación y con qué forma**, y —lo más importante— recoge las reglas del
substrato que **no están escritas en ninguna guía** y que sólo se aprenden chocando con ellas. La
sección §7 es el corazón del documento: si vas con prisa, leé esa.

El ejemplo recurrente es un presupuesto familiar. Cada vez que aparece, es **ilustración, no
prescripción**: generalizá la forma, no los nombres.

---

## 1. La forma de la solución

Cuatro proyectos de producción y uno de pruebas por cada uno que valga la pena probar.

```
<Repo>.sln
Directory.Build.props                 ← dónde vive Puppeteer

src/
  <App>.Domain/                       ← el modelo puro. NO referencia nada.
  <App>.Hosting/                      ← el puppet: qué actor, qué journal, el seed
  <App>.Api/                          ← una carátula (HTTP)
  <App>.Cli/                          ← otra carátula (consola)

tests/
  <App>.Domain.Tests/                 ← el modelo, sin substrato
  <App>.Api.Tests/                    ← cruzando el perform layer, journal real
  <App>.Cli.Tests/                    ← la herramienta como la ve quien la invoca
```

**Por qué esos cortes, y no otros.**

| Proyecto | Referencia a | Razón |
|---|---|---|
| `Domain` | **nada** | El modelo es el activo durable. Si referencia el host, muere con él. |
| `Hosting` | Puppeteer + Domain | El wiring es idéntico para toda carátula: mismo actor, mismo journal, mismo seed. |
| `Api` | Hosting | Sólo sabe de HTTP y códigos de estado. |
| `Cli` | Hosting | Sólo sabe de argv, stdout/stderr y códigos de salida. |

El error a evitar: que el CLI referencie al proyecto web para reusar el puppet. Eso arrastra ASP.NET
a una consola y acopla dos carátulas que no se conocen. Extraé `Hosting` **antes** de escribir la
segunda carátula, no después.

---

## 2. El orden de trabajo

El orden importa: cada fase deja verificado lo que la siguiente asume.

1. **El dominio, con sus tests.** Sin framework, sin journal, sin host. Corre en milisegundos.
2. **El hosting.** El puppet abre, hidrata y siembra. Verificalo con un spike desechable.
3. **La primera carátula.** Con tests que cruzan el perform layer contra un journal de verdad.
4. **La segunda carátula.** Si la primera dejó el wiring en su lugar, ésta es sólo entrada y salida.

No saltes la 1. Un dominio que se escribe al mismo tiempo que el host termina contaminado de
decisiones del host —fechas del reloj, formatos, DTOs— que después no se pueden sacar.

---

## 3. Fase 1 — el dominio puro

Esto lo gobierna `domain-guide.md`. Lo que hay que retener al empezar:

- **Todo `internal`.** El perform llega por reflexión; no necesita más. El host jamás nombra un tipo
  del dominio.
- **Entidades con identidad, nunca value objects.** Sin `Equals`/`GetHashCode`/`record`, sin
  colecciones llaveadas por valor. Llaveá por primitivo (un folio, un ordinal de mes).
- **La operación es lo primario, no el estado.** `presupuesto.PuedeLiquidar(folio, moneda)`, no
  `presupuesto.Planes[folio].Moneda`.
- **Los conjuntos cerrados son tipos cerrados**, comparados por referencia, con un resolutor total
  desde primitivo en el borde (`Recurrence.Named("Annual")` revienta ante lo desconocido).
- **Nada de reloj ni azar adentro.** La fecha, la tasa de cambio, el número aleatorio: **se reciben**
  como parámetro. Es lo que hace que replayar el journal dé exactamente el mismo estado.
- **Nombrá la ley.** No dejes que una regla viva sólo como aritmética. La provisión mensual de un
  gasto anual es una ley con nombre, no un `/12` suelto.

### La fachada es lo que el perform bindea

Su forma la decide el substrato, no el gusto:

- **Entra primitivo**: `string`, `decimal`, `int`, `DateTime`, y arreglos de ésos. Nunca un objeto del
  dominio, porque el perform sólo transporta primitivos.
- **Sale primitivo o proyección**: un escalar, un `int[]`, o un texto renderizado. Nunca un carrier
  del dominio: el perform no lo puede traer de vuelta.
- **El constructor ES la creación.** No hay una operación `NuevoX()` aparte.
- **Verbos del negocio, no toqueteo de la estructura.** `Liquidar(folio, monto)`, no
  `MarcarCelda(i, j)`.

### Predicados totales para poder preguntar antes

Por cada operación que el dominio pueda rechazar, expone un **predicado total** que responde sí o no
sin reventar:

```csharp
internal bool HandlesCurrency(string code);
internal bool CanSettle(int folio, string currency);
```

Esto parece un detalle y es lo contrario. Es lo que permite el `Check` de §7.3, sin el cual un
comando rechazado envenena el journal para siempre.

---

## 4. Fase 2 — el hosting

Dos tipos, y ninguno más.

**La configuración del journal** — un POCO: ruta, moneda base, tamaño de archivo, compresión, y el
método que arma el connection string.

**El puppet** — una subclase de `PerformanceV2`:

```csharp
public sealed class AppPuppet : PerformanceV2
{
    public const string Role = "<Actor>";
    private readonly AppJournal _journal;

    public AppPuppet(AppJournal journal)
        : base(Role, Assembly.Load(new AssemblyName("<App>.Domain")))
    {
        _journal = journal;      // asignado ANTES de Start(), porque OnHydrated lo lee
    }

    public AppPuppet Open()
    {
        Directory.CreateDirectory(_journal.Path);
        ConfigureStorage(DatabaseType.FileSystem, _journal.ConnectionString());
        Start();                 // hidrata: replaya lo que hay y aplica upgrades nuevos
        return this;
    }

    protected override void OnHydrated()
    {
        Actor.Using(@"
	upgrade('open-<app>') {
		root = <AggregateRoot>(@seedParam);
	}
")
        .WithParameters(p => { p["seedParam", typeof(string)] = _journal.SeedValue; })
        .PerformCommand();
    }
}
```

Cuatro cosas que hay que entender de ese bloque:

1. **`Assembly.Load` por nombre**, no `typeof(X).Assembly`. Nombrar un tipo obligaría a hacerlo
   público y a que el host lo vea. Cargando por nombre, el dominio entero queda `internal`.
2. **El seed va en un `upgrade`**, no en un `if (esNuevo)`. El `upgrade` se aplica una vez por actor;
   agregar una liberación nueva después es agregar otro `upgrade('nombre_v2')`, y se aplica solo.
3. **El agregado se construye DENTRO de un comando**, como global raíz. Un campo de C# no se
   journaliza y vuelve vacío al siguiente arranque.
4. **`Open()` y no el constructor.** `Start()` dispara `OnHydrated`, que es virtual; llamarlo desde el
   constructor sería usar un objeto a medio construir.

**Verificá esta fase con un spike desechable** antes de escribir carátula alguna: abrir, sembrar,
un comando, una consulta, cerrar, reabrir y comprobar que el dato sigue ahí. Media hora que ahorra
un día.

---

## 5. Fase 3 — una carátula HTTP

La regla: **el actor se construye una vez y se reusa**. Singleton de DI, abierto al arrancar para que
ninguna petición corra contra la hidratación.

Cada endpoint son siempre los mismos tres pasos:

```csharp
[HttpPost("salary")]
public IActionResult RecordSalary([FromBody] SalaryPayment payment)
{
    // 1. guardas de C#: forma, nulos, rangos. Antes de tocar el actor.
    if (payment.Amount <= 0m) return BadRequest("amount must be positive");

    // 2. los primitivos viajan como parámetros del perform
    string result = _budget.Actor.Using(@"
	{
		folio = budget.RecordIncome(@source, @note, @currency, @amount, @paidOn);
		print folio 'folio';
	}
")
    .WithParameters(p =>
    {
        p["source",   typeof(string)]   = "Salary";
        p["amount",   typeof(decimal)]  = payment.Amount;
        p["paidOn",   typeof(DateTime)] = payment.PaidOn;
    })
    .PerformCommand();

    // 3. lo que el print produjo YA es el JSON. Se devuelve tal cual.
    return Content(result, "application/json");
}
```

- **Nombrá la acción por intención del negocio**, no por el verbo HTTP: `RecordSalary`, no `Post`.
- **Nada de DTO de salida ni re-serialización.** El `print` ya hizo el trabajo.
- **Los valores de ruta y cuerpo son parámetros del perform**, no parte del nombre del actor.
- **Un filtro traduce el rechazo del dominio a 400.** El tipo de excepción del dominio es `internal`
  —el host no debe nombrarlo—, así que se reconoce por la excepción del framework que lo envuelve.

---

## 6. Fase 4 — una carátula de consola

Si el `Hosting` quedó bien, esto es entrada, salida y códigos de salida.

**La convención de la casa es AI-first.** Sin argumentos, el programa no imprime ayuda: imprime un
**landing que ramifica por audiencia**.

```
Si sos Claude, otro LLM o cualquier agente operando esta herramienta:
  -> corré `<app> describe` primero.
Si sos una persona:
  -> corré `<app> help`.
```

**Una sola fuente de verdad para la superficie.** Declarás los comandos y sus parámetros una vez
—nombre, obligatorio u opcional, tipo, default, valores legales— y de ahí se renderizan **las dos**
salidas: `help` en prosa y `describe` en formato máquina. Así un comando no puede existir sin
documentarse, ni su documentación desviarse de lo que acepta.

**El `describe` debe leer los valores legales del modelo vivo**, no de una lista escrita a mano. Si
la familia inventó una categoría, aparece. Es la diferencia entre un agente que acierta a la primera
y uno que descubre la herramienta a fuerza de errores.

**Forma plana para lo que va a parsear una máquina.** Dos colecciones —`commands` y `parameters`—
unidas por el nombre del comando, en vez de parámetros anidados. Sobrevive a cualquier formatter y a
cualquier parser.

**Códigos de salida con significado**, y documentados en el propio `describe`:

| | |
|---|---|
| 0 | hecho |
| 1 | la línea de comandos estaba mal (nunca llegó al modelo) |
| 2 | el modelo lo rechazó (no se escribió nada) |
| 3 | el journal no se pudo abrir |

---

## 7. Las reglas del substrato que no están en ninguna guía

Esta sección es el motivo de existir del documento. Todo lo de acá se descubrió a los golpes.

### 7.1 Un comando rechazado se journaliza igual — y envenena el replay

**El peor de todos.** Si el dominio revienta a mitad de un `PerformCommand`, la entrada **ya se
escribió**. Desde ese momento, cada rehidratación la replaya y vuelve a fallar:

```
[Puppeteer ERROR] Script execution failed at write-time. EntryId=33   ← se escribió
[Puppeteer ERROR] Rehydration execution failed. EntryId=33            ← y falla en cada arranque
```

Un error de tipeo en una moneda queda permanente en el journal.

**La solución** está en `control-flow.md` §9 y hay que aplicarla a **todo comando que el dominio
pueda rechazar**: guardarlo con un `Check` y usar `PerformCheckThenCommand`. Un `Check` que falla
corre bajo read lock, **antes** del write lock, y no escribe nada.

```csharp
actor.Using(
	check: @"
	Check(root.HandlesCurrency(@currency)) Error 'moneda desconocida';
	Check(root.CanSettle(@folio, @currency)) Error 'no hay plan esperando';
",
	@"
	{
		root.Settle(@folio, @currency, @amount);
		print @folio 'settled';
	}
")
.WithParameters(...)
.PerformCheckThenCommand();
```

Por eso el dominio necesita **predicados totales** (§3): el `Check` sólo puede preguntar lo que el
dominio sabe responder sin reventar.

### 7.2 El contrato de retorno de `PerformCheckThenCommand`

Medido, no supuesto:

| | retorno | entradas del journal |
|---|---|---|
| **rechazado** | `{"EWI":[{"Error":"…"}]}` (JSON) · `EWI:\n  - Error: "…"` (TOON) | **no avanzan** |
| **aceptado** | lo que el `print` produjo | avanzan |

El marcador es que el documento **empieza con `EWI`**. No alcanza con "retorno no vacío", porque un
comando aceptado que imprime algo también devuelve texto.

### 7.3 El dominio se resuelve por el assembly del actor

`ActorHandler` hace `actor.GetType().Assembly` para saber dónde buscar el vocabulario. Consecuencias:

- **Pasá el assembly explícitamente**: `new PerformanceV2(name, domainAssembly)`.
- El constructor de un solo argumento usa `Assembly.GetCallingAssembly()`, que desde el host es **el
  host**, no el dominio. Silencioso y equivocado.
- Cargalo por nombre (`Assembly.Load`) para no tener que hacer público ningún tipo del dominio.

### 7.4 El seed cuesta una entrada por arranque

`OnHydrated` corre en cada hidratación y re-emite el `upgrade`. El cuerpo **no** se re-ejecuta, pero
el comando **sí se journaliza**. Medido:

| operación | entryId |
|---|---|
| `Start()` con journal vacío | 0 |
| seed `upgrade` (forma nueva) | 2 — `define action` + `invoke` |
| seed `upgrade` otra vez (no-op) | 3 — **+1**, misma forma → sólo `invoke` |
| un comando nuevo | 5 |
| una consulta | 5 — **+0** |

Para un servicio que arranca una vez, es nada. **Para un CLI invocado cincuenta veces al día, son
cincuenta entradas.** Es el precio del idiom que prescribe la guía —y la alternativa
(`OnFirstHydration`) rompe el versionado de upgrades. Sabelo y decidí.

### 7.5 El framework escribe en stdout

`ActorHandler` hace `Console.WriteLine("Starting …")` y marcas de progreso `1%2%` **sin newline**, que
prefijan la línea siguiente. En un CLI cuya salida alguien va a parsear, eso la arruina.

**En una consola: capturá `Console.Out` mientras corre el framework y reenvialo a stderr.** stdout
lleva sólo la respuesta.

### 7.6 El formatter

- En un `PerformanceV2`, `.Formatter(new ToonFormatter())`.
- En un `ActorV2` pelado —que es por donde van los performs con parámetros— **no hay `.Formatter`**:
  se empuja por contexto, `using (FormatterContext.Push(formatter)) { … }`.
- JSON para APIs, TOON para consumo de IA, texto plano para personas.
- **Cuidado con `<InvariantGlobalization>true</InvariantGlobalization>`**: `ToonFormatter` construye
  un `CultureInfo("en-US")` en su constructor estático y revienta al arrancar.
- Una proyección multilínea renderizada como campo vuelve con los saltos escapados. Si es prosa para
  leer, sacala del documento e imprimila cruda.

### 7.7 Un journal, un escritor

El backend de file system toma lock. **No corras el API y el CLI contra el mismo journal a la vez.**
Connection string verificado:

```
path=<directorio>;maxFileSize=4194304;compression=GZip
```

### 7.8 Los valores viajan como parámetros, nunca concatenados

```csharp
// MAL: texto distinto en cada llamada → una Action nueva por llamada, journal que explota
actor.Using($"root.Add('{sku}', {qty});")

// BIEN: una plantilla, argumentos aparte
actor.Using("root.Add(@sku, @qty);").WithParameters(...)
```

El journal guarda **una sentencia reusable más sus argumentos**. Concatenar destruye esa densidad y
además rompe la identidad de la Action.

### 7.9 Verificá la versión del framework contra la que escribís

Las guías describen un commit concreto. Un checkout viejo puede no tener la API que documentan —y
fallar de maneras que parecen error propio. En este proyecto el checkout estaba **967 commits atrás**,
con `Parameters` exponiendo los indexers como `internal` (o sea: **imposible pasar `@params` desde
otro assembly**) y `PerformanceV2` sellado sin constructor de assembly.

**Antes de pelear con una API que no calza con la guía, comprobá en qué commit estás.**

---

## 8. Sobre las pruebas

Tres niveles, cada uno probando lo que el anterior no puede ver.

| Nivel | Contra qué | Qué demuestra |
|---|---|---|
| Dominio | el agregado directo | las leyes del negocio |
| Perform | journal real en carpeta temporal | el DSL, el binding de parámetros, la persistencia |
| Carátula | la herramienta como la invocan | argv/HTTP, salidas, códigos |

En el nivel perform, **la prueba que más vale es la del reinicio**: registrá algo, botá el puppet,
reabrí sobre el mismo journal y comprobá que sigue ahí. Es la única evidencia honesta de que el estado
lo sostiene el journal y no la memoria.

Y fijá con una prueba lo que descubriste en §7.1: que un rechazo **no deja nada** que haga fallar la
rehidratación siguiente.

---

## 9. Antes de dar por terminado

**El modelo**

- [ ] ¿El `.Domain` no referencia absolutamente nada?
- [ ] ¿Todos los tipos del dominio son `internal`? ¿El host no nombra ninguno?
- [ ] ¿Ninguna parte del dominio lee el reloj ni genera azar?
- [ ] ¿Las consultas de la fachada devuelven escalares, arreglos primitivos o proyecciones?

**El substrato**

- [ ] ¿El agregado se construye en un `upgrade`, como global raíz?
- [ ] ¿Cada comando que el dominio pueda rechazar va detrás de un `Check`? (§7.1)
- [ ] ¿Hay un predicado total por cada cosa que el `Check` necesita preguntar?
- [ ] ¿Ningún valor se concatena en el texto del DSL?
- [ ] ¿El actor se construye una vez y se reusa?

**Las carátulas**

- [ ] En consola: ¿stdout lleva sólo la respuesta?

**Las pruebas**

- [ ] ¿Existe una prueba de reinicio?
- [ ] ¿Existe una prueba de que un rechazo no envenena el journal?

---

## 10. Lo que esta guía todavía no cubre

Honestamente, para que nadie lo dé por resuelto:

- **La guía de arquitectura**, que es la que debería explicar *por qué* esta forma y no otra.
- **Reactions y `tell`.** La app de ejemplo es un solo actor. Nada de lo de acá se probó con varios
  actores hablándose.
- **Cómo elegir dónde cortar los actores.** Un actor es un rol y una unidad de encapsulamiento; qué
  hace a un buen corte no está escrito.
- **El paquete NuGet.** Acá se consumió Puppeteer por referencia a fuente porque el feed privado pide
  autenticación interactiva. El cambio es de dos líneas, pero no está probado.
- **Backup, réplica y despliegue.** Existen en las guías (`materialize-backup.md`,
  `replicate-and-offline.md`, `deploy-and-replay.md`); acá no se tocaron.
- **Multi-escritor.** Un journal, un proceso. Qué hacer cuando eso no alcanza, queda abierto.
