# Lo que hay que entregarle a quien publica la descarga

Este archivo es el paquete de entrega: qué mandar, qué poner en la página, qué pedirle al
hosting y **qué hay que decidir antes de publicar**. No incluye subir nada.

---

## 1 · Los archivos

| archivo | peso | qué es |
|---|---|---|
| `mymi-demo.ova` | 1250 MB | **la que se publica**: los cinco paquetes sin dar de alta, en español |
| `mymi-demo-cargada.ova` | 1341 MB | opcional: ya con las instituciones puestas y sus datos, en inglés |
| `COMO-CORRERLA.md` | 8 KB | las instrucciones, para convertir a página o publicar al lado |

Hoy están en `C:\Users\jon32\Downloads\` y en `\\wsl.localhost\Ubuntu\opt\vm\`.

**Con una alcanza.** Publicar las dos obliga a explicar la diferencia en la página, y la
diferencia es sutil (una se salta el alta, que es justo lo que vale mostrar). Si hay que elegir
una sola, es la **limpia**.

## 2 · Las sumas de verificación

Van **publicadas en la página**, no sólo en el correo: sirven para que quien baja compruebe que
el archivo llegó entero, y 1,2 GB por HTTP se cortan más de lo que uno cree.

```
SHA256
281854f8bf2a5c433d6d363f03e8787e1838f70a75a16b7c01e82c6f76298020  mymi-demo.ova
39b415760d0dde8b44c2ea0e6c9046b50297a665fa6ac80814b6d112a33bb71b  mymi-demo-cargada.ova

MD5
e4ad9666b060f3b20b6daec0400e3b80  mymi-demo.ova
c13b4959285c4074535ec0caaf0b2f76  mymi-demo-cargada.ova
```

Conviene subir también un `mymi-demo.ova.sha256` al lado del archivo, con esa línea adentro.

## 3 · Lo que hay que pedirle al hosting

- **No servirlo desde el servidor de los stages.** Son 1,2 GB por descarga; con diez personas
  bajándola a la vez se come el ancho de banda de todo lo demás que vive ahí.
- **Un objeto en almacenamiento, detrás de CDN.** Cloudflare R2, S3 o equivalente. La descarga
  directa desde un origen propio es la forma más fácil de tumbarlo.
- **`Content-Type: application/octet-stream`** y **`Content-Disposition: attachment`**, o algunos
  navegadores intentan abrir el `.ova` en vez de bajarlo.
- **Que acepte rango (`Accept-Ranges: bytes`)**, para que una descarga cortada se reanude en vez
  de empezar de cero.
- **Sin login.** Si hay que pedir datos, que sea un formulario antes del enlace, no autenticación
  sobre el archivo: rompe los gestores de descarga.

## 4 · Texto para la página

Para usar tal cual o recortar.

> ### Mymi — máquina virtual de prueba
>
> Una máquina virtual Linux con Mymi instalado, cinco instituciones de ejemplo y un asistente
> conectado. Se importa en VirtualBox o VMware y abre sola: no hay que instalar ni configurar
> nada.
>
> Adentro hay un laboratorio, un supermercado, un concesionario, una municipalidad y una estación
> de servicio. Cada uno entrega su paquete, y Mymi lo instala en una carpeta del aparato. Los
> datos no salen de la máquina: cada institución habla con su propio servicio, que corre adentro.
>
> **Descargar** · `mymi-demo.ova` · 1,2 GB
> SHA256 `281854f8…298020`
>
> **Hace falta:** VirtualBox o VMware · 4 GB de RAM libres · ~4 GB de disco · un equipo x86_64
> con la virtualización activada. **No corre en Mac con chip Apple** (M1/M2/M3).
>
> **Usuario `demo`, contraseña `mymi`.**
>
> El asistente que viene instalado pide tu propia cuenta: la imagen no trae ninguna sesión.

## 5 · Lo que hay que decidir ANTES de publicar

### ✅ Claude Desktop ya NO va adentro — resuelto el 2026-10-12

**Decisión tomada: los términos de Anthropic no cubren redistribuirlo**, así que la imagen sale
sin él. Una cosa es instalarlo en tu máquina y otra meterlo en algo que se publica.

La demo **pierde un paso, no una capacidad**. El `READ-ME.txt` del escritorio le da al visitante
los dos pasos, y ninguno es raro:

```bash
sudo apt update && sudo apt install claude-desktop     # el repo de Anthropic ya está registrado
/opt/mymi/host/Gnothi.Host --install /opt/mymi/Modulos  # apuntarlo a Mymi
```

**Lo que sí queda adentro es el `claude_desktop_config.json` con el conector** — 300 bytes que
escribió `Gnothi.Host --install`, configuración nuestra y no software de Anthropic. El efecto es
que quien instale Claude lo encuentra ya apuntando a Mymi, y el segundo paso sólo lo confirma.

**Y el repositorio apt de Anthropic queda registrado** en `/etc/apt/sources.list.d/`: es una URL
en un archivo de texto y su clave pública, no software. Si se prefiere que la imagen no lo traiga,
quitarlo es una línea — y entonces el visitante tiene que agregarlo él, que son cuatro comandos
más en vez de uno.

Ubuntu, en cambio, **sí se puede redistribuir** — es la razón por la que esta VM existe y la
objeción que mataba la versión de Windows.

### La página es la primera impresión

Dos cosas que conviene decir ahí y no dentro de la VM, porque si se descubren después se leen
como fallas:

- **La cuenta del asistente es de quien la usa.** La imagen no trae sesión, a propósito.
- **Los datos son de ejemplo y viven en la máquina virtual.** No hay nada real de nadie, y nada
  sale de ahí.

## 6 · Lo que NO hay que mandar

- Las imágenes de trabajo: `nueva.qcow2`, `demo.qcow2` (14 GB cada una, y la segunda es el origen
  de la cargada).
- `provision.sh`, `faro-aportes.sh` y el `README.md` de construcción: son para armar la imagen,
  no para correrla. Si se publican, confunden.
