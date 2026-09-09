using System;
using System.IO;
using NUnit.Framework;

namespace Concesionario.Dominio.Pruebas
{
	// EL BARRIDO DEL ARRANQUE. Cada prueba del nivel B abre un actor sobre su propia carpeta
	// temporal y la borra al terminar -- salvo que casi nunca puede: MEDIDO, de las 78 carpetas de
	// una corrida se borraron CERO. Performance.Dispose() no suelta el archivo del journal mientras
	// el proceso vive, asi que el Delete choca con un IOException que el aparejo se traga.
	//
	// Dejado asi, cada `dotnet test` deja 78 carpetas en el temporal de quien corre las pruebas, y
	// no las limpia nadie. Barrer la raiz al ARRANCAR si funciona, porque el proceso de la corrida
	// anterior ya murio y solto sus archivos. El techo pasa a ser una corrida, no todas.
	//
	// Va como SetUpFixture del namespace y no como TearDown de cada prueba a proposito: lo que hay
	// que soltar es lo de la corrida PASADA, y eso solo se puede en el arranque de la siguiente.
	[SetUpFixture]
	public sealed class TheTemporaryJournalsDoNotOutliveTheNextRun
	{
		[OneTimeSetUp]
		public void SweepWhatTheLastRunCouldNotRelease()
		{
			try
			{
				if (Directory.Exists(AScheduleOnARealJournal.TheRootOfEveryTemporaryJournal))
					Directory.Delete(
						AScheduleOnARealJournal.TheRootOfEveryTemporaryJournal, true);
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
