using System;
using System.Collections.Generic;
using System.Text.Json;
using Municipalidad.App.Events;
using NUnit.Framework;
using Puppets.Portador;

namespace Municipalidad.Dominio.Pruebas
{
	// LA FRONTERA DE LA INGESTA: null DESCARTA, y nunca se inventa un comando para tragarse una senal.
	//
	// POR QUE IMPORTA. El motor escribe la entrada del journal ANTES de correr el cuerpo del comando,
	// asi que una senal mal formada que llegue a un comando revienta DESPUES de haber sido escrita, y esa
	// entrada queda para siempre: el motor loguea un fallo de rehidratacion en cada arranque futuro de esa
	// cuenta. Descartar en la frontera es la unica respuesta que cuesta cero.
	//
	// DONDE ESTA LA LINEA EN ESTE MODULO. El interprete juzga la SENAL: el tag, el numero de evento, que
	// el payload sea JSON, que un campo este, que diga lo que dice decir, y que un numero caiga en una
	// escala que existe. Todo lo que hay que saber de la CUENTA para juzgarlo -- que el rubro exista, que
	// su cadencia lea ese periodo, que el monto sea positivo, que el recibo no estuviera ya, que el
	// encabezado haya llegado -- lo contesta el Check de PerformCheckThenCommand, que tampoco deja
	// entrada al rechazar. Es DEC-01 aplicado al feed: EsUnNombreConocido se pregunta ADENTRO del Check.
	//
	// LA UNICA EXCEPCION ES EL PERIODO, y tiene un motivo de mecanica y no de gusto. MunicipalidadAPI lo
	// parte con una guarda de C# ANTES del perform, asi que un texto fuera del alfabeto del recibo no
	// deja cicatriz: SALE del Aplicar con una excepcion y para el bloque de refresh ENTERO. Por eso el
	// interprete lo pregunta, y lo pregunta al predicado total de la puerta en vez de reescribir la
	// regla al lado.
	[TestFixture]
	public sealed class ASignalThatIsNotOursOrIsMalformedIsDiscarded
	{
		// EL TAG ES EL DISCRIMINADOR, y el barrido pasa por los seis. Un payload que calza campo por
		// campo sigue sin ser el hecho: lo que dice QUE hecho es, es el tag.
		[Test]
		public void ASignalWithOurPayloadButAnotherTypeIsStillNotOurs()
		{
			Assert.Multiple(() =>
			{
				foreach (SenalDeEvento senal in UnaNarrativaDeLaMunicipalidad.LaNarrativaEntera())
				{
					Type suyo = ElTipoDe(senal);

					Assert.That(
						UnaNarrativaDeLaMunicipalidad.Interpretar(
							suyo, UnaNarrativaDeLaMunicipalidad.ConElTag(senal, "AvisoDeCorteDeAgua")),
						Is.Null,
						"El payload de " + senal.Tipo + " calza campo por campo con el suyo y aun asi no "
						+ "es ese hecho: lo que dice QUE hecho es, es el tag.");
				}
			});
		}

		// Y QUE PASA CON EL PAYLOAD DE UNO PUESTO BAJO EL TAG DE OTRO. Es el hueco que el barrido de
		// arriba no ve: ahi el tag ajeno es de un tipo que la app no consume, asi que descartarlo no
		// prueba nada sobre como se distinguen estos seis ENTRE SI.
		//
		// LA PROPIEDAD QUE DE VERDAD SE CUMPLE NO ES "NINGUNO", Y ESTA PRUEBA LO ENCONTRO. La primera
		// version afirmaba que ningun payload contesta al tag de otro, y se puso ROJA: el payload de un
		// PendienteRegistrado trae `fechaDeLaConsulta`, que es EXACTAMENTE Y LO UNICO que exige
		// ConsultaTomada, asi que bajo ese tag se lee como una consulta perfectamente valida.
		//
		// No es un defecto del interprete y no se arregla adentro de el: exigirle a ConsultaTomada que el
		// payload NO traiga nada mas la romperia el dia que el feed le agregue un campo, que es
		// justamente lo que un feed hace. Lo que se puede afirmar -- y lo que se afirma -- es la regla
		// que lo explica: un payload contesta al tag de otro tipo EXACTAMENTE cuando los campos de ese
		// otro son un subconjunto de los suyos. La relacion se calcula de los payloads mismos, no de una
		// lista escrita a mano, asi que un tipo nuevo no puede meterse en la excepcion en silencio.
		//
		// LA CONSECUENCIA, DICHA: para ese par el tag es el UNICO discriminador. Una senal mal etiquetada
		// por el feed entra como el hecho que su tag dice, y eso es correcto -- lo que dice QUE hecho es,
		// es el tag -- pero significa que E-2 no tiene una segunda red debajo para ese par.
		[Test]
		public void APayloadAnswersAnotherTypesTagExactlyWhenItCarriesThatTypesFields()
		{
			List<SenalDeEvento> narrativa =
				new List<SenalDeEvento>(UnaNarrativaDeLaMunicipalidad.LaNarrativaEntera());

			int pares = 0;
			int contenidos = 0;

			Assert.Multiple(() =>
			{
				foreach (SenalDeEvento senal in narrativa)
					foreach (SenalDeEvento otra in narrativa)
					{
						if (otra.Tipo == senal.Tipo) continue;

						pares++;

						Type otro = ElTipoDe(otra);
						bool cabe = LosCamposDe(otra).IsSubsetOf(LosCamposDe(senal));
						object leido = UnaNarrativaDeLaMunicipalidad.Interpretar(
							otro, UnaNarrativaDeLaMunicipalidad.ConElTag(senal, otra.Tipo));

						if (cabe)
						{
							contenidos++;

							Assert.That(leido, Is.Not.Null,
								"Los campos de " + otra.Tipo + " estan todos en el payload de "
								+ senal.Tipo + ", asi que bajo ese tag se lee -- y decirlo es el punto "
								+ "de este renglon: para ese par el tag es el unico discriminador.");
						}
						else
						{
							Assert.That(leido, Is.Null,
								"El payload de " + senal.Tipo + " con el tag de " + otro.Name + " le "
								+ "falta algun campo que " + otro.Name + " exige, asi que no es uno.");
						}
					}
			});

			Assert.That(pares, Is.EqualTo(42),
				"Son SIETE senales por los SEIS tipos que no son el suyo. Si esta cuenta baja, la "
				+ "narrativa dejo de traer una de cada tipo y el barrido mide menos de lo que dice.");

			Assert.That(contenidos, Is.EqualTo(2),
				"Y hay DOS pares contenidos en los cuarenta y dos, cada uno decidido:\n"
				+ "  ConsultaTomada dentro de PendienteRegistrado -- los campos de la primera son uno "
				+ "solo, el dia, y el pendiente cuelga de esa misma consulta.\n"
				+ "  FincaIncorporada dentro de AvaluoFijado -- los campos de la primera son uno solo, "
				+ "el numero de finca, y todo avaluo lo trae porque es de una finca. El segundo par "
				+ "entro con el avaluo, y es aceptable por lo mismo que el primero: lo que dice QUE "
				+ "hecho es una senal, es su tag. Si este numero sube otra vez, hay un tercer par que "
				+ "solo el tag separa y alguien tiene que haberlo decidido.");
		}

		private static HashSet<string> LosCamposDe(SenalDeEvento senal)
		{
			HashSet<string> campos = new HashSet<string>();

			using (JsonDocument documento = JsonDocument.Parse(senal.Contenido))
				foreach (JsonProperty campo in documento.RootElement.EnumerateObject())
					campos.Add(campo.Name);

			return campos;
		}

		// LOS TRES DESCARTES QUE TODO HECHO COMPARTE, barridos sobre los seis: ninguno de los tres puede
		// quedar cubierto por casualidad en cinco de ellos.
		[Test]
		public void EveryEventChecksItsTagItsEventNumberAndItsPayload()
		{
			Assert.Multiple(() =>
			{
				foreach (SenalDeEvento senal in UnaNarrativaDeLaMunicipalidad.LaNarrativaEntera())
				{
					Type suyo = ElTipoDe(senal);

					Assert.That(
						UnaNarrativaDeLaMunicipalidad.Interpretar(
							suyo, new SenalDeEvento(0, senal.Tipo, senal.Contenido)),
						Is.Null,
						"Un numero de evento que no avanza desde cero no se puede journalizar como punto "
						+ "de control: " + senal.Tipo);

					Assert.That(
						UnaNarrativaDeLaMunicipalidad.Interpretar(
							suyo, new SenalDeEvento(-3, senal.Tipo, senal.Contenido)),
						Is.Null,
						"Y uno negativo tampoco: " + senal.Tipo);

					Assert.That(
						UnaNarrativaDeLaMunicipalidad.Interpretar(
							suyo, new SenalDeEvento(senal.Numero, senal.Tipo, "")),
						Is.Null,
						"Sin payload no hay hecho: " + senal.Tipo);

					Assert.That(
						UnaNarrativaDeLaMunicipalidad.Interpretar(
							suyo, new SenalDeEvento(senal.Numero, senal.Tipo, "no soy json")),
						Is.Null,
						"Y un payload que no parsea se descarta en vez de reventar: " + senal.Tipo);
				}
			});
		}

		// EL BARRIDO ES POR SITIO DE VALIDACION Y NO POR TIPO DE HECHO (C57 de Concesionario). Seis clases
		// de mensaje tienen SEIS textos obligatorios, porque un hecho trae varios y otro ninguno: la
		// linea del recibo trae dos y la consulta no trae ninguno. El `periodo` tambien es un texto y
		// NO se cuenta aca: tiene su propio barrido mas abajo, porque no se juzga por estar en blanco
		// sino por estar en el alfabeto del recibo.
		//
		// CADA UNO SE MIDE DOS VECES, en blanco y AUSENTE, porque son dos caminos distintos: Payload.Texto
		// contesta cadena vacia cuando la propiedad NO ESTA, y es esa cadena la que el guardia mira. Un
		// blanco prueba el recorte; una ausencia prueba que no hay que buscar la propiedad dos veces.
		[Test]
		public void EveryRequiredTextIsRequiredBlankAndAbsent()
		{
			(string Donde, Type Tipo, SenalDeEvento Senal, string Campo)[] textos =
			{
				("finca.numeroDeFinca",  typeof(FincaIncorporada),      LaFinca(),     "numeroDeFinca"),
				("cobro.nombreDelRubro", typeof(CobroPuesto),           ElCobro(),     "nombreDelRubro"),
				("recibo.numeroDeRecibo", typeof(PagoRegistrado),       ElRecibo(),    "numeroDeRecibo"),
				("linea.numeroDeRecibo", typeof(LineaDePagoRegistrada), LaLinea(),     "numeroDeRecibo"),
				("linea.nombreDelRubro", typeof(LineaDePagoRegistrada), LaLinea(),     "nombreDelRubro"),
				("pendiente.nombreDelRubro",
					typeof(PendienteRegistrado), ElPendiente(), "nombreDelRubro"),
			};

			Assert.That(textos.Length, Is.EqualTo(6),
				"Son SEIS textos obligatorios y no seis porque haya seis hechos: la linea trae DOS y la "
				+ "consulta ninguno -- una consulta es su dia y nada mas.");

			Assert.Multiple(() =>
			{
				foreach ((string donde, Type tipo, SenalDeEvento senal, string campo) in textos)
				{
					Assert.That(
						UnaNarrativaDeLaMunicipalidad.Interpretar(
							tipo, UnaNarrativaDeLaMunicipalidad.EnBlanco(senal, campo)),
						Is.Null,
						"Un " + donde + " en blanco tiene que DESCARTAR la senal: un nombre en blanco no "
						+ "nombra nada.");

					Assert.That(
						UnaNarrativaDeLaMunicipalidad.Interpretar(
							tipo, UnaNarrativaDeLaMunicipalidad.Sin(senal, campo)),
						Is.Null,
						"Y ausente tambien, que es el OTRO camino: " + donde);
				}
			});
		}

		// UN INSTANTE AUSENTE se leeria como el ano cero, y uno que no dice un instante no esta diciendo
		// uno menos preciso: no esta diciendo ninguno.
		[Test]
		public void EveryRequiredInstantMustBePresentAndReadable()
		{
			(string Donde, Type Tipo, SenalDeEvento Senal, string Campo)[] instantes =
			{
				("cobro.puestoEl",             typeof(CobroPuesto),         ElCobro(),     "puestoEl"),
				("recibo.fechaDelPago",        typeof(PagoRegistrado),      ElRecibo(),    "fechaDelPago"),
				("consulta.fechaDeLaConsulta", typeof(ConsultaTomada),      LaConsulta(),  "fechaDeLaConsulta"),
				("pendiente.fechaDeLaConsulta",
					typeof(PendienteRegistrado), ElPendiente(), "fechaDeLaConsulta"),
			};

			Assert.That(instantes.Length, Is.EqualTo(4),
				"Son CUATRO instantes obligatorios: el dia en que se puso al cobro, el dia del pago, el "
				+ "dia de la consulta y el dia de la consulta que destapo el pendiente. La finca y la "
				+ "linea del recibo no traen ninguno -- la linea la fecha su encabezado.");

			Assert.Multiple(() =>
			{
				foreach ((string donde, Type tipo, SenalDeEvento senal, string campo) in instantes)
				{
					Assert.That(
						UnaNarrativaDeLaMunicipalidad.Interpretar(
							tipo, UnaNarrativaDeLaMunicipalidad.Sin(senal, campo)),
						Is.Null,
						"Un " + donde + " ausente tiene que DESCARTAR la senal: sin el, la fecha se "
						+ "leeria como el ano cero.");

					Assert.That(
						UnaNarrativaDeLaMunicipalidad.Interpretar(
							tipo, UnaNarrativaDeLaMunicipalidad.Diciendo(senal, campo, "\"ayer\"")),
						Is.Null,
						"Y uno que no dice un instante tampoco: " + donde);
				}
			});
		}

		// UN NUMERO AUSENTE NO ES UN CERO. Cero es una afirmacion -- un recibo sin descuento, una linea
		// pagada a tiempo -- y un evento que omite el campo no la esta haciendo.
		//
		// Y UN NEGATIVO ES UNA ESCALA QUE NO EXISTE, que es la mitad de la validacion que si se puede
		// adelantar a la frontera: mira UN valor de la senal y nada del estado. Lo que NO se adelanta es
		// el umbral del dominio -- que un monto al cobro tenga que ser POSITIVO -- porque copiarlo aca
		// seria una segunda fuente de verdad para una regla que ya vive en un Puede.
		[Test]
		public void EveryRequiredAmountMustBePresentAndNotNegative()
		{
			(string Donde, Type Tipo, SenalDeEvento Senal, string Campo)[] montos =
			{
				("cobro.monto",          typeof(CobroPuesto),           ElCobro(),     "monto"),
				("recibo.descuento",     typeof(PagoRegistrado),        ElRecibo(),    "descuento"),
				("linea.monto",          typeof(LineaDePagoRegistrada), LaLinea(),     "monto"),
				("linea.intereses",      typeof(LineaDePagoRegistrada), LaLinea(),     "intereses"),
				("pendiente.monto",      typeof(PendienteRegistrado),   ElPendiente(), "monto"),
				("pendiente.intereses",  typeof(PendienteRegistrado),   ElPendiente(), "intereses"),
			};

			Assert.That(montos.Length, Is.EqualTo(6),
				"Son SEIS posiciones numericas en cuatro de los seis hechos: el cobro trae una, el "
				+ "encabezado del recibo una, y la linea y el pendiente dos cada uno.");

			Assert.Multiple(() =>
			{
				foreach ((string donde, Type tipo, SenalDeEvento senal, string campo) in montos)
				{
					Assert.That(
						UnaNarrativaDeLaMunicipalidad.Interpretar(
							tipo, UnaNarrativaDeLaMunicipalidad.Sin(senal, campo)),
						Is.Null,
						"Un " + donde + " ausente tiene que DESCARTAR la senal, no entrar como cero.");

					Assert.That(
						UnaNarrativaDeLaMunicipalidad.Interpretar(
							tipo, UnaNarrativaDeLaMunicipalidad.Diciendo(senal, campo, "-1")),
						Is.Null,
						"Y un " + donde + " negativo tampoco pasa: no hay cobro, mora ni descuento "
						+ "negativos.");

					SenalDeEvento comoTexto = UnaNarrativaDeLaMunicipalidad.ComoTexto(senal, campo);

					Assert.That(comoTexto.Contenido, Does.Contain("\"" + campo + "\":\""),
						"La cirugia tiene que dejar un JSON VALIDO con el numero entre comillas: si deja "
						+ "un JSON roto, lo descarta el catch del parseo y este renglon mide otra cosa. "
						+ donde);

					Assert.That(
						UnaNarrativaDeLaMunicipalidad.Interpretar(tipo, comoTexto),
						Is.Null,
						"Y un " + donde + " que llega entre comillas es una senal malformada, no un "
						+ "monto: un serializador que escribe todo como texto es lo que manda un feed de "
						+ "verdad.");
				}
			});
		}

		// EL ALFABETO DEL RECIBO ES AAAA.NN, Y ESTE ES EL DESCARTE QUE NO SE PODIA DEJAR AL CHECK. La
		// puerta parte el texto con una guarda de C# ANTES del perform, asi que un periodo fuera del
		// alfabeto no deja cicatriz: SALE del Aplicar y para el bloque de refresh entero.
		//
		// "2026-01" ES EL CASO QUE IMPORTA y no un texto cualquiera: es el OTRO alfabeto, el que el
		// dominio usa en sus etiquetas, asi que es la confusion que un feed real cometeria.
		[Test]
		public void EveryPeriodMustBeWrittenTheWayTheReceiptWritesIt()
		{
			(string Donde, Type Tipo, SenalDeEvento Senal)[] periodos =
			{
				("cobro.periodo",     typeof(CobroPuesto),           ElCobro()),
				("linea.periodo",     typeof(LineaDePagoRegistrada), LaLinea()),
				("pendiente.periodo", typeof(PendienteRegistrado),   ElPendiente()),
			};

			Assert.That(periodos.Length, Is.EqualTo(3),
				"Son TRES hechos que traen un periodo: el cobro, la linea del recibo y el pendiente. La "
				+ "finca, el encabezado del recibo y la consulta no traen ninguno.");

			string[] queNoLoDicen = { "\"2026-01\"", "\"2026\"", "\"2026.\"", "\"abril.2026\"",
				"\"2026.01.15\"", "\"\"", "\"   \"", "\"-2026.01\"" };

			Assert.Multiple(() =>
			{
				foreach ((string donde, Type tipo, SenalDeEvento senal) in periodos)
				{
					Assert.That(
						UnaNarrativaDeLaMunicipalidad.Interpretar(
							tipo, UnaNarrativaDeLaMunicipalidad.Sin(senal, "periodo")),
						Is.Null,
						"Un " + donde + " ausente tiene que DESCARTAR la senal.");

					foreach (string texto in queNoLoDicen)
						Assert.That(
							UnaNarrativaDeLaMunicipalidad.Interpretar(
								tipo, UnaNarrativaDeLaMunicipalidad.Diciendo(senal, "periodo", texto)),
							Is.Null,
							"Un " + donde + " que dice " + texto + " tiene que DESCARTAR la senal: "
							+ "llegando al comando, la puerta reventaria ANTES del perform y se llevaria "
							+ "el bloque de refresh entero.");
				}
			});
		}

		// LO QUE LA FRONTERA NO JUZGA, dicho en positivo y medido: un periodo BIEN ESCRITO que la cadencia
		// del rubro no puede leer PASA la frontera, para que lo refuse el Check con sus palabras. "2026.13"
		// no es un mes y "2026.05" no es un trimestre -- pero cual de las dos cosas es el 5 depende del
		// RUBRO, y el interprete no sabe de rubros. Copiar esa regla aca seria una segunda fuente de verdad
		// para PuedeLeerPeriodo.
		[Test]
		public void APeriodTheRubrosCadenceCannotReadStillCrossesTheBoundary()
		{
			Assert.That(
				CobroPuesto.Interpret(
					UnaNarrativaDeLaMunicipalidad.Diciendo(ElCobro(), "periodo", "\"2026.13\"")),
				Is.Not.Null,
				"Esta bien escrito en el alfabeto del recibo, asi que la puerta lo parte sin reventar y "
				+ "el Check lo rechaza sin dejar entrada. Que el 13 no sea un mes es del dominio.");
		}

		// Y UN CERO SE PUEDE DECIR, que es la otra mitad de la linea: lo que la frontera descarta es lo que
		// NO SE PUEDE DECIR. Un descuento en cero es un recibo sin descuento y una mora en cero es una
		// linea pagada a tiempo -- las dos son afirmaciones legitimas de este feed. Que un monto AL COBRO
		// en cero no sirva es una ley de la cuenta, y la dice PuedePonerAlCobro.
		[Test]
		public void AZeroIsSayableAndTheBoundaryLetsItThrough()
		{
			Assert.That(
				PagoRegistrado.Interpret(ElRecibo()).DescuentoDelRecibo, Is.EqualTo(0m),
				"Un recibo sin descuento es el caso normal.");

			Assert.That(
				LineaDePagoRegistrada.Interpret(LaLinea()).InteresesDeLaLinea, Is.EqualTo(0m),
				"Y una linea sin mora es una linea pagada a tiempo.");

			Assert.That(
				CobroPuesto.Interpret(
					UnaNarrativaDeLaMunicipalidad.Diciendo(ElCobro(), "monto", "0")),
				Is.Not.Null,
				"Y un monto al cobro en cero cruza la frontera para que lo refuse LA CUENTA, con sus "
				+ "palabras: el umbral es del dominio y adelantarlo aca seria copiarlo.");
		}

		// ------------------------------------------------------------------ las seis senales, una a una

		private static SenalDeEvento LaFinca()
		{
			return UnaNarrativaDeLaMunicipalidad.Finca(1, "1-234-567");
		}

		private static SenalDeEvento ElCobro()
		{
			return UnaNarrativaDeLaMunicipalidad.Cobro(
				2, "recoleccion-de-basura", "2026.01", "8000", "2026-01-05T00:00:00Z");
		}

		private static SenalDeEvento ElRecibo()
		{
			return UnaNarrativaDeLaMunicipalidad.Recibo(3, "R-9001", "2026-02-10T00:00:00Z", "0");
		}

		private static SenalDeEvento LaLinea()
		{
			return UnaNarrativaDeLaMunicipalidad.Linea(
				4, "R-9001", "recoleccion-de-basura", "2026.01", "8000", "0");
		}

		private static SenalDeEvento LaConsulta()
		{
			return UnaNarrativaDeLaMunicipalidad.Consulta(
				5, UnaNarrativaDeLaMunicipalidad.ElDiaDeLaConsulta);
		}

		private static SenalDeEvento ElPendiente()
		{
			return UnaNarrativaDeLaMunicipalidad.Pendiente(
				6, UnaNarrativaDeLaMunicipalidad.ElDiaDeLaConsulta, "bienes-inmuebles", "2026.01",
				"45000", "1200");
		}

		private static Type ElTipoDe(SenalDeEvento senal)
		{
			foreach (Type tipo in UnaNarrativaDeLaMunicipalidad.LosTiposDeMensaje())
				if (UnaNarrativaDeLaMunicipalidad.ElTagDe(tipo) == senal.Tipo) return tipo;

			throw new KeyNotFoundException(
				"Ningun tipo de mensaje se llama '" + senal.Tipo + "', asi que la narrativa trae una "
				+ "senal que nadie puede interpretar.");
		}
	}
}
