# Capítulo 6 — El CLI y el acceso de la IA

Cómo se le da a una máquina una superficie sobre la app, cuánto de eso se puede generar solo, y qué
parte no se va a poder generar nunca.

> Lo de CLI está verificado: sale de construir uno completo, con 44 pruebas. **Lo de MCP no**: sale
> del diagrama del equipo y del SDK que menciona (`ModelContextProtocol` en NuGet), sin haberlo
> escrito ni corrido. Está marcado donde corresponde.

---

## 1. Tres consumidores, un contrato

Sobre los mismos performs hay tres clientes distintos, y quieren cosas distintas:

| Quién | Qué quiere | Por dónde |
|---|---|---|
| Una persona | prosa, ejemplos, que le digan qué hacer | `app help` |
| Una IA operando la herramienta | el contrato completo, parseable, de una sola llamada | `app describe` |
| Una IA dentro de una conversación | una lista de tools que puede invocar | MCP |

El error a evitar es escribir tres cosas. **Es una declaración y tres renderizados.** Si el `help` y
el `describe` se escriben aparte, se separan en la segunda semana: alguien agrega un parámetro y
actualiza uno solo.

---

## 2. Qué se puede generar y qué no

Ésta es la respuesta honesta a *"que los CLI se generen automáticamente"*. Construir uno completo deja
la frontera bastante clara:

| | Se genera solo | Por qué |
|---|---|---|
| Parseo de argumentos, posicionales y `--opciones` | **sí** | Es mecánico dada la lista de parámetros |
| Validación de tipos, requeridos, valores legales | **sí** | La declaración ya dice tipo, obligatoriedad y conjunto |
| Rechazo de opciones desconocidas | **sí** | Es el complemento de la lista |
| El `help` en prosa | **sí** | Renderizado de la declaración |
| El `describe` máquina | **sí** | Idem, otro formato |
| Los códigos de salida y la higiene de stdout | **sí** | Es siempre lo mismo (§5) |
| El cuerpo DSL de cada comando | **casi** | Se deduce de la firma de la fachada (§3) |
| **Qué comandos existen** | **no** | Eso son los requerimientos |
| **Qué significa cada parámetro** | **no** | Prosa que alguien tiene que escribir |
| **Qué default tiene y por qué** | **no** | Es una decisión de diseño |
| **Las notas y advertencias** | **no** | Es lo que aprendiste construyéndolo |

La línea es nítida: **la mecánica se genera; el significado no.** Un generador que invente el resumen
de un comando produce documentación que miente, que es peor que no tenerla.

La meta realista no es *"escribí los requerimientos y salió el CLI"*. Es: **el desarrollador declara
la superficie y no escribe ni una línea de parseo, ayuda, validación ni formato.** Eso ya es la mayor
parte del trabajo, y es lo que hace que agregar un comando cueste minutos.

---

## 3. La fachada ya es media superficie

El capítulo 3 §3 pide que la fachada del dominio sea **primitiva de entrada** y **escalar o proyección
de salida**. Esa exigencia viene del substrato, pero tiene un regalo: **la firma de la fachada es
casi la declaración del comando.**

```csharp
internal int RecordIncome(string source, string description, string currency, decimal amount, DateTime on);
```

De ahí sale, por reflexión, sin que nadie lo escriba: el nombre, cinco parámetros, sus tipos, el orden
posicional, y que devuelve un entero.

Falta lo que la firma no puede decir: cuál es obligatorio y cuál tiene default, qué valores son
legales, qué significa cada uno, y ejemplos. **Ése es el archivo que el desarrollador escribe**, y es
corto.

Un generador razonable, entonces: recorre la fachada, emite el esqueleto de la declaración con los
parámetros ya puestos, y deja los huecos de prosa marcados para llenar. No inventa nada.

---

## 4. Los valores legales se leen del modelo vivo

Esto sí cambió la calidad de la herramienta, y es barato.

Las listas cerradas —categorías, fuentes, formas de pago— **no se escriben en el CLI**. Se le
preguntan al modelo al arrancar, y el contrato las reporta:

```
parameters:
  - command: "expense"
    name: "category"
    required: true
    choices: ["Housing", "Food", "Vehicle", …, "Apicultura"]
```

Esa última entrada es una categoría que la persona inventó. Aparece porque el contrato la leyó del
modelo, no de una lista escrita a mano.

Para una IA la diferencia es enorme: acierta a la primera en vez de descubrir la herramienta a fuerza
de errores. Para el desarrollador también: una categoría nueva no requiere tocar el CLI.

---

## 5. La higiene que no es opcional

Tres cosas que si no están, la herramienta no sirve para una máquina. Las tres se generan.

**Sin argumentos, el programa ramifica por audiencia.** No imprime ayuda: dice a quién le habla.

```
Si sos Claude, otro LLM o cualquier agente operando esta herramienta:
  -> corré `app describe` primero.
Si sos una persona:
  -> corré `app help`.
```

Es la convención del CLI del propio framework, y es un compromiso estructural: la herramienta es
AI-first, no AI-tolerada.

**stdout lleva sólo la respuesta.** Todo lo demás —el ruido del framework, avisos, fallas— va a
stderr. Esto no es cosmético: el framework escribe en stdout por su cuenta, incluidas marcas de
progreso **sin salto de línea** que se pegan al principio de la línea siguiente. Si no lo capturás y
lo redirigís, la salida deja de ser parseable. (Capítulo 3 §7.5.)

**Los códigos de salida distinguen de quién es la culpa:**

| | |
|---|---|
| 0 | hecho |
| 1 | la línea de comandos estaba mal — nunca llegó al modelo |
| 2 | el modelo lo rechazó — no se escribió nada |
| 3 | el journal no se pudo abrir |

Separar 1 de 2 importa: le dice a quien llama si tiene que corregir la invocación o aceptar una
respuesta del negocio.

**Forma plana para lo que va a parsear una máquina.** Dos colecciones —`commands` y `parameters`—
unidas por el nombre del comando, en vez de parámetros anidados adentro de cada comando. Sobrevive a
cualquier formatter y a cualquier parser.

---

## 6. CLI y MCP

> **No verificado.** Lo que sigue sale del diagrama del equipo, no de código corrido.

El diagrama muestra la superficie MCP como una sola tool genérica:

```csharp
[McpServerToolType]
public class PuppetTools
{
    [McpServerTool]
    public string Query(string puppet, string operation, string parameters)
        => Puppeteer.Find(puppet).PerformQuery(operation, parameters);
}
```

Y una nota al lado: *"tool list que puede usar la IA, sólo va a haber total"* — o sea, arrancar con
una sola tool y crecer.

La observación que importa para este capítulo: **es la misma declaración de superficie.** Un `describe`
que ya sabe qué operaciones hay, qué parámetros toman, cuáles son obligatorios y qué valores son
legales, tiene exactamente lo que hace falta para emitir una lista de tools MCP. Una fuente, tres
consumidores.

Cuándo cada uno, hasta donde se puede decir hoy:

| | CLI | MCP |
|---|---|---|
| Cuándo | desarrollo, scripts, la IA operando la máquina | la IA dentro de una conversación con la persona |
| Dónde corre | donde esté el journal | proceso local junto al journal |
| Cruza contenedores | sí, es un proceso | **no con stdio** — ver capítulo 0 §5.1 |

Ese último punto es la decisión abierta del capítulo 0, y toca directamente a este capítulo: si el MCP
no puede cruzar el borde, **un CLI dentro del contenedor podría ser parte de la respuesta**. No está
decidido y no lo decide esta guía.

---

## 7. Antes de dar el CLI por terminado

- [ ] ¿Hay **una sola** declaración, y `help` y `describe` se renderizan de ella?
- [ ] ¿Los valores legales se leen del modelo vivo, no de una lista escrita a mano?
- [ ] Sin argumentos, ¿ramifica por audiencia en vez de imprimir ayuda?
- [ ] ¿stdout lleva sólo la respuesta, con el ruido del framework mandado a stderr?
- [ ] ¿Los códigos de salida distinguen "la invocación estaba mal" de "el modelo dijo que no"?
- [ ] ¿El contrato máquina es **plano**, unido por nombre?
- [ ] ¿Cada comando que escribe está detrás de un `Check`? (capítulo 3 §7.1)
- [ ] ¿Una opción desconocida se rechaza nombrándola, en vez de ignorarse?

---

## 8. Lo que este capítulo no cubre

- **El generador en sí.** §2 y §3 dicen qué es generable y de dónde sale; **nadie lo ha escrito**.
- **MCP verificado.** Todo §6 es lectura de un diagrama.
- **MCP en móvil.** Marcado como crítico y sin confirmar en `casos-de-uso-y-plan.md`.
- **Autenticación de un MCP sobre HTTP.** Si el MCP deja de ser un proceso local, deja de estar
  protegido por serlo. Capítulo 0 §5.1.
- **Settings de qué ve la IA.** Un contrato que reporta todas las operaciones y una persona que quiere
  ocultar algunas son dos requisitos que todavía no se cruzaron.
