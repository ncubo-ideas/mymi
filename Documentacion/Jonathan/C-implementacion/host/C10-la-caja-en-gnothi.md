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

Por eso `CajaShellStub.cs` tira con un mensaje y ya. **En escritorio no hay tienda**, así que la
restricción 2 no aplica; y la 3 se resuelve distinto, con el proceso hijo y el confinamiento del
sistema operativo, que es lo que `B1` y `D2` describen. Lo que en un teléfono es una prohibición de
tienda, ahí no lo prohíbe nadie.

---

## Lo que se agregó

| archivo | de qué responde |
|---|---|
| `Gnothi.MobileShared/Wasmtime.cs` | la C API de wasmtime, a mano. 112 líneas de `DllImport` |
| `Gnothi.MobileShared/CajaDeWasm.cs` | **el motor, uno para todos**: precompilar, la huella, descartar y recompilar |
| `Gnothi.MobileShared/InstanciaEnCaja.cs` | **un módulo vivo** con su borde: el FIFO para hablarle, el callback para oírlo |
| `Gnothi.MobileShared/CajaShell.cs` | la cáscara: `IShell` sobre una instancia |
| `Gnothi.Host/CajaShellStub.cs` | el stub de escritorio, como los de webview e híbrida |
| `Gnothi.Mobile/LaCaja.cs` | lo que Android pone: las dos rutas, el log, y el interruptor de esta etapa |
| `Gnothi.Mobile/lib/arm64-v8a/libwasmtime.so` | 27 MB, publicado por bytecodealliance para `aarch64-android` |
| `Gnothi.Mobile/Assets/examenes-en-caja.wasm` | 6,3 MB *(ver «lo que este corte junta a propósito»)* |

**El APK pasa de 24 a 51,6 MB.**

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

Hoy la usa `LaCaja.cs` para correr un módulo híbrido **ya instalado** adentro de la caja **sin
reescribirle su `installation.json` a la persona**, que es un archivo suyo.

---

## La comparación con la híbrida, decisión por decisión

### La cáscara: son la misma clase con un campo cambiado

```csharp
// HybridShell                          // CajaShell
private readonly WebViewShell face;     private readonly WebViewShell face;
private readonly InProcessShell surface;private InstanciaEnCaja? viva;
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
// InProcessShell.View                          // CajaShell.View
=> Ask(name, arguments, accepts, ViewMarks);    => Crudo(await (await Caja(ct))
                                                     .Pedir("consultar", name, Carga(arguments), ct), name);
```

Y **la cara recibe lo mismo** porque el puente ya hacía `answer as string ?? Serialize(answer)`: una
cadena que ya es json pasa verbatim y no se serializa dos veces.

Para el asistente sí hay conversión: `InProcessShell` devuelve su `Project(...)` —diccionarios,
listas, primitivos— y la caja devuelve texto, así que `CajaShell.Aplanar` reconstruye esa misma
forma. Son veinte líneas y están ahí porque el frente de MCP espera objetos, no una cadena con json
adentro.

---

## Las cuatro decisiones que costaron, con su razón

**1 · El borde va por un FIFO, y el orden de apertura es lo que cuelga.**
La C API es asimétrica: `set_stdout_custom` entrega cada escritura del invitado a código nuestro,
pero **no hay callback de entrada** — sólo una ruta o un búfer fijo. Así que la ruta es un FIFO.

Abrir un FIFO para leer bloquea hasta que haya escritor, y al revés. `set_stdin_file` **abre para
leer ahí mismo**, así que llamarlo sin escritor en vuelo cuelga el hilo y **no se imprime una línea
más**: el log se corta y no hay error que leer. Por eso `InstanciaEnCaja` arranca un hilo que abre
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

**Y el código dice las dos cosas a la vez, así que conviene ser preciso.** `CajaDeWasm` es estática
y su comentario promete «uno para todos», pero guarda **un solo componente**: si llegara un segundo
módulo con otro `.wasm`, `Preparar` llama a `Soltar()` y recompila. Hoy no se nota porque hay un
módulo solo en caja, que es justo lo que este corte eligió medir. **El motor compartido es real; el
sustrato compartido todavía no.**

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
| `CajaDeWasm` | nada: el `.cwasm` no estaba atado a su fuente | un wasm nuevo con un precompilado viejo y sano corría **el código anterior** |
| `LaCaja` | **el largo** del asset del APK | el build nuevo salió con **los mismos 6 583 761 bytes** y el asset no se copió |
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

### 4 · Cuánto cuesta pintar la pantalla, contra el mismo módulo en proceso

**Bajado de prioridad el 2026-09-16.** Se deja escrito porque el número no existe y conviene que se
sepa que no existe.

«La pantalla» no es una pregunta: son **veintiuna**. `cara.js` pide `historial`, `alertas` y
`analitos`, y después **una `tendencia` por analito, de a una y en orden** —encadenadas con `reduce`,
no en paralelo—, que con 18 analitos son 18 más. Y se pinta **dos veces**: lo que ya hay, y otra vez
cuando vuelve el cartero. ~42 idas y vueltas por apertura.

En proceso cada una es una llamada a un método de un DLL cargado. En la caja es escribir en el FIFO,
despertar el hilo del invitado, invocar por reflexión, serializar adentro, salir por el callback — y
con `SemaphoreSlim(1,1)`, **una a la vez**. El spike midió ~1 ms por consulta pero sobre vistas
simples. **A 1 ms son 21 ms y nadie se entera; a 30 ms son 600 ms de pantalla en blanco.**
