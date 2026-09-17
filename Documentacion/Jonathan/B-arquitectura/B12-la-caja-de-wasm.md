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
