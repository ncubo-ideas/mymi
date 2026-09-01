# Lab.IntentsDesdeNet — ¿puede una app de .NET llevar App Intents adentro?

**Medido, no leído.** Toda una tarde tratamos `dotnet/macios#24863` como si cerrara la pregunta.
No la cierra: es **un** reporte, abierto desde marzo de 2026, sin respuesta de los mantenedores,
que vio el síntoma y nunca buscó la causa.

## La hipótesis

Las App Intents **no se descubren leyendo el binario**. Se descubren leyendo
`Metadata.appintents/extract.actionsdata`, un directorio de JSON que Xcode genera **en tiempo
de compilación** con `appintentsmetadataprocessor`. El build de .NET **no corre esa
herramienta**. Entonces:

> El binario está bien. La metadata falta. Y la metadata se puede generar aparte.

Lo segundo no es una esperanza: **Bazel lo hace en producción, fuera de Xcode.** Su código
(`rules_apple/apple/internal/resource_actions/app_intents.bzl`) pasa como `--binary-file` una
**ruta falsa** — con `--compile-time-extraction` el procesador extrae de los fuentes Swift y de
los `.swiftconstvalues`, no del ejecutable.

## Lo que el reportero no miró, y este spike sí

| | reportero de #24863 | este spike |
|---|---|---|
| ¿revisó si `Metadata.appintents` existía en el bundle? | no | **paso 3** |
| ¿usó `AppIntentsPackage`? | sí — y hay reportes de que duplica metadata | **no** |
| ¿`ForceLoad`? | no se sabe | **sí** — sin él, el linker borra la conformance porque nadie la llama por código |
| ¿framework dinámico o estático? | framework | **estático** — el tipo Swift queda en el mismo Mach-O que la app |

Lo último importa por otro dato: **en Xcode puro, las intents dentro de un framework dinámico
tampoco se descubren** (`AppIntentCounter2`, hilo 757950 de Apple). Puede que el problema del
reportero nunca haya sido .NET.

## Cómo correrlo

En la Mac, con un simulador abierto:

```bash
cd Spikes/Lab.IntentsDesdeNet && chmod +x armar.sh && ./armar.sh
```

Hace seis pasos e imprime un veredicto en cada uno. **El primero que falle es la respuesta.**

```
1  swiftc → libGnothiIntents.a + .swiftconstvalues
2  dotnet build, y verifica que la conformance sobrevivió al linker
3  ¿dotnet build dejó Metadata.appintents?          ← la hipótesis: se espera NO
4  generarla con appintentsmetadataprocessor e inyectarla, re-firmar
5  instalar en el simulador
6  medir a mano en Atajos: ¿aparece? ¿corre?
```

## Qué reportar

Cuatro líneas:

```
paso 3   Metadata.appintents en el bundle tras dotnet build:  SI / NO
paso 4   extract.actionsdata generado e inyectado:            SI / NO
6a       Atajos muestra «Probar desde .NET»:                  SI / NO
6b       al correrla dice «Hola desde Swift…»:                SI / NO
```

Y si algo falla, **la salida del script tal cual**, no interpretada.

## Qué significa cada resultado

**6a y 6b SÍ → una sola app en iOS es posible.** El Host de .NET bajo Mono —carga dinámica de
módulos incluida— con los App Intents en Swift adentro. Se abre la casilla que
`NotasGnothiHost/33` dio por inexistente, y las dos apps con App Group dejan de ser necesarias.

**6a SÍ, 6b NO → Atajos la ve pero no la instancia.** Es la incógnita que queda: si el runtime
resuelve el tipo por su nombre mangleado en cualquier imagen cargada o solo en algunas. El
enlace estático debería cubrirlo; si no, el error exacto de Atajos dice por dónde.

**6a NO → la metadata está pero no la leen.** Mirar `extract.actionsdata`: ahí se ve si el
nombre del módulo o del tipo no coincide con el binario.

**Paso 3 SÍ → la hipótesis era falsa** y `#24863` es otra cosa. También valioso.

## Lo que NO prueba, para no sobrevender

- No prueba que la intent pueda **llamar al Host de .NET**. Eso es interop Swift→C# y es el paso
  siguiente, no éste.
- No prueba nada en un **iPhone físico**: la firma ad-hoc del paso 4 es de simulador. En un
  aparato real hay que re-firmar con la identidad del equipo.
- No prueba que **sobreviva a Release** con `MtouchLink=SdkOnly`. Se mide en Debug sin linker a
  propósito: una variable a la vez.
