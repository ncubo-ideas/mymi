# D3 · El motor: versiones, paquete, y qué cambió

## Una sola línea para todo el repo

`Directory.Build.props` en la raíz:

```xml
<PropertyGroup>
  <VersionDelMotor Condition="'$(VersionDelMotor)' == ''">3.0.0-beta.10044</VersionDelMotor>
</PropertyGroup>
```

MSBuild lo busca **subiendo desde cada proyecto**, así que tiene que quedar en un directorio ancestro de
todos.

El `Condition` no es decorativo: deja **sobreescribir la versión por línea de comandos** con
`-p:VersionDelMotor=…`, que es la salida cuando el feed no responde y la caché local no tiene la versión
declarada. Ver la advertencia al final de esta sección.

## Por qué NuGet y no ProjectReference

Mientras se referenciaba el repo del motor, **el build dependía de qué rama estuviera checkouteada** en un
repo que no es parte del entregable — y se rompió de verdad cuando alguien volvió a la rama principal.

Con el paquete la versión queda fijada en un lugar y nadie la cambia por accidente. El cambio se pagó solo.

---

## Qué cambió en 10029, y por qué importa

Medido sobre los binarios de cada versión:

| versión | referencias al guard de hilo único |
|---|---|
| `3.0.0-beta.10027` | **0** |
| `3.0.0-beta.10029` | **1** |

**El replay secuencial ya viene en el paquete.** Antes de 10029, la carátula wasm tiraba
`Cannot wait on monitors on this runtime` al configurar el storage: el pipeline de rehidratación corría
tres etapas concurrentes unidas por una espera bloqueante, y en un host de un solo hilo eso no existe.

Con 10029 arranca. Verificado: la app del andamio levanta y da su total sin apuntar al repo del motor.

### El pendiente que sigue abierto, y no es ese

**El guard tiene forma de PLATAFORMA, no de capacidad.** Pregunta si está en un browser.

Bajo **WASI** —un módulo corriendo fuera del browser, que es a donde va el diseño— eso da `false`, corre la
rama bloqueante, y **estalla con la misma excepción que el browser ya tenía resuelta.**

Es el spike bloqueante de la especificación: *«especificar el guard por capacidad antes de mergear. Si entra
con forma de plataforma, cada hospedaje nuevo agrega un `||` a esa línea.»*

Un flag inyectable, o el conteo de procesadores como default con override explícito del Host.

**Resuelto para el browser, abierto para WASI.** Y se confirmó por un segundo camino independiente al
intentar el POC en wasm.

---

## El paquete adelgazó, y bastante

El paquete `3.0.0-beta.10029` declara **dos dependencias**: un analizador (cero costo en runtime) y una
librería de hashing.

La versión anterior del árbol arrastraba clientes de base de datos y cripto. Medido sobre lo que el browser
realmente baja:

| build del motor | por la red (gz) | descargado | assemblies |
|---|---|---|---|
| ProjectReference al árbol viejo | 14,79 MB | 37,42 MB | 237 |
| **NuGet `3.0.0-beta.10029`** | **~10 MB** | ~26 MB | **199** |

La diferencia son **4,4 MB de base de datos, nube y cripto** que el paquete ya no trae: una librería de
criptografía de 4,68 MB crudos —el assembly más grande de toda la app—, un cliente de MySQL, uno de SQL
Server, y con ellos toda la cadena de identidad de nube.

Y el dato que pone todo en perspectiva: **el código propio de la app son 0,04 MB comprimidos.** El 0,27%
del payload.

> ⚠ Esas cifras son **sin trimming**. La especificación pide explícitamente la que falta: **Release
> trimmed** contra el árbol actual. Y advierte que probablemente **degrade el argumento de tamaño y ascienda
> el de auditoría** — que es mejor, porque no envejece con el próximo release del trimmer. **No citar estos
> números afuera como si fueran el peso final.**

### El desenlace: con cargo, el módulo carga cero motor

Adelgazar el paquete fue quitarle al motor lo que no usaba. El modelo **cargo** lleva la idea hasta el final:
el módulo **no trae motor en absoluto**. El Host presta `Ncubo.Puppeteer` y `Puppets.Portador`, y también
`Gnothi.Contract` — una etiqueta mínima, cero-deps. El módulo queda en **dos DLLs** —la superficie y el
dominio— que **solo referencian `Gnothi.Contract`**: la clase lleva `[ModuleSurface]` y el método
`[Query("nombre")]`, y nada más. El `InProcessShell` reconoce esa superficie **por nombre** —arrays
`ClassMarks`/`ToolMarks`—, así que levanta tanto una superficie cargo como los `[McpTool]` viejos.

De los ~10 MB del paquete, el módulo cargo no arrastra ni uno: el motor lo pone el Host, una sola vez, para
todos los módulos. El grueso del modelo —el paquete `.gmod`, los campos `view`/`load` del manifiesto, los
comandos `--pack` / `--install-module` / `--query`— merece su propio documento: ver `C7`.

---

## El feed es privado

El paquete vive en un feed privado de Azure DevOps. Consecuencias prácticas:

- **Sin acceso a ese feed no compila nada.** El error es un `NU1103` que menciona las versiones encontradas
  en el feed — y es lo primero que va a frenar a alguien nuevo.
- **Un contenedor no puede restaurarlo** sin credenciales. De ahí que el Dockerfile compile el Host desde
  fuente y **copie el módulo ya compilado**. Ver `D2`.

### Y la caché local se queda atrás del repo

**Medido el 2026-09-11 en la máquina de desarrollo:** `Directory.Build.props` pide `3.0.0-beta.10044`, y
`~/.nuget/packages/ncubo.puppeteer` tenía hasta `3.0.0-beta.10029-lector.4`. Con el feed devolviendo 403
—síntoma `NU1301`, ver `E1 §8b`— **la versión declarada no existe en ninguna parte de la máquina**, y el
repo no compila.

La app **sí compila contra `10029-lector.4`** (solo warnings), así que el bloqueo es de acceso al paquete,
no de API. Pero:

> ⚠ **Un publish hecho con una versión del motor distinta a la declarada no es el artefacto del repo.**
> Y por la restricción de determinismo de replay que cierra este documento, **no basta con que compile**:
> dos versiones del sustrato pueden replayar el mismo journal a estados distintos. Sirve para levantar una
> demo; **no sirve para medir, ni para comparar contra cifras anteriores, ni para dar por buena una
> rehidratación.**

## Dos cosas que rompen el build y no lo parecen

**Un comentario XML no puede contener dos guiones seguidos.** Escribirlos invalida el `Directory.Build.props`
entero, MSBuild no lo aplica, `$(VersionDelMotor)` queda vacío, y el error sale del lado de NuGet:
*«PackageReference sin versión especificada»*. **No menciona XML en ninguna parte.**

Y no es solo cosa de `.csproj` o `.props`: **un `.wxs` de WiX es XML igual**, así que dos guiones seguidos en
un comentario lo rompen lo mismo — pero ahí el síntoma aparece del lado de WiX, no de NuGet. La trampa
completa, con sus dos caras, está en `E1`.

**Un servidor MCP vivo bloquea el dll.** Si una sesión dejó un módulo corriendo, el build falla con «el
archivo se ha bloqueado por .NET Host». Se baja así:

```bash
powershell -Command "Get-CimInstance Win32_Process -Filter \"Name='dotnet.exe'\" | Where-Object { $_.CommandLine -like '*Facturas.Mcp*' } | ForEach-Object { Stop-Process -Id $_.ProcessId -Force }"
```

## Y una restricción del motor que hay que respetar siempre

**Nunca activar globalización invariante.** El constructor estático del parser pide una cultura concreta y
tira `CultureNotFoundException` si no está.

Y es **justo el flag que uno agrega para bajar el payload de wasm**, así que es fácil reintroducirlo sin
querer. Está comentado en el csproj de la app por eso.

## El versionado del sustrato: la restricción que casi nadie ve

El journal se reproduce contra el dominio actual **interpretado por el sustrato actual**. Entonces la
compatibilidad que importa **no es de API: es determinismo de replay entre versiones del sustrato.** Si una
versión ejecuta un constructo del DSL aunque sea un poco distinto, replayar cinco años de historia produce
**otro estado**.

No es hipotético: el journal graba **el texto** de la llamada y la sobrecarga se resuelve **otra vez** en
cada replay.

**Cómo se driblea:** una suite de **journals dorados** — journals congelados junto con el estado que deben
producir, replayados contra cada candidato de sustrato. Convierte «mantenemos compatibilidad» de promesa en
**afirmación falsable**. Barato ahora, carísimo de retrofitear.

Y de ahí sale la regla del manifiesto: declara un **rango**, y **el autor no declara compatibilidad — el
sustrato se la gana**. Ver `B4`.
