# 07 — Preguntas abiertas: dónde sí tienen latitud

Este documento existe porque un set que solo transmite decisiones produce una de dos cosas malas: obediencia sin criterio, o rechazo de todo. **Nombrar el espacio abierto es lo que hace creíble el espacio cerrado.**

Todo lo que sigue está genuinamente sin decidir. Si tienen una opinión fundada, tráiganla — sobre todo si viene con una medición.

---

## 1. El formato del manifiesto

Sabemos **qué campos lleva**: rango de sustrato, capacidades declaradas, los tres ajustes de journal (`compression`, `encryption`, `maxFileSize`), endpoint del publicador, declaración de superficie de tools, firma. **No está escrito.**

Latitud: la forma, la extensibilidad, cómo se versiona el propio manifiesto.

Restricción: el modelo de capacidades tiene que ser **parte del contrato, no una recomendación**. Un módulo que declara "cero red" debe **fallar** en un host que se la conceda igual. Sin eso, publicar el formato es regalar la idea sin las salvaguardas que la hacen valer.

---

## 2. La view declarativa

Sabemos que **el módulo describe qué mostrar y el Host renderiza** — el módulo no ejecuta interfaz. Qué lenguaje, sin decidir.

Latitud grande. Vale mirar qué hay en el ecosistema de asistentes para UI dentro de la conversación antes de inventar un lenguaje propio: si la UI puede renderizarse **dentro del chat** en vez de vivir en una app aparte, encaja bien con lo que Sonia realmente quiere hacer (ver el detalle del cruce, revocar un servicio, revisar una factura) sin salir de donde está.

Restricción: si el módulo pudiera ejecutar interfaz, recupera iniciativa — y la asimetría "el módulo responde, no pregunta" se cae.

---

## 3. Firma, conformance y revocación

Es **la capa de gobernanza, y está sin diseñar.** Sabemos que hace falta: módulos firmados, manifiesto de capacidades público, mecanismo de revocación, y una suite de conformance que corra **contra cualquier host** (si el módulo solo corre en nuestro Host, "reemplazable" es una palabra y no un hecho).

Y hay un costo que hay que aceptar de entrada: **esto nos convierte en el guardián.** La confianza que se le quita a cada institución no desaparece — se concentra en nosotros. No hay salida puramente técnica; se mitiga con Host open source, builds reproducibles y manifiestos públicos, pero no se elimina. Es un intercambio bueno (es más fácil auditar un host que quince módulos, y Sonia confía una sola vez), pero hay que decirlo en voz alta.

---

## 4. El hueco de la superficie de tools como canal de atención

**Este es un problema real que encontramos y no resolvimos.**

La asimetría es "el módulo responde, no pregunta": no despierta, no notifica, no interrumpe. Pero **el nombre y la descripción de una tool son un canal hacia la atención de Sonia.** Un módulo puede publicar `completar_encuesta` y el modelo se lo va a ofrecer. Ahí la incapacidad no alcanza.

Defensa parcial que tenemos: el Host controla **la lista de tools que entrega al modelo**, más la política de Sonia, más una declaración en el manifiesto de que la superficie es read-only hacia su atención, verificable por conformance.

**Falta el diseño.** Es el primer requisito concreto de la capa de gobernanza y es un buen lugar para aportar.

---

## 5. Una aserción de Sonia que sirve a varios dominios

"Soy vegetariana" la usa el módulo de compras para filtrar, y podría usarla un dominio nutricional. Pero **los módulos no se ven entre sí.**

| opción | costo |
|---|---|
| **(a)** la declara en cada módulo que la sepa usar | duplicación chica y **semánticamente defendible** — "vegetariana" no significa exactamente lo mismo para un filtro de compras que para un dominio nutricional. Única consistente con el aislamiento tal como está |
| **(b)** vive en una capa compartida que los módulos leen | necesita una capability explícita mediada por el Host; erosiona el aislamiento |
| **(c)** el asistente la acarrea en tiempo de consulta | gratis, pero no durable |

**Lectura actual: (a) para el POC**, y (b) solo si el POC muestra que la duplicación duele de verdad. Es el primer lugar donde el modelo tiene tensión genuina.

---

## 6. La capa de composición del sujeto

Hay un tipo de cosa que no es del laboratorio ni del auto: **umbrales de qué le interesa saber, metas que cruzan dominios, definiciones de vistas compuestas.** Eso no tiene casa hoy.

Si aparece la necesidad, ese sí sería un journal donde **Sonia es la institución** — ella como única autora y única escritora. Puede que no haga falta para el MVP. No construirlo por si acaso; construirlo cuando duela.

---

## 7. Cuánto pesa de verdad el sustrato

**Pendiente y barato:** publicar en Release trimmed contra master actual y medir `_framework`. Los números que circulan son de un build Debug sin trimming contra un árbol **pre-split**, así que **sobrevalúan el problema**.

Y una consecuencia de razonamiento que conviene tener antes de medir: si el payload cae mucho, el argumento de **tamaño** para compartir sustrato se debilita para el MVP y sobrevive solo a escala. El argumento de **auditoría** (una sola cosa que auditar) es independiente de los bytes y no se debilita. La medición probablemente **degrade el motivo declarado y ascienda el otro** — y eso es mejor, porque es un argumento que no envejece con el próximo release del trimmer.

---

## 8. Modelo de negocio del asistente propio

Si construimos la Puerta 2, hay una decisión que **bloquea el diseño** y no es técnica: **BYOK (el usuario trae su llave) o llave nuestra.** Con llave nuestra, cada consulta de Sonia cuesta plata y aparece un backend de facturación — que irónicamente devuelve el servidor que la arquitectura presume no tener.

**Decidirlo antes de codificar, no después.**

---

## 9. Dónde vive la crypto cuando vuelva

Hoy `BouncyCastle` está en `Adapters.Sockets` y sirve a dos concerns co-locados: **socket+TLS** (SimpleX — Gnothi no lo necesita) y **firma + sealed box** (Ed25519 — **Gnothi sí lo necesita, en los dos lados**).

Por qué en los dos lados: el Host lo necesita para verificar firma de módulo y manifiesto; y **el módulo lo necesita para verificar que un evento entrante viene firmado por su propia institución** — con lo cual un host malicioso puede demorar o descartar, pero **no falsificar**. Eso convierte otra promesa en incapacidad.

⇒ Pide un **adapter propio de crypto**, no el core (pondría ~4.8 MB en todos los payloads) ni Sockets (arrastraría el listener a un módulo). El principio para la spec:

> **El eje del split no es "core vs host". Es quién paga los bytes.** Un adapter por capacidad, y cada hospedaje compone el conjunto que usa.

Medir antes de comprometerse: cuánto queda de BouncyCastle bajo trimming usando solo Ed25519 + sealed box; y si la BCL de .NET ya trae Ed25519 en la versión que vayan a usar (si lo trae, la dependencia desaparece entera).

---

## 10. Qué traer de vuelta

Reporten sobre todo estas tres cosas, porque cambian decisiones aguas arriba:

1. **Si el dominio necesitó cambios** para correr confinado. Eso no es un inconveniente, **es el hallazgo** — significa que hay una fuga de infraestructura y hay que nombrarla.
2. **Cualquier número.** Cold start, latencia, payload, batería. El proyecto entero se argumenta con mediciones; cada una que traigan mueve el pitch de "creemos" a "medimos".
3. **Cualquier cosa de este set que el código contradiga.** Los comentarios del motor ya se desincronizaron al menos una vez del motor; este set puede desincronizarse igual. Prefiero corregirlo que defenderlo.
