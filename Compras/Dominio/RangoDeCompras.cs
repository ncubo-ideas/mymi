using System;

namespace Compras.Dominio
{
	/// <summary>
	/// Desde cuando y hasta cuando hay compras en el historial, y cuantos meses
	/// distintos tienen alguna. Es el par presente/ausente: un historial vacio porta
	/// Ninguno y preguntarle sus meses revienta.
	/// </summary>
	// Existe por una razon concreta de la superficie: quien pregunta -sobre todo una IA-
	// no tiene forma de saber que meses tienen compras, y sin eso adivina un mes vacio,
	// recibe "no hay comparacion" y le dice a la persona que no hay datos cuando si hay.
	// El rango es el contexto que hace posible elegir bien.
	internal abstract class RangoDeCompras
	{
		/// <summary>Ausencia con nombre: un historial sin compras porta esto, nunca null.</summary>
		internal static readonly RangoDeCompras Ninguno = new SinCompras();

		internal static RangoDeCompras De(DateTime primera, DateTime ultima, int mesesConCompras) =>
			new Entre(primera, ultima, mesesConCompras);

		/// <summary>Consulta de presencia, total.</summary>
		internal abstract bool Existe();

		internal abstract int PrimerAnio { get; }
		internal abstract int PrimerMes { get; }
		internal abstract int UltimoAnio { get; }
		internal abstract int UltimoMes { get; }

		/// <summary>Cuantos meses distintos tienen al menos una compra.</summary>
		internal abstract int MesesConCompras { get; }

		/// <summary>Como aaaa-mm, que es la forma en que se le dice a quien pregunta.</summary>
		internal abstract string PrimerMesExpresado { get; }

		internal abstract string UltimoMesExpresado { get; }

		private sealed class SinCompras : RangoDeCompras
		{
			internal override bool Existe() => false;

			internal override int PrimerAnio => throw Vacio();
			internal override int PrimerMes => throw Vacio();
			internal override int UltimoAnio => throw Vacio();
			internal override int UltimoMes => throw Vacio();
			internal override int MesesConCompras => 0;
			internal override string PrimerMesExpresado => "";
			internal override string UltimoMesExpresado => "";

			private static InvalidOperationException Vacio() =>
				new InvalidOperationException(
					"El historial no tiene compras, asi que no tiene rango. Preguntar Existe() primero.");
		}

		private sealed class Entre : RangoDeCompras
		{
			private readonly DateTime primera;
			private readonly DateTime ultima;
			private readonly int mesesConCompras;

			internal Entre(DateTime primera, DateTime ultima, int mesesConCompras)
			{
				if (mesesConCompras <= 0)
					throw new ArgumentOutOfRangeException(nameof(mesesConCompras), mesesConCompras,
						"Un rango con cero meses no es un rango; eso es RangoDeCompras.Ninguno.");

				this.primera = primera;
				this.ultima = ultima;
				this.mesesConCompras = mesesConCompras;
			}

			internal override bool Existe() => true;

			internal override int PrimerAnio => primera.Year;
			internal override int PrimerMes => primera.Month;
			internal override int UltimoAnio => ultima.Year;
			internal override int UltimoMes => ultima.Month;
			internal override int MesesConCompras => mesesConCompras;

			internal override string PrimerMesExpresado => $"{primera.Year:0000}-{primera.Month:00}";
			internal override string UltimoMesExpresado => $"{ultima.Year:0000}-{ultima.Month:00}";
		}
	}
}
