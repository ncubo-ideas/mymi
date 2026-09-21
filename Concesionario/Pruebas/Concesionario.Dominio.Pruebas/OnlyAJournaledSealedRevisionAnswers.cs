using System;
using NUnit.Framework;

namespace Concesionario.Dominio.Pruebas
{
	// LA PRUEBA 1 y la 2 de §11 EN EL NIVEL B (ley L12, OnlyASealedRevisionAnswers). La 1 es la que
	// mas importa de toda la rebanada: una hoja a medio cargar tiene que ser INCITABLE.
	//
	// QUE AGREGA EL NIVEL B, porque el nivel A ya prueba la ley sobre el agregado directo. Aca el
	// contenido del borrador esta DE VERDAD EN EL DISCO: los comandos de publicacion escribieron sus
	// entradas y el journal las va a volver a correr en cada arranque. Asi que la pregunta que solo
	// se puede hacer aca es si el sello sobrevive a la reproduccion en la direccion correcta -- que
	// reproducir un borrador lo deje borrador. Un replay que sellara de mas dejaria a la persona
	// citando una hoja que el fabricante no termino de cargar, y ninguna prueba del nivel A la veria.
	[TestFixture]
	public sealed class OnlyAJournaledSealedRevisionAnswers
	{
		private const string TheVin = "1HGCM82633A004352";
		private const string TheRevision = "R1";
		private const string TheGrade = "5W-30";

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

		// PRUEBA 1 DE §11, por la puerta. El borrador tiene contenido -- se publico un fluido, una
		// llanta y el alcance -- y TODA lectura sigue contestando que no hay nada publicado.
		[Test]
		public void ADraftWithContentPublishedIntoItStillAnswersNotPublished()
		{
			ADraftWithContent();

			Assert.That(journal.Api.IsFluidPublished("EngineOil"), Is.False);
			Assert.That(
				journal.Api.DescribeFluid("EngineOil", TheLanguage.OfTheRecord),
				Does.Contain("no se ha publicado nada"));
			Assert.That(
				journal.Api.DescribeFluid("EngineOil", TheLanguage.OfTheRecord),
				Does.Not.Contain(TheGrade),
				"Y la frase no deja escapar el grado que ya esta adentro del borrador. Una ausencia "
				+ "que igual dice el dato es la hoja citada por otro camino.");
			Assert.That(journal.Api.ReadFluidRevision("EngineOil"), Is.EqualTo(""));

			Assert.That(journal.Api.IsTyrePublished("Front"), Is.False);
			Assert.That(journal.Api.IsCoveragePublished(), Is.False);
			Assert.That(journal.Api.CoveredSystems(), Is.Empty);

			Assert.That(journal.Api.HasASealedRevision(), Is.False,
				"Y no hay revision vigente que nombrar: el borrador no es una.");
			Assert.That(journal.Api.ReadRevisionState(TheRevision), Is.EqualTo("Draft"),
				"La revision EXISTE y se puede preguntar donde esta parada. Lo que no contesta es lo "
				+ "que lleva adentro.");
		}

		// Y SIGUE SIENDO UN BORRADOR DESPUES DEL REPLAY. Es la unica pregunta de esta ley que el
		// nivel A no puede hacer: el estado del borrador se reconstruye corriendo otra vez los
		// comandos que lo llenaron, y lo que se afirma es que ninguno de ellos lo sella.
		[Test]
		public void TheDraftIsStillADraftAfterTheJournalIsReplayed()
		{
			ADraftWithContent();

			journal.CloseAndReopenFromTheJournal();

			Assert.That(journal.Api.ReadRevisionState(TheRevision), Is.EqualTo("Draft"));
			Assert.That(journal.Api.IsFluidPublished("EngineOil"), Is.False);
			Assert.That(journal.Api.HasASealedRevision(), Is.False);
		}

		// PRUEBA 2 DE §11: sellada, las MISMAS lecturas contestan y cada una nombra la revision. Y lo
		// hacen igual reproducidas, que es lo que separa una respuesta del journal de un campo de C#.
		[Test]
		public void OnceSealedTheSameReadsAnswerAndEachOneNamesTheRevision()
		{
			ADraftWithContent();
			journal.Api.SealRevision(TheRevision);

			string fluid = journal.Api.DescribeFluid("EngineOil", TheLanguage.OfTheRecord);

			Assert.That(journal.Api.IsFluidPublished("EngineOil"), Is.True);
			Assert.That(fluid, Does.Contain(TheGrade));
			Assert.That(fluid, Does.Contain("revisión " + TheRevision),
				"Ley L1: la procedencia va DENTRO de la frase, no solo en la lectura aparte que un "
				+ "lector puede saltarse.");
			Assert.That(journal.Api.ReadFluidRevision("EngineOil"), Is.EqualTo(TheRevision));
			Assert.That(journal.Api.ReadTyreRevision("Front"), Is.EqualTo(TheRevision));
			Assert.That(journal.Api.ReadCoverageRevision(), Is.EqualTo(TheRevision));

			Assert.That(journal.Api.HasASealedRevision(), Is.True);
			Assert.That(journal.Api.ReadCurrentRevisionCode(), Is.EqualTo(TheRevision));

			journal.CloseAndReopenFromTheJournal();

			Assert.That(
				journal.Api.DescribeFluid("EngineOil", TheLanguage.OfTheRecord), Is.EqualTo(fluid));
			Assert.That(journal.Api.ReadCurrentRevisionCode(), Is.EqualTo(TheRevision));
		}

		// UNA REVISION MAS NUEVA EN BORRADOR NO SE VUELVE LA VIGENTE. Es la ley L12 aplicada a la
		// lectura que nombra la revision, y es la que MAS filtra si se equivoca: la frase de un item
		// se puede leer entera, pero un codigo de revision suelto solo dice un nombre -- y ese nombre
		// seria el de una publicacion que el fabricante todavia esta armando.
		[Test]
		public void ARevisionStillInDraftDoesNotBecomeTheCurrentOneJustForBeingNewer()
		{
			ADraftWithContent();
			journal.Api.SealRevision(TheRevision);

			journal.Api.OpenRevision("R2", Published.AddMonths(6));
			journal.Api.PublishFluidSpecification(
				"R2", "EngineOil", "0W-20", 4.2m, "UnitA", 10000, "UnitA", 365);

			Assert.That(journal.Api.ReadCurrentRevisionCode(), Is.EqualTo(TheRevision),
				"La vigente es la sellada mas nueva, no la mas nueva. R2 se esta armando.");
			Assert.That(
				journal.Api.ReadCurrentRevisionPublishedAt(), Is.EqualTo(Published));
			Assert.That(
				journal.Api.DescribeFluid("EngineOil", TheLanguage.OfTheRecord),
				Does.Not.Contain("0W-20"));
		}

		// LAS LECTURAS DE LISTA SON LA FILTRACION MAS FACIL DE PASAR POR ALTO. Una frase que se
		// escapa se lee; un arreglo que llega con tres elementos donde tenia que llegar vacio no se
		// lee, se usa. Y en combustible el que importa es el de prohibidos: uno que aparezca desde un
		// borrador es un grado que nadie publico todavia diciendo que no se puede echar.
		[Test]
		public void ADraftsFuelGradesDoNotLeakThroughTheListReads()
		{
			journal.Api.OpenRevision(TheRevision, Published);
			journal.Api.PublishFuelGrade(TheRevision, "91 RON regular", "Minimum");
			journal.Api.PublishFuelGrade(TheRevision, "95 RON Premium", "Permitted");
			journal.Api.PublishFuelGrade(TheRevision, "E85 Flex", "Prohibited");

			Assert.That(journal.Api.IsFuelPublished(), Is.False);
			Assert.That(journal.Api.ReadMinimumFuelGrade(), Is.EqualTo(""));
			Assert.That(journal.Api.PermittedFuelGrades(), Is.Empty);
			Assert.That(journal.Api.ProhibitedFuelGrades(), Is.Empty);

			journal.Api.SealRevision(TheRevision);

			Assert.That(journal.Api.ProhibitedFuelGrades(), Is.EqualTo(new[] { "E85 Flex" }),
				"Y sellada si salen: la lista vacia de arriba era la ley, no un camino roto.");
		}

		// Sellar una revision vacia se rechaza, y el rechazo llega por la puerta como excepcion del
		// dominio: es un defecto de quien publica, no un estado. La entrada ya escrita es el precio
		// que esta guarda paga hoy, y ARefusedCommandCostsAJournalEntryUnlessItIsGuarded lo mide.
		[Test]
		public void SealingARevisionWithNothingPublishedIntoItIsRefused()
		{
			journal.Api.OpenRevision(TheRevision, Published);

			Assert.Throws<DomainException>(() => journal.Api.SealRevision(TheRevision));

			Assert.That(journal.Api.HasASealedRevision(), Is.False);
			Assert.That(journal.Api.ReadRevisionState(TheRevision), Is.EqualTo("Draft"));
		}

		private void ADraftWithContent()
		{
			journal.Api.OpenRevision(TheRevision, Published);

			journal.Api.PublishFluidSpecification(
				TheRevision, "EngineOil", TheGrade, 4.2m, "UnitA", 10000, "UnitA", 365);

			journal.Api.PublishTyreSpecification(
				TheRevision, "Front", "205/55R16", 2.4m, "UnitA", "94V");

			journal.Api.PublishCoveredSystem(TheRevision, "POWERTRAIN", 0, 0, "UnitA");
		}
	}
}
