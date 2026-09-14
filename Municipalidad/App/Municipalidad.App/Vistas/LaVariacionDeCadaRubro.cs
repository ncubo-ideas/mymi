using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Municipalidad.App.Vistas
{
	/// <summary>
	/// Por que subio lo que esta persona paga, rubro por rubro, leido entero en UN momento. La pantalla
	/// no lee nada por su cuenta: recibe esto y lo dice.
	/// </summary>
	public sealed class LaVariacionDeCadaRubro
	{
		private LaVariacionDeCadaRubro() { }

		/// <summary>El umbral con que se midio si un cambio es un salto. Ninguna pantalla lo elige.</summary>
		public decimal UmbralDeSalto { get; private init; }

		/// <summary>Por debajo de esta proporcion un cambio es ruido y no se avisa.</summary>
		public decimal Ruido { get; private init; }

		/// <summary>Una fila por rubro del catalogo, incluidos los que no tienen con que contestar.</summary>
		public IReadOnlyList<LaVariacionDeUnRubro> Rubros { get; private init; } =
			new List<LaVariacionDeUnRubro>();

		/// <summary>Si alguno de los rubros llego a tener una variacion que mostrar.</summary>
		public bool AlgunoSePuedeVer { get; private init; }

		/// <summary>
		/// Lee los cinco rubros por la puerta, en un solo momento. Cada uno pregunta su par antes de la
		/// lectura guardada, asi que ninguna fila se construye sobre una respuesta que el dominio nego.
		/// </summary>
		public static LaVariacionDeCadaRubro LeerPor(MunicipalidadAPI api)
		{
			ArgumentNullException.ThrowIfNull(api);

			var conHistoria = new List<string>(api.RubrosConHistoria());
			var filas = new List<LaVariacionDeUnRubro>();
			bool alguno = false;

			foreach (string rubro in api.LosRubrosQueHay())
			{
				if (!conHistoria.Contains(rubro))
				{
					filas.Add(LaVariacionDeUnRubro.SinNingunComprobante(rubro));
					continue;
				}

				if (!api.SePuedeVerLaVariacionDe(rubro))
				{
					filas.Add(LaVariacionDeUnRubro.ConUnSoloComprobante(rubro));
					continue;
				}

				filas.Add(LaVariacionDeUnRubro.De(
					api.VariacionDe(rubro, LosJuiciosSinFirmar.UmbralDeSalto, LosJuiciosSinFirmar.Ruido)));

				alguno = true;
			}

			return new LaVariacionDeCadaRubro
			{
				UmbralDeSalto = LosJuiciosSinFirmar.UmbralDeSalto,
				Ruido = LosJuiciosSinFirmar.Ruido,
				Rubros = filas,
				AlgunoSePuedeVer = alguno,
			};
		}
	}

	/// <summary>Un rubro y su variacion, o la constancia en palabras de por que no la tiene.</summary>
	public sealed class LaVariacionDeUnRubro
	{
		private LaVariacionDeUnRubro() { }

		/// <summary>El nombre del rubro tal como lo escribe el registro. Es una llave, no un titulo.</summary>
		public string Rubro { get; private init; } = "";

		/// <summary>Si hay dos periodos que comparar. En false los montos no significan nada.</summary>
		public bool SePuedeVer { get; private init; }

		public string Desde { get; private init; } = "";

		public string Hasta { get; private init; } = "";

		public decimal Antes { get; private init; }

		public decimal Ahora { get; private init; }

		public decimal Proporcion { get; private init; }

		/// <summary>Que explica el cambio, en la palabra que eligio la ley de calculo del rubro.</summary>
		public string Explicacion { get; private init; } = "";

		/// <summary>Si el cambio pasa el umbral con que se midio.</summary>
		public bool EsSalto { get; private init; }

		/// <summary>La frase que compuso el dominio, o la que dice por que no hay variacion.</summary>
		public string Dice { get; private init; } = "";

		internal static LaVariacionDeUnRubro De(RegistroDeLaVariacion registro) =>
			new LaVariacionDeUnRubro
			{
				Rubro = registro.Rubro,
				SePuedeVer = true,
				Desde = registro.Desde,
				Hasta = registro.Hasta,
				Antes = registro.Antes,
				Ahora = registro.Ahora,
				Proporcion = registro.Proporcion,
				Explicacion = registro.Explicacion,
				EsSalto = registro.EsSalto,
				Dice = registro.Dice,
			};

		internal static LaVariacionDeUnRubro SinNingunComprobante(string rubro) =>
			new LaVariacionDeUnRubro
			{
				Rubro = rubro,
				SePuedeVer = false,
				Dice = "Tu app no ha visto ni un comprobante de este rubro, así que no hay nada que "
					+ "comparar. Eso no quiere decir que no lo pagués: quiere decir que tus pagos de este "
					+ "rubro todavía no han llegado aquí.",
			};

		internal static LaVariacionDeUnRubro ConUnSoloComprobante(string rubro) =>
			new LaVariacionDeUnRubro
			{
				Rubro = rubro,
				SePuedeVer = false,
				Dice = "De este rubro hay un solo período con comprobante, y para ver una variación hacen "
					+ "falta dos. No es que el cobro no haya cambiado: es que todavía no hay contra qué "
					+ "compararlo.",
			};
	}
}
