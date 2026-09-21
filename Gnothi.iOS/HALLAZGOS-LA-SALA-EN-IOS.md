# La sala en iOS — lo medido el 2026-09-21

**Quién:** Renán, con Claude. **Rama:** `fase0/la-sala-sustrato-en-el-motor-web`.
**Aparato:** simulador **iPhone 17 Pro / iOS 26.5**. **Sobre** Xcode 27.0 y .NET 9.0.200, con
`-p:ValidateXcodeVersion=false` (ver `Documentacion` y el punto 11 de las trampas de la máquina:
no hay workload de .NET 9 que acepte Xcode 27).

Etiquetas como en `Documentacion/Movil/00-INDICE.md`: **[MEDIDO]**, **[RAZONADO]**, **[NO MEDIDO]**.

> **Lo que hay que llevarse, en dos renglones.** La sala **corre** en iOS y un módulo real carga y
> contesta por ella. Y hay **una decisión pendiente de equipo**: el arranque del documento vive
> dentro del presupuesto por verbo de la cáscara, y cuando no entra, el arreglo actual empeora el
> problema en vez de recuperarlo (§4). Eso es código **compartido con Android**.

---

## Lo que pasó, en una línea cada uno

| | Estado | |
|---|---|---|
| **contexto seguro** bajo `gnothi-sala://` | ✅ **[MEDIDO]** | los tres `true`. No hace falta 127.0.0.1 |
| **`fetch` contra el esquema propio** | ✅ **[MEDIDO]** | `200 basic`. Era la que de verdad decidía (§2) |
| **el sustrato arranca** | ✅ **[MEDIDO]** | «la sala esta lista, hospedada por Gnothi» |
| **un módulo real carga** | ✅ **[MEDIDO]** | `examenes`, 506 ms, motor prestado, 2 consultas y 7 vistas |
| **la comprobación pasa** | ✅ **[MEDIDO]** | `matches:true`, y **en la primera instalación** (§3) |
| **la frontera entre journals** | ✅ **[MEDIDO]** | `"vecinos":[]`, `"retenidos":0` |
| **una consulta contesta con dato** | ⛔ **[NO MEDIDO]** | el dominio tiró por dato sembrado viejo (§3b) |
| **el arranque entra en el tope** | ⛔ **[FALLA]** | y el descarte lo retroalimenta (§4) |
| **memoria** | ⛔ **[NO MEDIDO]** | pide Instruments y un iPhone de verdad (§5) |

---

## 1 · Cómo se armó la corrida

El `.gmod` que estaba en `bundles/` es **hospedado** y no le sirve a la sala: su dominio viaja
dentro del wasm, no como `.dll`. Hubo que armar la forma híbrida:

```
pwsh -NoProfile -File Gnothi.Mobile/armar-bundle.ps1 -Modulo examenes -Forma hibrida
  -> bundles/hibrida/examenes.gmod · 0,2 MB · dominio/Examenes.App.dll + Dominio + Eventos
     el motor NO viaja: lo presta el Host
```

Y después, con el simulador arrancado:

```bash
# el mock del laboratorio, en el puerto que el manifiesto declara
dotnet run --project Examenes/EndPointDeEventos --no-launch-profile --urls http://0.0.0.0:8095
# el paquete, servido
cd Gnothi.Mobile/bundles/hibrida && python3 -m http.server 8096 --bind 127.0.0.1
# el interruptor de medición, DENTRO del contenedor de la app
echo examenes > "$(xcrun simctl get_app_container booted com.ncubo.gnothi.host data)/Library/Application Support/sala"
# y el alta
xcrun simctl openurl booted "gnothi://alta?bundle=http://127.0.0.1:8096/examenes.gmod"
```

**`--no-launch-profile` no es opcional:** sin eso `launchSettings.json` pisa el puerto y el mock
queda en 8091 mientras el manifiesto apunta a 8095. El síntoma es un alta que instala bien y
después no trae nada.

---

## 2 · La pregunta que decidía la forma no era el contexto seguro

**[MEDIDO]**, dos corridas:

```
sala: P0 · secureContext=true crypto.subtle=true storage=true wasm=true wasm.streaming=true
           origin=gnothi-sala://sala
sala: P0 · fetch(mismo origen)=200 basic
```

Sin un 404 y sin un rechazo de CSP.

**El renglón que importa es el segundo.** Es fácil creer que P0 de la forma hospedada
(`HALLAZGOS-CORRIENDO-EL-GUION.md` §1) ya lo cubría, y no: esa forma **nunca hace `fetch` por su
origen** — sus subrecursos los pide el documento, y leer, escribir y pull van por el puente. El
sustrato no puede evitarlo: Blazor baja `_framework/*` con `fetch`, y `Sala.cs` pide los `.dll` del
módulo y su journal con `HttpClient`, que en wasm **es** `fetch`. Si WebKit no entregara el esquema
propio a `fetch`, la sala no arrancaría aunque el origen fuera seguro, y el plan B sería un
servidor local en `127.0.0.1` con su puerto y su ciclo de vida.

Lo entrega. **`OriginFor` se queda como está** y se sigue interceptando antes de la red.

---

## 3 · El módulo carga, y la comprobación pasa en la primera instalación

**[MEDIDO]**, con la sala ya caliente:

```
alta: 'examenes' quedó instalado; 1 módulos
sala: examenes preparado desde .../Modulos/examenes/dominio
sala/cargar examenes: {"ok":true,"ms":506,"superficie":"ExamenesSurface","borde":"prestado",
  "consultas":["resumen","lista_de_examenes"],
  "vistas":["cabecera","examenes","serie","analitos","tendencia","historial","alertas"],
  "montado":{"archivos":2,"bytes":2068},
  "barrido":{"carpetas":0,"bytes":0,"vaciadas":true,"borradas":0},"retenidos":0,"vecinos":[]}
comprobación de examenes: {"matches":true,"declared":["lista_de_examenes","resumen"],
                           "actual":["lista_de_examenes","resumen"]}
```

Tres cosas que vale separar:

- **506 ms para cargar** el `.dll` de la institución en el runtime que ya estaba vivo, con el
  motor prestado (`"borde":"prestado"`).
- **`"vecinos":[]` y `"retenidos":0`** — la frontera entre journals se sostiene. Es la condición
  que `SalaShell.Vecindario` convierte en error, y acá reportó cero bytes.
- **`matches:true` en la PRIMERA instalación.** En la forma hospedada eso era imposible en este
  mismo simulador: el wasm del módulo tardaba ~64 s en frío y la comprobación se rendía a los 60
  (`HALLAZGOS-CORRIENDO-EL-GUION.md` §11.4). Con la sala el runtime ya está arrancado, así que lo
  único que falta cargar son los 200 KB del módulo.

### 3b · Y después tiró, por dato sembrado y no por la sala

`resumen` llegó hasta `ExamenesSurface.Resumen()` y el dominio tiró:

```
Puppeteer.LanguageException: Upgrade 'semilla' differs from the one previously applied to this actor.
  Applied:             { expediente = Expediente(60); bitacora = Bitacora(); }
  Attempting to apply: { expediente = Expediente(34); bitacora = Bitacora(); }
```

El journal venía de `Documents/incoming/journals/examenes`, sembrado desde el `paquete/` del
bundle; lo escribió un build de `Examenes` cuya `semilla` usaba `Expediente(60)`, y el `.dll` recién
compilado lleva `Expediente(34)`. **No es un hallazgo de la sala**: es el fixture desincronizado
del código. Pero conviene dejarlo escrito, porque el `paquete/` que viaja en el `.app` va a volver
a hacerlo — y además `paquete/Modulos/` trae `examenes/` y `facturas/` **vacíos**, así que sin un
alta real el Host arranca con «0 modules installed».

**Que una consulta conteste con dato real sigue [NO MEDIDO] en iOS.**

---

## 4 · LO QUE HAY QUE DECIDIR: el arranque no entra en el tope, y el descarte lo empeora

Al borrar el journal y relanzar —o sea, en el camino normal de una app con el módulo **ya
instalado**— la sala **no contesta nunca**. **[MEDIDO]**:

```
09:43:32.903  sala: creando el documento
09:43:53.811  sala: se tira el documento -- 'examenes' no contesto a 'cargar'
09:43:54.219  sala: creando el documento
09:44:04.228  sala: se tira el documento -- 'examenes' no contesto a 'cargar'
09:44:06.218  sala: creando el documento
09:44:16.288  sala: se tira el documento -- 'examenes' no contesto a 'cargar'
09:44:16.427  sala: creando el documento
09:44:26.488  sala: se tira el documento -- 'examenes' no contesto a 'cargar'
09:44:35.292  sala: NO arranco -- TimeoutException: The operation has timed out.
```

Cuatro ciclos y se rindió.

**El mecanismo.** `SalaShell.Pedir` envuelve **cada verbo** con `SalaShell.Tope`, que son 10 s. El
arranque del documento corre **adentro** del primer verbo, porque `IosSala.Pedir` —igual que
`AndroidSala.Pedir`— empieza con `await Arrancar(ct)`. Y el arranque del documento, medido cuatro
veces en este mismo simulador:

| | |
|---|---|
| app ociosa, sin módulos instalados | **3,84 s** y **4,18 s** |
| app ocupada en su propio arranque (sonda AOT, sonda del modelo, instantánea) | **14,6 s** |

Cuando pasa de 10, el `Descartar` tira el documento — **y el reintento vuelve a arrancar en frío**.
Se retroalimenta: nunca hay un intento que herede un documento tibio.

**Por qué la corrida del §3 sí anduvo:** ahí la sala se había calentado sola dos minutos y medio
antes del alta (el interruptor vacío la arranca sin módulos), así que `cargar` llegó a un documento
ya vivo. Eso midió la ruta del calentamiento, no la que importa.

**Esto NO es del simulador.** El mismo código está en Android, donde no se ve porque el sustrato
arranca en 439 ms en un A54 (`Spikes/Lab.Sala/LEEME.md`). Un aparato lento, un arranque en frío
después de que el sistema mate la app, o un sustrato que engorde, lo despiertan allá igual.

### La forma del arreglo que se propone, para discutir

`Descartar` existe por una razón buena y escrita: **un módulo puede colgar su propio desalojo** —
adentro corre su `Dispose()`, código que eligió la institución, y un ciclo infinito no se
interrumpe desde adentro de un motor web de un solo hilo. Tirar el documento es la única salida, y
es honesta porque lo que el módulo tenía montado vive en su memoria.

**Pero un documento que todavía no cargó ningún módulo no puede estar colgado en código de la
institución.** Ahí no hay nada que la institución haya elegido corriendo: es el sustrato de Gnothi
arrancando. Aplicarle el descarte no recupera nada y garantiza que el siguiente intento sea peor.

Dos formas, y las dos tocan **código compartido** (`Gnothi.MobileShared/SalaShell.cs` y el contrato
`ISalaHost`), así que la decisión no es de una plataforma:

1. **Sacar el arranque del presupuesto por verbo.** `Tope` mide lo que el módulo tarda en contestar,
   no lo que el motor tarda en existir. Pide separar «esperar a que la sala esté» de «pedirle algo».
2. **No descartar un documento que todavía está arrancando.** Más chico, y conserva `Tope` como
   está; el descarte sigue aplicando en cuanto haya un módulo cargado, que es el caso que lo
   motivó.

Lo que **no** conviene es subir `Tope`: acomoda el contrato al aparato más lento que se haya
probado, y no arregla la retroalimentación — solo la hace menos frecuente.

---

## 5 · Lo que queda sin medir

- **Una consulta que conteste con dato real** (§3b). Pide un journal sembrado que corresponda al
  `.dll`, o dejar que el cartero traiga los eventos del mock.
- **`vista`, `entregar`, `cursor` y `desalojar`** por la sala en iOS. Solo se ejercitaron `cargar`
  y `repertorio`.
- **El cruce entre dos módulos**, que es donde se paga el cambio de ranura (~200 a 450 ms medidos
  en Android). Hace falta un segundo bundle híbrido.
- **Memoria.** En iOS el motor web corre en `com.apple.WebKit.WebContent`, o sea **otro proceso**:
  medirla pide Instruments contando los dos, no un `dumpsys`. Y el simulador no tiene presupuesto
  de jetsam, así que el número solo significa algo en un iPhone de verdad.
- **Lo de Apple sigue abierto y no lo contesta un aparato.** `B11` deja sin saber si wasm o un IL
  corrido por el intérprete de Mono cuentan como *interpreted code*. Eso lo contesta una revisión,
  no una corrida. La prueba dice si funciona, no si pasa.
