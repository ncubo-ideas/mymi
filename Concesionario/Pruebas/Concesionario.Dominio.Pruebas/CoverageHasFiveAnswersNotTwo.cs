using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace Concesionario.Dominio.Pruebas
{
	// LEY L9 del brief, CoverageHasFiveAnswersNotTwo: "una respuesta de cobertura sale del
	// conjunto cerrado de cinco. Un booleano en este camino es un defecto AUNQUE ACIERTE."
	//
	// ES LA MITAD MECANICA DEL TEST 15 DE §11: "ninguna lectura del camino de cobertura devuelve
	// un booleano pelado sobre la cobertura". Se afirma por reflexion porque es una AUSENCIA: no
	// se puede probar llamando a algo que no existe. Es la misma forma de lista blanca que
	// LoQueUnaRevisionPuedeExponer, y por la misma razon -- el dia que alguien agregue un
	// IsCovered(codigo) nadie lo va a notar leyendo el diff, y sin embargo habra colapsado cinco
	// respuestas honestas en dos.
	//
	// LOS BOOLEANOS QUE SI PUEDEN ESTAR, y que es lo que contesta cada uno. Ninguno contesta "si
	// esto esta cubierto":
	//
	//   IsCoveragePublished            si hay alcance publicado. Presencia, no cobertura.
	//   TheVocabularyIsTheManufacturers  si el catalogo conoce el codigo (L10). Reconocimiento.
	//   CanCoverageBeEvaluated         si hay con que decidir (L8/E47). Evaluabilidad.
	//   HasWarrantyCoverage            si ESTA revision publico alcance. Presencia otra vez.
	//   HasAnythingPublished           si entro algo al alcance de esta revision.
	//   HasAnApplicableExclusion       si hay letra chica publicada sobre ese sistema (L4).
	//   NamesTheSystem                 si este item habla de ese codigo. Identificacion.
	//   NamesTheSameExclusionAs        si dos publicaciones son la misma exclusion corregida.
	//   Exists                         la pregunta total del par de ausencia (E45).
	//   HasADistanceHalf               si el limite publico su mitad de distancia.
	//   MeasuresDistanceIn             si el limite esta en esa unidad (L8).
	//   TheTimeHalfIsPast              si el vehiculo paso ESE limite. De un limite, no de la
	//   TheDistanceHalfIsPast          cobertura: la cobertura la decide el procedimiento entero.
	//   CanBeEvaluated                 la pregunta total de la base E47.
	//   SuspendedByAUnitMismatch       cual es la clase de ausencia, nombrada (E47).
	//
	// Y NINGUNO SE LLAMA "Covered". El canon del brief §4 prohibe "covered" como nombre de campo
	// booleano con todas las letras, y esa es la regla que se puede afirmar sin ambiguedad: un
	// booleano cuyo NOMBRE dice "covered" esta contestando la pregunta que tiene cinco respuestas.
	[TestFixture]
	public sealed class CoverageHasFiveAnswersNotTwo
	{
		private const string TheModel = "YARIS-2019";
		private const string TheRevision = "REV-2025-01";

		private static readonly DateTime PublishedAt = new DateTime(2025, 3, 14, 10, 0, 0);
		private static readonly DateTime DeliveredAt = new DateTime(2020, 1, 15, 9, 0, 0);
		private static readonly DateTime AsOf = new DateTime(2022, 1, 15, 9, 0, 0);

		private static readonly string[] LosTiposDelCaminoDeCobertura =
		{
			"WarrantyCoverage",
			"CoveredSystem",
			"CoverageExclusion",
			"SystemLimits",
			"ASystemLimits",
			"NoSystemLimits",
			"CoverageAnswer",
			"ASettledCoverageAnswer",
			"ACoverageSuspendedByAUnitMismatch",
			"CoverageStanding",
		};

		private static readonly string[] LosBooleanosQueElCaminoDeCoberturaPuedeTener =
		{
			"IsCoveragePublished",
			"TheVocabularyIsTheManufacturers",
			"CanCoverageBeEvaluated",
			"HasWarrantyCoverage",
			"HasAnythingPublished",
			"HasAnApplicableExclusion",
			"NamesTheSystem",
			"NamesTheSameExclusionAs",
			"Exists",
			"HasADistanceHalf",
			"MeasuresDistanceIn",
			"TheTimeHalfIsPast",
			"TheDistanceHalfIsPast",
			"CanBeEvaluated",
			"SuspendedByAUnitMismatch",
		};

		// TEST 15 DE §11, la mitad del booleano.
		[Test]
		public void NoReadOnTheCoveragePathReturnsABareBooleanAboutCover()
		{
			List<string> intrusos = LosBooleanosDelCaminoDeCobertura()
				.Where(nombre => !LosBooleanosQueElCaminoDeCoberturaPuedeTener.Contains(nombre))
				.ToList();

			Assert.That(intrusos, Is.Empty,
				"Booleanos nuevos en el camino de cobertura: " + string.Join(", ", intrusos) + ". "
				+ "Ley L9: la pregunta tiene cinco respuestas honestas. Si el booleano nuevo no "
				+ "contesta 'esta cubierto', agregarlo a la lista es correcto -- pero se dice antes "
				+ "que contesta, que es la pregunta que la ley hace.");
		}

		[Test]
		public void NoBooleanOnTheCoveragePathIsNamedAfterBeingCovered()
		{
			List<string> nombrados = LosBooleanosDelCaminoDeCobertura()
				.Where(nombre => nombre.IndexOf("Covered", StringComparison.Ordinal) >= 0)
				.ToList();

			Assert.That(nombrados, Is.Empty,
				"Booleanos que dicen 'Covered' en su nombre: " + string.Join(", ", nombrados)
				+ ". El canon del brief §4 lo prohibe con todas las letras, y la razon es que un "
				+ "booleano llamado asi ya contesto la pregunta de cinco respuestas con dos.");
		}

		// El conjunto no se entrega: la fachada proyecta el NOMBRE del miembro y nada mas (E24).
		// Entregar el objeto dejaria a la caratula ramificar sobre el en vez de leer la frase que
		// el dominio compuso.
		[Test]
		public void TheFacadeNeverHandsOutTheStandingItself()
		{
			List<string> entregan = typeof(ModelSpecification)
				.GetMembers(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public
					| BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
				.Where(EsExpuesto)
				.Where(miembro => TipoQueDevuelve(miembro) == typeof(CoverageStanding))
				.Select(miembro => miembro.Name)
				.ToList();

			Assert.That(entregan, Is.Empty,
				"Miembros que entregan el CoverageStanding: " + string.Join(", ", entregan)
				+ ". Sale ReadCoverageStanding() con el nombre.");
		}

		// Y las CINCO se alcanzan de verdad. Una prueba que solo mira ausencias pasaria igual con
		// tres miembros muertos, y tres respuestas muertas son un conjunto de dos disfrazado.
		[Test]
		public void EveryOneOfTheFiveAnswersIsReachableFromTheRead()
		{
			ModelSpecification specification = ASpecificationThatCanProduceAllFive();

			Assert.That(TheStandingOf(specification, "ENG-BLOCK"), Is.EqualTo("Covered"));
			Assert.That(TheStandingOf(specification, "TRANSMISSION"),
				Is.EqualTo("CoveredWithExclusion"));
			Assert.That(TheStandingOf(specification, "PAINT"), Is.EqualTo("NotCovered"));
			Assert.That(TheStandingOf(specification, "HV-BATTERY"), Is.EqualTo("OutsideEnvelope"));
			Assert.That(TheStandingOf(specification, "NO-SUCH"), Is.EqualTo("NotPublished"));
		}

		private static ModelSpecification ASpecificationThatCanProduceAllFive()
		{
			ModelSpecification specification = new ModelSpecification(TheModel);
			specification.OpenRevision(TheRevision, PublishedAt);
			specification.PublishCoveredSystem(
				TheRevision, "ENG-BLOCK", 0, 0, DistanceUnitCode.UnitA);
			specification.PublishCoveredSystem(
				TheRevision, "TRANSMISSION", 0, 0, DistanceUnitCode.UnitA);
			specification.PublishCoveredSystem(
				TheRevision, "HV-BATTERY", 12, 20000, DistanceUnitCode.UnitA);
			specification.PublishCoverageExclusion(
				TheRevision, "TRANSMISSION", "damage from towing above the rated weight",
				"when the vehicle has towed above its rated weight");
			specification.PublishCoverageExclusion(
				TheRevision, "PAINT", "paintwork and exterior trim are outside the warranty",
				"from delivery onwards");
			specification.SealRevision(TheRevision);

			return specification;
		}

		private static string TheStandingOf(ModelSpecification specification, string systemCode)
		{
			return specification.ReadCoverageStanding(
				systemCode, 36, 100000, DistanceUnitCode.UnitA,
				DeliveredAt, 10, 12000, DistanceUnitCode.UnitA, AsOf);
		}

		private static List<string> LosBooleanosDelCaminoDeCobertura()
		{
			List<Type> tipos = Assembly.Load(new AssemblyName("Concesionario.Dominio"))
				.GetTypes()
				.Where(tipo => LosTiposDelCaminoDeCobertura.Contains(tipo.Name))
				.ToList();

			Assert.That(tipos.Count, Is.EqualTo(LosTiposDelCaminoDeCobertura.Length),
				"Falta algun tipo del camino de cobertura: la lista de arriba nombra "
				+ LosTiposDelCaminoDeCobertura.Length + " y el ensamblado tiene " + tipos.Count
				+ ". Una lista blanca que mira tipos que no existen no protege nada.");

			List<string> booleanos = new List<string>();

			foreach (Type tipo in tipos)
				booleanos.AddRange(LosBooleanosExpuestosDe(tipo));

			booleanos.AddRange(LosBooleanosExpuestosDe(typeof(ModelSpecification))
				.Where(EsUnaLecturaDeCobertura));

			booleanos.AddRange(LosBooleanosExpuestosDe(typeof(PublishedRevision))
				.Where(EsUnaLecturaDeCobertura));

			return booleanos
				.Distinct()
				.OrderBy(nombre => nombre, StringComparer.Ordinal)
				.ToList();
		}

		private static bool EsUnaLecturaDeCobertura(string nombre)
		{
			return nombre.IndexOf("Coverage", StringComparison.Ordinal) >= 0
				|| nombre.IndexOf("Covered", StringComparison.Ordinal) >= 0
				|| nombre.IndexOf("Vocabulary", StringComparison.Ordinal) >= 0
				|| nombre.IndexOf("Exclusion", StringComparison.Ordinal) >= 0;
		}

		private static IEnumerable<string> LosBooleanosExpuestosDe(Type tipo)
		{
			return tipo
				.GetMembers(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public
					| BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
				.Where(EsExpuesto)
				.Where(miembro => TipoQueDevuelve(miembro) == typeof(bool))
				.Select(miembro => miembro.Name);
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
