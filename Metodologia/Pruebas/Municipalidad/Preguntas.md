# Preguntas — Municipalidad

Lo que esta app se compromete a poder contestar. Salió de la corrida en vivo del **2026-09-03** —los
papeles reales de una cuenta municipal sobre la mesa, y la persona que conoce el trámite contestando—
registrada en la [bitácora](BITACORA.md), y se escribió como `Preguntas.md` el **2026-09-08**,
siguiendo [la columna vertebral](../../Guias/columna-vertebral.md) como guía.

La persona es **un contribuyente** —física o jurídica— que paga a su municipalidad basura, limpieza,
policía, bienes inmuebles y parques, y que hoy tiene un portal que le dice **cuánto debe** y un
comprobante por correo cada vez que paga.

**Este dominio es híbrido, y se decidió a propósito.** La municipalidad manda los hechos —lo puesto al
cobro, lo pagado, la mora—; la persona aporta lo que la municipalidad no puede saber: **cuánto quiere
apartar por mes**, y **el valor de su finca** si lo declaró. Dos consecuencias:

> **El journal NO es enteramente reconstruible.** La reserva adoptada y el valor declarado son de la
> persona; borrar la carpeta los pierde. El respaldo no es opcional.
>
> **Y lo que la municipalidad no manda, no existe.** La app **recibe y alarma; no verifica**. La mora que
> viene en el recibo se registra tal cual — la app no la recalcula ni la discute.

La frase que la persona dio para la app: *"Tu Municipalidad te dice cuánto debés. Nosotros te ayudamos
a entenderlo, anticiparlo y administrarlo."*

---

# Parte 1 · Lo que el modelo sabe

Capacidad, no preguntas. La columna de la derecha se llenó **al modelar**, y es lo que hace corrible el
[observable 13](../../Guias/observables.md). No es una clase por renglón: es que **ningún renglón quede
vacío**.

| | Qué es | Dónde vive |
|---|---|---|
| **El rubro** | Qué cosa se cobra: basura, limpieza de calles, policía, bienes inmuebles, parques. Cada uno con **su cadencia** y **su ley de cálculo** | `Rubro` — **catálogo de cinco instancias**, no cinco clases. Los otros cinco del menú del portal —cementerio, patentes, licores, arreglo de pago, permiso— no tienen cadencia conocida: [hueco 9](HUECOS.md) |
| **La cadencia** | Los servicios se cobran **por mes**; bienes inmuebles y parques **por trimestre**. Vence **el último día** del período. Y cada cadencia **lee `Periodo` distinto**: `.04` es abril en una y cuarto trimestre en otra | `Cadencia` → `CobroMensual`, `CobroTrimestral` |
| **El período** | El mes o el trimestre que una línea cubre | `Periodo` → `Mes`, `Trimestre` |
| **La ley de cálculo** | Basura y policía se cobran **por unidad**; limpieza, bienes inmuebles y parques **sobre la base imponible** — el valor de la finca. Cambia **qué explica un aumento** | `LeyDeCalculo` → `TarifaPorUnidad`, `TarifaSobreBaseImponible` |
| **La finca** | Existe aunque ningún papel la nombre: el portal consolida por cédula. El IBI es **suyo** | `Finca`, `ValorDeFinca` — el valor entra por parámetro: [hueco 2](HUECOS.md) |
| **Lo puesto al cobro** | Lo que la municipalidad dice que **va a** cobrar: rubro, período, monto | `Cobro` — es intención de la institución |
| **El pago** | El comprobante: número de recibo, fecha, descuento, y sus líneas | `Pago`, `LineaPagada` |
| **La mora** | Los intereses que **vienen en el recibo**. Se reciben, no se calculan | `LineaPagada.Intereses` — hecho recibido |
| **La consulta** | La foto del portal: qué está pendiente hoy. **Fechada**, porque mañana no existe | `Consulta`, `LineaPendiente` |
| **La reserva** | Lo que la app **propone** apartar por mes, y lo que la persona **adoptó** | `ReservaPropuesta` (calculada), `ReservaDeclarada` (un comando) |
| **El descuento por pronto pago** | Una vez al año, en el primer trimestre, a la tasa del acuerdo municipal | Regla en `Cuenta.AhorroPorProntoPago(tasa)` — la tasa por parámetro |

## Los dos alfabetos de `Periodo` no entran al dominio

El recibo escribe `2026.04` y significa *abril* en una fila y *cuarto trimestre* en otra. **Eso se
resuelve en la API**, que parte el texto en año y número; el dominio recibe el número y la cadencia del
rubro lo lee — `CobroMensual.Leer(2026, 4)` es abril; `CobroTrimestral.Leer(2026, 4)` es el cuarto
trimestre. Si esa lógica entrara al dominio como texto, el dominio hablaría el idioma del portal.

## Lo que el portal ya contesta, y por eso se descarta **como pantalla**

- Cuánto debés ahora, por rubro, con sus intereses
- Cuánto sería pagar el año entero
- Si estás atrasado

**Todo entra al modelo; nada de eso tiene pantalla propia.** La corrección del bloque 1 del punto de
partida aplicada desde el primer día.

## Lo que NADIE tiene

**El valor de la finca**, si la persona no lo declara — y **los pagos hechos fuera del portal**, que
no dejan comprobante digital. Los dos son huecos con dueño, no defaults.

---

# Parte 2 · Los juicios que el dominio toma

Ninguno se deduce de los datos. La forma es *dirección + medida + objeto + contexto*, y cada uno
produce **al menos una pregunta** que el dominio contesta.

**Firmados el 2026-09-03 por la persona:**

| # | El juicio | La decisión | La pregunta que produce |
|---|---|---|---|
| 1 | **La cadencia y el vencimiento** | Servicios por mes, bienes inmuebles y parques por trimestre; vence el último día del período | `VencimientosAl(fecha, diasDeAviso)` |
| 2 | **La declaración de bienes inmuebles** | Cada 5 años, y puede cambiar el monto | `CambioDeAvaluo` — *¿por qué subió mi IBI?* |
| 3 | **La app recibe la mora, no la recalcula** | Decisión de producto: *recibir y alarmar, no verificar*. Las reglas se guardan sólo para proyectar | `LineaPagada.Intereses` es hecho; `CostoDePagarTarde` es proyección marcada |
| 4 | **La base del descuento** | Por todo el monto, prorrateado por el tiempo que se adelanta | `AhorroPorProntoPago(tasa)` |
| 5 | **La reserva: la app propone, la persona adopta** | Proyección del año ÷ 12 es una **propuesta**; lo que la persona adopta es **suyo**. Dos cosas, dos casillas | `ReservaPropuestaPara(anio)` · `AdoptarReserva(monto, fecha)` |
| 6 | **Cuándo avisar sobre la reserva** | Si lo adoptado queda **más de 10 % por debajo** de lo proyectado; urgente **sólo si el faltante vence en el mes** | `FaltanteDeReserva(anio, umbral)` |

**Propuestos el 2026-09-08 por quien modela — ⚠️ sin firmar. Entran por parámetro y no viven como
constantes en el dominio:**

| # | El juicio | Propuesta | La pregunta que produce |
|---|---|---|---|
| **M1** | Un salto de más de **25 %** en un cobro **mensual**, mismo rubro, es **tarifa nueva** | 25 % | `VariacionDe(rubro, umbral, ruido)` → `CambioDeTarifa` |
| **M2** | Un salto de más de **25 %** en un cobro **trimestral** sobre base imponible es **avalúo nuevo** | 25 % | → `CambioDeAvaluo` |
| **M3** | Un cambio de menos del **2 %** es ruido: no se avisa | 2 % | → `SinSalto` |
| **M4** | **Un aumento no es urgente** — ya pasó; **lo que vence sí**. La clase decide, no el monto | por clase | `Variacion.Urgencia()` = ninguna |
| **M5** | Avisar **7 días** antes del vencimiento; **urgente el día** | 7 días | `VencimientoProximo.Urgencia(fecha, diasDeAviso)` |
| **M6** | Avisar **en enero** que pagar todo el año en Q1 tiene descuento, con el ahorro calculado | tasa del año por parámetro (2026: 3,78 %) | `AhorroPorProntoPago(tasa)` |
| **M7** | La tasa de mora, **sólo para proyectar**: diaria desde el último día del período | por parámetro (2026: ≈ 6,2 %, reconstruida) | `CostoDePagarTarde(fecha, tasa)` |
| **M8** | Un período que figura pagado **sin comprobante** se **dice**, no se tapa | se marca | `PeriodosPagadosSinComprobante(consulta)` |

> **La regla que los une, y ésa sí se sostiene sin firma:** la app **nombra** lo que ve — *esto es un
> avalúo, esto vence el viernes, esto no tiene comprobante*—; **no inventa** ni corrige lo que la
> municipalidad cobró. Es la misma frontera que el laboratorio traza con el analito.

---

# Parte 3 · Lo que la UI muestra

Se decide **al final**, y va a mostrar menos que lo que el modelo sabe. Pendiente.

Lo ya decidido: **no hay pantalla de *cuánto debo*, ni de *cuánto sería el año*, ni de *estoy
atrasado*.** El portal las contesta. La app muestra lo que el portal **no** puede: por qué pago más,
qué se me vence, cuánto apartar, qué quedó sin comprobante.

---

# Las preguntas que originaron todo esto

1. *"¿Cuánto he pagado este año?"* — la serie de comprobantes
2. *"¿Por qué este trimestre pago más?"* — **la variación, con su explicación**: tarifa o avalúo
3. *"¿Cuánto debo apartar mensualmente?"* — la reserva propuesta, y la adoptada
4. *"¿Qué cambió desde mi último pago?"* — la variación otra vez, desde el último comprobante
5. *"Recordatorios de vencimientos"* — **la ausencia**: qué vence y cuándo, que ninguna pantalla del portal tiene

---

# Lo que se rechazó

| Se descarta | Por qué |
|---|---|
| **Verificar los cálculos de la municipalidad** — la mora, el descuento, los montos | La app **recibe y alarma**. Las reglas reconstruidas de los recibos quedan sólo para proyectar, marcadas; nunca para contradecir lo cobrado. Decidido el 2026-09-03 |
| **Ser el portal de pagos** | La app no paga ni muestra lo que el portal ya muestra |
| **Deducir la mora de cobros y pagos** | La municipalidad es la autoridad sobre su mora; llega como hecho |

---

# Candidatos a cruce

| La municipalidad pone | El otro módulo pone | De dónde vendría |
|---|---|---|
| Lo que se paga por mes y por trimestre | qué lugar ocupa en el presupuesto; la reserva adoptada contra lo apartado de verdad | Presupuesto |
| El valor de la finca | el patrimonio | Presupuesto |

---

# Verificación

| Se dijo | Dónde se comprueba |
|---|---|
| Los rubros son instancias, no clases | No hay una clase por rubro. `Rubro` es un catálogo con cinco instancias; sus reglas viven en la cadencia y la ley |
| Cada cadencia lee `Periodo` distinto | `CobroMensual.Leer(2026, 4)` es abril y `CobroTrimestral.Leer(2026, 4)` es el cuarto trimestre. Hay una prueba con el mismo número leído dos veces |
| La mora se recibe, no se calcula | Ninguna clase del dominio calcula intereses a partir de fechas; `Intereses` es un campo que entra |
| La reserva propuesta y la adoptada son dos | `ReservaPropuesta` se recalcula y no se guarda; `ReservaDeclarada` es un comando con fecha. No hay `bool adoptada` |
| El aumento se explica según la ley del rubro | El mismo +80 % da `CambioDeTarifa` en basura y `CambioDeAvaluo` en bienes inmuebles. Hay una prueba con los dos |
| Los umbrales entran por parámetro | Ninguna constante de M1–M7 vive en el dominio |
| El dominio no lee el reloj | Ninguna clase referencia `DateTime.Now`. La fecha de corte entra por parámetro |
| Sin comprobante se dice | Un período pagado que no tiene recibo aparece marcado, no contado como historia |
| Hay una pregunta que ninguna pantalla contesta | El vencimiento próximo |

---

# Pendientes, con nombre

| # | Qué falta | Quién puede saberlo |
|---|---|---|
| 1 | **Firmar M1 a M8.** Hoy los propone quien modela desde seis recibos | Jorge — en la hoja de firmas de la [bitácora](BITACORA.md) |
| 2 | **El valor de la finca**, para que *"¿por qué subió mi IBI?"* tenga respuesta completa | La persona, con su declaración o su avalúo |
| 3 | **Si los pagos en ventanilla dejan comprobante** | La persona — sabe cómo paga |
