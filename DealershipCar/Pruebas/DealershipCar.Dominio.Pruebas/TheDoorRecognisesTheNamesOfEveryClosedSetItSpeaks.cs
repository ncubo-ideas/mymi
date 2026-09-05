using System;
using System.Collections.Generic;
using System.IO;
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
	//
	// QUE CUBRE ESTE FIXTURE Y QUE NO, RE-DERIVADO EN CL-1 LEYENDO WireName MIEMBRO POR MIEMBRO.
	// WireName declara ONCE resolutores y NUEVE reconocedores, o sea NUEVE PARES; los cuatro
	// [TestCase] de aca abajo nombran CINCO de esos nueve. Los otros CUATRO pares existen y este
	// barrido no los toca: OperationKind, IntervalBasis, DistanceUnit y ReadingSource. Son
	// conjuntos de otras rebanadas, asi que CL-1 los REPORTA y no los arregla -- pero queda escrito
	// aca, que es donde el proximo lector va a mirar, porque el nombre de esta clase dice "EVERY" y
	// para esos cuatro es falso desde antes de CL-1.
	//
	// CL-1 AGREGA Currency, Y NO COMO UN [TestCase] MAS. Los cuatro metodos de abajo reciben un
	// TIPO ENUM y recorren Enum.GetNames sobre el; Currency no tiene enum de frontera, por decision
	// de §14 G1 y por lo que C52 midio, asi que un caso suyo necesitaria una forma distinta en los
	// cuatro. Va como un metodo aparte, en ESTE fixture, porque dejarlo fuera volveria falso el
	// nombre de la clase para un sexto conjunto.
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

		// EL SEXTO CONJUNTO, y el unico que se alcanza por un CODIGO EN TEXTO en vez de por un enum.
		// Las cuatro leyes de arriba son las mismas; lo que cambia es de que se entera la puerta.
		//
		// LA DIFERENCIA ES TODA LA DECISION DE §0 CONSEQUENCE 2: los otros cinco resolutores miran
		// un Enum.IsDefined, y este LE PREGUNTA AL DOMINIO -- Currency.IsNamed -- porque un conjunto
		// cerrado alcanzado por texto no tiene enum que mirar. Es tambien por eso que estos dos
		// miembros viven en DealershipCar.Frontera y no en la Superficie: para preguntarle al dominio
		// hay que nombrar un tipo del dominio, y Frontera es el unico ensamblado de produccion que
		// puede.
		//
		// SE FALSIFICA DESDE EL OTRO LADO: si Currency.IsNamed dejara de reconocer un codigo, este
		// test se pone rojo POR LA PUERTA, que es lo que prueba que la puerta no tiene una segunda
		// copia del conjunto.
		[Test]
		public void TheCurrencyIsRecognisedByAskingTheDomainAndNotAnEnum()
		{
			foreach (string code in new[] { "CRC", "USD" })
			{
				Assert.That(Resolve("Currency", code), Is.EqualTo(code),
					"El resolutor devuelve el codigo tal cual: es lo que viaja al script y lo que "
					+ "queda escrito en el journal.");
				Assert.That(Recognise("NamesACurrency", code), Is.True,
					$"'{code}' es una de las que el dominio nombra y el reconocedor no la conoce. "
					+ "Los dos tienen que estar mirando el MISMO conjunto.");
			}

			Exception refused = Assert.Catch(() => Resolve("Currency", "XYZ"));

			Assert.That(refused, Is.InstanceOf<ArgumentException>(),
				"Afuera del conjunto no hay moneda que resolver, asi que es un defecto de quien "
				+ "llama y revienta antes de performar.");
			Assert.That(refused.Message, Does.Contain("CRC").And.Contain("USD"),
				"Y el mensaje dice contra que miro. La lista la da el dominio, en un solo sitio, "
				+ "asi que no puede despegarse del conjunto.");

			Assert.That(Recognise("NamesACurrency", "XYZ"), Is.False,
				"El reconocedor contesta que no. Reventar aca haria que un payload ajeno tumbara la "
				+ "lectura del feed en vez de descartarse.");
			Assert.That(Recognise("NamesACurrency", "   "), Is.False);
			Assert.That(Recognise("NamesACurrency", null), Is.False);

			Assert.That(Recognise("NamesACurrency", "crc"), Is.False,
				"La caja importa, y la puerta es el unico sitio donde se puede insistir en ella.");
			Assert.That(
				Assert.Catch(() => Resolve("Currency", "crc")), Is.InstanceOf<ArgumentException>());

			Assert.That(Assert.Catch(() => Resolve("Currency", "")), Is.InstanceOf<ArgumentException>());
			Assert.That(
				Assert.Catch(() => Resolve("Currency", "  ")), Is.InstanceOf<ArgumentException>());
			Assert.That(
				Assert.Catch(() => Resolve("Currency", null)), Is.InstanceOf<ArgumentException>());
		}

		// Y LA MITAD QUE EL TEST DE ARRIBA NO PUEDE VER, MEDIDA EN CL-1 EN VEZ DE SUPUESTA. Se
		// falsifico cambiando NamesACurrency por un `code == "CRC" || code == "USD"` escrito a mano:
		// CERO rojos. Una puerta con su propia copia del conjunto coincide con el dominio POR
		// CONSTRUCCION mientras las dos listas digan lo mismo, y eso es E49 -- que dos consumidores
		// concuerden no dice nada del productor. Es tambien exactamente lo que C52 midio para las
		// unidades: apuntar un resolutor al conjunto equivocado dejaba la suite entera en verde.
		//
		// SE AFIRMA SOBRE LA FUENTE, que es la unica forma que hay: un `if` escrito a mano no
		// sobrevive a la compilacion como algo distinguible de una llamada. La fuente de Frontera ya
		// viaja al output como dato, y la lista de codigos NO se escribe aca -- se deriva de los
		// miembros del dominio, o el test tendria la tercera copia del conjunto.
		[Test]
		public void TheDoorKeepsNoCopyOfTheCurrencyCodesAndAsksTheDomainForThem()
		{
			string fuente = File.ReadAllText(Path.Combine(
				AppContext.BaseDirectory, "CodigoComoDato", "Frontera", "WireName.cs"));

			Assert.That(fuente, Does.Contain("Currency.IsNamed"),
				"La puerta tiene que preguntarle al dominio. Es lo unico que impide una segunda copia "
				+ "del conjunto, y una segunda copia se despega sin que nada se ponga rojo.");

			foreach (string codigo in LosCodigosQueElDominioNombra())
				Assert.That(fuente, Does.Not.Contain("\"" + codigo + "\""),
					$"La puerta escribe el codigo '{codigo}' como literal. Los codigos se escriben UNA "
					+ "vez, en el dominio; la puerta pregunta y la lista para el rechazo la pide.");
		}

		private static IEnumerable<string> LosCodigosQueElDominioNombra()
		{
			foreach (FieldInfo campo in typeof(Currency).GetFields(
				BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic
					| BindingFlags.DeclaredOnly))
				if (campo.IsInitOnly && campo.FieldType == typeof(Currency))
					yield return ((Currency)campo.GetValue(null)).Code;
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
			// LA PUERTA VIVE EN SU PROPIO ENSAMBLADO, DealershipCar.Frontera, y no en
			// DealershipCar.App. El namespace sigue siendo DealershipCar.App -- lo que se separo
			// es el ENSAMBLADO, que es lo que mira el InternalsVisibleTo del dominio: la puerta
			// necesita nombrar los conjuntos cerrados y la Superficie no, asi que el permiso es
			// de la puerta sola.
			Type door = Assembly
				.Load(new AssemblyName("DealershipCar.Frontera"))
				.GetType("DealershipCar.App.WireName");

			Assert.That(door, Is.Not.Null,
				"DealershipCar.App.WireName no existe en DealershipCar.Frontera.");

			MethodInfo method = door.GetMethod(
				name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

			Assert.That(method, Is.Not.Null,
				$"WireName.{name} no existe. Los cinco conjuntos que T5 le agrego al wire necesitan "
				+ "los dos: el resolutor para publicar y el reconocedor para la ingesta de T9.");

			return method;
		}
	}
}
