# Las reglas del substrato

Dieciocho cosas que el motor hace y ninguna documentación dice. **Todas se descubrieron a los
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
| **15** | [El `print` de varios campos lleva alias sin comillas](#15--el-print-de-varios-campos-lleva-alias-sin-comillas-separados-por-coma) | Al armar cualquier vista |
| **16** | [Un escalar tipado sale por parámetro `out`](#16--un-escalar-tipado-sale-por-parámetro-out-no-parseando-el-print) | Al querer un número y no un texto |
| **17** | [El `Check` pregunta exactamente lo que el dominio rechaza](#17--el-check-tiene-que-preguntar-exactamente-lo-que-el-dominio-rechaza) | **Corolario de la 1**, y envenena el journal igual |
| **18** | [Todo comando necesita `Check`, incluso el que no puede fallar](#18--todo-comando-necesita-check-incluso-el-que-no-puede-fallar) | **No falla: miente.** El peor de encontrar |

> La **14** es la única que no es una limitación del motor: es una decisión de diseño que se
> descubrió al chocar con el `print`. Se quedó acá porque se aprende en el mismo lugar.
>
> Las **15** y **16** salieron de construir el [Hola Mundo](../Ejemplos/HolaMundo/README.md), y la
> **2** se corrigió en la misma corrida.
>
> Las **17** y **18** son las más nuevas y **las dos corrigen a la 1**: salieron de atar el dominio
> del gimnasio al perform, donde la 1 se cumplió al pie de la letra y aun así se envenenaron tres
> journals. Si ya leíste la 1 alguna vez, **volvé por esas dos**.

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

> ⚠️ **Esta regla, sola, no alcanza — y se midió.** Cumplirla al pie de la letra y envenenar tres
> journals igual es perfectamente posible, por dos agujeros que tiene: el `Check` puede preguntar
> **otra cosa** que la que el dominio rechaza ([regla 17](#17--el-check-tiene-que-preguntar-exactamente-lo-que-el-dominio-rechaza)),
> y un comando que **no puede fallar** parece no necesitar `Check` y sí lo necesita
> ([regla 18](#18--todo-comando-necesita-check-incluso-el-que-no-puede-fallar)). Leé las dos antes de
> dar esta por sabida.

## 2 · El contrato de retorno de `PerformCheckThenCommand`

Medido, no supuesto:

| | retorno | entradas del journal |
|---|---|---|
| **rechazado** | `{"EWI":[{"Error":"…"}]}` (JSON) · `EWI:\n  - Error: "…"` (TOON) | **no avanzan** |
| **aceptado** | lo que el `print` produjo | avanzan |

El marcador es la **propiedad `EWI` en la raíz del documento** — y con el formatter JSON eso **no** es
lo mismo que "el texto empieza con `EWI`", porque empieza con `{`. Escribir `salida.StartsWith("EWI")`
compila, corre, y **nunca da verdadero**: se lee como que el `Check` no funciona cuando en realidad
funciona perfecto. Es un falso negativo silencioso, y costó una tarde.

```csharp
using JsonDocument documento = JsonDocument.Parse(salida);
bool rechazado = documento.RootElement.TryGetProperty("EWI", out JsonElement ewi);
```

Y no alcanza con "retorno no vacío": un comando aceptado que imprime algo también devuelve texto.

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
- **Un `ToString()` sobrescrito no se llama solo.** Aunque la clase lo tenga puesto justamente para
  poder imprimirse, hay que escribirlo en el script: `print lectura.ToString() estado`. Si no:

  ```
  'Print estado' emits a value of type 'Lectura', which is material, not a value.
  The projection is the actor's to author, not the material's: emit the leaves the
  view needs, or state the rendering explicitly (for instance '.ToString()').
  ```

  **La primera salida es la buena; la segunda es la escapatoria.** Escribir `.ToString()` en el
  script cambia **quién llama**, no **quién decide**: el material sigue eligiendo el texto y el actor
  sólo lo pide en voz alta. Si el tipo tiene una etiqueta, **exponela como hoja** — `Nitritos` en
  `Examenes` expone `Etiqueta`; los tipos de juicio del gimnasio guardan su `estado` privado y sólo
  se dejan leer por `ToString()`, que es el material eligiendo por el actor.

  **El tell: si la única forma de sacarle el texto a un tipo es `ToString()`, el actor no tiene entre
  qué elegir.** `.ToString()` en el script se justifica sólo cuando el tipo tiene **una sola**
  rendición y es del dominio —un conjunto cerrado diciendo su nombre—; un juicio que la superficie
  puede querer mostrar de varias formas expone hojas. (Guía de dominios, **E24**: `ToString` es para
  intención de dominio o depuración, **nunca un contrato que alguien parsee**.)

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


---

## 15 · El `print` de varios campos lleva alias sin comillas, separados por coma

La sintaxis no se adivina, y equivocarla falla en el **parser** — o sea, en tiempo de ejecución y
con un mensaje que no menciona las comillas:

```
Expected token type 'semicolon' at line 3, column 30, but found value 'f' of type 'id'.
```

Lo que el motor espera es **expresión, alias, coma**. El alias es un identificador desnudo:

```
foreach (factura in libro.Facturas) {
    print factura.Numero numero,
        factura.Fecha fecha,
        factura.CantidadDeLineas lineas,
        factura.Total total;
}
```

**Tres formas de escribirlo mal**, y las tres fallan distinto:

| Se escribe | Qué pasa |
|---|---|
| `print f.Numero 'numero' f.Total 'total';` | Falla el parser: las comillas no son un alias |
| `print f.Numero f.Total;` | Falla el parser: espera alias después de la primera expresión |
| `print facturas.Cantidad;` | *"Expected an alias for the 'print' expression"* — hasta un campo solo lo necesita |

El alias es lo que nombra la propiedad en el JSON de salida, así que sin él el motor no tiene cómo
armar el documento. Y el nombre del alias **es** la llave que después vas a leer:

```json
{"factura":[{"numero":"F-001","fecha":"03/01/2026","lineas":2,"total":9900.0}]}
```

Fijate que la llave del arreglo es el nombre de la **variable del `foreach`** (`factura`), no el de la
colección. Si renombrás la variable, cambiás el JSON.

---

## 16 · Un escalar tipado sale por parámetro `out`, no parseando el `print`

Son dos canales distintos y confundirlos es el error que más se repite.

| | Para qué | Cómo |
|---|---|---|
| **Parámetro `Out`** | Un **escalar** que la app necesita tipado | `@total = libro.Total;` con `Parameter.Out` |
| **`print`** | Una **vista**: varias filas, varios campos | `foreach` + alias, y devolvés el JSON tal cual |

Un escalar se pide así, y volvés con un `decimal` de verdad:

```csharp
using (RentedParameter alquilados = app.Actor.RentedParameters())
{
    app.Actor.Using("@total = libro.Total;")
    .WithParameters(alquilados, p => {
        p[Parameter.Out, "total", typeof(decimal)] = default;
    })
    .PerformQuery();

    return alquilados["total"].GetValue<decimal>();
}
```

**Por qué importa y no es preferencia de estilo.** Parsear el `print` para sacar un número te obliga
a adivinar el formato —separador decimal, cultura, cuántos ceros—, y eso es re-derivar en C# algo que
el actor **ya calculó**. Con `Out` el tipo lo declara quien pregunta. Es lo que le permite a una UI
hacer aritmética sin adivinar, y a una tool del asistente devolver un `decimal` en su
`structuredContent` en vez de una cadena.

> **Lo que NO es el antipatrón.** Leer el JSON de una **vista** para armar tus records de pantalla es
> correcto: ahí el `print` con formatter JSON es el canal declarado y ya hizo el trabajo de
> serializar. El antipatrón es usar ese canal para un **escalar**.

---

## 17 · El `Check` tiene que preguntar exactamente lo que el dominio rechaza

**Corolario de la [regla 1](#1--un-comando-rechazado-se-journaliza-igual--y-envenena-el-replay), y sin
él la 1 no alcanza.** La 1 dice: guardá con un `Check` todo comando que el dominio pueda rechazar.
Eso se puede cumplir al pie de la letra y envenenar el journal igual, si el `Check` pregunta **otra
cosa**.

Pasó tres veces seguidas en un mismo dominio. Los `Check` preguntaban cosas razonables, y ninguna era
la que hacía reventar al dominio:

| Comando | El `Check` preguntaba | El dominio rechazaba por |
|---|---|---|
| Agregar meta | ¿hay objetivo? | la meta **ya existía** |
| Prescribir ejercicio | ¿existe el plan? | el ejercicio **ya estaba prescrito** |
| Terminar plan | ¿ya terminó? | …y también si **terminaría antes de empezar** |
| Registrar medida | ¿el nombre existe? | …y también si **ya estaba en esa hoja** |

El `Check` pasa, el comando entra, el dominio revienta a mitad de la escritura — y como la entrada ya
se escribió, se replaya en cada arranque:

```
Rehydration execution failed. EntryId=55, Cause: Ya hay una meta para 'cintura'.
```

**La regla, y es de forma, no de cuidado: un predicado total por comando, en la raíz, que junte
TODAS las razones por las que ese comando fallaría.** Nombrado `Puede<Comando>`, uno por cada método
de la superficie:

```csharp
internal bool PuedeRegistrarMedida(DateTime fecha, string queSeMidio, decimal valor)
```

Un `Check` que llama a un predicado con el nombre del comando **no se puede desalinear**: si mañana
el dominio agrega una razón de rechazo, se agrega adentro de ese predicado y el `Check` la hereda.
Repartir las condiciones entre varios `Check` sueltos es lo que produce la tabla de arriba.

> **Y es auditable:** todo comando de la superficie tiene `Check`, y ese `Check` llama a exactamente
> un predicado `Puede<Comando>`. Se puede mirar sin entender el negocio.

---

## 18 · Todo comando necesita `Check`, incluso el que no puede fallar

**El más difícil de encontrar de las dieciocho, porque no produce ningún error.**

La [regla 1](#1--un-comando-rechazado-se-journaliza-igual--y-envenena-el-replay) habla de comandos que
el dominio **puede rechazar**. Leída así, un comando que no rechaza nada no necesita `Check` — y eso
es exactamente lo que se creyó de `Asistio`, que sólo agrega un día a una lista.

Corriendo el mismo journal dos veces:

```
primera corrida:  {"esperados":39,"fue":30,"faltaron":9,"cumplio":false}
segunda corrida:  {"esperados":39,"fue":60,"faltaron":0,"cumplio":true}
```

**No falló.** Contó los mismos treinta días otra vez y le dijo a la persona que cumplió su plan
cuando le faltaron nueve. Ninguna excepción, ningún `EWI`, ninguna línea roja. Una respuesta creíble
y equivocada **es peor que un error**, porque nadie la va a investigar.

**La pregunta correcta no es *¿este comando puede fallar?* sino *¿este mismo hecho puede volver a
llegar?*** — y en una app con journal **siempre** puede: un replay es exactamente eso, y un proveedor
que reenvía un evento también.

> **Los comandos que no pueden fallar necesitan `Check` por identidad, no por validez.** El predicado
> no pregunta si el dato es válido; pregunta si **ese hecho ya está adentro**.

La forma es la misma de la regla 17 — `PuedeRegistrarAsistencia(dia)` devuelve `false` si ese día ya
se registró — así que en la práctica las dos reglas se cumplen con el mismo trabajo: **un predicado
total por comando, sin excepciones, incluidos los que "no hacen nada peligroso"**.
