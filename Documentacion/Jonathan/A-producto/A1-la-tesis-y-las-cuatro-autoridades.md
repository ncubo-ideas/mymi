# A1 · La tesis, y el reparto de poderes que la sostiene

## El problema, en una frase

La historia de una persona **existe**, pero repartida: el laboratorio tiene sus exámenes, la agencia
las revisiones del carro, la gasolinera las cargas, el supermercado las compras. Cada pedazo está
bien guardado — ordenado para las preguntas *de esa institución*. Lo que no existe es un lugar donde
esté entera. **Ni siquiera para ella.**

## El argumento fuerte, que no es privacidad

Este es el que sostiene el proyecto, y conviene no confundirlo con el otro:

> Para el laboratorio, la unidad de registro es la **orden de servicio**: una venta, un evento
> facturable, cerrado en sí mismo. Diez años son diez órdenes independientes, y su ERP está bien
> diseñado para eso.
>
> Para la persona, la unidad es **el antígeno prostático como entidad viva con diez mediciones**. Un
> objeto continuo, no una colección de transacciones.
>
> Y ninguna institución puede reorientar ese eje **aunque quiera**: si cambió de laboratorio en el año
> seis, ninguno de los dos tiene la serie completa. **El único lugar donde el eje se puede reorientar
> hacia la persona es el dispositivo de la persona.**

La privacidad es **consecuencia**, no causa. Si en una discusión el argumento se desliza hacia
privacidad, se está usando la versión débil.

## Lo que hace que no sea «otra app de datos personales»

Cada institución entrega **tres cosas juntas**, no una:

1. **los datos** de la persona que ya tenía,
2. el **repertorio**: el conocimiento que sabe qué significan — que a los 5.000 km toca revisión, que
   ese modelo debería rendir 18 km/l, que un colesterol de 265 está fuera de rango para una mujer de 41,
3. una **historia** que sigue creciendo.

Unas tablas con columnas y números serían datos sin alma: cuando las de la agencia se encontraran con
las del laboratorio, nada sabría combinarlas. Acá sí, **porque el conocimiento viaja con los datos**.

Y entonces aparece lo nuevo — **el cruce**:

- *¿cuánto está gastando el carro?* → la agencia dice que ese modelo rinde 18 km/l; la gasolinera,
  cruzando cargas contra kilometraje, dice que rinde 9. Algo le pasa al carro, **y ninguno de los dos
  módulos podía saberlo solo.**
- *¿por qué subió mi colesterol?* → examen del laboratorio × compras del supermercado.

## Las cuatro autoridades

Esto no es un diagrama de componentes: es un reparto de **quién tiene derecho a decidir qué**. Es lo
más importante de todo el diseño, y cuando algo se siente raro, revisar esta tabla suele resolverlo.

| autoridad | quién | qué decide | qué **no** puede |
|---|---|---|---|
| **semántica** | el módulo | qué significan los hechos, qué operaciones tienen sentido | invocar, salir a internet, ver otro módulo |
| **interacción** | el Host | quién solicita, con qué permiso, qué representación, cuánto cuesta | interpretar el significado de un hecho |
| **consentimiento** | la persona | qué se instala, qué se enciende, qué se comparte, qué se purga | — |
| **inferencia y composición** | el LLM | cómo combinar respuestas | **nunca autoridad sobre los hechos** |

Cada pieza es **incompetente en el terreno de las otras**. De ahí se derivan todos los «lo que no
es»: el modelo no toca los módulos porque *inferir no es interactuar*; los módulos no se hablan porque
la semántica no tiene autoridad sobre la interacción; la institución no lee los otros módulos porque
su autoridad semántica es sobre *su* dominio y nada más.

## Por qué el aislamiento sale gratis, y qué se paga

La frase más filosa del diseño: **no hay comunicación entre módulos porque no hay dos aplicaciones.**

El aislamiento no se logra poniendo paredes: se logra eliminando la multiplicidad. La comunicación
entre apps es un problema *creado* por tener varias apps. Con un solo proceso, no hay nada que aislar
de nada, y toda la clase de diálogos de permiso «esta app quiere acceder a datos de esa otra»
desaparece porque la pregunta no existe.

**El precio:** si los módulos no se hablan, cruzar dominios solo puede pasar en un lugar.

## Por qué el LLM es constitutivo y no un adorno

**El asistente es el único órgano del sistema con derecho a cruzar dominios.** Sin él, seis módulos
aislados en un dispositivo son seis silos — exactamente igual que seis apps. El asistente es lo que
convierte la colección en una historia.

Y por eso el consentimiento está redactado con cuatro dimensiones —*«qué, por quién, por cuánto y para
qué»*—: el único punto de cruce es también el único punto de fuga.

Hay **dos composiciones distintas**, y viven en autoridades distintas:

| | quién | qué hace | qué necesita |
|---|---|---|---|
| **cronológica** | el Host | ordenar los hechos de todos los módulos en una línea de tiempo | introspección; **no entender nada** |
| **semántica** | el asistente | «¿puedo deducir este gasto?», «comparar seguros» | permiso explícito de la persona |

El Host puede ordenar sin comprender. Solo el asistente puede decir qué *significa* que esas cosas
hayan pasado juntas. Por eso la pestaña de historia no necesita permiso y una pregunta al asistente sí.

## La decisión más contraintuitiva: el asistente no escribe

El instinto de producto sería el opuesto — dejarlo guardar sus conclusiones para tener memoria. El
diseño lo prohíbe, por dos razones que aguantan:

**Una conclusión no es un hecho.** Si el asistente pudiera escribir, con el tiempo sus inferencias
serían indistinguibles de los hechos institucionales, y el journal dejaría de ser un registro para
volverse una mezcla de registro y opinión.

**Y es lo que lo hace reemplazable.** Si sus conclusiones quedaran grabadas, cambiar de asistente te
dejaría las opiniones del anterior incrustadas para siempre en tus datos. Sacándolo del journal, **el
asistente es intercambiable sin residuo**.

Eso quedó demostrado en la práctica: tres superficies distintas de asistente, con tres mecanismos de
registro completamente distintos, dieron el mismo dato — y ninguna sabía que había un módulo detrás.

## El consentimiento es un hecho journalizado

Entre un examen de sangre y una revisión técnica, en la historia de la persona, hay un asiento así:

> **Autorizo compartir con mi aseguradora** · 12 may 2025 09:20 · PROCEDENCIA **TÚ**

**El registro de consentimientos ES el journal.** El permiso no es una fila en una tabla de
configuración: es un hecho de su historia, con fecha, procedencia y append-only. Por lo tanto quién
consultó qué y con qué autorización queda auditable **por construcción**, y **revocar es agregar un
asiento, no borrar uno**.

## Capacidad, no permiso

La frase que resume el modelo de seguridad:

> **No lo hagas un acuerdo. Hacelo una incapacidad.**

Los acuerdos del sector se rompen; las capacidades que nunca se concedieron, no. Sin sockets
concedidos, un POST no es una regla violable: es **imposible**.

Por qué un token con scope **no** sirve para esto: lo emite la institución y lo valida el servidor de
la institución. Los dos extremos son la misma parte. Pedirle que se autolimite el scope es pedirle al
adversario que se ponga las esposas.

## El intercambio que hay que decir en voz alta

Al instalar el Host, el sistema operativo avisa en rojo: *«otorgará a esta extensión acceso a todo lo
que hay en tu computadora»*.

**No es una contradicción, es el diseño.** La confianza que se le quita a cada institución no
desaparece: se concentra en el Host. Es un buen intercambio —es más fácil auditar un host que quince
módulos, y la persona confía **una sola vez**— pero hay que decirlo, y hay que pagarlo con Host open
source, builds reproducibles y manifiestos públicos.

## Y el riesgo estructural, dicho sin maquillar

**El Host se vuelve plataforma, y una plataforma necesita dueño.** «Instalado una sola vez» significa
que una sola cosa controla el runtime de los módulos de *todas* las instituciones. Y esa frase ya
dejó de ser figura: hoy tiene cuerpo — un MSI per-usuario deja la marca de descubrimiento en
`HKCU\Software\Ncubo\Gnothi` y el runtime en `%LocalAppData%`, sin tocar la máquina ni pedir admin.
Ver `C7`.

Esa falla ya se vivió en miniatura durante este trabajo: un módulo compilado contra una versión del
motor dejó de arrancar cuando el motor pasó a un paquete publicado que no traía tres commits. Una
línea de configuración. En el modelo completo esa misma falla es **global** — una actualización del
Host puede romper todos los módulos a la vez.

Y ya no es riesgo de figura, porque la «carga» dejó de ser figura: el módulo instalado son **dos DLLs
—superficie y dominio— montadas sobre el motor que presta el Host**. El acople módulo-motor es
**permanente**, no una metáfora de flete: cada módulo depende, en cada arranque, de que el sustrato
del Host siga siendo el que esperaba.

Para eso está el manifiesto, y por eso **tiene que** declarar un **rango** de sustrato y no una versión
exacta: si pinea una versión puntual, cada actualización del Host huerfaniza todos los módulos
instalados. Ojo que esto dejó de ser recomendación y es **deuda urgente**: hoy el manifiesto solo trae
`load.entry` —cuál DLL trae la superficie—, no un rango. Mientras no lo tenga, el acople permanente
corre sin la red de contención que este párrafo describe.
