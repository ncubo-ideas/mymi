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
