using System;

namespace Cargas.App.Frases
{
	/// <summary>
	/// COMO SE ARMA CADA RESPUESTA a partir de lo que el dominio mando: su clave, sus claves de
	/// clausula y sus valores. Una funcion por respuesta, y el anidado -- que clausula va en que
	/// hueco -- esta escrito una sola vez, aca.
	/// <para>
	/// Ningun valor se traduce al entrar en un hueco: la placa, el numero de transaccion, el
	/// nombre de un surtidor y el simbolo de una unidad salieron del registro de la estacion o de
	/// lo que la persona eligio, y se muestran tal como se asentaron.
	/// </para>
	/// </summary>
	public static class LaRedaccion
	{
		/// <summary>
		/// LA LECTURA DE LA SERIE. La clave es el veredicto, que es semantico: mejoro y empeoro se
		/// juzgaron en canonico y ninguna unidad los invierte. Lo que si cambia con la unidad es la
		/// clausula del cambio, que el dominio nombra aparte por eso mismo.
		/// </summary>
		public static string Lectura(Frases frases, string veredicto, string simbolo,
			decimal actual, decimal habitual,
			string claveDeLoQueFalta, DateTime fechaDeLaCargaTrunca,
			DateTime fechaDelTotalQueNoCuadra, string motivoDeReserva,
			string claveDelCambio, int porcentaje)
		{
			ArgumentNullException.ThrowIfNull(frases);

			if (veredicto.Length == 0) return "";

			if (veredicto == "total-no-cuadra")
				return frases.Diga("lectura.total-no-cuadra",
					("fecha", frases.Dia(fechaDelTotalQueNoCuadra)));

			if (veredicto == "sin-tramo")
				return frases.Diga("lectura.sin-tramo",
					("falta", frases.Diga("falta." + claveDeLoQueFalta,
						("fecha", frases.Dia(fechaDeLaCargaTrunca)))));

			if (veredicto == "no-comparable")
				return frases.Diga("lectura.no-comparable",
					("reserva", frases.Diga("reserva." + motivoDeReserva)));

			if (veredicto == "mejoro" || veredicto == "empeoro")
				return frases.Diga("lectura." + veredicto,
					("habitual", frases.ConUnidad(habitual, simbolo)),
					("actual", frases.Cifra(actual)),
					("cambio", frases.Diga("cambio." + claveDelCambio,
						("porcentaje", frases.Porcentaje(Math.Abs(porcentaje))))));

			return frases.Diga("lectura." + veredicto,
				("actual", frases.ConUnidad(actual, simbolo)),
				("habitual", frases.ConUnidad(habitual, simbolo)));
		}

		/// <summary>
		/// QUE HACER Y CUANDO. Las dos mitades son claves del dominio y el orden en que se
		/// encadenan es del idioma, asi que la frase entera vive en el catalogo y no se pega aca.
		/// </summary>
		public static string Accion(Frases frases, string claveDeLaAccion, string claveDelPlazo)
		{
			ArgumentNullException.ThrowIfNull(frases);
			if (claveDeLaAccion.Length == 0) return "";

			return frases.Diga("accion.marco",
				("que", frases.Diga("accion." + claveDeLaAccion)),
				("cuando", frases.Diga("plazo." + claveDelPlazo)));
		}

		/// <summary>
		/// CUANTO SE MANEJA ESTE CARRO. Cuando no se pudo medir, la clave de la situacion ya dice
		/// cual de las tres causas es; no se rearma dividiendo los kilometros entre los dias, que
		/// daria un ritmo que nadie midio y con otro redondeo.
		/// </summary>
		public static string Ritmo(Frases frases, string situacion, decimal porDia,
			decimal kilometros, string simbolo, DateTime desde, DateTime hasta, int lecturas)
		{
			ArgumentNullException.ThrowIfNull(frases);
			if (situacion.Length == 0) return "";

			return frases.Diga("ritmo." + situacion,
				("porDia", frases.Cifra(porDia)),
				("kilometros", frases.Entera(kilometros)),
				("unidad", simbolo),
				("desde", frases.Dia(desde)),
				("hasta", frases.Dia(hasta)),
				("lecturas", frases.Kilometrajes(lecturas)));
		}

		/// <summary>CUANTO DEL APORTE DE ELLA YA ESTA: la linea discreta, nunca el reproche.</summary>
		public static string Completitud(Frases frases, int conKilometraje, int total, int calculados)
		{
			ArgumentNullException.ThrowIfNull(frases);

			return frases.Diga("completitud",
				("conKilometraje", conKilometraje.ToString()),
				("cargas", frases.Cargas(total)),
				("tramos", frases.TramosCalculables(calculados)));
		}

		/// <summary>
		/// UN DATO QUE FALTA, con lo que desbloquea. Cuando nace de una inferencia, la frase ES la
		/// suposicion y su consecuencia: enunciar y despues preguntar. La pantalla pedia confirmar
		/// una afirmacion que nunca decia, y "si, quedo asi" no dice que es "asi".
		/// </summary>
		public static string Faltante(Frases frases, string claveDeLoQueFalta,
			string claveDeLoQueDesbloquea, DateTime fechaDeLaCarga, decimal kilometros,
			DateTime desde, DateTime hasta, string loQueSeSupone, string claveDelCorte)
		{
			ArgumentNullException.ThrowIfNull(frases);
			if (claveDeLoQueFalta.Length == 0) return "";

			string desbloqueo = Desbloqueo(frases, claveDeLoQueDesbloquea, kilometros, desde, hasta);

			if (loQueSeSupone.Length > 0)
				return frases.Diga("faltante.supone",
					("suposicion", Suposicion(frases, loQueSeSupone, claveDelCorte)),
					("desbloqueo", desbloqueo));

			return frases.Diga("faltante.pide",
				("falta", frases.Diga("faltante.falta." + claveDeLoQueFalta)),
				("fecha", frases.Dia(fechaDeLaCarga)),
				("desbloqueo", desbloqueo));
		}

		/// <summary>
		/// LA SUPOSICION SOLA, para que los botones digan que es "asi". Quien sabe que un corte
		/// automatico significa tanque lleno es el dominio; lo que pone esta capa son las palabras.
		/// </summary>
		public static string Suposicion(Frases frases, string loQueSeSupone, string claveDelCorte)
		{
			ArgumentNullException.ThrowIfNull(frases);
			if (loQueSeSupone.Length == 0) return "";

			return frases.Diga("suposicion." + loQueSeSupone,
				("corte", frases.Diga("corte." + claveDelCorte)));
		}

		private static string Desbloqueo(Frases frases, string clave, decimal kilometros,
			DateTime desde, DateTime hasta)
		{
			if (clave.Length == 0) return "";

			return frases.Diga("faltante.desbloquea." + clave,
				("kilometros", frases.Entera(kilometros)),
				("desde", frases.Dia(desde)),
				("hasta", frases.Dia(hasta)));
		}

		/// <summary>
		/// LO QUE DIO UNA CARGA EN LA TOMBOLA, en pasado. Cuando la cuenta derivada no cuadra con
		/// lo emitido se dice que probablemente falta un dato, NUNCA que la estacion debe algo.
		/// </summary>
		public static string BoletosDeLaCarga(Frases frases, string clave, int emitidos,
			int queDaLaRegla, decimal sobrante, string moneda, string monedaDeLaRegla)
		{
			ArgumentNullException.ThrowIfNull(frases);
			if (clave.Length == 0) return "";

			return frases.Diga("boletos." + clave,
				("emitidos", frases.Boletos(emitidos)),
				("regla", frases.Boletos(queDaLaRegla)),
				("sobrante", frases.Monto(sobrante, moneda)),
				("moneda", moneda),
				("monedaDeLaRegla", monedaDeLaRegla));
		}

		/// <summary>UN SORTEO CON SUS BOLETOS, ya juzgado contra un dia que puso quien pregunta.</summary>
		public static string Sorteo(Frases frases, string clave, int boletos, DateTime fecha,
			int dias)
		{
			ArgumentNullException.ThrowIfNull(frases);
			if (clave.Length == 0) return "";

			return frases.Diga("sorteo." + clave,
				("boletos", frases.Boletos(boletos)),
				("fecha", frases.Dia(fecha)),
				("dias", frases.Dias(dias)));
		}

		/// <summary>EL GASTO DE UN PERIODO EN UNA MONEDA: el dominio no convierte dinero.</summary>
		public static string Gasto(Frases frases, decimal monto, string moneda, int cargas)
		{
			ArgumentNullException.ThrowIfNull(frases);

			return frases.Diga("gasto",
				("monto", frases.Monto(monto, moneda)),
				("cargas", frases.Cargas(cargas)));
		}

		/// <summary>LO QUE DE VERDAD LE IMPORTA A LA BILLETERA: cuanto cuesta cada kilometro.</summary>
		public static string Costo(Frases frases, decimal montoPorKilometro, string moneda,
			decimal kilometros)
		{
			ArgumentNullException.ThrowIfNull(frases);

			return frases.Diga("costo",
				("monto", frases.Monto(montoPorKilometro, moneda)),
				("kilometros", frases.Entera(kilometros)));
		}

		/// <summary>UN TRAMO: su cuenta cuando esta calculado, o que le falta cuando no.</summary>
		public static string Tramo(Frases frases, bool estaCalculado, decimal kilometros,
			decimal litros, decimal rendimiento, DateTime fechaDeLaCargaTrunca)
		{
			ArgumentNullException.ThrowIfNull(frases);

			if (!estaCalculado)
				return frases.Diga("tramo.bloqueado", ("fecha", frases.Dia(fechaDeLaCargaTrunca)));

			return frases.Diga("tramo.calculado",
				("kilometros", frases.Entera(kilometros)),
				("litros", frases.Cifra(litros)),
				("rendimiento", frases.Cifra(rendimiento)));
		}

		/// <summary>
		/// EL KILOMETRAJE CON SU PROCEDENCIA, que es la frase para quien lee la bitacora entera.
		/// La caratula muestra el numero solo: "observado" y "confirmado" son palabras del dominio
		/// que la persona no eligio.
		/// </summary>
		public static string Kilometraje(Frases frases, decimal kilometros, string unidad,
			string origen)
		{
			ArgumentNullException.ThrowIfNull(frases);
			if (origen.Length == 0) return "";

			return frases.Diga("kilometraje.conOrigen",
				("kilometraje", frases.ConUnidadEntera(kilometros, unidad)),
				("origen", frases.Diga("origen." + origen)));
		}

		/// <summary>EL TANQUE CON SU PROCEDENCIA, incluida la inferencia que prellena el switch.</summary>
		public static string Tanque(Frases frases, string claveDelEstado, string origen)
		{
			ArgumentNullException.ThrowIfNull(frases);
			if (claveDelEstado.Length == 0) return "";

			return frases.Diga("tanque.conOrigen",
				("tanque", frases.Diga("tanque." + claveDelEstado)),
				("origen", frases.Diga("origen." + origen)));
		}

		/// <summary>
		/// COMO SE DICE EL MOTIVO DE UNA RESERVA. Vive en la misma familia de claves que usa la
		/// clausula de <c>lectura.no-comparable</c>, y no en una propia: es la misma palabra en los
		/// dos lugares, y dos familias para un concepto se separan el dia que alguien toque una.
		/// </summary>
		public static string Reserva(Frases frases, string codigo)
		{
			ArgumentNullException.ThrowIfNull(frases);
			if (string.IsNullOrWhiteSpace(codigo)) return "";

			string dicho = frases.Diga("reserva." + codigo);
			return dicho.StartsWith("{", StringComparison.Ordinal) ? codigo : dicho;
		}

		/// <summary>
		/// COMO SE DICE EL NOMBRE DE UN ESTADO. El codigo es identidad y cruza el wire sin tocarse;
		/// esto es lo que una persona lee. Un codigo sin dicho se dice con su propio codigo, que es
		/// lo que la pantalla mostraba antes de que existiera esta tabla.
		/// </summary>
		public static string Etiqueta(Frases frases, string codigo)
		{
			ArgumentNullException.ThrowIfNull(frases);
			if (string.IsNullOrWhiteSpace(codigo)) return "";

			string dicho = frases.Diga("etiqueta." + codigo);
			return dicho.StartsWith("{", StringComparison.Ordinal) ? codigo : dicho;
		}
	}
}
