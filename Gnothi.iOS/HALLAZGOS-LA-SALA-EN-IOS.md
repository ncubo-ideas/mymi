# La sala en iOS — lo medido el 2026-09-21

**Quién:** Renán, con Claude. **Rama:** `fase0/la-sala-sustrato-en-el-motor-web`.
**Aparato:** simulador **iPhone 17 Pro / iOS 26.5**. **Sobre** Xcode 27.0 y .NET 9.0.200, con
`-p:ValidateXcodeVersion=false` (ver `Documentacion` y el punto 11 de las trampas de la máquina:
no hay workload de .NET 9 que acepte Xcode 27).

Etiquetas como en `Documentacion/Movil/00-INDICE.md`: **[MEDIDO]**, **[RAZONADO]**, **[NO MEDIDO]**.

> **Lo que hay que llevarse, en dos renglones.** La sala **corre** en iOS y un módulo real carga y
> contesta por ella, con el arranque del documento en **~0,9 s** y `cargar` en **53 a 102 ms**.
> Queda anotada una **fragilidad latente**, no una falla: el arranque vive dentro del presupuesto
> por verbo de la cáscara, así que si algún día pasa de 10 s, el arreglo actual empeora el problema
> en vez de recuperarlo (§4). Es código **compartido con Android**.

---

## Lo que pasó, en una línea cada uno

| | Estado | |
|---|---|---|
| **contexto seguro** bajo `gnothi-sala://` | ✅ **[MEDIDO]** | los tres `true`. No hace falta 127.0.0.1 |
| **`fetch` contra el esquema propio** | ✅ **[MEDIDO]** | `200 basic`. Era la que de verdad decidía (§2) |
| **el sustrato arranca** | ✅ **[MEDIDO]** | «la sala esta lista, hospedada por Gnothi» |
| **un módulo real carga** | ✅ **[MEDIDO]** | `examenes`, 506 ms, motor prestado, 2 consultas y 7 vistas |
| **la comprobación pasa** | ✅ **[MEDIDO]** | `matches:true`, y **en la primera instalación** (§3) |
| **la frontera entre journals** | ✅ **[MEDIDO]** | con DOS módulos y en los dos sentidos (§8) |
| **la cara abre y pinta dato real** | ✅ **[MEDIDO]** | `vista`, `entregar` y `guardar` por la sala (§6) |
| **el arranque entra en el tope** | ✅ **[MEDIDO]** | ~0,9 s contra 10 s. Con la Mac cargada, no (§4) |
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

## 4 · El arranque vive dentro del tope, y con la Mac cargada eso se rompe

> **CORRECCION DEL 2026-09-21, MISMO DIA.** La primera version de este apartado decia que el
> arranque **no entra** en el tope y lo daba por falla del diseño. **Estaba mal como titular.** Lo
> que se habia medido era una Mac con procesos `Gnothi.Host` desbocados comiendose la memoria;
> Renán los mató, y con la máquina sana los mismos pasos dan otra cosa. Se deja el error escrito
> porque la conclusión equivocada ya estaba commiteada, y porque la lección —medir la máquina
> antes de atribuirle una falla al diseño— es más cara que el hallazgo.

### Lo medido con la máquina sana **[MEDIDO]**

Cinco relanzamientos seguidos, con el módulo **ya instalado**, que es el camino normal:

| corrida | arranque del documento | `cargar` | descartes |
|---|---|---|---|
| 1 | 1,41 s | 64 ms | 0 |
| 2 | 0,88 s | 83 ms | 0 |
| 3 | 0,92 s | 102 ms | 0 |
| 4 | 0,92 s | 79 ms | 0 |
| 5 | 0,86 s | 53 ms | 0 |

Contra los 10 s de `SalaShell.Tope` hay **un orden de magnitud de aire**. Y `cargar` bajó de los
506 ms de la primera corrida a 53-102 ms, por el mismo motivo.

### Lo medido con la máquina cargada **[MEDIDO]**, y por qué se deja anotado

Antes de que esos procesos murieran, los mismos pasos daban:

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

Cuatro ciclos y se rindió, con arranques de 3,84 s, 4,18 s y 14,6 s en distintos momentos.

**La atribución es inferencia, no medición.** No se registró el estado de la máquina en ese
momento; lo que hay es la correlación —procesos desbocados antes, sanos después— y un mecanismo
plausible. Nadie midió CPU ni memoria mientras fallaba.

### La fragilidad que SÍ es del diseño, y es lo que va a la discusión

Independiente de qué tan rápida sea la máquina, esto es cierto por construcción:

- `SalaShell.Pedir` envuelve **cada verbo** con `Tope`, y `Arrancar` corre **adentro** del primero,
  porque `Pedir` de las dos salas empieza con `await Arrancar(ct)`. O sea que el presupuesto que
  existe para medir **lo que tarda el módulo** está pagando también **lo que tarda el motor en
  existir**.
- Si el arranque pasa de 10 s, `Descartar` tira el documento — **y el reintento arranca en frío**.
  No hay un intento que herede un documento tibio, así que el modo de falla es estable, no
  transitorio. Eso es lo que se vio arriba.

`Descartar` existe por una razón buena y escrita: un módulo puede colgar su propio desalojo, que
es código que eligió la institución. **Pero un documento que todavía no cargó ningún módulo no
puede estar colgado en eso** — es el sustrato de Gnothi arrancando —, así que descartarlo no
recupera nada.

Dos formas, las dos sobre **código compartido** (`Gnothi.MobileShared/SalaShell.cs` y el contrato
`ISalaHost`):

1. **No descartar un documento que todavía está arrancando.** Chica, conserva `Tope` como está, y
   el descarte sigue aplicando en cuanto haya un módulo cargado, que es el caso que lo motivó.
2. **Sacar el arranque del presupuesto por verbo.** De fondo: separar «esperar a que la sala esté»
   de «pedirle algo».

Subir `Tope` no es salida: acomoda el contrato al aparato más lento probado y deja la
retroalimentación en pie.

**Con qué urgencia.** Con la máquina sana hay 10x de aire, así que esto **no bloquea**. Es deuda
conocida, y el día que aparezca —un aparato lento, un arranque en frío después de que el sistema
mate la app, un sustrato que engorde— va a aparecer como «la sala no contesta nunca», que es un
síntoma caro de diagnosticar si no está escrito.

---

## 6 · La cara abre, y con ella cayeron `vista`, `entregar` y `guardar`

**[MEDIDO] el 2026-09-21**, tocando «Open» en la tarjeta de Delfos. Costó dos bugs de
`Localizar` que se cuentan en el §7; con los dos puestos, la corrida entera:

```
11:29:04.824  examenes: the bundle declares its base at /
11:29:06.868  examenes: P0 · secureContext=true crypto.subtle=true storage=true origin=gnothi-mod://examenes
11:29:07.441  abrir: 'examenes' en pantalla
11:29:11.641  examenes: the publisher rejected the credential; asking for another
11:29:12.959  examenes: Gnothi obtained its credential from the publisher
11:29:13.411  examenes: pull after=0 -> 4278 bytes
11:29:13.854  sala-web: log: Starting Puppeteer.EventSourcing.ActorHandler's Actor
11:29:14.318  sala/guardar examenes: examenes/meta.bin · 48 bytes
11:29:14.348  sala/guardar examenes: examenes/journal/journal_000001.bin · 4520 bytes
11:29:14.350  examenes: entrega a la superficie -> "leidos=18 aplicados=18 descartados=0 rechazados=0 ultimoEvento=217 cuenta=migrated"
```

Nueve segundos y medio de punta a punta, y casi todo es la credencial y la red — el trabajo de la
sala son los ~500 ms del final.

Cuatro cosas quedan medidas de un saque, y conviene separarlas:

- **`vista`.** La pantalla pinta con dato: «17 de tus 18 valores están en su rango», colesterol
  total 212 mg/dL con su `182 → 212`, cetonas negativo en 2 pruebas, y 9 analitos en «merece
  atención». Eso sale de los `[View]` de la superficie, que corre adentro de la sala.
- **`entregar`.** `leidos=18 aplicados=18 descartados=0 rechazados=0` — el sobre que el cartero
  bajó entró por `SalaShell.Deliver` y la superficie lo asentó entero.
- **`guardar`, o sea el viaje de vuelta.** Adentro de la sala no hay disco: el módulo escribió en
  memoria y lo escrito volvió por la respuesta a la carpeta de la persona. 48 bytes de `meta.bin`
  y 4.520 del primer segmento del journal, anclados por el anfitrión.
- **El actor arranca DENTRO de la sala**, y se ve en el prefijo: `sala-web:` es la consola del
  sustrato, no la del módulo hospedado.

**Ojo con una diferencia de forma que confunde:** la CARA se sirve por `gnothi-mod://examenes` —la
hospedada— y lo que corre por la sala es la SUPERFICIE. Son los dos WebView de la híbrida, y el
log los distingue por el prefijo.

**Un dato para Examenes, que no es de la sala:** la cara dice «17 de tus 18», que es lo que promete
la landing. Hay una nota vieja que decía que el dominio daba 6 sobre el mismo guion sembrado; con
este journal da 17, así que esa discrepancia conviene volver a mirarla antes de darla por viva.

---

## 7 · Los dos bugs de `Localizar`, que solo muerden en iOS

`Localizar` reescribe las rutas absolutas del `installation.json` porque el contenedor de iOS lleva
un GUID que cambia en cada instalación. Se escribió cuando había **dos** formas, y las que traen
cara **y** dll llegaron después.

**a) `bin/web`.** La condición preguntaba `Type == "webview"`, así que `hybrid` y `sala` caían en la
rama de cargo:

```
examenes: 404 / -> .../Modulos/examenes/bin/web/index.html
```

Es la reincidencia exacta del arreglo de `HALLAZGOS-CORRIENDO-EL-GUION.md` §2a, que cubrió la forma
que existía ese día. Ahora se enumeran las que guardan su cara al lado —`webview`, `hybrid`,
`sala`—.

> **Y no se escribió al revés.** El primer intento fue «todo lo que no sea cargo», y **la forma
> cargo no se llama `"cargo"` en ese campo: se llama `"in-process"`**. Con la cadena equivocada la
> condición se invierte en silencio y rompe justo la única que se quería dejar quieta.

**b) `Arguments[2]` no se reescribía nunca.** El comentario justificaba pasar el resto tal cual
porque el **segundo** es la URL del publicador —cierto—, pero el **tercero** es el dll, y en la
híbrida y la sala vive en el contenedor igual que el `web/`:

```
arg0: .../Application/A2854ED3-.../Modulos/examenes/web              <- reescrito
arg2: .../Application/F7787259-.../Modulos/examenes/dominio/...dll   <- contenedor ANTERIOR

sala: 500 modulo/examenes/indice.json -- DirectoryNotFoundException
sala/cargar examenes: {"ok":false,"motivo":"HttpRequestException: TypeError: Load failed"}
```

**Ninguno de los dos se arregla reinstalando.** `hoja` sale de `Path.GetFileName`, así que una vez
compuesto `bin/web` vuelve a dar `"web"` y se recompone igual. El alta deja las rutas **bien** y
este método las rompía en el arranque siguiente.

---

## 8 · El desalojo, con dos módulos y en los dos sentidos

**Esto es lo que la sala existe para sostener**, y hasta acá estaba medido con UN solo módulo
cargado — donde no hay nada que filtrar y la prueba no puede fallar. Con `examenes` y `compras` los
dos forzados a la sala (`echo examenes,compras > .../Library/Application Support/sala`):

### Ida — `examenes` sale, `compras` entra **[MEDIDO]**

```
11:46:22.011  sala/desalojar examenes: {"ok":true,"vaciada":true,"cerrado":"IDisposable",
              "desmontada":"vaciada: 3 archivos, 4708 bytes que el proximo modulo ya no ve"}
11:46:22.015  sala/guardar examenes: examenes/meta.bin · 48 bytes
11:46:22.016  sala/guardar examenes: examenes/journal/journal_000001.bin · 4630 bytes
11:46:22.017  sala/guardar examenes: examenes/index/index.bin · 30 bytes
11:46:22.017  sala: examenes soltado, ya no se sirve por el origen
11:46:22.098  sala/cargar compras: ok · 71 ms · ComprasSurface · borde prestado
              montado {archivos:0, bytes:0} · retenidos 1 · vecinos []
```

El orden importa y es el que el diseño promete: **primero se guarda lo que el módulo escribió,
después se suelta la ranura, y recién entonces entra el siguiente.** Si se guardara después, lo
asentado se evaporaría al vaciarle la carpeta.

### Vuelta — `compras` sale, `examenes` vuelve **[MEDIDO]**

```
11:47:55.138  sala/desalojar compras: {"ok":true,"vaciada":true,
              "desmontada":"vaciada: 0 archivos, 0 bytes que el proximo modulo ya no ve",
              "escrituras":[]}
11:47:55.216  sala/cargar examenes: ok · 56 ms · montado {archivos:3, bytes:4708}
              barrido {carpetas:1, bytes:0, vaciadas:true, borradas:1} · retenidos 2 · vecinos []
```

**`vecinos: []` en las dos cargas.** Ninguno de los dos ve un byte del otro, y el `barrido` dice que
lo que quedó de la carga anterior fueron carpetas de **cero bytes** — el residuo conocido y acotado
que `SalaShell.Vecindario` deja pasar a propósito.

**Y el journal sobrevivió al desalojo:** `examenes` vuelve con sus 3 archivos y 4.708 bytes, los
mismos que se vaciaron al salir.

### De regalo, `cursor` **[MEDIDO]**

```
11:47:55.305  examenes: la cara pidio desde 0; la superficie va en 217
11:47:55.700  examenes: pull after=217 -> 159 bytes
```

La superficie recordó dónde iba **a través del desalojo**, así que el cartero pidió 159 bytes en vez
de bajar el historial entero. Eso es `SalaShell.Cursor` andando, y es lo que hace que un refresco no
cueste lo mismo que una instalación.

### Lo que NO es gratis, y conviene mirarlo

`retenidos` pasó de 1 a 2. Es el número de cascarones que el sustrato retiene **a propósito**: si
los soltara, su finalizador despertaría a escribir en una carpeta borrada y eso mata el runtime
—medido en Android, «.NET runtime already exited with 255»—. El precio es memoria, y crece con cada
carga de la sesión, no con cada módulo instalado. **En iOS eso todavía no se midió** (§5): el motor
corre en otro proceso y pide Instruments.

---

## 5 · Lo que queda sin medir

- **El COSTO del cruce.** El cambio de ranura está medido en Android (~200 a 450 ms) y acá se vio
  que un `cargar` sale en 56 a 71 ms con el documento tibio, pero nadie cronometró una pregunta que
  toque los dos módulos en el mismo turno, que es el caso que paga.
- **Memoria.** En iOS el motor web corre en `com.apple.WebKit.WebContent`, o sea **otro proceso**:
  medirla pide Instruments contando los dos, no un `dumpsys`. Y el simulador no tiene presupuesto
  de jetsam, así que el número solo significa algo en un iPhone de verdad.
- **Lo de Apple sigue abierto y no lo contesta un aparato.** `B11` deja sin saber si wasm o un IL
  corrido por el intérprete de Mono cuentan como *interpreted code*. Eso lo contesta una revisión,
  no una corrida. La prueba dice si funciona, no si pasa.

---

## 9 · Cuando traigas `master` a este branch — ensayado el 2026-09-21

Se hizo el merge **en seco**, se resolvió, se compiló y se abortó. Nada de esto está commiteado;
lo que sigue es para que la vez de verdad no cueste dos veces. Eran **101 commits** de master,
casi todos la ola de localización.

**Hay UN conflicto, y es trivial: la lista de exclusiones de `Gnothi.iOS.csproj`.**

La resolución es la **unión**, no «gana el mío». En master alguien pegó con el mismo bug de los
stubs y arregló la mitad que existía allá:

```
0031cd8 fix(ios): el glob no excluia el stub del hibrido, y master no compilaba
```

No podía excluir `SalaShellStub` porque **`master` no tiene ni `Gnothi.Host/SalaShellStub.cs` ni
`Gnothi.MobileShared/SalaShell.cs`** — la sala vive solo acá. Comprobado: la línea de master es
exactamente la de este branch menos esa entrada, y en el árbol fusionado existen los dos stubs,
así que hacen falta las dos exclusiones. Quedarse con la de master deja iOS sin compilar, con el
`CS0101` sobre `SalaShell`.

**`master` NO toca `Localizar`.** Es lo primero que se revisó, porque `Gnothi.MobileShared/Shell.cs`
sí aparece en el merge y es donde vive el arreglo de `89db2ae`. Git lo fusiona limpio y además es
correcto: lo que master cambia ahí es la identidad del proveedor local y el encuadre localizado, en
otras regiones del archivo. **Un merge limpio no es prueba de nada; esto sí se miró a mano.**

**El guardián de CI nuevo pasa.** Master agrega `Herramientas/revisar-las-caras.mjs` como Guardián 3
del pipeline —verifica que cada cara híbrida no se desvíe de su app suelta—. Corrido sobre el árbol
fusionado: `TODO OK`, exit 0.

**Lo que SÍ va a morder, y no lo dice el mensaje: hay que regenerar `libGnothiIntents.a`.** Master
agrega la función nativa `gnothi_fm_razon` (`DllImport` en `BridgeProbe.cs`, `@_cdecl` en
`Intents/Puente.swift`), y como `Intents/out/` está gitignoreado la librería no viaja con el commit
que la necesita:

```
ld: Undefined symbols for architecture arm64:
      "_gnothi_fm_razon", referenced from: <initial-undefines>
```

El C# compiló bien; es solo la `.a`. La receta está en `README.md` §3b. Con eso puesto, el merge
compila: **`Build succeeded`**.

**Y para volver a medir después del merge:** `armar-bundle.ps1` cambió (+22 líneas), así que los
bundles híbridos de `examenes` y `compras` que produjeron los números de este reporte quedan
viejos — no llevan la localización nueva. Rearmarlos antes de repetir el guion.
