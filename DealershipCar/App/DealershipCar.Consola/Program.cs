using System;

namespace DealershipCar.Consola
{
	// ANDAMIO DE T1. Todavia no hace nada: el dominio no existe (T2..T5) y la capa de
	// perform tampoco (T6). Se llena en T7.
	//
	// Esta aca desde ahora para que el esqueleto compile y para que el arbol de proyectos
	// quede armado de una sola vez, no para que haga algo util todavia.
	internal static class Program
	{
		private static int Main(string[] args)
		{
			Console.WriteLine("DealershipCar - caratula de consola");
			Console.WriteLine();
			Console.WriteLine("Todavia no hay nada que correr. El orden es:");
			Console.WriteLine("  T2  conjuntos cerrados, SchedulePolicy, DomainException");
			Console.WriteLine("  T3  IntervalRule y sus cinco subtipos");
			Console.WriteLine("  T4  ScheduledOperation");
			Console.WriteLine("  T5  MaintenanceSchedule y la fachada");
			Console.WriteLine("  T6  DealershipCarApp + DealershipCarAPI (la capa de perform)");
			Console.WriteLine("  T7  esta caratula: medir, y la historia de seis meses");
			Console.WriteLine();
			Console.WriteLine("Ver DealershipCar/PLAN-OS2-MaintenanceSchedule.md");
			return 0;
		}
	}
}
