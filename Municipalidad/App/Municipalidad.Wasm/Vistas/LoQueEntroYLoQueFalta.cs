using Municipalidad.App;

namespace Municipalidad.Wasm.Vistas
{
	/// <summary>
	/// Hasta donde leyo esta app la narrativa de la municipalidad, y que mitad de una cuenta hibrida
	/// todavia falta. Se lee entera al instante en que la pantalla pregunta.
	/// <para>
	/// El valor DECLARADO de una finca no esta aca y su ausencia es la decision: esta app no se lo pide
	/// a la persona, asi que nombrarselo como un hueco suyo seria nombrarle uno que no puede cerrar por
	/// aqui. El avaluo si esta: ese lo publica la municipalidad.
	/// </para>
	/// </summary>
	public sealed class LoQueEntroYLoQueFalta
	{
		private LoQueEntroYLoQueFalta() { }

		/// <summary>El dia con que se contesto.</summary>
		public string PreguntadoEl { get; private init; } = "";

		/// <summary>
		/// Hasta que hecho de la municipalidad leyo esta app. Solo sirve comparado consigo mismo: sube
		/// cuando entra algo nuevo, y cero es un valor legal porque los hechos se numeran desde uno.
		/// </summary>
		public long UltimoEventoLeido { get; private init; }

		/// <summary>Un renglon por rubro del catalogo, diga o no que ya llego un comprobante suyo.</summary>
		public IReadOnlyList<UnRubroDelCatalogo> Rubros { get; private init; } =
			new List<UnRubroDelCatalogo>();

		/// <summary>Cuantos rubros del catalogo ya tienen algun comprobante en esta app.</summary>
		public int RubrosConHistoria { get; private init; }

		/// <summary>Si hay alguna consulta al portal registrada. En false no hay con que cotejar.</summary>
		public bool HayAlgunaConsulta { get; private init; }

		/// <summary>El dia de la consulta mas reciente, en yyyy-MM-dd.</summary>
		public string DiaDeLaUltimaConsulta { get; private init; } = "";

		/// <summary>Si la persona ya adopto una reserva. Es la mitad que ella aporta.</summary>
		public bool HayReservaAdoptada { get; private init; }

		/// <summary>Las fincas y el avaluo publicado de cada una, del mismo instante que el resto.</summary>
		public LasFincasQueTengo Fincas { get; private init; }

		/// <summary>Cuantas fincas esperan un avaluo. Es lo que deja a medias una respuesta del IBI.</summary>
		public int FincasSinAvaluoPublicado { get; private init; }

		/// <summary>true cuando no falta ninguna de las dos mitades que esta pantalla sabe mirar.</summary>
		public bool NoFaltaNadaQueEstaPantallaSepaMirar =>
			HayReservaAdoptada && FincasSinAvaluoPublicado == 0 && HayAlgunaConsulta;

		/// <summary>Las dos mitades leidas en un solo momento, para que hablen del mismo dia.</summary>
		public static LoQueEntroYLoQueFalta LeerPor(
			MunicipalidadAPI api, DateTime fechaDeCorte, string diaDicho, Textos textos)
		{
			ArgumentNullException.ThrowIfNull(api);

			bool hayConsulta = api.HayAlgunaConsulta();

			var conHistoria = new HashSet<string>(api.RubrosConHistoria(), StringComparer.Ordinal);

			var rubros = new List<UnRubroDelCatalogo>();

			foreach (string rubro in api.LosRubrosQueHay())
				rubros.Add(new UnRubroDelCatalogo
				{
					Nombre = rubro,
					TieneHistoria = conHistoria.Contains(rubro),
				});

			LasFincasQueTengo fincas = LasFincasQueTengo.LeerPor(api, fechaDeCorte);

			int sinAvaluo = 0;

			foreach (UnaFinca cada in fincas.Fincas)
				if (!cada.TieneAvaluo) sinAvaluo++;

			return new LoQueEntroYLoQueFalta
			{
				PreguntadoEl = diaDicho,
				UltimoEventoLeido = api.UltimoEventoAplicado(),
				Rubros = rubros,
				RubrosConHistoria = conHistoria.Count,
				HayAlgunaConsulta = hayConsulta,
				DiaDeLaUltimaConsulta = hayConsulta ? ElDiaDe(api.ConsultaMasReciente()) : "",
				HayReservaAdoptada = api.SePuedeVerElFaltanteDeReserva(),
				Fincas = fincas,
				FincasSinAvaluoPublicado = sinAvaluo,
			};
		}

		/// <summary>El dia de calendario de un instante que vino del registro, no de ningun reloj.</summary>
		private static string ElDiaDe(DateTime instante) =>
			instante.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
	}

	/// <summary>Un rubro del catalogo y si ya llego algun comprobante suyo a esta app.</summary>
	public sealed class UnRubroDelCatalogo
	{
		public string Nombre { get; init; } = "";

		/// <summary>false NO dice que la persona no lo pague: dice que sus pagos no han llegado aqui.</summary>
		public bool TieneHistoria { get; init; }
	}
}
