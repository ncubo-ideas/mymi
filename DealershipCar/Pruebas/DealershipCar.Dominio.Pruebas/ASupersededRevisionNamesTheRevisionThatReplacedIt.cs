using System;
using NUnit.Framework;

namespace DealershipCar.Dominio.Pruebas
{
	// LA ULTIMA FILA DE LA MAQUINA DE §10 del brief, y la ley L11 en su forma de comando:
	//
	//   Sealed | revision sustituida | Superseded (terminal) | nombra la revision que la sustituye
	//
	// SUSTITUIR NOMBRA. Una revision que se apaga sin decir quien la reemplazo deja a la
	// respuesta que alguien recibio de ella sin sucesora que consultar, y es lo que hace que
	// SupersedeRevision sea mas que cosmetico: la sucesora es la que contesta de ahora en
	// adelante, y quien tenga anotada la anterior puede llegar a ella.
	//
	// Y LA SUCESORA TIENE QUE ESTAR SELLADA ELLA MISMA. Si un borrador pudiera sustituir a una
	// revision sellada, el resultado seria una especificacion sin ninguna revision que
	// contesta: la vieja apagada, la nueva invisible (L12). Todas las respuestas del modulo se
	// caerian a "no publicado" en el instante de la sustitucion, sin que nadie hubiera
	// despublicado nada.
	//
	// DOS TRANSICIONES ILEGALES DE §10 SE PRUEBAN ACA: sustituir un Draft, y sustituir dos
	// veces (terminal). Las dos revientan (E32): esto llega del feed del fabricante, y un feed
	// que sustituye lo que no estaba sellado esta roto, no esta expresando un caso de negocio.
	//
	// EL PAR DE E41 ESTA EN LA PAREJA HasBeenSuperseded / SupersededByCode: la pregunta de
	// presencia es TOTAL y el accesor esta GUARDADO. La ausencia NO es un tipo aparte aca --
	// cabalga sobre el eje de estado que la maquina ya tiene, y E45 manda levantarla ahi en vez
	// de poner una ranura de ausencia en paralelo que pueda contradecir al estado.
	[TestFixture]
	public sealed class ASupersededRevisionNamesTheRevisionThatReplacedIt
	{
		private const string TheRevision = "REV-2025-01";
		private const string TheNextRevision = "REV-2025-02";

		private static readonly DateTime PublishedAt = new DateTime(2025, 3, 14, 10, 0, 0);

		private PublishedRevision revision;

		[SetUp]
		public void Setup()
		{
			revision = new PublishedRevision(TheRevision, PublishedAt);
			TheEngineOilEntersInto(revision);
			revision.Seal();
		}

		[Test]
		public void ASealedRevisionIsSupersededAndNamesItsSuccessor()
		{
			revision.SupersededBy(ASealedRevision(TheNextRevision));

			Assert.That(revision.ReadState(), Is.EqualTo("Superseded"));
			Assert.That(revision.HasBeenSuperseded(), Is.True);
			Assert.That(revision.SupersededByCode(), Is.EqualTo(TheNextRevision),
				"Ley L11: sustituir NOMBRA. Quien tenga anotada esta revision como procedencia "
				+ "tiene que poder llegar a la que la reemplazo.");
		}

		// La pregunta de presencia es total y el accesor esta guardado (E41): leer el codigo del
		// sucesor de una revision que no fue sustituida es un defecto de quien llama, no una
		// cadena vacia. Devolver "" ahi confundiria "no la sustituyeron" con "la sustituyo una
		// revision sin nombre".
		[Test]
		public void TheSuccessorsCodeCannotBeReadBeforeThereIsOne()
		{
			Assert.That(revision.HasBeenSuperseded(), Is.False);

			Assert.Throws<DomainException>(() => revision.SupersededByCode());
		}

		[Test]
		public void ADraftCannotBeSuperseded()
		{
			PublishedRevision borrador = new PublishedRevision("REV-BORRADOR", PublishedAt);

			DomainException error = Assert.Throws<DomainException>(
				() => borrador.SupersededBy(ASealedRevision(TheNextRevision)));

			Assert.That(error.Message, Does.Contain("Draft"));
			Assert.That(borrador.ReadState(), Is.EqualTo("Draft"),
				"El rechazo es inerte: el borrador sigue siendo un borrador, no quedo a medio "
				+ "sustituir.");
		}

		[Test]
		public void ADraftCannotSupersedeAnything()
		{
			PublishedRevision sucesoraSinSellar =
				new PublishedRevision(TheNextRevision, PublishedAt.AddDays(30));
			TheEngineOilEntersInto(sucesoraSinSellar);

			DomainException error = Assert.Throws<DomainException>(
				() => revision.SupersededBy(sucesoraSinSellar));

			Assert.That(error.Message, Does.Contain("Draft"));
			Assert.That(revision.ReadState(), Is.EqualTo("Sealed"),
				"Si esto pasara, la especificacion se quedaria sin ninguna revision que contesta: "
				+ "la vieja apagada y la nueva invisible por L12.");
		}

		[Test]
		public void SupersedingTwiceIsRefusedBecauseSupersededIsTerminal()
		{
			revision.SupersededBy(ASealedRevision(TheNextRevision));

			DomainException error = Assert.Throws<DomainException>(
				() => revision.SupersededBy(ASealedRevision("REV-2025-03")));

			Assert.That(error.Message, Does.Contain("Superseded"));
			Assert.That(revision.SupersededByCode(), Is.EqualTo(TheNextRevision),
				"Y el sucesor no se reescribio: la primera sustitucion es la que quedo, asi que la "
				+ "cadena hacia atras sigue diciendo lo mismo que decia.");
		}

		// El sucesor es un colaborador REQUERIDO, no opcional: "sustituida por nada" es apagar
		// la revision sin dejar donde seguir. Un null aca es un defecto de quien llama, guardado
		// igual que un rango numerico (E2, regla 1 de E45).
		[Test]
		public void SupersedingWithoutNamingASuccessorIsACallerBug()
		{
			Assert.Throws<DomainException>(() => revision.SupersededBy(null));

			Assert.That(revision.ReadState(), Is.EqualTo("Sealed"));
		}

		// Una revision no es su propia sucesora. §10 no nombra este caso, y se guarda igual:
		// pasaria las dos guardas anteriores -- esta sellada, y la sucesora tambien, porque es
		// ella misma -- y dejaria una revision apagada que se nombra a si misma como el sitio
		// donde seguir buscando.
		[Test]
		public void ARevisionIsNotItsOwnSuccessor()
		{
			DomainException error = Assert.Throws<DomainException>(
				() => revision.SupersededBy(revision));

			Assert.That(error.Message, Does.Contain(TheRevision));
			Assert.That(revision.ReadState(), Is.EqualTo("Sealed"));
			Assert.That(revision.HasBeenSuperseded(), Is.False);
		}

		// La sucesora no cambia por haber sustituido a nadie: sigue sellada y sigue siendo la
		// que contesta. Sustituir es un hecho de la revision vieja.
		[Test]
		public void TheSuccessorIsUnchangedByHavingSupersededSomething()
		{
			PublishedRevision sucesora = ASealedRevision(TheNextRevision);

			revision.SupersededBy(sucesora);

			Assert.That(sucesora.ReadState(), Is.EqualTo("Sealed"));
			Assert.That(sucesora.HasBeenSuperseded(), Is.False);
			Assert.That(sucesora.IsSealed(), Is.True);
		}

		private static PublishedRevision ASealedRevision(string code)
		{
			PublishedRevision sellada = new PublishedRevision(code, PublishedAt.AddDays(30));
			TheEngineOilEntersInto(sellada);
			sellada.Seal();

			return sellada;
		}

		private static void TheEngineOilEntersInto(PublishedRevision draft)
		{
			draft.PublishAFluidSpecification(
				FluidKindCode.EngineOil,
				new FluidSpecification(
					FluidKind.EngineOil, "5W-30", 4.6m, VolumeUnit.UnitA,
					new PublishedInterval(15000, DistanceUnit.UnitA, 365)));
		}
	}
}
