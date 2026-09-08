# 10 — Dos flujos, un solo `Dispatcher`

**Para el equipo del Host. Corte 2026-09-02, rev. 2.** *(La rev. 1 decía tres flujos, con Android como app nativa + IPC. Se reemplazó: Android y iOS comparten el flujo de móvil.)*

## El problema no era técnico: era querer que móvil fuera igual que escritorio

Escritorio y móvil tienen **primitivas distintas** para lo único que cambia — cómo llega una consulta desde el asistente hasta el actor que tiene el journal:

| | escritorio | móvil (Android e iOS) |
|---|---|---|
| ¿se puede lanzar un proceso hijo? | sí | **no** |
| ¿se puede cargar código descargado en el proceso? | sí | **no si Gnothi está en tienda** (IL = ejecutable descargado) |
| ¿qué código descargado SÍ se puede ejecutar? | cualquiera | **JavaScript / wasm dentro de un WebView** — la única excepción que Play y Apple nombran |
| ¿una app puede darle un servicio a otra de otro desarrollador? | sí | Android sí, **iOS no** — por eso no se puede construir sobre eso |

Forzar el flujo de escritorio sobre móvil es lo que produjo los callejones: IL que las tiendas rechazan, OPFS sin ruta, `InvalidStateError` por dos escritores. **Móvil tiene una solución sencilla si se le permite ser distinto.**

## Lo que NO cambia — y es lo único que tiene que ser igual

```
                 asistente (el que sea)
                        │
              ┌─────────┴─────────┐
           Puerta 1/3          Puerta 2
        (bucle ajeno)      (nuestro AgentLoop)
              └─────────┬─────────┘
                        ▼
              Dispatcher.Serve(Request) → Response     ← IDÉNTICO en los dos
                        ▼
                     IShell                            ← UNO por flujo
```

`Dispatcher`, `ModuleCatalog`, `Manifest`, `ToolIndex`, `Meter`, `ConsentWatch`, `AgentLoop`: **los mismos archivos**. Ya está medido que compilan sin el SDK de MCP. Cambia **una implementación de `IShell`**.

## Los dos flujos

| | **escritorio** (existe) | **móvil** (`11`, diferencias de plataforma en `12`) |
|---|---|---|
| **qué es el módulo** | `.gmod`: dos DLLs de IL (dominio + superficie) | **la PWA de la institución** — la misma Blazor WASM que ya publica |
| **dónde corre** | in-process en Gnothi | en un **WebView de Gnothi**, uno por módulo |
| **escritor único** | el módulo dentro de Gnothi | el módulo dentro de Gnothi |
| **dónde vive el journal** | carpeta que Gnothi concede | archivos del contenedor de Gnothi, por módulo |
| **quién trae los eventos** | Gnothi (cartero) | Gnothi (cartero) |
| **red del módulo** | ninguna, por regla | **ninguna, por incapacidad** — CSP `connect-src 'none'` |
| **`IShell`** | `InProcessShell` / `StdioShell` | **`WebViewShell`** |
| **sustrato** | Gnothi lo presta (17–54 KB por módulo) | **una instancia por módulo**; los bytes pueden ser de Gnothi (§ `11`) |
| **medir desde afuera** | ✅ | ✅ |
| **UI de la institución** | `view.json` (sin renderer todavía) | **la PWA se renderiza a sí misma**, contenida |
| **cumple tiendas** | n/a | ✅ contenido web en WebView |
| **"app primero"** | n/a | ✅ la PWA funciona sola antes de Gnothi; después migra adentro |

**El modelo original completo — módulo escritor dentro de Gnothi, Gnothi cartero, módulo sin red — se cumple en móvil, y en móvil es el único que las tiendas permiten.**

## La regla que gobierna los dos

> **Un journal, un escritor.** El escritor es el módulo, dentro de Gnothi, en los dos flujos.

Corolario para no volver a atascarse: **si una solución exige que dos procesos toquen el mismo archivo de journal, está mal.** Y una segunda, nueva: **si una solución exige que la institución publique una app nativa, está mal** — la institución publica una PWA, y Gnothi la hospeda.

## Qué le pasa a la lámina 3

El "sustrato prestado con 17 KB por módulo" **es de escritorio hoy**. En móvil cada módulo instancia su runtime dentro del wasm; **los bytes** pueden compartirse (Gnothi sirve el runtime a todos los orígenes) pero eso es optimización pendiente, no día 1. Hasta medirlo, dibujar el 45:1 en móvil es contar de más. Lo durable en los dos flujos es **la agregación**: un catálogo, un consentimiento, una mediación, y **un escritor**.
