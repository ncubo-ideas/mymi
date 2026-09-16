using Municipalidad.Wasm.Vistas;

namespace Municipalidad.Wasm
{
	/// <summary>
	/// El mismo juicio de tres estados ("al día" / "próximo" / "vencido") compuesto UNA vez a partir de
	/// LoQueSeVence, para que Inicio y Pagos y vencimientos digan exactamente lo mismo. No es un juicio
	/// nuevo: son las mismas dos listas que ya lee Pagos y vencimientos, solo resumidas.
	/// </summary>
	public static class ResumenDeVencimientos
	{
		public static (string Clase, string Icono, string Titulo) Estado(LoQueSeVence vence)
		{
			if (vence.Vencidos.Count > 0)
			{
				string cuantos = vence.Vencidos.Count == 1 ? "1 pago vencido" : $"{vence.Vencidos.Count} pagos vencidos";
				return ("vencido", "vencido", $"Tenés {cuantos}");
			}

			if (vence.PorVencer.Count > 0)
			{
				string cuantos = vence.PorVencer.Count == 1 ? "1 pago por vencer" : $"{vence.PorVencer.Count} pagos por vencer";
				return ("proximo", "proximo", $"Tenés {cuantos}");
			}

			return ("al-dia", "al-dia", "Al día");
		}
	}
}
