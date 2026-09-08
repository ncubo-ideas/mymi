# B2 · El eslabón Host ↔ módulo

**MCP muere en el Host.** Entre el Host y el módulo no hay MCP, ni stdio, ni protocolo alguno. Este
documento explica por qué, y qué hay en su lugar.

Es uno de los dos cortes que más fácil se deshacen sin darse cuenta.

---

## La regla, y de dónde sale

De la especificación, la autoridad 3 del Host: *«Mediación — frontera hacia los modelos. Habla MCP/MCPB
hacia afuera; **el módulo no necesita saber que MCP existe**.»*

```
asistente  ──── MCP sobre stdio ────▶  HOST  ──── invocación local ────▶  módulo
                 (protocolo)                       (sin protocolo)
```

**MCP es la lengua de frontera. Adentro no hace falta hablarla.**

## Lo que está cerrado con 🚫

De `04-restricciones` del arquitecto: *«stdio/ssh entre el Host y el módulo: descartado»*, por dos razones
independientes:

1. **ssh solo tiene sentido entre máquinas distintas.** Acá el Host y el módulo viven en el mismo
   dispositivo. No hay nada que puentear.
2. **Si el módulo implementa un aparato ssh, el módulo tiene sockets.** Se cae *«ninguno de ellos puede
   salir por su cuenta»*, que es la garantía que hace defendible todo lo demás.

Y una tercera, del repo del motor: arrastraría de vuelta la **capa de transportes de Choreography**, que
es exactamente la línea de amputación que se cortó para que el motor entre a un target confinado.

Lo correcto: **el módulo no tiene transporte. Expone su superficie; el Host elige la cáscara. Un solo
artefacto, dos hospedajes.**

## Y el otro extremo: el anti-patrón que dos IAs propusieron

De `03-el-modulo`, y vale citarlo porque es un error atractivo:

```csharp
// ❌ ANTI-PATRÓN
handle(request) → response      // con el nombre de la tool DENTRO de un string JSON
```

Está mal por dos razones: **desperdicia lo mejor de MCP —descubrimiento con schema por tool— y mete
strings en la frontera semántica.**

La superficie correcta son **N tools intencionales**, escritas por el autor de la app, cada una con su
firma tipada. Y el mundo exterior ve endpoints, **nunca puppets**: la unidad descubrible es la
aplicación; nadie afuera sabe si un endpoint consultó uno o cinco.

## Las dos capas que es fácil confundir

Esto reconcilia el «SSH» de la grabación con el «llamada a función» de la lámina, y lo dice la
especificación:

> *«A nivel de ABI de WebAssembly **sí** hace falta un export estrecho (los exports de wasm son números y
> punteros). Ese export es **cáscara de transporte, no el contrato semántico**: debajo va un dispatcher
> tipado, encima van tools con schema. **No confundir las dos capas.**»*

Por eso el export estrecho *parece* el anti-patrón y no lo es: el anti-patrón es poner el nombre de la
tool en un string **en la capa semántica**.

## Cómo se ve la superficie correcta

La forma canónica, el mismo idiom detrás de REST, CLI, wasm o MCP:

```csharp
[ModuleSurface]                     // en la CLASE — de Gnothi.Contract, NO de MCP
class ControllerLaboratorioAPI
{
    // Los puppets son PRIVADOS. El actor NO se crea por request:
    // se construye una vez y se reusa (posee estado persistente).
    private static PerformanceV2 examenes = ...;

    [Query("SeriePSA")]             // hermano de [HttpGet], en el MISMO método
    public IActionResult SeriePSA(int desde)
    {
        string result = examenes
            .Using("for e in psa[@desde] { print e.Fecha fecha, e.Valor valor; }")
            .WithParameters(p => { p["desde", typeof(int)] = desde; })
            .PerformQuery();
        return Content(result, "application/json");   // el print ya serializó
    }
}
```

**Por qué `[ModuleSurface]` y `[Query]`, y no `[McpTool]`.** Acá se cierra una tensión que este mismo
documento abre. Arriba dijimos *«el módulo no necesita saber que MCP existe»* — pero con `[McpTool]` sobre
el método, el módulo **sí** referenciaba un atributo de sabor MCP: para compilar tenía que conocer el
paquete de tools. `[ModuleSurface]` en la clase y `[Query("nombre")]` en el método vienen de
**`Gnothi.Contract`**, una etiqueta mínima **sin dependencias**, y recién ahí la frase se vuelve literal:
el módulo declara su superficie **sin nombrar MCP en ninguna parte**. El Host traduce esas marcas a
definiciones MCP en la frontera; el módulo nunca lo toca.

**Y el módulo no trae motor.** Esa superficie son **dos DLLs** —`X.App.dll` (la superficie) +
`X.Dominio.dll` (el dominio, cero-deps)—, sin `Ncubo.Puppeteer` ni `Puppets.Portador` adentro. El
`PerformanceV2` de arriba corre sobre un motor que **presta el Host** (decisión `A1`): el módulo pide, el
Host pone la máquina. Ver `C7`.

Las reglas que se siguen:

- **Nombrar por intención de dominio, no por verbo HTTP.** `SeriePSA`, `MontoDeLaCuota`. **Nunca** `Get`.
- **Sin DTOs.** El `print` del DSL ya produce el JSON. Si hay que devolver 17 datos anidados, se hacen
  `for` y `print` dentro del script. No hay tipo intermedio, no hay mapper, no hay re-serialización.
- **Pedir todo de una vez, no chatear.** Una pantalla que necesita cliente + orden + artículos + precios
  es **un** endpoint.
- **El adapter MCP entra ANTES del formateador textual** y devuelve salida estructurada. JSON, TOON y XML
  son **proyecciones hermanas**, no capas apiladas. No re-parsear un string.

## Cómo el Host sabe qué consultas hay — tres fuentes, tres preguntas

Acá es donde es fácil equivocarse. Son cosas distintas:

| pregunta | fuente | cuándo |
|---|---|---|
| ¿qué consultas **ofrece** este módulo? | el **descriptor** que trae + la declaración del manifiesto | al instalar |
| ¿qué **puede hacer** de verdad? | **el journal**, vía introspección | conformance y auditoría |
| ¿cuáles ve el asistente **ahora**? | el **consentimiento** de la persona filtra el catálogo | en cada listado |

La segunda es la más fácil de poner en el lugar equivocado. La especificación la enuncia así:

> **«El catálogo de lo que un módulo puede hacer ES el journal.»**
> *«"Qué puede hacer esta app" es una pregunta que se le hace al journal, no a la documentación ni al
> manifiesto. Eso vuelve el manifiesto verificable: el conformance no chequea que el autor dijo la
> verdad, **chequea el journal contra lo declarado**.»*

**Ojo con el matiz:** eso aplica a las **acciones**, o sea a lo que el módulo *escribe*. El motor
journaliza `define action` la primera vez y `invoke action:N` después. Las **consultas no dejan rastro**
en el journal por diseño, así que por ahí no se descubren — para eso está el descriptor.

## Y por qué el descriptor no puede salir de reflection

De `04-restricciones` R7, y es la corrección que más cuesta descubrir sola:

> *«Un SDK de tools basado en atributos + reflection **se rompe igual** [con trimming]. La salida correcta
> son **source generators**: las anotaciones se resuelven en compilación y emiten el descriptor de tools y
> el dispatcher como código concreto. **Trimming-safe por construcción.**»*

El atributo ya no es `[McpTool]`: es `[ModuleSurface]`/`[Query]` de `Gnothi.Contract` (arriba). Pero el
punto se mantiene, y no depende de cuál atributo sea: lo que importa es **quién lo lee**, un generador en
compilación y no reflection en runtime. La razón es concreta: el descubrimiento de dominio del motor ya es
por reflection y el trimmer **falla en silencio**, que es el peor modo de fallo. Y trimming es lo que hace
falta para bajar el payload.

**Y un matiz de cómo funciona hoy.** El `InProcessShell` no reconoce la superficie resolviendo el *tipo*
del atributo por reflection —eso es justo lo que el trimmer rompe— sino **por nombre**: lleva dos arrays,
`ClassMarks` y `ToolMarks`, con los nombres de las marcas que acepta. Por eso reconoce de una vez la marca
de carga **y** los `[McpTool]` viejos: son nombres en una lista, no tipos ligados. Y el descriptor no sale
solo de las marcas: el `manifest` gana un `load.entry` que dice **cuál DLL trae la superficie**, y las
deps se resuelven **por adyacencia** —al lado del `entry`—, no rastreando el grafo en runtime. Ver `C7`.

## La forma que queda

El trabajo del Host es la traducción:

1. leer las acciones canónicas y el descriptor del módulo
2. **generar** las definiciones MCP
3. recibir la llamada y **invocar** la acción
4. devolver el resultado **sin tocarlo**

Eso es literalmente el «proxy». Y no hay protocolo intermedio que diseñar.

## Una forma de quedarse con lo bueno de las dos

El **manifiesto del módulo sí puede tener forma MCP** — un documento de definiciones de tools. Es una
declaración estática, no una conexión. Eso le da al developer un formato familiar y al Host algo contra
qué comparar.

Y ahí se vuelve significativa la dualidad *«o los escaneamos, o los da en un archivo»*: **no son dos
alternativas, son las dos mitades de una verificación.**

- el **archivo** es lo que el módulo *dice* que hace,
- el **escaneo** es lo que el módulo *puede* hacer,
- y **el Host compara los dos**.

Un módulo que declara menos de lo que puede es exactamente la señal de alarma. Y eso es lo que hace
verdadero el aforismo: **lo invisible puede ser rico, pero nunca opaco** — no porque el módulo prometa,
sino porque el Host puede desmentirlo.

## El límite que queda igual

Si el descriptor sale del propio módulo, entonces **la institución escribe su propia descripción** — y esa
descripción es lo único que el asistente tiene para decidir si esa consulta contesta la pregunta de la
persona.

La introspección puede verificar **comportamiento** (qué escribe, qué clases toca, qué reacciones tiene).
**No puede verificar honestidad semántica**: un módulo que se describe mejor de lo que es no se detecta
reflejando ni escaneando. Eso queda del lado de la curaduría, o de que la persona lo apague cuando note
que le miente.
