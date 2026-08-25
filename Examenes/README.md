# Exámenes — laboratorio clínico

El negocio de Renán en el milestone **Gnothi**. Un laboratorio le entrega a su paciente una
app con **su propio historial**, para que ella y su IA lean la **serie completa** en vez del
último valor suelto.

Sigue la convención de estructura acordada por el equipo: un directorio por negocio, cinco
nodos adentro, nombres en español. Este README es **cómo se corre**.

---

## Los cinco nodos

| Carpeta | Qué es |
|---|---|
| `PaginaDeDescarga/` | **URL1** · nginx que sirve la app publicada. `8090` http, `8453` https. **No es un csproj** |
| `EndPointDeEventos/` | **URL2** · `8091`. El GET de eventos, el token de device y su rotación, y el señuelo que devuelve 403 |
| `InyectorDeEventosDemo/` | Siembra la historia en URL2. Valores y fechas **elegidos**, no aleatorios |
| `Dominio/` | El puppet: `Expediente`, `Examen`, `Magnitud`, `SerieDeAnalito`, `Bitacora`. **Cero dependencias** |
| `App/` | Una lógica y tres carátulas |

Dentro de `App/`:

| Proyecto | Qué es |
|---|---|
| `Examenes.App` | La lógica: los `PerformCommand` y `PerformQuery`. No sabe dónde corre |
| `Examenes.Wasm` | La app que instala Sonia. Blazor WASM, PWA |
| `Examenes.Mcp` | La superficie que ve la IA. **Solo lectura** sobre el journal |
| `Examenes.Consola` | Carátula de consola, para medir el dominio y el refresh sin browser |

`EndPointDeEventos` e `InyectorDeEventosDemo` **no referencian `Dominio`** ni tocan el
journal. Es un invariante del diseño, no una omisión: el proveedor no conoce el dominio de la
persona. Lo único que los conecta es HTTP.

## Puertos

Distintos de los de `Facturas` **a propósito**, para que dos negocios corran a la vez — las
historias cruzadas lo necesitan.

| Servicio | Facturas | Exámenes |
|---|---|---|
| PaginaDeDescarga http | 8080 | **8090** |
| PaginaDeDescarga https | 8443 | **8453** |
| EndPointDeEventos | 8081 | **8091** |

---

## Correrlo

### Lo que anda hoy: el ciclo por consola

El WASM está bloqueado por el motor, así que **se trabaja y se valida por `Consola`** — es
para lo que existe esa carátula. Esto no necesita Docker.

**1 · URL2**

```bash
dotnet run --project Examenes/EndPointDeEventos
```

```bash
dotnet run --project Examenes/InyectorDeEventosDemo -- listar
```

**2 · El ciclo completo, sin browser**

```bash
dotnet run --project Examenes/App/Examenes.Consola -- refrescar "/tmp/journal-examenes" http://localhost:8091/ expediente 1
```

Tiene que terminar en **8 exámenes**, evento **208**, y las tendencias correctas.

```bash
dotnet run --project Examenes/App/Examenes.Consola -- comprobar
```

`comprobar` corre las **60 comprobaciones del dominio puro** — sin actor, sin journal y sin
disco, así que cuando algo se rompe dice qué regla se rompió. `medir` aplica la historia y
verifica los invariantes; `estado` abre el journal y reporta (correr en proceso nuevo:
si las tendencias siguen ahí, el replay rehidrató); `tendencia` evalúa la serie de un
analito; `velocidad` da el ritmo de cambio.

```bash
dotnet run --project Examenes/App/Examenes.Consola -- velocidad "/tmp/journal-examenes" "Antigeno prostatico"
```

### URL1: necesita Docker

URL1 es nginx, así que no corre por consola. Publicar la app y levantar los dos servidores:

```bash
MSBuildEnableWorkloadResolver=false dotnet publish Examenes/App/Examenes.Wasm -c Release -o Examenes/PaginaDeDescarga/publicado -p:PublishTrimmed=false
```

> **Ese `MSBuildEnableWorkloadResolver=false` no es adorno.** Si el SDK te pide el workload
> `wasm-tools` (`NETSDK1147`), esa variable desactiva el resolver y el build pasa — la app
> no necesita el workload, solo publica sin optimizar. Instalarlo pide `sudo`, y un
> `dotnet workload install` que falla por permisos igual alcanza a actualizar los manifests
> del SDK y **deja el error pegado**. Si ya te pasó, esta es la salida sin contraseña.

```bash
cd Examenes && ./PaginaDeDescarga/generar-certificado.sh && docker compose up -d
```

Y queda en **`http://localhost:8090`** — la página de descarga de la institución — con la
app colgando de **`/app/`**.

Que la app no esté en la raíz no es cosmético. El service worker de la PWA toma scope del
directorio donde se registra y contesta **toda** navegación de ese scope con su `index.html`
cacheado. Con la app en `/`, la página de descarga era inalcanzable en cuanto alguien
instalaba la app: nginx la servía perfecto —medido, `GET /descarga 200 18173`— y el browser
mostraba igual el 404 del router de Blazor, porque la respuesta nunca llegaba a pedirse.

Con la app en `/app/` el scope es `/app/` y la raíz queda afuera. Lo hace la ruta, no una
lista de excepciones. Tres archivos tienen que decir lo mismo: el `<base href>` de
`index.html`, el `const base` de `service-worker.published.js` y el `nginx.conf`.

### Los tres modos de persistencia

El briefing del MVP (§6.2) es explícito: *"No hagas que la app falle donde no hay picker:
degrada a OPFS y ofrece export/import"*. La app detecta en runtime y ofrece los tres, y
**dice de cada uno lo que da y lo que no**:

| Modo | Persiste | ¿Lo lee el MCP? | Dónde funciona |
|---|---|---|---|
| **Una carpeta tuya** | sí, en tu disco | **sí** — es el caso estrella | Chromium desktop |
| **En este navegador** (OPFS) | sí, pero lo administra el browser | **no** | casi todos, incl. móvil |
| **Un archivo** (export/import) | manual | a mano | todos |

Que OPFS persista **no lo convierte en reemplazo**: el servidor MCP corre fuera del browser
y no ve ese almacenamiento, así que la mitad *"tu IA lo lee"* del producto no funciona ahí.
La pantalla lo dice con todas las letras en vez de dejar creer que está todo resuelto.

La pieza que hizo esto barato: **OPFS devuelve un `FileSystemDirectoryHandle`**, el mismo
tipo que el picker. Por eso `listar` / `leer` / `escribir` no distinguen uno del otro, y el
orden precarga-MEMFS → actor → flush **no se tocó** — sólo cambia a qué raíz apuntan.

**Medido** en el navegador, sobre la app servida por nginx: activar OPFS, escribir, listar y
leer da un round-trip byte por byte idéntico, y el modo queda recordado para el próximo
arranque.

### El caso estrella corre en Chrome o Edge, no en Safari

**Medido**: instalada desde Edge, la app solo tropieza con el bloqueo del motor. Desde
Safari muestra además que *no puede guardar en una carpeta tuya*, y el botón de elegir
carpeta queda deshabilitado.

No es un bug: `showDirectoryPicker` —la File System Access API— **existe solo en
navegadores Chromium** (Chrome, Edge, Brave, Arc). Safari y Firefox no la tienen.

Y esto **no es un detalle de pantalla, es una condición de la propuesta**. El argumento
del producto tiene dos mitades y las dos dependen de esa API:

| | Necesita |
|---|---|
| *"el expediente vive en una carpeta tuya"* | que la app escriba en una carpeta que la persona eligió |
| *"tu IA lo lee sin que salga de tu computadora"* | que el servidor MCP lea **esa misma carpeta** desde afuera del browser |

Safari tiene OPFS (`navigator.storage.getDirectory()`), pero **no sirve como reemplazo**:
es almacenamiento privado que administra el browser, no una carpeta que la persona eligió,
y no se puede leer desde afuera. Usarlo rompería las dos mitades a la vez — dejaría de ser
*su* carpeta y el MCP no encontraría nada.

Aplica a **los cuatro dominios**, no solo a este. Vale que Geaninna lo sepa antes de fijar
el speech: la narrativa puede seguir siendo cualquiera, pero el demo se enseña en un
navegador Chromium.

> El literal **`localhost`** importa. El secure context se decide por el *string* del host:
> medido en el negocio de facturas, `http://127.0.0.1.nip.io:8765` da
> `isSecureContext=false` —sin `showDirectoryPicker` y sin service worker— siendo el mismo
> socket loopback.

## El invariante, y es falsable

**La app no es cliente de los servicios generales del laboratorio.** Lo único que le pide es
su propia narrativa. URL2 tiene un señuelo que devuelve **403** con el token *válido* de la
app, porque su scope es `expediente.events.read` y nada más:

```bash
dotnet run --project Examenes/InyectorDeEventosDemo -- senuelo
```

```
GET expediente/1/events?after=0    -> 200
GET expediente/1/perfil            -> 403
     {"error":"el scope del token no cubre esta operacion",
      "scopeDelToken":"expediente.events.read","scopeExigido":"expediente.profile.read"}
```

---

## El dominio

### Dos jerarquías, y ninguna es decorativa

**Por examen** — de dónde salió la muestra:

```
Examen                        fecha + nombre. NO declara un valor numérico.
├── ExamenDeSangre            mide UN analito: rango por edad, umbrales, dirección
│   ├── Colesterol            total
│   ├── ColesterolDetallado   HDL / LDL
│   ├── AntigenoProstatico
│   └── Hemoglobina           el único donde preocupa que BAJE
└── ExamenDeOrina             informa un COLOR y unos NITRITOS. Ni serie ni número.
```

**Por magnitud** — qué forma tiene el resultado. Un laboratorio real reporta en **seis**
formas, y el urianálisis solo usa cuatro:

```
Magnitud
├── MagnitudContinua   forma 1 · Medicion       IMPLEMENTADA   colesterol, PSA, hemoglobina
│                      forma 2 · sin unidad     identificada   pH, densidad relativa
├── MagnitudOrdinal    forma 3 · cruces         identificada   tira reactiva
├── MagnitudDeConteo   forma 4 · por campo      identificada   eritrocitos
├── MagnitudBinaria    forma 5 · Nitritos       IMPLEMENTADA   positivo/negativo/no realizado
└── MagnitudNominal    forma 6 · ColorDeOrina   IMPLEMENTADA   color, aspecto, olor
```

`Examen` **no declara un valor medido**, y ahí está la decisión que ordena todo lo demás. Si
la base hubiera declarado un `decimal Valor`, cuatro de las seis formas tendrían que
devolver cero y mentir.

Las formas 2, 3 y 4 están **nombradas y no construidas**, a propósito: el encargo fue
modelar dos o tres analitos de verdad, no un sistema de laboratorio clínico.

### Por qué los nitritos no son un `bool`

Es la clase más corta que demuestra la taxonomía, y tiene tres argumentos, los tres del
negocio:

- **El vocabulario pertenece al analito.** Nitritos son *positivo/negativo*; una serología es
  *reactivo/no reactivo*. "Reactivo" separa *la prueba reaccionó* de *el paciente tiene la
  enfermedad*, que es una distinción real. Con un `bool` eso termina en la UI.
- **La polaridad no es obvia.** En Rh, positivo es lo común y no es un hallazgo. En nitritos
  sí lo es. Un `bool` no distingue **presencia** de **anormalidad**.
- **Falta un tercer estado.** `NoRealizado` no es lo mismo que negativo. Preguntar el
  resultado de una prueba que no se corrió **revienta**, no contesta "no".

### Las dos preguntas que ninguna pantalla contesta

`SerieDeAnalito` mira la **serie**, no el último valor, y contesta dos cosas distintas
mirando **tramos distintos**:

**`Evaluar()` — ¿para dónde viene, y es creíble?** Mira paso a paso entre mediciones
consecutivas para detectar lo imposible, y los extremos para la dirección.

| Serie | Lectura |
|---|---|
| colesterol 200 → 280 en **un día** | `salto inverosimil` → repetir el examen |
| colesterol 200 → 280 en **un año** | `en ascenso`, exige atención |
| antígeno 3,0 → 3,1 en **dos años** | `estable` |
| antígeno 3,0 → 5,2 en **un año** | `en ascenso`, exige atención |

**`Velocidad()` — ¿a qué ritmo?** Es la lectura que el antígeno prostático exige, y se mide
sobre el **tramo más reciente**, no sobre la serie entera. Con los datos del guion la
diferencia decide el caso:

| Tramo | Cálculo | Contra el umbral de 0,75 |
|---|---|---|
| serie entera 2023 → 2026 | 2,2 ng/mL en 1096 días = **0,73/año** | no alarmaría |
| tramo reciente 2025 → 2026 | 2,2 ng/mL en 365 días = **2,20/año** | **exige atención** |

Los tres años estables de antes no diluyen un salto de este año: lo hacen más notable.
Promediar sobre toda la historia es exactamente como se pierde un cambio recién empezado.

El dominio devuelve **hechos** —tendencia, extremos, unidad, días, ritmo— y nunca prosa. La
frase la arma la superficie. Es lo que hace **imposible** el consejo de plantilla: no hay de
dónde sacar *"bajá las grasas"*.

---

## Estado medido

| Capa | Estado |
|---|---|
| Dominio | ✅ 60 comprobaciones (`comprobar`), incluidas las cuatro lecturas del guion y la velocidad |
| Consola | ✅ ciclo completo: `comprobar`, `medir`, `estado`, `tendencia`, `velocidad`, `refrescar` |
| EndPointDeEventos | ✅ 8 eventos, rotación de token y señuelo 403 |
| MCP | ✅ heredado del paquete, no se volvió a medir después de tocar el dominio |
| PaginaDeDescarga | ⚠️ **no se pudo levantar**: Docker no está corriendo en esta máquina |
| Wasm | ⚠️ **arranca, renderiza y se instala**, y falla al abrir el actor — ver abajo |
| Navegador | ⚠️ la **carpeta propia** es solo Chromium. Safari y Firefox degradan a OPFS o a export/import — la app no se queda sin persistir |

### El bloqueo del motor, medido acá

Las notas del equipo dicen que la carátula del browser *"no arranca"*. Medido sirviendo el
publish por nginx: eso es **impreciso y conviene corregirlo**. La app carga, Blazor
renderiza y la pantalla de permisos se ve bien. Lo que falla es el paso siguiente, al abrir
el actor:

```
System.PlatformNotSupportedException: Cannot wait on monitors on this runtime.
```

La app lo atrapa y lo reporta en pantalla en vez de morirse en blanco. Sigue siendo el mismo
bloqueo —falta el replay secuencial en `3.0.0-beta.10027`— pero el síntoma real es más
acotado que lo que dice el doc, y eso importa para el reporte a Álvaro.

---

## Pendientes de este negocio

| # | Pendiente |
|---|---|
| 1 | **Versión del motor** con los tres commits de `wasm-replay`. Es una línea en `Directory.Build.props`. Es de Álvaro |
| 3 | **Tras publicar de nuevo, hay que cerrar todas las pestañas del origen.** El service worker de Blazor no llama a `skipWaiting()`, así que la versión nueva se instala pero queda esperando hasta que no quede ninguna pestaña abierta de `localhost:8090`. Un cambio en el SW que "no se aplica" casi siempre es esto |
| 4 | **El workload `wasm-tools-net9` no está instalado**, así que el publish sale sin optimizar (57 MB). Funciona; conviene instalarlo antes de mostrarlo |
| 5 | **La marca ficticia** (*Laboratorios Sotavento*) es un placeholder. La definitiva la define el equipo. Vive solo en `PaginaDeDescarga/descarga/index.html` y en los assets de la carátula |
| 6 | **La jerarquía por examen vs. por analito** sigue sin decidirse con Álvaro. Este dominio cuelga del **examen**, que es lo que él pidió textualmente, y la taxonomía de `Magnitud` cubre buena parte de lo que motivaba la otra |
