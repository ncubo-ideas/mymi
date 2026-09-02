# Las puertas de la IA

Hay **tres** formas de que una IA llegue a los datos de la app, y no son variantes de la misma cosa:
tienen dueño distinto, transporte distinto y **techos radicalmente distintos**. Elegir mal cuesta
semanas.

> **De dónde sale este capítulo.** De código que corre: `Gnothi.Assistant` y `Gnothi.Mobile` en la
> rama `integracion/desktop-mobile2`, y el spike `Spikes/Lab.Siri` en `spike/ios-app-intents`. Lo que
> es spike está marcado como spike; lo que está medido dice sobre qué aparato.
>
> El vocabulario **no es nuestro**: las «Puertas» son del proyecto, y son **entradas al Host, desde
> arriba**. La salida hacia los módulos no es una puerta, es la *cáscara* — ver
> [el manifiesto](el-manifiesto-y-la-instalacion.md) §6.

---

## 1. Las tres, de un vistazo

| | **Puerta 1** | **Puerta 2** | **El asistente del sistema** |
|---|---|---|---|
| Quién pregunta | Un asistente externo — Claude Desktop | **Nuestro propio lazo**, adentro del Host | Siri · Google Assistant |
| Cómo llega | MCP sobre stdio | HTTP al proveedor del modelo | App Intents · App Actions |
| Quién paga el modelo | Quien ya paga su asistente | **La persona, con su llave** | Nadie: es del sistema |
| Escritorio | ✅ medido | ✅ | — |
| Teléfono | ❌ **no hay** asistente que hable MCP | ✅ medido en un Galaxy A54 | ✅ con techo |
| Cruza dos módulos en un turno | ✅ | ✅ | ❌ |
| Techo | el del asistente | el del modelo | **una sola puerta: el nombre propio** |

**La 2 existe por una razón, no por preferencia:** en un teléfono no hay un Claude de escritorio que
hable MCP. Sin Puerta 2, en móvil no hay IA.

---

## 2. Lo que las tres comparten, y es lo que hay que no romper

Antes de las diferencias, la parte que **no** se duplica.

**El índice de tools es UNO.** Puerta 1 lo proyecta a `JsonElement` para MCP; Puerta 2 lo proyecta a
string para el modelo. Debajo es el mismo `ToolIndex`. El lazo **no arma la lista**:

> *"y **no** construye la lista de tools: se la pide al índice del Host, que es **el mismo** que
> publica la Puerta 1."*

**El permiso lo aplica el Dispatcher, no la puerta.** Eso es lo que permite borrar una puerta el día
que sobre:

> *"Lo único que conoce es el Dispatcher. Fijate lo que **no** está en esta clase: ninguna referencia
> a instancias de módulos, ningún directorio, ninguna ruta, ningún runtime, y ninguna política."*
>
> *"La forma enredada —el lazo dueño de los módulos, los permisos y las cuotas— funciona igual de
> bien y **hace imposible borrar el lazo** el día que deje de ser necesario, porque se lleva la lógica
> de los módulos con él."*

**Para la Metodología eso se vuelve una regla de diseño:** cuando agregues una vía de acceso,
preguntate **qué se lleva con ella si se borra**. Si se lleva el catálogo o los permisos, está mal
puesta.

---

## 3. Puerta 1 · un asistente externo, por MCP

Es la que ya está escrita: [capítulo 6](06-cli-y-acceso-de-la-ia.md). Acá va sólo lo que cambia al
haber tres.

Es la única que **no le cuesta plata a la persona** —quien paga es quien ya paga su asistente— y la
única que hoy **no existe en móvil**. Y arrastra un peso que las otras no: el módulo embarca el SDK de
MCP, ~1,3 MB. Ver la forma cargo en [el manifiesto](el-manifiesto-y-la-instalacion.md) §6.

---

## 4. Puerta 2 · nuestro propio lazo

`Gnothi.Assistant`, cuatro archivos. El comentario de cabecera avisa qué es: *"un adaptador, no un
producto"*.

### 4.1 Y es un LAZO, no una pregunta

Lo más importante del capítulo, porque la primera versión **no** lo era y falló de la peor manera
posible:

> *"La primera versión preguntaba una vez, corría una tool y componía. Eso **no puede cruzar dos
> módulos en un turno**, que es la tesis del proyecto entero: pedido de mirar los exámenes y las
> facturas juntos, el modelo devuelve **dos** llamadas, el código viejo tomaba la primera, y la
> respuesta se leía **como si esos fueran todos los hechos**. Fallaba en SILENCIO, que es la peor
> forma de fallar."*

De ahí la línea donde vive el cruce — **todas** las llamadas de la ronda, no la primera:

```csharp
foreach (ToolCall call in said.Calls)
    conversation.Add(await Run(index, call, Say, ct));
```

**Y componer no es un método aparte:** *"el paso sin tool calls **ES** la composición"*. Los hechos son
de los módulos; la frase es del asistente. Es la misma regla del capítulo 0 §8 —el dominio nombra pero
no inventa— vista del otro lado de la frontera.

### 4.2 Acotado a propósito

```csharp
private const int MaxRounds = 6;
```

> *"Un modelo que sigue pidiendo consultas es un modo de falla real, y un lazo sin cota **gasta la
> plata de la persona** mientras pasa."*

Es la primera vez en estas guías que un límite se justifica por el bolsillo de quien usa la app. Con
Puerta 1 el costo era de otro; acá es de ella.

### 4.3 Una falla vuelve al modelo, no se tira

```csharp
if (!r.Authorized)
    return new Returned(call.Id, r.Reason, Failed: true);
```

> *"Si uno de los dos módulos de un cruce está apagado, la respuesta correcta son **los hechos del
> otro módulo más la noticia de que éste no está disponible** — no una excepción que pierde los dos."*

Es el consentimiento del capítulo del manifiesto visto desde arriba: apagar una consulta **degrada** la
respuesta, no la rompe.

### 4.4 Se resuelve contra el índice, nunca cortando el nombre

```
facturas_total_de_facturas
```

Tres guiones bajos, y sólo el índice sabe cuál separa al módulo.

> *"La versión anterior cortaba en el primero, lo que funciona **mientras ningún módulo se llame
> `mi_super`** — y nada lo impide."*

Es la misma familia de defecto que el capítulo 6 §6.3 llama *"sin strings en la frontera semántica"*,
y acá muerde del lado del ruteo.

### 4.5 El catálogo se arma por pregunta, y no se cachea

> *"Por pregunta y no cacheado, porque el catálogo **ya viene filtrado por el consentimiento** y la
> persona puede apagar una consulta entre una pregunta y la siguiente."*

Cachearlo convertiría el interruptor del capítulo del manifiesto en algo que tarda en hacer efecto. **Un
permiso que tarda no es un permiso.**

### 4.6 `IModel`: dos implementaciones, y la segunda es la que enseña

`ModelClaude` y `ModelOpenAiCompatible`. Que haya dos no es tener opciones por tenerlas; las tres
razones están escritas en el código y las tres son buenas:

| | Por qué existe la segunda |
|---|---|
| **1** | *"La API de Anthropic **no tiene tier gratuito**: son créditos prepagados. Eso hace que terminar la Puerta 2 dependa de una decisión comercial —llave propia o de la persona— que es justamente lo que **no** está decidido."* Varios otros proveedores sí tienen tier gratuito permanente, y todos hablan esta forma |
| **2** | *"**Un modelo local también la habla.** LM Studio y Ollama sirven `/chat/completions`, así que esta misma clase apunta a `http://localhost:1234/v1` sin llave — y entonces **nada cruza a ninguna nube**, que es la versión más fuerte de la arquitectura"* |
| **3** | *"Y **prueba la costura.** Que `IModel` fuera una interfaz era una afirmación: que cambiar de modelo es configuración y no código. Hasta que hubo dos implementaciones estaba sin probar"* |

> **La regla que sale de acá, y sirve para cualquier costura que escribas:** una interfaz con una sola
> implementación es una **promesa**, no una costura. La segunda es la que la cobra.

### 4.7 Lo que se puede medir sin llave

```csharp
public static IReadOnlyList<ToolForTheModel> CatalogFor(Dispatcher host)
```

> *"El mismo catálogo, **sin necesitar un modelo**. Es lo que hace medible el costo de una pregunta
> antes de que haya llave — y en un teléfono, donde la llave es exactamente lo que no se decidió, es la
> única parte de la Puerta 2 que se puede mostrar funcionando."*

Medido: **~1.364 tokens de costo fijo por pregunta, y el 86 % es el catálogo.**

**Eso es un observable nuevo, y de los buenos:** el costo fijo de una pregunta se puede medir sin gastar
un centavo. Un catálogo que crece sin que nadie lo mire es una factura que crece sin que nadie la mire.

---

## 5. El asistente del sistema · la tercera puerta, y la que tiene techo

Siri y Google Assistant no hablan MCP y no van a llamar a nuestro lazo. Llegan por el mecanismo del
sistema operativo: **App Intents** en iOS, **App Actions** en Android.

> **Esto es un spike, y de anteayer.** `Spikes/Lab.Siri`, corrido el 2026-08-28 sobre un iPhone 13 Pro
> **sin Apple Intelligence** y dos simuladores. **8 de 9 pruebas.** No es una app; es una medición del
> techo. El gemelo de Android se cita pero **no está en el repo**, así que de Android acá no se afirma
> nada.

### 5.1 Tres alturas, y hay que probarlas separadas

Ésta es la parte metodológica que vale más allá de iOS: el spike no preguntó *"¿funciona?"*, preguntó
lo mismo **en tres alturas**. Sin ese corte, un ✅ y un ❌ se promedian y no se aprende nada.

| Altura | La pregunta | Resultado |
|---|---|---|
| **Piso** | ¿Contesta si la persona **nombra la app**? | ✅ por voz y escrito |
| **Techo** | ¿Contesta **sin** nombrarla? | ❌ cayó en búsqueda web y en Apple Cash |
| **Cadena** | ¿Puede la persona **armar la cadena** ella misma? | ✅ y es la única sin techo |

### 5.2 El hallazgo que vale el spike

De una entidad, **Spotlight indexa el título y nada más**. Tres búsquedas contra el mismo índice, en la
misma sesión:

| Se busca | De dónde sale el texto | |
|---|---|---|
| `automercado` | el comercio → `displayName` | ✅ |
| `gasolinera` | el comercio → `displayName` | ✅ |
| `combustible` | el detalle → `contentDescription` | ❌ **nada** |

Y el absurdo que lo vuelve incontestable: buscando `gasolinera`, el resultado que aparece **muestra la
palabra «Combustible» impresa en su propio subtítulo**. Está en pantalla. Buscarla no encuentra nada.

> **El techo tiene una sola puerta, y es el nombre propio.** El monto, el concepto, la descripción —
> todo invisible. La factura de la farmacia se encuentra diciendo *«Farmacia La Bomba»*, nunca
> *«medicinas»* ni *«₡3.200»*.
>
> Para el negocio es duro, y hay que decirlo antes de prometer nada: **obliga a la persona a recordar
> el nombre exacto del comercio, que es justamente lo que uno no recuerda** cuando busca una factura
> vieja.

### 5.3 Y el techo no es el que uno supondría

> *"El techo no es «iOS no encuentra tus facturas». Es **«Siri no le pregunta a tu app»**."*

Dos pruebas con un minuto de diferencia, mismo teléfono, mismos datos: dicho en voz alta *«buscá mis
facturas del Automercado»* → **Apple Cash**; escrito en Spotlight `automercado` → la factura. **La
entidad llegó al índice.** Lo que no ocurre es que Siri lo consulte.

Y pierde **callado**: *"no hay error, no hay fallback, no hay forma de detectarlo desde el código. La
app compite contra un dominio de Apple por su propio vocabulario y pierde en silencio."*

### 5.4 Cuatro costos del piso que no estaban en el plan

Incluso lo que **sí** funciona viene con fricción que conviene contar antes, no después:

- **Hay que nombrar la app.** Apple lo exige para desambiguar.
- **Hay una compuerta de activación explícita.** iOS pregunta *"¿Querés activar los atajos de Gnothi
  con Siri?"* antes de que ninguna frase funcione. Sin ese sí, la app **no compite**.
- **Las frases son por idioma.** Con Siri en inglés y frases en español, la app *"simplemente no
  existe para el asistente"* — sin error.
- **El texto libre no existe.** Una frase sólo puede cargar un valor de un **conjunto cerrado** que la
  app declare; interpolar un `String` da *"AppEntity and AppEnum are the only allowed types"*. Es un
  muro, no un error de escritura.

> Ese último merece atención: es **la misma regla que el capítulo 6 §6.3** —los argumentos de la
> frontera semántica son conjuntos cerrados, no strings— y acá no es doctrina nuestra, **es el sistema
> operativo el que la impone**. Cuando dos frentes independientes llegan a la misma restricción, deja
> de ser preferencia.

### 5.5 La cadena es la única ruta sin techo

El atajo *Facturas → Ask Claude* corrió sin configurar nada, y Claude verificó la suma. Se reprodujo en
un simulador limpio que nunca había visto la app.

> Es la única de las tres que **no depende de que el asistente del sistema elija a nuestra app**. La
> persona arma la cadena, y ahí no hay competencia de vocabulario.

Queda abierto —y es la tesis de privacidad del producto— **si la cadena contesta sin red**. No se pudo
medir: el aparato no tenía Apple Intelligence y el simulador no deja activarlo.

---

## 6. Cuál elegir

| Si tu caso es… | La puerta |
|---|---|
| Escritorio, y la persona ya usa un asistente | **1** — y no le cuesta nada |
| Teléfono | **2**. No hay alternativa: no existe asistente móvil que hable MCP |
| Que la persona no pague nada ni ponga llave | **el sistema**, aceptando el techo del §5.2 |
| Que **nada** salga del aparato | **2 apuntando a un modelo local** |
| Cruzar dos módulos en una sola pregunta | **1 o 2**. El asistente del sistema no cruza |

**Y no son excluyentes.** El Host publica el mismo índice a las tres; agregar una no toca a las otras.
Ésa era la razón de la costura del §2.

---

## 7. Antes de dar una puerta por terminada

- [ ] ¿La puerta **sólo conoce el Dispatcher**? Si toca módulos, rutas o permisos, no se va a poder
      borrar el día que sobre
- [ ] ¿Pide el catálogo al índice del Host, en vez de armar el suyo?
- [ ] ¿Se arma **por pregunta**, para que apagar una consulta tenga efecto inmediato?
- [ ] ¿Corre **todas** las llamadas de la ronda, y no la primera? (§4.1 — falla en silencio)
- [ ] ¿El lazo tiene **cota**? Es la plata de la persona
- [ ] ¿Una falla de un módulo **vuelve al modelo** como resultado, en vez de tirar una excepción?
- [ ] ¿El nombre se resuelve contra el índice, y no cortando strings?
- [ ] ¿Se puede medir el costo fijo de una pregunta **sin llave**?

---

## 8. Lo que este capítulo no cubre

- **La decisión comercial de la llave.** Llave propia o de la persona no está decidido, y es lo que
  bloquea terminar la Puerta 2. El código está escrito para que la decisión sea configuración.
- **Android como asistente del sistema.** El spike de App Actions se cita y no está en el repo.
- **Cómo se construye la app móvil.** Eso es un capítulo aparte: por qué la cáscara tiene que ser en
  proceso, y qué significa que sea *"el mismo código"*. **Pendiente.**
- **Si la cadena del §5.5 contesta sin red.** Es la tesis de privacidad y sigue sin medirse.
- **El techo de Siri con Apple Intelligence.** Las dos pruebas del techo corrieron en un aparato que
  no la tiene. Que fallen ahí **no dice** qué pasa en un 15 Pro o superior.
