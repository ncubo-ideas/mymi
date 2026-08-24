# Cómo se contribuye a Gnothi

## Activá el hook, una vez por clon

```bash
git config core.hooksPath .githooks
```

Sin eso el hook no corre — git no versiona `.git/hooks`, así que los hooks viven en
`.githooks/` y cada quien apunta su clon ahí. Es lo único que hay que hacer al clonar.

## Los commits siguen Conventional Commits, en español

```
tipo(alcance): descripción en minúscula, sin punto final
```

El asunto no pasa de **72 caracteres**. Lo que sobra va en el cuerpo, después de una línea
en blanco. El hook `commit-msg` lo verifica y, si algo falta, dice qué.

### Tipos

| Tipo | Cuándo | Equivale a |
|---|---|---|
| `func` | una capacidad nueva del producto | `feat` |
| `arreglo` | corrige un comportamiento que estaba mal | `fix` |
| `docs` | solo documentación | `docs` |
| `estilo` | formato, sin cambio de comportamiento | `style` |
| `refactor` | reescribe sin cambiar lo que hace | `refactor` |
| `rendimiento` | mejora de rendimiento | `perf` |
| `prueba` | pruebas | `test` |
| `construccion` | build, dependencias, Dockerfile, publish | `build` |
| `ci` | automatización | `ci` |
| `tarea` | mantenimiento que no entra en las anteriores | `chore` |
| `reversion` | deshace un commit anterior | `revert` |

Van **sin tilde**: son palabras clave que se escriben y se buscan con `grep`, no prosa.

La columna de la derecha existe porque la herramienta estándar (`semantic-release`,
`commitlint`, los generadores de changelog) espera `feat`/`fix` y **no va a reconocer estos
tipos**. Si algún día se automatiza el changelog, hay que configurarle el mapeo. Cambiar la
lista es una línea en `.githooks/commit-msg`.

### El alcance es el negocio, o el nodo dentro del negocio

```
func(examenes): evalua la velocidad de cambio de un analito
arreglo(examenes/url1): el service worker ya no tapa la pagina de descarga
construccion(examenes): corrige el puerto del Dockerfile de URL2
docs(examenes): explica por que los nitritos no son un bool
```

Con cuatro personas construyendo cuatro negocios en paralelo, el alcance es lo que hace
que un `git log` se pueda leer por dominio. Es la parte que más rinde de la convención.

### Un cambio que rompe compatibilidad lleva `!`

```
func(examenes)!: el evento de orina ahora exige nitritos
```

Y el cuerpo explica **qué se rompe y qué hay que hacer**, bajo `CAMBIO IMPORTANTE:`. En un dominio event-sourced esto
importa más que de costumbre: un cambio en la forma de un comando puede dejar sin
reproducir journals ya escritos.

### El cuerpo dice por qué, no qué

El diff ya dice qué cambió. El cuerpo es para lo que el diff no puede mostrar: qué se
midió, qué se descartó, qué queda abierto. Si un número se verificó, va el número.

## Saltarse el hook

```bash
git commit --no-verify
```

Existe para cuando de verdad haga falta. Si se vuelve costumbre, el problema es la
convención, no el hook — y se conversa.
