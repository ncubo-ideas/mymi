# Capítulo 1 — Preparar el escritorio

Qué se instala, qué se configura una vez por clon, y cómo saber que quedó bien.

> **De dónde sale este capítulo.** De preparar esta máquina y anotar dónde se rompió. Las versiones,
> los mensajes de error y los diagnósticos de abajo están **medidos**, no recordados. Donde algo no se
> pudo verificar acá, lo dice.

---

## 1. Hay dos niveles de "listo", y conviene saber en cuál estás

No es lo mismo poder **trabajar** que poder **mostrar la demo**. Muchos escritorios están en el
primero y les alcanza para meses.

| | Qué te habilita | Qué necesita |
|---|---|---|
| **Trabajar** | El dominio, la lógica, la carátula de consola. **El ciclo completo con un journal real** | .NET, git, y el motor restaurado |
| **Mostrar** | La app en el browser, la página de descarga, el flujo de la persona | Además: Docker, y un navegador **Chromium** |

**Empezá por el primero y no te bloquees en el segundo.** Es el orden que los negocios del repo ya
siguen: *"el WASM está bloqueado por el motor, así que se trabaja y se valida por Consola — es para lo
que existe esa carátula"*. Esta máquina, mientras se escribía este capítulo, tenía el primer nivel
completo y **ni Docker instalado**, y aun así corrió el ciclo entero de un negocio contra un journal.

---

## 2. Lo que se instala

| | Versión medida acá | Nota |
|---|---|---|
| **.NET SDK** | `9.0.313` | El repo apunta a `net9.0`. Cualquier 9.0.x sirve |
| **git** | `2.52.0` | Nada especial |
| Docker | — | **Sólo para el nivel "mostrar"**. Levanta la página de descarga y el endpoint de eventos |
| Un navegador **Chromium** | — | Chrome, Edge, Brave o Arc. Ver §7: no es preferencia |

El **motor no se instala**: viene por NuGet y su versión la fija el repo. Eso es a propósito y tiene
historia — mientras se referenciaba por código fuente, el build dependía de qué rama estuviera
checkouteada en un repo que no es parte del entregable, y se rompió de verdad cuando alguien volvió a
`master`.

---

## 3. El paso que hace perder la tarde

**El motor vive en un feed privado.** Ese solo hecho produce tres fallas distintas que **se ven
exactamente igual desde el error**, y se arreglan de tres maneras. Ésta es la tabla que hay que tener
a mano:

| El error dice | Lo que falta de verdad | Se arregla con |
|---|---|---|
| `NU1101 — no packages exist with this id` | Falta **declarar el feed** | Estar parado en el repo: su `NuGet.config` lo declara (§4) |
| `NU1301 — 401` **y no existe** `~/.nuget/plugins` | Falta el **credential provider** | Instalar el Azure Artifacts Credential Provider |
| `NU1301 — 401` **y sí existe** el plugin | **El token caducó** | `dotnet restore --interactive` (§4) |

El del medio y el de abajo son el mismo mensaje:

```
error NU1301: Unable to load the service index for source
  https://pkgs.dev.azure.com/.../nuget/v3/index.json
error NU1301:   Response status code does not indicate success: 401 (Unauthorized).
```

Léelo bien: **no dice que falte un token.** Dice que no puede cargar el índice. Quien lo vea por
primera vez va a mirar el `csproj`, la red o el proxy, que es donde no está.

**Cómo distinguir los dos últimos, en un comando:**

```bash
ls ~/.nuget/plugins/netcore/
```

Si aparece `CredentialProvider.Microsoft`, el plugin está y lo que caducó es el token.

### Y hay una segunda trampa, adentro de la primera

**El comando funciona o falla según desde dónde lo corras**, y cuando falla da **el mismo `NU1301`**.

El `NuGet.config` que declara el feed vive en la raíz del repo, y NuGet lo busca **subiendo desde el
directorio donde estás parado**. Desde tu carpeta de usuario ese archivo no existe para él: sólo ve
`nuget.org`, donde el paquete no está.

Medido: `dotnet nuget list source` desde el home **no lista** el feed privado; desde la raíz del repo,
sí. Alguien que reautentique tres veces desde el lugar equivocado va a concluir que su cuenta no tiene
permisos.

---

## 4. Los dos comandos de una vez por clon

Los dos se corren **desde la raíz del repo**, y los dos se olvidan.

**Uno · el hook de mensajes de commit.** No viene activado en un clon nuevo:

```bash
git config core.hooksPath .githooks
```

Sin esto, tus commits pasan igual y **rompen la convención en silencio** hasta que alguien lo note en
una revisión. Con esto, un asunto mal formado se rechaza y el hook **imprime el formato**, así que no
hay que ir a buscarlo a ningún lado.

**Dos · el token del feed.** Abre el navegador para autenticar:

```bash
dotnet restore --interactive
```

Una vez, y queda para todos los proyectos que consumen el motor.

---

## 5. La verificación: si estos tres corren, podés trabajar

No es una lista de deseos — son tres comandos que devuelven verde o rojo.

**1 · El SDK está y es el correcto**

```bash
dotnet --version
```

Tiene que empezar con `9.`

**2 · El feed se ve y el motor restaura**

```bash
dotnet restore
```

Si esto pasa, los tres problemas de §3 están resueltos. Si falla, la tabla de §3 dice cuál de los tres
es.

**3 · Un negocio corre de punta a punta**

Elegí cualquiera de los que ya existen y corré sus comprobaciones del dominio puro — sin actor, sin
journal y sin disco. Terminan diciendo cuántas pasaron.

Y si querés la prueba fuerte, corré el ciclo **con journal**: siembra una historia en una carpeta
temporal y contesta preguntas contra ella. Eso ejercita el DSL, el binding de parámetros y la
persistencia, que es lo que las comprobaciones del modelo puro **no** pueden ver (capítulo 3 §8).

---

## 6. Lo opcional, y qué te pierdes sin ello

**Docker.** Levanta la página de descarga (URL1) y el endpoint de eventos (URL2). Sin él no podés
mostrar el flujo de la persona instalando la app. **Podés hacer todo lo demás.**

**El workload `wasm-tools`.** Sin él, el publish del browser sale **sin optimizar** —funciona, pero
pesa mucho más—. Y hay una trampa que ya costó tiempo: si el SDK te pide el workload con un
`NETSDK1147`, la variable `MSBuildEnableWorkloadResolver=false` desactiva el resolver y el build pasa.
Instalarlo pide permisos de administrador, y **un `dotnet workload install` que falla por permisos
igual alcanza a actualizar los manifests del SDK y deja el error pegado**. Si ya te pasó, esa variable
es la salida.

---

## 7. El navegador no es preferencia, es una condición del producto

Sólo aplica al nivel "mostrar", y conviene saberlo antes de fijar un guion de demo.

La app guarda el journal en **una carpeta que la persona elige**, y eso usa `showDirectoryPicker` —la
File System Access API—, que **existe únicamente en navegadores Chromium**: Chrome, Edge, Brave, Arc.
Safari y Firefox no la tienen.

No es un detalle de pantalla. El argumento del producto tiene dos mitades y **las dos dependen de esa
API**: que el registro viva en una carpeta tuya, y que tu IA lo lea sin que salga de tu computadora —
porque el servidor MCP corre fuera del browser y lee **esa misma carpeta**.

Safari tiene OPFS y la app degrada a eso en vez de fallar, pero **no es un reemplazo**: es
almacenamiento que administra el browser, no una carpeta que la persona eligió, y no se puede leer
desde afuera. Usarlo rompe las dos mitades a la vez.

**Y desde un teléfono hay que entrar por https.** Las APIs de almacenamiento exigen contexto seguro:
https, o el literal `localhost`. Un teléfono no puede usar `localhost` para llegar a tu máquina —entra
por la IP— y por `http://` el navegador **ni define las APIs**. El síntoma es que la app reporta que
el navegador no permite guardar, y el culpable es el protocolo, no el navegador.

---

## 8. Dos trampas del repo que no son de tu escritorio

Las dos muerden temprano y ninguna avisa.

**`docs/` está en el `.gitignore`.** Si ponés documentación ahí, **git la ignora en silencio**: sin
error, sin warning, sin nada. Se descubre el día que alguien clona y no la encuentra. La razón es
buena —ahí viven transcripciones y análisis previos al código, algunos derivados de datos reales de
una persona— pero la consecuencia sorprende.

**Un worktree de herramienta de IA se commitea como submódulo fantasma.** Un worktree enlazado lleva
un `.git` que es **archivo**, así que git lo trata como repositorio embebido: un `git add -A` no
commitea los fuentes, commitea un **gitlink** —un puntero con un SHA y sin URL de submódulo—. Quien
clone después se encuentra una carpeta vacía que no puede resolver y no entiende por qué. Verificalo
antes de commitear con:

```bash
git add -A --dry-run
```

---

## 9. Antes de seguir

- [ ] ¿`dotnet --version` empieza con `9.`?
- [ ] ¿`git config core.hooksPath` devuelve `.githooks`?
- [ ] ¿`dotnet restore` pasa desde la raíz del repo?
- [ ] ¿Las comprobaciones del dominio de algún negocio corren y terminan en verde?
- [ ] ¿Corriste el ciclo **con journal**, y no sólo el del modelo puro?
- [ ] Si vas a mostrar la demo: ¿Docker corriendo y un navegador Chromium?

Con los primeros cinco ya podés hacer el capítulo 5 completo — el dominio, la lógica y la consola. El
sexto sólo hace falta para el paso 7 en adelante.
