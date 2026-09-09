# Gnothi.Host — el proxy

La IA de la persona habla con **un** interlocutor y ve las consultas de **todos** sus
módulos. El Host es lo que hace eso posible: un servidor MCP hacia el asistente que junta
N catálogos en una sola lista. Hacia cada módulo baja por una **cáscara**, y hay dos: un
cliente MCP sobre stdio, o una llamada a función por reflexión.

**Lo que resuelve, dicho de una vez:** ni el supermercado sabe el colesterol de la persona
ni el laboratorio sabe qué come. Los dos hechos solo se juntan en su computadora, y esto es
lo que los junta.

Antes de esto había un servidor MCP por negocio, cada uno declarado a mano en el
`.mcp.json`. Eso funcionaba y no agregaba: la IA veía dos proveedores separados, y la
persona no tenía dónde apagar una consulta.

---

## El código

`net9.0`, 1 391 líneas, dos paquetes. **No referencia Puppeteer** — y no tenerlo es cómo el
compilador garantiza que no se colara semántica de dominio.

| Archivo | Qué es |
|---|---|
| `Program.cs` | El arranque y la **Puerta 1**. Traduce y delega, nada más |
| `ToolBridge.cs` | Contesta `tools/list` y `tools/call`. El `inputSchema` sale del manifiesto |
| `Dispatcher.cs` | El núcleo: `Catalog()` y `Serve()`. **No sabe que existe MCP** |
| `ToolIndex.cs` | El índice de tools publicadas, **compartido por las dos Puertas** |
| `IShell.cs` | La costura hacia el módulo. **No nombra un solo tipo de MCP** |
| `StdioShell.cs` | Cáscara por proceso hijo con MCP sobre stdio |
| `InProcessShell.cs` | Cáscara por reflexión: el dll cargado en su propio contexto, sin proceso |
| `ModuleCatalog.cs` | Descubrimiento y recarga en caliente del consentimiento |
| `Module.cs` | La unidad de aislamiento: lo declarado + lo concedido + su cáscara |
| `Manifest.cs` · `Installation.cs` | Los dos archivos, las dos autoridades |
| `HostTools.cs` | Las tools `gnothi_*`: hablan de los módulos, no de los datos |
| `ConsentWatcher.cs` | Mira el archivo de la persona y manda `list_changed` |

### La dirección de las flechas es la propiedad

```
DOS ENTRADAS, un solo nucleo:

  Puerta 1   asistente externo ──MCP/MCPB──▶ Program → ToolBridge ──┐
                                                                    ├──▶ Dispatcher ──▶ Module
  Puerta 2   nuestro bucle propio ─────────▶ Gnothi.Assistant ──────┘

UNA SALIDA, dos cascaras, elegida por modulo en su instalacion.json:

  Module ──┬──▶ StdioShell ─────▶ proceso hijo ──MCP/stdio──▶ modulo
           └──▶ InProcessShell ──▶ llamada a funcion ────────▶ modulo
```

> **Ojo con «Puerta 2»: no es esto.** En el vocabulario del proyecto las dos Puertas son
> **entradas al Host, desde arriba**. La **Puerta 1** es un asistente externo pidiéndonos una
> tool call —lo que hay hoy—. La **Puerta 2** es *nuestro propio bucle de agente*, que existe
> solo porque la app de un asistente ajeno todavía no puede llamarnos en móvil, y está
> diseñada para poder borrarse. **Ya existe**, en `Gnothi.Assistant`, con un test que falla si
> llega a nombrar algo interno del Host. La salida hacia los módulos no es una Puerta: es la
> **cáscara**.

> **Y un hueco que este diagrama cerró a medias:** el diseño dice que el eslabón Host ↔
> módulos es **invocación local, una llamada a función, sin transporte** — en escritorio y en
> móvil por igual. `StdioShell` pone transporte ahí: un proceso hijo, y el módulo cargando su
> propio SDK de MCP de 1,3 MB. `InProcessShell` es el camino que el diseño pide, medido con
> los mismos resultados sobre los tres módulos del POC. **Falta que sea el único**: hoy stdio
> sigue siendo el valor por omisión, y lo que la cáscara en proceso NO contiene está medido
> en `NotasGnothiHost/10`.

Nunca al revés y nunca de costado. `Dispatcher` no sabe que existe MCP, ni un asistente, ni
un bucle de agente: recibe una `Request` y devuelve una `Response`.

Eso no es prolijidad. El día que exista una segunda entrada —en este proyecto va a ser el
bucle de agente propio para móvil— tiene que hacer **lo mismo**. Si el catálogo, los
permisos y las cuotas vivieran adentro de la clase que habla MCP, la segunda entrada obliga
a duplicarlos; y peor, no se podría **borrar** la primera entrada el día que sobre, porque
se llevaría los módulos con ella.

---

## Agregar un módulo son dos json

Cero código, cero recompilación del Host. Un directorio bajo `Modulos/`:

```
Modulos/
  examenes/
    manifest.json     <- lo escribe la INSTITUCIÓN: quién soy, qué necesito, qué ofrezco
    installation.json    <- lo escribe el HOST: dónde puse tu binario, qué carpeta te
                           concedí, y si estás encendido
```

Que sean dos archivos no es cosmético: **son dos autoridades distintas**. Si la cáscara y la
carpeta vinieran en el manifiesto, el módulo elegiría cómo se lo ejecuta y dónde escribe.
Conceder es un acto del que concede.

### El paquete se inspecciona antes de instalarse

Hoy un módulo llega como paquete —`<modulo>.gmod` en escritorio, el bundle que baja el
teléfono— y **antes de escribir un byte** pasa por `PackageInspector` (`PackageInspector.cs`),
la misma inspección en los dos instaladores. Distingue las dos formas —**cargo** (declara
`load.entry`, dos DLLs, el Host presta el motor) y **hospedado** (trae la app publicada en
`web/`, con su motor adentro porque un navegador no corre C# sin él)— y por eso lo que rechaza
depende de la forma: un cargo que trae el motor se rechaza; un hospedado lo trae y se **cuenta**.
Rechaza en las dos: ejecutables, un `installation.json` (ese lo escribe el Host), un journal de
muestra, rutas que salen de su carpeta, un nombre que no es identificador. Y reporta lo que la
persona tiene derecho a ver antes de aceptar: la forma, cuánto pesa, si trae motor, y qué
capacidades pide de más —con eso el módulo se instala **apagado**.

Lo que **no** hace es autenticar: dice qué trae el paquete, no quién lo respalda. Eso es el
pendiente 7.

**Medido:** el módulo de exámenes entró así, con sus tres tools, sin tocar una línea de
`Gnothi.Host` ni de `Examenes.Mcp`.

El manifiesto declara cada tool con su nombre, título, descripción, si es de solo lectura y
sus parámetros. Un manifiesto ilegible **se salta con aviso por stderr** y no tumba el
Host: uno que no arranca porque una institución publicó un json torcido le deja a un
tercero la disponibilidad de todos los demás módulos.

### Verificar que no miente

```
gnothi_verificar_modulo(examenes)
  declaradas: evaluar_tendencia, revisar_mis_analitos, velocidad_de
  reales:     evaluar_tendencia, revisar_mis_analitos, velocidad_de
  coincide:   true
```

Compara el manifiesto contra lo que el módulo **realmente** expone al ser consultado.
Cuando se declaró exámenes, esta tool encontró la primera versión del manifiesto con
`velocidad_de` sin declarar. Sirve.

---

## Correrlo

En el `.mcp.json` de la raíz, un solo servidor:

```json
"gnothi": {
  "command": "dotnet",
  "args": ["Gnothi.Host/bin/Debug/net9.0/Gnothi.Host.dll", "Modulos"]
}
```

Las dos rutas son relativas al repo, así que **ese archivo no cambia de máquina en
máquina**. Lo que sí es de cada máquina —dónde vive el binario de un módulo y qué carpeta se
le concedió— vive en `Modulos/<modulo>/installation.json`.

> **Con transporte stdio, stdout ES el canal JSON-RPC.** `Program.cs` desvía `Console.Out` a
> stderr **antes** de tocar cualquier otra cosa. Y no usar `dotnet run`: ese escribe a
> stdout y corrompe el protocolo. `dotnet <dll>` no.

El Host no levanta ningún módulo al arrancar: el catálogo sale de leer manifiestos. **Veinte
módulos son veinte json, no veinte replays de journal.** Cada módulo se levanta cuando se lo
llama y se desaloja al terminar.

---

## Los cuatro verbos, y cuáles son de verdad

| Verbo | Estado medido |
|---|---|
| **Ejecuta** | ✅ levanta el módulo, llama, desaloja. `levantado: false` incluso con `consultasDelAsistente: 2` |
| **Mide** | ✅ cuenta consultas por `modulo/tool`, y mide el tamaño del registro **desde afuera** con `stat` |
| **Autoriza** | 🟡 a medias: corta por **operación**, no por **hecho** |
| **Aísla** | ❌ todavía no existe |

Medir desde afuera no es un detalle de implementación: un módulo que reporta 4 MB mientras
ocupa 400 no puede existir si nadie le pregunta a él.

### `Aísla` no está, y conviene no decir que sí

El módulo corre como proceso hijo normal, con los mismos privilegios del sistema operativo
que el Host: disco completo, red. La `grantedFolder` es una **convención** que se pasa
como argumento — nada la hace cumplir. **Medido** con un módulo de prueba: devuelve la
carpeta concedida, y podría haber leído cualquier otra cosa del disco sin que nadie lo pare.

**Y con `InProcessShell` es peor, no igual:** ahí el módulo corre en el **mismo proceso** que
el Host, así que además del disco comparte memoria y puede tumbar al Host con una excepción.
Lo medido está en `NotasGnothiHost/10`: un módulo adversario contenido 5 de 5 veces por
stdio, 2 de 5 en proceso. La cáscara en proceso paga `Aísla` para cobrar la llamada directa
que el diseño pide; las dos deudas se saldan con la misma pieza, la cáscara wasm.

El aislamiento duro —memoria lineal propia, `--dir`— llega con la Etapa 2, y cambia **solo**
la cáscara: ni el manifiesto ni el módulo se enteran. Eso es lo que se quiere demostrar, un
artefacto y dos hospedajes. Este POC demuestra el eslabón de **agregación**, que es otra
cosa.

---

## El interruptor de la persona

`installation.json` tiene los dos cortes:

```json
"encendido": true,
"toolsApagadas": ["velocidad_de"]
```

El grueso apaga el módulo entero; el fino apaga una consulta. **Medido:** con `velocidad_de`
apagada el catálogo baja de 6 a 5 tools, las otras dos del mismo módulo siguen andando, y si
el asistente adivina el nombre igual recibe:

```json
{"error": {"code": -32603, "message": "Unknown tool: 'examenes_velocidad_de'"}}
```

Fijarse en qué **no** dice: no dice "no tenés permiso". Dice que esa tool no existe. La
decisión de la persona no se filtra, y el módulo tampoco se entera — no puede distinguir
"me apagaron" de "nadie preguntó". Esa indistinguibilidad es la propiedad, no un efecto
lateral.

### Y tiene efecto AHORA

Antes el consentimiento se leía una sola vez, al arrancar: la persona apagaba una consulta
en medio de una conversación y el asistente seguía viendo la lista vieja hasta que alguien
cerraba el Host. Un interruptor que hay que reiniciar para que sirva no es un interruptor,
es un archivo de configuración.

`ConsentWatcher` mira los `installation.json` y manda
`notifications/tools/list_changed`. **No manda el catálogo**: es un golpecito en el hombro,
y el cliente vuelve a pedir `tools/list`, que lo contesta el mismo `ToolBridge` sobre el
mismo `Dispatcher`. Una sola fuente de verdad.

**Medido de punta a punta**, dentro de una conversación abierta y sin reiniciar nada:
`capabilities.tools.listChanged=true` en el handshake, apagar `velocidad_de` la hace
desaparecer de la lista del asistente, encenderla la devuelve, y sale **una** notificación
por cambio real — no una por evento del sistema de archivos.

Tres decisiones de esa pieza que no son obvias:

**Solo recarga el consentimiento.** `Module.Reconsent(bool, IReadOnlyList<string>)` no
acepta una `Installation` entera **a propósito**: hace imposible por construcción que un
cambio de cáscara o de carpeta se cuele por la recarga y le mueva el piso a un módulo con
una llamada en vuelo. Si esos campos cambian, el Host lo dice y pide reinicio.

**Un `installation.json` ilegible apaga el módulo.** Es un archivo de consentimiento: cuando
no se puede saber qué autorizó la persona, la respuesta no es suponer que autorizó. Cuesta
un parpadeo si alguien lo pesca a medio escribir, y ese es el precio correcto.

**Vigila el archivo de la persona, no el manifiesto.** El manifiesto es de la institución y
no cambia mientras el Host corre; si cambiara, cambiaría lo que el módulo **ofrece**, y eso
es una reinstalación, no un cambio de opinión.

---

## Lo que el Host NO filtra

Vale decirlo antes de mostrarlo, porque "pasa todo por el proxy" se entiende de más:

**No mira el contenido de la respuesta.** Lo que devuelve el módulo se reenvía entero,
`_meta` incluido. Es un proxy de transporte y de autorización, no un filtro de contenido.

**El corte es la operación, no el dato.** Y esto muerde de verdad: apagar `velocidad_de` no
esconde el antígeno prostático, porque `revisar_mis_analitos` devuelve los tres analitos en
una sola respuesta. Para taparlo habría que apagar esa también, perdiendo el colesterol y la
hemoglobina, que a la persona no le molestaba compartir. Consentimiento por tool ≠
consentimiento por hecho, **medido**.

No hay salida obvia. Que el módulo declare tools más finas explota combinatoriamente y le da
al autor del módulo el poder de decidir la granularidad del consentimiento de la persona.
Que el Host filtre la respuesta exige que **entienda la semántica del módulo**, y eso rompe
la separación de autoridades: la semántica es del módulo, la interacción es del Host.

**Hay dos sistemas de permisos que no se hablan.** El módulo de exámenes tiene su propia
compuerta interna (`PermisosDeLaIA.EstaPermitida`) de la que el Host no sabe nada. Si esa
compuerta dice no, el Host devuelve un error genérico y no puede explicar por qué.

**El manifiesto no sabe declarar un enum.** `Examenes.Mcp` publica su parámetro como enum,
que es un contrato en el `inputSchema`. Al pasar por el manifiesto se vuelve `string` y los
valores válidos terminan en la descripción, que es prosa que la IA tiene que leer bien —
exactamente lo que el módulo había evitado. Tampoco sabe expresar parámetros opcionales: hoy
todos se publican requeridos.

---

## Pendientes

| # | Pendiente |
|---|---|
| 1 | **El manifiesto necesita expresar el `inputSchema` crudo**, para no perder enums ni opcionales. Se decide con Álvaro; es formato de manifiesto, no código del Host |
| 2 | **Consentimiento por hecho.** El corte por operación deja pasar hechos por la puerta de al lado, y peor: puede **invertir una conclusión**. Con `velocidad_de` apagada, promediar la serie entera da 0,73/año —bajo el umbral de 0,75— cuando el tramo reciente es 2,20/año |
| 3 | ~~**Reconciliar los dos sistemas de permisos**~~ **RESUELTO eliminando uno:** Renán sacó la compuerta interna del módulo (*"no son de esta app"*). El consentimiento es del Host |
| 4 | **`Aísla`.** La cáscara wasm con `--dir`. Es el spike bloqueante de la Etapa 2 |
| 5 | **Costo de arranque en frío.** Levantar, hacer replay del journal y desalojar se paga en CADA llamada, porque el módulo se desaloja al terminar. **Medido: 378 ms** por llamada sobre stdio con el journal del POC — no "unos segundos", como decía este README antes de medirlo. Falta el número de la cáscara en proceso y con journals grandes |
| 6 | **El catálogo se paga completo en cada pregunta.** Medido por la Puerta 2: con 8 tools son ~1 163 tokens, el **86%** del costo fijo por consulta, y viaja entero aunque la pregunta use una sola tool. Filtrarlo antes de mandarlo es opinar sobre semántica, que es del módulo: se decide con Álvaro |
| 7 | **El paquete no está autenticado.** La inspección dice qué trae, no quién lo respalda: nadie firma el zip y no se verifica que quien lo sirve sea quien el manifiesto dice ser. Falta atar el bundle a su origen (un `.well-known` en el dominio de la institución, y el manifiesto declarando ese origen) y, después, la firma |
