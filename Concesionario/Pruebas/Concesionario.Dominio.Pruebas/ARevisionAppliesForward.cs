using System;
using NUnit.Framework;

namespace Concesionario.Dominio.Pruebas
{
	// LEY L11 del brief, ARevisionAppliesForward: "adoptar una revision rige desde su instante de
	// publicacion hacia adelante. Una respuesta ya dada conserva la revision que nombro. Una
	// revision nunca alcanza hacia atras."
	//
	// SON LOS TESTS 5 Y 6 DE §11 del brief, mas la lectura D4 del plan (§4.3), que es la que
	// decide que significa exactamente "hacia adelante" cuando hay varias revisiones:
	//
	//   un item contesta desde la ULTIMA revision que esta Sealed Y QUE LO PUBLICO.
	//
	// De ahi salen las dos consecuencias que este archivo prueba y que no son obvias:
	//
	//   -- una revision que corrige UN aceite NO borra el resto de la hoja: los items que no
	//      republico siguen contestando desde la revision anterior, que sigue sellada;
	//   -- SUSTITUIR SI RETIRA. Sustituir la revision 1 apaga todo lo que la revision 2 no
	//      republico, y eso es lo que hace que SupersedeRevision sea mas que cosmetico.
	//
	// EL TEST 6 ES UNA RESPUESTA CAPTURADA, y es lo que separa una respuesta de una vista viva.
	// Se toma la respuesta antes de que la revision 2 se selle y se guarda; despues se sella la
	// 2. La respuesta guardada tiene que seguir nombrando la revision 1 -- porque es una
	// PROYECCION de la revision de la que salio, no una ventana al agregado. Si siguiera a la
	// revision 2, lo que alguien recibio el mes pasado cambiaria retroactivamente, que es
	// exactamente lo que L11 prohibe.
	//
	// EL ORDEN ES POR INSTANTE DE PUBLICACION, con el orden de LLEGADA como desempate (§3.5 del
	// plan): el journal trunca el instante a segundos enteros (C13), asi que dos revisiones
	// publicadas dentro del mismo segundo solo se distinguen por como llegaron.
	[TestFixture]
	public sealed class ARevisionAppliesForward
	{
		private const string TheModel = "YARIS-2019";
		private const string TheFirstRevision = "REV-2025-01";
		private const string TheSecondRevision = "REV-2025-02";
		private const string TheFirstGrade = "5W-30";
		private const string TheSecondGrade = "0W-20";

		private static readonly DateTime PublishedAt = new DateTime(2025, 3, 14, 10, 0, 0);

		private ModelSpecification specification;

		[SetUp]
		public void Setup()
		{
			specification = new ModelSpecification(TheModel);
			specification.OpenRevision(TheFirstRevision, PublishedAt);
			TheEngineOil(TheFirstRevision, TheFirstGrade);
			specification.SealRevision(TheFirstRevision);
		}

		// TEST 5 DE §11.
		[Test]
		public void TheLatestSealedRevisionAnswersAndTheOneItReplacedIsSuperseded()
		{
			ASecondRevisionSealedWith(TheSecondGrade);
			specification.SupersedeRevision(TheFirstRevision, TheSecondRevision);

			Assert.That(TheSentenceTheDoorWouldCompose.TheFluid(
				specification, FluidKindCode.EngineOil, TheLanguage.OfTheRecord),
				Does.Contain(TheSecondGrade));
			Assert.That(TheSentenceTheDoorWouldCompose.TheFluid(
				specification, FluidKindCode.EngineOil, TheLanguage.OfTheRecord),
				Does.Not.Contain(TheFirstGrade),
				"El grado viejo no puede seguir apareciendo: la revision 2 lo corrigio.");
			Assert.That(specification.ReadFluidRevision(FluidKindCode.EngineOil),
				Is.EqualTo(TheSecondRevision),
				"Ley L1: la respuesta nombra la revision que la dio, y ahora es la segunda.");
			Assert.That(specification.ReadRevisionState(TheFirstRevision),
				Is.EqualTo("Superseded"));
		}

		// La revision 2 contesta desde que se sella, ANTES de que nadie sustituya la 1. Sustituir
		// es un hecho aparte y llega despues; lo que decide cual contesta es cual es la ultima
		// sellada que publico el item.
		[Test]
		public void TheLatestSealedRevisionAnswersEvenBeforeTheOlderOneIsSuperseded()
		{
			ASecondRevisionSealedWith(TheSecondGrade);

			Assert.That(specification.ReadFluidRevision(FluidKindCode.EngineOil),
				Is.EqualTo(TheSecondRevision));
			Assert.That(specification.ReadRevisionState(TheFirstRevision), Is.EqualTo("Sealed"),
				"Y la revision 1 sigue sellada: contestar no es un estado, es una consecuencia de "
				+ "haber sido la ultima que publico ese item.");
		}

		// TEST 6 DE §11: LA RESPUESTA CAPTURADA. Dos mitades, y la segunda es la que muerde.
		[Test]
		public void AnAnswerCapturedBeforeTheNextRevisionSealedStillNamesTheOneItCameFrom()
		{
			string held = TheSentenceTheDoorWouldCompose.TheFluid(
				specification, FluidKindCode.EngineOil, TheLanguage.OfTheRecord);

			ASecondRevisionSealedWith(TheSecondGrade);

			Assert.That(held, Does.Contain(TheFirstRevision),
				"Ley L11: una revision no alcanza hacia atras. Lo que alguien recibio sigue "
				+ "diciendo de donde salio.");
			Assert.That(held, Does.Contain(TheFirstGrade));
			Assert.That(TheSentenceTheDoorWouldCompose.TheFluid(
				specification, FluidKindCode.EngineOil, TheLanguage.OfTheRecord),
				Does.Contain(TheSecondRevision),
				"Y una lectura NUEVA si trae la revision nueva: hacia adelante si alcanza.");
		}

		// La otra mitad del test 6, y la unica falsificable: la respuesta es una PROYECCION, no
		// una ventana. Se captura el objeto respuesta -- no su texto -- y se le vuelve a pedir la
		// frase despues de que la revision 2 se sello. Si APublishedSpecification guardara el
		// agregado y volviera a resolver, aca seguiria a la revision 2 y esta prueba lo diria.
		[Test]
		public void TheAnswerObjectItselfIsAProjectionAndNotALiveView()
		{
			SpecificationAnswer captured =
				specification.ARevisionAppliesForward(FluidKindCode.EngineOil);

			ASecondRevisionSealedWith(TheSecondGrade);

			Assert.That(captured.RevisionCode(), Is.EqualTo(TheFirstRevision));
			Assert.That(
				TheSentenceTheDoorWouldCompose.TheSpecificationAnswer(
					captured, TheLanguage.OfTheRecord),
				Does.Contain(TheFirstGrade),
				"La respuesta se compuso con la revision que la dio y se quedo con ella. Una "
				+ "respuesta que se re-resuelve cada vez que se lee no es una respuesta, es una "
				+ "consulta.");
		}

		// D4 DEL PLAN, primera mitad: una revision que corrige UN item no borra el resto de la
		// hoja. Es la lectura que hace que un fabricante pueda publicar una correccion de una
		// linea sin volver a publicar la hoja entera.
		[Test]
		public void ARevisionThatCorrectsOneItemDoesNotBlankTheOnesItDidNotRepublish()
		{
			TheBrakeFluidWasPublishedInTheFirstRevisionToo();

			ASecondRevisionSealedWith(TheSecondGrade);

			Assert.That(specification.ReadFluidRevision(FluidKindCode.EngineOil),
				Is.EqualTo(TheSecondRevision));
			Assert.That(specification.ReadFluidRevision(FluidKindCode.BrakeFluid),
				Is.EqualTo(TheFirstRevision),
				"El liquido de frenos sigue contestando desde la revision 1, que sigue sellada. La "
				+ "revision 2 corrigio el aceite y no dijo nada de los frenos: no decir nada no es "
				+ "despublicar.");
		}

		// D4 DEL PLAN, segunda mitad, y es la que le da sentido a sustituir: SUSTITUIR RETIRA.
		// Una vez que la revision 1 esta sustituida, lo que solo ella publico deja de contestar,
		// porque una revision apagada no contesta (L12). Eso es lo que "superseded" quiere decir
		// de verdad, y es la diferencia entre sustituir y simplemente publicar despues.
		[Test]
		public void SupersedingWithdrawsWhatTheSuccessorDidNotRepublish()
		{
			TheBrakeFluidWasPublishedInTheFirstRevisionToo();
			ASecondRevisionSealedWith(TheSecondGrade);

			specification.SupersedeRevision(TheFirstRevision, TheSecondRevision);

			Assert.That(specification.IsFluidPublished(FluidKindCode.EngineOil), Is.True);
			Assert.That(specification.IsFluidPublished(FluidKindCode.BrakeFluid), Is.False,
				"Sustituir RETIRA lo que la sucesora no republico. Si esto siguiera contestando, "
				+ "sustituir seria cosmetico y la especificacion iria acumulando para siempre lo "
				+ "que ninguna revision vigente respalda.");
			Assert.That(specification.ReadFluidRevision(FluidKindCode.BrakeFluid), Is.EqualTo(""));
		}

		// Manda el INSTANTE DE PUBLICACION, no el orden de llegada: una revision que llega tarde
		// pero esta fechada antes no desplaza a la que rige. Ordenar es del agregado y no de la
		// revision (T1), y esta es la prueba de que ordena por lo que corresponde.
		[Test]
		public void ItIsTheLatestByPublicationInstantAndNotTheLastToArrive()
		{
			specification.OpenRevision("REV-2024-12", PublishedAt.AddYears(-1));
			TheEngineOil("REV-2024-12", "10W-40");
			specification.SealRevision("REV-2024-12");

			Assert.That(specification.ReadFluidRevision(FluidKindCode.EngineOil),
				Is.EqualTo(TheFirstRevision),
				"La revision vieja llego de ultima y sigue siendo vieja: rige desde SU instante de "
				+ "publicacion, y ese instante ya paso.");
		}

		// Y dentro del MISMO instante desempata el orden de llegada, porque el journal trunca a
		// segundos enteros (C13) y no queda nada mas con que desempatar.
		[Test]
		public void WithinTheSameInstantTheOneThatArrivedLastAnswers()
		{
			specification.OpenRevision(TheSecondRevision, PublishedAt);
			TheEngineOil(TheSecondRevision, TheSecondGrade);
			specification.SealRevision(TheSecondRevision);

			Assert.That(specification.ReadFluidRevision(FluidKindCode.EngineOil),
				Is.EqualTo(TheSecondRevision));
		}

		// La guarda de T1 -- la sucesora tiene que estar sellada ella misma -- sigue viva cuando
		// la fachada resuelve los dos codigos. El agregado resuelve y ENTREGA EL OBJETO; no
		// reescribe la guarda, porque un codigo no puede contestar si esta sellado.
		[Test]
		public void ADraftCannotSupersedeAnythingThroughTheFacadeEither()
		{
			specification.OpenRevision(TheSecondRevision, PublishedAt.AddDays(30));
			TheEngineOil(TheSecondRevision, TheSecondGrade);

			DomainException error = Assert.Throws<DomainException>(
				() => specification.SupersedeRevision(TheFirstRevision, TheSecondRevision));

			Assert.That(error.Message, Does.Contain("Draft"));
			Assert.That(specification.ReadFluidRevision(FluidKindCode.EngineOil),
				Is.EqualTo(TheFirstRevision),
				"Y la especificacion no se quedo sin nadie que conteste, que es justo lo que la "
				+ "guarda evita.");
		}

		private void ASecondRevisionSealedWith(string grade)
		{
			specification.OpenRevision(TheSecondRevision, PublishedAt.AddDays(30));
			TheEngineOil(TheSecondRevision, grade);
			specification.SealRevision(TheSecondRevision);
		}

		private void TheBrakeFluidWasPublishedInTheFirstRevisionToo()
		{
			specification = new ModelSpecification(TheModel);
			specification.OpenRevision(TheFirstRevision, PublishedAt);
			TheEngineOil(TheFirstRevision, TheFirstGrade);
			specification.PublishFluidSpecification(
				TheFirstRevision, FluidKindCode.BrakeFluid, "DOT 4",
				1.0m, VolumeUnitCode.UnitA, 30000, DistanceUnitCode.UnitA, 730);
			specification.SealRevision(TheFirstRevision);
		}

		private void TheEngineOil(string revisionCode, string grade)
		{
			specification.PublishFluidSpecification(
				revisionCode, FluidKindCode.EngineOil, grade,
				4.6m, VolumeUnitCode.UnitA, 15000, DistanceUnitCode.UnitA, 365);
		}
	}
}
