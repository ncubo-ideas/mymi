using System.Runtime.CompilerServices;

// La superficie MCP se queda internal -- es de esta caratula y de nadie mas -- y las pruebas la
// ven desde atras, igual que el nivel B ve la puerta.
//
// Que las tools se prueben importa por lo mismo que importa el nivel B: lo que se afirma de este
// proyecto -- que ninguna pregunta escribe, que una carpeta sin app contesta en vez de reventar,
// que las dos lecturas guardadas no salen como error del protocolo, y que la credencial de la
// persona no viaja en ninguna respuesta -- no lo puede comprobar ninguna prueba del dominio.
//
// Esto NO es el AssemblyInfo del dominio ni relaja su regla: ahi siguen siendo exactamente dos
// amigos, y ninguno es este proyecto. DealershipCar.Mcp no referencia DealershipCar.Dominio.
[assembly: InternalsVisibleTo("DealershipCar.Dominio.Pruebas")]
