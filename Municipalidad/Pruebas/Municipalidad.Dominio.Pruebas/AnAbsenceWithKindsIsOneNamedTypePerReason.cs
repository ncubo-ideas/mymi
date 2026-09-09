using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Municipalidad.Dominio.Pruebas
{
	// NIVEL A: LAS DOS AUSENCIAS DE G6, cada razon con su propio tipo. Antes de M-6 las dos lecturas de
	// lista contestaban CERO FILAS por dos razones distintas cada una y no las separaban:
	//
	//   HistorialDe                    -> vacia por un rubro que no existe, y por uno sin pagos
	//   PeriodosPagadosSinComprobante  -> vacia porque no hay consulta de ese dia, y porque la
	//                                     consulta se cotejo y no destapo nada
	//
	// E47: una razon, un tipo, y el nombre del tipo la dice. Un solo tipo con un campo `motivo` lo
	// encuentra unicamente quien ya adivino como se escribio el texto.
	//
	// Y LAS DOS AUSENCIAS SON DE CLASES DISTINTAS, que es lo que hace que las bases pregunten cosas
	// distintas y no una sola compartida:
	//
	//   un rubro que no existe    -> PERMANENTE. Nada que llegue despues se lo cura. La base pregunta
	//                                si APLICA.
	//   no hay consulta de ese dia -> CONTINGENTE. Deja de serlo en cuanto se registre una. La base
	//                                pregunta si SE PUEDE DECIR.
	//
	// LA FRASE LLEGO EN M-7, Y NO SE AFIRMA ACA. Cuando se escribio esta fixture cada hoja exponia solo
	// su `Etiqueta` -- el slug, como Urgencia y Variacion -- porque DEC-16 le DEFIRIO la frase a M-7 en
	// vez de negarla. M-7 le puso a cada hoja su `Dice()`, y la ley de esas frases -- que cada una
	// nombre su propio sujeto -- vive en EverySentenceTheDomainComposesNamesItsOwnSubject. Aca se sigue
	// afirmando lo que esta fixture existe para afirmar: que la RAZON de cada ausencia es su propio
	// tipo y que las dos bases preguntan cosas distintas.
	[TestFixture]
	public sealed class AnAbsenceWithKindsIsOneNamedTypePerReason
	{
		// LOS DOS DIAS SON DEL SUJETO DE LA FRASE, no del cotejo: M-7 le puso el dia a la base para que
		// cada hoja pueda nombrar la consulta de la que habla en vez de que se lo pase quien pregunta.
		private static readonly DateTime ElDiaDeLaConsulta = new DateTime(2025, 9, 3);
		private static readonly DateTime ElDiaSinConsulta = new DateTime(2025, 1, 1);

		private static IReadOnlyList<LineaPagada> UnaSerieConUnaLinea()
		{
			return new List<LineaPagada>
			{
				new LineaPagada(Rubro.Basura(), new Mes(2024, 3), 5000m, 0m, 1, true),
			};
		}

		private static IReadOnlyList<LineaPagada> UnaSerieVacia()
		{
			return new List<LineaPagada>();
		}

		private static IReadOnlyList<PeriodoSinComprobante> UnPeriodoSinComprobante()
		{
			return new List<PeriodoSinComprobante>
			{
				new PeriodoSinComprobante(Rubro.Basura(), new Mes(2024, 7)),
			};
		}

		private static IReadOnlyList<PeriodoSinComprobante> NingunPeriodoSinComprobante()
		{
			return new List<PeriodoSinComprobante>();
		}

		// LAS TRES HOJAS DE LA SERIE, cada una contestando SUS TRES preguntas. Se afirman las tres en
		// cada hoja y no solo la que la distingue: una hoja que contestara bien su etiqueta y mal
		// `Aplica` mandaria a una caratula a decir que un rubro del catalogo no existe.
		[Test]
		public void TheThreeLeavesOfASeriesEachAnswerAllThreeQuestions()
		{
			EstadoDeLaSerie noExiste = EstadoDeLaSerie.De("cementerio", UnaSerieVacia());
			EstadoDeLaSerie sinHistoria = EstadoDeLaSerie.De("parques-y-zonas-verdes", UnaSerieVacia());
			EstadoDeLaSerie conHistoria = EstadoDeLaSerie.De("recoleccion-de-basura", UnaSerieConUnaLinea());

			Assert.Multiple(() =>
			{
				Assert.That(noExiste, Is.TypeOf<ElRubroNoExiste>(), "un nombre que el catalogo no tiene");
				Assert.That(noExiste.Aplica, Is.False, "ElRubroNoExiste.Aplica");
				Assert.That(noExiste.HayLineas, Is.False, "ElRubroNoExiste.HayLineas");
				Assert.That(noExiste.Etiqueta, Is.EqualTo("rubro-desconocido"), "ElRubroNoExiste.Etiqueta");

				Assert.That(sinHistoria, Is.TypeOf<ElRubroNoTieneHistoria>(), "un rubro del catalogo sin pagos");
				Assert.That(sinHistoria.Aplica, Is.True,
					"ElRubroNoTieneHistoria.Aplica: el rubro EXISTE, que es lo que lo separa del de arriba");
				Assert.That(sinHistoria.HayLineas, Is.False, "ElRubroNoTieneHistoria.HayLineas");
				Assert.That(sinHistoria.Etiqueta, Is.EqualTo("sin-historia"), "ElRubroNoTieneHistoria.Etiqueta");

				Assert.That(conHistoria, Is.TypeOf<ElRubroTieneHistoria>(), "un rubro con una linea");
				Assert.That(conHistoria.Aplica, Is.True, "ElRubroTieneHistoria.Aplica");
				Assert.That(conHistoria.HayLineas, Is.True, "ElRubroTieneHistoria.HayLineas");
				Assert.That(conHistoria.Etiqueta, Is.EqualTo("con-historia"), "ElRubroTieneHistoria.Etiqueta");
			});
		}

		// LAS TRES HOJAS DEL COTEJO, lo mismo. Y la primera es la que importa: `sin-consulta` NO dice
		// que todo este en orden -- dice que la pregunta no se pudo contestar. Confundir las dos es la
		// tercera anti-pauta de E47, la callada: devolver la respuesta benigna para una entrada que no
		// se pudo evaluar, que todo consumidor de abajo repite como hecho.
		[Test]
		public void TheThreeLeavesOfTheCrossCheckEachAnswerAllThreeQuestions()
		{
			EstadoDeLosPeriodosSinComprobante sinConsulta =
				EstadoDeLosPeriodosSinComprobante.De(ElDiaSinConsulta, false, NingunPeriodoSinComprobante());
			EstadoDeLosPeriodosSinComprobante nadaDestapado =
				EstadoDeLosPeriodosSinComprobante.De(ElDiaDeLaConsulta, true, NingunPeriodoSinComprobante());
			EstadoDeLosPeriodosSinComprobante conPeriodos =
				EstadoDeLosPeriodosSinComprobante.De(ElDiaDeLaConsulta, true, UnPeriodoSinComprobante());

			Assert.Multiple(() =>
			{
				Assert.That(sinConsulta, Is.TypeOf<NoHayConsultaDeEseDia>());
				Assert.That(sinConsulta.SePuedeDecir, Is.False, "NoHayConsultaDeEseDia.SePuedeDecir");
				Assert.That(sinConsulta.HayPeriodos, Is.False, "NoHayConsultaDeEseDia.HayPeriodos");
				Assert.That(sinConsulta.Etiqueta, Is.EqualTo("sin-consulta"));

				Assert.That(nadaDestapado, Is.TypeOf<LaConsultaNoDestapaNada>());
				Assert.That(nadaDestapado.SePuedeDecir, Is.True,
					"LaConsultaNoDestapaNada.SePuedeDecir: la consulta ESTA, que es lo que lo separa del "
					+ "de arriba -- y los dos traen cero periodos");
				Assert.That(nadaDestapado.HayPeriodos, Is.False, "LaConsultaNoDestapaNada.HayPeriodos");
				Assert.That(nadaDestapado.Etiqueta, Is.EqualTo("nada-sin-comprobante"));

				Assert.That(conPeriodos, Is.TypeOf<LaConsultaDestapaPeriodos>());
				Assert.That(conPeriodos.SePuedeDecir, Is.True, "LaConsultaDestapaPeriodos.SePuedeDecir");
				Assert.That(conPeriodos.HayPeriodos, Is.True, "LaConsultaDestapaPeriodos.HayPeriodos");
				Assert.That(conPeriodos.Etiqueta, Is.EqualTo("hay-periodos-sin-comprobante"));
			});
		}

		// LAS DOS HOJAS QUE TRAEN CERO SON DISTINGUIBLES, y esta es la prueba que G6 pedia. Sin ella
		// las dos ausencias se ven iguales desde afuera, que es exactamente el estado en que estaban.
		[Test]
		public void TheTwoEmptyLeavesOfEachSetAreDistinguishableFromEachOther()
		{
			EstadoDeLaSerie noExiste = EstadoDeLaSerie.De("cementerio", UnaSerieVacia());
			EstadoDeLaSerie sinHistoria = EstadoDeLaSerie.De("parques-y-zonas-verdes", UnaSerieVacia());

			EstadoDeLosPeriodosSinComprobante sinConsulta =
				EstadoDeLosPeriodosSinComprobante.De(ElDiaSinConsulta, false, NingunPeriodoSinComprobante());
			EstadoDeLosPeriodosSinComprobante nadaDestapado =
				EstadoDeLosPeriodosSinComprobante.De(ElDiaDeLaConsulta, true, NingunPeriodoSinComprobante());

			Assert.Multiple(() =>
			{
				Assert.That(noExiste.HayLineas, Is.EqualTo(sinHistoria.HayLineas),
					"Las dos traen cero lineas: por eso se confundian.");
				Assert.That(noExiste.Aplica, Is.Not.EqualTo(sinHistoria.Aplica),
					"Y se separan por si APLICA, que es la pregunta permanente.");
				Assert.That(noExiste.Etiqueta, Is.Not.EqualTo(sinHistoria.Etiqueta),
					"Y sus etiquetas son distintas, que es como cruzan a una caratula.");

				Assert.That(sinConsulta.HayPeriodos, Is.EqualTo(nadaDestapado.HayPeriodos),
					"Las dos traen cero periodos: por eso se confundian.");
				Assert.That(sinConsulta.SePuedeDecir, Is.Not.EqualTo(nadaDestapado.SePuedeDecir),
					"Y se separan por si SE PUEDE DECIR, que es la pregunta contingente.");
				Assert.That(sinConsulta.Etiqueta, Is.Not.EqualTo(nadaDestapado.Etiqueta));
			});
		}

		// LOS DOS RESOLUTORES SON TOTALES: cualquier nombre y cualquier serie dan una de las tres
		// hojas, y ninguno revienta. Una ausencia con clases no es una excepcion -- nada salio mal, los
		// hechos simplemente no estan.
		[Test]
		public void BothResolversAreTotalAndNeitherThrowsForAnyInput()
		{
			Assert.Multiple(() =>
			{
				Assert.That(EstadoDeLaSerie.De("", UnaSerieVacia()), Is.TypeOf<ElRubroNoExiste>(),
					"un nombre en blanco no es un rubro, y no revienta");
				Assert.That(EstadoDeLaSerie.De("  ", UnaSerieVacia()), Is.TypeOf<ElRubroNoExiste>(),
					"ni un nombre de espacios");
				Assert.That(EstadoDeLaSerie.De(null, UnaSerieVacia()), Is.TypeOf<ElRubroNoExiste>(),
					"ni null -- EsUnNombreConocido es total y lo contesta que no");

				foreach (string nombre in Rubro.LosQueHay())
					Assert.That(EstadoDeLaSerie.De(nombre, UnaSerieVacia()).Aplica, Is.True,
						"todo rubro del catalogo APLICA, incluso sin historia: " + nombre);
			});
		}
	}
}
