# F7 · La cuarta forma — Blazor corriendo en el Host

**2026-09-16.** Xiaomi 2407FPN8EG, Android 16. El spike es `Spikes/Lab.BlazorEnElHost`.

**La pregunta, en una línea:** ¿se puede hospedar un `Renderer` de Blazor desde una Activity de
Android, **sin MAUI**, empujando los diffs por el puente que Gnothi ya tiene?

**La respuesta: sí.** Y la pantalla real de una institución —`MisCompras.razor`, 1 798 líneas—
corrió nativa, leyó su propio journal y se pintó **igual que su app suelta**.

---

## 1 · De dónde salió la pregunta

La forma híbrida ya ganó la pelea del peso: municipalidad pesa **74 KB** contra **12,1 MB** de la
hospedada. Pero cobra un precio que está medido: **portar la pantalla de razor a JS costó −21 % de
líneas en concesionario**, y ése es el número honesto — en los otros módulos el ahorro venía
mezclado con trabajo que desapareció (el elegidor de carpeta, los permisos, los avisos de
migración).

Si el Host corre los componentes de la institución, ese costo baja. **Cuánto baja es lo que este
spike fue a medir.**

**Y no mide bytes.** La cara de compras pesa 13 KB de html+js; llevarla a cero deja el bundle
igual, porque su peso son las fotos (1 348 KB) y el CSS. Quien lea esto buscando ahorro de tamaño
está leyendo el documento equivocado.

---

## 2 · Lo que dio, medido

### Primera vuelta — el mecanismo, con un contador de 11 líneas

| | |
|---|---|
| ¿`Microsoft.AspNetCore.Components` restaura para `net9.0-android`? | **sí**, sin tocar nada |
| ¿compila un `.razor` en Android, sin MAUI? | **sí**, con `Microsoft.NET.Sdk.Razor` |
| ¿se puede subclasear `Renderer` y montar? | **sí** |
| warnings `IL` que agrega | **cero** |
| primer render | **44 ms** |
| primer lote | **542 bytes**, 15 marcos, aplicado en **0,7 ms** |
| evento + re-render | **12 ms** el primero, **0 ms** los siguientes |
| lote de un re-render | **~360 bytes, 2 marcos** |

**Los cero warnings `IL` son el dato que menos se esperaba**, porque `Lab.Aot` existe en este repo
para contarlos y hoy son 9, los 9 en `InProcessShell`.

**El lote incremental es lo que había que ver con los ojos.** El primero trae el árbol entero; los
siguientes traen sólo lo que cambió:

```
{"marcos":[{"t":"x","v":"2"},{"t":"x","v":"toques"}],
 "ediciones":[…StepIn…UpdateText…StepOut…]}
```

### Segunda vuelta — la pantalla real

`Compras.Wasm/Pages/MisCompras.razor`, compilada **desde el repo de la institución sin tocar una
línea**, con el journal real de compras copiado adentro (66 KB, 87 compras).

| | |
|---|---|
| ¿compila el razor tal cual? | **sí**, las 1 798 líneas |
| ¿monta, inyecta y corre su ciclo de vida? | **sí** |
| ¿las 14 lecturas del dominio contestan? | **sí**, todas |
| **¿pinta con datos reales?** | **SÍ** |
| **¿se ve como su app suelta?** | **SÍ**, con la hoja de la institución cargada |
| primer render | **664–755 ms** |
| **primer lote** | **30 782 bytes** |

En pantalla, leído de su propio journal: *«El tomate que comprás siempre bajó de precio ₡1.150 →
₡1.060»*, *«87 compras tuyas desde enero de 2025 · ₡3.217.096»*, *«Pechuga de pollo kg CAMBIÓ 1
vez/semana → 1 vez/mes»*, *«Tus 3 categorías principales representan el 51,9 %»*.

### El peso del renderizador

| | |
|---|---|
| `Microsoft.AspNetCore.Components.dll` (IL, del paquete) | 342 KB |
| el mismo, AOT y recortado dentro del apk (`arm64-v8a`) | **35,2 KB** |
| `Microsoft.AspNetCore.Components.Web.dll`, ídem | **3,8 KB** |
| **total que el Host cargaría, una vez para todos los módulos** | **39 KB** |

Contra los **4,3 MB** de runtime wasm que hoy trae **cada** módulo hospedado. El trimmer se lleva
el 91 % porque de todo el paquete sólo se usa el árbol de render.

---

## 3 · El inventario del andamio — que es la respuesta de verdad

`MisCompras.razor` está tipado contra `EstadoDeLaApp`, que es una clase **`sealed`** del proyecto
Wasm. **No se puede implementar una interfaz: hay que reponer la clase.** Sus 38 miembros se parten
casi en mitades exactas:

| | cuántos | qué son en esta forma |
|---|---:|---|
| lecturas del dominio | **19** | una línea cada una, contra el mismo `ComprasAPI` que usa la superficie híbrida |
| el problema del navegador | **19** | **constantes** — `EstaSoportado`, `OpfsSoportado`, `RecuperarCarpeta`, `PedirPermiso`, `MotivoSinAlmacenamiento`, `ReengancharModo`, `FaltaReotorgarPermiso`, `ConectarCarpeta`, `UsarAlmacenamientoDelNavegador`… |

**La mitad de la API contra la que está escrita la pantalla existe para resolver un problema que
esta forma no tiene** — la misma razón por la que la forma híbrida borró `ElArranque.razor` entero.
Y la pantalla las nombra igual, porque está acoplada **por tipo**.

Los últimos cuatro errores de compilación fueron, literalmente, `RecuperarCarpeta`,
`ConectarCarpeta`, `UsarAlmacenamientoDelNavegador` y `PedirPermiso`.

### Lo que hubo que escribir

| pieza | líneas | ¿se repite por módulo? |
|---|---:|---|
| `EstadoDeLaApp` repuesto (+ `ConfiguracionDeLaInstitucion`, `ResultadoDeCarpeta`) | **148** | **sí** |
| `NavegadorDelHost` — el `NavigationManager` | 33 | no, del Host |
| el contenedor de inyección | 11 | no, del Host |
| `RenderizadorAlPuente` — el `Renderer` | 173 | no, del Host |
| el aplicador de diffs (`cara.html`) | 161 | no, del Host |
| **lo que se repite por módulo** | **148** | |
| **lo que el Host escribe una vez** | **378** | |

Contra **1 798 líneas de razor que no se tocaron**. Por módulo, **1 a 12**.

Y la hoja de la institución —`css/`, `lib/`, `fuentes/`, `brand/`, `assets/`, 1,8 MB— viaja igual
que hoy: los `.razor` traen las **clases**, no el CSS.

---

## 4 · CUATRO FALLAS EN SILENCIO, Y ES EL HALLAZGO QUE MÁS VALE

Las cuatro compilaron, corrieron, y **no dijeron nada**. Las cuatro se vieron mirando la pantalla.

### 4.1 · El `@onclick` que llegó como texto

Primera corrida: compiló, pintó, **el botón no hacía nada**. El lote decía por qué:

```json
{"t":"m","v":"<button @onclick=\"Sumar\">Tocar</button>"}
```

Markup literal. Con sólo `Microsoft.AspNetCore.Components`, el compilador de Razor **no sabe que
`onclick` es un evento del DOM** — eso lo define `Microsoft.AspNetCore.Components.Web`.

### 4.2 · Montar por instancia deja todos los `@inject` en null

`Montar` recibía un `IComponent` construido con `new`. Compila, monta, y **la inyección por
propiedad nunca ocurre**: la hace `ComponentFactory.InstantiateComponent`, y un componente
instanciado a mano no pasa por ahí.

El síntoma era el peor posible: `NullReferenceException` adentro de `MisCompras.BuildRenderTree`,
**sin número de línea y con la pila apuntando al archivo de la institución** — que es donde uno no
va a buscar. Costó una hora. Lo destrabó instrumentar el andamio para ver **cuál** de las 14
lecturas devolvía null, y la respuesta fue *«ninguna, no se llamó a ninguna»*.

`InstantiateComponent` es `protected` en la clase base. Que lo sea era la pista.

### 4.3 · Un `bool` no es la cadena «True»

El valor de atributo salía por `ToString()`, así que `aria-hidden="true"` llegaba como `"True"` y
un atributo en `false` llegaba como `"False"` — **que en HTML es verdadero**. Blazor distingue:
`false` y `null` **quitan** el atributo, `true` lo pone vacío.

### 4.4 · El envoltorio del layout

`MisCompras` es la página; el `<main class="container py-4">` es de `MainLayout.razor`. Sin él, la
portada —que lleva `margin: -1.5rem -.75rem` para sangrar ese padding— se tira afuera de la
pantalla.

**Es exactamente el mismo defecto que la cara JS de exámenes con la `.hoja` que faltaba** (ver
`E4`). El markup de una institución tiene supuestos sobre su envoltorio, y copiar la sección sin él
copia los márgenes negativos sin lo que cancelan.

### Y el patrón

Estas cuatro, más las tres de `E4`, son **siete en tres días con la misma forma**:

> **Un dato que no llega se ve exactamente igual que un dato que dice «todo bien».**

Ninguna la detectó una prueba. Las siete se vieron mirando la pantalla.

---

## 5 · Dos choques de herramientas, anotados

**El SDK de Razor contra el multi-RID de Android.** El de Razor arma un manifiesto de *static web
assets*; el de Android compila **por RID**, así que lo busca en una carpeta por arquitectura donde
nunca se escribió:

    error : Manifest file at 'obj\Debug\net9.0-android\android-arm64\staticwebassets.build.json' not found.

Se apaga con `<StaticWebAssetsEnabled>false</StaticWebAssetsEnabled>`. Este camino **no sirve
`wwwroot`**: usa el **compilador** de Razor, no su canalización de archivos.

**MIUI rechaza instalaciones nuevas por USB** (`INSTALL_FAILED_USER_RESTRICTED`) y las acepta al
segundo intento. Una actualización (`install -r`) entra siempre.

**Y el namespace es del que compila, no del que escribe:** `MisCompras` vive en
`Compras.Wasm.Pages` en su repo y acá quedó en `Lab.BlazorEnElHost`, porque lo decide el
`_Imports.razor` del proyecto que lo compila.

---

## 6 · Lo que el spike NO contestó

**El router.** `MisCompras` tiene 7 rutas; compras y municipalidad tienen 7 cada una. El
`NavigationManager` del spike **no navega**: anota a dónde le pidieron ir. Qué significa una ruta
cuando el módulo es una pantalla adentro de Gnothi es una **decisión de producto**, no un detalle
de implementación.

**`IJSRuntime`.** No se implementó. Esta pantalla no lo usa en su camino feliz; otras sí. Medido
sobre los cinco módulos, casi todo lo que llaman a JS es el almacenamiento —`leer`, `escribir`,
`listar`, `pull`, `permisoActual`, `opfsSoportado`…— que es **justo lo que esta forma borra**. Lo
que queda afuera de esa familia es corto: `compartir`, `exportarJournal`, `esAngosta`,
`appearance.*`, las credenciales.

**La memoria por módulo.** El spike entero da **137,9 MB** de PSS, pero no es comparable de frente
con los 297,5 MB de Gnothi + un módulo híbrido: el spike es una app sola. Lo comparable es el
**delta por componente montado**, y para eso hace falta un segundo componente en el mismo proceso.

**Componentes anidados.** Un marco de tipo `Component` deja un ancla; sus marcos llegan en su
propio diff. Para esta pantalla alcanzó; para una con subcomponentes no.

**El bloque de permutaciones** (`@key`). El aplicador no lo tiene, y cuando llegue uno **lo dice en
el log** en vez de ignorarlo.

---

## 7 · La objeción que no se movió

Ese `Releer()` con sus 18 lecturas **corrió nativo, en el proceso**, y su `cuenta++` también.

Hoy `[View]` es una **puerta angosta**: cuatro métodos marcados, strings de ida y vuelta, y el
Host decide qué entra por cada una (ver `B8`). Un `Renderer` hospedado es una **puerta ancha**: los
manejadores de eventos de la institución corren con el framework a mano.

Es el trilema de `B7` otra vez, **peor que el híbrido**. Y se agrava con lo de `B9`: hoy el
ensamblado no se suelta nunca, y acá además tendría el estado de la UI adentro.

**Este spike contesta si se PUEDE. No contesta si se DEBE.**

---

## 8 · Las cuatro formas, como quedan hoy

| | trae la UI | runtime propio | quién pinta | peso típico | contención |
|---|---|---|---|---|---|
| **cargo** | no | no | nadie (sin pantalla) | 188 KB | firma + puertas marcadas |
| **hospedada** | sí | **10 MB** | el módulo, en su WebView | 12–14 MB | origen propio + CSP |
| **híbrida** | sí, reescrita a JS | no | el WebView | 74 KB – 1,5 MB | puertas marcadas + CSP |
| **la cuarta** | sí, **su razor tal cual** | no | **el Host** | ≈ híbrida | **la más débil** |

La cuarta no gana peso sobre la híbrida. Gana **que la institución no reescribe su pantalla**.
Paga con contención.
