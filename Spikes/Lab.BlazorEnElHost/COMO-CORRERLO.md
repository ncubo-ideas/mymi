# Lab.BlazorEnElHost — cómo correrlo

**La pregunta, en una línea:** ¿se puede hospedar un `Renderer` de Blazor desde una Activity de
Android, **sin MAUI**, empujando los diffs por el puente que Gnothi ya tiene?

Si la respuesta es sí, hay una **cuarta forma de módulo** que borra el costo de reescribir la UI.
Si es no, se cierra igual que se cerró la caja wasm en el teléfono.

---

## Qué mide y qué NO mide

**No mide bytes, y conviene decirlo antes de que alguien lea los números al revés.** La cara de
compras pesa **13 KB** de html+js; llevarla a cero deja el bundle igual, porque su peso son las
fotos (1 348 KB) y el CSS. La forma híbrida ya ganó la pelea del tamaño.

**Mide otra cosa: el costo de reescribir.** Portar la pantalla de razor a JS costó **−21 % de
líneas** en concesionario, y ése es el número honesto — en los otros módulos el ahorro venía
mezclado con trabajo que desapareció (el elegidor de carpeta, los permisos, los avisos de
migración). Si el Host corre los componentes de la institución, ese costo es **cero**.

**Y no contesta la objeción de fondo.** Hoy `[View]` es una puerta angosta: cuatro métodos
marcados, strings de ida y vuelta. Un `Renderer` hospedado es una puerta ancha — los **manejadores
de eventos** de la institución correrían nativos, en el proceso de Gnothi, con el framework a mano.
Es el trilema de `B7` otra vez, peor que el híbrido. **Este spike contesta si se PUEDE, no si se
DEBE.**

---

## Correrlo

    cd Spikes/Lab.BlazorEnElHost
    dotnet build -c Release
    adb install -r bin/Release/net9.0-android/com.ncubo.gnothi.spike.blazorenelhost-Signed.apk
    adb logcat -s SPIKE:I

Y en la pantalla: tocar el botón. El contador vive en `Cara/Contador.razor`, su `cuenta++` corre
**nativo**, y lo que cruza el puente es un lote de ediciones.

---

## Lo que ya está contestado — sin teléfono

**[MEDIDO 2026-09-16]** con `dotnet build`, que es la mitad del spike que no necesita aparato.

| pregunta | respuesta |
|---|---|
| ¿`Microsoft.AspNetCore.Components` restaura para `net9.0-android`? | **sí**, sin tocar nada |
| ¿compila un `.razor` en un proyecto de Android, sin MAUI? | **sí**, con `Microsoft.NET.Sdk.Razor` |
| ¿se puede subclasear `Renderer` y montar un componente? | **sí** — `AssignRootComponentId` + `RenderRootComponentAsync` |
| ¿cuántos warnings `IL` agrega? | **cero** |
| ¿hace falta un contenedor de inyección? | **no** — un `IServiceProvider` que devuelve `null` alcanza |

**Los cero warnings `IL` son el dato que más sorprende**, porque `Lab.Aot` existe en este repo
justamente para contarlos: hoy son 9 y los 9 están en `InProcessShell`. El renderizador de Blazor
no agrega ninguno. (Ojo: eso dice que el *analizador* no se queja, no que corra bajo AOT completo
en iOS — eso lo tapa el intérprete de Mono, igual que el resto.)

### El choque entre los dos SDK, que es un hallazgo del spike

El SDK de Razor arma un manifiesto de *static web assets* y el de Android compila **por RID**
(`android-arm64`, `android-x64`), así que el manifiesto se busca en una carpeta por arquitectura
donde nunca se escribió:

    error : Manifest file at 'obj\Debug\net9.0-android\android-arm64\staticwebassets.build.json' not found.

Se apaga con `<StaticWebAssetsEnabled>false</StaticWebAssetsEnabled>` y ya. Este camino **no sirve
`wwwroot`**: los `.razor` se compilan a C# y el html lo pone la Activity desde `Assets`. Lo que se
usa del SDK de Razor es el **compilador**, no su canalización de archivos estáticos.

### El peso

| | |
|---|---|
| `Microsoft.AspNetCore.Components.dll` (IL, del paquete) | **342 KB** |
| el mismo, AOT y recortado dentro del apk (`arm64-v8a`) | **30,5 KB** |
| apk Release del spike entero | 9,2 MB |

**30,5 KB es lo que el Host cargaría una vez para todos los módulos.** Comparado con los 4,3 MB de
runtime wasm que hoy trae *cada* módulo hospedado, y con los 342 KB que uno esperaría del IL: el
trimmer se lleva el 91 % porque de todo el paquete sólo se usa el árbol de render.

---

## LO QUE CONTESTÓ EL TELÉFONO — **2026-09-16**, Xiaomi 2407FPN8EG, Android 16, Release

**Las tres preguntas dieron que sí.**

| | |
|---|---|
| ¿arranca un `Renderer` propio en Mono/Android, sin MAUI? | **sí** |
| primer render | **44 ms** (59 y 70 ms en las corridas previas) |
| primer lote | **542 bytes**, 15 marcos, aplicado en **0,7 ms** |
| ¿vuelve el evento y corre el manejador nativo? | **sí** |
| ida y vuelta del evento + re-render | **12 ms** el primero, **0 ms** los siguientes |
| lote de un re-render | **~360 bytes, 2 marcos**, aplicado en **0,7–0,8 ms** |

**El lote incremental es lo que había que ver con los ojos.** El primero trae el árbol entero —
15 marcos —; los siguientes traen **dos**: el número y la palabra que cambiaron.

```
{"marcos":[{"t":"x","v":"2"},{"t":"x","v":"toques"}],
 "componentes":[{"id":0,"ediciones":[
   {"t":"StepIn","i":0},{"t":"StepIn","i":2},{"t":"StepIn","i":1},
   {"t":"UpdateText","i":0,"ref":0},{"t":"StepOut"},
   {"t":"UpdateText","i":3,"ref":1},{"t":"StepOut"},{"t":"StepOut"}]}]}
```

Eso es lo que haría barato el mecanismo aunque la pantalla sea grande, y ahora está medido en vez
de supuesto.

### La memoria, contra la forma híbrida

| | PSS |
|---|---|
| spike entero, con el componente pintado | **137,9 MB** |
| Gnothi solo, sin ningún módulo abierto | 238,7 MB |
| Gnothi + el primer módulo híbrido | 297,5 MB (**+58,8**) |

No son comparables de frente — el spike es una app sola y Gnothi carga su propia cara, el
asistente y el catálogo. Lo comparable es el **delta por módulo**, y ése todavía no se midió: hace
falta un segundo componente montado en el mismo proceso. **Queda pendiente.**

### El peso, corregido

| | |
|---|---|
| `Microsoft.AspNetCore.Components.dll` (IL, del paquete) | 342 KB |
| el mismo, AOT y recortado dentro del apk (`arm64-v8a`) | **35,2 KB** |
| `Microsoft.AspNetCore.Components.Web.dll`, ídem | **3,8 KB** |
| **total que el Host cargaría, una vez para todos los módulos** | **39 KB** |
| apk Release del spike entero | 9,2 MB |

### EL HALLAZGO QUE NO ESTABA PREVISTO: el núcleo solo no alcanza

La primera corrida en el teléfono **compiló, pintó, y el botón no hacía nada.** Ningún error, en
ningún lado. El lote decía por qué:

```json
{"t":"m","v":"<button @onclick=\"Sumar\">Tocar</button>"}
```

El botón llegó como **markup literal**, con el `@onclick` sin compilar. Con sólo
`Microsoft.AspNetCore.Components`, el compilador de Razor **no sabe que `onclick` es un evento del
DOM** — eso lo define `Microsoft.AspNetCore.Components.Web` — así que emitió el elemento entero
como texto. Agregando ese paquete (y su `@using` en `_Imports.razor`) el mismo marco pasa a:

```json
{"t":"e","n":"button","largo":3},{"t":"a","n":"onclick","v":"System.Action","ev":1}
```

**Falla en silencio, que es lo peor que tiene**, y es la tercera vez esta semana que aparece la
misma forma de error: el visto verde del concesionario, el `undefined` del color de la orina, y
esto. Un dato que no llega se ve igual que un dato que dice «todo bien».

---

## SEGUNDA VUELTA — montar una pantalla REAL · **2026-09-16**

La primera vuelta probó el mecanismo con un contador de 11 líneas. La pregunta que quedaba era la
que importa: **¿se puede reusar la app Blazor que ya existe?**

Se montó `Compras.Wasm/Pages/MisCompras.razor` — **1 798 líneas, 7 rutas, 3 inyecciones** —
compilada **desde el repo de la institución, sin tocar una línea**, por `Content Include`.

### Lo que salió

Con el journal real de compras copiado adentro — 66 KB, 87 compras, sacado de Gnothi con
`run-as … tar c` y metido con `run-as … tar x`:

| | |
|---|---|
| ¿compila el razor de la institución tal cual? | **sí**, las 1 798 líneas |
| ¿monta, inyecta y corre su ciclo de vida? | **sí** |
| ¿las 14 lecturas del dominio contestan? | **sí**, todas |
| **¿pinta, con datos reales?** | **SÍ** |
| primer render | **755 ms** |
| **primer lote** | **30 804 bytes** |

En pantalla, leído de su propio journal: *«El tomate que comprás siempre bajó de precio ₡1.150 →
₡1.060»*, *«87 compras tuyas desde enero de 2025 · ₡3.217.096»*, *«Pechuga de pollo kg CAMBIÓ 1
vez/semana → 1 vez/mes»*, *«Tus 3 categorías principales representan el 51,9 %»*.

**30 804 bytes contra los 542 del contador.** Es la medida que faltaba: una pantalla de 1 798
líneas cuesta 30 KB en su primer lote. No es gratis, pero es lo que pesa **una vez**, al abrir; los
re-renders siguen siendo las dos o tres ediciones que cambiaron.

Sale **sin CSS**, y no es un defecto del mecanismo: la página del spike no carga la hoja de La
Guaria. Y se ven dos fallas **de mi aplicador de 161 líneas**, no del renderizador: `True` impreso
donde van las fotos — un atributo booleano de .NET que mi `atributo()` no traduce — y textos
pegados sin espacio (`Otros₡97.080`). Trabajo conocido, no incógnita.

### EL SEGUNDO HALLAZGO QUE COSTÓ UNA HORA: montar por tipo, no por instancia

Antes de esto, la pantalla reventaba con `NullReferenceException` adentro de
`MisCompras.BuildRenderTree` — **con la pila apuntando al archivo de la institución**, que es donde
uno no va a buscar el error. Con el journal vacío y con el journal lleno, igual.

La causa: `Montar` recibía un `IComponent` ya construido con `new`. Compila, monta, y **todos los
`@inject` quedan en null** — la inyección por propiedad la hace
`ComponentFactory.InstantiateComponent`, y un componente instanciado a mano nunca pasa por ahí.

Se arregla montando por `Type` y pidiéndole la instancia a `InstantiateComponent`, que es
`protected` en la clase base: que lo sea era la pista de que el camino previsto es ése.

**Es, otra vez, la forma de falla de toda la semana:** nada avisa, y lo que se ve culpa al lugar
equivocado. Lo que lo destrabó fue instrumentar el andamio para ver **cuál** de las 14 lecturas
devolvía null — y la respuesta fue «ninguna, no se llamó a ninguna».

### LA MEDIDA QUE SE VINO A BUSCAR: cuánto andamio

`MisCompras.razor` está tipado contra `EstadoDeLaApp`, que es una clase **`sealed`** del proyecto
Wasm. No se puede implementar una interfaz: **hay que reponer la clase**. Los 38 miembros que la
pantalla usa se parten casi en mitades exactas:

| | cuántos | qué son en esta forma |
|---|---:|---|
| lecturas del dominio | **19** | una línea cada una, contra el mismo `ComprasAPI` que usa la superficie híbrida |
| el problema del navegador | **19** | **constantes** — `EstaSoportado`, `OpfsSoportado`, `RecuperarCarpeta`, `PedirPermiso`, `MotivoSinAlmacenamiento`, `ReengancharModo`, `FaltaReotorgarPermiso`, `ConectarCarpeta`, `UsarAlmacenamientoDelNavegador`… |

**La mitad de la API contra la que está escrita la pantalla existe para resolver un problema que
esta forma no tiene** — la misma razón por la que la forma híbrida borró `ElArranque.razor` entero.
Y la pantalla las nombra igual, porque está acoplada **por tipo**.

Los últimos cuatro errores de compilación fueron, literalmente, `RecuperarCarpeta`,
`ConectarCarpeta`, `UsarAlmacenamientoDelNavegador` y `PedirPermiso`.

### El andamio, contado

| | líneas |
|---|---:|
| `Andamio/EstadoDeLaApp.cs` — la clase repuesta + `ConfiguracionDeLaInstitucion` + `ResultadoDeCarpeta` | **148** |
| `Andamio/NavegadorDelHost.cs` — el `NavigationManager` | **33** |
| `ServiciosDelHost` en `MainActivity` — el contenedor | **11** |
| **total, para UNA pantalla de UN módulo** | **192** |

Contra las **1 798 líneas** que no hubo que reescribir. Es una relación de **1 a 9**, y la parte
buena es que el `NavigationManager` y el contenedor son del **Host** — se escriben una vez para
todas las instituciones. Lo que se repite por módulo es el `EstadoDeLaApp`.

### Y tres cosas que corrigen lo que se dijo en la primera vuelta

**1 · «No hace falta inyección» era mentira de un componente de juguete.** El contador no inyectaba
nada; `MisCompras` inyecta tres cosas y el `Renderer` las busca por el `IServiceProvider`.

**2 · `NavigationManager` es abstracta y no trae implementación.** En Wasm la pone el runtime del
navegador, en Blazor Server el circuito. Acá no hay ninguno de los dos. La del spike **no navega**:
anota a dónde le pidieron ir. Qué significa una ruta cuando el módulo es una pantalla adentro de
Gnothi es una **decisión de producto**, y el spike la deja a la vista en vez de resolverla a
escondidas.

**3 · El namespace es del que compila, no del que escribe.** `MisCompras` vive en
`Compras.Wasm.Pages` en su repo y acá quedó en `Lab.BlazorEnElHost`, porque lo decide el
`_Imports.razor` de este proyecto.

### La frase honesta, que reemplaza a la de la primera vuelta

No es *«se usa la app Blazor que ya existe»*. Es:

> **se reusan los componentes; hay que reponer el andamio** — el estado, el navegador y el
> contenedor — **y la mitad de ese andamio son constantes que contestan por un problema que esta
> forma no tiene.**

---

## Lo que falta, y se mide con el teléfono

Las tres preguntas que el `logcat` contesta, con el tag `SPIKE`:

1. **¿arranca el `Renderer` en Mono/Android?** → `primer render: N ms`
2. **¿llega el lote con contenido usable?** → `lote de N bytes`, y en pantalla el log de la página
   dice cuántos marcos trajo y en cuánto los aplicó
3. **¿vuelve el evento, corre el manejador nativo y re-renderiza?** → `evento N y su re-render: N ms`

Y la cuarta, que es la que decide si compite:

4. **¿cuánta memoria?** → `dumpsys meminfo com.ncubo.gnothi.spike.blazorenelhost`, contra los
   **45,6 MB** que cuesta el primer módulo en la forma híbrida
   (`Documentacion/Jonathan/F-evidencia/F1-mediciones.md`).

### Qué sería «que sí»

- El contador se pinta y el botón suma.
- El **primer** lote trae el árbol entero; los siguientes traen **tres o cuatro ediciones**. Eso es
  lo que haría barato el mecanismo aunque la pantalla sea grande, y es lo que hay que ver con los
  ojos en el log de la página, no suponerlo.

### Qué sería «que no»

- El `Renderer` no arranca — reflexión que Mono no resuelve, o el `Dispatcher` peleando con el
  hilo de UI de Android.
- El re-render tarda más que los ~150 ms de la puerta del Binder, y entonces no compite con
  `consultar`.
- La memoria no baja de lo que hoy cuesta la forma híbrida, y entonces el mecanismo no compra nada
  que no tengamos.

---

## Lo que el aplicador NO hace, y es a propósito

`Assets/cara.html` aplica las ediciones que el árbol de render produce de verdad —
`PrependFrame`, `RemoveFrame`, `SetAttribute`, `RemoveAttribute`, `UpdateText`, `UpdateMarkup`,
`StepIn`, `StepOut` — y **le falta el bloque de permutaciones**, que es reordenamiento de listas
con `@key`. Cuando llegue una, lo dice en el log en vez de ignorarla.

Es a propósito: un spike que se come lo que no entiende **contesta que sí por no haber mirado**. Es
la misma lección del visto verde en la cara del concesionario, del 2026-09-14.

Tampoco hay componentes anidados: un marco de tipo `Component` deja un ancla y sus marcos llegan en
su propio diff. Para el contador alcanza; para una pantalla de verdad no, y eso es trabajo conocido,
no una incógnita.

---

## Los dos precedentes de este repo, uno de cada lado

**A favor:** `BlazorWebView` **ya corre en Gnothi** — `Gnothi.Desktop` es Blazor Hybrid (WinForms +
`BlazorWebView`), con los componentes razor nativos y las queries yendo al Dispatcher. Ver `B5`,
`C7` y `D1`. El mecanismo no es teórico.

**En contra:** la **caja wasm** (`F5`, columna B de `R9`) se probó en escritorio con `wasmtime`, el
mismo `.gmod` contestó byte-idéntico dentro y fuera, y **nunca llegó al teléfono**. Que un
mecanismo funcione en un monitor no dice nada de Android — es literalmente lo que pasó con
`AssemblyDependencyResolver`, que en escritorio anda y en Android tira `PlatformNotSupportedException`
(ver el comentario largo de `InProcessShell.Start`).

Este spike existe para no repetir ninguna de las dos historias a medias.
