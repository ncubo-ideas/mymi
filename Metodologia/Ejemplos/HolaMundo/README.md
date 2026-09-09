# Capítulo 2 · Hola Mundo

**El paso 4 del [orden de trabajo](../../Guias/README.md).** Una app clientecéntrica del tamaño más
chico que sigue siendo honesta, para **tocarla y romperla** antes de construir la tuya.

Son **siete archivos de código**. Se lee de una sentada.

> **Éste es el único capítulo que no vive en `Guias/`**, y es a propósito: es el único que **es** un
> código. Un capítulo que explicara archivos de otra carpeta se desincronizaría; acá el texto y lo
> que describe se rompen juntos, y eso se nota.

> **Es nuestro, y es de mentira.** No es un negocio: es material de enseñanza. Los negocios de
> verdad —`Examenes`, `Compras`, `Cargas`, `Concesionario`— están en la raíz del repo, son de sus
> equipos, y son entre 15 y 30 veces más grandes. Cuando quieras ver cómo se ve esto en serio, andá
> ahí.

---

## Corré esto primero

```bash
dotnet run --project App/HolaMundo.Consola -- comprobar
```

Doce comprobaciones del dominio **puro**: sin actor, sin journal, sin disco. Si pasan,
el modelo piensa bien.

```bash
dotnet run --project App/HolaMundo.Consola -- correr C:/Gnothi/holamundo
```

El ciclo completo contra un journal de verdad.

**Y ahora corré el segundo comando otra vez, sobre la misma carpeta.** Eso es lo único
que hay que entender de todo el ejemplo:

```
  F-001           -> RECHAZADA — esa factura ya estaba en el libro
  F-001 de nuevo  -> RECHAZADA — esa factura ya estaba en el libro
  F-002           -> RECHAZADA — esa factura ya estaba en el libro
  Facturas en el libro : 2
  Total                : 13500
```

La app **se acordó**. No porque alguien guardara un archivo de estado, sino porque el
journal replayó todo lo que había pasado y reconstruyó el libro. Y las tres se
rechazaron sin ensuciar nada.

---

## Qué leer, y en qué orden

| | Archivo | Qué te enseña |
|---|---|---|
| 1 | [`Dominio/Linea.cs`](Dominio/Linea.cs) | Derivá, no recibas. Y validá al construir |
| 2 | [`Dominio/Factura.cs`](Dominio/Factura.cs) | Dónde se rechaza un dato mal formado: en el único lugar donde deja de ser listas sueltas |
| 3 | [`Dominio/Libro.cs`](Dominio/Libro.cs) | La raíz es la puerta. Y por qué no se llama como el negocio |
| 4 | [`App/HolaMundo.App/HolaMundoApp.cs`](App/HolaMundo.App/HolaMundoApp.cs) | El actor, y por qué la semilla va envuelta en `upgrade` |
| 5 | [`App/HolaMundo.App/HolaMundoAPI.cs`](App/HolaMundo.App/HolaMundoAPI.cs) | **El más importante.** El `Check`, y el valor tipado |
| 6 | [`App/HolaMundo.Consola/Program.cs`](App/HolaMundo.Consola/Program.cs) | El arnés con el que se trabaja |

Los dos `csproj` también valen: el del dominio no tiene **ni una** dependencia, y el de
la app **no referencia al dominio**. Los dos comentarios explican por qué.

---

## Cuatro cosas para romper

Aprendés más rompiendo que leyendo. Cada una falla de una forma distinta, y esa forma
es la lección.

**1 · Sacá el `Check`.** En `HolaMundoAPI.AgregarFactura`, cambiá
`PerformCheckThenCommand()` por `PerformCommand()` y borrá el `scriptForChk`. Corré
`correr` dos veces sobre una carpeta nueva.

> Ahora el duplicado revienta **adentro** del comando. Y la entrada ya se escribió: cada
> arranque siguiente la replaya y vuelve a fallar. **Rompiste el journal para siempre** —
> borrá la carpeta para recuperarte. Ésa es la regla 1 del substrato, y es la razón por
> la que existe el `Check`.

**2 · Sacá el `upgrade('semilla')`** en `HolaMundoApp.cs` y dejá el `libro = Libro();`
solo. Corré dos veces.

> La segunda corrida te borra todo: la semilla se vuelve a aplicar sobre un journal que
> ya tenía cosas.

**3 · Poné el dominio en `public`.** Cambiá `internal class Libro` por `public class
Libro` y quitá el `InternalsVisibleTo` del csproj del dominio.

> Compila igual — y ése es el punto. Nada te obliga; la cerca es una decisión. Ahora
> mirá cuánto tardás en tener un DTO del dominio viajando hasta la consola.

**4 · Pedile al `print` una colección.** En `ListaDeFacturas`, cambiá todo el `foreach`
por `print libro.Facturas;`.

> El motor lo rechaza con un mensaje que vale leer entero: *la proyección es del actor,
> no del material*. Decidir qué se ve de una colección es trabajo de la app.

---

## Lo que este ejemplo NO tiene

A propósito, y conviene saberlo antes de buscarlo:

| No hay | Dónde está eso |
|---|---|
| Proveedor de datos, tokens, refresh | Capítulo 5 — **pendiente** |
| Carátula de browser (Wasm), PWA | Capítulo 6 |
| Servidor MCP para el asistente | Capítulo 6 |
| Manifiesto e instalación | [El manifiesto](../../Guias/el-manifiesto-y-la-instalacion.md) |
| Un `Preguntas.md` | No lo tiene porque **no se construyó así**: se recortó hacia abajo desde algo que ya existía. Tu app sí empieza por ahí — [punto de partida](../../Guias/punto-de-partida.md) |

---

## Y después

Cuando esto te resulte obvio, seguís por el
[capítulo 3](../../Guias/03-construir-la-app.md), que es el mismo camino pero para tu
propio dominio. Y tené abierta al lado la página que de verdad se consulta:
[las reglas del substrato](../../Guias/reglas-del-substrato.md).
