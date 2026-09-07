# F2 · Estado y pendientes

## El estado, en cinco líneas

- **Anda:** el eslabón de agregación, en tres superficies de asistente distintas y en Docker.
- **Anda:** el consentimiento por consulta, y desaparece en vez de denegar.
- **Anda:** la verificación de lo declarado contra lo real.
- **Anda:** la **ventana de escritorio nativa** —`Gnothi.Desktop`, Blazor Hybrid— corre las consultas
  llamando al Dispatcher **directo**, sin wasm, sin puerto, sin http. Es una cara más del Host. Ver `C7`.
- **Anda:** los **dos instaladores MSI** — el de Gnothi deja la marca de descubrimiento en `HKCU`; el del
  comercio la lee, **bloquea si Gnothi no está** y engancha `compras.gmod`. Ver `C7`.
- **Anda:** un **segundo módulo** demo, de otro autor — compras / Super La Guaria — sobre el mismo mecanismo.
- **Novedad:** el motor `3.0.0-beta.10029` **ya trae el replay secuencial**, así que la carátula wasm arranca
  con el paquete. Antes hacía falta apuntar al repo del motor.
- **No anda / no está:** el sandbox de capacidades por módulo (en el camino in-process), las consultas con
  parámetros, y el catálogo en caliente de verdad.

---

## Pendientes, en orden de lo que desbloquea

### P1 · El guard de hilos por capacidad — **y no es código nuestro**

Cambiar la pregunta «¿estoy en un browser?» por una pregunta sobre **capacidad del hospedaje**. Un flag
inyectable, o el conteo de procesadores como default con override explícito del Host.

**Por qué primero.** Bloquea toda la etapa de WASI: sin esto, un módulo con el motor no arranca fuera del
browser. Y es barato ahora — si entra con forma de plataforma, cada hospedaje nuevo agrega una condición más
a esa línea.

**Criterio:** el replay secuencial se activa en un hospedaje sin hilos que **no** sea el browser.

**Va reportado, no parcheado.**

### P2 · El fixture del grafo de referencias

Poner el frente MCP en su propio assembly y un test que afirme que el assembly del despacho **no lo
referencia**.

**Por qué ahora.** Son veinte líneas, y es lo que hace que el principio se cobre en vez de quedar en
intención. **El día que se pierda la propiedad, se pierde en silencio** — ningún tipo ni método la expresa,
solo el grafo. Hay precedente exacto en el repo del motor.

**Criterio:** el test falla si alguien mete un `using` del SDK de MCP en el despacho.

### P3 · Consultas con parámetros, por source generator

Hoy se omiten con aviso. **Por qué el generador y no reflexión:** un SDK basado en atributos + reflection
**se rompe con trimming, y en silencio**.

**Camino intermedio si hace falta antes:** manejar la llamada a mano con los handlers del SDK, que existen.

**Criterio:** una consulta con dos parámetros tipados se publica con su esquema correcto y se invoca.

### P4 · El catálogo en caliente

**La instalación ya existe:** el MSI del comercio y el comando `--install-module` dejan el módulo nuevo en la
carpeta y se consulta (ver `C7`). Lo que falta es cablear la notificación de lista cambiada — el Host ya
declara que su lista puede cambiar; el mecanismo está, no está conectado.

**Criterio:** instalar un módulo nuevo y que el asistente vea sus consultas **sin reiniciar**.

### P5 · Sacar los tokens del journal

Hoy se asientan en texto plano, y el journal es append-only. Dos caminos, y el segundo es el bueno: llevarlos
al **Playbill**, o —mejor— **que el módulo nunca los reciba**, porque el que hace el GET es el Host. Eso es
P7.

**Criterio:** un journal nuevo, después de un ciclo completo, sin un solo token adentro.

---

## Lo que cambia la forma del POC

### P6 · El módulo sin transporte

Hoy trae su propio servidor MCP. La arquitectura destino se lo quita: **expone su superficie y el Host elige
la cáscara**.

**Por qué importa más de lo que parece.** Está cerrado con 🚫 en la especificación, por dos razones
independientes: stdio/ssh solo tiene sentido entre máquinas distintas, y **si el módulo implementa un
aparato de transporte, el módulo tiene sockets** — con lo cual se cae la garantía que hace defendible todo lo
demás.

Y arreglarlo **cierra del todo el bug de los dos escritores**: sin proceso aparte, no hay dos escritores
posibles.

**Criterio:** un solo artefacto de módulo corre en los dos hospedajes sin recompilar, y no expone ningún
puerto ni tubería propia.

### P7 · El Host como cartero

El Host hace el pull del feed del publicador y le pasa el payload al módulo **como parámetro**. El módulo
asienta.

**Por qué.** Hoy lo hace la app del andamio. Y el endpoint tiene que ser **fijo por publicador, registrado en
la instalación, no elegible en runtime** — si el módulo pudiera decir qué URL traer, exfiltra codificando
datos en la ruta.

**Criterio, y es falsable:** quitar al módulo del camino y comprobar que **no hay forma de que un asiento
aparezca**. El Host no escribe el journal.

### P8 · El módulo como `.wasm`

Se parte en dos, y conviene no mezclarlas:

**¿Puede un servidor MCP vivir en un `.wasm`?** Probablemente sí: WASI expone lectura y escritura sobre los
descriptores 0/1/2, que es lo que stdio necesita. **Se prueba con un módulo trivial, sin el motor**, para que
la respuesta no dependa de él.

Prerrequisitos, **ninguno instalado**: el workload de WASI experimental, y un runtime wasm. Candidato para
móvil según la especificación: uno que haga intérprete y AOT en las dos plataformas.

**¿Puede el módulo con el motor?** **Bloqueado por P1.**

---

## Dónde hay latitud de diseño

**El formato del manifiesto.** Lo que hay es **una propuesta, no la especificación**. Se sabe qué campos
lleva; falta la forma, la extensibilidad, y cómo se versiona el manifiesto mismo.

Una restricción que no es negociable: **el modelo de capacidades tiene que ser contrato, no recomendación.**
Un módulo que declara «cero red» debe **fallar** en un host que se la conceda igual.

**La view declarativa.** Se sabe que el módulo describe y el Host renderiza. Qué lenguaje, sin decidir. Y hay
una tensión real: *«tan buena como quiera»* empuja hacia libertad expresiva, *«pocos KB»* hacia un
vocabulario acotado. Las dos no pueden ser máximas.

**Firma, conformance y revocación.** Es la capa de gobernanza, y está sin diseñar. Y hay un costo que hay que
aceptar de entrada: **esto nos convierte en el guardián.** No hay salida puramente técnica; se mitiga con
Host open source, builds reproducibles y manifiestos públicos.

**La explosión de consultas.** N instituciones × M consultas degradan la precisión de selección del asistente
y se comen la ventana de contexto. Con veinte módulos es un problema real, y **no aparece en ningún panel de
espacio**.

**Y el hueco de la superficie como canal de atención.** La asimetría es «el módulo responde, no pregunta».
Pero **el nombre y la descripción de una consulta son un canal hacia la atención de la persona.** Un módulo
puede publicar `completar_encuesta` y el modelo se lo va a ofrecer. Ahí la incapacidad no alcanza, y el
diseño falta.

---

## Qué reportar de vuelta

Tres cosas cambian decisiones aguas arriba:

1. **Si el dominio necesitó cambios** para correr confinado. Eso no es un inconveniente, **es el hallazgo**:
   significa que hay una fuga de infraestructura y hay que nombrarla.
2. **Cualquier número.** Cold start, latencia, payload en Release trimmed, batería. El proyecto se argumenta
   con mediciones; cada una mueve el pitch de «creemos» a «medimos».
3. **Cualquier cosa de estos documentos que el código contradiga.** Ya pasó una vez con los comentarios del
   motor. Mejor corregirlo que defenderlo.
