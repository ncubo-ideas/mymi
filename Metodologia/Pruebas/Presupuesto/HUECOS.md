# Huecos — lo que `Preguntas.md` no alcanzó a decidir

El dominio de este directorio se construyó **únicamente con `Preguntas.md` en la mano**, sin volver a
preguntarle nada a nadie. Esa era la prueba: *¿alcanza el archivo para modelar?*

**Alcanzó para cinco de los ocho juicios y para las cinco preguntas.** Los ocho huecos de abajo son lo
que faltó. Cada uno está marcado también en el código, en el lugar donde muerde.

Regla que se siguió al toparse con cada uno: **tomar la decisión mínima —la que no inventa un
juicio— y dejarla a la vista.** Nunca rellenar con algo plausible.

> **Y ésa era la regla equivocada.** Un hueco no debería producir *una decisión con un comentario*:
> debería producir **una pregunta para una persona**. Este archivo fue el artefacto correcto para un
> experimento —medir si `Preguntas.md` alcanzaba— pero en trabajo real es una lista que nadie lee.
>
> De ahí salió el **bloque 5** de la entrevista: los huecos vuelven a quien sabe del negocio, en una
> conversación corta después de la primera pasada de modelado. **El hueco 7 se cerró así, y en un solo
> intercambio.**

> ### Este archivo es anterior a la doctrina que lo describe
>
> **Se escribió el 2026-08-26**, cuando existían siete guías y el `punto-de-partida` era la versión
> anterior a las seis correcciones que esta misma corrida produjo. No existían la hoja de la
> entrevista ni las reglas del substrato. **Presupuesto es el material del que salieron las guías, no
> un producto de ellas** — vale tenerlo presente antes de usarlo como evidencia de nada.
>
> Por eso su tabla **no lleva la columna de origen** que el [capítulo
> 3](../../Guias/03-construir-la-app.md) §8 bis pide hoy: los ocho son *descubiertos*, porque la
> entrevista de entonces no producía juicios declarados sin firmar. Esa forma apareció después, en el
> gimnasio.

---

## Los ocho

| # | Qué falta | Dónde muerde | Qué se hizo mientras tanto |
|---|---|---|---|
| **1** | **Cómo se establece la identidad de un gasto recurrente.** Ya venía declarado PENDIENTE en el propio archivo | `GastoRecurrente.cs` | El gasto **recibe** los textos con los que responde; no los deduce. Es lo único que no inventa un juicio |
| **2** | **Cómo entra al total mensual lo que no es mensual** | `Periodicidad.cs`, `Panorama.VarianciaDe` | No se prorratea, que es lo que el archivo literalmente dice. **Y eso rompe el juicio 2** |
| **3** | **La moneda.** No aparece ni una vez en `Preguntas.md` | `Cargo.cs` | Un solo monto sin moneda |
| **4** | **Para qué sirve el medio de pago.** Está listado como atributo y **ninguna de las cinco preguntas lo usa** | `Cargo.cs` | Se guarda, sin saber si es capacidad o ruido |
| **5** | **Qué convierte a un gasto en "responsable"** del movimiento del total | `VarianciaDelMes.cs` | Se listan **todos** los que cambiaron, ordenados por monto absoluto |
| **6** | **Cuánto vale un bien** | `Bien.cs` | Opcional. Sin valor se contesta en plata y **se dice** que el porcentaje no está disponible |
| **7** | ~~Cómo se fija un presupuesto~~ | `Panorama.CargosQueNadieReconocio` | ✅ **CERRADO.** No hay presupuesto declarado: *lo no presupuestado* **es** el imprevisto |
| **8** | **Cuánto se puede reducir** de un gasto reducible | `MargenParaRecortar.cs` | Se pone sobre la mesa cuánto hay en cada estado, sin decir si alcanza |

---

## Los tres que más duelen

### 7 · ~~La app se llama Presupuesto y el archivo no define el presupuesto~~ · CERRADO

Era el más grande, y se cerró **por borrado, no por relleno**: el concepto no faltaba, sobraba.

`Preguntas.md` usaba *"presupuestado"* como si estuviera definida —el juicio 8 dice que un imprevisto
es **lo no presupuestado**— y no decía quién fija un presupuesto. Lo que faltaba era preguntarlo, y la
respuesta fue:

> **Lo no presupuestado es lo mismo que un gasto imprevisto.**

O sea que **no hay un presupuesto declarado**, y no hace falta. La definición no es circular porque
las dos mitades las aportan cosas distintas:

| | |
|---|---|
| **Los candidatos** se deducen | Un cargo que **ningún gasto recurrente conocido reclama** |
| **El evento** lo marca la persona | *"Estos tres son la hospitalización"* |

O sea que *"presupuestado"* acaba significando **"atribuible a un gasto recurrente conocido"**, que es
una definición trabajable y que el modelo ya implementaba — sólo que describiéndose a sí mismo como
una aproximación.

> **Y una consecuencia que hay que dejar escrita**, porque es de las que alguien "arregla" en seis
> meses sin preguntar: en esta app, **"presupuesto" significa el total observado, no un plan**. El
> juicio 2 ya lo usaba así —*"el presupuesto mensual varió más de un 10 %"* es sobre lo que
> efectivamente se gastó—. Quien lea la palabra esperando un plan declarado va a creer que falta algo.
>
> Declarar un presupuesto —*"quiero gastar máximo 200 mil en comida"*— es una app distinta, y ninguna
> de las cinco preguntas la pidió. Agregarla sería modelar desde un diagrama en vez de desde las
> preguntas.

**Cómo apareció, y por qué importa para el método.** La entrevista no preguntó esto porque su bloque 2
va detrás de las **preguntas**, y *"cómo se fija un presupuesto"* es **vocabulario**. Cayó en el punto
ciego del guion — el mismo que dejó al dominio sin ningún tipo de ingreso. De ahí salió el **bloque 5**.

### 2 · El juicio 2 se rompe solo, y de una forma que nadie va a diagnosticar

El umbral del 10 % se vigila sobre el total mensual. Un seguro anual que cae entero en marzo dispara
ese umbral **sin que haya pasado absolutamente nada**: es el pago de siempre, en su mes de siempre.

O se prorratea —y entonces "el total mensual" deja de ser lo que se gastó y pasa a ser lo que se
devengó, que es una decisión con consecuencias— o el umbral tiene que saber distinguir. El archivo no
lo decidió, y el código deja el falso positivo **a la vista** en vez de taparlo con una decisión que
nadie tomó.

### 8 · La pregunta 3 no se puede contestar, sólo enmarcar

*"Con esta mensualidad nueva, ¿qué gastos puedo reducir?"* necesita saber **cuánto** se recorta de
cada gasto reducible. ¿El 100 % del streaming? ¿El 30 % de los restaurantes? Sin eso,
`MargenParaRecortar` no puede decir *"te alcanza"*: sólo puede decir cuánto hay en cada estado y
declarar que falta un dato.

Se prefirió eso a inventar el número, porque el número inventado **no revienta nunca**: da una
respuesta creíble y equivocada cada vez.

---

## Lo que sí alcanzó, y vale registrarlo

Cinco de los ocho juicios pasaron a código sin consultar nada, y tres de ellos produjeron
**estructura**, no configuración:

- **El juicio 5** —la severidad la decide el compromiso, no el monto— es lo que hace que
  `CuotaDePrestamo` y `PagoDeTarjetaDeCredito` sean clases y no etiquetas.
- **El juicio 6** produjo `Reducibilidad` con **tres** estados y no un booleano. Sin el tercero, un
  gasto sin marcar se contaría como no recortable y la pregunta 3 **subestimaría en silencio**.
- **El juicio 8** —la tasa se cuenta sobre eventos, no sobre cargos— es toda la razón de que
  `Imprevisto` exista como agregador.

La estructura de tres partes del archivo se mapeó sola: la **capacidad** fue el esqueleto de tipos,
los **juicios** fueron miembros de esos tipos, y la **UI** no hizo falta para nada, que es
exactamente lo que la doctrina predice.

---

## Y tres hallazgos que no son del archivo sino de las guías

> **Actualizado el 2026-09-01.** El [capítulo 3](../../Guias/03-construir-la-app.md) §8 bis dice ahora
> que los hallazgos sobre el método no viven en el `HUECOS.md` de un negocio: van a las guías, que es
> donde le sirven a los demás. Éstos se quedan porque **este directorio es un sujeto de prueba** —el
> archivo es el cuaderno de laboratorio— pero con su estado al día.

| El hallazgo | Estado |
|---|---|
| **La raíz no se llama como el negocio.** El primer intento nombró la raíz `Presupuesto` dentro de `Presupuesto.Dominio` y chocó; quedó `Panorama`. La convención del repo era uniforme y tácita | ✅ **Escrito.** Capítulo 3 §3, el 2026-09-01 — llevaba acá desde el 2026-08-26 diciendo *"el capítulo 3 debería decirlo"* |
| **La pregunta 3 no tiene doctrina.** Es **hacia adelante y contrafáctica**, y el capítulo 4 está construido enteramente sobre leer la serie del pasado | ⚠️ **Abierto.** Sigue sin capítulo. No es que esté mal contestada: es que las guías no saben explicar esta clase de pregunta |
| **Un `override` faltante desactivó un invariante en silencio.** `EsConsumo` **ocultaba** en vez de sobrescribir, así que la guarda contra contar dos veces cada compra con tarjeta no habría funcionado | ✅ **Ya era doctrina**, y esto la respalda: es el argumento más concreto que hay para el recuadro del capítulo 3 §8 —*un dominio no está terminado hasta que un perform lo tocó*— y para que la consola exista desde el primer día |

---

## Cómo se corre

```bash
dotnet run --project Metodologia/Pruebas/Presupuesto/App/Presupuesto.Consola -- comprobar
```

**31 comprobaciones**, sin actor, sin journal y sin disco. Cuando una falla, lo que se rompió es una
regla del negocio y no una tubería.
