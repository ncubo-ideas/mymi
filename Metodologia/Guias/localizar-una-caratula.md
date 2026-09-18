# Localizar una carátula

Este documento no repite las reglas: ésas están en
[`reglas-de-localizacion.md`](reglas-de-localizacion.md) y siguen siendo la autoridad. Lo que agrega
es **qué forma quedó adoptada**, **qué falta** y **qué trampas ya se pagaron**, para que nadie las
vuelva a pagar.

Escrito el 2026-09-15 como traspaso para cuatro carátulas. **Puesto al día el 2026-09-18, cuando ya
quedaba una.**

---

## Índice

- [Dónde está el trabajo](#dónde-está-el-trabajo)
- [La forma que quedó adoptada](#la-forma-que-quedó-adoptada)
- [Lo que falta: Exámenes](#lo-que-falta-exámenes)
- [Seis trampas que ya se pagaron](#seis-trampas-que-ya-se-pagaron)
- [Cómo se verifica](#cómo-se-verifica)
- [Lo que sigue sin decidirse](#lo-que-sigue-sin-decidirse)

---

## Dónde está el trabajo

La guía cerraba con un orden de cuatro pasos y una sección de *«lo que falta construir»*. **Los
cuatro pasos están hechos o casi, y lo que faltaba se construyó.**

| paso | estado |
|---|---|
| 1 · El Host (`ui.html` + Desktop sobre un catálogo compartido) | **hecho** — PRs 10078, 10080, 10081, 10082, 10084, 10085 |
| 2 · Acordar la forma en Municipalidad | **hecho por Kenneth**, y es la referencia |
| 3 · Un negocio de prueba con esa forma | **hecho** — Cargas |
| 4 · Los demás | **Compras y Concesionario hechos. Falta Exámenes.** |

El Host quedó con **354 claves** en `Gnothi.Host/textos.json` y un lector
([`Gnothi.Host/Textos.cs`](../../Gnothi.Host/Textos.cs)) que sirve las tres caras del mismo catálogo.

Y las dos cosas que la guía listaba como no construidas **ya existen**:

- **El canal que le dice a la carátula el idioma de la persona.** Se resolvió por el aparato, que es
  lo que la guía recomendaba: `navigator.languages`, leído **por un módulo JS propio y nunca por
  `eval`** — la CSP con que se sirve un módulo hospedado admite WebAssembly y no admite `eval`.
- **La forma clave-más-valores de la regla 5**, que la guía marcaba como *«propuesta, no
  construida»*. Ese renglón quedó viejo: hay cuatro implementaciones corriendo.

---

## La forma que quedó adoptada

Municipalidad fue la referencia y las otras tres la siguieron. **Lo que se copió fue la forma, no
los nombres**: cada negocio nombra en su propio idioma, y eso está bien — Concesionario tiene su
dominio en inglés y su carátula también.

| | Municipalidad | Cargas | Compras | Concesionario |
|---|---|---|---|---|
| el lector | `Textos.cs` (242) | `Textos.cs` (260) | `Textos.cs` (239) | `Texts.cs` (282) |
| clave + huecos | `Apunte.cs` | — | `Apunte.cs` | `Note.cs` |
| catálogos | `wwwroot/texto/` | `wwwroot/texto/` | `wwwroot/texto/` | `wwwroot/text/` |
| leer el idioma | `idioma-interop.js` | `arranque-idioma.js` | `idioma-interop.js` | (en su propio código) |
| frases del dominio | `App/…/Frases/` | `App/…/Frases/` | `App/…/Frases/` | `TheOwnerIsTold.cs` |
| pruebas de idioma | 7 archivos | — | — | **195 archivos** |

**Lo invariante, que es lo que hay que copiar:**

- **Dos catálogos, no uno.** Las palabras de la pantalla son de la carátula y se piden por HTTP al
  arrancar (JSON en `wwwroot`). Las frases que compone el dominio las arma **la puerta**, en C#,
  antes de que exista una pantalla — y las necesita también el MCP, que no tiene navegador. Meterlas
  en el mismo JSON obligaría a la puerta a leer un archivo de `wwwroot`, que es al revés de como
  corre.
- **Los huecos se nombran, no se numeran.** Está escrito en el propio `Textos.cs`: *«una frase
  traducida cambia el orden de sus partes y un hueco numerado se va al lugar equivocado sin que nada
  avise»*. Es la regla 7 aplicada.
- **Una clave que falta se dice con su propio nombre entre llaves** y no revienta. El hueco se ve,
  que es lo que se quiere.
- **El título del módulo vive fuera de los catálogos, a propósito.** Es una constante en C#: *«lo que
  no está en el archivo no se puede traducir por descuido»*. Es la regla 4 hecha estructura en vez de
  advertencia.
- **Una cifra se escribe en un solo lugar.** El renderizador de montos, porcentajes y fechas lo usan
  la puerta y la pantalla. Dos implementaciones de la coma decimal en la misma pantalla es la regla 8
  rota en pequeño.
- **El idioma se lee una vez, al arrancar, y nunca de la cultura del proceso.** En wasm esa cultura
  es la del runtime, no la de la persona.
- **El catálogo que no llega cae al idioma del registro**, no a claves entre llaves. Lo arregló
  Kenneth en Concesionario (`b549886`) después de medirlo escondiendo cada catálogo por turno.

**Dónde mirar según lo que estés haciendo:** Municipalidad si querés la forma completa con su
documentación; **Concesionario si querés ver cómo se prueba** — 195 archivos de prueba que hablan de
idioma, contra 7 de Municipalidad y ninguno de Cargas y Compras.

---

## Lo que falta: Exámenes

| | |
|---|---|
| nodos de texto en los `.razor` | ~100 |
| dominio | 90 archivos, 8 847 líneas |
| piezas de localización hoy | ninguna |

Es la última, y va al final por la misma razón por la que la guía la mandó ahí: **su inventario de
frases del dominio no está hecho.** El «0 frases» que salió del grep original es un falso 0 — salió
de buscar métodos que componen frase, y hay que contarlas a mano.

`HolaMundo/Facturas` **no entra**: `HolaMundo/` es material de referencia y descartable, según
`CLAUDE.md`.

---

## Seis trampas que ya se pagaron

Todas salieron de localizar el Host. Ninguna está en las reglas porque ninguna se sabía al
escribirlas.

### 1 · Comprobar que las claves existan NO alcanza

La suite comprobaba que **toda clave usada** estuviera en el catálogo. Nunca comprobaba lo
contrario: que **todo literal** estuviera convertido en clave. Por ese hueco pasaron **31 rótulos**,
y se descubrieron en un iPhone en inglés con Ajustes entero en español.

Hay que comprobar las dos direcciones. Para una carátula Blazor el equivalente de la aserción que
quedó en `Gnothi.Assistant.Tests` es: **ningún nodo de texto del marcado puede traer letras** — todo
lo visible entra por el catálogo o por un dato.

### 2 · Buscar palabras en español tampoco alcanza

El primer detector buscaba tildes y palabras españolas. **No vio «Proveedor», «Servidor», «Modelo»,
«Guardar», «Carpeta» ni «Copiar»**, que eran seis de los 31. La regla tiene que ser estructural, no
de idioma.

### 3 · Reusar antes de agregar

De las 31 frases que faltaban, **ocho ya tenían clave escrita** y sin usar: la pasada del escritorio
las había creado y la del teléfono nunca las enganchó. Antes de escribir una clave nueva, buscarla
por su **valor** en el catálogo.

### 4 · Hay valores que además son rótulos, y ésos no se traducen sin migración

`"Otro"` en `ui.html` y `Shell.ProveedorLocal = "En este teléfono"`
([`Gnothi.MobileShared/Shell.cs`](../../Gnothi.MobileShared/Shell.cs)) se **guardan y se comparan**
(`name == ProveedorLocal`), y además se muestran. Traducirlos rompe la comparación y huérfana los
ajustes ya guardados. **Es un arreglo con migración, no una clave más.** Sigue abierto.

Al inventariar una app, la pregunta es: *¿este string se compara con algo, o se guarda en disco?*
Si la respuesta es sí, no es un rótulo aunque se vea como uno.

### 5 · La decoración no va en el catálogo

`asistente.traza` valía `"· qué pasó por debajo"`. El punto medio es del render, no de la frase — y
mientras estuvo adentro, la clave no se podía compartir entre dos caras que lo pintan distinto.

### 6 · Alinear al render, nunca al literal

El bloque de éxito del instalador tenía el padding escrito a mano (`from     :`). Eso cuadra en un
idioma: «signature» mide nueve y «firma» cinco. Se mide el rótulo más largo **al pintar**.

### Y una que es de criterio, no de mecánica

El plan mandaba traducir los motivos de denegación del `Dispatcher` *«porque llegan hasta Siri»*.
Llegan — pero como **entrada del modelo**, no como frase hablada: el App Intent entrega la foto con
un encuadre que le pide al modelo contestar *«en su idioma»*.

**La pregunta correcta no es «¿esto se ve?» sino «¿quién lo lee?».** Un texto que pasa por una
pantalla camino a un modelo sigue siendo texto para el modelo. Se revisó cada consumidor uno por uno
antes de decidir, y quedó escrito en `Dispatcher.cs` para que nadie rehaga el análisis.

---

## Cómo se verifica

1. **Por consola primero**, como todo en este repo:
   `dotnet run --project <N>/App/<N>.Consola -- comprobar`.
2. **Las dos direcciones en la suite**: claves que se piden y existen, y literales que no quedaron
   cableados.
3. **Escondiendo un catálogo.** Es la medición de Kenneth en Concesionario y vale para cualquiera:
   con `en.json` escondido la pantalla tiene que salir entera en el idioma del registro; con los dos
   escondidos tienen que salir las claves, que es el control de esa medición.
4. **En el navegador, en los dos idiomas.** Cambiar el idioma del sistema y recorrer la pantalla
   entera. Lo que se busca es que **no quede una sola pantalla mezclada** — que es el defecto que se
   encontró en el Host y que ninguna prueba estática ve.
5. **Ojo con el Wasm**: `MSBuildEnableWorkloadResolver=false` ya **no hace falta** desde el SDK arm64
   (medido el 2026-09-15 en `Examenes.Wasm` y `Cargas.Wasm`, con `wasm-tools` sin instalar). Si algo
   falla con `NETSDK1147`, ése es el flag.

---

## Lo que sigue sin decidirse

- **El vocabulario entre caras.** El escritorio dice «Tus instituciones» donde el teléfono dice «Mis
  conexiones», y «Llave» donde el teléfono dice «Clave». **Lo decide Geaninna**, no quien toque el
  código.
- **Si las carátulas comparten catálogo con el Host o no.** Hoy no: el Host tiene el suyo embebido y
  cada carátula el suyo en `wwwroot`. Son dueños distintos (regla 1) y probablemente esté bien así,
  pero nadie lo decidió en voz alta.
- **Cuánta prueba de idioma es la correcta.** Concesionario tiene 195 archivos de prueba que hablan
  de idioma y Cargas y Compras ninguno. Las tres pasan. O sobra en una o falta en dos, y eso no se
  discutió.
- **El inventario de frases del dominio de Exámenes**, que es lo que la tiene al final.
