using System.Globalization;
using Cargas.App.Frases;

namespace Cargas.Wasm;

/// <summary>
/// COMO SE ESCRIBE UNA CIFRA EN EL TABLERO. Vive suelto y no dentro de una pantalla porque lo usan
/// las cuatro, y porque tenerlo en un solo lugar es lo que impide que dos pantallas redondeen el
/// mismo numero distinto.
/// <para>
/// NO ESCRIBE NINGUNA CIFRA POR SU CUENTA: delega en el MISMO renderizador con que la puerta compone
/// las frases del dominio. La cifra de un instrumento y la de la frase tres centimetros mas arriba
/// son la misma pantalla, y dos implementaciones de la coma decimal son la frase rival en pequeno.
/// Los tres formatos siguen siendo del dominio -- un decimal el rendimiento, dos el dinero, enteros
/// los kilometros -- y lo que cambia con el idioma es el separador.
/// </para>
/// <para>
/// EL IDIOMA SE FIJA UNA VEZ, al negociar, y antes del primer render. Es estatico por la misma razon
/// que esta clase lo es: la carátula tiene UNA eleccion de la persona, no una por pantalla, y
/// pasarla por parametro a cada llamada del marcado la volveria ilegible sin comprar nada.
/// </para>
/// </summary>
public static class Formato
{
	private static string idioma = Frases.IdiomaDelRegistro;

	/// <summary>
	/// Fija el idioma con que se escribe toda cifra de la carátula. Lo llama <see cref="Textos"/>
	/// al terminar de negociar con el dispositivo, y nadie mas.
	/// </summary>
	public static void Hablar(string etiqueta)
	{
		idioma = string.IsNullOrWhiteSpace(etiqueta) ? Frases.IdiomaDelRegistro : etiqueta;
		Cultura = ReferenceEquals(LasFrases, Frases.De(Frases.ElOtroIdioma))
			? CultureInfo.GetCultureInfo("en-US")
			: CultureInfo.GetCultureInfo("es-ES");
	}

	private static Frases LasFrases => Frases.De(idioma);

	/// <summary>Una decimal: rendimiento, litros, ritmos.</summary>
	public static string Cifra(decimal medido) => LasFrases.Cifra(medido);

	/// <summary>Kilometros. El dominio los expresa enteros y la pantalla tambien.</summary>
	public static string Entero(decimal medido) => LasFrases.Entera(medido);

	/// <summary>Dinero, dos decimales, SIN el simbolo: el marcado lo pone aparte.</summary>
	public static string Plata(decimal monto) => LasFrases.Cifra(Math.Round(monto, 2));

	/// <summary>El simbolo de la moneda, que no se traduce: es con lo que la estacion cobra.</summary>
	public static string Simbolo(string moneda) => Frases.Simbolo(moneda);

	public static string Mayuscula(string frase) =>
		frase.Length == 0 ? frase : char.ToUpperInvariant(frase[0]) + frase.Substring(1);

	/// <summary>
	/// LA CULTURA CON QUE SE ESCRIBE UN NOMBRE DE MES, que es lo unico para lo que hace falta una:
	/// el resto de las cifras las escribe el renderizador. Cambia con el idioma de la persona, y no
	/// se lee del proceso -- en wasm esa cultura es la del runtime y no la de nadie.
	/// </summary>
	public static CultureInfo Cultura { get; private set; } = CultureInfo.GetCultureInfo("es-ES");
}
