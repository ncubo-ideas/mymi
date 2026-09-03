using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace DealershipCar.Dominio.Pruebas
{
	// E35 Y LEY L3, LA MITAD ESTRUCTURAL. El conteo de dias se DERIVA de los dias en cada lectura y
	// no vive en ningun campo. Es el mismo barrido que RemainingIsDerivedNeverStored hace sobre el
	// sobre de garantia, con el mismo argumento: un contador guardado es correcto hasta el siguiente
	// hecho y esta equivocado desde ahi, y una prueba conductual no lo ve mientras alguien se
	// acuerde de recalcularlo.
	//
	// EL CENSO ES SOBRE LO QUE SOBREVIVE ENTRE DOS LECTURAS -- el agregado y la estadia cerrada. No
	// sobre StaysInAPeriod ni sus dos variantes: esas se construyen de nuevo en cada lectura y
	// guardan lo que acaban de calcular, que es lo contrario del defecto.
	//
	// LOS DIAS SI SE GUARDAN, y esa es la diferencia entera. Los dias son un hecho RECIBIDO (L4):
	// llegaron con la salida y el registro los conserva tal como llegaron. Lo que no se guarda es
	// CUANTOS son, que es una cuenta sobre ellos.
	//
	// Y EL CANON PROHIBE UN CAMPO LLAMADO duration (§4 del brief). Va en la lista de nombres
	// prohibidos junto con los demas que serian el mismo defecto con otra palabra.
	[TestFixture]
	public sealed class TheStaysDayCountIsDerivedAndNeverStored
	{
		private const BindingFlags Declarados = BindingFlags.DeclaredOnly | BindingFlags.Instance
			| BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

		private static readonly string[] NombresQueSerianUnConteoGuardado =
			{ "count", "total", "howmany", "number", "duration", "length", "size", "tally" };

		// EL CENSO, IMPRESO. Lo que el agregado retiene, campo por campo, con su tipo.
		[Test]
		public void TheAggregateHoldsTheVinAndTheStaysAndNothingElse()
		{
			List<string> censo = ElCensoDe(typeof(ShopStay));

			TestContext.Out.WriteLine("CENSO DE CAMPOS -- ShopStay: " + string.Join(", ", censo));

			Assert.That(censo, Is.EqualTo(new[] { "_stays : List<AStay>", "_vin : Vin" }),
				"§4.1 del plan: el agregado guarda las estadias en orden de entrada y nada mas que "
				+ "no sea derivable. Un campo de mas es por donde una respuesta cacheada entra sin "
				+ "que nadie la llame cache. Censo: " + string.Join(", ", censo));
		}

		// Y LA ESTADIA CERRADA GUARDA SUS DOS EXTREMOS Y LOS DIAS RECIBIDOS. No cuantos son.
		[Test]
		public void AClosedStayHoldsItsExitAndItsReceivedDaysAndNothingElse()
		{
			List<string> censo = ElCensoDe(typeof(AClosedStay));

			TestContext.Out.WriteLine("CENSO DE CAMPOS -- AClosedStay: " + string.Join(", ", censo));

			Assert.That(censo, Is.EqualTo(new[] { "_collectedAt : DateTime", "_days : List<StayDay>" }),
				"los dias SI se guardan -- son un hecho recibido (L4) -- y cuantos son NO. Censo: "
				+ string.Join(", ", censo));
		}

		// LA BASE TAMPOCO, que es donde un contador compartido se esconderia sin salir en ninguno de
		// los dos censos de arriba (los dos son DeclaredOnly).
		[Test]
		public void TheOpenStayAndTheBaseHoldNoCountEither()
		{
			List<string> deLaBase = ElCensoDe(typeof(AStay));
			List<string> deLaAbierta = ElCensoDe(typeof(AnOpenStay));

			TestContext.Out.WriteLine("CENSO DE CAMPOS -- AStay: " + string.Join(", ", deLaBase));
			TestContext.Out.WriteLine("CENSO DE CAMPOS -- AnOpenStay: " + string.Join(", ", deLaAbierta));

			Assert.That(deLaBase,
				Is.EqualTo(new[] { "_branch : String", "_enteredAt : DateTime", "_subject : StaySubject" }));
			Assert.That(deLaAbierta, Is.Empty,
				"una estadia abierta no tiene nada propio que guardar: su estado ES su tipo (E29).");
		}

		// NINGUN NOMBRE CON FORMA DE CONTEO, en ninguno de los cuatro. Un campo llamado _dayCount o
		// _duration es el mismo defecto con otra palabra, y duration esta prohibido por el canon.
		[Test]
		public void NoFieldOfAnythingThatSurvivesIsShapedLikeACount()
		{
			List<string> culpables = new List<string>();

			foreach (Type tipo in new[]
				{ typeof(ShopStay), typeof(AStay), typeof(AnOpenStay), typeof(AClosedStay) })
			{
				foreach (FieldInfo campo in tipo.GetFields(Declarados))
				{
					string nombre = campo.Name.ToLowerInvariant();

					if (NombresQueSerianUnConteoGuardado.Any(prohibido => nombre.Contains(prohibido)))
						culpables.Add(tipo.Name + "." + campo.Name);
				}
			}

			Assert.That(culpables, Is.Empty,
				"L3 y E35: aparecio estado con forma de conteo: " + string.Join(", ", culpables));
		}

		// LA MITAD CONDUCTUAL. El conteo se mueve con los dias, en las dos alturas -- la fila y el
		// periodo -- y siempre coincide con la lista de la que sale.
		[Test]
		public void TheCountFollowsTheDaysAtBothLevels()
		{
			ShopStay stays = new ShopStay("1FTFW1ET5DFC10312");

			stays.RecordStay("Taller Curridabat", "Cambio de aceite",
				new DateTime(2026, 6, 10, 8, 30, 0), new DateTime(2026, 6, 12, 16, 45, 0),
				new[] { "2026-06-10", "2026-06-11", "2026-06-12" });

			Assert.That(stays.DaysInTheShopCountBetween("2026-06-01", "2026-06-30"), Is.EqualTo(3));

			stays.RecordStay("Taller Escazu", "Llanta ponchada",
				new DateTime(2026, 6, 20, 7, 0, 0), new DateTime(2026, 6, 20, 11, 0, 0),
				new[] { "2026-06-20" });

			Assert.That(stays.DaysInTheShopCountBetween("2026-06-01", "2026-06-30"), Is.EqualTo(4),
				"si el total viviera en un campo, la segunda estadia no lo habria movido.");

			foreach (AStay row in stays.TheStaysBetween("2026-06-01", "2026-06-30").TheStays())
				Assert.That(((AClosedStay)row).HowManyDays(),
					Is.EqualTo(row.TheDaysItCovered().Length),
					row.Branch + ": el conteo de la fila sale de sus dias.");
		}

		// Y NADA DE LO QUE SOBREVIVE SE LE PUEDE ESCRIBIR. Un setter seria la puerta por la que un
		// conteo se vuelve estado sin llamarse conteo.
		[Test]
		public void NothingThatSurvivesBetweenTwoReadsCanBeSet()
		{
			List<string> culpables = new List<string>();

			foreach (Type tipo in new[]
				{ typeof(ShopStay), typeof(AStay), typeof(AnOpenStay), typeof(AClosedStay),
				  typeof(StayDay), typeof(StaySubject) })
			{
				culpables.AddRange(tipo
					.GetProperties(Declarados)
					.Where(propiedad => propiedad.GetSetMethod(true) != null)
					.Select(propiedad => tipo.Name + "." + propiedad.Name));
			}

			Assert.That(culpables, Is.Empty,
				"Se calculan y se dicen; no se les escribe: " + string.Join(", ", culpables));
		}

		private static List<string> ElCensoDe(Type tipo)
		{
			return tipo.GetFields(Declarados)
				.Select(campo => campo.Name + " : " + ComoSeDiceElTipo(campo.FieldType))
				.OrderBy(dicho => dicho, StringComparer.Ordinal)
				.ToList();
		}

		// Un List<AStay> se dice "List`1" si uno se queda con Name, y el censo tiene que ser
		// legible: el tipo del elemento es la mitad que importa.
		private static string ComoSeDiceElTipo(Type tipo)
		{
			if (!tipo.IsGenericType) return tipo.Name;

			string sinAridad = tipo.Name.Substring(0, tipo.Name.IndexOf('`'));

			return sinAridad + "<"
				+ string.Join(", ", tipo.GetGenericArguments().Select(ComoSeDiceElTipo)) + ">";
		}
	}
}
