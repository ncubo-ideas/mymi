# El manifiesto y la instalación

Los dos archivos que hacen que un módulo se pueda **catalogar, conceder y verificar** sin arrancarlo.

> **De dónde sale este capítulo, y con qué reservas leerlo.** Del Host, que existe y corre: la rama
> `master_gnothiHostDesktop` tiene `Gnothi.Host`, `Gnothi.Desktop` y `Gnothi.Contract`, y el esquema
> que se describe acá **no es una propuesta: es el `record` de C# que lo lee y lo valida**
> (`Gnothi.Host/Manifest.cs`).
>
> Y una reserva que importa: **el Host se está diseñando en paralelo con los módulos.** El propio
> código dice de sí mismo que hay cosas abiertas —*"the general case is noted as pending"*, *"still
> open in the team's set"*—. Este capítulo describe lo que hay; §8 dice qué está abierto.

---

## 1. Qué es un manifiesto, y qué no

La mejor definición no está en el Host: está en un campo del propio manifiesto de `Cargas`, llamado
`"//que-NO-va-aca"`.

> **Nada que el Host pueda enumerar solo.**
>
> `IActorIntrospection` ya le da símbolos, clases, acciones en DSL canónico, pools de parámetros,
> reacciones y entradas del journal, **sin cooperación del módulo**. El manifiesto es lo que **no se
> deduce del DLL**: intención, política y contexto.

Esa prueba —*¿lo puede saber el Host sin el módulo?*— es lo que evita que el manifiesto se convierta
en un cajón, que es el destino natural de todo archivo de configuración.

> Y vale la pena notar de dónde sale: la escribió **el lado del módulo**, no el Host. Los dos lados
> están en desacuerdo sobre el esquema (§8) y de acuerdo sobre el criterio.

**No lo confundas con `manifest.webmanifest`.** La advertencia también es de `Cargas`, y por algo la
escribieron: ése es el estándar PWA, vive en `App/<N>.Wasm/wwwroot/` y **lo lee el browser** al
instalar la app. El manifiesto del módulo **lo lee el Host**. Dos archivos de nombre casi igual, dos
lectores distintos.

---

## 2. El esquema, tal como el Host lo lee

`Gnothi.Host/Manifest.cs`. Las llaves van **en inglés** — es lo que el deserializador espera.

```jsonc
{
  "manifest": "1.0",
  "module": "examenes",
  "title": "My lab tests",
  "institution": "Clinical laboratory (POC mock)",

  "substrate": { "range": ">=3.0.0 <4.0.0" },

  "capabilities": { "network": false, "folder": "own",
                    "speaker": false, "microphone": false },

  "tools": [
    { "name": "…", "title": "…", "description": "…", "readOnly": true,
      "input": [ { "name": "…", "type": "string", "description": "…" } ] }
  ],

  "view": "…",            // el NOMBRE de un archivo, no la vista
  "load": { "entry": "dominio/Examenes.App.dll" }   // sólo módulos cargo
}
```

Lo único obligatorio de verdad: **`module`**. Si falta, `Manifest.Read` lanza *"the manifest does not
declare 'module'"*.

### Cuatro campos que cargan una decisión de diseño

**`substrate` es un rango, no una versión.**

> *"Si un módulo pinea una versión exacta, **cada actualización del Host huerfana a todos los módulos
> instalados**."*

**`capabilities` vale porque es contrastable.** No es informativo:

> *"Un módulo de exámenes que pida la red no necesita análisis, necesita un aviso. Un módulo que
> declara `network: false` y abre un socket es un módulo que **miente**, y eso es detectable."*

En el POC el Host **sólo avisa** —`Capabilities.Excesses()` enumera lo que el módulo pidió de más, y
`folder` cuenta como exceso apenas deja de ser `"own"`—; hacerlo **cumplir** necesita el sandbox de la
etapa 2.

**`readOnly` viene en `true` por omisión**, y ésa es la postura correcta: un módulo que escribe tiene
que **decir** que escribe. Si dejás el campo afuera, estás declarando sólo lectura, y el capítulo 6
§6.6 dice qué te obliga a cumplir eso: nunca un `PerformCommand`, y nunca la semilla.

**`load.entry` dice qué DLL soy**, y sólo lo llevan los módulos cargo (§6).

> *"«Qué DLL soy» es **identidad**, declarada por la institución — no algo que el Host conceda."*

---

## 3. Por qué las tools se copian y la vista se apunta

Parece una inconsistencia y es la misma razón aplicada dos veces.

**Las tools se copian al manifiesto**, en vez de pedírselas al módulo:

> *"Leer un json cuesta microsegundos y **no despierta nada**. El Host tiene que poder armar el
> catálogo de veinte módulos **sin instanciar veinte runtimes**. El módulo arranca sólo cuando
> alguien llama una de sus tools."*

**La vista se apunta**, y es el nombre de **un json** que viaja adentro del módulo:

> *"…describe la pantalla que la institución diseñó para **la PERSONA — no para el asistente**. El
> módulo describe; **el Host renderiza**. Es un nombre de archivo, y no la vista misma, porque el
> catálogo se arma **sólo con manifiestos** y no debe pagar por parsear cada pantalla por adelantado."*

Así que la regla no es *"no copies"* sino:

> **Lo que el catálogo necesita para existir, se declara. Lo demás, se apunta.**

Dos cosas que se leen al pasar y no son menores. **Un módulo sin vista es legítimo**: contesta
consultas y no tiene pantalla propia. Y **la vista es declarativa** — el módulo no embarca una
pantalla, embarca su descripción, y quien la dibuja es el Host.

---

## 4. Y por eso se puede verificar

Copiar una lista invita a que derive. La respuesta no es dejar de copiarla:

> *"Y **porque es una declaración, se puede verificar**: más adelante el Host le pide al módulo su
> `tools/list` real y compara. **Lo declarado contra lo que es.**"*

Y no quedó como intención: el Host trae la tool. `gnothi_verify_module` le pide al módulo su
`tools/list` real, lo cruza contra el manifiesto y devuelve `undeclared` —*"el módulo expone tools que
el manifiesto no declara"*—, `declaredButMissing` —*"el manifiesto declara tools que el módulo no
tiene"*— y un `matches` que sólo es verdadero cuando las dos listas están vacías.

> **Y esto es un observable de la Metodología, no sólo una función del Host.** *Todo lo que el
> manifiesto declara y el código también tiene, coincide.* Vale correrlo aunque el Host no esté
> instalado: un manifiesto que nadie lee y que deriva en silencio es peor que no tenerlo.

---

## 5. Las dos autoridades

El manifiesto **no viaja solo**. Su pareja es `installation.json`, y están separados a propósito:

> *"Son **dos autoridades**. La institución declara quién es y qué ofrece; **el Host** decide cómo se
> corre y qué carpeta le concede. Si la cáscara viniera en el manifiesto, el módulo elegiría cómo se
> lo ejecuta."*

| | Lo escribe | Declara |
|---|---|---|
| **`manifest.json`** | La institución | Quién es, qué **pide**, qué ofrece |
| **`installation.json`** | Quien opera el Host | Cómo se **corre**, qué se le **concede**, qué se le **apaga** |

```jsonc
{
  "shell": { "type": "in-process", "command": "…", "arguments": [ "…" ] },
  "grantedFolder": "C:/Gnothi/examenes",
  "enabled": true,
  "disabledTools": []
}
```

**`shell` es la pieza que absorbe la evolución.** Es el único bloque que cambia cuando cambia la forma
de correr: *"la etapa 2 cambia **sólo este bloque** por un runtime wasm con `--dir`, y **ni el
manifiesto ni el módulo se enteran**"*. Ya se movió una vez —la nota al lado dice *"proceso hijo +
stdio"* y el valor de hoy es `in-process`—, y ése es exactamente el comportamiento que se buscaba: se
movió el `shell` y nada más.

**`grantedFolder` es la única carpeta que el módulo puede tocar**, y no la elige él: *"conceder es un
acto de quien concede"*. **La carpeta concedida se pasa como último argumento**, no va adentro de
`arguments`.

> **La trampa que te va a pasar a vos.** Tiene que ser **la misma carpeta que elegiste en la app al
> instalar**. Si no coinciden, el módulo reporta `hayDatos=false` y el asistente te pide que abras la
> app — un síntoma que **no se parece en nada** a su causa. Es la primera hipótesis a revisar cuando
> un módulo recién instalado "no tiene datos".

Y las rutas van **con barra normal**: Windows las acepta igual y así se evita el doble escape de JSON,
*"que ya rompió este archivo una vez"*.

Y `shell.arguments[0]` **puede ser relativa**: si no es absoluta, el Host la ancla a **la carpeta del
propio `installation.json`**. Es el argumento que nombra código —el dll que se carga, la carpeta web
del webview— y es el único que se ancla; el resto son flags del shell, y la carpeta concedida nunca
viaja ahí. Las dos formas conviven: el **instalador** escribe la ruta absoluta —el dll queda dentro de
la carpeta del módulo, así que absoluta y relativa nombran el mismo archivo—, y el **banco versionado
en `Modulos/`** escribe `../../Compras/App/Compras.Mcp/bin/Debug/net9.0/Compras.Mcp.dll`, que arranca
en cualquier máquina que haya compilado el módulo. Antes traía el disco de quien armó el POC, y en
cualquier otra máquina —y en CI— ningún módulo arrancaba y la suite de conformance no verificaba nada.

**`enabled` y `disabledTools` son el interruptor**, y son del Host. El segundo es el que de verdad
hace falta — *"el corte fino: la tendencia sí, la velocidad no"*. El nombre va **tal como aparece en
el manifiesto, sin el prefijo del módulo**. Y tiene una propiedad de diseño que conviene conocer:

> Apagar es *"dejar las tools de este módulo fuera de la lista que Gnothi le entrega al asistente"*, y
> **el módulo NO se entera**: no puede distinguir *"me apagaron"* de *"nadie preguntó"*.

Ése es el mecanismo de *"la IA ve lo que el dueño permita"* del capítulo 0, hecho concreto y con
granularidad de tool.

---

## 6. Dos formas de módulo, y cambian el nodo que escribís

Esto es lo que más pesa de todo el capítulo, porque decide cómo se escribe la superficie.

| | **stdio** | **cargo** |
|---|---|---|
| Qué embarca | Su propio servidor MCP | **Dos DLL chicos**: dominio + superficie |
| El motor | Lo trae el módulo | **Lo presta el Host** |
| Cómo se declara | `[McpServerTool]` del SDK de MCP | `[ModuleSurface]` y `[Query]` de `Gnothi.Contract` |
| El SDK de MCP (~1,3 MB) | Sí | **No** |
| En el manifiesto | sin `load` | con `load.entry` |

`Gnothi.Contract` son **dos atributos sin comportamiento** y sin dependencias:

> *"El Host los descubre por reflexión —leyendo el atributo **por nombre**, igual que ya hace con los
> del MCP— así que el módulo **no carga el SDK**. Eso es lo que hace que el paquete cargo sean dos DLL
> chicos y no un megabyte."*

Una superficie cargo se ve así, y es notablemente más corta que un servidor MCP:

```csharp
[ModuleSurface]
public sealed class ExamenesSurface
{
    // El Host la construye pasándole la carpeta concedida. El módulo NUNCA abre el
    // journal por su cuenta ni elige la carpeta: es la convención del InProcessShell.
    public ExamenesSurface(string grantedFolder) { … }

    [Query("resumen")]
    public object Resumen() { … }
}
```

**Lo que NO cambia entre las dos formas** es la disciplina de sólo lectura, y son las mismas dos del
capítulo 6 §6.6: nunca un `PerformCommand`, y **nunca la semilla** — actor gemelo, mismo nombre, mismo
dominio, `OnHydrated` vacío. Si no, cada pregunta de la IA escribiría en el journal de la persona.

> **Cuál escribir hoy, y por qué la pregunta no la cierro yo.** En la rama del Host los tres negocios
> —`Compras`, `Examenes`, `Facturas`— **ya tienen su clase `*Surface`** con los atributos de cargo, y
> los tres `installation.json` dicen `"type": "in-process"`. Pero **ningún manifiesto declara `load`**,
> que es lo que un módulo cargo necesita. O sea: la forma cargo está construida y todavía no está
> declarada. En `master` esas clases **no existen**; ahí los negocios son sólo stdio.
>
> Consecuencia práctica: si arrancás un módulo hoy, **preguntá cuál se espera antes de escribir un
> servidor MCP completo**. En cargo ese nodo es una clase con dos atributos.

---

## 7. Qué se decide cuándo

Aunque el manifiesto se escribe al publicar, la mitad de sus campos son decisiones que salieron mucho
antes:

| Campo | Se decidió en |
|---|---|
| `capabilities` | Al modelar: si el dominio no hace red, `network: false` sale solo |
| `tools` | Paso 7 — y su lista es la del capítulo 6: sólo lo que ninguna pantalla contesta |
| `view` | Paso 7 |
| `substrate.range` | Al elegir la versión del motor |
| `institution`, `title` | En el punto de partida |

Escribir el manifiesto al final está bien; **descubrir sus respuestas al final, no**. Si al llegar
acá no sabés qué poner en `capabilities`, es que algo del modelado quedó sin decidir.

---

## 8. Lo que está abierto, y no lo cierra esta guía

- **`manifiesto.json` y `manifest.json`: ¿el mismo archivo, o dos?** Hay tres esquemas vivos, y ni
  siquiera los dos del lado del módulo coinciden entre sí:

  | Dónde | Cómo se identifica | Idioma |
  |---|---|---|
  | `Modulos/<m>/manifest.json` (rama del Host) | `"module": "compras"` | inglés |
  | `Compras/manifiesto.json` (`master`) | `"modulo": "compras"` | español |
  | `Cargas/manifiesto.json` (`master`) | `"id": "cr.faro.cargas"` + `"nombre"` | español |

  **La divergencia no es descuido: es que el lector no existía.** `Cargas` lo dice de sí mismo —*"NO
  HAY HOST TODAVÍA. Nada en el repo lee este archivo, y por un rato es una **declaración**, no una
  integración"*—. Se escribió igual, y con una razón que sigue siendo buena: es *"el lugar donde tiene
  que vivir lo que hoy está disperso"* —las instrucciones enterradas en `Cargas.Mcp/Program.cs`, la
  ruta de URL2 dentro del Portador, el catálogo de permisos de la IA—.

  Ahora el lector existe, y exige `"module"`. Así que ninguno de los dos del lado del módulo cargaría.
  **Cuál gana no lo decide esta guía**, y hay dos lecturas razonables: que sean el mismo artefacto y
  el español haya quedado por escribirse en paralelo, o que el rico sea un **documento de diseño** del
  que se deriva el que se instala. Lo que sí conviene no perder es lo que el rico tiene y el del Host
  no: la **procedencia** de cada aporte —`observado` / `estimado` / `confirmado`, con la regla de que
  *"un estimado jamás alimenta un cálculo que se presente como medición"*—, `cruzaPor`,
  `journal.reconstruible`, y **dos versiones separadas, dominio y repertorio**, para que actualizar
  una tasa no obligue a re-verificar el dominio.

  > **`journal.reconstruible` merece defensa aparte**, porque es *"el campo que no se deduce del
  > código"* — y porque la Metodología llegó a él sola. `Cargas` fue el primero en ponerlo en `false`:
  > la mitad de cada carga la aporta la persona y **no está en ningún otro lado**, así que borrar la
  > carpeta pierde su trabajo. El ejercicio de Presupuesto chocó con lo mismo sin conocer el campo:
  > *"el journal deja de ser reconstruible desde URL2… el respaldo deja de ser opcional"*. Que dos
  > dominios sin relación lo descubran por separado es la mejor evidencia de que sostiene peso.
- **`input` sólo cubre parámetros simples.** El POC llega hasta *"tools sin parámetros y tools con
  parámetros de texto, que es lo que el módulo de facturas necesita"*. El caso general —un schema de
  entrada arbitrario— *"pide manejar `tools/call` a mano en vez de sintetizar un delegado, y queda
  anotado como pendiente"*.
  > Para la Metodología eso significa que hoy **un `enum` no se puede declarar**: los valores válidos
  > terminan repetidos en la prosa de la descripción, que es justo lo que el capítulo 6 §6.3 pide
  > evitar. Si tu tool tiene un parámetro cerrado —un `Periodo`, por ejemplo—, sabé que lo estás
  > sosteniendo con texto hasta que ese pendiente se cierre.
- **Las capacidades hoy sólo avisan.** Hacerlas cumplir necesita el sandbox de la etapa 2.

---

## 9. Antes de dar el manifiesto por terminado

- [ ] ¿Declara `module`? Es lo único sin lo cual no carga
- [ ] ¿`substrate` es un **rango** y no una versión exacta?
- [ ] ¿Las `capabilities` dicen la verdad? Recordá que son **contrastables**
- [ ] ¿La lista de `tools` **coincide con la que el módulo expone**? (§4)
- [ ] ¿Las descripciones están escritas como **la pregunta de la persona**, y no como documentación?
      Viajan tal cual hasta la IA — capítulo 6 §6.3
- [ ] ¿`view` es un **nombre de archivo**, no la vista?
- [ ] Si es un módulo cargo, ¿declara `load.entry`?
- [ ] ¿Hay algún campo acá que **el Host podría deducir solo**? Si lo hay, sobra
