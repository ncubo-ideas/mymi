using System;
using System.Collections.Generic;

namespace Compras.Dominio
{
	/// <summary>
	/// CADA CUÁNTO compra ELLA algo, medido en su propio historial. No es cada cuánto
	/// "se debería" reponer: es cada cuánto lo repone esta persona.
	/// </summary>
	// EXISTE PARA CORREGIR UNA MENTIRA PIADOSA. El hueco se leia contra una periodicidad
	// fija por categoria -proteinas cada 10 dias, granos cada 35- que no salio de ningun
	// lado: la elegimos nosotros. Para una familia que compra cada quince dias, "hace 12
	// dias que no entra un lacteo" no es un hueco, es martes.
	//
	// El ritmo propio se mide con los INTERVALOS entre compras consecutivas de la
	// categoria, y hace falta al menos dos compras para tener uno. Cuando no alcanza, el
	// dominio lo dice -EsObservado en false- y quien lee usa la periodicidad del
	// repertorio como respaldo. Es la misma forma de las otras lecturas: cuando no hay con
	// que, se entregan los hechos y no se inventa una conclusion.
	//
	// Se usa la MEDIANA y no el promedio: una sola compra de reserva -el arroz de la
	// quincena que se compro dos veces en la misma semana- corre el promedio y no cambia
	// la mediana. Un habito se describe mejor por su intervalo tipico que por su media.
	public abstract class RitmoDeCompra
	{
		internal static RitmoDeCompra Ninguno(Categoria categoria) => new SinRitmo(categoria);

		internal static RitmoDeCompra De(Categoria categoria, IReadOnlyList<int> intervalos) =>
			new Observado(categoria, intervalos);

		public Categoria Categoria { get; private set; }

		private RitmoDeCompra(Categoria categoria)
		{
			ArgumentNullException.ThrowIfNull(categoria);
			Categoria = categoria;
		}

		/// <summary>
		/// Si el ritmo se pudo MEDIR en el historial. En false, quien lee cae en la
		/// periodicidad del repertorio, que es una suposición y no un hecho.
		/// </summary>
		public abstract bool EsObservado { get; }

		/// <summary>Cuántas veces se puede medir el intervalo: compras de la categoría menos una.</summary>
		public abstract int IntervalosMedidos { get; }

		/// <summary>El intervalo típico en días. Preguntar EsObservado primero.</summary>
		public abstract int DiasTipicos { get; }

		public abstract int DiasMasCorto { get; }
		public abstract int DiasMasLargo { get; }

		/// <summary>
		/// Los días con los que hay que leer un hueco de esta categoría: el ritmo medido si
		/// existe, y si no la periodicidad del repertorio. Total: siempre contesta.
		/// </summary>
		public int DiasParaLeerElHueco =>
			EsObservado ? DiasTipicos : Categoria.DiasDeReabastecimiento;

		public abstract string ParaElAsistente { get; }

		private sealed class SinRitmo : RitmoDeCompra
		{
			internal SinRitmo(Categoria categoria) : base(categoria) { }

			public override bool EsObservado => false;
			public override int IntervalosMedidos => 0;

			public override int DiasTipicos =>
				throw new InvalidOperationException(
					$"No hay dos compras de '{Categoria.Nombre}' con las que medir un ritmo. "
					+ "Preguntar EsObservado antes.");

			public override int DiasMasCorto => DiasTipicos;
			public override int DiasMasLargo => DiasTipicos;

			public override string ParaElAsistente =>
				$"todavía no hay con qué medir cada cuánto comprás {Categoria.Nombre}: "
				+ $"se lee con los {Categoria.DiasDeReabastecimiento} días del catálogo";
		}

		private sealed class Observado : RitmoDeCompra
		{
			private readonly List<int> intervalos;

			internal Observado(Categoria categoria, IReadOnlyList<int> intervalos) : base(categoria)
			{
				ArgumentNullException.ThrowIfNull(intervalos);
				if (intervalos.Count == 0)
					throw new ArgumentException("Un ritmo sin intervalos no es un ritmo; eso es Ninguno.",
						nameof(intervalos));

				this.intervalos = new List<int>(intervalos);
				this.intervalos.Sort();
			}

			public override bool EsObservado => true;
			public override int IntervalosMedidos => intervalos.Count;

			// La mediana: el intervalo del medio. Con un numero par se promedian los dos
			// centrales, que es lo convencional y no cambia la idea.
			public override int DiasTipicos
			{
				get
				{
					int medio = intervalos.Count / 2;
					return intervalos.Count % 2 == 1
						? intervalos[medio]
						: (int)Math.Round((intervalos[medio - 1] + intervalos[medio]) / 2.0);
				}
			}

			public override int DiasMasCorto => intervalos[0];
			public override int DiasMasLargo => intervalos[intervalos.Count - 1];

			public override string ParaElAsistente
			{
				get
				{
					string cada = $"cada {DiasTipicos} días";
					if (IntervalosMedidos == 1)
						return $"solo hay un intervalo medido de {Categoria.Nombre}: {cada}";

					return $"{Categoria.Nombre} lo comprás {cada}, sobre {IntervalosMedidos} intervalos "
						+ $"(el más corto de {DiasMasCorto} y el más largo de {DiasMasLargo})";
				}
			}
		}
	}
}
