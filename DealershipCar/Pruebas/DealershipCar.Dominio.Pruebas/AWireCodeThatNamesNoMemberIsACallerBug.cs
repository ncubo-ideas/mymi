using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace DealershipCar.Dominio.Pruebas
{
	// LOS CINCO CONJUNTOS CERRADOS DE ModelSpecification QUE SE ELIGEN DESDE EL WIRE (tabla
	// §3.1 del plan): FluidKind, TyrePosition, FuelGradeStanding, PressureUnit y VolumeUnit.
	// Cada uno llega como un XxxCode y se resuelve con Xxx.OfCode.
	//
	// LA POLARIDAD ES LO QUE ESTA PRUEBA DEFIENDE, y en este slice es la que mas facil se
	// confunde, porque TODO lo demas apunta al otro lado:
	//
	//   toda lectura de la especificacion es TOTAL -- un item que nadie publico contesta "no
	//   publicado", y eso es una respuesta legitima y no un error (E41, ley L2);
	//
	//   un RESOLUTOR de conjunto cerrado es lo CONTRARIO: afuera del conjunto no hay
	//   miembro, asi que un codigo desconocido no es una respuesta vacia sino un defecto de
	//   quien llama, y REVIENTA (E32, y la nota de polaridad de E41).
	//
	// En un modulo donde §9 del brief dice "no hay accesor guardado, toda lectura es total",
	// estos cinco resolutores son el unico sitio donde no contestar es lo correcto. Por eso
	// se prueba en las dos direcciones: los codigos conocidos caen SIEMPRE en la misma
	// instancia canonica, y uno inventado revienta nombrando el valor que llego.
	[TestFixture]
	public sealed class AWireCodeThatNamesNoMemberIsACallerBug
	{
		[Test]
		public void EveryFluidKindCodeLandsOnItsMember()
		{
			Assert.That(FluidKind.OfCode(FluidKindCode.EngineOil), Is.SameAs(FluidKind.EngineOil));
			Assert.That(FluidKind.OfCode(FluidKindCode.BrakeFluid), Is.SameAs(FluidKind.BrakeFluid));
			Assert.That(FluidKind.OfCode(FluidKindCode.Coolant), Is.SameAs(FluidKind.Coolant));
			Assert.That(FluidKind.OfCode(FluidKindCode.TransmissionFluid),
				Is.SameAs(FluidKind.TransmissionFluid));
			Assert.That(FluidKind.OfCode(FluidKindCode.PowerSteeringFluid),
				Is.SameAs(FluidKind.PowerSteeringFluid));
			Assert.That(FluidKind.OfCode(FluidKindCode.WasherFluid), Is.SameAs(FluidKind.WasherFluid));
		}

		[Test]
		public void AFluidKindCodeThatNamesNoMemberThrows()
		{
			DomainException error = Assert.Throws<DomainException>(
				() => FluidKind.OfCode((FluidKindCode)99));

			Assert.That(error.Message, Does.Contain("99"),
				"El mensaje tiene que decir QUE codigo llego. Un resolutor que revienta sin "
				+ "nombrar el valor obliga a adivinar de donde salio el defecto.");
		}

		[Test]
		public void EveryTyrePositionCodeLandsOnItsMember()
		{
			Assert.That(TyrePosition.OfCode(TyrePositionCode.Front), Is.SameAs(TyrePosition.Front));
			Assert.That(TyrePosition.OfCode(TyrePositionCode.Rear), Is.SameAs(TyrePosition.Rear));
			Assert.That(TyrePosition.OfCode(TyrePositionCode.Spare), Is.SameAs(TyrePosition.Spare));
		}

		[Test]
		public void ATyrePositionCodeThatNamesNoMemberThrows()
		{
			Assert.Throws<DomainException>(() => TyrePosition.OfCode((TyrePositionCode)99));
		}

		[Test]
		public void EveryFuelGradeStandingCodeLandsOnItsMember()
		{
			Assert.That(FuelGradeStanding.OfCode(FuelGradeStandingCode.Minimum),
				Is.SameAs(FuelGradeStanding.Minimum));
			Assert.That(FuelGradeStanding.OfCode(FuelGradeStandingCode.Permitted),
				Is.SameAs(FuelGradeStanding.Permitted));
			Assert.That(FuelGradeStanding.OfCode(FuelGradeStandingCode.Prohibited),
				Is.SameAs(FuelGradeStanding.Prohibited));
		}

		[Test]
		public void AFuelGradeStandingCodeThatNamesNoMemberThrows()
		{
			Assert.Throws<DomainException>(
				() => FuelGradeStanding.OfCode((FuelGradeStandingCode)99));
		}

		[Test]
		public void EveryPressureUnitCodeLandsOnItsMember()
		{
			Assert.That(PressureUnit.OfCode(PressureUnitCode.UnitA), Is.SameAs(PressureUnit.UnitA));
			Assert.That(PressureUnit.OfCode(PressureUnitCode.UnitB), Is.SameAs(PressureUnit.UnitB));
		}

		[Test]
		public void APressureUnitCodeThatNamesNoMemberThrows()
		{
			Assert.Throws<DomainException>(() => PressureUnit.OfCode((PressureUnitCode)99));
		}

		[Test]
		public void EveryVolumeUnitCodeLandsOnItsMember()
		{
			Assert.That(VolumeUnit.OfCode(VolumeUnitCode.UnitA), Is.SameAs(VolumeUnit.UnitA));
			Assert.That(VolumeUnit.OfCode(VolumeUnitCode.UnitB), Is.SameAs(VolumeUnit.UnitB));
		}

		[Test]
		public void AVolumeUnitCodeThatNamesNoMemberThrows()
		{
			Assert.Throws<DomainException>(() => VolumeUnit.OfCode((VolumeUnitCode)99));
		}

		// TODOS los codigos declarados llegan a un miembro. No alcanza con probar los que uno
		// se acordo de escribir arriba: si manana entra un miembro al enum y nadie toca el
		// switch, esto se pone rojo aca y no en el journal de una persona.
		[TestCase(typeof(FluidKindCode), typeof(FluidKind))]
		[TestCase(typeof(TyrePositionCode), typeof(TyrePosition))]
		[TestCase(typeof(FuelGradeStandingCode), typeof(FuelGradeStanding))]
		[TestCase(typeof(PressureUnitCode), typeof(PressureUnit))]
		[TestCase(typeof(VolumeUnitCode), typeof(VolumeUnit))]
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
				Assert.That(miembro, Is.Not.Null,
					$"{conjunto.Name}.OfCode({codigo}) no devolvio miembro.");
			}
		}

		// El resolutor NO fabrica una instancia por llamada: devuelve el miembro canonico. Si
		// fabricara, la comparacion por referencia -- que es la unica que estos tipos tienen
		// (E23) -- diria "distintos" para el mismo miembro del dominio.
		[Test]
		public void TheResolverAlwaysReturnsTheSameInstance()
		{
			Assert.That(FluidKind.OfCode(FluidKindCode.EngineOil),
				Is.SameAs(FluidKind.OfCode(FluidKindCode.EngineOil)));
			Assert.That(TyrePosition.OfCode(TyrePositionCode.Spare),
				Is.SameAs(TyrePosition.OfCode(TyrePositionCode.Spare)));
			Assert.That(PressureUnit.OfCode(PressureUnitCode.UnitB),
				Is.SameAs(PressureUnit.OfCode(PressureUnitCode.UnitB)));
			Assert.That(VolumeUnit.OfCode(VolumeUnitCode.UnitA),
				Is.SameAs(VolumeUnit.OfCode(VolumeUnitCode.UnitA)));
			Assert.That(FuelGradeStanding.OfCode(FuelGradeStandingCode.Prohibited),
				Is.SameAs(FuelGradeStanding.OfCode(FuelGradeStandingCode.Prohibited)));
		}

		// El nombre del miembro del enum de frontera y el del miembro del conjunto tienen que
		// coincidir EXACTAMENTE. No es cosmetico: MEDIDO en OS2 (spike S2, C12, C25) que el
		// parametro del DSL viaja como typeof(Enum) llevando el NOMBRE del miembro, asi que el
		// journal guarda "EngineOil". Si los dos lados se despegan, la coercion en el call site
		// del script falla en runtime -- y eso deja entradas permanentes (S5 de OS2).
		[TestCase(typeof(FluidKindCode), typeof(FluidKind))]
		[TestCase(typeof(TyrePositionCode), typeof(TyrePosition))]
		[TestCase(typeof(FuelGradeStandingCode), typeof(FuelGradeStanding))]
		[TestCase(typeof(PressureUnitCode), typeof(PressureUnit))]
		[TestCase(typeof(VolumeUnitCode), typeof(VolumeUnit))]
		public void TheBoundaryEnumNamesAndTheSetNamesMatch(Type codigos, Type conjunto)
		{
			List<string> nombresDelEnum = Enum.GetNames(codigos)
				.OrderBy(nombre => nombre, StringComparer.Ordinal)
				.ToList();

			List<string> nombresDelConjunto = MiembrosDe(conjunto)
				.Select(campo => campo.Name)
				.OrderBy(nombre => nombre, StringComparer.Ordinal)
				.ToList();

			Assert.That(nombresDelConjunto, Is.EqualTo(nombresDelEnum),
				$"{codigos.Name} y {conjunto.Name} no nombran lo mismo. El wire lleva el NOMBRE "
				+ "del miembro, asi que despegarlos rompe la coercion en runtime.");
		}

		// El Name que el conjunto proyecta es el nombre del miembro, no una etiqueta aparte.
		// Es lo unico que sale hacia afuera (E24), y si dijera otra cosa el journal y la
		// respuesta hablarian de dos vocabularios distintos.
		//
		// LAS TRES UNIDADES QUEDAN FUERA A PROPOSITO, y son el unico caso del modulo: su
		// miembro se llama UnitA y su Name dice km, L o bar. Ahi los dos vocabularios SI son
		// distintos, porque la identidad real de una unidad es cosa de quien la muestra
		// (DEALERSHIP-DOMAIN §6, la fila de DistanceUnit) y una persona no lee "UnitA".
		// La ley que las cubre a ellas es TheUnitsAreSaidInTheirRealWorldNames, que fija los
		// seis nombres uno por uno y vuelve a atar el wire al nombre del miembro.
		//
		// LA LISTA SE COMPLETO EN T1 DE WarrantyEnvelope, y estaba corta por lo que dice C57: el
		// archivo enumeraba SU sujeto -- los conjuntos que se eligen desde el wire -- mientras que
		// esta ley de aca arriba habla de TODOS ("son el unico caso del modulo"). RevisionState,
		// CoverageStanding y el nuevo EnvelopeStanding se eligen adentro y proyectan su nombre
		// igual, asi que caen bajo la misma ley y faltaban. Los tres pasaban ya; lo que faltaba
		// era que la barrida lo dijera.
		// MS-1 AGREGA RemainderStanding, y cae bajo la misma ley por lo mismo que los tres de
		// arriba: se elige adentro y proyecta su nombre. Su HowItIsSaid es un segundo vocabulario
		// -- el que una persona lee -- y no es lo que cruza la puerta.
		// OD-1 AGREGA RateStanding, por lo mismo, y con una consecuencia que vale escribir: como se
		// elige adentro y solo PROYECTA, no cruza nada hacia adentro, asi que NO necesita miembros
		// en WireName ni ensancha TheDoorRecognisesTheNamesOfEveryClosedSetItSpeaks. La lista de
		// aca abajo y la de aquel barrido no son la misma lista, y esa es la razon.
		[TestCase(typeof(FluidKind))]
		[TestCase(typeof(TyrePosition))]
		[TestCase(typeof(FuelGradeStanding))]
		[TestCase(typeof(RevisionState))]
		[TestCase(typeof(CoverageStanding))]
		[TestCase(typeof(EnvelopeStanding))]
		[TestCase(typeof(RemainderStanding))]
		[TestCase(typeof(RateStanding))]
		public void TheProjectedNameIsTheMembersOwnName(Type conjunto)
		{
			foreach (FieldInfo campo in MiembrosDe(conjunto))
			{
				object miembro = campo.GetValue(null);
				PropertyInfo nombre = conjunto.GetProperty("Name",
					BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

				Assert.That(nombre, Is.Not.Null,
					$"{conjunto.Name} no proyecta Name. Es lo unico que cruza hacia afuera.");
				Assert.That(nombre.GetValue(miembro), Is.EqualTo(campo.Name));
			}
		}

		private static List<FieldInfo> MiembrosDe(Type conjunto)
		{
			return conjunto
				.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic
					| BindingFlags.DeclaredOnly)
				.Where(campo => campo.IsInitOnly && campo.FieldType == conjunto)
				.ToList();
		}
	}
}
