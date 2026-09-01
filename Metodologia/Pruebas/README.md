# Pruebas — los sujetos con los que se validan las guías

Acá viven los dominios que se construyen **para probar que la Metodología funciona**, no para enseñar.

## No confundir con `Ejemplos/`

Son dos cosas distintas y mezclarlas arruina las dos:

| | Para qué está | Cómo es |
|---|---|---|
| **`Ejemplos/`** | Enseñar. Alguien lo abre, lo toca, lo rompe y entiende | **Chiquito.** Lo mínimo que se lee de una sentada |
| **`Pruebas/`** | Falsar. Se sigue la guía hasta el final y se ve dónde se cae | **Completo**, aunque duela. Una prueba a medias no prueba |

Un dominio de prueba puede ser grande y feo. No importa: nadie lo va a leer para aprender.

## Y tampoco son los negocios del repo

`Examenes/`, `Compras/` y `HolaMundo/Facturas/` son negocios reales, con sus dueños. La Metodología los
**cita** y los usa como vara para calibrar sus [observables](../Guias/observables.md), pero no los
administra y no depende de ellos.

Lo de acá es al revés: son de la Metodología, existen por la Metodología, y si la Metodología cambia de idea se
tiran.

---

## Los sujetos

| | Qué prueba | Estado |
|---|---|---|
| [`Presupuesto/`](Presupuesto/) | La cadena completa del orden de trabajo, de punta a punta | **Construido.** `Preguntas.md` → dominio → consola → MCP. 31 comprobaciones |
| [`Gimnasio/`](Gimnasio/) | **El punto de partida corrido con un peer del equipo** — la primera vez que la entrevista se usa con alguien que no la escribió | **Construido y atado al perform.** 19 clases, 46 comprobaciones, el ciclo con journal. [11 huecos](Gimnasio/HUECOS.md) |

### Por qué Gimnasio

Presupuesto probó si el `Preguntas.md` alcanza para modelar. **Gimnasio prueba algo distinto: si la
entrevista funciona con alguien que no la escribió.** Corrida el 2026-08-28, con un peer del equipo.

Y el dominio, además, es el **complemento exacto** de Presupuesto:

| | Presupuesto | Gimnasio |
|---|---|---|
| La institución | manda una parte | **manda todo** |
| La persona | aporta lo que el banco no sabe | **no aporta nada, por decisión de producto** |
| El journal | **no** es reconstruible | **sí** lo es |

Que los dos extremos estén cubiertos es lo que hace que las guías se puedan falsar en las dos
direcciones.

### Por qué Presupuesto

Es un dominio que **rompe el molde de los otros tres**, y por eso vale como prueba:

- Es **híbrido** — el banco manda una parte, la persona aporta el resto. Los otros reciben todo de
  la institución.
- Por eso su journal **no es reconstruible** desde el primer día, cosa que en los otros es una
  advertencia a futuro.
- Y una de sus preguntas es **hacia adelante y contrafáctica** —*"si tomo este préstamo, ¿qué gastos
  puedo reducir?"*— que es algo que la doctrina actual no sabe explicar, porque está construida
  sobre leer la serie del pasado.

Si las guías sobreviven a este dominio, sobreviven a bastante.

## Qué se probó exactamente, y qué contestó

El eslabón era: **¿alcanza el `Preguntas.md` para modelar sin volver a preguntar?**

**No alcanzó, y ése es el hallazgo.** El dominio se construyó entero con el archivo en la mano, pero
aparecieron **ocho huecos** — cosas que había que decidir y que nadie había decidido. Están en
[`HUECOS.md`](Presupuesto/HUECOS.md), cada uno con la decisión que se tomó para seguir.

Que hayan aparecido no invalida el método: **los ocho se descubrieron modelando, no discutiendo**, y
eso era justamente lo que se quería saber. Dos merecen atención especial:

- **El hueco 2 rompe un juicio declarado.** Lo que no es mensual no se prorratea, y el juicio 2 decía
  vigilar el total mensual. Un juicio del archivo era **inconsistente con otro del mismo archivo**, y
  eso no se ve leyendo: se ve al escribir la clase.
- **El hueco 7 se cerró por borrado**, no por relleno. La respuesta fue que la pregunta sobraba.

Uno cerrado, siete abiertos. **Que sigan abiertos es correcto:** son decisiones de negocio, y el
sujeto de prueba no tiene dueño de negocio que las tome.

> **Lo que esto le agregó al método.** La entrevista tenía cuatro bloques y una sola pasada. Ahora
> tiene un quinto que **vuelve** sobre las respuestas después del primer intento de modelar — porque
> ocho huecos en un dominio chico dicen que una pasada nunca alcanza.
