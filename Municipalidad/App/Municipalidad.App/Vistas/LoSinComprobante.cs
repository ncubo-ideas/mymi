using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

// EL RENDERIZADOR DE FRASES, con alias porque la clase y su espacio de nombres se llaman igual y sin
// esto la referencia queda ambigua. Es el mismo con que el dominio compone las suyas: las frases de
// esta Vista y las de la puerta se escriben en un solo lugar.
using LasFrases = Municipalidad.App.Frases.Frases;

namespace Municipalidad.App.Vistas
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
		public static LoSinComprobante LeerPor(MunicipalidadAPI api, string idioma)
		{
			ArgumentNullException.ThrowIfNull(api);

			LasFrases frases = LasFrases.De(idioma);

			if (!api.HayAlgunaConsulta())
				return new LoSinComprobante
				{
					HayAlgunaConsulta = false,
					Estado = "sin-consulta",
					Dice = frases.Diga("sinComprobante.nuncaHuboConsulta"),
				};

			DateTime laConsulta = api.ConsultaMasReciente();

			RegistroDePeriodosSinComprobante cotejo = api.PeriodosPagadosSinComprobante(laConsulta, idioma);

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

	}

	/// <summary>Un periodo que el portal da por saldado y del que aqui no hay recibo.</summary>
	public sealed class UnPeriodoSinRecibo
	{
		public string Rubro { get; init; } = "";

		public string Periodo { get; init; } = "";

		public string Dice { get; init; } = "";
	}
}
