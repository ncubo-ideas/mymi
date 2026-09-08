using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace Concesionario.Dominio.Pruebas
{
	// LA POLARIDAD PARTIDA DE E41 SOBRE LAS LECTURAS DEL ODOMETRO, como ambito de regla propio.
	//
	// POR QUE UNA FIXTURE NUEVA Y NO UNA LINEA EN LA QUE YA HAY: la prosa de
	// EveryReadOfTheScheduleKnowsItsPolarity se acota sola a "las nueve lecturas de §4" -- las
	// nueve del contrato de OS2 -- y las enumera y afirma las nueve, asi que su alcance declarado
	// sigue siendo exactamente lo que cubre. Estirarla a estas cinco la dejaria prometiendo un
	// ambito distinto del que su prosa nombra, que es el defecto C57 escrito a mano.
	//
	// LO QUE ESTA FIXTURE AGREGA Y NINGUNA OTRA TIENE: la ENUMERACION COMPLETA. Las otras dos del
	// slice afirman sobre miembros por nombre, asi que una sexta lectura del odometro con la
	// polaridad equivocada entraria verde. Aca la lista sale por reflexion y la polaridad se
	// afirma SOBRE ESA MISMA LISTA, de manera que agregar una sexta pone rojo la enumeracion antes
	// de que nadie se acuerde de escribirle su prueba.
	//
	// Y SE AFIRMAN LOS DOS LADOS DEL PARTIDO, no solo el de la ausencia: sin lectura usable
	// contesta exactamente la de presencia y revientan las otras cuatro; con lectura usable
	// contestan las cinco. Un guardado que reventara siempre pasaria la mitad de esta prueba.
	[TestFixture]
	public sealed class TheOdometerReadKnowsItsPolarity
	{
		private const string TheVin = "1HGCM82633A004352";
		private const int TheReading = 35000;

		private static readonly DateTime ObservedAt = new DateTime(2026, 3, 10);
		private static readonly DateTime TheInstantAsked = new DateTime(2026, 6, 15);

		// LA TOTAL, la unica que contesta cuando no hay nada que leer.
		private const string LaLecturaDePresencia = "HasAnOdometerReading";

		// Las cinco, en orden ordinal. La unidad sale DOS VECES a proposito: el nombre proyectado
		// para la puerta y el codigo para el script que lo cruza a otro global.
		private static readonly string[] LasCincoLecturasDelOdometro =
		{
			"HasAnOdometerReading",
			"OdometerUnit",
			"ReadOdometer",
			"ReadOdometerObservedAt",
			"ReadOdometerUnit",
		};

		[Test]
		public void TheOdometerReadsAreExactlyTheseFiveSoASixthCannotSlipInUnswept()
		{
			List<string> expuestas = LasLecturasDelOdometro()
				.Select(lectura => lectura.Name)
				.ToList();

			Assert.That(expuestas, Is.EqualTo(LasCincoLecturasDelOdometro),
				"Las lecturas del odometro de la agenda cambiaron: "
				+ string.Join(", ", expuestas) + ". Una barrida vale lo que vale su enumeracion "
				+ "(C57): esta prueba afirma la polaridad SOBRE esta lista, asi que una sexta "
				+ "lectura sin polaridad afirmada tiene que ponerla roja primero.");
		}

		[Test]
		public void WithNoUsableReadingExactlyThePresenceReadAnswers()
		{
			MaintenanceSchedule schedule =
				new MaintenanceSchedule(TheVin, UsageProfileCode.Standard);

			foreach (MethodInfo lectura in LasLecturasDelOdometro())
			{
				if (lectura.Name == LaLecturaDePresencia)
				{
					Assert.That(Invocar(lectura, schedule), Is.EqualTo(false),
						"La de presencia es TOTAL: sin lectura usable contesta que no hay, y esa "
						+ "ausencia es una respuesta legal.");
					continue;
				}

				Assert.That(LoQueRevento(lectura, schedule), Is.InstanceOf<DomainException>(),
					lectura.Name + " contesto sin lectura que leer. Ausente no es cero: un 0 "
					+ "aqui es un odometro sin recorrer, y contra un sobre de garantia eso es una "
					+ "promesa.");
			}
		}

		[Test]
		public void WithAUsableReadingAllFiveAnswer()
		{
			MaintenanceSchedule schedule =
				new MaintenanceSchedule(TheVin, UsageProfileCode.Standard);

			schedule.RecordReading(
				TheReading, DistanceUnitCode.UnitB, ObservedAt, ReadingSourceCode.Telemetry);

			foreach (MethodInfo lectura in LasLecturasDelOdometro())
				Assert.That(LoQueRevento(lectura, schedule), Is.Null,
					lectura.Name + " revento con una lectura usable presente. Un guardado que "
					+ "revienta siempre tambien pasa la mitad de ausencia de esta prueba.");

			// Y CONTESTAN LO QUE LA LECTURA DICE, no un valor por defecto: UnitB a proposito,
			// porque con UnitA -- el primer miembro -- un codigo defaulteado daria lo mismo (C60).
			Assert.That(schedule.HasAnOdometerReading(TheInstantAsked), Is.True);
			Assert.That(schedule.ReadOdometer(TheInstantAsked), Is.EqualTo(TheReading));
			Assert.That(schedule.ReadOdometerUnit(TheInstantAsked), Is.EqualTo("mi"));
			Assert.That(schedule.OdometerUnit(TheInstantAsked), Is.EqualTo(DistanceUnitCode.UnitB));
			Assert.That(schedule.ReadOdometerObservedAt(TheInstantAsked), Is.EqualTo(ObservedAt));
		}

		// Las cinco toman el INSTANTE PREGUNTADO y nada mas. Una que no lo tomara seria la forma
		// ligera que D3 rechazo: "la ultima registrada, y punto".
		[Test]
		public void EveryOneOfThemIsKeyedOnTheInstantAsked()
		{
			foreach (MethodInfo lectura in LasLecturasDelOdometro())
			{
				ParameterInfo[] parametros = lectura.GetParameters();

				Assert.That(parametros.Length, Is.EqualTo(1), lectura.Name);
				Assert.That(parametros[0].ParameterType, Is.EqualTo(typeof(DateTime)),
					lectura.Name + " no esta claveada al instante preguntado. Sin la clave, una "
					+ "lectura fechada hacia adelante entra por una puerta y no por la otra.");
			}
		}

		private static IEnumerable<MethodInfo> LasLecturasDelOdometro()
		{
			return typeof(MaintenanceSchedule)
				.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public
					| BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
				.Where(miembro => miembro.IsPublic || miembro.IsAssembly)
				.Where(miembro => miembro.Name.IndexOf("Odometer", StringComparison.Ordinal) >= 0)
				.OrderBy(miembro => miembro.Name, StringComparer.Ordinal);
		}

		private static object Invocar(MethodInfo lectura, MaintenanceSchedule schedule)
		{
			return lectura.Invoke(schedule, new object[] { TheInstantAsked });
		}

		private static Exception LoQueRevento(MethodInfo lectura, MaintenanceSchedule schedule)
		{
			try
			{
				Invocar(lectura, schedule);

				return null;
			}
			catch (TargetInvocationException envuelta)
			{
				return envuelta.InnerException;
			}
		}
	}
}
