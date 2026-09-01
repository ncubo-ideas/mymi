using System;
using System.Collections.Generic;
using System.Reflection;
using DealershipCar.App;
using NUnit.Framework;

namespace DealershipCar.Dominio.Pruebas
{
	// LA REGLA QUE T5 MIDIO QUE NO TENIA NINGUNA PRUEBA DETRAS. Los dos limites del sobre, y las dos
	// mitades de lo que queda, llegan a un llamador SOLO dentro de la frase que el dominio compone.
	// Quien arme "36 meses o 60000" con escalares se llevo la composicion fuera del dominio, y desde
	// ahi cada capa que la reformule puede perder una mitad, la unidad o la procedencia.
	//
	// MEDIDO EN T5: agregarle a la puerta un int ReadEnvelopeMonths() ponia rojo EXACTAMENTE UNA cosa
	// -- el censo de operaciones corridas -- y se ponia roja porque el miembro estaba SIN CORRER, no
	// porque entregara un escalar. Con el nombre en la lista, la suite quedaba verde. La regla la
	// sostenia la revision y nada mas, y T6 es donde eso se pone peor: las lecturas compuestas son
	// justo donde una caratula querria los numeros sueltos.
	//
	// EL INSTRUMENTO SE ARMA POR ELIMINACION Y TIENE DOS MITADES, y hace falta que sean dos.
	//
	//   POR CENSO: toda pregunta de la puerta que conteste con un NUMERO, como conjunto exacto y con
	//   su aridad. Una lectura escalar nueva se pone roja con CUALQUIER nombre, que es lo que un
	//   vocabulario de nombres no daria -- quien agregue la de comodidad elige como llamarla.
	//
	//   POR VALOR: con un sobre publicado, NINGUNA de esas preguntas contesta ninguno de sus dos
	//   limites. El censo solo prueba que nadie agrego un miembro sin escribirlo; esto prueba que
	//   ninguno de los que hay entrega media respuesta.
	//
	// POR QUE LOS SIETE QUE HAY SI PUEDEN. Ninguno es la mitad de una respuesta compuesta: cinco son
	// CONTEOS que la agenda y la bitacora publican por su cuenta -- vencidas, atrasadas, aplazos
	// usados, mantenimientos perdidos, ultimo evento aplicado -- y dos son HECHOS RECIBIDOS que el
	// registro guarda tal como llegaron: el odometro del dia de la entrega y el de hoy. Un hecho
	// recibido se puede dar suelto porque nadie lo derivo de otro; una mitad derivada no, porque su
	// unidad, su limite y el instante contra el que se midio viven en la frase.
	[TestFixture]
	public sealed class AHalfOfAComposedAnswerNeverLeavesTheDoorAsANumber
	{
		private const string TheVin = "2HGFC2F59KH123456";
		private const string TheUnitEverythingIsIn = "UnitB";

		// NUMEROS QUE NO SE PUEDEN CONFUNDIR CON NINGUN OTRO DE ESTE FIXTURE (C60). Un plazo de 36 o
		// un limite de 60000 se podrian cruzar con un conteo o con un odometro por casualidad; 37 y
		// 61111 no se cruzan con nada de lo que hay abajo.
		private const int TheMonthsNothingElseCouldAnswer = 37;
		private const int TheDistanceNothingElseCouldAnswer = 61111;

		private const int TheOdometerAtDelivery = 30000;
		private const int TheOdometerToday = 35000;

		private static readonly DateTime ThePublication =
			new DateTime(2023, 11, 15, 0, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime TheDayItWasHandedOver =
			new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime TheDayItWasObserved =
			new DateTime(2026, 1, 20, 0, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime TheDayItIsAsked =
			new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);

		private AScheduleOnARealJournal journal;

		[SetUp]
		public void Setup()
		{
			journal = new AScheduleOnARealJournal(TheVin, "Standard");

			journal.Api.PublishWarrantyEnvelope(
				TheMonthsNothingElseCouldAnswer, TheDistanceNothingElseCouldAnswer,
				TheUnitEverythingIsIn, ThePublication);
			journal.Api.RecordDelivery(
				TheDayItWasHandedOver, TheOdometerAtDelivery, TheUnitEverythingIsIn);
			journal.Api.RecordReading(
				TheOdometerToday, TheUnitEverythingIsIn, TheDayItWasObserved, "Telemetry");
		}

		[TearDown]
		public void TearDown()
		{
			journal.Dispose();
		}

		// LAS SIETE PREGUNTAS DE LA PUERTA QUE CONTESTAN CON UN NUMERO. Conjunto exacto, con la
		// aridad en la clave: una sobrecarga es su propio sitio y el rojo la nombra.
		private static readonly string[] EveryNumberTheDoorHandsOut =
		{
			"CountDue/0",
			"CountOverdue/0",
			"DeferralsUsed/1",
			"MissedCount/0",
			"LastEventApplied/0",
			"ReadDeliveryReading/0",
			"ReadOdometer/1",
		};

		[Test]
		public void TheDoorHandsOutTheseNumbersAndNoOthers()
		{
			Assert.That(EveryQuestionAnsweringWithANumber(), Is.EquivalentTo(EveryNumberTheDoorHandsOut),
				"La puerta gano o perdio una pregunta que contesta con un NUMERO. ESTA ES LA PRUEBA "
				+ "QUE SE PONE ROJA EL DIA QUE ALGUIEN SACA UNA MITAD DE LA FRASE: un limite del "
				+ "sobre, lo que le queda a una mitad, o cualquier cantidad que el dominio derivo de "
				+ "otras. Si lo nuevo es de verdad un hecho RECIBIDO que el registro guarda tal como "
				+ "llego, se agrega aca y se dice por que; si es una mitad de una respuesta "
				+ "compuesta, ya llega a quien pregunta dentro de la frase.");
		}

		// Y LA MITAD POR VALOR, que es la que el censo no da: con un sobre publicado, ninguna de las
		// siete contesta ninguno de sus dos limites. El censo prueba que nadie agrego un miembro sin
		// escribirlo; esto prueba que ninguno de los que hay entrega media respuesta.
		[Test]
		public void NotOneOfThemAnswersWithEitherLimitOfThePublishedEnvelope()
		{
			foreach (KeyValuePair<string, long> answered in EveryNumberAnsweredRightNow())
			{
				Assert.That(answered.Value, Is.Not.EqualTo(TheMonthsNothingElseCouldAnswer),
					answered.Key + " contesto el PLAZO del sobre.");
				Assert.That(answered.Value, Is.Not.EqualTo(TheDistanceNothingElseCouldAnswer),
					answered.Key + " contesto la DISTANCIA del sobre.");
			}
		}

		// Y LOS DOS LIMITES SI LLEGAN A QUIEN PREGUNTA -- dentro de la frase, que es el otro lado de
		// la misma ley. Sin este renglon, la de arriba se cumpliria tambien si la puerta no dijera
		// los limites en ninguna parte, que seria un defecto distinto y peor.
		[Test]
		public void BothLimitsReachTheCallerInsideTheSentence()
		{
			Assert.That(
				journal.Api.DescribePublishedWarrantyEnvelope(),
				Does.Contain("37 months or 61111 mi"));

			Assert.That(
				journal.Api.DescribeWarrantyEnvelope(TheDayItIsAsked),
				Does.Contain("37 months or 61111 mi"));
		}

		// Y LO QUE LE QUEDA A CADA MITAD, TAMBIEN DENTRO DE LA FRASE Y EN NINGUN OTRO LADO. Es la
		// misma ley aplicada a lo DERIVADO en vez de a lo publicado, y es la mitad que mas tentacion
		// da: una pantalla que quiera pintar una barra querria los dos numeros sueltos.
		[Test]
		public void WhatIsLeftOfEachHalfReachesTheCallerInsideTheSentenceAndNowhereElse()
		{
			string said = journal.Api.DescribeWarrantyEnvelope(TheDayItIsAsked);

			Assert.That(said, Does.Contain("Time: 29 months left of 37 months."));
			Assert.That(said, Does.Contain("Distance: 56111 mi left of 61111 mi."));

			foreach (KeyValuePair<string, long> answered in EveryNumberAnsweredRightNow())
			{
				Assert.That(answered.Value, Is.Not.EqualTo(29),
					answered.Key + " contesto los meses que quedan.");
				Assert.That(answered.Value, Is.Not.EqualTo(56111),
					answered.Key + " contesto la distancia que queda.");
			}
		}

		private static List<string> EveryQuestionAnsweringWithANumber()
		{
			var found = new List<string>();

			foreach (MethodInfo each in TheDoorsMembers())
				if (AnswersWithANumber(each))
					found.Add(each.Name + "/" + each.GetParameters().Length);

			return found;
		}

		// Cada una de las siete, invocada contra este registro. Los argumentos se arman por TIPO y no
		// por nombre: es lo que hace que una lectura nueva entre al barrido sola en vez de esperar a
		// que alguien la agregue.
		private List<KeyValuePair<string, long>> EveryNumberAnsweredRightNow()
		{
			var answered = new List<KeyValuePair<string, long>>();

			foreach (MethodInfo each in TheDoorsMembers())
			{
				if (!AnswersWithANumber(each)) continue;

				answered.Add(new KeyValuePair<string, long>(
					each.Name, Convert.ToInt64(Invoke(each))));
			}

			Assert.That(answered, Has.Count.EqualTo(EveryNumberTheDoorHandsOut.Length),
				"El barrido dejo de encontrar alguna de las que cuenta.");

			return answered;
		}

		private object Invoke(MethodInfo member)
		{
			var arguments = new List<object>();

			foreach (ParameterInfo each in member.GetParameters())
			{
				if (each.ParameterType == typeof(DateTime)) { arguments.Add(TheDayItIsAsked); continue; }
				if (each.ParameterType == typeof(string)) { arguments.Add("FluidChange"); continue; }

				Assert.Fail(
					"La pregunta '" + member.Name + "' toma un " + each.ParameterType.Name
					+ ", que este barrido no sabe armar. Se arma por TIPO a proposito; agregue el tipo "
					+ "nuevo y diga con que valor se pregunta.");
			}

			try
			{
				return member.Invoke(journal.Api, arguments.ToArray());
			}
			catch (TargetInvocationException wrapped)
			{
				throw wrapped.InnerException;
			}
		}

		private static bool AnswersWithANumber(MethodInfo member)
		{
			return member.ReturnType == typeof(int) || member.ReturnType == typeof(long);
		}

		private static MethodInfo[] TheDoorsMembers()
		{
			return typeof(DealershipCarAPI).GetMethods(
				BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
		}
	}
}
