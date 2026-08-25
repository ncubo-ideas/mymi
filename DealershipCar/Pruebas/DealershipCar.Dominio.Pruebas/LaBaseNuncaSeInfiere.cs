using System;
using NUnit.Framework;

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
	// LO QUE T2 CONSTRUYE ES LA GUARDA, NO LA JERARQUIA. Los cinco subtipos y sus dos
	// preguntas son T3. Asi que Of() valida y despues avisa que la construccion todavia no
	// existe: la prueba de mas abajo fija ese orden -- la guarda corre ANTES -- porque es la
	// propiedad que tiene que seguir siendo cierta cuando T3 reemplace el aviso por los
	// cinco constructores.
	[TestFixture]
	public sealed class LaBaseNuncaSeInfiere
	{
		// La marca del aviso pendiente. Cuando T3 construya los subtipos, las dos pruebas
		// que la usan se ponen rojas -- y eso es correcto: son el punto de entrega.
		private const string MarcaDeLoQuePendeDeT3 = "T3";

		private const int UnLimiteDeTiempo = 365;
		private const int UnLimiteDeDistancia = 10000;
		private const int SinLimite = 0;

		// --- lo que cada base EXIGE -------------------------------------------------------

		[Test]
		public void TimeOrDistanceExigeLosDosLimites()
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
		public void TimeAndDistanceExigeLosDosLimites()
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
		public void TimeOnlyConUnLimiteDeDistanciaRevienta()
		{
			Assert.DoesNotThrow(() => IntervalRule.BasisIsNeverInferred(
				IntervalBasisCode.TimeOnly, UnLimiteDeTiempo, SinLimite));

			Assert.Throws<DomainException>(() => IntervalRule.BasisIsNeverInferred(
				IntervalBasisCode.TimeOnly, UnLimiteDeTiempo, UnLimiteDeDistancia),
				"TimeOnly con limite de distancia: o la base esta mal declarada o el limite "
				+ "sobra. Aceptarlo callado es inferir la base.");
		}

		[Test]
		public void TimeOnlySinLimiteDeTiempoRevienta()
		{
			Assert.Throws<DomainException>(() => IntervalRule.BasisIsNeverInferred(
				IntervalBasisCode.TimeOnly, SinLimite, SinLimite),
				"OINV-19: no hay operacion sin regla de cuando vence. TimeOnly sin limite de "
				+ "tiempo no vence nunca.");
		}

		[Test]
		public void DistanceOnlyConUnLimiteDeTiempoRevienta()
		{
			Assert.DoesNotThrow(() => IntervalRule.BasisIsNeverInferred(
				IntervalBasisCode.DistanceOnly, SinLimite, UnLimiteDeDistancia));

			Assert.Throws<DomainException>(() => IntervalRule.BasisIsNeverInferred(
				IntervalBasisCode.DistanceOnly, UnLimiteDeTiempo, UnLimiteDeDistancia));
		}

		[Test]
		public void DistanceOnlySinLimiteDeDistanciaRevienta()
		{
			Assert.Throws<DomainException>(() => IntervalRule.BasisIsNeverInferred(
				IntervalBasisCode.DistanceOnly, SinLimite, SinLimite));
		}

		[Test]
		public void ConditionOnlyConCualquierLimiteRevienta()
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
		public void UnLimiteNegativoRevienta()
		{
			Assert.Throws<DomainException>(() => IntervalRule.BasisIsNeverInferred(
				IntervalBasisCode.TimeOnly, -1, SinLimite));

			Assert.Throws<DomainException>(() => IntervalRule.BasisIsNeverInferred(
				IntervalBasisCode.DistanceOnly, SinLimite, -1));
		}

		// Misma polaridad que un resolutor de conjunto cerrado: afuera del conjunto no hay
		// miembro, asi que un codigo inventado es defecto de quien llama.
		[Test]
		public void UnaBaseQueNoNombraNingunaReglaRevienta()
		{
			Assert.Throws<DomainException>(() => IntervalRule.BasisIsNeverInferred(
				(IntervalBasisCode)99, UnLimiteDeTiempo, UnLimiteDeDistancia));
		}

		// --- la fabrica ------------------------------------------------------------------

		// LA PROPIEDAD QUE TIENE QUE SOBREVIVIR A T3: Of() valida la base ANTES de intentar
		// construir. Una combinacion rechazada revienta con el mensaje de la guarda, no con
		// el aviso de lo que falta -- asi que el orden queda fijado por la prueba y no por
		// el orden en que alguien escribio las lineas.
		[Test]
		public void OfCorreLaGuardaAntesDeIntentarConstruir()
		{
			DomainException error = Assert.Throws<DomainException>(() => IntervalRule.Of(
				IntervalBasisCode.TimeOnly, UnLimiteDeTiempo, UnLimiteDeDistancia,
				DistanceUnitCode.UnitA));

			Assert.That(error.Message, Does.Not.Contain(MarcaDeLoQuePendeDeT3),
				"Of() intento construir antes de validar la base. La guarda va primero: una "
				+ "combinacion invalida nunca debe llegar a un constructor.");
		}

		// La combinacion VALIDA pasa la guarda -- y hoy se topa con lo que T2 no construye.
		// Cuando T3 traiga los cinco subtipos, esta prueba se pone roja y se convierte en la
		// que afirma que Of() devuelve la regla de la base pedida.
		[Test]
		public void UnaCombinacionValidaPasaLaGuardaYQuedaEsperandoASusSubtipos()
		{
			DomainException error = Assert.Throws<DomainException>(() => IntervalRule.Of(
				IntervalBasisCode.TimeOrDistance, UnLimiteDeTiempo, UnLimiteDeDistancia,
				DistanceUnitCode.UnitA));

			Assert.That(error.Message, Does.Contain(MarcaDeLoQuePendeDeT3),
				"La combinacion es valida, asi que la guarda tiene que haberla dejado pasar. Si "
				+ "revento por otra razon, la guarda esta rechazando algo que OINV-20 permite.");
		}

		// El codigo de unidad tambien es un primitivo del wire, y se resuelve con la misma
		// polaridad total-con-excepcion (E32).
		[Test]
		public void OfRechazaUnCodigoDeUnidadQueNoNombraNingunMiembro()
		{
			DomainException error = Assert.Throws<DomainException>(() => IntervalRule.Of(
				IntervalBasisCode.TimeOrDistance, UnLimiteDeTiempo, UnLimiteDeDistancia,
				(DistanceUnitCode)99));

			Assert.That(error.Message, Does.Not.Contain(MarcaDeLoQuePendeDeT3),
				"Of() llego al aviso pendiente con una unidad que no existe: la resolucion de la "
				+ "unidad tambien tiene que pasar antes de construir.");
		}

		// E43: IntervalRule es una base POLIMORFICA y se queda ABIERTA. Sellarla es el
		// reflejo que despues obliga a des-sellarla para meter los cinco subtipos.
		[Test]
		public void IntervalRuleSigueSiendoUnaBaseAbierta()
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
