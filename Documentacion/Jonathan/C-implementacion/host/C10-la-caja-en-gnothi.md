# C10 · La caja de wasm, implementada en Gnothi

**Escrito el 2026-09-16, corriendo en un Xiaomi con Android 16.** Qué se agregó, por qué así, y en
qué se diferencia de la forma híbrida renglón por renglón.

**El porqué de fondo está en [`B12`](../../B-arquitectura/B12-la-caja-de-wasm.md)** (qué es la caja)
y en [`F2` · `P19`](../../F-evidencia/F2-estado-y-pendientes.md) (por qué vale la pena). **Lo medido
está en [`F8`](../../F-evidencia/F8-el-paso-0-de-la-caja-de-wasm.md).** Este documento es lo otro:
el código.

> **Y una aclaración que ahorra la confusión de siempre: la caja NO es una cuarta forma del
> paquete.** El módulo de exámenes es —y sigue siendo— **híbrido**: su `.gmod` dice `hybrid`, su
> manifiesto no cambió y no se tocó un byte. Lo que la caja cambia es **el anfitrión**: quién
> ejecuta ese mismo paquete. Por eso «híbrida» y «en caja» se pueden comparar con un interruptor
> en el mismo teléfono, con el mismo journal y la misma cara — y por eso, en todo este documento,
> «contra la híbrida» quiere decir **contra el mismo módulo corriendo en proceso**.

> **Y LA MITAD DE ESTO VIVE EN F8, no acá.** Este documento es el código y el porqué; lo medido
> está allá, y buscarlo acá no lo encuentra:
>
> | qué | dónde |
> |---|---|
> | que el invitado puede cargar un ensamblado de afuera, los cinco pasos | F8 · «Separar el sustrato del módulo» |
> | el recorte, la trampa de `TrimmerRootAssembly`, los cuatro ensamblados | F8 · «El enraizado, medido» |
> | las dos puertas, el sólo lectura y el canario de dos-y-ocho | F8 · «La jaula de dos puertas, en Gnothi» |
> | el renombre del asset y las dos fugas de disco | F8 · «El asset cambiado» |

---

## Por qué existe, y por qué sólo en móvil

**Son dos restricciones a la vez, y hay que satisfacer las dos.**

**1 · El tamaño.** Un módulo **hospedado** trae su propio .NET compilado a wasm: **12,4 MB y +58,8 MB
de memoria por módulo vivo**, tanto que «un módulo vivo a la vez» dejó de ser precaución y pasó a ser
requisito. La forma **híbrida** existe justamente para escapar de eso: **74 KB de cara y 227 KB de
dominio**, con el motor prestado por Gnothi.

**2 · La regla de la tienda.** Y ahí la híbrida muere. El texto de Google Play exceptúa el código que
corre en una VM o intérprete **con acceso *indirecto* a las APIs de Android**; un DLL cargado por
Mono en el proceso de Gnothi da acceso **directo**. Ver [`B11`](../../B-arquitectura/B11-lo-que-dicen-las-tiendas.md).

| forma | tamaño | ¿pasa Play? |
|---|---|---|
| hospedada | 12,4 MB | **sí** |
| híbrida · cargo · Blazor en el Host | 74–227 KB | **no** |
| **la caja** | **227 KB** | **sí** |

**La caja es la única que satisface las dos.** Con `P18` decidido —Gnothi se baja de la tienda, al
teléfono de la persona— eso deja de ser una optimización.

**3 · Y la razón que no depende de ninguna tienda, que es el pilar.** Hoy el DLL de la institución
corre **con los permisos de Gnothi**. Podría abrir la carpeta de otro módulo, salir a la red, leer el
journal del laboratorio. **Nada en el sistema se lo impide: lo único que lo impide es que no lo
hace.** Medido: de nueve intentos de fuga, **siete funcionan en proceso y ocho de ocho cierran en la
caja**. En la caja no es disciplina — wasm no tiene syscalls, así que lo que Gnothi no le monte **no
existe** para él.

### Por qué en escritorio no hace falta

Por eso `BoxShellStub.cs` tira con un mensaje y ya. **En escritorio no hay tienda**, así que la
restricción 2 no aplica; y la 3 se resuelve distinto, con el proceso hijo y el confinamiento del
sistema operativo, que es lo que `B1` y `D2` describen. Lo que en un teléfono es una prohibición de
tienda, ahí no lo prohíbe nadie.

---

## Lo que se agregó

| archivo | de qué responde |
|---|---|
| `Gnothi.MobileShared/Wasmtime.cs` | la C API de wasmtime, a mano. 112 líneas de `DllImport` |
| `Gnothi.MobileShared/WasmBox.cs` | **el molde, uno para todos**: precompilar, la huella, descartar y recompilar |
| `Gnothi.MobileShared/BoxedInstance.cs` | **un módulo vivo** con su borde: el FIFO para hablarle, el callback para oírlo |
| `Gnothi.MobileShared/Cage.cs` | **la jaula en un solo lugar**: los dos montajes, y el sólo lectura del paquete |
| `Gnothi.MobileShared/Canary.cs` | las 2 puertas que deben abrir y las 8 fugas que deben cerrar, antes de abrir ningún módulo |
| `Gnothi.MobileShared/BoxShell.cs` | la cáscara: `IShell` sobre una instancia |
| `Gnothi.Host/BoxShellStub.cs` | el stub de escritorio, como los de webview e híbrida |
| `Gnothi.Mobile/TheBox.cs` | lo que Android pone: las rutas, el log, el portón y el interruptor de esta etapa |
| `Gnothi.Mobile/lib/arm64-v8a/libwasmtime.so` | 27 MB, publicado por bytecodealliance para `aarch64-android` |
| `Gnothi.Mobile/Assets/sustrato.wasm` | 11,1 MB — **el sustrato solo**, sin ningún módulo adentro |

**El APK pasa de 24 a 53,2 MB.**

> **Los nombres de esta tabla cambiaron el 2026-09-17**, cuando los identificadores de Gnothi
> pasaron a inglés (*esta línea decía «todo lo de Gnothi pasó a inglés» y **era falsa**: el
> 2026-09-28 había 370 líneas de comentario en castellano en los `.cs`, ~250 escritas DESPUÉS.
> Corregido — ver el capítulo final*):
> `CajaDeWasm` → `WasmBox`, `InstanciaEnCaja` → `BoxedInstance`, `Jaula` → `Cage`, `ElCanario` →
> `Canary`, `CajaShell` → `BoxShell`, `LaCaja` → `TheBox`. Y `examenes-en-caja.wasm` → `sustrato.wasm`,
> que no es un renombre sino **otro archivo**: el anterior llevaba exámenes adentro.

### Y una costura nueva en el núcleo

`Module.ChooseShell` gana una línea antes del `switch`:

```csharp
private static IShell ChooseShell(string name, Installation installation) =>
    ShellFactory?.Invoke(name, installation) ??
    installation.Shell.Type.ToLowerInvariant() switch { … };
```

**Existe para no meter una bandera de laboratorio adentro del switch.** La pone quien arma la
plataforma, devuelve `null` cuando no tiene opinión —que es lo normal— y no cambia el contrato: la
cáscara sigue saliendo del archivo del Host. Lo que permite es **sustituirla a propósito, en un solo
lugar y con nombre**.

Hoy la usa `TheBox.cs` para correr un módulo híbrido **ya instalado** adentro de la caja **sin
reescribirle su `installation.json` a la persona**, que es un archivo suyo.

---

## La comparación con la híbrida, decisión por decisión

### La cáscara: son la misma clase con un campo cambiado

```csharp
// HybridShell                          // BoxShell
private readonly WebViewShell face;     private readonly WebViewShell face;
private readonly InProcessShell surface;private BoxedInstance? viva;
```

`Invoke`, `View`, `Deliver`, `Cursor` y `ActualTools` delegan igual en las dos. **La cara es
literalmente el mismo objeto**, construido con los mismos `Shell.Arguments[0]` y `[1]`.

### Lo que cambia es quién ejecuta el C# de la institución

| | híbrida | caja |
|---|---|---|
| dónde corre el dominio | el proceso de Gnothi, sobre su Mono | una instancia de wasm, sobre el .NET compilado a wasm |
| cómo se carga | `AssemblyLoadContext` + `Assembly.Load` | ya está adentro del `.wasm` |
| cómo lo encuentra el Host | **reflexión**: `GetCustomAttributes`, `MethodInfo.Invoke` | **el módulo declara** y el Host llama por nombre |
| qué cruza | objetos de .NET | líneas de texto |
| qué puede alcanzar el módulo | **todo lo que Gnothi pueda** | sólo la carpeta concedida |
| quién lo mantiene vivo | `keepLoaded: true` en `InProcessShell` | la instancia, con su actor adentro |

**Medido el mismo día, el mismo binario en los dos anfitriones:** de nueve intentos de fuga, **siete
funcionan en proceso** —incluido leer y escribir el journal de otra institución y salir a Internet—
y **ocho de ocho cierran en la caja**. El noveno, leer su propio journal, es el que tiene que poder.

### Lo que NO cambia, y es la mitad de la gracia

- **La cara.** Mismo `gnothiPuente.consultar`, misma pantalla, mismo html. No sabe dónde aterriza su
  pregunta y no tiene cómo enterarse: `consultar` **no nombra un módulo** (`B8`).
- **El journal.** El módulo escribe en **la carpeta real**, no en una copia: el preopen de WASI es un
  paso directo al sistema de archivos del anfitrión. *(Esto lo distingue de un sustrato en WebView,
  donde el journal tiene que entrar a MEMFS y salir de nuevo.)*
- **El cartero.** Gnothi trae el sobre y lo entrega por `[Deliver]`. Y el módulo no sale a la red
  **no por disciplina**: no se le dio `wasi-http`.
- **El paquete de la institución.** El mismo `.gmod` híbrido, sin tocar un byte. *(Por ahora con una
  salvedad; ver abajo.)*
- **Los cinco marcadores del contrato.** `[ModuleSurface]`, `[Query]`, `[View]`, `[Deliver]`,
  `[Cursor]`, escritos igual.

### En la práctica, lo que el Host escribe

```csharp
// InProcessShell.View                          // BoxShell.View
=> Ask(name, arguments, accepts, ViewMarks);    => Raw(await (await Box(ct))
                                                     .Ask("consultar", name, Payload(arguments, accepts), ct), name);
```

Y **la cara recibe lo mismo** porque el puente ya hacía `answer as string ?? Serialize(answer)`: una
cadena que ya es json pasa verbatim y no se serializa dos veces.

Para el asistente sí hay conversión: `InProcessShell` devuelve su `Project(...)` —diccionarios,
listas, primitivos— y la caja devuelve texto, así que `BoxShell.Flatten` reconstruye esa misma
forma. Son veinte líneas y están ahí porque el frente de MCP espera objetos, no una cadena con json
adentro.

---

## Las cuatro decisiones que costaron, con su razón

**1 · El borde va por un FIFO, y el orden de apertura es lo que cuelga.**
La C API es asimétrica: `set_stdout_custom` entrega cada escritura del invitado a código nuestro,
pero **no hay callback de entrada** — sólo una ruta o un búfer fijo. Así que la ruta es un FIFO.

Abrir un FIFO para leer bloquea hasta que haya escritor, y al revés. `set_stdin_file` **abre para
leer ahí mismo**, así que llamarlo sin escritor en vuelo cuelga el hilo y **no se imprime una línea
más**: el log se corta y no hay error que leer. Por eso `BoxedInstance` arranca un hilo que abre
para escribir **antes** de configurar la entrada.

**2 · El callback se guarda en un campo.** Cruza a código nativo; si sólo viviera en la pila, el
recolector podría llevárselo con la caja corriendo.

**3 · Un pedido a la vez.** El invitado es un solo hilo y el protocolo es una línea por pedido, así
que dos preguntas simultáneas mezclarían las respuestas. `SemaphoreSlim(1,1)`.

**4 · El invitado corre en su propio hilo.** `wasmtime_component_func_call` no vuelve hasta que el
módulo termina, y en modo borde eso es cuando Gnothi le dice `chau`.

---

## Lo que este corte junta a propósito, y hay que deshacer

**`examenes-en-caja.wasm` es examenes entero** —su dominio y su superficie— dentro de un `.wasm` de
un solo archivo. **En el diseño de verdad eso es el SUSTRATO** —el runtime de .NET y el motor, uno
para todos— y los DLL del módulo entran desde su bundle.

Se juntaron porque **lo que este paso vino a medir es cómo ve el WebView a la caja**, no el formato
del paquete. Separarlos toca `PackageInspector`, `ModuleEnrollment` y el alta, y es otra
conversación.

**Y el código dice las dos cosas a la vez, así que conviene ser preciso.** `WasmBox` es estática
y su comentario promete «uno para todos», pero guarda **un solo componente**: si llegara un segundo
módulo con otro `.wasm`, `Prepare` llama a `Release()` y recompila. Hoy no se nota porque hay un
módulo solo en caja, que es justo lo que este corte eligió medir. **El motor compartido es real; el
sustrato compartido todavía no.**

> **DEJÓ DE SER CIERTO el 2026-09-17, y ésta es la frase que más envejeció de este documento.** El
> `.wasm` ya no lleva ningún módulo adentro: es el sustrato solo, y los DLL de cada módulo entran
> desde su bundle por un segundo montaje de sólo lectura. **El sustrato compartido es real y está
> medido**: los CINCO módulos del POC corriendo a la vez sobre **un solo componente**, sin que
> `Prepare` recompile una sola vez.
>
> Un molde, muchas coladas: el molde —motor, componente y linker estáticos— cuesta **~94 MB una
> vez**, y cada caja **~26 MB**. Ver F8, «La escalera de los cinco módulos».

---

## Lo que encontró ponerlo en Gnothi, y no lo había encontrado el spike

La misma cara, el mismo teléfono, el mismo journal, con la caja apagada y encendida:

| | híbrida | en caja, antes | en caja, ahora |
|---|---|---|---|
| «En general, tus resultados vienen bien» | sí | sí | sí |
| «TU ÚLTIMO CHEQUEO · 28 abr 2026» | sí | **no** | sí |
| «17 de tus 18 valores están en su rango» | sí | **no** | sí |
| «MERECE ATENCIÓN · 9» con las tarjetas | sí | **no** (8, sin tarjetas) | sí |
| «TU HISTORIAL · 32 resultados» | sí | sí | sí |

**La causa era una línea del borde:**

```csharp
return Ok(Json.De(porNombre[nombre].Invoke(superficie, null)));
```

**`null`** — los argumentos se descartaban. De las siete vistas de exámenes, dos los piden:
`Serie(string analito)` y `Tendencia(string analito)`. Tiraban adentro, el borde contestaba
`ok:false`, y **la cara degradaba en silencio**: no mostraba un error, mostraba menos pantalla.

**Estaba anotado como pendiente desde el paso 1** —*«las vistas con parámetros no se probaron»*— y
ahí parecía un detalle. **Puesto en una cara de verdad era media pantalla.**

> **La lección de método:** un arnés no sustituye a una cara. Los cinco verbos del borde estaban
> «ejercitados» y ninguno con argumentos, porque el arnés preguntaba lo que el arnés sabía preguntar.

### Cerrado el 2026-09-16

`ElBorde.Llamar`/`Ligar` ligan los parámetros desde un sobre que arma el Host:

```json
{ "args": { "analito": "Colesterol total" }, "accepts": "Json" }
```

`accepts` va **afuera** de `args` y se lee **antes**, que es la misma regla que `InProcessShell.Bind`
aplica con su parámetro reservado: el formato es del Host, no de quien pregunta. Si viajara adentro
del diccionario, una consulta podría elegirse su propio envoltorio.

Verificado en el teléfono contra el journal real: **la pantalla en caja quedó igual a la híbrida,
tarjeta por tarjeta.**

---

## Cuatro defectos en un día, y son el mismo defecto

Cerrar esa línea costó tres corridas más, y ninguna se perdió en el código que se estaba arreglando.
Vale escribirlos juntos porque **la forma es la misma las cuatro veces**, y esa forma es la trampa:

| dónde | qué comparaba | por qué mintió |
|---|---|---|
| `ElBorde` | nada: invocaba con `null` | los argumentos no llegaban, y el error salía como *menos pantalla* |
| `WasmBox` | nada: el `.cwasm` no estaba atado a su fuente | un wasm nuevo con un precompilado viejo y sano corría **el código anterior** |
| `TheBox` | **el largo** del asset del APK | el build nuevo salió con **los mismos 6 583 761 bytes** y el asset no se copió |
| el bundle de exámenes | nada: nadie lo volvió a armar | el arreglo del «212 → 212», escrito el 14, **se seguía viendo el 16** |

Los cuatro se leen igual desde afuera: **se cambia el código, se reinstala, y la pantalla sale
idéntica, sin un solo error**. Y los cuatro empujan a buscar el problema en el lugar equivocado —
en el código que uno acaba de escribir y que está bien.

> **Un arreglo no está hecho hasta que corre.** Entre el archivo que uno edita y el byte que ejecuta
> el aparato hay una cadena —compilar, empaquetar, firmar, copiar, precompilar— y **cada eslabón que
> decide «esto no cambió» puede equivocarse**. Cuando una corrección «no hizo nada», la primera
> pregunta no es qué está mal en el código: es **si el código llegó**.

Las dos comparaciones ahora son por huella: el artefacto se llama `sustrato-<huella16>.cwasm` por el
SHA-256 de su fuente, y el asset copiado lleva su `.sha256` al lado. **No cuesta más**: un asset de
Android tampoco dice su largo sin leerlo entero, así que la versión que mentía ya pagaba la lectura
de 6 MB.

Y Gnothi, en la cadena del bundle, se portó como se le pide: paró el rollback —*«es más viejo que el
que tenés: es la 2.4.0 y tenés la 3.9.0»*— y el paquete cambiado —*«no es el que sellaba el
enlace»*—, **las dos veces antes de tocar nada**.

---

## Lo que apareció y NO era de la caja

Dos cosas se veían en la pantalla en caja y las dos se comprobaron **apagándola**: salían idénticas
en híbrida. Vale contarlas porque la tentación, con una forma nueva en el medio, es atribuirle todo
lo que se ve mal.

**1 · `Color de la orina` y `Color de las heces` en `undefined`.** `cara.js` tenía dos ramas
—`presencia` y `hechos`— y la vista `tendencia` devuelve **tres** formas: la tercera, «categoría en
el tiempo», trae `constancia` + `observaciones`. El dato estaba ahí (`constancia.ultimaCategoria` =
`"ámbar"`) y el reparto ya sabía que eran tres. Arreglado el mismo día, verificado en las dos formas.
Ver E4 §2.

**2 · `no se pudo traer lo nuevo: Connection refused (127.0.0.1:8095)`.** Es el **cartero**, que es
de Gnothi, y faltaba el túnel al publicador de prueba. Distinto de su vecino: `Permission denied` en
ese puerto es **INTERNET sin declarar** en el manifiesto; `Connection refused` es que el socket abrió
bien y no había nadie del otro lado.

---

## PackageInspector, revisado — y casi no hay que tocarlo

La pregunta era si separar el sustrato obliga a cambiar la aduana del paquete. **Casi nada, y eso es
el resultado.**

### Lo que ya estaba bien, y por qué

**No hace falta un `PackageForm.Box`, y agregarlo sería un error.** La caja no es una forma del
paquete: es un anfitrión. El `.gmod` que corre en caja es el MISMO híbrido, con el mismo
`manifest.json`. `PackageForm` clasifica lo que el paquete ES; dónde corre lo decide el Host en
`installation.json`. Meter la caja ahí mezclaría las dos cosas.

**Y la lista `Engine` ya tenía escrito el reparto.** Dice que `Puppeteer`, `Choreography`,
`Puppets.Portador` y `Gnothi.Contract` son «el sustrato que el Host presta», y rechaza un híbrido que
los traiga. Cuando hubo que decidir qué referencia el sustrato del wasm, la respuesta resultó ser
exactamente esos cuatro — **la regla estaba escrita desde antes y se cumplió sin consultarla**. Es la
mejor señal de que el corte no es nuevo: es el mismo de siempre, ahora con una pared.

**Y sigue siendo correcto que un híbrido no traiga motor ni dentro de `web/`.** Con la caja también:
dos motores sobre un journal es justo lo que la forma existe para terminar.

### Una regla que era prolijidad y resultó ser contención

La aduana ya exigía que los DLL de un híbrido vivan **sólo bajo `dominio/`**. Eso era orden. En la
caja **esa carpeta es lo que se monta** (`Cage.Package`, de sólo lectura), así que la regla pasa a
acotar el alcance de lectura del módulo: lo más ancho que puede conseguir es `dominio/`, y no porque
se porte bien — porque la aduana no lo deja declarar otra cosa.

### El hueco que sí había

`dominio/sub/Entrada.dll` pasaba. Y rompe por dos razones a la vez:

- **Al cargar:** las dependencias se resuelven POR VECINDAD — el `.deps.json` no viaja en un bundle
  y `AssemblyDependencyResolver` necesita hostpolicy, que en móvil no existe. Una entrada en
  `dominio/sub/` no encuentra a sus hermanos en `dominio/`. **Esto no es nuevo**: `InProcessShell`
  tenía el mismo camino, y nadie lo había dicho.
- **Al contener:** en la caja esa carpeta es el montaje, así que la ruta de la entrada decidía el
  alcance de lectura del módulo. Ahora es siempre `dominio/`, conocido de antemano e igual para
  todos.

La aduana ahora exige que la entrada esté **directamente** en `dominio/`. Los cinco módulos del POC
ya declaran `dominio/<X>.App.dll`, así que no rechaza nada que exista; comprobado además al revés,
con un paquete armado a propósito con la entrada en una subcarpeta:

```
REJECTED, not installed:
  x load.entry = dominio/sub/Prueba.App.dll: a hybrid module.s entry has to sit directly in
    dominio/, because that folder is what the Host mounts for it and where its dependencies
    are looked up
```

---

## Lo que falta, por prioridad

**Cerrado el 2026-09-16:** el borde con argumentos. Detalle arriba y en F8.

### 1 · Separar el sustrato del módulo — y hay un filo que lo vuelve urgente

Hoy **el `.wasm` lleva exámenes adentro**. `Lab.WasiCaja.csproj` referencia `Examenes.App`, y el
bundle de un solo archivo mete todo junto: runtime de .NET, BCL, ICU, el borde, y los tres DLL del
módulo. Eso ya estaba dicho como una junta deliberada de este corte.

**Lo que no estaba dicho es la consecuencia.** El `.gmod` **también** trae esos DLL —204 KB en
`dominio/`— y en modo caja **no se usan nunca**: `Cage.Build` monta **un solo preopen**, la carpeta
concedida, y el paquete no se monta. Así que el código que corre es la copia del wasm, y la del
bundle es peso muerto.

> **Actualizar el módulo NO actualiza lo que corre en la caja.** El 2026-09-16 se pasó exámenes de
> 3.9.0 a 3.10.0 y la caja siguió ejecutando la copia del wasm. No se notó porque lo que cambió fue
> la cara —`cara.js`, que sí viaja en el bundle y sí se sirve—, pero **si hubiera cambiado el
> dominio, la caja habría corrido el viejo**.

**Es la misma familia que los cuatro defectos de ese día**: dos copias del mismo código, una
comparación que no se hace, y falla en silencio. Ver «Cuatro defectos en un día».

Está contenido mientras haya **un** módulo en caja y quien toque su dominio rearme el wasm a mano.
No es una salvaguarda: es una persona acordándose.

**Separarlo cierra la pregunta de raíz**: cuando el `.wasm` sea sólo el sustrato de Gnothi y los DLL
entren desde el bundle, hay **una sola copia** y no hay nada que sincronizar. Toca `PackageInspector`,
`ModuleEnrollment` y el alta, y con eso el formato del `.gmod`.

### 1b · CERRADO el 2026-09-17 — corrido en el teléfono

Dos preopens en `Cage`, el módulo entrando por `Shell.Arguments[2]`, el asset cambiado por el
sustrato solo (11,1 MB; el APK pasa de 51,6 a 53,2), **y la corrida en el aparato**: el sustrato
abrió exámenes desde su bundle en 34 ms —el `.wasm` no sabe qué es exámenes— y la cara pintó igual
que en híbrida. Detalle en F8, «El sustrato con el módulo afuera, corrido en el teléfono».

**Con eso desaparece el filo del punto 1**: ya no hay dos copias del dominio, hay una sola, la del
`.gmod`, y actualizar el módulo actualiza lo que corre.

Lo que queda del punto es el `.gmod`: sigue trayendo los tres DLL, que ahora **sí** se usan, así que
el formato no cambia. `PackageInspector` ya se revisó —le entró la regla de que la entrada de una
híbrida tiene que estar directamente en `dominio/`, porque esa carpeta es la que se monta.

### 2 · CERRADO el 2026-09-17 — el canario cantó en el aparato, en las dos condiciones

Normal: **8 de 8 fugas cerradas, 2 de 2 puertas abiertas**. Con `--ez jaulamala true`: vio la jaula
mal armada, nombró las dos fugas que se abrían, y **cerró la caja**. Y no deja residuo —
`PassesWithBrokenCage` no toca el veredicto real, así que el canario normal volvió a cantar 8 de 8 y
exámenes abrió.

De paso, el listado que el canario imprime para probar la fuga **delató una fuga de disco propia**:
dos artefactos del esquema de nombres anterior que el barrido no agarraba por buscar `sustrato-*`
con guion. Corregido a `sustrato*`. Ver F8.

### 3 · iOS

No hay JIT; wasmtime tendría que ir interpretado. Sin probar.

> **Probado el 2026-09-17, de escritorio: el sustrato COMPILA A PULLEY Y CORRE**, sin cambiarle una
> línea. Precio medido: **×19 el arranque, ×24 el borde**. Y con lo del punto `4` al lado, la pantalla
> de exámenes daría **~18 s** allá contra ~1,2 s acá — porque el intérprete no multiplica sólo el
> borde, multiplica **todo lo que corre adentro de wasm**, y ahí lo que manda es el replay del
> journal. Ver `B12`, «iOS: hay camino, y no es el de Android».

### 4 · CERRADO el 2026-09-17 — la caja agrega ~250–300 ms por pantalla

**Medido en el teléfono, con el journal real y el MISMO instrumento en los dos lados**:
`BoxedInstance.Ask` y `HybridShell.Counting` cuentan y cronometran cada ida y vuelta. Que esté en los
dos lados es la medición, no la prolijidad: la pregunta era «cuánto **más** que en proceso».

| | |
|---|---|
| idas y vueltas para pintar exámenes | **22** — y 18 son `tendencia`, una por analito |
| una consulta, en régimen | ~2–3 ms en proceso · **~10 ms en caja** |
| el calentón inicial, que pagan las dos | ~700–800 ms · **es el replay del journal, no el ejecutor** |
| la pantalla entera | ~970 ms en proceso · **~1 214 ms en caja** |

**Tres cosas que corrigen lo que decía este punto antes:**

- **Son 22, no ~42.** Aquella cifra suponía que la pantalla se pinta dos veces y salía de leer
  `cara.js`, no de contarlas corriendo.
- **La suma no es el tiempo.** En proceso las tres primeras llamadas **se solapan** —nada las
  serializa— y en la caja no, porque el borde es `SemaphoreSlim(1,1)`. Comparar las sumas daba «la
  caja es el doble de rápida», que es exactamente al revés.
- **El costo grande no estaba en el borde.** Domina el replay, que es trabajo del dominio y se paga
  igual en las dos formas.

Detalle completo en `F8`, «Lo que cuesta pintar la pantalla».

«La pantalla» no es una pregunta: son **veintiuna**. `cara.js` pide `historial`, `alertas` y
`analitos`, y después **una `tendencia` por analito, de a una y en orden** —encadenadas con `reduce`,
no en paralelo—, que con 18 analitos son 18 más. Y se pinta **dos veces**: lo que ya hay, y otra vez
cuando vuelve el cartero. ~42 idas y vueltas por apertura.

En proceso cada una es una llamada a un método de un DLL cargado. En la caja es escribir en el FIFO,
despertar el hilo del invitado, invocar por reflexión, serializar adentro, salir por el callback — y
con `SemaphoreSlim(1,1)`, **una a la vez**. El spike midió ~1 ms por consulta pero sobre vistas
simples. **A 1 ms son 21 ms y nadie se entera; a 30 ms son 600 ms de pantalla en blanco.**

---

## La escalera de los cinco, y lo que cierra · 2026-09-17

Medido en el teléfono, aparato limpio, los cinco módulos dados de alta como híbridos, proceso fresco
en cada escalón y el log verificado antes de leer la memoria. El detalle está en **F8, «La escalera de
los cinco módulos»**; acá va lo que cambia para esta implementación.

**El sustrato compartido dejó de ser una promesa del comentario.** Cinco cajas sobre un solo
componente, sin una recompilación. Eso es lo que `WasmBox` decía y ahora se puede afirmar.

**El portón dejó de elegir por nombre.** `TheBox.TheOneInTheBox = "examenes"` era andamio del primer
corte —cuando el sustrato llevaba exámenes adentro y no había otro que cupiera—. Ahora decide **la
forma**: todo módulo híbrido instalado corre en caja, y se puede estrechar desde afuera con
`--es caja-solo <lista>`, que es lo que la escalera necesitaba para subir de a uno.

**Y el control es lo que hace que los números digan algo.** Con `--ez caja` apagado los mismos cinco
módulos abren por `HybridShell`, en proceso: mismo paquete, mismos bytes, mismo trabajo, distinto
ejecutor. Sin esa mitad, la tabla podría ser de una pantalla vacía.

### La lista de prioridades, al cierre del día

| | |
|---|---|
| `1` separar el sustrato del módulo | **cerrado**, 2026-09-17 |
| `2` el canario en el aparato, dos condiciones | **cerrado**, 2026-09-17 |
| el barrido de artefactos viejos | **cerrado**, 2026-09-17 |
| **la memoria: Gnothi sola contra Gnothi con los cinco** | **cerrado**, 2026-09-17 |
| `4` cuánto cuesta **pintar la pantalla** | **cerrado**, 2026-09-17 — ~250–300 ms más que en proceso |
| `3` iOS | **se puede** (Pulley, probado de escritorio); lo que falta es decidir si **se debe** |
| **nuevo · el replay del journal** | **abierto** — ~700 ms una vez por arranque de la app, en las DOS formas |

**El `4` es lo que queda, y su prioridad cambió.** Mientras la pregunta era «cuánto memoria», el costo
de pintar era un detalle; ahora que la memoria está contestada, **es el único término sin medir de la
cuenta**. Y hay una razón nueva para que importe: si alguna vez el renderizador de la cuarta forma
—Blazor en el Host, `F7`— se metiera adentro del sustrato, el borde dejaría de ver ~42 idas y vueltas
por pantalla y pasaría a ver **una por evento**. Ahí el número deja de ser opcional.

**iOS no es lo que sigue.** Queda anotado porque no hay JIT y wasmtime tendría que ir interpretado,
pero no está en el camino que se está recorriendo.

### El pendiente nuevo, y no lo trajo la caja: el replay

**~700–800 ms, y se pagan igual con la caja encendida y apagada.** El actor se rehidrata leyendo el
journal la primera vez que alguien le pregunta algo.

> **CORRECCIÓN del 2026-09-17, el mismo día: NO es «por apertura».** Así lo escribí primero y está
> mal. Medido: **reabrir un módulo en la misma sesión cuesta CERO** — cero idas y vueltas, la cara
> servida una sola vez, y el log no dice nada al salir porque **volver atrás no cierra nada**. El
> replay se paga **una vez por arranque de la app, por módulo**.
>
> Lo escribí sin medirlo, de la misma forma en que salieron las ~42 idas y vueltas que resultaron 22.
> Es la tercera vez en el día.

Hasta hoy estaba escondido detrás de otra cosa: `BoxedInstance` ya decía *«288 ms para la primera,
1 ms para el resto»* y se leía como una propiedad del borde. **No lo es** — es el dominio
reconstruyéndose, y aparece igual en proceso.

**Por qué importa ahora:** es el término más grande de la pantalla, y el único que no se arregla
agrupando llamadas. Si el journal pudiera quedar caliente entre aperturas, la cuenta cambia en las
dos plataformas — y en iOS es la diferencia entre inviable y discutible, porque allá esos 700 ms son
~14 s.

**No es de la caja.** Es de cómo se abre un módulo, y toca al Host.

### El replay, investigado · 2026-09-17

**1 · No es por apertura: es por arranque de la app.** Reabrir el módulo cuesta cero, y volver atrás
no cierra nada. Lo paga la PRIMERA pregunta de la sesión, y sólo ésa.

**2 · No es proporcional al largo del journal**, que era la sospecha obvia:

| módulo | eventos | el arranque |
|---|---|---|
| compras | 390 | **337 ms** |
| exámenes | 225 | **694 ms** |

**El que tiene MÁS eventos tarda MENOS.** Así que no es leer el journal: es lo que cada dominio
reconstruye a partir de él. Exámenes arma 18 analitos con sus series; compras, otra cosa.

> **CORRECCIÓN del 2026-09-18, y es de unidades.** Esos «225» y «390» **no son entradas del journal**: son
> el **cursor de la institución** (`ultimoEventoProcesado`), que viaja en el sobre del cartero. Los dos
> journals reales tienen **~88–90 entradas** cada uno (`meta.bin`: `LastWrittenEntryId` 87 y 90). La
> conclusión sigue en pie —el largo del journal no es lo que cuesta— pero la tabla de arriba compara
> dos unidades distintas y la razón de la diferencia no es «18 analitos»: es que **el 65–85 % del primer
> arranque es calentón del motor**, y el resto es lo que se detalla en «Dónde se va el tiempo de verdad».

**3 · Dónde está.** `ExamenesSurface.Arrancar()` es `new ExamenesApp` + `ConfigureStorage` +
**`Start()`**, y ese `Start()` es del motor, no nuestro. Va detrás de un `Lazy<T>` —con una buena
razón escrita al lado: `ActualTools` construye la superficie sin preguntarle nada, y rehidratar el
journal para listar dos nombres sería pagar de más.

**4 · Y por eso el arreglo barato no es hacerlo más rápido.**

La pereza hace que el costo caiga **en la primera consulta**, que es exactamente cuando la persona
está mirando la pantalla en blanco. Pero nada obliga a que sea ahí: entre que Gnothi abre y que
alguien toca «Abrir» hay segundos de sobra.

**El replay no es demasiado lento: es demasiado tarde.**

**Lo que hay que sopesar antes de calentarlo, y está medido hoy:** un módulo vivo cuesta ~26 MB en
caja, y Gnothi con los cinco llegó a **482 MB**. Calentar los cinco al arrancar hace el proceso más
grande y **más matable por Android** — y cuando el sistema lo mata, el replay vuelve. Calentar de más
puede costar lo que pretende ahorrar.

### El calentado, probado · 2026-09-17

**Escrito, corrido y medido tres veces.** `Shell.Rehydrate` le pregunta el cursor a los módulos de una
lista, **en segundo plano y después de que la pantalla ya está pintada** — el mismo lugar donde ya
vivía `VerifyNewModules`, y por la misma razón.

Se prende desde afuera para poder correr el **mismo build** con y sin, que es lo único que hace
comparable la medición:

```
adb shell am start -n ... --es calentar examenes
```

| corrida | sin calentado | **con calentado** |
|---|---|---|
| 1 | 504 ms | **141 ms** |
| 2 | 660 ms | **130 ms** |
| 3 | 467 ms | **126 ms** |

**La espera de la persona baja de ~500 a ~130 ms**, unas cuatro veces. Y el calentado en sí costó
400 · 273 · 274 ms, pagados **al arrancar la app**, cuando nadie está esperando.

**Lo que queda de esos 130 ms ya no es el replay**: es el trabajo real de `historial` y `analitos`.
El `cursor` que después pide el cartero pasó a costar **0 ms**.

### Por qué se pregunta el cursor y no otra cosa

Es lo más barato que fuerza el arranque del actor, **y es la misma pregunta que el cartero hace
después de todos modos**. No inventa trabajo: lo adelanta. Por eso el `cursor` posterior sale gratis.

### Lo que NO se decidió acá

**Quiénes van en la lista.** Está vacía por omisión y la pone quien arma la plataforma, no el núcleo:
calentar cuesta memoria, y con los cinco vivos Gnothi llegó a 482 MB, lo que lo hace más matable por
Android — y cuando el sistema lo mata, el replay vuelve. Calentar de más puede costar lo que pretende
ahorrar. **Eso es una decisión de producto y no está tomada.**

### Y lo que esto le hace a iOS

Los ~700 ms que bajo Pulley eran ~14 s **salen de la espera de la persona**, no desaparecen. Siguen
costando lo mismo, pero se pagan al arrancar. **No alcanza para volver iOS viable** —14 s de trabajo
de fondo siguen siendo 14 s antes de que la primera pantalla pueda pintarse— pero cambia la forma del
problema: deja de ser «la pantalla tarda» y pasa a ser «cuánto tarda Gnothi en estar listo».

### Dónde se va el tiempo de verdad — la indagación · 2026-09-18

Cinco agentes sobre el código real —el motor descompilado y su fuente (`C:\…\repos\Puppeteer`, HEAD
`8ef4415` = el dll 3.0.0-beta.10044 que corre, verificado por SHA), los dominios, el almacenamiento, el
Host y lo ya escrito— y después refutadores contra cada palanca. Lo que sigue es lo que **sobrevivió**,
separando medido de leído de supuesto.

#### El costo NO es el journal — medido tres veces por caminos distintos

| | |
|---|---|
| costo marginal por entrada | **~0,005–0,015 ms** |
| de 25 a 900 entradas | 66 → 70 ms |
| de 74 a 2 075 entradas | 136 → 167 ms |
| abrir el almacenamiento (sin replay) | 4–8 ms en régimen |
| 8 KB contra 60 KB de journal, mismas entradas | abren igual |

**Achicar, comprimir, cifrar o distilar el journal ataca ~2,5 % del arranque.** Queda cerrado con número,
para que nadie lo vuelva a proponer: un snapshot de estado del dominio —que además **no existe en el
motor**, revisados sus 6 731 miembros— atacaría ese mismo 2,5 %.

#### Lo que sí cuesta, y de quién es

**1 · El 65–85 % del primer arranque es calentón del motor y del runtime, una vez por PROCESO.** El segundo
módulo —otro dominio, otro assembly— cuesta ~20–25 ms donde el primero costó 130–170. Es simétrico: da
igual cuál vaya primero.

**Y en la caja eso no se comparte.** Cada módulo es su propia instancia de wasm con su propio runtime
.NET adentro (`BoxedInstance.cs:112`, un `store` por módulo; `engine`, `component` y `linker` sí son uno).
El calentón no cruza el `store`: **en iOS son ~14 s POR MÓDULO, no una vez.** Compartir el `store` rompe la
caja estructuralmente —refutado 3 de 3—, y un «snapshot de la memoria lineal ya caliente» no tiene API en
lo que corre, es un componente y no un módulo core, y el estado es el cursor: refutado 3 de 3.

**2 · El replay COMPILA, y en la caja eso es pagar tres intérpretes.** Fuente del motor, `ActorHandler.cs`:
las entradas de *script* van interpretadas (líneas 1940, 2126, `useInterpretedMode: true`), pero las
**acciones** —`define action`, 14 en exámenes— van **compiladas** (2517, 2588, `useInterpretedMode: false`
→ `Program.cs:305`, `expression.Compile()` en la primera ejecución). En el BCL recortado de la caja
`System.Linq.Expressions.Compile()` es `LambdaCompiler.Compile` **sin guarda**, y
`RuntimeFeature.IsDynamicCodeCompiled` es un `return false` duro: **se paga Reflection.Emit para producir
IL que interpreta Mono, que interpreta wasm, que interpreta Pulley.** Medido en escritorio en frío:
`Automatic` ~150 ms con 14 compilaciones de ~18 ms; `AlwaysInterpreted` ~120 ms y `exec` 29 contra 51 ms.
El setter de `Actor.CompiledModePolicy` es `internal`; el motor ya tiene la señal (`HasNoWorkerThreads` ⇒
`IsBrowser() || IsWasi()`) y no la usa para esto.

**3 · El intérprete memoriza métodos pero no constructores.** `DotAccess.FindMethodCached` sí;
`NewInstance.InstantiateObject` resuelve la sobrecarga en cada ejecución. Micro-bench: un cuerpo con un
constructor cuesta 0,13–0,18 ms por evento contra 0,04–0,05 sin él. Exámenes tiene **un constructor por
evento** (`ExamenesAPI.cs:128-145`). Ataca el término proporcional, que es chico.

**4 · La semilla se re-journaliza en cada arranque — y es a propósito, no hay que tocarla.** Medido:
225 → 226 → 227 entradas en tres arranques. Parece una fuga y lo es, pero **re-emitir `upgrade('semilla')`
es lo único que hace correr `ValidateUpgradeSignature`**: el detector ruidoso de «el código dice un cuerpo y
el journal tiene otro» —el que hace que la app **no abra** si alguien cambia la edad de la semilla, en vez
de calcular veredictos clínicos sobre estado viejo. Moverla a `OnFirstHydration` borra ese detector y de
paso deja de entregar upgrades nuevos a journals ya abiertos (compras `'cartelera'`, los seis del
concesionario). `Metodologia/Guias/reglas-del-substrato.md §4` ya lo había dicho: *«sabelo y decidí»*.
Refutado 2 de 3, por la razón correcta.

**5 · iOS ya rehidrata TODOS los módulos al arrancar, dos veces, y nadie lo midió.** `Shell.Run` hace
`await Fotografiar()` (`Shell.cs:212`) **antes** de `VerifyNewModules` y del calentado; `Snapshot.Write`
llama `dispatcher.Serve` por **cada tool sin parámetros de cada módulo habilitado** (`Snapshot.cs:58-86`),
lo que arranca el actor de todos. iOS pasa `FotoLocal: true` **y** `Instantaneas: SharedContainer()`
(`ViewController.cs:134-138`): dos pasadas completas. Android no lo corre — por eso el número de los
694 ms se midió allá y no incluye esto. Y cada cambio de consentimiento vuelve a fotografiar
(`Shell.cs:253`). **Con cinco módulos a ~14 s, es del orden de un minuto antes de que Gnothi esté listo.**
Extrapolación, no medición: nadie corrió `Gnothi.iOS` con los cinco.

**6 · Cualquier alta, actualización o rollback mata los actores de TODOS los módulos.**
`ModuleCatalog.Publish` compara `Module` por referencia y `Load()` los reconstruye todos, así que los
cinco entran en `DisposeAsync`. Instalar uno cuesta re-calentar cinco.

**7 · Dos correcciones a lo que este documento decía arriba:** «225 y 390 eventos» eran cursores, no
entradas (los journals tienen ~88–90); y «no es proporcional al largo del journal» es cierto, pero la razón
no son los 18 analitos sino el calentón fijo del punto 1.

#### Lo que ya está descartado con número

| palanca | por qué |
|---|---|
| achicar / distilar / comprimir el journal | ataca ~2,5 % |
| compartir el `store` entre módulos | rompe la caja; refutado 3/3 |
| snapshot de la memoria lineal caliente | sin API, es componente, el estado es el cursor; 3/3 |
| «pagar el calentón una vez» | ya está hecho lo compartible (`component`); lo que sobra no se comparte; 3/3 |
| colapsar los 12–14 comandos en uno | ~1 ms por `define`; ~12 ms de 694 |
| mover la semilla a `OnFirstHydration` | borra el detector de firma; 2/3 |
| leer registros sin despertar al dominio (`Materialization`) | existe y es rápido, pero obliga a reimplementar el dominio o a pedir `expose`; 3/3 como respuesta a los 14 s |

#### El instrumento que el motor ya trae y nadie prendió

`LabInstrumentation.StageTimingEnabled = true` —o `PUPPETEER_STAGE_TIMING=1`— imprime `parse / resolve /
exec` por rehidratación y los contadores de la caché de métodos. Es público, es del dll que corre el
teléfono, y hay **cero** usos en Gnothi. Corrido en laptop sobre el journal real: `parse=10 resolve=4
exec=53` sobre un `Start()` de ~130 ms. **No llega a la caja** porque `Wasmtime.cs` no declara
`wasi_config_env`: son dos líneas.

Las palancas que quedan en pie están en refutación en este momento; se anotan cuando haya veredicto.

## Las tres formas conviven — revisado y probado · 2026-09-18

La pregunta: después de meter la caja, ¿siguen andando **hospedada**, **híbrida** y **caja** en Android?

### Por código: la costura las mantiene separadas

```csharp
ShellFactory?.Invoke(name, installation) ??                 // la caja
installation.Shell.Type.ToLowerInvariant() switch { … }     // webview / hybrid / in-process
```

El portón filtra **por forma**, no por nombre: `Shell.Type == "hybrid" && GoesInTheBox(name)`. Para una
hospedada devuelve `null`, cae al `switch` y le toca `WebViewShell` — el mismo camino de siempre.

| forma | quién la elige | ¿la tocó el cambio de la caja? |
|---|---|---|
| hospedada | el `switch`, `webview` → `WebViewShell` | **no** |
| híbrida | el `switch`, `hybrid` → `HybridShell` | sí, el contador del borde |
| caja | el `ShellFactory`, sólo híbridas de la lista | sí |

### Y un detalle del calentado que hay que saber antes de usarlo

`Shell.Rehydrate` le pregunta el cursor a **cualquier** módulo que esté en `--es calentar`. Para una
hospedada eso cae en el default de `IShell.Cursor`:

```csharp
// ZERO BY DEFAULT... Only the hybrid form answers it for real, because it is the
// only one where the page has no actor to know it with.
Task<long> Cursor(CancellationToken ct) => Task.FromResult(0L);
```

**Calentar una hospedada no rompe nada y tampoco hace nada**: contesta 0 al instante sin levantar su
WebView. No es un defecto — es que el cursor es la palanca de la híbrida y de la caja. Poner una
hospedada en esa lista es trabajo perdido, no daño.

### Y en el aparato NO convivían: una regresión de la caja sobre la hospedada · 2026-09-18

La revisión por código decía que las tres estaban separadas. **En el teléfono no lo estaban**, y el
defecto no se ve en la costura de `ChooseShell` sino tres archivos más allá.

`WebModule.Open` decide si a la cara se le cablea el camino de la híbrida —pedir vistas y pedirle al
cartero— con esto:

```csharp
bool preguntona = forma.Equals("hybrid", …)
    || forma.Equals("caja", …)
    || Gnothi.Host.BoxShell.Preferred;     // <- entró con la caja, en dac6af6
```

**`BoxShell.Preferred` lo prende `TheBox` con `--ez caja true`.** Con eso, **todo** módulo recibía el
cableado de la híbrida — **incluida una hospedada**, que no lo soporta.

#### Lo que se ve, y son dos daños de una

Municipalidad **hospedada**, con la caja prendida:

```
municipalidad: la cara pidio desde 23; la superficie va en 0
municipalidad: pull after=0 -> 3126 bytes
JSException: NotSupportedException: this shell does not take deliveries:
             the module has no surface that writes
```

**1 · El cursor de la cara se tiraba.** `Pull` reemplaza el `after` de la página por el de la
superficie, y en la hospedada no hay superficie: contesta el **0** del default de `IShell.Cursor`. O
sea que el cartero volvía a traer **el historial entero en cada refresco** — exactamente el defecto
que el comentario de ese mismo método dice haber arreglado, medido sobre cargas el 2026-09-14
(`leidos=114 aplicados=0 rechazados=114`).

**2 · Y después le entregaba el sobre a quien no recibe entregas**, con la excepción **a la vista de
la persona**, en un bloque amarillo sobre su pantalla.

#### El A/B, en el mismo teléfono y el mismo módulo

| | qué hace el cartero |
|---|---|
| **sin** `--ez caja true` | `pull after=23 -> 23 bytes` · sin entrega · sin error |
| **con** `--ez caja true` | `pull after=0 -> 3126 bytes` · entrega · **NotSupportedException** |

#### El arreglo, y por qué el disyunto no hacía falta

Se decide **por la forma del módulo**, no por si la caja está prendida. Y no se pierde nada: **un
módulo en caja sigue declarando `hybrid` en su `installation.json`** —la caja es el anfitrión, no una
forma del paquete, que es lo que este documento afirma desde el principio—, así que la primera
cláusula ya lo cubría.

#### Lo que esto enseña del método, y es lo que más duele

**La revisión estática dio «las tres separadas» y estaba bien** — la costura de `ChooseShell` sí las
separa. El defecto vivía en otra parte: en un interruptor global que la caja prende y que otro archivo
lee para decidir algo que no es de la caja.

**Y yo declaré «dos confirmadas» mirando líneas de log filtradas, sin abrir una sola captura de la
pantalla.** Lo que había en pantalla era una excepción. Lo dijo Jonathan, no la prueba.

## Llegó una cuarta forma, y se sentó en la misma silla · 2026-09-23

En `master` entró **la sala** (PR 10130): el mismo `.dll` híbrido, sin tocarle un byte, corriendo
adentro de un documento oculto del motor web que es de Gnothi. Es **hermana de la caja**, no un
escalón: mismo contrato de borde, distinto transporte —wasmtime de un lado, el motor web del otro.

Y por ser hermana, **eligió la misma costura**. Ahí está el problema.

### El choque: un solo campo estático y dos dueños

`Module.ShellFactory` es **una sola propiedad estática**. En Android hay **dos** asignaciones:

| quién | dónde | cuándo corre |
|---|---|---|
| la caja | `TheBox.cs:134` | `PrepareTheBox()`, en `OnCreate` |
| la sala | `MainActivity.cs:281` | dentro de `Start()`, **después** |

La última gana. Con `--ez caja true` y la sala forzada a la vez, **la sala borraba la fábrica de la
caja** y todo módulo que iba a entrar en caja corría híbrido — mientras el log seguía diciendo
`caja: encendida para todos los modulos hibridos`.

**El daño no es que dos laboratorios estuvieran prendidos: es que uno se apagaba sin avisar.** Una
escalera de memoria medida así compara nada contra nada, y el log habría jurado que la medición era
buena. Es el mismo modo de falla que el `--es calentar` que da falso verde sin `PrepareTheBox()`.

Esto es **leído del código**, no medido en el aparato: las dos asignaciones y su orden se ven en el
fuente, y nadie corrió todavía las dos prendidas a la vez en el teléfono.

### El arreglo: encadenar, no asignar

```csharp
// Gnothi.Host/Module.cs — la regla, en un solo lugar
internal static bool ChainShellFactory(Func<string, Installation, IShell?> newest)
{
    Func<string, Installation, IShell?>? before = ShellFactory;

    ShellFactory = before is null
        ? newest
        : (name, installation) => newest(name, installation) ?? before(name, installation);

    return before is not null;
}
```

**Primero se arregló en línea, en cada plataforma, y eso estaba mal por la misma razón que el
defecto:** la regla quedaba escrita **dos veces** —`MainActivity` y `ViewController`— y **ninguna de
las dos la compila ningún proyecto que corra en una máquina cualquiera**. Dos copias de una regla,
cero pruebas. Así que se mudó a `Gnothi.Host`, donde la suite sí llega, y las cuatro plataformas
—las dos cáscaras móviles, la caja y el escritorio— la llaman en vez de repetirla.

Lo llama **también la caja, que hoy corre primera y a la que encadenar no le cambia nada**. Está
así porque `TheBox.cs` es el archivo del que el próximo va a copiar, y lo que no puede copiar es la
forma que ya costó una medición. Por lo mismo su `catch` ahora **repone lo que encontró** en vez de
poner `null`.

**Y devuelve si había una antes**, porque el daño de septiembre no fue que hubiera dos laboratorios
prendidos: fue que uno se apagó sin una palabra. Encadenar en silencio devuelve el defecto en una
forma más tranquila.

**Lo que lo hace correcto es el contrato que la costura ya tenía escrito: una fábrica devuelve `null`
cuando no tiene opinión.** La sala contesta por sus nombres y le devuelve el resto a la de antes —la
caja, que contesta por los suyos y le pasa el resto al `switch`. Y si había una, ahora **se dice en
el log**, que es lo único que faltaba para que el problema fuera visible en vez de silencioso.

Lo mismo se aplicó en `ViewController.cs` de iOS, donde **hoy no rompe nada** porque no hay caja. Se
hizo igual: el defecto estaba en la forma, no en la coincidencia de que hubiera dos.

#### Y ahora tiene prueba, que es lo que faltaba de verdad

`Gnothi.Assistant.Tests/Costura.cs`, ocho afirmaciones. **Y se comprobó que discrimina**: se volvió a
poner la asignación plana a propósito y dos se pusieron rojas, con el mensaje diciendo qué pasó —

```
[FAIL] Y LA PRIMERA SIGUE VIVA para los demas -- el defecto del 2026-09-23
        contesto 0: la segunda se comio a la primera
[FAIL] si ninguna opina, las dos igual fueron consultadas y decide el switch
        se consultaron 1 de 2
```

El observable es `Module.Cursor()`, que devuelve un `long` y sale de la cáscara sin pasar por nada
más: cada cáscara falsa devuelve su propio número, así que se sabe **cuál** contestó y no sólo que
alguna lo hizo.

Una de las ocho es del orden, y no es un detalle de implementación: **la última en registrarse
pregunta primero**. Las fábricas no son igual de específicas —la de la caja reclama una **forma**,
toda híbrida; la de la sala una **lista de nombres**—, así que si la ancha contestara primero, la
angosta no ganaría un solo módulo nunca.

### Las cuatro formas, al 2026-09-23

| forma | quién la elige | anfitrión |
|---|---|---|
| hospedada | el `switch`, `webview` | su propio WebView, uno por módulo |
| híbrida | el `switch`, `hybrid` | el proceso de Gnothi |
| caja | el `ShellFactory` de `TheBox` | wasmtime |
| sala | el `ShellFactory` de `MainActivity` / el `switch`, `sala` | un documento del motor web, uno para todos |

Las dos últimas hospedan **el mismo paquete híbrido**. Por eso las dos se sientan en la costura, y
por eso encadenarlas no es un parche sino la forma que la costura pedía desde que hubo más de una.

### Y un segundo choque, éste latente

`WebModule.Open` decide el cableado de la cara con `preguntona`, y listaba `hybrid` y `caja`
**pero no `sala`**. `RoomShell` implementa `View`, `Deliver` y `Cursor` —las tres cosas que `Cablear`
conecta— y su propio comentario dice que sin eso «la pantalla no abre».

No había disparado, y no por diseño: **el alta móvil no tiene rama que escriba `"type": "sala"`**.
`ModuleEnrollment` deriva el tipo de lo que trae el paquete —`hybrid`, `in-process`, `webview`— así
que la sala sólo se prende hoy forzando la cáscara sin tocar el tipo, y la primera cláusula la
cubría. El día que alguien escriba ese tipo, la cara se quedaba en la excepción.

Que era omisión y no decisión lo delata `Shell.cs`, que **sí** nombra `sala` junto a `webview` y
`hybrid` en `guardaSuCaraAlLado`: el archivo que guarda la cara lo sabía, el que la cablea no.

Se agregó `sala` al `preguntona`. **Queda abierto quién escribe ese tipo en el alta**, y es decisión
de producto, igual que con la caja: qué forma corre un paquete híbrido la elige el Host.

### Lo que esto agrega al método

La regresión del 2026-09-18 enseñó que **la revisión estática puede dar bien y el aparato mal**.
Ésta enseña la otra mitad: **una costura que admite un solo ocupante y no lo declara es una trampa
para el segundo que llegue**, y el segundo siempre llega. Ninguna de las dos asignaciones estaba
mal escrita; lo que faltaba era que el campo dijera que es de uno solo.

### Y los nombres de la sala pasaron a inglés · 2026-09-23

La caja llegó con sus identificadores en inglés —`WasmBox.Prepare`, `BoxShell`, `SweepOldArtifacts`—
y la sala llegó entera en español. Eran dos hermanas escritas en dos idiomas, y eso ya había costado
un rato de lectura en este mismo documento: los párrafos de más arriba decían `CajaDeWasm`, `Preparar`
y `Soltar` mucho después de que la caja se llamara `WasmBox.Prepare` y `Release`. **Un documento que
nombra código con los nombres de antes no está viejo en un detalle: manda a buscar algo que no
existe.**

| antes | ahora |
|---|---|
| `SalaShell` · `ISalaHost` | `RoomShell` · `IRoomHost` |
| `AndroidSala` · `IosSala` | `AndroidRoom` · `IosRoom` |
| `Preparar` · `Pedir` · `Soltar` · `Guardar` · `Descartar` | `Prepare` · `Ask` · `Release` · `Save` · `Discard` |
| `Asegurar` · `Vecindario` · `Confirmar` | `Ensure` · `Neighbourhood` · `Confirm` |
| `Servir` · `Servible` · `Arrancar` · `Calentar` | `Serve` · `Servable` · `Start` · `WarmUp` |

Los archivos siguieron a sus tipos: `SalaShell.cs` → `RoomShell.cs`, y con ellos los tres `.csproj`
que los nombran por ruta —el `Exclude` del stub en Android y en iOS, y el `Compile Include` de la
suite—.

#### Lo que NO se renombró, y es la mitad que importa

**El protocolo con el sustrato no es código: es un contrato entre dos binarios que se compilan por
separado.** Se quedan exactamente como estaban los verbos (`cargar`, `desalojar`, `consultar`,
`vista`, `entregar`, `cursor`, `repertorio`), los campos del json (`ok`, `motivo`, `contenido`,
`escrituras`, `ruta`, `vaciada`, `vecinos`, `entrada`, `archivos`, `rutas`), las rutas que el
sustrato pide (`modulo/<m>/indice.json`, `dominio`, `journal`), los nombres de JavaScript
(`window.sala.pedido`, `SalaPuente`) y el `"type": "sala"` del `installation.json`.

Y los **mensajes en español** —logs y excepciones— también, por el mismo criterio con el que la caja
conserva los suyos: contra esos textos están escritos los hallazgos medidos.

**Y la prueba de que eso es un borde y no una inconsistencia está escrita en una sola línea**, la
misma en las dos hermanas:

```csharp
Ask("consultar", …)
```

**Método en inglés, verbo en castellano.** El renombre cruzó la clase y se detuvo en la comilla. Lo
mismo con `"entregar"`, `"cursor"` y `"repertorio"`. Del otro lado de esa comilla hay un binario que
se compila aparte y no se entera de nada: **renombrar algo adentro es gratis; renombrar algo que
cruza hay que hacerlo en los dos lados y en el mismo commit.**

Acá la suerte está de nuestro lado, y conviene decirlo para no confiarse: un contrato por **texto**
falla ruidoso y de inmediato —el verbo no coincide y la sala contesta `ok:false`—, a diferencia de
los demás defectos de este documento, que fallan callados. La regla no es menos necesaria; lo que
cambia es cuánto tarda en cobrarse.

#### Cómo se hizo, porque el método es lo reutilizable

Un renombrado masivo sobre código que habla con otro binario **falla en silencio**: compila, pasa las
pruebas y deja de contestar en el aparato. Así que se hizo con **auditoría de literales**: se
fotografiaron los 395 strings de los cuatro archivos, se renombró, y se comparó. Cualquier cambio que
no fuera una interpolación tenía que justificarse.

Atrapó dos roturas reales:

1. **`window.sala.pedido` → `window.sala.requested`.** Es el nombre de la función JS del sustrato.
   Habría dejado la sala sin contestar nunca, sin un error de compilación.
2. **`new { entrada, archivos }` → `{ entrada, files }`, y `{ rutas }` → `{ paths }`.** Campos json
   escritos como propiedades de un objeto anónimo, o sea **sin comillas: la auditoría de literales no
   los ve.** Aparecieron leyendo el resultado, no por la herramienta. Ahora van con nombre explícito
   —`archivos = files`, `rutas = paths`—, que los vuelve inmunes al próximo renombrado.

La lección de la segunda es la que vale guardar: **una barrera automática cubre la forma que sabe
mirar, y el contrato también vive en las formas que no.** Un campo de protocolo escrito como
identificador de C# es indistinguible de una variable para cualquier herramienta que no sepa qué es
protocolo.

#### Lo que queda sin verificar

Se verificó **símbolo por símbolo** que los 27 nombres nuevos de `IosRoom.cs` tienen declaración y
usos coherentes — y eso encontró un error real: `ViewController` seguía llamando `sala.Calentar`
cuando `IosRoom` ya exponía `WarmUp`.

> **Y ese método manual sobraba · corregido 2026-09-25.** Se hizo porque este documento afirmaba que
> «`Gnothi.iOS` no se compila desde Windows». **Se probó y compila**: `dotnet build Gnothi.iOS`
> produce `net9.0-ios/iossimulator-x64/Gnothi.iOS.dll` en 12,7 s, y puesto a prueba rompiéndolo a
> propósito da `CS0117`. **El compilador habría cantado el `Calentar`/`WarmUp` solo.**
>
> La afirmación nunca se había probado: se heredó, se repitió en dos documentos y sostuvo tres días
> de verificación a mano. Lo que la Mac sí sigue necesitando es **enlazar, empaquetar y correr** —
> compilar no es correr, igual que calentar no es cargar.

---

# Los tres anfitriones del mismo paquete, medidos el mismo día · 2026-09-25

El mismo módulo (`compras`, híbrido, 87 compras desde febrero de 2025), el mismo journal, la misma
máquina y la misma tarde, corrido tres veces: **en proceso, en la caja y en la sala.** Las tres
pintaron la misma pantalla —mismo total, `₡3.174.606`—, que es lo que vuelve comparables los números.

## Lo que hizo falta para poder correrla, y es el hallazgo

**Dos de los tres no sabían contar.** `BoxedInstance.Ask` cronometra cada vuelta al borde desde el
2026-09-18; `HybridShell` también. Pero:

- **La sala no tenía contador.** Se escribió entera sin él.
- **La híbrida lo tenía y nadie se lo cableaba en escritorio.** `HybridShell.Log` es un gancho que
  la plataforma llena al arrancar: `MainActivity` lo hace desde el 2026-09-18, `Program.cs` nunca lo
  hizo. La cáscara contaba, cronometraba, y se lo decía a un delegado vacío.

Las dos son la misma forma de defecto: **un instrumento que existe y no está enchufado no se
distingue de uno que no existe.** Y no se ve corriendo una forma sola —cada corrida se lee entera y
razonable—; se ve al ponerlas al lado, cuando una imprime doce líneas y la otra ninguna.

Se agregó `RoomShell.Log` + `trips` (con la misma forma de línea que las otras dos, para que los tres
logs se lean y se grepeen igual), y se cablearon los tres ganchos en las tres plataformas.

## Los números, medidos

Vueltas al borde para pintar la portada de `compras`, en milisegundos:

| vuelta | en proceso | caja (wasm) | sala (webview) |
|---|---|---|---|
| **primera pintada** ||||
| arranque / `cargar` | — (hidrata en la 1.ª) | `arranqueMs` 39 | `ms` 125 · borde 962 |
| cursor | 2803 | 447 | 8 |
| las 6 vistas | 2851–2955 | 37–325 | 55–521 |
| **reloj de pared** | ≈ **2955** (en paralelo) | **1757** (en serie) | **2186** (en serie) |
| **segunda pintada (en caliente)** ||||
| las 7 vueltas | 0–29 | 0–249 | 1–129 |
| mediana por vuelta | **14** | **41** | **44** |
| **reloj de pared** | ≈ **29** (en paralelo) | **566** (en serie) | **374** (en serie) |

**Esa es UNA corrida, y repetirla la desarmó en parte.** Lo que sigue es lo que quedó en pie.

Los dos números dicen cosas distintas y los dos hacen falta: **la mediana es lo que cuesta cruzar el
borde una vez; el reloj de pared es lo que espera la persona**, y entre uno y otro está lo que la
serialización agrega por sí sola.

## Ninguna de las dos columnas de la híbrida se suma, que es el segundo hallazgo

**La híbrida en proceso corre las siete consultas EN PARALELO; la caja y la sala las corren en
serie.** Se ve en el log sin instrumentar nada más: los números de vuelta salen desordenados —`#6`
antes que `#3`— y los siete tiempos son casi el mismo (2803–2955 ms en frío), porque los siete están
esperando **la misma** hidratación del journal.

Así que la columna «en proceso» **no se suma, ni en frío ni en caliente**: su reloj de pared es el
máximo. Las otras dos sí se suman, porque `oneAtATime` las serializa: una pregunta a la vez, que es
lo que exige un protocolo de una línea por pedido contra un invitado de un solo hilo.

**Y en caliente esto estuvo escrito mal acá durante una hora**, con la columna sumada a 82 ms y el
precio del borde dicho como «4,5× a 7×». La advertencia estaba escrita para la primera pintada
—donde el solapamiento es imposible de no ver, tres cifras casi idénticas— y **no se aplicó a la
segunda, donde los números son chicos y el solapamiento no salta a la vista**. Sigue estando ahí: las
siete vueltas salen desordenadas y anidadas —`#11` dura 29 ms y adentro de esa ventana terminan `#12`
en 1 ms y `#9` en 3—, y el orden en que terminan es casi exactamente el orden de sus duraciones, que
es lo que pasa cuando arrancan todas juntas.

La regla que queda: **una suma sólo es un reloj de pared si algo garantiza que no hubo solapamiento**,
y acá ese algo tiene nombre y está en el código —`oneAtATime`—, así que se puede verificar en vez de
suponerse. Donde no está, la suma es una cota superior y nada más.

Conviene decir hacia dónde erraba: **el error era conservador**, favorecía a la caja y a la sala
—ocultaba la mitad de su desventaja—, que es justamente por qué no molestaba al leerlo.

Dicho de otra forma: **en frío la caja gana por hacer menos trabajo a la vez, no por ser más
rápida.** Y en caliente, donde la hidratación ya está pagada en las tres, el orden se invierte y
queda el precio real del borde.

## Una trampa al leer los dos logs

Los dos protocolos usan la palabra `consultar` para cosas distintas:

| lo que se pide | caja | sala |
|---|---|---|
| una vista de la cara | `consultar/<vista>` | `vista/<vista>` |
| una consulta del asistente | `preguntar/<consulta>` | `consultar/<consulta>` |

Las líneas se ven iguales y **un `grep consultar` sobre los dos logs cuenta cosas diferentes**: en la
caja son las seis vistas, en la sala es la única pregunta del asistente. No es un error de ninguno de
los dos —cada protocolo se escribió aparte y cada uno es coherente—, pero es exactamente la clase de
parecido que produce una tabla equivocada.

## Lo que esto NO dice

- **No dice nada de iOS.** Todo esto es Cranelift sobre este portátil. En iOS el motor es un
  intérprete y el término que decide es justamente el de la columna del medio. Ver F8.
- **No dice nada de un teléfono.** La caja en Android tiene sus propios números medidos; estos son
  de escritorio y no se mezclan.
- **Una corrida por forma.** Los números de la segunda pintada son estables entre sí en forma
  —caliente contra frío— pero no se repitió la corrida: **tomar la tabla como precisión de
  milisegundo sería leerla de más.** Lo que sostiene es el orden de magnitud y el orden entre las
  tres.

## Repetida tres veces, y la mitad de la tabla no aguantó · 2026-09-25

Tres rondas de las tres formas, mismo módulo, mismo journal, misma máquina, la misma tarde.

### Lo que NO se confirmó: los agregados y el frío

**El frío no es reproducible, y por mucho.** La misma híbrida, el mismo trabajo:

| primera pintada, reloj de pared | ronda A | ronda B | ronda C |
|---|---|---|---|
| en proceso | 2955 | 436 | 383 |
| caja | 1757 | 2494 | 2719 |
| sala | 2186 | 3157 | 3253 |

**La híbrida se movió ×7,7 entre dos corridas de lo mismo.** Y se movió al revés que las otras dos:
en la ronda A fue la más lenta de las tres y en B y C la más rápida por goleada. Dos formas que se
mueven en direcciones opuestas entre rondas no se están midiendo a sí mismas: se está midiendo el
primer contacto con algo —caché del sistema de archivos, JIT, el antivirus mirando un binario recién
compilado—, y eso **no es una propiedad del anfitrión**. El journal no explica nada: tiene cuatro
archivos, `materialization` está vacía y nadie escribió después del alta.

**Las cifras de frío quedan retiradas.** No se usan para comparar nada.

**Y en caliente los agregados tampoco se sostienen en un número.** Suma de las siete vueltas:

| segunda pintada | A | B | C |
|---|---|---|---|
| en proceso (reloj = máximo) | 29 | 22 | 18 |
| sala (suma) | 374 | 644 | 623 |
| caja (suma) | 566 | 912 | 878 |

El **orden** aguanta las tres veces —en proceso ≪ sala < caja— y el **orden de magnitud** también:
**decenas de milisegundos contra centenas**. El cociente no: sale 13×/20× en A y 35×/49× en C.
Decir «el borde cuesta 13× y 20×» era quedarse con la ronda más favorable **sin saber que lo era**,
porque era la única que había.

### Lo que SÍ se confirmó, y es mejor que lo que se perdió

**Vuelta por vuelta, caja contra sala, las tres rondas:**

| vista | caja (A/B/C) | sala (A/B/C) | caja ÷ sala |
|---|---|---|---|
| `portada` | 249 / 382 / 351 | 129 / 202 / 192 | **1,9× · 1,9× · 1,8×** |
| `movimientos` | 192 / 339 / 307 | 104 / 173 / 183 | **1,8× · 2,0× · 1,7×** |
| `cabecera` | 7 / 13 / 10 | 22 / 42 / 27 | **0,32× · 0,31× · 0,37×** |
| `proxima` | 18 / 36 / 31 | 28 / 52 / 56 | 0,64× · 0,69× · 0,55× |
| `cursor` | 0 / 1 / 1 | 1 / 7 / 15 | ≈0 |
| `canasta` | 59 / 78 / 88 | 44 / 76 / 74 | 1,3× · 1,0× · 1,2× |
| `compras` | 41 / 63 / 90 | 46 / 92 / 76 | 0,9× · 0,7× · 1,2× |

**La caja es unas 3× más barata que la sala en las respuestas chicas y casi 2× más cara en las dos
grandes** —`portada` y `movimientos`, las dos vistas pesadas—, y esos cocientes se repiten dentro de
un ±10 % en las tres rondas. **Son mucho más estables que cualquier agregado**, porque cada uno
compara dos números tomados en la misma corrida y bajo la misma carga de máquina: lo que se mueve
entre rondas se mueve para los dos a la vez y se cancela al dividir.

De ahí la regla, que es lo reusable: **comparar de a pares dentro de una corrida aguanta; comparar
totales entre corridas, no.** Lo primero mide el anfitrión; lo segundo mide también el día.

### Una hipótesis con nombre, para la vez que se quiera bajar ese 1,9×

El patrón —barato en chico, caro en grande— tiene un candidato leíble en el código. En
`BoxedInstance.Receive`, el ensamblado de la línea que llega por el borde hace esto por cada trozo:

```csharp
halfLine.Append(chunk);
while ((cut = halfLine.ToString().IndexOf('\n')) >= 0)
```

`halfLine.ToString()` **materializa todo lo acumulado en cada trozo que llega**, o sea cuadrático en
el tamaño de la respuesta. Una vista chica no lo nota; `portada` y `movimientos` son justamente las
que llegan partidas en muchos trozos.

**Esto es SUPUESTO, no medido**: la correlación está medida tres veces, la causa está leída. Se
confirma o se descarta arreglando el ensamblado y volviendo a correr las mismas siete vistas — si el
1,9× de `portada` no se mueve, la causa es otra y hay que buscarla en el marshalling de wasm.

### Y hubo que arreglar el arnés otra vez, por tercera vez en dos días

Dos de las tres corridas de la ronda B fallaron con
`--abrir FALLÓ: la página de Gnothi todavía no está lista para mostrar un módulo`.

**La espera existía y era de 10 s**, copiada en dos lugares. El defecto no era que esperara poco:
**al agotarse seguía igual, sin decir nada**, y lo que llegaba al log era la excepción de más abajo
—un mensaje verdadero sobre la página, que no dice que el problema fue la espera—. Media hora
leyendo el síntoma equivocado.

Ahora es una función con nombre, `WaitForThePage`, con 60 s y **una línea cuando el plazo se agota**.
Lo segundo vale más que lo primero: 60 s tampoco alcanzarían en una máquina cargada, y lo que hace
falta entonces es saber **que fue eso**.

Y el primer arreglo introdujo otro defecto en dos minutos: la guarda era un `return` dentro del
lambda, y **el `--auto` que cierra la ventana está después del bloque**, así que la corrida quedaba
colgada con la ventana abierta. Quedó como `&&`. **Una guarda que se lleva puesto el cierre
automático es peor que la espera que vino a arreglar.**

Van tres en dos días —el contador de la sala que no existía, el `HybridShell.Log` que nadie
enchufaba, y esta espera que se agotaba en silencio— y las tres son la misma familia: **el arnés
falla callado y se lo confunde con lo medido.**

## La hipótesis del `Receive` cuadrático, REFUTADA · 2026-09-25

Se arregló y no movió nada. El criterio de refutación estaba escrito de antemano y se cumplió.

### El arreglo

`BoxedInstance.Receive` preguntaba `halfLine.ToString().IndexOf('\n')` en la condición del `while`:
materializaba **todo lo acumulado** en cada trozo que llegaba, o sea N² en el número de trozos. Ahora
el corte se busca en el trozo nuevo y el `StringBuilder` se materializa **una vez por línea
completa**, no una por trozo.

### La medida, y por qué es ésta y no la obvia

Comparar «antes» contra «después» es comparar **totales entre corridas**, que es justo lo que se
acaba de establecer que no aguanta. La medida buena es un cociente **dentro** de cada corrida:
`portada` ÷ `cabecera` —la vista más pesada contra la más liviana, en la misma corrida y bajo la
misma carga de máquina—. Si el costo fuera cuadrático en el tamaño, ese cociente tenía que bajar.

| `portada` ÷ `cabecera`, segunda pintada | ronda 1 | ronda 2 | ronda 3 | media |
|---|---|---|---|---|
| **antes** del arreglo | 35,6 | 29,4 | 35,1 | **33,4** |
| **después** del arreglo | 40,4 | 35,1 | 34,6 | **36,7** |

**No bajó.** Lo mismo con `movimientos` ÷ `cabecera`: 28 antes, 33 después. **La causa es otra.**

Y una de las tres corridas de después salió en una máquina cargadísima —`portada` en 4242 ms en vez
de 350—, y **su cociente igual dio 35,1**, en la mitad de la tabla. Eso no es un detalle: es la regla
de los pares funcionando a la vista. El ruido se llevó los absolutos por un factor de 12 y no tocó el
cociente.

### El arreglo se queda, y conviene decir por qué

No compró lo que se fue a buscar, pero **quita un cuadrático real** y hace menos trabajo por trozo.
Las tres corridas posteriores pintaron la misma pantalla de siempre (`₡3.174.606`), que es la
comprobación de que el ensamblado de líneas sigue bien. Queda como mejora sin mérito medido, dicho
así y no como «optimización».

### Adónde apunta lo que quedó, que es más interesante que la hipótesis caída

El cociente grande÷chico, en las tres formas:

| | en proceso | sala | caja |
|---|---|---|---|
| `portada` ÷ `cabecera` | ≈3 | ≈6 | **≈35** |

**La caja es la única que se dispara con el tamaño de la respuesta.** Y eso descarta la explicación
cómoda —«es el módulo corriendo en wasm»—: la sala **también** corre el dll del módulo sobre wasm, y
su cociente está del lado de la forma en proceso, no del de la caja. Lo caro no es computar adentro
de un wasm: **es pasar bytes por ESTE borde.**

Lo que queda por descartar es el otro lineal-pero-caro, que el arreglo de hoy no toca: `Receive` se
llama desde `set_stdout_custom`, **una vez por trozo que wasmtime decida entregar**, y cada llamada
cruza de nativo a manejado y hace `Marshal.Copy` + `Encoding.UTF8.GetString`. Si los trozos son
chicos, el costo es el número de cruces y no el tamaño total.

**Se mide con un instrumento de una línea**: contar trozos y bytes por vuelta. Si `portada` llega en
cientos de trozos y `cabecera` en uno, está contestado. Es la próxima, y **esta vez el instrumento va
antes que la hipótesis.**

## Se borró una sala entera, porque era para un modo muerto · 2026-09-25

En escritorio hubo dos salas por un día: la de Photino —un iframe oculto de la página de Gnothi,
servido por loopback— y una segunda sobre WebView2, con documento propio y host virtual, **407
líneas**. Esa segunda servía a `--cara ventana`, la forma vieja de Windows donde la cara de un
módulo es una ventana de WebView2 aparte.

**No la pasa nadie.** Ni un instalador, ni un script, ni un doc: sólo aparece si alguien la escribe a
mano. Un `grep` por el repo, un minuto.

Y con un módulo de verdad **ni siquiera cargaba**: timeout de 10 s en `cargar`, documento tirado, y
otra vez, cinco veces seguidas.

### Dos defectos, y el segundo es el que enseña

1. **Construir para una bandera sin comprobar quién la pasa.** La bandera existía y compilaba, o sea
   que todas las señales baratas decían que sí.

2. **Dar por verificada una forma con el calentado.** El README decía «los dos motores, medidos el
   2026-09-24». Lo medido era `--sala`: el sustrato levanta, la sonda P0 contesta. Todo cierto —y
   **no dice nada de si un módulo carga**, que es para lo que existe una sala. Calentar y cargar se
   parecen lo bastante en el log como para confundirlos, y **el calentado pasa en condiciones donde
   la carga falla**: eso es exactamente lo que había.

**Una forma sólo está probada por el escenario completo: un módulo de verdad pintando su pantalla.**

### Y no se encontró corriéndola, que es lo incómodo

Se encontró porque alguien preguntó «¿esto ya está terminado?». Las tres corridas de medición de esa
tarde —cocientes, hipótesis, refutación— no lo habrían encontrado nunca: **medían la parte que
andaba.** Un instrumento afinado sobre el camino bueno no dice nada del camino que nadie recorre.

### Lo que quedó, verificado después de borrar

| escenario | qué pasa |
|---|---|
| camino real (Photino) | caja encendida, la sala reclama `compras`, carga y pinta `₡3.174.606` |
| `--cara ventana` | la sala **se aparta con una línea** y la caja corre `compras`; la pantalla se pinta igual |

Lo segundo es lo que antes no pasaba: el modo muerto construía una sala rota. Ahora falla cerrada
hacia lo que ya funcionaba, que es el mismo criterio de la caja.

## Barrido por «¿qué más quedó a medias?» · 2026-09-25

Después de la sala muerta, la pregunta obvia. El barrido encontró **dos cosas del mismo tamaño**, y
las dos eran afirmaciones nunca probadas que sostenían trabajo.

### 1 · Se tocó código compartido y se compiló UN solo consumidor

`BoxedInstance`, `RoomShell`, `BorderPipe` y `WasmBox` los compilan **tres** proyectos: escritorio,
Android e iOS. En toda la tarde se compiló sólo el escritorio — `MainActivity.cs` y
`ViewController.cs` se editaron y no se pasaron por ningún compilador.

**Corregido: los tres compilan.** Android, 0 errores. Escritorio, 0 errores. Las 274 pruebas pasan.

### 2 · «`Gnothi.iOS` no se compila desde Windows» era falso

Esa frase estaba en este documento y en `PENDIENTE-DE-PROBAR-EN-MAC.md`, y **sostuvo tres días de
verificación símbolo por símbolo**. Se probó:

```
dotnet build Gnothi.iOS -c Debug
Gnothi.iOS -> ...\bin\Debug\net9.0-ios\iossimulator-x64\Gnothi.iOS.dll
0 Errores — 12,7 s
```

Y se comprobó que sirve, rompiéndolo a propósito: `RoomShell.LogQueNoExiste` da `CS0117` y
desaparece al restaurar. **El `Calentar`/`WarmUp` que se cazó a mano lo cantaba el compilador en doce
segundos.**

La Mac sigue haciendo falta para **enlazar, empaquetar y correr** — compilar no es correr, igual que
calentar no es cargar. Pero el trabajo manual sobraba entero.

### Lo que tienen en común las tres cosas del día

La sala muerta, el compilador de iOS y el calentado tomado por carga son **la misma forma**: una
afirmación barata de comprobar que nadie comprobó, sosteniendo trabajo caro.

- «nadie pasa esa bandera» — un `grep`, un minuto.
- «no compila desde Windows» — un build, doce segundos.
- «el calentado prueba que la sala anda» — una corrida con un módulo, un minuto.

Ninguna se cayó midiendo con más finura. **Las tres se cayeron al preguntar si eran ciertas.**

### Arreglado también: la línea que hizo posible el tercero

`--sala` ahora dice lo que **no** prueba:

```
sala: arranco sin modulos -- esto NO prueba que un modulo cargue;
      para eso  --sala-solo <modulo> --abrir <modulo>
```

Y se limpió un resto de la vuelta atrás del alta: `string tipo = forma;`, un alias que quedó cuando
se revirtió el gancho de `"sala"` y que daba dos nombres al mismo valor.

### Lo que queda pendiente, y está declarado

| pendiente | por qué no se cierra acá |
|---|---|
| iOS **corriendo** (`.app`, simulador, `libGnothiIntents.a`) | hace falta una Mac |
| Escritorio en **macOS y Linux** | el FIFO de POSIX de `BorderPipe` nunca corrió ahí |
| Los otros tres assets de wasmtime | hay que bajarlos en cada máquina y fijar su hash; hoy sin asset la caja **falla cerrada** y todo corre como antes |
| Caja **y** sala encendidas a la vez en el teléfono | leído del código, no medido |

---

# «Todo lo de Gnothi pasó a inglés» era falso · 2026-09-28

Este documento lo afirma en su línea 85, sobre el renombre del 2026-09-17. **Nadie lo volvió a
comprobar**, que es la misma forma de los tres defectos de la semana pasada.

## Lo que había

**370 líneas de comentario en castellano** en los `.cs` de `Gnothi.*` — más ~334 en el js/razor/html
propio. Y lo peor: **~250 de las 370 se escribieron DESPUÉS de la pasada**, en el código del
escritorio de la semana del 22. La pasada se hizo y después se deshizo sola, archivo nuevo a archivo
nuevo.

## Lo corregido

| | antes | después |
|---|---|---|
| comentarios en `.cs` de `Gnothi.*` | 370 | **99** |

Los cinco archivos que nacieron en castellano —`PhotinoRoom`, `RoomFiles`, `BorderPipe`,
`TheDesktopRoom`, `TheDesktopBox`— y los bloques agregados a `Program.cs`, `BoxedInstance.cs`,
`ModuleEnrollment.cs`, `MainActivity.cs`, `ViewController.cs`, `RoomShell.cs` y `Costura.cs` están en
inglés. `frames.js` también.

Las 99 que quedan son **anteriores** y de otros frentes: `Gnothi.Assistant/Json.cs` (44),
`Gnothi.Contract` (12), `Gnothi.iOS/AppDelegate.cs` (9), `Shell.cs` (7), `AgentLoop.cs` (7).

## Identificadores: `SalaBridge` → `RoomBridge`, y el puente JS con él

`SalaBridge`/`FromSala` → `RoomBridge`/`FromRoom`; `abrirSala`/`aLaSala`/`tirarSala` →
`openRoom`/`toRoom`/`dropRoom`; `salaFrame` → `roomFrame`. **Se pueden renombrar porque los dos lados
viajan juntos** en este repo, a diferencia de `SalaPuente` y `window.sala.pedido`, que son el
contrato con un sustrato que se compila aparte y **no se tocan**.

Verificado de punta a punta después: la sala carga `compras` y pinta `₡3.174.606`.

### Y el renombre masivo volvió a pisar la misma trampa, en el mismo día

`{ verbo: verbo, modulo: modulo }` quedó como `{ verbo: verb, modulo: module }`… pero el `sed` había
convertido **las dos** mitades, dejando `verbo: verbo` apuntando a una variable que ya no existía.
Son **campos del sobre** que el shim lee con `e.data.verbo`. Es exactamente la trampa que este
documento registra del 2026-09-23 — *un campo de protocolo escrito como identificador es
indistinguible de una variable para cualquier herramienta que no sepa qué es protocolo* — y se cayó
en ella otra vez, con la lección escrita cinco párrafos más arriba. Queda dicho en `frames.js`, al
lado del código.

## Trazas muertas en los docs

`Gnothi.iOS/HALLAZGOS-LA-SALA-EN-IOS.md` nombraba **11 veces** código que ya no existe:
`SalaShell.Vecindario`, `SalaShell.Tope`, `SalaShell.Pedir`, `SalaShell.Cursor`, `ISalaHost`,
`SalaShellStub`. Corregidas, con una tabla de equivalencias arriba y un párrafo que **queda con los
nombres viejos a propósito** porque describe el estado de dos ramas de git ese día.

### Y una traza muerta tapaba un mecanismo mal descrito

`RoomShell.cs:93` decía, **en un comentario en inglés**, que `Ask` empieza con `await Arrancar(ct)`.
Dos errores en una frase: `Arrancar` no existe desde el 2026-09-23 —en el archivo del propio
renombre— y la llamada no está en `Ask` sino en cada verbo, un nivel más arriba. El camino real es
`verbo → Ensure(ct) → Ask("cargar") → Ceiling`.

**Ese es el daño de verdad de una traza muerta: no manda a buscar algo que no existe, manda a creer
algo que no es.** El nombre viejo lo tapó cinco días.

## Identificadores chicos y nombres de archivo · 2026-09-28

### Renombrados

| antes | ahora | dónde |
|---|---|---|
| `Cerrar` · `AbrirModulo` · `EntregarTraspaso` | `Close` · `OpenModule` · `DeliverHandover` | `MainActivity` |
| `LeerTodo` · `LeerLoCompartido` · `LeerAperturaDe` · `LeerArchivoDeModulo` · `LeerTraspasoDe` | `ReadAll` · `ReadTheShared` · `ReadOpeningOf` · `ReadModuleFile` · `ReadHandoverOf` | `MainActivity` |
| `Cerrar` · `Soltar` · `AbrirModulo` · `LeerDocumento` | `Close` · `Release` · `OpenModule` · `ReadDocument` | `ViewController` |
| `Leer` · `crudo` | `Read` · `rawText` | `ModelFoundation` |
| `Cablear` · `preguntona` · `Consultar` | `Wire` · `asksTheHost` · `Query` | `WebModule` |
| `Asistente.razor` · `Ajustes.razor` | `Assistant.razor` · `Settings.razor` | + el `enum View` y los `<Tag/>` |
| `idioma-interop.js` · `fijarIdioma` | `language-interop.js` · `setLanguage` | + su llamante en `Root.razor` |
| `Costura.cs` · `Costura` | `Seam.cs` · `Seam` | + `Program.cs` de la suite |

### Lo que NO se renombró, y el criterio es el mismo de siempre

**`puente.js` se queda.** Es el shim que el escritorio sirve en `/__gnothi/puente.js` y que **el
`index.html` de cada módulo pide por su nombre** (`<script src="puente.js">`). Los módulos son
paquetes de las instituciones, empacados y firmados aparte: el nombre cruza esa frontera igual que
`SalaPuente` y `window.sala.pedido`. Renombrarlo sería romper cada módulo ya entregado.

**`Textos` se queda** (105 usos): el usuario lo dejó fuera de esta tanda, y renombrar `Textos.cs`
sin renombrar la clase dejaría el archivo peleado con su tipo.

### Una verificación que el compilador no daba

El `import` de `language-interop.js` y la llamada a `setLanguage` **se resuelven en runtime**: un
nombre mal puesto compila y falla en pantalla. Se comprobó en el log de una corrida —
`import ./language-interop.js` seguido de `setLanguage ["es"]` — y no leyendo el código. Los
componentes `.razor` sí los ata el compilador, así que ahí alcanzó con que compilara.

**Verificado:** escritorio, Android e iOS compilan · 274 pruebas pasan · la sala carga `compras` y
pinta `₡3.174.606`.

## `Textos` → `Texts`, con su recurso · 2026-09-28

El último identificador grande: **105 usos en 18 archivos**, más el recurso embebido.

| antes | ahora |
|---|---|
| `Textos` · `Textos.cs` | `Texts` · `Texts.cs` |
| `Idioma` · `Espanol` · `Ingles` | `Language` · `Spanish` · `English` |
| `Faltantes` · `Discrepancias` · `Identicas` | `Missing` · `Mismatches` · `Identical` |
| `DelAparato` · `Normalizar` · `Declarada` · `Marcadores` · `Frase` · `DelIdioma` · `Catalogo` · `Texto` | `FromTheDevice` · `Normalise` · `Declared` · `Placeholders` · `Phrase` · `OfTheLanguage` · `Catalogue` · `Text` |
| `textos.json` · `gnothi.textos.json` | `texts.json` · `gnothi.texts.json` |

`T(...)` se queda: es el nombre convencional de i18n y ya es neutro.

### Lo que NO lo cubría el compilador, y por eso se probó rompiéndolo

**El nombre del recurso se resuelve en ejecución.** `Texts.cs` lo busca por `gnothi.texts.json` y los
tres `.csproj` lo declaran con ese `LogicalName`; si alguno no coincidiera, **compila igual y la
pantalla sale sin texto**. El propio csproj lo dice desde antes: *«tocar una de las dos no rompe el
build: rompe la pantalla, en ejecución»*.

Así que se comprobó a la manera de siempre, rompiéndolo a propósito:

| qué se rompió | resultado |
|---|---|
| `LogicalName` de `Gnothi.Mobile.csproj` | `[FAIL] Gnothi.Mobile declares the catalog with the shared LogicalName` |
| el `const Resource` de `Texts.cs` | **8 pruebas en rojo** |
| restaurado | 0 en rojo, 274 pasan |

Ocho pruebas rojas al tocar una constante es exactamente lo que se quería saber: **la suite sí baja a
leer el recurso**, no sólo a compilar contra él.

### Y uno que se dejó a propósito

`Shell.cs` tiene `"textos" => Texts.Json`: es el **op del puente entre `ui.html` y el Host**, no un
identificador. Sus vecinos ya están en inglés (`modules`, `setTool`), así que renombrarlo sería
coherente — **y no se hizo porque no se puede verificar acá**. Ese camino sólo corre en los
teléfonos: el escritorio no pasa por ahí, lee `Texts.T` por referencia de proyecto. Un renombre en
los dos lados compilaría, pasaría las pruebas, y **la pantalla del teléfono saldría sin texto** sin
que nada lo dijera.

Es la misma regla que dejó tres afirmaciones falsas esta semana, aplicada antes y no después: **no se
da por hecho lo que no se puede correr.** Queda anotado para el día que haya un teléfono a mano.
