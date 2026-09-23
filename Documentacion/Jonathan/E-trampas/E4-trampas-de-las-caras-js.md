# E4 · Cómo fallan las caras en JS — seis defectos, y tres son el mismo

**Del 2026-09-14 al 16**, probando las cinco caras híbridas en un Xiaomi 2407FPN8EG con Android 16.
Ninguno lo detectó una prueba: los cinco se vieron **mirando la pantalla**.

---

## El patrón, que es lo que hay que llevarse

**Tres de los cinco fallan en silencio, y los tres lo hacen igual: un dato que no llega se ve
exactamente igual que un dato que dice «todo bien».**

Ningún error en el log. Ninguna excepción. La pantalla contesta — contesta mal.

| | qué se vio | qué era |
|---|---|---|
| **1** | el visto verde al lado de «1 te toca ahora» | el diccionario llegó como lista de pares |
| **2** | «Color de la orina: **undefined**» | un campo que no existe en la vista |
| **3** | el botón del spike no hacía nada | el `@onclick` llegó como texto |

Los otros dos son de otra familia — se ven y se arreglan mirando:

| | qué se vio | qué era |
|---|---|---|
| **4** | la placa dos veces en cargas | `[hidden]` le perdió al `display` de la institución |
| **5** | la cabecera mordida en exámenes | faltaba el envoltorio `.hoja` |

Y hay un sexto que no es del mismo tipo, pero que también se vio mirando:

| | | |
|---|---|---|
| **6** | «212 → 212» con el chip «↑ en ascenso» | la cifra y el veredicto salían de ventanas distintas |

---

## 1 · El visto verde del concesionario — **era un bug del Host**

La tarjeta «lo que sigue» pintaba un check verde justo debajo de una píldora ámbar que decía «1 te
toca ahora». Dos afirmaciones contradictorias a dos centímetros.

**No era la cara.** `InProcessShell.Project` tenía:

```csharp
if (v is System.Collections.IEnumerable list) return list.Cast<object?>()…
```

y **un `Dictionary<K,V>` es `IEnumerable<KeyValuePair<K,V>>`**. Así que el `dueStates` que la
superficie arma llegaba al JS como **lista de pares**:

```json
[{"key":"Inspection","value":"Due"}, …]     en vez de     {"Inspection":"Due", …}
```

La cara buscaba `dueStates[nextDueKind]`, encontraba `undefined`, y el respaldo de `dibujo()` era
`return 'check'`.

**Arreglado en dos lugares, y los dos hacían falta:**

- **El Host**: caso `IDictionary` **antes** del `IEnumerable`, con las llaves sin camelCasear —
  `"Inspection"` es un miembro del conjunto cerrado del dominio, no un nombre de propiedad.
- **La cara**: `dibujo()` ya no devuelve el visto para lo que no reconoce. Hay que ganárselo
  nombrando un estado que de verdad lo sea (`NotDue`, `Completed`, `Superseded`, leídos de
  `DueState.cs`). Y `glifo()` tenía **el mismo respaldo un piso más abajo** — dos redes que
  atrapaban lo desconocido y las dos lo pintaban de verde.

> **Un semáforo no puede tener un color por omisión.** El único fallo que no se le permite es decir
> que todo está bien cuando no lo sabe.

**Y esto pega más lejos que una cara:** cualquier módulo que devuelva un `Dictionary` desde un
`[View]` o un `[Query]` recibía una lista de pares. Hoy sólo lo hace el concesionario porque es el
único que lo usa.

## 2 · «Color de la orina: undefined»

`tarjetaAtencion` leía `r.presencia.ultimoResultado`, y `PresenciaDeAnalito` **no tiene ese campo**:
tiene `Presencia`. Con las cetonas funcionaba de casualidad; con los analitos de forma «categoría en
el tiempo» salía `undefined` **impreso en la pantalla, en tamaño titular**.

La lección es la de siempre en JS: `objeto.campoQueNoExiste` no es un error, es `undefined`, y
`String(undefined)` es `"undefined"`. En razor, `@r.Presencia.UltimoResultado` no compila.

**Es el costo real de pasar a JS**, y no aparece en la cuenta de líneas: los −21 % de concesionario
no descuentan los campos que el compilador dejó de revisar.

### Y el título de esta sección siguió siendo literal doce días más — 2026-09-16

**Ese arreglo no arregló «Color de la orina».** Arregló los binarios —cetonas, nitritos, glucosa en
orina— y ahí se dio por cerrado, porque los binarios eran los que se estaban mirando. El analito que
le da nombre a la sección siguió imprimiendo `undefined` hasta hoy, y **la causa era otra**:

```js
var v = r.presencia ? texto(r.presencia.ultimoResultado)
                    : valor(t.valorFinal) + ' <span>' + texto(t.unidad) + '</span>';
```

**Dos ramas, y el dominio manda tres.** La vista `tendencia` devuelve `presencia` para «presencia en
el tiempo», `hechos` para una serie continua, y **`constancia` + `observaciones`** para «categoría en
el tiempo». La tercera caía en la rama continua y pedía `hechos.valorFinal`, que para un nominal no
existe.

El dato estaba ahí todo el tiempo: `constancia.ultimaCategoria` = `"ámbar"`. Y el reparto ya sabía
que eran tres —`repartir` manda `r.constancia` a atención desde antes—, igual que la app suelta, que
las separa en tres puertas explícitas y lo dice con todas las letras: *«un color no tiene tendencia
ni puntos con número: tiene categorías»* (`MisExamenes.razor:1740`).

**Lo que faltaba era la rama de dibujo y la de la cifra, no el reparto.** La tira de los binarios
sirve igual sin tocarla: lo que marca no es «positivo» sino «se salió de lo esperable», que es la
misma pregunta —cuándo pasa y cuántas veces seguidas— sobre otro conjunto de valores.

> **La lección nueva, y es de método:** un defecto que se manifiesta en varios analitos puede tener
> **más de una causa**. Arreglar la que explica los casos que uno está mirando y cerrar el ticket
> deja el resto en pantalla, con el mismo síntoma y el mismo nombre. Verificado el 2026-09-16 en el
> teléfono, en híbrida **y** en caja: «Color de la orina → ámbar», «Color de las heces → marrón», con
> su tira y su «4 de 4 dentro de lo esperable».

## 3 · El `@onclick` que llegó como texto — el spike

En `Lab.BlazorEnElHost`, la primera corrida compiló, pintó, y el botón no hacía nada. El lote decía
por qué:

```json
{"t":"m","v":"<button @onclick=\"Sumar\">Tocar</button>"}
```

Markup literal. Con sólo `Microsoft.AspNetCore.Components`, el compilador de Razor **no sabe que
`onclick` es un evento del DOM** — eso lo define `Microsoft.AspNetCore.Components.Web`.

Lo pongo acá aunque sea de otro proyecto porque **es exactamente la misma forma de error**, y que
aparezca tres veces en tres días con tres causas distintas dice que no es mala suerte.

---

## 4 · La placa dos veces en cargas

`.placa` es `display: inline-flex` en la hoja de Estaciones Faro, y eso **le gana a `[hidden]`**,
que vive en la hoja del navegador — la de menor precedencia. Los dos nodos estaban marcados como
escondidos y los dos se veían.

**La app suelta no tiene el problema porque Blazor no esconde: con un `@if` el nodo que no toca no
se dibuja.** Una cara híbrida es HTML estático, los dos nodos existen siempre, y esconder es la
única herramienta — así que la herramienta tiene que funcionar.

Arreglado con `[hidden] { display: none !important }` en el `cara.css` de **las cinco**, que carga
después de la hoja de la institución. **Estaba latente en compras** con `.mk-cambio` y
`.mk-oportunidad`, que son `display: grid`: una persona sin cambios de ritmo habría visto una
rejilla vacía.

## 5 · La cabecera mordida en exámenes

Faltaba `<main class="hoja">`, que `MainLayout.razor` de la app sí tiene. `.hoja` lleva
`padding: 44px 26px` y `.inicio-hero` lleva `margin: -44px -26px` para **sangrar exactamente ese
padding**. Sin la `.hoja`, esos márgenes negativos no cancelan nada: tiraban el hero 44 px arriba
del borde y 26 px afuera a cada lado.

**Dos síntomas, una causa**: el título mordido por la barra de estado y el conteo de «MERECE
ATENCIÓN» cortado contra el borde derecho.

Y encima sí hacía falta el safe-area: el `index.html` de la app pide `initial-scale=1.0` a secas y
el mío pide `viewport-fit=cover`, así que la coordenada cero queda tapada por la barra.

## 6 · «212 → 212 · ↑ en ascenso»

La tarjeta mostraba los **dos últimos puntos** de la serie al lado de un veredicto que es sobre
**toda** la serie. Y en la misma tarjeta, dos renglones abajo, la frase del dominio decía *«subió de
182 a 212 mg/dL en 734 días»*.

Los datos no estaban mal — los dos últimos pueden ser iguales dentro de una serie que sube. **Pero
la pantalla se contradecía a sí misma**, y un laboratorio no se puede defender diciendo que
técnicamente ambas cosas son ciertas.

El dominio ya entrega los extremos de su propia ventana: `ValorInicial` y `ValorFinal`. Es lo que
empareja la app suelta (`Tendencia.razor:259`, `MisExamenes.razor:1801`).

**Y estaba en DOS funciones.** Lo arreglé primero en `tarjetaCambio` y al volver a probar seguía
ahí: la tarjeta que se veía era la de `tarjetaAtencion`, con la misma cuenta escrita aparte. Copiar
un cálculo es copiar su error.

---

## Qué se lleva de todo esto

**1 · El respaldo de una función que pinta estado no puede ser el estado bueno.** Tres veces:
`dibujo()` → `check`, `glifo()` → `check`, y el marco desconocido → se ignoraba. En una cara, el
caso por omisión tiene que ser «no sé», y verse distinto.

**2 · El compilador dejó de revisar los nombres de los campos.** `r.presencia.ultimoResultado` no
existe y nadie lo dijo. Es el costo de JS que no sale en la cuenta de líneas.

**3 · Esconder no es lo mismo que no dibujar.** Blazor no esconde; una cara estática sí, y el
`display` de la institución le gana al `[hidden]` del navegador.

**4 · El markup de la institución tiene supuestos sobre su envoltorio.** Copiar la sección sin la
`.hoja` copia los márgenes negativos sin lo que cancelan.

**5 · Una cifra y su veredicto tienen que salir de la misma ventana.** Si el dominio publica sus
extremos, se usan ésos — no los dos últimos puntos que uno tiene a mano.

**6 · Ningún defecto lo encontró una prueba.** Los seis se vieron mirando la pantalla, y tres de
ellos sólo son visibles con datos reales. La cara de un módulo **no tiene hoy ninguna red** que no
sean los ojos de alguien.

---

## 7 · El arreglo que nunca se empaquetó — 2026-09-16

El §6 se arregló el 2026-09-14 y **se siguió viendo en el teléfono el 16**: la captura del día de la
caja muestra «212 → 212» con «↑ en ascenso» al lado, dos días después de darlo por cerrado.

La corrección estaba bien escrita y estaba en el lugar correcto —`Examenes/App/Examenes.Cara/cara.js`—
pero ese archivo **no es el que corre**. El que corre es la copia que `armar-bundle.ps1` mete en el
`.gmod`, y el bundle no se había vuelto a armar:

```
Examenes/App/Examenes.Cara/cara.js                       31 254 bytes   (con el arreglo)
Gnothi.Mobile/bundles/hibrida/examenes/web/cara.js       30 xxx bytes   (sin él)
```

**No es un defecto de la cara: es un defecto de la cadena.** Y se lee igual que un arreglo que no
funciona, que es lo peor que puede pasar: se vuelve a mirar el código correcto buscando el error.

Es la misma familia que los dos de la caja del mismo día —el `.cwasm` sin atar a su fuente, y el
asset comparado por largo— y por eso vale escribirla como una sola regla:

> **Un arreglo no está hecho hasta que corre.** Entre el archivo que uno edita y el byte que ejecuta
> el aparato hay una cadena —compilar, empaquetar, firmar, copiar, precompilar— y **cada eslabón que
> decide «esto no cambió» puede equivocarse**. Cuando una corrección «no hizo nada», la primera
> pregunta no es qué está mal en el código: es si el código llegó.

Para exámenes la cadena entera es: editar `Examenes.Cara/cara.js` → `armar-bundle.ps1 -Forma hibrida
-Version <mayor a la instalada>` → copiar el `.gmod` a `bundles/` → reiniciar la página de la
institución (el `h` del enlace de alta sella el paquete) → `adb reverse tcp:8096` → actualizar el
módulo desde el teléfono. Gnothi avisa en cada paso si algo no cuadra: dijo *«es más viejo que el que
tenés: es la 2.4.0 y tenés la 3.9.0»* y *«el paquete no es el que sellaba el enlace»*, las dos veces
antes de tocar nada.

### Y no era uno: eran tres

Encontrado el §7, la comprobación obvia es barata —comparar las cinco caras fuente contra sus
bundles— y **había dos más**, los dos con el arreglo del §3, el `[hidden] { display: none
!important }`:

| módulo | estado |
|---|---|
| cargas | al día |
| dealership | al día |
| **exámenes** | `cara.js` sin el arreglo del «212 → 212» |
| **compras** | `cara.css` sin la regla de `[hidden]` |
| **municipalidad** | `cara.css` sin la regla de `[hidden]` |

Y el de compras **no era latente por casualidad**: el comentario del propio arreglo lo dice —
*«latente en compras con `.mk-cambio` y `.mk-oportunidad`, que son `display: grid`: sin esto, una
persona sin cambios de ritmo vería una rejilla vacía donde no hay nada que decir»*. Estaba escrito
que iba a pasar, en el archivo que no se empaquetó.

**Tres de cinco caras tenían un arreglo escrito y no entregado.** Los tres se armaron el 2026-09-16 y
se verificó la regla **adentro del `.gmod`**, no en la carpeta de salida — que es la comprobación que
esta sección existe para pedir.

> **La comprobación que vale la pena automatizar:** `diff -rq <Modulo>.Cara bundles/hibrida/<mod>/web`.
> Cuesta un segundo y contesta «¿lo que corre es lo que escribí?». Hoy nadie la hace, y por eso tres
> arreglos estuvieron escritos y sin entregar hasta dos días.
