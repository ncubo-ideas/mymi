// ANDAMIO DE T1. El servidor MCP de verdad se escribe en T9.
//
// LA LANDMINE, puesta desde el primer dia para que nadie la reintroduzca: con transporte
// stdio, STDOUT ES EL CANAL JSON-RPC. El replay de Puppeteer imprime su progreso con
// Console.Write ("1%2%3%...", y un "Starting ...'s Actor" en cada arranque; medido en T0).
// En una consola eso es ruido; aca CORROMPE EL PROTOCOLO.
//
// Se desvia ANTES de tocar el motor y para todo el proceso. Es la primera linea a proposito.
Console.SetOut(Console.Error);

Console.Error.WriteLine("DealershipCar.Mcp: todavia no implementado. Se escribe en T9.");
Console.Error.WriteLine("Ver DealershipCar/PLAN-OS2-MaintenanceSchedule.md");

// A proposito NO se registra en el .mcp.json de la raiz todavia: un servidor que arranca y
// se muere haria fallar el arranque de Claude Code en cada sesion de este repo. La entrada
// se agrega en T9, cuando haya algo que servir, y SIEMPRE local — ese archivo no se commitea.
return 0;
