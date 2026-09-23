# Puppeteer · Cómo se usa — guía técnica

Documento colaborativo (docs-as-code). La guía se escribe en **capítulos HTML
independientes** y se ensambla en un único `index.html` con un script sin
dependencias. Así varias personas pueden trabajar en paralelo y los cambios se
gestionan por commits y Pull Requests en Azure DevOps.

## Estructura

```
PuppeteerGuia/
  documento/
    capitulos/           <- FUENTE DE VERDAD: un archivo HTML por sección
      00-portada.html
      01-prefacio.html
      02-introduccion.html
      04-cap-1-fundamentos.html
      ... (un parcial por capítulo)
      12-anexo-a.html
    plantilla.html       <- cascarón: <head>, CSS, índice lateral, scripts
    construir.ps1        <- ensambla index.html desde plantilla + capitulos
    index.html           <- GENERADO. No editar a mano.
  ejemplos/              <- proyecto .NET; los ejemplos que compilan y corren
```

## Cómo hacer un cambio

1. **Edita el capítulo** correspondiente en `documento/capitulos/` (o pídele a
   Claude que lo haga). **No edites `index.html`**: es generado.
2. **Reconstruye** el documento:
   ```powershell
   cd documento
   ./construir.ps1
   ```
3. **Commitea** el capítulo modificado **y** el `index.html` regenerado, en una
   rama, y abre un **Pull Request** en Azure DevOps para revisión.

> Regla de oro: **una decisión = un capítulo = un PR**. Editar capítulos
> distintos en PRs distintos evita conflictos de merge. Si `index.html` da
> conflicto, no lo resuelvas a mano: acepta cualquier versión y vuelve a correr
> `./construir.ps1`.

## Editar con o sin IA

Ambos caminos funcionan porque todo son archivos de texto versionados:

- **Con Claude:** "corrige el Capítulo 5 para…". Claude edita el parcial, corre
  `construir.ps1` y deja listo el commit.
- **Sin IA:** abre el `.html` del capítulo y edítalo. La prosa es HTML simple
  (`<p>`, `<h3>`, tablas). Los recuadros (HAZ / NO HAGAS / Nota / Pruébalo) se
  copian del patrón de otro capítulo.

## Ver el resultado

- Abre `documento/index.html` en cualquier navegador (funciona offline, con modo
  claro/oscuro).
- **PDF** (cuando se necesite): con Chrome/Edge en modo headless:
  ```
  chrome --headless=new --no-pdf-header-footer --print-to-pdf=Puppeteer-Como-se-usa.pdf index.html
  ```

## Los ejemplos de código

Los fragmentos de juguete de la guía viven en `ejemplos/` y **compilan y corren**
(referencian el proyecto `Puppeteer`). Para correrlos:

```powershell
cd ejemplos
dotnet run              # todos
dotnet run -- cap4      # un capítulo concreto (cap2.1, cap2.2, cap3 … cap8)
```

Cuando cambies un ejemplo, corre el proyecto, captura la salida real y actualízala
en el capítulo y en el Anexo A.

## Convenciones del contenido

- Los fragmentos marcados **real** provienen de la app *Velada* (bingo sobre
  Puppeteer) y son ilustrativos; el resto son ejemplos de juguete verificados.
- Recomendaciones tipadas: `HAZ` / `CONSIDERA` / `EVITA` / `NO HAGAS`.
- Recuadros `Nota del diseñador` (el porqué) y `Pruébalo` (cómo correrlo).
