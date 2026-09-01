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

## La linea base, al 2026-09-01

37 warnings. 28 son `JsonSerializer` (arreglo mecanico: JsonSerializerContext) y 9 son de
`InProcessShell`, que es el cargador dinamico de modulos y NO se arregla anotando: los tipos
son desconocidos al compilar, que es todo el punto.

El nucleo -Dispatcher, ModuleCatalog, Module, ToolIndex, HostTools, ConsentWatch, IShell-
sale LIMPIO.

Si ese numero sube, alguien metio reflexion nueva en el nucleo. Ese es el valor de tener
esto: no es una prueba que pasa o falla, es una linea base que se vigila.

Detalle completo y que significa cada cosa en iOS: NotasGnothiHost/32.
