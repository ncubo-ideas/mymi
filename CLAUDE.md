# Gnothi — notas para Claude

## Commits

**Conventional Commits, sin excepciones.** El formato, los tipos y los alcances están en
[`CONTRIBUTING.md`](CONTRIBUTING.md). Hay un hook en `.githooks/commit-msg` que lo
verifica; si rechaza, el mensaje del hook dice qué faltó.

Asunto en minúscula, máximo 72 caracteres, sin punto final. El alcance es el negocio o el
nodo: `feat(examenes)`, `fix(examenes/url1)`.

El cuerpo dice **por qué**, no qué. El diff ya dice qué. Si algo se midió, va el número.

## La estructura

Un directorio por negocio en la raíz, con cinco nodos adentro y nombres en español:
`PaginaDeDescarga/` (URL1, es nginx, **no** es un csproj), `EndPointDeEventos/` (URL2),
`InyectorDeEventosDemo/`, `Dominio/` y `App/` (con `.App`, `.Wasm`, `.Mcp` y `.Consola`).

El directorio nombra el **objeto de negocio**, nunca la institución. La marca ficticia vive
solo en los assets y la config de `PaginaDeDescarga`.

**No propongas una estructura alternativa.** Es un acuerdo de equipo, no una preferencia.
Para un negocio nuevo se copia la carpeta de uno existente.

`HolaMundo/` es material de referencia y es **descartable**. Ningún negocio debe depender
de esa carpeta, aunque esté basado en ella.

## Invariantes que no se negocian

- `Dominio` no lleva **ninguna** referencia. Ni paquete ni proyecto.
- `EndPointDeEventos` e `InyectorDeEventosDemo` no referencian `Dominio` ni tocan el
  journal. Son el lado de la institución; lo único que los conecta es HTTP.
- Las tres carátulas entran por la API del dominio, nunca al actor crudo.
- Los servicios MCP son **solo query**. Nada de tools de escritura.
- El MCP usa `SoloLectura` con `OnHydrated` vacío, o cada pregunta de la IA escribe en el
  journal y rompe la invariante de un solo escritor.
- `Console.SetOut(Console.Error)` antes de tocar el motor en procesos stdio: con stdio,
  stdout **es** el canal JSON-RPC.
- El journal es un log append-only en file system. Nada de SQLite, MySQL ni SQL Server.
- Toda mutación pasa por un `PerformCommand` con guarda de idempotencia, y el dato más el
  checkpoint entran en el mismo comando.
- No reescribas `Puppets.Portador`.

## Cómo se valida

**Por consola.** El WASM está bloqueado por el motor (`3.0.0-beta.10027` sin replay
secuencial: la carátula carga y renderiza, pero tira `Cannot wait on monitors` al abrir el
actor). La carátula `.Consola` existe para eso.

```bash
dotnet run --project Examenes/App/Examenes.Consola -- comprobar
```

Un cambio en el dominio no está listo hasta que esas comprobaciones pasan.

## Al verificar algo servido por HTTP

**Mirá el contenido, no el status code.** Un `try_files ... /index.html` devuelve 200
sirviendo algo distinto de lo que pediste, y un 200 no distingue un caso del otro. Ya pasó:
`/descarga` respondía 200 mientras el browser mostraba el 404 de Blazor.
