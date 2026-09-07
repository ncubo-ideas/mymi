# 06 — Plan de implementación del Host

## La regla que ordena la secuencia

**Un spike bloqueante primero, lo demás después.** Dos de las decisiones grandes dependen de una medición que todavía no existe; construir sobre ellas sin medir es apostar el calendario.

---

## Fase 0 — Los dos spikes bloqueantes

### S1. Runtime embebido cargando un dominio como dato

**La pregunta:** ¿puede un runtime wasm embebido, en Android y en iOS, **cargar un assembly IL de dominio desde su directorio pre-abierto** y correr el descubrimiento por reflection sobre él?

**Por qué es bloqueante.** De esto depende la forma del módulo (R9 de `04`): si funciona, el módulo es *dominio como dato dentro de una instancia sandboxeada* y conservamos aislamiento duro **y** sustrato compartido. Si no funciona, hay que elegir entre las dos cosas.

**Entregable:** arranca, lee su journal, responde — y **con qué número de cold start**. Una persona, pocos días. Candidato de runtime: WAMR (ver R2).

### S2. El guard de hilos por capacidad

**Antes de mergear la rama `wasm-replay`**, cambiar `bool singleThreaded = OperatingSystem.IsBrowser()` por una pregunta sobre **capacidad** del hospedaje, no sobre plataforma (R6 de `04`). Barato ahora; si entra con forma de plataforma, cada hospedaje nuevo agrega un `||`.

**Entregable:** el replay secuencial se activa en un hospedaje sin hilos que **no** sea el browser.

---

## Fase 1 — El despacho, con las dos puertas desde el primer commit

Construir `DespachoDelHost` (ver `02`) y **el bucle de agente en su propio assembly**, con el fixture de grafo de referencias **el mismo día**.

**No dejar el fixture para después.** Es veinte líneas y es lo que hace que el principio de temporalidad se cobre en vez de quedar en intención. El día que se pierda la propiedad, se pierde en silencio.

**Criterio de aceptación:** el assembly del despacho **no referencia** el del bucle de agente, y hay un test que falla si eso cambia.

---

## Fase 2 — Un módulo, cargado y respondiendo

Cargar un módulo, concederle su directorio, invocar una tool, devolver el resultado.

**Criterios:**
- El módulo **no tiene red concedida**. Verificable: intentar salir falla porque la capacidad no existe, no porque una regla lo prohíba.
- Un directorio por módulo. Dos módulos no comparten ruta.
- El resultado vuelve con el formato que el hint pidió, y **el journal no registra el formato**.
- La procedencia de la invocación queda en el Playbill, atada al `entryId`.

---

## Fase 3 — Sincronización: el Host como cartero

El Host hace el pull del feed del publicador y le pasa el payload al módulo **como parámetro**. El módulo asienta.

**Criterios:**
- **El Host no escribe el journal.** Verificable: quitar al módulo del camino y comprobar que no hay forma de que un asiento aparezca.
- El endpoint es **fijo por publicador**, registrado en la instalación, **no elegible en runtime**.
- Cursores, tokens y reintentos viven en almacenamiento del Host, **fuera del journal**.
- Un payload malformado o de emisor no autorizado **no deja rastro** en la historia de Sonia. Un error de ejecución propio **sí** queda auditable. La distinción importa: basura de un publicador no debe manchar la historia; un error nuestro sí debe poder investigarse.

---

## Fase 4 — Lo que Sonia ve

Lista de módulos con tamaño y **tasa** (MB/mes, hechos/mes), On/Off por módulo, "qué trae este módulo" vía introspección, purgar, desinstalar.

**Criterios:**
- El tamaño lo **mide el Host desde afuera** (`stat` sobre el directorio que él concedió). Un módulo que reporte un tamaño falso no puede existir.
- Apagar = el Host **omite las tools** de la lista que entrega al agente, y el módulo **no puede distinguirlo** de "nadie preguntó".
- El repertorio se muestra **separado** del journal, o se castiga justo lo que se quiere premiar.
- La purga la **propone** el Host y la **ejecuta** el módulo, que puede marcar hechos **no podables**.

---

## Fase 5 — Dos módulos y el cruce

El primer momento en que el proyecto demuestra algo que nadie más puede dar.

**Criterios:**
- Ningún módulo ve al otro. **El cruce lo hace el asistente combinando respuestas.**
- **El cruce no está precocinado** (ver `05`). Si el diseño permite hacer trampa sin darse cuenta, decirlo y proponer cómo lo detecta el propio loop.
- Los dos módulos hablan de **la misma persona** — identidad entre módulos es un requisito, no un detalle.

---

## Fase 6 — Empaque y conformance

El artefacto del módulo, firmado, y la lista de verificaciones que corre antes de publicar:

1. **Grafo de referencias** — ningún tier de host en el dominio ni en la superficie.
2. **Journals dorados** — el journal replaya idéntico bajo todo el rango de sustrato declarado (R10 de `04`).
3. **Introspección** — lo que el Host va a mostrar del módulo es lo que el autor quiso exponer. Detecta exposición accidental.
4. **Cold start** con un journal realista.
5. **Superficie de tools** — ninguna existe para pedirle algo a Sonia.
6. **El empaque falla si trae un journal de muestra adentro.**

---

## Qué instrumentar desde el día 1

- **Cold start por módulo.** Es la métrica que en dos años va a importar más que los megabytes, y la más fácil de instrumentar ahora. Con stdio el proceso arranca por conversación y hace replay antes de responder: medir **cuánto tarda el primer token** con una historia realista. Si pasa de un par de segundos, hacen falta snapshots — y ya existen (ver `05`).
- **Latencia por tool call** en cada hospedaje.
- **Líneas de cáscara por hospedaje.** Si alguna pasa de cien, la abstracción del módulo-sin-transporte se rompió.
- **Batería** en cualquier camino que mantenga un proceso vivo.

## Y una advertencia sobre el número de módulos

**Explosión de tools:** N instituciones × M tools degradan la precisión de selección del asistente y se comen la ventana de contexto. Con veinte módulos instalados esto es un problema real, y **no aparece en ningún panel de espacio**. Vale pensar la forma de la superficie temprano — sin perder el descubrimiento con schema por tool, que es lo que hace que las tools intencionales valgan (ver `03`).

## Recursos de desarrollo: el elenco

Para poder probar cruces hace falta data de **otros** módulos. El artefacto:

```
personas/                  ← recurso de desarrollo. NUNCA se empaqueta.
├── sonia-41/
│   ├── identidad.json     ← quién es, y qué cruces soporta esta persona
│   ├── laboratorio.journal
│   ├── supermercado.journal
│   └── auto.journal
└── pedro-58/              ← el caso longitudinal
```

- **El elenco es del ecosistema; los journals son de cada módulo.** Si cada módulo inventa su propia persona, los journals no cruzan.
- **La coherencia solo se garantiza en los cruces que `identidad.json` declara.** Lab × supermercado necesita dieta y lípidos coherentes; lab × colegio no necesita nada. Sin esta regla el trabajo no termina nunca.
- **Determinístico con semilla**, para que el demo sea reproducible en cualquier máquina.
- **Marcados**: procedencia de muestra en el Playbill o nombre de actor en un espacio reservado, para que un Host de desarrollo avise y **un Host real se niegue a cargarlos**.
- El mismo artefacto sirve para cuatro cosas: cruces en desarrollo, journals dorados, medición de cold start, y demo reproducible.
