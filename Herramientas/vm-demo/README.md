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
| Módulos | los cinco `.gmod` híbridos, con el alta ya hecha |
| Cliente de chat | Claude Desktop, con el conector MCP registrado |
| Arranque | autologin, y Mymi abre sola |
| Escritorio | un `LEEME.txt` con tres cosas que probar |

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

## El alta va en la construcción, no en el arranque

Es la diferencia con el sandbox de contenedor (`Herramientas/sandbox/`), y es deliberada.

En el sandbox cada caja es **una persona nueva**, así que el alta corre al arrancar: cada
visitante ve su propia alta, con su carpeta concedida y el consentimiento en su estado inicial.

Acá la VM **es de una persona**. La baja, la abre y espera encontrar sus instituciones ya
puestas. Si el alta fuera en el arranque, la primera impresión sería una ventana vacía.

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

## Lo que falta decidir o probar
- **La cuenta de Claude es del visitante.** La sesión no viaja en la imagen, y está bien que no
  viaje. Conviene decirlo en la página de descarga para que nadie se sorprenda.
- **El expediente sembrado.** Los módulos quedan dados de alta pero sin datos si el cartero no
  puede salir a buscarlos. Falta sembrar el journal en la construcción — `demo-datos.ps1` es
  PowerShell, así que hay que llamar a `Examenes.Consola refrescar` o traer `pwsh`.
- **Los mocks del publicador.** Si se quiere que el botón de actualizar traiga algo, hay que
  meter los `docker-compose` de cada negocio dentro de la VM, o dejar el expediente fijo.
- **Dónde se publica el OVA.** Son 1,5 GB por descarga; conviene un objeto en R2 o similar
  detrás de Cloudflare, no el servidor de los stages.
