# 05 · Lo medido

**Corte: 2026-09-07.** Cada número con su fecha y su aparato. Nada de acá salió de un laboratorio
sintético: todo es la app corriendo.

**Los aparatos:** un **Galaxy A54 físico**, un emulador **Pixel 6 / API 34**, y un emulador
`sdk_gphone64_x86_64` (Android 16 / SDK 36). De iOS, **simulador** — no hay aparato.

---

## Tiempos

| qué | cuánto | dónde |
|---|---|---|
| llamada a un módulo sobre stdio | **378 ms** | escritorio. Corrigió el README, que decía «unos segundos» |
| Puerta 3 · arranque en frío, del `bindService` al JSON | **2 055 ms** | A54. Desglose: bind 22 → conexión 931 → catálogo 278 → `Serve` 1 054 |
| Puerta 3 · llamada en caliente | **153 ms** | A54 |
| motor del módulo hospedado, arrancando | **~1,0 s** | emulador, 2026-09-07 |
| módulo hospedado, de abrir a contestar | **~1,4 s** | emulador, incluye credencial y pull |

El arranque en frío de la Puerta 3 contesta una preocupación explícita —*«arranque del runtime
más hidratación con el usuario esperando»*—: **son ~2 segundos, y la segunda llamada cae a
0,15 s**.

## Tamaños

Sobre los cuatro módulos hospedados instalados por el alta real, **2026-09-07**:

| | |
|---|---|
| los cuatro módulos en disco | **81 MB** |
| lo que el servidor realmente sirve | **49 MB** — el 40% restante son copias `.br`/`.gz` que nunca se sirven |
| deduplicado por contenido | **23,8 MB** — el 65% de cada módulo es byte-idéntico entre los cuatro |

Y de qué está hecho un módulo hospedado (exámenes, publicado en Release con trimming):

| | | |
|---|---|---|
| el motor de event sourcing — Puppeteer, Choreography | 0,97 MB | **8,6%** |
| el dominio y la UI de la institución | 0,29 MB | 2,6% |
| correr .NET en un navegador | 10,02 MB | **88,8%** |

**Lo que se creía que era el peso resultó ser el acompañante.** El mismo módulo en su forma en
proceso pesa **188 KB** y le pide el motor prestado al Host — 60 veces menos, y la diferencia
entera es el préstamo.

## Contención

**[MEDIDO 2026-09-07]**, manejando el módulo vivo por el protocolo de DevTools, antes y después
del arreglo:

| intento | antes | después |
|---|---|---|
| POST de un formulario a otro origen | **salió** — la página terminó en `example.com` y el servidor contestó **405**, o sea que el POST llegó a la red | bloqueado por `form-action 'self'` |
| navegación de nivel superior | **salió**, con los datos en la URL | rechazada, y el rechazo queda en el log |

El 405 es lo que cierra el caso: **no era un agujero teórico, los datos salían del aparato.**

Y la política se prueba sola en cada corrida: el PWA de Blazor intenta registrar su service
worker y la CSP se lo deniega, sin preparar nada.

## Revocación del publicador

**[MEDIDO 2026-09-07]** con curl contra los mocks de exámenes y de compras, el ciclo entero:

```
active → Gnothi pide credencial → migrated + graceUntil, y la app suelta SIGUE LEYENDO
       → vence la gracia → leer 401 invalid_token · rotar 401 · pedir de nuevo 401
       → Gnothi lee 200 → se desvincula → active otra vez
```

Las tres puertas, y no una: **[MEDIDO leyendo el cliente]** que ante un 401 al leer rota la
credencial, y si la rotación falla pide una concesión nueva con su device key. Cerrar sólo la
lectura habría devuelto el acceso en dos saltos.

## La credencial no viaja

**[MEDIDO 2026-09-04]** sobre los cuatro módulos: cero coincidencias de credencial en los
journals, buscando `at_`, `rt_`, `device-`, `Token`, `Bearer`. Control de que la búsqueda vale:
en el mismo journal **sí** aparecen los datos legibles del negocio.

Y **[MEDIDO 2026-09-07]** que la política funciona sola: al reabrir un módulo con el publicador
reiniciado, el log dijo *«the publisher rejected the credential; asking for another»* y siguió.

## El paquete que se le pasó a Renan

**[MEDIDO 2026-09-07]** los cuatro módulos hospedados, instalados por el instalador de verdad
—se dieron de alta desde el enlace de su institución y se sacó lo que quedó—: 999 entradas,
52,2 MB, `"type": "webview"` los cuatro. Verificado archivo por archivo que **no lleva datos de
nadie**: sin journals, sin `meta.bin`, sin credenciales.

---

## Lo que NO está medido, y hay que decirlo

- **Nada de iOS corrió en un aparato.** Compila con 0 errores y el guion está escrito.
- **El enlace de la institución en iOS**, porque `go.gnothi.app` no existe.
- **El consumo de memoria con varios módulos abiertos a la vez.** La regla de desalojo hace que
  no pase —sólo el módulo en pantalla sobrevive— pero no se midió el pico.
- **La Puerta 3 con Apple Intelligence.** Falta el aparato que la soporta.
