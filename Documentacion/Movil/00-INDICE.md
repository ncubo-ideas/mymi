# El Host de Gnothi en el teléfono — Android e iOS

**Corte: 2026-09-07.** Esta carpeta es la parte de Edgar de lo que Álvaro pidió documentar:
**el Host móvil y el iPhone**. La parte de escritorio, el módulo y el bundle están en
`Documentacion/Jonathan/`; la especificación, en `Documentacion/Especificacion/`.

Está partida en ocho porque las preguntas que hay que contestar son distintas y no se leen
igual. Si sólo vas a leer una: **`03`**, que es el cuadro de qué anda hoy y en qué aparato.
Los dos últimos, `07` y `08`, no son parte de ese recorrido: son respuestas a preguntas que
Álvaro dejó abiertas, y se leen sueltos.

| Doc | La pregunta que contesta |
|---|---|
| `01-que-quisimos-hacer.md` | ¿Qué problema teníamos en el teléfono, y qué eje lo ordenó? |
| `02-que-construimos.md` | ¿Qué existe y corre, pieza por pieza? |
| `03-que-funciona-hoy.md` | **¿Qué anda hoy, en qué aparato, y qué lo frena?** |
| `04-por-donde-nos-devolvimos.md` | ¿Por dónde nos metimos y por qué salimos? Con las trampas |
| `05-lo-medido.md` | Los números, con fecha y con aparato |
| `06-lo-que-sigue-abierto.md` | ¿Qué falta decidir, y de quién es cada decisión? |
| `07-compartir-el-sustrato-medido.md` | **Para Álvaro.** ¿Se puede compartir el sustrato entre el Host y sus módulos? Con la medición que pidió (`_framework` en Release *trimmed*), la restricción **R9** y qué pasó con el spike **S1** |
| `08-hilos-y-procesos-del-sistema.md` | **Para Álvaro.** ¿Ese sustrato compartido se estira con hilos o con procesos del SO? Continuación del `07` |

---

## Cómo leer las etiquetas

Se usan las del documento del equipo, y **cada afirmación fuerte lleva una**:

- **[MEDIDO]** — se ejecutó y se verificó, acá, con fecha y aparato.
- **[FUENTE]** — documentado por el fabricante, con referencia.
- **[RAZONADO]** — inferencia nuestra. Falible, y se marca para que se pueda discutir.
- **[NO MEDIDO]** — no se probó. No es lo mismo que «no funciona».

Lo que no se midió está escrito como pendiente, no como hecho. Cuando algo se afirmó mal y
después se corrigió, **la corrección se dejó visible** en vez de reescribir el pasado: en `04`
hay tres, y una de ellas cambió la forma del producto en iOS.

---

## De dónde sale esto

De `NotasGnothiHost/`, que son 40 notas de trabajo tomadas mientras pasaba, y de las mediciones
propias sobre un **Galaxy A54 físico**, un emulador **Pixel 6 / API 34**, y un simulador de iOS.
Ninguna medición de acá es de laboratorio sintético: todas salieron de la app corriendo.

Los `.md` de esta carpeta **no repiten código**. Apuntan al archivo o al commit, porque un
documento que transcribe una implementación queda vencido en la primera refactorización y
después miente con confianza — que es peor que no existir.
