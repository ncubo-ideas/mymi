using System;
using System.IO;
using Presupuesto.Consola;

// La carátula de consola del sujeto de prueba.
//
//     dotnet run -- comprobar          las reglas del negocio, sin actor
//     dotnet run -- medir [carpeta]    el ciclo completo sobre un journal de verdad
//
// Los dos verbos contestan preguntas DISTINTAS y ninguno cubre al otro: `comprobar` dice
// si el dominio piensa bien, `medir` dice si se puede hospedar.

string verbo = args.Length > 0 ? args[0] : "";

if (verbo == "comprobar")
{
	Console.WriteLine("Comprobaciones del dominio de Presupuesto");
	Console.WriteLine("(construido solo con Preguntas.md; los huecos estan en HUECOS.md)");
	Console.WriteLine();
	return Comprobaciones.Correr();
}

if (verbo == "medir")
{
	string carpeta = args.Length > 1 && args[1].Length > 0
		? args[1]
		: Path.Combine(Path.GetTempPath(), "presupuesto-journal");

	Console.WriteLine("Ciclo completo sobre un journal real");
	Console.WriteLine("Carpeta: " + carpeta);
	return Medicion.Correr(carpeta);
}

Console.WriteLine("Uso: dotnet run -- <verbo>");
Console.WriteLine();
Console.WriteLine("  comprobar          las 31 comprobaciones del dominio puro:");
Console.WriteLine("                     sin actor, sin journal y sin disco.");
Console.WriteLine();
Console.WriteLine("  medir [carpeta]    abre el puppet sobre una carpeta real, siembra la");
Console.WriteLine("                     historia y corre las cinco preguntas a traves del");
Console.WriteLine("                     perform. Prueba que el dominio se pueda HOSPEDAR.");
return 0;
