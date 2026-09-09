using System.Runtime.CompilerServices;

// La Superficie llama a la puerta, y la puerta sigue siendo `internal`: no es API publica de
// nadie, es el paso previo al perform. Un grant nominal la mantiene asi sin obligar a abrirla.
//
// Las pruebas NO estan aca a proposito: entran por reflexion, que es lo que las obliga a
// ejercitar la puerta como la ejercita quien la llama y no como un detalle de implementacion.
[assembly: InternalsVisibleTo("Concesionario.App")]
