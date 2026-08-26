# Pruebas — los sujetos con los que se validan las guías

Acá viven los dominios que se construyen **para probar que el Taller funciona**, no para enseñar.

## No confundir con `Ejemplos/`

Son dos cosas distintas y mezclarlas arruina las dos:

| | Para qué está | Cómo es |
|---|---|---|
| **`Ejemplos/`** | Enseñar. Alguien lo abre, lo toca, lo rompe y entiende | **Chiquito.** Lo mínimo que se lee de una sentada |
| **`Pruebas/`** | Falsar. Se sigue la guía hasta el final y se ve dónde se cae | **Completo**, aunque duela. Una prueba a medias no prueba |

Un dominio de prueba puede ser grande y feo. No importa: nadie lo va a leer para aprender.

## Y tampoco son los negocios del repo

`Examenes/`, `Compras/` y `HolaMundo/Facturas/` son negocios reales, con sus dueños. El Taller los
**cita** y los usa como vara para calibrar sus [observables](../Guias/observables.md), pero no los
administra y no depende de ellos.

Lo de acá es al revés: son del Taller, existen por el Taller, y si el Taller cambia de idea se
tiran.

---

## Los sujetos

| | Qué prueba | Estado |
|---|---|---|
| [`Presupuesto/`](Presupuesto/) | La cadena completa del orden de trabajo, de punta a punta | Tiene su `Preguntas.md`. **Falta construir** |

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

## Qué se está probando exactamente

El eslabón que todavía no se probó: **¿alcanza el `Preguntas.md` para modelar sin volver a preguntar?**

Si se puede construir el dominio entero con ese archivo en la mano, el método se sostiene. Si a la
primera hora hay que ir a consultar tres juicios que faltaban, el archivo está incompleto — y
sabremos exactamente en qué, que es igual de útil.
