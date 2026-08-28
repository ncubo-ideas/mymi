# Capítulo 3 — Construir la app

> Capítulo 3 de las [guías](README.md). El 0 dice dónde corre todo; el 4, qué modelar; éste, cómo se escribe.

Receta de trabajo, derivada de construir dos apps completas de punta a punta: dominio puro, lógica,
carátulas de consola y de IA, sobre un journal de file system.

**Qué es esta guía y qué no.** No reemplaza a las guías de Puppeteer (`basics.md`, `parameters.md`,
`control-flow.md`, `conventions.md`, `output-and-controller.md`, `hosting-and-environments.md`) ni a
la guía de dominio (`domain-guide.md`). Aquéllas dicen **cómo funciona el framework**; ésta dice **en
qué orden armar una aplicación y con qué forma**.

> **Si vas con prisa, esto no es lo que buscabas.** Las reglas del substrato —lo que sólo se aprende
> chocando— viven ahora en [Las reglas del substrato](reglas-del-substrato.md). Este capítulo es el
> how-to que se recorre una vez; aquél es la referencia que se consulta seguido.

El ejemplo recurrente es un presupuesto familiar. Cada vez que aparece, es **ilustración, no
prescripción**: generalizá la forma, no los nombres.

---

## 1. La forma de la solución

**Un directorio por negocio, cinco nodos adentro, nombres en español.** Es la convención del equipo y
la sostienen idéntica los negocios que ya existen. El capítulo 0 §7 la describe entera; acá va lo que
manda al escribir.

```
<Negocio>/
├── Preguntas.md             lo que el negocio se compromete a contestar. NO es un nodo
├── PaginaDeDescarga/        URL1 · nginx. No es un csproj
├── EndPointDeEventos/       URL2 · eventos, token, rotación, señuelo 403
├── InyectorDeEventosDemo/   siembra la historia en URL2. Valores elegidos, no aleatorios
├── Mecanismo/               el Portador: refresh, token, checkpoint, puente de journal
├── Dominio/                 el puppet. CERO dependencias
└── App/                     una lógica y tres carátulas
    ├── <Negocio>.App        la lógica: los PerformCommand y PerformQuery. No sabe dónde corre
    ├── <Negocio>.Wasm       la app que instala la persona
    ├── <Negocio>.Mcp        la superficie que ve la IA. SOLO LECTURA
    └── <Negocio>.Consola    para medir el dominio y el refresh sin browser
```

**Quién puede nombrar a quién, que es lo que de verdad ordena los cortes:**

| Nodo | Referencia a | Razón |
|---|---|---|
| `Dominio` | **nada** | El modelo es el activo durable. Si referencia el host, muere con él |
| `<N>.App` | Puppeteer, y el dominio **sólo como archivo** | Es la lógica, idéntica para toda carátula. **No nombra un tipo del dominio en C#**: los tipos van dentro de los scripts y los resuelve el motor por reflexión |
| `<N>.Wasm`, `<N>.Mcp` | `<N>.App` | Sólo saben de su medio: pantalla, o tools y stdio |
| `<N>.Consola` | `<N>.App`, y el dominio **de verdad** | Es la única excepción, porque además del ciclo corre el fixture que instancia el modelo directo. Necesita `InternalsVisibleTo` |
| `EndPointDeEventos`, `InyectorDeEventosDemo` | **ni el dominio ni el journal** | *El proveedor no conoce el dominio de la persona.* Lo único que los conecta es HTTP |

Ese último es un **invariante falsable**, no una preferencia: el día que tu endpoint de eventos
necesite referenciar tu dominio, algo se cruzó.

**El error a evitar:** que una carátula referencie a otra para reusar el puppet. Extraé `<N>.App`
**antes** de escribir la segunda carátula, no después.

> **Y una advertencia sobre esta sección.** El árbol de arriba es el que tienen los negocios que
> corren hoy. La recomendación del equipo sobre `internal` viene con uno distinto —`Superficie`,
> `Cli`, `Tests`, `publicador-simulado`— y dice de sí misma que *"no es definitiva"*. Lo que **no**
> depende de cuál gane es todo lo demás de este capítulo: el dominio puro y `internal`, la fachada, y
> las [reglas del substrato](reglas-del-substrato.md).

---

## 2. El orden de trabajo

El orden importa: cada fase deja verificado lo que la siguiente asume.

1. **El dominio, con sus comprobaciones.** Sin framework, sin journal, sin host. Corre en
   milisegundos.
2. **La lógica y la carátula de consola, juntas.** El puppet abre, hidrata y siembra; la consola es
   el arnés que lo prueba. **No dejes esto para después** — ver el recuadro de §8: un dominio que
   compila y pasa sus comprobaciones puede ser imposible de hospedar, y no hay forma de enterarse
   hasta que un perform lo toca.
3. **URL2 y el inyector.** Recién acá entran datos que no inventaste vos.
4. **Las dos carátulas de afuera**: la que instala la persona y la que ve la IA. Si la lógica quedó
   en su lugar, éstas son sólo entrada y salida.

No saltes la 1. Un dominio que se escribe al mismo tiempo que el host termina contaminado de
decisiones del host —fechas del reloj, formatos, DTOs— que después no se pueden sacar.

Y no separes la 1 de la 2 más de lo necesario. Son dos preguntas distintas —*¿piensa bien?* y *¿se
puede hospedar?*— y ninguna red atrapa lo de la otra.

---

## 3. Fase 1 — el dominio puro

Esto lo gobierna `domain-guide.md`. Lo que hay que retener al empezar:

- **Todo `internal`**, y en su propio csproj. Es la regla que más se rompe y la que más cuesta:
  tiene su propia sección abajo.
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

### El dominio va `internal`, y el csproj separado es lo que lo hace cierto

Es la regla que más se rompe, casi siempre con la misma justificación —*"el motor necesita que sea
`public`"*— y esa justificación es falsa. Vale desarmarla entera, porque el argumento de verdad no es
el que uno espera.

**El motor nunca pidió `public`.** Tres hechos sobre su fuente:

| | |
|---|---|
| El registro de tipos usa **`assembly.GetTypes()`**, no `GetExportedTypes()` | Un dominio entero `internal` entra al registro sin excepción |
| Su único filtro es **`!t.IsNestedPrivate`** | Entra `internal` top-level, `internal` anidado, `protected`. Sólo quedan afuera las clases que el compilador emite para closures |
| **`BindingFlags.NonPublic`** en toda la ruta del intérprete | Ningún método ni field necesita ser público. Un método `internal` —o `private`— se invoca igual desde un script |

**Y el perform tampoco.** Acá está el malentendido que más se repite: *"el perform alcanza los
`internal` porque comparte assembly con el dominio"*. **El perform no alcanza internals ni
compartiendo assembly: entrega una cadena.**

```csharp
Using(@"
    upgrade('semilla') {
        facturas = Facturario();
    }
").PerformCommand();
```

`Facturario` aparece **dentro de un string**. Quien lo resuelve es el motor, por reflexión, y **la
reflexión no tiene frontera de assembly**: eso funciona idéntico con el dominio en otro csproj y
enteramente `internal`. Lo único que ese archivo le pide al dominio en C# es un `typeof(X).Assembly`
para el constructor — y hasta eso se evita:

```csharp
: base(nombreDelActor, ElDominio.Assembly)     // ver abajo: LoadFrom, no Load
```

Con eso se cae también el `using` del namespace del dominio. Ese `public` es **ergonomía**, no
necesidad.

> **Cuidado con `Assembly.Load` por nombre: no encuentra un DLL que está al lado.** Medido, y cuesta
> caro porque el error apunta a otro lado —
>
> ```
> FileNotFoundException: Could not load file or assembly 'Laboratorio.Dominio'
> ```
>
> La carga por nombre simple se resuelve contra el **`deps.json`**, y un proyecto que no referencia
> el dominio en compilación no lo tiene ahí — que es exactamente el caso que la cerca produce.
> **Copiar el archivo no alcanza.** Hay que cargarlo por ruta:
>
> ```csharp
> Assembly.LoadFrom(Path.Combine(AppContext.BaseDirectory, "Laboratorio.Dominio.dll"))
> ```
>
> Y no es un parche: es más fiel a lo que va a pasar de verdad, porque en la arquitectura objetivo
> **el Host carga el módulo como un archivo** que la persona bajó de la institución, no como una
> dependencia de compilación.
>
> Dos cosas más, del mismo hallazgo. **El `content` no es transitivo**: que un proyecto copie el
> dominio a su salida no lo pone en la salida de los que dependen de él — cada host que hospede un
> actor lo declara. Y **cargalo una sola vez**: `LoadFrom` sobre la misma ruta devuelve el mismo
> assembly, pero dos rutas distintas al mismo archivo dan dos assemblies, y el motor no reconoce los
> tipos de uno en el otro.

#### Por qué entonces insistir

Porque `public` no es sólo innecesario: **es dañino**, y por dos razones que no dependen de dónde
corra la app.

**Uno · los DTOs se van de paseo al dominio.** Con el dominio `public`, la superficie puede pasarle un
DTO a la lógica de negocio, y lo va a hacer: es el atajo más corto que existe. Lo que se pierde ahí no
es elegancia — es que **la interpretación del dato ocurre afuera del actor**. Qué significa, cómo se
coacciona, qué default toma cuando viene vacío: nada de eso queda en el journal. El journal deja de
ser el registro del acto y pasa a ser el registro de un acto **ya interpretado por alguien más**, cuya
interpretación no se puede replayar ni auditar.

**Dos · la lógica de negocio se muda a la superficie.** Si la superficie puede declarar una variable
de tipo dominio, la declara. Y con un objeto de dominio en la mano, la primera decisión "chiquita" se
escribe ahí mismo: un `if` sobre un estado, un cálculo, un default. Eso es lógica que quedó **fuera
del actor**: no se journaliza, no se replaya, no se rehidrata. Al reiniciar, el estado reconstruido no
la incluye, porque nunca fue un acto. Y el drenaje es silencioso — nadie escribe *"voy a mover el
dominio a la superficie"*; se escriben cinco `if` en seis meses, cada uno razonable por sí solo.

Con el dominio `internal`, **las dos cosas no compilan.**

#### Y por eso es csproj separado, no "disciplina"

Ésta es la parte que se pierde, y es toda la diferencia:

- **`internal` dentro del mismo assembly que la superficie no es una cerca.** La superficie está del
  lado de adentro y alcanza todo. La disciplina queda a cargo de la memoria del autor, y la memoria
  del autor no sobrevive a un sprint apretado.
- **`internal` en su propio assembly es una cerca que sostiene el compilador.** El atajo deja de ser
  de mal gusto y pasa a ser un **error de compilación**.

No es una cerca de seguridad: `internal` no defiende de nadie, cualquiera con reflexión pasa. Es una
**cerca contra el lenguaje anfitrión**: el DSL entra, C# no.

#### La única excepción, y cómo se pide

Un fixture que instancia el modelo directo, sin pasar por scripts. Ése es el único código C# fuera del
dominio que legítimamente nombra tipos del dominio, y **tampoco fuerza `public`**:

```xml
<ItemGroup>
  <InternalsVisibleTo Include="Laboratorio.Tests" />
</ItemGroup>
```

Sin `PublicKey` si nada está firmado. Por qué esto no rompe la cerca: es un permiso **explícito,
nominal y auditable**. Se ve en el diff, se justifica en la revisión, y se le puede pedir cuentas a
quien quiera agregar el segundo. `public` no le pide permiso a nadie ni deja rastro.

#### Si te toca hacer el barrido

1. Cambiar los tipos a `internal`.
2. Buscar cada nombre de tipo del dominio en todos los nodos de afuera.
3. Clasificar: **dentro de un string** (DSL) no es acoplamiento y se queda; **`typeof(X).Assembly`**
   es reemplazable; **declaración, `new`, cast, `is`, parámetro o retorno en C#** es acoplamiento real
   — se elimina o recibe su `InternalsVisibleTo` con justificación.
4. Compilar. **Lo que no compila es el mapa exacto de los atajos que la cerca estaba dejando pasar.**
   Cada error es un lugar donde el dominio se estaba filtrando, no una molestia.

Tres falsos positivos que el grep levanta y no son referencias a tipos: **constantes string** con el
nombre del tipo, **propiedades de DTOs propios** que se llaman parecido, y **métodos o records de
vista homónimos**. Revisá a mano.

> **Una precaución para WASM y WASI, no medida.** `typeof(X).Assembly` deja una referencia IL dura al
> assembly del dominio; `Assembly.Load(new AssemblyName(...))` no —el único vínculo es una cadena— y
> el trimmer que Blazor activa en `publish` Release podría sacarlo del output. El síntoma sería un
> `dotnet run` que anda y un publish que revienta. La respuesta es `<TrimmerRootAssembly>`, y conviene
> saber que **ese riesgo ya existe con el `typeof`**: ancla el tipo, no los miembros que el DSL
> resuelve por reflexión. Un dominio descubierto por reflexión y un trimmer agresivo son incompatibles
> por naturaleza.

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

Esto parece un detalle y es lo contrario. Es lo que permite el `Check` de la [regla 3](reglas-del-substrato.md), sin el cual un
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
        : base(Role, ElDominio.Assembly)          // LoadFrom por ruta — ver §3
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

## 5. Fase 3 — la lógica, y de ahí las carátulas

La regla que vale para todas: **el actor se construye una vez y se reusa**. La carátula lo abre al
arrancar y lo mantiene vivo; **un actor no se crea por request**.

> **Sobre el ejemplo de abajo.** Está escrito como un endpoint HTTP porque es la forma más corta de
> enseñar los tres pasos, y esos tres pasos son idénticos en las tres carátulas. Pero **una app
> clientecéntrica no tiene API REST propia**: sus carátulas son la que instala la persona, la que ve
> la IA, y la de consola. Leé el `[HttpPost]` como *"acá entra una petición de afuera"*.

Cada operación son siempre los mismos tres pasos:

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

## 7. Las reglas del substrato

**Se mudaron a su propio documento: [Las reglas del substrato](reglas-del-substrato.md).**

Son dieciséis cosas que el motor hace y ninguna documentación dice, todas descubiertas a los golpes.
Salieron de acá porque **no se leen igual que el resto del capítulo**: esto es un how-to que se
recorre una vez, y ésas son una referencia que se consulta cada semana. **Conservan su numeración**:
la *regla 5* de allá es la que hasta hoy se citaba como *§7.5*, acá y en commits viejos.

**Antes de escribir la fase 1, leé por lo menos estas tres:**

| | | Por qué ahora |
|---|---|---|
| **1** | Un comando rechazado se journaliza igual | Es la peor de todas: envenena el replay para siempre |
| **10** | Un parámetro le puede robar el nombre a un tipo | Te muerde al nombrar el primer perform |
| **13** | El `print` proyecta un solo nivel | Decide la forma de tus carriers, y rehacerla después duele |

---

## 8. Sobre las pruebas

> **Un dominio no está terminado hasta que un perform lo tocó.**
>
> Es la regla que más caro sale ignorar, y hay una medición detrás. Un dominio de veintidós clases
> compiló limpio y pasó **31 comprobaciones** del modelo puro — y era **imposible de hospedar**: tres
> de sus conjuntos cerrados no se podían nombrar desde un comando ([regla 11](reglas-del-substrato.md)), un parámetro tapaba a un
> tipo ([regla 10](reglas-del-substrato.md)), y sus carriers tenían dos niveles donde el `print` proyecta uno ([regla 13](reglas-del-substrato.md)).
>
> Ninguna de las dos redes lo atrapa. El compilador no ve el DSL, que se resuelve por reflexión en
> tiempo de ejecución; y las comprobaciones del dominio puro prueban las **leyes del negocio**, que
> estaban bien. Cinco de los seis defectos aparecieron **en la primera corrida contra un journal**, y
> ninguno antes.
>
> Traducido a orden de trabajo: no declares terminado el dominio para pasar a la carátula. Atalo a un
> perform **mientras** lo escribís, aunque sea con una carátula de consola de veinte líneas.

Tres niveles, cada uno probando lo que el anterior no puede ver.

| Nivel | Contra qué | Qué demuestra |
|---|---|---|
| Dominio | el agregado directo | las leyes del negocio |
| Perform | journal real en carpeta temporal | el DSL, el binding de parámetros, la persistencia |
| Carátula | la herramienta como la invocan | argv/HTTP, salidas, códigos |

En el nivel perform, **la prueba que más vale es la del reinicio**: registrá algo, botá el puppet,
reabrí sobre el mismo journal y comprobá que sigue ahí. Es la única evidencia honesta de que el estado
lo sostiene el journal y no la memoria.

Y fijá con una prueba lo que descubriste en la [regla 1](reglas-del-substrato.md): que un rechazo **no deja nada** que haga fallar la
rehidratación siguiente.

---

## 9. Antes de dar por terminado

**El modelo**

- [ ] ¿El `Dominio/` no referencia absolutamente nada?
- [ ] ¿Todos los tipos del dominio son `internal`? ¿El host no nombra ninguno?
- [ ] ¿Ninguna parte del dominio lee el reloj ni genera azar?
- [ ] ¿Las consultas de la fachada devuelven escalares, arreglos primitivos o proyecciones?
- [ ] ¿Cada conjunto cerrado tiene su **resolutor total** desde primitivo? Sin él, ningún comando lo
      puede nombrar ([regla 11](reglas-del-substrato.md))
- [ ] ¿Ningún carrier tiene un objeto o una colección adentro que la fachada exponga? ([regla 13](reglas-del-substrato.md))
- [ ] ¿Los nullable viajan como bool más valor, y no como hueco? ([regla 13](reglas-del-substrato.md))
- [ ] ¿Ninguna lectura que arma el dominio formatea una fecha? ([regla 14](reglas-del-substrato.md))

**El substrato**

- [ ] ¿**Un perform tocó el dominio**, aunque sea desde una consola de veinte líneas? Compilar y
      pasar las pruebas del modelo puro no alcanza — ver el recuadro de §8
- [ ] ¿Ningún parámetro se llama como un tipo del dominio? ([regla 10](reglas-del-substrato.md))
- [ ] ¿El agregado se construye en un `upgrade`, como global raíz?
- [ ] ¿Cada comando que el dominio pueda rechazar va detrás de un `Check`? ([regla 1](reglas-del-substrato.md))
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
