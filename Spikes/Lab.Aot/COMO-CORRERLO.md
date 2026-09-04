# Lab.Aot — como correrlo

    cd Spikes/Lab.Aot
    dotnet build

Solo interesan los warnings `IL####`. El resto (VSTHRD200 y demas) es ruido de otros
analizadores. Para verlos solos:

    dotnet build -v:quiet -clp:NoSummary 2>&1 | grep "warning IL"

Y para el inventario agrupado, que es como se lee:

    dotnet build -v:quiet -clp:NoSummary 2>&1 \
      | grep -o "[A-Za-z.]*\.cs([0-9]*,[0-9]*): warning IL[0-9]*" \
      | sort -u | sed 's/(.*//' | uniq -c | sort -rn

## Como se leen los codigos

    IL3050  RequiresDynamicCode        necesita generar codigo en runtime
                                       -> en iOS lo tapa el INTERPRETE de Mono
    IL2026  RequiresUnreferencedCode   el trimmer puede borrar tipos
    IL2070  reflexion sobre ctors      -> a estos los tapa MtouchLink
    IL2075  reflexion sobre miembros

## La linea base

    2026-09-01  primera medicion   37   28 de JsonSerializer + 9 de InProcessShell
    2026-09-01  despues del JSON    9   solo InProcessShell

**Hoy son 9, y los 9 estan en `InProcessShell.cs`.** Ese es el cargador dinamico de modulos, y
NO se arregla anotando: los tipos son desconocidos al compilar, que es todo su punto. O sea
que el numero ya no baja sin renunciar a cargar modulos en runtime -- que es exactamente la
decision del cuadro de NotasGnothiHost/33.

Los 28 de JSON se fueron asi:

    Gnothi.Host/HostJson.cs        JsonSerializerContext generado al compilar,
                                   para los tipos concretos del Host
    Gnothi.Assistant/Json.cs       escritura a mano con Utf8JsonWriter, para lo
                                   que viaja como `object` y no tiene tipo

Y se verifico que no cambio nada: la suite de conformance sigue diciendo que **las tres
puertas devuelven los MISMOS hechos**, byte a byte, sobre los tres modulos reales.

Si el numero sube, alguien metio reflexion nueva. Ese es el valor de tener esto: no es una
prueba que pasa o falla, es una linea base que se vigila.

**OJO CON EL ALCANCE.** Este proyecto tiene que compilar el MISMO glob que Gnothi.Mobile y
Gnothi.iOS. Si a las apps se les agrega un origen de codigo y aca no, la medicion miente por
omision -- ya paso una vez con Gnothi.Movil.

Detalle completo y que significa cada cosa en iOS: NotasGnothiHost/32 y /33.
