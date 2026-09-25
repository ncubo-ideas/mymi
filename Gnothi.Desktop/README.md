# Gnothi.Desktop — el Host en una ventana

La misma cara que el teléfono (`Assets/ui.html`, el mismo archivo), el mismo Host, y la ventana
pintada con **Photino** en las tres máquinas.

> Existe además `--cara ventana`, sólo en Windows: la forma vieja, donde la cara de un módulo es una
> ventana de WebView2 aparte. **No la pasa nadie** —ni un instalador, ni un script, ni un doc— y no
> hay sala en ese modo. Si aparece en un comentario, es historia.

Para correrlo no hace falta nada especial:

```bash
dotnet run --project Gnothi.Desktop
```

---

## La caja de wasm — corre, y el canario canta

**Qué hace la caja acá:** hoy el `.dll` de la institución corre **con los permisos de Gnothi**.
Puede abrir la carpeta de otro módulo, salir a la red, leer el journal del laboratorio. Nada en el
sistema se lo impide — lo único que lo impide es que no lo hace. La caja se lo quita: wasm no tiene
syscalls, así que **lo que Gnothi no le monte no existe**. Medido en el teléfono: de nueve intentos
de fuga, siete funcionan en proceso y ocho de ocho cierran en la caja.

> **Ojo con el porqué**, porque no es el del teléfono. Allá la caja además resuelve el texto de
> Google Play. **Acá no hay tienda**: lo único que aplica es la contención, y aplica igual.

**Medido el 2026-09-24**, arrancando la ventana con el dll puesto:

```
caja: sustrato compilado y guardado (18 MB)
caja: canario -- 8 de 8 fugas cerradas; 2 de 2 puertas abiertas
caja: canario -- LA JAULA ESTA CERRADA
caja: encendida; la superficie de los modulos hibridos corre adentro de wasm
```

**Ocho de ocho fugas cerradas, en escritorio, los mismos números que el teléfono.** Y en la segunda
corrida `caja: sustrato abierto del precompilado`: el `.cwasm` de 19 MB queda en
`%LOCALAPPDATA%\Gnothi\caja\` con su huella, y compilar no se paga dos veces.

### El borde no era portable, y costó encontrarlo · 2026-09-25

**Se vio comparándola con la sala: el mismo módulo, el mismo journal, la misma máquina.** La sala lo
corrió; la caja moría al primer uso:

```
DllNotFoundException: Unable to load DLL 'libc' … (0x8007007E)
```

Ese `0x8007007E` es un **HRESULT de Windows**, no un error de wasm — o sea que el P/Invoke se
intentó **en el proceso**, no adentro de la caja. Es esta línea, al final de `Wasmtime.cs`:

```csharp
// mkfifo(3). There is no managed API for this and nothing more than this is needed.
[DllImport("libc", SetLastError = true)]
internal static extern int mkfifo(…);
```

**El motor es portable; el borde no.** `BoxedInstance` le habla al invitado por un **FIFO de POSIX**,
y en Windows no hay `libc` ni `mkfifo`. Así que la caja llega hasta el canario —que es la mitad del
camino, y la mitad que prueba la contención— y se para justo antes de servir a alguien.

> **Y acá decía que los seis archivos eran «neutrales de plataforma, cero referencias a Android,
> verificado».** Era verdad y era engañoso: **se buscó Android, no se encontró, y se concluyó que no
> había ninguna dependencia de plataforma.** POSIX no es Android. Buscar una cosa y no encontrarla no
> dice nada de las demás.

**ARREGLADO el mismo día.** `BorderPipe` tiene ahora las dos formas —FIFO en POSIX, **named pipe** en
Windows— y las dos se abren **por ruta**, que es lo único que `set_stdin_file` acepta, así que el
invitado no se entera de la diferencia: sigue leyendo su stdin.

Y el mismo módulo, en la caja, en Windows:

```
caja: compras vivo -- {"listo":true,"arranqueMs":39,"declara":{…}}
caja: compras borde #1 cursor/ 447 ms
caja: compras borde #2 consultar/compras 252 ms
caja: compras borde #6 consultar/cabecera 37 ms
caja: compras borde #9 consultar/cabecera 7 ms
```

**Doce idas y vueltas por el borde, ocho respuestas con `hayDatos:true`, ninguna excepción.**

> En macOS y Linux el camino es el `mkfifo` de siempre, sin tocar. **Eso no se corrió**, pero es el
> código que Android ya usa.

### La librería nativa la trae el build, no vos

`Gnothi.Desktop/wasmtime.targets` baja `wasmtime.dll` **una vez por versión**, comprueba su SHA256
contra el que está fijado ahí, lo descomprime fuera del árbol de git y lo copia a la salida. Los
builds siguientes no bajan nada. **No hay paso manual.**

La versión está **fijada en 48.0.2**: es la misma con la que F8 midió el Paso 0 y la que `Wasmtime.cs`
conoce. La C API del *component model* todavía cambia entre versiones, así que **subirla es un cambio
que hay que medir**, no un número que se actualiza.

**Por qué no está versionada en el repo**, a diferencia del `.so` de Android: allá es **una**
arquitectura —«el aparato que hay»—; acá el camino completo son Windows, macOS arm64, macOS x64 y
Linux, o sea cuatro binarios de ~22 MB para siempre en la historia, y otros cuatro en cada suba de
versión. El repo ya mostró a dónde lleva eso: hasta el 2026-09-24 tenía el **mismo** `libwasmtime.so`
versionado **dos veces** (mismo md5), 52 MB en un `.git` de 49. Ese mismo día el spike pasó a
referenciar el del producto y quedó uno.

Y hay un tercer motivo que no es de peso: **un binario versionado es un binario que nadie vuelve a
mirar.** Uno que se baja con el hash comprobado se audita en cada build.

> **Sobre qué es ese hash.** bytecodealliance **no publica** un `.sha256` al lado del asset — se
> buscó, da 404. Así que **no es una atestación independiente del publicador**: es la huella de lo
> que había en esa URL ese día. Sirve para que los bytes no cambien sin que nadie se entere, y para
> nada más.

**Hoy sólo Windows tiene asset fijado.** Los otros tres se agregan el día que alguien los baje en la
máquina que corresponde y calcule el suyo. Poner un hash sin verificar sería peor que no ponerlo:
daría la apariencia de comprobación sin la comprobación.

### Qué pasa si no se puede bajar

| situación | qué hace |
|---|---|
| **sin red** | **aviso** del compilador, y el build sigue. Un build offline tiene que funcionar |
| **el hash no coincide** | **error**, el build para, y **se borra el zip** para que el siguiente no lo reuse |
| **plataforma sin asset fijado** | aviso, igual que sin red |

Las dos primeras son fallas distintas y no comparten severidad: que no haya red es una
circunstancia; que los bytes no sean los fijados es otra cosa.

Y el aviso dice lo que importa:

> `wasmtime: no se pudo traer wasmtime.dll -- LA CAJA NO SE VA A ENCENDER y la superficie de los
> modulos hibridos va a correr con los permisos de Gnothi, como antes.`

**Eso es lo que más importa de todo el arreglo.** Sin la librería la caja no se enciende y **todo
sigue funcionando**: los módulos corren, la ventana abre, nada falla. La contención se apaga **y se
ve igual que el éxito**. El aviso del compilador es el único lugar donde eso se ve antes de creerlo.

### Si hace falta comprobar un dll a mano

**Cuidado con cómo, porque acá se perdió un rato.** El chequeo obvio es

```bash
strings -a wasmtime.dll | grep -c wasmtime_component_
```

y en el Git Bash de esta máquina **`strings` no existe**. No falla: imprime nada, el `grep -c` da
**0**, y la conclusión que se saca es «este dll no sirve» — que fue exactamente lo que se concluyó,
sobre un archivo que sí servía. **Usar `grep` directo sobre el binario**, que no depende de nada:

```bash
grep -ac wasmtime_component_linker_new wasmtime.dll
```

Tiene que dar **1**. Y si dos candidatos se parecen, `md5sum` los dos.

**Y tiene que ser el `c-api`.** El del NuGet `Wasmtime` sólo carga módulos **core**, y el .NET de
WASI produce un **componente** — por eso existe `Wasmtime.cs` a mano. El `wasmtime.exe` de
`~/.wasmtime/bin` tampoco sirve: es el ejecutable de línea de comandos, no la librería.

### Cómo se sabe que funcionó

Al arrancar, el log dice una de estas:

```
caja: encendida; la superficie de los modulos hibridos corre adentro de wasm
caja: no se encontro wasmtime.dll al lado del ejecutable; los modulos corren como siempre
caja: no esta sustrato.wasm al lado del ejecutable; los modulos corren como siempre
caja: el canario NO paso; no se usa la caja y los modulos corren como siempre
```

**Las tres últimas no son fallas de la ventana**: la caja se intenta siempre y **falla cerrada hacia
lo que ya funcionaba**. Si no se puede, ningún módulo cambia de forma y nadie pierde su registro por
una contención que no se pudo montar.

### Qué reclama la caja, y por qué eso alcanza

Reclama la forma **híbrida**, que es **la forma en que un módulo se entrega**.

Un módulo **cargo** (`"type": "in-process"`) no queda contenido, y **eso no es un hueco que cerrar**.
Cargo **no pasa el texto de ninguna de las dos tiendas** —acceso directo a las APIs del sistema, ver
[`B11`](../Documentacion/Jonathan/B-arquitectura/B11-lo-que-dicen-las-tiendas.md)— y **no trae cara**,
así que no hay nada que la persona abra. Existe como tipo de cáscara porque lo usan la línea de
comandos y las pruebas, no porque una institución vaya a publicar uno.

Escribir una variante sin cara de la caja sería construirle un anfitrión a algo que no se le puede
entregar a nadie.

---

## La sala — corre, y está medida

La sala en escritorio **arrancó el 2026-09-24** —la primera vez que corre en algo que no es un
teléfono— y el **2026-09-25 cargó un módulo de verdad**: `compras`, 87 compras, la pantalla pintada
con su total (`₡3.174.606`), repetido tres veces.

Esa segunda fecha es la que vale. Ver abajo por qué: la primera decía «los dos motores» y **uno de
los dos nunca cargó un módulo**.

```bash
dotnet run --project Gnothi.Desktop -- --sala --auto 25
```

`--sala` la calienta **sin ningún módulo**, y eso es a propósito: la pregunta que decide esta forma
—si el sustrato levanta bajo este motor— se contesta con el sustrato **solo**. Medidas juntas, un
alta que falló se lee igual que un origen que no sirve, y son dos arreglos distintos.

### Con un módulo de verdad · 2026-09-25

El calentado contesta «¿levanta el sustrato?». Esto contesta la otra: **¿corre un módulo?**

```bash
dotnet run --project Gnothi.Desktop -- <tmp>/Modulos --journals <tmp>/journals \
    --alta Gnothi.Mobile/bundles/hibrida/compras.gmod --sala-solo compras --abrir compras
```

El alta es la **de verdad**, la misma de la ventana: examina el paquete (`forma=Hybrid`,
59 archivos, `motor=0`, `excesos=[]`), lo instala y relee el catálogo. Y después:

```
sala: compras preparado desde <tmp>\Modulos\compras\dominio
sala/cargar compras: {"ok":true,"ms":112,"superficie":"ComprasSurface","borde":"prestado",
  "consultas":["resumen","lista_de_compras"],
  "vistas":["portada","cabecera","canasta","movimientos","proxima","compras"],
  "montado":{"archivos":3,"bytes":60705}, "vecinos":[]}
```

**El `.dll` de la institución —182 KB de `Compras.App.dll`— cargó adentro del motor web en 112 ms**,
declaró su superficie, sus 2 consultas y sus 6 vistas, y **montó el journal real de la persona**: 3
archivos, 60 705 bytes. `vecinos: []` — la frontera limpia.

Después se abrió la pantalla, y ahí se ve el circuito entero:

```
cara (127.0.0.1:54845) → Host → RoomShell.View → sala (127.0.0.1:65468) → ComprasSurface
                                                                              ↓
cara ← window.gnothi.respuesta("c6", {"hayDatos":true,"compras":[{"numero":"T-88101", …
```

**Seis vistas pedidas, tres con `hayDatos:true` y compras reales adentro.** Sin una excepción.

Y nótense **los dos puertos**: la cara de la institución vive en su origen y la sala en el suyo. Son
dos orígenes distintos en la misma ventana, que es lo que mantiene la separación.

> **Y su `installation.json` dice `"type": "hybrid"`.** Corrió en la sala sin que su archivo lo
> mencione — que es exactamente el punto de la vuelta atrás del 25.

### Hubo una segunda sala, sobre WebView2, y se borró · 2026-09-25

**Esta sección decía que había dos salas y que las dos estaban medidas. Era falso**, y conviene que
quede el cómo antes que el qué.

Había 407 líneas de una sala sobre WebView2, con documento oculto propio servido por host virtual
(`https://sala.gnothi.local`). Servía a `--cara ventana`. **Dos cosas, las dos verificables en un
minuto, y ninguna se verificó:**

1. **Nadie pasa `--cara ventana`.** Un `grep` por el repo entero: ni un instalador, ni un script, ni
   un doc. Sólo aparece si alguien la escribe a mano.
2. **Con un módulo de verdad no cargaba.** Timeout de 10 s en `cargar`, documento tirado, y otra vez,
   cinco veces seguidas.

Lo que esta sección daba por «medido el 2026-09-24» era **el calentado sin módulo** (`--sala`): el
sustrato levanta, la sonda P0 contesta, `el sustrato contesta`. Todo cierto, y **no dice nada de si
un módulo carga** — que es para lo que existe una sala.

> **La trampa, porque es reutilizable:** calentar y cargar se parecen bastante en el log como para
> confundirlos, y el calentado pasa en condiciones donde la carga falla. Una forma sólo está probada
> por el escenario completo: **un módulo de verdad pintando su pantalla.**

Y la lección de la primera: **se construyó para una bandera sin comprobar quién la pasa.** La bandera
existía y compilaba, o sea que todas las señales baratas decían que sí. Se descubrió al preguntar
«¿esto ya está terminado?», no corriéndola.

### La sala, que es una: un iframe oculto servido por loopback

```
sala: sirviendo en http://127.0.0.1:61074/ (solo loopback)
sala: creando el documento
sala: P0 · secureContext=true crypto.subtle=true storage=true wasm=true
           wasm.streaming=true origin=http://127.0.0.1:61074
sala-web: log: la sala esta lista, hospedada por Gnothi. Esperando pedidos.
sala: el sustrato contesta
```

**Por qué así y no con un documento oculto propio** —que es lo que hacía la borrada—, y no es
capricho: son dos medidas.

1. **El esquema propio de Photino no llega a un marco**: el pedido de un frame a un segundo esquema
   nunca alcanzó al handler (2026-09-14, cero llamadas). Por eso el escritorio ya sirve las caras por
   loopback y no por el esquema, y por eso la sala hace lo mismo: el sustrato baja `_framework/*` con
   fetch y no puede vivir con esa incógnita.
2. **Una ventana de Photino es una ventana del sistema**, y la sala no tiene pantalla. Que una se
   pueda mantener oculta **no está medido**, así que no se construyó sobre eso.

**Y esto es lo que la vuelve creíble en macOS y Linux sin haberla corrido ahí:** usa HTTP, un iframe
y un `postMessage`. **Nada de plataforma.** Una ventana oculta habría sido lo contrario —
específica del sistema y no verificable desde acá.

Tres cosas del log de arriba valen leerse con cuidado:

- **`secureContext=true` sobre `http://127.0.0.1`** — un loopback es contexto seguro en Chromium y en
  WebKit, que es lo que el runtime necesita.
- **`hospedada por Gnothi`** — el sustrato decide con `!!window.SalaPuente`. Que lo diga prueba que el
  shim entró **antes** que él. En Photino no hay forma nativa de inyectar un script, así que la
  etiqueta `<script src="/__gnothi/sala.js">` se agrega **al servir el documento**, y la CSP se
  calcula **después** de agregarla. Si el orden fuera al revés, la política dejaría afuera al shim y
  el documento saldría en blanco.
- **El shim va como archivo del mismo origen y no en línea**, para que `script-src 'self'` lo admita
  sin hash. Es la misma solución que las caras de los módulos ya usaban.

**Y los pedidos viajan como dato, no como JavaScript.** La cara de un módulo recibe código y lo
evalúa, y por eso su política pide `'unsafe-eval'`; la sala recibe cinco campos y llama a la función
que el sustrato publicó. **No necesita esa concesión.**

### Si falta el sustrato

```
sala: no esta sala/index.html al lado del ejecutable; los modulos corren como siempre
sala: se regenera con  dotnet publish Spikes/Lab.Sala/Sustrato -c Release -o Spikes/Lab.Sala/Sustrato/publicado
```

El sustrato entra **por referencia y no se versiona**, igual que en los dos teléfonos: son 23 MB
publicados, 14 MB en la salida sin los `.gz`/`.br`.

---

### Qué módulos corren en la sala, y quién lo decide

```bash
dotnet run --project Gnothi.Desktop -- --sala-solo examenes
```

**Se decide en código, en cada arranque, y el archivo de la persona no se toca** — exactamente como
la caja, que nunca lo tocó: un módulo en caja sigue diciendo `"hybrid"` en su `installation.json`.

> **Esto estuvo escrito al revés y duró un día.** La primera versión reclamaba los módulos que
> **declaran** `"sala"`, y para que eso sirviera hubo que enseñarle al alta a escribir ese tipo. Dos
> errores: la fábrica no aportaba nada —el `switch` ya mapea `"sala"` a `RoomShell`— y, peor,
> guardar el anfitrión en el archivo de la persona hace que un `"sala"` sin sala disponible **rompa**
> el módulo en vez de degradarlo. La caja, por construcción, no le puede hacer eso a nadie: si mañana
> falta la librería nativa, la fábrica no se registra y todo corre como antes.

**Y por lista y no todas, que es lo único en que se aparta de la caja.** La caja reclama todas las
híbridas porque su trabajo es contenerlas. Si la sala hiciera lo mismo **se las quedaría todas**: se
registra después, y la costura le pregunta primero a la última. La caja quedaría encendida en el log
y apagada de hecho — el defecto del 2026-09-23, reintroducido por simetría mal entendida.

---

## Lo que el escritorio todavía no tiene

- **Los otros tres `wasmtime`** (macOS arm64, macOS x64, Linux): falta bajarlos en la máquina que
  corresponde y fijar su hash, arriba.
