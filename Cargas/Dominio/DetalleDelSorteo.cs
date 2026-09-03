using System;

namespace Cargas.Dominio
{
	/// <summary>
	/// LO QUE SE SABE DE UN SORTEO, con la ausencia como caso.
	///
	/// Existe porque un BoletoEmitido puede llegar ANTES que el anuncio de su sorteo: los
	/// eventos vienen en el orden en que la estacion los publico, no en el que a este
	/// modelo le conviene. El dominio no puede reventar por eso — crea el sorteo sin
	/// detalles y el anuncio lo completa despues.
	///
	/// Mismo patron que FichaDelVehiculo.Vacia: FueAnunciado DECIDE y los demas REVIENTAN,
	/// porque devolver un premio vacio o una fecha en cero seria inventar.
	/// </summary>
	internal abstract class DetalleDelSorteo
	{
		internal static readonly DetalleDelSorteo SinAnunciar = new Ausente();

		internal static DetalleDelSorteo Anunciado(string nombre, string premio,
			DateTime fechaDelSorteo, Dinero montoPorBoleto) =>
			new Anuncio(nombre, premio, fechaDelSorteo, montoPorBoleto);

		/// <summary>Total: decide sin reventar. Consultarlo antes que cualquier otro.</summary>
		internal abstract bool FueAnunciado { get; }

		internal abstract string Nombre { get; }
		internal abstract string Premio { get; }
		internal abstract DateTime FechaDelSorteo { get; }
		internal abstract Dinero MontoPorBoleto { get; }

		/// <summary>Si ya se rifo, AL INSTANTE QUE SE LE PASE. El dominio no lee el reloj:
		/// si lo hiciera, el mismo journal reproducido manana daria otra respuesta.
		/// Un sorteo sin anunciar no puede estar vencido: no tiene fecha que vencer.</summary>
		internal abstract bool EstaVencidoAl(DateTime hoy);

		private sealed class Ausente : DetalleDelSorteo
		{
			internal override bool FueAnunciado => false;

			internal override string Nombre => throw SinAnuncio();
			internal override string Premio => throw SinAnuncio();
			internal override DateTime FechaDelSorteo => throw SinAnuncio();
			internal override Dinero MontoPorBoleto => throw SinAnuncio();

			internal override bool EstaVencidoAl(DateTime hoy) => false;

			private static ErrorDeDominio SinAnuncio() => new ErrorDeDominio(
				"Este sorteo todavia no fue anunciado: consultar antes FueAnunciado. "
				+ "Sus boletos existen; su premio y su fecha no se saben.");
		}

		private sealed class Anuncio : DetalleDelSorteo
		{
			private readonly string nombre, premio;
			private readonly DateTime fechaDelSorteo;
			private readonly Dinero montoPorBoleto;

			internal Anuncio(string nombre, string premio, DateTime fechaDelSorteo,
				Dinero montoPorBoleto)
			{
				ArgumentNullException.ThrowIfNull(montoPorBoleto);

				if (fechaDelSorteo == default)
					throw new ErrorDeDominio("Un sorteo sin fecha no se puede vencer ni esperar.");

				// El monto es la REGLA: con cero se divide por cero, y con negativo una
				// carga daria infinitos boletos. Se para aca y no solo en la frontera,
				// para que el invariante viva donde se lo puede sostener.
				if (montoPorBoleto.Monto <= 0m)
					throw new ErrorDeDominio(
						"El monto por boleto tiene que ser mayor que cero: con cero, cada "
						+ "carga daria infinitos boletos.");

				this.nombre = nombre ?? "";
				this.premio = premio ?? "";
				this.fechaDelSorteo = fechaDelSorteo;
				this.montoPorBoleto = montoPorBoleto;
			}

			internal override bool FueAnunciado => true;
			internal override string Nombre => nombre;
			internal override string Premio => premio;
			internal override DateTime FechaDelSorteo => fechaDelSorteo;
			internal override Dinero MontoPorBoleto => montoPorBoleto;

			internal override bool EstaVencidoAl(DateTime hoy) => hoy.Date > fechaDelSorteo.Date;
		}
	}
}
