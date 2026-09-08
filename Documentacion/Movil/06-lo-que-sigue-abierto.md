# 06 · Lo que sigue abierto, y de quién es cada decisión

**Corte: 2026-09-07.** Separado por dueño, porque mezclar «falta escribir el `if`» con «hay que
decidir qué le pedimos a una institución» es lo que hace que las dos cosas se posterguen.

---

## De Álvaro

### 1 · ¿Se lleva la caja wasm al teléfono?

El spike `S1` de `NotasGnothi/06` preguntaba si un runtime wasm embebido puede cargar un dominio
como dato **en Android y en iOS**. **El mecanismo ya está probado en escritorio** con wasmtime, y
el módulo queda en 15 KB sin un byte de runtime adentro
(`Documentacion/Jonathan/F-evidencia/F5-la-caja-wasm.md`, medido el 2026-08-26). Lo que falta es
el port, y en un teléfono el runtime no sería wasmtime sino WAMR.

Con los números de `05` la pregunta ya tiene precio. **Con cuatro módulos** el disco queda casi
igual —la caja son 32 MB una vez más 4 × 15 KB, contra 45 MB de la forma hospedada— y **cobra en
tiempo**: 500–670 ms por consulta contra 150–217 en proceso.

Y hay algo que la caja **no** reemplaza: **en la caja no hay UI de la institución**. La forma
hospedada paga 11,3 MB por traer la pantalla del laboratorio. No son la misma cosa más barata:
son cosas distintas.

### 2 · ¿Se acepta que hay dos formas de módulo con garantías distintas?

Hoy es así **de hecho**, y no está escrito. El instalador rechaza un módulo en proceso que traiga
su propio `Puppeteer.dll` —es la decisión A1, «una sola cosa que auditar»— pero **el alta del
módulo hospedado nunca consulta esa lista**. No es un bug: el hospedado necesita su motor. Pero
la garantía vale para una forma y no para la otra.

### 3 · ¿La mudanza se le pregunta a la persona?

Cuando Gnothi pide su credencial, el publicador infiere que la cuenta se mudó y la app suelta de
la institución deja de actualizarse. **La hoja de consentimiento no dice esa consecuencia.**

Y el arreglo barato no es obviamente el correcto: que Gnothi lo avise sería **Gnothi afirmando
algo sobre el comportamiento de un tercero** que no puede saber — es política de la institución,
no del protocolo. Hay cinco opciones comparadas en `Documentacion/Especificacion/13` §10.5.

**La pregunta que decide, y no es técnica:** ¿la mudanza es una **consecuencia** de instalar el
módulo, o una **decisión aparte**?

---

## Del equipo

### 4 · ¿Vale 10 MB que la institución escriba su pantalla en .NET?

Es el 88,8% de lo que pesa un módulo hospedado, y **no es un costo que introduzca Gnothi**: la
app suelta de la institución pesa lo mismo servida por ella. Lo que compra es real —un dominio
escrito una vez, en C#, corriendo en el navegador y adentro de Gnothi—. Sacarlo significa pedirle
a la institución que traiga su dominio en .NET **y su pantalla en web**, que son dos cosas y dos
habilidades. Y hoy «la institución» somos nosotros, así que por ahora es decisión nuestra.

### 5 · Qué aserciones viajan de la app suelta a Gnothi

El traspaso está hecho en Exámenes y falta en Cargas, Compras y DealershipCar. **Lo que falta no
es sólo mecanismo: es decidir cuáles viajan, y eso es por institución** — Cargas tiene la
confirmación de tanque y las lecturas de odómetro, DealershipCar los aplazamientos y el perfil de
uso.

### 6 · El dominio para el enlace de la institución

`go.gnothi.app` no existe. Sin él, en iOS el alta por enlace no se puede probar, y en Android se
prueba con una muleta (`pm set-app-links`). Hace falta igual para el producto.

---

## Nuestro, y son trabajo

### 7 · Probar iOS en un aparato
Todo lo de `03` marcado **[NO MEDIDO]**. El guion está escrito y el paquete de módulos armado.
**Es lo que desbloquea la mitad de este documento.**

### 8 · La suite de journals dorados (R10)
No existe. El journal se reproduce contra el dominio actual **interpretado por el sustrato
actual**, así que la compatibilidad que importa no es de API sino de **determinismo de replay
entre versiones del sustrato**. Sin journals congelados, «mantenemos compatibilidad hacia atrás»
es una promesa y no una afirmación falsable. Álvaro la marcó como *barata ahora, carísima de
retrofitear*.

### 9 · Validar `substrate.range`
Se declara en el manifiesto y **no se lee en ningún lado**. Para la forma en proceso el sustrato
existe y su versión está clavada en `Directory.Build.props`: **no falta el sustrato, falta el
`if`**.

### 10 · El tag del log y las copias muertas del bundle
Dos cosas chicas y medidas: el 40% de cada bundle son copias `.br`/`.gz` que el servidor nunca
sirve, y los activos están fingerprinted por contenido, así que podrían servirse cacheables sin
riesgo de quedar viejos.

---

## Lo que NO está abierto, aunque lo parezca

Para no volver a discutirlo:

- **AppFunctions** no es una opción hasta que Google abra el EAP. No hay a quién pedírselo.
- **Declarar un App Intent desde C#** no se va a poder: App Intents es sólo-Swift por diseño del
  framework, no por un bug de las bindings.
- **Compartir el motor corriendo entre módulos hospedados** no se puede mientras el aislamiento
  sea por origen. Compartir el motor exige un runtime común, y un runtime común exige un origen
  común — que es exactamente lo que la contención existe para impedir.
