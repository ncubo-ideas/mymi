using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Choreography.Theater;
using Concesionario.App;
using NUnit.Framework;
using Puppeteer;

namespace Concesionario.Dominio.Pruebas
{
	// EL BORDE DE LA PUERTA PARA UN DIA DE CALENDARIO. Un dia llega RECIBIDO, dicho por la sucursal
	// en yyyy-MM-dd, y una forma que este registro no lea no es un dia menos preciso: no es ninguno.
	// El dominio ya lo dice en el constructor de StayDay -- y ahi el rechazo cuesta una entrada
	// permanente, porque para cuando el cuerpo del comando corre el motor ya la escribio.
	//
	// ASI QUE SE RECHAZA ANTES DE PERFORMAR, que es la unica forma de rechazarlo sin dejarle nada al
	// journal de la persona. Es la misma linea que WireName traza para el nombre de un miembro de un
	// conjunto cerrado, un tipo mas alla.
	//
	// Y EL ORACULO ES EL JOURNAL BYTE A BYTE, que es el correcto justo aca: un dia rechazado no tiene
	// que escribir POCO, tiene que escribir NADA. Un conteo de entradas no distinguiria una entrada
	// nueva de una que ya estaba.
	//
	// LO QUE ADay TIENE PROHIBIDO ES MEJORAR. Un instante NO es un dia: sacarle el dia pide una zona
	// horaria que este registro no tiene, y el dominio dice cada instante con su hora justamente para
	// que "2026-06-10 08:29:59" y "2026-06-10 08:30:00" no se lean como el mismo texto. Un borde que
	// se tragara un instante y lo recortara a su fecha seria esa zona inventada, escrita en el sitio
	// donde nadie la buscaria.
	//
	// ADayRange TODAVIA NO TIENE LLAMADOR EN PRODUCCION: las ocho lecturas de periodo son de SH-4.
	// Se escribe y se ejercita ahora porque las dos mitades del borde son una sola decision, y porque
	// un script que nunca corrio se descubre igual de tarde que uno que nunca existio.
	//
	// SE ALCANZAN LOS DOS POR REFLEXION. Son privados de la puerta, como InUtc, Normalized y TheText,
	// y la prueba entra por donde entra su llamador de verdad: por los comandos.
	[TestFixture]
	public sealed class TheDoorRefusesADayThatIsNotADayBeforePerforming
	{
		private const string TheVin = "5NPE24AF2FH123456";
		private const string TheModel = "YARIS-2019";
		private const string TheBranch = "Taller Curridabat";
		private const string TheSubject = "Cambio de aceite";

		private static readonly DateTime Entered =
			new DateTime(2026, 6, 10, 8, 30, 0, DateTimeKind.Utc);

		private static readonly DateTime Collected =
			new DateTime(2026, 6, 12, 16, 45, 0, DateTimeKind.Utc);

		// LAS FORMAS QUE NO SON UN DIA. Las tres ultimas son la mitad que importa: un instante, en las
		// tres formas en que el resto del modulo lo escribe.
		private static readonly string[] WhatDoesNotSayADay =
		{
			"10/06/2026",
			"2026-6-10",
			"2026/06/10",
			"20260610",
			"10 de junio",
			" 2026-06-10",
			"2026-06-10 ",
			"2026-06-10 08:30:00",
			"2026-06-10T08:30:00Z",
			"2026-06-10T08:30:00.0000000Z",
		};

		private string folder;
		private DealershipCarApp app;
		private DealershipCarAPI api;

		[SetUp]
		public void Setup()
		{
			folder = ATemporaryFolder();
			Open();
		}

		[TearDown]
		public void TearDown()
		{
			Close();
			Sweep(folder);
		}

		// LA LEY, POR LOS DOS COMANDOS QUE RECIBEN DIAS, y con el journal medido byte a byte alrededor
		// de cada rechazo.
		[Test]
		public void NeitherCommandThatTakesDaysWritesAnythingWhenADayIsNotADay()
		{
			api.RecordShopEntry(TheBranch, TheSubject, Entered);

			string before = TheBytesOf(folder);

			foreach (string notADay in WhatDoesNotSayADay)
			{
				Assert.Throws<ArgumentException>(
					() => api.RecordShopCollection(Collected, new[] { "2026-06-10", notADay }),
					"'" + notADay + "' no dice un dia, y la entrega lo lleva.");

				Assert.Throws<ArgumentException>(
					() => api.RecordShopStay(
						TheBranch, TheSubject, Entered, Collected, new[] { notADay }),
					"'" + notADay + "' tampoco lo dice por la ruta del acto unico.");
			}

			Assert.That(TheBytesOf(folder), Is.EqualTo(before),
				"BYTE A BYTE IGUAL despues de los veinte rechazos. Es la afirmacion correcta justo "
				+ "aca: un dia rechazado no tiene que escribir poco, tiene que escribir NADA.");
		}

		// Y EL RECHAZO NOMBRA EL PARAMETRO, que es lo que le deja a quien registra por donde empezar.
		[Test]
		public void TheRefusalNamesTheParameterAndTheTextThatDoesNotSayADay()
		{
			ArgumentException refusal = Assert.Throws<ArgumentException>(
				() => api.RecordShopStay(
					TheBranch, TheSubject, Entered, Collected, new[] { "10/06/2026" }));

			Assert.That(refusal.ParamName, Is.EqualTo("coveredDays"));
			Assert.That(refusal.Message, Does.Contain("10/06/2026"));
			Assert.That(refusal.Message, Does.Contain("yyyy-MM-dd"),
				"Y dice la forma que si se lee, porque el dia lo escribe quien manda el hecho.");
		}

		// EL DIA SE RECHAZA ANTES QUE EL ESTADO. Sin ninguna estadia abierta, una entrega con un dia
		// malformado se refusa por el DIA y no por el pre-chequeo: la forma de lo que llego no depende
		// de en que estado esta el registro, y preguntarle al actor por algo que nunca iba a ser legal
		// es una consulta de mas.
		[Test]
		public void TheDayIsRefusedBeforeTheStateIsEvenAskedAbout()
		{
			Assert.That(api.IsInsideTheShopAt(Collected), Is.False,
				"No hay ninguna estadia: el pre-chequeo de la entrega refusaria esta llamada.");

			ArgumentException refusal = Assert.Throws<ArgumentException>(
				() => api.RecordShopCollection(Collected, new[] { "10/06/2026" }),
				"Pero lo que sale es el rechazo del DIA. Si saliera el del estado, el borde estaria "
				+ "corriendo despues de la consulta en vez de antes.");

			Assert.That(refusal.ParamName, Is.EqualTo("coveredDays"));
		}

		// ADay NO MEJORA NADA: un dia bien dicho vuelve TAL CUAL. El borde parsea y vuelve a escribir,
		// asi que lo que cruza el wire es canonico por construccion; que eso sea la identidad para un
		// yyyy-MM-dd exacto es lo que separa "normalizar" de "arreglarle la ortografia a un hecho
		// recibido".
		[Test]
		public void ADayGivesBackTheDayItWasGivenAndImprovesNothing()
		{
			Assert.That(ADay("2026-06-10"), Is.EqualTo("2026-06-10"));
			Assert.That(ADay("2026-12-31"), Is.EqualTo("2026-12-31"));
			Assert.That(ADay("2024-02-29"), Is.EqualTo("2024-02-29"),
				"Y un 29 de febrero de un ano bisiesto es un dia; el borde no tiene que saber cuantos "
				+ "dias tiene febrero para decirlo.");

			Assert.Throws<ArgumentException>(() => ADay("2026-02-30"),
				"Y el 30 de febrero no lo es, en ningun ano.");
			Assert.Throws<ArgumentException>(() => ADay("2026-06-10 08:30:00"),
				"UN INSTANTE NO ES UN DIA. Recortarlo a su fecha seria inventar la zona horaria que "
				+ "este registro no tiene, en el sitio donde nadie la buscaria.");
		}

		// ADayRange: los dos extremos son dias, y el segundo no va antes que el primero.
		[Test]
		public void ADayRangeRefusesABoundThatIsNotADayAndAnEndBeforeItsStart()
		{
			Assert.DoesNotThrow(() => ADayRange("2026-01-01", "2026-12-31"));
			Assert.DoesNotThrow(() => ADayRange("2026-06-10", "2026-06-10"),
				"Un periodo de un solo dia es un periodo: los dos extremos son inclusivos.");

			ArgumentException backwards = Assert.Throws<ArgumentException>(
				() => ADayRange("2026-12-31", "2026-01-01"));

			Assert.That(backwards.Message, Does.Contain("2026-12-31"));
			Assert.That(backwards.Message, Does.Contain("2026-01-01"),
				"Y el rechazo nombra los DOS extremos, que es lo que dice cual de los dos mover.");

			Assert.Throws<ArgumentException>(() => ADayRange("10/06/2026", "2026-12-31"));
			Assert.Throws<ArgumentException>(() => ADayRange("2026-01-01", "31/12/2026"));
		}

		// ===== LOS DOS MIEMBROS PRIVADOS DEL BORDE, ALCANZADOS COMO SE ALCANZA InUtc ==========

		private static string ADay(string text)
		{
			return (string)Invoke("ADay", new object[] { text, "coveredDays" });
		}

		private static object ADayRange(string fromDay, string toDay)
		{
			return Invoke("ADayRange", new object[] { fromDay, toDay });
		}

		private static object Invoke(string name, object[] arguments)
		{
			MethodInfo edge = typeof(DealershipCarAPI).GetMethod(
				name, BindingFlags.NonPublic | BindingFlags.Static);

			Assert.That(edge, Is.Not.Null,
				"'" + name + "' sigue siendo el borde de la puerta. Si se renombro o se mudo, esta "
				+ "prueba mide otra cosa y hay que volver a apuntarla antes de creerle.");

			try
			{
				return edge.Invoke(null, arguments);
			}
			catch (TargetInvocationException wrapped)
			{
				throw wrapped.InnerException;
			}
		}

		private void Open()
		{
			app = new DealershipCarApp(TheVin, "Standard", TheModel);
			app.ConfigureStorage(DatabaseType.FileSystem, "path=" + folder);
			app.Start();

			api = new DealershipCarAPI(app);
		}

		private void Close()
		{
			if (app == null) return;

			app.Dispose();
			app = null;
			api = null;
		}

		private static string ATemporaryFolder()
		{
			string made = Path.Combine(
				AScheduleOnARealJournal.TheRootOfEveryTemporaryJournal,
				Guid.NewGuid().ToString("N"));

			Directory.CreateDirectory(made);

			return made;
		}

		private static void Sweep(string directory)
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

		private static string TheBytesOf(string folder)
		{
			List<string> files = new List<string>(
				Directory.GetFiles(folder, "*", SearchOption.AllDirectories));
			files.Sort(StringComparer.Ordinal);

			StringBuilder tree = new StringBuilder();

			foreach (string file in files)
			{
				using (FileStream open = new FileStream(
					file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
				{
					byte[] content = new byte[open.Length];
					open.ReadExactly(content, 0, content.Length);

					tree.Append(Path.GetRelativePath(folder, file).Replace('\\', '/'));
					tree.Append(' ');
					tree.Append(content.Length);
					tree.Append(' ');
					tree.AppendLine(Convert.ToHexString(MD5.HashData(content)));
				}
			}

			return tree.ToString();
		}
	}
}
