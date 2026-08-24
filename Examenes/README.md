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
dotnet publish Examenes/App/Examenes.Wasm -c Release -o Examenes/PaginaDeDescarga/publicado -p:PublishTrimmed=false
```

```bash
cd Examenes && ./PaginaDeDescarga/generar-certificado.sh && docker compose up -d
```

Y queda en **`http://localhost:8090`** — la app — y **`http://localhost:8090/descarga`** — la
página branded de la institución.

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
| Wasm | ⚠️ **arranca y renderiza**, y falla al abrir el actor — ver abajo |

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
| 2 | **Dónde va la página branded.** Hoy nginx sirve la app en `/` y la página en `/descarga`. Ponerla en la raíz obliga a cambiar el `<base href>` del publish y le quita al service worker el scope de `/`. Decisión de equipo |
| 3 | **La instalación de la PWA no se pudo confirmar de punta a punta.** nginx sirve todo lo que hace falta (`Service-Worker-Allowed: /`, `application/manifest+json`, `application/wasm`), pero el registro del service worker solo se probó en un panel de vista previa que los bloquea — un SW de tres líneas falla ahí igual. Falta confirmarlo en un Chrome de verdad |
| 4 | **El workload `wasm-tools-net9` no está instalado**, así que el publish sale sin optimizar (57 MB). Funciona; conviene instalarlo antes de mostrarlo |
| 5 | **La marca ficticia** (*Laboratorios Sotavento*) es un placeholder. La definitiva la define el equipo. Vive solo en `PaginaDeDescarga/descarga/index.html` y en los assets de la carátula |
| 6 | **La jerarquía por examen vs. por analito** sigue sin decidirse con Álvaro. Este dominio cuelga del **examen**, que es lo que él pidió textualmente, y la taxonomía de `Magnitud` cubre buena parte de lo que motivaba la otra |
