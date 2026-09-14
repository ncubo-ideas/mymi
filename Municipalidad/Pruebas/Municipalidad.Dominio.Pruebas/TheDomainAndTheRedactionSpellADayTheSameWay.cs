using System;
using System.Collections.Generic;
using LasFrases = Municipalidad.App.Frases.Frases;
using NUnit.Framework;

namespace Municipalidad.Dominio.Pruebas
{
	// DOS LUGARES ESCRIBEN UN DIA Y NO PUEDEN COMPARTIR UNO SOLO.
	//
	// Periodo.UltimoDiaDicho lo escribe el DOMINIO, porque DEC-12 decidio que las columnas vence y
	// vencio viajan como texto; Frases.Dia lo escribe la REDACCION, para el instante con que se
	// pregunto. Los dos eligen anio-mes-dia y los dos dicen en su /// que no cambia con el idioma.
	//
	// No pueden compartir la constante: Dominio/ tiene CERO referencias -- es la prueba de que el
	// dominio no cambia al cruzar a wasm -- asi que el formato esta escrito dos veces por construccion
	// y no por descuido.
	//
	// LO QUE ESTA FIXTURE IMPIDE es que las dos copias dejen de decir lo mismo en silencio. Una frase
	// de atraso lleva las DOS adentro: {vencio} lo escribio el dominio y {fecha} la redaccion. Si una
	// de las dos cambiara de forma, esa frase diria el mismo tipo de dato de dos maneras en la misma
	// oracion, que es exactamente lo que la regla 8 de la guia de localizacion existe para evitar y lo
	// que Plata.cs documenta como ya medido en Compras.
	//
	// SE AFIRMA EN LOS DOS IDIOMAS a proposito: la redaccion es la que PODRIA variar por idioma, y el
	// dominio no tiene como seguirla. Que las dos den lo mismo en ingles es la mitad que se romperia
	// primero.
	[TestFixture]
	public sealed class TheDomainAndTheRedactionSpellADayTheSameWay
	{
		// Anios con y sin bisiesto, para que el 29 de febrero entre y salga.
		private static readonly int[] LosAnios = { 2024, 2025, 2026 };

		[Test]
		public void EveryClosingDayOfAMonthIsSpelledTheSameByBoth()
		{
			Assert.Multiple(() =>
			{
				foreach ((Periodo periodo, string cual) in LosPeriodos())
					foreach (string idioma in new[] { LasFrases.IdiomaDelRegistro, LasFrases.ElOtroIdioma })
						Assert.That(LasFrases.De(idioma).Dia(periodo.UltimoDia),
							Is.EqualTo(periodo.UltimoDiaDicho),
							cual + " en " + idioma + ": el dominio y la redaccion escriben ese dia "
							+ "distinto, asi que una frase que lleve los dos lo dice de dos maneras");
			});
		}

		// EL DIA NO CAMBIA CON EL IDIOMA, que es la condicion que hace posible lo de arriba: si la
		// redaccion empezara a decirlo distinto en ingles, el dominio no tendria como enterarse.
		[Test]
		public void TheRedactionSpellsADayTheSameInBothLanguages()
		{
			Assert.Multiple(() =>
			{
				foreach ((Periodo periodo, string cual) in LosPeriodos())
					Assert.That(LasFrases.De(LasFrases.ElOtroIdioma).Dia(periodo.UltimoDia),
						Is.EqualTo(LasFrases.De(LasFrases.IdiomaDelRegistro).Dia(periodo.UltimoDia)),
						cual + ": la redaccion dice ese dia distinto en cada idioma, y el dominio "
						+ "escribe el suyo en uno solo");
			});
		}

		private static IEnumerable<(Periodo Periodo, string Cual)> LosPeriodos()
		{
			foreach (int anio in LosAnios)
			{
				for (int mes = 1; mes <= 12; mes++)
					yield return (new Mes(anio, mes), "el mes " + anio + "-" + mes);

				for (int trimestre = 1; trimestre <= 4; trimestre++)
					yield return (new Trimestre(anio, trimestre), "el trimestre " + anio + "-T" + trimestre);
			}
		}
	}
}
