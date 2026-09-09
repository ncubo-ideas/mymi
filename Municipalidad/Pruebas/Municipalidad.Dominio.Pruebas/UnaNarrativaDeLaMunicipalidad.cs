using System;
using System.Collections.Generic;
using System.Reflection;
using Municipalidad.App.Events;
using Puppets.Portador;

namespace Municipalidad.Dominio.Pruebas
{
	// LA NARRATIVA QUE LA MUNICIPALIDAD PUBLICA, escrita a mano y en el orden en que un feed la manda.
	// Es el dato compartido de las pruebas de la frontera de ingesta, y existe por lo mismo que
	// ElExpedienteDeDemo: un barrido sobre una lista ve un tipo nuevo que nadie ruteo, y una prueba con
	// seis renglones escritos a mano no.
	//
	// NO LEE NINGUN RELOJ, y es la regla de ../CLAUDE.md sobre las tres formas de un instante recibido:
	// lo que GUIONA una secuencia recibida no toma reloj, porque reproducir la secuencia es todo el
	// punto. Cada instante de aca es un literal.
	//
	// EL ORDEN ES EL DE DEC-07: el encabezado del recibo ANTES de sus lineas, y la consulta ANTES de sus
	// pendientes. Una linea cuyo encabezado no llego se rechaza sin dejar entrada y no se reintenta, asi
	// que el orden es del feed y no del bucle.
	internal static class UnaNarrativaDeLaMunicipalidad
	{
		internal const string ElDiaDeLaConsulta = "2026-04-15T00:00:00Z";

		internal static SenalDeEvento Finca(long numero, string numeroDeFinca)
		{
			return new SenalDeEvento(numero, FincaIncorporada.ElTipoDelEvento,
				"{\"numeroDeFinca\":\"" + numeroDeFinca + "\"}");
		}

		internal static SenalDeEvento Avaluo(long numero, string numeroDeFinca, string fechaDelAvaluo,
			string valor)
		{
			return new SenalDeEvento(numero, AvaluoFijado.ElTipoDelEvento,
				"{\"numeroDeFinca\":\"" + numeroDeFinca + "\",\"fechaDelAvaluo\":\"" + fechaDelAvaluo + "\""
				+ ",\"valor\":" + valor + "}");
		}

		internal static SenalDeEvento Cobro(long numero, string rubro, string periodo, string monto,
			string puestoEl)
		{
			return new SenalDeEvento(numero, CobroPuesto.ElTipoDelEvento,
				"{\"nombreDelRubro\":\"" + rubro + "\",\"periodo\":\"" + periodo + "\""
				+ ",\"monto\":" + monto + ",\"puestoEl\":\"" + puestoEl + "\"}");
		}

		internal static SenalDeEvento Recibo(long numero, string numeroDeRecibo, string fechaDelPago,
			string descuento)
		{
			return new SenalDeEvento(numero, PagoRegistrado.ElTipoDelEvento,
				"{\"numeroDeRecibo\":\"" + numeroDeRecibo + "\",\"fechaDelPago\":\"" + fechaDelPago + "\""
				+ ",\"descuento\":" + descuento + "}");
		}

		internal static SenalDeEvento Linea(long numero, string numeroDeRecibo, string rubro,
			string periodo, string monto, string intereses)
		{
			return new SenalDeEvento(numero, LineaDePagoRegistrada.ElTipoDelEvento,
				"{\"numeroDeRecibo\":\"" + numeroDeRecibo + "\",\"nombreDelRubro\":\"" + rubro + "\""
				+ ",\"periodo\":\"" + periodo + "\",\"monto\":" + monto
				+ ",\"intereses\":" + intereses + "}");
		}

		internal static SenalDeEvento Consulta(long numero, string fechaDeLaConsulta)
		{
			return new SenalDeEvento(numero, ConsultaTomada.ElTipoDelEvento,
				"{\"fechaDeLaConsulta\":\"" + fechaDeLaConsulta + "\"}");
		}

		internal static SenalDeEvento Pendiente(long numero, string fechaDeLaConsulta, string rubro,
			string periodo, string monto, string intereses)
		{
			return new SenalDeEvento(numero, PendienteRegistrado.ElTipoDelEvento,
				"{\"fechaDeLaConsulta\":\"" + fechaDeLaConsulta + "\",\"nombreDelRubro\":\"" + rubro + "\""
				+ ",\"periodo\":\"" + periodo + "\",\"monto\":" + monto
				+ ",\"intereses\":" + intereses + "}");
		}

		// UNA DE CADA TIPO, en el orden en que un feed las manda. Es la lista que los barridos recorren,
		// asi que un tipo nuevo que no entre aca deja de tener testigo en toda la suite.
		internal static IEnumerable<SenalDeEvento> LaNarrativaEntera()
		{
			yield return Finca(1, "1-234-567");
			yield return Avaluo(2, "1-234-567", "2026-01-02T00:00:00Z", "45000000");
			yield return Cobro(3, "recoleccion-de-basura", "2026.01", "8000", "2026-01-05T00:00:00Z");
			yield return Recibo(4, "R-9001", "2026-02-10T00:00:00Z", "0");
			yield return Linea(5, "R-9001", "recoleccion-de-basura", "2026.01", "8000", "0");
			yield return Consulta(6, ElDiaDeLaConsulta);
			yield return Pendiente(7, ElDiaDeLaConsulta, "bienes-inmuebles", "2026.01", "45000", "1200");
		}

		// LOS TIPOS DE MENSAJE, POR REFLEXION Y NO CONTRA UNA LISTA ESCRITA A MANO. Enumerar los tipos es
		// enumerar los sitios donde la regla se decide (C57 de Concesionario): una lista a mano no puede
		// ver un tipo nuevo que nadie le agrego, y es justo el tipo nuevo el que se quiere ver.
		internal static IReadOnlyList<Type> LosTiposDeMensaje()
		{
			List<Type> tipos = new List<Type>();

			foreach (Type candidato in typeof(FincaIncorporada).Assembly.GetTypes())
				if (typeof(IMensajeDeEvento).IsAssignableFrom(candidato) && !candidato.IsInterface)
					tipos.Add(candidato);

			tipos.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));

			return tipos;
		}

		// El tag de un tipo, leido de su propia constante. Se lee y no se escribe a mano para que una
		// prueba no pueda afirmar de un tipo lo que ese tipo no dice de si mismo.
		internal static string ElTagDe(Type tipo)
		{
			FieldInfo tag = tipo.GetField("ElTipoDelEvento", BindingFlags.Public | BindingFlags.Static);

			return tag == null ? null : (string)tag.GetRawConstantValue();
		}

		// Corre el Interpret de UN tipo. Deliberadamente NO es una ruta: la ruta la escribe E-2, y una
		// que viviera aca haria que las pruebas midieran la ruta de las pruebas.
		internal static object Interpretar(Type tipo, SenalDeEvento senal)
		{
			MethodInfo interpret = tipo.GetMethod(
				"Interpret", BindingFlags.Public | BindingFlags.Static);

			if (interpret == null)
				throw new KeyNotFoundException(tipo.Name + " no declara un Interpret estatico y publico.");

			return interpret.Invoke(null, new object[] { senal });
		}

		// La misma senal con OTRO tag y el payload intacto.
		internal static SenalDeEvento ConElTag(SenalDeEvento senal, string tipo)
		{
			return new SenalDeEvento(senal.Numero, tipo, senal.Contenido);
		}

		// LAS CIRUGIAS DEL PAYLOAD. Cada una deja un JSON VALIDO: una que rompiera el JSON haria que la
		// senal la descartara el catch del parseo y no el guardia que la prueba dice medir, y el renglon
		// pasaria por la razon equivocada. Es la correccion que la pasada de falsificacion de T9 de
		// Concesionario forzo, y aca se hereda hecha.

		// El campo queda PRESENTE y en blanco.
		internal static SenalDeEvento EnBlanco(SenalDeEvento senal, string campo)
		{
			return Reemplazando(senal, campo, "\"\"");
		}

		// El campo se va del payload entero: es otro camino que el de arriba, porque Payload.Texto
		// contesta cadena vacia cuando la propiedad NO ESTA y es esa cadena la que el guardia mira.
		internal static SenalDeEvento Sin(SenalDeEvento senal, string campo)
		{
			string contenido = senal.Contenido;
			int abre = ComienzoDelCampo(contenido, campo);
			int cierra = FinDelValor(contenido, abre);

			string sinComa = contenido.Remove(abre, cierra - abre);
			sinComa = sinComa.Replace("{,", "{").Replace(",,", ",").Replace(",}", "}");

			return new SenalDeEvento(senal.Numero, senal.Tipo, sinComa);
		}

		// El campo queda con OTRO valor, escrito tal cual: "\"x\"" para un texto, "-1" para un numero,
		// "[1]" para una lista.
		internal static SenalDeEvento Diciendo(SenalDeEvento senal, string campo, string valorJson)
		{
			return Reemplazando(senal, campo, valorJson);
		}

		// Un numero que llega ENTRE COMILLAS, que es lo que manda de verdad un serializador que escribe
		// todo como texto. El JSON sigue siendo valido, asi que lo tiene que descartar la comprobacion de
		// que el valor es un numero y no el catch del parseo.
		internal static SenalDeEvento ComoTexto(SenalDeEvento senal, string campo)
		{
			string contenido = senal.Contenido;
			int abre = ComienzoDelCampo(contenido, campo);
			int inicioDelValor = contenido.IndexOf(':', abre) + 1;
			int cierra = FinDelValor(contenido, abre);

			string valor = contenido.Substring(inicioDelValor, cierra - inicioDelValor);

			return Reemplazando(senal, campo, "\"" + valor + "\"");
		}

		private static SenalDeEvento Reemplazando(SenalDeEvento senal, string campo, string valorJson)
		{
			string contenido = senal.Contenido;
			int abre = ComienzoDelCampo(contenido, campo);
			int inicioDelValor = contenido.IndexOf(':', abre) + 1;
			int cierra = FinDelValor(contenido, abre);

			return new SenalDeEvento(senal.Numero, senal.Tipo,
				contenido.Remove(inicioDelValor, cierra - inicioDelValor).Insert(inicioDelValor, valorJson));
		}

		// LA CIRUGIA REVIENTA SI EL CAMPO NO ESTA, a proposito: un nombre de campo mal escrito en una
		// tabla de prueba dejaria la senal intacta -- y una senal intacta se interpreta, asi que el
		// renglon se pondria rojo igual, pero leyendose como una regla que no se cumple en vez de como la
		// tabla que esta mal.
		private static int ComienzoDelCampo(string contenido, string campo)
		{
			int donde = contenido.IndexOf("\"" + campo + "\":");
			if (donde < 0)
				throw new KeyNotFoundException(
					"La senal no trae el campo '" + campo + "', asi que esta cirugia no cambia nada: "
					+ contenido);
			return donde;
		}

		private static int FinDelValor(string contenido, int desdeElCampo)
		{
			int coma = contenido.IndexOf(',', contenido.IndexOf(':', desdeElCampo));
			int llave = contenido.IndexOf('}', contenido.IndexOf(':', desdeElCampo));
			return coma >= 0 && coma < llave ? coma : llave;
		}
	}
}
