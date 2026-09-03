using System;

namespace Cargas.Dominio
{
	/// <summary>
	/// LA MEMBRESIA DE SONIA CON LA ESTACION, con la ausencia como caso.
	///
	/// La emite la estacion, igual que los puntos y los boletos: la app no puede
	/// inventarse un numero de socio, porque el numero que no coincide con el de ellos no
	/// acredita nada y ademas se ve igual de bien impreso.
	///
	/// Mismo patron que DetalleDelSorteo.SinAnunciar: EstaAfiliada DECIDE y los demas
	/// REVIENTAN. Que la app corra sin membresia no es un caso raro — es el estado de
	/// cualquiera que abre la app antes de que llegue el primer evento.
	/// </summary>
	internal abstract class Membresia
	{
		internal static readonly Membresia SinAfiliar = new Ausente();

		internal static Membresia Emitida(string numero, string nombre, DateTime desde) =>
			new Emision(new NumeroDeSocio(numero), nombre, desde);

		/// <summary>Total: decide sin reventar. Consultarlo antes que cualquier otro.</summary>
		internal abstract bool EstaAfiliada { get; }

		internal abstract string Numero { get; }
		internal abstract string Nombre { get; }
		internal abstract DateTime Desde { get; }

		private sealed class Ausente : Membresia
		{
			internal override bool EstaAfiliada => false;

			internal override string Numero => throw SinMembresia();
			internal override string Nombre => throw SinMembresia();
			internal override DateTime Desde => throw SinMembresia();

			private static ErrorDeDominio SinMembresia() => new ErrorDeDominio(
				"Todavia no llego la membresia de la estacion: consultar antes EstaAfiliada. "
				+ "Un numero de socio inventado aca no acredita nada.");
		}

		private sealed class Emision : Membresia
		{
			private readonly NumeroDeSocio numero;
			private readonly string nombre;
			private readonly DateTime desde;

			internal Emision(NumeroDeSocio numero, string nombre, DateTime desde)
			{
				ArgumentNullException.ThrowIfNull(numero);

				if (desde == default)
					throw new ErrorDeDominio("Una membresia sin fecha no dice desde cuando es socia.");

				this.numero = numero;
				this.nombre = nombre ?? "";
				this.desde = desde;
			}

			internal override bool EstaAfiliada => true;
			internal override string Numero => numero.Texto;
			internal override string Nombre => nombre;
			internal override DateTime Desde => desde;
		}
	}
}
