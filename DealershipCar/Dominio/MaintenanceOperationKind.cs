using System;

namespace DealershipCar.Dominio
{
	// QUE OPERACION ES ESTA. Conjunto cerrado del canon (§3 del slice), comparado por
	// referencia: existen exactamente estas nueve instancias y ninguna mas.
	//
	// El enum de al lado NO es una duplicacion del conjunto: es su FRONTERA. El conjunto vive
	// adentro y se compara por identidad; el codigo es lo que cruza el wire, y las dos mitades
	// se unen en OfCode, que se resuelve UNA sola vez al entrar (E12, E32).
	//
	// POR QUE UN ENUM Y NO UN STRING: para una familia chica, fija y elegida desde el wire, la
	// guia prefiere el enum (E16, E39, E40) -- cerrado, con chequeo de tipo, sin el modo de
	// falla del string, que es que el wire pueda nombrar algo que no existe.
	//
	// Y COMO CRUZA DE VERDAD -- MEDIDO en T0, spike S2: el motor RECHAZA a proposito el tipo
	// enum concreto como tipo de parametro del DSL. La forma soportada es typeof(Enum)
	// llevando el NOMBRE del miembro como string, que el call site del script coacciona. La
	// firma del dominio se queda con el enum; lo que cambia es solo la declaracion del
	// parametro en DealershipCarAPI. Dos propiedades que conviene tener a proposito: el
	// journal guarda "FluidChange" y no "2", asi que renumerar el enum no puede reinterpretar
	// la historia en silencio; y el proyecto que llama nunca toma una referencia sobre un tipo
	// del dominio solo para nombrar una constante.
	//
	// Por eso los nombres de los dos lados tienen que coincidir EXACTAMENTE, y hay una prueba
	// que lo afirma: si se despegan, la coercion falla en runtime -- y un error de runtime en
	// un script deja entradas permanentes en el journal (spike S5).

	/// <summary>
	/// El codigo con el que una MaintenanceOperationKind cruza el wire hacia adentro.
	/// Cada miembro nombra al miembro homonimo del conjunto cerrado, donde esta documentado
	/// que significa; se resuelve con <see cref="MaintenanceOperationKind.OfCode"/>.
	/// </summary>
	internal enum OperationKindCode
	{
		/// <summary>Vease <see cref="MaintenanceOperationKind.Inspection"/>.</summary>
		Inspection,

		/// <summary>Vease <see cref="MaintenanceOperationKind.FluidChange"/>.</summary>
		FluidChange,

		/// <summary>Vease <see cref="MaintenanceOperationKind.FilterChange"/>.</summary>
		FilterChange,

		/// <summary>Vease <see cref="MaintenanceOperationKind.WearPartReplacement"/>.</summary>
		WearPartReplacement,

		/// <summary>Vease <see cref="MaintenanceOperationKind.TimedComponent"/>.</summary>
		TimedComponent,

		/// <summary>Vease <see cref="MaintenanceOperationKind.SoftwareUpdate"/>.</summary>
		SoftwareUpdate,

		/// <summary>Vease <see cref="MaintenanceOperationKind.RecallRemedy"/>.</summary>
		RecallRemedy,

		/// <summary>Vease <see cref="MaintenanceOperationKind.Diagnostic"/>.</summary>
		Diagnostic,

		/// <summary>Vease <see cref="MaintenanceOperationKind.Repair"/>.</summary>
		Repair,
	}

	/// <summary>
	/// Que operacion de mantenimiento es esta. Conjunto cerrado: las unicas instancias que
	/// existen son las nueve declaradas aca, y dos son la misma solo si son el mismo objeto.
	/// Es el QUE de la operacion; cuando vence lo decide su <see cref="IntervalRule"/>.
	/// </summary>
	internal sealed class MaintenanceOperationKind
	{
		/// <summary>Revisar sin necesariamente cambiar nada: el mecanico mira y reporta.</summary>
		internal static readonly MaintenanceOperationKind Inspection =
			new MaintenanceOperationKind("Inspection");

		/// <summary>Cambio de un fluido -- aceite, refrigerante, freno.</summary>
		internal static readonly MaintenanceOperationKind FluidChange =
			new MaintenanceOperationKind("FluidChange");

		/// <summary>Cambio de un filtro.</summary>
		internal static readonly MaintenanceOperationKind FilterChange =
			new MaintenanceOperationKind("FilterChange");

		/// <summary>
		/// Reemplazo de una pieza que se gasta con el uso -- pastillas, llantas, correas.
		/// </summary>
		internal static readonly MaintenanceOperationKind WearPartReplacement =
			new MaintenanceOperationKind("WearPartReplacement");

		/// <summary>
		/// Componente que se reemplaza por tiempo cumplido y no por desgaste medido: vence
		/// aunque el vehiculo no se haya movido.
		/// </summary>
		internal static readonly MaintenanceOperationKind TimedComponent =
			new MaintenanceOperationKind("TimedComponent");

		/// <summary>Actualizacion de software de un modulo del vehiculo.</summary>
		internal static readonly MaintenanceOperationKind SoftwareUpdate =
			new MaintenanceOperationKind("SoftwareUpdate");

		/// <summary>
		/// El remedio de un llamado a revision del fabricante. Es la operacion que puede venir
		/// clasificada como de seguridad, y una de seguridad no se aplaza nunca (L6).
		/// </summary>
		internal static readonly MaintenanceOperationKind RecallRemedy =
			new MaintenanceOperationKind("RecallRemedy");

		/// <summary>Diagnostico: averiguar que pasa, antes de saber que hay que reparar.</summary>
		internal static readonly MaintenanceOperationKind Diagnostic =
			new MaintenanceOperationKind("Diagnostic");

		/// <summary>Reparacion de algo que ya fallo, no prevencion de algo que va a fallar.</summary>
		internal static readonly MaintenanceOperationKind Repair =
			new MaintenanceOperationKind("Repair");

		/// <summary>
		/// El nombre del miembro. Es lo unico que sale del conjunto hacia afuera: el agregado
		/// proyecta un escalar (ReadNextDueKind devuelve este texto), nunca el objeto (E24).
		/// </summary>
		internal string Name { get; }

		private MaintenanceOperationKind(string name)
		{
			Name = name;
		}

		/// <summary>
		/// Resuelve el codigo del wire al miembro del conjunto. FUNCION TOTAL CON EXCEPCION:
		/// un codigo que no nombra ningun miembro es un defecto de quien llama y revienta.
		/// Es la polaridad OPUESTA a una lectura con clave sobre un espacio -- ahi la ausencia
		/// es una respuesta legitima; aca, afuera del conjunto cerrado no hay miembro que
		/// devolver. Se resuelve UNA vez al entrar; adentro nadie vuelve a validar.
		/// </summary>
		internal static MaintenanceOperationKind OfCode(OperationKindCode code)
		{
			switch (code)
			{
				case OperationKindCode.Inspection: return Inspection;
				case OperationKindCode.FluidChange: return FluidChange;
				case OperationKindCode.FilterChange: return FilterChange;
				case OperationKindCode.WearPartReplacement: return WearPartReplacement;
				case OperationKindCode.TimedComponent: return TimedComponent;
				case OperationKindCode.SoftwareUpdate: return SoftwareUpdate;
				case OperationKindCode.RecallRemedy: return RecallRemedy;
				case OperationKindCode.Diagnostic: return Diagnostic;
				case OperationKindCode.Repair: return Repair;
			}

			throw new DomainException(
				$"'{code}' no nombra ninguna clase de operacion de mantenimiento. El conjunto es "
				+ "cerrado: afuera no hay miembro, asi que esto no es una respuesta vacia sino un "
				+ "defecto de quien llama.");
		}
	}
}
