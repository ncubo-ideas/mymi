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
	internal abstract class FichaDelVehiculo
	{
		/// <summary>La ausencia, como instancia unica.</summary>
		internal static readonly FichaDelVehiculo Vacia = new FichaVacia();

		internal static FichaDelVehiculo Completada(string alias, string marca, string modelo,
			int anio, string color, RendimientoDeFabrica rendimientoDeFabrica) =>
			new FichaCompletada(alias, marca, modelo, anio, color, rendimientoDeFabrica);

		internal abstract bool Existe { get; }

		/// <summary>Revienta si la ficha esta vacia: consultar antes Existe.</summary>
		internal abstract string Alias { get; }
		internal abstract string Marca { get; }
		internal abstract string Modelo { get; }
		internal abstract int Anio { get; }
		internal abstract string Color { get; }

		/// <summary>Total: una ficha vacia no aporto rendimiento de fabrica.</summary>
		internal abstract RendimientoDeFabrica RendimientoDeFabrica { get; }

		private sealed class FichaVacia : FichaDelVehiculo
		{
			private static ErrorDeDominio SinFicha() =>
				new ErrorDeDominio(
					"Sonia todavia no completo la ficha de este vehiculo: consultar antes Existe.");

			internal override bool Existe => false;
			internal override string Alias => throw SinFicha();
			internal override string Marca => throw SinFicha();
			internal override string Modelo => throw SinFicha();
			internal override int Anio => throw SinFicha();
			internal override string Color => throw SinFicha();
			internal override RendimientoDeFabrica RendimientoDeFabrica => RendimientoDeFabrica.SinDato;
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

			internal override bool Existe => true;
			internal override string Alias => alias;
			internal override string Marca => marca;
			internal override string Modelo => modelo;
			internal override int Anio => anio;
			internal override string Color => color;
			internal override RendimientoDeFabrica RendimientoDeFabrica => rendimientoDeFabrica;
		}
	}
}
