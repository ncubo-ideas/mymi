using System;
using System.Collections.Generic;
using System.Reflection;
using DealershipCar.App;
using NUnit.Framework;

namespace DealershipCar.Dominio.Pruebas
{
	// SLICE-CostLedger §13 test 15 y §10.3: el resolvedor de la lectura vigente sigue siendo PRIVADO y
	// la distancia NO esta en la puerta como operando de esta respuesta.
	//
	// LOS TRES MIEMBROS DEL TRAMO SON internal Y NO SON OPERACIONES DE LA PUERTA, y eso no es estilo:
	// es un LIMITE. Un script de la puerta solo puede escribir 'schedule.X(...)', asi que un miembro
	// internal sin operacion de puerta que lo llame es alcanzable por el script y por nada mas. Ponerle
	// operacion de puerta a cualquiera de los tres pone rojo el censo de nombres de
	// NoReadReturnsTheEndsWithoutTheRateAndNoReadReturnsTheChangeAlone -- DistanceTravelledBetween esta
	// en su lista de prohibidos por nombre -- y ESO ES EL PUNTO.
	//
	// Y LOS TRES DEVUELVEN ESCALARES. Un portador -- ASpanOfUse, ASeriesRow, RateAnswer -- entregado
	// por cualquiera de los tres le daria a un script por donde llegar a DistancePerDay, que es lo que
	// NoExposedMemberOfTheScheduleHandsOutACarrierOfTheSeriesLooseOrInACollection prohibe. Aca se
	// afirma de los tres por nombre, donde un lector de este cruce los busca.
	//
	// LA UNIDAD SI CRUZA COMO CODIGO Y ESA ES LA EXCEPCION QUE DEC-24 YA PAGO: una ranura de argumento
	// entre globales es una ranura de expresion entera, asi que un enum cruzado por ahi llega TIPADO --
	// al reves de la frontera de @param, donde viaja el NOMBRE del miembro. Por eso hay DOS miembros
	// para un mismo hecho, y el que proyecta el nombre es el que sale a una vista.
	[TestFixture]
	public sealed class TheLatestUsableReadingAsOfIsStillPrivateAndTheDistanceIsNotOnTheDoor
	{
		// LOS TRES MIEMBROS QUE SOLO UN SCRIPT ALCANZA, con lo que cada uno contesta.
		private static readonly Dictionary<string, Type> TheThreeTheScriptReaches =
			new Dictionary<string, Type>
			{
				{ "FirstReadingInstantBetween", typeof(DateTime) },
				{ "DistanceTravelledBetween", typeof(int) },
				{ "UnitOfTravelBetween", typeof(DistanceUnitCode) },
			};

		// LOS PORTADORES DE LA SERIE. Ninguno de los tres entrega uno, ni suelto ni en una coleccion.
		private static readonly Type[] TheCarriersOfTheSeries =
		{
			typeof(ASpanOfUse), typeof(ASeriesRow), typeof(RateAnswer), typeof(AMeasuredRate),
			typeof(RateStanding), typeof(ATwoSpanComparison),
		};

		[Test]
		public void TheThreeMembersTheScriptReachesAreInternalAndAnswerExactlyTheseTypes()
		{
			foreach (KeyValuePair<string, Type> each in TheThreeTheScriptReaches)
			{
				MethodInfo found = typeof(MaintenanceSchedule).GetMethod(
					each.Key,
					BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
					| BindingFlags.DeclaredOnly);

				Assert.That(found, Is.Not.Null,
					"'" + each.Key + "' tenia que existir en la agenda: es lo que el script del cruce "
					+ "llama, y un script que nombra un miembro que no esta revienta al ejecutarse.");
				Assert.That(found.IsAssembly, Is.True,
					"'" + each.Key + "' dejo de ser internal. El DSL llama publicos e internals; "
					+ "publico, cualquier caratula lo llamaria tambien.");
				Assert.That(found.ReturnType, Is.EqualTo(each.Value),
					"'" + each.Key + "' dejo de contestar lo que el script le pasa al libro.");
			}
		}

		// Y NINGUNO DE LOS TRES ES UNA OPERACION DE LA PUERTA. Es el limite entero: internal mas sin
		// operacion de puerta es lo que los deja alcanzables por el script y por nada mas.
		[Test]
		public void AndNotOneOfTheThreeIsADoorOperation()
		{
			var onTheDoor = new List<string>();

			foreach (MethodInfo each in TheDoorsMembers())
				if (TheThreeTheScriptReaches.ContainsKey(each.Name)) onTheDoor.Add(each.Name);

			Assert.That(onTheDoor, Is.Empty,
				"Aparecio en la puerta: " + string.Join(", ", onTheDoor) + ". La distancia NO sale de "
				+ "la puerta como operando de esta respuesta: sale DENTRO de la frase, con su unidad y "
				+ "el ancla al lado. Quien tenga la distancia y una cifra de dinero arma un costo por "
				+ "kilometro que el dominio nunca dijo.");
		}

		// Y NINGUNO ENTREGA UN PORTADOR DE LA SERIE, que es por donde un script llegaria a
		// DistancePerDay. Los tres contestan escalares.
		[Test]
		public void AndNotOneOfTheThreeHandsOutACarrierOfTheSeries()
		{
			foreach (string name in TheThreeTheScriptReaches.Keys)
			{
				MethodInfo found = typeof(MaintenanceSchedule).GetMethod(
					name,
					BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
					| BindingFlags.DeclaredOnly);

				foreach (Type carrier in TheCarriersOfTheSeries)
					Assert.That(TheTypeOrItsElements(found.ReturnType), Is.Not.EqualTo(carrier),
						"'" + name + "' entrega " + carrier.Name + ". Lo que sale de la agenda son "
						+ "escalares; el tramo se arma adentro, en un miembro privado.");
			}
		}

		// EL RESOLVEDOR SIGUE PRIVADO -- DEC-25 -- Y AHORA HAY UN SEGUNDO MOTIVO PARA MIRARLO. Este
		// cruce le agrega tres lecturas nuevas al tramo, y ampliarlo seria la manera corta de darles el
		// ancla: entregaria el portador UsableReading fuera del agregado.
		[Test]
		public void AndTheResolverOfTheReadingInForceIsStillPrivate()
		{
			MethodInfo resolver = typeof(MaintenanceSchedule).GetMethod(
				"TheLatestUsableReadingAsOf",
				BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
				| BindingFlags.DeclaredOnly);

			Assert.That(resolver, Is.Not.Null);
			Assert.That(resolver.IsPrivate, Is.True,
				"El resolvedor de la lectura vigente dejo de ser privado. Un miembro privado que "
				+ "contesta una pregunta no es una lectura (DEC-25).");

			MethodInfo span = typeof(MaintenanceSchedule).GetMethod(
				"TheSpanOfUseBetween",
				BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
				| BindingFlags.DeclaredOnly);

			Assert.That(span, Is.Not.Null);
			Assert.That(span.IsPrivate, Is.True,
				"Y el compositor del tramo tambien: es el UNICO sitio donde un periodo se resuelve en "
				+ "lecturas, y los tres miembros del cruce lo envuelven en vez de repetirlo (E49).");
		}

		// Y LAS DOS LECTURAS DEL CRUCE CONTESTAN UN SI O NO Y UNA FRASE, Y NADA MAS. Sin este renglon,
		// el censo numerico de la puerta se cumpliria igual el dia que una de las dos empezara a
		// contestar un decimal: la cifra por unidad seria un OPERANDO y §10.3 la prohibe por nombre.
		[Test]
		public void AndTheTwoCrossingReadsOfTheDoorAnswerATruthAndASentenceAndNothingElse()
		{
			Assert.That(
				TheDoorsMember("CanCostPerDistanceBeSaid").ReturnType, Is.EqualTo(typeof(bool)));
			Assert.That(
				TheDoorsMember("DescribeCostPerDistanceBetween").ReturnType,
				Is.EqualTo(typeof(string)));

			var offending = new List<string>();

			foreach (MethodInfo each in TheDoorsMembers())
			{
				if (!each.Name.Contains("PerDistance", StringComparison.Ordinal)) continue;

				if (each.ReturnType != typeof(bool) && each.ReturnType != typeof(string))
					offending.Add(each.Name + " -> " + each.ReturnType.Name);
			}

			Assert.That(offending, Is.Empty,
				"Una lectura del costo por distancia contesta algo que no es un si o no ni una frase: "
				+ string.Join(", ", offending) + ".");
		}

		// Y NINGUNA DE LAS DOS PIDE UNA MEDICION. Los dos extremos son claves de catalogo -- eligen el
		// periodo y la respuesta los nombra -- y nada mas cruza: ni el odometro, ni la distancia, ni la
		// unidad. Es la primera invencion prohibida vista desde el cruce.
		[Test]
		public void AndNeitherCrossingReadAsksTheCallerForAFactAboutTheVehicle()
		{
			foreach (string name in new[] { "CanCostPerDistanceBeSaid", "DescribeCostPerDistanceBetween" })
			{
				ParameterInfo[] slots = TheDoorsMember(name).GetParameters();

				Assert.That(slots, Has.Length.EqualTo(2),
					"'" + name + "' gano un parametro. El unico hecho que le falta a esta respuesta es "
					+ "el tramo de uso, y el registro ya lo tiene: pedirlo seria la primera invencion "
					+ "prohibida con un esquema encima.");

				foreach (ParameterInfo slot in slots)
				{
					Assert.That(slot.ParameterType, Is.EqualTo(typeof(DateTime)));
					Assert.That(new[] { "from", "to" }, Does.Contain(slot.Name));
				}
			}
		}

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
				"La puerta declara " + found.Count + " miembros llamados '" + name + "'.");

			return found[0];
		}
	}
}
