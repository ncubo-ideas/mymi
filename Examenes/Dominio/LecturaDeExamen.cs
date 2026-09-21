using System;

namespace Examenes.Dominio
{
	// UNA COSA MEDIDA DENTRO DE UN EXAMEN: que analito es, y que dio.
	//
	// Existe porque un examen NO es necesariamente un analito. Un colesterol si —una
	// hoja, un numero—, pero un urianalisis informa color, nitritos y sangre en la misma
	// hoja, y son tres analitos distintos con tres lecturas distintas.
	//
	// Antes eso viajaba aplanado adentro de ResultadoExpresado, o sea como UN string:
	// "ambar, nitritos negativo, sangre negativo". Aplanar es exactamente lo que este
	// dominio evita en todos los otros pisos, y el costo se veia en el cuadro historico:
	// un urianalisis ocupaba UNA fila con su string entero adentro, en vez de aportar a
	// las filas de sus tres analitos.
	//
	// Carrier por identidad: sin Equals, sin GetHashCode, no es llave de nada.
	internal sealed class LecturaDeExamen
	{
		internal Analito Analito { get; private set; }

		internal Magnitud Resultado { get; private set; }

		internal LecturaDeExamen(Analito analito, Magnitud resultado)
		{
			ArgumentNullException.ThrowIfNull(analito);
			ArgumentNullException.ThrowIfNull(resultado);

			Analito = analito;
			Resultado = resultado;
		}

		// La llave del analito, que es con la que se pide su serie y su interpretacion.
		internal string Nombre => Analito.Nombre;

		/// <summary>El codigo del analito dentro de una clave de frase.</summary>
		internal string ClaveDelAnalito => Analito.Clave;

		/// <summary>El codigo de lo que dio, vacio cuando lo que dio es un numero.</summary>
		internal string ClaveDelResultado => Resultado.Clave;

		/// <summary>Si lo que dio es un numero con unidad, y no una categoria ni un si o un no.</summary>
		internal bool EsNumero => Resultado.EsNumero;

		/// <summary>
		/// El numero que dio, cero cuando no dio un numero. Viaja como numero y no como texto
		/// porque el separador decimal es del idioma y lo pone quien redacta.
		/// </summary>
		internal decimal Valor => Resultado is MagnitudContinua continua ? continua.Valor : 0m;

		/// <summary>El simbolo de la unidad, vacio cuando no dio un numero. NO se traduce.</summary>
		internal string Unidad => Resultado is Medicion medicion ? medicion.Unidad.Simbolo : "";
	}
}
