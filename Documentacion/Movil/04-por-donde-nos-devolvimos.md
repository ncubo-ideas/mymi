# 04 · Por dónde nos metimos, y por dónde nos devolvimos

**Corte: 2026-09-07.** Los caminos cerrados y las correcciones. Está acá porque un callejón
documentado ahorra el mes de volver a entrar, y porque **una corrección que se esconde vuelve**.

---

## 1 · Caminos que probamos y cerramos

### AppFunctions de Android ❌
Era el candidato natural para la Puerta 3: el asistente del sistema llamando funciones que la app
declara. **Está en private preview y el EAP está cerrado** — no hay a quién pedírselo. **[FUENTE]**

### El protocolo «octopus» de Claude ❌
Un camino para que la app de Claude en el teléfono hablara con apps locales. Detrás de un flag,
sin disponibilidad. **[FUENTE]**

### Gemini Nano por AICore ❌ *en nuestro aparato*
No está cerrado en general: **el Galaxy A54 no tiene AICore**, y Gemini Intelligence v3 pide 12 GB
de RAM. Es una restricción de hardware, no de diseño — pero decide con qué podemos probar.
**[MEDIDO]**

### Declarar un App Intent desde C# en iOS ❌
Y esto es el que más enseñó, porque **verificar *por qué* destapó un trade-off que decide el
producto**:

- La razón contingente: el issue `dotnet/macios#24863` está abierto desde marzo de 2026, sin una
  sola respuesta de los mantenedores y sin workaround. **[FUENTE]**
- La razón estructural, que es la que importa: **App Intents es un framework sólo-Swift**. Usa
  features del *lenguaje* Swift sin representación en Objective-C, y las bindings de .NET pasan
  por el runtime de ObjC. Encima, el índice `Metadata.appintents` lo genera el compilador de
  Swift escaneando fuentes Swift: **una clase C# nunca aparecería ahí**. Apple cambió de
  tecnología respecto de SiriKit —que sí era compatible con ObjC— y de paso cerró la puerta.

**El trade-off que abre:** en iOS hoy se puede tener Puerta 3 nativa **o** carga de módulos en
runtime. Elegir una apaga la otra. No es una limitación nuestra ni un bug que vayan a arreglar
pronto.

### Nuestro propio Host, antes de la rama de Jonathan ❌
Armamos un Host completo para entender el problema. Se aprendió, y **el código se descartó**. No
buscarlo como ejemplo de nada.

### Sintetizar un delegate por tool ❌
El primer intento del puente: derivar el `inputSchema` de una firma C#. Ata la tool a una firma
que el Host no puede conocer, y por eso las tools con parámetros quedaban sin publicar. **El
manifiesto es un dato, no un tipo.**

---

## 2 · Correcciones — cosas que afirmamos mal

Van con nombre y fecha porque el valor está en que se vea que cambiaron.

### «El AOT de iOS mata la tesis» — falso
La nota `31` dejó esto como el punto abierto más grave: *«si un módulo que llega después de
instalar no puede cargarse por reflexión con el AOT obligatorio, se cae la tesis y no sólo la
puerta»*.

**Se midió y la pared es más baja.** La pregunta estaba mal planteada: el núcleo del Host **no se
carga, se compila adentro** de la app; lo único que se carga en runtime son los DLL de los
módulos. **El riesgo real se movió de sitio: ya no es técnico, es la cláusula 2.5.2 de la App
Store.** **[MEDIDO 2026-09-01]**

### «La denegación de la CSP es muda» — a medias
Se documentó que Gnothi no se enteraba de un rechazo y que la prueba de contención necesitaba una
sonda temporal. **[MEDIDO 2026-09-07]** que no: el PWA de Blazor intenta registrar su service
worker en cada carga y la política se lo deniega **sola**, y eso ya aparecía en el log. Lo que
faltaba no era visibilidad sino que fuera **un hecho estructurado** — y eso se construyó.

### «El sustrato compartido es un spike sin medir» — falso
Escrito en `NotasGnothiHost/40` y corregido el mismo día: **Jonathan lo construyó y lo midió el
2026-08-26** (`Documentacion/Jonathan/F-evidencia/F5-la-caja-wasm.md`). Lo que sigue abierto es
más preciso: el mecanismo está probado **en escritorio con wasmtime**, y el port a móvil no.

### «El origen es lo que separa los journals» — falso
Dicho en voz alta al discutir si los módulos podían compartir origen. **[MEDIDO 2026-09-07]**: la
separación la da **el puente**, que se ata al WebView y no al origen. Con origen compartido los
journals siguen separados; lo que se comparte es la credencial en `localStorage` y los archivos
del otro módulo — que igual alcanza para no hacerlo.

---

## 3 · Trampas que muerden y no avisan

Para quien venga después. Todas costaron al menos una tarde.

**Un tag de logcat con dos puntos no se puede filtrar.** El guion decía
`adb logcat -s modulo:examenes:I`, y `-s` lee cada término como `TAG:PRIORIDAD`: un término
inválido hace que logcat **rechace toda la lista**. No salía ni `GNOTHI`, y la app parecía muda.
**[MEDIDO 2026-09-07]**

**PowerShell no entrecomilla `-ArgumentList`.** El escenario levantaba las instituciones con el
título partido: `--Titulo Supermercado La Guaria` llegaba como tres argumentos. La página decía
«Supermercado» y **el subtítulo se perdía entero**. Falla en silencio y el script igual anuncia
«levantado». **[MEDIDO 2026-09-07]**

**`adb` desde Git Bash destroza las rutas.** `screencap /sdcard/...` muestra su ayuda y
`uiautomator` falla mudo, porque MSYS convierte la ruta. Se resuelve con `MSYS_NO_PATHCONV=1` o
con `exec-out`.

**Con transporte stdio, stdout ES el canal JSON-RPC.** El replay del motor imprime `1%2%3%` con
`Console.Write`; en una consola es ruido, en el MCP **corrompe el protocolo** — y como no cierra
la línea, se pega adelante del mensaje siguiente y el cliente se desincroniza sin error claro.

**Cambiar el significado de un argumento rompe la sesión en silencio.** Se cambió un argumento de
«carpeta del módulo» a «raíz de datos»; el cliente tenía cacheado el viejo y el Host reportó **0
módulos, sin error**.

**Un comentario XML con `--` rompe el build y el error no nombra el archivo.** `plutil` no lo ve;
`Xamarin.MacDev` sí.

**El `<base href>` del bundle decide dónde hay que servirlo.** Servir en la raíz un bundle
publicado para `/app/` da 404 en todo y **una pantalla en blanco sin ninguna pista**.

**`Compress-Archive` de PowerShell 5.1 escribe rutas con backslash**, que viola el spec del ZIP.
En una Mac se extrae como archivos planos, no como árbol.

**Los heredoc de bash se trancan con documentos largos en UTF-8**, y colapsan los backslashes.
Pasó tres veces. Para texto largo, escribir el archivo directamente.
