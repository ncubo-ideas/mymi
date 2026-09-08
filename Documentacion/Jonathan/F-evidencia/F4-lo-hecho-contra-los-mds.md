# F4 · Lo que se hizo, contra lo que dicen los MDs

Pieza por pieza, con la cita. Y separado en cuatro categorías, porque no todo tiene el
mismo respaldo:

| | |
|---|---|
| ✅ **respaldado** | el MD lo pide y se hizo así |
| ⚖️ **elegido** | el MD ofrece dos caminos y se tomó uno |
| ⚠️ **contradice** | el MD dice otra cosa y se hizo distinto a propósito |
| ➕ **sin respaldo** | no está en ningún MD |

**Corte: 2026-08-26.** Las citas son de los ocho MDs del arquitecto en la raíz del repo.

---

## ✅ Respaldado

### El módulo sin motor ni runtime

> `R8`: *«**un solo Host, módulos puros sin runtime.** Se conserva casi todo el relato: el
> módulo se sigue bajando de la página de la institución, sigue sin pasar por tiendas. Lo
> que cambia es que **no trae motor**.»*
>
> `01`, pieza 2: *«El runtime .NET y el motor se instalan **una vez**, con el Host. Los
> módulos pesan decenas o centenas de KB porque no traen motor.»*

**Hecho.** El paquete pasó de 6.7 MB a 16.9 KB. El mecanismo es `ExcludeAssets="runtime"`,
que es literalmente lo que pidió el arquitecto en la reunión: *«que la compile pero que no tiene
que poner el Puppeteer en su aplicación… Gnothi va a usar el que tiene él»*.

### El módulo sin transporte

> `R4` 🚫: *«**stdio/ssh entre el Host y el módulo: descartado.** Si el módulo implementa un
> aparato ssh, **el módulo tiene sockets**. Se cae "ninguno de ellos puede salir por su
> cuenta".»*
>
> `R4`: *«Lo correcto: el módulo **no tiene transporte**. Expone su superficie; el Host
> elige la cáscara.»*

**Hecho.** El módulo no tiene servidor MCP ni ninguna librería de protocolo. El Host lo
carga y lo invoca.

### El Host como cartero

> `01`, invariante 2: *«El Host no escribe el journal. Trae bytes; el módulo asienta. **El
> Host es cartero, no escriba: no puede abrir el sobre ni dictar la carta.**»*
>
> `06`, Fase 3: *«El Host hace el pull del feed del publicador y le pasa el payload al
> módulo **como parámetro**. El módulo asienta.»*

**Hecho, y la garantía es estructural.** El bucle del sustrato pide tres piezas y están
repartidas: la red la pone el Host, la ruta y el diario los pone el módulo. El Host no
tiene ninguna llamada con la que escribir una entrada.

### El endpoint fijo, registrado en la instalación

> `02`, autoridad 2: *«Endpoint **fijo por publicador**, registrado en la instalación, **no
> elegible en runtime** (si el módulo pudiera decir qué URL traer, exfiltra codificando
> datos en la ruta o el query).»*

**Hecho.** El manifiesto lo declara, el Host lo copia a `instalacion.json` al instalar, y
de ahí manda esa copia. El `HttpClient` tiene el endpoint fijado en su `BaseAddress`.

### El token fuera del journal

> `02`: *«Cursores, tokens y reintentos son del Host y **no son historia de Sonia**. Que un
> pull haya fallado tres veces no es un hecho de su vida. Almacenamiento del Host,
> separado.»*

**Hecho, y medido.** El journal pasó de tres acciones definidas a una: el device key, el
access token y el refresh token salieron. Antes se veían en claro.

### El interruptor, y que el módulo no se entere

> `02`, autoridad 1: *«Apagar = el Host omite las tools de ese módulo de la lista que le
> entrega al agente, y **el módulo no se entera** (no puede distinguir "me apagaron" de
> "nadie preguntó").»*

**Hecho.** Y dicho en la ventana, donde se aprieta el interruptor.

### El tamaño medido desde afuera

> `06`, Fase 4: *«El tamaño lo **mide el Host desde afuera** (`stat` sobre el directorio que
> él concedió). Un módulo que reporte un tamaño falso no puede existir.»*

**Hecho.** `BytesEnDisco()` hace `stat` sobre la carpeta concedida. Nadie le pregunta al
módulo.

### La lista de módulos con su interruptor

> `06`, Fase 4 — *«Lo que Sonia ve»*: *«Lista de módulos con tamaño y tasa, On/Off por
> módulo…»*
>
> `02`: *«Lo que queda del Host es deliberadamente tonto: red, calendarización, ruteo de
> mensajes, **renderizado de UI declarativa**, política y medición.»*

**Hecho.** Es la tuerquita de la analogía del arquitecto.

### El view declarativo

> `03`: *«view declarativa ← **el módulo describe; el Host renderiza**»*
>
> `07` §2: *«el módulo **no ejecuta interfaz**»*

**Hecho.** El `.gmod` tiene cero componentes: un `view.json` de 4.5 KB. El renderer es del
Host.

### El view no se filtra por las tools apagadas

> `03`: *«**View ⟂ Repertorio**… las dos mitades las juzgan **jueces distintos**. La UI la
> juzga Sonia; el repertorio lo juzga el agente. **No compiten.**»*

**Hecho, después de corregirlo.** La primera versión filtraba la pantalla por las tools
apagadas — o sea, apagar algo para el asistente también se lo apagaba a ella. Está mal:
esos interruptores son para el asistente.

---

## ⚖️ Elegido entre lo que el MD ofrece

### La cáscara en-proceso, y no wasm

`R9` pone dos variantes en tabla y las dos son legítimas:

| | IL en el proceso del Host | dominio dentro de una instancia wasm |
|---|---|---|
| aislamiento | **ninguno** | **duro** |
| ancla de confianza | firma + reputación | **incapacidad** |
| estado | **«funciona hoy»** | **«spike sin medir»** |

**Se eligió la izquierda.** Es la que `R9` marca como *«funciona hoy»*, y se está pagando
exactamente el precio que cotiza: *«ninguno — mismo heap; `AssemblyLoadContext` **no es**
frontera de seguridad»*.

⚠️ **Con un matiz que hay que decir:** `R1` describe el escritorio como *«stdio con un
runtime wasm en escritorio, llamada en proceso en móvil»*. O sea que en-proceso es lo que
`R1` pone para **móvil**. Se usó en escritorio porque es la columna de `R9` que funciona
hoy, no porque `R1` lo indique.

### La ventana propia, y no el chat

`07` §2 deja la pregunta abierta e **inclina** hacia el chat:

> *«Vale mirar qué hay en el ecosistema de asistentes para UI dentro de la conversación
> antes de inventar un lenguaje propio: **si la UI puede renderizarse dentro del chat** en
> vez de vivir en una app aparte, encaja bien con lo que Sonia realmente quiere hacer.»*

**Se eligió la ventana.** Es una decisión del equipo, no una lectura del documento. La
razón: el chat no da interactividad —fila clickeable, navegación— y la Fase 4 pide una
pantalla de administración que en el chat queda rara.

Y dejó de ser «se probó»: la ventana es **la cara de escritorio que se entrega** —
`Gnothi.Desktop`, Blazor Hybrid, que corre las consultas llamando al Dispatcher **directo**
(sin wasm, sin puerto, sin http)— y viaja **dentro del MSI** de Gnothi. Ver `C7`. El renderer
de markdown para el chat también existe y funciona, pero la cara que se instala es la
ventana.

---

## ⚠️ Contradice un MD, a propósito

### El cursor se queda en el journal

> `06`, Fase 3, criterio: *«Cursores, tokens y reintentos viven en almacenamiento del Host,
> **fuera del journal**.»*

**Se hizo distinto: el token salió, el cursor se quedó.** Y creemos que el criterio está
mal para el cursor.

La razón está en el propio motor, en el comentario de `BucleDeRefresh`:

> *«Ambos salen del journal. El "desde" **NO** se guarda entre llamadas en un campo: si la
> app se cayó a mitad del bloque anterior, **el journal es el único que sabe la verdad**.»*

El checkpoint commitea en el **mismo comando** que el hecho —`bitacora.Avanzar(evento)`
adentro del insert—, así que un corte entre *«el módulo asentó»* y *«el Host guardó el
cursor»* **duplicaría al reintentar**. Un resume exacto exige que cursor y hecho sean un
solo commit.

**Para el token el criterio sí aplica** y se cumplió: no tiene requisito de atomicidad con
los hechos.

### El orden del plan

> `06`: *«**Un spike bloqueante primero, lo demás después.** Construir sobre ellas sin medir
> es **apostar el calendario**.»*

**Se hicieron las Fases 1 a 4 antes que la Fase 0.** Eso es exactamente lo que el plan dice
que no se haga. Se corrigió al final del día corriendo S1, que dio positivo — así que la
apuesta salió bien, pero fue una apuesta.

---

## ➕ Sin respaldo en ningún MD

Esto no contradice nada: son huecos que `07` deja abiertos o cosas que simplemente no están
escritas. Van como **aporte**, no como cumplimiento.

### El vocabulario del view

`07` §2 dice *«qué lenguaje, **sin decidir**. Latitud grande»* e invita a traer opinión.

**Aporte:** dos tipos de bloque (`resumen`, `tabla`), cuatro formatos (`texto`, `fecha`,
`entero`, `plata`), direccionamiento con puntos, y **un** concepto de navegación
(`alTocarFila` + `parametro` de pantalla).

Y no se inventó: **salió de medir la pantalla Blazor que ya existía.** De sus 274 líneas,
solo ~40 eran view del módulo; las otras 234 eran plomería del Host.

### Los atributos `[ModuleSurface]` y `[Query]`

No están en ningún MD. Lo más cercano es el instinto del arquitecto en la reunión: *«va con
bibliotecas del motor, en Choreography o a la par»*.

La etiqueta mínima es `[ModuleSurface]` sobre la clase y `[Query("nombre")]` sobre cada
verbo — sin dependencias, en un `Gnothi.Contrato` aparte, que se puso ahí **solo** porque no
se toca el repo del motor. Su lugar es el sustrato, y ese día el módulo deja de referenciar
nada de Gnothi.

Y el reconocimiento es **por nombre**: `InProcessShell` no ata contra el tipo del atributo,
compara contra dos tablas —`ClassMarks` y `ToolMarks`—, y por eso el **mismo código**
reconoce los módulos cargo y los que traían los atributos viejos de MCP.

### Los dos instaladores MSI

`02` lista *«instala, actualiza, habilita/deshabilita, desinstala»* como autoridad 1, pero
no dice cómo. Lo que se hizo son **dos MSI** (WiX v5, per-usuario, sin admin), y es un aporte
grande, no un detalle:

- **El de Gnothi** trae el Host y la ventana `Gnothi.Desktop`, corre un wizard y deja la
  **marca de descubrimiento** en `HKCU\Software\Ncubo\Gnothi`.
- **El del comercio** lee esa marca, **bloquea si Gnothi no está**, instala el PWA, la
  `Comercio.Shell` y `compras.gmod`, corre `--install-module` por custom action, y al
  desinstalar **quita el módulo**.

El paso a paso —la marca, las caras de escritorio, las custom actions— está en `C7`; las
trampas de WiX y `msiexec`, en `E1`.

### `gnothi_traer_lo_nuevo` como tool del asistente

**Este es el más discutible de todos**, y conviene señalarlo.

Los MDs dicen que el Host hace el pull (función 11) y que *«puede refrescar aunque el
módulo esté cerrado»* (función 12). **Ninguno dice que el asistente pueda dispararlo.**

Al exponerlo como tool, **se le dio al modelo una forma de causar tráfico de red**. Está
acotado —el endpoint es fijo, el módulo apagado no se refresca, y queda en el contador—
pero es una decisión nuestra sin respaldo, y hay un argumento para que no exista: la
alternativa es que solo la persona lo dispare, desde la ventana.

### El `.gmod` como formato

`07` §1 dice del manifiesto: *«Sabemos qué campos lleva… **No está escrito**»*. La extensión
y la forma del paquete tampoco. Lo que se hizo es una propuesta.

---

## El resumen honesto

**Nueve piezas respaldadas con cita**, dos elecciones legítimas entre opciones que los MDs
ofrecen, **dos contradicciones deliberadas** —una con argumento fuerte (el cursor), otra que
fue un error de orden (la Fase 0 al final)— y **cinco aportes** en terreno que `07` declara
abierto.

Lo que **no** se puede afirmar de nada de esto: que demuestre confinamiento. La cáscara
en-proceso no aísla, y `R9` lo dice. Eso lo demuestra el spike S1, que es otra cosa y está
en `F3`.
