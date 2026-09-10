# 03 — El módulo: qué carga el Host

## Anatomía

Un módulo publicado es:

```
dominio (DLL)        ← las clases con las reglas. CERO dependencias.
repertorio           ← tablas de referencia versionadas, recurso embebido
view declarativa     ← el módulo describe; el Host renderiza
manifiesto           ← capacidades declaradas, rango de sustrato, endpoint del publicador
```

Y **el journal crece aparte**, en el directorio que el Host le concedió.

Sin runtime, sin sustrato. Decenas a centenas de KB.

## ⚠️ Cómo se exponen los puppets — el error que dos IAs ya cometieron

Esto es lo más importante del documento. Dos asistentes de IA distintos, trabajando sobre este proyecto sin conocer el motor, propusieron independientemente una API genérica de un solo método:

```csharp
// ❌ ANTI-PATRÓN
handle(request) → response      // con el nombre de la tool DENTRO de un string JSON
```

Está mal por dos razones: desperdicia lo mejor de MCP (**descubrimiento con schema por tool**) y mete strings en la frontera semántica. La superficie correcta son **N tools intencionales**, escritas por el autor de la app, cada una con su firma tipada.

**El mundo exterior ve endpoints, nunca puppets.** La unidad descubrible es **la aplicación**; los puppets son implementación interna. Nadie afuera sabe si un endpoint consultó uno o cinco.

Forma canónica (el mismo idiom detrás de REST, CLI, wasm o MCP):

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

Reglas que se siguen:

- **Nombrar por intención de dominio, no por verbo HTTP.** `SeriePSA`, `MontoDeLaCuota`, `CalendarioDePagos`. **Nunca** `Get`.
- **Sin DTOs.** `PerformQuery`/`PerformCommand` devuelven el string que los `print` del DSL produjeron; con el formateador JSON por defecto **eso ya es el JSON**. Si hay que devolver 17 datos anidados o arreglos, se hacen `for` y `print` dentro del script. No hay tipo intermedio, no hay mapper, no hay re-serialización.
- **Pedir todo lo que se necesita de una vez, no chatear.** Una pantalla que necesita cliente + orden + artículos + precios + tipo de cambio es **un** endpoint.
- **A nivel de ABI de WebAssembly sí hace falta un export estrecho** (los exports de wasm son números y punteros). Ese export es **cáscara de transporte, no el contrato semántico**: debajo va un dispatcher tipado, encima van tools con schema. No confundir las dos capas.
- **El adapter MCP entra ANTES del formateador textual** y devuelve salida estructurada. JSON, TOON y XML son **proyecciones hermanas**, no capas apiladas. No re-parsear un string.

## El journal: un escritor, varios autores

Esta distinción se confundió una vez y produjo un diseño peor, así que vale precisarla:

> **Un journal tiene UN escritor: el actor.** No dice nada sobre cuántos **autores de hechos** hay.

Los eventos de la institución y el `PerformCommand` de Sonia llegan los dos como comandos **al mismo actor**, que sigue siendo el único que asienta. **No hay violación del invariante.**

```
                      ┌──────────────────────┐
   laboratorio  ──────►                      │   un escritor: el actor
   (eventos)          │   JOURNAL de Sonia   │   dos autores de hechos
                      │   en el módulo Lab   │
   Sonia  ────────────►                      │   procedencia atada a cada
   (PerformCmd desde  └──────────────────────┘   asiento — se distingue
    la view del módulo)                          para siempre
```

La formulación que queda, y sirve también para el pitch:

> **El journal es de Sonia. El dominio es de la institución. Los hechos pueden venir de las dos.**

**Por qué esto importa y no es un detalle:** poner "mi kilometraje es 999" en un journal genérico y aparte **amputaría el hecho del dominio que lo entiende**. Ese número no vale nada suelto; vale dentro del dominio de Auto, que sabe cruzarlo contra las cargas para sacar rendimiento y sabe que a los 5.000 km toca revisión. El hecho va donde está el dominio que lo interpreta.

### Regla que sale del invariante, no de una política

El journal de un módulo institucional tiene **exactamente un escritor legítimo**: el flujo de eventos del publicador. Entonces un `PerformCommand` **del modelo** ahí no es "riesgoso", es **ilegítimo por construcción**. Los comandos del modelo van al journal propio de Sonia.

Caso que suena ambiguo y se resuelve limpio: *"anotá que este examen me lo repitieron"* es una aserción **de ella sobre** un hecho institucional → va a su journal.

### Procedencia: atributo del asiento, no convención

Si los hechos de la institución y las acciones de Sonia caen en el mismo journal, hay que poder distinguirlos **para siempre**: para la UI (la factura dice lo que dice; tu anotación dice lo que anotaste), para la auditoría, y porque la política de purga y revocación difiere.

Mecanismo: el **Playbill**, con esquema declarado, atado al `entryId` del journal. El hecho va al journal; quién lo aportó va al lado. Campos a declarar para Gnothi: autor (`institución` | `sonia` | `modelo`), id del publicador, firma.

**No usar el overload de `PerformCmd` que recibe `ip`/`user`**: existe por compatibilidad y los valores **se ignoran** — no se journalean. La procedencia va por Playbill.

## Las cuatro categorías de dato (confundirlas rompe cosas)

| categoría | dueño | escritura | se comparte |
|---|---|---|---|
| **Journal institucional** (exámenes, cargas, compras) | Sonia | append-only, emisor institucional | jamás |
| **Repertorio** (rangos de referencia, horarios, reglas) | la institución | se reemplaza por versión | idéntico para todos |
| **Aserción propia** ("soy vegetariana", umbrales, metas) | Sonia | append-only, **autora = ella** | jamás |
| **Derivado externo** (composición de un producto que el asistente buscó) | nadie / el mundo | se cachea como repertorio, con fecha de consulta | — |

**Si los horarios del bus entran al journal** se rompe la inmutabilidad (los horarios cambian, y que hayan cambiado **no es un hecho de la vida de Sonia**) y se duplica el mismo contenido en cada usuario. El repertorio va con el módulo, versionado.

**La procedencia tiene que ser visible en la UI**: la factura dice lo que dice; una búsqueda dice lo que encontró. Mezclarlas visualmente es donde se pierde la confianza — que es el único activo del proyecto.

## El repertorio: dos cosas, y un lugar donde NO va

**(a) Reglas — son código.** Qué es un colesterol total, qué rango es normal para una mujer de 41, cómo se deriva una tendencia. Va en las clases de dominio. Es el activo real de la institución.

**(b) Tablas de referencia — datos versionados.** Rangos por edad y sexo, catálogo de analitos, mapeo de códigos.

| dónde poner (b) | veredicto |
|---|---|
| **Recurso embebido en el DLL**, leído por el dominio | ✅ **Esto.** Cambiar un rango = nueva versión del módulo, que es la semántica que le dimos al repertorio |
| Archivo de datos junto al DLL | ⚠️ funciona, pero abre quién lo firma y qué pasa si falta |
| Sembrado en el journal con `upgrade('...')` | ❌ **No.** Los rangos **no son hechos de la vida de Sonia** y se duplicarían por usuario |

Matiz legítimo para `upgrade`: la **configuración de esa instancia** (qué sucursal, qué sistema de unidades). Eso sí es estado del actor, y `OnHydrated()` + `upgrade('nombre')` es el idiom documentado, idempotente por diseño (el bloque se aplica una vez por actor, y editar un `upgrade` ya aplicado lanza excepción — se evoluciona agregando un nombre nuevo).

**Criterio falsable:** un cambio de rango de referencia produce un módulo nuevo y **no toca ningún journal existente**. Si para actualizar un rango hay que escribir en el journal de alguien, el repertorio se filtró a la historia.

## View ⟂ Repertorio: la propiedad comercial, y su condición de legitimidad

El tamaño y los temas de la app son **independientes** de la riqueza del módulo. La institución atrae con dashboards que Sonia valora, y con ellos viajan repertorio y actions que ella no sabe valorar — sus rangos normales, el histórico de otros exámenes, más datos que los que grafica la UI. Crípticos para ella; valiosos para el agente.

**Por qué funciona:** las dos mitades las juzgan jueces distintos. **La UI la juzga Sonia; el repertorio lo juzga el agente.** No compiten.

**Y es el mismo vector de abuso:** "invisible y más rico que la pantalla" es exactamente lo que un actor malicioso quiere. La defensa **no** es limitar el repertorio — es que sea **enumerable**:

`IActorIntrospection` es intrínseca (*"every actor exposes by virtue of being Puppeteer"*): `ShowEntry`, `ShowAction` (devuelve el **DSL canónico** de una acción), `ShowSymbols`, `ShowSymbol`, `ShowClass` (constructores, interfaces y métodos accesibles desde el DSL de un tipo), `ShowReactions`, `ShowReaction`, `FindPattern`, `ShowParameterPools`. Y `Actor.ConfigureStorageForIntrospection` lee entradas crudas **sin el dominio cargado**.

⇒ El Host puede mostrar qué carga un módulo y qué puede hacer **sin cooperación del módulo**. Eso es lo que vuelve la riqueza invisible **defendible** en vez de sospechosa.

## Actions: el catálogo ES el journal

El motor journalea `define action` la primera vez y `invoke action:N // args` después — de ahí sale la densidad. Consecuencia que sirve directo a la gobernanza:

> **El catálogo de lo que un módulo puede hacer ES el journal.**

"Qué puede hacer esta app" es una pregunta que se le hace al journal, no a la documentación ni al manifiesto. Eso vuelve el manifiesto **verificable**: el conformance no chequea que el autor dijo la verdad, chequea el journal contra lo declarado.

## Ajustes del journal que se fijan al crear y NO se pueden cambiar

`Puppeteer/EventSourcing/DB/FileSystem/FileSystemConnectionString.cs`:

| ajuste | valores | default |
|---|---|---|
| `path=<directorio>` | ruta | obligatorio si el string trae `=` |
| `maxFileSize=<bytes>` | entero > 0 | **4 MB** — rotación de archivo |
| `compression=` | `None` \| `GZip` | `None` |
| `encryption=` | `None` \| `Aes256Gcm` | `None` |
| `encryptionKey=` | base64 de **32 bytes** | ausente |

- Se aplica **al payload de cada registro**, y el orden es **comprimir → cifrar** (cifrar primero destruiría la compresibilidad).
- **Cada registro dice cómo está codificado** (bits `0x80` compresión / `0x40` cifrado en el byte de tipo) ⇒ el journal se puede leer sabiendo solo la llave, sin conocer la configuración con que se escribió.
- **El modo es propiedad del journal, no de la sesión.** Se graba en la metadata al crearlo, y pedir otro modo después **lanza excepción**. Hay un segundo guard: journal cifrado abierto sin llave → error explícito.
- **AES-256-GCM es autenticado**: detecta modificación, no solo oculta. Nonce aleatorio de 12 bytes por registro + tag de 16. Combinado con append-only: **nadie puede editar un hecho sin que la lectura falle.**
- **Costo: 28 bytes por registro.** Sobre un evento de menos de 100 bytes eso es del orden del 30% — **"<100 bytes por evento" y "cifrado activado" no son simultáneamente ciertas.** Reportarlas por separado.
- **GZip no tiene umbral**: si se pide, comprime siempre. Y el journal ya es denso por otras razones, así que en registros chicos **puede agrandarlos**. No ponerlo por default sin medir.
- La llave: en el dispositivo va al keystore del sistema (Keystore / Keychain), no al connection string en claro. Protege el **reposo** (respaldo del dispositivo, teléfono perdido); **no** protege contra un Host comprometido, porque en la consulta se descifra en memoria.

⇒ **Estos tres son campos del manifiesto, no configuración de runtime.** Si la spec no lo dice, alguna institución va a descubrir a los dos años que activar cifrado significa empezar un journal nuevo.

## Determinismo del DSL (para quien escriba dominio)

El journal graba **el texto de la llamada**, y a qué sobrecarga resuelve ese texto **se decide otra vez en cada replay**, contra la versión del dominio que esté cargada entonces. Una llamada que llegó a su parámetro por conversión implícita ligó **no exactamente**; el día que el autor agregue una sobrecarga que tome el tipo del argumento, una entrada ya grabada empieza a ejecutar **otro método** — silenciosamente.

**Nada supera a EXACTO.** Escribir siempre la forma explícita: literal tipado o cast para numéricos, `'A'c` para char, `(RuleKind)x` vs `(string)x` para enums, y texto se produce con `ToString()`, nunca con `(string)`.
