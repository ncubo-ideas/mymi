# C2 · Diez decisiones de código, y cómo se rompe cada una

Si vas a modificar el Host, esto es lo que hay que entender primero. Cada decisión dice qué se hizo, **por
qué**, y **cómo se rompería si alguien la deshace**.

---

## 1 · El despacho no sabe que existe MCP

`Despacho.cs` tiene dos métodos —`Catalogo()` y `Atender()`— y ninguna referencia a MCP, a modelos ni a
bucles de agente. `Program.cs` es el frente que traduce y delega.

**Por qué.** El diseño tiene **dos puertas**: un host externo que pide una llamada, y un bucle de agente
propio para móvil. Las dos tienen que hacer **lo mismo**. Si el catálogo, los permisos y la medición
vivieran adentro de la clase que habla MCP, la segunda puerta obliga a duplicarlos o a extraer a mano la
mitad de algo que ya está en producción.

Y peor: **no se podría borrar la primera puerta** el día que sobre, porque se llevaría los módulos con
ella. La Puerta 2 es transitoria por diseño; tiene que ser removible.

**Y eso dejó de ser hipótesis.** Hoy el despacho tiene **tres caras**, no dos: el frente MCP (Puerta 1),
el bucle de agente para móvil (Puerta 2) y ahora `Gnothi.Desktop`, la ventana nativa que llama al
despacho **directo** —sin MCP, sin puerto, sin http—. Esa tercera cara es la **prueba viva** de lo de
arriba: se le puede sacar la Puerta 1 y los módulos siguen ahí, porque nunca colgaron de ella. Ver `C7`
y `A2`.

**Cómo se rompe.** Un `using ModelContextProtocol` en `Despacho.cs`. Las flechas van en una dirección:
`frente → despacho → módulos`. Nunca al revés, nunca de costado.

**Lo que falta y es barato:** el fixture que lo prueba. Poner el frente en su propio assembly y un test
que afirme que el assembly del despacho no lo referencia. Son veinte líneas, y hay precedente exacto en el
repo del motor. **El día que se pierda la propiedad, se pierde en silencio** — ningún tipo ni método la
expresa, solo el grafo de referencias.

## 2 · El manifiesto y la instalación son dos archivos

Porque son dos autoridades. Si la cáscara viniera en el manifiesto, **el módulo elegiría cómo se lo
ejecuta y dónde escribe**. Conceder es un acto del que concede.

Se validó sin buscarlo: el hospedaje Docker usa **el mismo manifiesto, byte por byte**. Solo cambia la
instalación. Detalle completo en `B4`.

## 3 · La cáscara es el único lugar que sabe *cómo* se ejecuta un módulo

Ninguna otra clase lo nombra. Cuando se escribió esto había **una** cáscara, `CascaraStdio`, y se predijo
que la etapa siguiente —un runtime wasm con directorio pre-abierto— **reemplazaría** ese archivo sin tocar
ninguna otra línea.

**Llegó una etapa siguiente, y confirmó la propiedad por un camino distinto.** No fue wasm sino
**in-process**: una segunda cáscara, `InProcessShell`, que carga la superficie del módulo en el propio
proceso del Host y la despacha sin lanzar nada. Y **no reemplazó a `CascaraStdio`: se sumó al lado.** Hoy
conviven **dos** cáscaras, y elegir entre ellas es una línea de instalación, no de despacho.

`InProcessShell` reconoce la superficie **por nombre** —arrays `ClassMarks`/`ToolMarks`—, así engancha
tanto la superficie cargo nueva como los atributos MCP viejos. Ver `C5`.

Lo que importa: **la abstracción aguantó.** Sumar una cáscara entera —de un modelo de ejecución que no
existía cuando se diseñó— **no tocó una línea del despacho ni del catálogo.** Es exactamente la prueba que
el test de abajo pedía.

**El test de si está bien puesta la abstracción:** si para cambiar de hospedaje hay que editar el despacho
o el catálogo, está mal puesta.

## 4 · El catálogo sale de los manifiestos, no de arrancar los módulos

Al levantar, el Host lee los json y publica. **No levanta ningún módulo.** En la salida se ve el orden:
lo que publica aparece **antes** de que exista un proceso del módulo.

## 5 · Arranque perezoso, y desalojo después de cada llamada

**Por qué el desalojo, y esto costó un bug.** Un módulo vivo **tiene la carpeta del journal abierta**, y
hidratar un actor no es una lectura pasiva: crea el layout completo y toca la metadata. Con otro proceso
abierto sobre la misma carpeta, eso le invalida los handles cacheados.

Antes el módulo quedaba levantado para siempre desde la primera llamada. Parecía eficiente. **No cierra la
ventana del todo** —durante la llamada los dos coexisten— pero la baja de «para siempre» a los
milisegundos de una consulta. Detalle en `E3`.

Y es correcto por otra razón: N módulos vivos en un dispositivo es RAM real, y el módulo no inicia nada
por su cuenta, así que no hay nada que mantener despierto.

## 6 · La respuesta del módulo pasa sin transcodificar

`Invocar` devuelve el resultado del módulo entero. El despacho lo lleva como **objeto opaco** y el frente
lo devuelve tal cual.

**Por qué.** Si el Host parseara la respuesta tendría que **entender el dominio**, y ahí la capa se
contamina. Además cualquier pérdida en la conversión se vuelve un bug del Host **adentro de los datos de
la persona**.

## 7 · No existe ninguna consulta para encender, apagar o cambiar permisos

Las consultas propias del Host son de solo lectura. El consentimiento se edita en el archivo de
instalación.

**Por qué.** Si el asistente pudiera modificar el consentimiento, **la autoridad de consentimiento pasaría
al modelo**. Es la única de las cuatro autoridades que es de la persona y de nadie más.

Y el interruptor es del Host, no del módulo: *si lo implementa el módulo, no es un interruptor.*

## 8 · Apagar hace desaparecer, no denegar

Con una consulta apagada, no aparece en el listado, y llamarla da `Unknown tool`.

**Por qué importa la diferencia.** Un «no tenés permiso» le avisa al asistente que **hay algo ahí** y le da
algo que rodear o reintentar. Una consulta ausente no le da nada. Y el módulo tampoco se entera: no puede
distinguir «me apagaron» de «nadie preguntó» — si pudiera, tendría cómo insistir.

**Y sigue visible para la persona:** aparece encendido, con cero consultas publicadas. Invisible para el
asistente, visible para la dueña. Dos autoridades distintas, y se nota en la salida.

## 9 · Los nombres van prefijados por módulo

`facturas_total_de_facturas`, no `total_de_facturas`.

**Por qué.** Dos instituciones van a llamar «total» a cosas distintas, y el asistente elige por nombre y
descripción. Efecto secundario útil: **el prefijo es la prueba visible de que la consulta no estaba en la
configuración del asistente.**

**Y la descripción viaja tal cual.** No es documentación: es lo único que el modelo tiene para decidir si
esa consulta contesta la pregunta de la persona. Si el Host la reescribiera, estaría opinando sobre un
dominio que no entiende.

## 10 · El Host no referencia el motor

Su csproj tiene dos paquetes: el SDK de MCP y el hosting.

**Por qué.** El Host no necesita el motor para hacer de proxy, y **no tenerlo es la forma de que el
compilador garantice que no se metió semántica adentro**. El día que alguien necesite el motor en el Host,
eso es la señal de que una responsabilidad se corrió de lugar.

**Y esto ya cruzó de futuro a hecho.** El párrafo de arriba describe el csproj del proxy MCP —el del POC—.
En la arquitectura destino el Host lleva el sustrato **porque se lo presta** a los módulos cargo, y eso
**ya pasó**: el Host que hospeda a `compras` referencia `Ncubo.Puppeteer`, `Puppets.Portador` y
`Gnothi.Contract` para prestárselos. Ojo, no contradice el «por qué»: la alarma era que el Host **usara**
el motor para su lógica de proxy, y sigue sin hacerlo. **Cargar el sustrato para prestarlo es la señal
esperada, no la alarma — prestarlo no es usarlo.** Ver `C7`.

---

## Errores de implementación que valen registrar

**`StandardErrorLines` es un `Action<string>`, no un contador.** Parece un `int` y no lo es. Resultó más
útil: permite reenviar el stderr del hijo con prefijo, así el ruido de un módulo es rastreable hasta el
módulo — y nunca toca stdout.

**`ListToolsAsync` devuelve `IList`, no `IReadOnlyList`.** Detalle del SDK.

**Un candado alrededor del arranque.** Dos llamadas concurrentes sobre un módulo dormido levantarían dos
procesos, y el segundo quedaría colgado sin que nadie lo cierre.

**Y un módulo con manifiesto ilegible se salta con aviso, no tumba el Host.** Un Host que no arranca porque
una institución publicó un json torcido es un Host que le deja a un tercero la disponibilidad de todos los
demás módulos.
