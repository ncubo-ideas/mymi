using Municipalidad.App;

namespace Municipalidad.Wasm.Vistas
{
	/// <summary>
	/// Las fincas que la municipalidad mando y el AVALUO con que cobra cada una, leidos al instante en
	/// que la pantalla pregunta. Las dos cosas las publica la institucion: esta pantalla no le pide nada
	/// a la persona sobre su finca.
	/// </summary>
	public sealed class LasFincasQueTengo
	{
		private LasFincasQueTengo() { }

		public IReadOnlyList<UnaFinca> Fincas { get; private init; } = new List<UnaFinca>();

		public bool HayAlguna => Fincas.Count > 0;

		/// <summary>Cada avaluo detras de su par: sin avaluo publicado no se pregunta por el monto.</summary>
		public static LasFincasQueTengo LeerPor(MunicipalidadAPI api, DateTime fechaDeCorte)
		{
			ArgumentNullException.ThrowIfNull(api);

			var fincas = new List<UnaFinca>();

			foreach (string numero in api.NumerosDeLasFincas())
			{
				bool avaluada = api.TieneAvaluoLaFinca(numero, fechaDeCorte);

				fincas.Add(new UnaFinca
				{
					Numero = numero,
					TieneAvaluo = avaluada,
					Avaluo = avaluada ? api.AvaluoDeFincaAl(numero, fechaDeCorte) : 0m,
				});
			}

			return new LasFincasQueTengo { Fincas = fincas };
		}
	}

	/// <summary>Una finca de esta cuenta, con el avaluo que la municipalidad haya publicado de ella.</summary>
	public sealed class UnaFinca
	{
		public string Numero { get; init; } = "";

		/// <summary>Si la municipalidad publico un avaluo suyo vigente a la fecha de corte.</summary>
		public bool TieneAvaluo { get; init; }

		/// <summary>El avaluo con que la MUNICIPALIDAD cobra. Solo se lee con TieneAvaluo.</summary>
		public decimal Avaluo { get; init; }
	}
}
