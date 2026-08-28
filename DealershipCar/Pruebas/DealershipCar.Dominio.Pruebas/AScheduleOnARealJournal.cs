using System;
using System.IO;
using DealershipCar.App;
using Puppeteer;

namespace DealershipCar.Dominio.Pruebas
{
	// EL APAREJO DEL NIVEL B: un actor journalizado de verdad, sobre una carpeta temporal propia de
	// cada prueba, y la puerta -- DealershipCarAPI -- que es por donde se le habla.
	//
	// El nivel A maneja MaintenanceSchedule directo y no toca ni motor ni disco: es la clausula de
	// E4 para cuando no hay fixture (C4). Esto es E4 propiamente, y no es repeticion: lo que se
	// prueba aca no lo puede tocar ninguna prueba del nivel A -- que cada script del DSL parsee y
	// ejecute, que cada codigo cruce como NOMBRE, que cada escalar vuelva tipado, y que la historia
	// sobreviva al replay desde el journal.
	//
	// POR QUE UNA CARPETA POR PRUEBA. T0 lo aprendio de la peor manera: con todas las sondas sobre
	// un mismo actor, un comando que fallo la validacion estatica rompio el replay de las OTRAS y
	// una de las mediciones dio un resultado equivocado por una razon que no tenia nada que ver.
	// Aislar es lo que hace que un rojo signifique algo.
	internal sealed class AScheduleOnARealJournal : IDisposable
	{
		private readonly string vin;
		private readonly string usageProfile;
		private readonly string folder;

		private DealershipCarApp app;

		internal AScheduleOnARealJournal(string vin, string usageProfile)
			: this(vin, usageProfile, ATemporaryFolder())
		{
		}

		internal AScheduleOnARealJournal(string vin, string usageProfile, string folder)
		{
			this.vin = vin;
			this.usageProfile = usageProfile;
			this.folder = folder;

			Open();
		}

		// El modelo con el que este aparejo siembra la tercera global de raiz. Es un hecho de la
		// PRIMERA apertura -- el cuerpo de un upgrade no vuelve a correr -- asi que una prueba que
		// necesite otro modelo abre su propio host, no este.
		internal const string TheModelAtDelivery = "SM-SEDAN-2025";

		// Donde viven todas las carpetas temporales del nivel B. Una sola raiz, para que barrerlas
		// sea una sola operacion -- ver TheTemporaryJournalsDoNotOutliveTheNextRun.
		internal static string TheRootOfEveryTemporaryJournal
		{
			get { return Path.Combine(Path.GetTempPath(), "dealership-car-nivel-b"); }
		}

		// La unica puerta. Las pruebas no ven el actor.
		internal DealershipCarAPI Api { get; private set; }

		// Donde quedo el journal, para las pruebas que lo miran como bytes.
		internal string Folder
		{
			get { return folder; }
		}

		// Cierra el proceso y lo vuelve a abrir sobre el MISMO journal. Todo lo que sobreviva a
		// esto viene del journal y no de un campo de C#, que es lo que E14 promete y lo que un
		// campo de C# no cumple.
		internal void CloseAndReopenFromTheJournal()
		{
			Close();
			Open();
		}

		// Se INTENTA borrar, y casi nunca se puede: MEDIDO, de 78 carpetas de una corrida se
		// borraron cero. Performance.Dispose() no suelta el archivo del journal mientras el proceso
		// vive, asi que el Delete choca con un IOException. El intento se queda igual -- es correcto
		// el dia que el motor lo suelte -- y quien de verdad garantiza que no se acumulen es el
		// barrido del arranque, que corre cuando el proceso anterior ya murio.
		public void Dispose()
		{
			Close();

			try
			{
				if (Directory.Exists(folder)) Directory.Delete(folder, true);
			}
			catch (IOException)
			{
			}
			catch (UnauthorizedAccessException)
			{
			}
		}

		// El estado de la agenda entero, leido por la puerta, en una sola cadena comparable. Es lo
		// que la prueba 16 compara entre dos corridas y lo que la prueba de replay compara entre
		// antes y despues de rehidratar.
		internal string EveryDueStateItCanAnswer()
		{
			string everything = "vin=" + Api.ReadVin();

			foreach (string kind in Enum.GetNames(typeof(OperationKindCode)))
				everything += "\n" + kind
					+ " : " + Api.ReadDueState(kind)
					+ " | deferrals=" + Api.DeferralsUsed(kind)
					+ " | " + Api.ReadDueReason(kind);

			everything += "\ndue=" + Api.CountDue()
				+ " overdue=" + Api.CountOverdue()
				+ " missed=" + Api.MissedCount()
				+ " anything=" + Api.HasAnythingDue()
				+ " next=" + (Api.HasAnythingDue() ? Api.ReadNextDueKind() : "nothing")
				+ " evaluated=" + Api.HasBeenEvaluated()
				+ " lastEvaluatedAt="
				+ (Api.HasBeenEvaluated() ? Api.LastEvaluatedAt().ToString("O") : "never");

			return everything;
		}

		private void Open()
		{
			Directory.CreateDirectory(folder);

			app = new DealershipCarApp(vin, usageProfile, TheModelAtDelivery);
			app.ConfigureStorage(DatabaseType.FileSystem, "path=" + folder);
			app.Start();

			Api = new DealershipCarAPI(app);
		}

		private void Close()
		{
			if (app == null) return;

			app.Dispose();
			app = null;
			Api = null;
		}

		private static string ATemporaryFolder()
		{
			return Path.Combine(TheRootOfEveryTemporaryJournal, Guid.NewGuid().ToString("N"));
		}
	}
}
