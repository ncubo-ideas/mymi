using System.Globalization;

namespace Cargas.Dominio
{
	/// <summary>
	/// COMO SE ESCRIBE UNA CIFRA CUANDO EL DOMINIO ARMA UNA FRASE.
	///
	/// Dos razones para que exista, y ninguna es cosmetica:
	///
	/// 1. CON SEPARADOR DE MILES. "sobraron ₡9392" hay que contarlo con el dedo, y arriba
	///    en la misma pantalla la caratula ya escribe "₡9 392" -Formato.Plata- para el
	///    mismo tipo de cifra. La misma plata con dos convenciones en la misma pantalla
	///    se lee como dos monedas.
	///
	/// 2. INDEPENDIENTE DE LA CULTURA. Un "{Monto:0.##}" pelado usa la cultura del proceso:
	///    la misma frase da "1234.5" en la consola y "1234,5" en un navegador en español, y
	///    esa frase la lee tambien el MCP. El dominio no puede depender de donde corre.
	///
	/// El separador es un espacio fino, el mismo que usa la caratula, y el decimal sigue
	/// siendo el punto.
	/// </summary>
	internal static class Cifras
	{
		internal static readonly NumberFormatInfo Agrupadas = new NumberFormatInfo
		{
			NumberGroupSeparator = " ",   // espacio fino, escapado a proposito
			NumberDecimalSeparator = ".",
		};

		/// <summary>Dinero: hasta dos decimales, sin ceros de relleno.</summary>
		internal static string Plata(decimal monto) => monto.ToString("#,0.##", Agrupadas);

		/// <summary>Enteros de una frase: kilometros, cuentas.</summary>
		internal static string Entera(decimal valor) => valor.ToString("#,0", Agrupadas);

		/// <summary>Promedios: hasta un decimal, sin ceros de relleno. Un ritmo de 38.2
		/// km por dia redondeado a 38 pierde justo la precision por la que alguien
		/// pregunta cuanto maneja.</summary>
		internal static string Aproximada(decimal valor) => valor.ToString("#,0.#", Agrupadas);
	}
}
