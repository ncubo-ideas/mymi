# Gnothi en escritorio — la prueba manual, paso a paso

Cómo probar, de punta a punta, lo que hay hoy en la rama `master_gnothiHostDesktop`:

1. **Empaquetar** un módulo en la forma cargo (2 DLLs + `view.json`) e **importarlo**.
2. **La ventana** de escritorio que lista tus módulos.
3. **Registrar** Gnothi con el asistente (`--install`).
4. **La PWA por fuera**: la app del laboratorio (Delfos) corriendo sola en el navegador.
5. **El supermercado — Súper La Guaria**: la misma idea con marca propia (página + PWA
   branded), y una página que ofrece **los dos caminos** — la app sola, y el del asistente
   (bajar Gnothi + el módulo).

> Cada paso dice **qué tenés que ver**. Si no ves eso, pará ahí.
> Los comandos son para **PowerShell** salvo donde diga otra cosa. Están corridos.

Hacen falta: **.NET SDK 9**, y para las partes 4 y 5, **Docker Desktop** corriendo.

---

## Parte 0 · Compilar

```powershell
cd C:\Users\jon32\source\repos\Gnothi
dotnet build Gnothi.slnx -c Debug
```

**Tenés que ver** `Compilación correcta.` y `0 Errores`. (Los `warning NU1900` son de red al índice de NuGet, inofensivos.)

La ventana es un proyecto aparte —`net9.0-windows`, fuera del `.slnx` a propósito, como la app Android— así que se compila sola:

```powershell
dotnet build Gnothi.Desktop\Gnothi.Desktop.csproj -c Debug
```

---

## Parte 1 · Empaquetar un módulo cargo e importarlo

Esto es el corazón: un módulo que son **dos DLLs y nada más** (17–140 KB), sin el motor. El Host se lo presta.

### 1.1 · Preparar la fuente del paquete

El módulo de examenes ya está compilado por el paso 0. Armamos una carpeta fuente con las 2 DLLs, un `manifest.json` y un `view.json`:

```powershell
$T = "$env:TEMP\gnothi-demo"
$src = "$T\fuente"
New-Item -ItemType Directory -Force "$src\dominio" | Out-Null
$bin = "Examenes\App\Examenes.App\bin\Debug\net9.0"
Copy-Item "$bin\Examenes.App.dll","$bin\Examenes.Dominio.dll" "$src\dominio\"
```

Escribí el `manifest.json` (declara `load.entry` = qué DLL es la superficie):

```powershell
@'
{
  "manifest": "1.0",
  "module": "examenes",
  "title": "My lab tests",
  "institution": "Clinical laboratory (POC mock)",
  "substrate": { "range": ">=3.0.0 <4.0.0" },
  "capabilities": { "network": false, "folder": "own" },
  "view": "view.json",
  "load": { "entry": "dominio/Examenes.App.dll" },
  "tools": [
    { "name": "resumen", "title": "Summary", "readOnly": true, "input": [] },
    { "name": "lista_de_examenes", "title": "My exams", "readOnly": true, "input": [] }
  ]
}
'@ | Set-Content -Encoding utf8 "$src\manifest.json"

@'
{ "view": "1.0", "initialScreen": "list",
  "screens": [ { "name": "list", "title": "My lab tests",
    "blocks": [
      { "type": "summary", "query": "resumen",
        "fields": [ { "from": "examenes", "label": "exams" }, { "from": "analitosFueraDeRango", "label": "out of range" } ] },
      { "type": "table", "query": "lista_de_examenes", "rows": "examenes" }
    ] } ] }
'@ | Set-Content -Encoding utf8 "$src\view.json"
```

### 1.2 · Empaquetar

```powershell
dotnet run --project Gnothi.Host -- --pack "$env:TEMP\gnothi-demo\fuente" "$env:TEMP\gnothi-demo\examenes.gmod"
```

**Tenés que ver**:

```
[gnothi] packed examenes: 4 files, 54,6 KB
[gnothi] -> ...\examenes.gmod
```

El empaquetador **rechaza** un paquete que traiga el motor (Puppeteer/Choreography), un `.exe` o la BCL — probalo metiendo `Puppeteer.dll` en `fuente\dominio` y volviendo a empacar: dice `x carries the engine`.

### 1.3 · Importar

```powershell
dotnet run --project Gnothi.Host -- --install-module "$env:TEMP\gnothi-demo\examenes.gmod" "$env:TEMP\gnothi-demo\Modulos"
```

**Tenés que ver**:

```
installed 'examenes' (My lab tests)
  from     : examenes.gmod
  into     : ...\Modulos\examenes
  data     : ...\Documents\Gnothi\examenes
  enabled  : True
```

El Host **escribió** `installation.json` (serializado, no interpolado), concedió una carpeta de datos, y respeta el consentimiento previo si reinstalás. El módulo no eligió nada de eso.

### 1.4 · Sembrar un journal y consultar

Recién instalado, la carpeta de datos está vacía. Le metemos exámenes de muestra (esto es lo que haría la app de la persona):

```powershell
dotnet run --project Examenes\App\Examenes.Consola -- medir "$env:USERPROFILE\Documents\Gnothi\examenes"
```

Y consultamos el módulo **sin asistente**:

```powershell
dotnet run --project Gnothi.Host -- --query "$env:TEMP\gnothi-demo\Modulos" examenes resumen
```

**Tenés que ver** JSON con datos reales, servido desde las 2 DLLs con el motor prestado por el Host:

```json
{ "hayDatos": true, "examenes": 14, "analitosFueraDeRango": 4, "ultimoEventoLeido": 303 }
```

Y que las consultas **no tocan el journal** (solo lectura): corré `--query ... lista_de_examenes` dos veces y compará el hash de `Documents\Gnothi\examenes\examenes\journal\journal_000001.bin` — es idéntico.

---

## Parte 2 · La ventana

```powershell
dotnet run --project Gnothi.Desktop -- "$env:TEMP\gnothi-demo\Modulos"
```

Se abre una ventana nativa. **Tenés que ver** el módulo *My lab tests* como tarjeta, con su institución y `2 queries`.

- **Open** → la pantalla del módulo, con un botón por consulta. **Run** en `resumen` o `lista_de_examenes` muestra los datos (tabla si es una lista).
- **Import a module…** → elegí `%TEMP%\gnothi-demo\examenes.gmod` y lo instala desde la interfaz (esto es la Parte 1.3, pero con un clic).
- **Reload** relee `installation.json` del disco (así se ve un cambio de consentimiento hecho a mano).

> Sin argumento, la ventana busca `Modulos` al lado del ejecutable (instalada) o en la raíz del repo (desarrollo).

---

## Parte 3 · Registrar Gnothi con el asistente

Esto hace que Claude Desktop vea los módulos. **Ojo: modifica la config de Claude** (con respaldo y preguntando). Necesita el ejecutable **publicado**, no `dotnet run`:

```powershell
dotnet publish Gnothi.Host -c Release -o C:\Gnothi
C:\Gnothi\Gnothi.Host.exe --install "C:\Gnothi\Modulos"
```

**Tenés que ver** que descubre tu Claude Desktop, y **te pregunta** `Write it? [y/N]` antes de tocar nada. Si decís que sí, respalda el archivo (`.before-gnothi`) y agrega una sola llave `gnothi`. Reiniciá Claude y preguntale qué módulos tenés.

> Si Gnothi ya estaba registrado (probable), lo **actualiza** al nuevo exe + carpeta.

---

## Parte 4 · La PWA por fuera (Docker)

La app del laboratorio corriendo **sola en el navegador**, servida desde su página de descarga, sin Gnothi en el medio. Es el "andamio" del que sale el journal que después Gnothi lee.

### 4.1 · Publicar la PWA y generar certs locales

```powershell
dotnet publish Examenes\App\Examenes.Wasm -c Release -o Examenes\PaginaDeDescarga\publicado
```

Los certs (solo para que nginx arranque su listener https; usaremos http):

```powershell
New-Item -ItemType Directory -Force Examenes\PaginaDeDescarga\certs | Out-Null
cd Examenes\PaginaDeDescarga\certs
& "C:\Program Files\Git\usr\bin\openssl.exe" req -x509 -newkey rsa:2048 -sha256 -days 365 -nodes `
  -keyout localhost.key -out cadena.crt -subj "/CN=localhost" `
  -addext "subjectAltName=DNS:localhost,IP:127.0.0.1"
cd ..\..\..
```

### 4.2 · Levantar

```powershell
docker compose -f Examenes\docker-compose.yml up -d --build
```

**Tenés que ver** dos contenedores arriba:

```powershell
docker ps --format "{{.Names}}  {{.Status}}"
```
```
examenes-wasm-host    Up ...
examenes-eventos-api  Up ...
```

### 4.3 · Sembrar la historia en el proveedor (URL2)

```powershell
dotnet run --project Examenes\InyectorDeEventosDemo -- historia --url http://localhost:8091/
```

**Tenés que ver** ocho líneas `HTTP 200`, una por examen sembrado.

### 4.4 · Usarla en el navegador

Abrí **http://localhost:8090** — es la **página de descarga del laboratorio**: el pitch, el gráfico del antígeno, y **Instalar la aplicación**.

Hacé clic en **Instalar la aplicación** (va a **http://localhost:8090/app/**). En la pantalla de la app:

1. **Elegir carpeta** → el navegador te pide una carpeta tuya (File System Access). Elegí una vacía. *(Ahí va a vivir el journal, y es la carpeta que Gnothi podría leer después.)*
2. Apretá **Actualizar / Refresh** → la app baja los exámenes de URL2 (por el proxy `/app/eventos/`) y los muestra: la tendencia del antígeno, el salto del último año, la lista.

**Tenés que ver** los exámenes en pantalla, y en la carpeta que elegiste un `examenes/journal/`. Ese journal es el mismo formato que consultaste en la Parte 1.4 — la app por fuera y Gnothi leen lo mismo.

> **Contexto seguro:** `http://localhost` cuenta como seguro, así que File System Access funciona sin https. El listener https (`:8453`) usa el cert self-signed y el navegador lo va a marcar como no confiable — no hace falta para la prueba.

### 4.5 · Bajar todo

```powershell
docker compose -f Examenes\docker-compose.yml down
```

---

## Parte 5 · Súper La Guaria — el supermercado, con los dos caminos

Lo mismo que la Parte 4 (una app que corre sola en el navegador), pero para el súper, y con
una diferencia clave que Delfos no tiene: la página ofrece **también** el camino del
asistente — bajar Gnothi y el módulo. Es el negocio de **Compras**, con la marca ficticia
**Súper La Guaria**.

> Corre en puertos propios (`8100` la página/app, `8101` URL2), así que puede convivir con la
> Parte 4 (examenes, `8090/8091`) sin chocar.

### 5.1 · Publicar la PWA y generar certs

```powershell
dotnet publish Compras\App\Compras.Wasm -c Release -o Compras\PaginaDeDescarga\publicado
New-Item -ItemType Directory -Force Compras\PaginaDeDescarga\certs | Out-Null
cd Compras\PaginaDeDescarga\certs
& "C:\Program Files\Git\usr\bin\openssl.exe" req -x509 -newkey rsa:2048 -sha256 -days 365 -nodes `
  -keyout localhost.key -out localhost.crt -subj "/CN=localhost" `
  -addext "subjectAltName=DNS:localhost,IP:127.0.0.1"
cd ..\..\..
```

### 5.2 · Los descargables de la página (el módulo + Gnothi)

Los dos botones de "Para su asistente" ya sirven archivos reales, generados en
`Compras\PaginaDeDescarga\descarga\descargas\`. **No hace falta regenerarlos para probar.**
Si querés reconstruirlos:

```powershell
# --- el módulo (compras.gmod = 2 DLLs + manifest + view) ---
$bin = "Compras\App\Compras.App\bin\Debug\net9.0"
$src = "$env:TEMP\compras-src"
New-Item -ItemType Directory -Force "$src\dominio" | Out-Null
Copy-Item "$bin\Compras.App.dll","$bin\Compras.Dominio.dll" "$src\dominio\"
@'
{
  "manifest": "1.0", "module": "compras", "title": "Mis compras", "institution": "Supermercado La Guaria",
  "substrate": { "range": ">=3.0.0 <4.0.0" }, "capabilities": { "network": false, "folder": "own" },
  "view": "view.json", "load": { "entry": "dominio/Compras.App.dll" },
  "tools": [
    { "name": "resumen", "title": "Resumen", "readOnly": true, "input": [] },
    { "name": "lista_de_compras", "title": "Mis compras", "readOnly": true, "input": [] }
  ]
}
'@ | Set-Content -Encoding utf8 "$src\manifest.json"
'{ "view": "1.0", "initialScreen": "list", "screens": [ { "name": "list", "title": "Mis compras", "blocks": [ { "type": "table", "query": "lista_de_compras", "rows": "compras" } ] } ] }' | Set-Content -Encoding utf8 "$src\view.json"
dotnet run --project Gnothi.Host -- --pack $src Compras\PaginaDeDescarga\descarga\descargas\compras.gmod

# --- Gnothi: publicar y comprimir (agnóstico del dominio, sirve para cualquier módulo) ---
dotnet publish Gnothi.Host -c Release -o $env:TEMP\gnothi-dist
Compress-Archive $env:TEMP\gnothi-dist\* Compras\PaginaDeDescarga\descarga\descargas\gnothi\Gnothi.Host.zip -Force
```

### 5.3 · Levantar y sembrar

```powershell
docker compose -f Compras\docker-compose.yml up -d --build
dotnet run --project Compras\InyectorDeEventosDemo -- historia --url http://localhost:8101/
```

**Tenés que ver** los contenedores `compras-wasm-host` y `compras-eventos-api` arriba (más
`compras-caja`, la UI del inyector, en `8102`), y las líneas `HTTP 200` al sembrar.

### 5.4 · Usarla

Abrí **http://localhost:8100** — la página de **Súper La Guaria** (héroe, la ficha de la
canasta comparada, y abajo la sección **"Para su asistente"**).

- **Instalar la aplicación** → `http://localhost:8100/app/`: la PWA. Elegí una carpeta y
  tocá **Refresh** → baja las compras de URL2 y las muestra.
  **Tenés que ver** la app con la marca de La Guaria (verde y blanco), no el Bootstrap por
  defecto: la portada abre con la firma de la semana y el quiebre del mes.
- **Descargar el módulo** → baja `compras.gmod` (~54 KB, dos DLLs adentro).
- **Descargar Gnothi** → baja `Gnothi.Host.zip`.

### 5.5 · El camino del asistente, de punta a punta

Es la Parte 3 disparada desde la página. Descomprimí `Gnothi.Host.zip` en `C:\Gnothi`, y:

```powershell
C:\Gnothi\Gnothi.Host.exe --install
C:\Gnothi\Gnothi.Host.exe --install-module <ruta-a>\compras.gmod C:\Gnothi\Modulos
```

**El módulo recién instalado concede una carpeta vacía** (`Documents\Gnothi\compras`), así
que una consulta ahí daría "sin datos" — es el estado correcto de una instalación nueva. Hay
dos formas de que tenga datos:

- **La real:** en el paso 5.4, al usar la app, elegí como carpeta `Documents\Gnothi\compras`.
  Lo que la app escriba ahí es lo que el asistente lee. Es el punto entero: **la app y la IA
  leen el mismo journal.**
- **La rápida, para probar sin la app:** sembrala con la consola (en una carpeta vacía, para
  que el número dé exacto).

```powershell
dotnet run --project Compras\App\Compras.Consola -- medir "$env:USERPROFILE\Documents\Gnothi\compras"
```

Ahora consultá el módulo:

```powershell
C:\Gnothi\Gnothi.Host.exe --query C:\Gnothi\Modulos compras resumen
```

**Tenés que ver** el resumen con datos reales, servido desde las 2 DLLs con el motor prestado
por el Host:

```json
{ "hayDatos": true, "totalGastado": 3174606, "cantidadDeCompras": 87, "ultimoEventoLeido": 387 }
```

### 5.6 · Bajar

```powershell
docker compose -f Compras\docker-compose.yml down
```

---

## Qué prueba cada parte

| parte | qué demuestra |
|---|---|
| 1 | un módulo es **carga** (2 DLLs, sin motor); el Host se lo presta; se empaqueta, se importa y se consulta con datos reales, solo lectura |
| 2 | la persona **ve y usa** sus módulos en una ventana de escritorio, sin asistente |
| 3 | el asistente alcanza los módulos porque Gnothi se **registra** como su servidor MCP |
| 4 | la app del laboratorio corre **sola en el navegador**, escribe su journal en una carpeta de la persona, y ese journal es lo que Gnothi lee |
| 5 | el súper (**Súper La Guaria**) hace lo mismo, y su página ofrece **los dos caminos**: la PWA sola, y el del asistente (bajar Gnothi + el módulo) — el paso que Delfos no muestra |

Todo sobre la superficie pública del Host: el test de eliminabilidad (`Gnothi.Assistant.Tests`) sigue pasando, así que nada de esto ató el Host a una cara ni a un asistente.
