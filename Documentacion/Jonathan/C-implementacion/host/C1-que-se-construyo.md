# C1 · Qué se construyó

Un POC que prueba **un solo eslabón** del diseño: que un asistente le hable a un único servidor MCP y
reciba consultas de módulos que ese servidor no conocía de antemano.

**No prueba la arquitectura completa.** Lo que no prueba está al final, dicho sin maquillar.

---

## La cadena

```
asistente  ──MCP stdio──▶  Gnothi.Host  ──MCP stdio──▶  el módulo  ──▶  el journal
                            (lee manifiestos)             (rehidrata)
```

El asistente ve **una** lista. No sabe que hay dos saltos. Y la prueba la da el propio módulo en su log:

```
Server (app1-facturas 0.1.0), Client (Gnothi.Host 1.0.0.0) method 'tools/call'
```

**El módulo ve al Host como su cliente MCP.**

## Las piezas

```
GnothiHolaMundo/
├─ Gnothi.Host/
│  ├─ Despacho.cs             el núcleo. Catalogo() + Atender(). NO sabe de MCP
│  ├─ Program.cs              el frente MCP (Puerta 1). Traduce y delega
│  ├─ CascaraStdio.cs         el ÚNICO lugar que sabe cómo se ejecuta un módulo
│  ├─ PuenteDeTools.cs        manifiesto → consulta MCP publicada
│  ├─ Manifiesto.cs           lo que declara la institución
│  ├─ Instalacion.cs          lo que concede el Host
│  ├─ CatalogoDeModulos.cs    descubre Modulos/*/
│  ├─ Modulo.cs               una instancia: manifiesto + instalación + cáscara
│  └─ ToolsDelHost.cs         las consultas propias del Host
├─ Gnothi.Prueba/             cliente MCP portable: hace de asistente
├─ Modulos/facturas/          manifiesto + instalación (hospedaje Windows)
├─ docker/                    Dockerfile, compose, y el hospedaje Linux
├─ Gnothi.Desktop/            la ventana nativa del Host (Blazor Hybrid). Ver `C7`
├─ Comercio.Shell/            el PWA del comercio en una ventana WebView2. Ver `C7`
├─ instaladores/              los dos MSI (WiX v5, per-usuario, sin admin). Ver `C7`
├─ mcpb/ + gnothi.mcpb        el Host como extensión — intento fallido, ya no instala. Ver `C3`
└─ probar.ps1                 el escenario sin asistente, para ver el cable pelado
```

Y afuera, en el repo del ejemplo de negocio: los dos servidores del proveedor, el dominio, el mecanismo
de refresh, y el módulo.

## Las tres consultas que publica

| | de dónde sale |
|---|---|
| `gnothi_modulos_instalados` | del Host |
| `gnothi_verificar_modulo` | del Host |
| `facturas_total_de_facturas` | **agregada del manifiesto de un módulo** |

El prefijo `facturas_` lo puso el Host. Es la prueba visible de que la consulta **no estaba en la
configuración del asistente**: la agregó leyendo un json.

## Las cinco propiedades que quedaron demostradas

**1 · El catálogo sale del manifiesto, no de arrancar el módulo.** Al levantar, el Host imprime qué
publica — **antes** de que exista un proceso del módulo. Veinte módulos son veinte json, no veinte
replays de journal.

**2 · Arranque perezoso, y desalojo.** El módulo arranca en la **primera** llamada y se cierra al
terminar. `gnothi_modulos_instalados` reporta `levantado: false` entre consultas — y el chat de la app lo
leyó y lo explicó solo: *«encendido pero no levantado»*.

**3 · La respuesta pasa sin transcodificar.** Lo que llega al asistente es lo que produjo el módulo,
entero. Si el Host lo parseara tendría que **entender el dominio**.

**4 · El interruptor fino funciona, y desaparece en vez de negar.** Con una consulta apagada, la lista
baja a dos y llamarla da `Unknown tool`. **No «denegada».** Para el asistente esa consulta no existe.

**5 · Declarado contra real.** `gnothi_verificar_modulo` levanta el módulo, le pide su listado **por
protocolo**, y lo compara contra el manifiesto. Para verlo detectar algo: agregar una consulta inventada
al manifiesto y volver a correr.

## Y una propiedad del Host que ningún módulo puede dar

`bytesDelRegistro` lo mide el Host con `stat` sobre la carpeta que él concedió — **no se lo pregunta al
módulo**. Un módulo que reporta 4 MB mientras ocupa 400 no puede existir si nadie le pregunta.

Y `consultasDelAsistente` cuenta lo que el asistente extrajo. No es cuota para proteger el dispositivo:
es **vigilar al que vigila**.

## Cuatro formas de correrlo, las cuatro verificadas

| | qué agrega |
|---|---|
| **`Gnothi.Prueba` / `probar.ps1`** | ver el cable pelado, sin asistente |
| **Con un asistente real** | el escenario completo, en lenguaje natural |
| **En Docker** | **confinamiento falsable**: sin red, sin escritura fuera de su carpeta, sin capacidades |
| **La ventana nativa** (`Gnothi.Desktop`) | corre la consulta llamando al despacho **directo** — sin MCP, sin puerto, sin http |

Las tres primeras las detalla la capa D; la ventana nativa vive en `C7`. Que la cuarta cara corra el
mismo despacho sin protocolo de por medio es la prueba viva de que la Puerta 1 —el frente MCP— es una
cara más, no el Host. Ver `A2`.

---

## Qué NO demuestra

Esto es tan importante como lo anterior.

**El módulo `facturas` todavía tiene transporte.** Trae su propio servidor MCP, y la arquitectura destino
se lo quita: el módulo no debe hablar ningún protocolo. Con transporte propio, **el módulo tiene sockets**
— y ahí se cae la garantía que hace defendible todo lo demás. Está cerrado con 🚫 en la especificación;
ver `B2`.

**El módulo `facturas` trae el motor adentro.** En el destino lo presta el Host, una sola vez.

**Pero estos dos límites ya no describen el modelo destino — describen a `facturas`, el primer módulo.**
Llegó un segundo módulo real, `compras` (Súper La Guaria), y **cruzó**: es **carga** en el sentido pleno
—dos DLLs chiquitas (`Compras.App.dll` la superficie + `Compras.Dominio.dll` el dominio, cero-deps) +
`manifest.json` + `view.json`, empaquetadas en un `.gmod`—, **sin transporte y sin motor adentro**. El
Host le presta el sustrato (`Ncubo.Puppeteer` + `Puppets.Portador`) y `Gnothi.Contract`, una sola vez
(decisión `A1`). Su consulta `resumen`, corrida por consola con `--query`, devuelve un hecho real:

```json
{ "hayDatos": true, "totalGastado": 3174606, "cantidadDeCompras": 87, "ultimoEventoLeido": 387 }
```

Así que lo que sigue abierto no es «el módulo trae transporte y motor», sino terminar de mudar `facturas`
al molde en el que `compras` ya nació. Cómo se empaqueta e instala ese módulo cargo está en `C4`; cómo
llegó a la máquina, en `C7`.

**Las capacidades del manifiesto solo se avisan.** El catálogo grita si un módulo declara micrófono, pero
nada lo impide. Hacerlo cumplir necesita el sandbox.

**El aislamiento es de contenedor, no de módulo.** El contenedor pone paredes alrededor de *todo*; dos
módulos adentro compartirían todo. El aislamiento por instancia es la etapa siguiente.

**Y el arreglo de la concurrencia mitiga, no cierra.** Desalojar el módulo baja la ventana de dos
escritores a milisegundos. Se cierra cuando el módulo deja de ser un proceso aparte. Ver `B3` y `E3`.

## El límite conocido del código

`PuenteDeTools` solo publica **consultas sin parámetros**. Una con parámetros se **omite con aviso** en
vez de publicarse y fallar al llamarla — una consulta ausente es mejor que una que no se puede usar.

Sintetizar un delegate con N parámetros tipados en runtime pide árboles de expresión. Y hay una razón más
fuerte para no arreglarlo por reflexión: **el trimmer poda las anotaciones en silencio.** La salida
correcta son source generators. Ver `B2`.
