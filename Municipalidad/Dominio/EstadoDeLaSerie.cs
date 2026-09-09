using System;
using System.Collections.Generic;

namespace Municipalidad.Dominio
{
	/// <summary>
	/// Que dice la serie de un rubro, y cuando no dice nada, POR QUE. Tres hojas y no una con un
	/// motivo adentro: cada razon es su propio tipo y su nombre la dice.
	/// <para>
	/// Las dos preguntas son TOTALES y las contesta la base, asi que ningun consumidor pregunta de
	/// que tipo es esto. Un rubro que el catalogo no tiene es una ausencia PERMANENTE -- nada que
	/// llegue despues le da historia -- y un rubro conocido sin pagos es una respuesta de cero lineas.
	/// </para>
	/// <para>
	/// CADA HOJA DICE SU PROPIA FRASE, y la dice sobre el rubro con que se la pidio: el nombre viaja
	/// adentro y no se lo pasa quien pregunta, asi que una frase no puede nombrar otro rubro.
	/// </para>
	/// </summary>
	internal abstract class EstadoDeLaSerie
	{
		/// <summary>
		/// El nombre de rubro con que se pidio la serie, tal cual se pidio. Es el SUJETO de la frase, y
		/// para la hoja del rubro desconocido puede venir en blanco: ese es un nombre legal de pedir.
		/// </summary>
		internal string NombreDelRubro { get; }

		protected EstadoDeLaSerie(string nombreDelRubro)
		{
			NombreDelRubro = nombreDelRubro;
		}

		/// <summary>Si el rubro existe en el catalogo. TOTAL.</summary>
		internal abstract bool Aplica { get; }

		/// <summary>Si la serie trae al menos una linea. TOTAL.</summary>
		internal abstract bool HayLineas { get; }

		/// <summary>El nombre de este estado, que es como cruza a una caratula.</summary>
		internal abstract string Etiqueta { get; }

		/// <summary>
		/// La frase que una persona lee cuando esta serie trae lo que trae. TOTAL: las tres hojas
		/// dicen la suya, INCLUSO la que no aplica -- ahi la ausencia es justamente la respuesta a
		/// "por que no hay nada?", y callarla dejaria una pantalla vacia sin motivo.
		/// </summary>
		internal abstract string Dice();

		/// <summary>
		/// Resuelve el estado desde el nombre pedido y la serie que salio. TOTAL: cualquier nombre y
		/// cualquier serie dan uno de los tres.
		/// </summary>
		internal static EstadoDeLaSerie De(string nombreDelRubro, IReadOnlyList<LineaPagada> serie)
		{
			ArgumentNullException.ThrowIfNull(serie);
			if (!Rubro.EsUnNombreConocido(nombreDelRubro)) return new ElRubroNoExiste(nombreDelRubro);
			return serie.Count > 0
				? new ElRubroTieneHistoria(nombreDelRubro, serie.Count)
				: (EstadoDeLaSerie)new ElRubroNoTieneHistoria(nombreDelRubro);
		}
	}

	/// <summary>
	/// El nombre pedido no es de ningun rubro del catalogo. La ausencia es permanente: no hay pago
	/// que pueda darle historia a un rubro que no existe.
	/// </summary>
	internal sealed class ElRubroNoExiste : EstadoDeLaSerie
	{
		internal ElRubroNoExiste(string nombreDelRubro) : base(nombreDelRubro) { }

		internal override bool Aplica { get { return false; } }
		internal override bool HayLineas { get { return false; } }
		internal override string Etiqueta { get { return "rubro-desconocido"; } }

		/// <summary>
		/// Dice que el rubro no existe y CUALES si hay, leidos del catalogo y no de una lista escrita
		/// aqui. Un nombre en blanco es una peticion legal y tiene su propia clausula.
		/// </summary>
		internal override string Dice()
		{
			if (string.IsNullOrWhiteSpace(NombreDelRubro))
				return "No se pidió ningún rubro, así que no hay serie que mostrar. Los rubros que esta "
					+ "app conoce son " + string.Join(", ", Rubro.LosQueHay()) + ".";

			return "«" + NombreDelRubro + "» no es ninguno de los rubros que esta app conoce, así que no "
				+ "hay serie que mostrar. No es que falte información: ese rubro no existe en esta "
				+ "cuenta. Los que hay son " + string.Join(", ", Rubro.LosQueHay()) + ".";
		}
	}

	/// <summary>
	/// El rubro existe y la app no vio pagar nada de el. Cero lineas es la respuesta, y puede dejar
	/// de serlo en cuanto entre un recibo.
	/// </summary>
	internal sealed class ElRubroNoTieneHistoria : EstadoDeLaSerie
	{
		internal ElRubroNoTieneHistoria(string nombreDelRubro) : base(nombreDelRubro) { }

		internal override bool Aplica { get { return true; } }
		internal override bool HayLineas { get { return false; } }
		internal override string Etiqueta { get { return "sin-historia"; } }

		internal override string Dice()
		{
			return "Esta app no vio pagar nada de " + NombreDelRubro + ", así que su serie viene vacía. "
				+ "El rubro sí existe, y la serie deja de venir vacía en cuanto entre un recibo suyo.";
		}
	}

	/// <summary>El rubro existe y la serie trae lineas.</summary>
	internal sealed class ElRubroTieneHistoria : EstadoDeLaSerie
	{
		/// <summary>Cuantas lineas de comprobante trae la serie, que es lo que la frase cuenta.</summary>
		internal int CuantasLineas { get; }

		internal ElRubroTieneHistoria(string nombreDelRubro, int cuantasLineas) : base(nombreDelRubro)
		{
			CuantasLineas = cuantasLineas;
		}

		internal override bool Aplica { get { return true; } }
		internal override bool HayLineas { get { return true; } }
		internal override string Etiqueta { get { return "con-historia"; } }

		internal override string Dice()
		{
			return "La serie de " + NombreDelRubro + " trae " + CuantasLineas
				+ (CuantasLineas == 1 ? " línea" : " líneas") + " de comprobante, de la más vieja a la "
				+ "más reciente.";
		}
	}
}
