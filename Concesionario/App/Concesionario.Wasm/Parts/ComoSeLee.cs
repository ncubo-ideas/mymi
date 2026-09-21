using System.Collections.Generic;

namespace Concesionario.Wasm.Parts
{
	/// <summary>
	/// Como se le dice a la duena lo que el registro nombra en el vocabulario del dominio.
	///
	/// Aca SOLO se traducen NOMBRES CERRADOS: las nueve operaciones, los estados del calendario,
	/// las posturas del sobre. Son conjuntos que el dominio fija y que no crecen sin que alguien
	/// recompile, asi que una tabla es exactamente lo que hace falta y un nombre que no este en
	/// ella se muestra tal cual en vez de desaparecer.
	///
	/// LO QUE NO SE TRADUCE AQUI son las FRASES del dominio -- "Overdue, decided by distance..." --
	/// y no es un olvido. Esas frases las compone el dominio con los numeros adentro; reescribirlas
	/// en esta capa seria volver a decidir, en la pantalla, algo que ya se decidio en el registro, y
	/// la primera vez que las dos versiones no coincidieran ganaria la equivocada. Van citadas,
	/// literales, bajo "lo que dice tu registro". Traducirlas de verdad es trabajo del dominio.
	/// </summary>
	public static class ComoSeLee
	{
		private static readonly Dictionary<string, string> LasNueve = new()
		{
			["Inspection"] = "Revisión",
			["FluidChange"] = "Cambio de líquidos",
			["FilterChange"] = "Cambio de filtro",
			["WearPartReplacement"] = "Piezas de desgaste",
			["TimedComponent"] = "Componente por tiempo",
			["SoftwareUpdate"] = "Actualización de software",
			["RecallRemedy"] = "Llamado a revisión",
			["Diagnostic"] = "Diagnóstico",
			["Repair"] = "Reparación",
		};

		private static readonly Dictionary<string, string> Corto = new()
		{
			["Inspection"] = "Revisión",
			["FluidChange"] = "Líquidos",
			["FilterChange"] = "Filtro",
			["WearPartReplacement"] = "Desgaste",
			["TimedComponent"] = "Por tiempo",
			["SoftwareUpdate"] = "Software",
			["RecallRemedy"] = "Llamado",
			["Diagnostic"] = "Diagnóstico",
			["Repair"] = "Reparación",
		};

		private static readonly Dictionary<string, string> Estados = new()
		{
			["Overdue"] = "Vencido",
			["Due"] = "Toca ahora",
			["DueSoon"] = "Se acerca",
			["NotDue"] = "Al día",
			["Deferred"] = "Pospuesto",
			["Completed"] = "Realizado",
			["Missed"] = "No se hizo",
			["Superseded"] = "Reemplazado",
		};

		/// <summary>
		/// Las posturas del sobre y las de cobertura, que son otro conjunto: el sobre contesta
		/// Inside, Reached o NotPublished, y un sistema del alcance contesta una de cinco. Las dos
		/// comparten NotPublished y se dice igual en las dos.
		/// </summary>
		private static readonly Dictionary<string, string> Posturas = new()
		{
			["Inside"] = "Vigente",
			["Reached"] = "Alcanzada",
			["NotPublished"] = "Sin publicar",
			["Covered"] = "Cubierto",
			["CoveredWithExclusion"] = "Cubierto con exclusión",
			["NotCovered"] = "No cubierto",
			["OutsideEnvelope"] = "Fuera de los límites",
		};

		private static readonly Dictionary<string, string> Fluidos = new()
		{
			["EngineOil"] = "Aceite de motor",
			["BrakeFluid"] = "Líquido de frenos",
			["Coolant"] = "Refrigerante",
			["TransmissionFluid"] = "Líquido de transmisión",
			["PowerSteeringFluid"] = "Líquido de dirección",
			["WasherFluid"] = "Líquido limpiaparabrisas",
		};

		private static readonly Dictionary<string, string> Llantas = new()
		{
			["Front"] = "Delanteras",
			["Rear"] = "Traseras",
			["Spare"] = "Repuesto",
		};

		/// <summary>El nombre largo de una de las nueve.</summary>
		public static string Operacion(string kind) =>
			LasNueve.TryGetValue(kind, out string dicho) ? dicho : AppState.Spaced(kind);

		/// <summary>El nombre corto, para debajo de un punto de color.</summary>
		public static string OperacionCorta(string kind) =>
			Corto.TryGetValue(kind, out string dicho) ? dicho : AppState.Spaced(kind);

		/// <summary>El estado del calendario dicho como lo lee una persona.</summary>
		public static string Estado(string dueState) =>
			Estados.TryGetValue(dueState, out string dicho) ? dicho : AppState.Spaced(dueState);

		/// <summary>
		/// La postura del sobre, o la de un sistema del alcance, dicha como la lee una persona.
		/// </summary>
		public static string Postura(string standing) =>
			Posturas.TryGetValue(standing, out string dicho) ? dicho : AppState.Spaced(standing);

		/// <summary>
		/// El tono con que se pinta una postura del sobre. Los nombres son los del conjunto cerrado
		/// del dominio: adentro es la buena noticia, alcanzado es lo que hay que plantear, y que
		/// nadie haya publicado un sobre no es ninguna de las dos.
		/// </summary>
		public static string TonoDeLaPostura(string standing) => standing switch
		{
			"Inside" => "ok",
			"Covered" => "ok",
			"Reached" => "warn",
			"CoveredWithExclusion" => "warn",
			"NotCovered" => "danger",
			"OutsideEnvelope" => "danger",
			_ => "mute",
		};

		public static string Fluido(string kind) =>
			Fluidos.TryGetValue(kind, out string dicho) ? dicho : AppState.Spaced(kind);

		public static string Llanta(string position) =>
			Llantas.TryGetValue(position, out string dicho) ? dicho : AppState.Spaced(position);

		/// <summary>
		/// El tono con que se pinta un estado, que es el sufijo de la clase de css que lo pinta. Aca
		/// "NotDue" es VERDE y no neutro: para quien maneja el carro, "no toca" es la buena noticia, y
		/// pintarla gris al lado de dos rojos hace que la pantalla se vea peor de lo que el registro
		/// dice. Un estado que la lista no nombre se pinta neutro en vez de desaparecer.
		/// </summary>
		public static string Tono(string dueState) => dueState switch
		{
			"Overdue" => "danger",
			"Missed" => "danger",
			"Due" => "warn",
			"DueSoon" => "warn",
			"Deferred" => "warn",
			"NotDue" => "ok",
			"Completed" => "ok",
			_ => "mute",
		};

		/// <summary>El dibujo que acompana a un estado en el semaforo.</summary>
		public static string Dibujo(string dueState) => dueState switch
		{
			"Overdue" => "alert",
			"Missed" => "alert",
			"Due" => "clock",
			"DueSoon" => "clock",
			"Deferred" => "calendar",
			_ => "check",
		};

		private static readonly Dictionary<string, string> DibujosDeOperacion = new()
		{
			["Inspection"] = "wrench",
			["FluidChange"] = "drop",
			["FilterChange"] = "filter",
			["WearPartReplacement"] = "tyre",
			["TimedComponent"] = "clock",
			["SoftwareUpdate"] = "chip",
			["RecallRemedy"] = "phone",
			["Diagnostic"] = "pulse",
			["Repair"] = "wrench",
		};

		/// <summary>
		/// El dibujo que representa QUE es la operacion (no en que estado esta). Es lo que va adentro
		/// del punto en "Un vistazo a tu carro": el tono del punto ya dice si esta al dia o no, asi que
		/// el icono queda libre para decir de que operacion se trata.
		/// </summary>
		public static string DibujoOperacion(string kind) =>
			DibujosDeOperacion.TryGetValue(kind, out string dibujo) ? dibujo : "check";

		/// <summary>
		/// El formato de numero y los nombres de los meses van escritos aca y no salen de una
		/// CultureInfo. Un Blazor WASM puede publicarse con globalizacion invariante -- y si se
		/// publica asi, pedirle la cultura "es-CR" revienta en el navegador y no en el build. Son
		/// un separador de miles y doce palabras: no vale arrastrar ICU ni arriesgar ese fallo.
		/// </summary>
		private static readonly System.Globalization.NumberFormatInfo ComoSeEscribenLosNumeros =
			new() { NumberGroupSeparator = ".", NumberDecimalSeparator = ",", NumberGroupSizes = new[] { 3 } };

		private static readonly string[] LosMeses =
		{
			"enero", "febrero", "marzo", "abril", "mayo", "junio",
			"julio", "agosto", "setiembre", "octubre", "noviembre", "diciembre"
		};

		/// <summary>
		/// Un numero con separador de miles como se escribe en Costa Rica. Es formato de pantalla:
		/// el numero que cruzo la puerta sigue siendo el entero que el registro guarda.
		/// </summary>
		public static string Miles(int value) => value.ToString("#,##0", ComoSeEscribenLosNumeros);

		/// <summary>
		/// Una fecha del registro (yyyy-MM-dd, tal como la componen las caratulas) dicha en palabras.
		/// Si no calza con ese molde se devuelve intacta: inventarle un dia a un texto que no lo
		/// tiene seria peor que mostrarlo crudo.
		/// </summary>
		public static string Dia(string yyyymmdd)
		{
			if (string.IsNullOrWhiteSpace(yyyymmdd)) return "";

			if (!System.DateTime.TryParseExact(
					yyyymmdd.Length > 10 ? yyyymmdd[..10] : yyyymmdd,
					"yyyy-MM-dd",
					System.Globalization.CultureInfo.InvariantCulture,
					System.Globalization.DateTimeStyles.None,
					out System.DateTime day))
				return yyyymmdd;

			return day.Day + " de " + LosMeses[day.Month - 1] + " de " + day.Year;
		}

		/// <summary>La misma fecha, corta, para una linea de tiempo.</summary>
		public static string DiaCorto(string yyyymmdd)
		{
			if (string.IsNullOrWhiteSpace(yyyymmdd)) return "";

			if (!System.DateTime.TryParseExact(
					yyyymmdd.Length > 10 ? yyyymmdd[..10] : yyyymmdd,
					"yyyy-MM-dd",
					System.Globalization.CultureInfo.InvariantCulture,
					System.Globalization.DateTimeStyles.None,
					out System.DateTime day))
				return yyyymmdd;

			return day.Day + " " + LosMeses[day.Month - 1][..3] + " " + day.Year;
		}
	}
}
