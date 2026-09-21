# C4 · Qué es un módulo, y qué no

Para quien va a escribir uno. Un módulo lo hace el developer de la institución — el laboratorio, la
agencia, el supermercado — y **no es una app**.

---

## La anatomía

Un módulo cargo son **dos DLLs chiquitas** más dos JSON:

```
X.App.dll            ← la superficie: métodos anotados. Toma solo Gnothi.Contract
X.Dominio.dll        ← las clases con las reglas. CERO dependencias. Repertorio embebido
manifest.json        ← capacidades, rango de sustrato, endpoint del publicador, y el entry
view.json            ← el módulo describe la vista; el Host la renderiza
```

Y **el journal crece aparte**, en el directorio que el Host le concedió.

**No trae motor.** El sustrato —`Ncubo.Puppeteer` + `Puppets.Portador`— y `Gnothi.Contract` los **presta
el Host**, una sola vez, al cargar el módulo (decisión `A1`). Por eso las dos DLLs pesan lo que pesan:
decenas a centenas de KB. Como referencia medida: el dominio de un ejemplo real pesó 21 KB, y la view 23 KB.

### El empaque: un `.gmod`

Las cuatro piezas viajan juntas en un **`.gmod`** — un zip, nada más exótico que eso:

```
compras.gmod
├── manifest.json
├── view.json
└── dominio/*.dll        ← la superficie X.App.dll y su dominio X.Dominio.dll, adyacentes
```

Lo arma el Host con `--pack` y lo instala con `--install-module` (los comandos, más abajo). Y en `C7` está
cómo el instalador del comercio corre `--install-module` por custom action.

## La frase que hay que entender antes de escribir una línea

**Un módulo no es una aplicación: es carga del Host.**

Es flete. El Host es el vehículo, el módulo es lo que se transporta, y **el flete no conduce**.

Consecuencia práctica: **el tamaño del módulo no depende de la riqueza del dominio.** Un laboratorio con un
dominio sofisticadísimo sigue pesando poco, porque la sofisticación se paga en el DLL y la *maquinaria* no
está ahí.

---

## Lo que un módulo NO puede tener

Esta lista es más importante que la anterior. No son recomendaciones: son cosas que el Host no le va a
conceder, y en el diseño destino **cosas que no va a poder hacer aunque quiera**.

| ❌ | por qué |
|---|---|
| **Red** | dar red a un módulo es incontrolable en **dos ejes**: el verbo y el destino. Nadie puede garantizar que un `GET` no sea un `POST`, ni que vaya al servidor de su institución. El Host trae los bytes |
| **Transporte propio** | si implementa un aparato de red —ssh, un socket, un listener— **tiene sockets**, y se cae la garantía anterior. Expone su superficie; el Host elige la cáscara |
| **Otra carpeta** | una sola, la concedida. Y no la elige el módulo: *conceder es un acto del que concede* |
| **StateManager** | todas las operaciones son de tipo performance. Y no hace falta detectarlo: **un StateManager sin red es un objeto decorativo** |
| **Iniciativa** | no despierta, no notifica, no interrumpe. La persona va a él; él no viene a ella. **Sin iniciativa no hay marketing** |
| **Elegir su endpoint en runtime** | fijo por publicador, registrado en la instalación. Si el módulo pudiera decir qué URL traer, **exfiltra codificando datos en la ruta** |
| **Parlante, micrófono, cámara, GPS** | para empezar, no. Después se puede discutir caso por caso, pero de entrada la respuesta es no |
| **Disco duro completo** | *«eso no ocupo yo ser experto en seguridad»* |

## Y la señal de alarma es el exceso

Un módulo de facturas que pide micrófono no necesita análisis, necesita un aviso. El Host lo detecta
contrastando lo declarado contra lo que realmente pide.

Con un matiz: el Host **no impide correr** — se niega a mediar. *«Eso no significa "no la corro", significa
"vos expresamente le das derecho, sí o no"».* Y si alguien instaló algo que se hace pasar por del
ecosistema, el Host dice *«esa no es mía»*.

---

## Las cuatro categorías de dato, y dónde va cada una

Confundirlas rompe cosas, y es el error más común al escribir un módulo:

| categoría | dueño | escritura | dónde va |
|---|---|---|---|
| **Hechos institucionales** (exámenes, cargas, compras) | la persona | append-only, emisor institucional | **el journal** |
| **Repertorio** (rangos, horarios, reglas) | la institución | se reemplaza por versión | **recurso embebido en el DLL** |
| **Aserción propia** («soy vegetariana», umbrales) | la persona | append-only, autora ella | **el journal**, con su procedencia |
| **Contexto de sesión** (tokens, cursores, reintentos) | el Host | — | **NO va al journal** |

**Si los horarios del bus entran al journal** se rompe la inmutabilidad —los horarios cambian, y que hayan
cambiado **no es un hecho de la vida de la persona**— y se duplica el mismo contenido en cada usuario.

**Criterio falsable:** un cambio de rango de referencia produce un módulo nuevo y **no toca ningún journal
existente**. Si para actualizar un rango hay que escribir en el journal de alguien, el repertorio se filtró
a la historia.

## El repertorio: dos cosas, y un lugar donde NO va

**(a) Reglas — son código.** Qué es un colesterol total, qué rango es normal para una mujer de 41, cómo se
deriva una tendencia. Va en las clases de dominio. **Es el activo real de la institución.**

**(b) Tablas de referencia — datos versionados.** Rangos por edad y sexo, catálogo de analitos, mapeo de
códigos.

| dónde poner (b) | veredicto |
|---|---|
| **Recurso embebido en el DLL**, leído por el dominio | ✅ **Esto.** Cambiar un rango = nueva versión del módulo |
| Archivo de datos junto al DLL | ⚠️ funciona, pero abre quién lo firma y qué pasa si falta |
| Sembrado en el journal | ❌ **No.** Los rangos no son hechos de la vida de nadie, y se duplicarían por usuario |

Matiz legítimo: la **configuración de esa instancia** (qué sucursal, qué sistema de unidades) sí es estado
del actor, y hay un idiom documentado para eso, idempotente por diseño.

---

## La propiedad comercial, y su condición de legitimidad

Esto es lo que hace que valga la pena escribir un módulo rico:

**El tamaño y los temas de la app son independientes de la riqueza del módulo.** La institución atrae con
dashboards que la persona valora, y con ellos viajan repertorio y operaciones que ella no sabe valorar — sus
rangos normales, el histórico de otros exámenes, más datos que los que grafica la UI. **Crípticos para ella;
valiosos para el asistente.**

**Por qué funciona:** las dos mitades las juzgan jueces distintos. **La UI la juzga la persona; el repertorio
lo juzga el asistente.** No compiten.

**Y es el mismo vector de abuso:** «invisible y más rico que la pantalla» es exactamente lo que un actor
malicioso quiere. **La defensa no es limitar el repertorio — es que sea enumerable.**

El Host puede mostrar qué carga un módulo y qué puede hacer **sin cooperación del módulo**. Eso es lo que
vuelve la riqueza invisible **defendible** en vez de sospechosa:

> **Lo invisible puede ser rico, pero nunca opaco.**

Para el autor del módulo eso significa una cosa concreta: **no cuentes con que algo quede escondido.** Todo
lo que el módulo puede hacer va a ser enumerable desde afuera.

---

## El manifiesto que hay que escribir

```json
{
  "manifiesto": "1.0",
  "modulo": "facturas",
  "titulo": "Mis facturas",
  "institucion": "...",

  "view": "view.json",
  "load": { "entry": "facturas.App.dll" },

  "sustrato": { "rango": ">=3.0.0 <4.0.0" },

  "capacidades": { "red": false, "carpeta": "propia",
                   "parlante": false, "microfono": false },

  "journal": { "compression": "None", "encryption": "None", "maxFileSize": 4194304 },

  "tools": [ { "nombre": "...", "descripcion": "...", "soloLectura": true, "entrada": [] } ]
}
```

Dos campos nuevos respecto del POC: **`view`** nombra el json de la vista, y **`load.entry`** dice **cuál
DLL trae la superficie** (`X.App.dll`). Sus dependencias —el `X.Dominio.dll`— se resuelven **por
adyacencia**: el Host las busca al lado del `entry`, en la misma carpeta. No hay lista de dependencias que
mantener; están ahí o no están.

Tres cosas que se equivocan seguido:

**El rango, no una versión exacta.** Si pineás una versión puntual, cada actualización del Host huerfaniza tu
módulo. Y el rango que podés pinear **no lo declarás vos**: es el que el sustrato se ganó con su suite de
compatibilidad.

**Los tres ajustes de journal se fijan al crear y no se pueden cambiar.** Pedir otro modo después **lanza
excepción**. Si querés cifrado, decidilo antes del primer asiento — después significa **empezar un journal
nuevo**.

**Y el manifiesto no lleva rutas.** Eso es lo que lo hace portable entre hospedajes. Dónde está tu binario y
qué carpeta te concedieron es asunto del Host, en otro archivo.

---

## Los tres comandos del Host para un módulo cargo

El Host ganó tres verbos de consola para el ciclo de vida de un módulo cargo:

| comando | qué hace |
|---|---|
| `--pack` | arma el `.gmod` a partir de las dos DLLs, el `manifest.json` y el `view.json` |
| `--install-module` | instala un `.gmod`: lo extrae, concede la carpeta de datos, escribe `installation.json` |
| `--query` | corre una consulta del módulo **por consola**, sin asistente ni protocolo |

`--query` es la misma consulta que vería el asistente, pero por la línea de comando — útil para probar un
módulo recién instalado. Sobre el módulo `compras`:

```
Gnothi.Host --query <carpeta-de-modulos> compras resumen
  -> { "hayDatos": true, "totalGastado": 3174606, "cantidadDeCompras": 87, "ultimoEventoLeido": 387 }
```

Y `--install-module` es exactamente lo que el instalador del comercio corre por custom action al
instalarse. Ver `C7`.

---

## La lista que corre antes de publicar

Del plan de la especificación, y vale tenerla a mano desde el primer día:

1. **Grafo de referencias** — ningún tier de host en el dominio ni en la superficie.
2. **Journals dorados** — el journal replaya idéntico bajo todo el rango de sustrato declarado.
3. **Introspección** — lo que el Host va a mostrar de tu módulo es lo que quisiste exponer. **Detecta
   exposición accidental.**
4. **Cold start** con un journal realista.
5. **Superficie de consultas** — **ninguna existe para pedirle algo a la persona.**
6. **El empaque falla si trae un journal de muestra adentro.**

El punto 5 es el que más fácil se viola sin mala intención: una consulta llamada `completar_encuesta` es un
canal hacia la atención de la persona, y la asimetría «el módulo responde, no pregunta» se cae.
