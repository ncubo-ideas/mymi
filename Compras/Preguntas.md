# Preguntas — Supermercado La Guaria

Lo que esta app se compromete a poder contestar.

> ⚠️ **Este archivo se escribió DESPUÉS del código, y eso le quita una de sus dos funciones.**
>
> El [punto de partida](../Metodologia/Guias/punto-de-partida.md) lo define como un **insumo**: sale
> de la entrevista, antes de escribir una línea, y de ahí en adelante se construye con él en la mano.
> Acá el orden fue el inverso. La consecuencia es concreta: **la auditoría de deriva del paso 8 no
> vale casi nada contra este archivo.** Si el juicio dice "9 días" porque lo leí de `Cadencia.cs`, el
> observable va a reportar cero deriva por construcción — un boletín perfecto que no probó nada.
>
> Lo que sí hace este archivo es lo otro que la guía le pide, y que todavía nadie hizo: **poner los
> juicios donde el que conoce el negocio los pueda aprobar o rechazar.** Están marcados uno por uno.

**Este dominio recibe todo de la institución.** El súper publica los eventos de cada compra; la
persona no registra nada. El journal es reconstruible desde URL2 desde el primer día — al revés de
`Presupuesto`, y eso hace que acá no haya ningún juicio que dependa de que alguien marque algo.

**La frontera, que es el juicio de más arriba y no es nuestro:** este módulo concluye **sobre la
compra** y nunca **sobre el cuerpo**. Sale de la spec de marca de La Guaria y ya costó una corrección
—se fueron `PerfilDeConsumo` y `Veredicto`—, así que está en la Parte 2 con el número 0.

---

# Parte 1 · Lo que el modelo sabe

Capacidad, no preguntas. Declarado esto, las preguntas se componen encima.

| | Qué es |
|---|---|
| **Dimensión · el producto** | Una **identidad** (el sku), no una cadena. El súper lo describe distinto entre tickets: `Leche semidescremada 1L` y `LECHE SEMIDESC 1 LT` son el mismo producto |
| **Dimensión · el departamento** | El pasillo. **Conjunto cerrado de doce**, no texto libre: si un evento trae uno que no existe, la línea se descarta en la frontera |
| **Dimensión · la presentación** | Por unidad, con contenido (`1 L`), o en paquete (`6 pk`). Es lo que permite comparar precios entre tamaños |
| **Grano · la compra** | El ticket: número, total, y la fecha **con hora** |
| **Grano · la línea** | Qué producto, cuántas unidades, a qué precio, y si trae garantía |
| **Repertorio** | Los departamentos que **se reponen** y cada cuántos días, contra los que no se reponen nunca |

## La hora no es decoración

La fecha lleva hora porque una de las lecturas es *a qué hora vas al súper*, y sin la hora no existe.
Vale dejarlo escrito porque **ya nos morfió una vez**: el guion de demo tenía las 87 compras a las
17:40 —un relleno que "nadie usaba"— y la pantalla contestó, con toda seriedad, que esta familia hace
el 100 % de sus compras por la tarde. Un dato de relleno deja de ser inofensivo el día que alguien lo
lee.

## Lo que el modelo NO sabe, y conviene que quede dicho

**Nada de la persona.** No hay nombre, ni edad, ni cuántos son en la casa, ni dónde vive. El módulo
sabe qué se compró y cuándo; quién lo compró es el dueño del dispositivo y no hace falta modelarlo.

**Nada del cuerpo.** No hay peso, ni calorías, ni nutrientes, ni una tabla que diga qué es sano.

**Nada de otras instituciones.** No hay forma de que este módulo lea el journal del laboratorio.

## Y trae un defecto que hay que vigilar

⚠️ **La presentación está en el modelo pero no llega.** `Presentacion` sabe leer `1 L` y `6 pk`, pero
el evento del proveedor no manda el campo, así que toda línea se construye por unidad y **cada precio
de la app dice "por u"**. Arreglarlo cambia el contrato del proveedor, no nuestro código.

---

# Parte 2 · Los juicios que el dominio toma

Esta es la parte que importa. Un juicio no se compone ni se infiere: existe sólo si alguien lo
decidió, y **si nadie lo decidió, el modelo no lo puede inventar después**.

La columna de la derecha es la que hay que leer:

- **✓ de la spec** — sale de la wiki del ecosistema o de la spec de marca. Ya está aprobado.
- **⚠️ propuesta** — lo decidí yo construyendo, con su razón escrita al lado en el código, y **nadie
  del negocio lo ha visto todavía**.

| # | El juicio | La decisión | ¿Aprobado? |
|---|---|---|---|
| **0** | **Hasta dónde llega nuestra conclusión** | **Sobre la compra sí; sobre el cuerpo nunca.** "El helado pasó de 12 a 34" es nuestro. "Comés mucha azúcar" es del asistente, con permiso | **✓ de la spec** |
| 1 | **Cuáles son los departamentos** | Los doce del conjunto cerrado. Embutidos y comidas preparadas son **distintos** de proteínas, y mascotas existe | **✓ de la spec** (invariantes de la wiki) |
| 2 | **Qué vuelve habitual un producto** | **La mitad** de las compras miradas. En una de cada tres no es "lo de siempre"; en todas es una lista demasiado corta para reconocerse | ⚠️ propuesta |
| 3 | **Cuántas compras se miran para la firma** | **8** — unas dos meses de compras semanales: alcanza para que lo de siempre se repita, y es poco para que un cambio de hábito quede sepultado | ⚠️ propuesta |
| 4 | **Qué cuenta como cambio de ritmo** | **El escalón del conjunto, no los días.** De 7 días a 6 no es un cambio; de "todas las semanas" a "una vez al mes" sí | ⚠️ propuesta |
| 5 | **Cada cuántos días es cada cadencia** | 9 / 18 / 45 / 75, y el resto es "de vez en cuando". **La holgura es a propósito** (ver abajo) | ⚠️ propuesta |
| 6 | **Cuántas compras hacen falta para hablar de un ritmo** | **3 de cada lado.** Con dos, una compra corrida ya parece un cambio de hábito | ⚠️ propuesta |
| 7 | **Qué tan fiel es un producto** | Nunca falla (todas), casi nunca falta (¾), va y viene (⅖), de vez en cuando (el resto) | ⚠️ propuesta |
| 8 | **Cuándo empieza la tarde y la noche** | **La 1 pm y las 7 pm.** Es un juicio **cultural**, no técnico (ver abajo) | ⚠️ propuesta |
| 9 | **Qué parte del movimiento explica una diferencia** | **Un décimo** del total movido. Debajo de eso es ruido y nombrarlo estorba | ⚠️ propuesta |
| 10 | **Qué movimiento de PRECIO es real** | **3 %.** Menos es redondeo de góndola; más lo siente quien paga | ⚠️ propuesta |
| 11 | **Qué movimiento de GASTO es real** | **20 %.** Un quinto arriba o abajo es variación de despensa; más que eso es otro hábito. **No es el mismo juicio que el 10** (ver abajo) | ⚠️ propuesta |
| 12 | **Cada cuánto se repone cada departamento** | Frescos 10 días, lácteos 12, abarrotes 35, limpieza 45, mascotas 30. **Y cinco departamentos no se reponen nunca** (ver abajo) | ⚠️ propuesta |
| 13 | **Qué es comestible** | Y **la comida de la perra no lo es**, aunque sea comida | ⚠️ propuesta |
| 14 | **Cuándo urge una garantía** | Conviene revisarla a los 90 días de que se cierre la ventana; hay que moverse a los 30 | ⚠️ propuesta |
| 15 | **Cómo se escribe la plata** | `₡` y punto de miles. De Costa Rica, y armado a mano porque el wasm puede venir sin los datos de globalización | ⚠️ propuesta |
| 16 | **Contra qué pasado se mide la canasta** | **3 meses.** El "entonces" de cada habitual es el último precio que ELLA pagó en o antes del corte; la suma va en colones por la presentación MÁS RECIENTE (si pasó a la leche de 2 L, se pregunta cuánto costaba entonces ESA cantidad); un habitual sin precio tan viejo queda **afuera de la suma**, no en cero | ⚠️ propuesta (del mock de Fernanda) |
| 17 | **Qué es una "oportunidad"** | **La mayor bajada real** de los habituales, con el mismo 3 % del juicio 10. Solo bajadas: la tarjeta es una invitación, y las subidas ya tienen sus filas | ⚠️ propuesta (del mock de Fernanda) |
| 18 | **La buena noticia de un recibo** | De lo que llevó en ESA compra, **una sola** rebaja —la mayor—, por pieza de esa línea. "Hoy" es la última observación del historial (sus propios precios, no la góndola), solo si es posterior al recibo, y con el mismo 3 % del juicio 10 | ⚠️ propuesta (del mock de Fernanda) |

### Por qué la holgura del 5 es deliberada

Lo semanal llega hasta **9 días**, no hasta 7. Si el corte fuera exacto, una semana en que se fue el
jueves en vez del martes convertiría "todas las semanas" en "cada dos semanas", y la app anunciaría un
cambio de rutina que no existió. **Los cortes están corridos hacia arriba para que el ruido normal de
una familia no se lea como una noticia.** El precio de esta decisión es el simétrico: un cambio real
de 7 a 9 días pasa desapercibido, y me parece el error menos malo de los dos.

### El 8 es cultural y alguien de acá tiene que firmarlo

Que la tarde empiece a la 1 pm y la noche a las 7 es un juicio **de Costa Rica y de una familia que
cocina en casa**, no una verdad. En un país donde se cena a las 9 está mal. Es el juicio de esta lista
que más me incomoda haber tomado solo, porque **no hay ninguna forma de descubrir que está mal
mirando el código**: la app va a contestar "por la tarde" con total seguridad, esté bien o mal el
corte.

### El 10 y el 11 no son el mismo juicio, y comparten nombre

Los dos se llaman `CambioQueCuentaComoReal` en su archivo, y valen 3 % y 20 %. **No es un error**:
uno mide cuánto se movió el **precio** de una cosa, el otro cuánto se movió el **gasto** en esa cosa.
Que el gasto suba un 15 % puede ser simplemente que compraste más. Van separados en esta lista
justamente para que nadie los unifique creyendo que arregla una inconsistencia.

### El 12 tiene una mitad que es una decisión de producto, no una tabla

Cinco departamentos tienen reabastecimiento **cero** —dulces, snacks, aceites, embutidos y comidas
preparadas— y eso no es un dato faltante: es la respuesta a *"¿qué pasa si hace un mes que no compra
helado?"* La respuesta es **nada**. La lectura de "esto lo reponías y dejaste de reponerlo" **no
existe para los antojos**, a propósito, porque un módulo de súper que le avisa a alguien que hace un
mes que no compra postre está opinando sobre el cuerpo por la puerta de atrás — y eso es el juicio 0.

### Y el 13 es una palabra que engaña

`EsComestible` es falso para el departamento de mascotas. La croqueta **es** comida, pero no de la
persona, y la única razón por la que existe la marca es para poder decir "de tu canasta comestible" y
que la comida de Luna no esté ahí adentro. Está escrito en el código porque la línea
`new NoComestible("Mascotas", ...)` se lee como un error si no.

---

# Parte 3 · Lo que la UI muestra

Es lo barato: una pantalla se cambia en una tarde. Cinco, y **una sola UI que se comporta distinto
según la resolución** — no dos.

| Pantalla | Qué contesta |
|---|---|
| **Mis compras** (portada) | La firma y el quiebre. **La portada no es la lista de recibos**, y eso es lo que la spec cambió |
| **Los que nunca faltan** | La firma completa, y en qué departamentos se llena el carrito |
| **Un producto** | Cuántas veces entró, cuántas unidades, cuánto costó, cuándo fue la última |
| **Los recibos** | La lista, con búsqueda. Existe, y **no es la portada** |
| **El detalle** | Un ticket, línea por línea, con sus garantías |

---

# Las preguntas que originaron todo esto

No son las que la app contesta: son las que salieron del guion del ecosistema y contra las que se
verifica que el modelo sirve.

1. *"¿Cómo viene mi canasta?"* — **sin tener que elegir meses.** La app compara sola contra el mes
   anterior y dice **de qué pasillos** sale la diferencia
2. *"¿Qué es lo que nunca me falta?"*
3. *"¿Qué cambió en mi rutina, y desde cuándo?"*
4. *"¿Qué dejé de comprar?"* — **la ausencia.** Ninguna pantalla la podría mostrar, porque lo que hay
   que ver es lo que no está
5. *"¿Cuánto me subió a mí?"* — su inflación, producto por producto. Lo que la wiki señala como *el
   dato que ninguna app de súper da hoy*

**"¿Hasta cuándo tengo garantía de esto?" no está en la lista, y el filtro es la razón.** El portal
del súper ya la contesta: está impresa en el ticket. Lo que el portal **no** puede contestar es *"¿a
cuál de todas mis compras se le está cerrando la ventana?"*, que necesita la historia entera — y eso
sí lo hacemos, pero es una lectura, no una de las preguntas que originan el modelo. Queda anotado
porque el módulo tiene dos tools de garantía y alguien podría leerlas como pregunta fundadora.

⚠️ **La numeración `S` de la wiki no está verificada contra el original.** Lo que sí está confirmado
localmente es el reparto: **S14 es la única pregunta que es sólo del súper** —y su respuesta esperada
es monto sin valoración—, mientras que **S1 y S10** son donde el asistente opina viendo tres
instituciones, y **S6** es el permiso de la nutricionista. Antes de auditar contra esto, hay que abrir
la wiki y confirmar los números.

**Las que este módulo NO puede contestar solo, y no es un defecto:** *"¿estoy comiendo mejor?"*
necesita el laboratorio y el gimnasio. El súper aporta su mitad —qué entró a la canasta— y el juicio
es del asistente. Declararlo evita que alguien lea este módulo como incompleto cuando está siendo
correcto.

---

# Lo que se rechazó

Con su motivo, para que la discusión no se repita en tres meses.

| Se descarta | Por qué |
|---|---|
| **El juicio de salud** | Existió: `PerfilDeConsumo` (recomendado / neutro / ocasional) y `Veredicto` (mejorando / empeorando). Los decidí yo cuando no había spec. Se fueron, y hay comprobaciones que revientan si vuelven |
| **La periodicidad de los antojos** | Ver el juicio 12. No descarta un dato: descarta una **conclusión** |
| **El rojo** | No existe en la paleta, a propósito, para que nadie pueda pintar una alarma sobre una compra |
| **La tarjeta de puntos y el perfil de cliente** | La spec es explícita: *"nosotros no la seguimos"*. Es del súper, no de la persona |
| **Recategorizar un producto** | Nombrado y no construido: cambiaría el significado de las canastas **ya leídas**. Un journal es append-only y una recategorización lo reescribe hacia atrás |
| **El peso variable y el granel** | Dos formas de presentación que exigen cantidad decimal, y los journals ya escritos la tienen entera |
| **El sku en pantalla** | `OLLA-ARR-18` es la llave interna. Ni en la UI ni en las urls: la ficha de un producto se navega **por nombre** |

---

# Candidatos a cruce

Anotados en vez de forzados adentro.

| Compras pone | El otro módulo pone | De dónde vendría |
|---|---|---|
| Qué había en la canasta | ₡43.500 en el súper, y de qué cuenta salió | Presupuesto |
| Que entraron 34 helados este mes | El perfil lipídico | Exámenes |
| ₡25.000 al mes en comida de la perra | Un gasto fijo que nadie había nombrado | Presupuesto |
| Los frescos contra los snacks | Lo que la nutricionista puede ver, con permiso | (el caso del permiso acotado) |

> **El caso canónico, al revés.** En Exámenes el dato que sobra es el **precio**: es real, está en la
> factura, y no aporta nada a la salud — pero no va a la basura, va a Presupuesto. Acá pasa lo
> inverso: **el precio es nuestro dominio** y lo que pertenece a otro es la **lectura nutricional** de
> lo mismo. El filtro *"¿le sirve a la persona en este dominio?"* no descarta casi nada de plata,
> igual que en Presupuesto.

---

# Verificación

| Se dijo | Dónde se comprueba |
|---|---|
| El producto es identidad, no cadena | El tipo no es `string`. Dos descripciones distintas del súper resuelven al mismo producto |
| El departamento es un conjunto cerrado | Se compara **por referencia**, y `Categoria.Todas` tiene doce. Un nombre desconocido no construye |
| El cambio de ritmo es de escalón | Compara **cadencias**, no días: `!ReferenceEquals(antes, ahora)` |
| El dominio no juzga el cuerpo | Hay comprobaciones que revientan si una línea dice peor / mejor / empeora / mejora / debería / conviene / cuidado / mal, o si alguien le cuelga una etiqueta de valor a un departamento |
| El dominio no lee el reloj | Ninguna clase referencia `DateTime.Now`. Las fechas de corte entran por parámetro |
| La ausencia se contesta | Existe una operación que responde qué **dejó** de aparecer, sobre los departamentos que se reponen |
| Los pares son totales | Ninguna lectura del dominio devuelve `null`. La ausencia se pregunta antes de leer |

**461 comprobaciones en verde** al 2026-08-27.

## Lo que escribir este archivo encontró

Tres cosas que el código no delataba solo:

1. ⚠️ **El juicio 3 vive fuera del dominio.** El `8` es una constante de `ComprasAPI`, en la capa de
   aplicación. El dominio lo recibe por parámetro —eso está bien— pero la frase *"ocho son unas dos
   meses de compras semanales"* es conocimiento del negocio y está del lado equivocado de la línea.
2. ~~El corte del quiebre se recibe, no se encuentra.~~ **Arreglado.** `CambioEncontrado` prueba
   la primera compra de cada mes y elige el corte donde más productos entran y salen; la carátula
   ya no pasa ninguna fecha. Queda anotado porque el defecto **no dejaba un hueco visible**: le
   contestaba *"su canasta viene igual"* a quien había cambiado en otro mes, y eso se lee como un
   hecho y no como una falla.
3. ⚠️ **El defecto de la presentación** (Parte 1), que es del contrato del proveedor y no nuestro.

Ninguno de los tres es un incumplimiento de lo que este archivo declara — son el archivo haciendo su
trabajo la primera vez que se escribe.
