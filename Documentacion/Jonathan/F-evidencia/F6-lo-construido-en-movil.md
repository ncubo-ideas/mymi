# F6 · Lo que se construyó en móvil

**El Host móvil en sí lo documenta Edgar.** Esta página es lo otro: las cuatro piezas que se
escribieron encima, y el resultado de la corrida que las prueba.

La tesis que se fue a probar cabe en una línea: **la app que una institución ya publicó para el
navegador —la misma, sin recompilar— corre adentro de Gnothi, sin red propia, y escribe el journal
en los archivos de Gnothi.** Cuatro instituciones, cuatro módulos, el mismo binario en los dos
modos.

| módulo | institución | consultas |
|---|---|---|
| `examenes` | Laboratorio Clínico Delfos | `resumen`, `lista_de_examenes` |
| `compras` | Supermercado La Guaria | `resumen`, `lista_de_compras` |
| `cargas` | Estaciones Faro | `resumen`, `lista_de_vehiculos` |
| `dealership` | Sotaventos Motors (en inglés) | `resumen`, `hasta_donde_leyo` |

---

## 1 · El alta del módulo, con permiso

Llega como llega de verdad: **alguien toca un botón en la página de su institución**. Ese enlace
abre Gnothi con la URL del bundle.

Y son **dos actos, no uno**. Antes, tocar el enlace instalaba. Ahora `Examinar` baja el zip, lo
verifica y **no escribe nada**; dibuja una hoja que dice quién sirvió el bundle, qué consultas trae
y dónde va a vivir el journal. Recién `Aceptar` instala. La otra opción es **«Ahora no»**, y se
verificó que deja el aparato **sin rastro**: ni `files/Modulos` ni `files/journals` llegan a
existir.

**Lo que esa pantalla todavía no dice** es la consecuencia: que la app suelta de esa institución va
a dejar de sincronizar. Es una decisión de diseño abierta, no un olvido — está en `F2` como P11.

## 2 · El cartero

El módulo **no tiene red**. El pull del feed del publicador lo hace Gnothi, con **su propia
credencial**, contra un endpoint **fijo, registrado en la instalación y no elegible en runtime**; le
pasa el payload al módulo como parámetro y el módulo asienta. Es la invariante 2 de `01` puesta en
Android sin cambiarle una coma: *el Host es cartero, no escriba*.

La garantía no es una promesa, es la forma: **Gnothi no tiene ninguna llamada con la que escribir
una entrada**.

## 3 · La credencial de Gnothi, fuera del journal

Hasta el **2026-09-03** el device key y los dos tokens eran estado del dominio y **se
journalizaban** — o sea que viajaban en claro con cualquier copia de la historia de la persona.
Salieron. Ahora los guarda **quien hospeda**: `localStorage` del origen cuando la app corre suelta,
el Keystore de Gnothi cuando corre hospedada, y memoria que muere con el proceso en consola. El
módulo hospedado **no ve ningún token**.

Es P5 de `F2`, y es el prerrequisito que `13` pedía antes de tocar la transición.

## 4 · El WebView que hospeda la PWA

El módulo se sirve en un **host https virtual propio**, `https://{modulo}.gnothi.local{base}`,
interceptado **antes de la red**. Tres detalles que no son adorno:

- **https y no un esquema propio.** La primera versión usaba `gnothi-mod://` y hubo que cambiarla:
  no es contexto seguro, y sin contexto seguro se caen el almacenamiento del origen y
  `crypto.subtle`. El módulo ni arranca.
- **El `base href` se lee del bundle**, no se configura. Si el origen y la base no coinciden, todo
  da 404 y la pantalla sale en blanco sin un solo error.
- **La CSP se sirve por documento**, con el hash del script inline calculado al servir:
  `connect-src 'self'`, `frame-src 'none'`, `worker-src 'none'`.

El puente va en dos direcciones y **no devuelve nada**: recibe un id de pedido y contesta después.
Es feo a propósito — la interfaz nativa de Android es síncrona y congelaría la pantalla en un pull.
Esa misma forma es la que va a servir en iOS.

---

## El resultado de la corrida

**2026-09-04, de punta a punta**, sobre un emulador `sdk_gphone64_x86_64` (Android 16 / SDK 36):

| | examenes | compras | cargas | dealership |
|---|:-:|:-:|:-:|:-:|
| la app suelta corre y consigue credencial | ✅ | ✅ | ✅ | ✅ |
| el alta desde la página de la institución | ✅ | ✅ | ✅ | ✅ |
| el módulo hospedado, a pantalla completa | ✅ | ✅ | ✅ | ✅ |
| el cartero releva el pull | ✅ | ✅ | ✅ | ✅ |
| el journal en los archivos de Gnothi, **sin credencial adentro** | ✅ | ✅ | ✅ | ✅ |
| las consultas del módulo llegan al Host | ✅ | ✅ | ✅ | ✅ |
| la marca de migración | ✅ | ✅ | ✅ | ✅ |

**En el Xiaomi 14T Pro físico, el 2026-09-03:** la contención y la memoria — con Exámenes.

**Los números.** Pull inicial por institución: **6 673 · 72 928 · 3 985 · 7 825 bytes**; la segunda
vuelta de Compras dio `after=387 → 24 bytes`, el cursor avanzado y nada nuevo. Credencial en el
journal: **cero coincidencias en los cuatro**, con el control legible dando 6, 2, 4 y 6 — o sea que
el grep sabía buscar. Memoria con el módulo abierto: **216–248 MB** en el físico, **157–160** en el
emulador, **199** con los cuatro instalados y uno vivo. Bundles de **12,1 · 13,7 · 12,0 · 13,3 MB**.

**Y una que se ejecuta sola.** El PWA de Blazor intenta registrar su service worker en cada carga y
la política se lo niega **sin que nadie prepare nada** (`worker-src 'none'`, visto en logcat el
2026-09-07). Prueba con cero montaje que la CSP llega al módulo y que el motor la aplica. La
denegación es benigna: en la misma corrida `resumen` contestó igual.

---

## Las tres cosas que hay que leer al lado de esto

- **El guion completo**, con las nueve pruebas, sus comandos y 14 capturas:
  [`Gnothi.Mobile/PRUEBAS-WEBVIEW.md`](../../../Gnothi.Mobile/PRUEBAS-WEBVIEW.md). Es lo que se
  corre para reproducir la tabla.
- **Lo que falta y lo que está abierto:** [`F2`](F2-estado-y-pendientes.md), P9 a P12.
- **La especificación contra la que se construyó:** `11`, `12` y `13` en
  [`../../Especificacion/`](../../Especificacion/00-INDICE.md).

**Dos precisiones para no leer la tabla de más.** El **traspaso** de aserciones existe **solo en
Exámenes** — no confundirlo con la marca de migración, que sí está en los cuatro; cuáles aserciones
viajan es una decisión por institución. Y la **pantalla de consentimiento** se corrió en
**Compras**: el código es de Gnothi y es el mismo para los cuatro, pero los otros tres se dieron de
alta antes de que existiera.
