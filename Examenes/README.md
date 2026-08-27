# Exámenes — laboratorio clínico

El negocio de Renán en el milestone **Gnothi**. Un laboratorio le entrega a su paciente una
app con **su propio historial**, para que ella y su IA lean la **serie completa** en vez del
último valor suelto.

Sigue la convención de estructura acordada por el equipo: un directorio por negocio, cinco
nodos adentro, nombres en español — [y eso incluye los tipos del dominio](#el-idioma-del-dominio-es-el-idioma-del-canon).
Este README es **cómo se corre**.

---

## Los cinco nodos

| Carpeta | Qué es |
|---|---|
| `PaginaDeDescarga/` | **URL1** · nginx que sirve la app publicada. `8090` http, `8453` https. **No es un csproj** |
| `EndPointDeEventos/` | **URL2** · `8091`. El GET de eventos, el token de device y su rotación, y el señuelo que devuelve 403 |
| `InyectorDeEventosDemo/` | Siembra la historia en URL2. Valores y fechas **elegidos**, no aleatorios |
| `Dominio/` | El puppet: `Expediente`, `Examen`, `Analito`, `Magnitud`, `SerieDeAnalito`, `Bitacora`. **Cero dependencias** |
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

### El ciclo corto: por consola

El WASM ya corre —el bloqueo del motor se levantó en `3.0.0-beta.10029`—, pero el ciclo de
trabajo sigue siendo **`Consola`**: mide el dominio en microsegundos, no necesita Docker ni
browser, y cuando algo se rompe dice qué regla se rompió en vez de dejar una pantalla rara.
Un cambio en el dominio no está listo hasta que sus comprobaciones pasan.

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
arranque. En **Safari desktop** los modos 2 y 3 están disponibles: degrada, no falla.

### URL2 entra por el mismo origen que la app

El publish apunta a **`eventos/`**, una URL *relativa*, y nginx la proxea al contenedor de
URL2. No es que los dos nodos se fusionen —siguen siendo dos procesos, dos contenedores y
el token con su scope no cambia—: es sólo por dónde entra el pedido, y resuelve tres cosas
de una.

| | Con `http://localhost:8091/` | Con `eventos/` |
|---|---|---|
| desde un teléfono | `localhost` es el teléfono | sigue al host que sirvió la app |
| app servida por https | bloqueado por contenido mixto | mismo protocolo |
| CORS | preflight | deja de ser cross-origin |

**Medido**: en un iPhone la app abría, no alcanzaba al proveedor y quedaba en cero exámenes.
Con `eventos/`, el mismo publish sirve en `localhost`, por IP y por https sin recompilar.
Una URL absoluta en la config sigue ganando, para apuntar a otro host sin recompilar.

### Desde un teléfono hay que entrar por https

Las dos APIs de almacenamiento exigen **contexto seguro**: https, o el literal `localhost`.
Un teléfono no puede usar `localhost` para llegar a esta máquina — entra por la IP — y por
`http://` esa página **no es contexto seguro**, así que el navegador ni define las APIs.

**Medido**: en un iPhone entrando por `http://<ip>:8090` la app reportaba que el navegador
no permitía guardar. La misma Safari en la Mac, por `localhost`, sí podía. **No era Safari.**

Por eso la app ahora distingue las dos causas —`navegador` vs `inseguro`— y dice cuál es:
mandar a buscar otro navegador cuando el problema es el protocolo es culpar al equipo
equivocado.

`generar-certificado.sh` incluye la IP de la máquina en el SAN, así que:

```bash
cd Examenes && ./PaginaDeDescarga/generar-certificado.sh
```

deja la app accesible en **`https://<ip>:8453`**. Sigue siendo self-signed: hay que aceptar
la advertencia, y en iOS conviene instalar y confiar el certificado en Ajustes.

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

### El idioma del dominio es el idioma del canon

`Expediente`, `SerieDeAnalito`, `Magnitud`, `ColorDeOrina`, `ControlPendiente`. **En
español, y también los `///` y los comentarios.** No `MedicalRecord`, no `AnalyteSeries`.

No es preferencia estética. La guía de dominio pide nombrar con **la palabra que el equipo
dice en voz alta**, y este equipo dice «analito», «hallazgo», «carátula». Un dominio en
inglés con una UI en español obliga a traducir en cada frontera, y la traducción es donde
el significado se pierde: «hallazgo» no es *finding*, «analito» no es *test*.

**La regla no es «español»: es el idioma del canon.** El dominio de `Gnothi.Host` se queda
en **inglés** por decisión de su equipo, y eso no la contradice — la confirma. Dos
productos del mismo milestone pueden caer de lados distintos porque tienen canon distinto;
lo que no se puede es que un mismo producto hable dos idiomas por dentro.

Va en inglés lo que **no nombramos nosotros**: el tipo del commit (`feat`, `fix` — es el
estándar, y lo verifica `.githooks/commit-msg`), la API de Puppeteer (`PerformCommand`,
`Check`), las palabras del DSL (`print`, `foreach`) y C# con su BCL (`IReadOnlyList`).

> Se escribe acá el 2026-08-26 porque no estaba escrito en ningún lado. Édgar y Jonathan
> levantaron que en otro negocio la UI estaba en español y el dominio en inglés, y
> preguntaron si el skill de diseño de dominio no decía nada al respecto. No decía: sus
> ejemplos están todos en inglés, y «el canon manda» se leía como «usá la palabra del
> canon», no «usá el idioma del canon».

### Cuatro jerarquías, y ninguna es decorativa

**Por examen** — de dónde salió la muestra. Son tres muestras distintas, y por eso son tres
ramas:

```
Examen                        fecha + nombre. NO declara un valor numérico.
├── ExamenDeSangre            mide UN analito: rango POR PACIENTE, umbrales, dirección
│   ├── Colesterol            total
│   ├── ColesterolDetallado   LDL + HDL — el único de sangre que mide DOS
│   ├── AntigenoProstatico
│   └── Hemoglobina           el único donde preocupa que BAJE
├── ExamenDeOrina             color + nitritos + sangre oculta: TRES cosas medidas
└── ExamenDeHeces             color + sangre oculta
```

**Por analito** — *qué* se midió, como identidad y no como cadena. Un expediente real no
viene de un laboratorio sino de todos por los que pasó la persona, y cada uno lo nombra
distinto: `HEMOGLOBINA`, `HGB` y `Hemoglobina` tienen que caer en **una** serie de tres
puntos y no en tres de un punto:

```
Analito                       identidad + sinónimos + REQUISITO ANATÓMICO
├── AnalitoContinuo           tiene unidad y rango posible   PSA, colesterol, hemoglobina
├── AnalitoBinario            sin unidad, forma serie        nitritos, las dos sangres
└── AnalitoNominal            sin unidad, NO forma serie     los dos colores
```

**El requisito anatómico vive acá, en la identidad, y no en el examen.** Es lo que hace
que la dependencia del sexo no sea un filtro de pantalla: la aplicabilidad es una
propiedad de *qué se midió*, independiente de que exista una medición, así que la tool MCP
puede contestar *«este analito no te corresponde»* de un antígeno del que ni siquiera hay
serie. Y el predicado **no es el sexo, es la anatomía** — `RequisitoAnatomico.Prostata`,
no *«si es hombre»*— que es lo que deja el modelo abierto a los casos que un demo no
necesita sin cambiar su forma después.

La unidad vive en `AnalitoContinuo` y no en la raíz. Un color es algo que se midió —o sea,
un analito— y no se mide en nada; exigirle unidad a la raíz era lo que hacía que
`Analito` fuera, en realidad, *«analito cuantitativo de sangre»* con un nombre general
puesto encima.

**Por magnitud** — qué forma tiene el resultado. Un laboratorio real reporta en **seis**
formas, y el urianálisis solo usa cuatro:

```
Magnitud
├── MagnitudContinua   forma 1 · Medicion       IMPLEMENTADA   colesterol, PSA, hemoglobina
│                      forma 2 · sin unidad     identificada   pH, densidad relativa
├── MagnitudOrdinal    forma 3 · cruces         identificada   tira reactiva
├── MagnitudDeConteo   forma 4 · por campo      identificada   eritrocitos
├── MagnitudBinaria    forma 5                  IMPLEMENTADA   Nitritos, SangreEnOrina,
│                                                              SangreEnHeces
└── MagnitudNominal    forma 6                  IMPLEMENTADA   ColorDeOrina, ColorDeHeces
```

**Por serie** — cómo se lee la historia de ese analito. No todas las series se leen igual, y
ese es el corte más reciente:

```
SerieDeAnalito                identidad + cuándo fue la última + cada cuánto repetirla
├── SerieContinua             ordena, resta y divide por días  → EvaluacionDeTendencia
│   ├── SerieDeAntigenoProstatico   velocidad, nivel habitual, confirmación de un pico
│   ├── SerieDeColesterol           meta, partida del episodio, si viene bajando
│   ├── SerieDeColesterolLdl        si el HDL acompaña
│   └── SerieDeHemoglobina          el peor grado de anemia que hubo
└── SerieBinaria              cuenta apariciones y rachas      → EvaluacionDePresencia
    ├── SerieDeNitritos             si la infección cedió
    ├── SerieDeSangreEnOrina        si fue pasajera
    └── SerieDeSangreEnHeces        si se repitió y volvió a salir
```

`Examen` **no declara un valor medido**, y ahí está la decisión que ordena todo lo demás. Si
la base hubiera declarado un `decimal Valor`, cuatro de las seis formas tendrían que
devolver cero y mentir. Lo mismo un piso más abajo: `SerieDeAnalito` no declara `Cambio`,
porque un binario no tiene uno.

Las formas 2, 3 y 4 están **nombradas y no construidas**, a propósito: el encargo fue
modelar dos o tres analitos de verdad, no un sistema de laboratorio clínico.

> **Esto cierra la pregunta de «por examen o por analito»**, que estuvo abierta con Álvaro
> buena parte del milestone. La respuesta resultó ser que no era una disyuntiva: el examen
> es **de dónde salió la muestra**, el analito es **qué se midió**, y la serie es **cómo se
> lee su historia**. Son tres preguntas distintas y cada una tiene su jerarquía. Lo que sí
> era un error era tener una sola y hacerla contestar las tres.

### Un examen no mide una cosa: mide las que mide

Un urianálisis informa **tres** analitos y el de heces **dos**. `Examen.Lecturas` los emite
uno por uno —`LecturaDeExamen` es *qué* analito con *qué* resultado— y por eso el historial
muestra una fila por cosa medida en vez de esto:

```
Examen general de orina    ámbar, nitritos negativo, sangre negativo
```

Tres cosas aplanadas en una fila y en un string. Una prueba que el laboratorio **no corrió**
no emite lectura: no es un negativo, es una lectura que no existe.

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

### Las tres preguntas que ninguna pantalla contesta

La serie mira la **historia**, no el último valor, y contesta tres cosas distintas mirando
**tramos distintos**:

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

**`EvaluarPresencia()` — ¿apareció, persiste, o no se confirmó?** Es la de los binarios, y
no es una tendencia con otro nombre: un nitrito no viene subiendo, porque no existe *«más
positivo»*. Cuenta cuántas pruebas hubo, cuántas dieron positivas y —la que cambia la
conducta— cuántas **seguidas** al final.

| Serie de sangre oculta en heces | Lectura | Qué pide |
|---|---|---|
| negativo → **positivo** | `apareció` | repetir el examen |
| negativo → positivo → **positivo** | `persiste` | **mostrárselo al médico** |
| **positivo** → negativo | `no confirmado` | nada |

Las dos primeras filas terminan en el **mismo** resultado —`positivo`— y piden cosas
distintas. `SangreEnHeces.SeveridadDelHallazgo` contesta lo mismo en las dos, y está bien:
una magnitud solo se ve a sí misma. La que distingue un dato suelto de un hecho confirmado
es la serie.

La tercera fila es la que más aporta y la menos obvia: **sin serie, un positivo aislado se
lee igual para siempre**. La hoja de aquel día dice «positivo» y sólo la segunda prueba
puede desmentirlo. En la sangre en orina eso significa bajar de *«buscá atención médica
pronto»* a *«fue pasajera»*, que es la diferencia entre asustar a alguien un año y no
hacerlo.

### Los permisos de la IA se fueron

Hubo una pantalla de **Ajustes** para apagar cada consulta de la IA por separado, con la
decisión journalizada. La idea era buena y el mecanismo funcionaba, pero la
responsabilidad no era nuestra: **quien se conecta con la IA es el Gnothi Host**, no esta
app. Édgar y Jonathan reportaron que además daba problemas al conectar.

Su reporte trae la receta, y vale conservarla porque es una propiedad del motor:

> el `upgrade('permisos-de-la-ia')` no asienta en la corrida que siembra el journal, así
> que la vista de solo lectura del MCP no ve el global.

O sea: un `upgrade` agregado en un segundo `PerformCommand` **no queda disponible para
otro proceso que rehidrate ese mismo journal**. Verificado en un journal real: `'semilla'`
y `'permisos-de-la-ia'` aparecen dos veces cada uno. Si alguna vez hace falta un upgrade
nuevo, hay que probarlo **abriendo el journal desde cero con el MCP**, no solo con la app
que lo sembró.

#### Si tenías un journal de antes

Los journals escritos antes de este cambio tienen adentro `permisos = PermisosDeLaIA();`,
y el replay lo vuelve a ejecutar. Como la clase ya no existe, en cada arranque van a
aparecer dos líneas así:

```
[Puppeteer ERROR] Rehydration execution failed. EntryId=2,
Cause: Class 'PermisosDeLaIA' is not registered in the actor's library
```

**El expediente abre igual y el estado queda correcto** — medido: los nueve exámenes, el
cuadro y las tres lecturas de serie salen bien. El replay no aborta: registra el error de
esa entrada y sigue con la siguiente. Es ruido, no pérdida.

Para que deje de aparecer, **se borra la carpeta del journal y se aprieta Refresh**. Se
reconstruye entero desde URL2, porque el journal no es fuente de verdad: es la copia local
de los eventos del laboratorio. Medido con `refrescar` contra el proveedor real.

> Esto vale **mientras** todo lo del expediente venga de URL2. El día que la persona
> aporte datos propios que el laboratorio no manda —altitud de residencia, medicación; los
> que prevé `01-enriquecimiento-dominio-examenes.md`— el journal deja de ser reconstruible
> y borrarlo pasa a ser pérdida de datos. Hoy no es el caso: hasta la edad está quemada en
> la semilla.

### Lo que ve la IA

Cuatro tools intencionales, una por pregunta — nunca un `Query(puppet, operacion, ...)`
genérico, que convertiría todo en strings y desperdiciaría el schema por tool:

| Tool | Contesta | ¿Está en alguna pantalla? |
|---|---|---|
| `evaluar_tendencia` | para dónde viene y si es creíble | sí, la app la muestra |
| `velocidad_de` | **a qué ritmo, sobre el tramo reciente** | **no** |
| `presencia_de` | **si un hallazgo apareció, persiste o no se confirmó** | sí, desde que los binarios tienen serie |
| `que_me_falta_medir` | **cuánto hace que no se mide cada analito, y si ya es demasiado** | **no, y no puede** |

`que_me_falta_medir` reemplazó a `revisar_mis_analitos`, que recorría los analitos y
devolvía la tendencia de cada uno — o sea, exactamente lo que la app renderiza. Su propia
descripción tenía que aclarar que *«no es la lista que la app ya le muestra»*, y cuando una
tool necesita defenderse de eso, ya se contestó sola.

La que la reemplaza contesta lo único que una pantalla **no puede**: una **ausencia**. Una
pantalla solo renderiza datos que tiene; *«hace cuatro años que no te medís el
colesterol»* no es un dato del expediente, es una conclusión sobre el expediente **más el
calendario más la periodicidad que cada analito define para sí mismo** — y esa
periodicidad no es fija: un colesterol en rango se repite cada cinco años, uno alto todos
los años.

La fecha entra **por parámetro**. El dominio no lee el reloj: si lo hiciera, el mismo
journal reproducido mañana daría otra respuesta. Saber qué día es hoy es derecho de la
superficie, y el servidor MCP es quien lo pone.

El argumento es un **enum**, no un string. Con un string la lista de valores válidos vivía
en la descripción —prosa que la IA tiene que leer bien— y con un enum vive en el
`inputSchema`, que es un contrato: el cliente no puede mandar un valor que no exista.
**Medido**: un analito inexistente se rechaza en la frontera, y los llamados válidos que
viajan junto a él no se ven afectados.

Y son **dos enums**, no uno: `Analito` para las tres primeras columnas de arriba y
`AnalitoBinario` para `presencia_de`. Las preguntas no son las mismas — con un enum único
la IA podría pedir `velocidad_de(SangreEnOrina)` y recibir una respuesta vacía que después
tendría que interpretar. Separados, el schema lo impide.

El dominio devuelve **hechos** —tendencia, extremos, unidad, días, ritmo— y nunca prosa. La
frase la arma la superficie. Es lo que hace **imposible** el consejo de plantilla: no hay de
dónde sacar *"bajá las grasas"*.

---

## Estado medido

Medido el **26 ago 2026**, sobre `master`.

| Capa | Estado |
|---|---|
| Dominio | ✅ **356 comprobaciones** (`comprobar`), incluidas las cuatro lecturas del guion, la velocidad, la confirmación de un pico, la escalada de la serie binaria y los tres efectos del sexo |
| Consola | ✅ ciclo completo: `comprobar`, `medir`, `estado`, `tendencia`, `velocidad`, `refrescar` |
| EndPointDeEventos | ✅ **13 eventos**, rotación de token y señuelo 403 |
| MCP | ✅ **medido por stdio**: las cuatro tools responden, `presencia_de(SangreEnHeces)` devuelve `positivasSeguidas: 2` con la acción correcta, y sobre un expediente de **mujer** `evaluar_tendencia(AntigenoProstatico)` contesta `noLeCorresponde: true` **sin devolver un valor** |
| PaginaDeDescarga | ✅ levanta con `docker compose up`; sirve la descarga en `/` y la app en `/app/` |
| Wasm | ✅ **corre completo** desde `3.0.0-beta.10029` — el bloqueo del motor se levantó |
| Navegador | ⚠️ la **carpeta propia** es solo Chromium. Safari y Firefox degradan a OPFS o a export/import — la app no se queda sin persistir |

El `medir` sobre el guion completo: **13 exámenes aplicados, 0 descartados, 0 rechazados**,
último evento `212`, **3** analitos con algo que mirar.

Son 3 y no 4 desde que el sexo es una variable del dominio, y esa resta es el cambio
entero en un número: el guion arranca con el sexo **desconocido**, y ahí el antígeno
prostático no cuenta como hallazgo porque todavía no se sabe si le corresponde a esta
persona. Declararlo lo devuelve a 4 —el mismo journal, sin recargar ni migrar nada— y es
lo que la última sección de `medir` imprime paso a paso.

### El bloqueo del motor se levantó en `10029`

Durante todo el desarrollo la carátula cargaba, renderizaba y moría al abrir el actor con
`PlatformNotSupportedException: Cannot wait on monitors`. **Con `3.0.0-beta.10029` ya no.**

Cómo se distinguió la versión que sí lo traía de la que no:

| | 10027 | 10028 | **10029** |
|---|---|---|---|
| referencias a `IsBrowser` | 0 | 0 | **1** |
| tamaño de `Puppeteer.dll` | 815 104 | 815 104 | **815 616** |
| bytes distintos vs. la anterior | — | 281 | **680 368** |

`10028` se anunció como el arreglo y era una **recompilación del mismo código** — mismo
tamaño exacto, 281 bytes de diferencia, que es el MVID y el timestamp del PE. `10029` es
código nuevo, y la prueba en el browser lo confirma.

**Medido corriendo**: la app abre el actor, se pone al día contra URL2, muestra los 13
exámenes, las siete lecturas de serie —cada una con su forma—, la cronología de visitas y
la pantalla de detalle.

---

## Pendientes de este negocio

| # | Pendiente |
|---|---|
| 1 | **Ningún umbral clínico está revisado.** Velocidades, periodicidades, rangos posibles, grados de anemia, la meta de 240, las severidades de los binarios — 81 números, todos marcados con ⚠️ en el código, ninguno visto por alguien con criterio de laboratorio. **Es el único que no podemos cerrar nosotros**, y hay una hoja que los junta a todos para que se revisen de una pasada |
| 2 | **Que la app enganche.** Álvaro pidió que *«hasta le entretenga»*. Lo que falta no son más gráficos: es **movimiento y recompensa** —ver la barra de meta avanzar, que el historial se sienta un logro—. Es conversación de producto con él mirando la pantalla |
| 3 | **Tras publicar de nuevo, hay que cerrar todas las pestañas del origen.** El service worker de Blazor no llama a `skipWaiting()`, así que la versión nueva se instala pero queda esperando hasta que no quede ninguna pestaña abierta de `localhost:8090`. Un cambio en el SW que "no se aplica" casi siempre es esto |
| 4 | **El workload `wasm-tools-net9` no está instalado**, así que el publish sale sin optimizar (57 MB). Funciona; conviene instalarlo antes de mostrarlo |
| 5 | **La marca ficticia** (*Laboratorio Clínico Delfos*) sigue siendo ficticia, pero ya no es un placeholder: sus especificaciones vienen del código real de `LaboratorioDelfos.module.css` en nCuboSite (ver `docs/iteration/05-adopcion-marca-delfos.md`). Vive solo en `PaginaDeDescarga/descarga/index.html` y en los assets de la carátula |
| 6 | **El `--pino` a baja opacidad se lee gris** en las bandas de la hemoglobina y el antígeno: la «zona segura» no se siente segura. Cosmético, chico, y parte del #2 |
| 7 | ~~**El expediente sólo carga la edad.**~~ **Cerrado.** El expediente carga un `Paciente` —edad **y sexo**— y el sexo entra por dos puertas con su procedencia: el evento `DatosDePacienteActualizados` de URL2 (INSTITUCIÓN) y el comando `DeclararSexo` de la app (TÚ), y para interpretar gana ella. Ver `docs/iteration/06-spec-sexo-dominio-examenes.md`. Lo que **sigue** abierto son los antecedentes, la medicación y la altitud de residencia: los tres entrarían hoy por `Paciente`, sin tocar ninguna firma |
