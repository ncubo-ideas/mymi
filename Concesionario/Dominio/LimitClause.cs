using System;

namespace Concesionario.Dominio
{
	/// <summary>
	/// LAS CLAUSULAS que una dimension puede aportar al motivo de una operacion, por su codigo. La
	/// dimension que la ley no tiene no aporta ninguna, y esa ausencia se dice con la cadena vacia.
	/// </summary>
	internal static class LimitClause
	{
		/// <summary>Se conto y todavia no se llego al limite.</summary>
		internal const string BeforeTheLimit = "BeforeTheLimit";

		/// <summary>Se conto y se llego al limite exacto.</summary>
		internal const string AtTheLimit = "AtTheLimit";

		/// <summary>Se conto y se paso del limite.</summary>
		internal const string PastTheLimit = "PastTheLimit";

		/// <summary>No hay ninguna lectura usable de donde contar.</summary>
		internal const string AbsentReading = "AbsentReading";

		/// <summary>El limite y las mediciones estan en unidades distintas.</summary>
		internal const string UnitMismatch = "UnitMismatch";

		/// <summary>El perfil de uso declarado ignora los limites de esta dimension.</summary>
		internal const string UsageProfile = "UsageProfile";
	}
}
