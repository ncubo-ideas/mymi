# 05 — Medido vs supuesto, y los claims sobrevendidos

## Por qué este documento existe

El proyecto se apoya en unos pocos claims verificables. **Un claim inflado que se cae se lleva puestos a los que sí aguantan.** Así que la disciplina es: medir callado y falsable, no proclamar. Si algo no está en la columna izquierda, decirlo como hipótesis.

## La tabla

| ✅ MEDIDO (se verificó ejecutando) | ❓ SUPUESTO / SIN MEDIR |
|---|---|
| El motor completo de Puppeteer —intérprete DSL, descubrimiento por reflection, journal— **corre en WebAssembly dentro del browser** | Que corra igual bajo **WASI + runtime fuera del browser** (wasmtime/WAMR con `--dir`). Es **otro camino**, no medido |
| Un actor hidratado sobre un dominio real en **29 ms**, single-thread, en el browser | **Cold start con historia de 5–10 años.** Es la métrica que falta y la más fácil de instrumentar ahora |
| **Cero cambios al dominio** al cruzar a wasm: el proyecto de dominio del lab de prueba no tiene ni una dependencia y no se le tocó una línea | Que se sostenga en dominios con más superficie |
| El mismo actor y los mismos performs corren detrás de REST, de una CLI y de wasm: solo cambian carátula y formateador | — |
| Cada rotura de wasm fue **fuga de infraestructura del motor**, nunca del dominio | — |
| **Snapshots existen en el core con backend FileSystem, sin base de datos:** `EventMaterializationStorageFileSystem`, `EventElisionStorageFileSystem`, `MaterializationCheckpointStorageFileSystem` | Que el camino que use el módulo no tropiece con los blocking waits remanentes del motor (~448, ~733, ~2276) |
| **`PlaybillStoreFileSystem` está en el core** ⇒ la procedencia funciona en dispositivo sin DB | — |
| El **núcleo no lleva driver de base de datos ni host web**: `Puppeteer.csproj` tiene 2 `PackageReference` (un analizador + `System.IO.Hashing`) | — |
| El **guard de arquitectura existe** y lee el grafo de referencias emitido (`PuppeteerCoreDependencyTests`) | — |
| Cifrado del journal: **AES-256-GCM**, nonce aleatorio 12 B por registro + tag 16 B = **28 B/registro**; orden comprimir→cifrar; modo fijado al crear el journal | Cuánto crece o baja el journal real con `compression=GZip`. **GZip no tiene umbral**: en registros chicos puede agrandar |
| Dos POCs funcionando, armados **en horas**: un módulo wasm consultado por un asistente vía MCP, y una app descargada de una web que corre fuera del browser | Que la velocidad de desarrollo sea verificable **por un tercero**. Hoy es afirmación del autor |
| Suites verdes del motor con los cambios de wasm, sin regresión | Que **la reflection sobreviva a trimming/AOT** en Release. **Spike bloqueante, no medido** |
| Tamaños (Debug, sin trimming): sustrato ~1.3 MB IL / 0.44 MB gz · runtime ~7.4 MB / 2.7 gz · dominio de prueba **21 KB** | El payload en **Release trimmed después del split de adapters**. Va a ser menor; cuánto, no se sabe |
| — | Las cifras del video (**<100 B/evento**, ~100.000 hechos ≈ una foto): **tratarlas como "medición del prototipo"**, nunca como propiedad universal. Y el **repertorio pesa aparte** — no cabe en esa cifra |
| — | **EL CRUCE ENTRE MÓDULOS DE DOS AUTORES DISTINTOS.** Es el corazón de la propuesta y es lo único que sigue sin demostrarse |

**Discrepancia abierta, sin resolver:** `Puppeteer.dll` Release del nuget que usa otro proyecto mide **236 KB**; el build Debug del worktree mide **1 128 KB**. Puede ser metadata de debug o drift de versión. **No citar cifras de tamaño afuera sin un Release trimmed.**

## La restricción de integridad del demo

Escribirla en la pizarra **antes de la primera línea de código**, porque bajo presión de demo el equipo va a querer atajarla:

> **El cruce no se puede precocinar.** Si se escribe una función `calcularRendimiento()` que devuelve 9 km/l, no se probó nada: se probó que alguien sabe escribir una función. La agencia expone *rendimiento esperado*, la gasolinera expone *cargas*, **cada una ciega a la otra**, y la resta la hace el asistente.

Corolario para los datos de prueba: **se planta el hecho, no la respuesta.** Legítimo que el carro empiece a rendir menos en marzo porque algo le pasó. Ilegítimo que un módulo traiga un campo "rendimiento real".

## Claims ya sobrevendidos — corregirlos donde aparezcan

| ❌ como se dijo | ✅ la versión que aguanta |
|---|---|
| *"No podemos verlos, y cualquiera puede comprobarlo"* | El sandbox garantiza el aislamiento del módulo, pero **la garantía vale lo que vale el host**. La versión chica y buena: **no hay servidor central, no hay dónde robar** — estructural y verificable por ausencia |
| *"Nada sale del dispositivo"* | Falso: **la respuesta de cada consulta viaja al modelo**. Lo que nunca sale es **el journal completo**. Cuidar que nadie del equipo la infle |
| *"Con bases de datos no es viable"* | Se refuta en dos segundos ("dame PostgreSQL, Kubernetes y presupuesto"). La versión buena: *con bases de datos y fuerza bruta puede construirse **centralizando**; lo que no escala naturalmente es hacerlo ocho mil millones de veces **sin** centralizar* |
| *"MCP no consideró este escenario"* | Al revés: MCP nació **local-first** (transporte stdio, servidores de referencia corriendo en la máquina del usuario). Lo que no existe no es el protocolo: es **la oferta** (nadie construyó el lado institución→dispositivo) y **el dominio** (MCP transporta tools, no semántica) |
| *"El cómputo del teléfono está ocioso"* | Explica por qué es **posible**, no por qué **no se hizo**. No se hizo porque la centralización financia el producto gratis: se pelea contra un incentivo, no contra una limitación |
| *"Conectá Gnothi a tu asistente"* (en genérico) | Vale para los asistentes que **lanzan servidores MCP locales**: Claude Desktop y Claude Code (medido en Windows y Mac) y Codex (medido por Edgar). **La app de ChatGPT no**: sus conectores solo llegan a URLs remotas, y llegar ahí exige un túnel o relay que R13 de `04` cierra. Decir el nombre del asistente, no «tu asistente» |
| *"Somos los primeros"* | Los *personal data stores* llevan años de intentos fallidos (Solid, MyDex y otros). Murieron por **el lado de la oferta**, no por tecnología. El diferencial real no es la idea: es el **dominio rico sobre journal denso** |

## Cómo hablar de los dos tipos de evidencia de confinamiento

Se confunden fácil y son distintos:

- **"El motor entero cabe en el sandbox del browser"** — medido, y es evidencia de *portabilidad y limpieza del dominio*.
- **"El módulo no puede porque no se le concedió la capacidad"** — es el argumento **WASI/capabilities**, y **no está medido** en este proyecto.

En el pitch no fusionarlos: son dos evidencias con dos fuentes distintas.
