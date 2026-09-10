# C8 · El journal que escribe otro

**Primer approach del camino A4, construido el 2026-09-08.** Lo medido que lo sostiene está en `F1`;
la decisión y sus condiciones, en `F2` (P13). Esto es cómo quedó el código.

## El caso

La app de la institución corre **afuera** de Gnothi —en el navegador, como la persona ya la usa— y
escribe su journal en una carpeta que **ella eligió** con el picker. El módulo que Gnothi carga lee
**ese** registro.

**Y no puede leerlo donde está.** Medido: una concesión de árbol del SAF **no llega a `System.IO`** —
sobre la ruta deducida del tree URI, `EnumerateFiles` devuelve **cero archivos sin excepción** y
`File.OpenRead` da `UnauthorizedAccessException`. El motor solo sabe abrir una ruta. Así que Gnothi
**copia** la carpeta a la que le concedió al módulo, y el módulo lee la copia. La copia no es un
rodeo: es la única forma.

**Lo que este camino NO necesita:** ningún cambio en el motor. Ni un puerto de archivos ni un modo
lector — el módulo abre la copia, que está del lado privado de Gnothi, donde `System.IO` sí llega.

## Las tres piezas

| | archivo | qué hace |
|---|---|---|
| **el cartero** | `Gnothi.Mobile/FileCourier.cs` | reconoce, sella y copia. No decide cuándo |
| **el disparador** | `Gnothi.Mobile/ExternalJournals.cs` | decide **cuándo** y **para quién** |
| **el contrato** | `Gnothi.Host/Installation.cs` → `externalSource` | de dónde se llena la carpeta concedida |

`FileCourier` es **hermano de `GnothiCourier`**: aquel trae los eventos del publicador por la red,
este trae los archivos que otro dejó en una carpeta del aparato. Ninguno de los dos abre el sobre.

### 1 · Reconocer — la huella es intrínseca, y la institución dice cuál es la suya

El SAF **no verifica nada**: la persona puede conceder cualquier carpeta, incluida una vacía o la de
otra institución. Copiar a ciegas y descubrirlo al abrir es el peor orden posible.

**La institución no tiene que plantar ninguna marca.** Un journal del motor es `<actor>/meta.bin`
cuyos cuatro primeros bytes son el mágico **`PPMM`**, con `journal/` al lado.

**Pero eso solo dice «esto es *un* journal».** Para que diga «esto es *el* journal de esta
institución», el manifiesto declara **`actor`** — el nombre del actor cuyo registro lee el módulo. Va
en el manifiesto y no en la instalación porque *«mi diario se llama así»* es **identidad**, y la
identidad la declara quien la tiene.

| el manifiesto declara | qué hace el cartero |
|---|---|
| `actor` correcto | copia |
| `actor` de otra institución | **no copia, y dice por qué** |
| sin `actor` | acepta el único journal que haya; si hay varios, **no elige: lo dice** |

Esa última fila importa: elegir uno sería adivinar de quién es el registro de una persona, y ésa es
justo la clase de decisión que este proyecto no toma en silencio.

### 2 · Sellar — y por qué no se detecta el candado

La condición que se pidió era *«si alguien tiene tomados los archivos, no copiar»*. **Resultó
innecesaria.** Medido en el Chrome real: la escritura por File System Access es **atómica al
`close()`** — el archivo mantuvo el contenido viejo los 28 s que el writable estuvo abierto y cambió
de golpe al cerrar. Una copia tomada en cualquier instante obtiene la versión anterior **completa** o
la nueva **completa**, nunca una partida. Detectar el candado sería una carrera, y además sobra.

**El sellado —listar, copiar, listar— se justifica por otra cosa:** la atomicidad es **por archivo,
no por conjunto**. Copiar entre la escritura de `journal_N.bin` y la de `meta.bin` da un conjunto
coherente pero desactualizado. Como `meta.bin` e `index.bin` son caché reconstruible y el journal es
append-only, eso queda **viejo**, no **roto**.

### 3 · Copiar — a un lado, y cambiar al final

Se copia a `<destino>.copiando` y se mueve al terminar. Si la copia se corta a la mitad —la app
muere, se acaba el espacio— el módulo se queda con la copia **anterior entera** en vez de con media
copia nueva. Es la misma disciplina que Chrome aplica al escribir.

**Un detalle que encaja solo:** la copia preserva la carpeta del actor, y el motor compone
`path=<grantedFolder>` + nombre del actor. Así que `grantedFolder` apunta al padre y las dos
convenciones coinciden sin traducción.

## Cuándo se refresca

**Al entrar a Gnothi, en segundo plano.**

Y el «al entrar» no es comodidad: un módulo de este tipo **no se abre**. Es código que contesta
consultas, sin pantalla propia, así que **no hay un "cuando lo abre" del que colgarse**. Entrar a la
app es el único momento natural que queda.

**En segundo plano por un número:** refrescar cuesta ~1,3 s con un journal de 4 MB y ~7 s con uno de
120 MB, y lo caro no es copiar sino que el motor vuelva a abrir. Ponerlo en el camino de dibujar la
pantalla sería cobrarle a la persona un dato que quizá no vaya a pedir.

Va en `MainActivity.Start()`, **después** de `shell.Run()` —hace falta el catálogo— y **sin `await`**.

## El módulo puede venir dentro de la PWA

**Y entra por la misma puerta que ya existe.** `ModuleEnrollment` baja una URL, rechaza zip-slip,
descomprime, lee el manifiesto y escribe la instalación. **Un `.gmod` es un zip con `manifest.json`
adentro, igual que el bundle web.** Así que la institución pone un botón *«Importar a Gnothi»* en su
app suelta, apuntando a `go.gnothi.app/alta?bundle=…/compras.gmod`, y no hace falta un camino nuevo.

**Cómo se distingue una forma de la otra:** por **lo que el zip trae**, no por un campo que diga cómo
quiere ser cargado. Si hay `web/` → WebView; si el manifiesto declara `load.entry` y no hay `web/` →
in-process. Declarar la propia cáscara sería que el módulo elija cómo lo ejecutan, y eso es del Host;
`load.entry` no es esa declaración — es *«cuál de mis DLLs soy»*, que sí es identidad.

**Lo que gana además del gesto único:** el módulo y la app quedan **versionados juntos, por el mismo
publicador**. Hoy son dos artefactos que alguien tiene que mantener en sincronía.

**HECHO en la app suelta de Exámenes** (`EstadoDeLaApp.EnlaceParaImportarAGnothi` + el bloque de
`MisExamenes.razor`): el botón aparece cuando hay carpeta y esta instalación todavía no se retiró.
El `.gmod` se declara **relativo** en `appsettings.json` —lo publica la institución en su propio
origen, al lado de la app— y se vuelve absoluto contra `navegacion.BaseUri`, porque el host cambia
entre el laboratorio, la prueba y el túnel del emulador, y el módulo siempre está al lado.

**Y el texto no habla de mudarse.** Dice que el expediente **no se mueve ni se copia a ninguna nube**:
se queda donde está y Gnothi lo lee desde ahí. Es la corrección de §10.6 aplicada de entrada, en vez
de tener que arreglarla después.

**Y el enlace puede traer algo que Gnothi no tiene forma de saber:** cómo se llama la carpeta que la
persona eligió. La app suelta **sí sabe el nombre** —`carpeta.name` está disponible en JS; lo que no
existe es la ruta—, así que lo manda en `?carpeta=…` y Gnothi lo guarda en `externalFolderName`.
Sirve para decirle *«elegí la carpeta llamada X»* en vez de mandarla a adivinar. **Es una ayuda, no
una garantía**: un nombre se cambia. Lo que ata la carpeta a su institución es el `actor`.

**Lo que esto NO resuelve, y conviene no confundirlo:**

- **La tienda.** El problema no es cómo llega el código, es qué es: IL nativo cargado por
  `AssemblyLoadContext` está prohibido venga por donde venga. Meterlo dentro de la PWA no lo cambia.
- **El segundo gesto.** La persona igual tiene que elegir la carpeta en el picker de Gnothi: la PWA
  no puede pasarle el handle a otra app. Es el mismo muro que originó todo esto.

## El contrato

```json
{
  "shell": { "type": "in-process", "arguments": [ "…/Externa.dll" ] },
  "grantedFolder": "…/files/journals/externa",
  "externalSource": "content://com.android.externalstorage.documents/tree/primary%3ADocuments%2Fpwa-journal",
  "externalFolderName": "Mis compras",
  "enabled": true
}
```

> ⚠️ **TRAMPA, y es de las que tumban lo que ya andaba.** Un campo NUEVO del contrato llega **`null`**
> cuando la instalación no lo trae, **aunque la propiedad se declare `= ""`**. Y ninguna instalación
> escrita antes de que el campo existiera lo trae. La primera versión de esto hacía
> `m.Installation.ExternalSource.Length == 0` y reventaba con `NullReferenceException` en **todos los
> módulos ya instalados** — de la peor forma, dentro de una tarea de fondo que nadie mira. Se cazó
> porque el `catch` imprime el primer frame de la pila; sin eso el mensaje era «Object reference not
> set», que no dice dónde.
>
> **La regla, para el próximo campo:** al leer algo que puede faltar en una instalación vieja, `?? ""`
> y no confiar en el default del record.

**`externalSource` ausente significa que el módulo es dueño de su carpeta**, que es el caso normal y
el default: escribe su propio registro y nadie se la llena desde afuera. Vive en la **instalación** y
no en el manifiesto por la misma razón que `grantedFolder`: **qué se copia a la carpeta de un módulo,
y desde dónde, es decisión de quien concede** — nunca del módulo, que si no nombraría su propia
fuente.

## Qué está corrido y qué no

**Corrido de punta a punta en el emulador**, con un módulo hecho a mano cuya instalación declara
`externalSource`:

```
cartero: pwa-suelta, 3 archivo(s), 657 b
journals externos: 1 refrescado(s)
```

Antes de entrar, la carpeta del módulo no existía; después, el journal quedó completo adentro. Y en
la misma corrida el módulo **no cargó** —`Could not load file or assembly 'Externa.dll'`, porque el
módulo de prueba no trae DLL— lo cual es la separación correcta: **el refresco no depende de que el
módulo levante**, y la comprobación de conformance lo reportó honestamente en vez de callarlo.

**Y las tres formas de la huella, corridas:**

```
actor correcto            cartero: pwa-suelta, 3 archivo(s), 657 b   -> copia
actor de otra institucion cartero: no contiene el journal de otra-cosa -> NO copia
sin actor declarado       cartero: pwa-suelta, 3 archivo(s), 657 b   -> copia (compatibilidad)
```

**EL ALTA COMPLETA, corrida sin adb.** El enlace lleva el `.gmod` y el nombre de la carpeta:

```
https://go.gnothi.app/alta?bundle=…/externa.gmod&carpeta=pwa-journal

  -> hoja de consentimiento, con el manifiesto del .gmod
  -> «Instalar en mi teléfono»   -> instalado como in-process
  -> EL PICKER SE ABRE SOLO      -> concesión de SOLO LECTURA
  -> cartero: pwa-suelta, 3 archivo(s), 657 b
  -> externalSource anotado, y el journal en la carpeta del módulo
```

**Y con la carpeta equivocada** —se concedió `Documents`, un nivel arriba, que es el error realista—:

```
«la persona concedió una carpeta de donde traer su registro»
cartero: la carpeta concedida no contiene el journal de 'pwa-suelta'
«se quitó la carpeta de donde traía su registro»       -> externalSource vuelve a ""
```

**Se deshace a propósito.** Dejar anotada una carpeta que no sirve haría que cada arranque
reintentara contra ella y que el fallo se repitiera **en silencio para siempre**. Mejor una frase en
la pantalla y volver a preguntar.

**Lo que falta para que sea un producto:**

- **VOLVER a conceder la carpeta, y es el hueco más urgente.** Hoy la concesión se pide **una sola
  vez, dentro del alta**. Si ella elige mal, o cambia la carpeta, o revoca el permiso desde Ajustes,
  **no hay forma de volver a pedirla sin reinstalar el módulo**. Falta un botón en la ficha del
  módulo que llame a lo mismo que el alta ya llama; la pieza existe (`AsksForExternalFolder` →
  `PedirCarpetaPara`), lo que falta es el gesto en `ui.html`.
- **Un `.gmod` de verdad corriendo sobre la copia.** El mecanismo está medido con el motor abriendo
  la copia (`F1`), pero falta una institución real empaquetada así. El de la prueba lleva un
  `Externa.dll` que no es un ensamblado, y el Host lo dice con todas las letras —
  `BadImageFormatException`— sin que eso impida el refresco: la separación es correcta.
- **Refrescar más de una vez al día**, que fue lo pedido. Hoy es solo al entrar.
- **Decirlo en la pantalla del módulo.** Que su registro viene de una carpeta de ella, cuál, y
  cuándo se trajo por última vez. Hoy eso solo está en el log.


## La corrida de punta a punta, y lo que encontró — 2026-09-08

Se corrió el recorrido entero **desde la app suelta instalada en el aparato**, que es como lo va a
ver una persona:

```
página del laboratorio → «abrir la app en el navegador»
  → menú de Chrome → Add to home screen → INSTALL          WebAPK acuñado en 15 s
  → abrir desde el ícono                                   standalone; el 2.º modo pasa a decir
                                                           «DENTRO DE LA APP»
  → Elegir carpeta → crear «Mis examenes Delfos»           la persona la crea en el momento
  → la app escribe su journal ahí                          11 exámenes, hasta el evento 210
  → «Importar a Gnothi»                                    Gnothi abre DESDE la app instalada
  → hoja de consentimiento                                 las 2 consultas con su descripción
  → «Instalar en mi teléfono»                              matches: true
  → el picker se abre solo, pidiendo la MISMA carpeta
```

**Tres cosas que la corrida confirmó y no se sabían:**

1. **El acuñado del WebAPK depende de la cuenta del APARATO, no de la sesión de Chrome.** Con Chrome
   recién borrado y sin iniciar sesión, `Install` sigue ofreciéndose y acuña igual, en 15 s.
2. **El enlace cross-origin funciona desde un WebAPK.** Era el paso con más riesgo de fallar en
   silencio, y abre Gnothi sin dialogo de desambiguación (con `AutoVerify` y el dominio aprobado).
3. **El módulo contesta de verdad, y dice la verdad.** La comprobación corre una consulta real, y
   antes de conceder la carpeta `resumen` contestó:
   `{"hayDatos":false,…,"detalle":"The app has not recorded anything in this folder yet…"}` — que es
   exactamente lo correcto en ese momento. No inventó un cero.

### El bug que solo aparece viniendo de la app instalada

Al volver del picker, el alta murió diciendo **«la persona no eligió carpeta»** — cuando sí la había
elegido. El nombre del módulo que esperaba venía **vacío**.

**Causa:** `moduloEsperandoCarpeta` era un campo de la Activity, y **Android destruye y recrea la
Activity mientras el picker está delante**. En las pruebas anteriores no pasó porque Gnothi ya estaba
en primer plano; viniendo desde otra app, sí.

**Arreglo:** no depender del campo. Si vuelve vacío, el módulo se **deduce** del catálogo — el que
declara un actor y todavía no tiene fuente es, por construcción, el que acaba de pedirla. Si hubiera
más de uno no se adivina: se dice.

**La lección, que vale más que el arreglo:** el estado que tiene que sobrevivir a un `startActivityForResult`
no puede vivir en un campo de la Activity. Cualquier cosa que se lance así y espere respuesta tiene el
mismo problema.

> El arreglo quedó escrito y compila, pero **la corrida que lo verifica no se completó** — se paró
> antes. Es lo primero que hay que correr al retomar.

## Dos cosas que hay que decir antes de llevarlo a producción

**El `.gmod` en Android funciona pero no se puede publicar.** Lo dice el propio Host en
[`Module.cs`](../../../../Gnothi.Host/Module.cs): *«en un teléfono "in-process" no se puede publicar
aunque funcione»* — Play y Apple solo admiten JavaScript en un WebView para código que llega de
afuera. Para un POC no bloquea; para la tienda es una decisión de arquitectura, no un ajuste.

**El permiso de solo lectura se impone, pero no durante el alta.** La concesión guardada sí queda de
solo lectura y escribir falla con `SecurityException` — verificado tras reiniciar el aparato. Pero
**dentro de la sesión del picker Gnothi sí puede escribir**, porque `ACTION_OPEN_DOCUMENT_TREE`
entrega lectura y escritura en caliente. En ese rato la contención es disciplina, no incapacidad, y
conviene decirlo así en vez de afirmar «Gnothi no puede escribir en la carpeta de la persona».

---

## Los peros de este camino, todos juntos y todos medidos — 2026-09-08

Estaban desparramados en `F1`, `F2` y en las secciones de arriba. Van acá juntos, porque de eso
depende que alguien pueda decidir si este camino le sirve, y **la decisión no debería requerir leer
cuatro archivos**. Cada uno dice de dónde sale.

### 1 · Es una foto, no una lectura en vivo

Lo dice el diseño y no hay vuelta: el `.gmod` lee **una copia**. Entre el momento en que la PWA
escribe y el momento en que Gnothi lo ve, hay un refresco de por medio.

**Pero — y esto se midió el 2026-09-08 y juega a favor de este camino:** un lector *en vivo* sobre el
journal del dueño **también ve una foto**. Se midió con dos procesos, uno escribiendo 120 entradas y
otro leyendo diez veces: el lector llegó a 64 de 126 y **nunca alcanzó al dueño mientras estaba
abierto**. «En vivo» quiere decir **«fresco en cada apertura»**, no un flujo.

Con lo cual la distancia entre este camino y el vivo **no es de naturaleza: es de latencia y de
disco**. Ver `F1 · El lector mientras el dueño escribe`.

### 2 · El costo del refresco lo manda la cantidad de entradas, no el tamaño

Medido, y con una corrección de por medio: la primera curva mantuvo fijas las 2 000 entradas mientras
variaba el peso, lo que la volvía optimista. Rehecha a tamaño constante —2 000 entradas gordas contra
40 000 chicas—, **lo que domina es abrir, no copiar, y abrir escala con la cantidad de entradas.**

Viable hasta el orden de **decenas de MB**. Más allá hay que medir de nuevo antes de prometer nada.

### 3 · El journal queda dos veces en el aparato

Consecuencia directa de copiar. Con journals chicos no importa; con el crecimiento de años de una
persona, sí. No está medido a qué ritmo crece un journal real de una institución — **es una pregunta
abierta, no un número**.

### 4 · No hay cómo volver a conceder la carpeta sin reinstalar el módulo

**El hueco más urgente.** Si la persona revoca la concesión, se muda de carpeta, o restaura el
teléfono, hoy **no hay salida**: el módulo queda apuntando a una carpeta que ya no puede leer.

La plomería existe —`Shell.AsksForExternalFolder` → `MainActivity.PedirCarpetaPara`—; lo que falta es
**el gesto en `ui.html`** que la dispare a pedido.

### 5 · El refresco depende de que la persona entre a Gnothi

Un `.gmod` **no tiene pantalla que abrir**. Por eso el refresco se ata a la entrada a Gnothi más un
par de veces al día. Si la persona no entra, el módulo contesta con lo último que copió.

Contesta **con la verdad**, eso sí: en la corrida de punta a punta, antes de que hubiera carpeta
concedida, `resumen` devolvió `hayDatos:false` con el detalle correcto en vez de inventar un cero.

### 6 · El sellado no protege de lo que parecía

Se puso para no copiar a mitad de una escritura. Pero se midió qué deja Chrome en la carpeta mientras
escribe, y **sus escrituras son atómicas al `close()`** — no hay `.crswap` en la carpeta del SAF. O
sea que copiar a mitad de la escritura de **un archivo** no era el riesgo.

Lo que el sellado sí justifica es la inconsistencia **por conjunto**: que la copia agarre el archivo A
de antes y el B de después. Ese riesgo es real y el sellado lo cubre. **El motivo por el que se puso
estaba mal; la pieza sirve igual, por otra razón.**

### 7 · El reconocimiento se planta en una huella intrínseca, y es estricto a propósito

`<actor>/meta.bin` con la magia `PPMM` y un `journal/` al lado. La institución **no planta nada**. Y
se niega si encuentra **cero o más de uno** — antes prefiere no hacer nada que adivinar.

Un `Contains` en vez de un `EndsWith` ya hizo que una subcarpeta se confundiera con la carpeta padre y
diera «NO reconocida» sin ninguna pista. Está arreglado; queda como aviso de lo frágil que es la
comparación de URIs.

### 8 · Hay un arreglo sin verificar

El bug de la recreación de la Activity —volver del picker con el módulo en vacío— está **arreglado y
compila, pero la corrida de verificación nunca se completó**. Ver la sección de la corrida de punta a
punta.

### Lo que este camino NO necesita, y conviene no perderlo de vista

Ni el modo lector del motor, ni el puerto de archivos, ni que se resuelva lo del SAF. **Funciona hoy,
con el motor tal como está publicado.** Eso es lo que lo hace valioso aunque termine reemplazado: es
la única opción que no depende de que otro equipo acepte un cambio.
