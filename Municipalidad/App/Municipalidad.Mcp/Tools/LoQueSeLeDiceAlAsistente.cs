using System.Collections.Generic;

namespace Municipalidad.Mcp.Tools
{
	/// <summary>
	/// Lo que se le dice al asistente cuando la respuesta no puede ser la que pidio. Vive en un solo
	/// sitio porque todas las familias de tools contestan lo mismo ante lo mismo.
	/// </summary>
	internal static class LoQueSeLeDiceAlAsistente
	{
		/// <summary>Que la app nunca se abrio en esta computadora, que es un estado y no un error.</summary>
		internal const string LaAppNuncaSeAbrio =
			"La app todavía no se ha abierto en esta computadora, así que no hay nada registrado en esta "
			+ "carpeta. Pídale a la persona que abra su app de la municipalidad una vez; no es un error, "
			+ "y no es algo que usted ni la municipalidad puedan arreglar desde aquí.";

		/// <summary>
		/// Que lo que la app tiene es lo que la municipalidad le mando hasta el ultimo refresco, y que
		/// este proceso no puede traer mas.
		/// </summary>
		internal const string LoQueFaltaLoTraeUnRefresco =
			"Esta respuesta es sobre lo que la app ya trajo. La app trae hechos nuevos de la "
			+ "municipalidad cuando la persona la abre o la refresca, y usted NO puede disparar eso. Si "
			+ "la persona esperaba ver algo que no aparece, lo más probable es que su app todavía no lo "
			+ "haya traído, no que no exista: pídale que abra su app y vuelva a consultar.";

		/// <summary>
		/// Que la cuenta es hibrida y que lo que la persona no aporto no lo puede aportar el asistente.
		/// </summary>
		internal const string LoQueAportaLaPersonaNoLoAportaUsted =
			"ESTA CUENTA ES HÍBRIDA. La municipalidad manda los cobros, los recibos y la mora; la persona "
			+ "aporta lo que la municipalidad no puede saber: cuánto quiere apartar por mes y el valor de "
			+ "su finca si lo declaró. Cuando falta un aporte de la persona, la respuesta lo dice con "
			+ "palabras. No lo llene con un número plausible y no se lo pida a la persona dentro de esta "
			+ "conversación: lo pone ella en su app, y aquí no hay por dónde escribirlo.";

		/// <summary>
		/// Que la frase que compuso el dominio es la respuesta y no una decoracion sobre los campos.
		/// </summary>
		internal const string LaFraseEsLaRespuesta =
			"LO QUE DICE dice ES LA RESPUESTA, y la compuso el dominio, que es el dueño de las reglas. "
			+ "Dígala. No la rearme a partir de los otros campos ni la reescriba como consejo de "
			+ "plantilla del tipo 'manténgase al día con sus pagos': no tiene de dónde sacar eso, y la "
			+ "persona ya lo escuchó.";

		/// <summary>
		/// Que el nombre del rubro que viaja es una llave y no un titulo, y que nadie escribio el titulo.
		/// </summary>
		internal const string ElRubroViajaComoLlave =
			"EL NOMBRE DEL RUBRO VIAJA EN MINÚSCULAS Y CON GUIONES -- 'recoleccion-de-basura' -- porque "
			+ "es el nombre con que el registro lo identifica y nadie ha escrito todavía un título para "
			+ "leerlo en voz alta. Puede decirlo tal cual o leerlo natural, pero no lo traduzca a otro "
			+ "nombre ni lo abrevie: si lo cambia, deja de coincidir con lo que la persona ve en su app "
			+ "y en su recibo.";

		/// <summary>
		/// Que esta app no verifica lo que la municipalidad cobro, y por que eso es una decision.
		/// </summary>
		internal const string LaAppRecibeYAvisaPeroNoVerifica =
			"ESTA APP RECIBE Y AVISA; NO VERIFICA. Nombra lo que ve -- esto es un avalúo, esto vence el "
			+ "viernes, esto no tiene comprobante -- y nunca corrige ni discute lo que la municipalidad "
			+ "cobró. Si la persona cree que un monto está mal, eso se plantea con la municipalidad; no "
			+ "lo recalcule aquí ni le diga que el cobro está equivocado.";

		/// <summary>
		/// Lo que hay que decirle al asistente sobre cualquiera de los cinco umbrales sin firmar. Los
		/// numeros son de <see cref="Municipalidad.App.LosJuiciosSinFirmar"/>, que las dos caratulas
		/// comparten; estas palabras son de esta sola, porque estan escritas para un asistente.
		/// </summary>
		internal const string NadieLosHaFirmado =
			"EL NÚMERO CON QUE SE PREGUNTÓ ES UNA PROPUESTA DE ESTA APP Y NADIE LO HA FIRMADO TODAVÍA. "
			+ "Lo propuso quien modeló el dominio leyendo seis recibos, y está esperando a la persona que "
			+ "conoce el trámite en la municipalidad. Diga cuál fue el número: la respuesta cambia si el "
			+ "número cambia, y quien la lea tiene derecho a saber contra qué se midió. No lo presente "
			+ "como una regla de la municipalidad, y no lo cambie usted.";

		/// <summary>Lo que hay que decirle sobre el unico que si esta firmado.</summary>
		internal const string EsteSiEstaFirmado =
			"El umbral con que se preguntó lo firmó la persona el 2026-09-03, así que este sí es un "
			+ "criterio acordado y no una propuesta. Dígalo igual: la respuesta se midió contra él.";

		/// <summary>Lo que hay que decir de una tasa que esta app usa solo para proyectar.</summary>
		internal const string LaTasaSoloProyecta =
			"LA TASA CON QUE SE PROYECTÓ NO ES LA MORA QUE LA MUNICIPALIDAD COBRA. Esta app recibe la "
			+ "mora en el recibo y no la recalcula ni la discute; esta tasa se reconstruyó de recibos "
			+ "viejos y sirve para dar una idea de lo que costaría pagar tarde. La mora de verdad llega "
			+ "en el recibo. No la presente como un cobro, y no la sume a nada que la municipalidad haya "
			+ "puesto al cobro.";

		/// <summary>Une las frases que haya, sin dejar espacios de mas cuando falta alguna.</summary>
		internal static string TodoLoQueValeLaPenaDecir(params string[] frases)
		{
			List<string> dice = new List<string>();

			foreach (string cada in frases)
				if (!string.IsNullOrEmpty(cada)) dice.Add(cada);

			return string.Join(" ", dice);
		}
	}
}
