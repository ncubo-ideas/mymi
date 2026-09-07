# A4 · El caso de uso, con números reales

## En un párrafo

Una persona le pregunta a su asistente cuánto gastó en el supermercado, y el asistente le contesta
leyendo el registro que está en el disco de ella. **No le pregunta nada al supermercado, y no sabe que
detrás hay un módulo de facturas.**

El supermercado no entrega una API: entrega **sus facturas como eventos** en un solo endpoint de
lectura. La persona instala el módulo de ese negocio, y su app trae esos eventos y los asienta —uno por
uno, con checkpoint— en una carpeta suya. A partir de ahí el dato es de ella.

Cuando pregunta, el asistente le habla a **un solo servidor MCP**: el Host. Que ya sabía qué puede
contestar ese módulo porque leyó su manifiesto al arrancar, sin levantarlo. Recién con la pregunta lo
levanta, obtiene la respuesta, lo desaloja, y la devuelve tal cual.

```
el supermercado publica  →  la app la trae al disco de ella  →  ella pregunta
                                                                    ↓
                            asistente → Host → el módulo → su journal
```

---

## La traza, medida

*(Esta traza es la del **POC de `facturas`** — el primer módulo, que todavía trae su propio transporte y
el motor adentro. Los números de acá son de ese POC. El demo vigente, ya en el molde de módulo cargo,
está más abajo.)*

### Movimiento 1 · La historia entra

| paso | qué pasó |
|---|---|
| El proveedor publica | 4 eventos en su endpoint: `#101 TQ-0001 5.571,50` · `#102 TQ-0002 4.750,00` · `#103 TQ-0003 9.000,00` · `#104 TQ-9001 8.670,00` |
| La persona baja la app | de la página de descarga del negocio |
| Elige una carpeta suya | y la app le pide un token de device al proveedor |
| Trae los eventos posteriores a su checkpoint | `GET /loan/78341/events?after=103` |
| Los aplica **uno por uno**, con flush después de cada uno | si se cae, retoma desde el último procesado |

Resultado en su disco:

```
4 facturas · total 27.991,50 · leído hasta el evento 104
```

### Movimiento 2 · La pregunta sale

| paso | qué pasó |
|---|---|
| El modelo ↔ el cliente local | HTTPS |
| El cliente lanza el Host | proceso hijo + stdio |
| El Host ya tenía el catálogo | lo leyó del manifiesto al arrancar, **sin levantar el módulo** |
| Llega la llamada | ahí sí levanta el módulo |
| El módulo rehidrata su journal y responde | y el Host **lo desaloja** |
| La respuesta sube tal cual | `{"total": 27991.5, "cantidadDeFacturas": 4, "ultimoEventoLeido": 104}` |

Lo que ve el asistente son **tres consultas en una sola lista**: dos del Host y una que salió del
manifiesto de un módulo. No puede distinguirlas.

---

## El demo vigente · `compras` (Súper La Guaria)

Llegó un segundo módulo, `compras`, y ya nació en el molde destino: es **carga** en el sentido pleno
—dos DLLs (`Compras.App.dll` la superficie + `Compras.Dominio.dll` el dominio, cero-deps) +
`manifest.json` + `view.json`, empaquetadas en un `.gmod`—, **sin transporte propio y sin motor
adentro**. El Host le presta el sustrato una sola vez, y su instalación llegó a la máquina por **su
propio MSI** per-usuario, que corre `--install-module` para dejarlo listo. Ver `C7`.

Corrida la consulta `resumen` por consola con `--query`, el Host devuelve un hecho real:

```json
{ "hayDatos": true, "totalGastado": 3174606, "cantidadDeCompras": 87, "ultimoEventoLeido": 387 }
```

87 compras asentadas, leídas hasta el evento 387. Mismo mecanismo que la traza de `facturas` —la
persona custodia su historia y el Host la lee sin pasar por el proveedor—, pero con el módulo ya
convertido en pura carga: la diferencia que faltaba para que dejara de ser «el primer módulo» y pasara
a ser el patrón.

---

## Lo que lo vuelve un caso de uso y no una demo

Estas cuatro cosas son la prueba, y ninguna está en el camino feliz.

**La app no le pidió servicios al proveedor.** Solo su propia narrativa: un verbo de lectura. Y es
falsable — el endpoint señuelo del proveedor devuelve **403** con el token de la app, porque su scope es
`loan.events.read` y nada más.

**El asistente no pudo traer la factura nueva.** Después de inyectar la cuarta factura, siguió diciendo
**19.321,50** y pidió que se abriera la app. El número se movió recién con el *Refresh*. **El dato entra
por la puerta de la persona, no por la del asistente** — y no porque una regla lo prohíba: porque el
asistente no tiene forma de disparar eso.

**El asistente nunca habló con el módulo.** El módulo registró en su log `Client (Gnothi.Host)`, no el
nombre del asistente. Y no sabe que MCP existe hacia afuera.

**El modelo no vio el journal.** Recibió el resultado de una consulta, no acceso al registro. Ni un
handle, ni un token que le sobreviva a la llamada.

## Y dos que el Host puede contestar y ningún módulo puede

**El tamaño lo midió el Host desde afuera**, con `stat` sobre la carpeta que él concedió — no se lo
preguntó al módulo. Un módulo que reporta 4 MB mientras ocupa 400 no puede existir si nadie le pregunta.

**Y contó las consultas del asistente.** No es cuota para proteger el dispositivo: es que la persona
pueda ver cuánto le extrajeron.

## El interruptor

La persona decide **consulta por consulta**. Apagar «el total» no le niega el permiso al asistente:
**hace que la consulta no exista** para él. Verificado: la llamada devuelve `Unknown tool`.

Y el módulo sigue visible **para ella**, con cero consultas publicadas. Invisible para el asistente,
visible para la dueña.

## El detalle que resume la tesis mejor que cualquier lámina

Durante la prueba, la máquina se reinició. El servidor del proveedor guarda su estado en memoria, así
que **perdió la cuarta factura**.

El journal de la persona la conservó. Y el asistente la leyó de ahí.

**El proveedor olvidó, la persona no.** Eso es todo el proyecto en una línea, y no hubo que actuarlo:
pasó solo.
