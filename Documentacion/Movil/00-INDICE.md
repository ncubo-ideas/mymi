# El Host de Gnothi en el teléfono — Android e iOS

**Corte: 2026-09-07.** Esta carpeta es la parte de Edgar de lo que Álvaro pidió documentar:
**el Host móvil y el iPhone**. La parte de escritorio, el módulo y el bundle están en
`Documentacion/Jonathan/`; la especificación, en `Documentacion/Especificacion/`.

Está partida en siete porque las preguntas que hay que contestar son distintas y no se leen
igual. Si sólo vas a leer una: **`03`**, que es el cuadro de qué anda hoy y en qué aparato.

| Doc | La pregunta que contesta |
|---|---|
| `01-que-quisimos-hacer.md` | ¿Qué problema teníamos en el teléfono, y qué eje lo ordenó? |
| `02-que-construimos.md` | ¿Qué existe y corre, pieza por pieza? |
| `03-que-funciona-hoy.md` | **¿Qué anda hoy, en qué aparato, y qué lo frena?** |
| `04-por-donde-nos-devolvimos.md` | ¿Por dónde nos metimos y por qué salimos? Con las trampas |
| `05-lo-medido.md` | Los números, con fecha y con aparato |
| `06-lo-que-sigue-abierto.md` | ¿Qué falta decidir, y de quién es cada decisión? |
## ANEXO — y no es parte del recorrido

Los dos que siguen **no contestan lo que se pidió documentar.** Son la respuesta a una pregunta
técnica puntual de Álvaro sobre compartir el motor entre módulos, y están escritos como una
respuesta para él: se apoyan en su propio set de notas (`NotasGnothi/04` R1, R2, R9, y el spike
S1 de `06`) y entran en un detalle que no hace falta para entender qué construimos.

**Si esto va a un wiki, el anexo no va** — o va aparte, con su propio título. Están acá porque el
análisis vale y porque el `06` los referencia, no porque haya que leerlos para entender el resto.

| Doc | La pregunta que contesta |
|---|---|
| `07-compartir-el-sustrato-medido.md` | ¿Se puede compartir el motor entre módulos? Medido, y por qué la forma que corre no puede |
| `08-hilos-y-procesos-del-sistema.md` | ¿Y si se estira con hilos o con procesos del SO? Con el diagrama de las seis formas |

Son **un par**: el `07` mide y el `08` contesta. Entre los dos cubren el punto 7 de la lista de
pendientes de Jonathan —*«el sustrato compartido: cada módulo trae su propio motor»*— y el `08` lo
hace **frase por frase**, incluida la afirmación de ese punto que no se sostiene.

Si de los dos hay que leer una sola cosa, es el **§3 del `07`**: por qué la forma que corre hoy no
puede compartir el motor, con la analogía y el intercambio que hay detrás.

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
