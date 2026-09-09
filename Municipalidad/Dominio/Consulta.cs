using System;
using System.Collections.Generic;

namespace Municipalidad.Dominio
{
	/// <summary>
	/// La consulta de pendientes: la FOTO del estado de hoy en el portal, y por eso va FECHADA. Es un
	/// hecho de otra clase que el comprobante — la de manana sera otra y la de ayer no se puede
	/// recuperar —, asi que la historia de la app no sale de aca sino de los comprobantes.
	/// <para>
	/// Lo que si sale de aca es lo que queda puesto al cobro, y la evidencia de lo que NO esta
	/// pendiente: si la consulta de hoy no debe nada anterior a un periodo, lo anterior se pago, con o
	/// sin comprobante en la app.
	/// </para>
	/// </summary>
	internal sealed class Consulta
	{
		private readonly List<LineaPendiente> pendientes = new List<LineaPendiente>();

		internal DateTime Fecha { get; }

		internal Consulta(DateTime fecha) { Fecha = fecha.Date; }

		internal IReadOnlyList<LineaPendiente> Pendientes { get { return pendientes; } }
		internal int CuantosPendientes { get { return pendientes.Count; } }

		internal bool Trae(Rubro rubro, Periodo periodo)
		{
			foreach (LineaPendiente p in pendientes)
				if (p.EsDe(rubro, periodo)) return true;
			return false;
		}

		internal bool TraeAlgoDe(Rubro rubro)
		{
			foreach (LineaPendiente p in pendientes)
				if (p.Rubro.Es(rubro)) return true;
			return false;
		}

		internal void Agregar(LineaPendiente pendiente)
		{
			ArgumentNullException.ThrowIfNull(pendiente);
			if (Trae(pendiente.Rubro, pendiente.Periodo))
				throw new DomainException(
					"Esta consulta ya trae ese rubro para ese periodo. Preguntar Trae antes.");
			pendientes.Add(pendiente);
		}

		internal decimal TotalPendiente
		{
			get
			{
				decimal suma = 0m;
				foreach (LineaPendiente p in pendientes) suma += p.Total;
				return suma;
			}
		}

		/// <summary>
		/// El periodo pendiente MAS VIEJO de un rubro: todo lo anterior a el, el portal lo da por pagado.
		/// GUARDADO: preguntar TraeAlgoDe antes.
		/// </summary>
		internal Periodo PendienteMasViejoDe(Rubro rubro)
		{
			Periodo masViejo = null;
			foreach (LineaPendiente p in pendientes)
				if (p.Rubro.Es(rubro) && (masViejo == null || p.Periodo.EsAnteriorA(masViejo)))
					masViejo = p.Periodo;
			if (masViejo == null)
				throw new DomainException(
					"Esta consulta no trae nada de ese rubro. Preguntar TraeAlgoDe antes.");
			return masViejo;
		}
	}
}
