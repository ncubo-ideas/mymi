# Las reglas del substrato

Catorce cosas que el motor hace y ninguna documentación dice. **Todas se descubrieron a los
golpes**, escribiendo un dominio y viéndolo fallar.

> **Esto es una referencia, no un tutorial.** Si estás construyendo tu primera app, el
> [capítulo 3](03-construir-la-app.md) te lleva de la mano y te trae acá cuando corresponde. Si ya
> sabés lo que hacés, ésta es la página que querías: entrá por el índice y salí.

Las reglas **conservan su numeración histórica**: la **regla 5** de acá es la que hasta hoy se citaba
como *§7.5*.

## Índice

| | Regla | Cuándo te muerde |
|---|---|---|
| **1** | [Un comando rechazado se journaliza igual](#1--un-comando-rechazado-se-journaliza-igual--y-envenena-el-replay) | **El peor.** Un error de tipeo queda permanente |
| **2** | [El contrato de retorno de `PerformCheckThenCommand`](#2--el-contrato-de-retorno-de-performcheckthencommand) | Al distinguir aceptado de rechazado |
| **3** | [El dominio se resuelve por el assembly del actor](#3--el-dominio-se-resuelve-por-el-assembly-del-actor) | Silencioso y equivocado si te lo saltás |
| **4** | [El seed cuesta una entrada por arranque](#4--el-seed-cuesta-una-entrada-por-arranque) | Al segundo arranque |
| **5** | [El framework escribe en stdout](#5--el-framework-escribe-en-stdout) | Con transporte stdio, te corrompe el canal |
| **6** | [El formatter](#6--el-formatter) | Al parsear lo que devuelve el perform |
| **7** | [Un journal, un escritor](#7--un-journal-un-escritor) | Con dos procesos sobre la misma carpeta |
| **8** | [Los valores viajan como parámetros, nunca concatenados](#8--los-valores-viajan-como-parámetros-nunca-concatenados) | Con comillas, acentos o comas |
| **9** | [Verificá la versión del framework](#9--verificá-la-versión-del-framework-contra-la-que-escribís) | Cuando la doc no coincide con el binario |
| **10** | [Un parámetro le puede robar el nombre a un tipo](#10--un-parámetro-le-puede-robar-el-nombre-a-un-tipo) | Al nombrar `@examen` teniendo `Examen` |
| **11** | [Un `static readonly` no se alcanza desde el DSL](#11--un-static-readonly-no-se-alcanza-desde-el-dsl-un-método-estático-sí) | Con conjuntos cerrados |
| **12** | [El ternario no unifica dos subclases](#12--el-ternario-no-unifica-dos-subclases-a-su-base-común) | Al devolver una u otra rama |
| **13** | [El `print` proyecta un solo nivel](#13--el-print-proyecta-un-solo-nivel) | Con carriers anidados o nullables |
| **14** | [El dominio dice duraciones, no fechas](#14--el-dominio-dice-duraciones-no-fechas) | Es de diseño, no del motor |

> La **14** es la única que no es una limitación del motor: es una decisión de diseño que se
> descubrió al chocar con el `print`. Se quedó acá porque se aprende en el mismo lugar.

---

## 1 · Un comando rechazado se journaliza igual — y envenena el replay

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

## 2 · El contrato de retorno de `PerformCheckThenCommand`

Medido, no supuesto:

| | retorno | entradas del journal |
|---|---|---|
| **rechazado** | `{"EWI":[{"Error":"…"}]}` (JSON) · `EWI:\n  - Error: "…"` (TOON) | **no avanzan** |
| **aceptado** | lo que el `print` produjo | avanzan |

El marcador es que el documento **empieza con `EWI`**. No alcanza con "retorno no vacío", porque un
comando aceptado que imprime algo también devuelve texto.

## 3 · El dominio se resuelve por el assembly del actor

`ActorHandler` hace `actor.GetType().Assembly` para saber dónde buscar el vocabulario. Consecuencias:

- **Pasá el assembly explícitamente**: `new PerformanceV2(name, domainAssembly)`.
- El constructor de un solo argumento usa `Assembly.GetCallingAssembly()`, que desde el host es **el
  host**, no el dominio. Silencioso y equivocado.
- Cargalo **por ruta** (`Assembly.LoadFrom`), no por nombre. Así no hay que hacer público ningún
  tipo del dominio — y `Assembly.Load` con el nombre simple **falla** aunque el DLL esté al lado del
  ejecutable, porque resuelve contra el `deps.json`. El detalle, con las dos trampas que lo
  acompañan —el `content` no es transitivo, y cargalo una sola vez—, está en el
  [capítulo 3 §3](03-construir-la-app.md).

## 4 · El seed cuesta una entrada por arranque

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

**Y hay una consecuencia peor, que muerde a la superficie de sólo lectura.** Si el servidor MCP usara
la misma clase que la app, **cada pregunta de la IA escribiría una entrada en el journal de la
persona** y rompería la invariante de un solo escritor. La solución es un actor gemelo: mismo nombre,
mismo dominio, **`OnHydrated` vacío**. Capítulo 6 §6.6.

**Un segundo `upgrade` no se comporta como el primero.** Reportado por el equipo con receta de
reproducción: un `upgrade` agregado en un **segundo** `PerformCommand` **no asienta en la corrida que
siembra el journal**, así que otro proceso que rehidrate ese mismo journal no encuentra el global y
falla con algo que no parece culpa suya. Verificado en un journal real: los dos upgrades aparecían
**dos veces cada uno**, o sea que la guarda de una-vez-por-actor tampoco estaba conteniendo lo que
parecía.

> Si alguna vez hace falta un `upgrade` nuevo, **probalo abriendo el journal desde cero con otro
> proceso** —el MCP, por ejemplo— y no sólo con la app que lo sembró.

## 5 · El framework escribe en stdout

`ActorHandler` hace `Console.WriteLine("Starting …")` y marcas de progreso `1%2%` **sin newline**, que
prefijan la línea siguiente. En un CLI cuya salida alguien va a parsear, eso la arruina.

**En una consola: capturá `Console.Out` mientras corre el framework y reenvialo a stderr.** stdout
lleva sólo la respuesta.

## 6 · El formatter

- En un `PerformanceV2`, `.Formatter(new ToonFormatter())`.
- En un `ActorV2` pelado —que es por donde van los performs con parámetros— **no hay `.Formatter`**:
  se empuja por contexto, `using (FormatterContext.Push(formatter)) { … }`.
- JSON para APIs, TOON para consumo de IA, texto plano para personas.
- **Cuidado con `<InvariantGlobalization>true</InvariantGlobalization>`**: `ToonFormatter` construye
  un `CultureInfo("en-US")` en su constructor estático y revienta al arrancar.
- Una proyección multilínea renderizada como campo vuelve con los saltos escapados. Si es prosa para
  leer, sacala del documento e imprimila cruda.

## 7 · Un journal, un escritor

El backend de file system toma lock. **No corras el API y el CLI contra el mismo journal a la vez.**
Connection string verificado:

```
path=<directorio>;maxFileSize=4194304;compression=GZip
```

## 8 · Los valores viajan como parámetros, nunca concatenados

```csharp
// MAL: texto distinto en cada llamada → una Action nueva por llamada, journal que explota
actor.Using($"root.Add('{sku}', {qty});")

// BIEN: una plantilla, argumentos aparte
actor.Using("root.Add(@sku, @qty);").WithParameters(...)
```

El journal guarda **una sentencia reusable más sus argumentos**. Concatenar destruye esa densidad y
además rompe la identidad de la Action.

## 9 · Verificá la versión del framework contra la que escribís

Las guías describen un commit concreto. Un checkout viejo puede no tener la API que documentan —y
fallar de maneras que parecen error propio. En este proyecto el checkout estaba **967 commits atrás**,
con `Parameters` exponiendo los indexers como `internal` (o sea: **imposible pasar `@params` desde
otro assembly**) y `PerformanceV2` sellado sin constructor de assembly.

**Antes de pelear con una API que no calza con la guía, comprobá en qué commit estás.**

---

## 10 · Un parámetro le puede robar el nombre a un tipo

`@periodicidad` y `Periodicidad` son el mismo nombre para el resolutor del DSL, y **gana el
parámetro**. Este script:

```
g = GastoDeLaPersona(@nombre, Periodicidad.PorNombre(@periodicidad));
```

falla con

```
Unknown property or method 'PorNombre' on type 'String'
```

Léelo dos veces: dice **`String`**. El resolutor tomó `Periodicidad` como el parámetro —que es un
`string`— y el mensaje acusa al tipo equivocado, así que mandan a revisar la clase del dominio, que
está perfecta.

**La regla: ningún parámetro puede llamarse como un tipo del dominio**, ni siquiera con otra
capitalización. Nombralos por su papel en la frase (`@cadaCuanto`, `@desdeCuando`) y no por su tipo.

## 11 · Un `static readonly` no se alcanza desde el DSL; un método estático sí

Un conjunto cerrado suele exponerse así:

```csharp
internal static readonly Procedencia Institucion = new Procedencia("institucion");
```

Desde un script, `Procedencia.Institucion` **revienta**:

```
Unknown property or method 'Institucion' on type 'Object'
```

Un **método** estático sí resuelve. Y de ahí sale la consecuencia, que es más grande que el síntoma:

> **Un conjunto cerrado sin resolutor total es inalcanzable desde un comando.**

La guía de dominio ya pide ese resolutor —`Periodicidad.PorNombre('mensual')`, que revienta ante lo
desconocido— por la **totalidad**. En este substrato es además la **única puerta de entrada**. Si tu
conjunto cerrado no tiene uno, no es que quede feo: es que ningún comando lo puede nombrar.

## 12 · El ternario no unifica dos subclases a su base común

```
contra = @hay ? ContraUnaTasaAnual(@nombre, @tasa) : ContraSuPropiaHistoria();
```

```
Binary operation TernaryAstExpression cannot combine
ContraUnaTasaAnual and ContraSuPropiaHistoria
```

Las dos ramas tienen que dar **el mismo tipo**, aunque el parámetro que las recibe sea la base.

**Elegí el texto del script en C#**, no la rama adentro del DSL. Y en un comando eso además es más
honesto: el journal guarda **cuál se ejecutó de verdad**, en vez de una condición que hay que volver
a evaluar en cada replay.

## 13 · El `print` proyecta un solo nivel

`print v.Responsables` no cruza. `print r.Gasto.Nombre` tampoco. Lo que cruza es **un nivel de
escalares**, y eso decide la forma de tus carriers antes de que escribas el primero:

- **Un objeto adentro de un carrier** se aplana con un accesor: `NombreDelGasto`, `NombreDeLaSeveridad`.
- **Una colección adentro de un carrier** no cruza: o se convierte en un escalar (`CuantosSinMarcar`)
  o va en **una consulta aparte** con su propio `foreach`.
- **Una colección de filas que a su vez tienen celdas** tampoco. Se aplana con índices —una fila por
  celda, con su `fila` y su `columna`— y la superficie rearma la grilla sin volver a decidir nada.
- **Un nullable viaja como bool más valor**, nunca como hueco. Si mandás sólo el número, quien lo
  reciba no puede distinguir *"cero"* de *"no se sabe"*, y ésas son dos respuestas muy distintas.

## 14 · El dominio dice duraciones, no fechas

Una lectura que arma el dominio **no formatea una fecha**. Dice *"lleva 15 días sin aparecer"*, no
*"se esperaba el 05/07/2026"*.

Hay una razón de forma y una de fondo.

**De forma:** el `print` serializa las fechas en el formato del framework. Si tu frase formatea la
misma fecha con otro, el payload sale con **dos versiones del mismo instante** y quien lo lea tiene
que adivinar. Medido: el campo salía `07/05/2026` y la frase decía `05/07/2026` —los dos el 5 de
julio— y un consumidor que leyera el campo como día/mes iba a reportar dos meses de atraso donde
había quince días.

**De fondo, y es la que importa:** *"se esperaba el 05/07/2026"* obliga a quien lo lea a hacer la
resta. *"Lleva 15 días sin aparecer"* **ya es la lectura**, que es lo único que el objeto sabe decir
mejor que nadie. La fecha sigue viajando en su campo como `DateTime`; formatearla es derecho de la
superficie, que es la que sabe para quién escribe.

Corolario práctico: decí las duraciones como las diría una persona — días hasta un par de meses,
meses de ahí en adelante. Una app que informa *"lleva 437 días"* obliga a dividir.

