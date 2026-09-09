using System;

namespace Municipalidad.Dominio
{
	// UN PERIODO QUE FIGURA PAGADO Y DEL QUE LA APP NO TIENE COMPROBANTE.
	//
	// M8, propuesto: SE DICE, NO SE TAPA. La consulta de hoy no debe nada anterior a X,
	// asi que lo anterior a X esta pagado -en ventanilla, en el banco, donde sea-; pero
	// la app no vio el recibo. Contarlo como historia seria inventarla; ignorarlo seria
	// un hueco invisible. Hueco 3, con dueno: la persona sabe como paga.
	//
	// Carrier plano: rubro y periodo, para el print.
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

		// Hojas planas para el print (regla 13).
		internal string NombreDelRubro { get { return Rubro.Nombre; } }
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
