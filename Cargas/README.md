# Cargas — el negocio de la gasolinera

El cuarto negocio de Gnothi: las cargas de combustible de Sonia. La marca provisional
es **Estaciones Faro** (pendiente de la especificación de Geaninna) y el brief vigente
es [`docs/cargas/01-brief-dominio-cargas-v2.md`](../docs/cargas/01-brief-dominio-cargas-v2.md).

## La tesis

**Una carga tiene dos procedencias en el mismo hecho.** La estación sabe litros, precio,
tipo, fecha, surtidor y placa; solo Sonia sabe el kilometraje y si el tanque quedó
lleno. Y el rendimiento **no se puede calcular sin las dos partes**: sin el aporte de
ella, lo que tiene la estación es una factura; con él, es conocimiento sobre su carro.

De ahí lo que este negocio estrena respecto de `Examenes/`:

- **Aportes locales.** `AnotarOdometro`, `ConfirmarTanque`, `DeclararTanque`,
  `CompletarFicha` y `AdjuntarFoto` son comandos que no vienen de URL2. Por eso
  `manifiesto.json` declara `journal.reconstruible = false`: borrar la carpeta y
  refrescar recupera lo de la institución, **los aportes no**.
- **El surtidor como instrumento** (§6.3 del brief): dueño del precio y de la forma de
  corte, que **infiere** el tanque lleno para que Sonia solo confirme.
- **Cuatro dimensiones tipadas** (volumen, distancia, dinero, rendimiento) con el
  veredicto juzgado **una sola vez en canónico** (km/L): cambiar la unidad de
  presentación invierte la flecha, nunca el juicio.
- **El tramo como proyección**: el rendimiento no se calcula por carga sino entre dos
  tanques llenos, y un kilometraje agregado tarde redefine tramos ya vistos.

## Cómo se valida

**Por consola.** Es el ciclo corto: mide el dominio sin Docker ni browser.

```
dotnet run --project Cargas/App/Cargas.Consola -- comprobar
```

122 comprobaciones del dominio puro, con el guion del brief §13 y los ocho veredictos.
Un cambio en el dominio no está listo hasta que pasan.

```
dotnet run --project Cargas/App/Cargas.Consola -- comprobar-traspaso
```

El traspaso de las anotaciones a Gnothi (especificación 13 §5), con dos actores reales en
disco: la app suelta arma el sobre —solo lo que dijo la persona, sin credencial ni hechos de
la estación— y el módulo hospedado lo recibe. Comprueba que las anotaciones **esperan** a sus
cargas cuando todavía no llegaron, que con las cargas se asientan enteras y dan la misma
lectura del rendimiento, que repetir el traspaso **no deja entrada**, y que un sobre ajeno o
roto se dice y no se asienta. Imprime cuánto pesa el sobre del guion (~9.7 KB; ~21 KB escapado
para URL), que es por lo que en Cargas va por la hoja de compartir y no por el enlace.

```
dotnet run --project Cargas/App/Cargas.Consola -- medir [carpeta]
```

El ciclo completo con actor y journal: aplica el guion por la misma ruta del wire
(señal → ruta → comando con `Check`), los aportes de Sonia, y mide los criterios
(idempotencia del evento, rechazo del kilometraje que retrocede, veredicto `empeoro`).

```
dotnet run --project Cargas/App/Cargas.Consola -- estado [carpeta]
```

En un proceso nuevo: si los tramos siguen, el replay rehidrató.

## URL2 y el refresh

```
dotnet run --project Cargas/EndPointDeEventos          # URL2 en :8111, ya sembrado
dotnet run --project Cargas/App/Cargas.Consola -- refrescar [carpeta]
dotnet run --project Cargas/App/Cargas.Consola -- sembrar-aportes [carpeta]
```

`refrescar` consigue la credencial solo (device key → token corto → refresh rotatorio)
y aplica los 19 eventos; `sembrar-aportes` agrega lo que la estación no sabe. El
inyector opera el mock:

```
dotnet run --project Cargas/InyectorDeEventosDemo -- listar
dotnet run --project Cargas/InyectorDeEventosDemo -- historia
dotnet run --project Cargas/InyectorDeEventosDemo -- senuelo    # /perfil → 403 con token válido
dotnet run --project Cargas/InyectorDeEventosDemo -- inyectar --litros 42 --precio 668
```

## El guion del demo (§13 del brief, v2)

Veintiocho cargas de la placa `BQR-482` —la del canon, acordada con el demo del
concesionario como llave del cruce—, con fechas literales.

**El odómetro es el canon de Sonia**: 49.850 en diciembre y 60.962 en agosto, con el
quiebre de enero —850 km/mes antes del ascenso, 1.400 después—. De ahí sale todo lo
demás: los litros no son libres, porque `litros = distancia / rendimiento`, y la fecha
tampoco, porque sale de invertir la curva del odómetro. Por eso las cargas se aprietan
solas de una cada 17 días a una cada 8, y **ese apretón es el ascenso**: la gasolinera
lo cuenta sin que nadie lo explique, que es lo que ningún otro módulo puede hacer.

Las historias que cierran: la línea base de 12.6 km/L sobre trece tramos, la parcial
preseteada que la inferencia lee como «no fue lleno», la caída 12.6 → 9.4 contra la
propia base (−25%), el tramo no comparable del surtidor B-1 —10 L, y los únicos ₡6.520
del guion que no alcanzan para un boleto—, el kilometraje faltante de la carga del 17/08
que bloquea dos tramos finos y deja el largo de 743 km, la autonomía estimada de ~504 km,
y 998 puntos que cruzan los dos umbrales del programa: plata en febrero y oro en julio.

## El MCP

```
dotnet build Cargas/App/Cargas.Mcp
```

Cinco tools solo-query (`mis_cargas`, `autonomia_estimada`, `rendimiento_vs_habitual`,
`lo_que_falta_para_calcular`, `dias_hasta_la_proxima_carga`), registradas en el
`.mcp.json` de la raíz con `dotnet <dll>` — nunca `dotnet run`, que escribe a stdout y
con stdio stdout **es** el canal JSON-RPC. Lee la carpeta con `SoloLectura` (`OnHydrated`
vacío): cada pregunta de la IA rehidrata y no escribe jamás.

## El WASM y la página

```
MSBuildEnableWorkloadResolver=false dotnet publish Cargas/App/Cargas.Wasm -c Release \
  -o Cargas/PaginaDeDescarga/publicado -p:PublishTrimmed=false
cd Cargas && ./PaginaDeDescarga/generar-certificado.sh && docker compose up -d
```

Puertos de este negocio (⚠️ propuesta, pendiente de acuerdo de equipo):

| Servicio | Puerto |
|---|---|
| PaginaDeDescarga http | 8110 |
| PaginaDeDescarga https | 8473 |
| EndPointDeEventos | 8111 |

Al verificar por HTTP, **mirá el contenido, no el status code**: el `try_files` de
nginx devuelve 200 sirviendo otra cosa.
