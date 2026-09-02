using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace DealershipCar.Dominio.Pruebas
{
	// LAS DOS TRANSICIONES ILEGALES DE §10 del brief QUE HABLAN DE UNA REVISION SELLADA:
	//
	//   Sealed acepta un publish   -> revienta
	//   Sealed -> Draft            -> no existe camino
	//
	// Son dos formas de la misma ley: sellada quiere decir CERRADA. Una revision sellada ya
	// contesta (L12), y todo lo que contesto nombra esa revision (L1). Si despues de sellar
	// entrara contenido nuevo -- o si volviera a borrador y se sellara otra vez con contenido
	// distinto -- la revision que alguien tiene anotada como procedencia diria una cosa
	// distinta de la que dijo, sin haberse superseido. Eso es exactamente lo que la ley L11
	// prohibe: una revision no alcanza hacia atras.
	//
	// LA SEGUNDA MITAD SE AFIRMA COMO AUSENCIA, y por eso hay lista blanca. "Sealed -> Draft"
	// no se puede probar llamando a algo: se prueba mostrando que no hay NADA que llamar. Un
	// Reopen, un Unseal, un BackToDraft, un setter de estado -- cualquiera pone rojo esto y
	// obliga a leer L11 antes de seguir. Cuando entren los caminos de publicacion de verdad,
	// esta lista crece con ellos: cada miembro nuevo se lee contra esta ley y despues se
	// agrega, en ese orden y no al reves.
	[TestFixture]
	public sealed class ASealedRevisionIsClosedToPublicationAndCannotReopen
	{
		private const string TheRevision = "REV-2025-01";
		private const string TheNextRevision = "REV-2025-02";

		private static readonly DateTime PublishedAt = new DateTime(2025, 3, 14, 10, 0, 0);

		// La lista blanca de PublishedRevision: la identidad y el instante que recibio, la
		// proyeccion del estado, las tres preguntas totales, el accesor guardado del sucesor, las
		// dos transiciones, y el camino por el que entra contenido con su par de lectura. Nada
		// que reabra.
		//
		// EN T2 CRECIO y EN T3 CRECIO OTRA VEZ, con los otros cinco items. Cada miembro nuevo se
		// leyo contra esta ley PRIMERO y se agrego DESPUES, y los diecisiete de T3 son de tres
		// formas y nada mas: un camino de entrada que solo un borrador acepta, una pregunta de
		// presencia total, y un accesor guardado que compone una clausula. Ninguna de las tres
		// puede devolver una revision sellada a borrador, que es lo unico que esta lista defiende.
		//
		// LOS DOS "DescribeAgainst..." SON LA MITAD RECIBIDA de la ley L7: componen lo que ESTE
		// vehiculo trae contra lo publicado. Entran aca porque la comparacion necesita la unidad
		// publicada, que solo la revision tiene a mano -- y salen sin guardar nada.
		//
		// EN T4 CRECIO CON LOS OCHO DE LA GARANTIA, y son de las mismas tres formas: dos entradas
		// que solo un borrador acepta, una pregunta de presencia total, y cinco accesores
		// guardados que solo leen. La diferencia con los items de T2 y T3 es que el alcance
		// ACUMULA dentro de la revision en vez de estar indexado por una clave -- se publica de a
		// un sistema y de a una exclusion --, lo que no cambia nada de esta ley: lo que la ley
		// mira es si algo puede entrar DESPUES DEL SELLO, y las dos entradas pasan por la misma
		// guarda de borrador que todas las demas.
		//
		// TheCoverageStandingFor RECIBE los hechos del vehiculo y no guarda ninguno. Es la unica
		// de la lista que devuelve una respuesta compuesta en vez de una frase, porque es la unica
		// que puede quedar sin poder evaluarse -- y eso se dice con un tipo, no con un miembro
		// benigno del conjunto de respuestas.
		private static readonly string[] LoQueUnaRevisionPuedeExponer =
		{
			".ctor",
			"Code",
			"PublishedAt",
			"ReadState",
			"IsSealed",
			"HasContent",
			"HasBeenSuperseded",
			"SupersededByCode",
			"PublishAFluidSpecification",
			"HasAFluidSpecification",
			"DescribeFluidSpecification",
			"PublishATyreSpecification",
			"HasATyreSpecification",
			"DescribeTyreSpecification",
			"PublishAFuelGrade",
			"HasAFuelSpecification",
			"DescribeFuelSpecification",
			"TheMinimumFuelGradePublished",
			"ThePermittedFuelGradesPublished",
			"TheProhibitedFuelGradesPublished",
			"PublishAServiceInterval",
			"HasAServiceInterval",
			"DescribeServiceInterval",
			"DescribeAgainstTheServiceInterval",
			"PublishAServicePrice",
			"HasAServicePrice",
			"DescribeServicePrice",
			"PublishAServicingPolicy",
			"HasAServicingPolicy",
			"DescribeServicingPolicy",
			"DescribeAgainstTheServicingPolicy",
			"PublishACoveredSystem",
			"PublishACoverageExclusion",
			"HasWarrantyCoverage",
			"DescribeWarrantyCoverage",
			"TheCoveredSystemsPublished",
			"TheCoverageExclusionsPublished",
			"TheVocabularyIsTheManufacturers",
			"TheCoverageStandingFor",
			"Seal",
			"SupersededBy",
		};

		private PublishedRevision revision;

		[SetUp]
		public void Setup()
		{
			revision = new PublishedRevision(TheRevision, PublishedAt);
			TheEngineOilEntersInto(revision);
			revision.Seal();
		}

		[Test]
		public void PublishingIntoASealedRevisionIsRefused()
		{
			DomainException error = Assert.Throws<DomainException>(
				() => TheBrakeFluidEntersInto(revision));

			Assert.That(error.Message, Does.Contain("Sealed"),
				"El mensaje nombra el estado que rechazo la publicacion.");
			Assert.That(revision.ReadState(), Is.EqualTo("Sealed"),
				"Y el rechazo es inerte: la revision sigue sellada, con el contenido que tenia.");
			Assert.That(revision.HasAFluidSpecification(FluidKindCode.BrakeFluid), Is.False,
				"Inerte de verdad: lo que se quiso colar no entro. Si hubiera entrado y solo se "
				+ "hubiera reventado despues, la revision ya diria algo distinto de lo que dijo.");
		}

		[Test]
		public void PublishingIntoASupersededRevisionIsRefused()
		{
			revision.SupersededBy(ASealedRevision(TheNextRevision));

			DomainException error = Assert.Throws<DomainException>(
				() => TheBrakeFluidEntersInto(revision));

			Assert.That(error.Message, Does.Contain("Superseded"));
			Assert.That(revision.ReadState(), Is.EqualTo("Superseded"));
		}

		// Y lo que la revision publico ANTES de sellarse sigue ahi y se puede leer: cerrada
		// quiere decir que no entra nada nuevo, no que deje de contestar. Una revision sellada
		// que perdiera su contenido seria la unica que puede contestar contestando nada.
		[Test]
		public void ASealedRevisionStillAnswersForWhatItPublishedBeforeSealing()
		{
			Assert.That(revision.HasAFluidSpecification(FluidKindCode.EngineOil), Is.True);
			Assert.That(revision.DescribeFluidSpecification(FluidKindCode.EngineOil),
				Does.Contain("5W-30"));
		}

		// El par de E41 sobre el contenido: la pregunta de presencia es TOTAL y el accesor esta
		// GUARDADO. Pedir la clausula de un fluido que esta revision no publico es un defecto de
		// quien llama -- una clausula vacia se leeria como una especificacion sin grado.
		[Test]
		public void TheClauseOfAKindTheRevisionDidNotPublishCannotBeRead()
		{
			Assert.That(revision.HasAFluidSpecification(FluidKindCode.Coolant), Is.False);

			Assert.Throws<DomainException>(
				() => revision.DescribeFluidSpecification(FluidKindCode.Coolant));
		}

		[Test]
		public void NothingTakesASealedRevisionBackToADraft()
		{
			List<string> expuestos = typeof(PublishedRevision)
				.GetMembers(BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public
					| BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
				.Where(EsExpuesto)
				.Select(miembro => miembro.Name)
				.Distinct()
				.OrderBy(nombre => nombre, StringComparer.Ordinal)
				.ToList();

			List<string> intrusos = expuestos
				.Where(nombre => !LoQueUnaRevisionPuedeExponer.Contains(nombre))
				.ToList();

			Assert.That(intrusos, Is.Empty,
				"PublishedRevision gano miembros: " + string.Join(", ", intrusos) + ". "
				+ "§10 del brief no tiene fila de Sealed a Draft, y no tenerla es la ley: una "
				+ "revision que ya contesto no puede volver a borrador y cambiar lo que dijo. Si el "
				+ "miembro nuevo no reabre nada, agregarlo a la lista es correcto -- pero se lee "
				+ "contra esta ley primero.");
		}

		// El estado sale como NOMBRE y no como objeto (E24). Entregar el RevisionState dejaria
		// al de afuera ramificar sobre el interior en vez de preguntar; y como el conjunto se
		// elige adentro, no hay ninguna razon para que el objeto cruce.
		[Test]
		public void TheStateLeavesAsAScalarAndNotAsTheObject()
		{
			IEnumerable<MemberInfo> queEntreganElEstado = typeof(PublishedRevision)
				.GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
					| BindingFlags.DeclaredOnly)
				.Where(EsExpuesto)
				.Where(miembro => TipoQueDevuelve(miembro) == typeof(RevisionState));

			Assert.That(queEntreganElEstado, Is.Empty,
				"Algun miembro expuesto entrega el RevisionState. Sale ReadState() con el nombre, "
				+ "que es lo unico que cruza.");
		}

		private static PublishedRevision ASealedRevision(string code)
		{
			PublishedRevision sellada = new PublishedRevision(code, PublishedAt.AddDays(30));
			TheEngineOilEntersInto(sellada);
			sellada.Seal();

			return sellada;
		}

		private static void TheEngineOilEntersInto(PublishedRevision draft)
		{
			draft.PublishAFluidSpecification(
				FluidKindCode.EngineOil,
				new FluidSpecification(
					FluidKind.EngineOil, "5W-30", 4.6m, VolumeUnit.UnitA,
					new PublishedInterval(15000, DistanceUnit.UnitA, 365)));
		}

		private static void TheBrakeFluidEntersInto(PublishedRevision draft)
		{
			draft.PublishAFluidSpecification(
				FluidKindCode.BrakeFluid,
				new FluidSpecification(
					FluidKind.BrakeFluid, "DOT 4", 1.0m, VolumeUnit.UnitA,
					new PublishedInterval(0, DistanceUnit.UnitA, 730)));
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
