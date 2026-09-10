# Lo que NO va en el pedido al motor · 2026-09-09

**Compañero interno de
[`2026-09-09-modo-lector-y-puerto-de-lectura.md`](2026-09-09-modo-lector-y-puerto-de-lectura.md).
Esto no se entrega.**

Se separó a propósito. Lo de allá es **qué se pide y con qué evidencia**; lo de acá es **decisión
nuestra**: cuánto vale el pedido, qué hacemos si lo rechazan, y una pregunta de diseño que conviene
plantear hablando y no escrita al final de un documento.

> Ojo con lo que **sí** quedó en el pedido y no hay que sacar: la sección **«Lo que NO está
> medido»**. Esa habla de la evidencia del propio pedido, y si él la encuentra en vez de leerla ahí,
> se cae todo lo demás.

---

## Para dimensionar el pedido: hay un camino que ya funciona sin nada de esto

Que el Host **copie** el journal a su propia área y el módulo lea la copia. Está implementado y
corrido de punta a punta — ver [`C-implementacion/host/C8-el-journal-que-escribe-otro.md`](C-implementacion/host/C8-el-journal-que-escribe-otro.md).

Así que el pedido **no es «destrábennos»** — es **«déjennos dejar de copiar»**. Es más débil de
vender, y es lo que es.

**Por qué no va en el documento que se entrega:** porque argumenta contra el propio pedido. Que
exista una alternativa es información que él va a querer, y corresponde darla — pero **hablando**, y
después de que haya leído qué se pide y qué cuesta. Escrita al final del documento invita a cerrar el
archivo antes de evaluar nada.

**Lo que compra dejar de copiar,** para tenerlo a mano cuando pregunte:

- el journal no queda **dos veces** en el aparato
- no hay desfase entre lo que la persona trajo y lo que el módulo ve
- se va el costo de refrescar, que —medido— **escala con la cantidad de entradas, no con el tamaño**:
  lo caro no es copiar, es que el motor vuelva a abrir

**Y si lo rechazan, no perdemos nada:** la copia sigue en pie, y los ocho «peros» medidos de ese
camino están en `C8`.

---

## La forma de la costura: cómo quedó planteada, y por qué

**Esto SÍ terminó en el pedido**, pero no como estaba antes.

Al principio iba al final, titulado «pregunta de diseño abierta» — y así se lee como un cabo suelto:
un pedido que termina en «y además no tenemos decidido cómo funciona esto». Ahora va **dentro del
pedido 1**, como *«Una forma más limpia, si la prefieren»*, y dice tres cosas en vez de una:

1. en qué se queda corto el mecanismo por nombre — **pasamos de un global a un diccionario global**,
   y sigue siendo un localizador de servicios escondido en un string;
2. cuál es el síntoma concreto — el módulo consulta `IsRegistered`, o sea **una preocupación del Host
   filtrada al módulo**, y puede nombrar un puerto que nadie le asignó;
3. que **si viajara el objeto en vez del nombre**, la pregunta desaparece — y que del lado nuestro no
   cambia nada, porque el `SafJournalFiles` es el mismo objeto.

**Por qué el cambio de lugar importa:** dicho al final es una duda nuestra que él tiene que resolver;
dicho ahí es que ya vimos el límite de nuestra propia solución y le ofrecemos la mejor. Lo segundo
suma; lo primero resta.

Y queda explícito que lo elegimos **por costo, no por convicción** — meter el objeto obliga a hilarlo
por el handler—, así que la decisión es de él sin que parezca que le estamos pasando trabajo.

Está anotada también en el código, en el comentario de `JournalFiles.IsRegistered`.

---

## Dónde está el detalle de cada medición

En [`F-evidencia/F1-mediciones.md`](F-evidencia/F1-mediciones.md): cada número con su instrumento, y
—lo que más vale— **las trampas de medición que se cayeron por el camino**. Cuatro que conviene
recordar antes de volver a medir algo de esto:

| trampa | qué produjo |
|---|---|
| `GracefulExit()` no libera el `JournalWriter`: su última escritura de header ocurre en el **finalizador** | un falso negativo («no escribió nada») y después un falso positivo, culpando al lector de la escritura diferida del sembrador |
| la curva de costo mantenía **fijas las 2 000 entradas** mientras variaba el peso | la volvía optimista; rehecha a tamaño constante, lo que domina es **abrir**, y abrir escala con la **cantidad de entradas** |
| un spike que **siembra su propio fixture** | las compuertas 5, 7 y 8 aparecieron solo al sembrar *desorden*; FOREIGN borra solo carpetas **vacías**, y por eso se le escaparon dos |
| el servidor de la institución sirve desde `%TEMP%\examenes-publicada\wwwroot`, no desde el repo | varias corridas usaron el `.gmod` **viejo** sin que nada lo dijera |

**Por qué el enlace no va en el pedido:** `F1` es un documento de nuestro set, con nuestro lenguaje y
nuestras vueltas — incluidos los errores que corregimos. Mandarlo a leerlo es mandarlo a auditar
nuestro proceso en vez de evaluar el parche. Si pide más detalle, se le manda; no se le ofrece de
entrada.

---

## La respuesta a la objeción del candado, lista para cuando la haga

**Estaba escrita en el pedido y se sacó.** Refutar una objeción que él todavía no hizo se lee como
«ya sabemos qué vas a decir y por qué te equivocás» — el mismo problema de tono que la apertura. La
respuesta se tiene lista y se usa **si la hace**, no antes.

Lo que va a decir, con alguna variante de:

> «Si uno tiene el archivo tomado, el otro no debería tomarlo y debería dar una excepción.»

**Primero: no hay problema de contención, y está resuelto por decisión de ellos.** Los lectores del
backend abren con `FileShare.ReadWrite | FileShare.Delete` **a propósito** —hay comentarios en su
código explicándolo—, así que un lector no excluye al dueño y el dueño puede rotar archivos mientras
el lector los tiene. La medición concurrente lo confirma: **6 rollovers, nadie lanzó, nada perdido**.
Y el control **sin** el parche tampoco rompió por contención.

**Segundo: una excepción sería la respuesta equivocada**, porque el segundo lector es legítimo. El
dueño —la app de la persona— va a tener ese journal tomado casi siempre; si el motor lanzara cada vez
que otro lo tiene, el lector **nunca podría leer**. No se trata de separar dos escritores: se trata de
que **leer no debería ser escribir**.

**Tercero, y es lo que hay que llevarlo a mirar:** ninguna de las ocho escrituras es un choque.
**Todas tienen éxito.** Eso ya quedó dicho en el pedido, en el pedido 2, como afirmación propia y no
como refutación — así que si él llega por su cuenta a la objeción, la respuesta ya la leyó.

**Y dónde su instinto SÍ acierta, que es por donde conviene entrar:** el punto 3 de la tabla. Aunque
no escriba un byte, tener `FileAccess.ReadWrite` sobre el archivo activo del dueño **te hace escritor
para el sistema operativo**. El parche abre `FileAccess.Read`. Concederle esa —que es suya, y es
cierta— antes de discutir las otras siete vale más que ganarle el punto.

---

## Los dos huecos, y la decisión de no taparlos antes de mandar

Ninguno de los dos frena el pedido. **Los dos van declarados en «Lo que NO está medido»**, que es
donde corresponde: son límites de nuestra evidencia, no cosas que escondimos.

**Escritorio de punta a punta.** Se podría cerrar en una tarde —la app de Compras escribiendo en una
carpeta real y el MCP leyéndola con `readOnly=true`, con el mismo diff de md5 que la corrida de
Android— y es justo la plataforma donde el modo lector alcanza solo, o sea la más fácil de defender.
**Se decidió NO atrasar el pedido por eso.** Si lo pide, se corre entonces.

**El consolidador de elisiones.** El lock entre procesos por `FileShare.None` es el único lugar donde
el backend diseña explícitamente para dos procesos: es **su** terreno y es lo primero que va a mirar.
No lo ejercitamos y no se cierra en una tarde.
