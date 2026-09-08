using System;

namespace Cargas.Dominio
{
	/// <summary>
	/// UN SORTEO CON SUS BOLETOS, ya juzgado contra un dia. El "hoy" lo pone quien pregunta:
	/// el dominio no lee el reloj.
	/// </summary>
	internal sealed class BoletosDelSorteo
	{
		internal string Codigo { get; private set; }
		internal string Nombre { get; private set; }
		internal string Premio { get; private set; }
		internal bool FueAnunciado { get; private set; }
		internal DateTime FechaDelSorteo { get; private set; }
		internal int CantidadDeBoletos { get; private set; }
		internal bool EstaVencido { get; private set; }
		internal int DiasParaElSorteo { get; private set; }
		internal string MontoPorBoletoExpresado { get; private set; }

		internal BoletosDelSorteo(Sorteo sorteo, DateTime hoy)
		{
			ArgumentNullException.ThrowIfNull(sorteo);

			Codigo = sorteo.TextoDelCodigo;
			Nombre = sorteo.NombreDelSorteo;
			Premio = sorteo.PremioDelSorteo;
			FueAnunciado = sorteo.FueAnunciado;
			FechaDelSorteo = sorteo.FechaDelSorteo;
			CantidadDeBoletos = sorteo.CantidadDeBoletos;
			MontoPorBoletoExpresado = sorteo.MontoPorBoletoExpresado;
			EstaVencido = sorteo.Detalle.EstaVencidoAl(hoy);

			DiasParaElSorteo = FueAnunciado && !EstaVencido
				? (int)Math.Round((FechaDelSorteo.Date - hoy.Date).TotalDays)
				: 0;
		}

		internal string ParaSonia
		{
			get
			{
				string cuantos = CantidadDeBoletos == 1 ? "1 boleto" : $"{CantidadDeBoletos} boletos";

				if (!FueAnunciado)
					return $"{cuantos} de un sorteo que la estación todavía no anunció";

				if (EstaVencido)
					return $"{cuantos} para el sorteo del {FechaDelSorteo:dd/MM/yyyy}, que ya se rifó";

				return $"{cuantos} para el sorteo del {FechaDelSorteo:dd/MM/yyyy}"
					+ (DiasParaElSorteo > 0 ? $", en {DiasParaElSorteo} días" : ", hoy");
			}
		}
	}
}
