using System;

namespace Concesionario.Dominio
{
	/// <summary>
	/// Una estadia con entrada y sin salida: el vehiculo esta adentro y todavia no lo han entregado.
	/// No es un error ni una ausencia -- es un TODAVIA NO, y por eso es un tipo y no una salida
	/// nula.
	/// </summary>
	internal sealed class AnOpenStay : AStay
	{
		/// <summary>Se abre con lo que toda estadia tiene: donde, por que y desde cuando.</summary>
		internal AnOpenStay(string branch, StaySubject subject, DateTime enteredAt)
			: base(branch, subject, enteredAt)
		{
		}

		/// <summary>Si.</summary>
		internal override bool IsOpen()
		{
			return true;
		}

		/// <summary>Ninguno todavia: los dias llegan con la salida.</summary>
		internal override bool Covers(StayDay day)
		{
			return false;
		}

		/// <summary>
		/// Ninguno todavia, por lo mismo. Un periodo dicho en dias no la alcanza: decidir si su
		/// instante de entrada cae adentro pediria la zona que este registro no tiene.
		/// </summary>
		internal override bool OverlapsDays(StayDay from, StayDay to)
		{
			return false;
		}

		/// <summary>Adentro desde que entro, y sin salida que lo termine.</summary>
		internal override bool IsInsideAt(DateTime asOf)
		{
			return EnteredAt <= asOf;
		}

		/// <summary>Sigue abierta, por su codigo.</summary>
		internal override string TheShapeItHas()
		{
			return StayShape.StillOpen;
		}

		/// <summary>
		/// La estadia abierta como forma y valores: donde esta y desde cuando, sin inventar una salida
		/// ni ningun dia.
		/// </summary>
		internal override AStayClause TheClause()
		{
			return AStayClause.StillOpen(Branch, Subject.Value, TheInstant(EnteredAt));
		}

		/// <summary>
		/// Cierra la estadia con su instante de salida y los dias que la sucursal dice que cubrio, y
		/// devuelve la estadia CERRADA: al cerrarse, la identidad de la abierta se disuelve.
		/// </summary>
		internal AClosedStay CloseAt(DateTime collectedAt, StayDay[] days)
		{
			return new AClosedStay(Branch, Subject, EnteredAt, collectedAt, days);
		}
	}
}
