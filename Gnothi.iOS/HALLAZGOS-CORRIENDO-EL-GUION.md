# Corriendo `PRUEBAS-WEBVIEW-IOS.md` en una Mac — hallazgos para Edgar

**Fechas:** 2026-09-07 y 08. **Quién:** Renán, con Claude. **Rama:**
`fase0/modulos-hospedados-en-ios`. Sobre Xcode 26.6 y .NET 9.0.200.

**Dos aparatos, y cuál dice qué.** P0, P1, P3, P4, P5, P6 y P7 se midieron en un **simulador
iPhone 17 Pro / iOS 26.5**, y ahí valen: lo que prueban es comportamiento —contexto seguro, CSP,
journal, consentimiento— y eso no depende del silicio. **P2 se midió en un iPhone 13 Pro /
iOS 26.6.1 de verdad**, porque es memoria: el simulador no tiene presupuesto de jetsam y usa la
RAM de la Mac, así que ahí un número tranquilizador no prueba nada.

Etiquetas como en `Documentacion/Movil/00-INDICE.md`: **[MEDIDO]**, **[RAZONADO]**,
**[NO MEDIDO]**.

> El guion decía «NADA DE ESTO ESTÁ MEDIDO». Ya no: **las ocho pruebas están medidas.** P0, P1,
> P3, P4, P5, P6 y P7 en el simulador; **P2 en un iPhone 13 Pro de verdad**, que es el único
> aparato donde ese número significa algo. Lo que falta no son mediciones: son las dos cosas que
> le faltan a P6 para ser el camino de producción (§9) y el 404 del §10.

---

## Lo que pasó, en una línea cada uno

| | Estado | |
|---|---|---|
| **P0** contexto seguro | ✅ **[MEDIDO]** | `gnothi-mod://` **es** contexto seguro. El diseño no cambia |
| **P1a** el módulo se hospeda | ✅ **[MEDIDO]** | Arranca, sirve, hidrata y contesta. Tras dos bugs, corregidos |
| **P1b** el módulo se **ve** | ✅ **[MEDIDO]** | A pantalla entera, con una franja de Gnothi. Antes el botón no hacía nada (§2b) |
| **P4** journal y Keychain | ✅ **[MEDIDO]** | Después de un tercer bug, corregido |
| **P5** contención | ✅ **[MEDIDO]** | Con una corrección al guion: la directiva no es la que dice |
| **P3** el puente | ✅ **[MEDIDO]** | La consola del módulo vuelve por el puente |
| **P7** comprobación | ✅ **[MEDIDO]** | `matches:true` en los cuatro, con datos reales (§7) |
| **P6** el alta | ✅ **[MEDIDO]** | Por el esquema propio, con el descarte y la instalación (§9) |
| **P2** memoria | ✅ **[MEDIDO]** | En un iPhone 13 Pro. 527 MB con un módulo abierto (§10) |

---

## 1 · P0 — la pregunta que decidía todo, contestada

```
GNOTHI: cargas: P0 · secureContext=true crypto.subtle=true storage=true origin=gnothi-mod://cargas
```

**[MEDIDO]**, y en los cuatro módulos. **No hay que servir desde `127.0.0.1`**: no entran
puerto, ciclo de vida, ni perder la intercepción «antes de la red». `OriginFor` se queda como
está.

La sonda automática fue la decisión correcta y hay que decirlo: salió al log **antes** de que
los módulos arrancaran, así que estuvo contestada mientras todavía peleábamos con P1.

---

## 2 · P1 — dos bugs en cadena, con el mismo síntoma

Los dos daban **pantalla en blanco sin un solo error**, y por eso el primero tapaba al segundo.
Los dos están corregidos y commiteados en esta rama.

**a) `Localizar` componía `bin/web`** — `Gnothi.Movil/Shell.cs`. El molde era el de un módulo
cargo (`<dir>/bin/<dll>`) y se aplicaba a todos. Para uno hospedado, cuyo bundle vive en `web/`,
daba `<dir>/bin/web`: una ruta inexistente. `BaseOf` no encontraba el `index.html`, caía en su
`return "/"`, y el bundle publicado para `/app/` se servía en la raíz. **Y además se comía
`arguments[1]`** —la URL del publicador— porque reemplazaba el array entero.

**b) El base pedido sin la barra final** — `Gnothi.Movil/BundleServer.cs`. El origen es
`gnothi-mod://<módulo>/app/` y WebKit navega pidiendo **`/app`**, sin barra. El
`StartsWith(basePath)` no matcheaba, no se recortaba nada, y se buscaba `<bundle>/app`.

Con los dos arreglados **[MEDIDO]**: los cuatro módulos pasan de `base at /` a `base at /app/`,
sirven su documento, y arrancan su actor adentro del WebView:

```
GNOTHI: modulo:cargas: log: Starting Puppeteer.EventSourcing.ActorHandler's Actor
```

Esa línea cierra **P1a** y P3 juntas: **la consola del módulo vuelve por el puente**.

> **La lección, y es de proceso:** el `404` del `BundleServer` era silencioso a propósito —«un
> 404 es una respuesta»—. Cierto para el protocolo y **falso para diagnosticar**: hace
> indistinguible «no encuentro el archivo» de «la CSP lo rechazó», y las dos dejan la pantalla
> igual de blanca. Los dos bugs fueron invisibles hasta que ese 404 salió por el log, y después
> cada uno se leyó en un renglón. La línea quedó, y es la mitad que el vigía no cubre: él dice
> lo que se **rechazó**, ésta lo que **no está**.

---

## 2b · P1b — «Abrir» no hacía nada, y ya lo hace

El guion pide *«tocá su tarjeta y después «Abrir…». Qué tenés que ver: la UI de la institución a
pantalla completa»*. **Eso no ocurría en iOS**, y por eso vale contarlo separado de P1a: lo que
P1a prueba es que el módulo **se hospeda y contesta**, no que la persona pueda **verlo**. Ya está
implementado y medido; queda el diagnóstico porque explica por qué faltaba.

La causa está escrita en el propio código, en `Gnothi.iOS/ViewController.cs:242`:

```csharp
// TODO lo que la pagina pide lo atiende Shell.Handle, que es el MISMO archivo en las
// dos plataformas -- incluidos los dos verbos del alta, desde que la propuesta vive
// alla. iOS no intercepta nada: no tiene pantalla de modulo todavia.
completionHandler(host.shell?.Handle(defaultText) ?? "{}");
```

Y `Shell.Handle` **no tiene caso `"abrir"`**: cae en `_ => "{}"`. Por eso el log muestra
`GNOTHI: bridge: abrir` —se loguea en la línea 426, *antes* del switch— y después nada. El botón
llega al Host y el Host no tiene qué hacer con él. **[MEDIDO]**

En Android sí existe, y es corto — `MainActivity.cs:421` intercepta la op antes de delegar:

```csharp
"abrir" => host.AbrirModulo(req.GetProperty("module").GetString() ?? ""),
```

y `AbrirModulo` hace `anfitrion.Open(m, …)` y después `RunOnUiThread(() => Mostrar(vivo))`.

**Eso es lo que se implementó**, con los mismos tres nombres que Android. La vuelta es la única
pieza que Android no necesita —allá la da el botón atrás del sistema— y en iOS hubo que darla.
Costó **tres formas**, y las dos descartadas explican la que quedó:

1. **Un `‹ Gnothi` flotante.** Descartado medido: **le tapaba el logo y la primera letra a
   «Supermercado La Guaria»**. Un módulo full-bleed no deja lugar para cromo ajeno, ni arriba ni
   abajo, donde está su nav.
2. **Un sheet con grabber.** Descartado por criterio, no por defecto: el sheet *insinúa* que
   estás dentro de Gnothi —la app queda asomando detrás— y una franja lo *dice*. Y un botón con
   etiqueta le gana a un gesto: el grabber es estándar pero hay que descubrirlo, y «Listo» no.
3. **Pantalla entera con una franja de Gnothi arriba** — el patrón del in-app browser, que es lo
   que quedó. Decisión de Geaninna.

La franja dice *«<institución> · adentro de Gnothi»* con un **Listo** a la derecha, y el módulo
arranca **debajo** por constraint, así que la superposición de la forma 1 es imposible por
construcción. Lo que **no** se copia del patrón es la URL: acá el contenido lo sirve Gnothi desde
su propio contenedor y el origen es `gnothi-mod://<módulo>`, que no le dice nada a la persona —y
mostrarlo sería peor, porque parece una dirección y no lo es.

**[MEDIDO]** en el simulador: `compras` abre con sus datos reales —87 compras, ₡3 222 336, y el
aviso «Se está guardando en Gnothi» que solo sale hospedado—, el header de la institución queda
**entero y sin tapar** debajo de la franja, `Listo` deja `cerrar: 'compras' fuera de pantalla`, y
volver a abrirlo es instantáneo porque `Open` lo devuelve vivo.

> **La trampa que la pantalla obliga a resolver:** cerrar tiene que marcar el módulo como no
> visible. Si no, queda `Visible` para siempre — y `WebModuleHost.Evict` se niega a desalojar uno
> que se está mirando, así que Gnothi se quedaría con su motor de 20 MB en memoria sin nadie
> mirándolo. De ahí un `Soltar()` al que llaman todos los caminos de cierre, más un
> `UIAdaptivePresentationControllerDelegate` que lo dispara si el descarte no pasó por `Cerrar()`.
> Con la versión de sheet eso era obligatorio, porque el gesto no pasaba por ahí; se dejó puesto
> igual, para que agregar un cierre nuevo no reintroduzca la fuga.

> **[PENDIENTE DE GEANINNA]** La franja es la mínima a propósito, para que ella la ajuste sobre
> algo que ya corre; el alto está en `FranjaAlto`. Lo que hay que cuidar: el módulo **ya trae su
> propio header y su propia nav**, así que son dos encabezados apilados y si la franja engorda, la
> app de la institución parece encajonada. Y el nombre aparece dos veces con dos redacciones —la
> que Gnothi tiene registrada, «Mis compras», y la que la institución usa para sí misma,
> «Supermercado La Guaria»—, que no es un error pero es algo que ella va a querer resolver.

> **Y la lección de producto:** `ui.html` es el MISMO archivo en las dos plataformas, así que iOS
> dibujaba el botón igual sin tener con qué responderlo. Quien lo probaba concluía que estaba
> **roto**, no que **faltaba**. Cada vez que una op del protocolo se implemente en una sola
> plataforma, la cara compartida va a ofrecerla en las dos.

---

## 3 · P4 — el Keychain rompía el primer guardado SIEMPRE

`Gnothi.iOS/IosSecretStore.cs`. `SecItemUpdate` no admite `kSecClass` en su diccionario de
atributos-a-actualizar, y `SecRecord` no se puede construir sin una `SecKind`: la clase viaja
adentro siempre. Todo `Update` devolvía `NoSuchAttribute`, y como el único código tolerado antes
de caer al `Add` era `ItemNotFound`, tiraba.

**[MEDIDO]** antes: los cuatro módulos con
`the Keychain could not update 'gnothi.credential.<m>': NoSuchAttribute`, y los cuatro journals
en **0 B**.

**[MEDIDO]** después del arreglo — cero errores de Keychain, y los cuatro carteros pidiendo:

| módulo | pull | journal |
|---|---:|---:|
| examenes | 2 179 bytes | 8 K |
| compras | 73 019 bytes | 68 K |
| cargas | 4 076 bytes | 8 K |
| dealership | 7 916 bytes | 8 K |

**El `2179` de examenes es exactamente el número que pusiste en el `LEEME.txt` como medida tuya
en Android.** Mismo byte count, reproducido en iOS de forma independiente.

Tolerar el `NoSuchAttribute` no alcanzaba: en un *renewal* el item existe, el `Update` falla
igual y el `Add` posterior devuelve `DuplicateItem`. Por eso el orden quedó invertido — `Add`
primero, y si ya estaba, `Remove` + `Add`. Gana algo que no se buscaba: toda escritura pasa por
el registro completo, así que **`WhenUnlockedThisDeviceOnly` se reafirma en cada renovación**;
el `Update` no tocaba ese atributo.

**[NO MEDIDO]** la otra mitad que el archivo marca: que después de reinstalar no devuelva nada.

---

## 4 · P5 — el vigía funciona, y el guion espera la directiva equivocada

Tu vigía de `8bde23a` funcionó en su primera corrida real y encontró algo. **[MEDIDO]**:

```
GNOTHI: dealership: REFUSED by script-src-elem -- inline (from :34); 1 so far
```

El `:34` la identifica sin ambigüedad — es el registro del service worker:

```html
33    <script>
34        if ('serviceWorker' in navigator) {
35            navigator.serviceWorker.register('service-worker.js').catch(() => { });
```

**La denegación ocurre, pero un paso antes que en Android.** Allá el script corre y `worker-src`
bloquea al worker; acá `script-src-elem` bloquea el script de registro, así que nunca llega a
pedirlo. Mismo efecto, intercepción más temprana.

**Por eso `worker-src` no aparece nunca** — cero coincidencias en todo el log. El guion dice
*«tiene que aparecer al menos el `worker-src`»* y *«si en iOS no sale ninguno, el vigía no está
corriendo»*. **Ese criterio da un falso negativo en iOS** y conviene corregirlo: lo que hay que
buscar es una denegación del registro, sin fijar la directiva.

### El hilo que queda, y es tuyo

Corrí el mismo regex de `CspFor` sobre los cuatro bundles:

| módulo | scripts inline que encuentra | tamaño |
|---|---:|---|
| examenes | 1 | 54 chars (minificado, una línea) |
| compras | 1 | 54 chars |
| cargas | 1 | 54 chars |
| **dealership** | 1 | **143 chars (multilínea, indentado)** |

Los cuatro tienen el mismo script de registro y el hasher lo encuentra en los cuatro. **Pero
sólo el de dealership fue rechazado.** Si los otros tres pasaron, su hash coincidió y el script
**corrió** — y entonces `register()` se llamó y `worker-src 'none'` debería haber producido una
violación. No hay ninguna.

**[RAZONADO]** La explicación que mejor lo cubre: en WebKit, sobre un esquema propio,
`navigator.serviceWorker` no está expuesto, así que `'serviceWorker' in navigator` da `false`,
`register()` nunca se llama, y no hay nada que denegar. No sería contención más débil: sería que
la superficie no existe. **Se confirma con un renglón en el inspector de Safari:**
`'serviceWorker' in navigator`.

Lo que queda sin explicar —y es el hallazgo que te dejo— es **por qué el hash de dealership no
coincidió y el de los otros tres sí**, siendo el mismo script con distinto formato. O el hasher
o el bundle. El vigía es exactamente lo que lo hizo visible.

---

## 5 · Cosas que rompen y son de una línea

**`ConsentWatch` no corre en iOS.** `PlatformNotSupportedException: System.IO.FileSystem.Watcher
is not supported on this platform`. Es lo último de `Shell.Start()`, así que todo lo anterior ya
corrió y la app parece sana. Pero tu propio comentario dice para qué estaba: *«si la persona
apaga un módulo, la foto se reescribe SIN él… Sin esta línea quedaría una foto vieja contestando
algo que ella ya revocó»*. **En iOS esa línea no corre, así que el agujero de revocación está
abierto** y la carátula sigue contestando por un módulo apagado.

**El `paquete/` viejo sobrevive dentro del `.app`.** Aparté `Gnothi.Mobile/paquete/`, el script
avisó «NO hay paquete», y el `.app` igual salió con 128 archivos y 14 MB de una corrida
anterior: MSBuild no borra los `BundleResource` que ya copió. Mismo tipo de trampa que `b6edf0c`
arregló para la metadata.

**`404 /app/<Modulo>.Wasm.styles.css`** en los cuatro. El CSS con scope de Blazor no viaja en el
`web/` publicado. El módulo arranca; renderiza sin estilos.

---

## 6 · El escenario en la Mac

`scenario-ios.sh` levanta y baja bien, y **el `--stop` funciona** —el ramal que marcaste como no
ejercitado—: los ocho puertos abajo, sin procesos sobrevivientes. **[MEDIDO]**

Pero **las cuatro páginas de institución no levantan**, consistentemente. Los cuatro
publicadores sí. La causa está en su log:

```
error MSB3030: Could not copy the file
"PaginaDeLaInstitucion/obj/Debug/net9.0/apphost" because it was not found.
```

El `obj/` de `PaginaDeLaInstitucion` quedó incompleto y su `dotnet run` no compila. Un
`dotnet build` de ese proyecto —o borrarle el `obj/`— lo arregla. **Eso explica los cuatro
«flakes» que veníamos leyendo como sondeo de listo impreciso: no era el sondeo, esos servidores
nunca subieron.** Los carteros funcionaron igual porque piden al publicador, no a la página.
**P6 depende de la página**, así que hasta que eso se arregle no se puede correr.

Dos detalles menores del script: reporta «eight ports down» listando siete cuando uno era un
proceso preexistente que no estaba en su pidfile; y en un caso dijo `SCENARIO INCOMPLETE: 8
port(s) not answering` habiendo listado uno solo.

**CORRECCIÓN a lo que decía este reporte antes:** las páginas no levantaban por **dos** cosas, y
la segunda hacía parecer flaky a la primera. Una, el `obj/` de `PaginaDeLaInstitucion` estaba
incompleto —un `dotnet build` limpio de ese proyecto lo arregló—. Y dos, **la ventana de espera
del script es corta para esta máquina**: los ocho puertos suben, pero tardan más de lo que él
aguanta, así que reportaba `SCENARIO INCOMPLETE` sobre servidores que iban a contestar 40 s
después. Medido: con el `obj/` arreglado, a los 90 s están los ocho.

**Y no se puede compilar iOS mientras el escenario está arriba**: los dos tocan los mismos
proyectos compartidos y el publish muere. Con `armar-bundle.ps1` mandando el publish a
`| Out-Null`, el motivo se pierde y el log queda en 23 bytes diciendo «could not be built».
Bajar el escenario antes de compilar además libera ~2 GB, que en una Mac de 8 GB es la
diferencia entre que el paso 5 pase o agote sus 90 s.

---

## 7 · P7 — pasa, pero primero hay que sacarle el recibo al paquete

El paquete trae el `comprobado.json` de cada módulo —fechado el 2026-09-07 17:18, de cuando los
diste de alta en el emulador—, y `Shell.cs` hace `if (File.Exists(mark)) continue`. Gnothi ya
tiene el recibo y **no vuelve a comprobar**, así que de entrada P7 era inobservable.

No es un defecto del paquete: el paquete es **el resultado de un alta real**, que era justamente
su virtud. Para medirlo hay que borrar los cuatro recibos de `incoming` **y** del área privada
—el Host copia uno sobre el otro en cada arranque, así que borrar uno solo lo deja volver— y
relanzar. No hace falta recompilar.

Hecho eso, **[MEDIDO]**, los cuatro:

```
comprobación de cargas:     {"module":"cargas","matches":true,"declared":["lista_de_vehiculos","resumen"],"actual":[…],"undeclared":[],"declaredButMissing":[]}
comprobación de compras:    {"module":"compras","matches":true,…}
comprobación de dealership: {"module":"dealership","matches":true,…}
comprobación de examenes:   {"module":"examenes","matches":true,…}
```

Y cada uno contestó su `resumen` con datos de su journal real:

| módulo | lo que contestó |
|---|---|
| cargas | 9 cargas, 1 vehículo, último evento 219 |
| examenes | 11 exámenes, 0 analitos fuera de rango, último evento 210 |
| compras | ₡3 222 336 en 87 compras, último evento 387 |
| dealership | VIN `1HGCM82633A004352`, `anythingDue:true`, `dueCount:3` |

Los cuatro recibos se reescribieron solos al terminar.

> **Un cruce que vale la pena:** ese `3 222 336 en 87 compras` es **exactamente** lo que el mismo
> módulo contestó en el Host de escritorio, por MCP sobre stdio, en otra máquina y otra cáscara.
> Mismo dominio, mismo journal, dos hospedajes distintos, misma respuesta. Es la propiedad que el
> proyecto persigue —un artefacto, varios hospedajes— medida de punta a punta.

---

## 8 · Del ambiente, para el próximo que lo monte en una Mac

- **El workload pide Xcode 26.5 y acá hay 26.6.** `armar-intents.sh` ya lo saltea solo con
  `-p:ValidateXcodeVersion=false`. Un `dotnet build` a secas, no.
- **`dotnet build` a secas NO da «la app de siempre»**, como dice el README. `ModelFoundation.cs`
  declara los tres `DllImport("__Internal", gnothi_fm_*)` **sin condición** y los define
  `Intents/Puente.swift`; sin la `.a`, el link muere con `Undefined symbols for architecture
  arm64`. La `NativeReference` está condicionada, el C# no. En Windows no se nota porque nunca
  linkea. **Hay que correr `armar-intents.sh` primero.**
- **PowerShell ya no es un cask**: `brew install powershell` (fórmula). Y arrastra `dotnet`
  10.0.400 como dependencia, que en `/opt/homebrew/bin` **tapa** al .NET 9 con el workload de
  iOS. `brew unlink dotnet` lo saca del PATH y deja el Cellar, que es lo que `pwsh` usa. **Si
  alguien corre `brew link dotnet` o un `brew upgrade`, el build de iOS se rompe** con «workload
  no instalado».
- El `README.md` de iOS se contradice sobre la sonda AOT: arriba dice que ya corrió y pasó en un
  iPhone 13 Pro; abajo, en «Lo que NO está verificado», dice que nadie la corrió. Y desde el
  rename de `8d9ffa1` la línea 214 nombra `Sonda.cs`, que ahora es `Probe.cs`.

**La sonda AOT [MEDIDO] en el simulador**, de paso: `Supported=True / Compiled=False`, y los
cinco pasos en verde, con el 2 fallando igual que en Android y resolviéndose por vecindad.

**Y Foundation Models genera en el simulador** —`puente: estado = 2 (genera)`—, eligiendo la
tool y su argumento solo. Una nota que no es de plomería: en una corrida contestó *«Esto es un
signo de alerta y requiere atención médica inmediata»* sobre el colesterol de Sonia, y en otra,
con el mismo prompt, la versión sobria. Las instrucciones del módulo prohíben diagnosticar. **El
modelo del teléfono no las está respetando de forma estable.**

---

## 9 · P6 — el alta desde el enlace, medida

**[MEDIDO]** con el esquema propio de `9668ea4`, en el simulador. La corrida entera:

```
alta: bajando el bundle de 127.0.0.1
alta: propuesta de 'examenes' servida por 127.0.0.1
alta: 'examenes' descartado por la persona          ← «Ahora no»
alta: 'examenes' instalado en …/Data/Application/…  ← «Instalar en mi teléfono»
alta: 'examenes' quedó instalado; 1 módulos
```

La hoja de consentimiento trae **las cuatro cosas** que el guion pide: qué va a poder contestar
(las dos tools con su descripción), dónde va a vivir el registro (la ruta completa, más *«Es
tuyo. Gnothi se lo administra a esta institución, no se lo lleva.»*), con quién va a hablar
(`http://127.0.0.1:8095/expediente/1`, *«Es la única red que va a poder tocar»*) y quién sirvió
el bundle.

**El descarte deja el aparato como estaba, y es la aserción fuerte:** con «Ahora no» no existe
`Modulos/` ni `journals/`. Verificado en el contenedor, no solo en el log.

**Y aceptar encadena todo lo demás solo:** base en `/app/`, P0 en verde, credencial, `pull
after=0 -> 2223 bytes`, y la comprobación de P7 con `matches:true`.

> **Regalo de `cb46c30`:** en el camino aparecieron `the publisher rejected the credential;
> asking for another` y `Gnothi obtained its credential from the publisher`. O sea que el
> publicador que ahora revoca ejercitó el **re-grant**, y el arreglo del Keychain (§3) lo
> aguantó. Eso cierra la mitad que `IosSecretStore` tenía anotada como no medida.

### Las dos cosas que le faltan para ser el camino de verdad

**1. El esquema propio pide una confirmación, y eso lo hace no-automatizable.** iOS muestra
*«Open in "Gnothi Host"?»* con Cancel/Open, y **alguien tiene que tocar Open**. Un
`xcrun simctl openurl` a secas deja el diálogo esperando: la app no arranca, no aparece **nada**
en el log, y parece que el esquema no funciona. Perdí un rato ahí. Lo que lo destapó fue mandar
un enlace deliberadamente inválido y ver que `HandleLink` tampoco lo logueaba — o sea que el
problema estaba antes de la app, no adentro.

**2. La página de la institución todavía enlaza al universal link.**
`PaginaDeLaInstitucion` sirve `href="https://go.gnothi.app/alta?bundle=…"`, no `gnothi://`. Así
que **el botón «Agregar a Gnothi» sigue sin poder funcionar**: `9668ea4` le puso el esquema a la
app y la página no se enteró. El esquema hoy solo se alcanza tipeándolo o por `simctl`. Es un
cambio de una línea en la página, y sin él P6 se mide pero no se demuestra.

---

## 10 · P2 — cuánto pesa un módulo, en un iPhone de verdad

**[MEDIDO]** el 2026-09-08 en un **iPhone 13 Pro, iOS 26.6.1**, con los cuatro módulos del
paquete instalados. Es la única prueba que exige aparato: el simulador no tiene presupuesto de
jetsam, así que ahí un número tranquilizador no prueba nada.

| estado | app | WebKit | **total** |
|---|---:|---:|---:|
| arranque en frío (siembra 998 archivos + verifica los 4) | 458.5 | 54.2 | **512.7** |
| cuatro instalados, estabilizado, sin abrir ninguno a mano | 187.1 | 181.3 | **368.4** |
| **un módulo abierto y pintando** | 376.6 | 150.8 | **527.3** |

Contra Android —216–248 MB de PSS en aparato, 199 MB con cuatro instalados y uno vivo— **iOS
pide más del doble**. La comparación es aproximada porque Android reporta PSS y esto es
`phys_footprint`, que es lo que mira el jetsam; pero el orden de magnitud no se explica por la
diferencia de métrica.

### El asterisco, y es grande

Es un build **Debug** con `MtouchInterpreter=-all`: el intérprete de Mono cargado y sin linkear.
**527 MB es un techo, no la cifra de producción.** Release con `MtouchLink=SdkOnly` falta medir y
va a ser menor. Aun así el veredicto que el guion pide se activa: con ese techo un módulo entra
en un teléfono de 6 GB, y **cuatro módulos abiertos a la vez no**. La conversación sobre el
sustrato compartido queda abierta con un número atrás.

### Dos cosas que el total no dice

**El gauge de Xcode habría mentido por casi la mitad.** En el estado estabilizado la app son
187 MB y sus procesos de WebKit **181 MB**: mitad y mitad. `WKWebView` corre el contenido en
procesos aparte, así que el footprint de la app **no incluye** el motor de 20 MB del módulo.
Quien mida P2 con el gauge ve 187 y concluye que entra cómodo.

**Abrir el módulo mueve el costo de lado, no solo lo suma.** De 368 a 527 MB, pero el reparto se
invierte: la app pasa de 187 a 377 y WebKit **baja** de 181 a 151. Abrir un módulo libera los
WebViews que la verificación había dejado vivos y engorda el proceso de la app. No era lo
esperado, y es exactamente la razón por la que P2 pedía medir en vez de suponer.

### El método, porque sin él los números no se pueden repetir

Plantilla **`Activity Monitor`** de Instruments —no `Allocations`—, porque da memoria **por
proceso de todo el aparato** y es la única forma de capturar los `WebContent`:

```bash
xcrun xctrace record --device <UDID> --template 'Activity Monitor' \
  --all-processes --output x.trace --time-limit 12s
xcrun xctrace export --input x.trace \
  --xpath '/trace-toc/run[@number="1"]/data/table[@schema="sysmon-process"]'
```

**La atribución es el problema difícil, no la grabación.** El aparato tiene ~450 procesos y
docenas de `WebContent` de otras apps; ninguna tabla del trace trae el pid padre. Dos criterios
que hay que cruzar, y uno solo no alcanza:

- **filtrar por pid ≥ el de la app** — un hijo nace después. Solo, **se pasa de largo**: recoge
  demonios de iOS que arrancaron en el medio (medido: AirDropUI, translationd, homeenergyd,
  SharingUIService sumaban ~90 MB ajenos).
- **comparar contra una muestra con la app apagada** — lo que desapareció era suyo. Solo,
  **se pasa también**: un proceso ausente de una muestra de 12 s no está muerto, está no
  muestreado (medido: el driver de Wi-Fi, pid 113, apareció como «de Gnothi» con 341 MB).

Cruzados, dan los nueve procesos de la tabla. Ese es el número defendible.

### Lo que costó llegar, para que el próximo no lo pague

- **Instruments necesita el aparato «online», y `devicectl` lo tira.** Lanzar la app con
  `devicectl device process launch` deja el teléfono en `Devices Offline` para `xctrace`
  mientras esa app viva: el `record` **sale con éxito y el trace tiene cero procesos**, sin un
  solo error. La app hay que abrirla **desde el ícono**.
- **`system_profiler SPUSBDataType` no lista el iPhone.** Costó dos cambios de cable y uno de
  puerto perseguir un problema que no existía. Lo que sí lo ve es IOKit:
  `ioreg -p IOUSB -l | grep -i iPhone` mostró el aparato `registered, matched, active` todo el
  tiempo. **Y lo que destrabó la sesión fue abrir Xcode** en Window → Devices and Simulators.
- **`sysdiagnose` no sirve de atajo**: falla con `DiagnoseError error 0`, y aunque funcionara
  barre nombre, ubicación de dos días, correo e IPs para leer tres números.
- **`devicectl device info processes` no trae memoria**: solo ejecutable y pid.

### Y del build de aparato, que tampoco estaba andado

- **`armar-intents.sh` solo produce la `.a` de simulador** (`TRIPLE=...-simulator`), así que el
  build de aparato muere con `ld: building for 'iOS', but linking in object file built for
  'iOS-simulator'`. Hay que compilarla con `-sdk iphoneos -target arm64-apple-ios16.0`. El
  script debería saber para qué plataforma arma.
- **El perfil de un equipo personal no puede llevar App Groups ni associated-domains**, y firmar
  pidiendo lo que el perfil no concede **falla**, no degrada. De ahí `EntitlementsDevice.plist`,
  que es `Entitlements.plist` menos esas dos. No se pierde nada medible: la foto local ya existe
  y el universal link no funciona igual sin dominio.
- **Foundation Models da `estado = 0` en el 13 Pro.** El A15 no lo tiene —pide A17 Pro— y en el
  simulador daba 2. El diseño de tres estados hizo su trabajo: sin él, un `if isAvailable`
  habría pasado en el simulador y roto en el teléfono.

### El 404 en aparato: era la contención haciendo su trabajo

**[MEDIDO]** en el iPhone. El módulo abierto mostraba
`refrescar contra el proveedor → HttpRequestException: 404, Not Found`, y el log daba la causa
inmediata:

```
GNOTHI: compras: 404 /app/eventos/device/token -> …/Modulos/compras/web/eventos/device/token
```

El módulo pide `eventos/device/token` con una URL **relativa** —así lo declara su
`appsettings.json`: `"Proveedor": { "BaseUrl": "eventos/" }`—, así que resuelve contra
`gnothi-mod://<módulo>/app/` y **nunca sale del origen del módulo**: la intercepta el scheme
handler y devuelve 404.

**La cadena completa**, que costó dos hipótesis equivocadas antes de dar con ella:

1. El cartero no alcanza al publicador —el bundle llevaba `127.0.0.1`, y en un teléfono eso ES el
   teléfono— así que el journal queda vacío.
2. El módulo se abre, **no encuentra datos**, y corre su propio «refrescar contra el proveedor».
3. Ese refresco pide credencial: `ProveedorDeToken` → POST relativo a `eventos/device/token`.
4. El scheme handler lo contesta 404, y el módulo lo muestra.

**Rearmado el bundle con la IP de la Mac, el paso 2 no ocurre y el 404 desaparece.** Medido:
`pull after=0 -> 73063 bytes` —el mismo número que en el simulador—, el módulo muestra sus 87
compras y ₡3 222 336, y en el log no queda rastro de `eventos/device/token`.

#### Lo que sostiene la medición, y NADA MAS

Tres estados, ejercitados:

| estado | qué hace el módulo | resultado |
|---|---|---|
| journal vacío, al abrir | refresca solo | 404 → caja de error |
| journal lleno, al abrir | no refresca | sin error |
| journal lleno, **tocando «Buscar compras nuevas»** | refresca | **404 → caja de error** |

**El refresco del módulo NUNCA usa el puente.** Siempre va por su propio HTTP a `eventos/…`, que
hospedado está interceptado. Los datos en pantalla —87 compras, ₡3 222 336— los trajo el cartero
del Host al arrancar; el módulo no los pidió.

**El hueco es real y es de producto.** Hospedado, «Buscar compras nuevas» no puede funcionar
nunca, y la persona no tiene forma de pedir datos nuevos desde adentro del módulo: el único que
los trae es el cartero, y eso ocurre solo al arrancar.

**El arreglo es del módulo, no del Host, y está acotado:** cuando `hospedada()` sea true, el
refresco tiene que llamar a `gnothiPuente.pull` en vez de a su `HttpClient`. Proxear `eventos/`
desde el Host sería el arreglo equivocado — significaría conseguirle una credencial al módulo, que
es justo lo que el diseño le niega.

**Y el 404 en sí no es el defecto: es la contención funcionando.** El módulo intenta una ruta que
hospedado no le corresponde y la intercepción de origen se la niega. Lo defectuoso es el mensaje,
que le echa la culpa al proveedor cuando la puerta está cerrada por diseño.

> **TRES CONCLUSIONES MÍAS SOBRE ESTE MISMO 404, Y LAS DOS PRIMERAS ESTABAN MAL.** Quedan acá
> porque el patrón del error importa más que el error.
>
> 1. «Hay que rearmar los bundles con la IP de la LAN» → y después «eso no arregla nada, porque en
>    ese camino no hay salida a la red». Lo segundo era falso: rearmarlos **sí** repara el paso 1,
>    y así el refresco automático no corre.
> 2. «No hay hueco en el módulo», al ver que el 404 desaparecía con el journal lleno. Concluido sin
>    haber tocado el botón de refresco, que era lo obvio que faltaba ejercitar.
> 3. La de arriba, que se sostiene en los tres estados de la tabla.
>
> **La lección es de método:** dos de las tres salieron de mirar un síntoma que aparecía o
> desaparecía, en vez de ejercitar deliberadamente cada camino de la UI. Un síntoma ausente no es
> un camino probado.

### Y lo que hace falta para correr el guion contra un aparato

**Dos claves distintas en el `Info.plist`, y confundirlas cuesta caro.**

| clave | de qué es | qué pasa sin ella |
|---|---|---|
| `NSAllowsLocalNetworking` | ATS: texto claro hacia la red local | la conexión se bloquea por política de transporte |
| `NSLocalNetworkUsageDescription` | privacidad: iOS le pregunta a la persona | **deniega en silencio y el error dice «The Internet connection appears to be offline»** |

Ese mensaje es el que hace daño: manda a revisar el wifi y el router cuando lo que falta es un
permiso que nunca se pidió. **[MEDIDO]**: con la primera puesta y la segunda no, el alta falló
exactamente así. Con las dos, iOS muestra su diálogo y el alta pasa.

**Y el permiso no rige para la petición que lo dispara**: esa se deniega igual. Hay que reintentar,
y conviene relanzar la app.

**Los bundles hay que rearmarlos con la IP de la Mac** —`armar-bundle.ps1 -Publicador
"http://<IP>:<puerto>/"`— porque el manifiesto lleva el publicador ADENTRO y el módulo no lo puede
cambiar. Se comprueba en la hoja de consentimiento: «Con quién va a hablar» tiene que decir la IP,
no `127.0.0.1`.

**Y el alta en aparato no se puede disparar por línea de comandos.** `simctl openurl` es solo
simulador y `devicectl` no tiene `open`. Lo práctico es servir una página con el enlace
`gnothi://alta?bundle=…` y tocarla desde Safari: tipear esa URL en un teléfono es inviable.

Nada de esto aplica en producción, donde el publicador es un servidor real por HTTPS. Las dos
claves del `Info.plist` salen antes de publicar, y así está anotado ahí.
