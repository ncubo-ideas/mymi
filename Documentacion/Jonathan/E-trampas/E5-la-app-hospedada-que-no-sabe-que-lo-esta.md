# E5 · La app hospedada que no sabe que lo está

**Medido el 2026-09-16** en un Xiaomi 2407FPN8EG con Android 16, con `municipalidad` real y el
journal real de la persona (23 eventos).

Una app hospedada **le pregunta al navegador dónde guardar** en vez de preguntarle a Gnothi si está
hospedada, y la persona ve «DÓNDE GUARDAR» donde debería estar su cuenta. **El Host no puede
arreglarlo, y hasta este arreglo tampoco podía decir qué pasó.**

Es la trampa más barata de cometer del set: **es un `if` que hay que acordarse de escribir**, en un
método, en una página, y nada en el paquete, en el alta ni en la compilación lo echa de menos.

---

## El síntoma

`municipalidad` instalado en forma hospedada. Se abre, y en vez de la cuenta:

> **DÓNDE GUARDAR**
> [ Elegir una carpeta ] [ Guardar en este navegador ] [ Abrir sin guardar nada ]

Y en el log del Host:

    comprobación de municipalidad: {"matches":false, "actual":[],
      "detail":"could not query the module: TimeoutException: The operation has timed out."}

Dos pantallas de la misma cosa: la persona ve una app que se cree recién instalada, y el Host ve un
módulo que no contesta. **Ninguna de las dos dice por qué**, y las dos se ven exactamente igual que
un bundle mal armado, una CSP que rechazó algo, un 404 en el documento o un actor que no hidrató.

## La cadena de evidencia, y lo que costó

No se dedujo: se midió, parchando el interop **en el teléfono** —`run-as`, editar el `.js` servido
desde el bundle, reabrir— y leyendo la consola por DevTools. Cuatro renglones, en este orden:

| sonda | qué dijo | qué descarta |
|---|---|---|
| `SONDA hospedada: tipo=object claves=gnothiPuente,gnothi` | el puente **sí está** | no es que Gnothi no lo inyecte |
| **cero** `SONDA pide` | la app **nunca llama** al puente | no es que el puente falle |
| `SONDA journal: estaSoportado / opfsSoportado / estaInstalada / modoActual` | le habla al **navegador** | la app está decidiendo sola |
| nunca llega `listo` | la espera de 60 s se vence | de ahí el `TimeoutException` |

La segunda línea es la que importa: **el puente estaba puesto y nadie lo tocó.** Todo lo demás
—CSP, 404, base en `/app/`, service worker rechazado— era ruido que parecía causa.

## La causa

`Municipalidad.Wasm/Pages/ElArranque.razor`, en `OnInitializedAsync`, preguntaba por el navegador
antes de preguntar por Gnothi:

```csharp
haySelector  = await estado.HaySelectorDeCarpetasAsync();
hayNavegador = await estado.HayAlmacenamientoDelNavegadorAsync();
instalada    = await estado.CorreComoAppInstaladaAsync();
```

Ninguna de esas tres preguntas es ilegítima **suelta**. Hospedada no hay nada que elegir: la carpeta
la concede Gnothi y el destino ya está decidido antes de que la página pinte. La rama que faltaba es
una línea:

```csharp
if (estado.Hospedada) { await AbrirAsync(); return; }
```

Las otras cuatro instituciones la tienen —`MisExamenes.razor:1483`, `MisCompras.razor:1178`,
`Cargas/MainLayout.razor:484`, `MyCar.razor:366` (en inglés, `state.Hosted`)—. **Municipalidad es la
quinta, la que se hospedó después**, y es la única que se quedó sin ella: `F6` documenta la forma
hospedada corriendo con **cuatro** instituciones, no con cinco.

## Y no es el cambio de forma

Es la parte que conviene decir fuerte, porque la primera lectura fue ésa y era cómoda y falsa.

**Una instalación hospedada desde cero habría fallado idéntico.** El bug no lo causó pasar de
híbrida a hospedada; pasar de forma sólo lo **destapó**, porque hasta ese día esa app nunca había
corrido hospedada en el teléfono. Lo que sigue siendo cierto de `B10` es que la hoja del
consentimiento no compara la forma; lo que ya **no** es cierto es que el registro quedara fuera del
alcance de la forma hospedada. El registro estaba ahí y la forma lo alcanza: la app no lo fue a
buscar.

## El arreglo, que son dos y de dos dueños distintos

**1 · En la app — quien lo puede arreglar de verdad.** La rama, antes de cualquier pregunta al
navegador, con el porqué escrito al lado. Es de la institución, y el Host no tiene cómo suplirla.

**2 · En el Host — que el silencio diga qué faltó.** El Host tenía delante todo lo que hizo falta
para diagnosticarlo y no lo decía. Ahora `WebModule.Start`, cuando la espera de `listo` se vence,
no lanza un `TimeoutException` pelado sino uno de estos tres:

| lo que sabe el Host | lo que dice |
|---|---|
| no sirvió ni un documento | *no pidió ni su propio documento: la vista no llegó a navegar al bundle* |
| sirvió N archivos, **cero** verbos por el puente | *cargó N archivo(s) y NUNCA llamó al puente: su app no preguntó si estaba hospedada, así que no está hablando con Gnothi* |
| sirvió N archivos y hubo verbos, pero no `listo` | *cargó N archivo(s) y habló por el puente (listar, leer), pero su actor nunca terminó de hidratarse* |

Son dos contadores en `BundleServer` (`Served`, `Documents`) y un conjunto de verbos oídos en
`WebModule.Serve`. **El segundo renglón es, palabra por palabra, lo que costó una tarde de parchar
el teléfono.**

`violation` no cuenta como verbo oído a propósito: lo manda el watchdog que **Gnothi** inyecta, no
el bundle. Si contara, un módulo que jamás llamó al puente parecería que sí lo llamó — y el renglón
del medio, que es el que vale, no saldría nunca.

## Lo verificado

Bundle hospedado `2.4.0`, armado, firmado, servido e instalado por el enlace de alta.

    installation.json → "shell": { "type": "webview" }
    comprobado.json   → {"cuando":"2026-09-16T17:10:54.0766923Z"}

La marca de comprobado **sólo se escribe si el módulo pasa las dos mitades del chequeo** —lo que
declara contra lo que hace, y después una consulta de verdad contestada—. Con 2.3.0 no había marca.

Y en pantalla, abriendo el módulo: la portada de «Mi cuenta municipal» con sus seis consultas, y
`Por qué pago más` contestando con el journal de la persona —`recoleccion-de-basura`, de ₡5 000 en
2025-06 a ₡8 000 en 2026-06, «es un salto, y este rubro se cobra por unidad»—. **Los mismos datos
que la forma híbrida mostraba, leídos por la app hospedada desde la carpeta que Gnothi concede.**

Con esto, hospedada e híbrida son compatibles **de hecho y no sólo en el papel**: el mismo módulo,
el mismo journal, las dos formas, sin tocar el journal ni el Host para pasar de una a la otra.

## Lo que sigue abierto

**Nada obliga a una app hospedada a saber que puede estar hospedada.** Es una convención que la
institución tiene que acordarse de escribir, y hoy se descubre abriendo el módulo en un teléfono.

Se puede cerrar de tres maneras, en orden de lo que cuestan:

- **el renglón del timeout** (hecho): no lo evita, pero lo vuelve legible en un renglón;
- **la conformance** podría exigir que una app que se publica hospedada llame al puente —hoy
  `C4` revisa lo que el paquete **trae**, no lo que el código **pregunta**;
- **el Host podría dejar de esperar en silencio 60 segundos** y avisar a los 3 o 5 cuando el
  documento ya cargó y nadie llamó al puente. Es el mismo dato, más temprano.

---

## La regla, en una línea

> **Si una app puede correr adentro de Gnothi, la primera pregunta de su arranque es si está
> adentro.** Todo lo que se le pregunte al navegador antes de eso es una decisión tomada sin saber
> quién manda.

Emparentado: `E3` (las trampas del journal, y el picker), `E4` (los defectos de las caras JS),
`B10` (la forma que cambia sin avisarse), `F6` (la forma hospedada con cuatro instituciones).
