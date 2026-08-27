using System;

namespace Cargas.Dominio
{
	/// <summary>
	/// La moneda en que se pago una carga. Conjunto cerrado SIN factor de conversion,
	/// a proposito: EL DINERO NO SE CONVIERTE, SE REGISTRA. Una carga pagada en colones
	/// queda en colones para siempre; convertirla exige un tipo de cambio con fecha, y
	/// eso es presentacion, no dominio. Reconvertir un historico es falsear el pasado,
	/// y en un journal append-only ademas es contradictorio.
	/// </summary>
	// El balboa esta anclado 1:1 al dolar y el colon no: la conversion ni siquiera es
	// simetrica en dificultad. Que el tipo no tenga factor hace imposible olvidarlo.
	public sealed class Moneda
	{
		public static readonly Moneda Colon = new Moneda("CRC", "₡");
		public static readonly Moneda Balboa = new Moneda("PAB", "B/.");
		public static readonly Moneda Dolar = new Moneda("USD", "$");

		public string Codigo { get; private set; }
		public string Simbolo { get; private set; }

		private Moneda(string codigo, string simbolo)
		{
			Codigo = codigo;
			Simbolo = simbolo;
		}

		// La query de reconocimiento para la frontera: decide sin reventar.
		public static bool EsConocida(string codigo)
		{
			if (string.IsNullOrWhiteSpace(codigo)) return false;
			string normalizado = codigo.Trim().ToUpperInvariant();
			return normalizado == Colon.Codigo || normalizado == Balboa.Codigo
				|| normalizado == Dolar.Codigo;
		}

		public static Moneda PorCodigo(string codigo)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(codigo);

			string normalizado = codigo.Trim().ToUpperInvariant();
			if (normalizado == Colon.Codigo) return Colon;
			if (normalizado == Balboa.Codigo) return Balboa;
			if (normalizado == Dolar.Codigo) return Dolar;

			throw new ErrorDeDominio(
				$"'{codigo}' no es una moneda conocida. Las validas son: "
				+ $"{Colon.Codigo}, {Balboa.Codigo}, {Dolar.Codigo}.");
		}

		public bool EsLaMisma(Moneda otra)
		{
			ArgumentNullException.ThrowIfNull(otra);
			return ReferenceEquals(this, otra);
		}
	}
}
