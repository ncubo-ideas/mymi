using System;

namespace Cargas.Dominio
{
	/// <summary>
	/// UN SORTEO CON SUS BOLETOS, ya juzgado contra un dia. El "hoy" lo pone quien pregunta:
	/// el dominio no lee el reloj.
	/// </summary>
	internal sealed class BoletosDelSorteo
	{
		/// <summary>La estacion todavia no publico el anuncio de este sorteo.</summary>
		internal const string TodaviaNoSeAnuncio = "sin-anunciar";

		/// <summary>La fecha ya paso: se rifo.</summary>
		internal const string YaSeRifo = "ya-se-rifo";

		/// <summary>Se rifa hoy mismo.</summary>
		internal const string EsHoy = "es-hoy";

		/// <summary>Falta, y cuantos dias faltan es un valor de la frase.</summary>
		internal const string Faltan = "faltan-dias";

		internal string Codigo { get; private set; }
		internal string Nombre { get; private set; }
		internal string Premio { get; private set; }
		internal bool FueAnunciado { get; private set; }
		internal DateTime FechaDelSorteo { get; private set; }
		internal int CantidadDeBoletos { get; private set; }
		internal bool EstaVencido { get; private set; }
		internal int DiasParaElSorteo { get; private set; }

		/// <summary>El monto de la regla y su moneda, por separado: la cifra la escribe quien
		/// redacta, porque el separador de miles es una decision del idioma.</summary>
		internal decimal MontoPorBoleto { get; private set; }
		internal string MonedaPorBoleto { get; private set; }

		internal BoletosDelSorteo(Sorteo sorteo, DateTime hoy)
		{
			ArgumentNullException.ThrowIfNull(sorteo);

			Codigo = sorteo.TextoDelCodigo;
			Nombre = sorteo.NombreDelSorteo;
			Premio = sorteo.PremioDelSorteo;
			FueAnunciado = sorteo.FueAnunciado;
			FechaDelSorteo = sorteo.FechaDelSorteo;
			CantidadDeBoletos = sorteo.CantidadDeBoletos;
			MontoPorBoleto = sorteo.MontoPorBoleto;
			MonedaPorBoleto = sorteo.MonedaPorBoleto;
			EstaVencido = sorteo.Detalle.EstaVencidoAl(hoy);

			DiasParaElSorteo = FueAnunciado && !EstaVencido
				? (int)Math.Round((FechaDelSorteo.Date - hoy.Date).TotalDays)
				: 0;
		}

		/// <summary>LA CLAVE DE LA EXPLICACION: en cual de las cuatro situaciones esta.</summary>
		internal string ClaveDeLaExplicacion
		{
			get
			{
				if (!FueAnunciado) return TodaviaNoSeAnuncio;
				if (EstaVencido) return YaSeRifo;
				return DiasParaElSorteo > 0 ? Faltan : EsHoy;
			}
		}
	}
}
