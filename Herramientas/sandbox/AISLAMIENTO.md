# Cómo encerrar la caja de la demo

Investigación abierta el 2026-09-21. La pregunta: **que el visitante no pueda salir a la red ni
llegar a la máquina física que lo contiene.**

Importa más que en un sandbox cualquiera, porque el servidor candidato (`10.10.0.30`) sostiene
11 vClusters, una VM libvirt y **no tiene IPMI**: si alguien lo tumba, no hay reboot remoto.

---

## Qué puede hacer el visitante

No es un usuario de una app: es alguien con un escritorio Linux y un navegador adentro. Puede
abrir una terminal si encuentra cómo, correr lo que quiera, y desde ahí:

| lo que intentaría | qué lo frena |
|---|---|
| Salir a internet (relay, minería, abuso desde tu IP) | sin interfaz de red |
| Escanear la LAN y tocar los vClusters | sin interfaz de red |
| Hablarle al servidor por la IP de la bridge | sin interfaz de red |
| Llenar la RAM y provocar un OOM en el servidor | `mem_limit`, `DOTNET_GCHeapHardLimit` |
| Fork bomb | `pids_limit` |
| **Escapar del contenedor al kernel del servidor** | **nada de lo anterior** ← el hueco real |

Las primeras cinco filas están resueltas. La sexta es la que decide dónde se puede poner esto.

---

## 1 · Red: cero interfaces, no "red interna"

**Lo que NO alcanza:** una red `internal: true` de Docker corta la salida a internet, pero el
servidor sigue siendo alcanzable en la IP de la bridge, y desde ahí se ve la LAN. Es media
medida.

**Lo que se hizo:** la caja corre con `network_mode: none` — le queda `lo` y nada más. La
pantalla sale por un **socket Unix** (`x11vnc -unixsock`) en un volumen compartido, y un
sidecar sin nada adentro (`websockify --unix-target`) lo sirve como WebSocket. Lo único que
cruza la frontera es un flujo de píxeles.

Efecto colateral bueno: el cartero tampoco puede salir, así que el expediente tiene que venir
sembrado de fábrica. Es más fiel de lo que parece — la cara nunca sale a la red por diseño.

## 2 · Kernel: el hueco que ningún flag de Docker tapa

Un contenedor **comparte el kernel** con el servidor. Un bug explotable en una syscall es root
en el servidor, y ahí `cap_drop: ALL` no ayuda. Tres formas de cerrarlo:

**gVisor (`runsc`).** Intercepta las syscalls en espacio de usuario: el proceso del visitante
nunca habla directo con el kernel del servidor. Es exactamente para esto y Google lo usa así.
El costo es que las cargas intensivas en syscalls se penalizan, y Xvfb + WebKit hacen muchas
(dibujado, memoria compartida). **Hay que medirlo antes de prometerlo**: puede quedar fluido o
puede quedar inusable, y no lo sabremos sin probar.

**Kata Containers.** Cada contenedor en una microVM con **su propio kernel**, por QEMU o
Firecracker, con interfaz de contenedor normal. Aislamiento de máquina virtual de verdad. El
rendimiento dentro de la VM es casi nativo, que es justo lo que le conviene a WebKit. Cuesta
~1 s de arranque y RAM fija por VM. En este servidor encaja bien porque **ya corre libvirt y
QEMU**.

**Firecracker directo.** Lo mismo sin la capa de Kata: microVMs de ~125 ms. Más control, más
trabajo — hay que armar la imagen de VM en vez de una imagen OCI.

→ **Si va en el servidor propio, Kata.** El aislamiento es de VM y no castiga el dibujado.

## 3 · Higiene, que va en cualquier caso

- `userns-remap`: root en el contenedor ≠ root en el servidor. **Lo más valioso de esta lista.**
- `cap_drop: ALL`, `no-new-privileges`, seccomp por defecto, usuario sin privilegios
- rootfs de sólo lectura, con `tmpfs` para lo poco que se escribe
- sin `/dev/kvm`, sin dispositivos, sin `--privileged`
- **jamás** montar `/var/run/docker.sock` — es entregar el servidor
- contenedor efímero: se destruye al terminar la sesión, nunca se reusa
- tope de sesiones concurrentes y cola, para que el total no dependa de cuánta gente llegue

---

## 4 · La mitigación más fuerte: que no esté en tu servidor

Todo lo de arriba baja la probabilidad de un escape. Nada la lleva a cero. Así que la pregunta
verdadera no es *cómo lo encierro*, sino **qué hay del otro lado si se escapa**.

Si del otro lado hay una VM alquilada y desechable, un escape deja a alguien dentro de una
máquina vacía que se puede destruir y recrear. Si del otro lado está `10.10.0.30`, deja a
alguien dentro del servidor que sostiene los doce stages, sin IPMI para reaccionar.

Eso degrada el problema de crítico a molesto, y por mucho menos esfuerzo que montar Kata.

### Opciones, verificadas el 2026-09-21

Todas son lo mismo: **un VPS KVM con Linux**, root propio, Docker adentro. Lo que cambia es el
precio y a cuántos milisegundos está de Costa Rica — y la latencia pesa porque esto es VNC:
cada clic espera un viaje de ida y vuelta. A ~50 ms se siente cómodo; a ~160 ms arrastrar una
ventana se siente pegajoso, y quien prueba no piensa "el servidor está lejos", piensa "esta app
es lenta".

| | qué da | costo | latencia CR | nota |
|---|---|---|---|---|
| **InterServer** · 4 slices | 8 GB, 160 GB, 8 TB | **$12/mes** | **~40–60 ms** (Dallas) | KVM, "strong isolation" — VM real, no OpenVZ. **La mejor relación de las tres.** Confirmar cuántos vCPU trae cada slice: su tabla no los muestra. |
| **Hetzner** EU | CX32, 4 vCPU / 8 GB, 20 TB | ~€7/mes | ~140–170 ms | Lo más barato, pero Europa se siente en VNC |
| **Hetzner** US (Ashburn) | CPX31, 4 vCPU / 8 GB, 1–8 TB | ~$25/mes | ~70–90 ms | Subió ~3× el 15-jun-2026; en US sólo hay CPX y CCX |
| **Oracle Always Free** | 2 OCPU / 12 GB **ARM** | gratis | ~40 ms si toca Querétaro | Recortado de 4/24 el 15-jun-2026 sin anuncio; desde el 18-ago terminan lo que exceda. Falta de capacidad ARM al crear y registros rechazados. Y ARM apila una segunda incógnita sobre la de Linux. |
| Google Cloud free | e2-micro, 1 GB | gratis | — | 1 GB no alcanza para WebKit |
| AWS / Azure free | 1 GB, **12 meses** | gratis y se acaba | — | chico y con vencimiento |
| GitHub Codespaces | 120 core-h/mes | gratis | — | Requiere cuenta **del visitante**; sirve para técnicos, no para tráfico anónimo |

**Antes de pagar cualquiera de estos, tres preguntas al proveedor:** cuántos vCPU trae el plan,
si el precio es el de renovación o una promoción del primer mes, y **si permiten exponer un
escritorio interactivo a visitantes anónimos** — algunos lo consideran servicio compartido y lo
prohíben en sus términos. Eso último conviene preguntarlo por ticket antes, no después.

**Si va a ARM** (Oracle, o Hetzner CAX), hay que publicar con `-r linux-arm64`:
`Photino.Native` trae x64 y arm64, así que no hay bloqueo, pero es una variante más que probar.

### Recomendación

Para una demo pública, **Hetzner a ~5 €/mes**. Cuesta menos que una hora de discutirlo, la
capacidad está garantizada, y saca del servidor de los stages un riesgo que no tiene por qué
estar ahí. En una VM desechable se puede correr Docker normal sin Kata, porque el "servidor"
ya es reemplazable.

Oracle Always Free es tentador y alcanza técnicamente, pero depende de conseguir capacidad ARM
y de que Oracle no vuelva a recortar sin avisar — que es exactamente lo que acaba de hacer.

---

## 5 · La variante de máquina propia (guardado 2026-09-21, no descartado)

Si en vez de un VPS se usa **una máquina física nuestra dedicada**, hay tres capas para cerrar y
la tercera es la que se olvida.

**La VM, nivel razonable.** Una sola interfaz con egreso filtrado que permita **únicamente 443
hacia los rangos de Cloudflare** — lo que necesita `cloudflared` — y nada más: ni DNS interno,
ni la LAN, ni el resto de internet. nftables en la VM más reglas en el host de libvirt.

**La VM, nivel máximo: sin stack IP.** El túnel corre en el host, no en la VM, y le habla por
**virtio-vsock** — un canal punto a punto entre hipervisor y huésped que no es IP, no tiene
rutas y no se puede enrutar a ninguna parte. `socat` puentea el vsock al socket del VNC. La VM
no tiene por dónde intentar salir: no hay interfaz que configurar ni gateway que descubrir.

**La máquina física.** Si está en el mismo switch que `10.10.0.x`, todo lo anterior protege
contra un escape de contenedor pero no contra un escape de hipervisor: quien llegue al host
está en la LAN, con los vClusters a un `nmap` de distancia. Hace falta **VLAN propia** o su
propio router, con una regla que bloquee todo tráfico hacia la red interna. Desde la LAN, la
máquina del sandbox tiene que ser tan ajena como un visitante.

**Lo que ninguna regla de red arregla:** el escape de hipervisor (raro, pero existe — se mitiga
con que esa máquina no tenga nada de valor), y que **el enlace es compartido**: veinte sesiones
de VNC son 2–4 Mbps sostenidos de subida, del mismo caño que usa la gente para trabajar. La
reputación de la IP no corre riesgo si el egreso está cerrado de verdad, porque el sandbox no
puede generar tráfico hacia afuera.

**Cuándo conviene:** cuando el hardware ya está, se quiere cero costo recurrente y control del
hipervisor y de la VLAN. El aislamiento alcanzable es mejor que en un VPS. La contra es que hay
una ruta física entre el sandbox y la red de la empresa, y en un VPS esa ruta no existe.

## Lo que falta medir

- **gVisor con Xvfb + WebKit**: ¿queda usable? Es el único dato que decide entre gVisor y Kata.
- **Arranque en frío** de una caja: si son más de 3 s, hace falta un pool tibio.
- **RAM real por sesión** con el techo puesto en 1 GB: si WebKit no entra, hay que subirlo y
  eso cambia cuántas sesiones concurrentes caben.

Sources:
- [Oracle Quietly Halves Free Tier Ampere A1 Compute Limits — InfoQ](https://www.infoq.com/news/2026/07/oracle-cloud-free-tier-limits/)
- [Oracle Cloud free tier 2026: 4 OCPU/24GB cut to 2 OCPU/12GB — TerminalBytes](https://terminalbytes.com/oracle-cloud-free-tier-changes-2026/)
- [Breaking down the free tier of Oracle Cloud Infrastructure — Fullmetalbrackets](https://fullmetalbrackets.com/blog/oci-free-tier-breakdown)
