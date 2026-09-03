using System;
using System.IO;

namespace DealershipCar.Dominio.Pruebas
{
	// UNA CARPETA TEMPORAL PROPIA, para las pruebas del nivel B que abren su host a mano en vez de
	// pasar por AScheduleOnARealJournal -- las que necesitan ver el actor, o abrir y cerrar la misma
	// carpeta con dos apps distintas.
	//
	// CUELGA DE LA MISMA RAIZ que el resto del nivel B, para que el barrido de arranque que ya
	// existe -- TheTemporaryJournalsDoNotOutliveTheNextRun -- las alcance sin saber que existen.
	internal static class AFreshJournalFolder
	{
		internal static string Make()
		{
			string made = Path.Combine(
				AScheduleOnARealJournal.TheRootOfEveryTemporaryJournal,
				Guid.NewGuid().ToString("N"));

			Directory.CreateDirectory(made);

			return made;
		}

		// Se INTENTA borrar, y casi nunca se puede mientras el proceso vive: el motor no suelta el
		// archivo del journal. Quien de verdad garantiza que no se acumulen es el barrido del
		// arranque, que corre cuando el proceso anterior ya murio.
		internal static void Sweep(string directory)
		{
			try
			{
				if (Directory.Exists(directory)) Directory.Delete(directory, true);
			}
			catch (IOException)
			{
			}
			catch (UnauthorizedAccessException)
			{
			}
		}
	}
}
