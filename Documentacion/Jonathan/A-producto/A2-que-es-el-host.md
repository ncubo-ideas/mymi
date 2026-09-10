# A2 · Qué es el Host, en palabras simples

**El Host es un programa que instalás una vez y que corre los programas de otros en tu dispositivo,
sin que se toquen entre ellos ni te toquen a vos.**

Eso es todo. Lo que hace, en concreto:

**Trae el motor.** Los módulos no traen runtime. Solos no arrancan — no son programas, son carga: **dos
DLLs** (la superficie y el dominio, cero-deps) que no incluyen el motor. El Host es el que los pone a
andar, prestándoles el motor y la etiqueta mínima `Gnothi.Contract`. Ya no es a futuro: es lo que hace
hoy.

**Los corre separados.** Cada módulo en su espacio: su memoria, su carpeta, sus llaves, su cupo.
Ninguno ve al otro.

**Es la única salida a internet.** Nada entra ni sale del dispositivo si no es por él.

**Decide quién pregunta qué.** El asistente no le habla a los módulos: le pregunta al Host, y el Host
contesta solo lo que la persona autorizó.

**Firma cada hecho.** Deja marcado quién lo emitió —la persona o la institución— y eso ya no se puede
falsificar.

**Y puede abrir un módulo y decir qué hay adentro, sin preguntarle al módulo.** Por eso se puede dar
un permiso sabiendo de verdad qué se está permitiendo, en vez de creerle a la institución.

## La forma más fácil de verlo

Es la **administración de un edificio**. El edificio trae la estructura, el agua y la luz — nadie
construye su propia cañería. Cada departamento tiene su llave, su puerta y su medidor. Los vecinos no
se pasan a la casa del otro. Y hay una sola entrada, que el administrador controla.

Los módulos son los departamentos. La persona es la dueña. El asistente es alguien que quiere dejar
entrar un rato, a un lugar, para algo.

*(La analogía tiene un límite: un administrador de edificio no puede ver dentro de tu departamento, y
el Host sí. Eso está en la sección siguiente.)*

## Lo raro, y es lo importante

Es la pieza con más poder del sistema y **la que menos entiende**. Puede ver todo lo que hay en todos
los módulos, y no tiene idea de qué es un examen de sangre ni una factura. Su trabajo es **ejecutar,
separar, autorizar y medir**. Nada más.

Y justamente por eso se le puede confiar tanto. **Si el Host entendiera los datos, sería el lugar más
peligroso del sistema.**

## Los cuatro verbos, y por qué son esos

*Ejecuta · Aísla · Autoriza · Mide.* No son una lista bonita: se corresponden una a una con las cuatro
dimensiones del consentimiento.

| lo que la persona decide | quién lo puede hacer cumplir | verbo |
|---|---|---|
| **qué** se consulta | solo quien conoce el catálogo real | autoriza |
| **por quién** | solo quien autentica al que llama; el módulo no sabe quién pregunta | autoriza |
| **por cuánto** | solo quien cuenta a lo largo de la sesión | **mide** |
| **para qué** | solo quien puede exigir un propósito y atarlo a la llamada | autoriza |

Ninguna de las tres últimas la puede hacer cumplir un módulo. Un módulo no sabe quién pregunta, no
puede llevar un presupuesto entre llamadas, y no tiene a quién exigirle un propósito. **Las tres
necesitan interposición** — y por eso el Host no es una comodidad de arquitectura: es la condición de
posibilidad de un consentimiento con esas cuatro dimensiones.

## Lo que resuelve, y que no es obvio

**No pone un guardia: elimina las rutas.** El asistente no tiene dirección de un módulo; no existe el
cable. El permiso no se aplica *negando*, se aplica por **ausencia de camino**. Una regla que no se
puede eludir porque no hay alternativa es cualitativamente distinta de una regla que se verifica.

**Hace posible consentir sobre algo verdadero.** Si la descripción de un módulo viniera solo de su
propio manifiesto, la persona estaría concediendo permisos sobre una autodeclaración. El Host enumera
qué contiene y qué puede hacer **sin cooperación del módulo**, así que el catálogo de permisos se
deriva del artefacto, no de la promesa.

Eso también permite una granularidad que un sistema operativo no puede dar. Los permisos de un SO son
gruesos por necesidad —«esta app, esta clase de recurso, sí o no»— porque el SO no sabe qué hay
adentro. Acá el Host sí sabe, así que el permiso puede ser **por consulta**, no por aplicación. Es la
diferencia entre «dale acceso al laboratorio» y «puede ver resultados, no facturación».

**El asistente recibe respuestas, no acceso.** No hay handle, ni conexión, ni token que le sobreviva a
la llamada. Cada acceso es una decisión de autorización nueva.

**Y hace que revocar signifique algo.** Como todo pasa por el Host en el momento de la llamada,
revocar tiene efecto inmediato: no hay nada pendiente que recuperar.

## Una inversión fácil de pasar por alto

El Host lee la historia de la persona **sin pasar por el módulo**.

Eso quita a la institución del papel de portera de los datos de su propio cliente. Lo normal es que la
app que escribió tus datos controle tu acceso a ellos; acá el módulo escribe pero **no media la
lectura**. Es lo que hace verdadera la frase: *las instituciones pueden aportar hechos, pero no
controlarlos.*

## Apagar hace desaparecer, no denegar

Cuando la persona apaga una consulta, el asistente no recibe un «no tenés permiso»: **esa consulta no
existe** para él. Verificado corriendo: la llamada devuelve `Unknown tool`.

La diferencia importa. Un «no tenés permiso» le avisa que **hay algo ahí** y le da algo que rodear o
reintentar. Una consulta ausente no le da nada. Y el módulo tampoco se entera de que lo apagaron: no
puede distinguir «me apagaron» de «nadie preguntó» — si pudiera, tendría cómo insistir.

Y sigue **visible para la persona**: en su lista aparece encendido, con cero consultas publicadas.
Invisible para el asistente, visible para la dueña. Dos autoridades distintas, y se nota.

## Y algo que conviene tener claro

**No es una app que se usa.** Nunca «abrís el Host» como quien abre una app para pedirle algo. Es lo
que hace que lo demás pueda existir, como el sistema operativo de un teléfono — nadie usa Android, se
usan las cosas que Android permite correr.

Con un matiz que ahora hay que decir: **sí existe una ventana.** `Gnothi.Desktop` es una cara visible
—un panel de tarjetas nativo— que corre las consultas llamando al Dispatcher directo. Pero no es «el
Host»: es **una cara más** del sistema, como lo son las superficies del asistente. La analogía con el
SO aguanta —lo que corre por debajo no es el kernel—, pero la ventana está y conviene no negarla.
Ver `C7`.

## Dónde termina lo que puede resolver

El Host controla **el acceso**, no el destino. Una vez que el asistente recibe una respuesta
autorizada, esa respuesta viaja con el asistente; si es un modelo en la nube, el contenido está en la
nube de ese proveedor.

Por eso la garantía se redacta con precisión: *«la historia y los permisos siempre quedan en el
dispositivo»* — **no dice que las respuestas queden**. Y por eso el consentimiento es por consulta,
con propósito y cantidad, en vez de un permiso otorgado una vez: como no se puede controlar el
destino, se controla la granularidad y el registro.
