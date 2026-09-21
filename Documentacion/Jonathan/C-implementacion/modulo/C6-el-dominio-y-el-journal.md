# C6 · El dominio y el journal, desde el lado del módulo

Las reglas que gobiernan el código que el developer de la institución escribe de verdad: las clases, y lo
que queda asentado para siempre.

---

## El dominio: cero dependencias, y eso es la prueba

El csproj del dominio **no tiene un solo `PackageReference` ni `ProjectReference`**. Y es deliberado:

> **Es la prueba de que el dominio no cambia al cruzar de hospedaje. Si algún día necesita una dependencia,
> eso es el hallazgo.**

**En el empaque cargo esto tiene nombre y apellido:** el «cero dependencias» es el `X.Dominio.dll` —ni un
`PackageReference` ni un `ProjectReference`—, mientras la superficie `X.App.dll` toma **solo**
`Gnothi.Contract`, la etiqueta mínima. Dos DLLs, dos presupuestos de dependencia. Y nada del empaque nuevo
toca lo que este documento cuenta: el journal, el Playbill y el determinismo del DSL siguen igual. Ver `C4`.

Verificado en la práctica: el proyecto de dominio del ejemplo no tiene ni una dependencia y **no se le tocó
una línea** para correr confinado. Y el patrón se repitió:

> **Cada cosa que el confinamiento rompió fue una fuga de infraestructura del motor** —primero un framework
> de servidor web, después hilos bloqueantes— **nunca del dominio.**

O sea que el confinamiento funciona como **detector de porosidad falsable**: el compilador y el runtime
trazan juntos el borde. Si tu dominio necesita cambios para correr confinado, **anotalo: es el hallazgo**.

## El actor no se crea por request

```csharp
private static PerformanceV2 examenes = ...;   // una vez, y se reusa
```

Posee **estado persistente**. Crear uno por llamada significa rehidratar el journal cada vez, y con historia
real eso es el problema de cold start que en dos años va a importar más que los megabytes.

Los puppets son **privados**. Desde afuera nadie sabe si un endpoint consultó uno o cinco.

---

## Qué queda asentado, y qué no

Esta es la decisión que más consecuencias tiene, porque **el journal es append-only: lo que entra no sale.**

| ✅ va al journal | ❌ NO va al journal |
|---|---|
| hechos que la institución emite | **tokens, cursores, reintentos** |
| aserciones de la persona, con su procedencia | el formato de salida elegido |
| configuración de *esta* instancia (sucursal, unidades) | rangos y tablas de referencia |

**La regla, de la especificación:** *«cursores, tokens y reintentos son del Host y no son historia de la
persona. Que un pull haya fallado tres veces no es un hecho de su vida.»*

**Y hay un ejemplo vivo.** La consulta `resumen` del módulo `compras` (Súper La Guaria) devuelve
`ultimoEventoLeido: 387` al lado de los totales. Ese `387` es un **cursor** —hasta qué evento del publicador
se leyó— y es justo **el dato que NO va al journal**: es contexto de sesión del Host, no un hecho de la vida
de la persona. Que la consulta lo **exponga** —para que la vista sepa si conviene refrescar— no lo vuelve un
asiento. Ver `C4`.

### El error real que salió de esto

De un journal del POC, encontrado con la introspección:

```
define action 2 (token:string, refresh:string) as
  bitacora.GuardarCredencial(token, refresh); end;
invocation action 2  'at_9d97a926…','rt_1a863845…'
```

**Credenciales en texto plano, asentadas como hechos del dominio.** Dos problemas en uno:

1. Un token es **contexto de sesión**, no un hecho. Va al **Playbill** — un segundo store llaveado por el id
   de la entrada, **fuera del journal replayable**.
2. Y en el diseño destino **esa acción no debería existir**, porque el que hace el pull y custodia la
   credencial es el Host.

**Como el journal es append-only, ahí van a quedar.** Es la clase de error que hay que cazar antes del primer
asiento de producción.

## Procedencia: no es una convención, es un mecanismo

Si los hechos de la institución y las acciones de la persona caen en el mismo journal, hay que poder
distinguirlos **para siempre** — para la UI, para la auditoría, y porque la política de purga y revocación
difiere.

El mecanismo es el **Playbill**, con esquema declarado, atado al id de la entrada. **El hecho va al journal;
quién lo aportó va al lado.**

Y un detalle que ahorra un error: **no usar el overload de `PerformCommand` que recibe `ip`/`user`** — existe
por compatibilidad y **los valores se ignoran**, no se journalean.

## Un escritor, dos autores

> **Un journal tiene UN escritor: el actor.** No dice nada sobre cuántos **autores de hechos** hay.

Los eventos de la institución y las acciones de la persona llegan los dos como comandos **al mismo actor**,
que sigue siendo el único que asienta. **No hay violación del invariante.**

Y de ahí sale una regla que no es política: el journal de un módulo institucional tiene **exactamente un
escritor legítimo** —el flujo de eventos del publicador— así que un comando **del modelo** ahí no es
«riesgoso», es **ilegítimo por construcción**.

Caso que suena ambiguo y se resuelve limpio: *«anotá que este examen me lo repitieron»* es una aserción **de
ella sobre** un hecho institucional → va a su journal, con su procedencia.

## Y por qué el hecho va donde está el dominio que lo entiende

Poner «mi kilometraje es 999» en un journal genérico y aparte **amputaría el hecho del dominio que lo
interpreta**. Ese número no vale nada suelto; vale dentro del dominio de Auto, que sabe cruzarlo contra las
cargas para sacar rendimiento y sabe que a los 5.000 km toca revisión.

---

## El determinismo del DSL: la trampa silenciosa

Esta es la más peligrosa de todas para el autor de un dominio, porque el síntoma llega **años después**.

El journal graba **el texto de la llamada**. Y a qué sobrecarga resuelve ese texto **se decide otra vez en
cada replay**, contra la versión del dominio que esté cargada entonces.

Una llamada que llegó a su parámetro **por conversión implícita** ligó *no exactamente*. El día que agregues
una sobrecarga que tome el tipo del argumento, **una entrada ya grabada empieza a ejecutar otro método** — y
sin ningún error.

**Nada supera a EXACTO:**

- literal tipado o cast explícito para numéricos
- la forma explícita para caracteres
- cast al tipo concreto para enumerados
- y **texto producido con `ToString()`, nunca con un cast**

## El versionado, dicho desde el lado del módulo

El journal se reproduce contra tu dominio actual **interpretado por el sustrato actual**. Entonces la
compatibilidad que importa **no es de API: es determinismo de replay entre versiones.**

Si una versión del sustrato ejecuta un constructo del DSL aunque sea un poco distinto, replayar cinco años de
historia produce **otro estado**.

De ahí dos cosas que te afectan directamente:

**Declarás un rango, no una versión** — si pineás una versión puntual, cada actualización del Host huerfaniza
tu módulo.

**Y el rango no lo declarás vos: el sustrato se lo gana.** Con una suite de journals dorados — journals
congelados junto con el estado que deben producir, replayados contra cada candidato. Eso convierte «mantenemos
compatibilidad» de promesa en **afirmación falsable**.

---

## Los ajustes que se fijan al crear y no se pueden cambiar

| ajuste | default | qué implica |
|---|---|---|
| `maxFileSize` | **4 MB** | rotación de archivo. Sirve para respaldos incrementales: solo el delta |
| `compression` | None | **no tiene umbral**: si se pide, comprime siempre. En registros chicos **puede agrandarlos** |
| `encryption` | None | AES-256-GCM, **autenticado**: detecta modificación, no solo oculta |

Cosas que hay que saber antes de decidir:

- El orden es **comprimir → cifrar**. Cifrar primero destruiría la compresibilidad.
- **Cada registro dice cómo está codificado** ⇒ el journal se puede leer sabiendo solo la llave, sin conocer
  la configuración con que se escribió.
- Combinado con append-only: **nadie puede editar un hecho sin que la lectura falle.**
- **Costo: 28 bytes por registro.** Sobre un evento de menos de 100 bytes eso es del orden del 30% —
  **«menos de 100 bytes por evento» y «cifrado activado» no son simultáneamente ciertas.** Reportarlas por
  separado.
- **El modo es propiedad del journal, no de la sesión.** Pedir otro después **lanza excepción**.

⇒ **Son campos del manifiesto, no configuración de runtime.** Si no se dice, alguna institución va a
descubrir a los dos años que activar cifrado significa **empezar un journal nuevo**.

## Y una cosa que el módulo tiene derecho a hacer

**Marcar hechos como no podables.** La purga la **propone** el Host y la **ejecuta** el módulo — porque el
dominio es el único que sabe qué es prescindible.

La serie de diez años de un analito sostiene el caso de un paciente; el tiquete de bus de hace tres años no.
**Que el Host decidiera por tamaño sería destruir conocimiento por peso.**
