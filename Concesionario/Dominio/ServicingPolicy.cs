using System;

namespace Concesionario.Dominio
{
	/// <summary>
	/// La regla que el fabricante publico sobre hacerle servicio al vehiculo en otro taller, y que
	/// hay que hacer para conservar la cobertura -- o la constancia de que una revision no publico
	/// ninguna. Dice el alcance publicado y la posicion de este vehiculo frente a el; no dictamina
	/// que ninguna reclamacion se vaya a pagar, porque eso es del fabricante.
	/// </summary>
	internal abstract class ServicingPolicy
	{
		/// <summary>Todavia no se publico ninguna. Instancia unica, compartida.</summary>
		internal static readonly ServicingPolicy None = new NoServicingPolicy();

		/// <summary>
		/// La regla que si se publico, con lo que conserva la cobertura. Las dos mitades juntas o no
		/// es una regla: una regla sin que hacer al respecto deja a la persona sabiendo que tiene un
		/// problema y no que hacer con el.
		/// </summary>
		internal static ServicingPolicy Of(string rule, string whatPreservesCover)
		{
			return new AServicingPolicy(rule, whatPreservesCover);
		}

		/// <summary>Si hay una regla publicada. Total: contesta siempre.</summary>
		internal abstract bool Exists();

		/// <summary>
		/// La clausula de la regla, como forma y valores, con su texto tal como se publico. Revienta
		/// si no hay ninguna: hay que preguntar primero.
		/// </summary>
		internal abstract AServicingClause TheClauseItComposes(string revisionCode);

		/// <summary>
		/// La misma clausula CON la mitad recibida: donde esta parado este vehiculo frente a la regla,
		/// con los conteos tal como llegaron. Nada se guarda. Revienta si no hay regla publicada: sin
		/// mitad publicada no hay contra que pararlos.
		/// </summary>
		internal abstract AServicingClause TheClauseItComposesAgainst(
			string revisionCode, int externalServicesWithEvidence,
			int externalServicesWithoutEvidence, int missedMaintenanceCount);
	}
}
