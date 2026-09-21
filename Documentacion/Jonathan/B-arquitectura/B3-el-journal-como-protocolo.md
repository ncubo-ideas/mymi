# B3 · El journal como protocolo, y el invariante de un escritor

## Un escritor, varios autores

La distinción se confundió una vez y produjo un diseño peor, así que vale precisarla:

> **Un journal tiene UN escritor: el actor.** No dice nada sobre cuántos **autores de hechos** hay.

Los eventos de la institución y las acciones de la persona llegan los dos como comandos **al mismo
actor**, que sigue siendo el único que asienta. **No hay violación del invariante.**

```
   institución  ──────►  ┌──────────────────────┐   un escritor: el actor
   (eventos)             │   el journal de ella │   dos autores de hechos
                         │   en su módulo       │
   la persona   ──────►  └──────────────────────┘   procedencia atada a cada
   (desde la view)                                  asiento, para siempre
```

La formulación que queda, y sirve también para el pitch:

> **El journal es de la persona. El dominio es de la institución. Los hechos pueden venir de las dos.**

**Por qué importa y no es un detalle:** poner «mi kilometraje es 999» en un journal genérico y aparte
**amputaría el hecho del dominio que lo entiende**. Ese número no vale nada suelto; vale dentro del
dominio de Auto, que sabe cruzarlo contra las cargas para sacar rendimiento y sabe que a los 5.000 km
toca revisión. **El hecho va donde está el dominio que lo interpreta.**

## Una regla que sale del invariante, no de una política

El journal de un módulo institucional tiene **exactamente un escritor legítimo**: el flujo de eventos del
publicador. Entonces un comando **del modelo** ahí no es «riesgoso», es **ilegítimo por construcción**.

Caso que suena ambiguo y se resuelve limpio: *«anotá que este examen me lo repitieron»* es una aserción
**de ella sobre** un hecho institucional → va a su journal, con su procedencia.

## Procedencia: atributo del asiento, no convención

Si los hechos de la institución y las acciones de la persona caen en el mismo journal, hay que poder
distinguirlos **para siempre**: para la UI (la factura dice lo que dice; tu anotación dice lo que
anotaste), para la auditoría, y porque la política de purga y revocación difiere.

El mecanismo es el **Playbill**: esquema declarado, atado al `entryId` del journal. **El hecho va al
journal; quién lo aportó va al lado.** Campos a declarar: autor (institución / persona / modelo), id del
publicador, firma.

Y un detalle que ahorra un error: **no usar el overload de `PerformCmd` que recibe `ip`/`user`** — existe
por compatibilidad y los valores **se ignoran**, no se journalean.

## Las cuatro categorías de dato

Confundirlas rompe cosas:

| categoría | dueño | escritura | se comparte |
|---|---|---|---|
| **Journal institucional** (exámenes, cargas, compras) | la persona | append-only, emisor institucional | jamás |
| **Repertorio** (rangos, horarios, reglas) | la institución | se reemplaza por versión | idéntico para todos |
| **Aserción propia** («soy vegetariana», umbrales, metas) | la persona | append-only, **autora = ella** | jamás |
| **Derivado externo** (algo que el asistente buscó) | nadie / el mundo | se cachea como repertorio, con fecha | — |

**Si los horarios del bus entran al journal** se rompe la inmutabilidad —los horarios cambian, y que hayan
cambiado **no es un hecho de la vida de la persona**— y se duplica el mismo contenido en cada usuario.

**Criterio falsable:** un cambio de rango de referencia produce un módulo nuevo y **no toca ningún journal
existente**. Si para actualizar un rango hay que escribir en el journal de alguien, el repertorio se
filtró a la historia.

---

## El journal como canal: el journal YA es el protocolo

Cuando dos procesos comparten una carpeta de journal, **no hace falta inventar un protocolo entre ellos**.
Escrito como decisión en el POC:

> *«No le habla al browser ni el browser le habla a él. Los dos comparten la carpeta, y el journal YA es el
> protocolo: no hay que inventar uno.»*

Un registro append-only en disco ya es un canal: **durable, ordenado, con procedencia, y que no exige que
las dos partes estén vivas al mismo tiempo.**

Eso quedó demostrado sin buscarlo: la app escribió el journal, la máquina se reinició, y después un
proceso distinto leyó ese mismo dato de una app que ya ni existía.

## Y el precio: una carpeta no tiene dueño

Una tubería tiene un dueño claro. **Una carpeta la puede abrir cualquiera**, así que la disciplina de un
solo escritor hay que ponerla a mano. Y ahí hay dos cosas que costaron caro:

### El servidor de lectura no puede usar el host de la app

Si la app emite un comando de siembra al arrancar —cosa normal— y un segundo proceso usa el mismo host,
**cada consulta escribiría en el journal de la persona** y rompería el invariante. De ahí una clase
aparte: mismo actor, mismo dominio, hidratación vacía.

Verificado en el POC: nueve llamadas de protocolo dejaron el journal **byte por byte idéntico**.

### Pero «solo lectura» era cierto sobre los asientos, no sobre la carpeta

Este es el hallazgo que costó un bug entero, y es sutil:

**El actor nunca asienta. Pero el storage sí escribe archivos al inicializar.** Hidratar un actor **no es
una lectura pasiva**: crea el layout completo del journal (`outbox`, `materialization`, `skips`,
`reactions`, `followers`, `index`, `elision`) y toca `meta.bin`.

Medido, con fechas:

```
12:34:53   el proceso A escribe journal_000001.bin
12:36:32   el proceso B crea outbox, materialization, skips, reactions, followers, index, elision
12:38:31   el proceso B modifica meta.bin
```

Si el proceso A tenía handles cacheados de esa carpeta —como los tiene un browser con la File System
Access API— **su siguiente escritura muere**, con un error que habla del estado cambiado en disco.

**Son dos escritores sobre un journal.** Y son dos cosas distintas que estaban juntas en la cabeza de
todos:

| | ¿escribe? |
|---|---|
| El actor, en modo solo-lectura | **no** — cero asientos, verificado |
| El storage, al inicializar | **sí** — crea directorios y toca metadata |

## Cómo se mitigaba, y cómo se cerró

**Se mitigaba** desalojando el módulo después de cada llamada, en vez de dejarlo levantado. Eso bajaba la
ventana de «para siempre» a los milisegundos de una consulta. Y era correcto por otra razón: N módulos
vivos en un dispositivo es RAM real, y el módulo no inicia nada por su cuenta, así que no había nada que
mantener despierto. **Pero eso quedó como historia del modelo proceso-aparte:** mitigaba, no cerraba —
durante la llamada los dos escritores coexistían.

**Se cerró de verdad** cuando el módulo dejó de ser un proceso aparte y lo pasó a levantar el Host en su
propio proceso (`InProcessShell`): existe **una sola instancia**, así que el bug de dos escritores **no
puede ocurrir**. «El eslabón sin transporte de `B2`» dejó de ser promesa y pasó a ser hecho.

## Los tres ajustes que se fijan al crear y no se pueden cambiar

Esto es especificación del motor, y es campo de manifiesto, no configuración de runtime:

| ajuste | valores | default |
|---|---|---|
| `maxFileSize` | entero > 0 | **4 MB** — rotación de archivo |
| `compression` | None / GZip | None |
| `encryption` | None / Aes256Gcm | None |

- Se aplica **al payload de cada registro**, y el orden es **comprimir → cifrar** (cifrar primero
  destruiría la compresibilidad).
- **Cada registro dice cómo está codificado** ⇒ el journal se puede leer sabiendo solo la llave, sin
  conocer la configuración con que se escribió.
- **El modo es propiedad del journal, no de la sesión.** Se graba al crearlo, y pedir otro modo después
  **lanza excepción**.
- **AES-256-GCM es autenticado**: detecta modificación, no solo oculta. Combinado con append-only:
  **nadie puede editar un hecho sin que la lectura falle.**
- **Costo: 28 bytes por registro.** Sobre un evento de menos de 100 bytes eso es del orden del 30% —
  **«menos de 100 bytes por evento» y «cifrado activado» no son simultáneamente ciertas.** Reportarlas por
  separado.
- La llave va al keystore del sistema. Protege el **reposo** (respaldo, dispositivo perdido); **no**
  protege contra un Host comprometido, porque en la consulta se descifra en memoria.

⇒ **Si la especificación no dice que son campos del manifiesto, alguna institución va a descubrir a los
dos años que activar cifrado significa empezar un journal nuevo.**

## Y el determinismo, para quien escriba dominio

El journal graba **el texto de la llamada**, y a qué sobrecarga resuelve ese texto **se decide otra vez en
cada replay**, contra la versión del dominio cargada entonces.

Una llamada que llegó a su parámetro por conversión implícita ligó **no exactamente**; el día que el autor
agregue una sobrecarga que tome el tipo del argumento, una entrada ya grabada empieza a ejecutar **otro
método** — silenciosamente.

**Nada supera a EXACTO.** Escribir siempre la forma explícita: literal tipado o cast para numéricos, y
texto producido con `ToString()`, nunca con un cast.
