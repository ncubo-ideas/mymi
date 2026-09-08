# 01 · Qué quisimos hacer en el teléfono

**Corte: 2026-09-07.**

## El problema, en una frase

En la computadora, la IA de la persona llega a sus módulos porque el Host se los ofrece por MCP
y el asistente de escritorio sabe hablar MCP. **En el teléfono eso no existe.**

Ninguna app de asistente en un celular —ni Claude, ni ChatGPT, ni Gemini— llama a un servidor
local. **[MEDIDO 2026-08-26]**, y sigue siendo cierto: es la fila que en `03` aparece como
«Puerta 1 · ningún asistente móvil llama a un servidor local». Así que la pregunta del teléfono
no era «cómo portamos el Host», que resultó ser lo fácil. Era **quién le pregunta a Gnothi**.

## El eje que ordenó todo: quién corre el bucle

Ésta es la distinción que nos ahorró meses de comparar cosas que no se comparan, y sale del
criterio de Álvaro en `NotasGnothi/08`: lo que separa una puerta de otra **no es el protocolo,
es quién corre el bucle de tool-calling**.

| | quién corre el bucle | qué implica |
|---|---|---|
| **Puerta 1** | un asistente externo (MCP) | Gnothi es un servidor. Sólo escritorio |
| **Puerta 2** | **nosotros** — nuestro `AgentLoop` | Gnothi arma la respuesta. Corre en los tres aparatos |
| **Puerta 3** | el asistente del sistema (Siri, Gemini) | el SO rutea, Gnothi contesta una intención |

Dicho para quien no vive en el código: **una puerta es a quién le pregunta la persona — y quien
le pregunta es el que arma la respuesta.**

El equipo además usa «Puerta 4» para Apple Intelligence. No es una cuarta forma: es la Puerta 2
**sin llave** — nuestro bucle, con el modelo del propio teléfono en vez de uno en la nube.

## La regla que ordenó las dos plataformas

De `NotasGnothiHost/12`, y es la que evitó que termináramos con dos productos:

> **Arquitectura: una. Plataforma: dos.**

Todo lo que es **decisión** —la CSP, el journal, el cursor, el protocolo del puente, el
consentimiento, el alta— vive una sola vez, en código compartido. Cada plataforma pone
únicamente **el adaptador**: cómo se le entrega una respuesta a su motor web, cómo se guarda un
secreto, cómo se recibe un enlace.

No fue estética. Dos copias de la CSP son dos CSP el día que una deja de ser la real, y nadie se
entera hasta que un módulo se lleva algo.

## Qué queríamos que se pudiera decir al final

Tres cosas, y las tres son falsables:

1. **La persona le pregunta a su IA y la respuesta cruza módulos** que ninguna institución
   comparte entre sí — el laboratorio no sabe del supermercado.
2. **El dato no se mueve.** El journal está en el teléfono de ella, en una carpeta que Gnothi le
   administra a cada institución.
3. **El módulo no puede salir a la red.** Lo único que sale es el cartero de Gnothi, con la
   credencial de Gnothi.

Las tres se cumplen hoy en Android. La 2 y la 3 están construidas en iOS y **[NO MEDIDO]** en un
aparato — ver `03`.
