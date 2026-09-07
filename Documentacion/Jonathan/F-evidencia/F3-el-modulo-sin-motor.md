# F3 · El módulo sin motor ni runtime — medido

**Corte: 2026-08-26.** Todo lo de acá está corrido en esta máquina y tiene el comando que
lo reproduce. Lo que no está medido dice que no lo está.

Es la migración de `R8` y `R9` del set del arquitecto: **un solo Host que lleva el sustrato,
módulos puros que no lo traen.** Y del descarte de `R4`: entre el Host y el módulo no hay
protocolo.

---

## El resultado en una línea

El entregable de la institución pasó de **6.7 MB y 66 archivos** a **15 041 bytes y 3
archivos**, y contesta exactamente lo mismo.

```
total 27 991.50  ·  4 facturas  ·  leído hasta el evento 104
```

Idéntico a la corrida anterior, con el módulo cargado en proceso en vez de invocado por
MCP sobre un proceso hijo.

---

## Lo que pesaba y lo que pesa

### Antes · `Facturas.Mcp` self-contained

66 archivos, 6.7 MB. Desglosado por a quién le tocaba:

| | |
|---|---|
| el módulo de verdad — dominio + superficie | **31 KB** |
| el protocolo — `ModelContextProtocol.Core` 1.3 MB + `Extensions.AI` 660 KB + DI 92 KB | 2.1 MB |
| el motor — `Puppeteer` 800 KB + `Choreography` 172 KB | 972 KB |
| la BCL — `System.Text.Json` 636 KB, `DiagnosticSource` 200 KB, y el resto | ~3.6 MB |

**El protocolo pesaba más que el motor.** Sacar MCP del módulo ahorró más que sacar
Puppeteer.

### Ahora · el paquete que la institución publica

```
facturas-1.0.gmod                    15 041 B
├── manifiesto.json                   2 198 B
├── dominio/Facturas.App.dll         21 504 B      la superficie
└── dominio/Facturas.Dominio.dll     10 240 B      el dominio, cero dependencias
```

Sin motor, sin runtime, sin librerías de protocolo, sin ejecutable.

> El arquitecto dijo en la reunión: *«un programita puede ser 30k»*. El dominio más su superficie
> miden **31 744 bytes**.

```bash
.\Facturas\Modulo\empacar.ps1
```

---

## El mecanismo, y es el que él describió

> *«Nosotros le vamos a decir una forma de que la compile pero que **no tiene que poner el
> Puppeteer en su aplicación**, porque aunque se lo ponga, lo que haría es que su
> aplicación mide un mega y no tira. El celular no lo va a usar, **Gnothi va a usar el que
> tiene él**.»*

Se expresa en dos líneas de csproj:

```xml
<PackageReference Include="Ncubo.Puppeteer" Version="$(VersionDelMotor)" ExcludeAssets="runtime" />
<ProjectReference Include="...\Puppets.Portador.csproj" ExcludeAssets="runtime" Private="false" />
```

`ExcludeAssets="runtime"` compila contra el sustrato y no copia un byte. Para
`ProjectReference` hace falta además `Private="false"`: sin él el DLL se copia igual —
medido, es la diferencia entre 140 KB y 32 KB de salida.

**Y la contracara, que es el diseño y no una molestia:** el motor ya no llega
transitivamente, así que **cada hospedaje declara el sustrato que presta**.
`Facturas.Wasm`, `Facturas.Consola` y `Gnothi.Host` ahora lo referencian ellos.

### Y en ejecución, el préstamo son cinco líneas

```csharp
protected override Assembly? Load(AssemblyName solicitado)
{
    string candidato = Path.Combine(carpeta, solicitado.Name + ".dll");
    return File.Exists(candidato) ? LoadFromAssemblyPath(candidato) : null;
}
```

Devolver `null` hace que el runtime caiga al contexto por defecto — o sea, **a los
assemblies del Host**. Puppeteer, Choreography, Portador y la BCL entera se resuelven a la
copia de Gnothi.

A propósito **no** se usa `AssemblyDependencyResolver`, que es el patrón típico de plugins:
ese busca las dependencias *en* la carpeta del plugin, que es exactamente lo contrario. El
módulo no trae dependencias — las pide prestadas.

---

## Lo que se ganó además del tamaño

### 1 · La verificación se volvió gratis

Antes, contrastar lo declarado contra lo real exigía **arrancar el proceso del módulo** y
pedirle `tools/list`. Ahora es reflexión sobre un DLL: no arranca nada, no hidrata nada, y
no toca la carpeta de la persona.

```json
{ "modulo": "facturas", "coincide": true,
  "declaradas": ["total_de_facturas"], "reales": ["total_de_facturas"],
  "noDeclaradas": [], "declaradasQueNoExisten": [] }
```

Es la mitad «escaneo» de la función 3 del catálogo — *«o los escaneamos… o los da en un
archivo»* — y ahora las dos mitades cuestan lo mismo.

### 2 · El solo-lectura lo garantiza el Host, no la buena conducta del módulo

`ActorDeSoloLectura` con `OnHydrated` vacío ahora vive **en el Host**. Antes vivía en el
módulo, y era el módulo el que se acordaba de no sembrar.

Un módulo que quisiera escribir en cada arranque ya no tiene dónde: **no es el que
instancia el actor.**

**Verificado con tres consultas seguidas:**

```
antes  y  despues   \facturas\journal\journal_000001.bin  1742 B  0886AD4E6E127591714F8828DF8FB370
antes  y  despues   \facturas\index\index.bin               30 B  FCC7A8871D384AF67838453F41E77143
antes  y  despues   \facturas\meta.bin                      48 B  2BE540DD6017FA0B7683A0C0ADBF4417
```

Byte-idéntico. El Host no le escribe el journal a la persona.

### 3 · El conformance se cae de verdad

El empacador corre las verificaciones de la Fase 6 **antes** de producir archivo. Probado
sembrando a mano lo que no debe estar:

```
CONFORMANCE FALLIDO - no se produce paquete
 x trae sustrato adentro: Puppeteer.dll  (revisa ExcludeAssets/Private en Facturas.App.csproj)
 x trae algo que no le toca: System.Text.Json.dll
```

Chequea: sustrato adentro, BCL, ejecutables, **journal de muestra** (Fase 6 punto 6), que
el manifiesto declare su carga y que el build la haya producido, y que no declare
capacidades de más.

---

## El entregable del Host

| | |
|---|---|
| `Gnothi.Host.exe` | **71.4 MB**, self-contained, single-file, `win-x64` |
| `gnothi.mcpb` | **31.4 MB** — lo genera el propio ejecutable |

```bash
Gnothi.Host.exe --publicar-mcpb
```

**Gnothi se empaqueta a sí mismo**, y eso cierra una clase entera de bugs: el nombre del
binario, la versión y la plataforma no pueden desincronizarse de lo que el manifiesto
declara, porque salen del binario que se está ejecutando.

Y el `.mcpb` **no es el entregable de Gnothi**: es cómo un asistente de escritorio aprende
a alcanzarlo. En los términos de `01`, pertenece a la capa borrable.

El escenario completo corre contra el ejecutable publicado —
**desde la carpeta de descargas** — no solo contra el build de desarrollo.

> **Cómo baja hoy.** El binario autocontenido y el `.mcpb` **siguen existiendo**, pero lo que
> la persona baja **no** es el exe suelto: es el **MSI de Gnothi**, que trae el Host y la
> ventana `Gnothi.Desktop`. El detalle de los instaladores está en `C7`.

### Reproducido de cero, por otra persona

El ciclo entero se corrió una segunda vez desde estado limpio: journal vacío, cero módulos
instalados, el `.gmod` bajado del navegador desde URL1, instalado con `--instalar-modulo`.

| | |
|---|---|
| semilla del proveedor | eventos 101, 102, 103 |
| suma esperada | 5 571.50 + 4 750.00 + 9 000.00 |
| **lo que contestó el asistente** | **19 321.50 · 3 facturas · evento 103** |

Las dos corridas —27 991.50 con el journal previo y 19 321.50 desde cero— son la misma
evidencia por dos caminos. La segunda es la fuerte: **nada quedaba de la anterior.**

---

## URL1 dejó de servir una aplicación

Antes servía la app Blazor entera. Ahora es la página del supermercado con dos descargas:

```
HTTP/1.1 200 OK
Content-Type: application/octet-stream
Content-Length: 15041
Content-Disposition: attachment
```

```bash
cd "C:/Users/jon32/Documents/MCP Hola Mundo/Facturas" && docker compose up -d
```

Y la página dice lo que el arquitecto describió en la reunión: *«ellos lo que están interesados es
en que descargue la aplicación del módulo de ellos»*. El módulo primero; Gnothi como
dependencia.

---

## Lo que NO se resolvió, dicho como tal

### Espacios en la ruta del ejecutable — cerrado por construcción

En Windows el transporte stdio lanza el comando **a través de `cmd.exe`**. Si la ruta del
ejecutable tiene un espacio, el arranque muere — con dos síntomas distintos, que es lo que
lo vuelve confuso:

| | |
|---|---|
| sin comillas | *«'C:/Users/.../MCP' no se reconoce como un comando interno o externo»* |
| con comillas | *«el nombre de archivo, el nombre de directorio o la sintaxis de la etiqueta del volumen no son correctos»* |

**Medido: entrecomillar no lo arregla**, ni con los separadores normalizados. `cmd.exe`
reordena las comillas de su propia línea y necesita la forma doble, que la arma el SDK.

**No es una rareza de esta máquina.** Gnothi se iba a instalar bajo `Program Files` o bajo
un perfil como `C:\Users\Ana Maria\`, y las dos rutas tienen espacio. Las salidas eran:
instalar en una ruta sin espacios, registrar la ruta corta 8.3, o no pasar por `cmd`.

**Cerrado.** El MSI per-usuario instala a `%LocalAppData%\Programs\Gnothi` —**sin
espacios**—, que es la primera de esas salidas y la que este mismo doc ya recomendaba. El
bloqueante queda cerrado **por construcción**: la ruta de instalación no puede tener espacio.
Ver `C7` (los MSI) y `E1` (la trampa de `cmd.exe`).

### El aislamiento sigue en cero, y ahora por otra razón

`R9` lo dice sin vueltas para esta variante: *«ninguno — mismo heap; `AssemblyLoadContext`
**no es** frontera de seguridad»*.

Antes el módulo era un proceso aparte, lo cual daba separación de espacios de direcciones
(no aislamiento de seguridad, pero algo). Ahora corre en el heap del Host. **Un módulo que
revienta se lleva a Gnothi.**

Y el ancla de confianza es la que `R9` marca como la peor de las dos: firma y reputación,
no incapacidad. El aislamiento duro sigue esperando **S1** — si un runtime wasm puede
cargar el DLL de dominio desde su `--dir` y descubrirlo por reflexión.

### La ventana de los dos escritores se encogió pero no se cerró

Durante la consulta, el Host tiene la carpeta del journal abierta igual que antes. Lo que
desapareció es el arranque de un proceso: la ventana pasó de *«los milisegundos de levantar
un ejecutable y hacer replay»* a *«los milisegundos de hidratar»*.

Si la persona tiene la app abierta sobre la misma carpeta, el riesgo del `InvalidStateError`
de `E3` sigue existiendo. Se cierra de verdad cuando haya un solo dueño del filesystem.

### El empaquetado se invirtió · de MSIX a MSI

**El clic de instalar del `.mcpb` no se habilita.** Antes estaba anotado como «sin
confirmar»; ya se probó dos veces con el bundle que genera el propio ejecutable. Se descarta
ese camino.

**El registro que sí funciona es más simple:** apuntar el ejecutable directo en el
`claude_desktop_config.json`.

```json
"gnothi": {
  "command": "C:\\Users\\jon32\\Downloads\\Gnothi.Host.exe",
  "args": ["...\\GnothiHolaMundo\\Modulos"]
}
```

Verificado corriendo: el chat de la app contesta el total leyendo el módulo de 14.7 KB.

**Y el hallazgo vale más que el problema:** el `.mcpb` se pudo descartar **sin perder nada**.
Eso es exactamente lo que `01` predice de esa capa — *«¿quién le habla al Host desde afuera?
ahí vive la capa borrable»*. La costura aguantó: cambiar el mecanismo de registro no tocó
una línea del despacho, del catálogo ni del módulo.

**Y el empaque se movió en la dirección contraria a la que estaba anotada: de MSIX a MSI.**
El paquete dejó de ser **MSIX** porque su sandbox **prohíbe «registrar la máquina»** — justo
lo que Gnothi necesita, dejar la marca de descubrimiento en `HKCU\Software\Ncubo\Gnothi`
(`InstallPath`, `ModulesPath`, `DesktopExe`, `HostExe`, `Version`). Se entrega como **MSI
per-usuario, sin admin**. Y el registro del MCP en Claude **dejó de colgar del paquete**: es
un paso aparte (`HostInstaller` / primer arranque), **sin tocar `HKLM` ni el arranque de
Windows**.

El detalle de los dos MSI, la marca y las caras de escritorio está en `C7`; las trampas de
`msiexec` y WiX, en `E1`.

Se conserva `--publicar-mcpb` porque demuestra que el Host se empaqueta a sí mismo sin poder
desincronizar su manifiesto del binario. El día que la app lo acepte, el camino está hecho.

---

## Lo que quedó vivo a propósito

`CascaraStdio` **no se borró**. El mismo módulo se puede correr con el hospedaje viejo
cambiando una línea de `instalacion.json`:

```json
"cascara": { "tipo": "en-proceso", "carpetaDeCarga": "dominio" }
```

Eso es lo que vuelve demostrable *«un solo artefacto, dos hospedajes»* en vez de
afirmable — con el asterisco de que el hospedaje `stdio` necesita el módulo viejo, el que
sí traía su servidor MCP.

---

## Un hueco que la migración destapó: quién escribe

Al sacar la app de URL1, el demo se quedó **sin camino de escritura**. El módulo es
solo-lectura por construcción, así que nada trae facturas nuevas del proveedor.

Eso no es un descuido de la migración: es la **función 11 del catálogo** —*«el Host es el
único egreso, y hace el pull por cuenta del módulo»*— que es la Fase 3 del plan y no está
construida.

Mientras no exista, la app Blazor sigue viva **en otro puerto y marcada como andamio**:

```bash
docker compose up -d
```

| | | |
|---|---|---|
| `:8080` | **URL1** | la página del supermercado, con el módulo y Gnothi para bajar |
| `:8081` | **URL2** | el endpoint de eventos |
| `:8082` | *el andamio* | la app Blazor. Lo único que hoy escribe el journal |

Está deliberadamente **fuera** de URL1. Ponerlo ahí contaría una historia falsa: URL1 es la
página de la institución y lo que entrega es el módulo.

Y de paso deja el pendiente medible: cuando el Host haga el pull, el puerto 8082
desaparece. **Que siga existiendo es el indicador de que la Fase 3 falta.**
