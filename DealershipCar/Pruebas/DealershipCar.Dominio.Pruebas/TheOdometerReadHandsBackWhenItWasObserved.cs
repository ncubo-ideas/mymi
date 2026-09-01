using System;
using NUnit.Framework;

namespace DealershipCar.Dominio.Pruebas
{
	// TEST 14 DE §11 DEL BRIEF, y la LINEA DE HONESTIDAD de su §3: "la lectura del odometro
	// devuelve su instante de OBSERVACION, y es el observado, no el preguntado".
	//
	// POR QUE ES LA LINEA DE HONESTIDAD: este modulo no construye la mitad de frescura -- esa era
	// OS3 y OS3 no tiene brief -- asi que sin esto la app puede decir "faltan 1600" con toda
	// confianza desde una lectura de marzo. La mitigacion es estrecha y suficiente: la frase
	// compuesta puede NOMBRAR contra que lectura se midio y cuando se observo. Es la linea que OS3
	// convertiria en un sello de frescura; no es un sustituto de uno.
	//
	// LAS DOS FECHAS SE DIFERENCIAN EN MESES A PROPOSITO (C60): con una observacion y un instante
	// preguntado del mismo dia, devolver el preguntado en lugar del observado daria exactamente la
	// misma respuesta y la prueba estaria verde sin mirar el codigo.
	[TestFixture]
	public sealed class TheOdometerReadHandsBackWhenItWasObserved
	{
		private const string TheVin = "3VW2K7AJ9FM123456";
		private const int TheReadingInMarch = 35000;
		private const int TheReadingInMay = 38000;

		private static readonly DateTime ObservedInMarch = new DateTime(2026, 3, 10);
		private static readonly DateTime ObservedInMay = new DateTime(2026, 5, 4);
		private static readonly DateTime TheInstantAsked = new DateTime(2026, 6, 15);

		private MaintenanceSchedule schedule;

		[SetUp]
		public void Setup()
		{
			schedule = new MaintenanceSchedule(TheVin, UsageProfileCode.Standard);
		}

		[Test]
		public void TheInstantItHandsBackIsTheObservedOneAndNotTheOneAsked()
		{
			schedule.RecordReading(
				TheReadingInMarch, DistanceUnitCode.UnitA, ObservedInMarch,
				ReadingSourceCode.Telemetry);

			Assert.That(schedule.ReadOdometerObservedAt(TheInstantAsked),
				Is.EqualTo(ObservedInMarch));
			Assert.That(schedule.ReadOdometerObservedAt(TheInstantAsked),
				Is.Not.EqualTo(TheInstantAsked),
				"Devolver el instante preguntado haria que una lectura de marzo se leyera como "
				+ "tomada hoy, que es justo lo que la frase compuesta tiene que poder desmentir.");
		}

		// Y ES EL INSTANTE DE LA LECTURA QUE SE USO, no el de la ultima registrada: con dos
		// lecturas usables, la que manda es la mas reciente AL INSTANTE PREGUNTADO, y su fecha
		// viaja con su numero.
		[Test]
		public void TheInstantAndTheNumberComeFromTheSameReading()
		{
			schedule.RecordReading(
				TheReadingInMarch, DistanceUnitCode.UnitA, ObservedInMarch,
				ReadingSourceCode.Telemetry);
			schedule.RecordReading(
				TheReadingInMay, DistanceUnitCode.UnitA, ObservedInMay,
				ReadingSourceCode.Telemetry);

			Assert.That(schedule.ReadOdometer(TheInstantAsked), Is.EqualTo(TheReadingInMay));
			Assert.That(schedule.ReadOdometerObservedAt(TheInstantAsked), Is.EqualTo(ObservedInMay));

			Assert.That(schedule.ReadOdometer(ObservedInMay.AddDays(-1)),
				Is.EqualTo(TheReadingInMarch));
			Assert.That(schedule.ReadOdometerObservedAt(ObservedInMay.AddDays(-1)),
				Is.EqualTo(ObservedInMarch),
				"El numero y su fecha salen de la MISMA lectura. Que se despeguen es la forma en "
				+ "que una frase dice un numero de mayo fechado en marzo.");
		}

		// La unidad tambien viaja con el numero, dicha con su nombre de mundo real para quien lea.
		[Test]
		public void TheUnitTravelsWithTheNumberAndIsSaidInItsRealWorldName()
		{
			schedule.RecordReading(
				TheReadingInMarch, DistanceUnitCode.UnitB, ObservedInMarch,
				ReadingSourceCode.Telemetry);

			Assert.That(schedule.ReadOdometerUnit(TheInstantAsked), Is.EqualTo("mi"));
		}

		// LA UNIDAD SALE DOS VECES -- el nombre para la puerta, el codigo para el script -- y este
		// es el mapeo INVERSO en su SEGUNDA copia del modulo: la primera vive en
		// AWarrantyEnvelope.TheCodeOfItsDistanceUnit y no se puede reusar porque esta amarrada a la
		// unidad del sobre, y no puede vivir en DistanceUnit junto a OfCode porque su lista blanca
		// tiene exactamente cuatro miembros y no se toca.
		//
		// DOS COPIAS PUEDEN DESPEGARSE, asi que el resolutor las vuelve a juntar: si se despegaran,
		// el script cruzaria un codigo que mide contra una unidad distinta de la que la frase
		// nombra, y nada se pondria rojo. Las DOS unidades se recorren: con una sola, un mapeo que
		// devolviera siempre el primer miembro pasaria la mitad de la prueba.
		[Test]
		public void TheCodeAndTheNameAreTheSameUnitSaidTwice()
		{
			foreach (DistanceUnitCode unit in new[] { DistanceUnitCode.UnitA, DistanceUnitCode.UnitB })
			{
				MaintenanceSchedule inThatUnit =
					new MaintenanceSchedule(TheVin, UsageProfileCode.Standard);

				inThatUnit.RecordReading(
					TheReadingInMarch, unit, ObservedInMarch, ReadingSourceCode.Telemetry);

				Assert.That(inThatUnit.OdometerUnit(TheInstantAsked), Is.EqualTo(unit),
					"El codigo que sale tiene que ser el de la lectura que entro.");
				Assert.That(
					DistanceUnit.OfCode(inThatUnit.OdometerUnit(TheInstantAsked)).Name,
					Is.EqualTo(inThatUnit.ReadOdometerUnit(TheInstantAsked)),
					"El codigo y el nombre son la misma unidad dicha dos veces. Que el resolutor "
					+ "los vuelva a juntar es lo unico que impide que las dos copias del mapeo "
					+ "inverso se despeguen sin que nadie lo note.");
			}
		}
	}
}
