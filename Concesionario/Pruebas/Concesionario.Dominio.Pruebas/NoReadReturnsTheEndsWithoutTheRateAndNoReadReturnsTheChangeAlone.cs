using System;
using System.Collections.Generic;
using System.Reflection;
using Concesionario.App;
using NUnit.Framework;

namespace Concesionario.Dominio.Pruebas
{
	// LA LEY CENTRAL DEL SLICE, EN EL BORDE DONDE SE MUERE EN SILENCIO. El dominio recorre la serie
	// entera y la lectura devuelve LOS DOS EXTREMOS Y EL RITMO EN UNA SOLA RESPUESTA. El atajo es una
	// lectura que entregue los extremos y deje que la pantalla divida, y esa lectura se ve exactamente
	// igual de razonable que las seis que si estan.
	//
	// POR QUE ESTE FIXTURE EXISTE APARTE DEL CENSO NUMERICO. La mitad-ley
	// AHalfOfAComposedAnswerNeverLeavesTheDoorAsANumber se pondria roja por una lectura que entregara
	// un extremo COMO NUMERO -- pero solo si alguien pensara en censarla, porque su conjunto exacto se
	// amplia escribiendo el nombre nuevo adentro. Y no se pondria roja en absoluto por una lectura que
	// entregara los dos extremos como TEXTO, ni por una que entregara el porcentaje adentro de una
	// frase que no dijera los dos periodos. Este fixture es la mitad que falta.
	//
	// SE AFIRMA POR TRES MITADES Y HACEN FALTA LAS TRES:
	//
	//   POR NOMBRE: ningun miembro de la puerta se llama por un extremo suelto, por el cambio suelto,
	//   ni por ninguna de las palabras que §4 del brief prohibe. Por ENUMERACION contra la reflexion.
	//
	//   POR TIPO DE RETORNO: las SEIS lecturas de la serie contestan exactamente lo que §9 dice, y una
	//   sola de las seis contesta con un NUMERO -- una cuenta de filas, que es una respuesta entera.
	//   Un extremo suelto tendria que entrar por ahi y el mapa exacto lo nombra.
	//
	//   POR ALCANCE: los tres miembros que SI dan el ritmo como numero -- DistancePerDay,
	//   TheShapeOfItsChangeAgainst y TheChangeAgainstAsSaid -- viven en RateAnswer, que la puerta no
	//   puede nombrar NI ALCANZAR, porque
	//   ningun miembro expuesto de MaintenanceSchedule devuelve un portador de esta serie. La puerta
	//   solo puede escribir 'schedule.X(...)': si schedule no tiene por donde, no hay script que
	//   llegue.
	//
	// LA MITAD POR ALCANCE ES LA QUE CIERRA EL HUECO QUE OD-0 MIDIO. NoOdometerCarrierCrossesTheFacade
	// y OnlyOneMemberResolvesAReadingFromTheTimeline estan tipadas sobre el ESCALAR -- UsableReading y
	// OdometerReading -- asi que una List<ASeriesRow> se les cuela, y las dos estan acotadas a
	// MaintenanceSchedule, asi que un portador que llegara a la puerta por un tipo NUEVO tampoco se
	// ve. Las dos mitades de abajo son esas dos, y la segunda se afirma por ENSAMBLADO y no por una
	// lista de sustantivos (C57): lo que se enumera son los sitios por donde la regla puede romperse.
	[TestFixture]
	public sealed class NoReadReturnsTheEndsWithoutTheRateAndNoReadReturnsTheChangeAlone
	{
		// LAS SEIS LECTURAS DE LA SERIE Y LO QUE CADA UNA CONTESTA, como mapa exacto. Un extremo
		// suelto o un porcentaje suelto tendrian que aparecer aca, y aparecer aca es escribirlo.
		private static readonly Dictionary<string, Type> TheSixReadsOfTheSeries =
			new Dictionary<string, Type>
			{
				{ "CanARateOfUseBeSaid", typeof(bool) },
				{ "ReadRateOfUseStanding", typeof(string) },
				{ "DescribeRateOfUse", typeof(string) },
				{ "DescribeTwoPeriodsOfUseCompared", typeof(string) },
				{ "ReadingsBetween", typeof(OdometerSeriesRecord) },
				{ "ReadingCountBetween", typeof(int) },
			};

		// LO QUE UN EXTREMO SUELTO O UN CAMBIO SUELTO SE LLAMARIA, EN CUALQUIER GRAFIA. Los cuatro
		// primeros son los extremos y lo que se compone con ellos; los demas son la lista de §4 del
		// brief, que prohibe esas palabras como nombre de miembro en cualquier parte.
		private static readonly string[] TheNamesTheDoorMayNotCarry =
		{
			"PerDay", "DistanceTravelledBetween", "SpanStart", "SpanEnd",
			"FirstReadingOf", "LastReadingOf", "ReadingValueAt",
			"Delta", "PercentChange", "HowItCompares",
			"Average", "Mean", "Trend", "Slope", "Velocity", "Projection", "Forecast", "Estimate",
		};

		// LOS PORTADORES DE ESTA SERIE. Ninguno cruza la fachada de la agenda, ni suelto ni adentro de
		// una coleccion: lo que sale son escalares y columnas de escalares.
		private static readonly Type[] TheCarriersOfTheSeries =
		{
			typeof(ASpanOfUse), typeof(ASeriesRow), typeof(RateAnswer), typeof(AMeasuredRate),
			typeof(RateStanding), typeof(ATwoSpanComparison),
		};

		// ===== POR NOMBRE ================================================================

		[Test]
		public void NoMemberOfTheDoorIsNamedForALooseEndOrALooseChange()
		{
			var offending = new List<string>();

			foreach (MethodInfo each in TheDoorsMembers())
				foreach (string banned in TheNamesTheDoorMayNotCarry)
					if (each.Name.Contains(banned, StringComparison.OrdinalIgnoreCase))
						offending.Add(each.Name);

			Assert.That(offending, Is.Empty,
				"La puerta gano un miembro llamado por un extremo suelto, por el cambio suelto, o por "
				+ "una de las palabras que el canon prohibe. Lo que sale es el RITMO CON SU TRAMO "
				+ "DICHO, y las filas del periodo aparte: ninguna de las dos es la mitad de la otra.");
		}

		// Y NINGUNA COLUMNA DEL REGISTRO DE LA SERIE TAMPOCO. Una columna es texto y el censo numerico
		// no la puede ver, asi que un extremo suelto se colaria por ahi sin que nada lo notara.
		[Test]
		public void NoColumnOfTheSeriesRecordIsNamedForALooseEndEither()
		{
			foreach (PropertyInfo column in typeof(OdometerSeriesRecord).GetProperties(
				BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
				foreach (string banned in TheNamesTheDoorMayNotCarry)
					Assert.That(
						column.Name.Contains(banned, StringComparison.OrdinalIgnoreCase), Is.False,
						$"La columna '{column.Name}' del registro de la serie lleva un extremo suelto o "
						+ "un cambio. Una fila es COMPLETA: valor, unidad, instante y fuente.");
		}

		// ===== POR TIPO DE RETORNO =======================================================

		[Test]
		public void TheSixReadsOfTheSeriesAnswerExactlyTheseTypesAndOnlyOneOfThemAnswersWithANumber()
		{
			foreach (KeyValuePair<string, Type> each in TheSixReadsOfTheSeries)
				Assert.That(TheDoorsMember(each.Key).ReturnType, Is.EqualTo(each.Value),
					"'" + each.Key + "' dejo de contestar lo que §9 dice que contesta.");

			var answeringWithANumber = new List<string>();

			foreach (KeyValuePair<string, Type> each in TheSixReadsOfTheSeries)
				if (each.Value == typeof(int) || each.Value == typeof(long))
					answeringWithANumber.Add(each.Key);

			Assert.That(answeringWithANumber, Is.EqualTo(new[] { "ReadingCountBetween" }),
				"Una sola de las seis contesta con un NUMERO, y es una CUENTA DE FILAS: una cuenta es "
				+ "una respuesta entera, no le falta ni unidad ni tramo ni instante. Cualquier otra de "
				+ "las seis que empezara a contestar con un numero seria un extremo, una distancia, un "
				+ "tiempo o un porcentaje -- las cuatro son mitades.");
		}

		// Y LAS OTRAS CINCO CONTESTAN CON UNA FRASE, UN NOMBRE, UN SI O NO, O EL REGISTRO ENTERO. Sin
		// este renglon, el de arriba se cumpliria igual si una de las cinco pasara a devolver un
		// int[] de extremos: no seria un numero y el mapa exacto seguiria diciendo lo que dice.
		[Test]
		public void NoneOfTheSixAnswersWithABareCollectionOfNumbers()
		{
			foreach (KeyValuePair<string, Type> each in TheSixReadsOfTheSeries)
			{
				Type answered = TheDoorsMember(each.Key).ReturnType;

				Assert.That(answered, Is.Not.EqualTo(typeof(int[])),
					"'" + each.Key + "' contesta una coleccion pelada de numeros. Los valores de la "
					+ "serie salen adentro del REGISTRO, donde cada uno viene con su unidad, su "
					+ "instante y su fuente en la misma fila.");
				Assert.That(answered, Is.Not.EqualTo(typeof(long[])));
				Assert.That(answered, Is.Not.EqualTo(typeof(decimal[])));
			}
		}

		// ===== POR ALCANCE: LOS MIEMBROS QUE DAN EL RITMO COMO NUMERO ======================

		// LOS TRES EXISTEN Y SON INTERNAL DE RateAnswer, que es donde tienen que estar: el ritmo se
		// compone adentro. Sin este renglon la prueba de abajo se cumpliria tambien si alguien los
		// borrara, que seria un defecto distinto.
		//
		// ERAN DOS HASTA EL TURNO DEL IDIOMA. HowItComparesTo redactaba la clausula del cambio y se
		// partio en dos -- la SITUACION por su codigo y los DIGITOS del porcentaje --, porque una
		// redaccion no se queda en el dominio. El redondeo sigue viviendo en UN solo miembro.
		[Test]
		public void TheThreeMembersThatDoGiveTheRateAsANumberLiveOnTheAnswerAndAreInternal()
		{
			foreach (string each in new[]
				{ "DistancePerDay", "TheShapeOfItsChangeAgainst", "TheChangeAgainstAsSaid" })
			{
				MethodInfo found = typeof(RateAnswer).GetMethod(
					each, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

				Assert.That(found, Is.Not.Null,
					"'" + each + "' tenia que seguir existiendo en RateAnswer: el ritmo se compone "
					+ "adentro y esos dos son donde se compone.");
				Assert.That(found.IsAssembly, Is.True,
					"'" + each + "' dejo de ser internal. Publico, cualquier caratula lo llamaria.");
			}
		}

		// Y LA PUERTA NO LOS PUEDE ALCANZAR, y esto es lo que lo prueba: un script de la puerta solo
		// puede escribir 'schedule.X(...)', asi que si NINGUN miembro expuesto de MaintenanceSchedule
		// devuelve un portador de esta serie, no hay expresion que llegue a DistancePerDay.
		//
		// ESTA ES LA MITAD QUE CIERRA EL HUECO DE LA COLECCION. Los dos censos de portadores que ya
		// habia estan tipados sobre el ESCALAR, asi que una List<ASeriesRow> se les cuela; este mira
		// tambien el tipo de ADENTRO de una coleccion.
		[Test]
		public void NoExposedMemberOfTheScheduleHandsOutACarrierOfTheSeriesLooseOrInACollection()
		{
			var offending = new List<string>();

			foreach (MethodInfo each in typeof(MaintenanceSchedule).GetMethods(
				BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public
				| BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
			{
				if (each.IsPrivate) continue;

				foreach (Type carrier in TheCarriersOfTheSeries)
					if (TheTypeOrItsElements(each.ReturnType) == carrier)
						offending.Add(each.Name + " -> " + each.ReturnType.Name);
			}

			Assert.That(offending, Is.Empty,
				"Un miembro EXPUESTO de la agenda entrega un portador de la serie, suelto o adentro de "
				+ "una coleccion. Lo que sale de la agenda son escalares y columnas de escalares; el "
				+ "tramo se arma adentro, en un miembro privado, y nadie mas lo ve. Un portador "
				+ "expuesto le da a un script de la puerta por donde llegar a DistancePerDay.");
		}

		// ===== POR ALCANCE: NINGUN TIPO DEL DOMINIO CRUZA LA PUERTA, EN NINGUNA DIRECCION ==

		// LA OTRA MITAD DEL HUECO: los dos censos de portadores estan acotados a MaintenanceSchedule,
		// asi que un portador que llegara a la puerta por un TIPO NUEVO no lo verian. Esto se afirma
		// por ENSAMBLADO -- ningun miembro declarado de la puerta devuelve ni toma un tipo de
		// Concesionario.Dominio -- que es enumerar los SITIOS y no los sustantivos (C57).
		//
		// HOY ES ESTRUCTURALMENTE IMPOSIBLE, porque desde la cerca del compilador Concesionario.App no
		// puede nombrar un tipo del dominio. Se afirma igual: la cerca es un ProjectReference y una
		// lista de InternalsVisibleTo, y las dos se pueden ampliar sin que nada mas se ponga rojo.
		[Test]
		public void NoDeclaredMemberOfTheDoorReturnsOrTakesATypeFromTheDomainAssembly()
		{
			Assembly domain = typeof(MaintenanceSchedule).Assembly;

			var offending = new List<string>();

			foreach (MethodInfo each in TheDoorsMembers())
			{
				if (TheTypeOrItsElements(each.ReturnType).Assembly == domain)
					offending.Add(each.Name + " devuelve " + each.ReturnType.Name);

				foreach (ParameterInfo parameter in each.GetParameters())
					if (TheTypeOrItsElements(parameter.ParameterType).Assembly == domain)
						offending.Add(
							each.Name + " toma " + parameter.ParameterType.Name + " "
							+ parameter.Name);
			}

			Assert.That(offending, Is.Empty,
				"La puerta gano un miembro que nombra un tipo del dominio. Lo que cruza son primitivos "
				+ "y DTOs de Concesionario.App; un tipo del dominio en la superficie le entrega el "
				+ "portador a cualquier caratula, y de paso deja que la logica se mude para afuera.");
		}

		// ===== Y LO QUE §9 DEJA FUERA DE LA PUERTA A PROPOSITO ============================

		// LOS TRES DE LA LISTA DE "no va en la puerta", afirmados donde un lector los busca. Cada uno
		// tiene su propia prueba en otro lado; lo que este renglon agrega es que los tres siguen
		// valiendo DESPUES de que la serie llego a la puerta, que es el momento en que los tres se
		// vuelven tentadores.
		[Test]
		public void TheResolverStaysPrivateAndTheUnitCodeStaysOffTheDoor()
		{
			MethodInfo resolver = typeof(MaintenanceSchedule).GetMethod(
				"TheLatestUsableReadingAsOf",
				BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
				| BindingFlags.DeclaredOnly);

			Assert.That(resolver, Is.Not.Null);
			Assert.That(resolver.IsPrivate, Is.True,
				"El resolvedor de la lectura vigente dejo de ser privado. Un miembro privado que "
				+ "contesta una pregunta no es una lectura, y ampliarlo entrega el portador.");

			var onTheDoor = new List<string>();

			foreach (MethodInfo each in TheDoorsMembers())
				if (each.Name == "OdometerUnit") onTheDoor.Add(each.Name);

			Assert.That(onTheDoor, Is.Empty,
				"OdometerUnit apareció en la puerta. Devuelve el CODIGO de la unidad, y proyectar un "
				+ "codigo en la puerta deja el mapeo del conjunto cerrado en manos de quien lo "
				+ "muestra. El nombre lo da ReadOdometerUnit, y en el registro lo da la columna Unit.");
		}

		// El tipo, o el de sus elementos si es una coleccion o un arreglo. Es lo que hace que un
		// portador adentro de una List<> se vea, que es justo el hueco que los dos censos viejos
		// tienen.
		private static Type TheTypeOrItsElements(Type declared)
		{
			if (declared.IsArray) return declared.GetElementType();

			if (declared.IsGenericType)
			{
				Type[] arguments = declared.GetGenericArguments();

				if (arguments.Length == 1) return arguments[0];
			}

			return declared;
		}

		private static MethodInfo[] TheDoorsMembers()
		{
			return typeof(DealershipCarAPI).GetMethods(
				BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
		}

		// EL MIEMBRO, Y REVIENTA SI HAY DOS CON ESE NOMBRE. Devolver el primero seria afirmar sobre
		// 'el' miembro eligiendo uno y no mirando el otro.
		private static MethodInfo TheDoorsMember(string name)
		{
			var found = new List<MethodInfo>();

			foreach (MethodInfo each in TheDoorsMembers())
				if (each.Name == name) found.Add(each);

			Assert.That(found, Has.Count.EqualTo(1),
				$"La puerta tiene {found.Count} miembros llamados '{name}'.");

			return found[0];
		}
	}
}
