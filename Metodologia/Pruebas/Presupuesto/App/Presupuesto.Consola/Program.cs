using System;
using Presupuesto.Consola;

// La caratula de consola del sujeto de prueba. Mide el dominio sin browser y sin journal.
//
//     dotnet run -- comprobar

if (args.Length == 0 || args[0] != "comprobar")
{
	Console.WriteLine("Uso: dotnet run -- comprobar");
	Console.WriteLine();
	Console.WriteLine("  comprobar   corre las comprobaciones del dominio puro:");
	Console.WriteLine("              sin actor, sin journal y sin disco.");
	return 0;
}

Console.WriteLine("Comprobaciones del dominio de Presupuesto");
Console.WriteLine("(construido solo con Preguntas.md; los huecos estan en HUECOS.md)");
Console.WriteLine();

return Comprobaciones.Correr();
