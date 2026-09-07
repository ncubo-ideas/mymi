# 01 — La Arquitectura Gnothi

## El problema, en una frase

La historia completa de Sonia **existe**, pero repartida: el laboratorio tiene sus exámenes, la agencia las revisiones del carro, la gasolinera las cargas, el supermercado las compras, el colegio las notas de sus hijos. Cada pedazo está bien guardado — ordenado para las preguntas *de esa institución*. Lo que no existe es un lugar donde esté entera. Ni siquiera para ella.

La propuesta **no le quita a la institución su copia.** Agrega un movimiento: que además de guardar, contribuya a la historia de su cliente. Que el dato viaje una vez más de vuelta a su dueño — no a una nube que junte millones de personas, sino al dispositivo de cada una.

## Lo que hace que esto no sea "otra app de datos personales"

Cada institución entrega **tres cosas juntas**, no una:

1. **los datos** de Sonia que ya tenía,
2. el **repertorio**: el conocimiento que sabe qué significan (que a los 5.000 km toca revisión; que ese modelo debería rendir 18 km/l; que un colesterol de 265 mg/dL está fuera de rango para una mujer de 41),
3. una **historia** que sigue creciendo.

Unas tablas con columnas y números serían datos sin alma: cuando las de la agencia se encontraran con las del laboratorio, nada sabría combinarlas. Acá sí, **porque el conocimiento viaja con los datos.**

Y entonces el asistente que Sonia elija puede consultar varios diarios juntos, con permiso de ella, en su equipo. Ahí ocurre lo nuevo — **el cruce**:

- *¿cuánto está gastando el carro?* → la agencia dice que ese modelo rinde 18 km/l; la gasolinera, cruzando cargas contra kilometraje, dice que está rindiendo 9. Algo le pasa al carro, **y ninguno de los dos módulos podía saberlo solo.**
- *¿por qué subió mi colesterol?* → examen del laboratorio × compras del supermercado.

## El argumento estructural (más fuerte que la privacidad)

Este es el que hay que entender bien, porque es el que sostiene el proyecto y **no es sobre privacidad**:

> Para el laboratorio, la unidad de registro es la **orden de servicio**: una venta, un evento facturable, cerrado en sí mismo. Diez años son diez órdenes independientes, y su ERP está bien diseñado para eso — es lo que necesita para operar.
>
> Para Pedro, la unidad es **el antígeno prostático como entidad viva con diez mediciones**. Un objeto continuo, no una colección de transacciones.
>
> Y ninguna institución puede reorientar ese eje **aunque quiera**: si Pedro cambió de laboratorio en el año seis, ninguno de los dos tiene la serie completa. **El único lugar donde el eje se puede reorientar hacia la persona es el dispositivo de la persona.**

La privacidad es **consecuencia**, no causa. Si en una discusión el argumento se desliza hacia privacidad, se está usando la versión débil.

## Las cuatro piezas de la arquitectura

### 1. El módulo no es una app: es carga del Host

Android no permite que una app A se comunique arbitrariamente con una app B. La salida no es resolver ese problema — es **hacerlo desaparecer**: el laboratorio nunca fue una app. Es un **dominio (DLL) + repertorio + view declarativa + manifiesto** que el Host carga. No hay nada que comunicar entre A y B porque B vive adentro de A.

Esto encaja con el mecanismo real del motor: `new ActorV2(nombre, typeof(T).Assembly)` — **el assembly de dominio ya es la unidad**, descubierta por reflection. El módulo *quiere* ser un DLL.

### 2. El Host lleva el sustrato

El runtime .NET y el motor (`Puppeteer` + `Choreography`) se instalan **una vez**, con el Host. Los módulos pesan decenas o centenas de KB porque no traen motor.

El motivo obvio es el tamaño (ver `05` para los números: la relación sustrato:dominio es del orden de 60:1). Pero hay un segundo motivo que **no depende de los bytes y es el durable**: hay **una sola cosa que auditar**, con build reproducible y firma, en vez de quince motores ajenos cada uno con su propia superficie de bug. Si el tamaño mejora con trimming, este motivo sigue en pie.

### 3. Dos puertas, una costura

El Host tiene un **despacho interno** que concentra todo lo que es de Host —catálogo de tools, permisos, cuotas, procedencia, hint de formato, invocación— y los módulos ven **una sola puerta, siempre la misma**.

- **Puerta 1**: un host externo (un asistente de escritorio) nos pide una tool call.
- **Puerta 2**: nuestro propio bucle de agente, que existe **solo** porque la app del asistente ajeno todavía no puede llamarnos en móvil.

El detalle completo y el mecanismo de borrabilidad están en `02`. Lo importante acá: la Puerta 2 es una **capa transitoria por diseño**, no un producto.

### 4. El Host puede responder lo que ningún módulo puede responder de sí mismo

Cuánto ocupás, a qué tasa creces, cuántos hechos me mandaste, qué podés hacer, quién firmó esto. Vía `IActorIntrospection`, **sin cooperación del módulo**. Esta pieza es lo que hace verdadero todo lo demás: sin ella, la propuesta es indistinguible de la de un data broker.

## La matriz de autoridades

Cuatro autoridades distintas, y **ninguna debe invadir a la otra**. Cuando algo en el diseño se sienta raro, revisar esta tabla suele resolverlo:

| autoridad | quién | qué decide |
|---|---|---|
| **semántica** | el módulo | qué significan los hechos, qué operaciones tienen sentido, qué invariantes hay |
| **interacción** | el Host | quién solicita, qué capability, quién puede proveerla, qué permisos, qué representación, cuánto cuesta |
| **consentimiento** | Sonia | qué se instala, qué se enciende, qué se comparte, qué se purga |
| **inferencia / composición** | el LLM | cómo combinar respuestas — **nunca autoridad sobre los hechos** |

Corolario que hay que tener presente todo el tiempo: **el Host no debe entender de colesterol, automóviles, facturas ni notas escolares.** Si el Host necesita entender el significado de un evento para hacer su trabajo, la capa se contaminó y hay que devolver ese trabajo al módulo.

## Capacidad, no permiso

La frase que resume el modelo de seguridad, y que sirve también para el pitch:

> **No lo hagas un acuerdo. Hacelo una incapacidad.**

Los acuerdos del sector se rompen; las capacidades que nunca se concedieron, no. Concretamente: WebAssembly y WASI son *capability-based* — el módulo no tiene nada que el host no le conceda. Sin sockets concedidos, un `POST` no es una regla violable: es **imposible**.

Por qué un token con scope **no** sirve para esto: lo emite la institución y lo valida el servidor de la institución. Los dos extremos son la misma parte. Pedirle que se autolimite el scope es pedirle al adversario que se ponga las esposas.

## Los invariantes que no se negocian

1. **El módulo no tiene red.** El Host trae los bytes.
2. **El Host no escribe el journal.** Trae bytes; el módulo asienta. Si el Host fuera el escriba, el asiento deja de ser el acto del puppet y pasa a ser un efecto de infraestructura. **El Host es cartero, no escriba: no puede abrir el sobre ni dictar la carta.**
3. **Un journal, un escritor (el actor); varios autores de hechos** (la institución y Sonia). Ver `03`.
4. **El módulo no despierta, no notifica, no interrumpe.** Sonia va a él; él no viene a ella.
5. **Ningún módulo ve a otro.** Un directorio por módulo, sin rutas compartidas. El cruce ocurre en el asistente combinando respuestas, **nunca en el filesystem**.
6. **El Host no es un bus entre módulos.** Y no por prudencia: el invariante de un solo escritor **prohíbe atomicidad cross-journal**, así que un techo por encima de los módulos *no puede agregar semántica* — ni transacción, ni estado, ni journal común. Solo le queda despachar. Intentar hacerlo bus semántico recrea microservicios dentro del teléfono sin comprar nada.

## Tres ejes que NO hay que soldar

Se fusionaron varias veces en las discusiones previas. Son independientes:

1. **¿Cómo llega el módulo y quién lo ejecuta?** → resuelto: el módulo es carga del Host.
2. **¿Los módulos quedan aislados entre sí?** → lo decide **de qué está hecho** el módulo, no cómo llegó. Ver `04`.
3. **¿Quién le habla al Host desde afuera?** → ahí vive la capa borrable. No tiene relación con los dos anteriores.
