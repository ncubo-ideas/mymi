# La VM que se descarga

Una máquina virtual Linux con **Mymi**, los **cinco módulos ya dados de alta** y **Claude
Desktop** conectado por MCP. La persona la baja, la abre, y encuentra sus instituciones puestas
como si se las hubieran entregado.

Es el escalón de más peso de los tres que hay para la demo, y ahora es viable por una razón
concreta: **es Linux**. La objeción que la mataba era Windows, que no se puede redistribuir.
Ubuntu sí, sin pedirle permiso a nadie, y el tamaño baja de 10–15 GB a unos 3 (≈1,5
comprimido).

---

## Qué lleva adentro

| | |
|---|---|
| Base | **Ubuntu 24.04 LTS** con XFCE |
| Host | Mymi (`Gnothi.Desktop` + `Gnothi.Host`) en `/opt/mymi` |
| Módulos | los cinco `.gmod` híbridos, **entregados en `~/Entregas`** sin dar de alta |
| Publicador | el mock de Delfos corriendo en `127.0.0.1:8091`, con la historia adentro |
| Cliente de chat | Claude Desktop, con el conector MCP registrado |
| Arranque | autologin, y Mymi abre sola |
| Escritorio | un `LEEME.txt` con qué probar y cómo inyectar un examen en vivo |

**Ubuntu 24.04 y no Debian** porque es la única base que cubre los dos clientes de chat a la
vez: Claude Desktop pide Ubuntu 22.04+ o Debian 12+, y ChatGPT Desktop pide Ubuntu 24.04/26.04
o Debian 13. La 24.04 LTS entra en las dos ventanas.

## Cómo se arma

```bash
# 1 · publicar Mymi — DESDE LINUX y self-contained (ver la advertencia de abajo)
dotnet publish Gnothi.Desktop -c Release -r linux-x64 --self-contained -o /tmp/publicado/mymi
dotnet publish Gnothi.Host    -c Release -r linux-x64 --self-contained -o /tmp/publicado/host

# 2 · juntar los paquetes
mkdir -p /tmp/paquetes && cp Gnothi.Mobile/bundles/hibrida/*.gmod /tmp/paquetes/

# 3 · dentro de una Ubuntu 24.04 limpia
sudo ./provision.sh /tmp/publicado /tmp/paquetes ~/Descargas/claude-desktop.deb
```

Y después: apagar la VM y **exportar a OVA**.

El `.deb` de Claude no se descarga solo a propósito: la URL cambia y no vamos a adivinarla. Se
baja de `claude.ai/download` y se pasa como tercer argumento. Sin él la VM queda igual, y el
cliente se instala después corriendo `Gnothi.Host --install`.

## El alta la hace la persona, y los paquetes la esperan en `~/Entregas`

Es la diferencia con el sandbox de contenedor (`Herramientas/sandbox/`), donde el alta corre en
el arranque porque cada caja es **una persona nueva** y cada visitante tiene que ver su propia
alta, con su carpeta concedida y el consentimiento en su estado inicial.

Acá se dejó del otro lado, y **a pedido**: los cinco `.gmod` van en `~/Entregas` y la persona los
elige desde *«Elegir el archivo…»*. La razón es que el alta **es la demostración**: es donde se
ve qué declara el manifiesto, qué carpeta se le concede y qué consultas quedan encendidas. Una
VM que abre con las cinco instituciones ya puestas esconde justamente el momento que hay que
mostrar.

Hay una razón técnica que lo confirma: los cinco son **híbridos**, y el `--install-module` de la
CLI no los da de alta —el `ShellFactory` sólo tiene su implementación real dentro de
`Gnothi.Desktop`—. El alta de un híbrido pasa por la ventana o no pasa.

## Formato y a quién le sirve

| formato | corre en | nota |
|---|---|---|
| **OVA** (x86_64) | VirtualBox y VMware en Windows, Linux y Mac Intel | el más universal, es el que se publica |
| `qcow2` (x86_64) | QEMU/libvirt | para quien ya usa virsh |
| `qcow2` (arm64) | **UTM en Mac Apple Silicon** | hace falta publicar con `-r linux-arm64`; `Photino.Native` trae arm64, pero es una variante más que probar |

Un Mac con Apple Silicon **no puede correr el OVA x86**. Si hay que cubrir esos, es una segunda
imagen, no una conversión.

## Tamaño esperado

| | |
|---|---|
| Ubuntu 24.04 + XFCE | ~2 GB |
| .NET runtime + WebKitGTK | ~350 MB |
| Claude Desktop | ~200–300 MB |
| Los cinco módulos | ~1 MB |
| **disco de la VM** | **~3 GB** |
| **OVA comprimido** | **~1,5 GB** |

El paso 7 del script llena de ceros el espacio libre antes de exportar. Sin eso la imagen
arrastra todo lo que se borró durante la construcción y el OVA no comprime.

## Las cuatro trampas de armar la imagen

Ninguna es de Mymi; son de armar imágenes, y las cuatro cuestan una tarde si no se saben.
Encontradas el 2026-09-21 armando la primera.

**1 · `qemu-img resize` no agranda el filesystem.** Estira el disco virtual y deja la partición
como estaba, así que adentro sigue habiendo 2,5 GB y XFCE se topa con `No space left on
device` a mitad del `dpkg`. En una VM que arranca, `cloud-init` lo expande con `growpart`; si
provisionás **sin arrancar**, eso no pasa nunca. Lo correcto:

```bash
qemu-img create -f qcow2 mymi-demo.qcow2 16G
virt-resize --expand /dev/sda1 ubuntu-24.04.qcow2 mymi-demo.qcow2   # expande partición Y ext4
virt-df -h -a mymi-demo.qcow2                                        # comprobarlo
```

**2 · En WSL2, libguestfs no arranca sin un kernel instalado.** `supermin` necesita uno para
construir su appliance y WSL no trae `/boot/vmlinuz` porque usa el suyo. El síntoma es
`supermin exited with error status 1`. Se resuelve con `apt install linux-image-generic`, que
no cambia con qué arranca WSL. Y conviene `export LIBGUESTFS_BACKEND=direct`.

**3 · `TMPDIR` tiene que estar en el mismo filesystem que el destino.** El alta termina moviendo
el módulo del temporal a su carpeta, y un move entre filesystems falla con `EXDEV` — *Invalid
cross-device link*. Con `/tmp` en tmpfs y `/opt` en ext4, el alta baja el bundle, lo examina
bien, y muere al instalar.

**4 · El conector de Claude sólo se registra si su archivo ya existe.** `HostInstaller` busca
`~/.config/Claude/claude_desktop_config.json` y lo agrega con `if (File.Exists(...))`, porque en
una máquina normal lo crea la app en su primer arranque. En una imagen recién provisionada Claude
nunca se abrió, así que hay que crearlo con `{ "mcpServers": {} }` **antes** de correr
`--install`. Sin eso la VM sale sin conector, con el síntoma clásico: *"configuré Gnothi y el
asistente no lo ve"*.

## Reproducible: la receta antes que la imagen

Para la primera hay que hacerla a mano una vez: instalar Ubuntu, correr `provision.sh`,
exportar. Eso está bien para que funcione.

Para mantenerla, **Packer** con el builder `virtualbox-iso` y este mismo script como
provisioner produce el OVA de una sola corrida, y la receta vive en git en vez de en la cabeza
de alguien. Es lo que conviene en cuanto haya una segunda versión.

## ✅ Mymi corre en Linux — probado el 2026-09-21

Probado en WSL2 con Ubuntu 26.04 y WSLg. Photino levantó la ventana:

```
Photino.NET: "Gnothi".SetSize(1000, 740)
Photino.NET: "Gnothi".SetMinSize(720, 520)
```

Lo que hizo falta, y ninguna de las tres cosas estaba documentada en el repo:

**1 · Las dependencias exactas.** `ldd Photino.Native.so` pide `libwebkit2gtk-4.1.so.0`,
`libjavascriptcoregtk-4.1.so.0` (las dos vienen con el paquete 4.1), `libgtk-3.so.0` y
**`libnotify.so.4`** — esta última faltaba en la primera versión de este script.

**2 · Publicar desde Linux, no desde Windows.** El `TargetFramework` del desktop se decide por
el sistema donde compilás, no por el destino:

```xml
<TargetFramework Condition="$([MSBuild]::IsOSPlatform('Windows'))">net9.0-windows</TargetFramework>
```

Forzarlo con `-p:TargetFramework=net9.0` compila pero rompe el publish con `NETSDK1152`
(`Gnothi.Host` se construye dos veces y las salidas colisionan). Y si se usa el output de
`build` en vez del de `publish`, el `Gnothi.Desktop.staticwebassets.runtime.json` lleva **rutas
de Windows** grabadas, y en Linux eso tira `ArgumentException: The path must be absolute
(Parameter 'root')` al construir la ventana. El publish real no genera ese manifiesto —deja los
assets en `wwwroot/`— así que hay que publicar, no copiar `bin/`.

**3 · Self-contained**, porque Ubuntu 26.04 ya no empaqueta .NET 9, sólo el 10. Un build
framework-dependent queda atado a qué versión trajo Canonical ese año. (Para probar alcanza
`DOTNET_ROLL_FORWARD=Major` sobre el runtime 10, y funciona.)

**Lo que queda por medir: la fluidez.** Sin GPU, WebKit renderiza por software — en WSLg salen
avisos de `libEGL` y `MESA: ZINK` y cae a software. En un contenedor con Xvfb va a pasar lo
mismo. No impide que funcione, pero hay que ver cómo se siente antes de prometer una demo.

## El publicador vive adentro, y no hay que sembrar nada

Resuelto el 2026-09-21, y la resolución fue más simple que el plan.

El plan era sembrar el journal durante la construcción. **No hace falta, y hacerlo estaba mal.**
`EndPointDeEventos` —el mock del laboratorio— **ya nace con la historia adentro**: 20 eventos en
memoria, con la serie de colesterol que sube 182 → 188 → **212** y un perfil lipídico de 2021
que es lo único que sale vencido. Así que alcanza con que el publicador esté corriendo:

```ini
# /etc/systemd/system/delfos-mock.service
ExecStart=/opt/mocks/mock/EventosMock
Environment=ASPNETCORE_URLS=http://127.0.0.1:8091
Environment=DOTNET_ROLL_FORWARD=Major
User=demo
```

El bundle de `examenes.gmod` se rearmó apuntando a ese `:8091`. La persona da el alta, aprieta
actualizar, y el expediente se llena **por el camino real**: el cartero sale con la credencial
de la app, pide el sobre al publicador y el `[Deliver]` lo escribe. Nada precocinado.

**El inyector va aparte, para usarlo en vivo.** Queda en `/opt/mocks/inyector` y el `LEEME.txt`
del escritorio lo explica. Sirve para agregar un examen mientras alguien mira y que aparezca al
actualizar. El **antígeno prostático no está en la historia a propósito**: inyectarlo y después
declarar que la persona es mujer es lo que muestra que la app lo esconde.

**Y una unidad de semilla es un error, no una mejora.** La primera versión de esta imagen traía
un `delfos-semilla.service` que corría `InyectorDeEventosDemo historia`. Eso **postea el mismo
guion** que el mock ya tiene de fábrica, con Ids nuevos: duplicaba los siete exámenes. Y su
`sexo --sexo hombre` rompía un diseño deliberado —los analitos sembrados tienen rango
independiente del sexo justamente para que la app funcione sin que la persona declare nada—.
La unidad se quitó.

Un detalle que se paga al reiniciar: el mock guarda en `ConcurrentDictionary`, o sea **en
memoria**. Lo inyectado en vivo se pierde al apagar la VM; la historia de fábrica vuelve
siempre. Para la demo es la conducta que se quiere.

## El locale no es cosmético: es de dónde sale el idioma del Host

`Gnothi.Host/Textos.cs` resuelve el idioma **del aparato** y una sola vez
(`CultureInfo.CurrentUICulture`), porque la decisión es que no haya selector dentro de Gnothi.
En una imagen recién provisionada eso es `C`/`en_US`, así que **Mymi abre en inglés** aunque
todo lo demás esté bien. De ahí el paso del `language-pack-es` y el `/etc/environment`: no es un
adorno, es lo que hace que la cara hable español. Las reglas están en
`Metodologia/Guias/reglas-de-localizacion.md`.

## Lo que falta decidir o probar
- **La cuenta de Claude es del visitante.** La sesión no viaja en la imagen, y está bien que no
  viaje. Conviene decirlo en la página de descarga para que nadie se sorprenda.
- **Sólo Delfos trae datos.** Los cinco `.gmod` van en `~/Entregas`, pero el único publicador
  que corre adentro es el del laboratorio. Los otros cuatro dan de alta, pintan su cara y se
  quedan vacíos. Ponerles datos son cuatro servicios más del mismo patrón, cada uno en su
  puerto, y el bundle de cada uno rearmado contra él.
