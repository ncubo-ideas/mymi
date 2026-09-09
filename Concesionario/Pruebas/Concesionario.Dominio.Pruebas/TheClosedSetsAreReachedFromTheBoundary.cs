using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace Concesionario.Dominio.Pruebas
{
	// LOS SEIS CONJUNTOS CERRADOS DEL SLICE (§3), y la unica pregunta que de verdad muerde
	// en T2: se puede llegar a cada miembro desde un primitivo del wire, y que pasa cuando
	// el codigo que llega no nombra ningun miembro.
	//
	// LA POLARIDAD ES LO IMPORTANTE, y es al reves de la otra lectura del slice:
	//
	//   una lectura CON CLAVE sobre un espacio (ReadDueState de un kind sin operacion) es
	//   TOTAL: afuera del espacio hay una respuesta vacia legitima (E41);
	//
	//   un RESOLUTOR de un conjunto cerrado es lo contrario: afuera del conjunto NO HAY
	//   miembro, asi que "no encontrado" no es un valor, es un defecto de quien llama y
	//   REVIENTA (E32, y la nota de polaridad de E41).
	//
	// Por eso cada resolutor se prueba en las dos direcciones: los codigos conocidos caen
	// en la MISMA instancia siempre, y un codigo inventado revienta. Lo segundo es lo que
	// el "done when" del plan pide AFIRMADO y no supuesto.
	//
	// DueState es la excepcion de la tabla del plan (§3.1): se elige adentro, nunca cruza
	// el wire hacia adentro, y no se entrega nunca. Tambien eso se prueba, porque el dia
	// que alguien le agregue un OfCode habra abierto una puerta que el slice no quiso.
	[TestFixture]
	public sealed class TheClosedSetsAreReachedFromTheBoundary
	{
		[Test]
		public void EveryOperationCodeLandsOnItsMember()
		{
			Assert.That(MaintenanceOperationKind.OfCode(OperationKindCode.Inspection),
				Is.SameAs(MaintenanceOperationKind.Inspection));
			Assert.That(MaintenanceOperationKind.OfCode(OperationKindCode.FluidChange),
				Is.SameAs(MaintenanceOperationKind.FluidChange));
			Assert.That(MaintenanceOperationKind.OfCode(OperationKindCode.FilterChange),
				Is.SameAs(MaintenanceOperationKind.FilterChange));
			Assert.That(MaintenanceOperationKind.OfCode(OperationKindCode.WearPartReplacement),
				Is.SameAs(MaintenanceOperationKind.WearPartReplacement));
			Assert.That(MaintenanceOperationKind.OfCode(OperationKindCode.TimedComponent),
				Is.SameAs(MaintenanceOperationKind.TimedComponent));
			Assert.That(MaintenanceOperationKind.OfCode(OperationKindCode.SoftwareUpdate),
				Is.SameAs(MaintenanceOperationKind.SoftwareUpdate));
			Assert.That(MaintenanceOperationKind.OfCode(OperationKindCode.RecallRemedy),
				Is.SameAs(MaintenanceOperationKind.RecallRemedy));
			Assert.That(MaintenanceOperationKind.OfCode(OperationKindCode.Diagnostic),
				Is.SameAs(MaintenanceOperationKind.Diagnostic));
			Assert.That(MaintenanceOperationKind.OfCode(OperationKindCode.Repair),
				Is.SameAs(MaintenanceOperationKind.Repair));
		}

		[Test]
		public void AnOperationCodeThatNamesNoMemberThrows()
		{
			DomainException error = Assert.Throws<DomainException>(
				() => MaintenanceOperationKind.OfCode((OperationKindCode)99));

			Assert.That(error.Message, Does.Contain("99"),
				"El mensaje tiene que decir QUE codigo llego. Un resolutor que revienta sin "
				+ "nombrar el valor obliga a adivinar de donde vino el defecto.");
		}

		[Test]
		public void EveryUnitCodeLandsOnItsMember()
		{
			Assert.That(DistanceUnit.OfCode(DistanceUnitCode.UnitA), Is.SameAs(DistanceUnit.UnitA));
			Assert.That(DistanceUnit.OfCode(DistanceUnitCode.UnitB), Is.SameAs(DistanceUnit.UnitB));
		}

		[Test]
		public void AUnitCodeThatNamesNoMemberThrows()
		{
			Assert.Throws<DomainException>(() => DistanceUnit.OfCode((DistanceUnitCode)99));
		}

		[Test]
		public void EveryReadingSourceCodeLandsOnItsMember()
		{
			Assert.That(ReadingSource.OfCode(ReadingSourceCode.HumanEntry),
				Is.SameAs(ReadingSource.HumanEntry));
			Assert.That(ReadingSource.OfCode(ReadingSourceCode.Telemetry),
				Is.SameAs(ReadingSource.Telemetry));
			Assert.That(ReadingSource.OfCode(ReadingSourceCode.Inspection),
				Is.SameAs(ReadingSource.Inspection));
			Assert.That(ReadingSource.OfCode(ReadingSourceCode.CustomerDeclared),
				Is.SameAs(ReadingSource.CustomerDeclared));
			Assert.That(ReadingSource.OfCode(ReadingSourceCode.Correction),
				Is.SameAs(ReadingSource.Correction));
		}

		[Test]
		public void AReadingSourceCodeThatNamesNoMemberThrows()
		{
			Assert.Throws<DomainException>(() => ReadingSource.OfCode((ReadingSourceCode)99));
		}

		[Test]
		public void EveryUsageProfileCodeLandsOnItsMember()
		{
			Assert.That(UsageProfile.OfCode(UsageProfileCode.Standard), Is.SameAs(UsageProfile.Standard));
			Assert.That(UsageProfile.OfCode(UsageProfileCode.SevereDuty), Is.SameAs(UsageProfile.SevereDuty));
			Assert.That(UsageProfile.OfCode(UsageProfileCode.LowDistance), Is.SameAs(UsageProfile.LowDistance));
			Assert.That(UsageProfile.OfCode(UsageProfileCode.Commercial), Is.SameAs(UsageProfile.Commercial));
		}

		[Test]
		public void AUsageProfileCodeThatNamesNoMemberThrows()
		{
			Assert.Throws<DomainException>(() => UsageProfile.OfCode((UsageProfileCode)99));
		}

		[Test]
		public void EveryDeferralReasonCodeLandsOnItsMember()
		{
			Assert.That(DeferralReason.OfCode(DeferralReasonCode.Cost), Is.SameAs(DeferralReason.Cost));
			Assert.That(DeferralReason.OfCode(DeferralReasonCode.Timing), Is.SameAs(DeferralReason.Timing));
			Assert.That(DeferralReason.OfCode(DeferralReasonCode.SecondOpinion),
				Is.SameAs(DeferralReason.SecondOpinion));
			Assert.That(DeferralReason.OfCode(DeferralReasonCode.NotConvinced),
				Is.SameAs(DeferralReason.NotConvinced));
			Assert.That(DeferralReason.OfCode(DeferralReasonCode.WillDoElsewhere),
				Is.SameAs(DeferralReason.WillDoElsewhere));
		}

		[Test]
		public void ADeferralReasonCodeThatNamesNoMemberThrows()
		{
			Assert.Throws<DomainException>(() => DeferralReason.OfCode((DeferralReasonCode)99));
		}

		// TODOS los codigos declarados llegan a un miembro. No alcanza con probar los que
		// uno se acordo de escribir arriba: si manana entra un miembro al enum y nadie
		// toca el switch, esto se pone rojo aca y no en produccion.
		[TestCase(typeof(OperationKindCode), typeof(MaintenanceOperationKind))]
		[TestCase(typeof(DistanceUnitCode), typeof(DistanceUnit))]
		[TestCase(typeof(ReadingSourceCode), typeof(ReadingSource))]
		[TestCase(typeof(UsageProfileCode), typeof(UsageProfile))]
		[TestCase(typeof(DeferralReasonCode), typeof(DeferralReason))]
		public void NoDeclaredCodeIsLeftWithoutAMember(Type codigos, Type conjunto)
		{
			MethodInfo resolutor = conjunto.GetMethod("OfCode",
				BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

			Assert.That(resolutor, Is.Not.Null,
				$"{conjunto.Name} se elige desde el wire (tabla §3.1 del plan) y no tiene OfCode. "
				+ "Un conjunto cerrado que solo expone statics es INALCANZABLE desde el DSL (E16).");

			foreach (object codigo in Enum.GetValues(codigos))
			{
				object miembro = resolutor.Invoke(null, new[] { codigo });
				Assert.That(miembro, Is.Not.Null, $"{conjunto.Name}.OfCode({codigo}) no devolvio miembro.");
			}
		}

		// El resolutor NO fabrica una instancia por llamada: devuelve el miembro canonico.
		// Si fabricara, la comparacion por referencia -- que es la unica que estos tipos
		// tienen (E23) -- diria "distintos" para el mismo miembro del dominio.
		[Test]
		public void TheResolverAlwaysReturnsTheSameInstance()
		{
			Assert.That(MaintenanceOperationKind.OfCode(OperationKindCode.FluidChange),
				Is.SameAs(MaintenanceOperationKind.OfCode(OperationKindCode.FluidChange)));
			Assert.That(DistanceUnit.OfCode(DistanceUnitCode.UnitA),
				Is.SameAs(DistanceUnit.OfCode(DistanceUnitCode.UnitA)));
			Assert.That(UsageProfile.OfCode(UsageProfileCode.SevereDuty),
				Is.SameAs(UsageProfile.OfCode(UsageProfileCode.SevereDuty)));
		}

		// El conjunto es CERRADO: exactamente los miembros del canon (§3 del slice), cada
		// uno una instancia distinta. Ni uno de mas -- un miembro que el canon no nombra es
		// vocabulario inventado -- ni dos nombres apuntando al mismo objeto.
		[TestCase(typeof(MaintenanceOperationKind), 9)]
		[TestCase(typeof(DueState), 7)]
		[TestCase(typeof(DistanceUnit), 2)]
		[TestCase(typeof(ReadingSource), 5)]
		[TestCase(typeof(UsageProfile), 4)]
		[TestCase(typeof(DeferralReason), 5)]
		public void EveryClosedSetDeclaresExactlyItsMembers(Type conjunto, int cuantos)
		{
			List<object> miembros = MiembrosDe(conjunto);

			Assert.That(miembros.Count, Is.EqualTo(cuantos),
				$"{conjunto.Name} declara {miembros.Count} miembros y el canon nombra {cuantos}.");

			for (int i = 0; i < miembros.Count; i++)
			{
				for (int j = i + 1; j < miembros.Count; j++)
				{
					Assert.That(ReferenceEquals(miembros[i], miembros[j]), Is.False,
						$"Dos miembros de {conjunto.Name} son el mismo objeto. Con comparacion por "
						+ "referencia y sin igualdad escrita (E23), eso los vuelve indistinguibles.");
				}
			}
		}

		// El ctor privado es lo que hace CERRADO al conjunto: nadie puede fabricar un
		// miembro que el canon no nombra.
		[TestCase(typeof(MaintenanceOperationKind))]
		[TestCase(typeof(DueState))]
		[TestCase(typeof(DistanceUnit))]
		[TestCase(typeof(ReadingSource))]
		[TestCase(typeof(UsageProfile))]
		[TestCase(typeof(DeferralReason))]
		public void NoClosedSetCanBeInstantiatedFromOutside(Type conjunto)
		{
			ConstructorInfo[] ctores = conjunto.GetConstructors(
				BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

			foreach (ConstructorInfo ctor in ctores)
			{
				Assert.That(ctor.IsPrivate, Is.True,
					$"{conjunto.Name} tiene un constructor no privado. Un conjunto cerrado cuyo ctor "
					+ "se alcanza deja de ser cerrado: aparecen miembros que el canon no nombra.");
			}
		}

		// DueState se elige ADENTRO (tabla §3.1 del plan). No cruza el wire hacia adentro,
		// asi que no tiene resolutor ni enum de frontera -- y no tenerlos es la decision,
		// no un olvido.
		[Test]
		public void DueStateIsNotReachedFromTheBoundary()
		{
			Assert.That(typeof(DueState).GetMethod("OfCode",
					BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic),
				Is.Null,
				"DueState gano un OfCode. Segun la tabla §3.1 se elige adentro y nunca cruza el "
				+ "wire hacia adentro; un resolutor le abre una puerta que el slice no quiso.");

			IEnumerable<string> enumsDeFrontera = typeof(DueState).Assembly.GetTypes()
				.Where(tipo => tipo.IsEnum)
				.Select(tipo => tipo.Name);

			Assert.That(enumsDeFrontera, Does.Not.Contain("DueStateCode"),
				"Aparecio un DueStateCode. DueState solo sale como proyeccion escalar (E24).");
		}

		[Test]
		public void DueStateHasTheSevenStatesOfTheCanon()
		{
			Assert.That(DueState.NotDue.Name, Is.EqualTo("NotDue"));
			Assert.That(DueState.DueSoon.Name, Is.EqualTo("DueSoon"));
			Assert.That(DueState.Due.Name, Is.EqualTo("Due"));
			Assert.That(DueState.Overdue.Name, Is.EqualTo("Overdue"));
			Assert.That(DueState.Deferred.Name, Is.EqualTo("Deferred"));
			Assert.That(DueState.Completed.Name, Is.EqualTo("Completed"));
			Assert.That(DueState.Superseded.Name, Is.EqualTo("Superseded"));
		}

		// El nombre del miembro del enum de frontera y el nombre del miembro del conjunto
		// tienen que coincidir EXACTAMENTE. No es cosmetico: MEDIDO en T0 (spike S2) que el
		// parametro del DSL viaja como typeof(Enum) llevando el NOMBRE del miembro, asi que
		// el journal guarda "FluidChange". Si los dos lados se despegan, la coercion en el
		// call site del script falla en runtime -- y eso deja entradas permanentes (S5).
		[TestCase(typeof(OperationKindCode), typeof(MaintenanceOperationKind))]
		[TestCase(typeof(DistanceUnitCode), typeof(DistanceUnit))]
		[TestCase(typeof(ReadingSourceCode), typeof(ReadingSource))]
		[TestCase(typeof(UsageProfileCode), typeof(UsageProfile))]
		[TestCase(typeof(DeferralReasonCode), typeof(DeferralReason))]
		public void TheBoundaryEnumNamesAndTheSetNamesMatch(Type codigos, Type conjunto)
		{
			List<string> nombresDelEnum = Enum.GetNames(codigos)
				.OrderBy(nombre => nombre, StringComparer.Ordinal)
				.ToList();

			List<string> nombresDelConjunto = conjunto
				.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic
					| BindingFlags.DeclaredOnly)
				.Where(campo => campo.IsInitOnly && campo.FieldType == conjunto)
				.Select(campo => campo.Name)
				.OrderBy(nombre => nombre, StringComparer.Ordinal)
				.ToList();

			Assert.That(nombresDelConjunto, Is.EqualTo(nombresDelEnum),
				$"{codigos.Name} y {conjunto.Name} no nombran lo mismo. El wire lleva el NOMBRE "
				+ "del miembro (S2), asi que despegarlos rompe la coercion en runtime.");
		}

		private static List<object> MiembrosDe(Type conjunto)
		{
			return conjunto
				.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic
					| BindingFlags.DeclaredOnly)
				.Where(campo => campo.IsInitOnly && campo.FieldType == conjunto)
				.Select(campo => campo.GetValue(null))
				.ToList();
		}
	}
}
