# D2 · El Host y el módulo, contenidos

Correr el escenario completo dentro de un contenedor: el Host, el módulo y un cliente. Es la única forma
en que este POC **demuestra confinamiento** en vez de solo describirlo.

Los servidores del proveedor son otra cosa y van aparte — ver `proveedor/D4`.

---

## Correrlo

```bash
cd "C:/Users/jon32/Documents/MCP Hola Mundo/GnothiHolaMundo/docker"
```

```bash
docker compose build
```

```bash
docker compose run --rm gnothi
```

Da los mismos números que en Windows, con la carpeta concedida del contenedor. Y eso ya es un resultado: el
mismo módulo, el mismo manifiesto, otro hospedaje.

## Lo que está verificado corriendo

| | resultado | cómo lo consigue |
|---|---|---|
| **Sin red** | única interfaz `lo`, sin DNS | `network_mode: none` |
| **Sin escritura afuera** | falla en `/`, `/app`, `/etc`, `/usr` | `read_only: true` |
| El binario del propio módulo | **inmutable** | idem |
| La carpeta concedida | escribe, y es **la única** | `tmpfs` |
| **El journal real** | escritura falla | montado read-only |
| **Cero capacidades del kernel** | `CapEff: 0000000000000000` | `cap_drop: ALL` |

Reproducirlo en un comando:

```bash
docker compose run --rm --entrypoint /bin/sh gnothi -c 'echo "--- red:"; ls /sys/class/net; getent hosts nuget.org || echo "  sin DNS"; echo "--- escribir afuera:"; touch /app/x 2>&1 || true; touch /journal-origen/x 2>&1 || true; echo "--- en la concedida:"; touch /journal/x && echo "  ok"; echo "--- capacidades:"; grep CapEff /proc/self/status'
```

Eso es la diferencia entre *«el módulo promete no salir a internet»* y *«el módulo carece de la capacidad de
salir a internet»*. **Lo segundo se falsea con un comando; lo primero hay que creerlo.**

## Y no hay red porque no hace falta

`network_mode: none` no es paranoia decorativa: el escenario no necesita red. **El asistente, el Host y el
módulo están los tres del mismo lado.** Que el contenedor no la tenga vuelve el invariante verificable en vez
de prometido.

---

## El journal real no se toca nunca

Entra montado en **solo lectura** en `/journal-origen`, y el entrypoint lo copia a `/journal`, que es un
tmpfs que se evapora al terminar.

**Dos razones, y las dos importan:**

1. Los datos de la persona quedan intocables — el contenedor no puede escribirlos ni por error.
2. **Hidratar un actor crea directorios** (`elision`, `followers`, `outbox`, `skips`), así que un montaje de
   solo lectura directo **falla**. Medido.

Y de paso modela lo correcto: **el Host concede un directorio.** Conceder es un acto del que concede.

Verificado después de todas las corridas: el archivo del journal sigue con su fecha original y su md5 sin
cambiar.

```bash
powershell -Command "Get-FileHash '<ruta al journal>' -Algorithm MD5 | Select-Object -Expand Hash"
```

---

## Dos decisiones del Dockerfile que no son de gusto

### El Host se compila en la imagen; el módulo entra ya compilado

No es pereza: el paquete del motor vive en un feed **privado** y el build del contenedor no tiene
credenciales.

Y resulta ser lo correcto igual — **una institución te entrega un artefacto, no su código.** Que el módulo
llegue compilado *es* el modelo, no una concesión. Con el modelo **cargo** la frase se vuelve aún más literal:
el artefacto adelgaza a la superficie y el dominio, sin nada del motor.

### Debian, no alpine

El constructor estático del parser del motor pide una cultura concreta y tira `CultureNotFoundException` sin
los datos de ICU. Alpine no los trae, y **el síntoma aparece recién al hidratar el actor** — lejos de la
causa. Está comentado en el Dockerfile por eso.

---

## El manifiesto se comparte; la instalación no

El contenedor usa **el mismo `manifiesto.json`** que la versión de Windows, **sin una sola línea distinta**,
porque no tiene ninguna ruta adentro. Lo que cambia es el archivo de instalación, con rutas de Linux y otra
carpeta concedida.

Eso no salió por casualidad: **es la razón por la que son dos archivos.** Ver `B4`.

Es también la evidencia más limpia de la promesa «un solo artefacto, dos hospedajes» — con el asterisco de que
en este POC **coexisten dos modelos de módulo**. El de este contenedor —facturas— trae su propio motor y su
propio transporte; pero ya existe el modelo **cargo**, donde el módulo son dos DLLs más su manifiesto y su
vista, y **el Host presta el motor**. El asterisco se achica con cargo, no desaparece. El detalle del
empaquetado y de cómo se instala vive en `C7`.

---

## Lo que el contenedor NO demuestra

**El aislamiento es de contenedor, no de módulo.** Las paredes están alrededor de *todo*: dos módulos adentro
compartirían todo — misma red (ninguna), mismo filesystem, mismas capacidades (ninguna). El aislamiento **por
instancia** —memoria lineal propia, directorio propio— es la etapa siguiente.

Y **cargo no mueve esta línea:** un módulo cargado como DLL corre *in-process* en el Host, así que comparte su
memoria y su directorio igual que hoy. El aislamiento por instancia sigue siendo la etapa siguiente — cargo
adelgaza el paquete, no levanta paredes.

Y conviene no fusionar dos evidencias que se parecen:

| | estado |
|---|---|
| *«el motor entero cabe en un sandbox»* | **medido** — evidencia de portabilidad y limpieza del dominio |
| *«el módulo no puede porque no se le concedió la capacidad»* | **medido solo a nivel de contenedor**, no por módulo |

En un pitch, fusionarlas es sobrevender.

## Y una nota sobre el cliente

El escenario adentro del contenedor lo corre un cliente MCP propio, en .NET. Existe así —y no como script de
shell— **para que sea el mismo cliente en Windows y en Linux**. Eso hace que la comparación entre hospedajes
sea sobre el Host y el módulo, no sobre la herramienta de prueba.
