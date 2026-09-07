# E3 · Trampas del journal y de la concurrencia

La más caras de todas, porque el síntoma sale en un lugar y la causa está en otro proceso.

---

## 1 · Dos escritores sobre un journal

**Síntoma:**

```
No se pudo guardar en la carpeta: fallaron 1 de 2 archivos.
facturas/journal/journal_000001.bin (InvalidStateError: una operación que depende de estado
cacheado en un objeto de interfaz, pero el estado cambió desde que se leyó del disco)
```

Y **parece intermitente**, porque el archivo termina escrito igual: el fallo ocurre después.

**Causa.** Otro proceso tenía la misma carpeta abierta. Y hidratar un actor **no es una lectura pasiva**:
crea el layout completo del journal y toca la metadata. Medido, con fechas:

```
12:34:53   el proceso A escribe journal_000001.bin
12:36:32   el proceso B crea outbox, materialization, skips, reactions, followers, index, elision
12:38:31   el proceso B modifica meta.bin
```

El proceso A tenía handles cacheados de esa carpeta. Otro proceso la cambió → su siguiente escritura muere.

**Son dos escritores sobre un journal**, que es exactamente lo que el modelo prohíbe.

### La distinción que costó el bug entero

*«El servidor de lectura es solo lectura»* era cierto **sobre los asientos del journal** —el actor nunca
asienta, verificado con nueve llamadas que dejaron el archivo byte por byte idéntico— pero **no sobre la
carpeta**. El storage sí escribe archivos al inicializar.

Son dos cosas distintas y estaban juntas en la cabeza de todos.

### Cómo se mitigaba (nota histórica)

*Esto fue el parche de cuando el módulo era un proceso aparte.* Desalojar el módulo **después de cada
llamada**, en vez de dejarlo levantado desde la primera, bajaba la ventana de «para siempre» a los
milisegundos de una consulta.

**No la cerraba.** Durante la llamada los dos coexistían.

### Y cómo se cerró de verdad

Ya se cerró, del lado de Gnothi. El **modelo de carga** hace que el Host **preste el motor** y cargue el
módulo **in-process**: existe **una sola instancia**, y el bug interno —dos escritores sobre la misma
carpeta— no puede ocurrir. El eslabón sin transporte dejó de ser un plan y pasó a ser cómo funciona.
Ver `C7`.

### Pero el comercio volvió a poner dos manos sobre la carpeta

Con honestidad: se cerró de un lado y el comercio lo reabrió por otro. Ahora el que escribe no es una segunda
copia del módulo, es **el PWA** —adentro del WebView2 de `Comercio.Shell`— que hace el *pull* y **escribe** la
carpeta al actualizar; el que **lee** es el **módulo cargado dentro de Gnothi**. Durante la actualización,
los dos coexisten sobre la misma carpeta.

La regla operativa **«una superficie o la otra»** sobrevive intacta —solo que ahora corre entre el PWA y el
módulo, no entre dos copias del módulo—. Ver `C7`.

### La regla operativa de entonces (nota histórica)

*De cuando el módulo era un proceso aparte y podían quedar dos copias vivas.* **Una superficie o la otra, no
las dos a la vez** sobre la misma carpeta —la regla sigue viva arriba, ahora entre el PWA y el módulo—. Y si
el error aparecía, **mirá quién tiene la carpeta antes de tocar código**:

```bash
powershell -Command "Get-CimInstance Win32_Process -Filter \"Name='dotnet.exe'\" | Where-Object { $_.CommandLine -like '*Facturas.Mcp*' } | ForEach-Object { Write-Output $_.ProcessId }"
```

### Dos callejones que se exploraron y no eran

**Un handle colgando sin cerrar.** Real como problema —un stream que falla sin cerrarse sostiene un lock y
vuelve permanente un fallo pasajero— y el arreglo quedó (cerrar o abortar en un `finally`). **No era la
causa.**

**Una carrera entre el refresh de apertura y el del botón.** También real —la app se refresca sola al abrir
y el botón tenía su propia guarda que no cubría eso— y el arreglo quedó (un candado único para las dos
rutas). **Tampoco era la causa.**

Los dos son correctos igual. Pero si el error reaparece, **la causa a mirar primero es quién más tiene la
carpeta.**

## 2 · El service worker te hace probar el build de ayer

**Síntoma:** publicás un arreglo, recargás, y el comportamiento viejo sigue. **O al revés: algo «funciona»
que no debería.**

**Causa:** la PWA cachea la app entera.

**Nos mordió dos veces**, y una de ellas hizo retractar una medición **correcta**: el motor no arrancaba,
pero el browser servía una versión cacheada del día anterior que sí tenía el arreglo. La conclusión
verdadera se descartó por evidencia falsa.

**Cómo confirmar qué versión corrés:** DevTools → Application → Service Workers → **Unregister**, y
recargar. O recarga forzada con «Bypass for network».

**Y la lección general:** cuando una medición y un comportamiento observado se contradicen, **verificar qué
artefacto está corriendo** antes de decidir cuál de los dos está mal.

**Y cómo desaparece del lado de Gnothi:** al hospedar el PWA **local** —`Comercio.Shell` lo sirve por
WebView2— se le **quita el service worker** y se corrige el `base href` de `/app/` a `/`. Sin SW no hay
caché que sirva el build de ayer. Ver `C7`.

## 3 · El publish sobre una carpeta no vacía deja duplicados

**Síntoma:** el doble de assemblies de los esperados, y dudas sobre cuál se está usando.

**Causa:** publicar en un directorio con contenido previo no lo limpia. Con nombres hasheados por contenido
quedan **dos versiones del mismo assembly**, y aunque el manifiesto de arranque elija la correcta, el
diagnóstico se vuelve imposible.

**Regla:** limpiar antes de publicar. Y si el directorio está tomado por un proceso, borrar el contenido
alcanza — el directorio vacío no estorba.

## 4 · El literal `localhost` importa

**Síntoma:** en el browser no aparece el selector de carpeta, y no se registra el service worker.

**Causa:** el *secure context* se decide por **el string del host**. Medido: una IP loopback con otro nombre
da `isSecureContext=false`, **siendo el mismo socket**.

## 5 · El permiso de carpeta caduca al reabrir

Y el handle guardado puede quedar **colgando**: si la persona movió, renombró o borró la carpeta, el handle
sigue en la base pero ya no resuelve.

**Lo que hay que hacer con eso:** decirlo. Antes salía como una excepción cruda del navegador en la cara del
usuario; es una situación normal de la vida de una carpeta y la app tiene que saber nombrarla.

## 6 · Y un flush que falla en silencio es peor que uno que falla

Esto es principio, no bug.

Si un flush falla y la app sigue mostrando lo que tiene en memoria, el journal en disco **se queda atrás**,
y quien lo lea después —otro proceso, o la próxima apertura— **ve un estado viejo sin que nada avise**.

Eso no es la ventana de pérdida documentada, que es de milisegundos: **es una divergencia permanente y
muda.**

De ahí que el flush cuente los fallos y los reporte con nombre de archivo y motivo. El mensaje feo que
empezó esta investigación —*«fallaron 1 de 2 archivos»*— **es la feature que permitió encontrar el bug.**

## 7 · Los tokens quedaron en el journal, en texto plano

Salió de la introspección, sin buscarlo:

```
define action 2 (token:string, refresh:string) as
  bitacora.GuardarCredencial(token, refresh); end;
invocation action 2  'at_9d97a926…','rt_1a863845…'
```

**Dos problemas en uno.** Un token **no es un hecho del dominio**: es contexto de sesión, y va al Playbill —
un segundo store llaveado por el id de la entrada, **fuera** del journal replayable.

Y en el diseño destino **esa acción no debería existir**: el que hace el GET y custodia la credencial es el
Host. La especificación lo dice: *«cursores, tokens y reintentos son del Host y no son historia de la
persona. Que un pull haya fallado tres veces no es un hecho de su vida.»*

**Como el journal es append-only, ahí van a quedar.** Es la clase de error que conviene detectar temprano.
