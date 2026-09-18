using System;
using System.Collections.Generic;

namespace Cargas.Dominio
{
	/// <summary>
	/// UNA TOMBOLA, con sus boletos y su regla.
	///
	/// LA REGLA VIVE ACA -"un boleto por cada X"- y no en un tipo aparte, y eso resuelve
	/// gratis el problema del replay: cada sorteo tiene su propia ventana de tiempo, asi
	/// que la regla queda VERSIONADA EN EL TIEMPO sin ninguna maquinaria. Si manana la
	/// estacion cambia el monto, cambia con el sorteo siguiente y las cargas viejas
	/// reproducidas siguen dando lo mismo.
	///
	/// Un sorteo puede existir SIN ANUNCIAR: sus boletos llegaron antes que su anuncio.
	/// </summary>
	internal sealed class Sorteo
	{
		internal CodigoDeSorteo Codigo { get; private set; }
		internal DetalleDelSorteo Detalle { get; private set; }

		private readonly List<Boleto> boletos = new List<Boleto>();
		private readonly Dictionary<string, Boleto> porNumero =
			new Dictionary<string, Boleto>(StringComparer.OrdinalIgnoreCase);

		internal Sorteo(string codigo)
		{
			Codigo = new CodigoDeSorteo(codigo);
			Detalle = DetalleDelSorteo.SinAnunciar;
		}

		internal void Anunciar(string nombre, string premio, DateTime fechaDelSorteo,
			decimal montoPorBoleto, string moneda)
		{
			// Dos anuncios del mismo sorteo son un defecto del emisor, igual que dos
			// cargas con la misma transaccion: la guarda del evento ya paro los
			// reintentos, asi que esto no es un redelivery.
			if (Detalle.FueAnunciado)
				throw new ErrorDeDominio(
					$"El sorteo {Codigo.Texto} ya fue anunciado: la institucion emitio dos "
					+ "anuncios para la misma identidad.");

			Detalle = DetalleDelSorteo.Anunciado(nombre, premio, fechaDelSorteo,
				new Dinero(montoPorBoleto, Moneda.PorCodigo(moneda)));
		}

		internal bool ConoceBoleto(string numeroDeBoleto) =>
			!string.IsNullOrWhiteSpace(numeroDeBoleto)
			&& porNumero.ContainsKey(numeroDeBoleto.Trim());

		internal void Emitir(Boleto boleto)
		{
			ArgumentNullException.ThrowIfNull(boleto);

			// El numero ES la identidad, asi que repetirlo es el mismo defecto del emisor.
			if (porNumero.ContainsKey(boleto.Numero.Texto))
				throw new ErrorDeDominio(
					$"Ya hay un boleto {boleto.Numero.Texto}: la institucion emitio dos "
					+ "hechos con la misma identidad.");

			porNumero[boleto.Numero.Texto] = boleto;

			// En orden de emision, que es como se leen.
			int posicion = boletos.Count;
			while (posicion > 0 && boletos[posicion - 1].Fecha > boleto.Fecha) posicion--;
			boletos.Insert(posicion, boleto);
		}

		internal IReadOnlyList<Boleto> Boletos => boletos;
		internal int CantidadDeBoletos => boletos.Count;

		internal int BoletosDeLaTransaccion(string numeroDeTransaccion)
		{
			int cuantos = 0;
			foreach (Boleto boleto in boletos)
				if (boleto.EsDeLaTransaccion(numeroDeTransaccion)) cuantos++;
			return cuantos;
		}

		// ------------------------------------------------------------------ la regla

		/// <summary>Si la regla de este sorteo aplica a ese total. Total: decide sin
		/// reventar. NO aplica si el sorteo no se anuncio -no hay regla que aplicar- ni si
		/// la moneda es otra: el dominio no convierte, y una carga en dolares contra una
		/// regla en colones no tiene respuesta, tiene una pregunta mal hecha.</summary>
		internal bool LaReglaAplicaA(Dinero total)
		{
			if (total == null || !Detalle.FueAnunciado) return false;
			return Detalle.MontoPorBoleto.Moneda.EsLaMisma(total.Moneda);
		}

		/// <summary>Cuantos boletos DARIA la regla por ese total. Revienta si no aplica:
		/// consultar antes LaReglaAplicaA.</summary>
		internal int BoletosQueDaria(Dinero total)
		{
			if (!LaReglaAplicaA(total))
				throw new ErrorDeDominio(
					$"La regla del sorteo {Codigo.Texto} no aplica a ese total: consultar "
					+ "antes LaReglaAplicaA.");

			return (int)Math.Floor(total.Monto / Detalle.MontoPorBoleto.Monto);
		}

		/// <summary>Lo que sobro y NO se acumula para la proxima compra. Es la mitad de la
		/// regla que la gente no espera, asi que la app tiene que poder decirla.</summary>
		internal decimal SobranteDe(Dinero total)
		{
			if (!LaReglaAplicaA(total))
				throw new ErrorDeDominio(
					$"La regla del sorteo {Codigo.Texto} no aplica a ese total: consultar "
					+ "antes LaReglaAplicaA.");

			return total.Monto - BoletosQueDaria(total) * Detalle.MontoPorBoleto.Monto;
		}

		// ------------------------------- proyecciones planas, para el print del actor

		internal string TextoDelCodigo => Codigo.Texto;
		internal bool FueAnunciado => Detalle.FueAnunciado;
		internal string NombreDelSorteo => Detalle.FueAnunciado ? Detalle.Nombre : "";
		internal string PremioDelSorteo => Detalle.FueAnunciado ? Detalle.Premio : "";
		internal DateTime FechaDelSorteo =>
			Detalle.FueAnunciado ? Detalle.FechaDelSorteo : default;

		internal decimal MontoPorBoleto =>
			Detalle.FueAnunciado ? Detalle.MontoPorBoleto.Monto : 0m;

		internal string MonedaPorBoleto =>
			Detalle.FueAnunciado ? Detalle.MontoPorBoleto.CodigoDeMoneda : "";
	}
}
