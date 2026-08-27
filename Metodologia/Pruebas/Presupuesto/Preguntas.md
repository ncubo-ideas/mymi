# Preguntas — Presupuesto personal y familiar

Lo que esta app se compromete a poder contestar. Salió de la entrevista del **2026-08-26**, antes de
escribir una línea de código y sin mirar ningún modelo previo.

**Este dominio es híbrido, y no es un detalle.** El banco manda los estados de cuenta; la persona
aporta lo que el banco no puede saber. Sin esa segunda mitad, ninguna de las preguntas de abajo tiene
respuesta.

---

# Parte 1 · Lo que el modelo sabe

Capacidad, no preguntas. Declarado esto, las preguntas se componen encima.

| | Qué es |
|---|---|
| **Dimensión · el gasto recurrente** | Una **identidad**, no una cadena. El banco lo nombra distinto cada vez: `CNFL`, `CNFL S.A.`, `PAGO SERV ELECTRICO` |
| **Dimensión · la razón** | El **propósito** del gasto, no el comercio. El banco dice "farmacia"; el propósito puede ser salud o un regalo |
| **Dimensión · el bien** | A qué cosa se atribuye un gasto. La propiedad alquilada, el carro |
| **Grano · el cargo** | Monto, fecha, medio de pago |
| **Grano · el evento imprevisto** | **Agrupa varios cargos.** Una hospitalización produce hospital, farmacia y laboratorio: tres cargos, un imprevisto |
| **Repertorio** | Las categorías que se deducen solas: cine, delivery, bar, restaurante, viajes |

## Las marcas de la persona son hechos, no configuración

Aparecieron cuatro veces en la entrevista, sin que nadie lo propusiera como patrón: qué cargos son el
mismo gasto, qué es reducible, qué es de la propiedad, qué cargos forman un imprevisto. Siempre con
la misma forma — **marcar primero, inferir después**.

Eso las convierte en hechos con procedencia `TÚ`, que van al journal como comandos. Y trae una
consecuencia arquitectónica que en los otros negocios es hipotética y acá es del día uno:

> **El journal deja de ser reconstruible desde URL2.** Borrar la carpeta ya no es "volver a bajar los
> eventos": es perder el trabajo de la persona. El respaldo deja de ser opcional.

## Y trae un costo que hay que vigilar

Cada marca es trabajo que la app le pide a la persona. Cuánto puede pedir antes de que la abandonen
es una restricción real de este diseño, y **no hay respuesta todavía**.

---

# Parte 2 · Los juicios que el dominio toma

La parte que sí se enumera. Ninguno se deduce de los datos: existen porque alguien los decidió.

| # | El juicio | La decisión |
|---|---|---|
| 1 | **Qué hace que dos cargos sean el mismo gasto** | ⚠️ **PENDIENTE.** Nadie lo sabe todavía. Ver abajo |
| 2 | **Qué variación importa** | **10 %, y sobre el presupuesto mensual TOTAL** — no sobre cada gasto por separado. Cuando el total se mueve, se va a buscar a los responsables |
| 3 | **Cada cuánto se espera cada cargo** | Mensual, trimestral o anual. Parte lo declara la persona, parte se deduce de la historia |
| 4 | **Cuánto tarde es tarde** | Que no haya llegado antes de la fecha de pago. Sin ese dato, **3 días de gracia** — por fin de semana o feriado |
| 5 | **Qué tan grave es una ausencia** | **La decide el compromiso, no el monto.** Un recibo de luz que no llegó es un aviso; una cuota de préstamo o un pago de tarjeta es **crítico** |
| 6 | **Qué gasto es reducible** | Deducible para el consumo discrecional (el repertorio); **marcado por la persona** para todo lo demás |
| 7 | **Contra qué se compara el rendimiento de un bien** | **Es parámetro de la persona.** A uno le basta con que se pague sola; otro la compara contra un certificado de depósito o contra cripto. **Piso fijo:** contra sus propios períodos anteriores, que siempre está disponible |
| 8 | **Qué es un imprevisto** | **Lo no presupuestado ES el imprevisto** — no hay presupuesto declarado. Los **candidatos** se deducen: un cargo que ningún gasto recurrente conocido reclama. El **evento** lo marca la persona, agrupando cargos. La tasa se calcula **sobre eventos, no sobre cargos** |

### Por qué el 2 es distinto de lo que parecía

La primera versión era de abajo hacia arriba: cada gasto vigila su propio 10 % y avisa. Se descartó
en la misma entrevista: con doce gastos vigilándose, algo dispara todos los meses y nadie mira la app
a los tres meses. **Se vigila el total; el detalle aparece sólo para explicar un movimiento.**

### El pendiente del 1, acotado

Es el mismo problema que Exámenes resolvió con la identidad del analito. Pero **la solución de
Exámenes no se puede copiar**: ahí el conjunto de analitos es cerrado y se embarca con la app. Los
gastos recurrentes de una persona son suyos y abiertos — ningún módulo los puede traer precargados.
Así que la identidad la establece la persona marcando, o se deduce de la historia. **Falta decidir
cuál, y quién lo sabe.**

---

# Parte 3 · Lo que la UI muestra

Se decide **al final**, y va a mostrar menos que lo que el modelo sabe. Pendiente.

Un requisito que ya se sabe, y que sale de que el dominio sea híbrido: **la clasificación tiene que
ser visible y corregible.** No alcanza con mostrarla — la persona es quien la establece.

---

# Las preguntas que originaron todo esto

No es la lista de lo que la app contesta: son las que salieron de la entrevista y contra las que se
verifica que el modelo sirve.

1. *"¿Se incrementó el pago de un gasto fijo?"*
2. *"Los gastos de mi propiedad alquilada subieron. ¿Vale la pena el alquiler que recibo?"*
3. *"Si tomo este préstamo y tengo que pagar esta mensualidad, ¿qué gastos puedo reducir?"*
4. *"¿Qué pago fijo no me cobraron este mes?"* — **la ausencia.** Ninguna pantalla la puede mostrar
5. *"¿Cuánto debería estar apartando para imprevistos?"*

La **3** no tiene equivalente en Exámenes ni en Compras: es **hacia adelante y contrafáctica**. No
pregunta qué pasó, pregunta qué pasaría. La doctrina actual está construida sobre leer la serie del
pasado, y esta pregunta no encaja ahí.

---

# Lo que se rechazó

⚠️ **Casi vacío, y eso es un hallazgo.** En salud, *"¿este dato le sirve a la persona en este
dominio?"* rechaza el precio del examen de un plumazo. En presupuesto no rechaza casi nada, porque el
dominio **es** la plata. El filtro se cae acá y todo el peso queda en los otros dos.

| Se descarta | Por qué |
|---|---|
| **Ser una contabilidad** | No descarta un dato: descarta una **ambición**. Sin esto, en tres meses alguien pide partida doble y conciliación |
| Los estados de cuenta de **tarjeta de débito** | Se duplican con los de la cuenta bancaria |

---

# Candidatos a cruce

Presupuesto es el **receptor universal**: casi todo lo que los otros dominios descartan, acá sirve.

Y el cruce no es el gasto —**el banco ya lo tiene**— sino el significado:

| El banco pone | El otro módulo pone | De dónde vendría |
|---|---|---|
| ₡43.500 en el súper | qué había en la canasta | Compras |
| ₡28.000 en el laboratorio | que fue un perfil lipídico | Exámenes |
| ₡25.000 en la gasolinera | los litros y el kilometraje | Gasolinera |
| ₡180.000 en el taller | qué se reparó, y si tiene garantía | Agencia Auto |

> **La tensión que esto le pone al anexo A.** Si Presupuesto necesita el detalle de todos los demás,
> o duplica sus datos —y rompe el aislamiento— o el cruce ocurre al preguntar, vía el asistente. Lo
> segundo es lo que dice el capítulo 0 §6, y significa que **Presupuesto no puede calcular totales
> propios con datos ajenos: sólo puede preguntar.**

---

# Verificación

| Se dijo | Dónde se comprueba |
|---|---|
| El gasto recurrente es identidad, no cadena | El tipo no es `string`. Dos textos distintos del banco resuelven al mismo gasto |
| La vigilancia es sobre el total | Existe una operación que evalúa el presupuesto mensual completo, no sólo gastos sueltos |
| La severidad la decide el compromiso | La severidad es miembro del tipo de compromiso, no un umbral sobre el monto |
| El punto de comparación es parámetro | La operación de rendimiento **recibe** contra qué comparar, y tiene un caso por defecto contra la propia historia |
| La tasa de imprevistos es sobre eventos | La operación cuenta eventos agrupados, no cargos sueltos |
| El dominio no lee el reloj | Ninguna clase del dominio referencia `DateTime.Now`. Las fechas de vencimiento entran por parámetro |
| Hay una pregunta que sólo contesta la IA | Al menos una tool que ninguna vista renderiza. Candidata: la 4 |

---

# Pendientes, con nombre

| # | Qué falta | Quién puede saberlo |
|---|---|---|
| 1 | Cómo se establece la identidad de un gasto recurrente | Sin asignar |
| 2 | Cuánto trabajo de marcado puede pedir la app antes de que la abandonen | Es decisión de producto, no de dominio |
| 3 | Qué más se descarta | Sin asignar. El filtro del dominio no ayuda acá |
