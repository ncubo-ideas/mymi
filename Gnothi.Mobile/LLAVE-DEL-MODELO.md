# La llave del modelo — cómo configurarla

Para quien tenga la app de Gnothi en un teléfono y quiera que el **Asistente** conteste.
Verificado el 2026-08-27 en un Galaxy A54 y en el emulador.

**La idea en una línea:** los módulos y tus journals viven en el teléfono y no necesitan
llave de nada; la llave hace falta **solo** para que el Asistente le hable a un modelo de
lenguaje, y **la ponés vos** — gratuita o de pago, da igual.

---

## El camino normal: todo desde la app

1. Abrí Gnothi → pestaña **Ajustes**.
2. **Cambiar proveedor** → elegí uno de la lista (o dejá Groq, que es el que viene).
3. Si ese proveedor no tiene llave guardada, el editor se abre solo: **pegá la llave** y
   **Guardar**.
4. Listo. Andá a **Asistente** y preguntá.

No hay paso 5. La llave queda guardada **en el área privada de la app, en ese teléfono**,
y no se muestra nunca más — solo se ve *"Guardada en este teléfono"*. Para reemplazarla:
**Cambiar**. Para sacarla: **Borrar**.

**Las llaves se guardan POR PROVEEDOR.** Si pasás de Groq a ChatGPT y después volvés, la de
Groq sigue ahí. Cambiar de proveedor no borra nada.

---

## De dónde saco una llave

| Proveedor | Dónde | Cuesta |
|---|---|---|
| **Groq** *(el que viene)* | console.groq.com → API Keys | **Gratis, sin tarjeta.** Llaves `gsk_…` |
| **OpenAI (ChatGPT)** | platform.openai.com → API keys | De pago (créditos aparte de ChatGPT Plus). Llaves `sk-…` |
| **Claude (Anthropic)** | platform.claude.com → API keys | De pago (créditos aparte de la suscripción). Llaves `sk-ant-…` |
| **Cerebras** | cloud.cerebras.ai | Gratis, 1M tokens/día |
| **Gemini (Google)** | aistudio.google.com | Gratis, sin tarjeta |
| **OpenRouter** | openrouter.ai → Keys | Una llave, ~30 modelos gratis |

**Ojo con la trampa de las suscripciones:** tener ChatGPT Plus o Claude Pro **no** te da
llave de API. Son productos separados y se pagan aparte. La única de la lista que no pide
ni tarjeta es Groq (y Gemini).

---

## Los límites del nivel gratis de Groq

- **8 000 tokens por minuto**, por cuenta.
- Una pregunta simple ≈ 2 500 tokens → pasan 2–3 por minuto.
- El **cruce** (exámenes + compras juntos) ≈ 5 000 → **una por minuto**.
- Si te pasás, la burbuja muestra el error `429 rate_limit_exceeded`. No está roto:
  **esperá 30 segundos y volvé a preguntar.**
- Dos personas preguntando a la vez con la misma cuenta chocan entre sí. Para una demo:
  de a una pregunta, con calma.

---

## «Otro» — un modelo en tu propia red, sin llave

La opción **Otro** del selector acepta cualquier servidor que hable el formato OpenAI
(`/chat/completions`). El caso estrella: **LM Studio** corriendo en tu compu.

- URL: `http://<LA-IP-DE-TU-COMPU>:1234/v1` — **no** `localhost`: en el teléfono,
  localhost es el teléfono.
- Modelo: el nombre que LM Studio muestre.
- Llave: ninguna — dejá la que sea o guardá cualquier cosa; sin llave el header no viaja.

Con eso **nada sale de tu red**: ni el catálogo, ni los hechos. Es la versión más privada
de todas. Lo que no está medido es si un modelo chico elige bien entre las 8 consultas —
probalo antes de prometerlo.

---

## Seguridad, en cuatro reglas

1. **La llave nunca va al repo ni al log.** El log dice `model key: saved (56 chars)` — el
   largo, jamás el valor.
2. **Nunca se muestra de vuelta.** El editor abre vacío siempre; la UI solo dice si hay o
   no hay.
3. **Vive en `files/key-<Proveedor>.key`**, área privada de la app: otra app del teléfono
   no puede leerla. (Un backup del teléfono o root, sí — no es una bóveda.)
4. **Si una llave se pegó en un chat o quedó en una captura, se rota** en la consola del
   proveedor. Es un minuto.

---

## Para desarrolladores: la ruta por cable

Sin abrir la UI, con el teléfono en depuración USB:

```
adb push mi.key /data/local/tmp/mk
adb shell "run-as com.ncubo.gnothi.host sh -c 'cp /data/local/tmp/mk files/key-Groq.key'"
adb shell rm /data/local/tmp/mk
```

El nombre del archivo es `key-` + el nombre del proveedor sin espacios ni símbolos
(`key-Groq.key`, `key-OpenAIChatGPT.key`). El proveedor elegido queda en `files/model.cfg`.

**Migración:** las instalaciones viejas tenían un solo `files/model.key`; al primer
arranque de esta versión se convierte solo en `key-Groq.key`.

## Para desarrolladores: agregar un proveedor a la lista

La lista vive en `Assets/ui.html`, constante `PROV` — nombre, URL base, modelo por defecto
y una nota. Una línea por proveedor. Todos tienen que hablar el formato OpenAI, que es el
que implementa `Gnothi.Assistant/ModelOpenAiCompatible.cs`; el preset de Claude usa el
endpoint de compatibilidad de Anthropic (`api.anthropic.com/v1`) — está en la lista pero
aún sin probar de punta a punta, a diferencia de Groq, que es el medido.
