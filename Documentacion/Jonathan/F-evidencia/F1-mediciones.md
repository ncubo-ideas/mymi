# F1 · Lo medido, con cómo reproducirlo

**Regla del proyecto:** un claim inflado que se cae se lleva puestos a los que sí aguantan. Si algo no está
en la primera tabla, decilo como hipótesis.

---

## El eslabón de agregación

| ✅ medido | evidencia |
|---|---|
| El asistente ve **una** lista con consultas de dos orígenes | tres consultas: dos del Host, una del manifiesto de un módulo |
| El catálogo se arma **sin arrancar el módulo** | lo que publica sale en el arranque, antes de que exista el proceso |
| El módulo ve al Host como **su** cliente MCP | `Server (app1-facturas 0.1.0), Client (Gnothi.Host 1.0.0.0) method 'tools/call'` |
| Arranque perezoso | `levantado: false` entre consultas — el chat de la app lo leyó y lo explicó solo |
| La respuesta pasa entera y sin tocar | `{"total":27991.5,"cantidadDeFacturas":4,"ultimoEventoLeido":104}` |
| Apagar **desaparece**, no deniega | el listado baja a dos; la llamada da `-32602 Unknown tool` |
| Lo declarado se verifica contra lo real | `coincide: true, declaradas: [...], reales: [...]` |
| El tamaño lo mide el Host **desde afuera** | `bytesDelRegistro: 1774`, por `stat` sobre la carpeta concedida |
| **Tres superficies distintas dan el mismo dato** | CLI del asistente · chat de la app · cliente propio |
| **El mismo mecanismo, con otro autor y otro dato** | compras / Super La Guaria, «resumen» → `{"hayDatos":true,"totalGastado":3174606,"cantidadDeCompras":87,"ultimoEventoLeido":387}` — Ver `C7` |

**Cómo reproducirlo:** `probar.ps1`, o `Gnothi.Prueba`, o el escenario en Docker. Ver la capa D.

## El confinamiento en Docker

| ✅ medido | resultado |
|---|---|
| El módulo **no tiene red** | única interfaz `lo`, sin DNS |
| **No puede escribir** fuera de su carpeta | falla en `/`, `/app`, `/etc`, `/usr`, `/journal-origen`; sí en `/journal` |
| **Cero capacidades del kernel** | `CapEff: 0000000000000000` |
| El journal real **no se toca** | mismo md5 y misma fecha después de N corridas |

```bash
docker compose run --rm --entrypoint /bin/sh gnothi -c 'ls /sys/class/net; touch /app/x; touch /journal/x; grep CapEff /proc/self/status'
```

## El motor

| ✅ medido | evidencia |
|---|---|
| **10029 ya trae el replay secuencial** | `10027` → 0 referencias al guard; `10029` → 1 |
| **La carátula wasm arranca con el paquete** | su total, sin `Cannot wait on monitors` |
| El paquete adelgazó | declara dos dependencias: un analizador y una librería de hashing |

## El ciclo completo de datos

| ✅ medido | evidencia |
|---|---|
| El proveedor solo expone su narrativa | un verbo de lectura, y el señuelo devuelve **403** con el token de la app |
| El asistente **no puede** disparar el refresh | después de inyectar, siguió dando el total viejo y pidió abrir la app |
| El número se mueve con el Refresh | 19.321,50 → **27.991,50**, checkpoint 103 → 104 |
| **El proveedor olvida, el journal no** | tras un reinicio, el mock perdió el evento y el journal lo conservó |

## La introspección del motor

| ✅ medido | evidencia |
|---|---|
| `IActorIntrospection` existe, con **nueve verbos** | `ShowEntry`, `ShowAction`, `ShowSymbols`, `ShowSymbol`, `ShowClass`, `ShowReactions`, `ShowReaction`, `FindPattern`, `ShowParameterPools` |
| Es **intrínseca**, no del dominio | *«every actor exposes by virtue of being Puppeteer»* |
| Es **solo lectura por contrato** | *«writing goes through the invocation surface, never through here»* |
| **El catálogo de escritura ES el journal** | tres `define action` con su firma canónica en el DSL |
| Hay un CLI del motor, **AI-first** | *«If you are Claude…: run `describe` first. That is your onboarding contract»* — *«always current, derived from runtime, not docs»* |
| Existe aislamiento por shadow | una sesión en memoria contra un journal primario, que se descarta al salir |

```bash
puppeteer show entry 2 --journal "path=<carpeta>" --actor-name <actor>
```

## Los tamaños, con la advertencia al lado

Medido sobre el conjunto que declara el manifiesto de arranque — **lo que el browser realmente baja**:

| build del motor | por la red (gz) | descargado | assemblies |
|---|---|---|---|
| ProjectReference al árbol viejo | 14,79 MB | 37,42 MB | 237 |
| **NuGet `3.0.0-beta.10029`** | **~10 MB** | ~26 MB | **199** |

De qué está hecho el peso del primero:

| | por la red | archivos |
|---|---|---|
| BCL + framework de UI | **7,32 MB** | 194 |
| **base de datos / nube / cripto** | **4,38 MB** | 17 |
| runtime + globalización | 2,12 MB | 6 |
| otros terceros | 0,54 MB | 14 |
| **el motor** | **0,38 MB** | 2 |
| **el código de la app** | **0,04 MB** | 4 |

**El código propio son 40 KB de 14,79 MB. El 0,27%.** Todo lo demás es sustrato.

Y el assembly más grande de toda la app no era el runtime: era una librería de criptografía de **4,68 MB
crudos**, arrastrada por dependencias del motor que en un browser no se ejecutan jamás. El paquete ya no las
trae.

> ⚠ **Debug-ish, sin trimming.** La especificación pide la que falta: **Release trimmed** contra el árbol
> actual. Y advierte que probablemente **degrade el argumento de tamaño y ascienda el de auditoría** — que
> es mejor, porque no envejece con el próximo release del trimmer. **No citar estos números afuera como si
> fueran el peso final.**

## El bundle instalable

| ✅ medido | evidencia |
|---|---|
| El manifiesto valida | «todos los requisitos cumplidos», con autor, versión y licencia leídos |
| El bundle extraído funciona | corrido desde un directorio limpio: 3 consultas y el total correcto |
| El ejecutable autocontenido anda solo | 70 MB de exe, 30 MB comprimido |

> El `.mcpb` y el exe autocontenido **dejaron de ser la forma de entrega**: hoy la persona
> baja **dos MSI** —uno de Gnothi, otro del comercio— y esos 70 MB quedan como lo que **pesa
> el binario**, no lo que instala la persona. Ver `C7`.

---

## Lo que NO está medido

| ❓ | por qué importa |
|---|---|
| **Que un servidor MCP pueda vivir en un `.wasm`** | es la etapa siguiente. Falta el workload de WASI y un runtime wasm; **no hay ninguno instalado** |
| Que el módulo con el motor corra bajo WASI | **bloqueado**: el guard tiene forma de plataforma |
| **Cold start con historia de 5–10 años** | la métrica que en dos años va a importar más que los megabytes, y la más fácil de instrumentar ahora |
| Que la reflexión sobreviva a trimming/AOT | spike bloqueante de tamaño |
| El aislamiento **por módulo** | el contenedor pone paredes alrededor de *todo* |
| **El cruce entre módulos de dos autores distintos** | es el corazón de la propuesta y queda **pendiente de correr con ambos módulos instalados** |
| Por qué el botón de instalar estaba bloqueado | hay un gate por versión; **no se confirmó que fuera eso** |

---

## Dos hallazgos que salieron sin buscarlos

**1 · Los tokens están en el journal, en texto plano.** Un token no es un hecho del dominio: es contexto de
sesión, y va al Playbill. Y en el diseño destino esa acción no debería existir, porque el que custodia la
credencial es el Host. Como el journal es append-only, ahí van a quedar.

**2 · El «solo lectura» era cierto sobre los asientos, no sobre la carpeta.** El actor nunca asienta,
verificado. Pero el storage **sí escribe archivos al inicializar**. Es la causa del bug de concurrencia. Ver
`E3`.

## Y una corrección de hecho que vale registrar

**El prototipo no usa OPFS.** Grep sobre todo el código: cero ocurrencias de `opfs`,
`navigator.storage`, `storage.getDirectory`. Usa la File System Access API e IndexedDB **solo** para
recordar el handle del directorio.

La distinción no es cosmética: **OPFS es un almacén privado del origen que ningún otro proceso puede
abrir** — usarlo habría matado el caso de uso.

## Y una retractación, para que quede el método

Se afirmó que una versión nueva de la aplicación **sí** soportaba el tipo de servidor `binary`, basándose en
un conteo de ocurrencias de la cadena en sus recursos.

**La evidencia no lo sostenía.** Al mirar el contexto, los hits eran falsos positivos: encodings de buffer,
gramáticas de resaltado de sintaxis. Un conteo no es una medición.
