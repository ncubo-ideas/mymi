using System;
using NUnit.Framework;

namespace DealershipCar.Dominio.Pruebas
{
	// LAS PRUEBAS 5 y 6 de §11 EN EL NIVEL B (leyes L1 y L11, ARevisionAppliesForward).
	//
	// QUE AGREGA EL NIVEL B. El nivel A ya prueba que la revision mas nueva contesta y que una
	// respuesta ya dada no cambia. Lo que solo se ve aca es que el ORDEN DE PUBLICACION sobreviva al
	// disco: el journal trunca el instante a segundos enteros, asi que la linea de tiempo con la que
	// se elige la revision vigente se reconstruye desde instantes que ya perdieron su fraccion. Una
	// respuesta que dependa de esa fraccion contesta una cosa viva y otra reproducida, sin que nadie
	// haya publicado nada.
	//
	// Y ES DONDE SE MIDE LA MITAD DE SALIDA DEL INSTANTE. ReadCurrentRevisionPublishedAt es la
	// primera lectura de la especificacion que pasa por AnInstant, que le vuelve a poner el sello de
	// UTC que el journal borra. Comparar los dos programas del journal NO puede defender eso -- el
	// journal repite esa normalizacion por su cuenta -- asi que lo que la mide es una respuesta VIVA
	// contra la misma respuesta REPRODUCIDA.
	[TestFixture]
	public sealed class AJournaledRevisionAppliesForward
	{
		private const string TheVin = "1HGCM82633A004352";
		private const string TheFirstGrade = "5W-30";
		private const string TheSecondGrade = "0W-20";

		private static readonly DateTime TheFirstPublication =
			new DateTime(2026, 1, 15, 9, 30, 0, DateTimeKind.Utc);
		private static readonly DateTime TheSecondPublication =
			new DateTime(2026, 7, 1, 14, 45, 0, DateTimeKind.Utc);

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

		// PRUEBA 5 DE §11, por la puerta: sellada la segunda revision, las lecturas contestan desde
		// ella y la nombran, y la primera queda Superseded.
		[Test]
		public void TheLatestSealedRevisionAnswersAndTheOneItReplacedIsSuperseded()
		{
			ARevisionPublishingTheOil("R1", TheFirstPublication, TheFirstGrade);
			ARevisionPublishingTheOil("R2", TheSecondPublication, TheSecondGrade);
			journal.Api.SupersedeRevision("R1", "R2");

			Assert.That(journal.Api.DescribeFluid("EngineOil"), Does.Contain(TheSecondGrade));
			Assert.That(journal.Api.ReadFluidRevision("EngineOil"), Is.EqualTo("R2"));
			Assert.That(journal.Api.ReadRevisionState("R1"), Is.EqualTo("Superseded"));
			Assert.That(journal.Api.ReadRevisionState("R2"), Is.EqualTo("Sealed"));
			Assert.That(journal.Api.ReadCurrentRevisionCode(), Is.EqualTo("R2"));
		}

		// PRUEBA 6 DE §11: una respuesta que se dio antes de que sellara la segunda revision sigue
		// nombrando la primera. Lo que cruza la puerta es una CADENA, asi que una respuesta ya dada
		// no puede volver a resolverse -- que es lo que la ley pide y lo que el nivel A prueba contra
		// el objeto. Aca se prueba la otra mitad: la lectura NUEVA si cambia.
		[Test]
		public void AnAnswerGivenBeforeTheNextRevisionSealedStillNamesTheOneItCameFrom()
		{
			ARevisionPublishingTheOil("R1", TheFirstPublication, TheFirstGrade);

			string given = journal.Api.DescribeFluid("EngineOil");

			ARevisionPublishingTheOil("R2", TheSecondPublication, TheSecondGrade);

			Assert.That(given, Does.Contain("revision R1"),
				"Lo que la persona recibio sigue diciendo de donde salio. Una revision no llega para "
				+ "atras a cambiarlo.");
			Assert.That(journal.Api.DescribeFluid("EngineOil"), Does.Contain("revision R2"),
				"Y preguntar de nuevo si contesta desde la nueva: aplica desde su instante HACIA "
				+ "ADELANTE, que es la otra mitad de la ley.");
		}

		// LO QUE LA SUCESORA NO REPUBLICO SE RETIRA CON LA SUSTITUCION, y lo que ni siquiera toco
		// sigue contestando desde donde salio. Las dos mitades de D4, medidas despues del replay
		// porque es ahi donde la linea de publicacion se vuelve a armar desde el disco.
		[Test]
		public void SupersedingWithdrawsOnlyWhatTheSuccessorDidNotRepublish()
		{
			journal.Api.OpenRevision("R1", TheFirstPublication);
			journal.Api.PublishFluidSpecification(
				"R1", "EngineOil", TheFirstGrade, 4.2m, "UnitA", 10000, "UnitA", 365);
			journal.Api.PublishTyreSpecification(
				"R1", "Front", "205/55R16", 2.4m, "UnitA", "94V");
			journal.Api.SealRevision("R1");

			ARevisionPublishingTheOil("R2", TheSecondPublication, TheSecondGrade);

			Assert.That(journal.Api.IsTyrePublished("Front"), Is.True,
				"R2 solo corrigio el aceite: la llanta que no toco sigue contestando desde R1.");
			Assert.That(journal.Api.ReadTyreRevision("Front"), Is.EqualTo("R1"));

			journal.Api.SupersedeRevision("R1", "R2");
			journal.CloseAndReopenFromTheJournal();

			Assert.That(journal.Api.IsTyrePublished("Front"), Is.False,
				"Sustituida R1, lo que R2 no republico se retira con ella: nadie responde ya por esa "
				+ "llanta, y contestarla igual seria citar una publicacion apagada.");
			Assert.That(journal.Api.ReadFluidRevision("EngineOil"), Is.EqualTo("R2"),
				"Y el aceite, que R2 si republico, sigue contestando.");
		}

		// EL INSTANTE DE LA REVISION VIGENTE, VIVO Y REPRODUCIDO. El journal borra el DateTimeKind, y
		// eso no cambia ningun valor -- un DateTime compara por ticks -- pero si cambia lo que un
		// shell hace con el: ToString("O") y ToLocalTime dan otra cosa. Sin volver a sellar el UTC en
		// la salida, la app diria una hora recien abierta y otra despues de reabrir.
		[Test]
		public void ThePublicationInstantOfTheCurrentRevisionComesBackInUtcLiveAndReplayed()
		{
			ARevisionPublishingTheOil("R1", TheFirstPublication, TheFirstGrade);

			DateTime live = journal.Api.ReadCurrentRevisionPublishedAt();

			Assert.That(live, Is.EqualTo(TheFirstPublication));
			Assert.That(live.Kind, Is.EqualTo(DateTimeKind.Utc));

			journal.CloseAndReopenFromTheJournal();

			DateTime replayed = journal.Api.ReadCurrentRevisionPublishedAt();

			Assert.That(replayed, Is.EqualTo(live));
			Assert.That(replayed.Kind, Is.EqualTo(DateTimeKind.Utc),
				"El journal no guarda el Kind, asi que reproducido vuelve Unspecified si la puerta no "
				+ "lo vuelve a sellar.");
			Assert.That(replayed.ToString("O"), Is.EqualTo(live.ToString("O")),
				"Y esta es la comparacion que lo delata: el valor era igual en los dos casos, el "
				+ "TEXTO no. Es tambien la unica forma de verlo -- el journal repite por su cuenta "
				+ "todo lo que la puerta normaliza, asi que compararlo consigo mismo no dice nada.");
		}

		// LA VIGENTE ES LA SELLADA MAS NUEVA POR INSTANTE DE PUBLICACION, no la ultima que llego.
		// Una correccion que el fabricante fecha antes no se vuelve la vigente por haber entrado
		// despues, y eso tambien tiene que sobrevivir al replay.
		[Test]
		public void TheCurrentRevisionIsTheLatestByPublicationInstantAndNotTheLastToArrive()
		{
			ARevisionPublishingTheOil("R2", TheSecondPublication, TheSecondGrade);
			ARevisionPublishingTheOil("R1", TheFirstPublication, TheFirstGrade);

			Assert.That(journal.Api.ReadCurrentRevisionCode(), Is.EqualTo("R2"));
			Assert.That(journal.Api.ReadFluidRevision("EngineOil"), Is.EqualTo("R2"));

			journal.CloseAndReopenFromTheJournal();

			Assert.That(journal.Api.ReadCurrentRevisionCode(), Is.EqualTo("R2"));
			Assert.That(journal.Api.ReadFluidRevision("EngineOil"), Is.EqualTo("R2"));
		}

		private void ARevisionPublishingTheOil(string code, DateTime publishedAt, string grade)
		{
			journal.Api.OpenRevision(code, publishedAt);

			journal.Api.PublishFluidSpecification(
				code, "EngineOil", grade, 4.2m, "UnitA", 10000, "UnitA", 365);

			journal.Api.SealRevision(code);
		}
	}
}
