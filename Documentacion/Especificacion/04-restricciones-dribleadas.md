# 04 — Las restricciones técnicas y cómo las dribleamos

Este es el documento que más tiempo les ahorra. Cada entrada es una restricción real, qué bloquea, y qué hicimos. **Las marcadas con 🚫 son callejones cerrados: no los vuelvan a explorar.**

---

## R1. En móvil no hay `exec` — ni siquiera desde tu propia app

**El hecho.** Android bloquea `execve` sobre archivos del directorio de datos de la app desde API 29; los binarios ejecutables solo salen del `lib/` del APK. iOS directamente no tiene `fork`/`exec` para apps.

**Qué bloquea.** Cualquier diseño donde el Host lance el módulo como proceso hijo. Es decir: **stdio no puede ser el mecanismo en móvil, ni con host propio.**

**Cómo lo dribleamos.** El runtime va **embebido como biblioteca** y el módulo se invoca **in-process**. La cáscara in-process no es un lujo: es el único camino. Y como el módulo no tiene transporte (expone su superficie y nada más), **el mismo artefacto corre en los dos lados**: el Host elige la cáscara — stdio con un runtime wasm en escritorio, llamada en proceso en móvil.

---

## R2. iOS prohíbe JIT

**El hecho.** Apple no permite generación de código en runtime.

**Qué bloquea.** `wasmtime` queda fuera de iOS: depende de JIT.

**Cómo lo dribleamos.** Runtime en modo **intérprete o AOT**. Candidato principal: **WAMR** (WebAssembly Micro Runtime — pensado para embebidos, intérprete y AOT, ambas plataformas, soporte WASI preview1). Alternativa solo-Android: **Chicory** (runtime wasm puro en Java, sin NDK ni cadena nativa; cero fricción de build a costa de rendimiento). **wasm3** si hace falta huella mínima.

**Verificar antes de comprometerse:** el nivel de soporte WASI **actual** de cada uno. Ahí es donde se rompen las cosas, no en la ejecución del módulo.

**Nota relacionada, medida:** en wasm, `Expression.Compile()` no emite IL nativo (wasm prohíbe codegen en runtime) y cae al intérprete de LINQ Expressions. Consecuencia fina: **en wasm la ventaja del modo compilado del motor sobre el interpretado casi se colapsa** — ambos terminan interpretados. Simetría interesante: la prohibición de codegen que mata el fast path es la misma que hace segura la caja.

---

## R3. 🚫 La app del asistente de terceros no puede llamar a un módulo local en el teléfono

**El hecho.** La app móvil de un asistente de terceros es un cliente delgado: el bucle de tool-calling ocurre del lado del servidor. **No tiene mecanismo** para lanzar un proceso local ni para brokear sus pipes hacia el modelo. Y los conectores MCP remotos no se pueden ni dar de alta desde la app móvil — hay que registrarlos en la web y se sincronizan.

**Qué bloquea.** El escenario más deseable: "Sonia pregunta en su app de asistente, y el módulo en su teléfono responde".

**Es el único hueco que ninguna capa nuestra rellena.** No es un permiso que falte ni una API que podamos envolver: es una pieza que solo puede agregar quien escribe esa app.

**Cómo lo dribleamos — tres caminos, con su costo:**

| camino | funciona hoy | costo |
|---|---|---|
| **A. nuestra app es el host MCP** (implementa el *cliente* + el bucle de agente contra el proveedor de modelo) | ✅ | nos convierte en fabricante de shell de asistente; y fuerza la decisión BYOK vs. llave propia |
| **B. túnel / relay por device** | ✅ | no escala a consumidor (una URL por usuario, batería), y **un relay que termina TLS rompe la garantía central por diseño** |
| **C. Remote Control** (celular = ventana, ejecución en el escritorio) | ✅ | el journal vive en el escritorio; honesto como demo, no como producto |

**Elegido: A**, con la Puerta 2 aislada para poder borrarla (ver `02`). Y A tiene un valor extra: **demuestra que la arquitectura no depende de nadie**, incluso si nunca es el producto.

🚫 **No re-explorar B como producto.** El relay debilita el claim más fuerte del proyecto: un intermediario que termina TLS **puede ver el tráfico**, y ahí "no podemos verlos" deja de ser cierto por diseño y pasa a ser una promesa.

---

## R4. 🚫 stdio/ssh entre el Host y el módulo: descartado

**Qué se propuso.** Que el módulo hablara con el Host por stdio sobre ssh.

**Por qué se descartó — dos razones independientes:**

1. **ssh solo tiene sentido entre máquinas distintas.** En esta arquitectura el Host y el módulo viven en el mismo dispositivo. No hay nada que puentear.
2. **Si el módulo implementa un aparato ssh, el módulo tiene sockets.** Se cae "ninguno de ellos puede salir por su cuenta", que es la garantía que hace defendible todo lo demás.

Y una tercera de este repo: arrastraría de vuelta la **capa de transportes de Choreography**, que es exactamente la línea de amputación que se cortó para que el motor entre a un target confinado (ver R5).

**Lo correcto:** el módulo **no tiene transporte**. Expone su superficie; el Host elige la cáscara. Un solo artefacto, dos hospedajes.

✅ **ssh sí sirve como herramienta de desarrollo** para depurar contra un device. Nunca como capacidad del módulo publicado.

---

## R5. El motor arrastraba un framework de servidor web

**El hecho (medido).** `Choreography.csproj` tenía `<FrameworkReference Include="Microsoft.AspNetCore.App" />` porque lo necesitaba el listener HTTPS sobre Kestrel. Al compilar para `browser-wasm`:

```
error NETSDK1082: There was no runtime pack for Microsoft.AspNetCore.App
available for the specified RuntimeIdentifier 'browser-wasm'.
```

**Ese fue el muro #1**, y es notable porque invirtió las prioridades esperadas: no fue reflection, fue el shared framework de servidor.

**🚫 Trampa de MSBuild — no pierdan horas acá (medido).** Condicionar el `FrameworkReference` **dentro** del csproj **NO funciona**: `$(RuntimeIdentifier)` no se propaga por `ProjectReference`, y **NuGet restore ignora `AdditionalProperties`**, así que la referencia queda grabada en el `project.assets.json` del **proyecto que referencia** y el error reaparece aunque la condición esté "bien".

**Cómo se dribleó primero:** un csproj espejo aparte (`Choreography.Wasm`) que compilaba las mismas fuentes con otro conjunto de includes.

**Cómo está resuelto ahora, y esto es lo importante:** en master `67c9747` el problema **se arregló en la fuente**. `Choreography.csproj` ya **no** tiene el `FrameworkReference`; Kestrel/HttpsTransport, el transporte SimpleX, el StageManager, el flujo de onboarding Usher y el sink SMTP viven todos en `Ncubo.Puppeteer.Adapters.Sockets`. ⇒ **el carve `Choreography.Wasm` quedó obsoleto**: ya no hace falta csproj espejo, ni el truco de `AssemblyName` + firmar con la `.snk` de test.

Y `Puppeteer.csproj` quedó con **dos** `PackageReference`: un analizador (cero costo runtime) y `System.IO.Hashing`. Fuera: `Microsoft.Data.SqlClient`, `MySql.Data`, `Sodium.Core`, `Microsoft.AspNetCore.Mvc.*`.

**El guard que lo protege existe:** `UnitTestPuppeteer/PuppeteerCoreDependencyTests.cs` lee el **grafo de referencias emitido** y falla si el tier portable nombra uno de host (prefijos vigilados: `Microsoft.Data.SqlClient`, `MySql`, `MySqlConnector`, `Microsoft.AspNetCore`, `Sodium`).

**Hueco conocido del guard:** vigila referencias **IL**. Un `PackageReference` **sin uso** no deja referencia IL — pero **sí se propaga por el grafo de restore** al app consumidor. Caso vivo: `Choreography.csproj` conserva `BouncyCastle.Cryptography` (~4.8 MB) sin que ningún `.cs` de Choreography lo use; todo el uso real está en `Adapters.Sockets`. Hay tarea abierta para quitarlo.

**Si necesitan storage SQL en algún host:** `Ncubo.Puppeteer.Adapters.Sql` + `SqlStorageAdapters.Register()` una vez al arrancar, **antes** de cualquier `ConfigureStorage`. Es idempotente y thread-safe. Si lo olvidan **compila igual** (`DatabaseType.MySQL` sigue en el core) y falla en runtime con *"No storage provider is registered for DatabaseType…"*. `IN_MEMORY`, `FileSystem` y plaintext son in-core: sin registro.

---

## R6. wasm es single-thread y el replay del motor bloqueaba

**El hecho (medido).** El pipeline de rehidratación corría 3 etapas concurrentes (`parserTask` / `resolverTask` / `executionTask`) unidas por `Task.WaitAll`. En Mono-wasm single-thread eso lanza:

```
System.PlatformNotSupportedException: Cannot wait on monitors on this runtime.
  at Task.WaitAllBlockingCore(...)
  at ActorHandler.EventSourcingStorage(Diary)
```

**Sin este cambio el motor NO arranca en el browser.**

**🚫 Camino descartado: `WasmEnableThreads=true`.** Se probó y quedó **inconcluso** (la prueba no fue limpia: recargas en carrera). Se descartó **por principio, no por refutación**: mete `SharedArrayBuffer` + headers COOP/COEP + payload mayor **para que el código poroso siga funcionando**. El arreglo correcto es no bloquear.

**Cómo se dribleó (medido).** Las 3 lambdas se extrajeron a funciones locales y se agregó una rama: colas *unbounded* + etapas **inline en secuencia**, sin `Task.Run` ni `Task.WaitAll`. La ruta multihilo de producción quedó **idéntica**. Suite verde, sin regresión. Aclaración conceptual: no toca `PerformCommand`/`PerformQuery` — lo secuencializado es el **replay de arranque**, y su paralelismo era de *pipeline* (solape de fases), nunca ejecución desordenada: el orden del journal siempre se preservó.

**⚠️ ESTADO REAL, IMPORTANTE:** ese cambio **no está en master**. `ActorHandler.cs:1285` de master `67c9747` sigue con los tres `Task.Run` + `Task.WaitAll`. El camino secuencial vive en la rama **`wasm-replay`** (pusheada: `origin/wasm-replay`), sin mergear.

**⚠️ Y el guard está mal formado para móvil.** En esa rama, `ActorHandler.cs:1027`:

```csharp
bool singleThreaded = OperatingSystem.IsBrowser();
```

Tiene **forma de plataforma, no de capacidad**. Bajo WASI en un teléfono (fuera del browser) `IsBrowser()` es **false** ⇒ corre la rama bloqueante y estalla con la misma excepción que el browser ya resolvió. **Especificar el guard por capacidad ("este hospedaje no tiene hilos") antes de mergear.** Un flag inyectable, o `Environment.ProcessorCount == 1` como default con override explícito del Host. Si entra con forma de plataforma, cada hospedaje nuevo agrega un `||` a esa línea.

**Y nota de honestidad:** quedan otros blocking waits en el motor (`ActorHandler.cs` ~448, ~733, ~2276). **En la ruta ya ejercitada no se activan** (medido), pero si usan Materialization, Outbox, Shadow, Playbill o Saga, **es la primera cosa que puede reventar**. Medirlo temprano.

---

## R7. Trimming vs reflection

**El hecho.** El descubrimiento de dominio de Puppeteer es por **reflection** sobre el assembly de dominio. El trimmer de Release puede podar esos tipos, y **falla en silencio**, que es lo peor.

**Qué bloquea.** Publicar en Release con trimming — que es lo que hace falta para bajar el payload.

**Estado:** en Debug sin trimming la reflection **sobrevivió** (medido). En Release con trimming **no está medido**. Este es el **spike bloqueante real** del tamaño, antes de pelear con los megabytes.

**Cómo dribliarlo si aparece:** marcar dominio y motor con `TrimmerRootAssembly` / `[DynamicDependency]`, o desactivar trimming para ellos. **Prueba de humo de reflection primero**, optimización de tamaño después.

**Y el corolario para el descubrimiento de superficie:** un SDK de tools basado en atributos + reflection se rompe igual. La salida correcta son **source generators**: las anotaciones se resuelven en compilación y emiten el descriptor de tools y el dispatcher como código concreto. Trimming-safe por construcción.

---

## R8. 🚫 Un `.mcpb` por institución no sirve para contención

**Qué se propuso.** Que cada institución empaquetara su módulo junto con el runtime, en un bundle instalable de un clic.

**Por qué se descartó.** Si cada institución empaqueta el runtime, **cada institución controla el host** — y toda la garantía de capacidades se evapora. Un `.mcpb` por institución sirve para **distribución**, no para **contención**.

**Lo correcto:** **un solo Host, módulos puros sin runtime.** Se conserva casi todo el relato: el módulo se sigue bajando de la página de la institución, sigue sin pasar por tiendas. Lo que cambia es que **no trae motor**.

---

## R9. El sustrato repetido por módulo

**El hecho (medido, Debug sin trimming).** Sustrato ≈ 1.3 MB de IL (`Puppeteer` 1 128 KB + `Choreography` 145 KB) más un runtime .NET de ~7.4 MB. El dominio de un lab real: 50–200 KB (el dominio de Tetris midió **21 KB**). ⇒ relación **~60:1**.

**Cómo lo dribleamos.** El Host lleva el sustrato; el módulo es carga. Y hay **dos variantes**, con costos muy distintos:

| | DLL de IL en el proceso del Host | DLL de dominio **dentro** de una instancia wasm |
|---|---|---|
| Un solo sustrato | sí | sí |
| Tamaño del módulo | decenas de KB | decenas de KB |
| **Aislamiento entre módulos** | **ninguno** — mismo heap; `AssemblyLoadContext` **no es** frontera de seguridad (y .NET ya no tiene CAS/partial-trust) | **duro** — memoria lineal propia, `--dir` propio |
| Ancla de confianza | firma + reputación | **incapacidad** |
| Estado | funciona hoy | **spike sin medir** |
| Política de tiendas | un DLL de IL cargado en tu runtime **es** código ejecutable descargado | *datos interpretados en un sandbox* — mejor historia |

**El spike que decide la forma (NO medido):** ¿puede el runtime, ya dentro del sandbox, **cargar un assembly IL desde su `--dir`** y correr reflection sobre él? En modo intérprete debería —es el camino que Puppeteer ya usa— pero hay que medirlo.

**Costo a mitigar:** N instancias de runtime en un teléfono es RAM real. Mitigación gratis por diseño: **el módulo no inicia nada** — sin loop propio no hay nada que mantener vivo, así que se instancia por demanda y se desaloja.

**Y una razón para el sustrato compartido que NO depende de los bytes:** hay **una sola cosa que auditar**. Si el trimming mejora el tamaño, esta razón sigue en pie.

---

## R10. Versionado del sustrato: la restricción que casi nadie ve

**El hecho.** El journal se reproduce contra el dominio actual **interpretado por el sustrato actual**. Entonces la compatibilidad que importa **no es de API**: es **determinismo de replay entre versiones del sustrato**. Si v3 ejecuta un constructo del DSL aunque sea un poco distinto de v2, replayar el journal de cinco años de Sonia produce **otro estado**.

No es hipotético: el peligro ya está documentado un piso más abajo — el journal graba **el texto** de la llamada y la sobrecarga se resuelve **otra vez** en cada replay.

**Cómo dribliarlo.** Una **suite de journals dorados**: journals congelados junto con el estado que deben producir, replayados contra cada candidato de sustrato. Convierte "mantenemos compatibilidad hacia atrás" de promesa en **afirmación falsable**. Barato ahora, carísimo de retrofitear.

**Decisiones que se siguen:**
- **Un sustrato, compatible hacia atrás**, con la suite como guard. Varios sustratos side-by-side = escotilla de último recurso: recrea "N motores que auditar" **dentro** del instalador y duplica RAM si dos módulos pinean distinto.
- El manifiesto declara un **rango** (`>= 2.1 < 3.0`), no una versión: si declara una versión exacta, cada actualización del Host huerfaniza todos los módulos.
- **El autor no declara compatibilidad — el sustrato se la gana.** El rango que un módulo puede pinear es el que la suite certificó.

---

## R11. Límites de background en móvil

**El hecho.** Android (WorkManager) e iOS (BGTaskScheduler) deciden cuándo corre trabajo en background, e iOS es tacaño y no negocia.

**Qué bloquea.** Sincronización predecible.

**Cómo dribliarlo.** La política de sync tiene que **tolerar** no correr cuando el sistema no quiere. Degrada frescura; no rompe la arquitectura. No inventar mecanismos para forzarlo.

---

## R12. Política de tiendas — la única dependencia real de los dueños del SO

**El hecho.** Una app que descarga y ejecuta código en runtime es lo que las tiendas miran con más lupa.

**Qué bloquea.** Potencialmente el canal de distribución desde la página de la institución, que es pieza central del relato — **aunque la arquitectura funcione**.

**Cómo dribliarlo.** Es la razón no-técnica que favorece la variante wasm de R9: un módulo interpretado por un runtime es *datos interpretados en un sandbox*, más cerca de contenido que de código.

**⚠️ Sin verificar contra la política vigente. Cambia seguido. Verificar antes de apoyarse en esto.**

**Formulación honesta del claim del proyecto:**

> **La Arquitectura Gnothi no requiere que nadie le dé permiso. Lo que depende de otros es la distribución, y eso es política, no arquitectura.**

---

## R13. 🚫 ChatGPT no puede ser la Puerta 1: sus conectores solo llegan a servidores remotos

**El hecho (2026-09-09).** Los conectores de ChatGPT —Apps & Connectors, Developer Mode, Apps SDK— reciben **una URL de un servidor MCP remoto**, por SSE o Streamable HTTP, con OAuth o sin auth. Quien se conecta a esa URL **son los servidores de OpenAI**, no la app de escritorio. La app de ChatGPT para macOS no lanza procesos ni habla stdio, y una URL `localhost` la rechaza con «Unsafe URL». Todos los tutoriales de «MCP local con ChatGPT» pasan por un túnel público (ngrok, cloudflared) o un relay de terceros.

Gnothi es lo contrario: **un servidor MCP local por stdio**, sin puerto, sin URL, sin auth, que el asistente lanza como proceso hijo. Es lo que Claude Desktop y Claude Code saben consumir, y por eso la Puerta 1 en Mac se cerró en una tarde (medido: 48 tools, cruces examenes×compras y cargas×concesionario).

**Qué bloquea.** El pitch «conectá Gnothi a tu asistente» dicho en genérico. Hoy vale para los asistentes que lanzan servidores MCP locales; ChatGPT no es uno de ellos.

**Es el gemelo de escritorio de R3.** Allá la app móvil del asistente no puede lanzar un proceso local; acá la app de escritorio de ChatGPT tampoco. La diferencia es que en escritorio **sí existe** un asistente de terceros que lo hace, y esa excepción es la que sostiene la Puerta 1.

**Lo que haría falta, y qué le hace a la premisa.** Un transporte HTTP en el Host (el SDK lo trae; no toca el despacho, igual que el `.mcpb` y el MSI no lo tocaron), una URL pública hacia la máquina de la persona, y OAuth delante. Lo primero es acotado. Lo segundo y lo tercero son el problema, y **no es el que parece**: que la respuesta de cada tool viaje al modelo ya pasa con Claude Desktop, y está dicho en `00` y `05` («nunca se transfiere la historia completa»). Lo que ChatGPT rompe es otra cosa:

| | Claude Desktop (stdio local) | ChatGPT (MCP remoto) |
|---|---|---|
| **quién inicia** | la app de la persona lanza el Host cuando lo necesita | los servidores de OpenAI llaman a la máquina de la persona |
| **superficie expuesta** | ninguna: no hay puerto ni URL | una URL pública hacia el Host, viva mientras se quiera usar ChatGPT |
| **terceros en el camino** | ninguno | el túnel o relay, que ve pasar cada pregunta y cada respuesta |
| **quién opera** | nadie: no hay servicio | alguien mantiene un servicio con auth, por persona |

Y una de producto: «tu laboratorio y tu súper solo se juntan en tu computadora» deja de poder decirse con un relay en medio, aunque el relay no guarde nada. Es la misma razón por la que R3 cierra el camino B.

**Cómo lo dribleamos.** No se driblea: se dice. Es una restricción de OpenAI, documentada, y no nuestra. El Host es compatible por protocolo con cualquier cliente MCP; el día que ChatGPT lance servidores locales, entra sin cambiar el despacho.

🚫 **No construir un relay propio.** Sería construir la pieza que Gnothi existe para no tener: un servidor de ncubo en el camino de los datos de la persona. Un spike de compatibilidad —HTTP en el Host más ngrok más Developer Mode, una tarde— vale solo como evidencia de «habla con cualquier cliente MCP», y no se entrega a personas.

**Formulación honesta para el pitch:**

> **Gnothi funciona hoy con Claude Desktop y Claude Code, y con cualquier asistente que lance servidores MCP locales. Con ChatGPT, cuando OpenAI lo permita: la razón es de ellos.**

Fuentes: OpenAI, *Developer mode* (`developers.openai.com/api/docs/guides/developer-mode`: «Supported MCP protocols: SSE and streaming HTTP», «remote MCP server»); el rechazo «Unsafe URL» a `localhost` reportado en dev.to (`how-can-i-connect-a-local-mcp-server-python-to-chatgpt-unsafe-url-issue`); Claude Desktop, *Getting started with local MCP servers*.

---

## El patrón que se repite

Vale nombrarlo porque predice dónde van a aparecer los próximos problemas:

> **Cada cosa que wasm rompió fue una fuga de infraestructura del motor** — primero el framework de servidor, después los hilos bloqueantes — **nunca del dominio.** En el precedente medido, el proyecto de dominio no tiene ni una dependencia y no se le tocó una línea.

wasm funcionó como **detector de porosidad falsable**: el compilador y el runtime trazan juntos el borde. Si el dominio de ustedes necesita cambios para correr confinado, **eso es el hallazgo** — anótenlo.
