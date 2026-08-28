using System;

namespace Cargas.Dominio
{
	/// <summary>
	/// Lo que el manual dice que el carro rinde. Es un DATO que Sonia aporta (o que un
	/// dia cruzara desde el modulo del concesionario), no una medicion de este dominio:
	/// participa del cruce "el manual dice 18, tus cargas dicen 9" y de nada mas.
	/// </summary>
	public abstract class RendimientoDeFabrica
	{
		/// <summary>La ausencia, como instancia unica: Sonia no lo aporto.</summary>
		public static readonly RendimientoDeFabrica SinDato = new NoAportado();

		public static RendimientoDeFabrica Declarado(decimal kilometrosPorLitro) =>
			new Aportado(Rendimiento.DeFabrica(kilometrosPorLitro));

		public abstract bool Existe { get; }

		/// <summary>Revienta si no fue aportado: consultar antes Existe.</summary>
		public abstract Rendimiento Valor { get; }

		private sealed class NoAportado : RendimientoDeFabrica
		{
			public override bool Existe => false;

			public override Rendimiento Valor =>
				throw new ErrorDeDominio(
					"El rendimiento de fabrica no fue aportado: consultar antes Existe.");
		}

		private sealed class Aportado : RendimientoDeFabrica
		{
			private readonly Rendimiento valor;

			internal Aportado(Rendimiento valor)
			{
				this.valor = valor;
			}

			public override bool Existe => true;
			public override Rendimiento Valor => valor;
		}
	}
}
