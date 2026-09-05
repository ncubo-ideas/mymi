using System;
using System.Globalization;
using NUnit.Framework;

namespace DealershipCar.Dominio.Pruebas
{
	// L5, Y LA DECISION QUE §10 DEL BRIEF EXIGE -- DEC-99. Re-placar es ordinario: un traspaso, un
	// cambio de provincia, un cambio de clase de placa. Una segunda placa es un HECHO NUEVO y no una
	// correccion de la primera, y la respuesta a "cual es la placa" es "cual placa al instante X".
	//
	// LO QUE §10 DEJA ABIERTO Y DEC-99 CIERRA: una asignacion que LLEGA despues pero esta FECHADA
	// antes. Se COLOCA donde su instante manda -- insercion ordenada -- y no se rechaza ni pisa a la
	// vigente.
	//
	//   pisar        es la forma de RecordDelivery, que es correctivo POR DECISION (DEC-23) porque la
	//                entrega es UN hecho historico que pudo registrarse mal. L5 dice lo contrario de
	//                una placa, asi que copiar esa forma haria que una placa vieja que llega tarde
	//                DESTRUYA la vigente;
	//
	//   rechazar     deja cicatriz. Un hecho viejo que llega tarde es normal en un feed y nadie se
	//                equivoco; la excepcion caeria adentro del comando, donde la entrada ya se pago, y
	//                el motor loguearia un fallo de rehidratacion en cada arranque futuro.
	//
	// LOS DOS SUBCASOS QUE UNA PRUEBA NO VE SI NADIE LOS ESCRIBE, y por eso estan abajo con nombre:
	//
	//   DOS AL MISMO INSTANTE. El journal trunca a segundos enteros y borra el Kind, asi que dos
	//   llegadas dentro de un mismo segundo son INDISTINGUIBLES despues de reabrir: el empate no es
	//   exotico, es lo que el sustrato produce. Gana la ULTIMA EN LLEGAR -- se inserta DESPUES de los
	//   iguales -- que es la forma que los cuatro sitios de insercion de este modulo ya usan: un
	//   'while (place > 0 && lista[place-1].X > llegando.X) place--' con MAYOR ESTRICTO. Y un > donde
	//   iba un >= es INVISIBLE al segundo programa: el texto del DSL y sus argumentos son identicos
	//   en los dos casos, porque el orden pasa adentro del dominio y nunca cruza el wire.
	//
	//   LA MISMA PLACA DOS VECES. Dos filas, sin dedupe. El unico dedupe del modulo esta en
	//   APeriodWithStaysOnRecord y es sobre DIAS, que son elementos DERIVADOS: nada en este modulo ha
	//   descartado nunca algo que llego.
	[TestFixture]
	public sealed class ASecondPlateIsANewFactAndNotACorrection
	{
		private const string TheVin = ADealershipNarrative.TheVin;

		private const string TheFirstPlate = "BQR-482";
		private const string TheSecondPlate = "XKD-119";
		private const string TheThirdPlate = "MPL-007";

		private static readonly DateTime InMarch =
			new DateTime(2025, 3, 10, 0, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime InJune =
			new DateTime(2025, 6, 20, 0, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime InNovember =
			new DateTime(2025, 11, 5, 0, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime InTheFollowingYear =
			new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc);

		// ===== PRUEBA 8 · DOS ASIGNACIONES, LA POSTERIOR VIGENTE, LA ANTERIOR NO PERDIDA ===

		[Test]
		public void TwoAssignmentsAtDifferentInstantsAreBothReadableAndTheLaterOneIsCurrent()
		{
			using (AScheduleOnARealJournal journal = AJournal())
			{
				APlateOnTheDoorAndInTheDomain.RecordAssignment(journal.Api, TheFirstPlate, InMarch);
				APlateOnTheDoorAndInTheDomain.RecordAssignment(journal.Api, TheSecondPlate, InJune);

				Assert.That(
					APlateOnTheDoorAndInTheDomain.AsPublished(journal.Api, InTheFollowingYear),
					Is.EqualTo(TheSecondPlate),
					"La placa de hoy es la ultima asignada, no la primera.");

				// Y LA ANTERIOR NO SE PERDIO: se pregunta por su propio instante y contesta.
				Assert.That(
					APlateOnTheDoorAndInTheDomain.AsPublished(journal.Api, InMarch.AddDays(5)),
					Is.EqualTo(TheFirstPlate),
					"Preguntar por un instante entre las dos tiene que dar la PRIMERA. Si da la "
					+ "segunda, la lectura no esta filtrando por el instante y contesta siempre lo "
					+ "ultimo que entro.");

				Assert.That(
					APlateOnTheDoorAndInTheDomain.AssignmentColumn(journal.Api, "AsPublished"),
					Is.EqualTo(new[] { TheFirstPlate, TheSecondPlate }),
					"Las dos filas quedan, en orden de la linea de tiempo.");
			}
		}

		// ANTES DE LA PRIMERA ASIGNACION NO HAY PLACA, y es lo que separa "todavia no" de "no hay
		// ninguna": el mismo registro contesta las dos cosas segun el instante que se le pregunte.
		[Test]
		public void AndBeforeTheFirstAssignmentThereIsNoPlateAtThatInstant()
		{
			using (AScheduleOnARealJournal journal = AJournal())
			{
				APlateOnTheDoorAndInTheDomain.RecordAssignment(journal.Api, TheFirstPlate, InJune);

				Assert.That(
					APlateOnTheDoorAndInTheDomain.IsPublished(journal.Api, InMarch), Is.False,
					"En marzo todavia no habia placa, aunque hoy la haya.");

				Assert.That(
					APlateOnTheDoorAndInTheDomain.IsPublished(journal.Api, InNovember), Is.True);
			}
		}

		// ===== PRUEBA 9 · LA QUE LLEGA TARDE Y ESTA FECHADA ANTES ==========================

		[Test]
		public void AnAssignmentArrivingWithAnEarlierInstantIsPlacedAndNeitherPlateIsLost()
		{
			using (AScheduleOnARealJournal journal = AJournal())
			{
				// Llega primero la de NOVIEMBRE y despues la de MARZO. El orden de LLEGADA es el
				// contrario al de la linea de tiempo, que es el caso ordinario de un feed que
				// rellena hacia atras.
				APlateOnTheDoorAndInTheDomain.RecordAssignment(journal.Api, TheSecondPlate, InNovember);
				APlateOnTheDoorAndInTheDomain.RecordAssignment(journal.Api, TheFirstPlate, InMarch);

				Assert.That(
					APlateOnTheDoorAndInTheDomain.AssignmentColumn(journal.Api, "AsPublished"),
					Is.EqualTo(new[] { TheFirstPlate, TheSecondPlate }),
					"La que llego tarde se COLOCA donde su instante manda. Si sale al final, la "
					+ "historia esta en orden de llegada y no de asignacion.");

				Assert.That(
					APlateOnTheDoorAndInTheDomain.AsPublished(journal.Api, InTheFollowingYear),
					Is.EqualTo(TheSecondPlate),
					"Y la vigente NO cambio. Pisar seria la forma de RecordDelivery, que L5 prohibe: "
					+ "una placa vieja que llega tarde no destruye la de hoy.");

				Assert.That(
					APlateOnTheDoorAndInTheDomain.AsPublished(journal.Api, InJune),
					Is.EqualTo(TheFirstPlate),
					"Y en junio contesta la de marzo, que es la que regia entonces.");
			}
		}

		// Y NO SE RECHAZA. El rechazo caeria adentro del comando, donde la entrada ya esta escrita.
		[Test]
		public void AndItIsNotRefused()
		{
			using (AScheduleOnARealJournal journal = AJournal())
			{
				APlateOnTheDoorAndInTheDomain.RecordAssignment(journal.Api, TheSecondPlate, InNovember);

				Assert.DoesNotThrow(
					() => APlateOnTheDoorAndInTheDomain.RecordAssignment(
						journal.Api, TheFirstPlate, InMarch),
					"Un hecho viejo que llega tarde es normal en un feed y nadie se equivoco. "
					+ "Rechazarlo deja una cicatriz permanente que se loguea en cada arranque.");
			}
		}

		// ===== SUBCASO 1 · DOS AL MISMO INSTANTE ==========================================

		[Test]
		public void AtAnEqualInstantTheLaterArrivalIsTheOneThatAnswers()
		{
			using (AScheduleOnARealJournal journal = AJournal())
			{
				APlateOnTheDoorAndInTheDomain.RecordAssignment(journal.Api, TheFirstPlate, InJune);
				APlateOnTheDoorAndInTheDomain.RecordAssignment(journal.Api, TheSecondPlate, InJune);

				Assert.That(
					APlateOnTheDoorAndInTheDomain.AsPublished(journal.Api, InNovember),
					Is.EqualTo(TheSecondPlate),
					"Con el MISMO instante gana la ULTIMA EN LLEGAR: se inserta DESPUES de sus "
					+ "iguales. Es la forma de los cuatro sitios de insercion del modulo -- un "
					+ "'>' estricto -- y un '>=' aca daria la primera sin que nada mas cambiara.");

				Assert.That(
					APlateOnTheDoorAndInTheDomain.AssignmentColumn(journal.Api, "AsPublished"),
					Is.EqualTo(new[] { TheFirstPlate, TheSecondPlate }),
					"Y las dos quedan: un empate no descarta ninguna.");
			}
		}

		// Y SIGUE ASI DESPUES DE REABRIR EL JOURNAL, que es donde el subcaso deja de ser exotico: el
		// journal trunca a segundos enteros, asi que dos instantes que en memoria se distinguian por
		// milisegundos son el MISMO instante despues del replay.
		[Test]
		public void AndTheOrderSurvivesTheJournalTruncatingEveryInstantToAWholeSecond()
		{
			using (AScheduleOnARealJournal journal = AJournal())
			{
				DateTime early = new DateTime(2025, 6, 20, 10, 0, 0, 250, DateTimeKind.Utc);
				DateTime late = new DateTime(2025, 6, 20, 10, 0, 0, 750, DateTimeKind.Utc);

				APlateOnTheDoorAndInTheDomain.RecordAssignment(journal.Api, TheFirstPlate, early);
				APlateOnTheDoorAndInTheDomain.RecordAssignment(journal.Api, TheSecondPlate, late);

				string live = APlateOnTheDoorAndInTheDomain.AsPublished(journal.Api, InNovember);

				journal.CloseAndReopenFromTheJournal();

				Assert.That(
					APlateOnTheDoorAndInTheDomain.AsPublished(journal.Api, InNovember),
					Is.EqualTo(live),
					"Las dos se truncaron al MISMO segundo, asi que despues de reabrir el empate es "
					+ "real. La respuesta viva y la reproducida tienen que coincidir, y coinciden "
					+ "solo si la regla del empate es la de la insercion y no la del milisegundo.");

				Assert.That(
					APlateOnTheDoorAndInTheDomain.AsPublished(journal.Api, InNovember),
					Is.EqualTo(TheSecondPlate));
			}
		}

		// ===== SUBCASO 2 · LA MISMA PLACA DOS VECES =======================================

		[Test]
		public void TheSamePlateAssignedTwiceIsTwoRowsAndNotOne()
		{
			using (AScheduleOnARealJournal journal = AJournal())
			{
				APlateOnTheDoorAndInTheDomain.RecordAssignment(journal.Api, TheFirstPlate, InMarch);
				APlateOnTheDoorAndInTheDomain.RecordAssignment(journal.Api, TheSecondPlate, InJune);
				APlateOnTheDoorAndInTheDomain.RecordAssignment(journal.Api, TheFirstPlate, InNovember);

				Assert.That(
					APlateOnTheDoorAndInTheDomain.AssignmentColumn(journal.Api, "AsPublished"),
					Is.EqualTo(new[] { TheFirstPlate, TheSecondPlate, TheFirstPlate }),
					"Tres asignaciones, tres filas, aunque dos digan lo mismo. Descartar la tercera "
					+ "seria borrar un hecho que la agencia mando -- y una placa SI se re-emite, "
					+ "cuando un traspaso se revierte.");
			}
		}

		// ===== LAS FILAS SE DESCRIBEN A SI MISMAS, Y ESTAN ATADAS A LO RECIBIDO =============

		// EL ANCLA DE E49. Todo lo de arriba compara una lectura contra otra o contra la enumeracion,
		// y las dos pasan por el MISMO resolvedor privado: se ponen de acuerdo por construccion,
		// incluido el caso en que el resolvedor este uniformemente equivocado. Esto ata las columnas a
		// los LITERALES que se mandaron.
		//
		// LA FALSIFICACION ES CONCRETA: borrar el filtro 'assignedAt <= asOf' del resolvedor y volver
		// a correr. Si todo sigue verde, las pruebas estan midiendo el par equivocado.
		[Test]
		public void EveryColumnCarriesWhatTheDealershipActuallySent()
		{
			using (AScheduleOnARealJournal journal = AJournal())
			{
				APlateOnTheDoorAndInTheDomain.RecordAssignment(journal.Api, TheFirstPlate, InMarch);
				APlateOnTheDoorAndInTheDomain.RecordAssignment(journal.Api, TheSecondPlate, InJune);
				APlateOnTheDoorAndInTheDomain.RecordAssignment(journal.Api, TheThirdPlate, InNovember);

				Assert.That(
					APlateOnTheDoorAndInTheDomain.AssignmentColumn(journal.Api, "AsPublished"),
					Is.EqualTo(new[] { TheFirstPlate, TheSecondPlate, TheThirdPlate }),
					"El texto TAL COMO SE PUBLICO, con su guion.");

				Assert.That(
					APlateOnTheDoorAndInTheDomain.AssignmentColumn(journal.Api, "JoinKey"),
					Is.EqualTo(new[] { "BQR482", "XKD119", "MPL007" }),
					"Y la llave, normalizada. Las dos columnas salen de la puerta YA DECIDIDAS: "
					+ "ninguna caratula deriva una de la otra (L7).");

				Assert.That(
					APlateOnTheDoorAndInTheDomain.AssignmentColumn(journal.Api, "AssignedAt"),
					Is.EqualTo(new[]
					{
						TheDay(InMarch), TheDay(InJune), TheDay(InNovember),
					}),
					"Y el instante de cada una, que es lo que hace que la respuesta pueda decir "
					+ "DESDE CUANDO (L5).");

				Assert.That(
					APlateOnTheDoorAndInTheDomain.AssignmentColumn(journal.Api, "IsCurrent"),
					Is.EqualTo(new[] { "false", "false", "true" }),
					"Y cual es la vigente: exactamente una, y es la ultima de la linea de tiempo.");
			}
		}

		private static string TheDay(DateTime instant)
		{
			return instant.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
		}

		private static AScheduleOnARealJournal AJournal()
		{
			return new AScheduleOnARealJournal(TheVin, "Standard");
		}
	}
}
