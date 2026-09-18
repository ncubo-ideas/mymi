using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace Concesionario.Dominio.Pruebas
{
	// E47: "la ausencia con CLASES es un tipo con nombre por motivo, y el motivo va en el NOMBRE
	// DEL TIPO y no en un campo. Un solo NoAnswer con reason = 'falta la lectura' solo lo
	// encuentra quien ya adivino como estaba redactado."
	//
	// EL SLICE TIENE TRES MOTIVOS, y son tres formas distintas de que el registro no pueda decir
	// donde esta parado el vehiculo:
	//
	//   AnEnvelopeSuspendedByAnAbsentDelivery  -- no hay ancla desde donde medir ninguna mitad
	//   AnEnvelopeSuspendedByAnAbsentReading   -- no hay odometro al instante preguntado
	//   AnEnvelopeSuspendedByAUnitMismatch     -- el limite y el odometro estan en unidades distintas
	//
	// LAS TRES PIDEN TRABAJO DISTINTO a quien las lea, y por eso son tres tipos: la primera la
	// arregla la agencia registrando la entrega, la segunda la arregla el feed, y la tercera no la
	// arregla nadie convirtiendo -- se deja de comparar (ley L4).
	//
	// NINGUN LLAMADOR RAMIFICA POR EL TIPO CONCRETO. Los tres predicados viven en la BASE y son
	// TOTALES, asi que no hay 'is', ni cast, ni switch: se pregunta y se contesta. La prueba de
	// abajo que ata cada tipo con su predicado por el NOMBRE es la que hace ejecutable la parte de
	// E47 que suele quedar como costumbre -- que el motivo se pueda grepear.
	[TestFixture]
	public sealed class EachSuspensionNamesItsOwnReason
	{
		private const string ElPrefijoDeLasSuspensiones = "AnEnvelopeSuspendedBy";

		// Cada suspension declara EXACTAMENTE UN motivo. Dos a la vez es un motivo inventado, y
		// cero es una suspension que no se puede explicar.
		[Test]
		public void ASuspendedAnswerNamesExactlyOneReason()
		{
			foreach (EnvelopeAnswer suspended in LasTresSuspensiones())
			{
				Assert.That(suspended.CanBeEvaluated(), Is.False, suspended.GetType().Name);
				Assert.That(CuantosMotivosDeclara(suspended), Is.EqualTo(1),
					suspended.GetType().Name + " declara " + CuantosMotivosDeclara(suspended)
					+ " motivos y tiene que declarar uno.");
			}
		}

		// Y la que SI se decidio no esta suspendida por ninguno. "No suspendida" es una respuesta
		// total, no la ausencia de una.
		[Test]
		public void ASettledAnswerIsSuspendedByNothing()
		{
			EnvelopeAnswer settled = new ASettledEnvelopeAnswer(
				EnvelopeStanding.Inside, AClauseUnderTest.WithNoEnvelope());

			Assert.That(CuantosMotivosDeclara(settled), Is.EqualTo(0));
		}

		// EL MOTIVO ESTA EN EL NOMBRE DEL TIPO, y esto lo vuelve ejecutable en vez de dejarlo como
		// convencion: para cada AnEnvelopeSuspendedByX la base declara un SuspendedByX, y es
		// verdadero en esa variante y falso en todas las demas. El dia que alguien agregue una
		// suspension sin su predicado -- o un predicado sin su suspension -- esto se pone rojo.
		[Test]
		public void EveryReasonInATypeNameHasItsOwnTotalPredicateOnTheBase()
		{
			List<EnvelopeAnswer> todas = LasCuatroVariantes();

			foreach (EnvelopeAnswer suspended in LasTresSuspensiones())
			{
				string tipo = suspended.GetType().Name;

				Assert.That(tipo, Does.StartWith(ElPrefijoDeLasSuspensiones),
					"Una suspension que no nombre su motivo en su propio tipo no se puede "
					+ "grepear, que es la mitad de para que E47 pide un tipo por motivo.");

				string predicado = "SuspendedBy" + tipo.Substring(ElPrefijoDeLasSuspensiones.Length);
				MethodInfo declarado = typeof(EnvelopeAnswer).GetMethod(predicado,
					BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

				Assert.That(declarado, Is.Not.Null,
					"EnvelopeAnswer no declara " + predicado + ", asi que el unico modo de saber "
					+ "que la respuesta es " + tipo + " es preguntarle el tipo concreto -- y eso "
					+ "es exactamente lo que la base existe para evitar.");

				foreach (EnvelopeAnswer variante in todas)
					Assert.That(declarado.Invoke(variante, null),
						Is.EqualTo(variante.GetType() == suspended.GetType()),
						predicado + " sobre " + variante.GetType().Name);
			}
		}

		// TRES MOTIVOS, TRES TIPOS, y las cuentas cuadran por los dos lados (C57). Un predicado de
		// mas sin variante es un motivo que nadie produce; una variante de mas sin predicado es un
		// motivo que nadie puede preguntar sin ramificar por el tipo.
		[Test]
		public void ThereAreAsManyReasonPredicatesAsThereAreSuspensions()
		{
			List<string> predicados = LosPredicadosDeMotivo()
				.Select(metodo => metodo.Name)
				.OrderBy(nombre => nombre, StringComparer.Ordinal)
				.ToList();

			List<string> esperados = LasTresSuspensiones()
				.Select(suspension => "SuspendedBy"
					+ suspension.GetType().Name.Substring(ElPrefijoDeLasSuspensiones.Length))
				.OrderBy(nombre => nombre, StringComparer.Ordinal)
				.ToList();

			Assert.That(predicados, Is.EqualTo(esperados));
		}

		// LAS TRES FRASES SON DISTINTAS. Tres tipos que digan lo mismo son un solo tipo con tres
		// nombres: quien lea la respuesta no sabria cual de los tres arreglos le toca.
		[Test]
		public void TheThreeReasonsAreSaidDifferently()
		{
			List<string> frases = LasTresSuspensiones()
				.Select(suspension => TheSentenceTheDoorWouldCompose.TheWarrantyEnvelope(
					suspension, TheLanguage.OfTheRecord))
				.ToList();

			Assert.That(frases.Distinct().Count(), Is.EqualTo(frases.Count),
				"Dos suspensiones se dicen igual: " + string.Join(" | ", frases));
		}

		[Test]
		public void TheAbsentDeliverySaysThereIsNothingToMeasureFrom()
		{
			Assert.That(
				TheSentenceTheDoorWouldCompose.TheWarrantyEnvelope(
					new AnEnvelopeSuspendedByAnAbsentDelivery(AClauseUnderTest.WithNoEnvelope()), TheLanguage.OfTheRecord),
				Does.Contain("ninguna entrega"));
		}

		// LEY L5: una lectura ausente se DICE. Nunca cero, nunca la de la entrega haciendo de la
		// de hoy. La frase tiene que decir que falta, no dar un numero.
		[Test]
		public void TheAbsentReadingSaysTheReadingIsMissingAndNotThatItIsZero()
		{
			string said = TheSentenceTheDoorWouldCompose.TheWarrantyEnvelope(
				new AnEnvelopeSuspendedByAnAbsentReading(AClauseUnderTest.WithNoEnvelope()), TheLanguage.OfTheRecord);

			Assert.That(said, Does.Contain("ninguna lectura del odómetro"));
			Assert.That(said, Does.Not.Contain(" 0 "),
				"Ley L5: un cero en la frase de una lectura ausente se lee como un odometro en "
				+ "cero, y desde ahi todo lo recorrido cabe adentro de cualquier sobre.");
		}

		// LEY L4: no se convierte, y se dice CUAL contra CUAL. Decir que no se pudo medir sin
		// nombrar las dos unidades no le deja a nadie corregirlo -- es el mismo guard que
		// ACoverageSuspendedByAUnitMismatch lleva del otro lado.
		[Test]
		public void TheUnitMismatchNamesBothUnitsAndSaysTheyAreNeverConverted()
		{
			string said = TheSentenceTheDoorWouldCompose.TheWarrantyEnvelope(
				new AnEnvelopeSuspendedByAUnitMismatch(
					DistanceUnit.UnitA, DistanceUnit.UnitB,
					AClauseUnderTest.WithAnEnvelopeIn(DistanceUnit.UnitA)), TheLanguage.OfTheRecord);

			Assert.That(said, Does.Contain(DistanceUnit.UnitA.Name));
			Assert.That(said, Does.Contain(DistanceUnit.UnitB.Name));
			Assert.That(said, Does.Contain("nunca se convierten"));
		}

		[Test]
		public void AUnitMismatchThatDoesNotNameBothUnitsIsRefused()
		{
			Assert.Throws<DomainException>(() => new AnEnvelopeSuspendedByAUnitMismatch(
				null, DistanceUnit.UnitB, AClauseUnderTest.WithAnEnvelopeIn(DistanceUnit.UnitA)));
			Assert.Throws<DomainException>(() => new AnEnvelopeSuspendedByAUnitMismatch(
				DistanceUnit.UnitA, null, AClauseUnderTest.WithAnEnvelopeIn(DistanceUnit.UnitA)));
		}

		// Una respuesta decidida sin resultado no es una respuesta decidida.
		[Test]
		public void ASettledAnswerWithoutAStandingIsRefused()
		{
			Assert.Throws<DomainException>(() => new ASettledEnvelopeAnswer(null, AClauseUnderTest.WithNoEnvelope()));
		}

		private static int CuantosMotivosDeclara(EnvelopeAnswer answer)
		{
			return LosPredicadosDeMotivo()
				.Count(metodo => (bool)metodo.Invoke(answer, null));
		}

		private static List<MethodInfo> LosPredicadosDeMotivo()
		{
			return typeof(EnvelopeAnswer)
				.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
					| BindingFlags.DeclaredOnly)
				.Where(metodo => metodo.Name.StartsWith("SuspendedBy", StringComparison.Ordinal))
				.Where(metodo => metodo.ReturnType == typeof(bool)
					&& metodo.GetParameters().Length == 0)
				.ToList();
		}

		private static List<EnvelopeAnswer> LasCuatroVariantes()
		{
			List<EnvelopeAnswer> todas = new List<EnvelopeAnswer>
			{
				new ASettledEnvelopeAnswer(
					EnvelopeStanding.Inside, AClauseUnderTest.WithNoEnvelope()),
			};

			todas.AddRange(LasTresSuspensiones());

			return todas;
		}

		private static List<EnvelopeAnswer> LasTresSuspensiones()
		{
			return new List<EnvelopeAnswer>
			{
				new AnEnvelopeSuspendedByAnAbsentDelivery(AClauseUnderTest.WithNoEnvelope()),
				new AnEnvelopeSuspendedByAnAbsentReading(AClauseUnderTest.WithNoEnvelope()),
				new AnEnvelopeSuspendedByAUnitMismatch(
					DistanceUnit.UnitA, DistanceUnit.UnitB, AClauseUnderTest.WithAnEnvelopeIn(DistanceUnit.UnitA)),
			};
		}
	}
}
