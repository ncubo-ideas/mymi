using Choreography.Theater;
using DealershipCar.App;
using Puppeteer;

namespace DealershipCar.Mcp
{
	// LEE LA APP INSTALADA REHIDRATANDOLA DESDE SU CARPETA DE JOURNAL.
	//
	// La IA no consulta "la app corriendo": consulta la criatura, reconstruida desde su narrativa. No
	// hay protocolo entre este proceso y la app -- los dos comparten la carpeta, y el journal YA es el
	// protocolo.
	//
	// SOLO LECTURA, y son dos disciplinas distintas:
	//
	//  1. NUNCA UN PerformCommand. De DealershipCarAPI se llaman solo las lecturas. Ni Evaluate (que
	//     es comando y escribe), ni Apply, ni RegisterDevice, ni StoreCredential, ni ninguno de los
	//     nueve comandos del dominio.
	//  2. NUNCA LA SEMILLA. Por eso el host es ReadOnlyDealershipCarApp y no DealershipCarApp: el
	//     OnHydrated de la app deja una entrada en cada hidratacion aunque no haga nada.
	//
	// Y HAY UNA TERCERA QUE NO ES NUESTRA Y SE DOCUMENTA: el motor no tiene modo de solo lectura.
	// Medido en T9 -- el primer Start sobre un journal existente CREA index/index.bin y reescribe
	// meta.bin, y Start(asFollower: true) hace exactamente lo mismo. Lo que NO toca nunca es
	// journal_000001.bin, que es donde viven los hechos de la persona. Del segundo Start en adelante
	// no cambia ni un byte. Ver CONTRADICTIONS.md C30.
	internal sealed class JournalReader
	{
		private readonly string folder;
		private readonly string pinnedVin;

		internal JournalReader(string folder, string vin)
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(folder);

			this.folder = Path.GetFullPath(folder);
			this.pinnedVin = vin ?? "";
		}

		internal string Folder => folder;

		// QUE VEHICULO SE ESTA LEYENDO, o "" si la app nunca se abrio en esta carpeta.
		//
		// SE VUELVE A PREGUNTAR EN CADA CONSULTA, igual que la rehidratacion y por la misma razon: la
		// persona puede instalar y abrir su app DESPUES de que arranco este proceso. Un VIN cacheado
		// al inicio contestaria "todavia no hay datos" para siempre.
		internal string Vin
		{
			get
			{
				if (pinnedVin.Length > 0)
					return ReadOnlyDealershipCarApp.AJournalExistsFor(folder, pinnedVin)
						? pinnedVin
						: "";

				IReadOnlyList<string> vehicles = ReadOnlyDealershipCarApp.TheVehiclesIn(folder);
				return vehicles.Count > 0 ? vehicles[0] : "";
			}
		}

		// Cuantos vehiculos hay en la carpeta. Con mas de uno se lee el primero y se lo DICE, en vez
		// de contestar en silencio por un vehiculo que la persona no pregunto.
		internal int HowManyVehicles =>
			pinnedVin.Length > 0 ? 1 : ReadOnlyDealershipCarApp.TheVehiclesIn(folder).Count;

		// LA APP TODAVIA NO ESCRIBIO NADA. No es un error: es que la persona no la abrio. Se pregunta
		// ANTES de abrir el actor porque abrirlo sobre una carpeta virgen la CREA (medido, T9).
		internal bool HasJournal => Vin.Length > 0;

		// SE REHIDRATA EN CADA CONSULTA A PROPOSITO. La app instalada le escribe a esta carpeta
		// mientras este proceso vive, asi que un actor cacheado contestaria con el estado de la
		// primera pregunta: la persona apretaria Refresh, preguntaria de nuevo y veria lo viejo.
		// Medido en unos pocos KB para la historia entera, asi que releer sale barato; si el journal
		// creciera, ACA va un cache invalidado por fecha de la carpeta.
		//
		// UNA SOLA REHIDRATACION POR TOOL, y esto es lo que obliga a que las tools sean preguntas y no
		// lecturas sueltas: partir una respuesta en varias llamadas releeria el journal varias veces
		// y, peor, podria mezclar DOS ESTADOS distintos si la app escribe en el medio.
		internal T Read<T>(Func<DealershipCarAPI, T> question, Func<T> whenThereIsNoData)
		{
			ArgumentNullException.ThrowIfNull(question);
			ArgumentNullException.ThrowIfNull(whenThereIsNoData);

			string vin = Vin;
			if (vin.Length == 0) return whenThereIsNoData();

			var reader = new ReadOnlyDealershipCarApp(vin);
			try
			{
				reader.ConfigureStorage(DatabaseType.FileSystem, "path=" + folder);
				reader.Start();

				// LA CARPETA EXISTE PERO NO HAY NI UNA ENTRADA, y ese estado es alcanzable: lo deja
				// justamente un Start de solo lectura sobre una carpeta virgen, o una app que murio
				// antes de sembrar. Con el journal vacio no hay globals de raiz -- ni schedule ni
				// logbook -- y CUALQUIER lectura revienta con LanguageException ("Unknown property or
				// method 'ReadVin' on type 'Object'", medido). Es el mismo "todavia no hay datos" y se
				// contesta igual, en vez de dejar que una excepcion del motor salga como error del
				// protocolo.
				if (reader.Actor.CurrentEntryId == 0) return whenThereIsNoData();

				return question(new DealershipCarAPI(reader));
			}
			finally
			{
				try { reader.Actor.GracefulExit(); } catch { }
			}
		}
	}
}
