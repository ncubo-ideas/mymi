# Spike · La caja de wasm en iOS

**Qué es esto:** el plan para que alguien con **Mac e iPhone** conteste lo único que decide si la
caja existe en iOS. Está escrito para correrse de arriba hacia abajo y **abandonarse en el primer
paso que falle** — cada paso tiene su criterio de muerte escrito.

Contexto: `B12` (sección de iOS) y `F8`. Lo que hay que leer antes de tocar la Mac:
[`PENDIENTE-DE-PROBAR-EN-MAC.md`](PENDIENTE-DE-PROBAR-EN-MAC.md) — **pero ese documento es de la
sala, y esto es de la caja. Son dos cosas distintas y no se bloquean.**

---

## 0 · Lo que YA está contestado, para no volver a correrlo

| | |
|---|---|
| ¿el sustrato traduce a `pulley64`? | **SÍ**, medido el 2026-09-17 |
| ¿corre, los cinco pasos del cargador? | **SÍ**, sin tocar una línea del sustrato |
| ¿cuánto cuesta? | **×19–24** contra Cranelift |
| ¿de dónde sale ese número? | **de una laptop Windows x86_64 interpretando `pulley64`** |

**Eso NO es iOS.** Nunca corrió ni la caja ni Pulley sobre un iPhone ni sobre un simulador. Lo que
sí corrió en iOS es **la sala**, que es la otra forma.

Y hay tres renglones de `B12` que son **lectura de documentación, no experimento**:

- que Apple no le deja a un proceso de terceros escribir memoria ejecutable;
- que *«statically linking in a single precompiled module is not supported at this time»*;
- que el paquete comunitario de wasmtime para iOS sirve.

**Ninguno se verificó.** Este spike existe para convertir esos tres en sí o no.

---

## 1 · Las cuatro preguntas, en orden de qué invalida a qué

1. **¿Existe un `wasmtime` con modelo de componentes que compile para `aarch64-apple-ios`?**
   Si no, la caja en iOS no existe y lo demás sobra.
2. **¿Linkea y carga dentro de una app de .NET iOS?** Es donde muere el 90 % de los embebidos.
3. **¿El motor arranca en modo Pulley y abre el `.cwasm` precompilado?** Acá se falsifica —o se
   confirma— la frase de la documentación.
4. **¿Cuánto cuesta en un A17, y entra en memoria?** Recién acá los números valen para producto.

> **La cuarta trae un invitado que no está en `B12`: la memoria.** En Android cada módulo vivo
> costó **+29 MB**. iOS mata la app cuando se pasa del límite de jetsam, y wasmtime **reserva
> espacio de direcciones por instancia** por omisión. Nadie midió el RSS de la caja en un iPhone.
> Es sospecha, no medición — pero es barata de contestar y cara de descubrir tarde.

---

## 2 · La decisión de forma: NO tocar `Gnothi.iOS` todavía

**El spike va aparte, en `Spikes/Lab.WasiCaja.iOS`, espejo de `Spikes/Lab.WasiCaja.Android`** —
que ya tiene exactamente esta forma: `MainActivity.cs`, `Borde.cs`, `Canario.cs` y nada más.

La razón es que `Gnothi.iOS` **hoy no compila y no se sabe por qué** (todo el trabajo del 22 al 24
se hizo a ciegas desde Windows). Si se mete la caja ahí primero, un rojo no distingue entre «la
caja no anda en iOS» y «el renombrado de la sala rompió algo». **Una falla tiene que querer decir
una sola cosa.**

Además `Gnothi.iOS` arrastra el feed privado, los App Intents, la `.a` de Swift y el App Group.
Nada de eso hace falta para contestar las cuatro preguntas.

---

## 3 · Paso 0 — en Windows, ANTES de mandar el paquete

No consume tiempo de Mac y evita el viaje de ida y vuelta.

```bash
wasmtime compile --target pulley64 sustrato.wasm -o sustrato-pulley64.cwasm
sha256sum sustrato-pulley64.cwasm
```

**Con wasmtime v48.0.2 y no otra versión.** Es la de la medición del 17 y la que hay en
`.wasmtime/48.0.2/`. Si la librería de iOS se construye de otra versión, el `.cwasm` no abre — y
peor: si abre, la comparación contra los números de la laptop no dice nada. Es el mismo problema
que el `.so` duplicado que documenta el `.csproj` de `Lab.WasiCaja.Android`.

**Lo que viaja a la Mac:**

```
sustrato.wasm                 11 131 552 bytes   (por si hay que recompilar alla)
sustrato-pulley64.cwasm       ~15,5 MB
sustrato-pulley64.cwasm.sha256
dominio/                      el de examenes, de solo lectura
journal/                      DOS: uno vacio y uno real (~225 eventos, el del telefono)
```

> **Los dos journals no son un lujo.** El vacío reproduce la medición del 17 para que los números
> sean comparables. El real es el único que dice cuánto tarda de verdad — `B12` mide el replay con
> 18 eventos y avisa que el teléfono tiene ~225, así que **el número real está por encima, no por
> debajo**.

---

## 4 · Paso 1 — ¿existe el motor? *(el portón)*

**bytecodealliance no publica nada para iOS.** En Android el camino entero fue posible porque el
`libwasmtime.so` de `aarch64-android` viene oficial. Acá hay que construirlo o adoptar el de la
comunidad, y **eso cambia de quién es el motor**: pasa a ser un costo permanente, una vez por
versión de wasmtime, para siempre. No es bloqueante; es una línea en el presupuesto que hoy no está
en ninguna medición.

Dos caminos, en este orden:

**a. Desde fuente, que es el que deja el control:**

```bash
rustup target add aarch64-apple-ios aarch64-apple-ios-sim
git clone --branch v48.0.2 --depth 1 https://github.com/bytecodealliance/wasmtime
cd wasmtime/crates/c-api
cargo build --release --target aarch64-apple-ios
cargo build --release --target aarch64-apple-ios-sim
```

**Las features son el punto fino y hay que resolverlas ahí.** Hace falta `component-model` y
`pulley`; Cranelift sobra y probablemente haya que apagarlo (`--no-default-features` y sumar de a
una). **No pierdas tiempo adivinando la lista: construí y preguntale al binario.**

**b. El paquete comunitario** ([`swift-wasmtime`](https://github.com/OpenCow42/swift-wasmtime)),
que vendoriza rebanadas de device y simulador. Se describe a sí mismo como *«a community
build/test package, not an official Wasmtime distribution»*. Sirve para destrabar rápido; **no
sirve para decidir**, porque no controlás ni la versión ni las features.

### El veredicto, y es una sola línea

```bash
nm -gU libwasmtime.a | grep -c wasmtime_component_linker_new
lipo -info libwasmtime.a
```

Tiene que dar **≥ 1** y la arquitectura correcta (`arm64` para aparato, `arm64` de simulador para
el simulador — **son binarios distintos, no intercambiables**).

> **No uses `strings`.** En el Git Bash de la máquina de Windows no existe, devuelve vacío en
> silencio y ya hizo descartar un binario que estaba bien. En la Mac existe, pero `nm` contesta la
> pregunta de verdad y `grep -ac` sirve de respaldo.

**Si esto da 0:** el binario es de módulos core y la caja en iOS **no existe hoy**. Fin del spike,
y es un resultado, no un fracaso — es exactamente lo que `B12` no podía afirmar.

---

## 5 · Paso 2 — ¿linkea dentro de una app de .NET iOS?

`Gnothi.MobileShared/Wasmtime.cs` tiene **30 entradas** que dicen `[DllImport("wasmtime")]`
(línea 18: `private const string L = "wasmtime";`). El comentario del archivo ya avisa que en iOS
no hay `libwasmtime` y que por eso no molesta: *un `DllImport` que nunca se llama no falla al
cargar*. **En el momento en que se llame, sí falla** — y ahí hay que decidir la forma del linkeo.

**Static (`.a`) es el camino recomendado**, porque en iOS un `.dylib` embebido hay que firmarlo y
colocarlo en `Frameworks/`, y agrega un modo de falla que no aporta nada al spike.

Con static, el nombre de la librería en .NET iOS es `__Internal`:

```csharp
#if IOS
	private const string L = "__Internal";
#else
	private const string L = "wasmtime";
#endif
```

Y en el `.csproj` del spike:

```xml
<NativeReference Include="lib/ios-arm64/libwasmtime.a" Kind="Static" />
```

**Cómo se ve que pasó:** la app arranca y un `wasm_engine_new()` devuelve algo distinto de cero.
**Cómo se ve que falló:** `ld: Undefined symbols for architecture arm64` con nombres
`_wasmtime_*` — eso es el linkeo, no el C#. Es el mismo modo de falla que el README §3b describe
para la `.a` de Swift, y se lee igual de mal.

> **Si el `.a` trae símbolos de Rust que chocan con el runtime de Mono**, que es la sorpresa
> clásica, el síntoma son duplicados en el link, no undefined. No está previsto ni descartado.

---

## 6 · Paso 3 — ¿el motor arranca en Pulley?

**Hoy `WasmBox.cs` llama `Wasmtime.wasm_engine_new()` a secas, sin configuración** (línea 40), y
en `Wasmtime.cs` **no hay una sola binding de config**. Para Pulley hacen falta tres que no
existen, más una que da el veredicto:

```csharp
[DllImport(L)] internal static extern nint wasm_config_new();
[DllImport(L)] internal static extern nint wasmtime_config_target_set(nint config, string target);
[DllImport(L)] internal static extern nint wasm_engine_new_with_config(nint config);
[DllImport(L)] internal static extern bool  wasmtime_engine_is_pulley(nint engine);
```

Las cuatro están en el header que ya hay en el repo
(`.wasmtime/48.0.2/.../include/wasmtime/config.h` línea 498 y `engine.h` línea 44).

```csharp
nint config = Wasmtime.wasm_config_new();
Fail(Wasmtime.wasmtime_config_target_set(config, "pulley64"), "fijar el target");
engine = Wasmtime.wasm_engine_new_with_config(config);
Log($"caja: pulley = {Wasmtime.wasmtime_engine_is_pulley(engine)}");
```

**`wasmtime_engine_is_pulley` tiene que dar `true`.** Es un bool y cierra de un saque la pregunta
«¿de verdad estoy en el intérprete o me cayó Cranelift por la ventana?».

### Y acá están las tres perillas de iOS que conviene tener a mano

No son opcionales de adorno: son las candidatas a explicar un crash que no se entienda.

| perilla | por qué |
|---|---|
| `wasmtime_config_signals_based_traps_set(config, false)` | las trampas por señal dependen de páginas de guarda y de un handler; Pulley puede no necesitarlas |
| `wasmtime_config_native_unwind_info_set(config, false)` | no hay código nativo que desenrollar |
| `wasmtime_config_memory_reservation_set(config, <chico>)` | **la de la memoria**: la reserva por omisión es enorme y es la sospechosa del paso 6 |

**Probá primero SIN tocarlas.** Si anda, son tres líneas que no hacen falta y el spike es más
corto. Si crashea al instanciar, empezá por `signals_based_traps`.

---

## 7 · Paso 4 — ¿abre el `.cwasm` precompilado?

**Este paso es el que le pone número a la frase de la documentación, y hay una distinción que vale
plata:**

> *«statically linking in a single precompiled module is not supported»* habla de **meter el
> precompilado adentro del binario**. **No** es lo mismo que **abrir un `.cwasm` desde un archivo
> en tiempo de ejecución**, que es lo que hace `wasmtime_component_deserialize_file`.

Y hay una razón concreta para esperar que el segundo sí funcione con Pulley: **un `.cwasm` de
`pulley64` no contiene código máquina, contiene bytecode**. No hay nada que mapear como ejecutable,
que es justamente lo que iOS no deja. **Es una hipótesis, no un hecho** — pero es la hipótesis
central del spike y se contesta con una llamada.

```csharp
Wasmtime.wasmtime_component_deserialize_file(engine, path, out component);
```

**Ojo con la ruta, porque `WasmBox` no sirve tal cual.** Hoy hace: buscar el precompilado en la
carpeta privada → si no está, **compilar en el aparato** con `wasmtime_component_new` y
serializarlo. **En iOS ese camino no puede existir** (compilar necesita Cranelift). El `.cwasm`
viene firmado dentro del bundle y solo se deserializa.

Es una rama que **no está escrita** y hay que agregar. Y cambia algo del lado de la garantía, para
mejor: hoy la huella contesta *«no está corrupto»*; en iOS el artefacto **viene firmado junto con
la app**, que es una posición más fuerte. La verificación de huella que hace `WasmBox` sigue
sirviendo, pero ya no es lo único que hay.

**Si `deserialize_file` falla:** guardá el mensaje completo de `wasmtime_error_message`. Que diga
*«compiled for a different target»* es una cosa (versión o target mal); que hable de memoria
ejecutable es **la otra**, y esa sí cierra el camino.

---

## 8 · Paso 5 — el canario, antes de cualquier número

`Canary.Passes(engine, component, linker, privateFolder, Log)` — las **8 fugas cerradas y las 2
puertas abiertas**. Ya está escrito y es neutral de plataforma.

**Por qué no se saltea, aunque el motor sea el mismo wasmtime de Android:** la contención son los
preopens de WASI y el `fsMutable:false` de `Cage`, y **nadie demostró que en iOS se comporten
igual**. `B12` lo dice para el caso de cambiar de motor; cambiar de plataforma y de estrategia de
ejecución a la vez merece el mismo trato. Cuesta una corrida.

**El modo de falla que importa no es que el canario cante mal: es que NO cante.** Que una fuga
«no se pueda» porque la ruta ni existe en el sandbox de iOS se lee igual que contención real. Si
alguna de las 8 pasa por una razón distinta a la de Android, **eso es el hallazgo**, no un verde.

---

## 9 · Paso 6 — los números, en el aparato

Recién acá. Con el **mismo instrumento** que se usó en Android, que es la única forma de que las
dos columnas se puedan mirar juntas.

| | Android, Cranelift *(medido)* | iOS, Pulley |
|---|---|---|
| arranque del módulo, los 5 pasos | 32–50 ms | |
| una consulta por el borde | ~1 ms | |
| **el replay, journal real** | ~700 ms | |
| hasta la primera pantalla | | |
| **RSS con un módulo vivo** | +29 MB | |
| el artefacto en el bundle | 19,3 MB | 15,5 MB *(ya medido)* |

**Y el supuesto que sigue en pie desde el 17: cuántas idas y vueltas son de verdad.** Las «42»
salen de **leer `cara.js`**, no de contarlas corriendo. `B12` lo deja dicho con el número al lado:
**si son 42 la pantalla tarda un segundo; si son 12, un tercio.** Es el pendiente `4` de `C10` y
lo decide iOS. **Contalas instrumentando el borde, no leyendo el JS.**

**Dos cosas más que solo el aparato contesta:**

- **Jetsam.** Si la app se muere sin excepción y sin stack, no es un bug de la caja: es iOS
  matándola por memoria. Se ve en los *Device Logs* de Xcode, no en el log de la app. Con cinco
  módulos esto deja de ser hipotético.
- **Simulador ≠ aparato.** El simulador corre con la memoria y el reloj de una Mac. **Sirve para
  los pasos 1 a 5; no sirve para el 6.** Cualquier número del simulador es de la Mac, y ese error
  ya se cometió una vez en este proyecto.

---

## 10 · El orden, y dónde abandonar

| | paso | si falla |
|---|---|---|
| 1 | el `.a` tiene `wasmtime_component_linker_new` | **fin.** La caja en iOS no existe hoy |
| 2 | linkea y `wasm_engine_new` devuelve ≠ 0 | problema de empaquetado, no de concepto — seguir |
| 3 | `wasmtime_engine_is_pulley` = `true` | probar las tres perillas; si no, fin |
| 4 | `deserialize_file` abre el `.cwasm` | **leer el mensaje.** Si habla de memoria ejecutable, fin |
| 5 | el canario canta, y por las razones de Android | no bloquea los números, pero **se reporta** |
| 6 | los números, en aparato | no falla: informa |

**Los pasos 1 a 5 se hacen en el simulador.** Solo el 6 necesita iPhone.

---

## 11 · Lo que hay que reportar — seis líneas

```
paso 1   ¿el .a trae componentes?  nm -gU | grep -c    NUMERO + version de wasmtime + como se construyo
paso 2   ¿linkea y el engine no es 0?                  SI / NO   (+ el error de ld si NO)
paso 3   wasmtime_engine_is_pulley                     true / false   (+ que perillas hizo falta tocar)
paso 4   deserialize_file del .cwasm de pulley64       SI / NO   (+ EL MENSAJE COMPLETO si NO)
paso 5   canario: 8 fugas / 2 puertas                  8/8 y 2/2, o cual y por que
paso 6   arranque · consulta · replay · RSS · idas y vueltas REALES   (aparato, no simulador)
```

**Y una línea de prosa que no está en la lista:** si algo pasó por una razón distinta a la
esperada. Este documento vale por los modos de falla silenciosos que nombra, y el que aparezca va
a ser uno que no nombra.

---

## 12 · Lo que este spike NO contesta, a propósito

- **Si se debe.** Con ×19–24 medido, «se puede» ya no es la pregunta; es un número de producto
  contra los ~49 ms de Android, y se decide con los del paso 6 sobre la mesa.
- **El `4.7` de Apple.** Si wasm cuenta como *interpreted code* — `B11`. Con Pulley la defensa es
  más fácil (**es literalmente un intérprete**), pero el texto de Apple enumera *«HTML5 and
  JavaScript»* y **no nombra wasm**. Eso no lo contesta un spike; lo contesta Apple.
- **Si el sustrato puede publicarse como módulo core con WASI p1.** Es la pregunta de mayor
  palanca de todas —abriría Wasm3 y WAMR con AOT, que es velocidad cercana a nativa sin depender
  del WebView— y es **un spike distinto, del lado del build de .NET, sin Mac**. Ver `B12`.
- **La sala.** Otra forma, ya construida y arrancando en iOS. Ver `HALLAZGOS-LA-SALA-EN-IOS.md`.
