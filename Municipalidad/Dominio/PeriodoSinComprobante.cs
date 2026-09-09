using System;

namespace Municipalidad.Dominio
{
	/// <summary>
	/// Un periodo que figura pagado y del que la app no tiene comprobante. SE DICE, NO SE TAPA: si la
	/// consulta de hoy no debe nada anterior a un periodo, lo anterior esta pagado — en ventanilla, en
	/// el banco, donde sea —, pero la app no vio el recibo. Contarlo como historia seria inventarla;
	/// ignorarlo seria un hueco invisible.
	/// </summary>
	internal sealed class PeriodoSinComprobante
	{
		internal Rubro Rubro { get; }
		internal Periodo Periodo { get; }

		internal PeriodoSinComprobante(Rubro rubro, Periodo periodo)
		{
			ArgumentNullException.ThrowIfNull(rubro);
			ArgumentNullException.ThrowIfNull(periodo);
			Rubro = rubro;
			Periodo = periodo;
		}

		/// <summary>El nombre del rubro, aplanado para que el print lo alcance en un solo nivel.</summary>
		internal string NombreDelRubro { get { return Rubro.Nombre; } }

		/// <summary>El periodo, aplanado por la misma razon.</summary>
		internal string EtiquetaDelPeriodo { get { return Periodo.Etiqueta; } }

		/// <summary>
		/// La frase que una persona lee sobre este periodo: de que periodo y de que rubro, y que lo que
		/// falta es EL COMPROBANTE y no el pago. Decirlo al reves acusaria a la persona de no pagar.
		/// </summary>
		internal string Dice()
		{
			return "La consulta da por pagado el período " + EtiquetaDelPeriodo + " de " + NombreDelRubro
				+ " y esta app no vio ese recibo. No dice que esté sin pagar: dice que el comprobante no "
				+ "le llegó a la app.";
		}
	}
}
