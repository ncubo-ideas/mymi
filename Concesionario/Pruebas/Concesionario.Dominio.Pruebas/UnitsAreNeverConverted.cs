using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace Concesionario.Dominio.Pruebas
{
	// LEY L8 del brief, UnitsAreNeverConverted: "una presion y una capacidad se guardan con su
	// unidad y nunca se normalizan. Dos valores en unidades distintas no son comparables, y la
	// respuesta a una comparacion cruzada es 'no se puede evaluar' -- nunca un numero convertido."
	//
	// ES EL TEST 9 DE §11, Y TIENE DOS MITADES:
	//
	//   -- lo publicado en UnitA se contesta en UnitA, y en ningun sitio aparece una conversion;
	//   -- una comparacion CRUZADA -- un intervalo publicado en una unidad contra una distancia
	//      recibida en la otra -- contesta que NO SE PUEDE EVALUAR, y no dice ningun numero
	//      convertido.
	//
	// LA SEGUNDA MITAD ES LA QUE NECESITABA UNA LECTURA DONDE VIVIR, y por eso el miembro con
	// nombre esta en PublishedInterval: es el unico sitio de esta tarea donde un valor publicado se
	// pone al lado de uno recibido. Sin comparacion no hay nada que suspender, y una ley que nunca
	// se dispara no es una ley.
	//
	// ThePressureAndVolumeUnitsAreNeverConverted protege la AUSENCIA en los tipos de unidad; esta
	// protege la ausencia en los tipos que LLEVAN una unidad -- que son los que de verdad tienen
	// las dos puntas a mano y la tentacion de restarlas.
	[TestFixture]
	public sealed class UnitsAreNeverConverted
	{
		private const string TheModel = "YARIS-2019";
		private const string TheRevision = "REV-2025-01";
		private const string EnsambladoDelDominio = "Concesionario.Dominio";

		private static readonly DateTime PublishedAt = new DateTime(2025, 3, 14, 10, 0, 0);

		// La lista blanca de un intervalo publicado: se arma, se dice, y se dice contra que se
		// puede o no comparar. Nada que convierta.
		private static readonly string[] LoQueUnIntervaloPuedeExponer =
		{
			".ctor",
			// Describe() se fue: el intervalo ya no redacta, entrega sus mitades como forma y
			// valores. Son CUATRO accesores donde habia una frase, y ninguno convierte nada --
			// la unidad sale con su nombre y sin par al que llevarla.
			"TheHalvesItPublished",
			"TheDistanceItPublished",
			"TheDaysItPublished",
			"TheUnitItsDistanceIsIn",
			"UnitsAreNeverConverted",
		};

		// La de un hito: se arma, se dice, y se pregunta si es el mismo -- que es la comparacion
		// con nombre que reemplaza al Equals prohibido (E23) y la que se niega a convertir.
		private static readonly string[] LoQueUnHitoPuedeExponer =
		{
			".ctor",
			"Describe",
			"IsTheSameMilestoneAs",
		};

		private ModelSpecification specification;

		[SetUp]
		public void Setup()
		{
			specification = new ModelSpecification(TheModel);
			specification.OpenRevision(TheRevision, PublishedAt);
		}

		// PRIMERA MITAD DEL TEST 9. Publicada en UnitA, contestada en UnitA, y la otra unidad no
		// aparece por ningun lado.
		[Test]
		public void APressurePublishedInOneUnitIsAnsweredInThatUnit()
		{
			specification.PublishTyreSpecification(
				TheRevision, TyrePositionCode.Front, "205/55R16",
				2.4m, PressureUnitCode.UnitA, "91V");
			specification.SealRevision(TheRevision);

			string answer = TheSentenceTheDoorWouldCompose.TheTyre(
				specification, TyrePositionCode.Front, TheLanguage.OfTheRecord);

			Assert.That(answer, Does.Contain("2,4 bar"));
			Assert.That(answer, Does.Not.Contain("psi"));
		}

		// SEGUNDA MITAD DEL TEST 9, Y LA QUE IMPORTA. El intervalo se publico en UnitA y la
		// distancia que queda llega en UnitB: no se compara, se dice que no se puede, y se nombran
		// las dos unidades para que quien lea sepa por que.
		[Test]
		public void ACrossUnitComparisonAnswersThatItCannotBeEvaluated()
		{
			specification.PublishServiceInterval(
				TheRevision, OperationKindCode.FluidChange, 10000, DistanceUnitCode.UnitA, 365);
			specification.SealRevision(TheRevision);

			string answer = TheSentenceTheDoorWouldCompose.TheServiceInterval(
				specification, OperationKindCode.FluidChange, 8400, DistanceUnitCode.UnitB,
				TheLanguage.OfTheRecord);

			Assert.That(answer, Does.Contain("no se puede evaluar"));
			Assert.That(answer, Does.Contain("publicado en km"));
			Assert.That(answer, Does.Contain("8.400 mi"),
				"Se nombran las DOS unidades. 'No se puede evaluar' sin decir cual contra cual "
				+ "obliga a adivinar donde esta el problema.");
			Assert.That(answer, Does.Contain("nunca se convierten"));
		}

		// Y NO APARECE NINGUN NUMERO CONVERTIDO. Los dos numeros de la frase son los dos que
		// entraron: el publicado y el recibido. Cualquier tercero seria el resultado de una cuenta
		// que esta ley prohibe.
		[Test]
		public void TheCrossUnitAnswerCarriesOnlyTheTwoNumbersThatWereGiven()
		{
			specification.PublishServiceInterval(
				TheRevision, OperationKindCode.FluidChange, 10000, DistanceUnitCode.UnitA, 0);
			specification.SealRevision(TheRevision);

			string answer = TheSentenceTheDoorWouldCompose.TheServiceInterval(
				specification, OperationKindCode.FluidChange, 8400, DistanceUnitCode.UnitB,
				TheLanguage.OfTheRecord);

			Assert.That(LosNumerosDe(SinLaProcedencia(answer)),
				Is.EqualTo(new[] { "10.000", "8.400" }),
				"Los numeros de la respuesta son exactamente el publicado y el recibido, en ese "
				+ "orden. Un tercero es una conversion, la reste quien la reste. La procedencia se "
				+ "quita antes de contar: el codigo de una revision lleva su propio ano.");
		}

		// Cuando las dos unidades SI coinciden, se dicen las dos mitades y ya: comparar es del que
		// pregunta, no de esta hoja. Lo que la ley pide es que la comparacion sea posible, no que
		// se haga aca.
		[Test]
		public void WhenTheUnitsMatchBothHalvesAreSaidWithoutConvertingAnything()
		{
			specification.PublishServiceInterval(
				TheRevision, OperationKindCode.FluidChange, 10000, DistanceUnitCode.UnitA, 365);
			specification.SealRevision(TheRevision);

			string answer = TheSentenceTheDoorWouldCompose.TheServiceInterval(
				specification, OperationKindCode.FluidChange, 8400, DistanceUnitCode.UnitA,
				TheLanguage.OfTheRecord);

			Assert.That(answer, Does.Contain("8.400 km"));
			Assert.That(answer, Does.Not.Contain("no se puede evaluar"));
		}

		// Un intervalo publicado SOLO como plazo tampoco se puede comparar con una distancia. No es
		// una discrepancia de unidad: es que no hay mitad de distancia publicada contra la cual
		// pararse, y contestar que faltan 8400 para un plazo seria inventar la mitad que falta.
		[Test]
		public void ATermOnlyIntervalCannotBeEvaluatedAgainstAReceivedDistanceEither()
		{
			specification.PublishServiceInterval(
				TheRevision, OperationKindCode.TimedComponent, 0, DistanceUnitCode.UnitA, 1825);
			specification.SealRevision(TheRevision);

			string answer = TheSentenceTheDoorWouldCompose.TheServiceInterval(
				specification, OperationKindCode.TimedComponent, 8400, DistanceUnitCode.UnitA,
				TheLanguage.OfTheRecord);

			Assert.That(answer, Does.Contain("no se puede evaluar"));
			Assert.That(answer, Does.Contain("1.825 días"));
		}

		// LA AUSENCIA DE CONVERSION, AFIRMADA COMO LISTA BLANCA. Un Convert, un ToUnitA, un factor
		// privado: cualquiera pone rojo esto y obliga a leer la ley antes de seguir.
		[TestCase(typeof(PublishedInterval))]
		public void ThePublishedIntervalExposesNothingOutsideItsAllowlist(Type tipo)
		{
			Assert.That(LoQueExpone(tipo).Where(
					nombre => !LoQueUnIntervaloPuedeExponer.Contains(nombre)),
				Is.Empty,
				$"{tipo.Name} gano miembros. Un intervalo lleva su unidad y no la cambia: con "
				+ "conversion no hay discrepancia que suspender y la ley se queda sin significado.");
		}

		[TestCase(typeof(PublishedMilestone))]
		public void ThePublishedMilestoneExposesNothingOutsideItsAllowlist(Type tipo)
		{
			Assert.That(LoQueExpone(tipo).Where(
					nombre => !LoQueUnHitoPuedeExponer.Contains(nombre)),
				Is.Empty,
				$"{tipo.Name} gano miembros. Dos hitos son el mismo o no lo son; no hay un tercer "
				+ "camino que pase por multiplicar.");
		}

		// UN FACTOR PRIVADO NO LO AGARRA NINGUNA LISTA BLANCA -- las listas miran lo expuesto -- asi
		// que se mira aparte. Un factor entre dos unidades de distancia es una fraccion, y una
		// fraccion en este dominio solo puede ser decimal o punto flotante: los dos numeros que
		// estos tipos si llevan (la distancia y el plazo) son enteros.
		[TestCase(typeof(PublishedInterval))]
		[TestCase(typeof(PublishedMilestone))]
		public void NeitherCarriesAFractionalFieldThatCouldOnlyBeAConversionFactor(Type tipo)
		{
			IEnumerable<string> fracciones = tipo
				.GetFields(BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public
					| BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
				.Where(campo => campo.FieldType == typeof(decimal)
					|| campo.FieldType == typeof(double) || campo.FieldType == typeof(float))
				.Select(campo => campo.Name + " : " + campo.FieldType.Name);

			Assert.That(fracciones, Is.Empty,
				$"{tipo.Name} lleva un numero fraccionario. Sus dos numeros publicados son enteros, "
				+ "asi que una fraccion aca es un factor de conversion aunque no se llame asi.");
		}

		// Y LA MISMA AUSENCIA SOBRE EL ENSAMBLADO ENTERO, por nombre. Es la red que agarra la
		// conversion que se cuela en un tipo que esta lista blanca no nombra -- incluidos los que
		// T4 y T5 todavia no escribieron.
		//
		// LA LEY MISMA ES LA UNICA EXCEPCION, y no es una concesion: el miembro que se llama como la
		// ley lleva la palabra prohibida adentro justamente porque es el sitio donde la negativa se
		// decide. Es el unico nombre exacto que se deja pasar; cualquier variante -- Convert,
		// ConvertTo, Converted -- no calza y cae.
		[Test]
		public void NoTypeInTheDomainDeclaresAMemberThatConvertsBetweenUnits()
		{
			string[] loQueSuenaAConversion =
			{
				"Convert", "ToUnit", "InUnit", "Normalize", "Normalise",
			};

			List<string> culpables = new List<string>();

			foreach (Type tipo in Assembly.Load(new AssemblyName(EnsambladoDelDominio))
				.GetTypes()
				.Where(tipo => tipo.Namespace == EnsambladoDelDominio))
				foreach (string miembro in LoQueExpone(tipo))
					if (miembro != "UnitsAreNeverConverted"
						&& loQueSuenaAConversion.Any(
							sospechoso => miembro.IndexOf(sospechoso, StringComparison.Ordinal) >= 0))
						culpables.Add(tipo.Name + "." + miembro);

			Assert.That(culpables, Is.Empty,
				"Miembros que suenan a conversion: " + string.Join(", ", culpables) + ". "
				+ "Una distancia mal convertida da una fecha de servicio equivocada; una presion "
				+ "mal convertida es alguien inflando una llanta al numero de la otra unidad, en "
				+ "una bomba de gasolinera, con el manometro en la mano.");
		}

		private static string SinLaProcedencia(string frase)
		{
			int procedencia = frase.IndexOf(" Publicado en la revisión", StringComparison.Ordinal);

			return procedencia < 0 ? frase : frase.Substring(0, procedencia);
		}

		// UN NUMERO PUEDE LLEVAR EL SEPARADOR DE MILES DEL IDIOMA ADENTRO, y desde que la frase la
		// redacta el catalogo lo lleva: "10.000". Cortar en el punto partiria un numero en dos y esta
		// ley diria que hay cuatro donde hay dos, que es un rojo por la razon equivocada. El
		// separador cuenta como parte del numero solo entre digitos, asi que un punto final sigue
		// terminandolo.
		private static string[] LosNumerosDe(string frase)
		{
			List<string> numeros = new List<string>();
			int indice = 0;

			while (indice < frase.Length)
			{
				if (!char.IsDigit(frase[indice])) { indice++; continue; }

				int comienzo = indice;

				while (indice < frase.Length
					&& (char.IsDigit(frase[indice]) || SeparaMiles(frase, indice))) indice++;

				numeros.Add(frase.Substring(comienzo, indice - comienzo));
			}

			return numeros.ToArray();
		}

		// El separador de miles del idioma del registro, y solo entre dos digitos.
		private static bool SeparaMiles(string frase, int indice)
		{
			return frase[indice] == '.'
				&& indice + 1 < frase.Length
				&& char.IsDigit(frase[indice + 1])
				&& char.IsDigit(frase[indice - 1]);
		}

		private static List<string> LoQueExpone(Type tipo)
		{
			return tipo
				.GetMembers(BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public
					| BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
				.Where(EsExpuesto)
				.Select(miembro => miembro.Name)
				.Distinct()
				.OrderBy(nombre => nombre, StringComparer.Ordinal)
				.ToList();
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
