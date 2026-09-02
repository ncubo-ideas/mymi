using System;
using NUnit.Framework;

namespace DealershipCar.Dominio.Pruebas
{
	// LAS DOS PRIMERAS FILAS DE LA MAQUINA DE §10 del brief (E36):
	//
	//   --      | revision abierta   | Draft |
	//   Draft   | cualquier publish  | Draft | el contenido se acumula, invisible a las lecturas
	//
	// Una revision nace BORRADOR y no hay forma de que nazca en otro estado: no se recibe el
	// estado como dato, se recibe la apertura. Esa es la mitad de por que RevisionState se
	// elige adentro y no tiene enum de frontera.
	//
	// "El contenido se acumula" es lo que hace testeable la guarda de sello vacio de la ley
	// L12. En T1 el contenido era un CONTADOR y AcceptAPublication era la unica forma de
	// hacerlo subir, porque los items publicados todavia no existian. En T2 entro el primero de
	// verdad -- la especificacion de fluido -- asi que el contador desaparecio adentro del
	// camino de publicacion real y HasContent() lee los items. Este archivo se reescribio en
	// esa entrega, no se borro: la maquina que afirma es la misma. Los otros cinco items llegan
	// en T3 por el mismo camino.
	//
	// Y EL INSTANTE SE RECIBE (E44, §8 del brief): la revision llega con su instante de
	// publicacion, no lo lee de ningun reloj. El dominio no decide cuando una revision empieza
	// a regir.
	[TestFixture]
	public sealed class ARevisionOpensAsADraft
	{
		private const string TheRevision = "REV-2025-01";

		private static readonly DateTime PublishedAt = new DateTime(2025, 3, 14, 10, 0, 0);

		private PublishedRevision revision;

		[SetUp]
		public void Setup()
		{
			revision = new PublishedRevision(TheRevision, PublishedAt);
		}

		[Test]
		public void ARevisionWithoutACodeCannotBeNamedAfterwards()
		{
			Assert.Throws<DomainException>(() => new PublishedRevision(null, PublishedAt));
			Assert.Throws<DomainException>(() => new PublishedRevision("", PublishedAt));
			Assert.Throws<DomainException>(() => new PublishedRevision("   ", PublishedAt),
				"Ley L1: toda respuesta nombra la revision de la que salio. Una revision sin codigo "
				+ "no se puede nombrar, asi que sus respuestas serian rumores.");
		}

		[Test]
		public void ItIsBornADraft()
		{
			Assert.That(revision.ReadState(), Is.EqualTo("Draft"));
			Assert.That(revision.IsSealed(), Is.False,
				"Ley L12: solo una revision sellada contesta. Si naciera sellada, una hoja de "
				+ "especificaciones a medio cargar seria citable desde el primer publish.");
			Assert.That(revision.HasBeenSuperseded(), Is.False);
		}

		[Test]
		public void ItIsBornEmpty()
		{
			Assert.That(revision.HasContent(), Is.False,
				"Una revision recien abierta no tiene nada publicado adentro, y eso es lo que la "
				+ "guarda de sello vacio de L12 tiene que poder ver.");
		}

		[Test]
		public void ItKeepsTheCodeAndTheInstantItWasOpenedWith()
		{
			Assert.That(revision.Code, Is.EqualTo(TheRevision));
			Assert.That(revision.PublishedAt, Is.EqualTo(PublishedAt),
				"E44: el instante se RECIBE. El dominio no lee ningun reloj y no decide cuando una "
				+ "revision empieza a regir.");
		}

		// La fila del medio de §10: publicar en un borrador lo deja borrador. No es un no-op --
		// el contenido entro -- pero tampoco lo sella. Lo que cambia es que ahora hay algo.
		[Test]
		public void PublishingIntoADraftLeavesItADraftAndGivesItContent()
		{
			TheEngineOilEntersInto(revision);

			Assert.That(revision.ReadState(), Is.EqualTo("Draft"),
				"§10: cualquier publish sobre un Draft devuelve Draft. Sellar es un comando aparte, "
				+ "no la consecuencia de haber publicado algo.");
			Assert.That(revision.HasContent(), Is.True);
		}

		// Acumular quiere decir que la revision se queda con TODO lo que entro, no solo con lo
		// ultimo. Con clases distintas se ve; con la misma clase tres veces no se veria, porque
		// ahi manda la ultima -- que es una correccion antes de sellar y no una acumulacion.
		[Test]
		public void PublicationsAccumulate()
		{
			TheEngineOilEntersInto(revision);
			TheBrakeFluidEntersInto(revision);

			Assert.That(revision.ReadState(), Is.EqualTo("Draft"));
			Assert.That(revision.HasContent(), Is.True);
			Assert.That(revision.HasAFluidSpecification(FluidKindCode.EngineOil), Is.True);
			Assert.That(revision.HasAFluidSpecification(FluidKindCode.BrakeFluid), Is.True,
				"El contenido se ACUMULA en la revision: publicar lo segundo no reemplaza lo "
				+ "primero.");
		}

		// Y lo que no entro no esta. La pregunta por clase es TOTAL: una clase que esta revision
		// no publico contesta que no, sin reventar, porque una revision publica lo que publica y
		// no tiene por que traer las seis.
		[Test]
		public void AKindTheRevisionDidNotPublishSimplySaysSo()
		{
			TheEngineOilEntersInto(revision);

			Assert.That(revision.HasAFluidSpecification(FluidKindCode.Coolant), Is.False);
		}

		// La revision se abre con el instante que le dan, incluso uno anterior a otro que ya
		// haya llegado. Ordenar revisiones es de la ley L11 y vive en el agregado, no aca --
		// entre otras cosas porque el journal trunca el instante a segundos enteros (C13) y dos
		// revisiones publicadas dentro del mismo segundo se distinguen por orden de llegada,
		// que es algo que una revision suelta no puede saber.
		[Test]
		public void ItDoesNotJudgeWhereItsInstantFallsAgainstAnyOtherRevision()
		{
			PublishedRevision anterior =
				new PublishedRevision("REV-2024-12", PublishedAt.AddYears(-1));

			Assert.That(anterior.PublishedAt, Is.LessThan(revision.PublishedAt));
			Assert.That(anterior.ReadState(), Is.EqualTo("Draft"),
				"Una revision con un instante anterior sigue naciendo Draft. La revision no ordena "
				+ "revisiones: eso lo hace el agregado, donde vive L11.");
		}

		private static void TheEngineOilEntersInto(PublishedRevision draft)
		{
			draft.PublishAFluidSpecification(
				FluidKindCode.EngineOil,
				new FluidSpecification(
					FluidKind.EngineOil, "5W-30", 4.6m, VolumeUnit.UnitA,
					new PublishedInterval(15000, DistanceUnit.UnitA, 365)));
		}

		private static void TheBrakeFluidEntersInto(PublishedRevision draft)
		{
			draft.PublishAFluidSpecification(
				FluidKindCode.BrakeFluid,
				new FluidSpecification(
					FluidKind.BrakeFluid, "DOT 4", 1.0m, VolumeUnit.UnitA,
					new PublishedInterval(0, DistanceUnit.UnitA, 730)));
		}
	}
}
