# Compras — supermercado

El negocio de Pleitez en el milestone **Gnothi**. Un supermercado le entrega a su
cliente una app con **su propio historial de compras**, para que ella y su IA lean la
**canasta completa** en vez del ticket suelto — y para que la garantía de la olla
arrocera aparezca con su respaldo, sin buscar papeles.

Sigue la convención de estructura acordada por el equipo: un directorio por negocio,
cinco nodos adentro, nombres en español. Este README es **cómo se corre**.

---

## Los cinco nodos

| Carpeta | Qué es |
|---|---|
| `PaginaDeDescarga/` | **URL1** · nginx que sirve la app publicada. `8100` http, `8463` https. **No es un csproj** |
| `EndPointDeEventos/` | **URL2** · `8101`. El GET de eventos, el token de device y su rotación, y el señuelo que devuelve 403 |
| `InyectorDeEventosDemo/` | Siembra la historia en URL2. Valores y fechas **elegidos**, no aleatorios. Dos caras: el CLI (para guionar) y **la caja** en `ui/` (para mostrar) |
| `Dominio/` | El puppet: `Historial`, `Compra`, `Item`, `Categoria`, `Garantia`, `Canasta`, `FirmaDeLaCanasta`, `CambioEnLaCanasta`, `Severidad`, `AbastecimientoPendiente`, `Bitacora`. **Cero dependencias** |
| `App/` | Una lógica y tres carátulas |
| `manifiesto.json` | Lo que **el Host** lee para armar el catálogo sin arrancar el módulo: quién es, qué sustrato necesita, qué capacidades pide y sus tools |

Dentro de `App/`:

| Proyecto | Qué es |
|---|---|
| `Compras.App` | La lógica: los `PerformCommand` y `PerformQuery`. No sabe dónde corre |
| `Compras.Wasm` | La app que instala Sonia ("La Guaria y vos"). Blazor WASM, PWA |
| `Compras.Mcp` | La superficie que ve la IA. **Solo lectura** sobre el journal |
| `Compras.Consola` | Carátula de consola, para medir el dominio y el refresh sin browser |

`EndPointDeEventos` e `InyectorDeEventosDemo` **no referencian `Dominio`** ni tocan el
journal. Es un invariante del diseño, no una omisión: el proveedor no conoce el dominio
de la persona. Lo único que los conecta es HTTP.

`Mecanismo/Puppets.Portador` es copia textual de la de Exámenes (ver su README): con dos
negocios vivos, lo que corresponde es subirlo a la raíz del repo — pendiente de acordar.

## Puertos

Distintos de los de `Facturas` y `Examenes` **a propósito**, para que los negocios
corran a la vez — las historias cruzadas lo necesitan.

| Servicio | Facturas | Exámenes | Compras |
|---|---|---|---|
| PaginaDeDescarga http | 8080 | 8090 | **8100** |
| PaginaDeDescarga https | 8443 | 8453 | **8463** |
| EndPointDeEventos | 8081 | 8091 | **8101** |
| Caja (solo Compras) | — | — | **8102** |

---

## Correrlo

### El ciclo por consola (no necesita Docker ni browser)

Es el arnés de siempre: mide el dominio y el refresh sin el sandbox del browser encima.

**1 · URL2**

```bash
dotnet run --project Compras/EndPointDeEventos
```

```bash
dotnet run --project Compras/InyectorDeEventosDemo -- listar
```

**2 · El ciclo completo, sin browser**

```bash
dotnet run --project Compras/App/Compras.Consola -- refrescar "/tmp/journal-compras" http://localhost:8101/ cliente 1
```

Tiene que terminar en **6 compras**, evento **306**, total **172,630**, la comparación
de canastas correcta y la garantía de la olla vigente (`CRITERIOS CORRECTOS : True`).

```bash
dotnet run --project Compras/App/Compras.Consola -- comprobar
```

`comprobar` corre las **324 comprobaciones del dominio puro** — sin actor, sin journal y
sin disco, así que cuando algo se rompe dice qué regla se rompió. `medir` aplica la
historia y verifica los invariantes; `estado` abre el journal y reporta (correr en
proceso nuevo: si las canastas siguen ahí, el replay rehidrató); `detalle` muestra un
ticket línea por línea; `canasta` y `comparar` son las lecturas del mes; `garantia`
busca un producto con su respaldo.

```bash
dotnet run --project Compras/App/Compras.Consola -- garantia "/tmp/journal-compras" "olla arrocera"
```

Y los dos que miran **lo que no está**:

```bash
dotnet run --project Compras/App/Compras.Consola -- pendientes "/tmp/journal-compras"
```

```bash
dotnet run --project Compras/App/Compras.Consola -- porvencer "/tmp/journal-compras"
```

### URL1: necesita Docker

```bash
dotnet publish Compras/App/Compras.Wasm -c Release -o Compras/PaginaDeDescarga/publicado -p:PublishTrimmed=false
```

```bash
cd Compras && ./PaginaDeDescarga/generar-certificado.sh && docker compose up -d
```

Y queda en **`http://localhost:8100`** — la página de descarga de la institución — con
la app colgando de **`/app/`** (la separación por el scope del service worker, medida
por Exámenes; tras republicar hay que cerrar TODAS las pestañas del origen).

### La caja: la demo en vivo

El pedido de la reunión era explícito: *"los inyectores de datos del demo también con UI
presentable, no la consola del developer… tiene que verse corriendo en la realidad, no
una filmina"*. Eso es `InyectorDeEventosDemo/ui/`: **el sistema del supermercado**, con
cara de software de operación y no de landing, porque no es para el cliente.

```bash
dotnet run --project Compras/EndPointDeEventos
```

y la caja se sirve como estático (con Docker es el servicio `caja` en el `8102`; sin
Docker, cualquier estático sobre `Compras/InyectorDeEventosDemo/ui`).

El guion de la demo, medido de punta a punta: se arma un ticket con productos del
catálogo, **Registrar compra** → *"publicada como evento 307"*, y en la app del cliente
un **Refresh** la trae. Con la compra de salmón, espinaca y leche del ejemplo cambiaron
tres cosas a la vez, y ninguna la escribió nadie a mano:

- proteínas y lácteos y huevos aparecieron entre los pasillos del mes;
- **proteínas y lácteos desaparecieron de "lo que dejó de entrar"** — el hueco se cerró
  porque acababa de entrar lo que faltaba;
- la licuadora apareció en garantías vigentes, con vencimiento en 2028.

El catálogo (`GET /catalogo`, 34 productos con precio, categoría y garantía) es **del
supermercado**, no del dominio de la persona: la caja le pregunta a su propio sistema qué
vende, en vez de llevarlo hardcodeado. Y el invariante se mantiene — la caja habla con
URL2 por HTTP y nada más: no conoce el dominio, no toca el journal y no sabe qué es una
canasta.

El CLI no se va: sirve para guionar una siembra o correrla en CI. La caja es para
mostrarlo.

## El invariante, y es falsable

**La app no es cliente de los servicios generales del supermercado.** Lo único que le
pide es su propia narrativa. URL2 tiene un señuelo que devuelve **403** con el token
*válido* de la app, porque su scope es `cliente.events.read` y nada más:

```bash
dotnet run --project Compras/InyectorDeEventosDemo -- senuelo
```

```
GET cliente/1/events?after=0    -> 200
GET cliente/1/perfil            -> 403
     {"error":"el scope del token no cubre esta operacion",
      "scopeDelToken":"cliente.events.read","scopeExigido":"cliente.profile.read"}
```

---

## El dominio

### El conocimiento viaja con los datos

Una línea de ticket sin categoría es **muda**: "HELADO-1L x3" no dice nada. La
`Categoria` (conjunto cerrado, 9 miembros) es el **departamento** del supermercado, y eso
es lo que vuelve la narrativa **analizable**. Cada uno lleva dos nombres: la **llave**,
que es la que viaja en el evento y ya está escrita en los diarios, y el **título**, que es
lo que lee una persona.

```
Categoria           título                  llave              ejemplos
├── Proteinas       proteínas               (comestible)       pollo, tilapia
├── FrutasYVerduras frutas y verduras       (comestible)       brocoli
├── LacteosYHuevos  lácteos y huevos        (comestible)       leche, huevos
├── GranosYAbarrotes granos y abarrotes     (comestible)       arroz, frijoles
├── AceitesYGrasas  aceites y grasas        (comestible)       aceite, manteca
├── Azucarados      dulces y postres        (comestible)       helado, sodas, chocolate
├── Snacks          snacks                  (comestible)       papas fritas
├── Limpieza        limpieza                NO comestible      detergente
└── Electrohogar    electrodomésticos       NO comestible      la olla arrocera (con garantía)
```

**El departamento no valora nada, y es deliberado.** Antes cada uno cargaba un *perfil de
consumo* (recomendado / neutro / ocasional) que salió de una decisión nuestra, no de
ninguna especificación. La de marca lo prohíbe con todas las letras — *"no inventar en la
app conclusiones de salud: es la línea que separa lo honesto de lo que no"* — y con razón:
el súper no sabe si la persona está en una dieta, si son cinco en la casa, ni si el helado
era de un cumpleaños. "Azucarados" es un hecho de góndola; "ocasional" era un juicio.
El cruce y el juicio son del **asistente**, con permiso de la persona. Hay una
comprobación que revienta si alguien vuelve a colgarle una etiqueta de valor al
repertorio.

Tres decisiones que ordenan lo demás (las lecciones de Exámenes y la guía de dominio
`puppeteer-domain-modeling` **v1.1.0**, aplicadas acá):

- **La llave nunca se le muestra a nadie**: `FrutasYVerduras` es lo que viene en el evento
  y no puede cambiar sin romper los diarios ya escritos; `Titulo` y `Mencion` son las dos
  formas legibles ("frutas y verduras" para una lista, "una fruta o verdura" dentro de una
  frase). La spec prohíbe la jerga de retail en lo que ve Sonia.
- **La garantía no es un `int`**: sabe desde cuándo corre (la fecha de la compra) y
  contesta si está vigente en una fecha **recibida** — el dominio no lee el reloj.
- **La ausencia tiene TIPO, no `null` (E45).** Lo que no trae cobertura porta
  `Garantia.Ninguna`; lo no comestible es el subtipo `NoComestible`; una búsqueda sin
  resultado devuelve el registro `Ausente`. Ningún campo, parámetro ni retorno del
  dominio es `Type?`. El par es siempre el mismo: una **consulta de presencia total**
  (`Existe()`, `TieneGarantia`, `Encontrada`) y un **lector guardado** que revienta si
  se lo llama sin preguntar.

### Las dos preguntas que ningún ticket contesta

**`CanastaDe(anio, mes)` / `CompararCanastas(...)`** — la canasta se suma **departamento
por departamento**, con lo comestible y lo no comestible separados para que una olla
arrocera de ₡45.900 no se coma la lectura de la comida. Y la comparación no da un
veredicto: da **de dónde sale la diferencia**, que es la pregunta S14 de la wiki
(*"¿en qué se me va la plata en el súper?"*). Con el guion sembrado:

```
en 2026-08 lleva ₡32.600 y en 2025-08 fueron ₡32.910;
la diferencia se explica sobre todo por dulces y postres (que no aparecía el otro mes);
en cambio proteínas y lácteos y huevos bajaron
```

Un pasillo entra a esa frase solo si mueve al menos **un décimo** del movimiento total
(`ParteDelMovimientoQueCuenta`); menos que eso es ruido de despensa y ensucia la lectura.

Es la mitad "compras" del **cruce preplanificado** del milestone: el laboratorio
siembra colesterol 200 (ago-2025) → 280 (ago-2026), y "el colesterol alto es por culpa
del helado" lo arma el **asistente**, que es el único que ve los dos negocios. Los
módulos no se hablan.

**`BuscarGarantia(texto, fechaDeConsulta)`** — la respuesta a *"¿cuándo compré la olla
arrocera y hasta cuándo tiene garantía?"*: compra T-93872, 2026-05-10, 12 meses, vence
2027-05-10. Total: lo que no está o no tiene garantía devuelve el registro **`Ausente`**
(`Encontrada=false`), no una excepción. Si se recompró, gana la compra más reciente.

### La app señala y se calla

El dominio **redacta la lectura pero no la juzga**, y las dos mitades de esa frase
importan.

Redacta: la línea (`ParaSonia`) la escribe el dominio, no la pantalla. La posición
anterior — *"el dominio devuelve hechos, la frase la arma la superficie"* — sigue valiendo
y los hechos siguen viajando al lado; lo que cambia es quién arma la oración, porque
decidir qué pasillos merecen nombrarse ya es conocimiento del negocio. La **acción**
(`Severidad.QueHacer`) sale de un conjunto cerrado para que la pantalla no pueda inventar
una.

No juzga: *"el ámbar no es un regaño. Que el helado pasara de mensual a semanal es un
dato, no una falta. La app lo señala y se calla; el juicio no es suyo."* Por eso la
dirección de un cambio vive en el **signo** (`Cambio`, `CambioEnElTotal`) y en un lector
guardado (`Subio`, que revienta si no hay cambio real que leer), y no en un veredicto
*mejorando / empeorando*. Hay comprobaciones que verifican que las líneas del dominio no
contengan *peor, mejor, empeora, mejora, debería, conviene, cuidado* ni *mal*.

Las acciones de este negocio son **administrativas** (una garantía que se cierra), nunca
consejos de salud. Y la lectura sale igual de rica: la firma de su semana, el mes en que
la canasta cambió, de qué pasillos salió la diferencia y su propia inflación producto por
producto son todas conclusiones — sobre la **compra**, que es lo que este módulo sí sabe.

### Lo que NO está

**`AbastecimientosPendientes(fecha)`** es la única pregunta del dominio que mira **el
hueco**: qué categorías de las que se reponen cada tanto hace demasiado que no entran.

Una pantalla solo puede mostrar lo que tiene — puede ordenar las compras, sumarlas,
pintarlas — pero **no puede renderizar una ausencia que nadie le pasó**. "Hace 12 meses
que no entra un lácteo" no es un dato del historial: es una conclusión sobre el historial
más el calendario más la periodicidad propia de cada categoría (proteínas cada 10 días,
granos cada 35, limpieza cada 45).

Con una decisión de dominio deliberada: **los antojos no tienen periodicidad**.
Preguntarle a `Categoria.Azucarados` cada cuánto se repone **revienta**, y un año sin
comprar helado no genera ningún pendiente. Este módulo no existe para decirle a nadie que
hace un mes que no compra helado.

---

## La superficie MCP (medida)

Seis tools intencionales. Las cuatro primeras contestan lo que la app ya muestra en
pantalla y sirven de contexto: `total_de_compras`, `canasta_del_mes`,
`comparar_canastas`, `garantia_de_producto`.

Las dos últimas son las que **justifican conectar una IA a este módulo**, según el
criterio de Álvaro que Exámenes dejó escrito — *si la IA contesta algo que ya está en una
pantalla, el demo no probó nada*:

- **`que_dejaste_de_comprar`** — el hueco: las categorías atrasadas, de la más atrasada a
  la menos. Lista vacía = está al día.
- **`garantias_por_vencer`** — solo las que **ya piden algo**, de la más urgente a la
  menos, con los días que quedan y qué conviene hacer. No es la lista de vigentes que la
  app muestra.
- **`consumo_de(termino)`** — la **serie mes a mes** de una categoría o un producto. La
  app muestra tickets y totales por mes; esto muestra la *forma* de la serie, que es lo
  único con lo que se puede decir **desde cuándo** cambió un consumo.
- **`como_viene_mi_canasta`** — la lectura por defecto, sin obligar a elegir meses.

### Por qué la serie, y no la lista de tickets

Probando con una IA de verdad apareció la pregunta *"¿compré un brócoli hoy?"*, y la
respuesta correcta fue que **no puede contestarla**: no exponemos líneas de compra. Eso
es deliberado — la lista y el detalle de cada ticket ya están en la app, y una IA que los
lea es un lector de facturas, no un lector de la historia.

Lo que sí faltaba, y es distinto, es **cuándo**. `consumo_de` lo da:

```
consumo_de('helado') -> 3 meses, ₡25.060 en total, subió
   serie: 2026-02=₡6,900   2026-07=₡7,120   2026-08=₡11,040
   pico: 2026-08
```

Eso es lo que hace creíble una relación con cualquier otra cosa que le pase a la persona:
no *"tu canasta empeoró"* sino *"el helado se duplicó y arranca en febrero"*. Es la
contraparte de la `SerieDeAnalito` del laboratorio, del lado de las compras — y el cruce
lo hace **el asistente en el dispositivo de ella**, nunca una institución consultando a
la otra.

La dirección es un **hecho**, no una valoración: `hayCambioReal` dice si se movió más de
un quinto (menos que eso es variación de despensa) y el signo de `cambioDeGasto` dice para
dónde. Un solo mes no tiene dirección y preguntarla revienta, en vez de inventar una
lectura. Que subir helado sea bueno o malo **no lo contesta este módulo**.

Verificado con un cliente stdio: `tools/list` devuelve las seis, y `tools/call` sobre
`que_dejaste_de_comprar` trae los cuatro pendientes con su línea del dominio;
`garantias_por_vencer` devuelve `[]` con el guion (la olla vence en 258 días: todavía no
pide nada, y eso **es** la respuesta). La sección `compras` está en el `.mcp.json` de la
raíz.

---

## Estado medido

| Capa | Estado |
|---|---|
| Dominio | ✅ 324 comprobaciones (`comprobar`): los montos del guion, los límites de la garantía, la ausencia con tipo (E45), de qué pasillos sale la diferencia, la firma y el mes del cambio, que ninguna línea juzgue, el hueco, el rango y la serie de consumo |
| Consola | ✅ ciclo completo: `comprobar`, `medir`, `estado`, `detalle`, `canasta`, `comparar`, `garantia`, `pendientes`, `porvencer`, `refrescar` |
| Manifiesto | ✅ escrito contra el contrato de la rama `master_gnothiHost`; ⚠️ **no ejercitado con el Host**, que todavía no está en master |
| EndPointDeEventos | ✅ 6 eventos sembrados, rotación de token (medida con `/device/expirar-access`) y señuelo 403 |
| Inyector CLI | ✅ `listar`, `inyectar` (líneas con categoría y garantía), `senuelo` |
| Caja (UI) | ✅ medida en browser: catálogo de 34 productos con filtros, ticket con cantidades y precios editables, **Registrar compra** publicando el evento 307, y la narrativa publicada a la derecha |
| Catálogo | ✅ `GET /catalogo` con las 9 categorías y los 4 electrodomésticos que traen garantía |
| MCP | ✅ medido con cliente stdio y **probado con Claude Code real**: las 8 tools en `tools/list` y `tools/call` con structuredContent tipado |
| Wasm | ✅ **CORRE EN EL BROWSER** con el motor `10029` — ver abajo |
| PaginaDeDescarga | ✅ landing servida y verificada; ⚠️ con **nginx/Docker sin probar** (daemon apagado): la prueba se hizo sirviendo el mismo webroot (landing en `/`, publish en `/app/`) con un estático |

### La carátula, medida en el browser

Con `3.0.0-beta.10029` la app abre el actor y muestra todo: **8 compras**, el total, la
canasta comparada, las dos garantías vigentes con su vencimiento, y el detalle de un
ticket con la categoría de cada línea y el badge *con garantía*. Y el ciclo en vivo:
inyectando una compra sana (salmón + espinaca) mientras la app estaba abierta, **Refresh**
la trajo — evento 307 → 308, 7 → 8 compras — y los pasillos del mes cambiaron **de tres a
68%**. El dominio reacciona al dato nuevo, que es exactamente lo que la demo promete.

> **TRAMPA QUE COSTÓ UNA HORA, y no es del motor.** Con el `Directory.Build.props` ya en
> `10029`, la carátula seguía fallando con `Cannot wait on monitors`: el `obj/` de los
> proyectos venía de antes del bump y el publish arrastraba el **10027**. No se ve en el
> log del build. Se detecta **midiendo el artefacto publicado**, que es lo que hizo
> falsable el diagnóstico:
>
> | `_framework/Puppeteer.*.wasm` | Versión |
> |---|---|
> | 814.869 b | 10027 (el que fallaba) |
> | **815.381 b** | **10029 (el que corre)** |
>
> La cura: borrar `obj/` y `bin/` del negocio **y** el directorio de publish, y publicar
> de nuevo. Si la carátula falla con el error del motor teniendo la versión buena en el
> props, **medí el .wasm antes de culpar al motor**.

## Pendientes de este negocio

| # | Pendiente |
|---|---|
| 1 | **Probar URL1 con nginx/Docker** y el flujo de instalación de la PWA. El proxy `/app/eventos/` ya está en el `nginx.conf` (patrón de Renán: base relativa para que funcione desde un teléfono y por https), pero **sin Docker no se pudo ejercitar**: la prueba en browser se hizo con base absoluta al mock |
| 2 | **`Puppets.Portador` a la raíz**: con dos negocios vivos ya aplica la convención. Acordar con Renán y el equipo, borrar las copias y referenciar la de la raíz |
| 3 | **La marca es *Supermercado La Guaria*** (en inglés *La Guaria Market*: el nombre propio no se traduce, solo el descriptor), confirmada con el equipo. La especificación de marca vive fuera del repo y trae logo, paleta y tipografía; el retoque final de la UI es de Fer, Nina y Celia. Falta aplicar el logo de la guaria morada y las pastillas de los botones |
| 4 | **Probar URL1 con Docker** (publish + certificados + compose) y el flujo de instalación de la PWA |
| 5 | **La gasolinera**: Tareas.md la asigna también a Pleitez "de momento"; puede salir del ecosistema si no cuenta una historia interesante. Este negocio ya deja el molde para armarla rápido |
| 6 | **Icons de la carátula** (favicon/icon-192/icon-512) siguen siendo los del template copiado de Exámenes |
| 7 | **Bootstrap**: la carátula carga `bootstrap.min.css` (227 KB, el único de los 44 archivos del template que la página pide — el resto no se versionó). Exámenes lo quitó del todo y escribió su propio CSS; conviene alinearse cuando la UI la revise Fernanda |
| 8 | **El `instalacion.json` es del Host**, no de este módulo (son dos autoridades: la institución declara qué ofrece, el Host decide cómo se lo ejecuta y qué carpeta le concede). Cuando el Host entre a master, quien lo opere debe crear `Modulos/compras/` con nuestro `manifiesto.json` y su propio `instalacion.json` apuntando a `Compras/App/Compras.Mcp/bin/.../Compras.Mcp.dll` y a la carpeta concedida — **la misma que se eligió en la app**, o el módulo reporta `hayDatos=false` |
| 9 | **Nombres para mostrar**: `AbastecimientoPendiente` traduce la llave del repertorio (`FrutasYVerduras`) a algo legible con un `switch`. Exámenes resolvió lo mismo tipando el analito con un `ParaMostrar` propio; conviene mover eso a `Categoria` |
| 10 | **La lectura del consumo compara los extremos**, así que un valle en el medio no lo mueve: las proteínas del guion (13.600 → 4.350 → 12.800) salen sin cambio real, y es cierto entre extremos pero esconde el bajón de febrero. Exámenes resolvió lo análogo con dos lecturas — tendencia sobre la serie entera y velocidad sobre el tramo reciente. Conviene traer esa segunda lectura |
