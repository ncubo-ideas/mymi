namespace Concesionario.Mcp.Tools
{
	/// <summary>
	/// Las clases de operacion que esta agenda modela. Conjunto CERRADO.
	/// </summary>
	///
	/// ES UN ENUM Y NO UN STRING, y eso es lo que compra el schema. Con un string, la lista de
	/// valores validos vive en la DESCRIPCION de la tool -- prosa que la IA tiene que leer,
	/// entender y copiar bien. Con un enum vive en el SCHEMA, que es un contrato: el cliente MCP
	/// no puede mandar un valor que no exista, y no hay que validar que exista ni explicar la
	/// lista en la prosa.
	///
	/// LOS NOMBRES SON LOS DEL DOMINIO, letra por letra, porque el nombre del miembro ES lo que
	/// cruza la puerta: DealershipCarAPI toma el NOMBRE del codigo, no el enum del dominio,
	/// que es internal y ninguna caratula puede nombrar. Asi que la traduccion es ToString() y no
	/// hay tabla que se pueda desincronizar.
	///
	/// Que esto sea una segunda escritura del conjunto cerrado es real, y lo que lo hace barato es
	/// que sea un catalogo de nombres y no un segundo conjunto cerrado, mas una prueba por
	/// reflexion que lo fije contra el enum del dominio miembro por miembro. Esa prueba es
	/// TheClosedSetAtTheAssistantsEdgeIsTheDomainsOwn, y va roja si al dominio se le agrega,
	/// se le quita o se le renombra una clase.
	public enum MaintenanceOperation
	{
		Inspection,
		FluidChange,
		FilterChange,
		WearPartReplacement,
		TimedComponent,
		SoftwareUpdate,
		RecallRemedy,
		Diagnostic,
		Repair,
	}

	internal static class TheOperationKind
	{
		/// <summary>
		/// El nombre con que esta clase cruza la puerta.
		/// </summary>
		///
		/// No hace falta el switch total-con-throw que llevaria un resolver desde string: un enum
		/// no puede traer un valor que no este, asi que la rama imposible no se escribe. Eso es
		/// exactamente lo que se gana al tipar.
		internal static string WireName(this MaintenanceOperation operation) => operation.ToString();

		/// <summary>
		/// Las nueve clases, en el orden en que el canon las escribe.
		/// </summary>
		internal static MaintenanceOperation[] All =>
			(MaintenanceOperation[])Enum.GetValues(typeof(MaintenanceOperation));
	}
}
