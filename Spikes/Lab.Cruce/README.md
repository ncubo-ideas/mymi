# El cruce entre módulos contra el modelo on-device

**Medido el 2026-09-02, simulador de iOS 26.5, Mac M2 / macOS 26.6.2 / Xcode 26.6.**
No es aparato. En iOS **26.2 no corre** — ver [`../Lab.Siri/HALLAZGOS.md`](../Lab.Siri/HALLAZGOS.md) §8.

```bash
./armar.sh ["la pregunta"]
```

La pregunta del spike es la de más arriba de todas: **¿puede el asistente contestar algo que
ningún módulo sabe solo?** El ejemplo que la ordena es *«tengo el colesterol alto porque vengo
comprando alimentos que me lo suben»* — exámenes tiene el analito, compras tiene la canasta, y
la conclusión no vive en ninguno de los dos.

## Lo que sí funciona, y es la noticia

**El tool calling corre en el simulador, y el modelo cruza dos módulos en un solo turno.** Con
`examenes_evaluar_tendencia` y `compras_consumo_de_grasas_saturadas` en la misma sesión, tres
corridas de tres llamaron a las dos y compusieron la respuesta:

> «Sí, tu colesterol está subiendo y está por encima del umbral. Has comprado manteca y
> embutidos en los últimos 18 meses, ambos en aumento.»

Eso es el boss final, en miniatura y con datos de pie. **El cableado no es el problema.**

## El cuello de botella es la SELECCIÓN de tools, no el razonamiento

Con una tool genérica de búsqueda por término en la misma sesión —`compras_consumo_de(termino)`,
que es una de las 15 que `compras` publica hoy— el modelo **la agarra siempre** y le pasa
`"colesterol"`:

| tools en la sesión | qué llamó | resultado |
|---|---|---|
| tendencia + `consumo_de` | `consumo_de("colesterol")` + tendencia | ❌ «no hay relación», 29 s |
| + descripción afilada, «buscá alimentos, no analitos» | `consumo_de("colesterol")`, y ya no llamó a exámenes | ❌ peor, 29 s |
| + la tool con el cruce ya nombrado, las tres juntas | `consumo_de("colesterol")` | ❌ ignoró la específica, 235 s |
| **solo tendencia + el cruce nombrado** | las dos correctas | ✅ **3/3, 18 s** |

**Afilar la descripción no sirvió**: se midió y no movió nada. El modelo empareja las palabras de
la pregunta con el parámetro y llama a la genérica.

Y no es que le falte instrucción, es que **le falta el dominio**: el mismo modelo contesta que un
analito es *«un componente esencial en una computadora»* (§8). No sabe qué alimentos suben el
colesterol, así que no puede derivar el cruce por su cuenta.

**Regla de diseño que sale de acá:** el cruce hay que **nombrarlo en una tool**, y las genéricas
no pueden estar compitiendo en la misma sesión. El mecanismo para eso ya existe y es
`disabledTools` en `installation.json` — el corte fino que hoy sirve para «la tendencia sí, la
tasa no» es el que hace falta para armar la sesión que se le entrega al modelo on-device.

## Lo que NO funciona: el encuadre

Las tres corridas buenas eligieron bien las tools y **ninguna respetó el encuadre igual**:

| corrida | qué hizo con el encuadre |
|---|---|
| 1 | limpia: los dos hechos y su relación, sin causa afirmada |
| 2 | *«este aumento es preocupante»* — juicio clínico que la instrucción prohibía |
| 3 | **markdown** pedido explícitamente que no, y *«está contribuyendo»* como causa probada |

La instrucción decía, textual, *«el veredicto clínico ya viene hecho y no es tuyo»* y *«en prosa
corrida y sin markdown»*. **Dos de tres la violaron.**

Eso mide **c6** (prosa o markdown) y **c8** (respeta el veredicto o diagnostica) del marcador, y
las dos salen mal on-device. Es el hallazgo que más pesa para el producto: la promesa de Gnothi
se apoya en que el asistente no diagnostique, y este modelo diagnostica.

**No dice nada sobre Claude por red**, que es la otra mitad de la Puerta 2: un modelo grande puede
respetar el encuadre y además no necesitar que le nombren el cruce. Queda por medir, y el
contraste entre los dos es la decisión de producto.

## Lo que este spike NO responde

- **Los payloads son de pie**, con la forma exacta de las tools reales. Cablearlo al `Dispatcher`
  es el paso siguiente y no es la parte dudosa: el Host ya llega a los módulos.
- **`compras` no tiene journal sembrado acá**, así que el `consumo_de_grasas_saturadas` no existe
  como tool de verdad todavía. Quién debe conocer el mapa alimento→analito es una pregunta de
  diseño abierta: ningún módulo puede ver al otro.
- **La latencia varía muchísimo** — 18 s la buena, 235 s la de tres tools. Sin medir por qué.

---

# La misma pregunta, por red

**Medido el 2026-09-02 con `claude-sonnet-5`, 7 corridas.** Mismas tools, mismos payloads, misma
pregunta y mismo encuadre que el lado on-device: lo único que cambia es el modelo.

```bash
dotnet run --project Spikes/Lab.Cruce/Red -- [curado|completo] ["la pregunta"]
```

## El resultado que decide

| | on-device 3B | Claude por red |
|---|---|---|
| selección con catálogo **curado** (2 tools) | ✅ 4/4 | ✅ 3/3 |
| selección con catálogo **completo** (5 tools, con genéricas) | ❌ **0/3** | ✅ **4/4** |
| afirma una causa que los datos no prueban | ❌ 2 de 4 | ✅ 0 de 7 |
| markdown cuando se pidió prosa | ❌ 1 de 4 | ✅ 0 de 7 |
| inventa cuando la tool dice `hayDatos:false` | sin medir | ✅ no |
| latencia | 18–235 s | **6–9 s** |
| vueltas hasta cerrar | 1 | 2 |

**Con el catálogo completo —las mismas genéricas que al modelo chico le capturaban la pregunta y
lo mandaban a `consumo_de("colesterol")`— Claude eligió las dos correctas las cuatro veces y no
tocó las genéricas ni una.**

De ahí sale lo que importa para el diseño: **recortar el catálogo es una concesión al modelo
chico, no una regla del producto.** Si mañana la Puerta 2 corre contra un modelo grande, los 15
tools de `compras` no son un problema y `disabledTools` vuelve a ser lo que dice el manifiesto que
es —el corte que decide la persona—, no una muleta del asistente.

Y lo otro: **el problema del encuadre era del modelo, no del encuadre.** El mismo texto de
instrucciones que on-device se violaba dos de cuatro veces, acá se respetó siete de siete:

> «Los dos datos coinciden en el tiempo y van en la misma dirección, pero esto no alcanza para
> decir que uno haya causado el otro: son dos registros que suben juntos, no una prueba de
> causalidad.»

Sobre datos vacíos tampoco rellena:

> «No tengo datos de hemoglobina cargados en tus registros… puede que ese análisis no esté entre
> los que autorizaste compartir desde tu laboratorio.»

## Sin la muleta: ¿deduce el cruce, o solo lo elige?

Las dos variantes de arriba le dan `compras_consumo_de_grasas_saturadas` — o sea, **el cruce ya
resuelto**. Que lo elija bien prueba que sabe elegir, no que sepa deducir. La comparación estaba
despareja: al modelo chico se lo probó primero SIN esa tool, y al grande siempre con ella.

El modo `crudo` la empareja: `evaluar_tendencia` + `consumo_de(termino)` genérica, nada más. Es
exactamente la configuración donde el 3B llamó `consumo_de("colesterol")` y contestó «no hay
relación».

**Claude no llamó a `consumo_de("colesterol")` ni una vez en tres.** Dedujo los alimentos solo:

    #1   manteca · queso · carne roja                                  13 s
    #2   manteca · queso                                               15 s
    #3   grasa saturada · manteca · queso · carne roja · aceite        17 s

| configuración | on-device 3B | Claude por red |
|---|---|---|
| con el cruce nombrado | ✅ 4/4 | ✅ 3/3 |
| con el cruce nombrado + genéricas | ❌ 0/3 | ✅ 4/4 |
| **sin el cruce nombrado** | ❌ **0/2** | ✅ **3/3** |

### Lo que esto decide

**Un modelo capaz no necesita que el Host sepa de dominio.** La genérica alcanza: el conocimiento
de qué alimentos suben el colesterol lo pone el modelo, no el catálogo. Así que **nombrar el cruce
en una tool del Host es una compensación para el modelo chico**, y meterla ahí dejaría
conocimiento de negocio grabado en la capa que está diseñada para no entender de negocios — a
cambio de la limitación de un solo modelo.

Si el destino es on-device, esa compensación hace falta igual. Pero conviene saber que es eso, y
no una pieza que la arquitectura pedía.

### Y encontró un problema en la premisa

Ninguno de nosotros lo había dicho:

> «no hay forma de saber desde estos datos cuánto de eso efectivamente comiste, en qué cantidades,
> ni qué otros factores influyen en tu colesterol»

**Comprar no es comer.** El boss final —«tengo el colesterol alto porque compro estos alimentos»—
cruza una compra con un analito, y entre las dos cosas falta quién lo consumió y cuánto. Es una
limitación del ejemplo, no del mecanismo, pero si la respuesta va a decirse en serio hay que
decirla con ese cuidado.

## Un matiz que conviene discutir, no dar por bueno

Las siete corridas terminan derivando al médico —*«lo indicado es que lo charles con tu médico»*—.
La instrucción decía *«no diagnostiques ni recomendes tratamiento»*, y derivar no es ninguna de
las dos: es lo contrario de diagnosticar, y coincide con lo que el propio dominio de exámenes
dice de los controles con `exigeAtencion`. **Lo cuento como cumplimiento, pero es un juicio, no
una medición.** Si el equipo lo lee distinto, el encuadre debería decirlo explícitamente.

## Lo que esta mitad NO responde

- **Es la nube.** No dice nada de un modelo grande corriendo local (LM Studio, Ollama), que es la
  variante más alineada con la tesis del producto y que `ModelOpenAiCompatible` ya soporta
  cambiando la base URL.
- **Siete corridas de una sola pregunta.** El encuadre aguantó acá; c8 pide la pregunta que más
  invita a diagnosticar, con el antígeno en 5,2 y sangre oculta positiva.
- **Un error del banco de pruebas, corregido a mitad de camino:** el filtro devolvía los mismos
  números para `ColesterolLdl` que para `ColesterolTotal`. Lo detectó el propio modelo —*«tus dos
  mediciones de colesterol total dieron el mismo dato»*—. Ya está arreglado en los dos lados.
