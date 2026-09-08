using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;

namespace Concesionario.Dominio.Pruebas
{
	// L3 Y L4. La placa NO es una identidad: el Vin identifica y la placa describe.
	//
	// L3 -- ninguna igualdad escrita a mano, ningun hash, ningun IEquatable. E23, y el otro modulo
	// tomo la misma decision con el mismo argumento: los diccionarios se llavean por el TEXTO, no por
	// el objeto.
	//
	// LA MITAD DE ARRIBA YA LA CUBRE UN INSTRUMENTO QUE EXISTE. TheDomainWritesNoEquality corre sobre
	// el ENSAMBLADO ENTERO, asi que los tipos de la placa entran solos el dia que existan y no hay que
	// escribirle nada. Lo que ese instrumento NO hace es lo que §12 prueba 6 pide en su segunda mitad:
	// ENUMERAR los miembros del tipo de la placa. Un tipo puede no escribir igualdad y aun asi ofrecer
	// un IsTheSame que nadie llama, y eso es lo que este archivo mira.
	//
	// Y NO HAY IsTheSame, A PROPOSITO. Cargas tiene EsLaMisma; de este lado NADIE lo llamaria: L4
	// prohibe toda lectura cuyo parametro sea una placa, "cual es la vigente" se decide por POSICION
	// en la historia ordenada (DEC-99) y no comparando dos placas, y el hallazgo permanente de este
	// modulo es que un guard por el que nada se pone rojo no esta defendido. Escribir un miembro
	// muerto para que una tabla de leyes se vea completa es el defecto, no la omision.
	[TestFixture]
	public sealed class ThePlateIsComparedByANamedOperationAndNotByEquality
	{
		// ===== PRUEBA 6 · POR ENUMERACION DE LOS MIEMBROS DEL TIPO =========================

		// LOS TIPOS SE NOMBRAN CON typeof Y YA NO POR CADENA. LP-1 los escribio y el proyecto de
		// pruebas esta en el InternalsVisibleTo del dominio, asi que un tipo renombrado o borrado deja
		// de COMPILAR en vez de resolverse a null en runtime (DEC-101).
		[TestCase(typeof(LicensePlate))]
		[TestCase(typeof(TheJoinKey))]
		[TestCase(typeof(APlateAssignment))]
		public void NoPlateTypeDeclaresEqualityAHashOrIEquatable(Type plate)
		{
			var written = new List<string>();

			foreach (MethodInfo each in plate.GetMethods(
				BindingFlags.Instance | BindingFlags.Static
				| BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
				if (each.Name == "Equals" || each.Name == "GetHashCode"
					|| each.Name == "op_Equality" || each.Name == "op_Inequality")
					written.Add(each.Name);

			foreach (Type each in plate.GetInterfaces())
				if (each.IsGenericType
					&& each.GetGenericTypeDefinition() == typeof(IEquatable<>))
					written.Add("IEquatable<>");

			Assert.That(written, Is.Empty,
				plate.Name + " escribio igualdad: " + string.Join(", ", written) + ". La comparacion "
				+ "es una operacion con nombre sobre el texto normalizado, nunca un Equals -- que es "
				+ "lo que el otro lado decidio, y el cruce depende de que los dos lo hagan igual.");

			Assert.That(plate.IsValueType, Is.False,
				plate.Name + " es un struct, y un struct trae igualdad de valor sin que nadie la "
				+ "escriba.");

			Assert.That(
				plate.GetMethod("<Clone>$",
					BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
					| BindingFlags.DeclaredOnly),
				Is.Null,
				plate.Name + " es un record. La igualdad viene de regalo con la palabra.");
		}

		// Y NINGUNO OFRECE UN "ES LA MISMA" QUE NADIE LLAMA. La omision es la decision (DEC-100), asi
		// que si alguien lo agrega tiene que borrar esta prueba, y borrar una prueba es un acto.
		[TestCase(typeof(LicensePlate))]
		[TestCase(typeof(TheJoinKey))]
		public void AndNoPlateTypeOffersASamenessOperationNobodyCalls(Type plate)
		{
			var found = new List<string>();

			foreach (MethodInfo each in plate.GetMethods(
				BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
				| BindingFlags.DeclaredOnly))
				foreach (ParameterInfo parameter in each.GetParameters())
					if (parameter.ParameterType == plate) found.Add(each.Name);

			Assert.That(found, Is.Empty,
				plate.Name + " toma otro " + plate.Name + " en " + string.Join(", ", found) + ". "
				+ "Comparar dos placas no lo pide nada de este lado del cruce.");
		}

		// E43: los dos variantes van SELLADOS y la base queda ABIERTA. Sellar la base es el reflejo
		// que despues obliga a des-sellarla.
		[Test]
		public void TheTwoVariantsAreSealedAndTheBaseThePairHangsFromIsLeftOpen()
		{
			Assert.That(typeof(AnAssignedPlate).IsSealed, Is.True);

			Assert.That(typeof(NoPlateOnRecord).IsSealed, Is.True);

			Type onRecord = typeof(PlateOnRecord);

			Assert.That(onRecord.IsSealed, Is.False,
				"La base del par queda abierta: es a lo que se extiende una variante nueva.");

			Assert.That(onRecord.IsAbstract, Is.True);
		}

		// TODO EL DOMINIO SIGUE SIENDO internal. Un tipo publico deja que un llamador se salte el
		// actor, y aca ademas dejaria que una caratula nombrara una placa.
		[TestCase(typeof(LicensePlate))]
		[TestCase(typeof(TheJoinKey))]
		[TestCase(typeof(APlateAssignment))]
		[TestCase(typeof(PlateOnRecord))]
		[TestCase(typeof(AnAssignedPlate))]
		[TestCase(typeof(NoPlateOnRecord))]
		public void EveryPlateTypeIsInternal(Type plate)
		{
			Assert.That(plate.IsPublic, Is.False, plate.FullName + " es publico.");
		}

		// ===== L4 · NADA SE LLAVEA POR UNA PLACA ==========================================

		// E23 lo prohibe en general -- ninguna coleccion con clave de valor -- y aca ademas es §1.4:
		// el Vin identifica, la placa describe. Un diccionario llaveado por placa es el agregado
		// re-llaveado sin que nadie lo haya decidido.
		[Test]
		public void NoCollectionAnywhereInTheDomainIsKeyedByAPlate()
		{
			var plateTypes = new List<Type> { typeof(LicensePlate), typeof(TheJoinKey) };

			var keyed = new List<string>();

			foreach (Type each in TheDomainsTypes())
				foreach (FieldInfo field in each.GetFields(
					BindingFlags.Instance | BindingFlags.Static
					| BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
				{
					if (!field.FieldType.IsGenericType) continue;

					Type shape = field.FieldType.GetGenericTypeDefinition();

					if (shape != typeof(Dictionary<,>) && shape != typeof(HashSet<>)) continue;

					if (plateTypes.Contains(field.FieldType.GetGenericArguments()[0]))
						keyed.Add(each.Name + "." + field.Name);
				}

			Assert.That(keyed, Is.Empty,
				"Algo se llavea por una placa: " + string.Join(", ", keyed) + ". El agregado se "
				+ "llavea por Vin y nada mas -- una placa cambia y un Vin no.");
		}

		// Y EL PERFIL RECIBE LA PLACA COMO TEXTO, no como carrier. Es E3 en el borde: el perform
		// delgado no puede construir un carrier, asi que lo que cruza es el texto recibido y la
		// escalada pasa ADENTRO del agregado.
		[Test]
		public void TheProfileTakesThePlateAsReceivedTextAndNeverAsACarrier()
		{
			// LA LLAMADA DIRECTA ES LA MITAD FUERTE, y LP-1 la puso aca. Compila solo si el miembro se
			// sigue llamando asi y sigue tomando (string, DateTime): un cambio de firma es un error del
			// COMPILADOR, mientras que resolverlo por nombre lo dejaba pasar hasta runtime -- y con los
			// tipos de los parametros cambiados por debajo lo dejaba pasar del todo (DEC-101).
			new VehicleProfile(ADealershipNarrative.TheVin).RecordPlateAssignment(
				"BQR-482", new DateTime(2026, 1, 15, 9, 0, 0, DateTimeKind.Utc));

			MethodInfo command = typeof(VehicleProfile).GetMethod(
				APlateOnTheDoorAndInTheDomain.TheCommand,
				BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

			Assert.That(command, Is.Not.Null,
				"VehicleProfile ya no declara '" + APlateOnTheDoorAndInTheDomain.TheCommand
				+ "'. ESTE nombre es el que el journal congela: lo que se guarda en cada entrada es el "
				+ "texto del script, y el texto nombra el miembro del DOMINIO.");

			ParameterInfo[] parameters = command.GetParameters();

			Assert.That(parameters, Has.Length.EqualTo(2),
				"El comando toma dos parametros y los dos son planos. La lista tambien queda "
				+ "congelada: una entrada vieja replaya su propio texto contra la firma de hoy.");

			Assert.That(parameters[0].ParameterType, Is.EqualTo(typeof(string)));
			Assert.That(parameters[1].ParameterType, Is.EqualTo(typeof(DateTime)));

			Assert.That(command.ReturnType, Is.EqualTo(typeof(void)));
		}

		// EL ENSAMBLADO SE ALCANZA POR UN TIPO QUE SE NOMBRA, no por su nombre en una cadena: el
		// barrido tiene que cubrir los tipos que TODAVIA no existen, asi que sigue siendo reflexion --
		// pero ya no puede quedarse vacio en silencio porque alguien renombro el ensamblado.
		private static List<Type> TheDomainsTypes()
		{
			var found = new List<Type>();

			foreach (Type each in typeof(VehicleProfile).Assembly.GetTypes())
				if (each.Namespace == typeof(VehicleProfile).Namespace) found.Add(each);

			return found;
		}
	}
}
