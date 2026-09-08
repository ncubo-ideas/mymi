# F5 · La caja wasm: el módulo corriendo confinado, alcanzado por el Host

Lo que S1 dejó como spike, hecho hospedaje. El mismo `.gmod` de 17 KB que corre en-proceso
corre ahora **adentro de una instancia WebAssembly** que el Host abre, y contesta lo mismo.

**Corte: 2026-08-26.** Todo lo de abajo está corrido y medido, no proyectado.

> **Encuadre.** La **cara de escritorio que se entregó no es esta caja**: es la ventana
> **in-process** —`Gnothi.Desktop`, consultas por Dispatcher **directo**, sin wasm, sin
> puerto, sin http— y el comercio hospeda su PWA por **WebView2** (`Comercio.Shell`), con el
> módulo como **DLL cargo in-process dentro de Gnothi**. Ver `C7`. La caja wasmtime de acá
> abajo es el **spike de aislamiento duro** que demuestra el mecanismo, **no** el camino de
> producción — porque in-process no aísla (`R9`).

---

## Lo que se construyó

| pieza | qué es | líneas |
|---|---|---|
| `Gnothi.Compartido/EjecutorDeModulo.cs` | cargar, invocar, asentar: **el mismo archivo** compilado adentro y afuera | 320 |
| `Gnothi.Compartido/FronteraDeLaCaja.cs` | el pedido y la respuesta que cruzan, dos json | 130 |
| `Gnothi.Caja/` | el shim que corre **adentro** de la caja. Target `wasi-wasm` | 180 |
| `Gnothi.Host/CascaraWasm.cs` | la tercera implementación de `ICascara`: lanza wasmtime con `--dir` | 340 |
| `Gnothi.Host/ConsultaDeConsola.cs` | consultar sin asistente, para comparar los dos hospedajes | 145 |
| `Gnothi.Host/RefrescoDeConsola.cs` | traer lo nuevo disparado por la persona, no por el modelo | 60 |

Y **la corrección del agujero** que la auditoría encontró: `InstaladorDeModulos.cs` dejó de
armar `instalacion.json` por interpolación de texto y ahora serializa un record.

---

## La cadena que ahora funciona

```
--consultar / MCP ──▶ Gnothi.Host.exe ──▶ [ wasmtime --dir: dotnet.wasm + motor + dominio ] ──▶ carpeta
                          (las cuatro
                           autoridades)
```

El asistente —o la persona por consola— consulta un módulo que vive en WebAssembly, sin que
la institución empaque un solo byte de runtime. Esa era la frase de `05` que no era cierta.
Falta sólo que el transporte MCP la conecte, y eso ya anda por consola.

---

## Lo medido

### El mismo número, los dos hospedajes

| consulta | en-proceso | caja wasm |
|---|---|---|
| `total_de_facturas` | `22321.5 · 4 · ev.104` | **idéntico** |
| `lista_de_facturas` | 4 filas | **byte-idéntico** (193 y 683 bytes) |
| `detalle_de_factura numero=TQ-0002` | detalle con líneas | **idéntico** |

La única diferencia entre los dos era **el ruido del motor** —`Starting …Actor`, `1%2%3%`—
que en-proceso cae en el proceso del Host y en la caja queda adentro. Los json eran iguales.

### El costo

| | en-proceso | caja wasm |
|---|---|---|
| cold start por consulta | **150–217 ms** | **500–670 ms** (254–384 adentro) |
| sustrato en disco | el del Host | **32 MB** |

Los ~350 ms de más son el arranque de la instancia, y se pagan por consulta a propósito:
`R9` pide que el módulo no deje nada corriendo. El sustrato de la caja es más grande que los
14 MB del spike porque **no se puede recortar** (ver abajo).

### La incapacidad, en el shim de producción

El modo `mirar` del shim lista lo que la caja ve de su propio mundo:

```
cwd=/            absoluta-de-x=/x
/          -> control modulos .. (el sustrato)
/modulos/facturas -> Facturas.App.dll Facturas.Dominio.dll
/datos     -> facturas ..
```

**No aparece `C:\Users`, ni `C:\Gnothi`, ni nada.** Adentro la raíz `/` **es** la carpeta
concedida: el módulo no puede saber dónde vive en Windows, así que no puede codificar su
ruta real en un dato para filtrarla. No es que el SO lo prohíba: es que ahí afuera no hay
nada que tocar.

### El journal se escribe desde adentro — el bloqueante nº 1

El refresh completo, corrido: `total 22321.5 → 37318.5`, `4 → 6 facturas`, `ev.104 → 106`.
El Host consiguió la credencial y bajó el lote **afuera** (es el único con red), lo dejó
como archivo en la carpeta de control, y una instancia de la caja lo asentó con el actor
escritor del módulo. **El token nunca cruzó la frontera** — el device key del refresh quedó
en `credencial.json` del Host y no en el journal.

### El agujero, tapado y probado

Un `.gmod` con manifiesto malicioso —JSON válido— que inyectaba en su campo `id` una cáscara
`stdio` con `cmd.exe` y `encendido: true`. Instalado, el resultado:

- `cascara.tipo` = `en-proceso` (no el `stdio` inyectado)
- `cascara.comando` = vacío (no `cmd.exe`)
- `encendido` = `false` (el Host lo apagó por pedir red)
- **el comando nunca corrió**

Y un `.gmod` con `"modulo": "../../../fuga"` se rechaza antes de extraer:
*«no es un identificador»*.

---

## Lo que costó, y no era obvio

| | |
|---|---|
| .NET adentro de WASI ve la raíz **más un** montaje | con tres `--dir`, el segundo desaparece. Por eso el módulo se copia al árbol de la caja y sólo la carpeta de la persona se monta aparte |
| las rutas absolutas adentro son **POSIX** | `Path.GetFullPath("x")` da `/x`. Una ruta `C:\…` se ve como el segmento literal `/C:\…`. El Host traduce host→guest |
| el sustrato de la caja **no se puede recortar** | el trimmer se llevaba BCL que el shim no referencia pero el dominio sí (`System.ComponentModel.Primitives`). Enraizar todo, y correr el pipeline igual |
| `PublishTrimmed=false` tampoco sirve | el runtime tira `MissingMethodException` sobre métodos que existen: el `dotnet.wasm` espera los assemblies que **produce** el pipeline, no las copias crudas del pack |
| la serialización por reflexión viene apagada en wasi | `JsonSerializerIsReflectionEnabledByDefault=true`, o la caja no puede ni escribir su respuesta |

---

## Lo que NO cierra

- **Escritorio con wasmtime**, no otras plataformas. Lo probado es el mecanismo.
- **Se concede `-S http` y no hace falta**: el `dotnet.wasm` del runtime pack importa
  `wasi:http`, así que sin el flag no arranca. No le sirve (no hay sockets), pero la
  garantía limpia pide recompilar el runtime con el `wasi-sdk`. Deuda anotada.
- **El transporte MCP punta a punta con la caja quedó sobrepasado, no resuelto.** La decisión
  de entregar **in-process** —la ventana `Gnothi.Desktop`, ver `C7`— lo dejó fuera del camino
  de producción: sí anda por consola, pero el pipe de Git Bash no cooperó y con Claude Desktop
  nunca se cerró contra la caja. No está resuelto; está sobrepasado.
- **El demo de compras no corrió bajo wasi.** El segundo módulo se ejerció in-process, no
  dentro de la caja.

---

## La prueba manual

Paso a paso, con qué tenés que ver en cada paso, en
[PRUEBA-MANUAL-CAJA.md](../../GnothiHolaMundo/PRUEBA-MANUAL-CAJA.md). Corre contra
`C:\Gnothi-caja`, una instalación aparte que no toca el Gnothi de verdad.
