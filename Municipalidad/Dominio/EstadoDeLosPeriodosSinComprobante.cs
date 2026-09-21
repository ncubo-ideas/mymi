using System;
using System.Collections.Generic;

namespace Municipalidad.Dominio
{
	/// <summary>
	/// Que dice el cotejo entre una consulta y lo que la app vio pagar, y cuando no dice nada, POR
	/// QUE. Tres hojas y no una con un motivo adentro: cada razon es su propio tipo.
	/// <para>
	/// La ausencia de aca es de la otra clase que la de la serie de un rubro: no hay consulta de ese
	/// dia es CONTINGENTE -- deja de serlo en cuanto se registre una -- mientras que un rubro que no
	/// existe no se cura. Por eso la pregunta total de la base es si se PUEDE decir, y no si aplica.
	/// </para>
	/// <para>
	/// CADA HOJA DICE SU PROPIA FRASE, y la dice sobre el dia con que se la pidio. Las dos que traen
	/// cero periodos son las que una caratula confunde mas facil, asi que sus frases no se parecen:
	/// una dice que no hubo con que cotejar y la otra que se cotejo y salio limpia.
	/// </para>
	/// </summary>
	internal abstract class EstadoDeLosPeriodosSinComprobante
	{
		/// <summary>El dia de consulta con que se pidio el cotejo. Es el SUJETO de la frase.</summary>
		internal DateTime FechaDeLaConsulta { get; }

		protected EstadoDeLosPeriodosSinComprobante(DateTime fechaDeLaConsulta)
		{
			FechaDeLaConsulta = fechaDeLaConsulta.Date;
		}

		/// <summary>Ese dia, dicho como dia de calendario.</summary>
		internal DateTime DiaDeLaConsulta { get { return FechaDeLaConsulta.Date; } }

		/// <summary>Si hay una consulta de ese dia con que cotejar. TOTAL.</summary>
		internal abstract bool SePuedeDecir { get; }

		/// <summary>Si el cotejo destapa al menos un periodo sin comprobante. TOTAL.</summary>
		internal abstract bool HayPeriodos { get; }

		/// <summary>El nombre de este estado, que es como cruza a una caratula.</summary>
		internal abstract string Etiqueta { get; }

		/// <summary>
		/// CUANTOS PERIODOS DESTAPA EL COTEJO, y cero donde no destapa ninguno. Valor para un hueco.
		/// </summary>
		internal virtual int CuantosPeriodos { get { return 0; } }

		/// <summary>
		/// Resuelve el estado desde el dia pedido, si habia consulta de ese dia y los periodos que
		/// salieron. TOTAL.
		/// </summary>
		internal static EstadoDeLosPeriodosSinComprobante De(
			DateTime fechaDeLaConsulta, bool hayConsultaDeEseDia,
			IReadOnlyList<PeriodoSinComprobante> periodos)
		{
			ArgumentNullException.ThrowIfNull(periodos);
			if (!hayConsultaDeEseDia) return new NoHayConsultaDeEseDia(fechaDeLaConsulta);
			return periodos.Count > 0
				? new LaConsultaDestapaPeriodos(fechaDeLaConsulta, periodos.Count)
				: (EstadoDeLosPeriodosSinComprobante)new LaConsultaNoDestapaNada(fechaDeLaConsulta);
		}
	}

	/// <summary>
	/// No hay consulta de ese dia, asi que no hay con que cotejar. No es que este todo en orden: es
	/// que la pregunta no se puede contestar todavia, y se puede contestar en cuanto entre una.
	/// </summary>
	internal sealed class NoHayConsultaDeEseDia : EstadoDeLosPeriodosSinComprobante
	{
		internal NoHayConsultaDeEseDia(DateTime fechaDeLaConsulta) : base(fechaDeLaConsulta) { }

		internal override bool SePuedeDecir { get { return false; } }
		internal override bool HayPeriodos { get { return false; } }
		internal override string Etiqueta { get { return "sin-consulta"; } }
	}

	/// <summary>
	/// La consulta de ese dia se cotejo y no destapo nada: todo lo que el portal da por pagado, la
	/// app lo vio pagar. Cero periodos es la respuesta.
	/// </summary>
	internal sealed class LaConsultaNoDestapaNada : EstadoDeLosPeriodosSinComprobante
	{
		internal LaConsultaNoDestapaNada(DateTime fechaDeLaConsulta) : base(fechaDeLaConsulta) { }

		internal override bool SePuedeDecir { get { return true; } }
		internal override bool HayPeriodos { get { return false; } }
		internal override string Etiqueta { get { return "nada-sin-comprobante"; } }
	}

	/// <summary>La consulta de ese dia destapa periodos que la app no vio pagar.</summary>
	internal sealed class LaConsultaDestapaPeriodos : EstadoDeLosPeriodosSinComprobante
	{
		/// <summary>Cuantos periodos destapa el cotejo, que es lo que la frase cuenta.</summary>
		internal override int CuantosPeriodos { get { return cuantosPeriodos; } }

		private readonly int cuantosPeriodos;

		internal LaConsultaDestapaPeriodos(DateTime fechaDeLaConsulta, int cuantosPeriodos)
			: base(fechaDeLaConsulta)
		{
			this.cuantosPeriodos = cuantosPeriodos;
		}

		internal override bool SePuedeDecir { get { return true; } }
		internal override bool HayPeriodos { get { return true; } }
		internal override string Etiqueta { get { return "hay-periodos-sin-comprobante"; } }
	}
}
