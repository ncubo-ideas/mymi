using System;
using System.IO;
using System.Text;
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

		// LA FICHA ENTERA, leida por la puerta, en una sola cadena comparable. Es a la especificacion
		// lo que EveryDueStateItCanAnswer es a la agenda, y existe para comparar dos caminos que
		// tienen que dejar la misma hoja: la ficha publicada por la puerta y la MISMA ficha llegada
		// como feed de hechos sueltos.
		//
		// BARRE TODOS LOS MIEMBROS de cada conjunto cerrado y NOMBRA EL MIEMBRO en cada renglon, las
		// dos cosas a proposito. Una lectura que contestara siempre por el aceite dejaria las seis
		// clases con la misma frase, y una comparacion que solo mirara que la frase no viene vacia no
		// podria verlo -- es el defecto que T8 midio dos veces (C60). Con el miembro escrito al lado
		// de su respuesta, cualquier cruce cambia la cadena.
		internal string EverySpecificationAnswerItCanGive()
		{
			StringBuilder sheet = new StringBuilder();

			sheet.Append("model=").Append(Api.ReadModel());
			sheet.Append("\nsealedRevision=").Append(Api.HasASealedRevision());

			if (Api.HasASealedRevision())
				sheet.Append("\ncurrent=").Append(Api.ReadCurrentRevisionCode())
					.Append(" published=").Append(Api.ReadCurrentRevisionPublishedAt().ToString("O"));

			foreach (string kind in Enum.GetNames(typeof(FluidKindCode)))
				sheet.Append("\nfluid ").Append(kind)
					.Append(" : ").Append(Api.IsFluidPublished(kind))
					.Append(" | ").Append(Api.ReadFluidRevision(kind))
					.Append(" | ").Append(Api.DescribeFluid(kind));

			foreach (string position in Enum.GetNames(typeof(TyrePositionCode)))
				sheet.Append("\ntyre ").Append(position)
					.Append(" : ").Append(Api.IsTyrePublished(position))
					.Append(" | ").Append(Api.ReadTyreRevision(position))
					.Append(" | ").Append(Api.DescribeTyre(position));

			sheet.Append("\nfuel : ").Append(Api.IsFuelPublished())
				.Append(" | ").Append(Api.ReadFuelRevision())
				.Append(" | minimum=").Append(Api.ReadMinimumFuelGrade())
				.Append(" | permitted=").Append(string.Join(",", Api.PermittedFuelGrades()))
				.Append(" | prohibited=").Append(string.Join(",", Api.ProhibitedFuelGrades()))
				.Append(" | ").Append(Api.DescribeFuel());

			foreach (string kind in Enum.GetNames(typeof(OperationKindCode)))
			{
				sheet.Append("\ninterval ").Append(kind)
					.Append(" : ").Append(Api.IsServiceIntervalPublished(kind))
					.Append(" | ").Append(Api.ReadServiceIntervalRevision(kind))
					.Append(" | ").Append(
						Api.DescribeServiceInterval(kind, TheDistanceRemaining, TheUnitItAsksIn));

				sheet.Append("\nprice ").Append(kind)
					.Append(" : ").Append(
						Api.IsServicePricePublished(kind, TheMilestone, TheUnitItAsksIn))
					.Append(" | ").Append(
						Api.ReadServicePriceRevision(kind, TheMilestone, TheUnitItAsksIn))
					.Append(" | ").Append(Api.DescribeServicePrice(
						kind, TheMilestone, TheUnitItAsksIn, TheDayItAsks));
			}

			sheet.Append("\ncoverage : ").Append(Api.IsCoveragePublished())
				.Append(" | ").Append(Api.ReadCoverageRevision())
				.Append(" | systems=").Append(string.Join(",", Api.CoveredSystems()))
				.Append(" | exclusions=").Append(string.Join(" / ", Api.CoverageExclusions()))
				.Append(" | ").Append(Api.DescribeCoverageSummary());

			foreach (string systemCode in new[] { "POWERTRAIN", "SUSPENSION" })
				sheet.Append("\nvocabulary ").Append(systemCode)
					.Append(" : ").Append(Api.TheVocabularyIsTheManufacturers(systemCode));

			sheet.Append("\nservicingPolicy : ").Append(Api.IsServicingPolicyPublished())
				.Append(" | ").Append(Api.ReadServicingPolicyRevision())
				.Append(" | ").Append(Api.DescribeServicingElsewhereRule(2, 1, 0));

			return sheet.ToString();
		}

		// Los tres valores del vehiculo con que la ficha de arriba hace sus preguntas. LITERALES y no
		// calculados: una hoja leida contra el reloj daria otra vigencia manana, y lo que estas
		// comparaciones afirman es que dos caminos dejan la misma hoja (C27).
		internal const int TheMilestone = 10000;
		internal const int TheDistanceRemaining = 2000;
		internal const string TheUnitItAsksIn = "UnitA";

		internal static readonly DateTime TheDayItAsks =
			new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);

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
