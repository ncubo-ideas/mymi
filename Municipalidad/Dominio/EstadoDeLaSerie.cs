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
		/// CUANTAS LINEAS TRAE LA SERIE, y cero donde no trae ninguna. Es un valor para el hueco de una
		/// frase: cual hoja es lo dice <c>Etiqueta</c>.
		/// </summary>
		internal virtual int CuantasLineas { get { return 0; } }

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
	}

	/// <summary>El rubro existe y la serie trae lineas.</summary>
	internal sealed class ElRubroTieneHistoria : EstadoDeLaSerie
	{
		/// <summary>Cuantas lineas de comprobante trae la serie, que es lo que la frase cuenta.</summary>
		internal override int CuantasLineas { get { return cuantasLineas; } }

		private readonly int cuantasLineas;

		internal ElRubroTieneHistoria(string nombreDelRubro, int cuantasLineas) : base(nombreDelRubro)
		{
			this.cuantasLineas = cuantasLineas;
		}

		internal override bool Aplica { get { return true; } }
		internal override bool HayLineas { get { return true; } }
		internal override string Etiqueta { get { return "con-historia"; } }
	}
}
