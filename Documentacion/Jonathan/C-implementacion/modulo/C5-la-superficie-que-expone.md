# C5 · La superficie que expone un módulo

Cómo se escribe lo que el asistente va a poder preguntarle. Es la decisión de diseño más visible de un
módulo, y la que tiene un anti-patrón muy atractivo.

---

## La forma canónica

El mismo idiom detrás de REST, CLI, wasm, MCP — o la ventana nativa, que llama al despacho directo (ver `C7`):

```csharp
class ControllerLaboratorioAPI
{
    // Los puppets son PRIVADOS. El actor NO se crea por request:
    // se construye y configura una vez y se reusa (posee estado persistente).
    private static PerformanceV2 examenes = ...;

    [McpTool]                       // hermano de [HttpGet], en el MISMO método
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

Y esa es toda la superficie. **El módulo no implementa ningún servidor, ningún parser de comandos, ninguna
tubería.** Expone métodos anotados; el Host traduce.

**El atributo, en cargo.** El ejemplo usa `[McpTool]`, pero un módulo **cargo** marca su superficie con
`[ModuleSurface]` en la clase y `[Query("nombre")]` en el método, de `Gnothi.Contract`. Los dos valen:
`InProcessShell` reconoce la superficie **por nombre** —arrays `ClassMarks`/`ToolMarks`—, así engancha
tanto la etiqueta nueva como el `[McpTool]` viejo. Por qué se prefiere en cargo: `[McpTool]` **arrastra el
SDK de MCP al DLL**; `[Query]` no arrastra nada. El DLL cargo tiene que quedar liviano y sin transporte.
Ver `C4`.

## El anti-patrón, y por qué es atractivo

Dos asistentes de IA distintos, trabajando sobre este proyecto sin conocer el motor, propusieron
independientemente lo mismo:

```csharp
// ❌ ANTI-PATRÓN
handle(request) → response      // con el nombre de la consulta DENTRO de un string JSON
```

Está mal por dos razones:

1. **Desperdicia lo mejor de MCP**: descubrimiento con esquema por consulta. Un solo método genérico no
   publica nada que el asistente pueda usar para elegir.
2. **Mete strings en la frontera semántica.** El nombre de la operación viaja como texto sin tipo, y del otro
   lado hay que parsearlo.

Y hay una tentación gemela, del lado del dominio: exponer una consulta genérica
`Query(puppet, operacion, parametros)`. Eso **le daría al asistente una consola de DSL en vez de un
dominio**, tiraría a la basura el descubrimiento por esquema, y **expondría los puppets** — cuando desde
afuera nadie tiene por qué saber si un total salió de uno o de cinco.

> **El mundo exterior ve endpoints, nunca puppets. La unidad descubrible es la aplicación.**

## Las dos capas que es fácil confundir

Y esto reconcilia el anti-patrón con algo que sí hace falta:

> *«A nivel de ABI de WebAssembly **sí** hace falta un export estrecho (los exports de wasm son números y
> punteros). Ese export es **cáscara de transporte, no el contrato semántico**: debajo va un dispatcher
> tipado, encima van consultas con esquema. **No confundir las dos capas.»**

Por eso el export estrecho *parece* el anti-patrón y no lo es. El anti-patrón es poner el nombre de la
consulta en un string **en la capa semántica**.

---

## Las cinco reglas al escribir la superficie

**1 · Nombrar por intención de dominio, no por verbo HTTP.** `SeriePSA`, `MontoDeLaCuota`,
`CalendarioDePagos`. **Nunca** `Get`.

**2 · Sin DTOs.** `PerformQuery`/`PerformCommand` devuelven el string que los `print` del DSL produjeron;
con el formateador JSON por defecto **eso ya es el JSON**. Si hay que devolver 17 datos anidados o arreglos,
se hacen `for` y `print` dentro del script. **No hay tipo intermedio, no hay mapper, no hay
re-serialización.**

**3 · Pedir todo lo que se necesita de una vez, no chatear.** Una pantalla que necesita cliente + orden +
artículos + precios + tipo de cambio es **un** endpoint.

**4 · La descripción es semántica, no documentación.** Esto es lo más subestimado:

> El texto de la descripción **es lo único que el asistente tiene** para decidir si esa consulta contesta la
> pregunta de la persona.

Por eso se redacta **como la pregunta que una persona se hace sobre sus propios datos**, no como el nombre de
un método. Y viaja tal cual: el Host no la reescribe, porque estaría opinando sobre un dominio que no
entiende.

**5 · Y el tipo de retorno tiene forma, no es un string.** Con eso el esquema de salida se genera solo y el
asistente recibe un decimal de verdad en vez de texto que tiene que volver a parsear.

## Lo que el módulo NO decide

**El formato de salida.** JSON, TOON y XML son **proyecciones hermanas**, no capas apiladas — y el
formateador es propiedad de la capa de hosting, no del módulo. El Host pasa un *hint* de qué acepta, la
cáscara empuja el formateador, y **el dominio no se entera**.

Reglas que salen de ahí: **JSON obligatorio** en todo módulo (es la línea base de interoperabilidad), TOON
opcional y negociado, **negociación por sesión y elección por llamada** — negociar en cada llamada es
overhead puro.

**Y el formato nunca entra al journal**, mismo criterio que los cursores y los tokens: es proyección, no un
hecho de la vida de la persona.

---

## Consultas contra acciones: dos superficies distintas

Es la distinción que más cuesta ver, y determina cómo el Host descubre cada cosa.

| | qué es | ¿deja rastro? | cómo se descubre |
|---|---|---|---|
| **Consulta** (`PerformQuery`) | lectura, proyección | **no** — «queries leave no trace» | por el **descriptor** del módulo |
| **Acción** (`PerformCommand`) | escritura, asiento | **sí** — `define action` la primera vez | **por el journal** |

Consecuencia para el autor: **lo que tu módulo puede *escribir* es enumerable desde el journal, con su firma
canónica en el DSL.** No se puede esconder.

> **El catálogo de lo que un módulo puede hacer ES el journal.**

Eso vuelve el manifiesto **verificable**: el conformance no chequea que el autor dijo la verdad, **chequea el
journal contra lo declarado**.

## Y por qué el descriptor no puede salir de reflexión

Esto es una corrección que cuesta descubrir sola, y está en la especificación:

> *«Un SDK de consultas basado en atributos + reflection **se rompe igual** [con trimming]. La salida correcta
> son **source generators**: las anotaciones se resuelven en compilación y emiten el descriptor y el
> dispatcher como código concreto. **Trimming-safe por construcción.**»*

El atributo sigue estando sobre el método. Lo que cambia es **quién lo lee**: un generador en compilación, no
reflection en runtime. La razón es concreta: el descubrimiento de dominio del motor ya es por reflection y el
trimmer **poda en silencio** — el peor modo de fallo. Y trimming es lo que hace falta para bajar el payload.

**Para el autor del módulo eso no cambia nada del código que escribe.** Cambia quién lo procesa.

---

## Lo que el Host hace con todo esto

Cuatro pasos, y ninguno pide nada especial del módulo:

1. leer el descriptor y las acciones canónicas
2. **generar** las definiciones que el asistente ve, prefijadas por módulo
3. recibir la llamada e **invocar** el método
4. devolver el resultado **sin tocarlo**

Ese prefijo —`facturas_total_de_facturas` en vez de `total_de_facturas`— existe porque dos instituciones van
a llamar «total» a cosas distintas. Y tiene un efecto secundario útil: **es la prueba visible de que la
consulta no estaba en la configuración del asistente.**

## El límite honesto

Si el descriptor sale del propio módulo, **la institución escribe su propia descripción** — y esa descripción
es lo único que el asistente tiene para elegir.

La introspección puede verificar **comportamiento**: qué escribe, qué clases toca, qué reacciones registró.
**No puede verificar honestidad semántica.** Un módulo que se describe mejor de lo que es no se detecta
escaneando.

Eso queda del lado de la curaduría, o de que la persona lo apague cuando note que le miente. Y es una razón
más para que la descripción sea un compromiso serio y no marketing.
