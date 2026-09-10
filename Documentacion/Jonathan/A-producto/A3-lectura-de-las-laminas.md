# A3 · Lectura de las seis láminas

Qué dicen las infografías de Gnothi, qué resuelven entre ellas, y qué dejan abierto. Leídas en sus
propios términos, no como diferencia contra ningún prototipo.

---

## La inversión central: el módulo es *carga*, no aplicación

Lo normal es que una aplicación sea código + runtime + UI empaquetados en un artefacto. Acá el runtime
se vuelve **infraestructura del dispositivo**, instalada una vez, y la institución entrega únicamente
lo que es suyo: el significado (Dominio), los datos de referencia (Repertorio), la presentación (View)
y la declaración (Manifiesto).

La palabra **«carga»** está elegida con precisión: es flete. El Host es el vehículo, el módulo es lo
que se transporta, y **el flete no conduce**.

Y esa figura ahora tiene artefacto: la carga se empaqueta en un **`.gmod`** — un zip con las dos DLLs
(superficie y dominio), el `manifest.json` y el `view.json`. La superficie se marca con
`[ModuleSurface]` en la clase y `[Query("nombre")]` en el método, de `Gnothi.Contract`, y el Host la
reconoce **por nombre** —no por el tipo del atributo—, así que lee tanto la marca nueva como los
`[McpTool]` viejos. El manifiesto ganó dos campos para sostener esto: `view` (cuál json es la vista) y
`load.entry` (cuál DLL trae la superficie). Ver `C7`.

Consecuencia que la lámina pone como check: *«el tamaño del módulo no depende de la riqueza del
dominio»*. Un laboratorio con un dominio sofisticadísimo sigue pesando ~120 KB, porque la
sofisticación se paga en el DLL de dominio y la *maquinaria* no está ahí.

Y «Assembly App» es un doble sentido deliberado: assembly de .NET, y algo que se **monta** sobre otra
cosa.

## Las cuatro piezas del módulo, y qué separa cada corte

| pieza | qué es |
|---|---|
| **Dominio** | la *forma* del significado. Qué es un examen, qué es una cuota |
| **Repertorio** | el *contenido* que lo parametriza: rangos, tablas, reglas, catálogos |
| **View** | la presentación declarativa |
| **Manifiesto** | la declaración, menos de 5 KB |

El corte Dominio/Repertorio es el que más trabajo hace: permite que la institución **actualice sus
tablas sin publicar código nuevo**, y es la razón técnica de que el DLL de dominio se mantenga chico
incluso en un dominio rico.

## «Compartir código, no confianza»

El principio de la lámina 3 en cuatro palabras, y la v2 agrega la frase que lo vuelve operativo: *«el
sustrato es código abierto y verificable. La confianza se construye por aislamiento, no por
compartición.»*

Compartir un runtime normalmente obliga a confiar en los co-inquilinos. Acá no, por dos razones
simultáneas: **lo compartido es auditable** (código abierto, uno solo, una superficie de auditoría en
vez de N) y **lo no compartido está separado por construcción**, no por buena voluntad.

Las cuatro paredes de la arena, que convierten «aislamiento» de eslogan en definición operativa:

**Memoria separada · Directorio separado · Claves y permisos propios · Cupos y políticas por
instancia**

Las dos últimas resuelven cosas concretas. **Claves propias** significa que las credenciales de cada
institución viven en su instancia, no agrupadas: el Host es el único que sale a internet, pero no es
una bóveda con todos los secretos juntos. **Cupos por instancia** es el verbo «mide»: un módulo no
puede agotar el dispositivo.

Y el cierre: *«no comparten memoria, estado ni capacidades entre sí»*. La tercera —capacidades— es la
importante: dos instancias sobre el mismo motor no heredan los mismos permisos.

## La pieza que hace honesto todo lo demás: `IActorIntrospection`

> *El Host puede enumerar, sin cooperación del módulo, qué contiene y qué puede hacer:*
> Símbolos y clases · Acciones (DSL canónico) · Pools de parámetros · Reacciones · Entradas del journal

Con su aforismo, que es la mejor línea de todo el material: **«lo invisible puede ser rico, pero nunca
opaco.»**

Esto es el fundamento del consentimiento, no un extra de observabilidad. **Nadie puede consentir sobre
algo opaco.** Si el Host solo pudiera reportar lo que el módulo declara, el Manifiesto sería publicidad
y el permiso sería teatro.

Y nótese qué incluye la lista: **entradas del journal**. El Host lee la historia directamente, sin
pasar por el módulo. Consecuencia fuerte: **el módulo no es el guardián de la historia de la persona.**

*(Verificado: la interfaz existe, con nueve verbos. Ver `F1-mediciones.md`.)*

## El journal: una pregunta que las láminas resuelven entre ellas

La lámina 1 dibuja **una caja de journal por módulo** y el subtítulo dice **«un único journal por
Sonia»**. Parece contradicción. Las láminas 3 y 4 lo cierran:

- La 3: *«cada módulo es una instancia aislada con su propio dominio y su propio journal»*, y entre las
  garantías de aislamiento, **«directorio separado»**. Físicamente hay uno por instancia.
- La 4: *«un único escritor: tú (a través de los módulos)»*. **«Único» no habla de almacenamiento,
  habla de autoría.** Una persona, una pluma, una historia.

Y el timeline unificado de la lámina 4 —examen, autorización, revisión técnica, carga, compra, en
orden cronológico— **es una vista, no el almacenamiento**. El Host la compone leyendo las instancias
por introspección, y eso no rompe el aislamiento porque el Host tiene autoridad de interacción sobre
todas.

## Dos autores, un registro, y qué lo hace posible

El panel «¿quién escribe en el journal?» plantea algo que parece problema y es la solución: **dos
autores en un solo registro**. La institución emite hechos (un examen, una compra) y la persona
escribe los suyos (anotaciones, preferencias, umbrales).

Solo es sostenible por la línea siguiente: *«cada asiento guarda procedencia (quién lo emitió), cuándo
y qué. Así se distingue para siempre.»*

Sin procedencia, dejar que la persona escriba en un registro institucional destruiría su valor
probatorio. Con procedencia, **puede anotar libremente sin contaminar nada**. El vocabulario es
contable a propósito —«asiento», append-only, «no reescritura»— porque lo que se describe es un libro
mayor: no se corrige, se agrega.

Y ahí se resuelve la paradoja aparente entre *«la historia completa vive en el dispositivo de Sonia»* y
*«Dominio: propiedad de la institución»*. **Ella lo custodia; no lo emitió.** Eso es distinto de la
propiedad del dato, y es un encuadre más defendible.

Sumado a *«firmado por el Host»*, la procedencia deja de ser una etiqueta y pasa a ser **no
falsificable**: el Host actúa de notario.

## Lámina 2 no describe, argumenta

Sostiene una tesis: **el sistema tiene una sola variable**. Lo invariante es el eslabón de abajo —Host
↔ Módulos, siempre invocación local, mismo dispositivo, sin transporte. Lo variable es únicamente cómo
el asistente alcanza al Host.

- **Escritorio.** El asistente corre en la misma máquina: spawn local sobre stdio. Sin red en el
  eslabón. De ahí «sin huecos».
- **Móvil.** El asistente es una app que no puede lanzar un proceso local, así que se interpone
  **Puerta 2**, caracterizada como *adaptable* y explícitamente desechable: *«si el sistema operativo
  incorpora nuevas capacidades, esta capa puede simplificarse o sustituirse. El Host y los módulos no
  cambian.»* La intención es tratar el hueco del móvil como **problema de época del sistema operativo,
  no de arquitectura**.
- **Híbrido.** Modelo en la nube por TLS. Y acá la lámina hace algo que casi ningún diagrama hace:
  **nombra su propio peor caso** — *«un relay que termina TLS recrea el intermediario, no lo
  eliminamos»*.

Un detalle de lectura para no confundir: la columna móvil está rotulada **TARGET**, pero el recuadro de
Puerta 2 dice **«100% funcional hoy»**. La afirmación es más angosta de lo que parece: el mecanismo
funciona; lo que es *target* es la solución de producto terminada.

## Las dos versiones de la lámina 3 no son borrador y corrección

Son dos piezas para dos audiencias, y conviene conservar las dos:

- **La v1 argumenta.** Tiene el panel «modelo de hoy (ineficiente) — el motor se repite N veces», con
  el recuadro rojo: peso repetido, más superficie de auditoría, más memoria. Es la **justificación
  económica**. Sirve para convencer a quien no lo da por sentado.
- **La v2 especifica.** Elimina el contraste, enuncia el diseño en positivo, y agrega lo del código
  abierto y verificable. Sirve para quien ya aceptó la premisa.

La v2 gana en limpieza y pierde el argumento. No conviene mezclarlas: v1 para vender, v2 para construir.

## Lo que las láminas dejan abierto

**1 · Quién renderiza la View.** Se mide «View Tetris (Blazor) 23 KB» y se describe como «View
declarativa, pocos KB», pero el sustrato itemizado es .NET + Puppeteer + Choreography + Host, y el Host
está descrito como políticas, red, discovery y cuotas — sin capa de UI. O el renderer está adentro sin
figurar en el presupuesto, o falta del presupuesto. Importa porque **~3,2 MB es el titular de tres
láminas**.

Y hay una tensión entre dos frases de la misma lámina: *«la experiencia de cada institución puede ser
tan buena como quiera»* empuja hacia libertad expresiva, y *«View declarativa, pocos KB»* empuja hacia
un vocabulario acotado. Las dos no pueden ser máximas a la vez.

**Parcialmente cerrado.** Ya no es incógnita *dónde* vive el renderer: en las **caras nativas** —
`Gnothi.Desktop` (ventana Blazor Hybrid) y `Comercio.Shell` (WebView2 que hospeda el PWA) —, no en el
Host, que sigue sin capa de UI. Y el manifiesto ahora **nombra su vista** con el campo `view`. Lo que
queda abierto es el presupuesto: dónde pesa el renderer de esas caras. Ver `C7`.

**2 · La evolución del dominio contra un journal append-only.** El dominio es de la institución y va a
cambiar; el journal es indeleble y se replaya con el dominio. Si el laboratorio publica Dominio v2,
¿replaya lo que se escribió bajo v1? Es el problema más difícil de todo sistema event-sourced y no está
tocado. El Manifiesto es el lugar natural para declarar compatibilidad, pero no lo dice.

**3 · Cómo se verifica un módulo antes de correrlo.** Se descargan «desde la web de la institución
(como hoy)» — distribución directa, sin tienda. Entonces la introspección es la única defensa, y no
aparece nada sobre **firma del módulo**. Se habla de firma para los asientos del journal, no para el
artefacto que se instala.

La distribución además sumó **MSI**: cada institución instala su `.gmod` con su propio instalador
per-usuario, que corre `--install-module` por custom action. Pero eso no cambia el fondo — sigue sin
tienda y **sin firma de módulo**, así que la introspección sigue siendo la única defensa. Ver `C7`.

**4 · Qué es Puerta 2.** Está nombrada, caracterizada y declarada funcional, pero en ninguna lámina se
dice qué **es**. En un material donde todo lo demás está especificado hasta el nombre de la interfaz,
ese hueco resalta.

## Cómo leer los números

El «~3,2 MB» es el total **comprimido**: 2,7 MB gz del runtime + 0,44 MB gz del sustrato, como dice el
pie. Los números al lado del signo `+` mezclan escalas — el 2,7 del .NET es gz, mientras 1,128 MB de
Puppeteer y 145 KB de Choreography son en disco. Sumados como están dan 3,97, no 3,2.

No es error de medición, es de presentación, y vale arreglarlo antes de que alguien lo use para
desacreditar el número — porque **el número aguanta**: ~3,2 MB comprimido / ~8,7 MB en disco, y el pie
aclara que es **Debug y sin trimming**, o sea el peor caso.

## Inconsistencias concretas a corregir

- **La lámina 4 se contradice sobre quién escribe.** El encabezado dice *«el único escritor eres tú (a
  través de los módulos)»* y el recuadro «tu espacio privado» dice *«ni el Host ni los módulos pueden
  escribir en él»*. La intención se entiende —el módulo es la pluma, no el autor— pero hay que elegir
  una redacción.
- **«Compra #4821» marcada PROCEDENCIA TÚ** contradice la lámina 1, que pone *«una compra»* como
  ejemplo de hecho **emitido por la institución**. Y en el mismo feed la carga de gasolina va como
  INSTITUCIÓN. Falta la regla: probablemente el hecho lo emite quien lo *certifica*, no quien lo
  protagoniza.
- **«Cifrado de extremo a extremo»** bajo la caja del journal. E2E implica dos extremos y un canal; para
  un registro en reposo se diría «cifrado en reposo». Probablemente aplica al camino de compartir, pero
  como está invita a un escrutinio que no hace falta.
- Typos: **«carve wasm»** en la v1 y **«clave wasm»** en la v2; **«Prestato total»** donde va «Sustrato
  total».

## En una línea

Con las seis juntas: **una sola instalación provee el sustrato; cada institución entrega solo
significado, datos y presentación; cada módulo vive en una arena con sus propias paredes, llaves y
cupos; la persona es la única autora de una historia indeleble donde cada asiento dice quién lo emitió,
firmado por el Host; y un asistente reemplazable es lo único con derecho a cruzar dominios, solo con
permiso, y sin poder dejar una sola letra escrita.**

Lo que sostiene todo eso no es el ahorro de megabytes — es que **el Host puede decir la verdad sobre un
módulo sin pedirle permiso al módulo**. Sin introspección, el resto es una promesa.
