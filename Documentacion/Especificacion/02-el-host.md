# 02 — El Host: lo que ustedes construyen

## Las cuatro autoridades

El Host **no** es "la pantalla bonita" ni "un launcher". Es la pieza que tiene cuatro autoridades que no pueden pertenecer ni al modelo ni a cada módulo, porque gobiernan la relación entre ambos:

1. **Ciclo de vida** — instala, actualiza, habilita/deshabilita (On/Off), desinstala módulos. El interruptor es del Host: *si el interruptor lo implementa el módulo, no es un interruptor.* Apagar = el Host omite las tools de ese módulo de la lista que le entrega al agente, y **el módulo no se entera** (no puede distinguir "me apagaron" de "nadie preguntó"). Desinstalar = el Host borra el directorio y revoca; el módulo no participa de su propia muerte.
2. **Red** — el módulo no tiene internet; el Host sí. Endpoint **fijo por publicador**, registrado en la instalación, **no elegible en runtime** (si el módulo pudiera decir qué URL traer, exfiltra codificando datos en la ruta o el query).
3. **Mediación** — frontera hacia los modelos. Habla MCP/MCPB hacia afuera; el módulo no necesita saber que MCP existe.
4. **Ecosistema** — hace cumplir las capacidades. Un módulo puede venir de cualquier institución y **no por eso adquiere cualquier capacidad**. El Host es lo que convierte *"esta aplicación promete respetar a Sonia"* en *"esta aplicación carece de las capacidades necesarias para dejar de respetarla"*.

## Las dos puertas y una costura

Esta es la decisión de arquitectura más importante del Host, y la que decide si el proyecto envejece bien.

```
       PUERTA 1                          PUERTA 2
  un host externo nos pide           nuestro propio bucle
  una tool call (MCP)                de agente + llave del modelo
       │                                     │
       └──────────────┬──────────────────────┘
                      ▼
        ┌───────────────────────────────┐
        │  DESPACHO DEL HOST            │  catálogo · permisos · cuotas
        │  (todo lo que es de Host)     │  procedencia · formato · invocación
        └───────────────┬───────────────┘
                        ▼
                  instancia del módulo   ← ve UNA puerta, siempre la misma
```

### La forma enredada (la que sale natural, y hay que evitar)

```csharp
// Adentro del bucle de agente
class BucleDeAgente
{
    List<InstanciaModulo> modulos;   // ← conoce los módulos
    Permisos permisos;
    Cuotas cuotas;

    async Task Responder(string pregunta)
    {
        var tools = modulos.SelectMany(m => m.Tools());           // arma la lista él
        var decision = await modelo.Preguntar(pregunta, tools);
        var m = modulos.First(x => x.Nombre == decision.Modulo);   // elige él
        if (!permisos.Permitido(m, decision.Tool)) return;         // aplica política él
        cuotas.Contar(m);
        var r = m.Invocar(decision.Tool, decision.Args);           // invoca él
    }
}
```

Funciona perfecto. El problema aparece el día que un asistente móvil de terceros pueda llamarnos: hace falta una segunda entrada que haga *lo mismo*, y toda esa lógica vive adentro de una clase cuyo propósito es hablar con el modelo. Dos salidas malas: duplicar la lógica, o extraer a mano la mitad de una clase que ya está en producción. Y peor: **no se puede borrar el bucle de agente** aunque ya no se necesite, porque se lleva la lógica de módulos con él.

### La forma con costura (la correcta)

```csharp
// El núcleo del Host. NO sabe que existen modelos ni bucles de agente.
class DespachoDelHost
{
    public CatalogoDeTools Catalogo(Consentimiento c)     // la lista, para cualquier llamador
        => modulos.Where(m => c.Habilitado(m)).SelectMany(m => m.Tools());

    public Respuesta Atender(Peticion p)                   // la ejecución, para cualquier llamador
    {
        var m = modulos.First(x => x.Nombre == p.Modulo);
        if (!permisos.Permitido(m, p.Tool)) return Respuesta.Denegada();
        cuotas.Contar(m);
        var r = m.Invocar(p.Tool, p.Args, p.FormatosAceptados);
        playbill.Registrar(r.EntryId, autor: "modelo", ...);
        return r;
    }
}

// Adapter. Existe SOLO porque la app del asistente ajeno todavía no puede llamarnos.
class BucleDeAgente
{
    DespachoDelHost host;                 // ← lo único que conoce

    async Task Responder(string pregunta)
    {
        var tools = host.Catalogo(consentimiento);
        var decision = await modelo.Preguntar(pregunta, tools);
        var r = host.Atender(new Peticion(decision.Modulo, decision.Tool, decision.Args));
    }
}

// Y la Puerta 1, el día que exista, es OTRO adapter con la misma forma:
class FrenteMcp
{
    DespachoDelHost host;
    Respuesta OnToolCall(McpRequest req) => host.Atender(Traducir(req));
}
```

### Por qué funciona: es la dirección de las referencias

No es buena voluntad, es mecánica y es **medible**:

- Las flechas van en **una sola dirección**: `bucle → despacho → módulos`. Nunca al revés, nunca de costado.
- El bucle de agente **no tiene referencias a instancias de módulo**, no conoce sus directorios, no conoce el runtime.
- La lista de tools que usa el bucle es **la misma** que el despacho le daría a cualquiera. No la arma el bucle.
- No hay estado compartido entre el bucle y la capa de módulos salvo por parámetros y valor de retorno.

**El test:** poner el bucle de agente en **su propio assembly**, y afirmar que el assembly del despacho **no lo referencia**. Si el despacho llegara a nombrarlo, el grafo de referencias lo delata.

Hay precedente exacto en el repo: `UnitTestPuppeteer/PuppeteerCoreDependencyTests.cs` hace esto con `Microsoft.Data.SqlClient`, `MySql`, `Microsoft.AspNetCore` y `Sodium`. Su comentario de cabecera explica por qué hace falta un test para esto, y aplica palabra por palabra:

> *la propiedad no la expresa ningún tipo ni método, solo el grafo de referencias, así que nada en una revisión normal nota cuando se pierde — un solo `using` en un solo archivo alcanza, y el build queda verde porque la máquina del developer tiene todo instalado.*

**En una frase: "diseñada para ser borrada" = está en su propio assembly, nadie la referencia, y hay un test que lo prueba.**

Honestidad sobre el costo: *poco impacto* no significa *poco código*. Un bucle de agente con streaming y manejo de llave es trabajo real. Lo chico es la **costura**, no la capa.

## Lo que el Host NUNCA debe hacer

| ❌ | por qué |
|---|---|
| **Escribir el journal** | el asiento es el acto del puppet; si el Host escribe, pasa a ser efecto de infraestructura |
| **Transcodificar la salida del módulo** | tendría que parsear el resultado del dominio, y ahí deja de ser tonto; además cualquier pérdida en la conversión se vuelve un bug del Host adentro de los datos de la app |
| **Entender un evento** | si necesita el significado para hacer su trabajo, la capa se contaminó |
| **Ser bus entre módulos** | el invariante de un solo escritor garantiza que no compraría nada (ver `01`) |
| **Aceptar que el módulo elija la URL** | canal de exfiltración |
| **Compartir directorio entre módulos** | rompe el aislamiento |
| **Dejar que el módulo inicie algo** | sin iniciativa no hay marketing; el módulo no despierta, no notifica, no interrumpe |

Lo que queda del Host es **deliberadamente tonto**: red, calendarización, ruteo de mensajes, renderizado de UI declarativa, política y medición. Cero semántica.

## Estado de transporte: fuera del journal

Cursores, tokens y reintentos son del Host y **no son historia de Sonia**. Que un pull haya fallado tres veces no es un hecho de su vida. Almacenamiento del Host, separado.

## Formato de salida (JSON / TOON): quién decide

El formateador **no está horneado en el módulo**. En el motor es propiedad de la capa de hosting:

- `PerformanceV2.Formatter(IOutputFormatter)` — fluido. Y el constructor de N-proyección `new PerformanceV2(otra)` comparte **el mismo actor, hook y journal**, así que se pueden tener dos fachadas sobre el mismo puppet (una JSON, una TOON).
- Sobre un `ActorV2` sin capa de hosting es **por llamada**: `using (FormatterContext.Push(new ToonFormatter())) { … }`.

⇒ **No hace falta negociación de tres partes ni transcodificación en el Host.** El Host pasa un *hint* (`accepts: [json, toon]`), la cáscara del módulo empuja el formateador, y el dominio no se entera. **El formato de salida es proyección, nunca un hecho de la vida de Sonia, y no entra al journal** — mismo criterio que cursores y tokens.

Reglas para la especificación:
- **JSON obligatorio** en todo módulo: es la línea base que garantiza interoperabilidad.
- **TOON opcional y negociado**: el Host declara qué acepta; el módulo elige por respuesta.
- **Negociación por sesión, elección por llamada.** Negociar en cada tool call es overhead puro.
- **Nada fijo.** Fijar el formato hoy es apostar a algo de meses de vida en una especificación que queremos que dure.

Matiz técnico útil: el formato óptimo depende de **la forma de cada respuesta** — TOON gana claro con arreglos uniformes de objetos (diez años de una serie); JSON minificado suele ganar con estructuras anidadas o no uniformes (la ficha de un examen suelto). Como la elección es por llamada, eso sale gratis. Y cuidado con las cifras de ahorro de TOON que circulan: suelen medirse contra JSON *con formato bonito*, no contra minificado, que es lo que realmente se manda.

## Lo que el Host le muestra a Sonia

Esta es la parte que convence a un escéptico, y la columna de la derecha es la que hace verdadera la de la izquierda.

**Tamaño y tasa.** Ni hace falta preguntarle al módulo: el Host concedió el directorio, así que lo mide desde afuera. Un módulo que reporta 4 MB mientras ocupa 400 **no puede existir** — no hay superficie donde mentir. Y lo que vale mostrar no es solo el total sino **la tasa**: MB por mes y hechos por mes. Un total grande porque Sonia lleva ocho años con esa app es virtud; 40 MB en tres semanas es otra cosa. El ratio que delata al ineficiente sin que nadie juzgue a nadie: **bytes por consulta útil**.

**Cuidado con una injusticia:** el repertorio **no es historia de Sonia**. Un módulo con buen conocimiento de dominio pesa legítimamente más. Separarlo en la UI, o se castiga exactamente lo que se quiere premiar.

**Qué trae este módulo.** No lo declara el módulo — lo lee el Host con introspección (ver `03`). Sonia no necesita entender qué es un antígeno prostático; necesita poder ver **que ese dato existe y de dónde vino**.

**Purgar, como intermedio entre apagar y desinstalar.** Desinstalar es la pena de muerte y Sonia va a dudar porque pierde también lo bueno; un modelo que solo ofrece la opción máxima es uno donde nadie ejerce presión. Y purgar es podar la historia, que es el activo. La resolución: **la purga la propone el Host, la ejecuta el módulo, y el módulo tiene derecho a marcar hechos como no podables** — la serie de PSA de diez años sostiene el caso de Pedro; el tiquete de bus de hace tres años no. El dominio es el único que sabe qué es prescindible; que el Host decidiera por tamaño sería destruir conocimiento por peso.

**Snapshots ya existen y no requieren base de datos** — ver `05`.
