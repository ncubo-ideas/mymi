# D4 · Los servidores del proveedor

Lo que la institución expone. Va en contenedores porque **simula algo remoto** — y ese corte no es de
despliegue, es del diseño.

---

## El reparto, y por qué el corte está ahí

```
DOCKER  (el proveedor, que está "afuera")     LA MÁQUINA  (lo que es de la persona)
────────────────────────────────────────      ──────────────────────────────────────
URL1  :8080   la página de descarga           el asistente
URL2  :8081   el endpoint de eventos            │ MCP sobre stdio
                                              el Host
                                                │
                                              el módulo  →  el journal
```

URL1 y URL2 son del supermercado. El Host, el módulo y el journal son de la persona. **Ese es el punto del
diseño**, y tenerlo reflejado en dónde corre cada cosa hace la demo más honesta.

## Levantarlos

```bash
cd "C:/Users/jon32/Documents/MCP Hola Mundo/Facturas"
```

```bash
docker compose up -d
```

```bash
curl -s http://localhost:8081/salud
```

---

## Los dos nodos

| | qué es |
|---|---|
| **URL1** `:8080` | nginx sirviendo la app publicada. `8443` https con certificado self-signed |
| **URL2** `:8081` | el GET de eventos, el token de device y su rotación, **y el señuelo que devuelve 403** |

### URL1 · la página de descarga

De acá la persona baja e instala. Nada más que archivos estáticos: **el proveedor no hospeda la aplicación
corriendo, la entrega.**

Un detalle de nginx que vale: sirve los archivos **precomprimidos** que el publish genera, así que lo que
viaja es la versión gz sin recomprimir en cada request.

Lo que URL1 entrega creció: además de la app del andamio, ahora sirve el **instalador MSI del comercio** —que
trae el PWA, el `Comercio.Shell` y el módulo `compras.gmod`—. Y ese instalador cierra el lazo hacia acá: si al
correr **no encuentra la marca de descubrimiento de Gnothi**, es él quien **manda de vuelta a esta página**.
El detalle del MSI y de la marca vive en `C7`.

### URL2 · lo único que el proveedor le expone a la app

**Un solo verbo de lectura**, a propósito:

```
GET /{recurso}/{id}/events?after=N
```

Devuelve solo los eventos posteriores a `N`, que es el checkpoint que la app tiene journalizado. Más los dos
endpoints de credencial: emitir un token de device y rotarlo.

## El señuelo, que es la pieza importante

URL2 tiene un endpoint que **exige un scope de perfil que ningún token de device tiene**, así que responde
**403** con la credencial de la app.

> **Existe para demostrar el invariante, no para servir a nadie.**

Eso convierte *«la app no es cliente de los servicios generales del proveedor»* de promesa en **respuesta
HTTP**. Si algún día alguien agregara una llamada a eso desde el mecanismo de refresh, el proveedor la
rechazaría.

Y el mecanismo es estructural, no un chequeo de ruta: el scope del token es `<recurso>.events.read` y nada
más.

## El control del mock, y por qué existe aparte

Dos endpoints que **no son del proveedor real**, y están marcados como tales:

**Agregar una factura.** Simula que el proveedor le registró algo nuevo. No pide el token de la app: es una
acción **del proveedor**, así que la dispara quien opera el mock.

**Expirar los access tokens dejando vivos los refresh.** Es el caso que el diseño declara normal —access
corto, refresh durable y rotatorio— y **sin una forma de provocarlo no hay manera de demostrar que la app
rota sola** en vez de pedir una credencial nueva.

---

## El inyector de eventos

```bash
dotnet run --project "C:\Users\jon32\Documents\MCP Hola Mundo\Facturas\InyectorDeEventosDemo" -- listar
```

```bash
dotnet run --project "C:\Users\jon32\Documents\MCP Hola Mundo\Facturas\InyectorDeEventosDemo" -- inyectar --numero TQ-9001 --linea "ACE-1L,Aceite de girasol 1L,3,2890.00"
```

Comandos: `listar`, `historia`, `inyectar`, `aleatoria`.

**Existe aparte, y no es un detalle:** los datos del demo **se orquestan como test cases**. Con una factura
aleatoria el total no se puede predecir, y **una demo se cuenta con números elegidos**.

De ahí que la traza tenga cifras exactas: 3 × 2.890,00 = 8.670,00, y 19.321,50 + 8.670,00 = **27.991,50**.
Si el número no es predecible, no se puede afirmar que el ciclo cerró.

## El estado es en memoria, y salió a favor

El mock no persiste. Al reiniciar el contenedor vuelve a los eventos sembrados.

Eso resultó ser una **demostración accidental de la tesis**: la máquina se reinició, el proveedor perdió la
factura nueva, y **el journal de la persona la conservó**. El asistente la leyó de ahí.

> **El proveedor olvidó, la persona no.**

No hubo que actuarlo. Pasó solo.

---

## Un detalle que muerde, si vas a usar la app del andamio

**El literal `localhost` importa.** El *secure context* se decide por **el string del host** — medido, una IP
loopback con otro nombre da `isSecureContext=false`: sin selector de carpeta y sin service worker, **siendo el
mismo socket**.

## Y los certificados

El listener https del `8443` necesita un certificado, que se genera una vez con el script que está junto a la
página de descarga.

**No van en ningún paquete de entrega** — no se reparten llaves privadas, ni de prueba. Y para la demo el
`8080` alcanza: `http://localhost:8080` **es** secure context.

---

## Cómo se agrega otro proveedor

Copiar la forma, y llenar **cuatro costuras** del mecanismo compartido:

1. el assembly de dominio
2. el routing de eventos: señal cruda → mensaje tipado → un comando
3. el juego de URLs con su recurso y scope de token
4. su superficie de consultas

**El bucle de eventos, el token, la rotación y el checkpoint no se vuelven a escribir.** Eso es lo que
justifica que el mecanismo viva afuera de cualquier negocio en particular.

Con una honestidad al respecto: **la abstracción solo queda validada cuando exista el segundo negocio**, así
que hoy está deliberadamente delgada. No hay generalidad especulativa.

Con un matiz nuevo: ya existe un segundo módulo que consume eventos —**compras**, el de Super La Guaria—, así
que el mecanismo compartido corrió dos veces. Pero eso **todavía no valida** la abstracción del «segundo
negocio»: bien puede ser el mismo supermercado reimplementado como cargo, no un rubro genuinamente distinto.
Consumir eventos lo hace; cerrar la abstracción, no —todavía.
