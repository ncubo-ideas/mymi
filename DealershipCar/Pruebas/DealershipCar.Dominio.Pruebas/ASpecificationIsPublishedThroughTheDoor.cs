using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using NUnit.Framework;

namespace DealershipCar.Dominio.Pruebas
{
	// LO QUE LA PUERTA LE HACE A UN HECHO PUBLICADO ANTES DE MANDARLO, medido contra un journal de
	// verdad. El nivel A ya prueba que el dominio guarda y contesta lo que le entra; lo que solo se
	// puede ver aca es lo que pasa en el camino: que el decimal llegue con la forma que va a tener
	// despues del replay, que el instante llegue truncado, que el codigo cruce como NOMBRE, y que un
	// rechazo cueste o no cueste una entrada.
	//
	// LAS OBSERVACIONES SE HACEN POR LA PUERTA. Mientras T5 tenia los once comandos de publicacion y
	// NINGUNA lectura de la especificacion, se hacian contra el actor, con los scripts que T6 iba a
	// construir; ahora que existen, entran por Api. Lo que se mide es entonces lo mismo que
	// observaria una caratula, que es la unica forma de ver la mitad NORMALIZADA: el journal repite
	// por su cuenta cada normalizacion de la frontera, asi que compararlo consigo mismo la deja
	// invisible, y lo que la delata es una respuesta viva contra la misma respuesta reproducida.
	[TestFixture]
	public sealed class ASpecificationIsPublishedThroughTheDoor
	{
		private const string TheVin = "1HGCM82633A004352";
		private const string TheRevision = "R1";

		// Se publica con un CERO A LA DERECHA a proposito: es el unico valor de la tabla de T0 que
		// vuelve del journal escrito distinto de como se mando.
		private const decimal TheCapacityAsThePublisherWroteIt = 2.40m;

		private static readonly DateTime Published =
			new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc);

		private AScheduleOnARealJournal journal;

		[SetUp]
		public void Setup()
		{
			journal = new AScheduleOnARealJournal(TheVin, "Standard");
		}

		[TearDown]
		public void TearDown()
		{
			journal.Dispose();
		}

		// EL DECIMAL, LAS DOS MITADES DE UNA VEZ. El journal escribe un decimal sin los ceros que le
		// sobran a la derecha (MEDIDO en T0/S2), asi que una capacidad publicada como 2.40 vuelve
		// como 2.4: mismo valor, distinto ToString. Sin normalizar en la frontera, la respuesta viva
		// dice "capacidad 2.40" y la reproducida dice "capacidad 2.4" -- la misma pregunta contestada
		// de dos maneras segun si la app se acaba de abrir, sin revision de por medio y sin nada que
		// senalar.
		[Test]
		public void APublishedDecimalIsSaidTheSameLiveAndReplayed()
		{
			ASealedSpecSheet(Published);

			string live = TheFluidClause("EngineOil");

			Assert.That(live, Does.Contain("capacidad 2.4 L"),
				"Con la forma que va a tener despues del journal, desde la primera lectura.");
			Assert.That(live, Does.Not.Contain("2.40"),
				"Y no con la que el editorial escribio, porque esa no sobrevive al replay. El "
				+ "residuo se dice en voz alta: un 2.40 del fabricante se muestra 2.4.");

			journal.CloseAndReopenFromTheJournal();

			Assert.That(TheFluidClause("EngineOil"), Is.EqualTo(live),
				"Y reproducido contesta EXACTAMENTE lo mismo. Sin normalizar en la frontera esto se "
				+ "pone rojo aca y en ningun otro lado: el valor compara igual, es el texto el que "
				+ "cambia.");
		}

		// La misma normalizacion, vista desde el journal: el argumento que quedo escrito es el que
		// la puerta mando, no una version recortada al guardarlo.
		[Test]
		public void TheDecimalTheDoorSendsIsTheOneTheJournalKeeps()
		{
			ASealedSpecSheet(Published);

			string program = TheSecondProgramOf(journal.Folder);

			Assert.That(program, Does.Contain("2.4"));
			Assert.That(program, Does.Not.Contain("2.40"),
				"Un 2.40 en el blob de argumentos querria decir que la puerta mando una forma que el "
				+ "journal no puede devolver.");
		}

		// EL INSTANTE, Y POR QUE MIRAR EL JOURNAL NO ALCANZA PARA VERLO. El journal trunca a segundos
		// enteros de todas formas (MEDIDO en OS2/S4), asi que una hoja publicada con fraccion y otra
		// sin ella escriben EL MISMO texto: comparar los dos programas deja verde una puerta que no
		// trunca. La diferencia esta entre lo VIVO y lo REPRODUCIDO, y hay que hacerle una pregunta
		// al dominio para verla.
		//
		// ESTA es esa pregunta. Dos revisiones publicadas dentro del mismo segundo se distinguen por
		// ORDEN DE LLEGADA, que es lo unico que queda cuando el instante ya no las separa. Sin
		// truncar en la puerta, la fraccion las separa mientras la app esta abierta y deja de
		// separarlas despues de reabrir: la misma pregunta, contestada por dos revisiones distintas,
		// sin que nadie haya publicado nada.
		[Test]
		public void TwoRevisionsInsideTheSameSecondAnswerTheSameLiveAndReplayed()
		{
			ARevisionPublishingTheOil("R1", Published.AddMilliseconds(500), "5W-30");
			ARevisionPublishingTheOil("R2", Published.AddMilliseconds(200), "0W-20");

			string live = TheFluidClause("EngineOil");

			Assert.That(live, Does.Contain("0W-20"),
				"Dentro de un mismo segundo manda el orden de llegada, y R2 llego despues.");
			Assert.That(live, Does.Contain("revisión R2"));

			journal.CloseAndReopenFromTheJournal();

			Assert.That(TheFluidClause("EngineOil"), Is.EqualTo(live),
				"Y reproducido contesta lo mismo. Sin truncar en la puerta, viva contestaria R1 -- "
				+ "que trae la fraccion mas alta -- y reproducida contestaria R2.");
		}

		// La otra mitad del mismo instante, del lado de la vigencia de un precio: preguntar en el
		// segundo exacto en que empieza a regir tiene que contestar lo mismo antes y despues de
		// reabrir. Sin truncar, viva dice que todavia no empieza -- por medio segundo que el journal
		// no guarda -- y reproducida dice que rige.
		[Test]
		public void APriceValidityIsJudgedTheSameLiveAndReplayed()
		{
			journal.Api.OpenRevision(TheRevision, Published);

			journal.Api.PublishServicePrice(
				TheRevision, "FluidChange", 10000, "UnitA", 75.50m, "USD",
				Published.AddMilliseconds(500), Published.AddYears(1));

			journal.Api.SealRevision(TheRevision);

			string live = ThePriceClauseAsOf(Published);

			Assert.That(live, Does.Contain("está rigiendo"));

			journal.CloseAndReopenFromTheJournal();

			Assert.That(ThePriceClauseAsOf(Published), Is.EqualTo(live),
				"El valor no cambia nunca -- un DateTime compara por ticks -- pero el juicio que se "
				+ "hace con el si, y en silencio.");
		}

		// LO QUE VIAJA COMO PARAMETRO Y LO QUE VIAJA COMO NOMBRE. Los once comandos nuevos son
		// sentencias con parametros formales, los valores van aparte, y todo conjunto cerrado cruza
		// por el NOMBRE de su miembro: renumerar un enum manana no puede reinterpretar una hoja ya
		// publicada.
		[Test]
		public void TheSecondProgramCarriesTheCodesAsNamesAndTheValuesAsArguments()
		{
			ASealedSpecSheet(Published);

			string program = TheSecondProgramOf(journal.Folder);

			Assert.That(program, Does.Contain("specification = ModelSpecification(model)"),
				"La tercera global quedo como sentencia con parametro formal, igual que las otras "
				+ "dos.");
			Assert.That(program, Does.Contain("'YARIS-2019'"),
				"Y el modelo viaja aparte, como argumento de la semilla.");
			Assert.That(program, Does.Contain("'EngineOil'"));
			Assert.That(program, Does.Contain("'Front'"));
			Assert.That(program, Does.Contain("'Minimum'"));
			Assert.That(program, Does.Contain("'FluidChange'"));
			Assert.That(program, Does.Not.Contain("EngineOil,"),
				"Y ninguno aparece suelto en el texto de una sentencia: eso seria interpolacion, y "
				+ "un dato interpolado deja de ser un dato.");
		}

		// EL CERO DE LOS LIMITES PROPIOS NO SE "ARREGLA" EN LA PUERTA. Un cero en las DOS mitades es
		// como el wire dice que el sistema no publico limites propios, y traducirlo es del dominio:
		// la puerta lo manda tal cual. Si lo tocara -- convirtiendolo en un uno, o rechazandolo --
		// el sistema quedaria con limites de cero, que es estar fuera de su sobre el dia de la
		// entrega, o no se podria publicar un sistema que se mide contra el sobre del vehiculo.
		[Test]
		public void AZeroInBothHalvesOfTheOwnLimitsTravelsUntouched()
		{
			journal.Api.OpenRevision(TheRevision, Published);
			journal.Api.PublishCoveredSystem(TheRevision, "POWERTRAIN", 0, 0, "UnitA");
			journal.Api.SealRevision(TheRevision);

			string scope = journal.Api.DescribeCoverageSummary();

			Assert.That(scope, Does.Contain("'POWERTRAIN' está dentro del alcance publicado"));
			Assert.That(scope, Does.Not.Contain("con límites propios"),
				"El sistema no publico limites propios, asi que rige el sobre del vehiculo. Un cero "
				+ "convertido en limite diria lo contrario, y lo diria en una frase que se lee bien.");
		}

		// UN RECHAZO EN LA PUERTA NO CUESTA NADA. Un texto obligatorio en blanco lo rechaza la
		// puerta con una ArgumentException antes de performar, asi que el journal queda byte a byte
		// como estaba. El dominio tambien lo rechaza, con sus propias palabras, pero para entonces
		// la entrada ya estaria escrita.
		// CADA cadena obligatoria de los once comandos, una por una. No alcanza con probar las que
		// uno se acuerde de escribir: la guarda que falta es siempre la del parametro que se agrego
		// despues, y su costo no es una excepcion fea sino una entrada permanente en el journal de
		// una persona.
		[Test]
		public void EveryRequiredTextIsRefusedAtTheDoorWhenItIsBlankAndCostsNothing()
		{
			journal.Api.OpenRevision(TheRevision, Published);

			string before = TheBytesOf(journal.Folder);

			foreach (KeyValuePair<string, TestDelegate> each in EveryRequiredTextLeftBlank())
				Assert.That(Assert.Catch(each.Value), Is.InstanceOf<ArgumentException>(), each.Key);

			Assert.That(TheBytesOf(journal.Folder), Is.EqualTo(before),
				"Ninguna se performo: el journal quedo byte a byte como estaba.");
		}

		// Y CADA parametro que nombra un miembro de un conjunto cerrado, uno por uno, por la misma
		// razon: son once ranuras y basta olvidar el WireName de una para que un nombre inventado
		// llegue al script.
		[Test]
		public void EveryClosedSetNameIsRefusedAtTheDoorWhenItNamesNoMemberAndCostsNothing()
		{
			journal.Api.OpenRevision(TheRevision, Published);

			string before = TheBytesOf(journal.Folder);

			foreach (KeyValuePair<string, TestDelegate> each in EveryClosedSetNameMadeUp())
				Assert.That(Assert.Catch(each.Value), Is.InstanceOf<ArgumentException>(), each.Key);

			Assert.That(TheBytesOf(journal.Folder), Is.EqualTo(before));
		}

		// LO QUE LA PUERTA SI TOCA Y LO QUE NO. Recorta los extremos, que es lo que hace que quien
		// publica y quien pregunta nombren la misma cosa; no toca ni la caja ni lo de adentro,
		// porque el vocabulario es del fabricante. Un codigo de sistema pasado a mayusculas "para
		// normalizar" es este modulo arreglandole la ortografia a alguien.
		[Test]
		public void ThePublishedTextIsTrimmedAtItsEndsAndTouchedNowhereElse()
		{
			journal.Api.OpenRevision(TheRevision, Published);

			journal.Api.PublishCoveredSystem(TheRevision, "  Powertrain-Hybrid  ", 0, 0, "UnitA");

			journal.Api.PublishFluidSpecification(
				TheRevision, "EngineOil", "  5W-30 Full Synthetic  ",
				TheCapacityAsThePublisherWroteIt, "UnitA", 10000, "UnitA", 365);

			journal.Api.SealRevision(TheRevision);

			Assert.That(
				journal.Api.DescribeCoverageSummary(),
				Does.Contain("'Powertrain-Hybrid' está dentro del alcance publicado"),
				"Recortado en los extremos, y nada mas: tal como lo publicaron.");

			Assert.That(TheFluidClause("EngineOil"), Does.Contain("grado 5W-30 Full Synthetic,"),
				"Los espacios de adentro son del texto publicado; los de los extremos son del "
				+ "transporte.");
		}

		// PERO UNA GUARDA DEL DOMINIO ADENTRO DEL CUERPO SI CUESTA UNA ENTRADA PERMANENTE, y las
		// guardas de la especificacion estan casi todas ahi: publicar dentro de una revision sellada,
		// sellar una vacia, nombrar una revision que no existe. Todas leen estado que la puerta no
		// proyecta todavia, asi que no hay Check que se las pueda preguntar -- el par de lecturas de
		// presencia que lo haria posible es de T6.
		//
		// Esta prueba dice que hoy es asi, no que este bien. Va en rojo el dia que se muevan.
		[Test]
		public void AGuardOfTheSpecificationThrownInsideTheBodyCostsAPermanentEntry()
		{
			ASealedSpecSheet(Published);

			string before = TheBytesOf(journal.Folder);

			Assert.Throws<DomainException>(
				() => journal.Api.PublishFuelGrade(TheRevision, "95 RON", "Permitted"),
				"La revision ya esta sellada: publicar adentro es un defecto de quien llama y el "
				+ "dominio revienta, como debe.");

			Assert.That(TheBytesOf(journal.Folder), Is.Not.EqualTo(before),
				"PERO la entrada ya se habia escrito. El motor la salta en cada rehidratacion y la "
				+ "loguea como fallo, para siempre.");
		}

		// Y la cicatriz no envenena: el estado sigue siendo el correcto y el journal rehidrata.
		[Test]
		public void TheScarredJournalStillRehydratesToTheRightState()
		{
			ASealedSpecSheet(Published);

			string clause = TheFluidClause("EngineOil");

			Assert.Throws<DomainException>(
				() => journal.Api.PublishFuelGrade(TheRevision, "95 RON", "Permitted"));

			journal.CloseAndReopenFromTheJournal();

			Assert.That(TheFluidClause("EngineOil"), Is.EqualTo(clause),
				"La entrada marcada se salta y el replay sigue.");
		}

		// UN CODIGO DE REVISION SE RECORTA EN LA PUERTA, que es lo que hace que quien publica y quien
		// sella nombren la misma revision. El dominio compara ordinal y no recorta: si la puerta
		// tampoco lo hiciera, abrir " R1 " y sellar "R1" reventaria por una diferencia que nadie ve.
		[Test]
		public void ARevisionCodeIsTrimmedAtTheDoorSoBothEndsNameTheSameRevision()
		{
			journal.Api.OpenRevision("  " + TheRevision + "  ", Published);

			journal.Api.PublishServicingPolicy(
				TheRevision, "service anywhere", "keep the receipts");

			Assert.DoesNotThrow(() => journal.Api.SealRevision(TheRevision));
		}

		// Las trece cadenas obligatorias de los once comandos, cada una en blanco por turno y el
		// resto valido. La clave de cada par es lo que se lee cuando esa es la que falla.
		private Dictionary<string, TestDelegate> EveryRequiredTextLeftBlank()
		{
			const string Blank = "   ";

			return new Dictionary<string, TestDelegate>
			{
				{ "OpenRevision.revisionCode",
					() => journal.Api.OpenRevision(Blank, Published) },
				{ "PublishFluidSpecification.revisionCode",
					() => journal.Api.PublishFluidSpecification(
						Blank, "EngineOil", "5W-30", 2.4m, "UnitA", 10000, "UnitA", 365) },
				{ "PublishFluidSpecification.grade",
					() => journal.Api.PublishFluidSpecification(
						TheRevision, "EngineOil", Blank, 2.4m, "UnitA", 10000, "UnitA", 365) },
				{ "PublishTyreSpecification.size",
					() => journal.Api.PublishTyreSpecification(
						TheRevision, "Front", Blank, 2.4m, "UnitA", "94V") },
				{ "PublishTyreSpecification.ratings",
					() => journal.Api.PublishTyreSpecification(
						TheRevision, "Front", "205/55R16", 2.4m, "UnitA", Blank) },
				{ "PublishFuelGrade.grade",
					() => journal.Api.PublishFuelGrade(TheRevision, Blank, "Minimum") },
				{ "PublishCoveredSystem.systemCode",
					() => journal.Api.PublishCoveredSystem(TheRevision, Blank, 0, 0, "UnitA") },
				{ "PublishCoverageExclusion.systemCode",
					() => journal.Api.PublishCoverageExclusion(
						TheRevision, Blank, "wear parts", "when consumable") },
				{ "PublishCoverageExclusion.exclusion",
					() => journal.Api.PublishCoverageExclusion(
						TheRevision, "POWERTRAIN", Blank, "when consumable") },
				{ "PublishCoverageExclusion.condition",
					() => journal.Api.PublishCoverageExclusion(
						TheRevision, "POWERTRAIN", "wear parts", Blank) },
				{ "PublishServicePrice.currency",
					() => journal.Api.PublishServicePrice(
						TheRevision, "FluidChange", 10000, "UnitA", 75.50m, Blank,
						Published, Published.AddYears(1)) },
				{ "PublishServicingPolicy.rule",
					() => journal.Api.PublishServicingPolicy(
						TheRevision, Blank, "keep the receipts") },
				{ "PublishServicingPolicy.whatPreservesCover",
					() => journal.Api.PublishServicingPolicy(
						TheRevision, "service anywhere", Blank) },
				{ "SealRevision.revisionCode",
					() => journal.Api.SealRevision(Blank) },
				{ "SupersedeRevision.supersededByCode",
					() => journal.Api.SupersedeRevision(TheRevision, Blank) },
			};
		}

		// Las once ranuras de conjunto cerrado de los once comandos, cada una con un nombre
		// inventado por turno.
		private Dictionary<string, TestDelegate> EveryClosedSetNameMadeUp()
		{
			const string NoMember = "NoEsMiembroDeNada";

			return new Dictionary<string, TestDelegate>
			{
				{ "PublishFluidSpecification.kind",
					() => journal.Api.PublishFluidSpecification(
						TheRevision, NoMember, "5W-30", 2.4m, "UnitA", 10000, "UnitA", 365) },
				{ "PublishFluidSpecification.capacityUnit",
					() => journal.Api.PublishFluidSpecification(
						TheRevision, "EngineOil", "5W-30", 2.4m, NoMember, 10000, "UnitA", 365) },
				{ "PublishFluidSpecification.intervalUnit",
					() => journal.Api.PublishFluidSpecification(
						TheRevision, "EngineOil", "5W-30", 2.4m, "UnitA", 10000, NoMember, 365) },
				{ "PublishTyreSpecification.position",
					() => journal.Api.PublishTyreSpecification(
						TheRevision, NoMember, "205/55R16", 2.4m, "UnitA", "94V") },
				{ "PublishTyreSpecification.pressureUnit",
					() => journal.Api.PublishTyreSpecification(
						TheRevision, "Front", "205/55R16", 2.4m, NoMember, "94V") },
				{ "PublishFuelGrade.standing",
					() => journal.Api.PublishFuelGrade(TheRevision, "91 RON", NoMember) },
				{ "PublishCoveredSystem.ownLimitUnit",
					() => journal.Api.PublishCoveredSystem(
						TheRevision, "POWERTRAIN", 0, 0, NoMember) },
				{ "PublishServiceInterval.kind",
					() => journal.Api.PublishServiceInterval(
						TheRevision, NoMember, 10000, "UnitA", 365) },
				{ "PublishServiceInterval.intervalUnit",
					() => journal.Api.PublishServiceInterval(
						TheRevision, "FluidChange", 10000, NoMember, 365) },
				{ "PublishServicePrice.kind",
					() => journal.Api.PublishServicePrice(
						TheRevision, NoMember, 10000, "UnitA", 75.50m, "USD",
						Published, Published.AddYears(1)) },
				{ "PublishServicePrice.milestoneUnit",
					() => journal.Api.PublishServicePrice(
						TheRevision, "FluidChange", 10000, NoMember, 75.50m, "USD",
						Published, Published.AddYears(1)) },
			};
		}

		private void ASealedSpecSheet(DateTime publishedAt)
		{
			ASealedSpecSheet(journal, publishedAt);
		}

		private void ARevisionPublishingTheOil(string code, DateTime publishedAt, string grade)
		{
			journal.Api.OpenRevision(code, publishedAt);

			journal.Api.PublishFluidSpecification(
				code, "EngineOil", grade, TheCapacityAsThePublisherWroteIt, "UnitA",
				10000, "UnitA", 365);

			journal.Api.SealRevision(code);
		}

		// Una hoja completa, publicada por la puerta: los seis items, cada uno por su comando.
		private static void ASealedSpecSheet(AScheduleOnARealJournal on, DateTime publishedAt)
		{
			on.Api.OpenRevision(TheRevision, publishedAt);

			on.Api.PublishFluidSpecification(
				TheRevision, "EngineOil", "5W-30", TheCapacityAsThePublisherWroteIt, "UnitA",
				10000, "UnitA", 365);

			on.Api.PublishTyreSpecification(
				TheRevision, "Front", "205/55R16", 2.4m, "UnitA", "94V");

			on.Api.PublishFuelGrade(TheRevision, "91 RON", "Minimum");

			on.Api.PublishServiceInterval(TheRevision, "FluidChange", 10000, "UnitA", 365);

			on.Api.PublishServicePrice(
				TheRevision, "FluidChange", 10000, "UnitA", 75.50m, "USD",
				publishedAt, publishedAt.AddYears(1));

			on.Api.PublishCoveredSystem(TheRevision, "POWERTRAIN", 60, 100000, "UnitA");

			on.Api.PublishCoverageExclusion(
				TheRevision, "POWERTRAIN", "wear parts", "when the part is consumable");

			on.Api.PublishServicingPolicy(
				TheRevision, "service anywhere", "keep the receipts");

			on.Api.SealRevision(TheRevision);
		}

		// LA LECTURA DE LA PUERTA. Mientras T5 tenia los once comandos y ninguna lectura, estas tres
		// se hacian a mano contra el actor; T6 las construyo, asi que ahora entran por Api como todo
		// lo demas, y lo que se observa es exactamente lo que un shell observaria.
		private string TheFluidClause(string kind)
		{
			return journal.Api.DescribeFluid(kind);
		}

		private string ThePriceClauseAsOf(DateTime asOf)
		{
			return journal.Api.DescribeServicePrice("FluidChange", 10000, "UnitA", asOf);
		}

		// El arbol entero del journal reducido a una cadena: nombre relativo mas hash del contenido,
		// en orden. Repetido a proposito -- cada fixture de esta suite es dueño de su aparejo.
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

		// El SEGUNDO PROGRAMA que el journal guarda: cada define, cada invoke y cada blob de
		// argumentos, en orden. Se saca leyendo los tramos imprimibles del binario, que es
		// exactamente lo que el motor escribe como texto -- los ticks de cada entrada y su checksum
		// son binarios y no forman tramos de este largo.
		private static string TheSecondProgramOf(string folder)
		{
			StringBuilder program = new StringBuilder();

			List<string> files = new List<string>(
				Directory.GetFiles(folder, "*", SearchOption.AllDirectories));
			files.Sort(StringComparer.Ordinal);

			foreach (string file in files)
			{
				using (FileStream open = new FileStream(
					file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
				{
					byte[] content = new byte[open.Length];
					open.ReadExactly(content, 0, content.Length);

					StringBuilder run = new StringBuilder();

					foreach (byte each in content)
					{
						if (each >= 0x20 && each <= 0x7E) { run.Append((char)each); continue; }

						if (run.Length >= 6) program.AppendLine(run.ToString());

						run.Clear();
					}

					if (run.Length >= 6) program.AppendLine(run.ToString());
				}
			}

			return program.ToString();
		}
	}
}
