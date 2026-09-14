using System;
using System.Collections.Generic;
using LasFrases = Municipalidad.App.Frases.Frases;
using LaRedaccion = Municipalidad.App.Frases.LaRedaccion;
using NUnit.Framework;

namespace Municipalidad.Dominio.Pruebas
{
	// NIVEL A: LO QUE M-7 AGREGO, Y LA UNICA COSA QUE HAY QUE AFIRMAR DE UNA FRASE.
	//
	// EL DEFECTO QUE ESTA FIXTURE EXISTE PARA IMPEDIR ES UNO QUE CONCESIONARIO TODAVIA CARGA, y esta
	// escrito en su propio CLAUDE.md: `AnEnvelopeSuspendedByAUnitMismatch.Describe()` escribe a mano
	// "el odometro de este vehiculo" mientras la clausula interna de la MISMA frase dice bien "la
	// lectura del odometro registrada en la entrega". Las unidades que nombra estan bien, y por eso
	// TODA prueba que afirmaba unidades lo dejo pasar: ninguna afirmaba el SUSTANTIVO.
	//
	// Asi que aca se afirma el SUJETO de cada frase -- el nombre del rubro, la etiqueta del periodo, el
	// dia de la consulta, de quien es cada mitad -- y no solo que los numeros y los slugs esten
	// adentro. `Is.Not.Empty` prueba que llego una frase y nunca que es de este portador.
	//
	// Y SE AFIRMA TAMBIEN EL SUJETO AJENO, EN NEGATIVO. Una frase que nombra su propio rubro Y no
	// nombra el otro es la unica forma de que un sujeto intercambiado se ponga rojo: con dos frases
	// correctas, afirmar solo la propia pasa igual si se copian una de la otra.
	//
	// LOS HECHOS SE ESCRIBEN AQUI Y NO SE LEEN DEL PORTADOR (E49). Cada afirmacion se ancla a un monto
	// y un periodo escritos en esta prueba; comparar la frase contra otro miembro del mismo portador
	// seria comparar dos consumidores de un mismo resolutor, que concuerdan por construccion.
	[TestFixture]
	public sealed class EverySentenceTheDomainComposesNamesItsOwnSubject
	{
		private const string ElRubroPorUnidad = "recoleccion-de-basura";
		private const string ElRubroSobreBase = "bienes-inmuebles";

		private static readonly DateTime ElDiaDeCorte = new DateTime(2025, 10, 5);

		// EL MISMO SALTO EN DOS RUBROS, que es el caso de DEC-05: +80 % por periodo en los dos, y la
		// ley del rubro es lo unico que cambia entre ellos.
		// LOS DOS TRIMESTRES DEL SALTO DEL IBI, y los DOS AVALUOS que la municipalidad publico entre
		// ellos: 2023-01-15 esta vigente cuando cierra 2024-T1 y 2025-01-20 cuando cierra 2025-T1. Los
		// montos NO comparten digitos con los montos por periodo (20000 / 36000) a proposito: si los
		// compartieran, una afirmacion sobre el monto por periodo se pondria verde leyendo el avaluo.
		private static readonly Periodo ElTrimestreAnterior = new Trimestre(2024, 1);
		private static readonly Periodo ElTrimestreActual = new Trimestre(2025, 1);

		private const decimal ElAvaluoAnterior = 18500000m;
		private const decimal ElAvaluoActual = 33300000m;

		private static EstadoDelAvaluo SinNingunaFinca()
		{
			return EstadoDelAvaluo.De(ElTrimestreAnterior, ElTrimestreActual, new List<Finca>());
		}

		private static EstadoDelAvaluo ConLosDosAvaluosPublicados()
		{
			Finca laFinca = new Finca("F-1");
			laFinca.FijarAvaluo(new ValorDeFinca(new DateTime(2023, 1, 15), ElAvaluoAnterior));
			laFinca.FijarAvaluo(new ValorDeFinca(new DateTime(2025, 1, 20), ElAvaluoActual));

			return EstadoDelAvaluo.De(
				ElTrimestreAnterior, ElTrimestreActual, new List<Finca> { laFinca });
		}

		private static EstadoDelAvaluo SinAvaluoPublicado()
		{
			return EstadoDelAvaluo.De(
				ElTrimestreAnterior, ElTrimestreActual, new List<Finca> { new Finca("F-1") });
		}

		private static EstadoDelAvaluo ConVariasFincas()
		{
			return EstadoDelAvaluo.De(ElTrimestreAnterior, ElTrimestreActual,
				new List<Finca> { new Finca("F-1"), new Finca("F-2") });
		}

		private static Variacion ElSaltoDeBasura()
		{
			return Variacion.Entre(
				new LineaPagada(Rubro.Basura(), new Mes(2024, 3), 5000m, 0m, 1, false),
				new LineaPagada(Rubro.Basura(), new Mes(2025, 3), 9000m, 0m, 1, false),
				0.25m, 0.02m, SinNingunaFinca());
		}

		private static Variacion ElSaltoDeBienesInmuebles()
		{
			return ElSaltoDeBienesInmueblesCon(ConLosDosAvaluosPublicados());
		}

		private static Variacion ElSaltoDeBienesInmueblesCon(EstadoDelAvaluo elAvaluo)
		{
			return Variacion.Entre(
				new LineaPagada(Rubro.BienesInmuebles(), ElTrimestreAnterior, 20000m, 0m, 1, false),
				new LineaPagada(Rubro.BienesInmuebles(), ElTrimestreActual, 36000m, 0m, 1, false),
				0.25m, 0.02m, elAvaluo);
		}

		private static Variacion UnCambioMenor()
		{
			return Variacion.Entre(
				new LineaPagada(Rubro.Policia(), new Mes(2024, 3), 6000m, 0m, 1, false),
				new LineaPagada(Rubro.Policia(), new Mes(2025, 3), 6600m, 0m, 1, false),
				0.25m, 0.02m, SinNingunaFinca());
		}

		private static Variacion UnSinCambio()
		{
			return Variacion.Entre(
				new LineaPagada(Rubro.Policia(), new Mes(2024, 3), 6000m, 0m, 1, false),
				new LineaPagada(Rubro.Policia(), new Mes(2025, 3), 6006m, 0m, 1, false),
				0.25m, 0.02m, SinNingunaFinca());
		}

		private static IReadOnlyList<LineaPagada> DosLineasDeBasura()
		{
			return new List<LineaPagada>
			{
				new LineaPagada(Rubro.Basura(), new Mes(2024, 3), 5000m, 0m, 1, false),
				new LineaPagada(Rubro.Basura(), new Mes(2024, 4), 5000m, 0m, 1, false),
			};
		}

		// TODA FRASE SE DICE EN LOS DOS IDIOMAS, Y LAS DOS MITADES SON DISTINTAS.
		//
		// El defecto que esta prueba impide es el unico que el tipo Idioma no puede impedir solo: una
		// clausula que se agrega al espanol y se copia tal cual al ingles -- o al reves -- deja una
		// frase mitad traducida y NADA se pone rojo, porque la frase sigue llegando entera. Afirmar
		// que llega no prueba nada; hay que afirmar que las dos mitades NO son la misma.
		//
		// La comparacion es por frase y no por conjunto: dos frases que se cruzaran entre portadores
		// darian un conjunto distinto igual.
		[Test]
		public void CadaFraseSeDiceEnLosDosIdiomasYNoSonLaMisma()
		{
			List<KeyValuePair<string, string>> enEspanol =
				new List<KeyValuePair<string, string>>(TodasLasFrases("es"));
			List<KeyValuePair<string, string>> enIngles =
				new List<KeyValuePair<string, string>>(TodasLasFrases("en"));

			Assert.That(enIngles.Count, Is.EqualTo(enEspanol.Count),
				"Los dos idiomas tienen que componer la misma cantidad de frases.");

			for (int fila = 0; fila < enEspanol.Count; fila++)
			{
				string portador = enEspanol[fila].Key;

				Assert.That(enIngles[fila].Key, Is.EqualTo(portador),
					"El orden de los portadores tiene que ser el mismo en los dos idiomas.");

				Assert.That(enEspanol[fila].Value, Is.Not.Empty, portador + " no dice nada en espanol.");
				Assert.That(enIngles[fila].Value, Is.Not.Empty, portador + " no dice nada en ingles.");

				Assert.That(enIngles[fila].Value, Is.Not.EqualTo(enEspanol[fila].Value),
					portador + " dice lo mismo en los dos idiomas: la mitad inglesa quedo sin traducir.");
			}
		}

		// UNA ETIQUETA QUE ESTA APP NO HABLA CAE AL IDIOMA DEL REGISTRO, y no a una frase vacia ni a
		// una excepcion: una consulta no refuta una pregunta legitima por el nombre de un idioma.
		[Test]
		public void UnaEtiquetaDesconocidaCaeAlIdiomaDelRegistro()
		{
			foreach (string etiqueta in new[] { "", "   ", "fr", "de-DE", "xx" })
				Assert.That(LasFrases.De(etiqueta), Is.SameAs(LasFrases.De("es")),
					"La etiqueta '" + etiqueta + "' tiene que caer al idioma del registro.");

			foreach (string etiqueta in new[] { "es", "es-CR", "ES-cr" })
				Assert.That(LasFrases.De(etiqueta), Is.SameAs(LasFrases.De("es")));

			foreach (string etiqueta in new[] { "en", "en-US", "EN-gb" })
				Assert.That(LasFrases.De(etiqueta), Is.SameAs(LasFrases.De("en")));
		}

		// TODA FRASE QUE ESTA APP COMPONE, en un solo lugar, para las afirmaciones que valen para todas.
		//
		// LA REDACCION YA NO ES DEL DOMINIO. El dominio entrega su CLAVE y sus VALORES, y App/Frases la
		// redacta. Esta fixture renderiza por ese mismo camino, asi que sigue afirmando lo mismo que
		// antes -- que cada frase nombra SU sujeto -- sobre el texto que la persona lee de verdad.
		private static IEnumerable<KeyValuePair<string, string>> TodasLasFrases(string idioma)
		{
			LasFrases f = LasFrases.De(idioma);

			yield return Una("CambioDeTarifa", LaFraseDe(ElSaltoDeBasura(), f));
			yield return Una("CambioDeAvaluo", LaFraseDe(ElSaltoDeBienesInmuebles(), f));
			yield return Una("SinSalto cambio-menor", LaFraseDe(UnCambioMenor(), f));
			yield return Una("SinSalto sin-cambio", LaFraseDe(UnSinCambio(), f));

			Atraso elAtraso = UnAtraso();
			yield return Una("Atraso", LaRedaccion.Atraso(f,
				elAtraso.EtiquetaDelPeriodo, elAtraso.NombreDelRubro, f.Dia(elAtraso.Vencio),
				elAtraso.Monto, elAtraso.DiasDeAtraso(ElDiaDeCorte), ElDiaDeCorte, 0.062m,
				elAtraso.CostoDePagarTarde(ElDiaDeCorte, 0.062m)));

			VencimientoProximo elVencimiento = UnVencimiento();
			DateTime alDia = new DateTime(2025, 10, 1);
			yield return Una("VencimientoProximo", LaRedaccion.Vencimiento(f,
				elVencimiento.EtiquetaDelPeriodo, elVencimiento.NombreDelRubro, f.Dia(elVencimiento.Vence),
				elVencimiento.Monto, elVencimiento.Urgencia(alDia, 7).Etiqueta,
				elVencimiento.DiasQueFaltan(alDia), alDia, 7));

			FaltanteDeReserva elFaltante = UnFaltante();
			yield return Una("FaltanteDeReserva", LaRedaccion.Faltante(f,
				elFaltante.PropuestoPorMes, elFaltante.DeclaradoPorMes,
				elFaltante.ClaveDeLoQueFalta, elFaltante.Faltante, elFaltante.Proporcion,
				elFaltante.ClaveDelAviso(0.10m), 0.10m,
				elFaltante.ClaveDeLoQueVence(0.10m), elFaltante.VenceEsteMes));

			ReservaPropuesta laPropuesta = new ReservaPropuesta(2025, 144000m);
			yield return Una("ReservaPropuesta", LaRedaccion.ReservaPropuesta(f,
				laPropuesta.Anio, laPropuesta.ProyeccionDelAnio, laPropuesta.MontoMensual));

			PeriodoSinComprobante elPeriodo = new PeriodoSinComprobante(Rubro.Basura(), new Mes(2024, 7));
			yield return Una("PeriodoSinComprobante", LaRedaccion.PeriodoSinComprobante(f,
				elPeriodo.EtiquetaDelPeriodo, elPeriodo.NombreDelRubro));

			yield return Una("ElRubroNoExiste",
				LaFraseDe(EstadoDeLaSerie.De("cementerio", new List<LineaPagada>()), f));
			yield return Una("ElRubroNoTieneHistoria",
				LaFraseDe(EstadoDeLaSerie.De("parques-y-zonas-verdes", new List<LineaPagada>()), f));
			yield return Una("ElRubroTieneHistoria",
				LaFraseDe(EstadoDeLaSerie.De(ElRubroPorUnidad, DosLineasDeBasura()), f));

			yield return Una("NoHayConsultaDeEseDia", LaFraseDe(ElCotejo(false, 0), f));
			yield return Una("LaConsultaNoDestapaNada", LaFraseDe(ElCotejo(true, 0), f));
			yield return Una("LaConsultaDestapaPeriodos", LaFraseDe(ElCotejo(true, 2), f));

			yield return Una("ElAvaluoExplicaElSalto", LaFraseDelAvaluoDe(ConLosDosAvaluosPublicados(), f));
			yield return Una("LaMunicipalidadNoPublicoElAvaluo", LaFraseDelAvaluoDe(SinAvaluoPublicado(), f));
			yield return Una("LaCuentaNoTieneNingunaFinca", LaFraseDelAvaluoDe(SinNingunaFinca(), f));
			yield return Una("ElReciboNoDiceDeCualFinca", LaFraseDelAvaluoDe(ConVariasFincas(), f));
		}

		// LO QUE LA PERSONA LEE, por el mismo camino que la puerta: la clave y los valores del portador,
		// redactados en App/Frases. Una sobrecarga por portador, y el idioma del registro, que es el que
		// estas afirmaciones citan palabra por palabra.
		private static string Dicha(Variacion v)
		{
			return LaFraseDe(v, LasFrases.De("es"));
		}

		private static string Dicha(EstadoDeLaSerie e)
		{
			return LaFraseDe(e, LasFrases.De("es"));
		}

		private static string Dicha(EstadoDeLosPeriodosSinComprobante e)
		{
			return LaFraseDe(e, LasFrases.De("es"));
		}

		private static string Dicha(ReservaPropuesta r)
		{
			return LaRedaccion.ReservaPropuesta(LasFrases.De("es"), r.Anio, r.ProyeccionDelAnio, r.MontoMensual);
		}

		private static string Dicha(PeriodoSinComprobante p)
		{
			return LaRedaccion.PeriodoSinComprobante(LasFrases.De("es"), p.EtiquetaDelPeriodo, p.NombreDelRubro);
		}

		private static string Dicha(Atraso a, DateTime fecha, decimal tasaAnual)
		{
			LasFrases f = LasFrases.De("es");
			return LaRedaccion.Atraso(f, a.EtiquetaDelPeriodo, a.NombreDelRubro, f.Dia(a.Vencio),
				a.Monto, a.DiasDeAtraso(fecha), fecha, tasaAnual, a.CostoDePagarTarde(fecha, tasaAnual));
		}

		private static string Dicha(VencimientoProximo v, DateTime fecha, int diasDeAviso)
		{
			LasFrases f = LasFrases.De("es");
			return LaRedaccion.Vencimiento(f, v.EtiquetaDelPeriodo, v.NombreDelRubro, f.Dia(v.Vence),
				v.Monto, v.Urgencia(fecha, diasDeAviso).Etiqueta, v.DiasQueFaltan(fecha), fecha, diasDeAviso);
		}

		private static string Dicha(FaltanteDeReserva elFaltante, decimal umbral)
		{
			return LaRedaccion.Faltante(LasFrases.De("es"),
				elFaltante.PropuestoPorMes, elFaltante.DeclaradoPorMes,
				elFaltante.ClaveDeLoQueFalta, elFaltante.Faltante, elFaltante.Proporcion,
				elFaltante.ClaveDelAviso(umbral), umbral,
				elFaltante.ClaveDeLoQueVence(umbral), elFaltante.VenceEsteMes);
		}

		// EL MISMO CAMINO QUE LA PUERTA: la clave y los valores del portador, redactados en App/Frases.
		private static string LaFraseDe(Variacion v, LasFrases f)
		{
			return LaRedaccion.Variacion(f,
				v.NombreDelRubro, v.EtiquetaDelPeriodoAnterior, v.EtiquetaDelPeriodoActual,
				v.MontoAnteriorPorPeriodo, v.MontoActualPorPeriodo, v.Proporcion,
				v.ClaveDelCambio, v.Etiqueta, v.NombreDeLaLey,
				v.EtiquetaDelAvaluo, v.NumeroDeLaFincaDelAvaluo, v.CuantasFincasDelAvaluo,
				v.LosAvaluosDelSalto);
		}

		private static string LaFraseDe(EstadoDeLaSerie e, LasFrases f)
		{
			return LaRedaccion.EstadoDeLaSerie(f, e.Etiqueta, e.NombreDelRubro, e.CuantasLineas,
				string.Join(", ", Rubro.LosQueHay()));
		}

		private static string LaFraseDe(EstadoDeLosPeriodosSinComprobante e, LasFrases f)
		{
			return LaRedaccion.Cotejo(f, e.Etiqueta, e.DiaDeLaConsulta, e.CuantosPeriodos);
		}

		// EL ESTADO DEL AVALUO SOLO, sin el salto que lo cita: la clausula tiene que nombrar su finca
		// por si misma, y envuelta en la frase de la variacion no se veria cual de las dos la nombro.
		private static string LaFraseDelAvaluoDe(EstadoDelAvaluo e, LasFrases f)
		{
			decimal[] avaluos = e.SePuedeDecir
				? new[] { e.AvaluoAnterior, e.AvaluoActual }
				: new decimal[0];

			return LaRedaccion.Variacion(f,
				ElRubroSobreBase, e.EtiquetaDelPeriodoAnterior, e.EtiquetaDelPeriodoActual,
				20000m, 36000m, 0.8m,
				"mas", "cambio-de-avaluo", "sobre-base-imponible",
				e.Etiqueta, e.NumeroDeLaFinca, e.CuantasFincas, avaluos);
		}

		private static KeyValuePair<string, string> Una(string portador, string frase)
		{
			return new KeyValuePair<string, string>(portador, frase);
		}

		private static Atraso UnAtraso()
		{
			return new Atraso(new Cobro(
				Rubro.Basura(), new Mes(2025, 9), 8000m, new DateTime(2025, 9, 10)));
		}

		private static VencimientoProximo UnVencimiento()
		{
			return new VencimientoProximo(new Cobro(
				Rubro.Basura(), new Mes(2025, 10), 8000m, new DateTime(2025, 9, 10)));
		}

		private static FaltanteDeReserva UnFaltante()
		{
			return new FaltanteDeReserva(propuestoPorMes: 12000m, declaradoPorMes: 8000m, venceEsteMes: 5000m);
		}

		private static EstadoDeLosPeriodosSinComprobante ElCotejo(bool hayConsulta, int cuantos)
		{
			List<PeriodoSinComprobante> periodos = new List<PeriodoSinComprobante>();
			for (int cual = 0; cual < cuantos; cual++)
				periodos.Add(new PeriodoSinComprobante(Rubro.Basura(), new Mes(2024, 7 + cual)));

			return EstadoDeLosPeriodosSinComprobante.De(
				new DateTime(2025, 9, 3), hayConsulta, periodos);
		}

		// ---------------------------------------------------------------- lo que vale para todas

		// NINGUNA FRASE ES VACIA Y NINGUNA ES UN SLUG. Una frase que devolviera su etiqueta habria
		// cumplido "compone una frase" sin componer ninguna, y es exactamente lo que estos portadores
		// hacian antes de M-7.
		[Test]
		public void EveryCarrierComposesASentenceAndNoneOfThemIsJustItsSlug()
		{
			Assert.Multiple(() =>
			{
				foreach (KeyValuePair<string, string> cada in TodasLasFrases("es"))
				{
					Assert.That(cada.Value, Is.Not.Null.And.Not.Empty, cada.Key + ": tiene frase");
					Assert.That(cada.Value, Does.Contain(" "),
						cada.Key + ": una frase tiene palabras, un slug no tiene ninguna");
					Assert.That(cada.Value.Trim(), Does.EndWith("."),
						cada.Key + ": una frase termina, y quien la muestre no le agrega el punto");
					Assert.That(cada.Value, Does.Not.Contain("  "),
						cada.Key + ": dos espacios seguidos son una clausula que se armo mal");
				}
			});
		}

		// TODA FRASE LLEVA SUS ACENTOS, que es la mitad de la regla de lenguaje de este modulo: la
		// persona lee espanol acentuado, y los identificadores y los mensajes de excepcion no. Una
		// frase sin ni un acento delata a quien los quito para que "compilara igual".
		[Test]
		public void EverySentenceCarriesItsAccentsBecauseAPersonReadsIt()
		{
			Assert.Multiple(() =>
			{
				foreach (KeyValuePair<string, string> cada in TodasLasFrases("es"))
					Assert.That(cada.Value, Does.Match("[áéíóúñÁÉÍÓÚÑ]"),
						cada.Key + ": la frase que una persona lee lleva sus acentos");
			});
		}

		// LAS CIFRAS SE DICEN DE UNA SOLA MANERA, y por eso viven en un solo lugar: dos portadores que
		// dijeran 8000 de dos formas serian la frase rival que DEC-05 existe para impedir, en pequeno.
		[Test]
		public void TheSameFigureIsSaidTheSameWayByEveryCarrierThatSaysIt()
		{
			string elAtraso =Dicha(UnAtraso(), ElDiaDeCorte, 0.062m);
			string elVencimiento =Dicha(UnVencimiento(), new DateTime(2025, 10, 1), 7);

			Assert.Multiple(() =>
			{
				Assert.That(elAtraso, Does.Contain("8000"),
					"Atraso dice el monto con los digitos de su columna tipada");
				Assert.That(elVencimiento, Does.Contain("8000"),
					"y VencimientoProximo dice el mismo monto igual");

				foreach (string separadorDeMiles in new[] { "8,000", "8.000", "8 000" })
				{
					Assert.That(elAtraso, Does.Not.Contain(separadorDeMiles),
						"Atraso: ningun separador de miles, que sin moneda no se puede leer de una sola "
						+ "manera");
					Assert.That(elVencimiento, Does.Not.Contain(separadorDeMiles),
						"VencimientoProximo: tampoco");
				}

				Assert.That(Dicha(ElSaltoDeBasura()), Does.Contain("80 %"),
					"una proporcion se dice como porcentaje y no como 0,8");
			});
		}

		// ---------------------------------------------------------------- Variacion

		// EL MISMO SALTO, DOS RUBROS, DOS EXPLICACIONES, Y CADA FRASE NOMBRA SU PROPIO RUBRO. Es la
		// frase que DEC-05 nombra como el caso que hace concreta la decision entera.
		[Test]
		public void TheSameJumpSaysTariffInOneRubroAndAssessedValueInTheOtherAndNeitherNamesTheOthersRubro()
		{
			string basura =Dicha(ElSaltoDeBasura());
			string ibi =Dicha(ElSaltoDeBienesInmuebles());

			Assert.Multiple(() =>
			{
				// EL SUJETO. Cada una nombra el suyo y NO el ajeno: es lo que se pone rojo si alguien
				// escribe el sustantivo a mano o copia una clausula de la otra hoja.
				Assert.That(basura, Does.Contain(ElRubroPorUnidad), "la frase de basura nombra basura");
				Assert.That(basura, Does.Not.Contain(ElRubroSobreBase),
					"y NO nombra bienes inmuebles: un sujeto intercambiado se ve solo asi");
				Assert.That(ibi, Does.Contain(ElRubroSobreBase), "la frase del IBI nombra bienes inmuebles");
				Assert.That(ibi, Does.Not.Contain(ElRubroPorUnidad), "y NO nombra basura");

				// LOS DOS PERIODOS, cada uno en el alfabeto de su rubro: meses en basura, trimestres en
				// bienes inmuebles. El mismo numero leido de dos maneras es la ley del modulo.
				Assert.That(basura, Does.Contain("2024-03").And.Contain("2025-03"),
					"los dos meses de la serie de basura");
				Assert.That(ibi, Does.Contain("2024-T1").And.Contain("2025-T1"),
					"y los dos TRIMESTRES de la del IBI, no dos meses");

				// LA EXPLICACION, que es lo que la ley del rubro decide y lo unico que la app agrega.
				Assert.That(basura, Does.Contain("tarifa"), "en basura, lo que cambio fue la tarifa");
				Assert.That(basura, Does.Contain("por unidad"),
					"y la clausula de la ley la dice la ley, no la hoja: se cobra por unidad");
				Assert.That(basura, Does.Not.Contain("avalúo"),
					"y la frase de basura NO habla de avaluo, que es la explicacion del otro rubro");

				Assert.That(ibi, Does.Contain("avalúo"), "en el IBI, el salto viene del valor de la finca");
				Assert.That(ibi, Does.Contain("base imponible"), "y la ley dice su propia clausula");
				Assert.That(ibi, Does.Not.Contain("lo que cambió fue la tarifa"),
					"y la frase del IBI NO dice que cambio la tarifa");

				// LOS DOS MONTOS POR PERIODO, anclados a lo que esta prueba escribio.
				Assert.That(basura, Does.Contain("5000").And.Contain("9000"), "de 5000 a 9000 por mes");
				Assert.That(ibi, Does.Contain("20000").And.Contain("36000"),
					"de 20000 a 36000 por trimestre");
			});
		}

		// LA HOJA DEL AVALUO DICE DE CUANTO A CUANTO CUANDO LA MUNICIPALIDAD LO PUBLICO, que es la mitad
		// del hueco 2 que el avaluo por el feed cierra. Los dos montos estan ANCLADOS a los literales
		// que esta prueba escribio (E49): compararlos contra otro miembro del mismo portador seria
		// comparar dos consumidores de un mismo resolutor.
		[Test]
		public void TheAssessedValueLeafSaysFromWhatToWhatOnceTheMunicipalityHasPublishedIt()
		{
			string ibi =Dicha(ElSaltoDeBienesInmuebles());

			Assert.Multiple(() =>
			{
				Assert.That(ibi, Does.Contain("18500000"), "el avaluo vigente cuando cerro 2024-T1");
				Assert.That(ibi, Does.Contain("33300000"), "y el vigente cuando cerro 2025-T1");
				Assert.That(ibi, Does.Contain("F-1"), "y de cual finca es, que es su sujeto");
				Assert.That(ibi, Does.Contain("la municipalidad cobró"),
					"y dice que ese es el valor con que cobro la INSTITUCION, no uno declarado");
				Assert.That(ibi, Does.Not.Contain("no le llega a esta app"),
					"y ya no dice que el valor no llegue: llego");
			});
		}

		// LAS TRES HOJAS QUE NO PUEDEN DECIRLO DICEN RAZONES DISTINTAS, y ninguna contesta un cero. Es
		// la tercera anti-pauta de E47, la callada: un cero se leeria como una finca que no vale nada, y
		// todo consumidor de abajo lo repetiria como hecho.
		[Test]
		public void TheThreeLeavesThatCannotSayTheAssessedValueEachSayADifferentReasonAndNoneSaysZero()
		{
			string sinAvaluo =Dicha(ElSaltoDeBienesInmueblesCon(SinAvaluoPublicado()));
			string sinFinca =Dicha(ElSaltoDeBienesInmueblesCon(SinNingunaFinca()));
			string variasFincas =Dicha(ElSaltoDeBienesInmueblesCon(ConVariasFincas()));

			Assert.Multiple(() =>
			{
				Assert.That(sinAvaluo, Does.Contain("no publicó un avalúo de la finca F-1"),
					"la finca ESTA y lo que falta es el avaluo, que es lo que la separa de la de abajo");
				Assert.That(sinAvaluo, Does.Contain("Lo que vos hayás declarado es tuyo"),
					"y dice que lo declarado por la persona no es el valor con que le cobraron");

				Assert.That(sinFinca, Does.Contain("no tiene ninguna finca incorporada"),
					"aca no hay ni sobre cual leerlo");
				Assert.That(sinFinca, Does.Not.Contain("F-1"), "y no nombra una finca que no tiene");

				Assert.That(variasFincas, Does.Contain("2 fincas"),
					"y aca sobran: el recibo no dice de cual es la linea");
				Assert.That(variasFincas, Does.Contain("consolida por cédula"),
					"y dice POR QUE no lo dice, que es el hueco 2 en su mitad que sigue abierta");

				foreach (string cada in new[] { sinAvaluo, sinFinca, variasFincas })
				{
					Assert.That(cada, Does.Contain("dice el salto"),
						"las tres dicen lo que SI se puede decir");
					Assert.That(cada, Does.Not.Match(@"avalúo[^.]* 0[ ,.]"),
						"y ninguna contesta un cero por avaluo");
				}

				Assert.That(sinAvaluo, Is.Not.EqualTo(sinFinca), "las tres razones son tres frases");
				Assert.That(sinFinca, Is.Not.EqualTo(variasFincas));
			});
		}

		// Y EL MONTO SE PIDE CON GUARDA: donde no se puede decir, preguntarlo revienta en vez de
		// contestar un cero. Es la pareja pregunta-total + lectura-guardada de E41.
		[Test]
		public void TheAssessedValueIsGuardedAndNeverAnswersZeroWhereItCannotBeSaid()
		{
			Assert.Multiple(() =>
			{
				Assert.That(ConLosDosAvaluosPublicados().SePuedeDecir, Is.True);
				Assert.That(ConLosDosAvaluosPublicados().AvaluoAnterior, Is.EqualTo(ElAvaluoAnterior));
				Assert.That(ConLosDosAvaluosPublicados().AvaluoActual, Is.EqualTo(ElAvaluoActual));

				foreach (EstadoDelAvaluo cada in
					new[] { SinAvaluoPublicado(), SinNingunaFinca(), ConVariasFincas() })
				{
					Assert.That(cada.SePuedeDecir, Is.False, cada.Etiqueta + ".SePuedeDecir");
					Assert.Throws<DomainException>(
						() => { decimal _ = cada.AvaluoAnterior; }, cada.Etiqueta + ".AvaluoAnterior");
					Assert.Throws<DomainException>(
						() => { decimal _ = cada.AvaluoActual; }, cada.Etiqueta + ".AvaluoActual");
				}
			});
		}

		// LAS DOS ETIQUETAS DE SinSalto SON DOS FRASES DISTINTAS, y tienen que serlo: un cambio menor
		// SI cambio y no se explica; un sin-cambio no cambio. Decirlas igual haria que la primera
		// pareciera un error de la app.
		[Test]
		public void TheTwoSpellingsOfNoJumpSayTwoDifferentThings()
		{
			Variacion menor = UnCambioMenor();
			Variacion quieto = UnSinCambio();

			Assert.Multiple(() =>
			{
				Assert.That(menor.Etiqueta, Is.EqualTo("cambio-menor"), "el de arriba del ruido");
				Assert.That(quieto.Etiqueta, Is.EqualTo("sin-cambio"), "y el de abajo del ruido");

				Assert.That(Dicha(menor), Does.Contain("umbral"),
					"el cambio menor dice que quedo por debajo del UMBRAL, asi que cambio");
				Assert.That(Dicha(quieto), Does.Contain("ruido"),
					"y el sin-cambio dice que quedo por debajo del RUIDO, asi que no cambio");

				Assert.That(Dicha(menor), Is.Not.EqualTo(Dicha(quieto)),
					"Y las dos frases no son la misma frase. Con una sola, la etiqueta diria dos cosas "
					+ "que la frase no distingue.");
			});
		}

		// ---------------------------------------------------------------- Atraso

		// LA PROYECCION SE DICE COMO PROYECCION, que es el juicio 3 firmado -- recibir y alarmar, no
		// verificar-- puesto en palabras. La frase no puede quedar leyendose como la mora que la
		// municipalidad cobra.
		[Test]
		public void TheOverdueSentenceMarksItsProjectionAsAProjectionAndNamesItsOwnPeriod()
		{
			string frase =Dicha(UnAtraso(), ElDiaDeCorte, 0.062m);

			Assert.Multiple(() =>
			{
				Assert.That(frase, Does.Contain("2025-09"), "nombra SU periodo");
				Assert.That(frase, Does.Contain(ElRubroPorUnidad), "y su rubro");
				Assert.That(frase, Does.Not.Contain(ElRubroSobreBase), "y no el ajeno");
				Assert.That(frase, Does.Contain("2025-09-30"), "el dia en que vencio, no el periodo otra vez");
				Assert.That(frase, Does.Contain("8000"), "lo que se cobra");
				Assert.That(frase, Does.Contain(" 5 días de atraso"),
					"y los dias que lleva al 5 de octubre, que son cinco");

				Assert.That(frase, Does.Contain("proyección"), "la palabra proyeccion esta");
				Assert.That(frase, Does.Contain("no la mora que la municipalidad cobra"),
					"y dice que NO es la mora cobrada, que es la mitad que no se puede omitir");
				Assert.That(frase, Does.Contain("6,2 %"), "y la tasa con que se pregunto");
				Assert.That(frase, Does.Contain("6,79"),
					"y el costo proyectado: 8000 x 0,062 x 5 / 365, anclado a los hechos de esta prueba");
			});
		}

		// ---------------------------------------------------------------- VencimientoProximo

		// LOS TRES NIVELES DE URGENCIA, TRES CLAUSULAS, y la del medio es la que importa: un aviso NO
		// es una parada obligatoria, y es la confusion que las instrucciones de todo servidor MCP de
		// este juego piden no cometer.
		[Test]
		public void TheDueSoonSentenceSaysWhichOfTheThreeUrgenciesItIsAndNeverCallsANoticeAStop()
		{
			VencimientoProximo vence = UnVencimiento();

			string elDia =Dicha(vence, new DateTime(2025, 10, 31), 7);
			string dentroDelAviso =Dicha(vence, new DateTime(2025, 10, 28), 7);
			string todaviaNo =Dicha(vence, new DateTime(2025, 10, 1), 7);

			Assert.Multiple(() =>
			{
				foreach (string frase in new[] { elDia, dentroDelAviso, todaviaNo })
				{
					Assert.That(frase, Does.Contain("2025-10"), "las tres nombran SU periodo");
					Assert.That(frase, Does.Contain(ElRubroPorUnidad), "y su rubro");
					Assert.That(frase, Does.Contain("2025-10-31"), "y el dia en que vence");
				}

				Assert.That(elDia, Does.Contain("vence ese mismo día"), "urgente: vence hoy");

				Assert.That(dentroDelAviso, Does.Contain("faltan 3 días"),
					"aviso: los dias que faltan, anclados al 28 de octubre");
				Assert.That(dentroDelAviso, Does.Contain("recordatorio y no una parada obligatoria"),
					"y un aviso se dice como recordatorio: presentarlo como parada obligatoria es la "
					+ "confusion que vale la pena evitar");

				Assert.That(todaviaNo, Does.Contain("faltan 30 días"),
					"ninguna: treinta dias del 1 al 31 de octubre");
				Assert.That(todaviaNo, Does.Contain("todavía no apura"), "y no apura");

				Assert.That(elDia, Is.Not.EqualTo(dentroDelAviso), "las tres clausulas son tres");
				Assert.That(dentroDelAviso, Is.Not.EqualTo(todaviaNo));
			});
		}

		// ---------------------------------------------------------------- FaltanteDeReserva

		// LAS DOS MITADES, Y DE QUIEN ES CADA UNA (E48). Se afirma el numero PEGADO a la clausula que
		// dice de quien es, no que los dos numeros esten en la frase: con las dos mitades adentro,
		// intercambiarlas deja verde cualquier afirmacion que solo las busque.
		[Test]
		public void TheReserveShortfallSentenceSaysWhichHalfIsTheAppsAndWhichIsThePersons()
		{
			string frase =Dicha(UnFaltante(), 0.10m);

			Assert.Multiple(() =>
			{
				Assert.That(frase, Does.Contain("La app propone apartar 12000"),
					"los 12000 son de la APP, y van pegados a la clausula que lo dice");
				Assert.That(frase, Does.Contain("vos adoptaste 8000"),
					"y los 8000 son de la PERSONA. Intercambiados, esta afirmacion se pone roja y la de "
					+ "'los dos numeros estan' no");

				Assert.That(frase, Does.Contain("Te faltan 4000"), "el faltante, anclado a 12000 - 8000");
				Assert.That(frase, Does.Contain("33,3 %"), "y como proporcion de lo propuesto");
				Assert.That(frase, Does.Contain("10 %"), "y el umbral con que se pregunto");
				Assert.That(frase, Does.Contain("se avisa"), "que en este caso se pasa, asi que se avisa");
			});
		}

		// LO URGENTE PIDE LAS DOS CONDICIONES -- juicio 6, firmado: que falte mas del umbral Y que lo que
		// vence en el mes pase lo apartado-- y la frase tiene TRES clausulas porque hay tres casos.
		//
		// ESTE DEFECTO ESTUVO ESCRITO Y CORRIO. La primera version de la clausula miraba `EsUrgente`
		// suelto, asi que `medir` imprimio "eso es lo que lo vuelve urgente" al lado de una columna de
		// urgencia que decia `ninguna`: la frase contradecia la urgencia de su propia respuesta. La
		// clausula ahora la decide `Urgencia(umbral)`, que es el mismo miembro que llena la columna, y
		// las tres afirmaciones de abajo estan ancladas a montos escritos en esta prueba.
		[Test]
		public void TheReserveShortfallSentenceNeverCallsItUrgentWhereItsOwnUrgencyIsNot()
		{
			FaltanteDeReserva loCubre = new FaltanteDeReserva(12000m, 13000m, 5000m);
			FaltanteDeReserva soloVenceDeMas = new FaltanteDeReserva(12000m, 13000m, 30000m);
			FaltanteDeReserva lasDos = new FaltanteDeReserva(12000m, 8000m, 30000m);

			Assert.Multiple(() =>
			{
				Assert.That(loCubre.Urgencia(0.10m).Etiqueta, Is.EqualTo("ninguna"),
					"13000 adoptados cubren 12000 propuestos y 5000 vencen: no apura");
				Assert.That(Dicha(loCubre, 0.10m), Does.Contain("lo que apartás alcanza a cubrir"));
				Assert.That(Dicha(loCubre, 0.10m), Does.Not.Contain("urgente"),
					"y la frase no dice urgente");

				Assert.That(soloVenceDeMas.Urgencia(0.10m).Etiqueta, Is.EqualTo("ninguna"),
					"con 30000 venciendo pero sin faltante, el juicio 6 NO lo vuelve urgente");
				Assert.That(Dicha(soloVenceDeMas, 0.10m), Does.Contain("30000"),
					"la frase dice el hecho: este mes vence mas de lo apartado");
				Assert.That(Dicha(soloVenceDeMas, 0.10m), Does.Contain("Por sí solo eso no lo vuelve urgente"),
					"y dice que ese hecho SOLO no alcanza. Es la clausula que contradecia la columna");
				Assert.That(Dicha(soloVenceDeMas, 0.10m), Does.Not.Contain("eso es lo que lo vuelve urgente"),
					"y nunca la del caso urgente");

				Assert.That(lasDos.Urgencia(0.10m).Etiqueta, Is.EqualTo("urgente"),
					"con faltante Y con lo del mes por encima, si apura");
				Assert.That(Dicha(lasDos, 0.10m), Does.Contain("eso es lo que lo vuelve urgente"),
					"y ahi la frase dice QUE lo vuelve urgente");
			});
		}

		// Y LA LEY GENERAL, sobre las cuatro combinaciones: la frase nunca llama urgente a una
		// respuesta cuya urgencia no lo es. Es estructural -- la clausula lee `Urgencia(umbral)`-- y se
		// afirma igual, porque lo que la volvio estructural fue este defecto.
		[Test]
		public void NoShortfallSentenceEverSaysUrgentUnlessItsOwnUrgencyIsUrgent()
		{
			decimal[][] losCuatroCasos =
			{
				new[] { 12000m, 13000m, 5000m },
				new[] { 12000m, 13000m, 30000m },
				new[] { 12000m, 8000m, 5000m },
				new[] { 12000m, 8000m, 30000m },
			};

			Assert.Multiple(() =>
			{
				foreach (decimal[] caso in losCuatroCasos)
				{
					FaltanteDeReserva faltante = new FaltanteDeReserva(caso[0], caso[1], caso[2]);
					bool loDice =Dicha(faltante, 0.10m).Contains("eso es lo que lo vuelve urgente");

					Assert.That(loDice, Is.EqualTo(faltante.Urgencia(0.10m).EsUrgente()),
						"propuesto " + caso[0] + ", declarado " + caso[1] + ", vence " + caso[2]
						+ ": la frase y la urgencia de la MISMA respuesta tienen que decir lo mismo");
				}
			});
		}

		// E48: LA MITAD GENERAL SE PUEDE DECIR SOLA, y la frase compuesta NO la contiene textual, asi
		// que una superficie puede mostrar las dos sin imprimir la misma clausula dos veces. Se AFIRMA
		// la no-contencion en vez de asumirla, que es lo que la enmienda pide.
		[Test]
		public void TheGeneralHalfSaysItselfWithoutThePersonsFigureAndTheComposedSentenceDoesNotRepeatIt()
		{
			string laPropuestaSola =Dicha(new ReservaPropuesta(2025, 144000m));
			string elFaltante =Dicha(UnFaltante(), 0.10m);

			Assert.Multiple(() =>
			{
				Assert.That(laPropuestaSola, Does.Contain("2025"), "la mitad general nombra su anio");
				Assert.That(laPropuestaSola, Does.Contain("144000"), "y la proyeccion de la que sale");
				Assert.That(laPropuestaSola, Does.Contain("12000"), "y el monto mensual que propone");
				Assert.That(laPropuestaSola, Does.Contain("proyección de esta app"),
					"y dice que es una propuesta de la APP y no un monto puesto al cobro");
				Assert.That(laPropuestaSola, Does.Not.Contain("adoptaste"),
					"y se dice SIN saber nada de lo que la persona adopto: tener una mitad no es "
					+ "condicion de leer la otra");

				Assert.That(elFaltante, Does.Not.Contain(laPropuestaSola),
					"Y la frase compuesta NO contiene la general textual, asi que mostrar las dos no "
					+ "imprime la misma clausula dos veces.");
				Assert.That(elFaltante, Does.Contain("12000"),
					"aunque las dos dicen el mismo monto propuesto, y este esta anclado a 144000 / 12");
			});
		}

		// ---------------------------------------------------------------- PeriodoSinComprobante

		// LO QUE FALTA ES EL COMPROBANTE Y NO EL PAGO. Decirlo al reves acusaria a la persona de no
		// pagar, que es el sustantivo equivocado mas caro que esta frase podria tener.
		[Test]
		public void TheMissingReceiptSentenceSaysTheReceiptIsMissingAndNotThePayment()
		{
			string frase =Dicha(new PeriodoSinComprobante(Rubro.Basura(), new Mes(2024, 7)));

			Assert.Multiple(() =>
			{
				Assert.That(frase, Does.Contain("2024-07"), "nombra SU periodo");
				Assert.That(frase, Does.Contain(ElRubroPorUnidad), "y su rubro");
				Assert.That(frase, Does.Contain("da por pagado"), "la consulta lo da por pagado");
				Assert.That(frase, Does.Contain("no dice que esté sin pagar").IgnoreCase,
					"y la frase niega en voz alta la lectura que acusaria a la persona");
				Assert.That(frase, Does.Contain("comprobante"), "lo que falta es el comprobante");
			});
		}

		// ---------------------------------------------------------------- las dos ausencias de M-6

		// LAS TRES HOJAS DE LA SERIE DICEN SU FRASE, INCLUSO LA QUE NO APLICA. E47 ilustra la variante
		// que no aplica devolviendo "" -- "una clausula que la regla no tiene no se nombra"-- y aca es
		// al reves: la ausencia ES la respuesta a "por que no hay nada?", asi que se dice. DEC-16 dejo
		// esta decision a M-7 en vez de tomarla.
		[Test]
		public void TheThreeSeriesStatesEachSayASentenceAndEachNamesTheRubroItWasAskedAbout()
		{
			string noExiste =Dicha(EstadoDeLaSerie.De("cementerio", new List<LineaPagada>()));
			string sinHistoria =Dicha(EstadoDeLaSerie.De("parques-y-zonas-verdes", new List<LineaPagada>()));
			string conHistoria =Dicha(EstadoDeLaSerie.De(ElRubroPorUnidad, DosLineasDeBasura()));

			Assert.Multiple(() =>
			{
				Assert.That(noExiste, Does.Contain("cementerio"),
					"la hoja del rubro desconocido nombra el nombre que se PIDIO, que es su sujeto");
				Assert.That(noExiste, Does.Contain("no existe en esta cuenta"),
					"y dice que el rubro no existe");
				Assert.That(noExiste, Does.Not.Contain("no vio pagar"),
					"y NO dice que no se haya pagado nada de el, que es la otra respuesta de cero filas");
				foreach (string cada in Rubro.LosQueHay())
					Assert.That(noExiste, Does.Contain(cada),
						"y dice cuales SI hay, sacados del catalogo: " + cada);

				Assert.That(sinHistoria, Does.Contain("parques-y-zonas-verdes"), "la otra nombra la suya");
				Assert.That(sinHistoria, Does.Contain("no vio pagar"), "y dice que no se vio pagar nada");
				Assert.That(sinHistoria, Does.Contain("El rubro sí existe"),
					"y dice que el rubro SI existe, que es lo que la separa de la de arriba");

				Assert.That(conHistoria, Does.Contain(ElRubroPorUnidad), "y la tercera la suya");
				Assert.That(conHistoria, Does.Contain("2 líneas"),
					"y cuenta las lineas que trae, ancladas a las dos que esta prueba escribio");

				Assert.That(noExiste, Is.Not.EqualTo(sinHistoria),
					"Y las dos respuestas de cero filas dicen frases distintas, que es todo el punto.");
			});
		}

		// LA PAREJA QUE UNA CARATULA CONFUNDE MAS FACIL, y por eso sus frases no comparten ni una
		// clausula: `sin-consulta` es una pregunta sin contestar y `nada-sin-comprobante` es una cuenta
		// en orden. Devolver la benigna para la primera es la tercera anti-pauta de E47, la callada.
		[Test]
		public void TheTwoZeroRowCrossCheckStatesSayOppositeThingsAndBothNameTheirOwnDay()
		{
			string sinConsulta =Dicha(ElCotejo(false, 0));
			string nadaDestapado =Dicha(ElCotejo(true, 0));
			string destapa =Dicha(ElCotejo(true, 2));

			Assert.Multiple(() =>
			{
				foreach (string cada in new[] { sinConsulta, nadaDestapado, destapa })
					Assert.That(cada, Does.Contain("2025-09-03"),
						"las tres nombran el DIA con que se las pidio, que es su sujeto");

				Assert.That(sinConsulta, Does.Contain("no se puede contestar"),
					"sin consulta: la pregunta no se puede contestar");
				Assert.That(sinConsulta, Does.Contain("no dice que todo esté en orden"),
					"y la frase niega la lectura benigna EN VOZ ALTA");
				Assert.That(sinConsulta, Does.Not.Contain("cotejó"),
					"y no dice que se haya cotejado nada, porque no hubo con que");

				Assert.That(nadaDestapado, Does.Contain("se cotejó y no destapó"),
					"con consulta y sin hallazgos: SI se cotejo");
				Assert.That(nadaDestapado, Does.Contain("esta app lo vio pagar"),
					"y todo lo que el portal da por pagado se vio pagar");
				Assert.That(nadaDestapado, Does.Not.Contain("no se puede contestar"),
					"y esta si se pudo contestar");

				Assert.That(destapa, Does.Contain("destapa 2 períodos"),
					"y la tercera cuenta los periodos, anclados a los dos que esta prueba escribio");
			});
		}

		// LOS DOS RESOLUTORES SIGUEN SIENDO TOTALES CON LA FRASE ADENTRO, y el caso que M-7 agrego es
		// el nombre EN BLANCO: pedir la serie de "" es legal, y la frase no puede salir con un sujeto
		// vacio entre comillas.
		[Test]
		public void ASeriesAskedWithNoRubroAtAllStillSaysSomethingTrue()
		{
			Assert.Multiple(() =>
			{
				foreach (string enBlanco in new[] { "", "   ", null })
				{
					string frase =Dicha(EstadoDeLaSerie.De(enBlanco, new List<LineaPagada>()));

					Assert.That(frase, Does.Contain("No se pidió ningún rubro"),
						"un nombre en blanco dice que no se pidio ninguno, no «» entre comillas");
					Assert.That(frase, Does.Not.Contain("«»"), "y no queda un sujeto vacio a la vista");
				}
			});
		}
	}
}
