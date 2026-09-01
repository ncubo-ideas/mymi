using System;

namespace Cargas.Dominio
{
	/// <summary>
	/// El "tu nivel de siempre" del vehiculo: la mediana de los tramos comparables y no
	/// sospechosos. Contra ella se juzga el tramo mas reciente, nunca contra un promedio
	/// nacional -la regla del no receta ciega-.
	/// </summary>
	// La ausencia es parte del tipo: con menos de dos tramos comparables no hay nivel
	// de siempre, y decirlo es informacion, no un error.
	internal sealed class LineaBaseDeRendimiento
	{
		internal static readonly LineaBaseDeRendimiento SinBase =
			new LineaBaseDeRendimiento(null, 0);

		internal static LineaBaseDeRendimiento De(Rendimiento mediana, int tramosQueLaSustentan) =>
			new LineaBaseDeRendimiento(mediana, tramosQueLaSustentan);

		private readonly Rendimiento mediana;

		internal int TramosQueLaSustentan { get; private set; }

		private LineaBaseDeRendimiento(Rendimiento mediana, int tramosQueLaSustentan)
		{
			this.mediana = mediana;
			TramosQueLaSustentan = tramosQueLaSustentan;
		}

		internal bool Hay => mediana != null;

		/// <summary>Revienta si no hay base: consultar antes Hay.</summary>
		internal Rendimiento Rendimiento =>
			mediana ?? throw new ErrorDeDominio(
				"Todavia no hay linea base: hacen falta al menos dos tramos comparables. "
				+ "Consultar antes Hay.");

		/// <summary>Total, para el print: cero cuando no hay base.</summary>
		internal decimal KilometrosPorLitro => mediana == null ? 0m : mediana.KilometrosPorLitro;
	}
}
