# Observables — cómo se audita una app contra lo que prometió

El instrumento del **paso 8** del orden de trabajo. Toma el `Preguntas.md` de un negocio y su árbol
de fuentes, y reporta la diferencia.

No juzga estilo. Contesta una sola pregunta: **¿los juicios quedaron donde se dijo que iban a
quedar?**

---

## Las tres clases de afirmación

No toda regla de las guías se puede auditar, y fingir que sí es peor que admitirlo. Cada afirmación
cae en una de estas tres:

| Clase | Qué significa | Qué se hace con ella |
|---|---|---|
| **Discrimina** | Se puede mirar, y hay apps buenas que la pasan y apps que no | Va a la auditoría. Es lo valioso |
| **No discrimina** | Se puede mirar, pero **nadie la viola** — todos la pasan siempre | Va a la guía como principio. En la auditoría es ruido |
| **No es observable** | Requiere que un humano juzgue | Se dice que es doctrina, y no se finge que es chequeable |

Una afirmación que empieza en la primera clase puede caer a la tercera cuando se intenta escribir el
chequeo. Eso no es un fracaso: es el chequeo haciendo su trabajo.

---

## La regla que hay que leer antes de escribir un observable

> **Un `grep` no es un observable.**

En la primera corrida, dos de seis chequeos fallaron en los tres negocios y **ninguno era real**: se
estaba contando comentarios. El `DateTime.Now` que "encontró" en el dominio de Exámenes estaba dentro
de un comentario que explica *por qué* el dominio no llama a `DateTime.Now`. Y la "referencia" de las
tres `Dominio.csproj` era la línea que dice *"que este csproj no tenga un solo `PackageReference` ni
`ProjectReference` es…"*.

Lo irónico y lo importante: **cuanto mejor documentado está el código, más falsos positivos produce
un observable ingenuo.** O sea que falla justo en los proyectos más cuidados, que son los que uno usa
para calibrar.

Todo observable expresado como búsqueda de texto **tiene que declarar que excluye comentarios**. Y
antes de reportar un hallazgo, se mira la línea.

---

## La tabla

`✅` pasa · `❌` falla · `—` no se corrió

| # | La afirmación | Dónde se mira | Qué cuenta como pasar | Clase |
|---|---|---|---|---|
| 1 | El dominio no tiene dependencias | `<Negocio>/Dominio/*.csproj` | Cero elementos `<PackageReference>` y `<ProjectReference>` | no discrimina |
| 2 | El dominio no lee el reloj | `<Negocio>/Dominio/**/*.cs`, **sin comentarios** | Cero `DateTime.Now`, `.Today`, `.UtcNow`. La fecha entra por parámetro | no discrimina |
| 3 | El proveedor no conoce el dominio | `EndPointDeEventos/` e `InyectorDeEventosDemo/` | Ninguno referencia `Dominio/` ni toca el journal. Lo único que los conecta es HTTP | no discrimina |
| 4 | No hay tool genérica | Las tools del `.Mcp` | Ninguna con forma `Query(puppet, operacion, parametros)`. Cada tool es una pregunta | no discrimina |
| 5 | **Hay al menos una pregunta que sólo contesta la IA** | Nombres de tools vs. lo que renderizan las `.razor` | Existe **al menos una** tool cuya respuesta ninguna vista muestra | **discrimina** |
| 6 | Los argumentos de la frontera son conjuntos cerrados | Firmas de las tools | El parámetro es un `enum`, no un `string`: la lista de valores válidos vive en el `inputSchema`, no en la prosa de la descripción | por correr |
| 7 | Los juicios viven en el dominio | Umbrales, periodicidades, direcciones | Son miembros de clases de `Dominio/`, no constantes en `App/`, `Wasm/` o `Mcp/` | por correr |
| 8 | El MCP no corrompe su propio canal | `Program.cs` del `.Mcp` | Redirige `stdout` antes de tocar el motor — con transporte stdio, `stdout` **es** el canal JSON-RPC | por correr |
| 9 | La app no es cliente de los servicios generales | URL2 | Hay un señuelo que devuelve **403 con el token válido** de la app | por correr |
| 10 | Cada clase justifica existir | Las clases del dominio | Cada una tiene al menos un miembro que ninguna otra tiene | **no es observable** — requiere juicio |

### Sobre el 5, que es el único que discriminó

Es el que vale, y por eso conviene precisarlo. La regla del equipo es *"si la IA puede contestar algo
que ya está en una pantalla, el demo no probó nada"* — y eso es sobre **la demostración**, no sobre
cada tool. Una tool que duplica una pantalla no hace daño; simplemente no es la prueba.

Por eso el observable es de **existencia** (*"al menos una"*) y no de pureza (*"ninguna duplica"*).
Escrito como pureza marcaría cosas legítimas.

### Sobre el 10, que se cayó al escribirlo

*"Cada clase tiene algo que ninguna otra tiene"* es exactamente el criterio correcto —la hemoglobina
sabe el grado de anemia, y no existe el grado de anemia de un colesterol— pero decidir si un miembro
es un juicio propio o un accesor disfrazado necesita entender el negocio. **Es doctrina, y la guía lo
dice así.** Un chequeo que lo intentara daría ruido en las dos direcciones.

---

## La primera corrida

2026-08-26, contra los tres negocios del repo. **Los negocios eran la vara, no los examinados**: se
estaba calibrando el chequeo contra apps que ya se sabe que están bien hechas.

| # | Exámenes | Compras | Facturas |
|---|---|---|---|
| 1 | ✅ | ✅ | ✅ |
| 2 | ✅ | ✅ | ✅ |
| 3 | ✅ | ✅ | ✅ |
| 4 | ✅ | ✅ | ✅ |
| **5** | ✅ | ❌ | ❌ |

**Facturas** tiene una sola tool, `total_de_facturas`, y `MisFacturas.razor:86` muestra ese total. Es
el reclamo que el equipo ya había hecho.

**Compras** también falla, y eso no estaba previsto: sus cuatro tools tienen contraparte en pantalla
—el total en `MisCompras.razor:77`, la comparación de canastas en la 118, la garantía en
`Detalle.razor:43`—. La lectura probable es que **Compras no está mal, está antes**: Exámenes llegó a
tener tools sin pantalla por un refactor explícito y reciente, el PR 10005, que se llama *"la tool de
la IA pasa a mirar el hueco, no el dato"*. Quien decide si eso es un defecto es el dueño del negocio,
no el chequeo.

**Exámenes** pasa limpio: `velocidad_de` y `que_me_falta_medir` no están en ninguna pantalla.
`Tendencia.razor` menciona "ritmo", pero en un comentario — hubo que mirar la línea, que es
exactamente lo que la regla de arriba manda hacer.

### Qué cambió en las guías por esta corrida

Un observable falló y **el que estaba mal era la guía**, no las apps. El capítulo 0 §8 decía *"el
dominio devuelve hechos, nunca prosa"*. `Interpretacion.cs` lo contradice con argumento: la lectura
**sí** la arma el analito, *"es lo único que puede decir mejor que nadie"*; lo que no se inventa es
el **consejo**, que sale de un conjunto cerrado. Y los hechos viajan siempre al lado.

El límite no estaba donde yo lo había puesto. Está entre **nombrar** e **inventar**. El capítulo 0
ya quedó corregido.
