# E1 · Trampas de plataforma y herramientas

Cada una da **un síntoma que no se parece a su causa**. Eso es lo que las vuelve caras.

Ordenadas por cuánto tiempo cuestan si no las conocés.

Las cuatro primeras salieron armando los instaladores MSI: van arriba porque son las más caras. Para qué
instalan y por qué, ver `C7`.

---

## 1 · Nunca interrumpas un `msiexec`

**Síntoma:** códigos `1618` / `1631` intermitentes —«hay otra instalación en curso»— que aparecen **incluso
durante un `wix build`** que ni siquiera instala nada. Y peor: un `/x` que se cuelga buscando un paquete que
no encuentra.

**Causa doble.** Cortaste el proceso a la mitad y dejaste el servicio `msiserver` con el **mutex tomado**; y
además **corrompiste el registro** — el `LocalPackage` de la entrada quedó vacío, y el paquete cacheado ya
no se resuelve por `ProductCode`.

**Regla:** nunca cortes la barra de progreso de un `msiexec`. Si ya pasó, `Restart-Service msiserver`
elevado, y desinstalá con `msiexec /x` apuntando **al archivo `.msi`**, no al `ProductCode` —que es justo lo
que dejó de resolver—.

## 2 · WiX v7 te pide firmar el EULA de OSMF

**Síntoma:** `WIX7015` — el `wix build` se planta exigiendo aceptar el EULA de OSMF antes de compilar.

**Regla:** usar **WiX v5**, que no lo pide. Por eso los MSI de Gnothi están hechos con WiX v5; ver `C7`.

## 3 · Cada `wix build` inventa un `ProductCode` nuevo

**Síntoma:** en «Agregar o quitar programas» se acumulan **entradas gemelas**, una por cada build que
instalaste.

**Causa:** WiX genera un `ProductCode` distinto en cada compilación. Sin `AllowSameVersionUpgrades`, Windows
los trata como productos distintos y ninguno reemplaza al anterior.

**Regla:** declarar `AllowSameVersionUpgrades="yes"` en el `MajorUpgrade`, así una versión pisa a la anterior
en vez de sumarse.

## 4 · WebView2 ensucia tu carpeta de programa

**Síntoma:** al lado del `.exe` aparece una carpeta `<exe>.exe.WebView2` con datos de usuario, y el
desinstalador **no la barre** — queda basura después de desinstalar.

**Causa:** por defecto WebView2 planta su *user-data folder* en `<exe>.exe.WebView2`, junto al ejecutable.

**Regla:** reubicar el *user-data folder* a `%LOCALAPPDATA%` al crear el entorno. Es el shell del comercio el
que lo hospeda; ver `C7`.

## 5 · Dos guiones seguidos rompen un `Directory.Build.props`

**Síntoma:**

```
error NU1015: Los siguientes elementos PackageReference no tienen una versión especificada
```

No menciona XML en ninguna parte.

**Causa:** un comentario XML **no puede contener `--`**. Escribirlo invalida el archivo entero, MSBuild no
lo aplica, la propiedad de versión queda vacía, y el error sale del lado de NuGet.

**Regla:** en comentarios de `.props` / `.csproj`, paréntesis o raya larga. **Nunca `--`.**

Y la misma trampa te espera del lado del instalador: un `.wxs` de WiX también es XML, así que el mismo par de
guiones te invalida el comentario ahí.

## 6 · La virtualización de archivos de una app de la Store

**Síntoma:** editás la configuración de la aplicación, guardás, y la aplicación la ignora — o la reescribe y
borra tu clave.

**Causa:** es una app MSIX. Su `%APPDATA%` **redirige** a:

```
%LOCALAPPDATA%\Packages\<paquete>\LocalCache\Roaming\...
```

Los dos caminos pueden apuntar al mismo archivo, pero el que la aplicación abre es el del paquete.

**La forma segura de llegar:** usar el botón de la propia aplicación que abre el archivo. Nada de adivinar
rutas.

**Y hay un segundo nivel:** una versión anterior de la aplicación **reescribía el archivo descartando las
claves que no conocía**. Si tu bloque desaparece al reiniciar, es eso — no un error de sintaxis.

**Y por esto los instaladores de Gnothi son MSI y no MSIX:** MSIX es sandbox y **prohíbe registrar la
máquina** —escribir en `HKCU\Software\Ncubo\Gnothi`, la marca de descubrimiento que el comercio necesita
leer—. Ver `C7`.

## 7 · Windows PowerShell 5.1 no tiene `&&`

**Síntoma:** `El token '&&' no es un separador de instrucciones válido en esta versión.`

**Regla:** `;`, o dos comandos separados. Mejor todavía: **rutas absolutas y ningún `cd`**, así el comando
es uno solo:

```bash
dotnet run --project "C:\ruta\absoluta\Proyecto" -- listar
```

**Y ojo con `2>&1` sobre ejecutables nativos** en 5.1: envuelve cada línea de stderr en un ErrorRecord y
pone `$?` en falso aunque el exe haya salido con 0.

**Tampoco hay:** operador ternario, `??`, `?.`. Y `ConvertFrom-Json` devuelve un objeto, no un hashtable.

## 8 · El feed privado

**Síntoma:** `NU1103`, con la lista de versiones encontradas en el feed.

**Consecuencia:** sin acceso a ese feed **no compila nada** que dependa del motor, y **un contenedor no
puede restaurarlo** sin credenciales.

**Cómo se dribleó:** compilar en la imagen solo lo que usa paquetes públicos, y **copiar el resto ya
compilado**. Y resulta ser lo correcto igual: una institución te entrega un artefacto, no su código.

### 8b · El otro síntoma del mismo feed: `NU1301` con 403

Y hay un segundo síntoma que **no se parece al primero**, porque no habla de versiones ni de paquetes:

```
error NU1301: No se puede cargar el índice de servicio del origen
  https://pkgs.dev.azure.com/ncubo/_packaging/Puppeteer/nuget/v3/index.json
error NU1301:   Response status code does not indicate success: 403 (Forbidden -
  User '<guid>' lacks permission to complete this action. You need to have 'ReadPackages'.)
```

**`NU1103` es «el paquete no está». `NU1301` con 403 es «el feed no te deja entrar».** El primero se
arregla declarando el feed; el segundo, **no se arregla del lado del repo**: o la identidad no tiene
`ReadPackages` en el feed, o el token cacheado venció y el restore salió sin credencial.

**Lo que lo dispara sin aviso:** no hay `~/.nuget/plugins`, o sea **el Azure Artifacts Credential Provider
no está instalado**. El `NuGet.config` avisa que la autenticación la resuelve ese plugin, pero si falta,
el restore no pide credenciales: **sale anónimo y cobra el 403**. Instalarlo, y después `dotnet restore
--interactive`.

**Y deja daño colateral:** el restore fallido **reescribe `obj/project.assets.json` vacío** —4 KB, con
`"targets": {}`— así que después ni `--no-restore` sirve para compilar con lo que ya había.

**La salida de emergencia, sin tocar el repo:** la caché global (`~/.nuget/packages/ncubo.puppeteer`)
suele tener versiones viejas del motor. Se puede compilar contra una de ellas pasando la propiedad por
línea de comandos, sin editar `Directory.Build.props`:

```bash
dotnet publish App/Examenes.Wasm -c Release -o PaginaDeDescarga/publicado \
  -p:PublishTrimmed=false -p:VersionDelMotor=3.0.0-beta.10029-lector.4
```

⚠ **Eso produce un artefacto que NO es el que declara el repo.** Sirve para desbloquear una demo; no
sirve para medir ni para citar cifras. Ver `D3`.

## 9 · Debian, no alpine

**Síntoma:** `CultureNotFoundException` al **hidratar el actor** — lejos de la causa.

**Causa:** el constructor estático del parser del motor pide una cultura concreta y alpine no trae los datos
de ICU.

**Regla:** la imagen de runtime Debian, no la variante alpine.

## 10 · Globalización invariante rompe el motor

Mismo `CultureNotFoundException`. Y es **justo el flag que uno agrega para bajar el payload de wasm**, así
que es fácil reintroducirlo sin querer.

## 11 · El trimmer poda la reflexión en silencio

El descubrimiento de dominio del motor es por **reflection**, y el descubrimiento de consultas de un SDK
basado en atributos también. El trimmer de Release puede podar esos tipos, y **falla en silencio** — que es
el peor modo de fallo.

**Consecuencia práctica:** cuando se empaquetó el Host como ejecutable autocontenido, **se publicó sin
trimming a propósito**. Pesa más y arranca seguro.

**La salida correcta a futuro:** source generators. Las anotaciones se resuelven en compilación y emiten el
descriptor y el dispatcher como código concreto. Trimming-safe por construcción.

## 12 · Un proceso vivo bloquea el dll

**Síntoma:** `MSB3027: el archivo se ha bloqueado por ".NET Host (PID)"`.

**Causa:** un servidor MCP de otra sesión tiene el dll tomado.

```bash
powershell -Command "Get-CimInstance Win32_Process -Filter \"Name='dotnet.exe'\" | Where-Object { $_.CommandLine -like '*Facturas.Mcp*' } | ForEach-Object { Stop-Process -Id $_.ProcessId -Force }"
```

## 13 · `Compress-Archive` escribe backslashes en el zip

**Síntoma:** un bundle bien armado que un lector estricto no puede abrir, o al que no le encuentra los
archivos anidados.

**Causa:** PowerShell 5.1 escribe las entradas como `carpeta\archivo`. **El formato zip pide barra
normal.**

**Cómo se arregla:** armar el zip entrada por entrada, normalizando el separador. Y verificar después que
`manifest.json` esté en la raíz y que ninguna entrada tenga `\`.

**Caso vivo:** el empaquetador `.gmod` del Host (`--pack`) es exactamente este zip y esta regla —
`manifest.json` en la raíz, ninguna entrada con `\`—. Ver `C7`.

## 14 · Y una que no es trampa, es disciplina

**No confundir dos evidencias de confinamiento**, que se parecen y no son lo mismo:

- *«el motor entero cabe en un sandbox»* — **medido**, y es evidencia de portabilidad y limpieza del
  dominio.
- *«el módulo no puede porque no se le concedió la capacidad»* — es el argumento de capacidades, y **solo
  está medido a nivel de contenedor**, no por módulo.

En un pitch, fusionarlas es sobrevender.
