# 07 · El sustrato compartido, medido — y por qué la forma que corre no puede

**2026-09-07.** **Para Álvaro.** Contesta tres cosas que él dejó abiertas: la medición pendiente
de `NotasGnothi/07 §7`, la restricción **R9** de `04-restricciones-dribleadas`, y qué pasó con el
spike bloqueante **S1** de `06-plan-implementacion`.

La respuesta corta: **el equipo construyó una tercera forma de módulo que no está en la tabla de
R9**, y esa forma está en el extremo opuesto de la que comparte sustrato. No es que compartir sea
imposible: es que las dos formas que corren hoy son los dos extremos de su propia tabla, y la del
medio —su S1— está probada en escritorio pero **no llegó al teléfono**, que es donde S1 preguntaba.

---

## 1 · La medición que pidió en `07 §7`

Él escribió: *«Pendiente y barato: publicar en Release trimmed contra master actual y medir
`_framework`. Los números que circulan son de un build Debug sin trimming contra un árbol
pre-split, así que sobrevalúan el problema.»*

Los bundles de los módulos hospedados se publican con `dotnet publish -c Release` —trimmed—
contra master actual. **[MEDIDO 2026-09-07]** sobre el módulo de exámenes instalado:

| | estimado (Debug, sin trim, pre-split) | **medido** (Release trimmed) |
|---|---|---|
| sustrato · `Puppeteer` + `Choreography` + Portador | 1,3 MB | **0,97 MB** |
| runtime .NET | ~7,4 MB | **10,02 MB** |
| dominio | 50–200 KB | **0,29 MB** (dominio + UI) |
| relación sustrato+runtime : dominio | ~60:1 | **38:1** |

**Su predicción salió a medias, y la mitad que falló es la que importa.** El sustrato bajó —el
trimmer funcionó— pero **el runtime subió**, de ~7,4 a 10,02 MB. Así que el problema de tamaño
**no está en el sustrato**: está en el runtime, y el trimming no lo toca.

Consecuencia directa para su razonamiento: él anticipó que la medición *«degrade el motivo
declarado (tamaño) y ascienda el otro (auditoría)»*. Pasó eso, pero por otra razón que la
esperada — no porque el sustrato encogiera lo suficiente, sino porque **compartirlo dejó de ser
la palanca**. En la forma que corre hoy, el sustrato es sólo el **8,6%** del bundle:

| | | |
|---|---|---|
| **A** · sustrato — lo que el Host ya tiene y podría prestar | 0,97 MB | **8,6%** |
| **B** · dominio y UI del módulo | 0,29 MB | 2,6% |
| **C** · correr .NET en un navegador | 10,02 MB | **88,8%** |

Los 10 MB están porque la UI de la institución está escrita en Blazor: un navegador no ejecuta
C#, así que hay que mandarle el .NET entero compilado a wasm antes que la pantalla. **No es un
costo que introduzca Gnothi** — la app suelta de la institución pesa lo mismo servida por ella,
sin Gnothi en el medio, porque es el mismo publish (`escenario-webview.ps1` lo dice: *«la app
suelta se sirve desde el MISMO publish que va adentro del bundle»*).

Y lo que esos 10 MB compran no es nada: el laboratorio escribe su dominio **una vez, en C#**, y
ese mismo código corre en el navegador y adentro de Gnothi.

---

## 2 · La tabla de R9, con la columna que faltaba

R9 comparó dos variantes. **La forma que hoy corre en el teléfono es una tercera.**

| | **A ·** DLL de IL en el proceso del Host | **B ·** dominio dentro de una instancia wasm | **C ·** módulo hospedado en WebView |
|---|---|---|---|
| **estado** | **funciona hoy** | **mecanismo probado en escritorio**; móvil sin medir | **funciona hoy** |
| **un solo sustrato** | **sí** | **sí** | **no — uno por módulo** |
| **tamaño del módulo** | **188 KB** | **15 KB** | **11,3 MB** |
| **sustrato en disco** | el del Host | **32 MB, y no se puede recortar** | 11,3 MB × módulo |
| **costo por consulta** | 150–217 ms | **500–670 ms** | ~1,0 s de arranque de motor |
| **aislamiento entre módulos** | ninguno — mismo heap | **duro — memoria lineal y `--dir` propios** | **duro — origen propio + CSP** |
| **de dónde sale el runtime** | del Host | **del Host** | **del bundle** |
| **trae la UI de la institución** | no | no | **sí** |
| **ancla de confianza** | firma + reputación | incapacidad | incapacidad |
| **una sola cosa que auditar** | **sí** — el instalador lo exige | sí | **no** |

> **CORRECCIÓN, 2026-09-07 · tarde.** La primera versión de este documento decía que la columna
> B era «un spike sin medir». **Es falso, y conviene saberlo antes de plantearlo en una
> reunión.** Jonathan la construyó y la midió el **2026-08-26**, y está documentada en
> `Documentacion/Jonathan/F-evidencia/F5-la-caja-wasm.md`, que entró al repo hoy: `CascaraWasm.cs`
> lanza wasmtime con `--dir`, `Gnothi.Caja/` es el shim que corre adentro, y **el mismo `.gmod`
> contesta byte-idéntico en proceso y adentro de la caja**. Las columnas B de arriba son sus
> números, no estimaciones.
>
> **Lo que sigue abierto es exactamente lo que S1 preguntaba:** su texto dice *«¿puede un runtime
> wasm embebido, **en Android y en iOS**…?»*, y F5 dice de sí mismo *«Escritorio con wasmtime,
> **no otras plataformas**. Lo probado es el mecanismo.»* Así que el mecanismo está resuelto y
> **el port a móvil no**. No es lo mismo que no haberlo intentado.

Las columnas A y C son medición propia: 188 KB son las DLL de `Examenes.Mcp` publicado, y 11,3 MB
lo que sirve el bundle hospedado de exámenes sin contar las copias comprimidas.

**Y hay un eco que vale mirar.** En la columna A, F3 mide que sacar el motor del módulo ahorró
menos que sacar el protocolo: *«el protocolo pesaba más que el motor»* — MCP 2,1 MB contra
Puppeteer + Choreography 972 KB. En la columna C pasa lo mismo un piso más arriba: el sustrato
es 0,97 MB y el runtime del navegador 10,02 MB. **Las dos veces, lo que se creía que era el peso
resultó ser el acompañante.**

**La A cumple lo que R9 pedía, y con guardia.** La decisión está escrita en
`Gnothi.Host/Gnothi.Host.csproj` como «decisión A1», el mecanismo es una línea de
`InProcessShell.cs` que le pregunta primero al contexto del Host, y el instalador **rechaza** un
paquete que traiga `Puppeteer.dll`, `Choreography.dll`, `Puppets.Portador.dll`,
`Gnothi.Contract.dll` o los dos de MCP. Su *«una sola cosa que auditar»* está implementada como
una lista de prohibidos, no como una intención.

**Esa guardia no se aplica a la C.** **[MEDIDO]** `ModuleEnrollment` —el alta desde el enlace de
la institución— nunca consulta esa lista. Así que hoy: un módulo en proceso que trae su motor se
rechaza; un bundle hospedado con 10 MB de motor entra sin que nada lo mencione. No es un bug —la
C **necesita** el suyo— pero significa que la garantía vale para una forma y no para la otra, y
eso no está escrito en ningún lado.

---

## 3 · Por qué la C no puede compartir

**En la C, el runtime ES el bundle.** El módulo corre adentro de un WebView que trae su propio
.NET compilado a WebAssembly. El .NET nativo de Gnothi y el del WebView son dos motores que no se
ven: el préstamo de la A es un mecanismo de `AssemblyLoadContext`, y ahí no hay ninguno que
compartir. Por el puente pasa **texto**, no objetos cargados.

Dicho en una línea: **la C no comparte porque en la C el Host no es dueño del runtime.**

Lo único que sí se le puede prestar es **el archivo, no el motor corriendo**. **[MEDIDO]** de los
11,4 MB que sirve cada módulo, **7,4 MB (65%) son byte-idénticos en los cuatro** —
`dotnet.native.wasm`, los tres shards de ICU, `Puppeteer.wasm`, `Choreography.wasm`,
`dotnet.runtime.js`. Guardar una copia y servirla a los cuatro orígenes ahorraría **disco**: los
cuatro pasan de 49 MB servidos a 23,8 MB deduplicados. No ahorra RAM ni tiempo, porque cada
WebView vuelve a montar su ejemplar.

**`System.Private.CoreLib` queda afuera de ese bloque, y es definitivo.** **[MEDIDO]** los cuatro
tienen hashes distintos y tamaños distintos —1.873.173 / 1.872.661 / 1.873.685 / 1.912.085
bytes—, y el diff byte a byte de dos de ellos da **1.603.141 de 1.873.173 bytes distintos, el
86%**. No es metadata de build: ningún `Deterministic=true` arregla un 86%. Se probó
explícitamente para descartar esa hipótesis antes de dar por buena la otra.

---

## 4 · Y si se les diera el mismo origen

Es la salida que aparece sola al mirar la tabla, así que conviene tenerla contestada.

**Primero, una corrección a lo que se dijo antes en voz alta:** el origen **no** es lo que separa
los journals. **[MEDIDO]** el puente se ata al WebView, no al origen —
`AddScriptMessageHandler(new BridgeToModule(this), …)` en iOS,
`AddJavascriptInterface(new BridgeToModule(this), …)` en Android — así que cada WebView tiene su
propio puente atado a su propio módulo, que conoce su propia ruta de journal. Con origen
compartido, **los journals de Gnothi siguen separados**.

Lo que sí protege el origen:

| | origen propio | origen compartido |
|---|---|---|
| journal de Gnothi | separado — por el puente | **sigue separado** |
| archivos del otro módulo (su wasm, su config) | no los puede pedir | **los puede pedir** |
| `localStorage` — donde va la credencial | separado | **compartido** |
| IndexedDB — donde va el journal de la app suelta | separado | **compartido** |
| navegar al origen del otro | rechazado en iOS | **legal** |

Y no es teórico: **[MEDIDO]** los bundles traen `credencial-interop.js`, que hace
`localStorage.setItem(CLAVE, …)`, y `journal-interop.js`, que abre IndexedDB. Es el camino de la
app suelta, y como el bundle y la suelta son el mismo binario a propósito, **ese código viaja en
los cuatro módulos**. Con origen compartido la protección pasaría a depender de que cada módulo
*elija* no usar el almacenamiento del navegador — que es el tipo de garantía que el diseño de
origen + CSP existe para no tener que pedir.

**Y lo decisivo: no compraría lo que se busca.**

1. **[MEDIDO]** hoy no se cachea nada. `BundleServer` sirve todo con `Cache-Control: no-store`, y
   el comentario dice por qué: una entrada vieja mostraría el build anterior de la institución sin
   forma de notarlo. Así que compartir el origen, solo, no ahorra un byte.
2. Aun cacheando, **cada WebView instancia su propio runtime**. Compartir bytes no es compartir el
   motor cargado: mejora el tiempo de carga, no la RAM.
3. Los navegadores particionan el caché por origen a propósito. **No existe «orígenes separados
   pero caché compartido»**: o se da el origen, o cada módulo paga lo suyo.

Para compartir el motor **instanciado** haría falta un solo documento — y eso ya no es cambiar el
origen, es fusionar los módulos en una app, que es la columna B y le saca a la institución su
pantalla propia.

---

## 5 · Lo que sí se puede hacer sin decidir nada

Tres cosas medidas, ninguna toca el contrato del módulo ni pide permiso a nadie.

**a · El 40% de cada bundle nunca se sirve.** **[MEDIDO]** cada activo viaja tres veces —crudo,
`.br` y `.gz`— y `BundleServer` no hace negociación de contenido: no lee `Accept-Encoding` ni
emite `Content-Encoding`. Son **32 MB muertos en los cuatro módulos**. Se verificó que sacarlos es
seguro: `blazor.boot.json` no los lista, `service-worker-assets.js` tampoco, y la única mención a
`.br` en todo el bundle es el XHR del runtime **leyendo** el header de la respuesta, nunca
pidiendo el archivo por nombre. El arreglo va en `armar-bundle.ps1` —nuestro script— y **no** en
apagar la compresión, porque la app suelta sí se sirve por red desde el mismo publish.

**b · `immutable` para lo fingerprinted.** Los activos llevan el hash del contenido en el nombre
(`dotnet.native.6buk8i5pk6.wasm`), así que **no pueden quedar viejos**: si cambia el build, cambia
el nombre. Se puede servir `immutable` para `_framework/*` y dejar `no-store` para el documento de
entrada. Gana la **reapertura** de un módulo — que es el caso común — sin exponer nada. Es la
única de las tres opciones de origen que no pide devolver algo a cambio.

**c · `substrate.range` se puede validar hoy, para la forma A.** **[MEDIDO]** el campo se
deserializa en `Gnothi.Host/Manifest.cs` y **no se lee en ningún lado**. `13 §10.2` de la entrega
de Jonathan lo explica como *«falta porque no hay un sustrato compartido cuya versión negociar»*,
y **eso ya no es exacto**: para la forma A el sustrato existe, es obligatorio, y su versión está
clavada en `Directory.Build.props` como `VersionDelMotor = 3.0.0-beta.10029`. Los manifiestos
declaran `">=3.0.0 <4.0.0"`. No falta el sustrato: falta el `if`.

---

## 6 · Números de contexto

**[MEDIDO 2026-09-07]**, sobre los cuatro módulos instalados por el alta real en un emulador:

| | |
|---|---|
| los cuatro en disco | **81 MB** |
| lo que el servidor realmente sirve | **49 MB** |
| deduplicado por contenido | **23,8 MB** |
| motor arrancando, por apertura | **~1,0 s** (emulador) |
| exámenes en proceso · propio | **188 KB** |
| exámenes en proceso · prestado por el Host | **2,35 MB** |

**Sobre la RAM, una corrección a `13 §10.2` de Jonathan.** Ahí dice que *«el costo de memoria es
por módulo… la regla de desalojo pasa a ser obligatoria»*. **La regla ya existe y ya es
obligatoria**: `WebViewShell.DisposeAsync()` desaloja al terminar cada llamada, y sólo el módulo
en pantalla sobrevive. Así que los 4 × 200 MB simultáneos no pasan.

El costo real no es RAM simultánea: es **latencia**. Cada pregunta cruzada arranca el motor de
cero en cada módulo que toca, ~1,0 s cada uno. Eso reencuadra R9: **el sustrato no es sobre el
peso del bundle, es sobre el tiempo de una pregunta cruzada** — que es la operación central de
Gnothi.

---

## 7 · Las tres preguntas que quedan, y de quién son

1. **¿Se lleva la caja wasm al teléfono?** No es «¿se mide S1?»: el mecanismo ya está probado en
   escritorio con wasmtime, con el módulo en **15 KB** y sin un byte de runtime adentro. Lo que
   falta es el port, y en un teléfono el runtime embebido no es wasmtime — es WAMR, que es
   justamente el candidato que él nombró en R2.

   Con los números de este documento se puede poner precio a la pregunta. **Con cuatro módulos**
   —los que hay— el disco queda casi igual: la caja son **32 MB una vez** más 4 × 15 KB ≈ 32 MB,
   contra 4 × 11,3 = **45 MB** de la forma hospedada. La caja gana, pero no por el orden de
   magnitud que sugería el 60:1, y **cobra en tiempo**: 500–670 ms por consulta contra 150–217
   en proceso.

   Y hay algo que la caja **no** reemplaza, y conviene decirlo: en la caja **no hay UI de la
   institución**. La forma hospedada paga 11,3 MB por traer la pantalla del laboratorio; la caja
   no la trae. No son la misma cosa más barata: son cosas distintas. **Es de Álvaro.**
2. **Si no se mide, ¿se acepta que hay dos formas de módulo con garantías distintas?** Hoy es así
   de hecho —la lista de prohibidos y el préstamo valen para la A y no para la C— y no está
   escrito. **Es de Álvaro.**
3. **¿Vale 10 MB que la institución escriba su pantalla en .NET?** Compra un dominio en un solo
   lenguaje, corriendo en los dos lados. Es una pregunta sobre el trato con la institución, no
   sobre la arquitectura de Gnothi — y hoy «la institución» somos nosotros, así que por ahora es
   decisión del equipo. **Es del equipo.**

Y **R10 sigue sin existir**: no hay suite de journals dorados. Mientras `substrate.range` no se
valide y no haya journals congelados, *«mantenemos compatibilidad hacia atrás»* sigue siendo una
promesa y no una afirmación falsable — que es exactamente lo que R10 vino a evitar.

---

## Fuentes de este documento

- `NotasGnothi/04-restricciones-dribleadas.md` — **R9** y **R10**, la tabla original.
- `NotasGnothi/06-plan-implementacion.md` — **S1**, el spike bloqueante.
- `Documentacion/Jonathan/F-evidencia/F5-la-caja-wasm.md` — **S1 hecho en escritorio**, medido el
  2026-08-26. Entró al repo el 2026-09-07 y obligó a corregir este documento.
- `Documentacion/Jonathan/F-evidencia/F3-el-modulo-sin-motor.md` — el préstamo medido del lado de
  la forma A: el módulo de 15 KB y el desglose de qué pesaba.
- `NotasGnothi/07-preguntas-abiertas.md` — **§7**, la medición que este doc contesta.
- La entrega de Jonathan, `13-movil-transicion-standalone-a-gnothi.md` **§10.2**.
- Todas las mediciones salen de los cuatro módulos instalados por el alta real el 2026-09-07 y
  del publish en Release de `Examenes.Mcp`. Ninguna es de laboratorio sintético.
