# La caja de la demo — Mymi en un contenedor Linux, servido al navegador

Un contenedor efímero con un escritorio Linux mínimo, la ventana de **Mymi** abierta y el
módulo de Delfos ya dado de alta. Su pantalla llega al navegador por VNC sobre WebSocket, así
que se embebe en un `<iframe>` del sitio y **el visitante no baja nada**.

Reemplaza a la idea de entregar una VM: sin licencias, arranca en segundos, y en el mismo
hierro entran decenas de sesiones porque son contenedores y no máquinas virtuales.

---

## ⚠️ Lo que todavía no está probado

**Mymi nunca corrió en Linux.** El `.csproj` compila para `net9.0` fuera de Windows y la
prueba manual cubre Windows y macOS, pero en todo el repo no hay una sola mención de
`webkit2gtk`. Este Dockerfile es el primer intento.

Si la ventana no abre, el primer sospechoso es **la versión de WebKitGTK contra la que enlaza
`Photino.Native` 4.0.22**. La imagen instala `libwebkit2gtk-4.1-0`; si resulta que pide la
4.0, hay que bajar la base a `bullseye` o traer el paquete viejo.

---

## 1 · Publicar (afuera del contenedor)

La imagen no compila a propósito: `NuGet.config` apunta al feed privado de Azure donde vive
`Ncubo.Puppeteer`, y un `restore` adentro exigiría meterle un PAT. Se publica donde el feed ya
está autenticado y la imagen sólo copia binarios.

Desde la raíz del repo:

```bash
dotnet publish Gnothi.Desktop -c Release -r linux-x64 --self-contained false -o Herramientas/sandbox/publicado/mymi
dotnet publish Gnothi.Host    -c Release -r linux-x64 --self-contained false -o Herramientas/sandbox/publicado/host
```

Funciona igual desde Windows: es cross-publish, no necesita una máquina Linux. El `-r
linux-x64` es lo que deja en `runtimes/` la nativa de Photino correcta y descarta las de
Windows y macOS.

## 2 · Poner el paquete

```bash
cp Gnothi.Mobile/bundles/hibrida/examenes.gmod Herramientas/sandbox/paquetes/
```

Es el bundle **híbrido** (175 KB): `web/` con la cara del laboratorio, `dominio/` con tres
DLL, y el motor lo presta el Host. Si no está armado, lo arma
`Gnothi.Mobile/armar-bundle.ps1 -Modulo examenes -Forma hibrida`.

## 3 · Construir y correr una caja

```bash
cd Herramientas/sandbox && docker compose up --build
```

Y abrir `http://127.0.0.1:6080/vnc.html?autoconnect=1&resize=remote`.

---

## Qué hay adentro

| | |
|---|---|
| Base | `dotnet/runtime:9.0-bookworm-slim` |
| Display | `Xvfb :99` a 1440×900, con `openbox` |
| Pantalla al navegador | `x11vnc` en loopback → `websockify` en 6080 |
| Usuario | `visita`, sin privilegios, `cap_drop: ALL` |
| Módulo | alta en el arranque, no en el build |
| Vida | 15 minutos, y se muere sola |

**El alta corre en el arranque y no en el build**, y eso es deliberado:
`installation.json` es el archivo de la persona. Cada caja es una persona distinta y empieza
con su propia alta, con su carpeta concedida por el Host y el consentimiento en su estado
inicial. Hornearlo en la imagen sería decidir por ella de fábrica.

## La contención no es opcional

El host de los stages es la prueba: seis pods sin `limits.memory` se comieron 71 GB, porque el
GC de .NET toma cuanta RAM ve. Una caja pública sin techo es lo mismo pero con un desconocido
del otro lado. De ahí `mem_limit: 1g`, `cpus: 1.0`, `pids_limit: 300` y
`DOTNET_GCHeapHardLimit`.

Y **sin red hacia afuera** (`internal: true`): la cara no sale a internet — no puede y no le
hace falta — y el único que lo haría es el cartero contra su mock. Una caja pública con egress
libre es un relay abierto saliendo por la IP del servidor.

---

## Publicarlo en el sitio

El sitio es `ncubo.com` y el servidor sale por otro dominio. Un `<iframe>` cross-domain
funciona; hay tres cosas que configurar y una que evitar:

1. **`frame-ancestors`, no `X-Frame-Options`.** En el ingress del sandbox:
   `Content-Security-Policy: frame-ancestors https://ncubo.com https://www.ncubo.com`, y
   **quitar** `X-Frame-Options` — no admite listas y en varios navegadores gana sobre el CSP.
2. **Cookies `SameSite=None; Secure`** en el gateway de sesiones. Es la causa número uno de
   "en pestaña nueva anda, en el iframe no".
3. **Un solo hostname, sesiones por path** — `https://{alias}-demo.<dominio>/s/<token>`.
   El Universal SSL de Cloudflare cubre un solo nivel de wildcard (de ahí el formato DASH de
   los stages): un hostname por sesión te deja sin certificado.
4. **Evitar WebRTC.** El proxy de Cloudflare no transporta UDP, y el router sólo natea 80 y
   443. Por eso la imagen usa VNC sobre WSS y no Selkies.

Lo más corto es un **Cloudflare Tunnel** desde el servidor: publica el hostname sin depender
del NAT ni abrir puertos, y no toca el ingress de los stages, que ya funciona.

## Todavía falta

- **El broker**: una caja por visitante, token de un solo uso, cola y tope de sesiones
  concurrentes. Hoy el compose levanta una sola caja para probar la imagen.
- **Los mocks del cartero**: `Examenes/docker-compose.yml` levanta el publicador en `:8095`.
  El manifiesto del bundle lo apunta ahí, así que hay que unir las dos redes o dejar el
  expediente sembrado de fábrica.
- **Sembrar el journal**: `demo-datos.ps1` es PowerShell. En Linux hay que llamar a
  `Examenes.Consola refrescar` o traer `pwsh`.
- **Un cliente MCP en la caja**: Claude Desktop y ChatGPT Desktop tienen build para Linux y
  ambos hablan stdio, pero piden cuenta. Para tráfico anónimo conviene el **MCP Inspector**,
  que muestra `tools/list` y `tools/call` sin credenciales.

## Nota de infraestructura

En el host de los stages (`10.10.0.30`) **Docker está instalado pero el daemon no corre** —
ese host usa containerd para los vClusters. Arrancar `dockerd` ahí crea reglas de iptables y
una bridge en una máquina que sostiene 11 vClusters y no tiene IPMI, así que conviene decidirlo
a propósito y no de paso. Alternativas: construir en otra máquina y empujar la imagen, o
construir con buildkit dentro de Kubernetes.
