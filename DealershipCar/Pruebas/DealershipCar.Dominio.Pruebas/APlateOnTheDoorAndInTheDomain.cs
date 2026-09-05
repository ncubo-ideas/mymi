using System;
using System.Collections.Generic;
using System.Reflection;
using DealershipCar.App;
using NUnit.Framework;

namespace DealershipCar.Dominio.Pruebas
{
	// EL ANDAMIO DE LP-0, Y SE CAE EN LP-1 Y LP-2. DEC-101.
	//
	// E4 pide las pruebas ANTES de que exista el dominio. En este modulo hay UN solo proyecto de
	// pruebas, asi que una prueba que nombre un miembro que todavia no existe deja la SUITE ENTERA sin
	// compilar -- las 2000 y pico que ya estan verdes incluidas -- y LP-0 no puede certificar nada, ni
	// la sesion del usuario que trabaja en el mismo arbol. Se entra por REFLEXION: compila hoy, se pone
	// roja en runtime, y el rojo NOMBRA el miembro que falta.
	//
	// LA OBLIGACION QUE VIENE CON ESO: cuando LP-1 y LP-2 escriban los miembros, ESTE ARCHIVO se
	// reescribe a llamadas directas. La reflexion que sobreviva a un verde es un instrumento mas debil
	// para siempre -- un cambio de firma que el compilador habria atrapado se vuelve un
	// MissingMethodException, y un miembro resuelto por nombre sigue pasando cuando le cambian los
	// TIPOS de los parametros por debajo.
	//
	// Y NO CONFUNDIR ESTO CON TheDoorRecognisesTheNamesOfEveryClosedSetItSpeaks, que entra por
	// reflexion y tiene razon PARA SIEMPRE: DealershipCar.App no declara ningun InternalsVisibleTo, asi
	// que WireName no se puede nombrar desde aca ni despues. Aquello es un aparejo; esto es un andamio,
	// y en un diff se ven iguales.
	//
	// LO QUE DE VERDAD QUEDA CONGELADO, RE-DERIVADO EN LP-0 CONTRA SPIKES-OS2 S5 Y CONTRA LA PUERTA:
	//
	//   el journal guarda EL TEXTO DEL SCRIPT de cada entrada mas sus argumentos, y la rehidratacion
	//   vuelve a ejecutar ese texto. Lo que el texto nombra es el miembro DEL DOMINIO
	//   -- "profile.RecordPlateAssignment(@plate, @assignedAt);" -- asi que lo congelado es el NOMBRE
	//   DEL MIEMBRO DEL DOMINIO y su lista de parametros. El nombre publico de la puerta NO aparece en
	//   ninguna entrada y no esta congelado; se mantiene igual por convencion y no por el journal.
	//
	//   S5 midio la mitad de esto: un cuerpo de script que nombra un metodo inexistente se atrapa en
	//   VALIDACION ESTATICA, al escribir, deja DOS entradas, y el motor loguea "Rehydration execution
	//   failed. EntryId=..." en cada arranque futuro. Renombrar el miembro despues es el MISMO fallo
	//   descubierto al LEER en vez de al escribir: las entradas viejas llevan el nombre viejo. Un tipo
	//   de parametro invalido, en cambio, se atrapa ANTES de escribir y no cuesta entrada.
	internal static class APlateOnTheDoorAndInTheDomain
	{
		// EL COMANDO. Este nombre y estos dos parametros son lo unico que el journal congela.
		internal const string TheCommand = "RecordPlateAssignment";

		// LAS CINCO LECTURAS de §9 del brief. Ninguna esta congelada por el journal -- una lectura no
		// escribe -- pero LP-2 las va a tomar de aca porque son las que estas pruebas nombran.
		internal const string IsAPlatePublished = "IsAPlatePublished";
		internal const string DescribePlate = "DescribePlate";
		internal const string ReadPlateAsPublished = "ReadPlateAsPublished";
		internal const string ReadPlateJoinKey = "ReadPlateJoinKey";
		internal const string PlateAssignments = "PlateAssignments";

		// EL TIPO DE EVENTO DEL FEED, y los dos campos del payload. Los nombres son ingles camelCase
		// como los de VehicleDelivered -- "deliveredAt", "reading", "unit" -- que es el patron que
		// LP-3 copia.
		internal const string TheEventType = "PlateAssigned";

		internal const string TheDomainAssembly = "DealershipCar.Dominio";

		internal const string TheJoinKeyType = TheDomainAssembly + ".TheJoinKey";
		internal const string TheLicensePlateType = TheDomainAssembly + ".LicensePlate";
		internal const string ThePlateAssignmentType = TheDomainAssembly + ".APlateAssignment";
		internal const string ThePlateOnRecordType = TheDomainAssembly + ".PlateOnRecord";
		internal const string TheAssignedPlateType = TheDomainAssembly + ".AnAssignedPlate";
		internal const string TheNoPlateOnRecordType = TheDomainAssembly + ".NoPlateOnRecord";

		// ===== LA PUERTA ==================================================================

		internal static void RecordAssignment(
			DealershipCarAPI door, string plate, DateTime assignedAt)
		{
			Call(door, TheCommand, plate, assignedAt);
		}

		internal static bool IsPublished(DealershipCarAPI door, DateTime asOf)
		{
			return (bool)Call(door, IsAPlatePublished, asOf);
		}

		internal static string Describe(DealershipCarAPI door, DateTime asOf)
		{
			return (string)Call(door, DescribePlate, asOf);
		}

		internal static string AsPublished(DealershipCarAPI door, DateTime asOf)
		{
			return (string)Call(door, ReadPlateAsPublished, asOf);
		}

		internal static string JoinKeyAsOf(DealershipCarAPI door, DateTime asOf)
		{
			return (string)Call(door, ReadPlateJoinKey, asOf);
		}

		// LAS FILAS. El DTO no existe todavia, asi que se lee por columnas: cada columna es un array
		// primitivo paralelo, que es la forma de ShopStayRecord y la que §9 pide.
		internal static string[] AssignmentColumn(DealershipCarAPI door, string column)
		{
			object rows = Call(door, PlateAssignments);

			PropertyInfo found = rows.GetType().GetProperty(column);

			Assert.That(found, Is.Not.Null,
				"El registro de asignaciones no tiene la columna '" + column + "'. Las cuatro que §9 "
				+ "pide son el texto publicado, la llave, el instante y cual es la vigente.");

			object value = found.GetValue(rows);

			if (value is string[] many) return many;

			Assert.Fail("La columna '" + column + "' no es un array primitivo. Un registro crudo son "
				+ "columnas paralelas: un carrier del dominio no puede cruzar la puerta.");

			return null;
		}

		// ===== EL DOMINIO =================================================================

		internal static Type ADomainType(string fullName)
		{
			Type found = Assembly.Load(new AssemblyName(TheDomainAssembly)).GetType(fullName);

			Assert.That(found, Is.Not.Null,
				"El dominio todavia no declara '" + fullName + "'. Es LP-1, y hasta entonces esta "
				+ "prueba tiene que estar roja: es E4.");

			return found;
		}

		// La llave de union de un texto recibido, por el tipo del dominio. Total con reventon: un
		// texto que no deja nada al normalizar no tiene llave.
		internal static string TheKeyOf(string received)
		{
			object made = CallStatic(ADomainType(TheJoinKeyType), "Of", received);

			PropertyInfo value = made.GetType().GetProperty(
				"Value", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

			Assert.That(value, Is.Not.Null,
				"TheJoinKey no expone el texto normalizado. Es lo unico que tiene que cruzar.");

			return (string)value.GetValue(made);
		}

		// EL PREDICADO TOTAL. Contesta para TODO texto, incluidos los que el constructor rechaza --
		// que es la mitad que una propiedad sobre LicensePlate no podria contestar, porque para
		// llegar a ella hay que construir una placa primero.
		internal static bool YieldsAKey(string received)
		{
			return (bool)CallStatic(ADomainType(TheJoinKeyType), "CanBeMadeFrom", received);
		}

		// El rechazo YA VIENE DESENVUELTO: Unwrapped lo hace en el sitio de la invocacion, asi que lo
		// que llega aca es la excepcion del dominio y no la de la reflexion.
		internal static Exception TheRefusalOf(string received)
		{
			return Assert.Catch(
				() => TheKeyOf(received),
				"'" + received + "' no deja nada al normalizar y tiene que ser rechazado.");
		}

		// ===== LA RESOLUCION, Y EL ROJO QUE NOMBRA LO QUE FALTA ===========================

		internal static MethodInfo TheDoorsMember(string name)
		{
			var found = new List<MethodInfo>();

			foreach (MethodInfo each in typeof(DealershipCarAPI).GetMethods(
				BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
				if (each.Name == name) found.Add(each);

			Assert.That(found, Has.Count.EqualTo(1),
				"La puerta tiene " + found.Count + " miembros llamados '" + name + "'. Con cero es "
				+ "LP-2 y esta prueba tiene que estar roja; con dos, afirmar sobre 'el' miembro seria "
				+ "elegir uno y no mirar el otro.");

			return found[0];
		}

		internal static bool TheDoorDeclares(string name)
		{
			foreach (MethodInfo each in typeof(DealershipCarAPI).GetMethods(
				BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
				if (each.Name == name) return true;

			return false;
		}

		private static object Call(DealershipCarAPI door, string name, params object[] arguments)
		{
			return Unwrapped(() => TheDoorsMember(name).Invoke(door, arguments));
		}

		private static object CallStatic(Type type, string name, params object[] arguments)
		{
			MethodInfo found = type.GetMethod(
				name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

			Assert.That(found, Is.Not.Null,
				"'" + type.Name + "' no declara '" + name + "'. Es LP-1.");

			return Unwrapped(() => found.Invoke(null, arguments));
		}

		// La reflexion envuelve TODA excepcion del cuerpo en TargetInvocationException. Sin
		// desenvolverla, una prueba que espera el rechazo del dominio veria siempre el mismo tipo de
		// la infraestructura y no podria decir cual rechazo fue.
		private static object Unwrapped(Func<object> invocation)
		{
			try
			{
				return invocation();
			}
			catch (TargetInvocationException wrapped)
			{
				if (wrapped.InnerException == null) throw;

				throw wrapped.InnerException;
			}
		}
	}
}
