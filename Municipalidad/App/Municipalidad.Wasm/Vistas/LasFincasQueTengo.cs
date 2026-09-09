using Municipalidad.App;

namespace Municipalidad.Wasm.Vistas
{
	/// <summary>
	/// Las fincas que la municipalidad mando y el valor que la persona declaro de cada una, leidas al
	/// instante en que la pantalla pregunta. La finca la manda la institucion; el valor es de ella.
	/// </summary>
	public sealed class LasFincasQueTengo
	{
		private LasFincasQueTengo() { }

		public IReadOnlyList<UnaFinca> Fincas { get; private init; } = new List<UnaFinca>();

		public bool HayAlguna => Fincas.Count > 0;

		/// <summary>Cada finca detras de su par: sin valor declarado no se pregunta por el valor.</summary>
		public static LasFincasQueTengo LeerPor(MunicipalidadAPI api, DateTime fechaDeCorte)
		{
			ArgumentNullException.ThrowIfNull(api);

			var fincas = new List<UnaFinca>();

			foreach (string numero in api.NumerosDeLasFincas())
			{
				bool declarado = api.TieneValorDeclaradoLaFinca(numero, fechaDeCorte);

				fincas.Add(new UnaFinca
				{
					Numero = numero,
					TieneValorDeclarado = declarado,
					Valor = declarado ? api.ValorDeFincaAl(numero, fechaDeCorte) : 0m,
				});
			}

			return new LasFincasQueTengo { Fincas = fincas };
		}
	}

	/// <summary>Una finca de esta cuenta, con el valor que la persona haya declarado o sin el.</summary>
	public sealed class UnaFinca
	{
		public string Numero { get; init; } = "";

		public bool TieneValorDeclarado { get; init; }

		public decimal Valor { get; init; }
	}
}
