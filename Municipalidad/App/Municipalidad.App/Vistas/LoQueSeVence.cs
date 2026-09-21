using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Municipalidad.App.Vistas
{
	/// <summary>
	/// Lo que se le vence a esta persona y lo que ya se le vencio, leido entero al instante en que la
	/// pantalla pregunta. Son DOS listas y no se suman: son dos situaciones distintas.
	/// </summary>
	public sealed class LoQueSeVence
	{
		private LoQueSeVence() { }

		/// <summary>El dia con que se contesto. Los dias que faltan y los de atraso son contra este.</summary>
		public string PreguntadoEl { get; private init; } = "";

		/// <summary>Cuantos dias antes del vencimiento se marca aviso. Ninguna pantalla lo elige.</summary>
		public int DiasDeAviso { get; private init; }

		/// <summary>La tasa anual con que se PROYECTO el costo de pagar tarde. No es la mora cobrada.</summary>
		public decimal TasaConQueSeProyecto { get; private init; }

		/// <summary>Los periodos que todavia no vencen. Vacio es una respuesta.</summary>
		public IReadOnlyList<UnPeriodoPorVencer> PorVencer { get; private init; } =
			new List<UnPeriodoPorVencer>();

		/// <summary>Los periodos que ya vencieron y siguen sin pagar. Vacio es una respuesta.</summary>
		public IReadOnlyList<UnPeriodoVencido> Vencidos { get; private init; } =
			new List<UnPeriodoVencido>();

		/// <summary>Las dos listas leidas en un solo momento, para que hablen del mismo dia.</summary>
		public static LoQueSeVence LeerPor(MunicipalidadAPI api, DateTime fechaDeCorte, string diaDicho,
			string idioma)
		{
			ArgumentNullException.ThrowIfNull(api);

			RegistroDeVencimientos vencimientos =
				api.VencimientosAl(fechaDeCorte, LosJuiciosSinFirmar.DiasDeAviso, idioma);

			RegistroDeAtrasos atrasos =
				api.AtrasosAl(fechaDeCorte, LosJuiciosSinFirmar.TasaDeMora, idioma);

			var porVencer = new List<UnPeriodoPorVencer>();

			for (int fila = 0; fila < vencimientos.Rubro.Length; fila++)
				porVencer.Add(new UnPeriodoPorVencer
				{
					Rubro = vencimientos.Rubro[fila],
					Periodo = vencimientos.Periodo[fila],
					Vence = vencimientos.Vence[fila],
					Monto = vencimientos.Monto[fila],
					Faltan = vencimientos.Faltan[fila],
					Urgencia = vencimientos.Urgencia[fila],
					Dice = vencimientos.Dice[fila],
				});

			var vencidos = new List<UnPeriodoVencido>();

			for (int fila = 0; fila < atrasos.Rubro.Length; fila++)
				vencidos.Add(new UnPeriodoVencido
				{
					Rubro = atrasos.Rubro[fila],
					Periodo = atrasos.Periodo[fila],
					Vencio = atrasos.Vencio[fila],
					Monto = atrasos.Monto[fila],
					Dias = atrasos.Dias[fila],
					CostoProyectado = atrasos.CostoProyectado[fila],
					Urgencia = atrasos.Urgencia[fila],
					Dice = atrasos.Dice[fila],
				});

			return new LoQueSeVence
			{
				PreguntadoEl = diaDicho,
				DiasDeAviso = LosJuiciosSinFirmar.DiasDeAviso,
				TasaConQueSeProyecto = LosJuiciosSinFirmar.TasaDeMora,
				PorVencer = porVencer,
				Vencidos = vencidos,
			};
		}
	}

	/// <summary>Un periodo que todavia no vence.</summary>
	public sealed class UnPeriodoPorVencer
	{
		public string Rubro { get; init; } = "";

		public string Periodo { get; init; } = "";

		/// <summary>El ultimo dia del periodo, en yyyy-MM-dd. Lo decidio la cadencia del rubro.</summary>
		public string Vence { get; init; } = "";

		public decimal Monto { get; init; }

		public int Faltan { get; init; }

		/// <summary>'ninguna' o 'aviso'. Un aviso es cortesia y nunca una parada obligatoria.</summary>
		public string Urgencia { get; init; } = "";

		public string Dice { get; init; } = "";
	}

	/// <summary>Un periodo que ya vencio y sigue sin pagar.</summary>
	public sealed class UnPeriodoVencido
	{
		public string Rubro { get; init; } = "";

		public string Periodo { get; init; } = "";

		public string Vencio { get; init; } = "";

		public decimal Monto { get; init; }

		public int Dias { get; init; }

		/// <summary>Lo que esta app PROYECTA de mora. No es lo que la municipalidad cobra.</summary>
		public decimal CostoProyectado { get; init; }

		public string Urgencia { get; init; } = "";

		public string Dice { get; init; } = "";
	}
}
