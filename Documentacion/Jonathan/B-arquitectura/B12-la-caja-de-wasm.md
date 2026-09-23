# B12 · La caja de wasm — qué es, y por qué una caja y no una regla

**El porqué, el tablero y los pasos están en [`F2` · `P19`](../F-evidencia/F2-estado-y-pendientes.md);
lo medido, en [`F8`](../F-evidencia/F8-el-paso-0-de-la-caja-de-wasm.md).** Este documento contesta
sólo una pregunta: **qué es la caja**, porque se confunde con una máquina virtual, con un contenedor
y con «el módulo compilado a wasm», y no es ninguna de las tres.

**En una línea:** una **instancia de wasm** que Gnothi crea por módulo, dentro de su propio proceso,
cuyo único contacto con el mundo son las funciones que Gnothi le pasa.

---

## Las capas, que son tres y se mezclan

```
Gnothi (proceso .NET de Android)
 └─ libwasmtime.so            ← el MOTOR de wasm. Librería nativa, 27 MB en arm64.
     └─ una INSTANCIA         ← LA CAJA. Una por módulo. Gnothi puede tener varias vivas.
         ├─ memoria lineal    ← un arreglo plano de bytes. Es TODO su mundo direccionable.
         ├─ imports           ← las únicas funciones que puede llamar hacia afuera
         └─ adentro corre:
             dotnet.wasm      ← el runtime de .NET compilado a wasm (12,5 MB)
              └─ Examenes.App.dll + Dominio + Eventos + Portador   (227 KB)
```

**«La caja» es la instancia, no wasmtime.** wasmtime es quien las crea y las ejecuta. El 2026-09-16
se midieron **tres vivas a la vez** en el mismo proceso, cada una con su propio runtime de .NET
adentro.

**El motor, el BCL y el runtime los pone Gnothi**, una vez, para todos los módulos. De la institución
son los 227 KB del último renglón — **y la cara, que no sale en este diagrama porque no vive acá.**
Ver *«Dónde queda la cara»*, abajo.

---

## Por qué esto es una caja y no una comprobación

**Wasm no tiene syscalls.** No existe `open()`, no existe `socket()`, no existe `mmap()`. El código
adentro sólo sabe hacer aritmética sobre su propia memoria lineal **y llamar a las funciones que le
hayan entregado en la lista de imports**. No hay más verbos en el lenguaje.

**La contención, entonces, no es una política que alguien revisa: es una propiedad del modelo de
ejecución.** No hay nada que evadir porque no hay instrucción que exprese «salir».

Es la diferencia que `D2` pide y que hasta hoy no teníamos: una contención **falsable**, que se puede
intentar romper y verla fallar, en vez de una promesa que se sostiene porque el otro se porta bien.

## WASI es la lista de imports, y el permiso es una lista de carpetas

**WASI** —*WebAssembly System Interface*— es el conjunto estándar de funciones que un anfitrión le
puede pasar a una caja para que adentro exista algo parecido a un sistema operativo: archivos, reloj,
argumentos, salida estándar.

Es lo que hace que `File.ReadAllBytes` funcione adentro sin cambiar una línea del módulo: el BCL de
.NET llama a WASI, WASI lo implementa wasmtime, y **wasmtime hace sólo lo que su configuración
permite**. Esa configuración, entera, es esto:

```csharp
wasi_config_preopen_dir(wasi, paquete,   "/",      fsMutable: false);  // su código, no escribible
wasi_config_preopen_dir(wasi, concedida, "/datos", fsMutable: true);   // su journal
```

**Y no es un filtro de rutas.** No dice *«permitir `/datos` y bloquear el resto»*. Dice: *«al invitado
se le entregó un descriptor para esta carpeta»* — y no existe ningún otro descriptor en el universo
del invitado. Es un modelo de **capacidades**, no de reglas.

**Por eso el intento de leer la carpeta de otro módulo no devuelve «acceso denegado»** sino:

```
DirectoryNotFoundException: IO_PathNotFound_Path, /vecino/municipalidad
```

**Desde adentro, el journal del vecino no está prohibido: no existe.** Es exactamente la forma que
tienen las tres puertas de [`B8`](B8-la-puerta-de-View.md) —*lo del otro no le es negado, no
existe*— aplicada al sistema de archivos.

**La red se controla con la misma palanca**, y es una llamada aparte: `add_wasi_http` está separada
de `add_wasip2`. Un módulo al que no se la den no tiene por dónde salir. *(Hoy hay que dársela para
que el componente de .NET instancie, porque importa `wasi:http`; ver `F8`.)*

---

## Dónde queda la cara

**No se mueve. Sigue en el WebView, exactamente igual que en la forma híbrida.** La caja es sólo para
el dominio y la superficie.

**La primera versión de este documento no la mencionaba**, y eso estaba mal: transcribía el alcance
del spike —que compiló sólo el dominio y no tiene cara— como si fuera el del módulo. Un módulo real
en la caja lleva las dos cosas.

`HybridShell` son literalmente dos campos:

```csharp
private readonly InProcessShell surface;   // dominio + superficie
private readonly WebViewShell   face;      // la cara HTML/JS
```

`Invoke`, `Deliver`, `Cursor` y `View` van a `surface`; el resto a `face`. **La caja reemplaza
`surface`; `face` no se toca.**

**Y la cara no se entera, por diseño:**

```
hoy      cara (WebView) ──gnothiPuente.consultar──► Gnothi ──InProcessShell.View──► superficie (proceso de Gnothi)
caja     cara (WebView) ──gnothiPuente.consultar──► Gnothi ──────borde───────────► superficie (caja de wasm)
```

**Gnothi está en el medio en los dos casos.** La cara nunca le habla a la superficie: le habla a
Gnothi por el verbo `consultar`, y Gnothi decide dónde aterriza esa pregunta. Es la propiedad que
[`B8`](B8-la-puerta-de-View.md) describe —*«no nombra un módulo, a propósito»*—: como la cara nunca
supo dónde vive la superficie, mudarla le es invisible.

**Lo que el módulo trae, entonces, completo:**

| pieza | dónde corre | tamaño |
|---|---|---|
| `web/` — la cara | el WebView | **9 a 13 KB** de HTML+JS |
| dominio, superficie, eventos, portador | **la caja** | **227 KB** |
| el contrato y el borde | la caja | ~7 KB, **o prestados por el Host** |

**~240 KB**, contra los 12,4 MB de un bundle hospedado. *(En compras la cara pesa 1 553 KB, pero
1 348 son fotos: el código sigue siendo de ese orden.)*

### Y la pregunta abierta: la cuarta forma

En la forma de `F7` la cara no es HTML escrito a mano: son componentes `.razor`, y **el renderizador
corre en el .NET nativo de Gnothi**. Si el dominio se muda a la caja, el renderizador queda de un
lado y el dominio del otro.

Hay un indicio a favor, **sin probar**: el renderizador de `F7` **ya serializa el `RenderBatch` a
JSON** para empujarlo al WebView —542 bytes el primer lote del contador, 30 782 el de `MisCompras`,
~360 los re-renders—. Si ya cruza un borde, no debería importarle de qué lado del wasm corre: sería
un salto más, no un rediseño. **Es una suposición, no una medición.**

---

## Lo que la caja NO es

| no es | por qué importa la distinción |
|---|---|
| **una máquina virtual** (VirtualBox, una VM de nube) | no hay kernel, ni sistema operativo, ni hardware emulado, ni imagen que mantener |
| **un contenedor** (Docker) | no hay namespaces ni cgroups; el sistema operativo ni se entera de que existe |
| **un proceso aparte** | corre **dentro del proceso de Gnothi**, en un hilo suyo: aislamiento sin la factura de un proceso |
| **«el módulo compilado a wasm»** | eso ya existe y es la **forma hospedada**: wasm corriendo en el motor del navegador. Lo que cambia acá es **de quién es la VM** |

Está más cerca de **un intérprete con vocabulario fijo** que de cualquiera de las tres primeras.

**Y la cuarta fila es la que más se confunde**, así que conviene decirla al derecho: la pregunta nunca
fue *«¿wasm sí o no?»*. La forma hospedada ya es el módulo en wasm. La pregunta es **¿la máquina
virtual de quién?** — la del navegador, que viene con todo lo que un navegador trae, o **una que
Gnothi embebe y cuyos imports decide**.

---

## Qué cambia respecto de cómo corre hoy

Hoy, en las formas cargo, híbrida y Blazor-en-el-Host, **el DLL de la institución lo carga el mismo
runtime de .NET que Gnothi, en el mismo proceso, en el mismo heap, con los mismos permisos.**

Medido el 2026-09-16 corriendo **el mismo binario** en los dos anfitriones (`F8`, paso 2):

| el módulo intenta… | en proceso | en la caja |
|---|---|---|
| leer su propio journal | pudo | **pudo** |
| leer/escribir la carpeta de **otro módulo** | **pudo** | no pudo |
| listar el disco de la persona | **pudo** · 31 entradas de `C:` | no pudo |
| salir a la red | **pudo** · HTTP 200 | no pudo |

**Siete de ocho funcionan en proceso.** Ninguno es un ataque: son las cosas que cualquier módulo
puede hacer con la biblioteca base, sin trucos.

---

## Qué cuesta

| | medido en el Xiaomi, 2026-09-16 |
|---|---|
| compilar el runtime a código arm64 | 1,2 s · **163 MB, una vez** |
| **cada caja viva** | **~16 MB** |
| el dominio y la superficie, adentro de la caja | **227 KB** |
| **el módulo entero**, con su cara | **~240 KB** contra 12,4 MB de un bundle hospedado |

Contra la forma hospedada en el mismo aparato y con el mismo instrumento: **+58,8 MB el primer módulo
vivo, y uno vivo a la vez.** Por módulo vivo la caja cuesta la cuarta parte; **pero paga 163 MB de
entrada**, así que con un solo módulo vivo la hospedada todavía gana. *(Precompilar el componente
debería borrar ese costo fijo, y no está probado.)*

---

## Lo que queda decidido y lo que queda abierto

**Decidido, y escrito en `P19`:** el borde y el contrato **los presta el Host**, montados de solo
lectura. Si el lector de marcas viniera dentro del módulo, la lista de lo que expone sería una
**declaración** del módulo; prestándolo, es una **extracción** hecha por código de Gnothi sobre
metadatos reales. El privilegio de mirar adentro no se pierde: **se muda adentro con el lector**.

**Cerrado el mismo día (`F8`):**

- **El paquete ya no se monta.** Con `WasmSingleFileBundle` el runtime, ICU, el BCL y los DLLs del
  módulo van **dentro de un solo `.wasm` de 6,3 MB**, así que lo único montado es la carpeta
  concedida. Los **nueve intentos de fuga pasan a ocho cerrados**: `/` no existe, `..` no lleva a
  ningún lado, y el módulo ya no puede reescribir sus propios DLLs. Y arranca más rápido: **247 ms**
  contra 443–532.
- **La huella del artefacto precompilado.** Wasmtime valida la cabecera y la versión pero **no el
  contenido** — 4 KB cambiados en el medio pasaban sin aviso. Gnothi guarda un `sha256` y lo verifica
  antes de abrirlo: **22 ms**, y el artefacto roto se descarta **antes** de entregárselo al motor.

- **El canal del borde ya no se comparte.** El invitado se queda con el descriptor 1 crudo
  (`Console.OpenStandardOutput()`) y manda `Console.Out` a un archivo, **antes de arrancar nada**.
  De 541 bytes de ruido del motor en el canal del protocolo a **cero**.
- **El canario**, que es lo que `D2` pedía de verdad. Ver abajo.

**Y dos caminos que se cerraron, con su razón:**

- **El módulo como *reactor*** con funciones exportadas: **no se puede con este toolchain.**
  `WasiApp.targets` exporta cuatro símbolos fijos y el mundo del componente está clavado en
  `WasiHttpWorld`; `componentize-dotnet` haría mundos WIT propios pero usa NativeAOT-LLVM, otro
  runtime, que invalidaría el paso 0. **Y lo que el reactor compraba —que el actor viva entre
  preguntas— ya lo teníamos**: la segunda y tercera consulta se midieron en 1 y 0 ms.
- **El préstamo del borde por el Host: descartado**, porque el `.wasm` de un solo archivo lo hace
  imposible y compra más. La declaración vuelve a ser del módulo, y la comprobación en sus dos
  mitades sigue siendo el control. **El privilegio de mirar adentro no se muda: se cambia por la
  contención.**

**El contador de intentos negados: imposible, y por una razón que es una virtud.**
`WebModule.Violations` cuenta lo que la CSP **rechaza**, y puede porque el navegador deja pasar la
petición y después la niega. Con los preopens **no hay evento que contar**: al invitado nunca se le
entregó el descriptor, así que del otro lado hay una **ausencia**, no una negación. Y la C API no
expone gancho ninguno sobre el sistema de archivos.

**En su lugar, el canario.** Gnothi arma una caja con **exactamente** la configuración que le va a
dar al módulo, mete adentro una sonda suya, y verifica que las ocho fugas fallen. Si alguna pasa,
**no se abre ningún módulo**. Medido: 8 de 8 con la jaula bien armada; con una mal armada a
propósito detectó las dos que se abrían y se negó a seguir.

No cuenta lo que un módulo malo intenta —eso es invisible por construcción— sino que **verifica la
jaula**, que es lo único que puede degradarse sin que nadie lo note.

**Abierto:**

- **iOS.** No hay JIT: wasmtime tendría que ir interpretado o compilado antes. Sin medir. *(En
  Android ya está contestado: Cranelift compiló 12 MB de wasm a arm64 en 1,2 s.)*
  **Investigado de escritorio el 2026-09-17 — ver abajo, «iOS: hay camino, y no es el de Android».**
- **Las vistas con parámetros.** `serie` y `tendencia` piden un analito; las siete se llamaron sin
  argumentos.

**Y una pregunta que la caja contesta igual que hoy:** el módulo **sigue escribiendo su journal**, y
**Gnothi sigue siendo el cartero**. Medido: el sobre del publicador cruzó el borde en 14 ms, el
cursor pasó de 303 a 401, la cuenta de 19 a 21 exámenes, y **el journal real creció de 8 175 a 8 555
bytes** — la carpeta concedida *es* la carpeta, no una copia en memoria que haya que sincronizar. El
mismo sobre repetido no asentó nada: **la idempotencia es del módulo y sobrevive al cruce**.

Y el módulo no tiene red **no por disciplina**: no se le dio `wasi-http`.

---

## Y cómo se relaciona con las cuatro formas

No es una quinta forma: **es un cambio de anfitrión para las formas que ya existen.** El dominio, la
superficie, los cuatro marcadores y el journal son los mismos. Lo que cambia es **quién ejecuta el C#
de la institución y qué puede alcanzar desde ahí** — y con eso, dos cosas que ninguna de las cuatro
resolvía a la vez: que el módulo pese lo que es, y que no haya que confiar en que se porte bien.

---

# iOS: hay camino, y no es el de Android · 2026-09-17

> **ACTUALIZADO el mismo día, y ya no es sólo escritorio: el sustrato COMPILA y CORRE sobre Pulley,
> y el precio está medido.** Ver «Lo que se corrió», al final de esta sección. Lo que sigue
> inmediatamente abajo es la investigación previa; se deja porque es el razonamiento que llevó a la
> prueba, pero los números inferidos **quedaron superados por los medidos**.

**Lo que sigue era investigación de escritorio**: no había un iPhone en la prueba, y ni siquiera
estaba comprobado que el sustrato compilara para el destino que se propone. «Hay camino» quería decir
que existe la pieza y que el binario se puede construir — **no que ande**.

## Lo que se cae, y es más de lo que parecía

**Cranelift está descartado**, y no sólo por el JIT. iOS no le deja a una app de terceros crear
páginas ejecutables, así que el compilador no tiene dónde escribir. Eso ya se sabía.

**Lo que NO se sabía es que tampoco salva precompilar.** El truco que hizo barata la caja en Android
—compilar una vez y después abrir el `.cwasm`— no cruza: la documentación de wasmtime lo dice en la
misma frase que el requisito de páginas ejecutables, *«Support for statically linking in a single
precompiled module is not supported at this time»*. Abrir un precompilado **también** necesita mapear
código como ejecutable.

Así que la respuesta de Android no se puede copiar. Hace falta otra.

## La pieza que existe: Pulley

Es el **intérprete portable** de wasmtime. En las plataformas donde Cranelift no corre, se usa solo,
y sus requisitos son los de wasmtime mismo: si el motor compila para ese destino, Pulley corre ahí.

Y hay un detalle que **mejora** la historia del artefacto en vez de empeorarla: Pulley igual necesita
que el módulo se traduzca a su bytecode, pero **eso lo hace Cranelift en otra máquina**
(`wasmtime compile --target pulley64`). O sea que en iOS el `.cwasm` del sustrato **viajaría dentro
del bundle firmado de la app**, no producido en el aparato.

Hoy el artefacto es «confiable porque lo produjo Gnothi en este teléfono» —por eso se le verifica la
huella antes de abrirlo, porque wasmtime no valida su contenido—. En iOS sería «viene firmado junto
con la app». Es una posición más fuerte, no más débil.

## Y una diferencia con Android que hay que decir fuerte

Lo que hizo posible el camino de Android fue que **bytecodealliance publica `libwasmtime.so` ya
compilado para `aarch64-android`**. Para iOS **no publica nada**. Lo que hay es un paquete
comunitario que vendoriza rebanadas de iOS device y simulador **compiladas desde fuente por ellos
mismos**, y que se describe a sí mismo como *«a community build/test package, not an official
Wasmtime distribution»*.

**Eso cambia de quién es el motor.** En Android usamos un binario oficial; en iOS habría que hacerse
cargo de construirlo y de sostenerlo en cada versión. No es un bloqueante, es un costo permanente que
no aparece en ninguna medición.

## El precio, y por qué es el número que decide

La documentación de Pulley dice **~10x más lento** que Cranelift o que nativo, y avisa que no es apto
para tareas intensivas en cómputo.

Aplicado a lo medido en Android, da esto — **y los tres renglones de la derecha son inferencia, no
medición**:

| | Android, Cranelift *(medido)* | iOS, Pulley *(inferido)* |
|---|---|---|
| arranque de un módulo | 32–50 ms | ~320–500 ms |
| una consulta por el borde | ~1 ms | ~10 ms |
| pintar una pantalla (~42 idas y vueltas) | ~42 ms | **~420 ms** |

**Ese ~420 hay que tratarlo con desconfianza, porque tiene tres supuestos encadenados:**

1. el **~1 ms** lo midió el spike **sobre vistas simples**, no sobre las de exámenes;
2. las **~42 idas y vueltas** salen de **leer `cara.js`**, no de contarlas corriendo;
3. el **×10** es la cifra **genérica** de la documentación, no medida sobre nuestra carga — que es el
   runtime de .NET adentro de wasm, un caso bastante peor que el promedio de un benchmark.

Un número con tres supuestos encadenados **se lee igual de firme que uno medido**. Es el modo exacto
en que este proyecto ya se equivocó tres veces en un día: el largo del asset que mentía, el artefacto
viejo que corría, el barrido que no barría. Los tres parecían datos.

**Y acá se cruzan dos pendientes:** el único término que quedó sin medir de la caja —cuánto cuesta
pintar la pantalla— es justo el que decide iOS. En Android, a 1 ms, nadie se entera; a 10 ms son
cuatro décimas de pantalla en blanco por apertura, y eso sí se ve. Ver `F2` · `P24`.

## Una nota que juega a favor, del lado de la tienda

`B11` deja abierto *«si wasm cuenta como interpreted code para Apple»*. Con Pulley la pregunta se
vuelve más fácil de defender: no es una metáfora, **es literalmente un intérprete**, y el código de
la institución nunca llega a ser código nativo. De las cuatro formas, ésta es la que mejor se para
frente al `4.7` — con el hueco que `B11` ya señalaba intacto: el texto de Apple enumera *«HTML5 and
JavaScript»* y **no nombra wasm**.

## El primer paso, y es barato

**Compilar nuestro `sustrato.wasm` con `--target pulley64` en la laptop.** Sin iPhone, sin motor de
iOS, sin nada. Contesta lo único que hoy no se sabe y que invalida todo lo demás si sale mal: si un
componente WASI preview 2 con el runtime de .NET adentro **tiene traducción a Pulley**.

Si eso falla, los ~420 ms no son una estimación optimista: **son un número sobre algo que no
existe**.

**Fuentes, consultadas el 2026-09-17:** [Platform Support](https://docs.wasmtime.dev/stability-platform-support.html) ·
[Using Pulley](https://docs.wasmtime.dev/examples-pulley.html) ·
[swift-wasmtime](https://github.com/OpenCow42/swift-wasmtime) ·
[issue #12251](https://github.com/bytecodealliance/wasmtime/issues/12251), donde alguien le hizo esta
misma pregunta a los mantenedores y **se cerró sin respuesta**.

## Lo que se corrió · 2026-09-17

**El sustrato compila a Pulley y corre.** CLI de wasmtime v48.0.2, en la laptop, con el mismo
`sustrato.wasm` (11 131 552 bytes) que va en el APK y el `dominio/` de exámenes montado de sólo
lectura — las dos puertas, igual que en el teléfono.

Los cinco pasos del cargador dieron OK en los dos motores: la carpeta montada, `Assembly.LoadFrom`,
`GetTypes` (57 tipos), la clase marcada (`ExamenesSurface`) y la construcción sobre la carpeta
concedida. **No hubo que cambiar una línea del sustrato.**

### El precio, medido

| | Cranelift *(nativo)* | **Pulley** | cociente |
|---|---|---|---|
| arranque del módulo, los 5 pasos | **17 ms** *(16–18, n=5)* | **329 ms** *(315–341, n=5)* | **×19** |
| 42 consultas por el borde | **49 ms** | **1 157 ms** | **×24** |
| una consulta | ~1,2 ms | **~27,5 ms** | ×24 |
| el artefacto | 19,3 MB | **15,5 MB** | — |
| compilarlo | 0,71 s | 0,68 s | — |

**Y hay que decir que la estimación previa se quedó corta por tres veces.** Acá arriba, antes de
correr nada, este documento decía ~420 ms para pintar una pantalla, aplicando el ×10 genérico de la
documentación. **Lo medido da ~1,2 s** — porque el multiplicador real sobre nuestra carga no es 10
sino **19 a 24**. El runtime de .NET adentro de wasm es un caso peor que el promedio de un benchmark,
que era justamente la sospecha anotada y ahora es un hecho.

**Dos supuestos de los tres se cayeron, y uno se confirmó.** El ~1 ms por consulta que el spike había
medido sobre vistas simples **se confirmó de forma independiente**: 1,2 ms. El ×10 **se refutó**. Lo
que sigue sin medirse es la cuenta de 42, que sale de leer `cara.js` y no de contarlas corriendo.

### Lo que este número NO dice, y hay que leerlo con eso al lado

- **Es un PISO.** El journal estaba **vacío**, así que las vistas casi no trabajaron. Con el registro
  real de una persona, las dos columnas suben — y la de Pulley sube ×24.
- **Es una laptop x86_64 interpretando `pulley64`, no un iPhone.** El cociente debería trasladarse;
  los valores absolutos no. En el teléfono el arranque nativo son 32–50 ms, no 17.
- **Y el artefacto es MÁS CHICO**, que es lo único que mejora: 15,5 MB contra 19,3. El bytecode de
  Pulley es más compacto que el código máquina, y ese archivo es el que viajaría dentro del IPA.

### Qué queda decidido

**«Se puede» dejó de ser una pregunta abierta: se puede, y no hace falta tocar el sustrato.** Lo que
está abierto ahora es si **se debe**, y eso ya no es una duda técnica sino un número de producto:
**más de un segundo de pantalla en blanco por apertura, en el mejor caso**, contra los ~49 ms de hoy
en Android.

Antes de mover una pieza más conviene contar las idas y vueltas de verdad —el último supuesto que
queda en pie— porque **si son 42 la pantalla tarda un segundo, y si son 12 tarda un tercio**. Es el
mismo pendiente `4` de `C10`, y ahora tiene dueño: lo decide iOS.

## Los otros motores, y el patrón que recién se ve mirándolos juntos · 2026-09-17

Quedarse en wasmtime hacía parecer que el problema era de wasmtime. **No lo es: es de la
plataforma**, y se ve al poner los cinco al lado.

| motor | ¿JIT en iOS? | ¿modelo de componentes? | velocidad |
|---|---|---|---|
| **wasmtime + Pulley** | no | **SÍ** | **×19–24**, medido sobre nuestra carga |
| **WAMR** con AOT | n/a — compila antes | **no** | cerca de nativa; **iOS no está en su lista** |
| **Wasm3** | no | **no** | «el intérprete de wasm más rápido»; recomendado explícitamente para iOS |
| **WasmKit** (Swift) | no | **no** | intérprete de máquina de registros; **~3,5× más lento que Wasm3** |
| **JavaScriptCore en proceso** | **NO** | n/a | intérprete |
| **JavaScriptCore en un WKWebView** | **SÍ** | n/a | **nativa** |

### Primer patrón: en iOS, TODO motor en proceso es un intérprete

No es una carencia de wasmtime. **Apple no le deja a ningún proceso de terceros escribir memoria
ejecutable**, así que Pulley, Wasm3 y WasmKit están todos en la misma situación por la misma razón, y
sólo se diferencian en cuánto pierden.

Y el caso de JavaScriptCore lo deja desnudo, porque es **el mismo motor en los dos lados de la línea**:

- **En proceso**, por `JSContext` desde la app: **sin JIT**. Apple no se lo habilita a nadie.
- **Dentro de un `WKWebView`**: **con JIT**, porque ahí el código corre en el proceso *WebContent* de
  Apple, no en el nuestro.

La única excepción es `BrowserEngineKit`, de iOS 17.4 para navegadores alternativos en la UE, con un
entitlement que una app común no obtiene.

### Segundo patrón: el único que habla componentes es wasmtime

Wasm3, WasmKit y WAMR son **módulos core + WASI preview 1**. Nuestro sustrato es un **componente
preview 2** — por eso `WasmBox` llama a `wasmtime_component_*` y hay que darle `wasi:http` para que
instancie.

**Así que la elección de hoy no es «qué motor»: es que el formato del sustrato ya eligió el motor.**

### Y el tercero, que es el incómodo

**El motor de wasm más rápido disponible en un iPhone es el que Gnothi YA usa** — el del WebView, en
la forma hospedada. Con JIT, en otro proceso, y gratis.

Eso le da a la pregunta de este documento —*«¿la máquina virtual de quién?»*— una respuesta específica
de iOS, y es un dilema, no una opción:

| | la VM es de Gnothi | la VM es del WebView |
|---|---|---|
| quién decide los imports | **Gnothi**, y está medido con el canario | el puente, que es JS |
| velocidad | intérprete: ×19–24 | **nativa** |
| contención | **capacidades de WASI, falsable** | el sandbox del proceso WebContent |
| el reparo de la tienda | el `4.7` de Apple, sin nombrar wasm | **el `4.7.2`**, que `B11` ya marca: exponerle archivos y red a software que no viene en el binario **requiere permiso previo de Apple** |

En Android no hay que elegir: la caja es rápida **y** los imports son de Gnothi. **En iOS hay que
elegir**, y las dos columnas pierden algo que en Android se tenía junto.

### Lo que esto convierte en la pregunta de mayor palanca

**¿Puede el sustrato publicarse como módulo core con WASI preview 1, en vez de componente preview 2?**

De esa respuesta cuelga todo lo demás:

- **si sí** → se abren Wasm3 (el intérprete más rápido, pensado para iOS) y **WAMR con AOT**, que es
  velocidad cercana a nativa sin depender del WebView. Es el único camino que rompe el dilema de
  arriba en vez de elegir un lado.
- **si no** → en iOS queda wasmtime con Pulley y su ×19–24, o mudar el sustrato al WebView y aceptar
  que los imports dejan de ser de Gnothi.

**Y hay un aviso para la segunda opción:** un `WKWebView` tampoco corre un componente p2 directamente.
Haría falta transpilarlo (`jco`), que es otra pieza y otro riesgo.

### Lo que NO cambia con ningún motor

**Cambiar de motor obliga a rehacer la garantía, no sólo el binding.** Son las 30 entradas de
`Wasmtime.cs`, `Cage` entero —los preopens y el `fsMutable:false` son API de wasmtime— y **el canario
vuelto a escribir y a correr contra la semántica nueva**. Las 8 fugas cerradas y las 2 puertas
abiertas no se heredan: se vuelven a demostrar.

Y ninguno toca lo que hoy domina la pantalla, que es **el replay del journal**: ~700 ms en cada
apertura, iguales en la caja y en proceso. A velocidad nativa siguen siendo ~700 ms.

**Fuentes, 2026-09-17:** [WAMR](https://github.com/wasm-micro-runtime/wasm-micro-runtime) ·
[Wasm3](https://github.com/wasm3/wasm3) · [WasmKit](https://github.com/swiftwasm/WasmKit) ·
[Platform Support — Wasmtime](https://docs.wasmtime.dev/stability-platform-support.html)

### El replay bajo Pulley, MEDIDO — ya no es «×20 inferido» · 2026-09-17

Hasta acá los «~14 s» eran una multiplicación: 700 ms del teléfono × el ×19–24 medido con el journal
vacío. Ahora hay un journal real en la laptop —armado desde el publicador con el `entregar` del borde,
**18 eventos, 4,5 KB**— y el replay corrió en los dos motores, tres veces cada uno:

| | Cranelift | **Pulley** | cociente |
|---|---|---|---|
| arranque sin pedidos | 166 ms | 967 ms | ×6 |
| **el replay** (primer `cursor`) | **~290 ms** | **~4 600 ms** | **×16** |
| tres vistas después del replay | ~135 ms | ~2 700 ms | ×20 |
| **hasta la primera pantalla** (sin las 18 tendencias) | ~590 ms | **~8,2 s** | ×14 |

**Con 18 eventos.** El teléfono tiene ~225 y ahí el replay nativo cuesta ~700 ms, así que el número
real de iOS está **por encima** de estos 4,6 s, no por debajo. La forma de la cuenta se confirma: el
intérprete multiplica el trabajo del dominio, y el dominio es lo que manda.

**Lo que esto cierra:** «¿cuánto cuesta el replay bajo Pulley?» ya no se infiere. Con un journal
chico son **cuatro segundos y medio**, más casi tres para las tres primeras vistas — antes de las 18
`tendencia`. Calentarlo lo saca de la espera de la persona pero **el trabajo sigue ahí**: bajar iOS a
algo usable exige reducir el trabajo del replay, no moverlo.
