# Localizar la cara híbrida

Complemento de [`localizar-una-caratula.md`](localizar-una-caratula.md), que cubre la **app suelta**
en Blazor. Esto cubre la otra pantalla del mismo negocio: la **cara híbrida**, la que corre adentro
de Mymi. Las reglas siguen siendo las de
[`reglas-de-localizacion.md`](reglas-de-localizacion.md).

Escrito el 2026-09-18, después de poner al día las cinco.

---

## Por qué existe este documento

Cada negocio tiene **dos pantallas del mismo hecho** y **nada las ata**:

| | quién la pinta | qué corre |
|---|---|---|
| `<X>.Wasm` | Blazor, la fuente del diseño | el navegador, o el WebView en la forma hospedada |
| `<X>.Cara` | html y js planos, escritos a mano | adentro de Mymi, contra la superficie C# en proceso |

Son código distinto que nadie compara. Las caras se escribieron el 14 de setiembre y las apps se
localizaron el 16 y el 17: para el 18, **sólo una de las cinco hablaba inglés adentro de Mymi**
mientras por fuera ya eran bilingües. Cuatro días bastaron.

**Si tocás `<X>.Wasm`, la cara de ese negocio es parte del cambio.**

---

## Lo que hay que hacer, en orden

Referencia viva: **Faro** (`Cargas/App/Cargas.Cara`) fue la primera y es la más corta de leer.

### 1 · El negociador

Un archivo propio, `idioma.js` (en Concesionario, `language.js`: ese módulo nombra todo en inglés).
Hace lo mismo que `Textos.NegociarAsync` de la app: lee `navigator.languages`, toma la primera
etiqueta cuya **subetiqueta primaria** coincida con `['es-CR', 'en']`, cae a `es-CR`, y fija
`document.documentElement.lang`.

Expone `etiqueta`, `lengua`, `texto(clave)`, `con(clave, huecos)` y `pintarLoEstatico()`.

**No uses `eval`**: la CSP con que se sirve un módulo no lo admite. Por eso es un módulo JS propio y
no una evaluación.

### 2 · Los catálogos

`texto/es-CR.js` y `texto/en.js` (en Concesionario, `text/`), cada uno publicando
`window.TextoDeLaCara['<etiqueta>'] = { ... }` (en Concesionario, `window.FaceText`).

**Los valores se COPIAN del catálogo de la app**, no se reescriben. Si la app ya dice una frase, la
cara dice **exactamente** esa, con la misma clave si se puede. Una cara con su propia redacción es
una segunda institución, y eso es lo que el guardián llama desvío.

Lo que la cara dice y la app no —el pie del cartero, el «preguntándole a Mymi…»— va en claves
propias. Lo que la app dice y la cara no, no se copia.

Se cargan **síncronos y antes** de `cara.js`, en el `<head>`.

### 3 · El HTML

Cada texto fijo lleva `data-texto="clave"` (o `data-texto-aria`, `data-texto-html`, `data-text` en
Concesionario) **con el español adentro como respaldo**. El `<html>` va **sin** `lang`: lo pone el
negociador después de elegir.

### 4 · `cara.js`

Ninguna cadena visible escrita en el archivo: todo por `I.texto(...)` / `I.con(...)`.

Y **el idioma viaja con la pregunta**: cada `P.pedir('<vista>', { … })` suma `idioma: I.etiqueta`
(en Concesionario, `language: L.tag`).

### 5 · La superficie

Cada `[View]` que **redacta una frase** recibe `string idioma = ""` y lo pasa a las vistas del
dominio, resolviéndolo con un ayudante de dos líneas:

```csharp
private static string Elegido(string idioma) =>
    Frases.EsUnaEtiquetaConocida(idioma) ? idioma : Frases.IdiomaDelRegistro;
```

En Concesionario se llama `Chosen` y usa `Phrases.IsALanguageThisModuleSpeaks`.

Una vista que sólo devuelve cuentas y banderas **no** lleva el parámetro. Los `[Query]` **tampoco**:
ésos los lee el asistente, no la pantalla.

El Host liga los argumentos **por nombre** y **descarta los que sobran**
(`Gnothi.Host/InProcessShell.cs`), así que una cara vieja que no mande nada sigue recibiendo
respuesta.

### 6 · El puente

`puente.js` lo pone el Host y es el **mismo archivo en los cinco módulos**, así que no tiene catálogo
propio: lee sus seis frases de `window.TextoDelPuente`. El negociador lo publica desde el bloque
`puente` del catálogo:

```js
if (catalogo.puente) window.TextoDelPuente = catalogo.puente;
```

Si no se las das, habla en español. **Las llaves y los huecos no se traducen**: son el cable, como
`window.gnothi` y `consultar`.

---

## Cómo se verifica

```bash
node Herramientas/revisar-las-caras.mjs            # las cinco
node Herramientas/revisar-las-caras.mjs compras    # una
```

Sale con **código 1** si encuentra algo, así que sirve de compuerta antes de commitear. Compara tres
cosas: que `css/app.css` de la cara sea idéntica a la de la app, que **toda clase que la cara escribe
tenga regla** en alguna hoja que viaja, y que los catálogos sean simétricos, sin claves pedidas que
falten y **sin un solo valor distinto** del de la app.

**Lo que no puede ver, y es lo peor:** si la cara lee un campo que la superficie no serializa. Eso
sólo se ve corriendo.

Para verla de verdad, en el emulador:

```bash
node Gnothi.Mobile/conducir-el-alta.mjs pegar http://127.0.0.1:8096/<modulo>.gmod
node Gnothi.Mobile/conducir-el-alta.mjs instalar
node Gnothi.Mobile/pantalla.mjs "document.body.innerText" "<modulo>.gnothi.local"
adb shell cmd locale set-app-locales com.ncubo.gnothi.host --locales es-CR
```

Filtrá por el url del módulo o enganchás la cáscara. Para volver al idioma del sistema,
`--locales ""`, y entre medio hay que matar y relanzar la app.

---

## Seis trampas que ya se pagaron

**1 · Campos que la superficie no manda.** La cara leía `h.stays.stays`, `h.plate.plate` y
`x.cuando`, y pintaba vacío **para siempre, sin un error en consola**. Las estadías del taller y la
placa de Sotaventos llevaban días sin aparecer. Antes de leer un campo, **leé el record en
`Vistas/`**, no lo que parece razonable.

**2 · Códigos del dominio que no son los que parecen.** En Exámenes son `progreso-hacia-meta` y
`en-ascenso`, **con guion**. Comparar contra `'progreso hacia meta'` falla en silencio: la barra del
colesterol, que es el caso insignia, no se dibujó nunca. Leé el código del dominio
(`FormaDeLectura.cs`, `Tendencia.cs`), no lo escribas de memoria.

**3 · Clases que ninguna hoja define.** Es el modo mudo de verse mal: el bloque sale sin caja, sin
color o sin espacio, y nadie avisa. `inicio-atencion-detalle` dejó la caja de lectura de Delfos
pelada, y `apunte` —que escribe el puente en las **cinco** caras— no estaba en tres hojas, así que un
fallo se veía igual que una nota.

**4 · La hoja de la institución se copia entera.** `css/app.css` de la cara es **la misma** de la
app, byte por byte. Entresacar reglas a mano es decidir cómo se ve ella. Lo propio de la cara va en
`cara.css`, que es corto a propósito.

**5 · La marca vive en la app suelta.** La foto y la tipografía están en
`<X>.Wasm/wwwroot/brand/`. `armar-bundle.ps1` completa desde ahí lo que la cara no trae, y **declara
la fuente sólo si el archivo quedó en el paquete**: antes Faro y Sotaventos nombraban una tipografía
que no viajaba.

**6 · Al localizar, no arrastres el texto viejo de la cara.** Al localizar Delfos, el saludo del hero
quedó diciendo «Hola.» porque eso decía la cara, mientras la app dice «Hola, Sonia.». La fuente de
verdad es **la app**, no lo que la cara venía diciendo.

---

## Lo que queda fuera a propósito

Las caras no tienen las rutas, pestañas ni formularios de sus apps: no hay router y la superficie no
expone comandos, sólo `[View]` y `[Query]`. El estilo y las palabras coinciden; **el inventario de
pantallas no**, y eso es una decisión, no un pendiente.
