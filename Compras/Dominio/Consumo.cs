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
	// El VEREDICTO sale del perfil de la categoria, no del signo: que los azucarados
	// suban es empeorar y que las proteinas suban es mejorar. Cuando no hay perfil que
	// consultar -una busqueda por texto que cruza categorias, o algo no comestible- el
	// consumo entrega los hechos y NO se pronuncia: SePuedeJuzgar lo dice.
	public abstract class Consumo
	{
		/// <summary>Lo que no se encontro: nunca null.</summary>
		internal static Consumo Ninguno(string termino) => new SinConsumo(termino);

		internal static Consumo De(string termino, bool esCategoria, PerfilDeConsumo perfil,
			IReadOnlyList<MesDeConsumo> meses) =>
			new Serie(termino, esCategoria, perfil, meses);

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

		/// <summary>Si hay con que juzgar: hace falta perfil de consumo y al menos dos meses.</summary>
		public abstract bool SePuedeJuzgar { get; }

		/// <summary>Que SIGNIFICA el cambio, cuando se puede decir. Si no, Indeterminado.</summary>
		public abstract Veredicto Veredicto { get; }

		public string NombreDelVeredicto => Veredicto.Nombre;

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
			public override bool SePuedeJuzgar => false;
			public override Veredicto Veredicto => Veredicto.Indeterminado;

			public override string ParaSonia =>
				$"no hay compras de «{Termino}» en el historial";
		}

		private sealed class Serie : Consumo
		{
			// Cuanto tiene que moverse el gasto para llamarlo un cambio real. Un quinto
			// arriba o abajo es variacion de despensa; mas que eso es otro habito.
			private const decimal CambioQueCuentaComoReal = 0.20m;

			private readonly bool esCategoria;
			private readonly PerfilDeConsumo perfil;
			private readonly IReadOnlyList<MesDeConsumo> meses;

			internal Serie(string termino, bool esCategoria, PerfilDeConsumo perfil,
				IReadOnlyList<MesDeConsumo> meses) : base(termino)
			{
				ArgumentNullException.ThrowIfNull(meses);
				if (meses.Count == 0)
					throw new ArgumentException("Una serie sin meses no es una serie; eso es Consumo.Ninguno.",
						nameof(meses));

				this.esCategoria = esCategoria;
				this.perfil = perfil;
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

			// Un solo mes no es una serie, y sin perfil no hay direccion deseable que
			// consultar: en los dos casos el consumo entrega los hechos y no se pronuncia.
			public override bool SePuedeJuzgar => meses.Count >= 2 && perfil != null;

			public override Veredicto Veredicto
			{
				get
				{
					if (!SePuedeJuzgar) return Veredicto.Indeterminado;
					if (ReferenceEquals(perfil, PerfilDeConsumo.Neutro)) return Veredicto.Estable;

					decimal referencia = GastoEnElPrimerMes;
					if (referencia == 0m) return Veredicto.Indeterminado;

					decimal proporcion = CambioDeGasto / referencia;
					if (Math.Abs(proporcion) < CambioQueCuentaComoReal) return Veredicto.Estable;

					// En lo ocasional subir es empeorar; en lo recomendado, mejorar.
					bool subio = proporcion > 0m;
					if (perfil.EsOcasional) return subio ? Veredicto.Empeorando : Veredicto.Mejorando;
					return subio ? Veredicto.Mejorando : Veredicto.Empeorando;
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

					if (Veredicto.EsEmpeorando || Veredicto.EsMejorando)
						return $"{que} pasó {tramo}, sobre {MesesConConsumo} meses con compras "
							+ $"(el mes más alto fue {MesDelPicoExpresado})";

					return $"{que} se mantuvo {tramo}";
				}
			}

			private static string Colones(decimal monto) =>
				"₡" + monto.ToString("N0", CultureInfo.InvariantCulture);
		}
	}
}
