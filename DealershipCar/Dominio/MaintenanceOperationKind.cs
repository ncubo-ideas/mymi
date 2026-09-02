using System;

namespace DealershipCar.Dominio
{
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
			new MaintenanceOperationKind("Inspection", "Revisión");

		/// <summary>Cambio de un fluido -- aceite, refrigerante, freno.</summary>
		internal static readonly MaintenanceOperationKind FluidChange =
			new MaintenanceOperationKind("FluidChange", "Cambio de fluido");

		/// <summary>Cambio de un filtro.</summary>
		internal static readonly MaintenanceOperationKind FilterChange =
			new MaintenanceOperationKind("FilterChange", "Cambio de filtro");

		/// <summary>
		/// Reemplazo de una pieza que se gasta con el uso -- pastillas, llantas, correas.
		/// </summary>
		internal static readonly MaintenanceOperationKind WearPartReplacement =
			new MaintenanceOperationKind("WearPartReplacement", "Cambio de pieza de desgaste");

		/// <summary>
		/// Componente que se reemplaza por tiempo cumplido y no por desgaste medido: vence
		/// aunque el vehiculo no se haya movido.
		/// </summary>
		internal static readonly MaintenanceOperationKind TimedComponent =
			new MaintenanceOperationKind("TimedComponent", "Componente por tiempo");

		/// <summary>Actualizacion de software de un modulo del vehiculo.</summary>
		internal static readonly MaintenanceOperationKind SoftwareUpdate =
			new MaintenanceOperationKind("SoftwareUpdate", "Actualización de software");

		/// <summary>
		/// El remedio de un llamado a revision del fabricante. Es la operacion que puede venir
		/// clasificada como de seguridad, y una de seguridad no se aplaza nunca.
		/// </summary>
		internal static readonly MaintenanceOperationKind RecallRemedy =
			new MaintenanceOperationKind("RecallRemedy", "Llamado a revisión");

		/// <summary>Diagnostico: averiguar que pasa, antes de saber que hay que reparar.</summary>
		internal static readonly MaintenanceOperationKind Diagnostic =
			new MaintenanceOperationKind("Diagnostic", "Diagnóstico");

		/// <summary>Reparacion de algo que ya fallo, no prevencion de algo que va a fallar.</summary>
		internal static readonly MaintenanceOperationKind Repair =
			new MaintenanceOperationKind("Repair", "Reparación");

		/// <summary>
		/// El nombre. Es lo unico que sale del conjunto hacia afuera; el objeto no se entrega.
		/// </summary>
		internal string Name { get; }

		/// <summary>
		/// Como se dice esta operacion en la frase que alguien lee. NO es lo que cruza la puerta
		/// ni lo que viaja en el wire: eso sigue siendo <see cref="Name"/>.
		/// </summary>
		internal string HowItIsSaid { get; }

		private MaintenanceOperationKind(string name, string howItIsSaid)
		{
			Name = name;
			HowItIsSaid = howItIsSaid;
		}

		/// <summary>
		/// Resuelve el codigo del wire al miembro, una sola vez al entrar. Total con excepcion: un
		/// codigo desconocido es un defecto de quien llama y revienta.
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
