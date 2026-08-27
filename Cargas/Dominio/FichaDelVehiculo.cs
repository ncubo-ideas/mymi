using System;

namespace Cargas.Dominio
{
	/// <summary>
	/// Lo que SOLO SONIA sabe de su vehiculo: alias, marca, modelo, anio, color y el
	/// rendimiento del manual. La estacion captura la placa; que carro es ese se queda
	/// en el journal de ella. La ficha vacia es un tipo, no seis nulls: un vehiculo
	/// puede existir con solo la placa, y ese es justo el momento de pedirle los datos
	/// -cuando ya hay algo que nombrar, no un formulario al inicio-.
	/// </summary>
	public abstract class FichaDelVehiculo
	{
		/// <summary>La ausencia, como instancia unica.</summary>
		public static readonly FichaDelVehiculo Vacia = new FichaVacia();

		public static FichaDelVehiculo Completada(string alias, string marca, string modelo,
			int anio, string color, RendimientoDeFabrica rendimientoDeFabrica) =>
			new FichaCompletada(alias, marca, modelo, anio, color, rendimientoDeFabrica);

		public abstract bool Existe { get; }

		/// <summary>Revienta si la ficha esta vacia: consultar antes Existe.</summary>
		public abstract string Alias { get; }
		public abstract string Marca { get; }
		public abstract string Modelo { get; }
		public abstract int Anio { get; }
		public abstract string Color { get; }

		/// <summary>Total: una ficha vacia no aporto rendimiento de fabrica.</summary>
		public abstract RendimientoDeFabrica RendimientoDeFabrica { get; }

		private sealed class FichaVacia : FichaDelVehiculo
		{
			private static ErrorDeDominio SinFicha() =>
				new ErrorDeDominio(
					"Sonia todavia no completo la ficha de este vehiculo: consultar antes Existe.");

			public override bool Existe => false;
			public override string Alias => throw SinFicha();
			public override string Marca => throw SinFicha();
			public override string Modelo => throw SinFicha();
			public override int Anio => throw SinFicha();
			public override string Color => throw SinFicha();
			public override RendimientoDeFabrica RendimientoDeFabrica => RendimientoDeFabrica.SinDato;
		}

		private sealed class FichaCompletada : FichaDelVehiculo
		{
			private readonly string alias;
			private readonly string marca;
			private readonly string modelo;
			private readonly int anio;
			private readonly string color;
			private readonly RendimientoDeFabrica rendimientoDeFabrica;

			internal FichaCompletada(string alias, string marca, string modelo, int anio,
				string color, RendimientoDeFabrica rendimientoDeFabrica)
			{
				// El alias es lo que motiva la ficha: "el Corolla", "el de Isa". Sin el
				// no hay ficha que completar.
				ArgumentNullException.ThrowIfNullOrWhiteSpace(alias);
				ArgumentNullException.ThrowIfNull(rendimientoDeFabrica);
				if (anio < 1900 || anio > 2100)
					throw new ErrorDeDominio($"{anio} no es un anio de vehiculo verosimil.");

				this.alias = alias.Trim();
				this.marca = (marca ?? "").Trim();
				this.modelo = (modelo ?? "").Trim();
				this.anio = anio;
				this.color = (color ?? "").Trim();
				this.rendimientoDeFabrica = rendimientoDeFabrica;
			}

			public override bool Existe => true;
			public override string Alias => alias;
			public override string Marca => marca;
			public override string Modelo => modelo;
			public override int Anio => anio;
			public override string Color => color;
			public override RendimientoDeFabrica RendimientoDeFabrica => rendimientoDeFabrica;
		}
	}
}
