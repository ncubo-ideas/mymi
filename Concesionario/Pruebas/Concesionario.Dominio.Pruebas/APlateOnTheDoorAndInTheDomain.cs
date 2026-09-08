using System;
using System.Collections.Generic;
using System.Reflection;
using Concesionario.App;
using NUnit.Framework;

namespace Concesionario.Dominio.Pruebas
{
	// EL ANDAMIO DE LP-0, Y YA SE CAYO ENTERO -- el dominio en LP-1 y la puerta en LP-2. DEC-101.
	//
	// E4 pide las pruebas ANTES de que exista el dominio. En este modulo hay UN solo proyecto de
	// pruebas, asi que una prueba que nombre un miembro que todavia no existe deja la SUITE ENTERA sin
	// compilar -- las 2000 y pico que ya estan verdes incluidas -- y LP-0 no puede certificar nada, ni
	// la sesion del usuario que trabaja en el mismo arbol. Se entro por REFLEXION: compilaba, se ponia
	// roja en runtime, y el rojo NOMBRABA el miembro que faltaba.
	//
	// LO QUE LP-1 BAJO: TheKeyOf, YieldsAKey y TheRefusalOf son ahora llamadas DIRECTAS a TheJoinKey,
	// y los tipos del dominio se nombran con typeof en vez de resolverse por cadena.
	//
	// LO QUE LP-2 BAJO: los seis miembros de LA PUERTA. Ya existen, asi que se nombran, y con ellos se
	// fueron el Call y el Unwrapped que envolvian la reflexion. Una reflexion que sobrevive a un verde
	// es un instrumento mas debil para siempre -- un cambio de firma que el compilador habria atrapado
	// se vuelve un MissingMethodException, y un miembro resuelto por nombre sigue pasando cuando le
	// cambian los TIPOS de los parametros por debajo.
	//
	// LO QUE SE QUEDA POR REFLEXION Y NO ES ANDAMIO: TheDoorsMember y TheDoorDeclares, que ENUMERAN los
	// miembros de la puerta para afirmar una propiedad sobre TODOS. Eso es un aparejo -- la censa no se
	// puede escribir de otra forma -- y en un diff se ve igual que un andamio, que es por que DEC-101
	// esta escrita.
	//
	// Y NO CONFUNDIR NADA DE ESTO CON TheDoorRecognisesTheNamesOfEveryClosedSetItSpeaks, que entra por
	// reflexion y tiene razon PARA SIEMPRE: Concesionario.App no declara ningun InternalsVisibleTo, asi
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

		// ===== LA PUERTA, POR LLAMADA DIRECTA =============================================

		// LP-2 BAJO ESTE ANDAMIO, que es la otra mitad de la obligacion de DEC-101. Los seis miembros
		// existen, asi que se nombran y el COMPILADOR defiende cada firma: cambiarle el tipo a un
		// parametro deja de compilar, mientras que resolverlo por nombre lo dejaba pasar del todo.

		internal static void RecordAssignment(
			DealershipCarAPI door, string plate, DateTime assignedAt)
		{
			door.RecordPlateAssignment(plate, assignedAt);
		}

		internal static bool IsPublished(DealershipCarAPI door, DateTime asOf)
		{
			return door.IsAPlatePublished(asOf);
		}

		internal static string Describe(DealershipCarAPI door, DateTime asOf)
		{
			return door.DescribePlate(asOf);
		}

		internal static string AsPublished(DealershipCarAPI door, DateTime asOf)
		{
			return door.ReadPlateAsPublished(asOf);
		}

		internal static string JoinKeyAsOf(DealershipCarAPI door, DateTime asOf)
		{
			return door.ReadPlateJoinKey(asOf);
		}

		// LAS FILAS, columna por columna. El DTO son arreglos primitivos paralelos -- la forma de
		// ShopStayRecord y la que §9 pide -- y cada nombre de columna de abajo es una propiedad que
		// el compilador resuelve: borrarla o renombrarla ya no se descubre en runtime.
		internal static string[] AssignmentColumn(DealershipCarAPI door, string column)
		{
			PlateAssignmentRecord rows = door.PlateAssignments();

			if (column == "AsPublished") return rows.AsPublished;
			if (column == "JoinKey") return rows.JoinKey;
			if (column == "AssignedAt") return rows.AssignedAt;
			if (column == "IsCurrent") return rows.IsCurrent;

			Assert.Fail("El registro de asignaciones no tiene la columna '" + column + "'. Las cuatro "
				+ "que §9 pide son el texto publicado, la llave, el instante y cual es la vigente.");

			return null;
		}

		// ===== EL DOMINIO, POR LLAMADA DIRECTA ============================================

		// LP-1 BAJO ESTE ANDAMIO. El proyecto de pruebas esta en el InternalsVisibleTo del dominio,
		// asi que los tres miembros se nombran y el compilador es quien defiende la firma: si
		// TheJoinKey.Of dejara de tomar un string, esto no compila -- por reflexion habria seguido
		// verde hasta que alguien leyera el rojo de otra prueba.

		// La llave de union de un texto recibido. Total con reventon: un texto que no deja nada al
		// normalizar no tiene llave.
		internal static string TheKeyOf(string received)
		{
			return TheJoinKey.Of(received).Value;
		}

		// EL PREDICADO TOTAL. Contesta para TODO texto, incluidos los que Of rechaza -- que es la
		// mitad que una propiedad sobre LicensePlate no podria contestar, porque para llegar a ella
		// hay que construir una placa primero.
		internal static bool YieldsAKey(string received)
		{
			return TheJoinKey.CanBeMadeFrom(received);
		}

		// El rechazo llega TAL CUAL: sin reflexion de por medio no hay TargetInvocationException que
		// desenvolver, asi que lo que se atrapa es la excepcion del dominio y nada mas.
		internal static Exception TheRefusalOf(string received)
		{
			return Assert.Catch(
				() => TheJoinKey.Of(received),
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

	}
}
