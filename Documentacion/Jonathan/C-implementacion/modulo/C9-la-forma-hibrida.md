# C9 · La forma híbrida — la cara sin motor, la superficie al lado

**Medido el 2026-09-11**, Xiaomi `2407FPN8EG` con **Android 16**, build **Release**, motor
`3.0.0-beta.10029`, módulo `examenes` **3.7.0**.

Una tercera forma de módulo: **`web/` para la cara, `dominio/` para la superficie, y el motor
prestado por el Host**. Nace de una pregunta que venía de la lámina 04 de [`B7`](../../B-arquitectura/B7-el-canal-de-la-lamina-04.md):
*que el Puppeteer no venga en el módulo, sino que se use el que provee Gnothi*.

---

## Por qué existe

Las dos formas que había eran medias respuestas para el teléfono:

| forma | presta el motor | trae pantalla |
|---|---|---|
| **cargo** | ✅ | ❌ — el Host renderiza `view.json` |
| **hospedada** | ❌ — carga el suyo | ✅ |
| **híbrida** | ✅ | ✅ |

Y estaba **prohibida por nombre**, con un test defendiéndola:

```csharp
if (declaresLoad && carriesWeb)
    rejections.Add("... a module is cargo or hosted, not both");
```

Esa regla no era un descuido: protegía que **la cáscara fuera decidible desde el paquete solo**.
Eso se conserva — la forma se sigue leyendo de lo que el paquete trae, solo dejó de ser binaria.

## Lo que pesa

| | hospedada | híbrida |
|---|---|---|
| el `.gmod` | **12 594 KB**, 238 archivos | **97 KB**, 9 archivos |
| motor adentro | `Puppeteer`, `Choreography`, `Puppets.Portador`, `Gnothi.Contract` en `web/_framework` | **cero** — la aduana lo rechaza |
| la cara | Blazor Wasm | **57 KB** de html, css y un js |
| el dominio | en `.wasm` | **200 KB** en tres `.dll` |

**130 veces menos.** Y el ahorro no es el motor: medido antes, correr .NET en un navegador es el
**88,8 %** de una hospedada y el motor apenas el 8,6 %. Lo que la forma saca es el runtime entero.

> Los primeros números de esta tabla fueron **77 KB y una cara de 9,5 KB**, con la cara todavía a
> medias. Portar la pantalla real —la portada, los chips, las cinco formas de dibujo— la llevó a
> 57 KB y el paquete a 97. **Es lo que cuesta que se parezca de verdad**, y sigue siendo dos
> órdenes de magnitud menos que el runtime.

---

## Las siete costuras

| # | dónde | qué |
|---|---|---|
| 1 | `PackageInspector` | `PackageForm.Hybrid`, con la regla **más estricta de las tres**: rechaza el motor **incluso dentro de `web/`** |
| 2 | `ModuleEnrollment` | deja de escribir `"webview"` quemado; deriva el tipo de lo que el paquete trajo |
| 3 | `HybridShell` | compone las otras dos: la superficie en proceso **contesta**, el WebView **pinta** |
| 4 | el puente | **`consultar`**, el séptimo verbo, en las cuatro partes: Android, iOS, el despacho compartido y la fábrica |
| 5 | `armar-bundle.ps1` | tercera `-Forma`; el booleano `$esCargo` se parte en dos preguntas independientes |
| 6 | `[Deliver]` | el cartero le entrega **a la superficie**, no a la página: la cara no tiene motor con qué asentar |
| 7 | `[View]` | **el segundo público**, y la corrección más importante de todo el camino |

### 4 · `consultar`, y la flecha al revés

En la hospedada **la página ES la superficie**: Gnothi le pregunta por `window.gnothi.call`. En la
híbrida la página **pregunta**, y contesta la superficie en proceso.

La respuesta **no vuelve por retorno**, y no es una preferencia: los métodos que Android inyecta
con `@JavascriptInterface` son síncronos y la página se congelaría; iOS ni siquiera puede devolver
un valor. Por eso el híbrido **reusa el buzón** que ya existía: `window.gnothi.respuesta`.

**No lleva el nombre del módulo, a propósito.** Si lo llevara, una cara podría preguntar por el
journal de otra institución y **ninguna regla río abajo lo frenaría** — el Host vería una pregunta
legítima de un módulo legítimo.

### 7 · `[Query]` y `[View]` son dos públicos, no uno

Es la corrección que más cambió el diseño, y salió de una pregunta: *la superficie no pinta esos
dos servicios; esos dos son lo que Gnothi responde hacia afuera*.

| | `[Query]` | `[View]` |
|---|---|---|
| público | **el asistente** | **la cara de la institución** |
| cuántas, en exámenes | **2** | 21 lecturas disponibles en el dominio |
| forma | resúmenes gruesos, descripción escrita **para el modelo** | series, puntos, líneas de referencia — **para dibujar** |
| en el manifiesto | sí | **no** |
| la apaga el consentimiento | **sí** | **no** |
| la ve el modelo | sí | **nunca** |

Una pantalla no puede pintar una tendencia con un resumen, y al asistente no le sirven los puntos
de un gráfico. Y hay una prueba de que son conjuntos distintos que da el propio sistema:
**declarar las 21 como tools haría que el alta reportara 19 `undeclared`** contra un manifiesto que
nombra dos.

Son **dos métodos en `IShell`, no un parámetro**: con un solo camino, una consulta de pantalla
podría llegar por la puerta del asistente — y al revés, que es peor — sin que nada lo impidiera.

> **Y `[View]` no pasa por el consentimiento.** Apagar una consulta decide **qué puede preguntar el
> asistente** sobre la persona; no le prohíbe a la institución mostrarle su propio expediente en la
> pantalla que ella acaba de abrir. **Son decisiones con sujetos distintos.**
>
> Lo mismo vale para `[Deliver]`: un módulo con todas sus consultas apagadas **sigue siendo el
> registro de la persona**, y que se siga llenando es lo que hace que volver a encenderlas sirva de
> algo.

---

## La cara: no una parecida, la misma

La primera cara se escribió de cero con los **tokens** de `Examenes.Wasm/wwwroot/css/app.css` —el
navy, el celeste, Georgia, la gota— y se veía bien. No alcanzaba, y la frase que lo cerró fue:
*«eso no se parece nada a la interfaz visual que ya existía para laboratorio»*.

Tenía razón, y la distinción importa: **copiar la paleta no es portar la pantalla**. Una cara que
reinventa los colores es otra institución para quien la mira; una que reinventa el **layout** es
otra app. Lo que faltaba era todo lo que no es color:

| lo que faltaba | de dónde salió |
|---|---|
| la **portada** con sus veredictos y las caritas, y `Actualizar` **adentro** de la tarjeta | `MisExamenes.razor` |
| la **tarjeta de serie**: chip, relato, valor en serif, referencia en mono, lectura abajo | `MisExamenes.razor` |
| las **cinco formas de dibujo** | `Examenes.Wasm/Formas/*.razor` |
| el **historial como cronología de visitas**, agrupado por día | `MisExamenes.razor` |

### Las cinco formas, y por qué no es una

El dominio dice qué forma le toca a cada analito, y la pantalla **la lee, no la inventa**:

| forma | qué dibuja | por qué no es una línea |
|---|---|---|
| `trayectoria con linea base` | la serie contra **la banda del nivel de siempre** | sin la banda es «una línea que sube», que es lo que la tabla ya decía |
| `progreso hacia meta` | el eje es **el valor**, de la partida a la meta | *«yo no ocupo ver esa historia»*: importa cuánto falta, no de dónde viene |
| `posicion en banda` | dónde caés entre piso y techo | alejarse es peor **en las dos direcciones**: no hay flecha |
| `presencia en el tiempo` | la tira de pruebas, la racha final en grueso | no hay eje vertical: no existe «más positivo» |
| `categoria en el tiempo` | igual, con vocabulario de colores | las categorías **no se ordenan**: ámbar no es «más» que amarillo pálido |

El comentario del `.razor` lo dice sin rodeos: dibujarlas todas igual **sería la misma tabla que se
rechazó en el dominio, un piso más arriba**. La primera versión de la cara hacía exactamente eso —
un gráfico de línea genérico para todo— y por eso un binario salía como una serie plana en cero.

El arreglo fue mover la decisión al lugar donde vive: **`[View("tendencia")]` pregunta por la forma
antes que por los hechos**, igual que la app suelta, y devuelve un paquete distinto según cuál sea.

**Dos diferencias deliberadas con el original**, las dos por la misma razón —esta cara todavía no
tiene pantalla de detalle— y las dos anotadas en el código:

- las tarjetas son `<article>` y no `<button>`: un botón que no lleva a ningún lado miente;
- el **pedido del sexo** es informativo y no editable: declararlo es una **escritura**, y
  `consultar` solo alcanza `[View]`.

Y una diferencia de **una letra**: el separador entre la lectura y la acción lleva espacio después
del punto. En el `.razor` es `.` pelado, y cuando un analito no pide nada pero trae acción —el cloro
de una sola medición— las dos frases salen pegadas: *«…para dónde viene.repetí el examen»*. Medido
en el teléfono.

---

## Un icono que parece la app de la institución

Es lo más cerca que se puede estar de *«su app»* sin que la institución publique una, y es lo que la
lámina 04 pedía sin nombrarlo. **`ShortcutManager.requestPinShortcut`**, desde el detalle del
módulo: un icono con el **logo y el nombre de la institución** que al tocarlo levanta su módulo.

| decisión | por qué |
|---|---|
| **intent explícito**, no `go.gnothi.app/abrir?modulo=…` | un `https` sin `assetlinks.json` verificado saca el diálogo de «¿con qué app abro esto?», depende de DNS, y cualquiera puede registrar un handler para ese dominio |
| **el icono se compone**, no se usa el logo tal cual | un icono adaptativo recorta todo lo que queda fuera del 66% central; se dibuja sobre el color de marca, al 52% de un lienzo de 432 px |
| `UpdateShortcuts` **antes** de `RequestPinShortcut` | si ya está fijado, pedirlo de nuevo no le cambia nada: el icono se queda con el rótulo y el logo del día que se puso |
| **lo pide ella, nunca se pide solo** | Android muestra su propio diálogo encima; dispararlo al instalar sería poner un icono en su teléfono porque instaló algo |
| el rótulo es **el nombre**, en los dos campos | empezó siendo el título del módulo y MIUI —que prefiere el largo— lo cortó a *«Tus examenes,…»*. Un icono lleva un nombre, no un eslogan |

**No es hospedaje de la forma híbrida**: sirve para cualquier módulo encendido. Vive acá porque es
lo que completa la ilusión que esta forma persigue.

### Lo que MIUI hizo, y no es lo que estaba previsto

Dos cosas se anticiparon al revés y conviene que estén escritas:

- **No mostró ningún diálogo de confirmación.** Puso el icono directo. La previsión era que Android
  8+ siempre pregunta; este launcher no.
- **No estampa el badge de Gnothi** en la esquina. La previsión era que sí, y era el argumento de
  honestidad: *«dice quién corre el código»*. **En este teléfono, en la pantalla de inicio nada dice
  que ese icono es Gnothi.** Es un punto a discutir, no un detalle de implementación.

Y un hallazgo útil: **`adb shell cmd shortcut clear-shortcuts <paquete>`** quita los accesos
directos fijados sin tocar los módulos ni el journal. Desde adentro de la app **no se puede**: la
API solo deja *deshabilitarlos* con un mensaje. Y está bien que sea así — lo puso la persona, lo
saca la persona.

---

## La cadena completa, corriendo

```
la cara         gnothiPuente.pull(id, '0')      JS plano, sin motor
Gnothi          courier.Pull(0)                 la red, con SU credencial
Gnothi          Entrega(sobre)                  el sobre NO vuelve a la página
la superficie   [Deliver] Entregar(...)         ExamenesApp, actor escribible
                leidos=26 aplicados=26 descartados=0 rechazados=0 ultimoEvento=225
la cara         consultar('cabecera')           [View], no [Query]
la superficie   ExamenesAPI                     26 exámenes, 0 fuera de rango
```

`aplicados=26 descartados=0 rechazados=0` dice que **la frontera de entrada corrió** —sinónimos,
verosimilitud, el LDL con su HDL— y no tuvo que descartar nada. `cuenta=migrated`, que el cartero
consiguió su credencial.

Y el alta lo verificó por su cuenta, contra la superficie **real** cargada del DLL:

```
comprobación de examenes: {"matches":true,
  "declared":["lista_de_examenes","resumen"],
  "actual":  ["lista_de_examenes","resumen"]}
```

Las **siete** `[View]` —`cabecera`, `examenes`, `serie`, `analitos`, `tendencia`, `alertas`,
`historial`— **no aparecen ahí**, que es exactamente lo que tiene que pasar.

---

## Lo que quedó contestado, y nunca se había medido

**Mono carga un ensamblado ajeno en Android.** `InProcessShell` —con `AssemblyLoadContext`,
`GetTypes`, `GetMethods` y `GetProperties`— funciona en un teléfono físico, en Release. No había
ninguna corrida que lo demostrara: el `StdioShellStub` de Android apunta a `"in-process"` desde
siempre, pero nadie lo había ejercido ahí.

**Una pantalla de institución, completa, sin motor propio.** No una demo con dos números: la
portada con sus veredictos, los chips, las cinco formas de dibujo y la cronología de visitas,
pintadas con datos reales del journal y con el dominio corriendo sobre el motor prestado.

**Un solo actor alcanza para las tres puertas** — el asistente, la cara y el cartero — sobre un
journal que Gnothi posee solo. Ver más abajo.

### Tres trampas que solo se ven en el aparato

Ninguna de las tres se habría encontrado razonando, y las tres costaron horas:

- **`INTERNET` falta en Release.** .NET Android agrega ese permiso **solo en Debug**. El síntoma era
  `HttpRequestException: Permission denied (127.0.0.1:8095)`, que **se lee como una restricción de
  MIUI**, y solo se veía con `dumpsys package`. Se arregla con un `[assembly: UsesPermission]`
  explícito.
- **Una carrera en el cableado.** `Surface` se conectaba **después** de `Start`, pero una cara
  híbrida llama `listo()` y pregunta **en el mismo tick**: *«este módulo no tiene superficie en
  proceso»*. Se arregló con un callback que corre **entre `Create` y `Start`**.
- **El orden de los argumentos.** En un híbrido son `[0]=web, [1]=endpoint, [2]=dll`. Poner el dll
  en `[0]` parece natural —es lo que contesta— pero `WebModuleHost.Open` lee `[0]` como la carpeta
  del bundle e intentaría servir un `.dll` como carpeta, fallando lejos de ahí. **El consumidor
  nuevo es el que se acomoda.**

Y una del método, no del código: **MIUI inunda el logcat** (31 721 líneas en una corrida). Lo que
destrabó el diagnóstico fue **hacer que la pantalla reportara el resultado con su milisegundaje**:
230 ms = bloqueado antes de la red, 1003 ms = hubo ida y vuelta y falló adentro, 517 ms = bien.

---

## Lo que NO resuelve

**La objeción de tienda, entera.** Un DLL de la institución ejecutándose nativo es código que llega
de afuera. El híbrido hereda eso sin cambios.

**Corregido el 2026-09-16** con los textos en la mano —están citados literales en
[`B11`](../../B-arquitectura/B11-lo-que-dicen-las-tiendas.md)—, porque este párrafo decía algo que
ya no es cierto:

- **Google Play**: la palabra que decide no es «JavaScript» sino **«indirect»**. El WebView da
  acceso *indirecto* a las APIs de Android y está permitido; un DLL cargado por Mono en el proceso
  de Gnothi da acceso *directo* y no lo está. Contra ese texto, **el híbrido no pasa.**
- **Apple**: ya **no** nombra WebKit ni JavaScriptCore. Dice *«interpreted code»* con tres
  condiciones, y una de ellas —no crear un escaparate para otras aplicaciones— le pega a Gnothi
  entero, no a esta forma.
- **Y la hospedada tampoco está limpia**: el §4.7.2 de Apple prohíbe exponerle APIs nativas al
  software que no viene en el binario sin permiso previo, que es exactamente lo que hace el puente.

Y el trilema de `B7` sigue en pie, con el matiz de que **ninguna de las tres patas está limpia en
iOS**: **cargo** no la acepta la tienda, **compilado adentro** no deja que la institución entregue
sin republicar Gnothi, y **web en WebView** pasa el texto de Google pero choca con el 4.7.2 de
Apple. El híbrido es cargo con pantalla: mejora el peso y el dueño único, no la política.

---

## Un actor, no dos — cerrado

Esto era el pendiente grande, y se cerró. Queda escrito porque el razonamiento vale más que el
arreglo.

Las consultas usaban un actor `ReadOnly` y la entrega un `ExamenesApp`, y el `Read()` **rehidrataba
en cada consulta**. Ese diseño venía del mundo A2/A3 y su propio comentario lo decía: *«the
installed app writes to this folder while this process lives»*. **En el híbrido no hay app externa:
Gnothi es el único dueño**, que era el punto de la forma.

El síntoma era este, y aparecía **solo en la tabla**, justo después de que la entrega escribiera:

```
FileNotFoundException: Temp file not found: …/files/journals/examenes/…
```

Un `.tmp` que ya se había renombrado cuando el lector fue a buscarlo — con exactitud, **el mismo
problema que el modo lector del motor existe para resolver**: dos actores sobre un journal, uno
escribiendo y otro leyendo. Y encima cargando y descargando el ensamblado por llamada, con un
`Unload` que es **cooperativo** y no completa si quedaron referencias vivas.

**El arreglo son dos cosas**, y la pregunta que las destrabó fue: *«¿por qué deberían ser dos
actores cuando podría ser uno, si la superficie solo expone tools para afuera?»*

1. **Un solo actor vivo**, `static ExamenesApp` bajo `lock`, compartido por las tres puertas.
2. **`keepLoaded: true`** en el `InProcessShell` de la cáscara híbrida: la cara pregunta varias veces
   seguidas mientras la persona mira, y recibe entregas en el medio.

Lo que el split compraba era **menos de lo que parecía**: `ReadOnly` y `ExamenesApp` son el mismo
`PerformanceV2` sobre el mismo dominio, así que *«preguntar nunca escribe»* nunca fue estructural —
lo único estructural era **no emitir la semilla**. Y las escrituras ya están cerradas **por las
puertas**: `[Deliver]` lo busca la cáscara por su marcador, no está en el manifiesto, y `consultar`
solo alcanza `[View]`.

Y ahí apareció el matiz que dio vuelta la conclusión: **la semilla es obligatoria**. Un actor con
`OnHydrated` vacío nunca la emite, y la semilla **es lo que crea los globales** `expediente` y
`bitacora` que el DSL direcciona. Sin ella, lo primero que hace el bucle —preguntar el cursor por
`bitacora.UltimoEvento`— muere con:

```
Unknown property or method 'UltimoEvento' on type 'Object'
```

Estaba advertido en `ExamenesApp` —*«la vista de solo lectura del MCP rehidrataba y no encontraba el
global»*— y hubo que medirlo en el teléfono para creerlo. Sobre un journal ya sembrado el actor único
no escribe nada: la semilla se recrea **replicando el journal**, no emitiéndola.

---

## Pendiente

- **`--pack` no conoce la tercera forma**, así que el híbrido se arma con `Compress-Archive` y pierde
  la inspección temprana. La del alta sí corre —está verificado—, pero la garantía se debilitó.
- **La latencia de `consultar` sin medir** contra los **153 ms** de la puerta Binder, que es el
  número que la lámina 04 necesita.
- **El badge que MIUI no pone.** Si en la pantalla de inicio nada dice que el icono es Gnothi, hay
  que decidir si eso se acepta o se compensa en otro lado — por ejemplo en la primera apertura desde
  el icono.
- **La guía para instituciones describe dos formas.** Ahora hay tres, más `[View]`, `[Deliver]` y
  `consultar`.
- **El feed de NuGet devuelve 403**, así que todo se arma con `-Motor 3.0.0-beta.10029` (la rama
  pide la `10044`). El parámetro `-Motor` de `armar-bundle.ps1` nació de ahí: exporta
  `$env:VersionDelMotor`, que MSBuild toma como propiedad para los tres builds —el módulo, y el Host
  dos veces, para empaquetar y para firmar— sin tocar las líneas largas, que están escritas enteras
  a propósito (splatear con `@` en PowerShell 5.1 reparte una cadena letra por letra).
