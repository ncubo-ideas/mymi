using System;
using System.Reflection;
using DealershipCar.App;
using NUnit.Framework;

namespace DealershipCar.Dominio.Pruebas
{
	// LOS CINCO CONJUNTOS CERRADOS QUE T5 LE AGREGO AL WIRE, vistos desde el lado de la PUERTA.
	// AWireCodeThatNamesNoMemberIsACallerBug prueba la otra mitad -- los resolutores del DOMINIO,
	// que van de un XxxCode a su miembro. Aca se prueba el paso anterior: de un NOMBRE en texto,
	// que es lo unico que cruza la frontera, a ese XxxCode.
	//
	// LA POLARIDAD ES DISTINTA EN CADA MITAD DE LA PUERTA, y las dos estan bien:
	//
	//   el resolutor (FluidKind, TyrePosition, ...) es TOTAL CON REVENTON. Un nombre que no es de
	//   ningun miembro es un defecto de quien llama, y rechazarlo ACA es lo unico que lo rechaza
	//   sin dejar una entrada permanente en el journal de la persona;
	//
	//   el reconocedor (NamesAFluidKind, ...) CONTESTA. Es para la ingesta, donde un payload que no
	//   nombra nada conocido se descarta en vez de reventar la lectura del feed.
	//
	// SE ENTRA POR REFLEXION, y no por comodidad: DealershipCar.App no declara ningun
	// InternalsVisibleTo, asi que WireName no se puede nombrar desde aca. Los resolutores se
	// ejercitan igual por los once comandos de publicacion; los reconocedores NO tienen ni un
	// llamador hasta T9, y esta es la unica prueba que los toca.
	[TestFixture]
	public sealed class TheDoorRecognisesTheNamesOfEveryClosedSetItSpeaks
	{
		[TestCase("FluidKind", "NamesAFluidKind", typeof(FluidKindCode))]
		[TestCase("TyrePosition", "NamesATyrePosition", typeof(TyrePositionCode))]
		[TestCase("FuelGradeStanding", "NamesAFuelGradeStanding", typeof(FuelGradeStandingCode))]
		[TestCase("PressureUnit", "NamesAPressureUnit", typeof(PressureUnitCode))]
		[TestCase("VolumeUnit", "NamesAVolumeUnit", typeof(VolumeUnitCode))]
		public void EveryDeclaredMemberNameIsResolvedAndRecognised(
			string resolver, string recogniser, Type closedSet)
		{
			foreach (string member in Enum.GetNames(closedSet))
			{
				Assert.That(Resolve(resolver, member), Is.EqualTo(member),
					"El resolutor tiene que devolver el nombre tal cual: es lo que viaja al script "
					+ "y lo que queda escrito en el journal.");
				Assert.That(Recognise(recogniser, member), Is.True,
					$"'{member}' esta declarado en {closedSet.Name} y el reconocedor no lo conoce. "
					+ "Los dos tienen que estar mirando el MISMO conjunto.");
			}
		}

		[TestCase("FluidKind", "NamesAFluidKind", typeof(FluidKindCode))]
		[TestCase("TyrePosition", "NamesATyrePosition", typeof(TyrePositionCode))]
		[TestCase("FuelGradeStanding", "NamesAFuelGradeStanding", typeof(FuelGradeStandingCode))]
		[TestCase("PressureUnit", "NamesAPressureUnit", typeof(PressureUnitCode))]
		[TestCase("VolumeUnit", "NamesAVolumeUnit", typeof(VolumeUnitCode))]
		public void ANameOfNoMemberThrowsInTheResolverAndAnswersFalseInTheRecogniser(
			string resolver, string recogniser, Type closedSet)
		{
			Exception refused = Assert.Catch(() => Resolve(resolver, "NoEsMiembroDeNada"));

			Assert.That(refused, Is.InstanceOf<ArgumentException>(),
				"Afuera del conjunto no hay miembro que resolver, asi que es un defecto de quien "
				+ "llama y revienta antes de performar.");

			// Y EL MENSAJE NOMBRA EL CONJUNTO CONTRA EL QUE MIRO. No es cosmetico: PressureUnitCode
			// y VolumeUnitCode declaran los MISMOS dos nombres, asi que atar un resolutor al
			// conjunto equivocado no cambia lo que acepta y no lo cambiaria hasta que uno de los dos
			// creciera. El mensaje es lo unico que los distingue hoy.
			Assert.That(refused.Message, Does.Contain(closedSet.Name),
				$"El resolutor {resolver} no esta mirando {closedSet.Name}.");

			Assert.That(Recognise(recogniser, "NoEsMiembroDeNada"), Is.False,
				"El reconocedor contesta que no. Reventar aca haria que un payload ajeno tumbara la "
				+ "lectura del feed en vez de descartarse.");

			Assert.That(Recognise(recogniser, "   "), Is.False);
			Assert.That(Recognise(recogniser, null), Is.False);
		}

		// LA CAJA IMPORTA, y la puerta es el UNICO sitio donde se puede insistir en ella: el motor
		// resuelve un nombre de miembro sin mirar mayusculas, asi que 'engineoil' ataria feliz y el
		// journal guardaria esa ortografia para siempre. El canon manda como se escribe.
		[TestCase("FluidKind", "NamesAFluidKind", "engineoil")]
		[TestCase("TyrePosition", "NamesATyrePosition", "FRONT")]
		[TestCase("FuelGradeStanding", "NamesAFuelGradeStanding", "minimum")]
		[TestCase("PressureUnit", "NamesAPressureUnit", "unita")]
		[TestCase("VolumeUnit", "NamesAVolumeUnit", "UNITB")]
		public void AMemberNameInTheWrongCaseIsNotThatMember(
			string resolver, string recogniser, string miscased)
		{
			Assert.That(
				Assert.Catch(() => Resolve(resolver, miscased)), Is.InstanceOf<ArgumentException>());

			Assert.That(Recognise(recogniser, miscased), Is.False);
		}

		// Y un nombre en blanco tampoco nombra nada. Se separa del caso de arriba porque el mensaje
		// que la puerta da es otro: no es "eso no existe", es "no me dijeron cual".
		[TestCase("FluidKind")]
		[TestCase("TyrePosition")]
		[TestCase("FuelGradeStanding")]
		[TestCase("PressureUnit")]
		[TestCase("VolumeUnit")]
		public void ABlankNameIsRefusedByTheResolver(string resolver)
		{
			Assert.That(Assert.Catch(() => Resolve(resolver, "")), Is.InstanceOf<ArgumentException>());
			Assert.That(Assert.Catch(() => Resolve(resolver, "  ")), Is.InstanceOf<ArgumentException>());
			Assert.That(Assert.Catch(() => Resolve(resolver, null)), Is.InstanceOf<ArgumentException>());
		}

		private static string Resolve(string resolver, string value)
		{
			MethodInfo method = TheDoorsNameResolver(resolver);

			object[] arguments = method.GetParameters().Length == 2
				? new object[] { value, "unit" }
				: new object[] { value };

			return (string)Invoke(method, arguments);
		}

		private static bool Recognise(string recogniser, string value)
		{
			return (bool)Invoke(TheDoorsNameResolver(recogniser), new object[] { value });
		}

		// La reflexion envuelve lo que el metodo tire en una TargetInvocationException. Se
		// desenvuelve aca para que las pruebas puedan afirmar sobre la excepcion de verdad y no
		// sobre la caja en que llego.
		private static object Invoke(MethodInfo method, object[] arguments)
		{
			try
			{
				return method.Invoke(null, arguments);
			}
			catch (TargetInvocationException wrapped)
			{
				throw wrapped.InnerException;
			}
		}

		private static MethodInfo TheDoorsNameResolver(string name)
		{
			Type door = typeof(DealershipCarAPI).Assembly.GetType("DealershipCar.App.WireName");

			Assert.That(door, Is.Not.Null, "DealershipCar.App.WireName no existe.");

			MethodInfo method = door.GetMethod(
				name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

			Assert.That(method, Is.Not.Null,
				$"WireName.{name} no existe. Los cinco conjuntos que T5 le agrego al wire necesitan "
				+ "los dos: el resolutor para publicar y el reconocedor para la ingesta de T9.");

			return method;
		}
	}
}
