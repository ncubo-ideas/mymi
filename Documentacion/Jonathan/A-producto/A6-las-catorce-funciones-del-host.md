# A6 · Las catorce funciones del Host

El catálogo funcional, agrupado por **con quién habla cada función**. Sale de la grabación del arquitecto
explicando el diseño, y es bastante más completo que lo que muestran las láminas.

Sirve como especificación de lo que el Host tiene que hacer, y como checklist de lo que falta.

Las citas son de la grabación. El contexto de la reunión y las dos tensiones que abrió están en `A5`.

*(Las catorce salen de la grabación. Al final se agregan dos —15 y 16— que son **posteriores** a ella: la
superficie de consola y las caras de escritorio, sin cita porque no estaban en la reunión.)*

---

## Hacia el asistente

### 1 · Es la única boca

Nadie le habla al módulo directo. *«El que va a hablar con Claude es el Host, no la aplicación.»*

### 2 · Compone su propia superficie, y hace un join

La función central, y la que no está en las láminas:

> *«¿De dónde carga qué servicios tiene para devolverle al modelo? Él va y crea su superficie a partir de todo
> lo que tiene instalado.»*
> *«El laboratorio dice tengo seis, pero el Host tiene diez aplicaciones cada uno con seis. Le dice al modelo:
> tengo diez aplicaciones y tengo sesenta skills.»*

**60 consultas de 10 módulos en una sola lista.** Y se recompone al instalar: *«ahora mi superficie incluye el
colegio o la gasolinera»*.

**Estado:** ✅ implementado y verificado, con un módulo. El join con N está probado en su forma, no en escala.

### 3 · Descubre las consultas de dos maneras

*«O los escaneamos, porque le decimos voy a ir a rebuscar tus declaraciones, o los da en un archivo.»*

Ese **escaneo** es el «sin cooperación del módulo». Y las dos juntas **no son alternativas, son las dos
mitades de una verificación**: el archivo es lo que el módulo *dice*, el escaneo es lo que *puede*, y el Host
compara.

**Estado:** ✅ las dos, y la comparación también.

### 4 · Contabiliza al asistente

Idea propia de la grabación, que no está en ninguna lámina:

> *«El volumen de red son los mensajes que han ido para arriba o para abajo pasando aquí. Viene el asistente,
> le pregunta algo, él responde y uno cuenta.»*

**No es cuota para proteger el dispositivo. Es vigilar al que vigila:** el Host lleva la cuenta de cuánto
extrajo el asistente y se la muestra a la persona.

**Estado:** ✅ contador por módulo y por consulta.

---

## Hacia los módulos

### 5 · Presta el runtime y el motor, una sola vez

*«Va a tener solo el DLL del dominio y una aplicacioncita pequeña que no incluye la referencia de todo el
motor… un programita puede ser 30k.»*

**Estado:** ✅ implementado. El Host presta el motor **y** la etiqueta mínima `Gnothi.Contract`; el módulo
quedó en un `.gmod` de **dos DLLs** (superficie y dominio, cero-deps) más el manifiesto, que ahora trae
`load.entry` —cuál DLL trae la superficie— y `view`. Ya no lleva el motor adentro. **Matiz:** presta *un*
motor; todavía no resuelve la multi-versión — eso es la función 6. Ver `C7`.

### 6 · Resuelve versiones de framework

*«Este examen está usando una versión y después viene el colegio y ya no… el Host tiene que saber qué
frameworks tiene instalada la máquina.»*

**Estado:** ❌. La tensión con WASM ya se cerró —**hacia carga in-process**, no WASM (ver `A5`)—, pero eso
resolvió *quién* pone el runtime, no la **multi-versión**: hoy el Host presta un solo motor y el manifiesto
solo trae `load.entry`, no un rango. La versión sana sigue igual: el Host con **varios runtimes propios** y
el manifiesto declarando un rango de sustrato.

### 7 · Aplica el sandbox

Y la lista de negativas es dura y explícita:

> *«Solo tienen acceso a su carpeta.»*
> *«Nunca les des acceso a nada, ni siquiera a la red, ni siquiera a otra carpeta.»*
> *«Ni al parlante, menos al micrófono, y jamás de los jamases al disco duro, y por nada en el mundo al
> internet.»*

Y el punto fino: **no lo garantiza una política, lo garantiza la tecnología.** *«Todo lo que es entrada y
salida está blindado.»*

**Estado:** ⚠️ medido **a nivel de contenedor**, no por módulo — y la deuda **creció**. Al resolver la carga
por vía **in-process** (motor prestado en el mismo proceso, sin la pared de WASM), el módulo corre en plena
confianza y **puede salirse de su arena**: la promesa de que «lo garantiza la tecnología» todavía no aplica
por módulo. El ⚠️ se mantiene, pero hay que ser honestos sobre por qué creció la deuda — la in-process
**pospuso más** el sandbox por módulo de lo que lo habría hecho WASM. Ver `A5`.

### 8 · Valida el módulo, y la señal de alarma es el exceso de permisos

> *«Parece que el usuario le dio permisos de ver más archivos o del micrófono. Solo le pedimos tal cosa. Por
> problemas de seguridad no va a hablar contigo, estás violando las reglas.»*

Con un matiz que él mismo aclara: el Host **no impide correr**, se niega a mediar. *«Eso no significa "no la
corro", significa "vos expresamente le das derecho, sí o no"».*

Y si te instalaron algo que se hace pasar por del ecosistema: *«esa no es mía, tú la instalaste, es tu
celular, pero mía no es»* — con una calavera.

**Estado:** ⚠️ el POC **avisa** cuando un módulo declara capacidades de más. No lo impide.

### 9 · Neutraliza el StateManager sin tener que detectarlo

Lo más elegante de toda la conversación:

> *«Aquí no hay State Managers, todos son performance.»*
> *«Si hacen un state manager… uno le dice, perfecto, ningún problema: yo nunca te voy a abrir un puerto ni un
> socket ni un HTTP hacia el mundo externo.»*

No hay que rechazar el constructo prohibido. Se le quita la capacidad que lo haría significar algo, y **queda
inerte solo**. Un StateManager sin red es un objeto decorativo.

**Estado:** ⚠️ depende de 7. Sin sandbox por módulo, la neutralización es conceptual.

### 10 · Custodia las llaves, una por módulo y distintas

> *«Tiene la llave gigantesca para encriptar el archivo de laboratorio, el de agencia y el de supermercado, y
> no son la misma. Aunque yo se la pase a la aplicación de agencia… lo único que puede saber es la clave de su
> diario.»*

Eso responde de frente la objeción obvia —«un Host comprometido es todas las instituciones»—. Y la deferencia
es honesta: *«para efectos de empezar, los journals están sin encriptar»*.

El problema real no es cifrar, es **dónde vive la llave**: la persona va a poner el nombre de su perro. La
salida es apoyarse en el desbloqueo del dispositivo, no en una passphrase.

**Estado:** ❌ no implementado. El motor **sí** soporta cifrado autenticado del journal; falta la custodia.

---

## Hacia internet

### 11 · Es el único egreso, y hace el pull por cuenta del módulo

El razonamiento es el argumento más fuerte del bloque:

> *«Si uno deja que la aplicación haga un HTTP GET, no puede controlar luego qué hace esa aplicación en
> cualquier parte de la red. Uno no puede controlar que solo sea GET, podría ser POST, DELETE. Y además
> tampoco puede controlar que sea a la aplicación del laboratorio: el programador pudo haberle puesto que
> vamos a hacer un GET a mi compañía o a los servers de Google.»*

**Dar red a un módulo es incontrolable en dos ejes a la vez: el verbo y el destino.** Por eso el egreso se
centraliza — no por eficiencia, porque **no hay forma de acotarlo desde afuera**.

**Estado:** ❌ en el POC lo hace la app del andamio. Es el pendiente «el Host como cartero».

### 12 · Puede refrescar aunque el módulo esté cerrado

*«Vamos a seguirle llenando porque esa aplicación hace 3 días no la cargo. ¿Por dónde vas? Voy por 5.»*

Para eso tiene que leer el checkpoint del journal — lo cual encaja con que la introspección incluya las
entradas.

**Estado:** ❌ depende de 11.

---

## Hacia la persona

### 13 · Consentimiento por consulta, no por app

Lo más filoso de la transcripción, y **más fuerte que lo que muestran las láminas**:

> *«Los servicios de costo de la gasolina me parecen muy personales… entonces puede venir y poner off,
> disable esos dos servicios. O sea, no es poner disable toda la aplicación: esas dos llamadas.»*
> *«No puede hacer perform command ni query de esas dos llamadas… no los puede ver.»*

**Tres granularidades distintas:** app entera apagada, consulta individual off, y —la más fina— *«inclusive no
le va a poder decir "este escribe y estos leen": aún así ella va a poder decir "este que lee no me parece que
lo vea"»*. **Ni la clasificación lectura/escritura la ata.**

**Estado:** ✅ las dos primeras, implementadas y verificadas. Y **desaparece en vez de denegar**: la consulta
apagada devuelve `Unknown tool`, no un permiso denegado.

### 14 · Estadísticas, respaldo y purga

Tamaño del journal por módulo, volumen de red por módulo, y *«¿quiere respaldar todos los archivos de todos
tus journals?»*.

Y la purga, que las láminas afinan: **la propone el Host y la ejecuta el módulo**, que tiene derecho a marcar
hechos **no podables**. Porque el dominio es el único que sabe qué es prescindible — *que el Host decidiera
por tamaño sería destruir conocimiento por peso*.

**Estado:** ⚠️ el tamaño **medido desde afuera** y el contador, sí. Respaldo y purga, no.

---

## Hacia el operador y el escritorio

Superficie que no está en la grabación —es posterior— pero que ya existe y conviene catalogar junto al resto.

### 15 · Comandos de consola para empaquetar, instalar y consultar

El Host ganó tres verbos de línea de comandos: **`--pack`** arma un `.gmod` desde las piezas del módulo,
**`--install-module`** instala un `.gmod` en el runtime, y **`--query`** corre una consulta desde la consola.
Son la plomería sobre la que se apoyan los instaladores.

**Estado:** ✅ los tres.

### 16 · Las caras de escritorio

El Host dejó de ser solo un proceso sin ventana: tiene **caras**. `Gnothi.Desktop` es una ventana Blazor
Hybrid (panel de tarjetas nativo) que corre las consultas llamando al **Dispatcher directo** —sin wasm, sin
puerto, sin HTTP—, una cara más del Host. `Comercio.Shell` es un shell `WebView2` que hospeda el PWA del
comercio. Y las instala cada quien con **su propio MSI** per-usuario, sin admin. Todo el detalle —los dos
MSI, la marca de descubrimiento, las dos caras— está en `C7`.

**Estado:** ✅ ambas caras y los dos MSI.

---

## El resumen, para saber dónde está el proyecto

| | funciones | estado |
|---|---|---|
| ✅ **hechas y verificadas** | 1, 2, 3, 4, 5, 13, 15, 16 | la agregación, el consentimiento, la carga real y las caras |
| ⚠️ **parciales** | 7, 8, 9, 14 | el POC avisa o mide, pero no hace cumplir |
| ❌ **faltan** | 6, 10, 11, 12 | el multi-runtime, las llaves, y el egreso en frío |

Ya no es cierto que «falta casi todo lo que trata con los módulos»: la **carga real** (5) y el empaquetado
más las **caras** (15, 16) están hechos. Lo que queda es más acotado y más duro: la **multi-versión** de
sustrato (6), la **custodia de llaves** (10), y el **egreso** con refresh en frío (11-12) — que es donde
viven las garantías fuertes del modelo.

Eso conviene decirlo así cuando se muestra: el POC prueba que la mediación **y la carga** funcionan. **No
prueba todavía que el confinamiento funcione por módulo** — al contrario, la vía in-process lo dejó más
lejos (ver la función 7).
