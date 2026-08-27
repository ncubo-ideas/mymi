using System;
using System.Collections.Generic;
using System.Globalization;

namespace Compras.Dominio
{
	/// <summary>
	/// CUANTO Y CUANDO entro algo a la canasta, mes a mes. Es la serie, no el total: lo
	/// que permite decir que un consumo se disparo y DESDE QUE MES, que es lo que hace
	/// creible una relacion con cualquier otra cosa que le pase a la persona.
	/// </summary>
	// Es la contraparte de la serie de un analito del lado de las compras. Una pantalla
	// muestra tickets y totales por mes; esto mira la forma de la serie completa.
	//
	// Dice CUANTO se movio y DESDE QUE MES, nunca si eso esta bien: que el helado pase de
	// mensual a semanal es un dato, no una falta. Quien cruza esa serie con cualquier otra
	// cosa que le pase a la persona es el asistente, con permiso; el super no tiene con
	// que hacerlo.
	public abstract class Consumo
	{
		/// <summary>Lo que no se encontro: nunca null.</summary>
		internal static Consumo Ninguno(string termino) => new SinConsumo(termino);

		internal static Consumo De(string termino, bool esCategoria, IReadOnlyList<MesDeConsumo> meses) =>
			new Serie(termino, esCategoria, meses);

		public string Termino { get; private set; }

		private Consumo(string termino)
		{
			ArgumentNullException.ThrowIfNull(termino);
			Termino = termino;
		}

		/// <summary>Consulta de presencia, total: si hay algo de esto en el historial.</summary>
		public abstract bool Existe();

		/// <summary>Si el termino era una categoria del repertorio o un texto de producto.</summary>
		public abstract bool EsCategoria { get; }

		public abstract IReadOnlyList<MesDeConsumo> Meses { get; }

		public abstract int MesesConConsumo { get; }
		public abstract decimal TotalGastado { get; }
		public abstract int TotalUnidades { get; }

		public abstract string PrimerMesExpresado { get; }
		public abstract string UltimoMesExpresado { get; }

		/// <summary>El mes de mayor gasto: donde el consumo pesa mas.</summary>
		public abstract string MesDelPicoExpresado { get; }

		public abstract decimal GastoEnElPrimerMes { get; }
		public abstract decimal GastoEnElUltimoMes { get; }

		/// <summary>El HECHO, con signo: cuanto cambio el gasto entre el primer mes y el ultimo.</summary>
		public abstract decimal CambioDeGasto { get; }

		/// <summary>Si hay serie con la que leer un cambio: hacen falta al menos dos meses.</summary>
		public abstract bool HaySerie { get; }

		/// <summary>Si el gasto se movio lo bastante como para no ser variacion de despensa.</summary>
		public abstract bool HayCambioReal { get; }

		/// <summary>La direccion del cambio, sin valorarla. Preguntar HayCambioReal primero.</summary>
		public abstract bool Subio { get; }

		/// <summary>La linea, escrita por el dominio con SUS hechos. Nunca un consejo.</summary>
		public abstract string ParaSonia { get; }

		private sealed class SinConsumo : Consumo
		{
			internal SinConsumo(string termino) : base(termino) { }

			public override bool Existe() => false;
			public override bool EsCategoria => false;
			public override IReadOnlyList<MesDeConsumo> Meses => Array.Empty<MesDeConsumo>();
			public override int MesesConConsumo => 0;
			public override decimal TotalGastado => 0m;
			public override int TotalUnidades => 0;
			public override string PrimerMesExpresado => "";
			public override string UltimoMesExpresado => "";
			public override string MesDelPicoExpresado => "";
			public override decimal GastoEnElPrimerMes => 0m;
			public override decimal GastoEnElUltimoMes => 0m;
			public override decimal CambioDeGasto => 0m;
			public override bool HaySerie => false;
			public override bool HayCambioReal => false;

			public override bool Subio =>
				throw new InvalidOperationException(
					$"No hay compras de «{Termino}»: no hay direccion que dar. Preguntar HayCambioReal antes.");

			public override string ParaSonia =>
				$"no hay compras de «{Termino}» en el historial";
		}

		private sealed class Serie : Consumo
		{
			// Cuanto tiene que moverse el gasto para llamarlo un cambio real. Un quinto
			// arriba o abajo es variacion de despensa; mas que eso es otro habito.
			private const decimal CambioQueCuentaComoReal = 0.20m;

			private readonly bool esCategoria;
			private readonly IReadOnlyList<MesDeConsumo> meses;

			internal Serie(string termino, bool esCategoria, IReadOnlyList<MesDeConsumo> meses)
				: base(termino)
			{
				ArgumentNullException.ThrowIfNull(meses);
				if (meses.Count == 0)
					throw new ArgumentException("Una serie sin meses no es una serie; eso es Consumo.Ninguno.",
						nameof(meses));

				this.esCategoria = esCategoria;
				this.meses = meses;
			}

			public override bool Existe() => true;
			public override bool EsCategoria => esCategoria;
			public override IReadOnlyList<MesDeConsumo> Meses => meses;
			public override int MesesConConsumo => meses.Count;

			public override decimal TotalGastado
			{
				get
				{
					decimal total = 0m;
					foreach (MesDeConsumo mes in meses) total = total + mes.Gasto;
					return total;
				}
			}

			public override int TotalUnidades
			{
				get
				{
					int total = 0;
					foreach (MesDeConsumo mes in meses) total = total + mes.Unidades;
					return total;
				}
			}

			public override string PrimerMesExpresado => meses[0].MesExpresado;
			public override string UltimoMesExpresado => meses[meses.Count - 1].MesExpresado;

			public override string MesDelPicoExpresado
			{
				get
				{
					MesDeConsumo pico = meses[0];
					foreach (MesDeConsumo mes in meses) if (mes.Gasto > pico.Gasto) pico = mes;
					return pico.MesExpresado;
				}
			}

			public override decimal GastoEnElPrimerMes => meses[0].Gasto;
			public override decimal GastoEnElUltimoMes => meses[meses.Count - 1].Gasto;

			public override decimal CambioDeGasto => GastoEnElUltimoMes - GastoEnElPrimerMes;

			// Un solo mes no es una serie: con una sola observacion no hay cambio que leer.
			public override bool HaySerie => meses.Count >= 2;

			public override bool HayCambioReal
			{
				get
				{
					if (!HaySerie) return false;

					decimal referencia = GastoEnElPrimerMes;
					if (referencia == 0m) return false;

					return Math.Abs(CambioDeGasto / referencia) >= CambioQueCuentaComoReal;
				}
			}

			public override bool Subio
			{
				get
				{
					if (!HayCambioReal)
						throw new InvalidOperationException(
							$"El consumo de «{Termino}» no se movio lo suficiente como para darle una "
							+ "direccion. Preguntar HayCambioReal antes.");

					return CambioDeGasto > 0m;
				}
			}

			public override string ParaSonia
			{
				get
				{
					string que = esCategoria ? Termino : $"«{Termino}»";

					if (meses.Count == 1)
						return $"{que} aparece una sola vez, en {PrimerMesExpresado}, por "
							+ $"{Colones(TotalGastado)}: todavía no hay serie para decir para dónde viene";

					string tramo = $"de {Colones(GastoEnElPrimerMes)} en {PrimerMesExpresado} a "
						+ $"{Colones(GastoEnElUltimoMes)} en {UltimoMesExpresado}";

					if (HayCambioReal)
						return $"{que} pasó {tramo}, sobre {MesesConConsumo} meses con compras "
							+ $"(el mes más alto fue {MesDelPicoExpresado})";

					return $"{que} se mantuvo {tramo}";
				}
			}

			private static string Colones(decimal monto) =>
				Plata.Colones(monto);
		}
	}
}
