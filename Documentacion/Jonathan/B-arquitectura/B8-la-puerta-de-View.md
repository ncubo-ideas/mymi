# B8 · La puerta de `[View]` — lo que se salteó al no pasar por el despacho

**Planteado en revisión el 2026-09-14.** No es un bug: es una decisión de diseño que se tomó sin
discutirse, y una consecuencia que no se había visto.

---

## Antes de la observación: las tres puertas de una superficie híbrida

*Agregado el 2026-09-15. Estaba entendido pero no escrito, y sin esto el resto del documento no se
puede leer en frío.*

La superficie de un módulo híbrido es **una sola clase**, con sus métodos etiquetados. La etiqueta
decide **quién puede llamarlo**:

| marcador | público | ¿en el manifiesto? | ¿el modelo lo ve? | ¿escribe? |
|---|---|---|---|---|
| `[Query]` | **el asistente** | **sí**, una por una | sí | no |
| `[View]` | **la pantalla** | no | **nunca** | no |
| `[Deliver]` | **el cartero** | no | nunca | **es lo único que escribe** |
| `[Cursor]` | el cartero, antes de pedir | no | nunca | no |

### Los tres caminos

```
ASISTENTE                          PANTALLA                     CARTERO
   │                                  │                            │
   ▼                                  ▼                            ▼
Dispatcher.Serve                 cara.js                    WebModule.Pull
   ├─ Allows(tool)  ← consent    gnothiPuente.consultar           │
   ├─ meter.Count   ← contador       │                     Module.Cursor
   │                                 ▼                            │  [Cursor]
   ▼                            WebModule.Surface                 ▼
Module.Invoke                        │                      red (credencial
   │                                 ▼                       de Gnothi)
   ▼                            Module.View                       │
InProcessShell.Invoke                │                            ▼
   └─ Ask(nombre, ToolMarks)         ▼                     Module.Deliver
                              InProcessShell.View                 │
                                 └─ Ask(nombre, ViewMarks)        ▼
                                                        InProcessShell.Deliver
                                                           └─ DeliverAttribute
```

### Cómo se aíslan, y es más simple de lo que parece

**Cada puerta busca el método por nombre, pero sólo dentro de su propio conjunto de marcadores.**

```csharp
Invoke(tool, …)  => Ask(tool, …, ToolMarks);   // ["QueryAttribute", "McpServerToolAttribute"]
View(name, …)    => Ask(name, …, ViewMarks);   // ["ViewAttribute"]
```

Un método `[View]` **no le es negado** al asistente: **no existe** para él. Si el modelo pidiera
`"tendencia"` por la puerta de las tools, `Ask` recorre sólo los `[Query]`, no lo encuentra, y
contesta `module 'examenes' does not expose 'tendencia'`.

El comentario de `InProcessShell` dice por qué no se unificaron:

> *«No se mezcla con Invoke a proposito: si las dos puertas buscaran en el mismo conjunto, una
> consulta de pantalla podria llegar por la puerta del asistente —y al reves, que es peor— sin que
> nada lo impidiera.»*

**Son tres capas, no una:**

1. **El conjunto de marcadores.** Lo de arriba. Nadie alcanza lo del otro.
2. **El manifiesto.** Sólo los `[Query]` están declarados, y la comprobación del alta compara
   declarado contra real **sólo sobre `ToolMarks`**. Por eso compras reporta `matches:true` con
   `declared:[lista_de_compras, resumen]` aunque su superficie tenga seis `[View]`.
3. **Lo que cruza.** `consultar` va forzado a `Accepts.JsonOnly`: la página no elige su formato de
   salida, que es una palanca que las otras puertas sí dan.

### Lo que NO se aísla, a propósito

**El actor.** Los tres caminos usan el MISMO, vivo. No hay tres lectores ni un lector y un
escritor: uno solo (ver `B9` y `C9`). **El aislamiento es de las PUERTAS, no de los datos** — es el
mismo expediente con tres públicos, y un laboratorio no tiene «los datos del asistente» y «los
datos de la pantalla».

Y lo que cierra las escrituras no es un actor de sólo lectura: **son las puertas**. `[Deliver]` lo
busca la cáscara por su propio marcador, no está en el manifiesto, y `consultar` sólo llega a
`[View]`. No hay ningún camino desde el modelo ni desde la página hasta el método que escribe.

### Y de ahí sale la observación de abajo

Mirando el dibujo se ve solo: **`Allows` y `meter.Count` están únicamente en el camino del
asistente.** Los otros dos no los tocan. Eso es lo que el resto de este documento discute.

---

## La observación, tal como llegó

> `[View]` no pasa por el Dispatcher. No es solo que salta el consentimiento, que él lo pone a
> discusión: tampoco pasa por el Meter ni por la procedencia. La cara queda sin medir. Si el Host
> promete «Mide», hoy no mide lo que la pantalla pregunta.

---

## Qué hace el despacho, y qué se saltea

Cuando **el asistente** pregunta, la petición entra por `Dispatcher.Serve` y pasa por tres cosas en
orden:

```csharp
if (!m.Allows(r.Tool)) return Response.Denied(...);   // 1. el consentimiento de la persona
meter.Count(r.Module, r.Tool);                        // 2. el contador
object content = await m.Invoke(...);                 // 3. recien ahi, la pregunta
```

Cuando **la cara** pregunta, el camino es otro:

```
consultar  ->  WebModule.Surface  ->  Module.View  ->  shell.View
```

No toca el `Dispatcher`. Así que **no pasa por (1) ni por (2)**.

| | el asistente | la cara |
|---|---|---|
| entra por | `Dispatcher.Serve` | `Module.View`, directo |
| consentimiento (`Allows`) | ✅ | ❌ |
| contador (`Meter.Count`) | ✅ | ❌ |
| procedencia por petición | ❌ — **no existe** | ❌ |

---

## Las dos que son ciertas

**El consentimiento.** Es deliberado y está argumentado en `C9`: apagar una consulta decide qué
puede preguntar *el asistente* sobre la persona, no le prohíbe a la institución mostrarle su
expediente en la pantalla que ella acaba de abrir. **Son decisiones con sujetos distintos.** El
argumento sigue en pie — pero es exactamente la clase de decisión que tiene que estar discutida y
no asumida, y hasta hoy solo estaba escrita en un comentario.

**El contador. Este es el hueco real, y no se había visto.** Hoy **nada cuenta lo que pregunta la
pantalla**: ni el contador del asistente ni ningún otro. Una cara híbrida hace entre 5 y 21
lecturas del dominio por apertura, y de ninguna queda registro.

## La que no es cierta

**La procedencia.** `Provenance` vive en `Installation.cs` y `ModuleInstaller.cs`: es el **acta del
alta** — qué origen sirvió el bundle, cuándo lo aceptó la persona, qué versión — y se escribe una
vez al instalar. **No hay procedencia por petición**, ni para las tools ni para las vistas.
`[View]` no se la saltea: no existe para nadie.

---

## Dónde la conclusión se pasa de largo

El `Meter` lleva su propósito escrito encima, en `Dispatcher.cs`:

> *METERING is one of the Host's four verbs, and what gets metered is the **ASSISTANT**: how many
> times it asked and what for. It is not a quota to protect the device — it is so the person can
> see how much was taken from them.*

Lo que ese número mide es **cuánto te sacó el asistente**. Si se le sumaran las lecturas de la
pantalla dejaría de significar eso: se inflaría con **el propio acto de la persona mirando su
expediente**. Abre su laboratorio, el contador sube 21, y la frase «esto es lo que te sacaron» pasa
a ser falsa.

Así que *«el Host promete Mide y no mide lo que pregunta la pantalla»* es **cierto en el hecho y
discutible en la conclusión**: «Mide» nunca prometió medir a la persona mirándose a sí misma.

---

## La propuesta

**No meter `[View]` en el `Dispatcher`.** Hacerlo volvería a poner el consentimiento del asistente
por encima de la pantalla de la institución, que es justo lo que se decidió no hacer.

**Sí contarlo aparte.** Un segundo contador —«la pantalla preguntó N veces»— que contesta dos
preguntas que hoy no tienen respuesta:

- ¿cuánto le cuesta al aparato abrir una cara?
- y la que de verdad importa: **¿una cara está preguntando más de lo que su pantalla necesita?**

Ese número es el único instrumento que detectaría a un módulo usando `[View]` para barrer el
journal sin que el consentimiento lo frene. Hoy podría hacerlo y no quedaría rastro.

Son pocas líneas: `HybridShell.View` ya es el único punto por donde pasan todas.

---

## La pregunta de fondo, que es más grande que el contador

**Es la segunda vez que el mismo diseño genera la misma pregunta.** `[View]` fuera del manifiesto
significa también fuera del consentimiento, fuera del contador, y —cuando se agregue— fuera de lo
que venga después. Cada capa nueva del Host va a tener que acordarse de que hay un segundo camino.

Quizá lo que falta no es un contador más, sino decidir si **`[View]` necesita su propio despacho**
en vez de ir directo a la cáscara: un lugar donde poner lo que la pantalla sí debe atravesar, y
donde se vea que es otra cosa que el camino del asistente.

**Es para el arquitecto.** Las dos salidas dejan el mismo comportamiento hoy; lo que cambia es
dónde hay que acordarse mañana.

---

## Lo que queda anotado igual

Aunque la decisión se posponga, dos cosas ya son ciertas y conviene que no se pierdan:

1. **La cara no está medida.** Si alguien pregunta «¿cuánto pregunta un módulo?», hoy la respuesta
   solo cubre al asistente.
2. **El argumento del consentimiento está en un comentario de `ExamenesSurface` y en `C9`**, no en
   un lugar donde alguien lo vaya a buscar. Este documento es ese lugar.
