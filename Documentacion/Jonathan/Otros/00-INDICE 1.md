# Gnothi — documentación de arquitectura para el equipo del Host

**Para:** los desarrolladores que están implementando el Host de Gnothi, y sus asistentes de IA.
**De:** el arquitecto del sistema (Ncubo), autor del framework Puppeteer.
**Corte:** 2026-08-23.

---

## Por qué existe este set

Están implementando el Host a partir de los recursos visuales (las infografías). Eso alcanza para entender la propuesta, pero no para construir: las láminas muestran **qué** queremos, no **por qué** cada pieza está donde está ni **qué caminos ya se probaron y se descartaron**.

El riesgo concreto que este set evita es re-derivar. Varias decisiones que parecen abiertas ya están cerradas, y algunas se cerraron **contra** la intuición inicial —incluida la mía— después de medir. Si vuelven a explorarlas desde cero van a gastar semanas y probablemente van a llegar a la misma respuesta por el camino largo.

## Orden de lectura

| # | documento | qué contesta |
|---|---|---|
| 01 | `01-arquitectura.md` | Qué es la Arquitectura Gnothi, sus cuatro piezas, y la matriz de autoridades. **Empezar acá.** |
| 02 | `02-el-host.md` | Lo que ustedes construyen: las cuatro autoridades, las dos puertas y una costura, y lo que el Host **nunca** debe hacer. |
| 03 | `03-el-modulo.md` | Qué carga el Host: dominio + repertorio + view + manifiesto. Cómo se exponen los puppets (y el anti-patrón que dos IAs ya propusieron mal). |
| 04 | `04-restricciones-dribleadas.md` | **El documento más importante para no perder tiempo.** Cada restricción técnica, qué bloquea, y qué hicimos. Incluye los callejones cerrados. |
| 05 | `05-medido-vs-supuesto.md` | La evidencia con su fuente, y los claims que ya sabemos que están sobrevendidos. |
| 06 | `06-plan-implementacion.md` | La secuencia: qué construir primero, spikes bloqueantes, criterio de aceptación por etapa. |
| 07 | `07-preguntas-abiertas.md` | Dónde **sí** tienen latitud de diseño, y qué hallazgos traer de vuelta. |

## Cómo trabajar con esto

**1. Lo de 01–04 son decisiones tomadas.** Se pueden discutir con un argumento nuevo o con una medición que las contradiga — de hecho eso se agradece. Lo que no sirve es re-abrirlas por intuición. Si algo les parece mal, revisen primero `04` y `05`: es probable que la razón esté ahí.

**2. Distingan siempre lo medido de lo supuesto.** En `05` está la tabla. Si van a afirmar algo que no está ahí, díganlo como hipótesis. Esta disciplina no es formalismo: el proyecto entero se apoya en unos pocos claims verificables, y un claim inflado que se cae se lleva puestos a los que sí aguantan.

**3. No agreguen features especulativos.** El framework crece por dolor real, no por completitud imaginada. "Podríamos soportar X por si acaso" no ayuda; "medimos X y duele" sí.

**4. Cuando algo no se pueda hoy por un componente de terceros:** hagan el workaround, **pero pónganlo donde se pueda borrar**. Ver el patrón de la Puerta 2 en `02`. La regla del proyecto es que el día que un release ajeno resuelva el hueco, se borre una capa y no se reescriba nada.

**5. Verifiquen contra el código antes de afirmar.** Los archivos y líneas que este set cita están en el repo `Puppeteer Pacifico`, master `67c9747` salvo donde diga otra cosa. Los comentarios del código a veces están desincronizados con el código — hay al menos un caso documentado en `04`.

## Glosario mínimo

| término | qué es |
|---|---|
| **Sonia** | la persona dueña de los datos. Autoridad de consentimiento. (En videos y láminas viejas aparece como *Carmen*.) |
| **Institución** | laboratorio, agencia, supermercado, colegio, Estado. **Proveedor externo de eventos, nada más.** |
| **Módulo** | lo que la institución le entrega a Sonia. **No es una app: es carga del Host.** |
| **Repertorio** | el conocimiento de dominio de la institución (reglas, rangos, cálculos). Se reemplaza por versión. |
| **Journal** | los hechos de la vida de Sonia. Append-only. **Es de ella**, aunque el dominio sea de la institución. |
| **Sustrato** | el motor: runtime .NET + `Puppeteer` + `Choreography`. **Lo lleva el Host, una sola vez.** |
| **Host** | lo que ustedes construyen. Ejecuta · Aísla · Autoriza · Mide. Única pieza con red. |
| **Puerta 1 / Puerta 2** | las dos entradas al Host. La 2 es la capa transitoria. Ver `02`. |
| **Elenco** | el conjunto de personas sintéticas de prueba, con un journal por módulo. Recurso de desarrollo. |
| **Puppet / actor** | la unidad de Puppeteer: dominio + su journal, con un único escritor. |
