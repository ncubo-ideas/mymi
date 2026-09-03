using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace DealershipCar.Dominio.Pruebas
{
	// LEY L1 del brief, EveryAnswerNamesItsRevision: "toda respuesta lleva la PublishedRevision de
	// la que salio. Una respuesta sin procedencia es un rumor, y una revision no puede cambiar en
	// silencio lo que a alguien le dijeron el mes pasado."
	//
	// L1 ES LA LEY QUE SOSTIENE TODA LA TOPOLOGIA (§2 del brief, G1): la copia que vive en el
	// journal de este vehiculo se queda vieja cuando el fabricante publica. Eso es aceptable
	// UNICAMENTE porque cada respuesta dice de que revision salio, asi que una respuesta atrasada
	// se ve atrasada. Si alguna respuesta sale sin revision, el argumento entero se cae y el
	// modulo es un PDF con peor tipografia.
	//
	// POR ESO SE MECANIZA EN VEZ DE REVISARSE. Una ley con prueba es una ley; una ley con parrafo
	// es una esperanza. Hay dos mitades y las dos son por reflexion:
	//
	//   -- CADA FRASE TIENE SU LECTURA DE PROCEDENCIA. Se buscan por reflexion todos los
	//      Describe* de la fachada y se exige que cada uno este emparejado en la tabla de abajo
	//      con su Read*Revision, y que el par exista de verdad. Un Describe* nuevo en T3 o T4 pone
	//      rojo esto hasta que alguien diga cual es su procedencia. Se empareja por TABLA y no por
	//      regla de nombre porque §4.2 del plan ya tiene dos pares cuyos nombres no calzan
	//      (DescribeCoverageSummary y DescribeServicingElsewhereRule), y una regla que se va a
	//      tener que romper en dos casos no es una regla.
	//
	//   -- NINGUNA RESPUESTA PUBLICADA SE PUEDE CONSTRUIR SIN REVISION. La unica variante de
	//      SpecificationAnswer a la que se llega sin una PublishedRevision es la ausencia, y hay
	//      exactamente una (D6). Si manana apareciera una segunda forma de fabricar una respuesta
	//      publicada sin procedencia, esto lo dice.
	[TestFixture]
	public sealed class EveryAnswerNamesItsRevision
	{
		private const string TheRevision = "REV-2025-01";
		private const string TheClause = "Aceite de motor: grado 5W-30";

		private static readonly DateTime PublishedAt = new DateTime(2025, 3, 14, 10, 0, 0);

		// Cada frase de la fachada con la lectura que dice de que revision salio. Crece con cada
		// item publicado que entra en T3 y T4: primero se decide cual es su procedencia, despues
		// se agrega la fila.
		//
		// EN T3 ENTRARON CINCO, y dos de ellas son justo las que la tabla existe para poder
		// emparejar: DescribeServicingElsewhereRule sale de ReadServicingPolicyRevision, y
		// DescribeServicePrice de ReadServicePriceRevision -- ninguna regla de nombre habria
		// llegado a la primera.
		//
		// EN T4 ENTRARON DOS FRASES QUE COMPARTEN UNA SOLA PROCEDENCIA, que es el caso que
		// termina de justificar la tabla: DescribeCoverageSummary y DescribeCoverage salen las
		// dos de ReadCoverageRevision, porque el alcance de garantia es UN item publicado y las
		// dos frases lo leen -- el resumen entero y la situacion de un sistema contra el. Si las
		// dos nombraran revisiones distintas, una de las dos estaria mintiendo.
		private static readonly string[][] CadaFraseConSuProcedencia =
		{
			new[] { "DescribeFluid", "ReadFluidRevision" },
			new[] { "DescribeTyre", "ReadTyreRevision" },
			new[] { "DescribeFuel", "ReadFuelRevision" },
			new[] { "DescribeServiceInterval", "ReadServiceIntervalRevision" },
			new[] { "DescribeServicePrice", "ReadServicePriceRevision" },
			new[] { "DescribeServicingElsewhereRule", "ReadServicingPolicyRevision" },
			new[] { "DescribeCoverageSummary", "ReadCoverageRevision" },
			new[] { "DescribeCoverage", "ReadCoverageRevision" },
			new[] { "DescribePublishedServiceInterval", "ReadServiceIntervalRevision" },
			new[] { "DescribePublishedServicingElsewhereRule", "ReadServicingPolicyRevision" },
			new[] { "DescribePublishedServicePrice", "ReadServicePriceRevision" },
			new[] { "DescribeEachPublishedPriceAsOf", "PublishedPriceRevisionsAsOf" },
		};

		// LAS DOS QUE ENTRARON CON LA LEY L7b son el mismo caso que las dos de T4 y por eso la
		// tabla ya sabia contestarlas: la mitad publicada y la emparejada son DOS FRASES DEL MISMO
		// ITEM, asi que salen de UNA sola procedencia -- la que ya estaba declarada. Nombrar
		// revisiones distintas seria una de las dos mintiendo, que es literalmente el argumento con
		// que DescribeCoverageSummary y DescribeCoverage comparten ReadCoverageRevision.
		//
		// Y notese lo que esto dice de la ley: la procedencia NUNCA estuvo fundida. Solo la frase
		// lo estaba. Las lecturas de presencia y de procedencia ya contestaban sin un solo hecho
		// del vehiculo -- las dos pasan por la sobrecarga sin parametros recibidos -- asi que lo
		// que L7b agrego a la superficie fue la tercera lectura del trio, no un trio nuevo.
		//
		// EN MS-3 ENTRARON DOS, Y LA SEGUNDA ES UNA FORMA NUEVA PARA ESTA TABLA. DescribePublishedServicePrice
		// es el caso ya conocido -- la mitad publicada de un item comparte la procedencia de la
		// compuesta, igual que las dos de L7b --, pero DescribeEachPublishedPriceAsOf es la primera
		// frase de esta fachada que contesta una COLUMNA. Su procedencia es la columna PARALELA
		// PublishedPriceRevisionsAsOf, que empareja fila por fila en vez de llamada por llamada: las
		// dos abren con la misma enumeracion, asi que la fila N de una y la fila N de la otra hablan
		// del mismo precio por construccion. La ley no cambia -- una respuesta sin revision sigue
		// siendo un rumor --; lo que crecio es la FORMA en que se cumple, y por eso DevuelveTexto
		// acepta ahora tambien una columna de frases.
		//
		// La otra mitad de esa procedencia sigue estando DENTRO de cada frase: la columna Says trae
		// "Publicado en la revision X." al final, como toda respuesta publicada de este agregado.

		[Test]
		public void EveryDescribeReadOfTheFacadeIsPairedWithARevisionRead()
		{
			List<string> frases = LasFrasesDeLaFachada();

			List<string> sinProcedencia = frases
				.Where(frase => !CadaFraseConSuProcedencia.Any(par => par[0] == frase))
				.ToList();

			Assert.That(sinProcedencia, Is.Empty,
				"Frases sin lectura de procedencia declarada: " + string.Join(", ", sinProcedencia)
				+ ". Ley L1: una respuesta sin revision es un rumor. Antes de agregar la fila hay "
				+ "que decidir de que revision sale esa frase -- que es la pregunta que la ley "
				+ "hace.");
		}

		[Test]
		public void BothHalvesOfEveryPairExistOnTheFacadeAndAnswerWithText()
		{
			foreach (string[] par in CadaFraseConSuProcedencia)
			{
				Assert.That(DevuelveTexto(par[0]), Is.True,
					$"La fachada no expone '{par[0]}' devolviendo texto.");
				Assert.That(DevuelveTexto(par[1]), Is.True,
					$"La fachada declara la frase '{par[0]}' pero no expone su procedencia "
					+ $"'{par[1]}'. La pareja es la ley: la frase sola no se puede auditar.");
			}
		}

		// La ausencia es la UNICA variante a la que se llega sin revision, y hay exactamente una
		// (D6). Todo lo demas que sea una respuesta pide su procedencia en el constructor.
		[Test]
		public void TheOnlyAnswerReachableWithoutARevisionIsTheSingleKindOfAbsence()
		{
			List<Type> variantes = Assembly.Load(new AssemblyName("DealershipCar.Dominio"))
				.GetTypes()
				.Where(tipo => tipo != typeof(SpecificationAnswer)
					&& typeof(SpecificationAnswer).IsAssignableFrom(tipo))
				.ToList();

			Assert.That(variantes, Is.Not.Empty, "SpecificationAnswer no tiene ni una variante.");

			List<string> sinProcedencia = variantes
				.Where(variante => !PideUnaRevisionAlConstruirse(variante))
				.Select(variante => variante.Name)
				.OrderBy(nombre => nombre, StringComparer.Ordinal)
				.ToList();

			Assert.That(sinProcedencia, Is.EqualTo(new[] { "AnUnpublishedSpecification" }),
				"Variantes que se construyen sin nombrar una revision: "
				+ string.Join(", ", sinProcedencia) + ". Solo la ausencia puede, y solo hay una "
				+ "clase de ausencia en este modulo (D6). Cualquier otra seria una respuesta sin "
				+ "procedencia, que es lo que L1 existe para que no exista.");
		}

		// Y la ausencia efectivamente contesta que no esta publicada: si la unica variante sin
		// revision dijera que si, la prueba de arriba estaria mirando el tipo equivocado.
		[Test]
		public void TheOneAnswerReachableWithoutARevisionSaysItIsNotPublished()
		{
			ModelSpecification specification = new ModelSpecification("SM-SEDAN-2025");

			SpecificationAnswer absent = specification.UnpublishedIsAnAnswer("EngineOil");

			Assert.That(absent.IsPublished(), Is.False);
			Assert.That(absent.RevisionCode(), Is.EqualTo(""));
			Assert.That(absent.Describe(), Is.Not.Empty);
		}

		[Test]
		public void APublishedAnswerCarriesTheRevisionCodeAndSaysItInTheSentence()
		{
			SpecificationAnswer answer = SpecificationAnswer.EveryAnswerNamesItsRevision(
				ASealedRevision(), TheClause);

			Assert.That(answer.IsPublished(), Is.True);
			Assert.That(answer.RevisionCode(), Is.EqualTo(TheRevision));
			Assert.That(answer.Describe(), Does.Contain(TheClause));
			Assert.That(answer.Describe(), Does.Contain(TheRevision),
				"La procedencia va en la FRASE y no solo en la lectura aparte: quien lea la "
				+ "respuesta y no pregunte por la revision igual tiene que verla.");
		}

		// La fabrica esta GUARDADA, y esa es la forma que toma L1 en codigo: no hay camino a una
		// respuesta publicada que no pase por nombrar una revision.
		[Test]
		public void AnAnswerCannotBeMintedWithoutARevision()
		{
			Assert.Throws<DomainException>(
				() => SpecificationAnswer.EveryAnswerNamesItsRevision(null, TheClause));
		}

		// Ni con una clausula vacia: una respuesta publicada que no dice nada tiene procedencia y
		// ningun contenido, que es peor que no tener respuesta -- viene firmada.
		[Test]
		public void AnAnswerCannotBeMintedWithNothingToSay()
		{
			Assert.Throws<DomainException>(
				() => SpecificationAnswer.EveryAnswerNamesItsRevision(ASealedRevision(), "   "));
		}

		// La respuesta nombra la revision con su CODIGO y no entregando el objeto (E24). Si el
		// objeto cruzara, quien recibe la respuesta podria ramificar sobre el estado de la
		// revision en vez de preguntarle al agregado, y el gate de sellado dejaria de ser el
		// unico camino.
		[Test]
		public void NoAnswerHandsOutThePublishedRevisionItself()
		{
			List<Type> tipos = Assembly.Load(new AssemblyName("DealershipCar.Dominio"))
				.GetTypes()
				.Where(tipo => typeof(SpecificationAnswer).IsAssignableFrom(tipo))
				.ToList();

			List<string> entregan = new List<string>();

			foreach (Type tipo in tipos)
				foreach (MemberInfo miembro in tipo.GetMembers(BindingFlags.Instance
					| BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic
					| BindingFlags.DeclaredOnly))
					if (EsExpuesto(miembro) && TipoQueDevuelve(miembro) == typeof(PublishedRevision))
						entregan.Add(tipo.Name + "." + miembro.Name);

			Assert.That(entregan, Is.Empty,
				"Miembros que entregan la revision: " + string.Join(", ", entregan) + ". Sale el "
				+ "CODIGO, que es lo que la respuesta nombra.");
		}

		private static List<string> LasFrasesDeLaFachada()
		{
			return typeof(ModelSpecification)
				.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
					| BindingFlags.DeclaredOnly)
				.Where(metodo => !metodo.IsSpecialName)
				.Where(metodo => metodo.IsPublic || metodo.IsAssembly)
				.Where(metodo => metodo.Name.StartsWith("Describe", StringComparison.Ordinal))
				.Select(metodo => metodo.Name)
				.Distinct()
				.OrderBy(nombre => nombre, StringComparer.Ordinal)
				.ToList();
		}

		// Texto es una frase O una columna de frases. Las dos son respuestas en palabras; la
		// diferencia es cuantas, y la ley de procedencia se cumple igual en las dos -- por una
		// lectura aparte en la primera y por una columna paralela en la segunda.
		private static bool DevuelveTexto(string miembro)
		{
			return typeof(ModelSpecification)
				.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
					| BindingFlags.DeclaredOnly)
				.Any(metodo => metodo.Name == miembro
					&& (metodo.ReturnType == typeof(string) || metodo.ReturnType == typeof(string[]))
					&& (metodo.IsPublic || metodo.IsAssembly));
		}

		private static bool PideUnaRevisionAlConstruirse(Type variante)
		{
			return variante
				.GetConstructors(BindingFlags.Instance | BindingFlags.Public
					| BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
				.All(ctor => ctor.GetParameters()
					.Any(parametro => parametro.ParameterType == typeof(PublishedRevision)));
		}

		private static PublishedRevision ASealedRevision()
		{
			PublishedRevision revision = new PublishedRevision(TheRevision, PublishedAt);
			revision.PublishAFluidSpecification(
				FluidKindCode.EngineOil,
				new FluidSpecification(
					FluidKind.EngineOil, "5W-30", 4.6m, VolumeUnit.UnitA,
					new PublishedInterval(15000, DistanceUnit.UnitA, 365)));
			revision.Seal();

			return revision;
		}

		private static Type TipoQueDevuelve(MemberInfo miembro)
		{
			if (miembro is FieldInfo campo) return campo.FieldType;
			if (miembro is PropertyInfo propiedad) return propiedad.PropertyType;
			if (miembro is MethodInfo metodo) return metodo.ReturnType;
			return null;
		}

		private static bool EsExpuesto(MemberInfo miembro)
		{
			if (miembro is FieldInfo campo) return campo.IsPublic || campo.IsAssembly;
			if (miembro is PropertyInfo propiedad)
			{
				MethodInfo lector = propiedad.GetGetMethod(true);
				return lector != null && (lector.IsPublic || lector.IsAssembly);
			}
			if (miembro is MethodInfo metodo)
				return !metodo.IsSpecialName && (metodo.IsPublic || metodo.IsAssembly);
			if (miembro is ConstructorInfo ctor) return ctor.IsPublic || ctor.IsAssembly;
			return false;
		}
	}
}
