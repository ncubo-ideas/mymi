# F8 · La caja de wasm — los cuatro pasos, y el precompilado

**Medido el 2026-09-16** en una laptop Windows, con wasmtime 48.0.2, .NET 9.0.19 `wasi-wasm` y el
motor `3.0.0-beta.10049`. El spike está en
[`Spikes/Lab.WasiCaja`](../../../Spikes/Lab.WasiCaja/).

Cuatro pasos de `F2` · `P19`, corridos el mismo día:

- **paso 0** — ¿arranca el motor de la institución en un anfitrión sin hilos que NO es el navegador?
  **Sí**, y el módulo pesa **227 KB** en vez de 12,4 MB.
- **paso 1** — ¿puede Gnothi hablarle a un módulo que no cargó, sin reflexión? **Sí**, y el borde
  cuesta **~1 ms**.
- **paso 2** — ¿la contención es falsable? **Sí en lo que importa**: el mismo binario que en proceso
  lee y escribe el journal de otra institución, lista el disco entero y sale a Internet, adentro de
  la caja no puede nada de eso. Con **dos huecos nombrados** y **una mitad sin cumplir**: Gnothi no
  VE los intentos.
- **paso 3** — ¿se puede embeber wasmtime en vez de lanzar un proceso? **Sí**, por la C API y no por
  las bindings de C#. **Construido y corriendo en el teléfono**, con `libwasmtime.so` que
  bytecodealliance publica ya compilado para `aarch64-android`.
- **y el precompilado** — `wasmtime_component_serialize` baja el arranque de **1 109 ms a 4 ms** y la
  memoria de **249 MB a 80 MB**. Con eso, **cada módulo vivo cuesta +29 MB contra los +58,8 MB de uno
  hospedado**, sin costo fijo y sin la regla de «uno vivo a la vez».

---

# Paso 0 · El motor bajo WASI

## Lo que corrió

`Examenes` —dominio, superficie y portador, sin tocar una línea— compilado a `wasi-wasm` y
ejecutado por wasmtime, con una carpeta montada como única cosa que el módulo puede ver:

```
wasmtime run -S http --dir .::/ dotnet.wasm Lab.WasiCaja /datos
```

Salida, tal cual:

```
caja: procesadores      = 1
caja: OS                = Other / wasi-wasm
caja: actor construido            (67 ms)
caja: almacenamiento configurado  (68 ms)
1%2%caja: ACTOR ARRANCADO             (443 ms)
caja: cursor del journal          = 0  (561 ms)
1%2%3%caja: Resumen() contesto: { hayDatos=True examenes=0 analitosFueraDeRango=0 ultimoEventoLeido=0 }
```

**`ProcessorCount = 1`** es la prueba de que está en el camino sin hilos, y **`1%2%3%`** es el
progreso de replay del motor: rehidrató de verdad, no arrancó en vacío.

Y dejó el journal completo, con la estructura que el motor arma:

```
examenes/
  journal/journal_000001.bin   252 bytes
  index/index.bin               30 bytes
  meta.bin                      48 bytes
  elision/pending/  followers/  materialization/  outbox/  reactions/  skips/
```

**Las tres cosas del paso 0 pasaron:** el almacenamiento sobre el sistema de archivos, la
rehidratación y el despacho. Están las tres en el camino de un solo `Start()`, que es por lo que
esta prueba es corta.

## Y con un journal real, que es lo que de verdad contesta la pregunta

Lo de arriba arranca sobre una carpeta vacía: crea el journal y reproduce **una** entrada, la
semilla. Eso prueba que el motor arranca y **no** prueba que reproduzca.

Así que se le metió un journal de verdad, producido por el fixture del propio módulo
—`Examenes.Consola medir`, los dieciocho eventos del expediente de Sonia con sus fechas literales—
**corriendo nativo en Windows**, y después se le entregó esa misma carpeta a la caja de wasm:

```
caja: procesadores      = 1
1%2%…24%caja: ACTOR ARRANCADO             (532 ms)
caja: entradas en el journal     = 39
caja: cursor del journal          = 303
caja: Resumen() contesto: { hayDatos=True examenes=19 analitosFueraDeRango=1 ultimoEventoLeido=303 }
```

**Y el mismo journal, corrido nativo:**

```
caja: procesadores      = 16
1%2%…24%caja: ACTOR ARRANCADO             (245 ms)
caja: cursor del journal          = 303
caja: Resumen() contesto: { hayDatos=True examenes=19 analitosFueraDeRango=1 ultimoEventoLeido=303 }
```

**Idéntico: 19 exámenes, 1 analito fuera de rango, cursor 303.** No parecido: igual.

Eso es más de lo que el paso 0 pedía, y conviene nombrarlo aparte: **el journal es portátil entre
anfitriones.** Escrito por .NET nativo en Windows, reproducido por el intérprete de wasm bajo WASI,
misma respuesta. Es exactamente la propiedad que `C6` llama *determinismo de replay* y que hasta hoy
sólo estaba medida entre versiones del dominio, no entre máquinas virtuales distintas.

## Lo que cuesta, comparado

### Tiempo · cómo escala el replay

Tres tamaños del mismo journal, el mismo binario, la misma carpeta copiada para cada corrida:

| entradas en el journal | WASI (intérprete, 1 CPU) | nativo (JIT, 16 CPU) | razón |
|---|---|---|---|
| **1** (sólo la semilla) | ~500 ms | ~376 ms | 1,3× |
| **39** | 532 ms | 245 ms | 2,2× |
| **89** | 547 · 571 · 575 ms | 246 · 252 · 254 ms | **2,2×** |

**El costo marginal por entrada es lo que importa, y es chico:** entre 39 y 89 entradas, WASI
sumó ~32 ms por 50 entradas —**0,6 ms por entrada**— y el nativo ~6 ms, o sea 0,12 ms. El intérprete
paga unas cinco veces más por entrada, **en una escala en la que cinco veces casi nada sigue siendo
casi nada**.

Extrapolado —y es extrapolación, no medición— un journal de 314 entradas como el que tiene `compras`
en el teléfono costaría **~700 ms bajo WASI contra ~280 ms nativo**. Por debajo del segundo, con el
intérprete, en un anfitrión de un solo hilo.

**Lo que sigue sin medirse:** un journal de miles de entradas, y el mismo ejercicio en el teléfono,
donde el procesador es otro. La pendiente de 0,6 ms/entrada es de esta laptop.

### Tiempo — primera comparación, sobre carpeta vacía

| | procesadores | JIT | trimming | corridas |
|---|---|---|---|---|
| **nativo** `win-x64` | 16 | sí | no | **329 · 397 · 402 ms** |
| **WASI** en wasmtime | 1 | no (intérprete) | sí | **474 · 502 · 624 ms** |

**Entre 1,4 y 1,6 veces más lento.** No es un orden de magnitud, y es contra un nativo con dieciséis
procesadores y JIT. *(La primera corrida en frío daba a la inversa —WASI 499 ms contra nativo
1 136 ms— porque el nativo paga el arranque del JIT. Ese número no se usa: es un artefacto de
arranque en frío y decirlo al revés habría sido vender humo.)*

### Tamaño — y ésta es la razón por la que vale la pena

El `AppBundle` completo mide 18 MB, pero **está partido en dos dueños**:

| pieza | tamaño | de quién |
|---|---|---|
| `dotnet.wasm` | 12,5 MB | **Gnothi**, una vez |
| `icudt.dat` | 1,5 MB | **Gnothi**, una vez |
| motor: `Puppeteer` + `Choreography` | 1,05 MB | **Gnothi**, una vez |
| BCL recortado | ~3,2 MB | **Gnothi**, una vez |
| `Examenes.App` + `Dominio` + `Eventos` + `Portador` | **227 KB** | **la institución** |

**Y la comparación, dicha bien.** Los 227 KB son **dominio y superficie**, que es todo lo que este
spike compiló: **no tiene cara.** Un módulo real lleva además su `web/` —9 a 13 KB de HTML+JS en las
cinco instituciones, que **no entra a la caja**: sigue en el WebView (ver
[`B12`](../B-arquitectura/B12-la-caja-de-wasm.md))—. O sea **~240 KB contra los 12,4–14,1 MB de un
bundle hospedado, que sí incluye su cara.**

**Cincuenta veces menos.** Y el resto
—los 17,8 MB— **se paga una sola vez para todos los módulos**, que es exactamente lo que `P10` pide
y lo que le devolvería sentido a `substrate.range`.

Los 12,5 MB de `dotnet.wasm` son además el runtime **precompilado sin recortar**: este paso usó
`WasmBuildNative=false` para no instalar el toolchain de C. Con el relink nativo baja, y cuánto está
sin medir.

## Las cuatro trampas que costó, y tres fallan en silencio

**1 · `WasiNativeWorkload9` — la peor, porque el build sale en verde.**
Con el workload `wasi-experimental-net9` instalado y `RuntimeIdentifier=wasi-wasm`, el
`dotnet publish` **termina bien y no produce nada ejecutable**: sólo los `.dll` administrados. Ni
`dotnet.wasm`, ni `AppBundle`, ni error, ni advertencia. La cadena es `WasmBuildApp` ← `IsWasiProject`
← `UsingWasiRuntimeWorkload` ← `WasiNativeWorkloadAvailable` ← **`WasiNativeWorkload9`, que nadie
pone**. Se diagnostica con:

```
dotnet msbuild <proyecto> -getProperty:IsWasiProject
```

que devuelve vacío. Con SDK 10.0.400 y manifiesto 10.0.111.

**2 · `wasi-sdk`.** Con el valor por omisión, el publish pide el toolchain de C
(`Could not find wasi-sdk`) para relinkear el runtime. `-p:WasmBuildNative=false` usa el
`dotnet.wasm` precompilado del runtime pack y alcanza para esto.

**3 · `icudt.dat`.** `InvariantGlobalization=true` es una opción **administrada**; el `dotnet.wasm`
precompilado busca ICU igual y muere en `load_icu_data` con un *trap* de wasm, antes de la primera
línea de C#. Hay que copiar el archivo del runtime pack al `AppBundle` a mano.

**4 · `JsonSerializer` con reflexión apagada.** El build de WASI trae
`JsonSerializerIsReflectionDisabled`, y eso es correcto —es lo que hace posible el recorte—, pero
tumba cualquier impresión ingenua. El spike describe los objetos a mano en vez de encender la
reflexión sólo para imprimir, que habría sido cambiar lo que se está midiendo.

## Lo que este paso NO prueba, y hay que decirlo

- **El journal real llega a 89 entradas, no a miles.** El fixture del módulo tiene dieciocho eventos
  con fechas literales, así que reproducirlo otra vez casi no agrega: la guarda de duplicados hace
  su trabajo. La pendiente de 0,6 ms por entrada está medida entre 39 y 89 y **extrapolada** más
  allá. Un journal de miles de entradas sigue sin medirse, y **el teléfono también** — esa pendiente
  es de esta laptop.
- **No hay borde.** El programa llama a la superficie **adentro del mismo wasm**. Que Gnothi le
  hable desde afuera, sin reflexión, es el **paso 1** — y es el riesgo de diseño de verdad, porque
  toca `Gnothi.Contract`.
- **La contención no está medida, sólo insinuada.** `--dir .::/` monta todo; el módulo vio `/datos`
  porque se lo monté. **Ésa es la forma del paso 2**: montar sólo la carpeta concedida y verificar
  que salir de ella falla y queda registrado.
- **No hay teléfono.** wasmtime en un APK, memoria por módulo vivo y latencia contra los 153 ms de
  la puerta Binder son el **paso 3**.

## Qué cambia en el tablero

`P19` sigue en pie, con su paso 0 en verde. Y se puede decir una cosa que ayer no:

> **El camino de wasm no depende de que nadie nos arregle nada.** El guard de hilos ya está en el
> motor (`P1`), el workload ya está en la máquina, y el módulo real arranca. Lo que falta es
> diseño nuestro: el borde y la contención.

---

# Paso 1 · El borde sin reflexión

**Medido el 2026-09-16**, mismo día y misma máquina. El lado de afuera está en
[`Spikes/Lab.WasiCaja.Host`](../../../Spikes/Lab.WasiCaja.Host/).

**La pregunta era: ¿puede Gnothi hablarle a un módulo que no cargó?** Hoy encuentra los cuatro
marcadores del contrato por reflexión sobre un `Type` de .NET, porque el DLL de la institución está
en su proceso. Del otro lado del borde no hay un `Type`: hay bytes.

**Sí. Y el contrato no cambió de forma — cambió de lugar.**

> **CORREGIDO el 2026-09-16, después de una pregunta de un compañero.** Lo que sigue describe lo que
> **corrió**: el lector de marcas compilado dentro del módulo, que entonces *declara* lo que ofrece.
> **Eso no tiene por qué ser así, y no debería:** en la caja Gnothi compone la carpeta que el módulo
> ve, así que puede **prestarle el lector**, montado de solo lectura — y entonces el que reflexiona
> adentro es código de Gnothi y la lista se **extrae** en vez de declararse. La condición completa
> está en [`F2` · `P19`](F2-estado-y-pendientes.md). **No está construido.**

## El hallazgo de diseño, que es lo que este paso vino a buscar

**La reflexión no desaparece: se muda adentro del módulo.**

El módulo reflexiona sobre **su propia** superficie —su código, su ensamblado, su decisión— y
**publica** lo que ofrece. El Host recibe una lista de nombres y llama por nombre.

Eso cambia quién tiene el privilegio. Hoy **el Host mira adentro del DLL ajeno**; en la caja **el
módulo dice lo que ofrece y el Host le cree o no**. Es el mismo reparto de autoridad que `A1` pide
en todo lo demás, aplicado al único lugar donde todavía no estaba.

Y la búsqueda sigue siendo **por nombre de atributo**, igual que en `InProcessShell`. Eso no es
pereza: si el módulo usara `typeof(QueryAttribute)` tendría que cargar el **mismo** ensamblado del
contrato que el Host, y eso es justo el acoplamiento que la forma cargo evita prestando su copia.

## Lo que corrió

```
gnothi: saludo en 867 ms:
gnothi:   {"listo":true,"arranqueMs":636,"entradas":90,"declara":{
            "consultas":["resumen","lista_de_examenes"],
            "vistas":["cabecera","examenes","serie","analitos","tendencia","historial","alertas"],
            "entrega":true,"cursor":true}}

gnothi: la consulta del asistente                     268 ms  {"ok":true,"dice":{"hayDatos":true,"examenes":19,…}}
gnothi: la vista de la pantalla                        24 ms  {"ok":true,"dice":{"cantidad":19,"edad":34,"sexo":"mujer",…}}
gnothi: hasta donde leyo                                1 ms  {"ok":true,"dice":303}
gnothi: una VISTA pedida por la puerta del asistente    1 ms  {"ok":false,"dice":"'cabecera' no es una consulta de este modulo"}
gnothi: la misma consulta, segunda vez                  1 ms  {"ok":true,…}
gnothi: la misma consulta, tercera vez                  0 ms  {"ok":true,…}
```

**El módulo declaró dos consultas y siete vistas**, que es exactamente el reparto que `B8`
describe: dos públicos, no uno.

### Y las tres puertas siguen aislando

La línea que importa es la cuarta: pedir **`cabecera`** —una vista— por la puerta del asistente
**falla**, aunque el método exista del otro lado. No se deniega: *no es una consulta de este
módulo*. Es la propiedad de `B8` —*«lo del otro no le es negado, no existe»*— sobreviviendo al
cruce, y **más explícita que antes**, porque el conjunto se elige antes de buscar el nombre.

## El costo del borde

| | ms |
|---|---|
| `cursor` — el viaje más corto que existe | **1 ms** |
| una consulta repetida | **0–1 ms** |
| la primera consulta | 268 ms |
| una vista con su carga completa | 24 ms |

**El borde cuesta ~1 ms.** Los 268 ms de la primera son del módulo —su primera pregunta arma
caminos que después reutiliza—, no del cruce: la misma consulta, dos líneas después, vuelve en 0 ms.

Para comparar: la puerta Binder que `B7` mide cuesta **153 ms**. **El borde de la caja es dos
órdenes de magnitud más barato que la puerta que la cara híbrida ya cruza hoy.**

## Lo que el lado de Gnothi NO tiene, que es la prueba

`Lab.WasiCaja.Host.csproj` no declara **una sola** `ProjectReference` ni `PackageReference`. Su
`Program.cs` no menciona `Assembly`, `Type`, `MethodInfo`, `Activator` ni `typeof`. 138 líneas, y
hablan con un módulo que no conocen.

| | líneas |
|---|---|
| el Host (fuera de la caja) | 138 |
| el borde (dentro del módulo) | 120 |
| serialización | 21 |

**Las 120 líneas del borde las escribe el Host una vez y viajan con el andamio**, igual que las 378
de la cuarta forma en `F7`: la institución no las escribe.

## Las dos cosas que costaron, y una es vieja conocida

**1 · `stdout` es el protocolo, y el motor no lo sabe.** `Starting … Actor` y el progreso de replay
(`1%2%3%`) salen por la **misma** salida por la que viaja la respuesta. **541 bytes de ruido** en la
corrida medida, y la primera versión del Host murió con
`'S' is an invalid start of a value`.

Es **la trampa de `E2` otra vez**, en un sitio nuevo: allá rompía MCP sobre stdio, acá rompe el
borde. El parche de esta prueba es saltarse lo que no empieza con llave. **La solución de verdad no
es ésa**: el canal del borde no puede ser el mismo que cualquier librería usa para escribir. En una
caja real eso se resuelve con exportaciones de wasm en vez de stdio, que además es lo que evita el
proceso por módulo.

**2 · Serializar pasa a ser del módulo.** Hoy la superficie devuelve un `object` y lo serializa el
Host, que lo tiene en la mano. Cruzando el borde el objeto no cruza: cruza texto. Eso obliga a
encender `JsonSerializerIsReflectionEnabledByDefault`, que el build de WASI trae apagado **a
propósito** porque es lo que permite recortar. Lo correcto es un `JsonSerializerContext` generado,
que el módulo escribe una vez; esta prueba encendió la reflexión para medir el camino entero.

## Lo que el paso 1 NO prueba

- **Es un proceso por módulo**, no una instancia de wasm dentro de Gnothi. wasmtime como librería
  embebida, con el módulo como *reactor* y funciones exportadas, es otra cosa — y es lo que hace
  falta para que el costo de memoria sea el que `P19` promete.
- **`entregar` no se ejercitó.** Es el único verbo que escribe, y cruzar un sobre del cartero por el
  borde no está medido.
- **Las vistas con parámetros tampoco.** Las siete que declaró se llamaron sin argumentos; `serie` y
  `tendencia` piden un analito, y el protocolo de esta prueba pasa la carga como un campo suelto.

---

# Paso 2 · La contención, falsable

**Medido el 2026-09-16.** El módulo que se porta mal está en
[`Spikes/Lab.WasiCaja/LosIntentos.cs`](../../../Spikes/Lab.WasiCaja/LosIntentos.cs).

`D2` pide que la contención se pueda **intentar romper y verla fallar**. Hoy no se puede: el DLL de
la institución corre en el proceso de Gnothi y **no hay cerca que intentar**.

Así que se escribió un módulo que se porta mal —nueve intentos, ninguno un ataque: las cosas que
cualquier módulo puede hacer con la biblioteca base, sin trucos y sin código nativo— y **se corrió
el mismo binario en los dos anfitriones**.

## El resultado, lado a lado

| el módulo intenta… | **en proceso** (como corre hoy) | **en la caja** |
|---|---|---|
| leer **su propio** journal | PUDO · 8 505 bytes | PUDO · 8 505 bytes |
| listar la carpeta de **otro módulo** | **PUDO** · 2 entradas | NO PUDO |
| leer un archivo de **otro módulo** | **PUDO** · 31 bytes | NO PUDO |
| **escribir** en la carpeta de otro módulo | **PUDO** | NO PUDO |
| listar una carpeta de afuera (`C:\Users\jon32`) | **PUDO** · 62 entradas | NO PUDO |
| salir con `..` desde la concedida | **PUDO** · 66 entradas | PUDO · 11 *(ver abajo)* |
| salir a la **red** por su cuenta | **PUDO** · HTTP 200 | NO PUDO *(ver abajo)* |
| escribir **su propio paquete** | *(no comparable)* | **PUDO** |
| listar la raíz que ve | **PUDO** · 31 entradas: **todo `C:`** | PUDO · 11: su propio paquete |

**Siete de ocho funcionan en proceso.** Incluye **escribir dentro de la carpeta de otra
institución** y **HTTP 200 al exterior**. No es una hipótesis: es el mismo código compilado, corrido
dos veces.

Dicho de la manera incómoda: **hoy, el módulo del supermercado puede leer el journal del
laboratorio, escribir en él, listar el disco entero de la persona y salir a Internet. Lo único que
lo impide es que no lo hace.**

En la caja, todo lo que sale del mundo del propio módulo falla. **Esa diferencia es la contención,
medida en lugar de prometida.**

## Los dos huecos, que son reales y tienen dueño

**1 · `..` cae adentro de su propio paquete, y puede reescribirlo.**

El `dotnet.wasm` precompilado busca `./icudt.dat` y `./managed/` **relativo a la raíz del invitado**,
así que el paquete tiene que ir montado en `/`. Se probó montarlo en `/app` y el runtime muere en
`load_icu_data` antes de la primera línea de C#. Consecuencia: `/datos/..` es `/`, que está
montado — y el módulo **escribió un archivo dentro de su propio `managed/`**.

**El alcance está acotado y hay que decirlo:** desde ahí ve su propio código, **no** el journal de
otro módulo ni nada del anfitrión. Pero puede reescribir sus propios DLLs, y **ninguna firma lo
detectaría después de instalado**.

**El arreglo es conocido y suma dos motivos al mismo trabajo:** un `.wasm` de un solo archivo
—`WasmSingleFileBundle`, que necesita el `wasi-sdk`— deja el paquete **adentro** del módulo, y
entonces lo único montado es la carpeta concedida. Ese mismo trabajo es el que achica los 12,5 MB
del runtime.

**2 · La red falló por la razón equivocada.**

```
NO PUDO   salir a la RED por su cuenta -> PlatformNotSupportedException: Cannot wait on monitors on this runtime.
```

**No la bloqueó una política: la bloqueó la falta de hilos.** `HttpClient` no llegó ni a intentarlo.
**La contención de red NO está demostrada.** Y hay algo que mirar: el `dotnet.wasm` de net9 importa
`wasi:http`, así que **wasmtime se niega a instanciar el módulo sin `-S http`**. Que el runtime de
.NET exija la capacidad de red para arrancar es justo lo contrario de lo que esta arquitectura
quiere, y hace falta averiguar si `-S http` concede salida real o sólo satisface el import.

## Y la mitad que NO se cumplió

`D2` pide dos cosas y esto sólo entrega una. **Los intentos fallan, pero Gnothi no los ve.**

wasmtime los niega en silencio: en modo CLI no hay nada que le cuente al anfitrión que un invitado
quiso abrir `/vecino/municipalidad`. Un módulo que lo intenta cien veces por segundo es
indistinguible de uno que nunca lo intentó — **que es exactamente el problema que `WebModule.Violations`
resolvió para la CSP**, con su comentario diciendo que *«un módulo cuyo contador nunca se mueve es o
bien educado o NO ESTA SIENDO MIRADO, y hasta ahora no había forma de distinguirlos»*.

El equivalente existe y es trabajo: **wasmtime como librería embebida**, envolviendo la
implementación de archivos de WASI para contar y reportar los rechazos. Es la misma pieza que hace
falta para quitar el proceso por módulo (`paso 1`) y para el `.wasm` de un solo archivo (hueco 1).

**Tres razones distintas apuntan al mismo trabajo**, y eso es lo más útil que dejó este paso.

## Veredicto del paso 2

**Verde con dos huecos nombrados y un requisito sin cumplir.** Lo que `P19` puso como razón número
uno —*«hoy la contención es una promesa y debería ser un mecanismo»*— quedó demostrado en la mitad
que importa: **la carpeta del vecino, el disco de la persona y la red pasan de PUDO a NO PUDO sin
cambiar una línea del módulo.**

---

# Paso 3 · wasmtime embebido — la sonda, no la obra

**Medido el 2026-09-16.** La sonda está en
[`Spikes/Lab.WasiCaja.Embebido`](../../../Spikes/Lab.WasiCaja.Embebido/).

El paso 2 dejó **tres cosas distintas apuntando al mismo trabajo**: wasmtime **como librería
embebida** en vez de proceso.

| lo que arregla | por qué |
|---|---|
| el hueco del paquete montado en `/` | con el `.wasm` de un solo archivo, lo único montado es la carpeta concedida |
| el proceso por módulo | es lo que hace real el ahorro de memoria que `P19` promete |
| **Gnothi no ve los intentos** | envolviendo las funciones del anfitrión se pueden contar, como `WebModule.Violations` con la CSP |

Así que antes de construir nada, la pregunta era si **se puede**. La respuesta tiene dos mitades.

## Por el camino obvio, NO — y está medido

`Wasmtime` 44.0.0, las bindings oficiales de C# (`wasmtime-dotnet`), cargando el `.wasm` del módulo:

```
sonda: wasmtime-dotnet 44.0.0.0
sonda: cargando dotnet.wasm (12 479 078 bytes)
sonda: NO CARGO -> WasmtimeException: WebAssembly module 'dotnet' is not valid:
                   expected a WebAssembly module but was given a WebAssembly component
```

**El .NET de WASI produce un *componente*, no un módulo core**, y `wasmtime-dotnet` sólo carga
módulos core: *«Component model support»* sigue siendo un issue **abierto** en su repositorio.

Que la salida es un componente se ve además en el propio SDK: el paso de enlace pasa
`-Wl,--component-type … WasiHttpWorld_component_type.wit`
(`WasiApp.targets:356`), y la pila del paso 0 mostraba
`wit-component:adapter:wasi_snapshot_preview1!wasi:cli/run@0.2.0#run`.

## Por la C API, SÍ — y con más de lo que se pedía

`wasmtime` v48 trae `include/wasmtime/component/` en su C API, y ahí está todo lo que los tres
pendientes necesitan:

| función de la C API | para qué nos sirve |
|---|---|
| `wasmtime_component_linker_add_wasip2` | WASI completo, sin salir a un proceso |
| `wasmtime_component_linker_add_wasi_http` | **está SEPARADO** — se puede dar WASI sin dar red |
| `wasmtime_component_linker_define_unknown_imports_as_traps` | el módulo instancia aunque falte un import, y **trapea si lo usa** |
| `wasmtime_component_linker_instance_add_func` | **funciones del anfitrión que el invitado importa** — el borde sin stdio |
| `wasmtime_component_linker_instantiate` | una instancia por módulo, en el mismo proceso |

**El camino es P/Invoke desde C# sobre la C API**, que además es **el mismo camino que haría falta
en Android**: ahí wasmtime entra como librería nativa en el APK igual que en escritorio.

### Y dos de los problemas de los pasos anteriores ya tienen respuesta de diseño

**El `-S http` obligatorio** (hueco 2 del paso 2) deja de ser un dilema: `add_wasi_http` es una
llamada aparte de `add_wasip2`, y con `define_unknown_imports_as_traps` el módulo **arranca sin red
y trapea si intenta usarla**. Eso es contención de red por construcción, no por falta de hilos.

**El borde por `stdout`** (la trampa de `E2` del paso 1) deja de hacer falta:
`instance_add_func` permite que el anfitrión defina las funciones que el módulo importa, así que el
protocolo **no comparte canal con la salida de diagnóstico**. El problema de `E2` no se resuelve
disciplinando librerías: se resuelve cambiando de canal, y acá está el canal.

## El veredicto del paso 3

**No está hecho, y no se pretendió hacerlo hoy.** Lo que este paso entrega es una respuesta:

> **Se puede, y no por el camino que parecía.** Las bindings de C# no sirven —medido, con el error
> exacto—; la C API sí, con las cinco funciones que hacen falta, y de paso resuelve el canal del
> borde y la contención de red.

**Lo que falta es construirlo**, y es el trabajo más grande de `P19`: un envoltorio de P/Invoke
sobre `component.h`, el `.wasm` de un solo archivo (que necesita el `wasi-sdk`), y el módulo
expuesto como *reactor* con funciones exportadas en vez de un `main`.

**Lo que NO se sabe todavía:** si `dotnet.wasm` acepta arrancar como reactor sin `wasi:cli/run`,
cuánta memoria ocupa una instancia frente a los ~200 MB de un WebView, y cómo se comporta wasmtime
en Android e iOS —donde no hay JIT y tiene que ir interpretado o compilado antes.

---

# Paso 3 · CONSTRUIDO — wasmtime adentro del proceso, y el número que corrige el argumento

**Medido el 2026-09-16.** El envoltorio está en
[`Spikes/Lab.WasiCaja.Embebido`](../../../Spikes/Lab.WasiCaja.Embebido/): `Wasmtime.cs` es el
P/Invoke sobre la C API, `Program.cs` es Gnothi armando la caja desde código.

**Corre.** El módulo de la institución se ejecuta **dentro del proceso de .NET del Host**, sin
subproceso, con la caja configurada en código, y contesta lo mismo que siempre:

```
gnothi: componente compilado        (818 ms, 12 479 078 bytes)
gnothi: linker: wasip2 SI, wasi-http NO
gnothi: montado: paquete en / (solo lectura), concedida en /datos (escribible)
gnothi: INSTANCIADO                 (822 ms)
gnothi: entrada encontrada: wasi:cli/run@0.2.0
        caja: ACTOR ARRANCADO             (901 ms)
        caja: Resumen() contesto: {"hayDatos":true,"examenes":19,"analitosFueraDeRango":1,"ultimoEventoLeido":303}
```

## Lo que se cerró de los pasos anteriores

**El hueco del paquete (paso 2) se cierra sin el `wasi-sdk`.** `wasi_config_preopen_dir` tiene un
cuarto parámetro, `fs_mutable`. El paquete va montado con **`false`** y la carpeta concedida con
`true`: el módulo ya no puede reescribir sus propios DLLs, y no hizo falta el `.wasm` de un solo
archivo.

**La red deja de depender de la falta de hilos.** `add_wasi_http` es una llamada aparte de
`add_wasip2`. **Sin ella el componente no instancia** —`component imports instance
'wasi:http/types@0.2.1', but a matching implementation was not found`— así que hoy hay que dársela
para que arranque. La palanca existe y está en nuestras manos; lo que falta es que el runtime de
.NET no exija el import.

**Y un bug de wasmtime, reportable:** `wasmtime_component_linker_define_unknown_imports_as_traps`
**panica** con `unreachable!()` en `component.rs:512` al recorrer los imports de este componente
(wasmtime 48.0.2). Era la vía limpia —instanciar sin red y trapear si la toca— y no funciona.

## EL NÚMERO, y corrige el argumento de `P19`

Tres instancias vivas a la vez, compartiendo motor y componente compilado, que es lo que un Host
haría:

| | proceso | acumulado | **marginal** |
|---|---|---|---|
| instancia 1 | 186 MB | +179 MB | +179 MB *(incluye compilar el componente)* |
| instancia 2 | 259 MB | +252 MB | **+72 MB** |
| instancia 3 | 331 MB | +324 MB | **+72 MB** |

De los 324 MB, **11 MB son montículo administrado del Host**. Todo lo demás es de wasmtime: la
memoria lineal de cada instancia, con su propio runtime de .NET adentro.

### Y acá hay que corregir algo que este set venía repitiendo

**«La forma hospedada se lleva ~200 MB por módulo vivo» es falso**, y se repitió en `P18`, `P19` y
`P20`. Lo medido el mismo día en el teléfono fue: **base 238,7 MB, el primer módulo +58,8 MB, y los
módulos 2 a 5 entre +2,2 y +7,7 MB cada uno.** Los 199 MB eran el **total con cuatro instalados**,
no el costo por módulo.

**Con los números correctos, la caja no gana en memoria — puede perder:**

| | primer módulo | módulos siguientes |
|---|---|---|
| hospedada, en el teléfono | +58,8 MB | **+2,2 a +7,7 MB** |
| caja de wasm, en esta laptop | +179 MB | **+72 MB** |

**No son comparables de frente** —PSS en Android contra memoria privada en Windows, y en el teléfono
rige «un módulo vivo a la vez», así que esos +2 a +8 MB son el costo de estar *instalado*, no de
estar *vivo*—. Pero la dirección es clara y hay que decirla: **cada instancia de wasm trae su propia
memoria lineal con un runtime de .NET adentro, y eso son 72 MB que no se comparten con nadie.** Los
WebViews de Android comparten páginas; las instancias de wasmtime no.

### Qué queda en pie del porqué de `P19`, entonces

| razón | estado después de medir |
|---|---|
| **1 · la contención deja de ser promesa** | **en pie, y medida** (paso 2): siete de ocho fugas pasan de PUDO a NO PUDO |
| **2 · es la única forma que pasa Play y baja el tamaño** | **en pie**: 227 KB contra 12,4 MB, con acceso indirecto |
| **3 · «es lo único que baja la memoria»** | **CAÍDA.** No está demostrado y la evidencia apunta al revés |
| **4 · le devuelve sentido a `substrate.range`** | en pie |
| **5 · quita el argumento del código nativo en proceso** | en pie |

**La caja sigue valiendo la pena por 1, 2 y 5 — no por 3.** Y eso cambia una decisión: si lo que
más duele fuera la memoria, el camino sería otro. Como lo que duele es el **tamaño** (12,4 MB por
módulo) y la **regla de Play**, la caja sigue siendo el camino.

## Lo que falta del paso 3

- **El módulo sigue siendo un `command`**, no un *reactor*: cada llamada es un `wasi:cli/run` que
  arranca y termina. Para que el actor viva entre preguntas hacen falta funciones exportadas, y eso
  necesita el `wasi-sdk`.
- **El borde todavía viaja por `stdout`.** `instance_add_func` existe y es la salida; no se usó acá.
- **El contador de intentos negados sigue sin existir.** Con el anfitrión en código nuestro ya es
  posible, pero `add_wasip2` trae la implementación de wasmtime: para contar hay que envolverla.
- **Los 72 MB son de esta laptop.** En el teléfono, con otro procesador y otro presupuesto, el
  número es otro y **es el que decide**.

---

# Paso 3 en el teléfono — la medición que decide, y desdice a la laptop

**Medido el 2026-09-16** en el Xiaomi 2407FPN8EG (Android 16), con `libwasmtime.so` para
`aarch64-android` que **bytecodealliance publica ya compilado** (27,5 MB). El spike está en
[`Spikes/Lab.WasiCaja.Android`](../../../Spikes/Lab.WasiCaja.Android/) y **usa el mismo
`Wasmtime.cs` que en escritorio, sin una línea distinta**.

## Primero: corre, y contesta lo mismo

```
caja: COMPILADO (1 198 ms, 11 MB)
caja: ### instancia 1: 550 ms
      caja: ACTOR ARRANCADO (365 ms)
      caja: Resumen() contesto: {"hayDatos":true,"examenes":19,"analitosFueraDeRango":1,"ultimoEventoLeido":303}
caja: ### instancia 2: 1 533 ms   · ACTOR ARRANCADO (826 ms)
caja: ### instancia 3: 1 468 ms   · ACTOR ARRANCADO (812 ms)
```

**La misma respuesta que en la laptop y que nativo en Windows**, sobre el mismo journal real de 89
entradas. Y **el JIT de Cranelift funciona en Android**: wasmtime compiló 12 MB de wasm a código
arm64 y lo ejecutó, en 1,2 s.

## La memoria, con el mismo instrumento que la forma hospedada

El PSS que reporta `ActivityManager` desde adentro **está limitado por Android** y devolvía siempre
el mismo número (124 MB) — eso hay que saberlo o se mide cualquier cosa. Los números buenos salen de
`dumpsys meminfo`, **un proceso fresco por cada N**, que es lo único que evita que el ruido del
arranque domine:

| instancias vivas | PSS | marginal |
|---|---|---|
| *(sin motor ni componente)* | **97 MB** | — |
| 0 — motor y componente compilado | **260 · 262 · 269 MB** | **+163 MB, una vez** |
| 1 | **275 · 276 · 276 · 281 · 282 MB** | **+~16 MB** |
| 2 | 294 · 297 MB | +~16 MB |
| 3 | 243 · 248 · 249 · 320 · 321 MB | **inestable** |

**Tres lecturas, y la tercera es la interesante.**

**1 · Cada instancia viva cuesta ~16 MB.** Estable y repetible: cinco corridas de N=1 dan 275–282.

**2 · El componente compilado cuesta 163 MB, una sola vez.** Es Cranelift traduciendo 12 MB de wasm
a arm64. **Es el costo grande y es evitable**: `wasmtime_component_serialize` / `deserialize`
permiten compilar una vez —al instalar— y después mapear el resultado. No se probó.

**3 · Con tres instancias el PSS baja tanto como sube**, entre 243 y 321 MB. Eso no es error de
medición: **es Android reclamando páginas del código compilado**, y es evidencia de que esos 163 MB
están mapeados y no clavados.

## Contra la forma hospedada, en el mismo teléfono y con el mismo instrumento

| | primer módulo VIVO | módulos vivos a la vez |
|---|---|---|
| **hospedada** | **+58,8 MB** | **uno**, por la regla de desalojo |
| **caja de wasm** | **+~16 MB** | tres medidos, sin regla que lo impida |

**Por módulo vivo la caja cuesta la cuarta parte.** Y permite tres vivos por menos de lo que la
hospedada cuesta por uno.

**Pero paga 163 MB de entrada.** Para **un** módulo vivo, la caja es peor (163 + 16 contra 58,8).
Empata alrededor del décimo módulo vivo — o gana desde el primero **si el componente se precompila**,
que es la pieza que falta probar.

## Y hay que decir que esto desdice lo que se escribió hace unas horas

En la laptop, tres instancias costaron **+72 MB cada una** de memoria privada, y con ese número se
declaró **caída** la razón 3 de `P19` (*«es lo único que baja la memoria»*). **En el teléfono, con
PSS y con el mismo instrumento que la hospedada, son ~16 MB.**

**La razón 3 vuelve a estar en pie, con condiciones**, y las dos mediciones son correctas: miden
cosas distintas —memoria privada en Windows contra PSS en Android, con otro procesador y otro
sistema de páginas compartidas—. La que vale para decidir es la del teléfono, porque el teléfono es
el destino.

**Lo que sigue sin probarse, y es lo que decide de verdad:** precompilar el componente
(`wasmtime_component_serialize`) para no pagar los 163 MB en cada arranque.

---

# Precompilar el componente — el ajuste que cierra el número

**Medido el 2026-09-16** en el mismo Xiaomi, con `wasmtime_component_serialize` y
`wasmtime_component_deserialize_file`.

El paso 3 dejó un costo fijo incómodo: **compilar los 12 MB del runtime costaba 1,2 s y ~163 MB**, y
se pagaba **en cada arranque**. La C API tiene la salida: compilar una vez, guardar el resultado, y
después abrirlo desde disco.

## El antes y el después

| | tiempo | PSS con 0 instancias |
|---|---|---|
| **compilar** el componente | **1 109 ms** | **249 MB** |
| **deserializar** el precompilado | **4 ms** | **80 MB** |

**277 veces más rápido y 169 MB menos.** El artefacto pesa **11 MB en disco** y se escribe en 32 ms.

Que la memoria se desplome no es magia: el precompilado se **mapea desde disco**, así que son páginas
respaldadas por un archivo en vez de memoria anónima privada. Es la misma razón por la que el sistema
puede reclamarlas cuando le hagan falta.

## Y con el costo fijo fuera, el marginal se lee limpio

Tres repeticiones de cada uno, proceso fresco, `dumpsys meminfo`:

| instancias vivas | PSS (tres corridas) | marginal |
|---|---|---|
| *(sin motor)* | 96 · 95 · 95 MB | — |
| 0 — motor y componente precompilado | **80 · 80 · 80 MB** | — |
| 1 | **113 · 113 · 113 MB** | **+33 MB** |
| 2 | **143 · 142 · 142 MB** | **+29 MB** |
| 3 | **171 · 171 · 171 MB** | **+29 MB** |

**Idénticas hasta el megabyte.** Y monótona, que es lo que la serie anterior no era: midiendo con el
componente **compilado**, el PSS oscilaba entre 243 y 321 MB con tres instancias, porque Android
reclamaba páginas del código recién generado. **Con el precompilado ese ruido desaparece.**

**El marginal bueno es +29 MB por instancia viva, no los +16 que dio la serie sucia.**

## Contra la forma hospedada, en el mismo teléfono y con el mismo instrumento

| | primer módulo VIVO | vivos a la vez | costo fijo |
|---|---|---|---|
| **hospedada** | **+58,8 MB** | **uno**, por la regla de desalojo | — |
| **caja, compilando** | +~16 MB *(lectura sucia)* | tres | **+163 MB** |
| **caja, precompilada** | **+29 MB** | tres, y la serie es lineal | **~0** |

**Con el precompilado la caja gana desde el primer módulo:** la mitad de memoria por módulo vivo, sin
costo fijo, y sin la regla de «uno vivo a la vez».

## Y un detalle que multiplica el ahorro

**El artefacto precompilado es del runtime, no del módulo.** `dotnet.wasm` es idéntico para todas las
instituciones: los DLLs de cada módulo los carga *ese* runtime desde la carpeta montada.

Así que **son 11 MB en disco UNA VEZ, para todos los módulos**, no 11 MB por módulo. Es exactamente
el sustrato compartido que `P10` pide y que hasta ahora no existía.

## Lo que falta

- **El artefacto lo produce el aparato**, y depende de la versión de wasmtime, del procesador y de las
  opciones del motor. Hay que decidir **cuándo se genera** —al instalar el primer módulo, al
  actualizar Gnothi— y **qué pasa si no coincide**: `deserialize_file` tiene que fallar limpio y
  recompilar, y eso no se probó.
- **No se probó que sea seguro contra un archivo corrupto.** La cabecera dice, textual: *«This
  function is not safe to receive arbitrary user input»*. El artefacto vive en la carpeta privada de
  Gnothi, pero la regla hay que escribirla: **ese archivo lo produce Gnothi y nadie más lo toca.**

## Y qué pasa si el artefacto no coincide

**Medido el 2026-09-16**, rompiéndolo de cuatro maneras y midiendo la quinta con dos versiones
reales de wasmtime.

| cómo no coincide | qué hace wasmtime |
|---|---|
| **otra versión** — artefacto de la 48 abierto con la 44 | **error limpio**: *«Module was compiled with incompatible version '48'»* |
| cabecera volteada | **error limpio**: *«failed to parse precompiled artifact as an ELF · Unsupported ELF header»* |
| truncado a la mitad | **error limpio**: *«failed to parse section headers · Invalid ELF section header offset/size/alignment»* |
| **4 KB volteados en el medio** | **NO lo detecta.** Deserializó en 5 ms, corrió, y dio la respuesta correcta |

**Ninguno mató el proceso**, que era la pregunta que la documentación de la C API dejaba abierta
—*«This function is not safe to receive arbitrary user input»*—. Los tres primeros devuelven un
`wasmtime_error_t` y ahí se acaba.

**La prueba de versión es real, no simulada.** El artefacto lo produjo `wasmtime 48.0.2` y lo abrió
`wasmtime 44.0.0`, las dos bajadas de bytecodealliance. *(Intentar simularlo cambiando la versión
dentro del archivo no funciona: no está como texto.)*

### El camino de vuelta funciona, y es corto

El código hace lo que hay que hacer en producción: si no sirve, **se descarta y se recompila**. En la
misma corrida:

```
caja: EL PRECOMPILADO NO SIRVIO -> failed to load code for: … | Unsupported ELF header
caja: descartado; se recompila
caja: COMPILADO (1 173 ms, 11 MB)
caja: SERIALIZADO a disco (38 ms, 11 MB)
caja: ### instancia 1: 542 ms
      caja: Resumen() contesto: {"hayDatos":true,"examenes":19,…}
```

**El costo de que no coincida es un arranque lento, una sola vez.**

### El renglón cuatro es el hallazgo, y hay que decirlo fuerte

**Wasmtime valida la cabecera y la versión; NO valida el contenido.** Cuatro kilobytes cambiados en
medio del artefacto pasaron sin una advertencia, y el módulo corrió. *(Esta vez la respuesta salió
bien — probablemente esos bytes caían en una región que no se ejecuta. Eso es suerte, no una
garantía.)*

**Eso es exactamente lo que la cabecera de la C API advierte**, y convierte al artefacto en **entrada
de confianza**: no es un archivo que se pueda aceptar de nadie.

**Tres reglas que salen de esto, y van al diseño:**

1. **El artefacto lo produce Gnothi y vive en su carpeta privada.** Nunca viaja en el bundle de la
   institución, nunca se descarga, nunca se acepta de afuera. *(Tampoco podría: depende de la versión
   de wasmtime, del procesador y de las opciones del motor.)*
2. **Gnothi guarda un hash del artefacto y lo verifica antes de abrirlo.** Cuesta leer 11 MB y cierra
   el único renglón que wasmtime no cubre. Es barato y no está hecho.
3. **Si no sirve, se borra y se recompila** — ya está probado, y el costo es 1,2 s una vez.

---

# Dos de los cinco pendientes del paso 3, cerrados · 2026-09-16

## La huella del artefacto — cierra el renglón que wasmtime no cubre

Wasmtime valida la cabecera ELF y la versión del motor, **no el contenido**. Así que Gnothi guarda un
`sha256` al lado del artefacto y lo verifica **antes de abrirlo**. Medido en el teléfono:

```
A) limpio        COMPILADO (1 174 ms) · SERIALIZADO (52 ms) + huella
B) normal        huella verificada en 21 ms · DESERIALIZADO (5 ms)
C) 4 KB rotos    LA HUELLA NO COINCIDE (22 ms)
                 esperaba 59D84B04CFD4AACE…  encontró 52CB2A31588D7DA5…
                 descartado ANTES de abrirlo; se recompila (1 169 ms) y queda sano
D) otra vez      huella verificada en 22 ms · DESERIALIZADO (6 ms)
```

**El caso que wasmtime dejaba pasar —los 4 KB volteados en el medio— ahora se detecta**, y se detecta
**antes** de entregarle nada al motor.

| | |
|---|---|
| costo de la garantía | **22 ms** (leer 11 MB y hacer el sha256) |
| arranque completo verificado | **27 ms** (22 de huella + 5 de deserializar) |
| contra compilar | **1 109 ms** |

Y el precompilado sin huella **tampoco se acepta**: *«no trae huella; no se puede confiar»*, se borra
y se recompila. Un artefacto que nadie firmó no es un artefacto de Gnothi.

## El `.wasm` de un solo archivo — cierra el hueco del paso 2

Con el `wasi-sdk 24` instalado y `WasmSingleFileBundle=true`, el enlace nativo produce **un solo
archivo**:

| | archivos | tamaño |
|---|---|---|
| bundle suelto | `dotnet.wasm` + `icudt.dat` + `managed/` (34 dll) | **18 MB** |
| **un solo archivo** | `Lab.WasiCaja.wasm` | **6,3 MB** |

**Menos de la mitad**, y el runtime, ICU, el BCL y los DLLs del módulo van adentro.

**Y arranca más rápido:** el actor levanta en **247 ms** contra los 443–532 ms del bundle suelto — ya
no hay que buscar treinta y cuatro archivos en un sistema de archivos virtual.

### Lo que esto cierra, que es lo que importa

Ahora **no hay paquete que montar**: lo único que Gnothi monta es la carpeta concedida. Los nueve
intentos de fuga, con `--dir solodatos::/datos` y nada más:

```
  PUDO      leer MI PROPIO journal                -> 8 395 bytes
  NO PUDO   listar la carpeta de OTRO modulo      -> IO_PathNotFound_Path, /vecino/municipalidad
  NO PUDO   leer un archivo de OTRO modulo        -> IO_PathNotFound_Path
  NO PUDO   ESCRIBIR en la carpeta de otro modulo -> IO_PathNotFound_Path
  NO PUDO   listar una carpeta de AFUERA          -> IO_PathNotFound_Path, /Users
  NO PUDO   salir con .. desde la concedida       -> IO_PathNotFound_Path, /
  NO PUDO   salir a la RED por su cuenta          -> PlatformNotSupportedException
  NO PUDO   ESCRIBIR en su propio paquete         -> IO_PathNotFound_Path, /managed/…
  NO PUDO   listar la raiz que ve el modulo       -> IO_PathNotFound_Path, /
```

**Ocho de ocho cerrados.** Lo único que el módulo puede hacer es leer y escribir su propio journal.

Comparado con el paso 2, donde `..` caía en su propio paquete y **podía reescribir sus DLLs**: ahora
`/` no existe. Y el `fs_mutable: false` del paso 3 deja de ser necesario para eso — **el paquete ya
no es un archivo, es parte del `.wasm`**.

*(La red sigue fallando por falta de hilos y no por política; esa sigue abierta, y la palanca es
`add_wasi_http`, que con el Host embebido ya está en nuestras manos.)*

### Lo que costó

**El `wasi-sdk 24` pesa 1,2 GB** y el SDK de .NET lo busca por `$(WASI_SDK_PATH)` comprobando que
exista un archivo llamado **`VERSION24`** en la raíz — **que el tarball de bytecodealliance no trae**.
Hay que crearlo a mano o el build sigue diciendo *«Could not find wasi-sdk»* con el SDK ya instalado.

Y un dato del enlace que explica parte del costo por instancia: el linker pasa
**`--initial-memory=52428800`**, o sea **50 MB de memoria lineal inicial** por instancia. Eso es una
opción, no una ley: bajarla es una de las cosas que quedan por probar.

## Lo que queda de los cinco

| | estado |
|---|---|
| el módulo como *reactor* con funciones exportadas | **falta** — el `wasi-sdk` ya está, que era el prerrequisito |
| el borde por `instance_add_func` en vez de `stdout` | **falta** |
| el préstamo del borde y el contrato por el Host | **falta**, y depende del reactor |
| **la huella del artefacto** | **hecho y medido** |
| el contador de intentos negados | **falta** — hay que envolver la implementación de WASI |

## Bajar la memoria inicial — el último ajuste barato

En la línea del enlace nativo aparece `--initial-memory=52428800`: **50 MB de memoria lineal
inicial por instancia**, que sale de `$(WasmInitialHeapSize)` y es un valor por omisión, no una ley.

**El piso real es 13,7 MB**, y el linker lo dice con precisión:

```
wasm-ld : error : initial memory too small, 13642464 bytes needed
```

Es lo que ocupan los ensamblados y el ICU embebidos en el segmento de datos. Con **14 MB**
(`-p:WasmInitialHeapSize=14680064`), tres repeticiones de cada N, proceso fresco, `dumpsys meminfo`:

| instancias vivas | PSS (tres corridas) | marginal |
|---|---|---|
| *(sin motor)* | 72 · 73 · 73 MB | — |
| 0 — motor y componente precompilado | **73 · 72 · 73 MB** | **~0** |
| 1 | **105 · 105 · 104 MB** | +32 MB |
| 2 | **128 · 128 · 128 MB** | **+23 MB** |
| 3 | **151 · 151 · 152 MB** | **+23 MB** |

**De +29 a +23 MB por instancia viva**, y la base baja de 80 a 73 MB. Con el componente
precompilado y mapeado, **instanciar el motor cuesta cero**: N=0 mide lo mismo que sin motor.

Y lo demás también mejoró con el `.wasm` de un solo archivo: **compilar baja de 1 109 a 516 ms**, el
artefacto de 11 a **9 MB**, y el actor arranca en **~300 ms** contra los 365–826 del bundle suelto.

### Una trampa del cambio, y falla con un mensaje que no se parece a su causa

Con el bundle suelto los argumentos eran `dotnet.wasm Lab.WasiCaja /datos`: el runtime necesitaba
que le dijeran **cuál dll cargar**. En el `.wasm` de un solo archivo el ensamblado va adentro, así
que el primer argumento de verdad es la carpeta. Dejar el nombre puesto da:

```
System.ArgumentException: Storage configuration key 'path' points to 'Lab.WasiCaja',
which cannot be created or accessed as a directory
```

...y del lado del anfitrión lo único que se ve es `Exited with i32 exit status 1`, que no dice nada.
**Por eso el anfitrión lee la salida del módulo pase lo que pase**, no sólo cuando termina bien.

## Estado de los cinco pendientes del paso 3

| | |
|---|---|
| **la huella del artefacto** | **hecho y medido** |
| **el `.wasm` de un solo archivo** *(no estaba en la lista y cerró el hueco del paso 2)* | **hecho y medido** |
| el módulo como *reactor* con funciones exportadas | falta — el `wasi-sdk` ya está |
| el borde por `instance_add_func` en vez de `stdout` | falta |
| el préstamo del borde y el contrato por el Host | falta, y depende del reactor |
| el contador de intentos negados | falta |

---

# El canal del borde — cerrado, y por otro camino · 2026-09-16

El paso 1 dejó **541 bytes de ruido del motor en el canal del protocolo** y tumbó la primera versión
del Host con `'S' is an invalid start of a value`. Era la trampa de `E2` —*«stdout es el protocolo, y
el motor no lo sabe»*— en un sitio nuevo, y el parche fue saltarse lo que no empieza con `{`.

**Ya no hace falta el parche: el canal dejó de compartirse.**

```
gnothi: ruido del motor en el canal del protocolo: 0 bytes
```

## El arreglo, que es de tres líneas

Los dos siguen siendo `stdout`, **y ninguno es `Console.Out`**:

```csharp
var protocolo = new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true };
Console.SetOut(new StreamWriter(File.Create(Path.Combine(carpeta, "ruido-del-motor.log")))
    { AutoFlush = true });
```

- **`Console.OpenStandardOutput()` devuelve el descriptor 1 crudo**, y lo sigue devolviendo aunque
  después se cambie `Console.Out`. Ése queda para el protocolo, y nadie más lo tiene.
- **`Console.SetOut` desvía a un archivo todo lo que escriba quien use `Console.Write`** — el motor,
  la app, una librería cualquiera. Los 537 bytes de `Starting … Actor` y `1%2%3%…` aterrizan ahí.

**Y el orden importa, literalmente.** La primera versión lo hacía después de `app.Start()`, y ahí el
motor ya había escrito el progreso del replay: **quedaban 266 bytes**. Apartando el canal antes de
arrancar el actor, quedan **cero**.

## El muro que hay detrás, y conviene decirlo

Lo correcto de verdad sería que el anfitrión defina las funciones que el invitado importa
—`wasmtime_component_linker_instance_add_func`— y que el módulo sea un *reactor* con funciones
exportadas. **Las dos cosas necesitan un mundo WIT propio, y el SDK de .NET no lo expone:**

- `WasiApp.targets` exporta **exactamente cuatro** símbolos, fijos:
  `--export=malloc,--export=free,--export=__heap_base,--export=__data_end`. No hay ítem de MSBuild
  para agregar uno.
- El mundo del componente está clavado: el enlace pasa
  `--component-type … WasiHttpWorld_component_type.wit`.
- **`componentize-dotnet`** de BytecodeAlliance sí genera componentes con mundos WIT propios, pero
  usa **NativeAOT-LLVM** — otro runtime, no el intérprete de Mono. Eso invalidaría el paso 0 entero
  (el motor corre sobre Mono y usa reflexión a fondo) y hoy sólo compila en Windows.

**Así que el reactor no es trabajo pendiente: es un camino cerrado con este toolchain.** Lo que
quedaba de valor en él —que el actor viva entre preguntas— **ya lo teníamos**: el paso 1 midió la
segunda y la tercera consulta en 1 y 0 ms. Lo que faltaba era el canal, y el canal está.

## Y el segundo muro: el préstamo del borde ya no es posible

`P19` dejó escrito que **el borde y el contrato los presta el Host**, montados de solo lectura, para
que la lista de lo que el módulo expone sea **extraída** por código de Gnothi y no **declarada** por
el módulo.

**El `.wasm` de un solo archivo lo hace imposible**, y es el mismo cambio que cerró el hueco de
contención: si el paquete va adentro del wasm, **no hay archivo que Gnothi pueda sustituir**.

**Los dos pendientes están en tensión y hay que elegir:**

| | el borde lo presta el Host | el paquete va adentro del `.wasm` |
|---|---|---|
| la declaración | **extraída** por código de Gnothi | **declarada** por el módulo |
| `..` desde la concedida | cae en el paquete montado | **no lleva a ningún lado** |
| el módulo puede reescribir sus DLLs | sí, salvo `fs_mutable:false` | **no existen como archivos** |
| fugas cerradas (de nueve intentos) | 6 de 8 | **8 de 8** |

**La elección es el `.wasm` de un solo archivo**, por dos razones: la contención es medible y el
préstamo no compraba tanto como parecía. Un módulo que miente sobre lo que declara **ya lo detecta la
comprobación**, en sus dos mitades —lo declarado contra el manifiesto, y *«que conteste de verdad»*—,
y la segunda no depende de dónde esté el lector.

**Lo que se pierde y hay que decirlo:** la declaración vuelve a ser del módulo. `P19` decía que el
privilegio de mirar adentro *«se muda adentro con el lector»*; con el archivo único, **no se muda:
se cambia por la contención**.

## Los cinco pendientes del paso 3, al cierre

| | |
|---|---|
| **la huella del artefacto** | **hecho y medido** — 22 ms, detecta lo que wasmtime no |
| **el `.wasm` de un solo archivo** *(no estaba en la lista)* | **hecho y medido** — 6,3 MB, 8 de 8 fugas cerradas |
| **el canal del borde** | **hecho y medido** — 0 bytes de ruido, por otro camino |
| el módulo como *reactor* con funciones exportadas | **CERRADO POR EL TOOLCHAIN**, y lo que compraba ya lo teníamos |
| el préstamo del borde y el contrato | **DESCARTADO** a favor del archivo único |
| el contador de intentos negados | **falta** — hay que envolver la implementación de WASI de wasmtime |

---

# El último pendiente: el contador que no se puede, y el canario que sí

## Por qué el contador de intentos negados no se puede

`WebModule.Violations` cuenta lo que la CSP **rechaza**, y puede porque el navegador **deja pasar la
petición y después la niega**: hay un evento que reportar.

**Con los preopens de wasm no hay evento.** Al invitado nunca se le entregó el descriptor, así que
del otro lado no hay una negación: hay una **ausencia**. Por eso el intento devuelve
`DirectoryNotFoundException` y no «acceso denegado».

Y la C API no ofrece gancho ninguno:

| lo que `wasi_config` permite | |
|---|---|
| `preopen_dir`, `set_argv`, `set_env` | configurar |
| `inherit_*`, `set_std*_file`, `set_std*_custom` | redirigir stdio |
| **un callback sobre el sistema de archivos** | **no existe** |

`wasmtime_component_linker_allow_shadowing` permitiría sombrear la implementación de WASI, pero
sombrearla obliga a **definirla entera** con `instance_add_func`, que es el muro del mundo WIT.

**Así que el contador no es trabajo pendiente: es imposible con este toolchain, y por una razón que
es una virtud.** Lo que hacía falta contar en la CSP es lo que acá directamente no ocurre.

## Lo que sí se puede, y es lo que `D2` pedía

`D2` no pide un contador: pide que la contención sea **falsable** — *que se pueda intentar romper y
verla fallar*. Eso sí se puede, y de una manera que además cubre lo único que puede romperse de
verdad: **que la jaula esté mal armada**.

**El canario.** Gnothi arma una caja con **exactamente** la configuración que le va a dar al módulo,
mete adentro una sonda suya, y mira que las ocho fugas fallen. Medido en el teléfono:

```
A) jaula bien armada
   canario: 8 fugas cerradas de 8 · lee lo suyo: True
   canario: LA JAULA ESTA CERRADA
   caja: ### instancia 1: 463 ms

B) jaula mal armada a proposito (la carpeta de la app montada en /)
   canario: 6 fugas cerradas de 8 · lee lo suyo: True
   canario:   ABIERTA -> PUDO  salir con .. desde la concedida      -> 11 entradas
   canario:   ABIERTA -> PUDO  listar la raiz que ve el modulo      -> /bundle, /concedida,
                                /dotnet.cwasm, /dotnet.cwasm.sha256, /canario.txt
   canario: LA JAULA NO ESTA CERRADA -- no se abre ningun modulo
```

**Una jaula abierta no es una advertencia: no se abre ningún módulo.**

### Y lo que el canario encontró en la jaula mala es peor de lo que se buscaba

Mirar la lista de la raíz: con `/` montado, el módulo ve **`/dotnet.cwasm` y
`/dotnet.cwasm.sha256`** — o sea que **podría reescribir el artefacto precompilado Y su huella**, y
la comprobación de integridad que se agregó hoy no serviría de nada.

**Las dos defensas se protegen entre sí**, y ninguna de las dos lo dice por su cuenta:

- la **huella** detecta un artefacto corrupto… siempre que el módulo no pueda tocar la huella;
- el **archivo único** impide que lo toque… y el canario es lo que verifica que siga siendo así.

Eso no se dedujo: **lo encontró la sonda**, que es exactamente para lo que existe.

## Lo que el canario NO es

**No cuenta lo que un módulo malo intenta** — eso es invisible por construcción y no hay forma de
verlo. Lo que hace es **verificar la jaula**, que es lo que puede degradarse sin que nadie lo note:
un preopen de más, un `fs_mutable` mal puesto, una regresión al cambiar de empaquetado. El módulo
sigue funcionando igual en todos esos casos, y ése es el problema.

**Y en producción la sonda tiene que ser de Gnothi**, un `.wasm` chico suyo. En el spike se reusa el
del módulo por comodidad, y **eso no sirve en serio**: un módulo malo mentiría en su propio canario.

## Los cinco pendientes del paso 3, cerrados

| | |
|---|---|
| **la huella del artefacto** | **hecho** — 22 ms, detecta lo que wasmtime no |
| **el `.wasm` de un solo archivo** *(apareció en el camino)* | **hecho** — 6,3 MB, 8 de 8 fugas cerradas |
| **el canal del borde** | **hecho** — 0 bytes de ruido |
| el módulo como *reactor* | **cerrado por el toolchain**, y lo que compraba ya lo teníamos |
| el préstamo del borde | **descartado** a favor del archivo único |
| **el contador de intentos negados** | **imposible, y reemplazado por el canario** — hecho y medido |

---

# `entregar` por el borde — el único verbo que escribe

**Medido el 2026-09-16.** Era el último pendiente del paso 1: el módulo **declaraba**
`"entrega":true` y nunca se le había cruzado un sobre real.

Gnothi le entrega un sobre con dos exámenes nuevos —ids 400 y 401, **después del cursor 303 que el
propio módulo acababa de contestar**— con la forma que el publicador emite:

```json
{"account":{"state":"active"},"events":[
  {"id":400,"tipo":"ExamenAgregado","examen":"Hemoglobina","fecha":"2026-09-16T08:30:00","valor":13.8,"unidad":"g/dL"},
  {"id":401,"tipo":"ExamenAgregado","examen":"Colesterol","fecha":"2026-09-16T08:30:00","valor":195.0,"unidad":"mg/dL"}
],"last":401}
```

```
--- el cartero entrega dos examenes nuevos ---
entregar el sobre del publicador       14 ms  {"ok":true,"dice":"leidos=2 aplicados=2 descartados=0 rechazados=0 ultimoEvento=401 cuenta=active"}
hasta donde leyo DESPUES de entregar    0 ms  {"ok":true,"dice":401}
la consulta DESPUES de entregar         0 ms  {"ok":true,"dice":{"examenes":21,"analitosFueraDeRango":0,"ultimoEventoLeido":401}}
```

**El cursor pasó de 303 a 401 y la cuenta de 19 a 21 exámenes**, cruzando el borde.

Y en disco, el journal **real** —el de la carpeta concedida, no una copia en memoria— creció:

```
antes:    journal_000001.bin   8 175 bytes
despues:  journal_000001.bin   8 555 bytes
```

## Y la idempotencia, que es la mitad que faltaba comprobar

El **mismo** sobre, otra vez:

```
--- el mismo sobre, otra vez ---
entregar el MISMO sobre                 1 ms  {"ok":true,"dice":"leidos=2 aplicados=0 descartados=0 rechazados=2 …"}
hasta donde leyo despues del repetido   0 ms  {"ok":true,"dice":401}
la consulta despues del repetido        0 ms  {"ok":true,"dice":{"examenes":21,…,"ultimoEventoLeido":401}}
```

**`aplicados=0 rechazados=2`**, el cursor no se movió y la cuenta tampoco. **La idempotencia es del
módulo, no del cartero** —es su guarda de `Check`— y sobrevive al cruce sin que el borde tenga que
saber nada.

## Lo que esto cierra

| | |
|---|---|
| el módulo **escribe** su journal en la caja | **medido**: 8 175 → 8 555 bytes, en la carpeta real |
| **Gnothi sigue siendo el cartero** | el módulo no tiene red — no por disciplina: no se le dio `wasi-http` |
| `[Deliver]` cruza el borde | **medido**: 14 ms la primera, 1 ms la repetida |
| `[Cursor]` releva desde dónde pedir | **medido**: 303 → 401 |
| la idempotencia sobrevive | **medida**: el sobre repetido no asienta nada |
| el ruido del motor no toca el protocolo | **0 bytes**; los 537 al `ruido-del-motor.log` |

**Con esto los cinco verbos del borde están ejercitados**: `declarar`, `preguntar`, `consultar`,
`cursor` y `entregar`.

**Lo que sigue sin probarse** son las vistas **con parámetros** —`serie` y `tendencia` piden un
analito, y las siete se llamaron sin argumentos— e **iOS**, que no tiene JIT.

---

# El borde vivo con wasmtime embebido — lo que decidía el diseño

**Medido el 2026-09-16 en el Xiaomi.** Es la pregunta que el spike nunca tuvo que contestar, porque
en escritorio el Host **lanza `wasmtime.exe` como proceso** y se queda con sus tuberías. Embebido no
hay tuberías, y **la C API es asimétrica**:

| | |
|---|---|
| **stdout** | `wasi_config_set_stdout_custom` — **un callback**. El Host **oye** al módulo, sin archivos ni polling. |
| **stdin** | sólo `set_stdin_file` (una ruta) o `set_stdin_bytes` (un búfer fijo). **No hay callback.** |

O sea: **el Host puede oír al módulo pero no hablarle mientras corre.** Y sin resolver eso quedaban
dos salidas malas:

- **un `run` por pregunta** — el actor rearranca cada vez (~300 ms medidos). La cara de exámenes hace
  **21 lecturas** para pintar una pantalla: seis segundos. Inusable.
- **un proceso por módulo** — `B1` documenta que en móvil no se lanzan procesos, y ante un revisor de
  tienda «la app ejecuta un binario que corre código bajado» se ve peor, no mejor.

## La tercera salida: `set_stdin_file` apuntando a un FIFO

Funciona:

```
borde: FIFO creado en /data/user/0/…/files/pedidos.fifo
borde: FIFO abierto por los dos lados
borde: saludo en 320 ms
borde:   {"listo":true,"arranqueMs":288,"entradas":89,"declara":{"consultas":[…],"vistas":[…]}}
borde: 1a consulta                    170 ms  {"ok":true,"dice":{"examenes":19,…,"ultimoEventoLeido":303}}
borde: el cursor                        3 ms  {"ok":true,"dice":303}
borde: 2a consulta (actor ya vivo)      1 ms  {"ok":true,"dice":{"examenes":19,…}}
borde: una vista                       16 ms  {"ok":true,"dice":{"cantidad":19,"edad":34,"sexo":"mujer",…}}
borde: EL BORDE VIVO FUNCIONA EMBEBIDO
```

**El actor arranca una vez (288 ms) y después contesta en 1 ms.** Eso es exactamente lo que
`HybridShell` consigue hoy con `keepLoaded: true`, y es lo que hace pintable una pantalla que
pregunta veintiuna veces.

### Las dos piezas, y ninguna necesita un mundo WIT

```csharp
mkfifo(tuberia, 0600);                                    // libc, por P/Invoke
W.wasi_config_set_stdout_custom(wasi, callback, …);       // el Host OYE
W.wasi_config_set_stdin_file(wasi, tuberia);              // el Host HABLA
```

Y el invitado corre en **otro hilo**, porque `wasmtime_component_func_call` no vuelve hasta que el
módulo termina — que en modo borde es cuando el Host le dice `chau`.

### El orden es lo que costó la primera corrida

**Abrir un FIFO para leer bloquea hasta que haya escritor; para escribir, hasta que haya lector.**
`set_stdin_file` abre para leer ahí mismo, así que llamarlo sin escritor en vuelo **cuelga el hilo y
no se imprime una línea más** — el log se corta en «FIFO creado» y no hay error que leer.

Los dos lados tienen que estar esperándose: un hilo abriendo para escribir, y `set_stdin_file` en el
otro.

## Con esto el diseño de la cáscara queda cerrado

| | |
|---|---|
| un motor de wasm para **todos** los módulos | wasmtime embebido, componente precompilado y mapeado |
| una **instancia viva por módulo** | +23 MB de PSS cada una |
| el actor **vive entre preguntas** | 288 ms la primera, **1 ms** las siguientes |
| el Host habla y oye | FIFO para pedir, callback para recibir |
| sin procesos, sin mundos WIT, sin NativeAOT | todo con la C API y `libc` |

**Y `HybridShell` son dos campos** —`face` y `surface`—, así que la cáscara nueva es esa misma clase
con la superficie cambiada. **La cara no se entera**, que es lo que falta medir: cómo ve el WebView a
la caja cuando la pregunta viene de `gnothiPuente.consultar` y no de un arnés.

---

# La caja adentro de Gnothi — no como spike · 2026-09-16

**El spike nunca midió lo que importaba: cómo ve el WebView a la caja.** Un arnés que pregunta por
consola no es una cara. Así que la caja entró en Gnothi Android como cáscara de verdad.

## Lo que se escribió

| | |
|---|---|
| `Gnothi.MobileShared/Wasmtime.cs` | la C API, compartida con iOS |
| `Gnothi.MobileShared/CajaDeWasm.cs` | el motor, **uno para todos**: precompilado, huella, y el descarte-y-recompila |
| `Gnothi.MobileShared/InstanciaEnCaja.cs` | un módulo vivo con su borde: FIFO para hablarle, callback para oírlo |
| `Gnothi.MobileShared/CajaShell.cs` | la cáscara: `HybridShell` con la superficie mudada |
| `Gnothi.Host/CajaShellStub.cs` | el stub de escritorio, como los otros dos |
| `Gnothi.Mobile/LaCaja.cs` | lo que Android pone: rutas, log, y el interruptor de esta etapa |

Y **una costura nueva en `Module.ChooseShell`**: `ShellFactory`, que la plataforma puede poner para
sustituir una cáscara **a propósito y con nombre**, en vez de meter una bandera de laboratorio
adentro del switch. Devuelve `null` cuando no tiene opinión, que es lo normal.

**El APK pasa de 24 a 51,6 MB**: 27 MB de `libwasmtime.so` y 6,3 MB del `.wasm`.

## Lo que corrió

```
caja: examenes-en-caja.wasm copiado (6 MB)
caja: encendida para 'examenes'
caja: sustrato compilado y guardado (9 MB)
caja: examenes vivo -- {"listo":true,"arranqueMs":543,"entradas":66,
      "declara":{"consultas":["resumen","lista_de_examenes"],
                 "vistas":["cabecera","examenes","serie","analitos","tendencia","historial","alertas"],
                 "entrega":true,"cursor":true}}
```

**La caja arrancó dentro de Gnothi real, sobre el journal real de la persona** —66 entradas— y la
cara se abrió y pintó. **La cara no cambió ni un byte**: sigue preguntando por
`gnothiPuente.consultar` y no sabe dónde aterriza su pregunta.

## Y la comparación encontró el defecto, que es lo que esto vino a hacer

La misma cara, el mismo teléfono, el mismo journal, con la caja apagada y encendida:

| | híbrida | en caja |
|---|---|---|
| «En general, tus resultados vienen bien» | sí | sí |
| «TU ÚLTIMO CHEQUEO · 28 abr 2026» | sí | **no** |
| «17 de tus 18 valores están en su rango» | sí | **no** |
| «MERECE ATENCIÓN · 9» con las tarjetas de cada analito | sí | **no** |
| «TU HISTORIAL · 32 resultados» | sí | sí |

**La causa está en el borde y es de una línea.** `ElBorde.Atender` invoca así:

```csharp
return Ok(Json.De(porNombre[nombre].Invoke(superficie, null)));
```

**`null`** — los argumentos se descartan. Y de las siete vistas de exámenes, dos los piden:
`Serie(string analito)` y `Tendencia(string analito)`. Esas tiran adentro, el borde contesta
`ok:false`, y **la cara degrada en silencio**: no muestra un error, muestra menos pantalla.

Estaba anotado como pendiente desde el paso 1 —*«las vistas con parámetros no se probaron»*— y ahí
parecía un detalle. **Puesto en una cara de verdad es media pantalla.**

## Lo que esto deja dicho

1. **La cara no se entera de la mudanza**: mismo verbo, mismo puente, misma pantalla. Eso queda
   demostrado y era la pregunta.
2. **Un arnés no sustituye a una cara.** Los cinco verbos del borde estaban «ejercitados» y ninguno
   con argumentos, porque el arnés preguntaba lo que el arnés sabía preguntar.
3. **El siguiente paso es el borde con argumentos**, y después medir lo que todavía no se midió:
   cuántas preguntas hace la cara para pintar y cuánto tarda la pantalla entera, contra la híbrida.

---

# El borde con argumentos, cerrado — 2026-09-16

`ElBorde.Llamar` / `ElBorde.Ligar` ligan los parámetros desde un sobre que arma el Host:

```json
{ "args": { "analito": "Colesterol total" }, "accepts": "Json" }
```

`accepts` va **afuera** de `args` y se lee **antes**, que es la misma regla que `InProcessShell.Bind`
aplica con su parámetro reservado: el formato es del Host, no de quien pregunta. Si viajara adentro
del diccionario, una consulta podría elegirse su propio envoltorio.

**Medido en el teléfono, con la caja encendida sobre el journal real:**

| | antes | ahora |
|---|---|---|
| «TU ÚLTIMO CHEQUEO · 28 abr 2026» | no | **sí** |
| «17 de tus 18 valores están en su rango» | no | **sí** |
| «MERECE ATENCIÓN» | 8 | **9**, con las tarjetas |
| «TU HISTORIAL» con las cuatro visitas | sí | sí |

La pantalla en caja quedó **igual a la híbrida**, tarjeta por tarjeta.

## Y costó dos defectos más, los dos de comparar barato

**Uno.** El precompilado no estaba atado a su fuente: un `.wasm` nuevo con un `.cwasm` viejo y sano
al lado hacía que Gnothi siguiera corriendo **el código anterior, sin un solo error**. El artefacto
ahora se llama `sustrato-<huella16>.cwasm`, por el SHA-256 de su fuente.

**Dos, y es el mismo defecto un eslabón más arriba.** `LaCaja.PrepararLaCaja` decidía si copiar el
asset del APK **comparando el largo**. El build que traía el arreglo del borde salió con
**6 583 761 bytes, los mismos exactos** que el anterior —cambió metadata, no tamaño—, así que el
asset no se copió, la huella del archivo viejo no cambió, el precompilado viejo siguió sirviendo, y
la pantalla salió idéntica. Se perdió una corrida entera buscándolo en el lugar equivocado.

Ahora el asset se compara **por huella**, con el SHA escrito al lado de la copia. Y no cuesta más:
un asset de Android tampoco dice su largo sin leerlo entero, así que la versión que mentía ya pagaba
la lectura de 6 MB.

> **La lección:** el largo no identifica un contenido. Se arregló en `CajaDeWasm` y se volvió a
> cometer en `LaCaja` el mismo día, en la misma cadena.

## Lo que NO era de la caja, y se comprobó apagándola

**`Color de la orina` y `Color de las heces` pintan `undefined`** en la cifra grande. Se verificó con
la caja apagada, en híbrida, misma cara y mismo journal: **sale idéntico**. Es un defecto viejo de
la cara de exámenes, no de la caja.

`cara.js:264` tiene dos ramas y la forma tiene tres:

```js
var v = r.presencia ? texto(r.presencia.ultimoResultado)
                    : valor(t.valorFinal) + ' <span>' + texto(t.unidad) + '</span>';
```

La vista `tendencia` devuelve `presencia` para «presencia en el tiempo», `hechos` para una serie
continua, y **`constancia` para «categoría en el tiempo»** —que no tiene rama—, así que
`t = {}` y `valor(undefined)` sale impreso. El valor está ahí: `constancia.ultimaCategoria`, que la
caja devolvió `"ámbar"` cuando se le preguntó directo. Va a la deuda de la cara de exámenes.

**`no se pudo traer lo nuevo: Connection refused (127.0.0.1:8095)`**, al pie, tampoco es de la caja:
es el **cartero**, que es de Gnothi. El publicador de prueba corría en la laptop pero faltaba el
túnel. Se arregla con una línea, y va al runbook:

```
adb reverse tcp:8095 tcp:8095
```

Vale distinguirlo de su vecino: `Permission denied (127.0.0.1:8095)` es **INTERNET sin declarar** en
el manifiesto —documentado en `MainActivity`—; `Connection refused` es que el socket abrió bien y no
había nadie del otro lado.

## Y lo que falta medir, dicho con precisión

Arriba quedó escrito *«cuánto tarda la pantalla entera»*, y la frase no dice nada. Contado en
`cara.js:528-562`, **pintar la pantalla de exámenes son veintiuna preguntas**:

```
historial          1
alertas            1
analitos           1
tendencia × 18     18    ← de a una y EN ORDEN, encadenadas con reduce
                   ──
                   21
```

Y la pantalla **se pinta dos veces** —lo que ya hay, y otra vez cuando vuelve el cartero—, así que
abrir el módulo son **~42 idas y vueltas por el borde**.

Lo verificado hoy es que el **contenido** sale igual. Lo que no está medido es el **costo de cada
ida y vuelta**:

| | en proceso | en la caja |
|---|---|---|
| qué es una consulta | una llamada a un método de un DLL cargado | escribir en el FIFO · despertar el hilo del invitado · invocar por reflexión · serializar adentro · volver por el callback |
| pueden solaparse | sí | **no**: `SemaphoreSlim(1,1)` |

El spike midió **~1 ms por consulta**, pero sobre vistas simples y por el arnés. `tendencia`
serializa una interpretación entera, dieciocho veces seguidas, y es justo la que el arnés no sabía
pedir. **A 1 ms son 21 ms y nadie se entera; a 30 ms son 600 ms de pantalla en blanco que la
híbrida no tiene.**

> Y una precisión de vocabulario, porque se presta a confusión: **la caja no es una forma del
> paquete**. Exámenes es y sigue siendo **híbrido** — mismo `.gmod`, mismo manifiesto, misma cara.
> La caja cambia **el anfitrión**. Por eso «contra la híbrida» quiere decir, siempre, **contra el
> mismo módulo corriendo en proceso**, que es lo que pasa con el interruptor apagado.

---

# Separar el sustrato del módulo — la pregunta, contestada · 2026-09-16

**Se puede.** Y tiene un precio, medido.

> **DÓNDE SE MIDIÓ TODO LO QUE SIGUE, y hace falta decirlo antes que nada: EN LA LAPTOP.** Los cinco
> pasos de carga y el borde contestando, con el CLI de `wasmtime` v48; la jaula de dos puertas y el
> sólo lectura, con la C API por `Lab.WasiCaja.Embebido`. **Nada de esto corrió en el teléfono**:
> ahí el motor es otro binario (`libwasmtime.so` para aarch64), la arquitectura es otra, y el camino
> no es el del spike sino el de Gnothi — `Cage`, `BoxedInstance`, `WasmBox`.
>
> Así que lo contestado acá abajo es **«el toolchain lo permite y cuesta esto»**, no «anda en el
> aparato». La distinción no es formalismo: ese día, cuatro veces, «compila y anda acá» no fue lo
> mismo que «corre allá».
>
> **Y el aparato ya contestó.** El 2026-09-17 el sustrato cargó exámenes desde el bundle y el canario
> cantó en el teléfono, en las dos condiciones — ver «El sustrato con el módulo afuera, corrido en el
> teléfono», al final de este documento. La salvedad queda en pie para **los números de acá abajo**,
> que siguen siendo de laptop; ya no para la pregunta de si anda.

## La pregunta que decidía todo

¿Puede el invitado cargar un ensamblado que **no venía adentro del `.wasm`**? Si no, la separación
es imposible con este toolchain y había que decirlo con esa palabra. Nadie lo había probado.

Se construyó un segundo build del mismo proyecto — `-p:ConModulo=false` — que **no referencia
`Examenes.App`**: sólo el motor, el BCL, el borde y el contrato. El módulo entra por una segunda
carpeta que el anfitrión monta.

**Cinco pasos, y se informan uno por uno a propósito** (la misma forma que la sonda de iOS: un «no
cargó» sin decir en cuál de los pasos cuesta una tarde):

```
1) la carpeta del modulo esta montada .......... OK (3 dll)
2) Assembly.LoadFrom ........................... OK (Examenes.App)
3) GetTypes .................................... OK (57 tipos)
4) la clase marcada ............................ OK (ExamenesSurface)
5) construida sobre la carpeta concedida ....... OK
```

Y el borde contesta de verdad, con el journal real:

```
{"listo":true,"arranqueMs":14,"entradas":-1,"declara":{"consultas":["resumen","lista_de_examenes"],
 "vistas":["cabecera","examenes","serie","analitos","tendencia","historial","alertas"],…}}
{"ok":true,"dice":{"analito":"Color de la orina","lectura":{…"veredicto":"estable"…}}}
```

**`arranqueMs` NO es comparable con el del corte anterior** y conviene decirlo: allá el programa
hacía `app.Start()` explícito, que hidrata el journal entero; acá la superficie se construye y la
hidratación ocurre en la primera consulta. 14 ms es construir, no arrancar. El número de arranque
real hay que volver a tomarlo.

`entradas` sale en **-1** porque eso lo sabía `app.Actor`, que era del módulo. El sustrato no tiene
por qué conocer el actor de nadie — es justamente lo que se separa.

## Los dos defectos del camino, y el segundo es estructural

**Uno · el motor se lo lleva el trimmer.** `Puppets.Portador` quedó fuera del publish: el sustrato lo
**presta** y no lo **usa**, así que para el recortador es código muerto. Falló en el paso 3 con
`FileNotFoundException: Could not load file or assembly 'Puppets.Portador'` — o sea **después** de
que `LoadFrom` saliera bien. Se enraíza con `TrimmerRootAssembly`.

Y de paso apareció un dato que hay que sostener: **cada módulo del repo tiene su copia del
Portador** — seis, idénticas byte a byte hoy. `Gnothi.Host` referencia la de `Examenes`, así que el
sustrato referencia esa misma. Idénticas hoy no es una garantía: el día que una se separe habría dos
motores según dónde corra el módulo.

**Dos, y es el que cuesta plata: el sustrato no se puede recortar contra código que no ha visto.**

Con el recorte puesto, el paso 5 muere así:

```
TypeLoadException: Could not load type of field 'ExamenesSurface:app' (4) due to:
Could not resolve type with token 01000013 from typeref
(expected class 'System.Lazy`1' in assembly 'System.Runtime')
```

El trimmer se llevó `System.Lazy<T>` **del BCL** porque el sustrato no lo usa. El módulo sí. Y eso no
es un caso particular: es la forma del problema. Un sustrato que sirve a cualquier módulo no puede
recortarse contra su propio código.

## El precio, medido

| | bytes del `.wasm` | piso de memoria | ¿corre? |
|---|---|---|---|
| sustrato **+ módulo adentro** (el corte anterior) | 6 582 225 | 14 MB | sí |
| sustrato **solo**, recortado | 6 387 009 | 14 MB | **no** — `System.Lazy` recortado |
| sustrato **solo**, sin recortar | **25 592 305** | **32 MB** | **sí** |

Sin recortar, el enlace primero se niega — `wasm-ld: initial memory too small, 31 396 080 bytes
needed` — y hay que subir `WasmInitialHeapSize` de 14 a 32 MB.

**Separar cuesta +19 MB en el artefacto y +18 MB de piso de memoria.** Se paga **una vez** —el
sustrato es uno para todos los módulos— contra los 12,4 MB **por módulo** de la forma hospedada, así
que a partir del segundo módulo sigue ganando. Pero deja de ser gratis y hay que decirlo.

## Lo que falta probar antes de dar el número por bueno

`PublishTrimmed=false` es el martillo. El camino del medio es **enraizar los ensamblados del
framework** (`TrimmerRootAssembly` sobre `System.Runtime`, `System.Collections`, …) en vez de apagar
el recorte entero. Eso puede recuperar buena parte de los 19 MB, y no está medido.

Mientras tanto el número honesto es el de la tabla: **25,6 MB y 32 MB de piso**.

## El enraizado, medido — y cambia la respuesta · 2026-09-16

La conclusión provisional era «separar cuesta +19 MB». **Con el mecanismo correcto cuesta +4,5.**

| configuración | bytes del `.wasm` | piso | ¿corre? |
|---|---|---|---|
| sustrato **+ módulo adentro** (el corte anterior) | 6 582 225 | 14 MB | sí, pero **un wasm por módulo** |
| sustrato solo, recortado | 6 387 009 | 14 MB | no — `System.Lazy` |
| sustrato solo, **+ `TrimmerRootAssembly`** ×4 | 6 387 009 | 14 MB | no — **idéntico al byte** |
| sustrato solo, **+ descriptor** (sólo CoreLib) | 11 061 408 | 17 MB | no |
| sustrato solo, **+ descriptor** (4 ensamblados) | **11 131 552** | **17 MB** | **sí** |
| sustrato solo, sin recortar | 25 592 305 | 32 MB | sí |

### Hay dos mecanismos y sólo uno se honra

**`TrimmerRootAssembly` no hace NADA en el pipeline de WASI.** El `.wasm` sale **idéntico al byte**
—6 387 009— con y sin raíces sobre `System.Private.CoreLib`, `System.Runtime`, `System.Collections`
y `System.Linq`, y falla igual.

**`TrimmerRootDescriptor` sí.** Y se supo por accidente: la primera versión del XML reventó al
parsearse, lo cual probó que ILLink lo estaba leyendo de verdad.

> **Y casi se documenta al revés.** Antes de esto quedó escrito en el csproj que enraizar
> `Puppets.Portador` era lo que lo había hecho llegar al publish. **Era falso**: quitando la raíz,
> el Portador llega igual. Lo que lo arregló fue corregir la ruta del `ProjectReference`, que
> apuntaba a una carpeta inexistente. Se comprobó a propósito —publicar con la raíz quitada— antes
> de dejar el comentario. El comentario ahora dice que no sirven.

### La cuenta, rehecha

El sustrato se paga **una vez**; la hospedada, **por módulo**. Y el corte anterior también se paga
por módulo, porque el `.wasm` lleva exámenes adentro: un segundo módulo en caja necesitaría su
propio wasm.

| módulos | hospedada (12,4 c/u) | caja **junta** (6,6 c/u) | caja **separada** (11,1 + 0,2 c/u) |
|---|---|---|---|
| 1 | 12,4 MB | **6,6 MB** | 11,3 MB |
| 2 | 24,8 MB | 13,2 MB | **11,5 MB** |
| 3 | 37,2 MB | 19,8 MB | **11,7 MB** |
| 5 | 62,0 MB | 33,0 MB | **12,1 MB** |

**Con un módulo la junta gana; del segundo en adelante gana la separada**, y a partir de ahí la
distancia sólo crece. Contra la hospedada, la separada gana desde el primero.

### Lo que no se midió

- **Si hacen falta más ensamblados con OTRO módulo.** Los cuatro salieron de hacer correr exámenes.
  Compras o cargas pueden pedir uno más, y el síntoma sería el mismo `TypeLoadException` con otro
  nombre. **La lista es empírica, no derivada**, y eso hay que decirlo donde se use.
- **El arranque real.** `arranqueMs = 17` es construir la superficie; la hidratación del journal
  ocurre ahora en la primera consulta. No es comparable con los 288/583 ms del corte anterior.

## La jaula de dos puertas, en Gnothi · 2026-09-16

Separar el sustrato del módulo obliga a montar **una segunda carpeta**: los DLL del módulo, que
antes viajaban adentro del `.wasm`. Y una puerta nueva no se agrega y ya — **se verifica**.

```
  /datos    la carpeta concedida    leer Y escribir    es el registro de la persona
  /modulo   su propio paquete       SOLO LECTURA       es lo que la institución firmó
```

**Por qué el paquete va de sólo lectura**, y no es precaución: un módulo que puede escribir sus
propios DLL reescribe el código que corre la próxima vez, y ninguna firma lo detectaría — el paquete
se verificó **al dar el alta**, no en cada arranque. `fsMutable: false` es lo que lo hace imposible
en vez de meramente descortés.

### Lo que se tocó

`Cage.Build(concedida, paquete)` · `BoxedInstance` le pasa al invitado `--modulo /modulo <entrada>`
· `BoxShell` saca las dos cosas de `Shell.Arguments[2]` —**el mismo campo que usa la forma
híbrida**, así que el `.gmod` no cambia ni se redeclara nada— · `WasmBox.Open` los reenvía.

### El canario creció, y esa es la parte que importa

Pasó de «una que debe poder y ocho que deben fallar» a **dos y ocho**:

```
MustWork = ["MI PROPIO journal", "MI PROPIO paquete"]
```

La segunda es nueva y es la mitad que se olvida: **una puerta comprobada sólo por lo que NO debe
dejar pasar puede pasar la prueba estando tapiada**. Ahora el veredicto las busca por nombre, no por
cuenta, y el log dice cuál falta — «no puede leer su registro» y «no puede leer su paquete» son dos
cosas rotas distintas y necesitan dos arreglos distintos.

### Medido, por la misma llamada que hace `Cage.Build`

El CLI de wasmtime **no sabe montar de sólo lectura** (`--dir` no tiene modo), así que esto se probó
por la C API, con `Lab.WasiCaja.Embebido`:

```
PUDO      leer MI PROPIO journal          -> 6 bytes
PUDO      leer MI PROPIO paquete          -> 6 bytes
NO PUDO   ESCRIBIR en su propio paquete   -> IOException          ← fsMutable: false
NO PUDO   listar la carpeta de OTRO       -> DirectoryNotFound
NO PUDO   leer un archivo de OTRO         -> DirectoryNotFound
NO PUDO   ESCRIBIR en la carpeta de otro  -> DirectoryNotFound
NO PUDO   listar una carpeta de AFUERA    -> DirectoryNotFound
NO PUDO   salir con .. desde la concedida -> DirectoryNotFound
NO PUDO   salir a la RED por su cuenta    -> PlatformNotSupported
NO PUDO   listar la raiz                  -> DirectoryNotFound
```

**Y la clase de excepción es el dato.** `DirectoryNotFoundException` es «eso no existe para vos»;
`IOException` sobre el paquete es «existe y no te dejo escribirlo». Son dos contenciones distintas y
se distinguen en la salida.

**Y el canario lo detecta si no está.** Montando el paquete CON escritura —lo que hace el CLI, que
no puede expresar otra cosa— la cuenta baja a 7 de 8 y el veredicto es que la jaula no está cerrada.
La comprobación está viva, no es un `return true` con adornos.

### Dos trampas del camino

**Montar `/` tapa a los demás preopens.** Con el bundle montado en `/`, ni `/datos` ni `/modulo`
resuelven: WASI busca dentro del preopen más general y ahí no están, así que **todo** da
`IOException`, incluido lo que tiene que poder. `Cage.Build` no monta `/` y por eso no le pasa; el
probe embebido sí lo hacía, de la época en que los DLL del módulo vivían ahí. Se le agregó
`--sin-raiz` para igualar las dos jaulas.

**Git Bash convierte las rutas del invitado.** `/datos` llegó como `C:/Program Files/Git/datos`.
Pero `MSYS_NO_PATHCONV=1` rompe las del **anfitrión** —`/tmp/...` deja de resolverse—. Hay que
pasar las del anfitrión en forma de Windows Y apagar la conversión.

### Lo que falta

El código de Gnothi ya monta las dos puertas y compila. **El asset sigue siendo el `.wasm` con
exámenes adentro**, que ignora `--modulo`: falta cambiarlo por el del sustrato solo y correr el
canario en el teléfono, en las dos condiciones.

## El asset cambiado: de «exámenes en caja» al sustrato · 2026-09-17

`Gnothi.Mobile/Assets/examenes-en-caja.wasm` (6 582 225) salió;
**`Gnothi.Mobile/Assets/sustrato.wasm` (11 131 552) entró.**

**El nombre cambió porque ya no decía la verdad.** Eso no es «exámenes en una caja»: es el runtime
de .NET, el BCL, el motor y el borde, **uno para todos los módulos**. Lo que era exámenes ahora entra
desde su bundle, por el segundo montaje.

### Lo que cuesta en el APK, y es menos de lo que el wasm sugiere

| | bytes |
|---|---|
| APK con `examenes-en-caja.wasm` | 51,6 MB |
| APK con `sustrato.wasm` | **53,2 MB** |

El `.wasm` creció 4,5 MB y **el APK sólo 1,6**: el BCL sin recortar comprime bien. El número que
importa para la tienda es el segundo.

### Y dos fugas de disco que el cambio de nombre hizo visibles

Ninguna es del cambio: estaban desde antes y el rename las sacó a la luz.

**Los artefactos precompilados nunca se borraban.** Se llaman por la huella de su fuente —que es lo
que hace imposible correr uno viejo— y eso significa que **cada sustrato que alguna vez se instaló
deja 9 MB para siempre**. En la primera actualización nadie lo nota; en la quinta son 45 MB del
almacenamiento de la persona gastados en artefactos de wasm que ya no están en el aparato.
`WasmBox.Prepare` ahora barre los `sustrato-*.cwasm` que no son el actual, **después** de abrir el
vigente: un barrido que falle no cuesta nada porque la caja ya está arriba.

**Y el `.wasm` viejo tampoco.** `examenes-en-caja.wasm` quedaría en la carpeta privada, 6,5 MB que
ya no nombra nadie. `TheBox` barre cualquier `.wasm` que no sea el actual — **por regla y no por ese
nombre**, porque el próximo rename haría lo mismo.

## El sustrato con el módulo afuera, corrido en el teléfono · 2026-09-17

Esta es la corrida que faltaba, y cierra de una vez las dos cosas que estaban abiertas: que el
sustrato cargue exámenes **desde el bundle del módulo** por el segundo montaje, y que el canario
cante en el aparato real.

### Condición normal — `--ez caja true`

```
caja: sustrato.wasm copiado, huella E2EC4A1793BFD2F4
caja: sustrato viejo borrado (examenes-en-caja.wasm)
caja: encendida para 'examenes'
caja: sustrato compilado y guardado (16 MB)
caja: artefacto viejo borrado (sustrato-D3D0E1C013C1D915.cwasm)
caja: artefacto viejo borrado (sustrato-C191ED121D0AE032.cwasm)
caja: canario -- 8 de 8 fugas cerradas; 2 de 2 puertas abiertas
caja: canario -- LA JAULA ESTA CERRADA
caja: examenes vivo -- {"listo":true,"arranqueMs":34,"entradas":-1,"declara":{...7 vistas...}}
```

Cinco cosas en ese log, y ninguna era obvia antes de correrlo:

- **El sustrato abre un módulo que no lleva adentro.** `arranqueMs: 34` es el módulo cargándose por
  `Assembly.LoadFrom` desde `/modulo`, la carpeta que el Host monta de sólo lectura. El `.wasm` no
  sabe qué es exámenes.
- **Los dos barridos funcionaron**, y borraron cosas reales: el `.wasm` anterior y **dos** artefactos
  precompilados de sustratos que ya no están. Eso son 6,5 MB + 18 MB que estaban muertos en la
  carpeta de la persona.
- **El canario, 8 de 8 y 2 de 2**, en el aparato. La jaula que estaba medida en la laptop es la misma
  acá.
- **La cara pinta idéntica a la híbrida** — cabecera, «17 de tus 18 valores», MERECE ATENCIÓN · 9, las
  tarjetas de analitos. Que es el punto: la persona no ve ninguna diferencia.
- **Las 7 vistas y los 2 verbos de consulta se declaran igual.** La superficie no cambia porque el
  ejecutor cambió.

### Condición de control — `--ez jaulamala true`

Y ésta es la que le da valor a la anterior, porque **un canario que siempre canta «cerrada» se ve
exactamente igual que uno que funciona**. Con la carpeta privada montada en `/`:

```
caja: canario con la jaula MAL ARMADA a proposito (la carpeta privada montada en /)
caja: canario -- 6 de 8 fugas cerradas; 2 de 2 puertas abiertas
caja: canario   FUGA ABIERTA -> PUDO  salir con .. desde la concedida  -> 15 entradas
caja: canario   FUGA ABIERTA -> PUDO  listar la raiz que ve el modulo  -> 15 entradas
caja: canario -- LA JAULA NO ESTA CERRADA; no se abre ningun modulo
caja: prueba del canario con la jaula mala -- canto abierta (bien: la vio)
```

La vio, dijo cuáles, y **cerró la caja**. Las dos puertas legítimas siguieron abriendo, que es lo que
hace que la señal sea específica: no es «algo anda mal», es «estas dos fugas, y sólo estas dos».

**Y no deja residuo.** Después de la prueba de la jaula mala, el canario normal volvió a cantar 8 de
8 y exámenes abrió. Es a propósito: `PassesWithBrokenCage` no toca `sang`, el veredicto real, así que
la prueba de control no puede envenenar la operación.

### Lo que el listado de la jaula mala encontró de paso, y no lo buscaba

El canario, para probar que la fuga está abierta, **imprime lo que el módulo alcanza a ver**. En la
jaula mala eso fue el listado entero de la carpeta privada de Gnothi —los `journals`, los `Modulos`,
las credenciales— que es exactamente el daño que la jaula existe para impedir, y verlo escrito es más
convincente que cualquier número.

Pero además **delató una fuga de disco propia**, que no tiene nada que ver con la contención: entre
los 15 renglones estaban `sustrato.cwasm` y `sustrato.cwasm.sha256`, del esquema de nombres **anterior
al actual**. El barrido buscaba `sustrato-*.cwasm`, con guion, así que esos dos no los iba a borrar
nadie, nunca. Corregido a `sustrato*.cwasm`.

**La lección no es de ese nombre, es del patrón**: un barrido escrito contra el esquema vigente el día
que se escribió deja de barrer el día que el esquema cambia —que es justo el día en que hace falta.
Es la tercera vez en este set que una comparación barata cuesta la tarde: el largo que mentía, el
artefacto viejo que corría, y ahora el barrido que no barría.

### Lo que esto cierra, y lo que no

| | |
|---|---|
| El sustrato carga el módulo desde el bundle, en el teléfono | **cerrado, medido 2026-09-17** |
| El canario canta en el aparato, en las dos condiciones | **cerrado, medido 2026-09-17** |
| La cara en caja se ve igual que en híbrida | **cerrado, comparado a ojo** |
| El barrido de `sustrato*.cwasm` sin guion | **cerrado, medido 2026-09-17** |
| Lo que cuesta pintar la pantalla entera contra la híbrida | a medir, y no es prioridad |

El barrido corregido se verificó **con el mismo instrumento que encontró el defecto**, que es la
parte elegante: se reinstaló, se abrió exámenes —`caja: artefacto viejo borrado (sustrato.cwasm)`— y
se volvió a correr la jaula mala para leer la raíz. **De 15 renglones a 13**: los dos del esquema
viejo ya no están. El listado que prueba la contención sirve también de inventario.

> **La piedra del día, y no es del código.** Entre una corrida y otra el build se cayó con **403** del
> feed privado: no hay proveedor de credenciales de Azure Artifacts instalado (`~/.nuget/plugins` no
> existe) y `Ncubo.Puppeteer 3.0.0-beta.10044` ya no estaba en la caché local. Y el restore fallido
> **pisa `obj/project.assets.json`** —queda un stub de 5 KB—, así que después del 403 tampoco sirve
> `--no-restore`: se pierden las dos salidas de una vez. Se resolvió dando acceso al feed.
