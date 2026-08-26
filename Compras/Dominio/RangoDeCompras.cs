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
	public abstract class RangoDeCompras
	{
		/// <summary>Ausencia con nombre: un historial sin compras porta esto, nunca null.</summary>
		public static readonly RangoDeCompras Ninguno = new SinCompras();

		internal static RangoDeCompras De(DateTime primera, DateTime ultima, int mesesConCompras) =>
			new Entre(primera, ultima, mesesConCompras);

		/// <summary>Consulta de presencia, total.</summary>
		public abstract bool Existe();

		public abstract int PrimerAnio { get; }
		public abstract int PrimerMes { get; }
		public abstract int UltimoAnio { get; }
		public abstract int UltimoMes { get; }

		/// <summary>Cuantos meses distintos tienen al menos una compra.</summary>
		public abstract int MesesConCompras { get; }

		/// <summary>Como aaaa-mm, que es la forma en que se le dice a quien pregunta.</summary>
		public abstract string PrimerMesExpresado { get; }

		public abstract string UltimoMesExpresado { get; }

		private sealed class SinCompras : RangoDeCompras
		{
			public override bool Existe() => false;

			public override int PrimerAnio => throw Vacio();
			public override int PrimerMes => throw Vacio();
			public override int UltimoAnio => throw Vacio();
			public override int UltimoMes => throw Vacio();
			public override int MesesConCompras => 0;
			public override string PrimerMesExpresado => "";
			public override string UltimoMesExpresado => "";

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

			public override bool Existe() => true;

			public override int PrimerAnio => primera.Year;
			public override int PrimerMes => primera.Month;
			public override int UltimoAnio => ultima.Year;
			public override int UltimoMes => ultima.Month;
			public override int MesesConCompras => mesesConCompras;

			public override string PrimerMesExpresado => $"{primera.Year:0000}-{primera.Month:00}";
			public override string UltimoMesExpresado => $"{ultima.Year:0000}-{ultima.Month:00}";
		}
	}
}
