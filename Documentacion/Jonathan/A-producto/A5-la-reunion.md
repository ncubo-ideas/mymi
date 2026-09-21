# A5 · La reunión: qué dijo, y las dos tensiones que abrió

De una grabación del arquitecto explicando el diseño al equipo. Aporta tres cosas que no están en las láminas:
**la mejor definición del Host**, un catálogo funcional mucho más completo, y **dos tensiones de diseño que
conviene resolver antes de construir**.

El catálogo está aparte, en [`A6-las-catorce-funciones-del-host.md`](A6-las-catorce-funciones-del-host.md).
Acá está el marco y lo que quedó abierto.

*(La transcripción viene de dictado: «Noti» es Gnothi, «MSPB» es MCPB, «popeter» es Puppeteer, «Wasi/Wason»
es WASI/WASM. Se usan los términos buenos.)*

---

## La mejor definición está en la primera analogía

> *«En celular Android, cuando yo me meto a notificaciones, ahí aparece la lista de aplicaciones y uno les
> puede cambiar el ringtone a cada notificación. Para mí este Host se parece como a la tuerquita.»*

Es la identidad del Host mejor dicha que en cualquier lámina: **es el panel de ajustes de tus datos.** Una
lista de módulos, y por cada uno se decide qué puede oír el asistente.

Y lo que la hace buena es lo que la analogía **excluye**: la tuerquita de Android no *hace* nada. No conoce
tus notificaciones, no las interpreta, no decide cuáles importan. **Media, expone y deja decidir.** Igual el
Host.

Con un detalle que él mismo señala y que vale para el producto: *«la tuerquita per se no tiene como muy bonito
aspecto»*. La función es de ajustes; la presentación no puede serlo.

## Y la palabra técnica es la correcta

La usa tres veces:

> *«Esa aplicación en realidad va a funcionar como una especie de proxy… a laboratorio, a agencia de autos y a
> supermercado.»*
> *«En el fondo lo que es, es como una especie de proxy nada más.»*

Un proxy que además **compone, filtra y contabiliza**. Ese «nada más» es la clave: el Host no agrega
semántica. Si lo hiciera, sería el punto de agregación más peligroso del sistema.

---

## Tensión 1 · El eslabón Host → módulo

La grabación propone insistentemente un **«SSH»** entre el Host y el módulo:

> *«El DLL suyo de la factura no tiene standard input output.»*
> *«El DLL se levanta una sola vez y él empieza a interrogarlo, pero como si el DLL tuviera un SSH.»*
> *«Uno le pueda preguntar: oiga, help. Y él dice: este es mi help, pregúntame por el comando exámenes… y se
> lo dice en texto.»*
> *«Yo ya eso lo he practicado y él se ubica lo más bien para hacerlo todo por línea de comandos.»*

Y la lámina 2 dice lo contrario, con énfasis: *«el eslabón Host ↔ Módulos siempre es invocación local
(llamadas a función). No hay transporte entre módulos ni hacia ellos.»*

### Cómo se resolvió

**La documentación posterior del arquitecto le da la razón a la lámina**, y lo cierra con 🚫:

> *«stdio/ssh entre el Host y el módulo: descartado.»*
> 1. *«ssh solo tiene sentido entre máquinas distintas. Acá viven en el mismo dispositivo.»*
> 2. *«Si el módulo implementa un aparato ssh, **el módulo tiene sockets**. Se cae "ninguno de ellos puede
>    salir por su cuenta", que es la garantía que hace defendible todo lo demás.»*

Lo correcto: **el módulo no tiene transporte. Expone su superficie; el Host elige la cáscara.**

### Pero la intuición de fondo era buena

Lo que él quería —*«que todos los sistemas sepan decir standard input output **sin que cada ejecutable de
estos 12 tenga que implementar su standard input output**»*— es exactamente correcto, y es lo que se logra
**no construyéndolo**: la superficie ya está declarada en el DSL canónico, así que el Host la traduce.

Y su instinto sobre **dónde** ponerlo —*«va con bibliotecas del motor, en Choreography o a la par»*— también:
si va a existir un atributo para marcar consultas, tiene que vivir en el sustrato, no en el SDK de ningún
protocolo. Detalle en `B2` y `C5`.

✅ Y una cosa que él aclara y sigue en pie: **ssh sí sirve como herramienta de desarrollo** para depurar
contra un dispositivo. Nunca como capacidad del módulo publicado.

---

## Tensión 2 · El wizard de framework

La grabación describe al Host resolviendo versiones de .NET como Windows con Excel:

> *«Si usted instala Excel y Excel dice yo estoy en la versión 7, y ve que su máquina no tiene ese framework,
> se lo instala.»*
> *«¿Dónde están los instaladores oficiales de .NET versión 4? Voy a descargarlo.»*

**Ese es el modelo de procesos nativos con framework a nivel de máquina**, y choca con otra cosa que la misma
grabación pide: que el módulo pese 30k.

### Las dos no pueden ser verdad a la vez

| | wizard de framework | módulo de 30k |
|---|---|---|
| multi-versión | ✅ procesos separados | ✅ instancias independientes |
| **el sandbox** | ❌ un proceso nativo tiene los permisos del usuario | ✅ capacidades concedidas |
| **el tamaño** | ❌ cada módulo trae su stack | ✅ el runtime lo pone el Host |

Con WASM el runtime lo pone el Host y el módulo trae solo su IL — que es justamente lo que hace posible el
«30k». Y eso **no es una limitación**: es lo que hace que el módulo no pueda salirse de su arena.

### La versión sana de la idea

El problema que él identifica es real, y el rumbo profundo —módulo chico, runtime del Host— **se cumplió.
Pero no por WASM: por carga in-process.** El módulo terminó siendo dos DLLs (superficie y dominio) montadas
sobre el motor que presta el Host, marcadas con `[ModuleSurface]`/`[Query]` de `Gnothi.Contract`. El runtime
es del Host, como él quería, y el «wizard» sano sigue en pie: el Host puede tener **varios runtimes propios**,
el módulo declara un rango de sustrato y el Host lo satisface con uno de los suyos, sin tocar el sistema
operativo.

El **tamaño chico se logró**. El **confinamiento no**: in-process es plena confianza —el módulo corre en el
mismo proceso que el motor, con sus permisos, y **puede salirse de su arena**—, así que la garantía *«no
puede salirse»* queda **pendiente**. Es la misma deuda que `A6` marca en la función 7: el sandbox por módulo
se pospuso, no se resolvió — y la vía in-process, sin la pared de WASM, lo dejó más lejos.

El WASM no desapareció: **sobrevivió en otro lado.** Vive en el PWA del comercio, corriendo dentro de un
`WebView2` en `Comercio.Shell` — no en el módulo de Gnothi. Ver `C7`.

*(La especificación posterior de runtime es más estricta todavía: **un sustrato, compatible hacia atrás**,
con una suite de journals dorados como guard. Varios side-by-side es escotilla de último recurso, porque
recrea «N motores que auditar» adentro del instalador.)*

---

## Y algo que no es técnico, y es de lo más lúcido

> *«Esto no es para que nosotros lo corramos. Supone que yo se lo vendo a Microsoft Costa Rica… El que va a
> operar esto, uno le tiene que decir: tiene toda esta lista de cosas.»*
> *«Es necesario que nosotros las empecemos a listar… una lista de todos los elementos de seguridad que
> nosotros verificamos.»*

**El Host no se vende por lo que hace. Se vende por la lista enumerable de lo que impide.**

Y esa lista hoy vive repartida entre una grabación y unas láminas. Es, probablemente, el entregable más
valioso que falta — y no es código.

## Dos observaciones de producto que vale rescatar

**El mercado decide hasta dónde se da el dato, no nosotros.** La secuencia que describe —un supermercado da el
total, el otro tiene que dar el impuesto, después el SKU, después el costo unitario— es competencia, no
diseño. *«Si faltan dos años para que lo dé, no hay ningún problema.»* Lo que se construye es **la
herramienta**, no el incentivo.

**Y la apuesta, dicha sin vueltas:** *«donde usted vea lo que puede hacer con su propia información, usted va a
exigir.»* El laboratorio que solo imprime un papel deja de ser competitivo cuando el de al lado entrega una
historia. Eso es lo que tiene que pasar para que el modelo funcione, y **no depende de la tecnología.**
