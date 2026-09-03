using System.Globalization;

namespace Cargas.Wasm;

// COMO SE ESCRIBE UNA CIFRA EN EL TABLERO. Vive suelto y no dentro de una pantalla porque
// lo usan las cuatro, y porque tenerlo en un solo lugar es lo que impide que dos pantallas
// redondeen el mismo numero distinto.
//
// LOS TRES FORMATOS SON DEL DOMINIO, no de la caratula. Hubo uno solo -"0.##" para todo- y
// eso ponia a la pantalla a contradecirse consigo misma: el instrumento decia "9.43 km/L"
// mientras la frase del dominio, tres centimetros mas arriba, decia "el ultimo tramo dio
// 9.4". La misma cifra con dos redondeos se lee como dos mediciones distintas.
//
// La precision de cada magnitud ya estaba decidida, y no aca: Rendimiento.Expresado es
// "0.#", Dinero.Expresado es "0.##", y Tramo.Expresado da los kilometros enteros.
public static class Formato
{
	// Formato explicito SIEMPRE: la escala del decimal no sobrevive el journal (5571.50
	// entra, 5571.5 sale del replay), asi que un ToString() pelado mostraria valores
	// distintos antes y despues de reabrir la app.
	//
	// CON SEPARADOR DE MILES, y es del tablero: aca la cifra es el contenido, y "29392"
	// hay que contarlo con el dedo. El separador es un espacio fino -el mismo que usa el
	// dominio cuando arma una frase con plata adentro, Cifras.Plata- y el decimal sigue
	// siendo el punto, asi que no hay dos convenciones en la misma pantalla.
	private static readonly NumberFormatInfo Miles = new NumberFormatInfo
	{
		NumberGroupSeparator = "\u202f",   // espacio fino, escapado a proposito: invisible en el fuente
		NumberDecimalSeparator = ".",
	};

	/// <summary>Una decimal: rendimiento, litros, ritmos. El formato de Rendimiento.</summary>
	public static string Cifra(decimal medido) => medido.ToString("#,0.#", Miles);

	/// <summary>Kilometros. El dominio los expresa enteros y la pantalla tambien.</summary>
	public static string Entero(decimal medido) => medido.ToString("#,0", Miles);

	/// <summary>Dinero, dos decimales. El formato de Dinero.Expresado.</summary>
	public static string Plata(decimal monto) => monto.ToString("#,0.##", Miles);

	/// <summary>El simbolo de la moneda. Si no la conoce, devuelve el codigo y un espacio:
	/// mostrar "USD 48" es feo pero cierto, e inventar un simbolo no lo es.</summary>
	public static string Simbolo(string moneda) => moneda switch
	{
		"CRC" => "₡",
		"PAB" => "B/.",
		"USD" => "$",
		_ => moneda + " ",
	};

	public static string Mayuscula(string frase) =>
		frase.Length == 0 ? frase : char.ToUpperInvariant(frase[0]) + frase.Substring(1);

	public static readonly CultureInfo Cultura = new CultureInfo("es-ES");
}
