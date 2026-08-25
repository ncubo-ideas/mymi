using System;
using NUnit.Framework;

// El canon (§4 del slice) nombra a la base "TimeOnly", y el canon manda sobre los nombres.
// Da la casualidad de que System.TimeOnly existe desde .NET 6, y desde ESTE espacio de
// nombres gana el del using: la busqueda mira primero los miembros del namespace y despues
// sus usings, y DealershipCar.Dominio no es un namespace que encierre a este. Adentro del
// dominio no hay ambiguedad -- ahi el tipo propio esta en el namespace mismo -- pero aca hace
// falta decir cual. El alias es la forma de no renombrar el canon por una colision del BCL.
using TimeOnly = DealershipCar.Dominio.TimeOnly;

namespace DealershipCar.Dominio.Pruebas
{
	// OINV-20, ultima frase: "LA BASE NUNCA SE INFIERE."
	//
	// Es la mitad de OINV-20 que se olvida. La primera mitad -- either / both / single /
	// none -- es comportamiento y vive en los cinco subtipos de IntervalRule (T3). Esta
	// segunda mitad es una GUARDA, y vive en la fabrica: quien siembra una operacion DECLARA
	// la base, y los limites que manda tienen que concordar con la base que declaro.
	//
	// Por que importa tanto como la cuenta: si la fabrica INFIRIERA la base de los limites
	// -- "vinieron los dos, entonces sera TimeOrDistance" -- una operacion sembrada con un
	// limite de mas cambiaria silenciosamente de LEY. TimeAndDistance y TimeOrDistance
	// reciben exactamente los mismos numeros y deciden al revés; ningun dato los distingue.
	// La unica forma de que la base sea la que el plan del fabricante dice es que llegue
	// declarada y que la fabrica RECHACE lo que no concuerda.
	//
	// Polaridad (E32): esto es un defecto de QUIEN LLAMA, no un resultado del negocio. Por
	// eso REVIENTA y no devuelve un estado. Un aplazamiento rechazado en el tope es lo otro
	// -- un estado -- y eso llega en T4.
	//
	// T2 CONSTRUYO LA GUARDA; T3 TRAJO LA JERARQUIA. Mientras los cinco subtipos no existian,
	// Of() validaba y despues avisaba que no habia a quien construir, y dos pruebas de aca
	// abajo fijaban ese aviso. Al llegar T3 se pusieron rojas -- estaba previsto: eran el
	// punto de entrega -- y hoy afirman lo que quedo en su lugar: que la guarda sigue
	// corriendo ANTES de construir, y que Of() devuelve la regla de la base pedida.
	[TestFixture]
	public sealed class TheBasisIsNeverInferred
	{
		private const int UnLimiteDeTiempo = 365;
		private const int UnLimiteDeDistancia = 10000;
		private const int SinLimite = 0;

		// --- lo que cada base EXIGE -------------------------------------------------------

		[Test]
		public void TimeOrDistanceRequiresBothLimits()
		{
			Assert.DoesNotThrow(() => IntervalRule.BasisIsNeverInferred(
				IntervalBasisCode.TimeOrDistance, UnLimiteDeTiempo, UnLimiteDeDistancia));

			Assert.Throws<DomainException>(() => IntervalRule.BasisIsNeverInferred(
				IntervalBasisCode.TimeOrDistance, UnLimiteDeTiempo, SinLimite),
				"TimeOrDistance sin limite de distancia no tiene con que comparar la segunda "
				+ "dimension: la ley 'la primera que llegue' se queda sin una de las dos.");

			Assert.Throws<DomainException>(() => IntervalRule.BasisIsNeverInferred(
				IntervalBasisCode.TimeOrDistance, SinLimite, UnLimiteDeDistancia));
		}

		[Test]
		public void TimeAndDistanceRequiresBothLimits()
		{
			Assert.DoesNotThrow(() => IntervalRule.BasisIsNeverInferred(
				IntervalBasisCode.TimeAndDistance, UnLimiteDeTiempo, UnLimiteDeDistancia));

			Assert.Throws<DomainException>(() => IntervalRule.BasisIsNeverInferred(
				IntervalBasisCode.TimeAndDistance, UnLimiteDeTiempo, SinLimite));

			Assert.Throws<DomainException>(() => IntervalRule.BasisIsNeverInferred(
				IntervalBasisCode.TimeAndDistance, SinLimite, UnLimiteDeDistancia),
				"TimeAndDistance sin limite de tiempo nunca podria cumplir 'los dos limites'.");
		}

		[Test]
		public void TimeOnlyWithADistanceLimitThrows()
		{
			Assert.DoesNotThrow(() => IntervalRule.BasisIsNeverInferred(
				IntervalBasisCode.TimeOnly, UnLimiteDeTiempo, SinLimite));

			Assert.Throws<DomainException>(() => IntervalRule.BasisIsNeverInferred(
				IntervalBasisCode.TimeOnly, UnLimiteDeTiempo, UnLimiteDeDistancia),
				"TimeOnly con limite de distancia: o la base esta mal declarada o el limite "
				+ "sobra. Aceptarlo callado es inferir la base.");
		}

		[Test]
		public void TimeOnlyWithoutATimeLimitThrows()
		{
			Assert.Throws<DomainException>(() => IntervalRule.BasisIsNeverInferred(
				IntervalBasisCode.TimeOnly, SinLimite, SinLimite),
				"OINV-19: no hay operacion sin regla de cuando vence. TimeOnly sin limite de "
				+ "tiempo no vence nunca.");
		}

		[Test]
		public void DistanceOnlyWithATimeLimitThrows()
		{
			Assert.DoesNotThrow(() => IntervalRule.BasisIsNeverInferred(
				IntervalBasisCode.DistanceOnly, SinLimite, UnLimiteDeDistancia));

			Assert.Throws<DomainException>(() => IntervalRule.BasisIsNeverInferred(
				IntervalBasisCode.DistanceOnly, UnLimiteDeTiempo, UnLimiteDeDistancia));
		}

		[Test]
		public void DistanceOnlyWithoutADistanceLimitThrows()
		{
			Assert.Throws<DomainException>(() => IntervalRule.BasisIsNeverInferred(
				IntervalBasisCode.DistanceOnly, SinLimite, SinLimite));
		}

		[Test]
		public void ConditionOnlyWithAnyLimitThrows()
		{
			Assert.DoesNotThrow(() => IntervalRule.BasisIsNeverInferred(
				IntervalBasisCode.ConditionOnly, SinLimite, SinLimite));

			Assert.Throws<DomainException>(() => IntervalRule.BasisIsNeverInferred(
				IntervalBasisCode.ConditionOnly, UnLimiteDeTiempo, SinLimite),
				"ConditionOnly vence cuando llega un cruce de umbral, y §4 del slice dice que "
				+ "NO tiene limite de tiempo ni de distancia.");

			Assert.Throws<DomainException>(() => IntervalRule.BasisIsNeverInferred(
				IntervalBasisCode.ConditionOnly, SinLimite, UnLimiteDeDistancia));

			Assert.Throws<DomainException>(() => IntervalRule.BasisIsNeverInferred(
				IntervalBasisCode.ConditionOnly, UnLimiteDeTiempo, UnLimiteDeDistancia));
		}

		[Test]
		public void ANegativeLimitThrows()
		{
			Assert.Throws<DomainException>(() => IntervalRule.BasisIsNeverInferred(
				IntervalBasisCode.TimeOnly, -1, SinLimite));

			Assert.Throws<DomainException>(() => IntervalRule.BasisIsNeverInferred(
				IntervalBasisCode.DistanceOnly, SinLimite, -1));
		}

		// Misma polaridad que un resolutor de conjunto cerrado: afuera del conjunto no hay
		// miembro, asi que un codigo inventado es defecto de quien llama.
		[Test]
		public void ABasisThatNamesNoRuleThrows()
		{
			Assert.Throws<DomainException>(() => IntervalRule.BasisIsNeverInferred(
				(IntervalBasisCode)99, UnLimiteDeTiempo, UnLimiteDeDistancia));
		}

		// --- la fabrica ------------------------------------------------------------------

		// LA PROPIEDAD QUE SOBREVIVIO A T3: Of() valida la base ANTES de construir. Una
		// combinacion rechazada revienta con EL MENSAJE DE LA GUARDA, palabra por palabra --
		// no con uno propio de la fabrica ni con uno de un constructor. Comparar contra el
		// mensaje que la guarda produce por su cuenta es lo que fija el orden sin depender de
		// como este redactado.
		[Test]
		public void OfRunsTheGuardBeforeConstructing()
		{
			DomainException laGuardaSola = Assert.Throws<DomainException>(() =>
				IntervalRule.BasisIsNeverInferred(
					IntervalBasisCode.TimeOnly, UnLimiteDeTiempo, UnLimiteDeDistancia));

			DomainException laFabrica = Assert.Throws<DomainException>(() => IntervalRule.Of(
				IntervalBasisCode.TimeOnly, UnLimiteDeTiempo, UnLimiteDeDistancia,
				DistanceUnitCode.UnitA));

			Assert.That(laFabrica.Message, Is.EqualTo(laGuardaSola.Message),
				"Of() no revento con el mensaje de la guarda. La guarda va primero: una "
				+ "combinacion invalida nunca debe llegar a un constructor.");
		}

		// Lo que T2 dejo pendiente. La combinacion valida pasa la guarda y ahora sale de la
		// fabrica convertida en la regla de la base que se declaro -- una por base, cinco en
		// total, y ninguna inferida.
		//
		// Va como una sola prueba con cinco afirmaciones y no como cinco TestCase porque un
		// IntervalBasisCode no puede viajar por la firma de un metodo publico: el enum es
		// internal, como todo el dominio (E2), y NUnit descubre metodos publicos.
		[Test]
		public void OfReturnsTheRuleOfTheRequestedBasis()
		{
			Assert.That(
				IntervalRule.Of(IntervalBasisCode.TimeOrDistance, UnLimiteDeTiempo,
					UnLimiteDeDistancia, DistanceUnitCode.UnitA),
				Is.InstanceOf<TimeOrDistance>(),
				"La base ES el subtipo: si la fabrica se equivoca de tipo, la operacion cambia de "
				+ "ley en silencio.");

			Assert.That(
				IntervalRule.Of(IntervalBasisCode.TimeAndDistance, UnLimiteDeTiempo,
					UnLimiteDeDistancia, DistanceUnitCode.UnitA),
				Is.InstanceOf<TimeAndDistance>());

			Assert.That(
				IntervalRule.Of(IntervalBasisCode.TimeOnly, UnLimiteDeTiempo, SinLimite,
					DistanceUnitCode.UnitA),
				Is.InstanceOf<TimeOnly>());

			Assert.That(
				IntervalRule.Of(IntervalBasisCode.DistanceOnly, SinLimite, UnLimiteDeDistancia,
					DistanceUnitCode.UnitA),
				Is.InstanceOf<DistanceOnly>());

			Assert.That(
				IntervalRule.Of(IntervalBasisCode.ConditionOnly, SinLimite, SinLimite,
					DistanceUnitCode.UnitA),
				Is.InstanceOf<ConditionOnly>());
		}

		// Las mismas dos bases que reciben los mismos numeros salen tipos distintos. Es la
		// forma mecanica de OINV-20: no hay dato que las distinga, solo la declaracion.
		[Test]
		public void TheSameNumbersWithTwoDifferentBasesGiveTwoDifferentRules()
		{
			IntervalRule primera = IntervalRule.Of(IntervalBasisCode.TimeOrDistance,
				UnLimiteDeTiempo, UnLimiteDeDistancia, DistanceUnitCode.UnitA);

			IntervalRule segunda = IntervalRule.Of(IntervalBasisCode.TimeAndDistance,
				UnLimiteDeTiempo, UnLimiteDeDistancia, DistanceUnitCode.UnitA);

			Assert.That(primera.GetType(), Is.Not.EqualTo(segunda.GetType()));
		}

		// Los cinco subtipos son HOJAS y se sellan (E43). La base se queda abierta; lo que
		// nadie extiende, se cierra.
		[TestCase(typeof(TimeOrDistance))]
		[TestCase(typeof(TimeAndDistance))]
		[TestCase(typeof(TimeOnly))]
		[TestCase(typeof(DistanceOnly))]
		[TestCase(typeof(ConditionOnly))]
		public void EverySubtypeIsASealedLeaf(Type subtipo)
		{
			Assert.That(subtipo.IsSealed, Is.True,
				$"{subtipo.Name} no esta sellado. E43: se sellan las hojas y los conjuntos "
				+ "cerrados; solo la base polimorfica se queda abierta.");

			Assert.That(subtipo.BaseType, Is.EqualTo(typeof(IntervalRule)),
				$"{subtipo.Name} dejo de heredar de IntervalRule.");
		}

		// El codigo de unidad tambien es un primitivo del wire, y se resuelve con la misma
		// polaridad total-con-excepcion (E32) -- antes de construir nada.
		[Test]
		public void OfRejectsAUnitCodeThatNamesNoMember()
		{
			DomainException elResolutorSolo = Assert.Throws<DomainException>(
				() => DistanceUnit.OfCode((DistanceUnitCode)99));

			DomainException laFabrica = Assert.Throws<DomainException>(() => IntervalRule.Of(
				IntervalBasisCode.TimeOrDistance, UnLimiteDeTiempo, UnLimiteDeDistancia,
				(DistanceUnitCode)99));

			Assert.That(laFabrica.Message, Is.EqualTo(elResolutorSolo.Message),
				"Of() construyo una regla con una unidad que no existe: la resolucion de la "
				+ "unidad tambien tiene que pasar antes de construir.");
		}

		// E43: IntervalRule es una base POLIMORFICA y se queda ABIERTA. Sellarla es el
		// reflejo que despues obliga a des-sellarla para meter los cinco subtipos.
		[Test]
		public void IntervalRuleRemainsAnOpenBase()
		{
			Assert.That(typeof(IntervalRule).IsAbstract, Is.True,
				"IntervalRule dejo de ser abstracta. La base es el lugar de la pregunta, no de "
				+ "una respuesta.");

			Assert.That(typeof(IntervalRule).IsSealed, Is.False,
				"IntervalRule quedo sellada. E43: se sellan las hojas y los conjuntos cerrados; "
				+ "una base cuyo proposito es que la extiendan se queda abierta.");
		}
	}
}
