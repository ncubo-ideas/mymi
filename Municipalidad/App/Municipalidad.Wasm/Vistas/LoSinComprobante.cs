using Municipalidad.App;

namespace Municipalidad.Wasm.Vistas
{
	/// <summary>
	/// Los periodos que el portal da por pagados y de los que esta app nunca vio un recibo, cotejados
	/// contra la consulta mas reciente. No poder cotejar es un tercer estado y no es estar en orden.
	/// </summary>
	public sealed class LoSinComprobante
	{
		private LoSinComprobante() { }

		/// <summary>Si hay alguna consulta al portal registrada. En false no hay con que cotejar.</summary>
		public bool HayAlgunaConsulta { get; private init; }

		/// <summary>El dia de la consulta contra la que se cotejo, en yyyy-MM-dd.</summary>
		public string DiaDeLaConsulta { get; private init; } = "";

		/// <summary>El estado del cotejo, en la palabra que eligio el dominio.</summary>
		public string Estado { get; private init; } = "";

		/// <summary>La frase del estado: la compuso el dominio, o la escribio la ausencia de consulta.</summary>
		public string Dice { get; private init; } = "";

		/// <summary>Un renglon por periodo sin comprobante. Vacio con una consulta hecha es estar al dia.</summary>
		public IReadOnlyList<UnPeriodoSinRecibo> Periodos { get; private init; } =
			new List<UnPeriodoSinRecibo>();

		/// <summary>El cotejo, detras de su par: sin consulta no se pregunta por la consulta.</summary>
		public static LoSinComprobante LeerPor(MunicipalidadAPI api)
		{
			ArgumentNullException.ThrowIfNull(api);

			if (!api.HayAlgunaConsulta())
				return new LoSinComprobante
				{
					HayAlgunaConsulta = false,
					Estado = "sin-consulta",
					Dice = NuncaSeHaRegistradoUnaConsulta,
				};

			DateTime laConsulta = api.ConsultaMasReciente();

			RegistroDePeriodosSinComprobante cotejo = api.PeriodosPagadosSinComprobante(laConsulta);

			var filas = new List<UnPeriodoSinRecibo>();

			for (int fila = 0; fila < cotejo.Rubro.Length; fila++)
				filas.Add(new UnPeriodoSinRecibo
				{
					Rubro = cotejo.Rubro[fila],
					Periodo = cotejo.Periodo[fila],
					Dice = cotejo.Dice[fila],
				});

			return new LoSinComprobante
			{
				HayAlgunaConsulta = true,
				DiaDeLaConsulta = ElDiaDe(laConsulta),
				Estado = cotejo.Estado,
				Dice = cotejo.EstadoDicho,
				Periodos = filas,
			};
		}

		/// <summary>El dia de calendario de un instante que vino del registro, no de ningun reloj.</summary>
		private static string ElDiaDe(DateTime instante) =>
			instante.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);

		private const string NuncaSeHaRegistradoUnaConsulta =
			"Tu app no tiene ninguna consulta al portal registrada, así que no hay con qué cotejar lo que "
			+ "sí vio pagar. Esto NO dice que todo esté en orden: dice que la pregunta todavía no se "
			+ "puede contestar, y se va a poder en cuanto entre una consulta.";
	}

	/// <summary>Un periodo que el portal da por saldado y del que aqui no hay recibo.</summary>
	public sealed class UnPeriodoSinRecibo
	{
		public string Rubro { get; init; } = "";

		public string Periodo { get; init; } = "";

		public string Dice { get; init; } = "";
	}
}
