# D1 · Las cuatro superficies

**Cada superficie de asistente tiene su propio mecanismo de registro, y son incompatibles entre sí.** Eso no
es un defecto: es exactamente lo que la arquitectura anticipa con las dos puertas — el Host queda igual y **lo
único que cambia es el eslabón de arriba**.

Pero son **cuatro**, no tres, y la cuarta es de otra especie: no se registra en ninguna parte porque **es el
Host mismo poniéndose una cara**. Rompe a propósito la simetría «tres superficies, tres mecanismos de registro
MCP» — y, al romperla, la refuerza.

Las tres primeras se probaron y dieron el mismo dato; la cuarta da ese mismo dato por construcción —es el Host
llamándose a sí mismo.

---

## Resumen

| superficie | dónde se registra | rutas |
|---|---|---|
| **Claude Code** | `.mcp.json` del proyecto | **relativas** al proyecto |
| **El chat de la app** | `claude_desktop_config.json` del paquete | **absolutas** |
| **Cliente propio** | argumentos de línea de comando | absolutas |
| **Ventana de escritorio** | **no se registra: es el Host mismo** | — |

---

## 1 · Claude Code

`.mcp.json` en la raíz del proyecto:

```json
{
  "mcpServers": {
    "gnothi": {
      "command": "dotnet",
      "args": [
        "GnothiHolaMundo/Gnothi.Host/bin/Debug/net9.0/Gnothi.Host.dll",
        "GnothiHolaMundo/Modulos"
      ]
    }
  }
}
```

**Una sola entrada.** Antes había un servidor MCP **por negocio** y el asistente les hablaba directo. Ahora
le habla al Host, y el Host agrega. Esa es la diferencia entera.

**`dotnet <dll>`, nunca `dotnet run`** — `run` escribe mensajes propios a stdout, y con stdio eso rompe el
protocolo.

Las rutas relativas funcionan acá porque el directorio del proyecto es el cwd.

## 2 · El chat de la aplicación de escritorio

### La ruta real, que cuesta encontrar

```
C:\Users\<usuario>\AppData\Local\Packages\Claude_<hash>\LocalCache\Roaming\Claude\claude_desktop_config.json
```

**No** `%APPDATA%\Claude\claude_desktop_config.json`.

Es una aplicación de la Microsoft Store, así que corre con **virtualización de sistema de archivos**: su
`%APPDATA%` redirige a `LocalCache\Roaming`. Los dos caminos apuntan al mismo archivo, pero el que la
aplicación abre —y el que hay que usar— es el del paquete.

**La forma segura de llegar:** en la aplicación, **Ajustes → Desarrollador → Servidores MCP locales →
«Editar configuración»**. Ese botón abre el archivo correcto sin que haya que adivinar la ruta.

### El bloque

Va **dentro del objeto raíz**, al mismo nivel que las demás claves:

```json
{
  "mcpServers": {
    "gnothi": {
      "command": "dotnet",
      "args": [
        "C:/Users/jon32/Documents/MCP Hola Mundo/GnothiHolaMundo/Gnothi.Host/bin/Debug/net9.0/Gnothi.Host.dll",
        "C:/Users/jon32/Documents/MCP Hola Mundo/GnothiHolaMundo/Modulos"
      ]
    }
  },
  "coworkUserFilesPath": "...",
  "preferences": { ... }
}
```

Tres cosas que hacen fallar esto:

- **Rutas absolutas, obligatorio.** La aplicación no tiene proyecto contra el cual resolver relativas.
- **Barras normales, no `\`.** Windows las acepta y evitás el escapado doble de JSON.
- **La coma** después de la llave que cierra `mcpServers`. Si el JSON queda inválido, **la aplicación lo
  descarta entero sin decir nada.**

Y el error de estructura más fácil: **cerrar una sola llave.** Hay dos abiertas —`mcpServers` y el nombre
del servidor— y hay que cerrar las dos:

```json
      ]        ← cierra args
    }          ← cierra el servidor
  },           ← cierra mcpServers, con coma
```

### Una versión vieja lo descartaba

Medido: una build anterior de la aplicación **reescribía el archivo y borraba la clave `mcpServers`**,
porque su serializador solo escribía las claves que conocía y esa página de ajustes no existía todavía.

La build que la tiene lo preserva — verificado sobreviviendo a un reinicio completo.

**Si el bloque desaparece al reiniciar, es eso**, y el camino alternativo es el `.mcpb` (ver `C3`).

### Y hay que reiniciar

Los servidores MCP se registran **al arrancar la aplicación**. Editar el archivo con la aplicación abierta
no tiene efecto — y además la aplicación puede pisarlo al cerrarse. **La secuencia segura es editar por el
botón, guardar, y reiniciar.**

## 3 · El cliente propio

Sin configuración: los argumentos van en la línea de comando.

```bash
dotnet <ruta>/Gnothi.Prueba.dll <ruta>/Gnothi.Host.dll <ruta>/Modulos
```

Corre igual en Windows y dentro de un contenedor, y es el que muestra el escenario paso a paso con la
explicación de cada respuesta. Existe en .NET y no en PowerShell precisamente para que sea **el mismo
cliente en los dos lados**.

Hay también un `probar.ps1` que hace lo mismo hablando JSON-RPC **a mano**, sin librería de MCP — sirve para
ver el cable pelado.

## 4 · La ventana de escritorio

Las tres anteriores son clientes MCP: cada una levanta el Host como proceso aparte y le habla por stdio. **La
cuarta no le habla al Host — es el Host.**

`Gnothi.Desktop` es una ventana **Blazor Hybrid** (WinForms + `BlazorWebView`, `net9.0-windows`, `WinExe`) con
un **`ProjectReference` al Host** —a su superficie pública, nada más—. Muestra tarjetas y corre queries **en
nativo**, llamando al Dispatcher **directo**: sin wasm, sin puerto, sin http, sin stdio. Por eso su fila no
tiene «dónde se registra»: no hay nada que registrar cuando el asistente y el Host son el mismo ejecutable.

Ese hueco en la tabla **es el punto.** Las tres primeras prueban que el eslabón de arriba es intercambiable;
la cuarta lo lleva al extremo y muestra que el eslabón puede **desaparecer** — el Host poniéndose una cara, no
un asistente hablándole desde afuera.

> ⚠ **Está EXCLUIDA del `.slnx`.** El target `net9.0-windows` rompe cualquier build de CI o de Linux, así que
> no entra en la solución que se compila en todos lados; se arma aparte, en Windows.

El detalle de la ventana —cómo se empaqueta, cómo convive con el shell del comercio, los MSI que la instalan—
vive en `C7`.

---

## Lo que las cuatro juntas demuestran

Cuatro superficies, tres mecanismos de registro y una cuarta que no se registra, un solo Host, un solo dato.
Para el módulo de facturas ese dato es **27.991,50, 4 facturas, evento 104** — números válidos para *ese*
módulo, no un total del Host: con el segundo módulo instalado —compras— el «un solo dato» ya abarca dos
negocios a la vez.

**Ninguna de las superficies sabe que hay un módulo detrás.** Eso es la cuarta autoridad funcionando de
verdad: el asistente es intercambiable porque no tiene nada propio en el medio — y la ventana de escritorio lo
lleva al límite, porque ahí ni siquiera hay un asistente, solo el Host con cara.

## La regla operativa mientras el módulo sea un proceso aparte

**No consultes desde dos superficies a la vez sobre la misma carpeta.** Cada una levanta su propio Host, y
cada Host su propio módulo — dos escritores sobre un journal. Con el desalojo la ventana es de
milisegundos, pero preguntá en una o en la otra. Ver `E3`.

## Diagnóstico

La aplicación escribe un log por servidor:

```bash
powershell -Command "Get-ChildItem \"$env:APPDATA\Claude\logs\" -Filter 'mcp*.log' | Select-Object Name, Length, LastWriteTime"
```

`mcp-server-gnothi.log` tiene el stderr del Host — donde salen las líneas de qué módulos encontró y qué
publica. **Si esas aparecen, arrancó bien** y el problema es de otro lado.

Y el log principal de la aplicación es útil para entender qué soporta: ahí se ve un
`LocalMcpServerManager`, un `localMcpBridge` que reporta cuántos servidores stdio tiene conectados, y el
reparto entre servidores remotos y locales.
