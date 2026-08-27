using Choreography.Theater;
using DealershipCar.App;
using Puppeteer;

namespace DealershipCar.Mcp
{
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

		internal int HowManyVehicles =>
			pinnedVin.Length > 0 ? 1 : ReadOnlyDealershipCarApp.TheVehiclesIn(folder).Count;

		internal bool HasJournal => Vin.Length > 0;

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
