# Capítulo 0 — La arquitectura clientecéntrica

Dónde corre cada cosa, quién es dueño de qué, y qué le pide todo eso a la app que vas a escribir.

> **De dónde sale este capítulo.** De dos fuentes que ahora sí se pueden cruzar: las infografías del
> equipo (Anatomía, Frontera del Modelo, Sustrato Compartido, Tu espacio, Tu IDE) y **el repo Gnothi
> corriendo** — `Examenes`, `Compras` y `HolaMundo/Facturas`, con sus READMEs de "cómo se corre".
> Donde las dos coinciden, el capítulo afirma. Donde el dibujo describe el objetivo y el código
> todavía hace otra cosa, lo dice con esas palabras. La versión anterior de este capítulo se escribió
> sin el repo a la vista y **tenía mal la pieza central**: le faltaba el Host.

---

## 1. El giro: el journal es de la persona

Todo lo demás se deduce de esto.

En la forma corriente, la institución guarda tus datos y te deja mirarlos. Acá **la institución te los
manda**: publica un módulo y un flujo de eventos, y el registro queda en el dispositivo de la persona,
en una carpeta que ella eligió.

La consecuencia no es de privacidad —o no sólo—: es que **habilita cruces que ningún proveedor puede
hacer solo**. El ejemplo del equipo lo dice mejor que cualquier explicación:

> El manual del carro dice 18 km/l. Tus cargas de gasolina dicen 9. *"¿Te compraste un 4x4?"*
> **Ninguna de las dos apps lo sabe sola.** El concesionario no ve tus cargas; la gasolinera no ve tu
> manual. La persona ve las dos.

Ese es el producto. La app bonita es el envoltorio.

### Dos fuentes de hechos, y la procedencia queda pegada

En el journal conviven dos orígenes, y se distinguen para siempre:

| Fuente | Ejemplos | Procedencia |
|---|---|---|
| **La institución** | un examen, una revisión técnica, una compra, una carga de gasolina | `INSTITUCIÓN` |
| **La persona** | anotaciones, preferencias, umbrales, autorizaciones | `TÚ` |

Cada asiento guarda **quién lo emitió, cuándo y qué**. No es metadato de adorno: es lo que permite
que un mismo journal sea a la vez copia de lo que dice el proveedor y registro de lo que dice la
persona, sin que se confundan.

Y tiene una consecuencia operativa que hoy casi no se nota. **Mientras todo venga de la institución,
el journal es reconstruible**: se borra la carpeta, se aprieta Refresh y vuelve entero desde URL2 —
el journal es la copia local de los eventos, no la fuente de verdad. Eso vale hoy porque en Exámenes
y en Compras hasta la edad está quemada en la semilla. **El día que la persona aporte datos propios**
—altitud de residencia, medicación, un umbral suyo— el journal deja de ser reconstruible y borrarlo
pasa a ser pérdida de datos. Es el mismo cambio de estatus que la procedencia ya anticipa.

---

## 2. Dos dibujos, no uno: lo que hay y a dónde va

Esta es la distinción que más confusión ahorra, y la que la versión anterior de este capítulo no
hacía.

| | **Modelo de hoy** (es lo que hay en el repo) | **Modelo objetivo** (es lo que dibujan las infografías) |
|---|---|---|
| Qué se instala | Una app completa por negocio: `Laboratorio.wasm`, `Supermercado.wasm`… | El **Host Gnothi**, una sola vez |
| Qué trae cada app | Runtime .NET + Puppeteer + Core **repetidos en cada una** | Sólo **dominio + repertorio + view + manifiesto** |
| Peso | El motor N veces | El sustrato una vez (~3,2 MB), y módulos de decenas de KB |
| Superficie de auditoría | N motores que actualizar | Uno |
| Qué es la app | Una aplicación | **Carga del Host.** No es una aplicación |

El modelo de hoy funciona y es donde se está aprendiendo. Pero se paga: *"mucho peso repetido por
módulo, más superficie de actualización y auditoría, mayor consumo de memoria"*.

**Escribí tu dominio pensando en el modelo objetivo.** Es gratis hacerlo: lo único que el objetivo te
pide es exactamente lo que el capítulo 3 ya te pide —dominio puro, cero dependencias, que no sepa
dónde corre—. Si tu dominio no sabe si lo hospeda una consola, un browser o un Host, la migración es
cambiarle la carátula.

---

## 3. Las piezas

### El Host: lleva el sustrato y es el único que sale a internet

| Componente del sustrato | Peso |
|---|---|
| Runtime .NET | 2,7 MB gz |
| Puppeteer | 1,128 MB |
| Choreography (clave wasm) | 145 KB |
| Host Gnothi (políticas, red, discovery, cuotas) | — |
| **Total, una sola vez en el dispositivo** | **~3,2 MB** |

El Host **ejecuta, aísla, autoriza y mide**. Y presta ese sustrato a todos los módulos.

Tres afirmaciones suyas que son restricciones para vos:

- **Es la única pieza que sale a internet.** Ojo con esta, porque hoy no es literalmente cierta y §5
  explica por qué. La forma que vale en los dos modelos es más precisa: **el dominio nunca sale a
  internet; el mecanismo sí.** Si tu diseño necesita que el *dominio* llame a un servicio, el diseño
  está mal.
- **Host ↔ módulos es siempre invocación local**, llamada a función, dentro del mismo dispositivo.
  No hay transporte hacia los módulos ni entre ellos.
- **El Host puede enumerar qué contiene tu módulo sin tu cooperación** (`IActorIntrospection`):
  símbolos y clases, acciones en DSL canónico, pools de parámetros, reacciones, entradas del journal.
  No podés esconder nada, y no deberías querer. *"Lo invisible puede ser rico, pero nunca opaco."*

### El módulo: es carga, no aplicación

| Parte | Tamaño de referencia |
|---|---|
| Dominio (DLL) | 50–200 KB · Tetris midió 21 KB |
| Repertorio (rangos, tablas, reglas) | 10 KB – varios MB |
| View declarativa (UI) | pocos KB · Tetris midió 23 KB |
| Manifiesto | < 5 KB |

Se baja de la web de la institución, como hoy. Y **cada módulo tiene su propio journal de la
persona**: crece a menos de 100 B por evento — 100 000 hechos pesan como una foto.

Fijate en el tamaño del repertorio: puede ser el componente más grande del módulo, más que el
dominio. Eso es sano. **El tamaño del módulo no depende de la riqueza del dominio.**

### Las dos URLs de la institución

Estas dos sí están construidas y corriendo, en los tres negocios del repo.

| Pieza | Qué es | En el repo |
|---|---|---|
| **URL1** | La página de descarga de la institución. nginx sirviendo el módulo publicado | `PaginaDeDescarga/` — **no es un csproj** |
| **URL2** | El endpoint de eventos: el GET, el token de device y su rotación | `EndPointDeEventos/` |

### El asistente: lee, nunca escribe

El LLM te ayuda a comprender y decidir. Lee lo que la persona permite, combina información de sus
módulos, responde y recomienda. **No escribe. No modifica.** En el repo eso no es una promesa: los
servidores MCP de Exámenes y de Compras son **solo lectura** sobre la carpeta del journal.

El único escritor del journal es la persona, a través de los módulos.

---

## 4. La frontera del modelo: lo único que cambia es el eslabón de arriba

En los tres casos el Host y los módulos corren **dentro del dispositivo de la persona**. Lo que
cambia es cómo llega el asistente hasta el Host.

| | Cómo llega el asistente | Estado |
|---|---|---|
| **Escritorio** | MCPB sobre **stdio** (spawn + stdio), un clic de instalación. Claude Desktop, ChatGPT Desktop | **Medido.** Todo local, sin huecos. Es el camino del POC |
| **Móvil** | **Puerta 2 — capa adaptable**, que conecta el asistente con el Host | **Objetivo.** 100 % funcional hoy. Si el sistema operativo incorpora capacidades nuevas, esta capa se simplifica o se sustituye, y el Host y los módulos no cambian |
| **Híbrido** | Modelo en la nube por red TLS | **Medido, y con un costo:** un relay que termina TLS puede intervenir. **Recrea el intermediario, no lo elimina** |

Esto responde una pregunta que este capítulo tenía abierta y ya no lo está: no hay tres caminos sin
probar para alcanzar el MCP. Hay un camino medido para escritorio, un objetivo para móvil con su capa
nombrada, y un tercero que se conoce y se sabe qué cuesta.

Lo que se mantiene invariante en los tres: el Host es la única pieza que sale a internet, el modelo
nunca accede directo a los módulos, y la historia y los permisos siempre quedan en el dispositivo.

### Por qué el MCP tiene que ser un proceso aparte

Está escrito en el código, y vale citarlo porque ahorra una tarde:

> Una página en el browser no puede hospedar un servidor MCP, porque no puede escuchar conexiones
> entrantes. No hay forma de evitarlo. Pero **no necesita hablar con el browser: lee la misma
> carpeta.**

---

## 5. Cómo se alimenta el journal — y quién hace esa llamada

Si el Host es la única pieza que sale a internet, ¿cómo pregunta la app por sus eventos? Es la
pregunta correcta, y la respuesta es otra vez que hay dos dibujos.

**Hoy la llamada la hace la app.** Concretamente `Mecanismo/Puppets.Portador`, que es quien tiene el
`HttpClient`. El dominio no: `Dominio/` tiene **cero dependencias** y no sabe que existe HTTP.

**En el modelo objetivo esa llamada la hace el Host.** No es una promesa suelta: las infografías
listan **`red`** entre las herramientas que el Host presta —junto con discovery, políticas y
cuotas— exactamente igual que lista el runtime y Puppeteer.

Y hay tres señales, en el código, de que el Portador está escrito para ese viaje:

| Señal | Qué implica |
|---|---|
| El GET vive detrás de una costura, `IFuenteDeEventos` | Cambiar quién hace la red es cambiar una implementación, no la app |
| Las costuras están nombradas como formas de **Choreography** — `InputSignal`, `IInputSource`, `InputRouting`, `IDispatchMessage` | Choreography es parte del sustrato que carga el Host. El Portador es un suplente escrito a mano de algo que el Host va a proveer |
| Se copia **byte por byte** entre negocios, y su README dice *"no lo edites"* y que debe subir a la raíz | Es mecanismo, idéntico para todos. Esa es la forma de algo que va camino a ser sustrato |

Por eso la regla que conviene grabarse no es *"nadie sale a internet salvo el Host"* sino: **la red es
del mecanismo, nunca del dominio.** Esa vale hoy, vale en el objetivo, y es la que te deja migrar sin
tocar el modelo.

### El pedido

La app le pide eventos a URL2 desde un cursor:

```
GET expediente/1/events?after=0
Authorization: Bearer <access-token>
```

El `after` es el cursor: la app **guarda por dónde quedó** y la próxima vez pide desde ahí. Refrescar
es barato y repetible.

**Las credenciales:** el token se pide a URL2 con la device key y queda asociado al device. Access
token corto, refresh token durable y **rotatorio**. Scope mínimo y explícito.

### El invariante, y es falsable

Esta es la parte que conviene copiar en tu negocio, porque es la que convierte un principio en algo
que se puede romper y por lo tanto verificar.

**La app no es cliente de los servicios generales de la institución.** Lo único que le pide es su
propia narrativa. Y para probarlo, URL2 tiene un **señuelo**: una operación que devuelve **403 con el
token válido de la app**, porque su scope no la cubre.

```
GET expediente/1/events?after=0    -> 200
GET expediente/1/perfil            -> 403
     {"error":"el scope del token no cubre esta operacion",
      "scopeDelToken":"expediente.events.read","scopeExigido":"expediente.profile.read"}
```

Un token robado del dispositivo de la persona sirve para leer sus propios eventos y **nada más**.

Hay un segundo invariante, estructural, que se sostiene solo si lo respetás al escribir: en los tres
negocios del repo, **`EndPointDeEventos` e `InyectorDeEventosDemo` no referencian `Dominio`** ni tocan
el journal. Es deliberado: *el proveedor no conoce el dominio de la persona.* Lo único que los conecta
es HTTP. Si algún día tu endpoint de eventos necesita referenciar tu dominio, algo se cruzó.

---

## 6. Aislamiento: los módulos no se hablan

Cada módulo vive en su propia arena. No se ven y no se hablan.

| Compartido entre módulos | Aislado por módulo |
|---|---|
| El código del motor (runtime, Puppeteer, Choreography, Host) | Dominio, repertorio, view |
| Las herramientas del Host: red, discovery, políticas, cuotas | El journal de la persona |
| | Memoria, directorio, claves y permisos, cupos y políticas |

El principio que lo ordena: **compartir código, no confianza.** El sustrato es código abierto y
verificable; la confianza se construye por aislamiento, no por compartición.

Y una frase de la infografía que resuelve una duda antes de que aparezca: *"no hay comunicación entre
módulos porque no hay dos aplicaciones: todas las llamadas pasan por el Host."*

### Entonces, ¿cómo se cruzan los datos entre negocios?

Esta es la pregunta del anexo A —el precio del examen no agrega valor en salud pero sí en presupuesto
personal— y ahora tiene una primera respuesta, que no es módulo a módulo.

**Cruza el asistente, con permiso de la persona.** Es lo que dice la infografía —*"combina información
de tus módulos"*— y es lo que ya hace el `.mcp.json` del repo, que declara **un servidor MCP por
negocio en la raíz**, con este comentario:

> El sentido es que **UNA IA vea VARIOS negocios a la vez** — es el universo de datos de la persona,
> no una app aislada.

Por eso los puertos de cada negocio son distintos a propósito: para que varios corran a la vez, *"las
historias cruzadas lo necesitan"*.

Esto no cierra el anexo A —falta el caso en que un módulo necesita un dato de otro **sin** que haya
una IA en el medio— pero sí fija el punto de partida: **el cruce es una capacidad del asistente sobre
journals que la persona autorizó, no un canal entre módulos.**

---

## 7. La convención de estructura

Acordada por el equipo y sostenida idéntica en los tres negocios del repo: **un directorio por
negocio, cinco nodos adentro, nombres en español.**

```
<Negocio>/
├── PaginaDeDescarga/        URL1 · nginx. No es un csproj
├── EndPointDeEventos/       URL2 · eventos, token, rotación, señuelo 403
├── InyectorDeEventosDemo/   siembra la historia en URL2. Valores y fechas elegidos, no aleatorios
├── Dominio/                 el puppet. CERO dependencias
└── App/                     una lógica y tres carátulas
    ├── <Negocio>.App        la lógica: los PerformCommand y PerformQuery. No sabe dónde corre
    ├── <Negocio>.Wasm       la app que instala la persona. Blazor WASM, PWA
    ├── <Negocio>.Mcp        la superficie que ve la IA. SOLO LECTURA sobre el journal
    └── <Negocio>.Consola    para medir el dominio y el refresh sin browser
```

**"Una lógica y tres carátulas"** es la frase que hay que retener, y es exactamente lo que el
capítulo 3 llama la carátula: el dominio y la lógica no se duplican por superficie.

La carátula de consola no es un juguete. Es el arnés con el que se trabaja: mide el dominio y el
refresh **sin el sandbox del browser encima**, y por eso es donde se valida primero.

**Los puertos se eligen distintos entre negocios a propósito**, para que corran a la vez:

| Servicio | Facturas | Exámenes | Compras |
|---|---|---|---|
| PaginaDeDescarga http | 8080 | 8090 | 8100 |
| PaginaDeDescarga https | 8443 | 8453 | 8463 |
| EndPointDeEventos | 8081 | 8091 | 8101 |

---

## 8. Qué le pide esta arquitectura a la app que vas a escribir

**El dominio no cambia.** Todo el capítulo 3 —modelo puro, `internal`, el puppet, las reglas del
substrato— aplica igual, y es justamente lo que hace que el módulo sea portable entre el modelo de
hoy y el objetivo.

**La carátula sí cambia.** Una app clientecéntrica **no tiene API REST propia**: tiene una carátula
WASM, una carpeta que la persona elige, un poller contra URL2 y un MCP como proceso aparte. El
capítulo 3 §5 describe una carátula HTTP que acá no aplica tal cual.

**No vuelvas a cuadrar los datos que manda el proveedor.** Esta es la instrucción más importante del
capítulo y la más fácil de desobedecer sin darse cuenta. Un listado de facturas, una tabla de
resultados, un historial de compras: eso el proveedor ya lo tiene y ya lo muestra. Lo que la persona
no tiene en ningún lado es la **lectura** de esa serie.

| No esto | Sino esto |
|---|---|
| La lista de tus exámenes | *"Tu colesterol pasó de 185 a 212 en un año: +14,6 %, en ascenso"* |
| El historial de mantenimientos | *"Te faltan 800 km para el cambio recomendado; ya podés agendar"* |
| Las facturas del súper, una por una | *"Snacks +35 %, frutas y vegetales −18 % en tres meses"* |

**La app tiene que ser tan linda como un juego.** No es un requisito estético suelto: es el corolario
de lo anterior. Si la app fuera un listado, la belleza sería decoración; siendo un dashboard que
contesta preguntas del dominio, la presentación *es* el producto.

**La pantalla y el modelo no tienen por qué coincidir.** *"La IA tiene acceso a todo lo que el dueño
permita, aunque la pantalla muestre poco."* La prueba que el equipo se puso es hacer una pregunta que
la UI **no** muestre —*"¿cuánto hace que no me mido el colesterol?"*, *"¿cuándo compré la olla arrocera
y cuándo vence?"*— y que la IA la conteste del journal.

De ahí sale la consecuencia de diseño que desarrolla el capítulo 4: **modelás para las preguntas, no
para las pantallas.**

Y dos reglas que el repo sostiene y que conviene adoptar desde el primer día:

- **El dominio no lee el reloj.** La fecha entra por parámetro. Si el dominio consultara la hora, el
  mismo journal reproducido mañana daría otra respuesta. Saber qué día es hoy es derecho de la
  superficie.
- **El dominio nombra, pero no inventa.** Tiene tres partes y colapsarlas en *"devuelve hechos, nunca
  prosa"* pierde justo lo que importa:
  - **Sí** arma su propia lectura, en una línea. *"Tu antígeno pasó de 3,0 a 5,2 ng/mL en tres
    años"* la escribe el analito, con sus hechos y su vocabulario, porque es **lo único que puede
    decir mejor que nadie**.
  - **No** inventa el consejo. La acción sale de un conjunto cerrado de textos fijos asociados a la
    severidad — ésa es la garantía de que ningún analito se fabrique una recomendación propia.
  - **Los hechos viajan siempre al lado** de la lectura, para que la carátula o la IA puedan decir
    otra cosa con ellos. La interpretación no los reemplaza, y por eso la conclusión es auditable.

  El límite no está entre el hecho y la frase: está entre **nombrar** e **inventar**. Eso es lo que
  hace imposible el consejo de plantilla — no hay de dónde sacar *"bajá las grasas"*.

---

## 9. Cómo se desarrolla contra esto

De la infografía del IDE, y coincide con cómo están armados los negocios del repo:

| Paso | Qué es |
|---|---|
| **Modelás** | Dominio y repertorio con C# + DSL. **Sin dependencias del host** |
| **Conversás** | Tu agente usa tus endpoints desde el día 1. Prototipás conversando |
| **Simulás** | Datos falsos **determinísticos** y eventos desde un publicador simulado. Desarrollás sin esperar a la institución — es exactamente lo que hace `InyectorDeEventosDemo` |
| **Inspeccionás** | Introspección y trazabilidad completa. **Ves lo mismo que verán los auditores** |
| **Probás** | El Host de escritorio corre tu módulo como lo hará el dispositivo real. Mismo sustrato, otra carátula |
| **Publicás** | Un archivo liviano del módulo, y el feed se despliega en la institución. **Dos deploys, y sólo uno es a un servidor** |

"Determinísticos" no es un detalle: los valores y las fechas del inyector son **elegidos, no
aleatorios**, porque el guion de la demo depende de que la serie diga algo.

---

## 10. Estado medido, y lo que este capítulo no cubre

| Pieza | Estado |
|---|---|
| URL1 y URL2, un `docker-compose` por negocio | ✅ construidos en Exámenes, Compras y Facturas |
| Dominio y carátula de consola | ✅ Exámenes 60 comprobaciones, Compras 58 |
| Carátula WASM | ✅ corre completo desde el motor `3.0.0-beta.10029` |
| Servidor MCP | ✅ construido y corriendo, uno por negocio |
| **El Host Gnothi con sustrato compartido** | **es el objetivo.** Hoy cada app repite el motor |
| Cifrado en reposo del journal | pendiente de documentar |
| Respaldo y restauración de la carpeta | hoy es copiar archivos a mano |
| Settings de qué ve la IA | **salió de las apps a propósito**: quien se conecta con la IA es el Host, no la app |

Dos restricciones del navegador que condicionan la demo y no son bugs:

- **La carpeta propia es sólo Chromium.** `showDirectoryPicker` (File System Access API) existe en
  Chrome, Edge, Brave y Arc; no en Safari ni Firefox. Ahí se degrada a OPFS o a export/import, y
  **OPFS no es reemplazo**: el MCP corre fuera del browser y no ve ese almacenamiento, así que la
  mitad *"tu IA lo lee"* deja de funcionar. La app lo dice con todas las letras en vez de dejar creer
  que está resuelto.
- **Desde un teléfono hay que entrar por https.** Las APIs de almacenamiento exigen contexto seguro:
  https o el literal `localhost`. Un teléfono entra por IP, y por `http://` el navegador ni define
  las APIs.

Lo que este capítulo deja para otros:

- **Compartir información entre módulos sin una IA en el medio** — anexo A. §6 fija el punto de
  partida, no lo cierra.
- **El detalle de cómo se prepara el escritorio y cómo se corre el Hello World** — capítulos 1 y 2,
  que ahora sí se pueden escribir mirando el repo.
