# Cómo correr la VM de Mymi

Una máquina virtual Linux con **Mymi**, los cinco paquetes de las instituciones y sus
publicadores corriendo adentro. No hace falta instalar nada del proyecto: se importa y se abre.

Este documento es para **quien la corre**. Para quien la construye está [`README.md`](README.md).

---

## Cuál de las dos

| | peso | qué trae | para qué |
|---|---|---|---|
| **`mymi-demo.ova`** | 1250 MB | los cinco paquetes en `~/Entregas`, **sin dar de alta** · en español | **la que se entrega** |
| **`mymi-demo-cargada.ova`** | 1042 MB | las cinco instituciones ya de alta y **con sus registros llenos**, Faro incluido con sus kilometrajes anotados · en inglés, con un `READ-ME.txt` en el escritorio | mostrarla sin esperar |

**Elegí la limpia salvo que tengas prisa**, y no es por tamaño: **el alta es la demostración.**
Es la pantalla donde se ve qué declara el paquete, qué va a poder contestar, en qué carpeta va a
vivir el registro y con quién va a hablar. La versión cargada se salta justo eso.

Verificá la descarga antes de importarla:

```
mymi-demo.ova          md5  e4ad9666b060f3b20b6daec0400e3b80
mymi-demo-cargada.ova  md5  c13b4959285c4074535ec0caaf0b2f76
```

```bash
md5sum mymi-demo.ova
```

```bash
certutil -hashfile mymi-demo.ova MD5
```

## Lo que hace falta

- **Virtualización por hardware encendida** en la BIOS/UEFI (VT-x en Intel, AMD-V en AMD). Sin
  eso arranca, pero por emulación: lentísimo.
- **4 GB de RAM** para la VM, más lo que use tu sistema.
- **~4 GB de disco** libres después de importar.
- **x86_64.** Un Mac con Apple Silicon **no puede correr esta imagen** — ver el final.

---

## VirtualBox — la más universal

Es la que conviene si no tenés preferencia: gratis, en Windows, Linux y Mac Intel.

1. Descargar de [virtualbox.org](https://www.virtualbox.org/wiki/Downloads).
2. **Archivo → Importar servicio virtualizado…**, elegir el `.ova`, Siguiente, Importar.
3. Arrancar la máquina.

O desde la línea de comandos:

```bash
VBoxManage import mymi-demo.ova
```

```bash
VBoxManage startvm "Mymi demo"
```

**Si la ventana abre enorme o el escritorio no la sigue:** a la imagen le faltan las *Guest
Additions*, que son las que hacen que el escritorio se adapte al tamaño de la ventana. Mientras
no estén, se ajusta desde adentro:

```bash
xrandr -s 1600x900
```

**Y un aviso para Windows:** VirtualBox y WSL2/Hyper-V se disputan el hipervisor. Las versiones
recientes conviven, pero si VirtualBox se queja de VT-x o todo va muy lento, es eso.

## VMware Workstation / Player / Fusion

Importa el mismo `.ova`: **Abrir → elegir el archivo**. Puede advertir que el OVF no cumple la
especificación estricta; **Reintentar** lo acepta y la máquina arranca igual.

## QEMU / KVM — en Linux

El camino más directo si ya usás virsh o qemu. El `.ova` es un tar: adentro está el disco.

```bash
tar -xf mymi-demo.ova && qemu-img convert -f vmdk -O qcow2 mymi-demo.vmdk mymi-demo.qcow2
```

```bash
qemu-system-x86_64 -enable-kvm -m 4096 -smp 2 -drive file=mymi-demo.qcow2,if=virtio,format=qcow2 -netdev user,id=n0 -device virtio-net,netdev=n0 -device virtio-vga,xres=1600,yres=900 -usb -device usb-tablet -display gtk,zoom-to-fit=on
```

Las dos banderas que parecen de adorno y no lo son:

- **`-device usb-tablet`**. Sin tablet el mouse es *relativo*: el cursor de adentro se
  desincroniza del de afuera y los clics aterrizan en otro lado. El síntoma no es «no responde»,
  es **«hago clic en el botón y no pasa nada»**, que se diagnostica fatal.
- **`xres`/`yres` sobre `virtio-vga`**. Sin eso el guest toma el modo más grande que la tarjeta
  ofrece —con virtio, enorme— y la ventana no entra en la pantalla.

## Windows sin instalar un hipervisor — WSL2 + noVNC

Si ya tenés WSL2 y no querés instalar VirtualBox, se puede correr adentro de WSL y mirarla en el
navegador. Es lo que usamos para desarrollar esta imagen.

```bash
wsl -u root -- bash -lc 'apt-get install -y qemu-system-x86 novnc websockify'
```

Copiar el disco a WSL, convertirlo como en la sección anterior, y después:

```bash
qemu-system-x86_64 -enable-kvm -m 4096 -smp 2 -drive file=mymi-demo.qcow2,if=virtio,format=qcow2 -netdev user,id=n0 -device virtio-net,netdev=n0 -vga virtio -usb -device usb-tablet -display none -vnc :7 -daemonize
```

```bash
websockify --web /usr/share/novnc 0.0.0.0:6080 localhost:5907 &
```

Y abrir en el navegador de Windows:

```
http://localhost:6080/vnc.html?autoconnect=1&resize=scale
```

**Por qué el navegador y no una ventana.** En WSLg, `-display gtk` **no se ve**: qemu arranca sin
un solo error y no hay ventana en la barra de tareas. Está probado con Wayland, con
`GDK_BACKEND=x11`, con `setsid`, con el `XDG_RUNTIME_DIR` de WSLg y a pantalla completa, y
ninguna lo arregla — mientras que `xeyes` lanzado igual **sí** se ve, así que no es el canal
gráfico. Es GTK dentro de qemu sobre WSLg. El VNC no tiene ese problema, y además escala solo a
la ventana del navegador.

## Mac con Apple Silicon

**No corre esta imagen.** El disco es x86_64 y los M1/M2/M3 son arm64: UTM o Parallels la
emularían, y una demo gráfica emulada no sirve para mostrar nada.

Las salidas son dos: correrla en otra máquina, o construir una imagen arm64 — `provision.sh`
publica con `-r linux-arm64` y `Photino.Native` trae arm64, pero es una variante que **no está
probada**.

---

## Adentro

**Usuario `demo`, contraseña `mymi`.** Arranca solo y Mymi abre sola.

El recorrido, en orden:

1. **Dar de alta una institución.** En Mymi, *«Elegir el archivo…»* y buscar uno de los cinco
   paquetes en la carpeta **`Entregas`** de la carpeta personal. En el diálogo, **Ctrl+L** y
   escribir `/home/demo/Entregas` es lo más rápido.

   **Leé la hoja que sale antes de instalar.** Es la pantalla que más dice de todo el producto:
   quién es, qué va a poder contestar, dónde va a vivir el registro, con quién va a hablar, y qué
   revisó Mymi del paquete.

2. **Abrí su pantalla.** Es la cara que diseñó la institución, corriendo adentro de Mymi con el
   motor prestado. **Abrirla es lo que dispara la búsqueda de datos** — hay que hacerlo con cada
   institución, una por una; dar de alta no llena nada.

3. **Consultá.** Los dos botones *«Consultar»* son la otra puerta: la misma que usa un asistente
   por MCP, leyendo el mismo registro que pintó la pantalla.

4. **El asistente de adentro.** La pestaña *«Asistente»*. En *«Ajustes»* elegís qué modelo
   redacta las respuestas y pegás **tu** llave: no viene nada puesto, y la llave se queda en
   esta máquina.

5. **Y un asistente de AFUERA, por MCP** — que es lo más interesante de la demo: algo que no es
   parte de Mymi preguntándole. **Claude Desktop no viene dentro de la imagen**, lo instalás vos
   en dos pasos:

   ```bash
   sudo apt update && sudo apt install claude-desktop
   ```

   ```bash
   /opt/mymi/host/Gnothi.Host --install /opt/mymi/Modulos
   ```

   El repositorio de Anthropic ya está registrado en la máquina, y el conector de Mymi también
   está escrito, así que el segundo paso puede decirte que ya estaba: está bien, ese archivo es
   de Mymi, no de Claude.

   Después firmá con tu cuenta, **cerrá Claude y volvé a abrirlo** —lee su lista de consultas al
   arrancar— y preguntale algo como *«¿cómo vengo con mis exámenes?»*.

### Las cinco instituciones

```
Delfos              laboratorio     tus examenes, con su historia
La Guaria           supermercado    87 compras, que subio y que cambiaste
Sotaventos Motors   concesionario   el plan de tu YARIS-2019
Monteverde          municipalidad   por que subio cada rubro
Faro                combustible     tus cargas, tramo a tramo
```

Los cinco publicadores corren **adentro**, cada uno en su puerto de `127.0.0.1`. **De esta
máquina no sale nada**, y eso se puede comprobar: el módulo declara en la hoja del alta la única
dirección que va a poder tocar.

---

## Lo que se ve raro y no está roto

**Faro dice que no puede calcular el rendimiento** — sólo en la imagen **limpia**, y la primera
vez. Sus 28 cargas llegaron, pero **el kilometraje no lo manda la estación**: nadie sabe el
odómetro de tu carro salvo vos, y sin dos tanques llenos con kilometraje no hay tramo que medir.
Es, de hecho, lo que el producto quiere mostrar: hay datos que no son de ninguna institución.

La imagen los anota sola al minuto de que Faro traiga sus cargas; **para verlos hay que cerrar
Mymi y volver a abrirla**, porque mientras corre tiene el registro abierto y no ve lo que se
escribe por fuera. Después queda así: *«You were doing 12.6 km/L and the latest leg came out at
9.4: 25% less»*.

En la imagen **cargada** esto ya está hecho y no hay que esperar nada.

**Claude responde en español aunque el sistema esté en inglés.** Su idioma sale de **la cuenta**,
no del sistema operativo: se cambia en su menú de usuario → **Idioma**.

**La cara de la institución no se traduce.** El Host habla el idioma del aparato; el nombre de la
institución, su lema y la voz de su pantalla quedan como ella los escribió. Son dos voces en la
misma ventana, a propósito.

**El asistente dice que no ve datos.** No es un fallo: leyó el registro y está vacío porque nadie
abrió esa institución todavía. **El asistente no sale a buscar datos** — lee lo que ya está en el
aparato. Abrí la pantalla de esa institución y volvé a preguntar.

**Desde el asistente no se puede abrir la pantalla de una institución.** Esas vistas las pide su
cara, y el servidor MCP no tiene ventana donde abrirla. Las consultas sí funcionan.

**Claude Desktop no viene instalado.** Es deliberado: los términos de Anthropic no cubren
redistribuirlo dentro de una imagen que se publica. Se instala en dos pasos y están arriba. La
cuenta, además, es de quien abre la VM.

**El asistente de Mymi no trae modelo ni llave.** Al entrar a *«Ajustes»* vas a ver el selector
de proveedor y *«Todavía no hay llave»*: eso es lo correcto. La llave la ponés vos y se queda en
esta máquina.
