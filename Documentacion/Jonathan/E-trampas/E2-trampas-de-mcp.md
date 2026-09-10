# E2 · Trampas de MCP y stdio

---

## 1 · Con stdio, **stdout ES el protocolo**

**Síntoma:** el cliente se desincroniza sin un error claro. No dice «JSON inválido»: se pierde.

**Causa:** el motor imprime su progreso de replay con `Console.Write("1%2%3%…")`, **sin salto de línea**. Y
eso es **peor** que escribir una línea basura: como no cierra la línea, esos caracteres **se pegan adelante
del siguiente mensaje JSON**. El cliente recibe `1%2%3%{"result":...}`.

**Regla:** `Console.SetOut(Console.Error)` como **primera línea**, antes de tocar cualquier otra cosa.

Es una trampa fea justamente porque en cualquier otro contexto ese `Console.Write` es inofensivo.

## 2 · `dotnet run` escribe a stdout

**Regla:** en cualquier configuración de servidor MCP, **`dotnet <dll>`, nunca `dotnet run`**. `run` escribe
mensajes propios y rompe el protocolo antes de que empiece.

Verificado: `dotnet <dll>` no escribe nada propio a stdout.

## 3 · Cerrar stdin demasiado pronto mata la respuesta

**Síntoma:** «el servidor MCP no contesta nada».

**Causa:** EOF en stdin es la señal de apagado ordenado. Si mandás los mensajes y cerrás enseguida, el hijo
**se apaga antes de responder**.

**Regla:** en un cliente de prueba, dormir antes de cerrar. El servidor respondía; nadie leyó.

## 4 · Los servidores se registran **al arrancar la sesión**

Editar la configuración no tiene efecto en una sesión abierta. Hace falta una nueva.

Y vale también para la configuración de los módulos: **el catálogo se arma al arrancar el Host**, así que
apagar una consulta necesita reiniciar.

El protocolo ya tiene el mecanismo para hacerlo en caliente —el servidor puede declarar que su lista cambia
y empujar una notificación— pero **no está cableado**.

## 5 · Cada superficie de asistente tiene su propio registro

Y son incompatibles. Esto no es un defecto del diseño: es exactamente el eslabón que la arquitectura
declara variable.

| superficie | dónde | rutas |
|---|---|---|
| el CLI del asistente | archivo del proyecto | **relativas** |
| el chat de la app de escritorio | archivo del paquete MSIX | **absolutas** |
| un cliente propio | línea de comando | absolutas |

**La trampa concreta:** copiar la configuración de una superficie a otra. Las rutas relativas funcionan en
una porque el proyecto es el cwd; en la otra no hay proyecto contra el cual resolverlas, y el servidor
arranca y falla sin decir por qué.

**Y hay una cuarta cara que no entra en la tabla** —justamente porque no se registra en ningún lado—: la
ventana `Gnothi.Desktop` habla con el Dispatcher **directo**, sin MCP, sin stdio, sin configuración que
copiar. Es el eslabón variable llevado al extremo: cuando el transporte desaparece, no hay registro que
equivocar. Ver `C7`.

## 6 · Las rutas en JSON, y la coma

Tres formas de romper una configuración de servidor MCP, en orden de frecuencia:

**Backslashes sin escapar.** `C:\Users\...` en JSON hace que `\U` sea un escape inválido. **Usar barras
normales** — Windows las acepta y el problema desaparece.

**La coma después de la llave de cierre.** Si falta, o sobra, **la aplicación descarta el archivo entero sin
decir nada.**

**Cerrar una sola llave.** Hay dos abiertas —el bloque de servidores y el nombre del servidor— y hay que
cerrar las dos:

```json
      ]        ← cierra args
    }          ← cierra el servidor
  },           ← cierra el bloque, con coma
```

Si se cierra una sola, todo lo que sigue queda **adentro** del bloque de servidores. El JSON puede ser
válido y la configuración estar mal.

## 7 · Detalles del SDK que parecen otra cosa

**`StandardErrorLines` es un `Action<string>`, no un contador.** Parece un `int`. Resultó más útil: permite
reenviar el stderr del hijo **con prefijo**, así el ruido de un módulo es rastreable hasta el módulo y nunca
toca stdout.

**`ListToolsAsync` devuelve `IList`, no `IReadOnlyList`.**

**Y el resultado estructurado hay que pedirlo explícito.** Sin activarlo, el SDK serializa el objeto de
retorno y lo manda como bloque de **texto**: la IA recibe un string que tiene que volver a parsear. Con él,
viaja en el campo estructurado y la consulta publica su esquema de salida.

## 8 · El botón de instalar bloqueado, sin decir por qué

**Síntoma:** un bundle valida —«todos los requisitos cumplidos», con autor y versión leídos— y el botón de
instalar está muerto.

**Lo que se sabe:** existe en la aplicación un **gate por versión** (*«para instalar esta extensión,
actualizá a la última versión»*). Y la aplicación se actualizó sola durante la sesión.

**Lo que NO se confirmó:** se exploró la hipótesis de que el tipo de servidor `binary` no estuviera
soportado, y **la evidencia no la sostuvo** — los hits en los recursos eran falsos positivos.

**La salida práctica:** registrar el servidor a mano en la configuración de la aplicación. **El bundle es
empaquetado, no arquitectura.**

**Alcance:** todo esto es sobre instalar en la *app* de escritorio. Para meter un módulo en el propio Gnothi
hay ahora otro camino, que no pasa por ningún botón: el paquete `.gmod` y `--install-module` del Host
—empaquetado también, pero del Host, no de la app—. Ver `C7`.

## 9 · El diagnóstico, cuando nada de esto alcanza

La aplicación escribe un log **por servidor**:

```bash
powershell -Command "Get-ChildItem \"$env:APPDATA\Claude\logs\" -Filter 'mcp*.log' | Select-Object Name, Length, LastWriteTime"
```

Un `mcp-server-<nombre>.log` tiene el **stderr** del servidor. Como los servidores stdio usan stderr para
todo su logging, ese archivo no es solo de errores: **ahí sale el arranque completo.** Si aparecen las
líneas de qué encontró y qué publica, arrancó bien y el problema es de otro lado.

Y el log principal de la aplicación dice qué soporta: `LocalMcpServerManager`, un puente que reporta cuántos
servidores stdio tiene conectados, y el reparto entre remotos y locales.
