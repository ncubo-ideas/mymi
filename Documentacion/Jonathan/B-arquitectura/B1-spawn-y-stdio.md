# B1 · spawn + stdio, a fondo

El mecanismo por el que un asistente de escritorio alcanza un servidor local. Es la Puerta 1, es lo
único que ya funciona sin inventar nada, y **es también la razón exacta por la que el móvil no puede**.

---

## El spawn

No hay ningún servidor corriendo esperando conexiones. El cliente **crea el proceso** en el momento, y
por ser hijo hereda todo lo que hace falta y no hace falta nada más:

- **No hay puerto.** Nada que elegir, nada que chocar, nada que abrir en el firewall.
- **No hay autenticación.** El sistema operativo ya la hizo: el hijo corre con tu usuario porque lo lanzó
  un proceso que corre con tu usuario. **No hay «quién llama» porque solo puede llamar quien lo creó.**
- **No hay instalación.** No es un servicio, no queda residente. Muere cuando el cliente muere.
- **No hay red.** Los bytes nunca salen del árbol de procesos.

## El stdio

Todo proceso nace con tres descriptores: **0 stdin, 1 stdout, 2 stderr**. Normalmente stdin es el teclado
y stdout la pantalla.

Cuando lanzás un hijo podés **redirigirlos a tuberías que vos sostenés**. Ahí está el truco entero: **lo
que era la pantalla del programa ahora es un canal de mensajes.** MCP lo usa para JSON-RPC 2.0, **una
línea de JSON por mensaje** — por eso los mensajes van comprimidos, sin saltos de línea adentro: el salto
de línea *es* el separador.

## El diálogo, tal cual pasa

Probado a mano, sin librería de MCP en el medio:

```
proceso hijo lanzado, PID 2848

-> {"method":"initialize","params":{...},"id":1,"jsonrpc":"2.0"}
-> {"method":"notifications/initialized","jsonrpc":"2.0"}
-> {"method":"tools/list","id":2,"jsonrpc":"2.0"}
-> {"method":"tools/call","params":{"name":"total_de_facturas"},"id":3,"jsonrpc":"2.0"}

<- {"result":{"protocolVersion":"...","capabilities":{...},"instructions":"..."},"id":1}
<- {"result":{"tools":[{"name":"...","description":"...","outputSchema":{...}}]},"id":2}
<- {"result":{"content":[...],"structuredContent":{...}},"id":3}
```

Tres cosas de ese intercambio que vale mirar:

**El campo `instructions` del handshake no es documentación: es para la IA.** Llega en el `initialize` y
aparece literal en su contexto. Es el servidor enseñándole cómo interpretar lo que va a leer — incluido
qué decir si no hay datos.

**El `outputSchema` se genera del código, no se declara aparte.** Nadie escribe ese JSON: sale de la
clase de retorno y sus atributos de descripción.

**El resultado viene dos veces:** `content` con texto para leer, y `structuredContent` con el objeto
tipado para usar. Sin eso último, la IA recibe un string que tiene que volver a parsear.

## Por qué stdout es sagrado

El logging del host sale por **stderr** y no molesta a nadie. Esa es la válvula de escape.

Y por eso un servidor MCP tiene que desviar `Console.Out` a stderr **antes de tocar cualquier otra cosa**.
El motor imprime su progreso de replay con `Console.Write("1%2%3%…")`, **sin salto de línea** — y eso es
peor que escribir una línea basura: como no cierra la línea, esos caracteres **se pegan adelante del
siguiente mensaje JSON**. El cliente recibe `1%2%3%{"result":...}` y no hay parser que sobreviva. El
protocolo no se cae con un error claro: **se desincroniza**.

Es una trampa fea justamente porque en cualquier otro contexto ese `Console.Write` es inofensivo.

## Cerrar stdin es decir «apagate»

EOF en stdin es la señal de apagado ordenado. Elegante — pero muerde: si le mandás todos los mensajes de
un saque y cerrás enseguida, el hijo ve EOF y **se apaga antes de contestar**. El síntoma es «el servidor
no responde nada», cuando en realidad respondía y se moría antes de que nadie leyera.

## La seguridad sale gratis, y no es casualidad

Un servidor stdio **no tiene dirección**. No hay host, no hay puerto, no hay URL. No se puede escanear,
no se puede alcanzar desde afuera, no hay nada que exponer por accidente.

**La tubería *es* la credencial**: solo la tiene quien creó el proceso. No es que el acceso esté
protegido — es que **no existe una segunda forma de llegar**. Misma idea que el resto del diseño:
seguridad por ausencia de camino, no por vigilancia.

---

## El LLM no spawnea nada

Esta precisión importa y es fácil de perder. Del árbol de procesos, medido:

```
explorer.exe → claude.exe → claude.exe → dotnet Gnothi.Host.dll
```

El padre del proceso MCP es **la aplicación de escritorio**, no el modelo. El modelo no tiene manos. Son
**dos enlaces encadenados**, y solo el segundo es spawn+stdio:

```
   LLM  ←── HTTPS, nube ──→  el cliente local  ←── spawn + stdio ──→  el servidor MCP
 (afuera)                     (tu máquina)                             (tu máquina)
```

La secuencia de una llamada:

1. el cliente le manda al modelo la conversación **y la declaración de las tools** → por red
2. el modelo contesta *«llamá a esa tool»* — como salida estructurada. **No toca ninguna tubería**
3. el cliente escribe el JSON-RPC en el stdin del hijo → tubería local
4. el hijo contesta por stdout → tubería local
5. el cliente le manda el resultado al modelo → **por red**
6. el modelo compone la respuesta

**El modelo aparece en 1, 2, 5 y 6. La tubería existe solo en 3 y 4. Nunca se tocan.**

Y la consecuencia que importa: **el journal nunca sale de la máquina, pero el resultado sí.** Por eso
*«todo local, sin huecos»* es exacto **sobre el eslabón MCP** y no sobre el camino completo.

## Y ahora el spawn no es la única puerta

Todo lo de arriba describe **el eslabón MCP/nube** — el de la lámina, con `claude.exe` spawneando al
servidor. Pero ese eslabón dejó de ser el único camino al módulo: `Gnothi.Desktop` (una cara del Host) y
el comando `--query` por consola alcanzan **el mismo módulo, en proceso**, llamando al Dispatcher
**directo** — sin proceso hijo, sin tubería, sin puerto, sin stdio. El spawn sigue siendo la puerta del
eslabón MCP para la nube; ya no la única. Ver `C7` (las caras) y `B2` (el eslabón en proceso).

Y una desambiguación que ahorra un enredo: **«la aplicación de escritorio» del diagrama es `claude.exe`**
—el cliente MCP, el que crea el proceso—, **no `Gnothi.Desktop`**. Los dos son «de escritorio», pero
`Gnothi.Desktop` es una cara del Host, del *otro* lado del eslabón, y no spawnea nada.

---

## Por qué esto no existe en móvil

Acá está el punto, y es más duro de lo que parece.

**`spawn + stdio` no es un transporte que se pueda portar.** Es una consecuencia del modelo de procesos
de un sistema operativo de escritorio: cualquier programa tuyo puede crear otro programa tuyo y quedarse
con sus tuberías.

En móvil eso está prohibido por diseño:

- **Android** le da a cada app su propio UID y su propio sandbox, y bloquea `execve` sobre archivos del
  directorio de datos desde API 29. Una app **no puede lanzar el binario de otra app** ni quedarse con su
  stdio. La comunicación entre apps va por Binder — mediada, declarada, con las dos partes aceptando.
- **iOS** es más estricto: las apps de terceros directamente no crean procesos.

O sea que en el teléfono no falta *stdio*. Falta **el permiso de crear el proceso del otro**, que es el
cimiento sobre el que se apoya todo lo anterior. Sacado ese cimiento, no se cae el transporte: **se cae
la idea entera.**

## La regla que decide todo, en una línea

Un dispositivo **siempre puede llamar hacia afuera. Nadie puede llamarlo hacia adentro.**

Ahí está el asunto del móvil completo. El MCP local falla no por permisos ni configuración: falla porque
la app del asistente no hace la conexión — la hace la nube del proveedor, y esa nube tiene que *entrar*
al teléfono.

Así que la pregunta no es «cómo abro el teléfono», es **quién llama a quién**. Si el que llama está en el
teléfono, funciona hoy, sin pedirle nada a nadie.

## Y una corrección que importa para saber a quién pedirle

Las láminas dicen *«si el sistema operativo incorpora nuevas capacidades»*. En Android **el sistema
operativo no es el que falta**: un servicio en primer plano escuchando en localhost es perfectamente
posible hoy, y una app puede conectarse a otra en el mismo teléfono.

El que falta es **la app del asistente**: hoy toma direcciones de MCP remoto y la conexión la abre la
nube, no el teléfono. Lo que hace falta es que el proveedor deje que **su app** sea el cliente y hable
con localhost.

Eso cambia a quién hay que pedirle: no es esperar una versión de Android, es que un proveedor de
asistentes soporte MCP local. **Es una decisión de producto de un tercero, no una limitación técnica del
teléfono** — y por eso conviene no depender de ella.
