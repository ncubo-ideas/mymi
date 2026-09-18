# B9 · Lo que quedó del lado del módulo — el ensamblado que no se suelta y el candado de la institución

**Planteadas en revisión el 2026-09-14**, junto con la de `B8`.

**La #4 es cierta y es peor de lo que dice.** **La #5 apunta al lugar correcto por el motivo
equivocado**: la carrera que describe no existe —el actor de Puppeteer ya se protege solo— pero el
código de concurrencia que hay en las cinco instituciones sobra igual, y sobra por culpa del Host.

---

## Observación #4 — «`keepLoaded` deja el DLL cargado y no hay plan para actualizarlo»

> El módulo queda cargado mientras la cara está abierta. Eso cuesta memoria —54 MB para el
> primero— y, sobre todo, no está probado qué pasa si se actualiza un módulo mientras su DLL
> está cargado: si la próxima consulta usa el viejo, si el nuevo carga encima, o si falla.

### Lo que dice el código

**`keepLoaded: true` significa que `Evict()` no se llama nunca.** En `InProcessShell` el desalojo
está colgado del final de cada llamada:

```csharp
finally { if (!keepLoaded) Evict(); }   // en Ask (Invoke y View) y en ActualTools
```

Con `keepLoaded: true` esa línea no hace nada, y `Evict()` queda con un solo llamador:
`DisposeAsync()`. Y `HybridShell.DisposeAsync` se alcanza por **un solo camino**:

```
ModuleCatalog.DisposeAsync  ->  foreach (Module m in modules) await m.DisposeAsync();
```

que es el apagado del Host. **Nada más lo llama.**

### El comentario de `HybridShell` está equivocado

Ahí está escrito:

> *«El modulo queda cargado mientras la cara esta abierta, y se suelta al desalojarla.»*

**No se suelta.** Desalojar la cara es `WebModule.Evict() -> Teardown()`, que desarma el **WebView**
y nada más: no toca el `Module`, ni el `HybridShell`, ni el `InProcessShell`, ni el
`AssemblyLoadContext`. El ensamblado de la institución **queda cargado hasta que el proceso de
Gnothi muere**, la cara esté abierta o no.

### Y la actualización: la respuesta es la tercera opción, la que no se había mirado

El camino de actualización existe y está pensado (`ModuleEnrollment.Accept`, 44 §3.4-3.6):

```
Accept  ->  BeforeReplacing(module)        // «cerrar el módulo si está abierto»
        ->  mover <module> a <module>.anterior
        ->  mover la carpeta nueva a <module>
        ->  ReloadAfterInstall -> catalog.Load()
```

Los dos eslabones fallan **para el híbrido**, cada uno por su lado:

1. **`BeforeReplacing` cierra la cara, no la superficie.** En Android se cumple con
   `anfitrion.Evict(modulo)` — otra vez el `Teardown` del WebView. El `AssemblyLoadContext` con el
   DLL viejo sigue en pie.

2. **`catalog.Load()` reemplaza la lista sin disponer la anterior.** Termina con
   `modules = cargados;`. El reemplazo atómico está ahí a propósito —y su comentario lo
   argumenta: cierra los `Collection was modified`—, pero **nadie hace `DisposeAsync` de los
   `Module` que salieron de la lista.** El `HybridShell` viejo queda huérfano y **vivo**.

Así que la respuesta a *«¿el viejo, el nuevo, o falla?»* es:

> **La próxima consulta usa el nuevo, y el viejo se queda cargado al lado.**

Y el viejo no es un ensamblado inerte: adentro tiene su `static vivo`, el actor, **abierto sobre la
misma carpeta**. La carpeta concedida se conserva entre actualizaciones a propósito
(`priorGranted`), así que los dos actores apuntan al mismo journal.

**Eso es exactamente el escenario de dos actores sobre un journal que `keepLoaded` existe para
evitar** — el que se veía como `FileNotFoundException: Temp file not found`. Y ahora sí se sale del
alcance del `rwLock` del actor, que protege **un** actor contra sus propios llamadores, no dos
actores distintos contra el mismo archivo. La decisión que cerró el problema en el caso normal lo
vuelve a abrir en el caso de la actualización, y ahí abajo ya no hay red.

De las tres posibilidades que plantea la observación, es **la peor**, y es la que no se había
probado.

### Lo que no se puede confirmar todavía

**Los 54 MB no están medidos de nuestro lado.** No hay ninguna cifra de memoria en `F1`. La
observación puede tener razón y el número puede ser el correcto, pero acá no hay con qué
confirmarlo ni con qué desmentirlo. **Falta la medición**, y hace falta por módulo, no una vez.

### El arreglo

Son dos cosas, y las dos son del Host:

- **Que `catalog.Load()` disponga lo que saca de la lista.** No adentro del reemplazo —el
  reemplazo tiene que seguir siendo atómico— sino después, sobre la lista vieja, con `await`.
- **Que `BeforeReplacing` cierre el módulo entero**, no su WebView: el `Module` que se va a
  reemplazar tiene que soltar su ensamblado antes de que la carpeta se mueva.

Con la advertencia de siempre: **`Unload` es cooperativo.** Soltar el contexto no garantiza que se
descargue si el actor dejó referencias vivas — y el actor de la institución es justo el que las
deja. Por eso el primer paso, antes de programar nada, es el caso de prueba: actualizar un módulo
híbrido abierto y mirar qué queda. Y por eso el arreglo de la #5 (abajo) es la mitad de éste:
mientras el actor no tenga dueño, no hay a quién pedirle que lo suelte.

---

## Observación #5 — «el candado lo escribe cada institución, no el Host»

> La protección contra dos llamadas pisándose vive en `ExamenesSurface` —código de la
> institución—, no en el Host. Cada institución tiene que saber escribirlo. Además se vio un
> cierre abrupto del proceso en una de seis aperturas, así que el candado tampoco es toda la
> historia. El Host debería serializar las llamadas a la superficie: una cola por módulo dentro
> de `HybridShell` o `InProcessShell`.

### Lo que es cierto: el Host no serializa nada

`InProcessShell` **tiene** un semáforo:

```csharp
private readonly SemaphoreSlim gate = new(1, 1);
```

pero se toma en **dos lugares y ninguno es una llamada**:

| línea | dónde | qué serializa |
|---|---|---|
| 68 | `Start()` | cargar el ensamblado |
| 355 | `Evict()` | descargarlo |

`Invoke` (144), `View` (152), `Deliver` (193) y `Cursor` (226) **no lo toman**. El Host serializa
cargar y descargar; **no serializa preguntar**.

### Y lo que la observación da por hecho, y no es así: eso no hace falta

**El actor ya se protege solo.** `ActorHandler` (repo `Puppeteer`) tiene un
`ReaderWriterLockSlim rwLock`, y las dos puertas lo toman:

| | qué toma |
|---|---|
| `PerformCmd` — lo que usa `[Deliver]` | `rwLock.EnterWriteLock()` |
| `PerformQry` — lo que usan `[Query]` y `[View]` | `rwLock.EnterReadLock()` |

El comentario del propio `PerformCmd` lo dice con todas las letras:

> *«Concurrency: WRITE LOCK — a single thread at a time.»*

Y hay más que el candado: `GuardAgainstNestedPerform` convierte un perform anidado en un error del
contrato —con el script y quién tiene el candado— en vez del `LockRecursionException` pelado que
tiraría la BCL.

Así que **una entrega escribiendo y una vista leyendo al mismo tiempo no se pisan**: el lector
espera al escritor y al revés. La carrera que la observación quiere prevenir no está pasando, y una
cola por módulo en el Host sería un segundo candado encima de uno que ya funciona.

### Entonces el candado de la institución, ¿para qué sirve?

Para lo único que el actor no puede protegerse a sí mismo: **cuál actor existe, antes de que exista
ninguno.**

```csharp
private static readonly object candado = new();
private static ExamenesApp vivo;

private ExamenesApp Actor()
{
    lock (candado)
    {
        if (vivo is not null && carpetaDelVivo == folder) return vivo;
        if (vivo is not null) vivo.Actor.GracefulExit();
        ...construir, ConfigureStorage, Start()...
    }
}
```

Sin ese `lock`, dos llamadas simultáneas sobre un módulo recién abierto construyen **dos actores** —
y ahí sí vuelve el `Temp file not found`, porque son dos actores sobre un journal y ninguno sabe del
otro. El `rwLock` no puede ayudar: vive *adentro* de un actor.

Es decir: **el candado está bien puesto y cubre exactamente lo que tiene que cubrir.** Que no
envuelva la lectura no es un descuido — la lectura ya está cubierta un nivel más abajo.

### Pero la conclusión de la observación igual se sostiene, por otro camino

La pregunta que queda es mejor que la original: **¿por qué una institución tiene que escribir un
campo `static` y un candado?**

Por una decisión del Host. `InProcessShell.Ask` hace, en **cada llamada**:

```csharp
object? instance = method.IsStatic ? null : Build(t, 0);
```

La clase de la superficie **se reconstruye por llamada**. Entonces cualquier cosa que tenga que
sobrevivir entre llamadas —y el actor tiene que sobrevivir: arrancarlo es rehidratar el journal
entero— está obligada a ser `static`. Y un `static` construido perezosamente desde varios hilos
obliga a un candado.

**El Host mantiene el ensamblado cargado y tira la instancia.** Ese es el desajuste. Las cinco
instituciones escriben el mismo campo estático con el mismo candado para reparar a mano lo que la
cáscara les rompe — y lo copiamos cinco veces, que es la prueba de que no era decisión de nadie.

### El arreglo

No una cola: **que la cáscara guarde la instancia**, igual que ya guarda el `Type` y el
`AssemblyLoadContext`. Una superficie por módulo, reusada en las cuatro puertas, soltada en
`Evict()`.

Y entonces la superficie de una institución vuelve a ser lo que debería haber sido: **sin `static`,
sin `lock`, sin `carpetaDelVivo`, sin `GracefulExit` defensivo.** La concurrencia queda repartida
donde corresponde — el Host decide que hay una superficie, y el actor se defiende de sus llamadores.

Además **destraba la #4**: hoy nadie es dueño del actor, así que no hay a quién pedirle que lo
suelte cuando llega una versión nueva. Con la instancia en la cáscara, `Evict()` tiene qué cerrar.

#### La condición, y es la parte fácil de hacer mal

**La instancia tiene que nacer en `Start()`, bajo el `gate`.** Si nace perezosa adentro de `Ask` sin
tomarlo, la carrera del primer uso —dos llamadas simultáneas sobre un módulo recién abierto— no
desaparece: **se muda del módulo al Host**, que es peor, porque ahí ya nadie la está mirando.

`Start()` sirve tal cual: su doble chequeo (`if (marked is not null) return marked;` antes y después
del `gate`) ya publica el `Type` una sola vez, y la instancia puede publicarse por el mismo camino.

#### El detalle que la condición destapa: `ActualTools` también llama a `Start()`

Y ahí hay un costo que hay que mirar antes de escribirlo. Si la instancia nace en `Start()` **y** la
institución construye su actor en el constructor, entonces **listar los nombres de las tools
rehidrata el journal entero** — y `ActualTools` es lo que corre la comprobación de módulos, al
instalar y al arrancar la app.

La salida no obliga a elegir: **la instancia eager, el actor perezoso pero con una sola línea.**

```csharp
private readonly Lazy<ExamenesApp> app;   // ExecutionAndPublication por omisión
public ExamenesSurface(string folder) => app = new(() => Arrancar(folder));
```

`Lazy<T>` da la publicación única sin que la institución escriba un candado a mano, y sin hidratar
nada hasta que alguien pregunte de verdad. Es **una línea en vez de cuatro campos y un
double-checked lock**, que es la diferencia que importa: lo que se le pide a una institución deja de
ser «sepa escribir concurrencia» y pasa a ser «use el tipo de la BCL que existe para esto».

#### Y `Evict()` tiene que cerrar el actor

Al guardar la instancia, la cáscara pasa a ser **dueña del actor**, así que soltarlo es parte de
desalojar. Sin inventar un marcador nuevo: que `Evict()` haga `if (instance is IDisposable d)
d.Dispose();` y que el `Dispose` de la institución llame a `GracefulExit()`. El contrato crece en
una interfaz de la BCL, no en una etiqueta de Gnothi.

**Ese es el eslabón que le falta a la #4.** `Unload` es cooperativo y no completa si quedaron
referencias vivas — y el actor de la institución es justo quien las deja. Sin este cierre, «disponer
el `Module` viejo al actualizar» no descarga nada.

### Dos cabos sueltos

**El cierre abrupto en una de seis aperturas queda sin explicar.** Con el `rwLock` a la vista, la
carrera lectura/escritura deja de ser una hipótesis razonable. Hace falta el `logcat -b crash` o el
tombstone de esa corrida; sin eso cualquier explicación es inventada.

**Y uno para reportar en `Puppeteer`, no acá — verificado, con las líneas exactas:**

`SymbolTable.cs:122` declara `private bool _readOnlyMode = false;` — un campo **de instancia**, uno
por actor, no `[ThreadStatic]`. Lo encienden y lo apagan **tres** métodos que corren bajo el
**read** lock, que por definición sostienen varias consultas a la vez:

| método | enciende | apaga |
|---|---|---|
| `PerformQry` | `ActorHandler.cs:3656` | `:3669` |
| `PerformEmit` | `:3772` | `:3795` |
| `PerformChk` | `:3892` | `:3911` |

La primera que termina apaga la bandera mientras las otras siguen corriendo. Lo que esa bandera
protege, junto con `StrictQueryValidation`, son las dos guardas de `SymbolTable.cs:223` y `:274`:
que una consulta no escriba variables globales por accidente. **El efecto no es corrupción del
journal — es que esa guarda queda ciega durante los solapamientos**, y una consulta con un error
podría mutar estado sin que nadie lo detecte. Se arregla con un contador en vez de un booleano, o
con una bandera por ejecución.

*(El cuarto uso, `ActorHandler.cs:4048` en `PerformCheckThenCmd`, **no** está afectado: corre bajo
el write lock, que es exclusivo.)*

---

## Lo que las dos tienen en común, y `B8` también

Las observaciones —la de `B8` y estas dos— caen en el mismo lugar: **trabajo del Host que hoy está
del lado del módulo, o sin hacer.**

| | dónde está hoy | dónde va |
|---|---|---|
| medir lo que pregunta la pantalla (`B8`) | en ningún lado | Host |
| pedir desde N (el `[Cursor]`) | **ya se movió**: era de la cara, quedó en la superficie | Host ✅ |
| soltar el ensamblado al actualizar | sin hacer | Host |
| ser dueño del actor entre llamadas | en un `static` + `lock` por institución | Host |
| que dos llamadas no se pisen | **`ActorHandler.rwLock`, y ya está bien** | Puppeteer ✅ |

Ninguna tumba la forma híbrida. Dicen todas lo mismo de ella: **está probada como demo de un módulo
a la vez, y no como algo que se actualiza y se usa en paralelo.**
