using System;

namespace Municipalidad.Dominio
{
	// COMO SE CALCULA UN RUBRO: por unidad, o sobre la base imponible.
	//
	// Es una clase porque OPINA sobre algo que la app agrega y el recibo no trae: QUE
	// EXPLICA UN AUMENTO. Un rubro sobre base imponible responde al valor de la finca;
	// cuando salta, hubo avaluo. Un rubro por unidad no: cuando salta, hubo tarifa nueva.
	// "Por que subio mi IBI?" y "por que subio mi basura?" tienen respuestas distintas
	// por esta clase, y es lo unico que la app puede decir que el portal no dice.
	//
	// El recibo la esconde en un sufijo -"LIMPIEZA DE CALLES BASE IMP"- y en nada mas.
	//
	// CONJUNTO CERRADO de dos, resolutor total (regla 11), comparado por nombre.
	internal abstract class LeyDeCalculo
	{
		internal abstract string Nombre { get; }

		// LO PROPIO: si responde al avaluo, un salto se explica por el valor de la finca.
		internal abstract bool RespondeAlAvaluo { get; }

		/// <summary>
		/// Como se cobra este rubro, dicho para caber adentro de una frase que una persona lee. La dice
		/// la ley y no la hoja de Variacion que la cita: una hoja que la escribiera a mano afirmaria de
		/// memoria algo que solo esta clase sabe.
		/// </summary>
		internal abstract string Dice();

		internal bool Es(LeyDeCalculo otra)
		{
			return otra != null && otra.Nombre == Nombre;
		}

		internal static LeyDeCalculo PorUnidad() { return new TarifaPorUnidad(); }
		internal static LeyDeCalculo SobreBaseImponible() { return new TarifaSobreBaseImponible(); }

		internal static LeyDeCalculo PorNombre(string nombre)
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(nombre);
			switch (nombre.Trim().ToLowerInvariant())
			{
				case "por-unidad": return PorUnidad();
				case "sobre-base-imponible": return SobreBaseImponible();
				default:
					throw new ArgumentOutOfRangeException(nameof(nombre), nombre,
						"Ley de calculo desconocida. Las que existen son: por-unidad, sobre-base-imponible.");
			}
		}

		internal static bool EsUnNombreConocido(string nombre)
		{
			if (string.IsNullOrWhiteSpace(nombre)) return false;
			switch (nombre.Trim().ToLowerInvariant())
			{
				case "por-unidad":
				case "sobre-base-imponible": return true;
				default: return false;
			}
		}
	}

	// COBRADO POR UNIDAD -una casa, una recoleccion-. No responde al valor de la finca:
	// cuando salta, cambio la tarifa.
	internal sealed class TarifaPorUnidad : LeyDeCalculo
	{
		internal override string Nombre { get { return "por-unidad"; } }
		internal override bool RespondeAlAvaluo { get { return false; } }

		internal override string Dice() { return "se cobra por unidad"; }
	}

	// COBRADO SOBRE LA BASE IMPONIBLE: el valor de la finca, declarado cada cinco anios o
	// avaluado por la municipalidad (juicio 2, firmado). Cuando salta, hubo avaluo.
	internal sealed class TarifaSobreBaseImponible : LeyDeCalculo
	{
		internal override string Nombre { get { return "sobre-base-imponible"; } }
		internal override bool RespondeAlAvaluo { get { return true; } }

		internal override string Dice() { return "se cobra sobre la base imponible, que es el valor de la finca"; }
	}
}
