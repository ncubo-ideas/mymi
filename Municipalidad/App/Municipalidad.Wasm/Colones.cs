namespace Municipalidad.Wasm
{
	/// <summary>
	/// Un monto como dinero, para las pantallas que lo necesitan asi: colones, con separador de
	/// miles. Reusa Textos.Monto para los digitos y la coma decimal real -- SOLO le agrega el
	/// simbolo y el agrupamiento que Textos.Monto deliberadamente no pone, porque ese metodo lo
	/// comparten otras pantallas que siguen sin simbolo de moneda.
	/// </summary>
	public static class Colones
	{
		public static string Formatear(Textos textos, decimal monto)
		{
			string dicho = textos.Monto(monto);
			int coma = dicho.IndexOf(',');
			string enteros = coma < 0 ? dicho : dicho[..coma];
			string resto = coma < 0 ? "" : dicho[coma..];
			return "₡" + ConSeparadorDeMiles(enteros) + resto;
		}

		private static string ConSeparadorDeMiles(string digitos)
		{
			if (digitos.Length <= 3) return digitos;

			int primerGrupo = digitos.Length % 3;
			if (primerGrupo == 0) primerGrupo = 3;

			var partes = new System.Collections.Generic.List<string> { digitos[..primerGrupo] };
			for (int i = primerGrupo; i < digitos.Length; i += 3)
				partes.Add(digitos.Substring(i, 3));

			return string.Join(' ', partes);
		}
	}
}
