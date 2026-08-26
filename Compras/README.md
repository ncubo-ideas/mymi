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
| `InyectorDeEventosDemo/` | Siembra la historia en URL2. Valores y fechas **elegidos**, no aleatorios |
| `Dominio/` | El puppet: `Historial`, `Compra`, `Item`, `Categoria`, `Garantia`, `Canasta`, `Bitacora`. **Cero dependencias** |
| `App/` | Una lógica y tres carátulas |

Dentro de `App/`:

| Proyecto | Qué es |
|---|---|
| `Compras.App` | La lógica: los `PerformCommand` y `PerformQuery`. No sabe dónde corre |
| `Compras.Wasm` | La app que instala Sonia ("La Cosecha y vos"). Blazor WASM, PWA |
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

`comprobar` corre las **58 comprobaciones del dominio puro** — sin actor, sin journal y
sin disco, así que cuando algo se rompe dice qué regla se rompió. `medir` aplica la
historia y verifica los invariantes; `estado` abre el journal y reporta (correr en
proceso nuevo: si las canastas siguen ahí, el replay rehidrató); `detalle` muestra un
ticket línea por línea; `canasta` y `comparar` son las lecturas del mes; `garantia`
busca un producto con su respaldo.

```bash
dotnet run --project Compras/App/Compras.Consola -- garantia "/tmp/journal-compras" "olla arrocera"
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
`Categoria` (conjunto cerrado, 9 miembros) lleva el **perfil de consumo** —
recomendado / neutro / ocasional — y eso es lo que vuelve la narrativa **analizable**:

```
Categoria
├── Proteinas          recomendado      pollo, tilapia
├── FrutasYVerduras    recomendado      brocoli
├── LacteosYHuevos     neutro           leche, huevos
├── GranosYAbarrotes   neutro           arroz, frijoles
├── AceitesYGrasas     ocasional        aceite, manteca
├── Azucarados         ocasional        helado, sodas, chocolate
├── Snacks             ocasional        papas fritas
├── Limpieza           NO comestible    detergente
└── Electrohogar       NO comestible    la olla arrocera (con garantía)
```

Tres decisiones que ordenan lo demás (las lecciones de Exámenes y la guía de dominio
`puppeteer-domain-modeling` **v1.1.0**, aplicadas acá):

- **Preguntarle el perfil a un detergente revienta**, no contesta "neutro": un perfil
  inventado contaminaría las proporciones de la canasta. Quien suma pregunta
  `EsComestible` antes (el mismo argumento por el que los nitritos no son un `bool`).
- **La garantía no es un `int`**: sabe desde cuándo corre (la fecha de la compra) y
  contesta si está vigente en una fecha **recibida** — el dominio no lee el reloj.
- **La ausencia tiene TIPO, no `null` (E45).** Lo que no trae cobertura porta
  `Garantia.Ninguna`; lo no comestible es el subtipo `NoComestible`; una búsqueda sin
  resultado devuelve el registro `Ausente`. Ningún campo, parámetro ni retorno del
  dominio es `Type?`. El par es siempre el mismo: una **consulta de presencia total**
  (`Existe()`, `TieneGarantia`, `Encontrada`) y un **lector guardado** que revienta si
  se lo llama sin preguntar.

### Las dos preguntas que ningún ticket contesta

**`CanastaDe(anio, mes)` / `CompararCanastas(...)`** — la proporción de gasto ocasional
se calcula **sobre lo comestible**: la olla arrocera de ₡45.900 no puede diluir el
helado. Con el guion sembrado:

| Canasta | Ocasional | Lectura |
|---|---|---|
| agosto 2025 | 2.890 de 32.910 = **8,8%** | la canasta base |
| agosto 2026 | 31.680 de 32.600 = **97,2%** | se volteó |

Es la mitad "compras" del **cruce preplanificado** del milestone: el laboratorio
siembra colesterol 200 (ago-2025) → 280 (ago-2026), y "el colesterol alto es por culpa
del helado" lo arma el **asistente**, que es el único que ve los dos negocios. Los
módulos no se hablan.

**`BuscarGarantia(texto, fechaDeConsulta)`** — la respuesta a *"¿cuándo compré la olla
arrocera y hasta cuándo tiene garantía?"*: compra T-93872, 2026-05-10, 12 meses, vence
2027-05-10. Total: lo que no está o no tiene garantía devuelve el registro **`Ausente`**
(`Encontrada=false`), no una excepción. Si se recompró, gana la compra más reciente.

El dominio devuelve **hechos** — montos, proporciones, fechas — y nunca prosa. La frase
la arma la superficie. Es lo que hace **imposible** el consejo de plantilla: no hay de
dónde sacar *"comprá menos helado"*.

---

## La superficie MCP (medida)

Cuatro tools intencionales: `total_de_compras`, `canasta_del_mes`, `comparar_canastas`,
`garantia_de_producto`. Verificado con un cliente stdio: `initialize` → instructions,
`tools/list` → schemas, y `tools/call` devuelve `structuredContent` tipado
(la garantía con su respaldo; la comparación con 0.0878 → 0.9718 y
`crecioLoOcasional:true`). La sección `compras` ya está en el `.mcp.json` de la raíz.

---

## Estado medido

| Capa | Estado |
|---|---|
| Dominio | ✅ 60 comprobaciones (`comprobar`), incluidas las dos proporciones del guion, los límites de la garantía y la ausencia con tipo (E45) |
| Consola | ✅ ciclo completo: `comprobar`, `medir`, `estado`, `detalle`, `canasta`, `comparar`, `garantia`, `refrescar` |
| EndPointDeEventos | ✅ 6 eventos sembrados, rotación de token (medida con `/device/expirar-access`) y señuelo 403 |
| Inyector | ✅ `listar`, `inyectar` (líneas con categoría y garantía), `senuelo` |
| MCP | ✅ medido con cliente stdio: initialize, tools/list y dos tools/call con structuredContent |
| Wasm | ✅ **CORRE EN EL BROWSER** con el motor `10029` — ver abajo |
| PaginaDeDescarga | ✅ landing servida y verificada; ⚠️ con **nginx/Docker sin probar** (daemon apagado): la prueba se hizo sirviendo el mismo webroot (landing en `/`, publish en `/app/`) con un estático |

### La carátula, medida en el browser

Con `3.0.0-beta.10029` la app abre el actor y muestra todo: **8 compras**, el total, la
canasta comparada, las dos garantías vigentes con su vencimiento, y el detalle de un
ticket con la categoría de cada línea y el badge *con garantía*. Y el ciclo en vivo:
inyectando una compra sana (salmón + espinaca) mientras la app estaba abierta, **Refresh**
la trajo — evento 307 → 308, 7 → 8 compras — y la proporción ocasional **bajó de 97,5% a
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
| 3 | **La marca ficticia** (*Supermercado La Cosecha* / *La Cosecha y vos*) es un placeholder; la definitiva la define el equipo. Vive solo en `PaginaDeDescarga/descarga/index.html` y en los assets de la carátula |
| 4 | **Probar URL1 con Docker** (publish + certificados + compose) y el flujo de instalación de la PWA |
| 5 | **La gasolinera**: Tareas.md la asigna también a Pleitez "de momento"; puede salir del ecosistema si no cuenta una historia interesante. Este negocio ya deja el molde para armarla rápido |
| 6 | **Icons de la carátula** (favicon/icon-192/icon-512) siguen siendo los del template copiado de Exámenes |
| 7 | **Bootstrap**: la carátula carga `bootstrap.min.css` (227 KB, el único de los 44 archivos del template que la página pide — el resto no se versionó). Exámenes lo quitó del todo y escribió su propio CSS; conviene alinearse cuando la UI la revise Fernanda |
