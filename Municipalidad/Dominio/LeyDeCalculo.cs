using System;

namespace Municipalidad.Dominio
{
	/// <summary>
	/// Como se calcula un rubro: por unidad, o sobre la base imponible. Es una clase porque decide QUE
	/// EXPLICA UN AUMENTO, que es algo que la app agrega y el recibo no trae: un rubro sobre base
	/// imponible responde al valor de la finca, y cuando salta hubo avaluo; uno por unidad no, y cuando
	/// salta hubo tarifa nueva.
	/// <para>
	/// El recibo la esconde en un sufijo del nombre del rubro y en nada mas.
	/// </para>
	/// <para>
	/// Conjunto cerrado de dos, comparado por nombre, con resolutor total desde el primitivo.
	/// </para>
	/// </summary>
	internal abstract class LeyDeCalculo
	{
		internal abstract string Nombre { get; }

		/// <summary>
		/// Lo propio de cada ley: si responde al avaluo, un salto se explica por el valor de la finca.
		/// </summary>
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

	/// <summary>
	/// Cobrado por unidad. No responde al valor de la finca: cuando salta, cambio la tarifa.
	/// </summary>
	internal sealed class TarifaPorUnidad : LeyDeCalculo
	{
		internal override string Nombre { get { return "por-unidad"; } }
		internal override bool RespondeAlAvaluo { get { return false; } }

		internal override string Dice() { return "se cobra por unidad"; }
	}

	/// <summary>
	/// Cobrado sobre la base imponible, que es el valor de la finca: el que la persona declara o el que
	/// la municipalidad avalua. Cuando salta, hubo avaluo.
	/// </summary>
	internal sealed class TarifaSobreBaseImponible : LeyDeCalculo
	{
		internal override string Nombre { get { return "sobre-base-imponible"; } }
		internal override bool RespondeAlAvaluo { get { return true; } }

		internal override string Dice() { return "se cobra sobre la base imponible, que es el valor de la finca"; }
	}
}
