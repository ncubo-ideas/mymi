using System;
using NUnit.Framework;

namespace Municipalidad.Dominio.Pruebas
{
	// HUECO 2, EN SU MITAD CERRADA Y EN LA QUE SIGUE ABIERTA. La finca existe en el modelo aunque ningun
	// papel la nombre -- el portal consolida por cedula, pero el IBI es de la finca --, y su valor tiene
	// AHORA DOS ORIGENES:
	//
	//   el AVALUO      -> lo fija la municipalidad y llega por su feed. Es el valor con que ella COBRA,
	//                     asi que es el unico que puede explicar un salto en un rubro sobre base
	//                     imponible.
	//   la DECLARACION -> la hace la persona. Es suya, la lee su asistente, y NO es el valor con que le
	//                     cobraron.
	//
	// Antes de esto habia una sola linea de tiempo sin origen, que es el hueco 10: "distinguir avaluo de
	// declaracion es una clase mas que nadie pidio todavia". La distincion resulto NO ser una clase mas
	// sino dos lineas de tiempo separadas, cada una con su Puede... y su pareja pregunta-total +
	// lectura-guardada.
	//
	// LO QUE ESTA FIXTURE EXISTE PARA IMPEDIR es que una linea conteste por la otra. Declarar un valor
	// no publica un avaluo, y con el avaluo publicado la declaracion sigue siendo un hecho aparte: si
	// una contestara por la otra, la app afirmaria como valor con que la municipalidad cobro un numero
	// que escribio la persona.
	//
	// Regla 18 y huecos 12 y 13: todo comando necesita Check por identidad, incluso el que no puede
	// fallar. Cada linea acepta UN hecho por dia y en orden, por separado.
	[TestFixture]
	public sealed class AFincaHasTwoValueTimelinesAndNeitherSpeaksForTheOther
	{
		private static readonly DateTime ElDiaDelAvaluo = new DateTime(2025, 1, 20);
		private static readonly DateTime ElDiaDeLaDeclaracion = new DateTime(2025, 6, 30);
		private static readonly DateTime UnDiaPosterior = new DateTime(2026, 1, 1);
		private static readonly DateTime UnDiaAnterior = new DateTime(2024, 6, 1);

		private const decimal LoQueAvaluoLaMunicipalidad = 36000000m;
		private const decimal LoQueDeclaroLaPersona = 60000000m;

		[Test]
		public void TheFincaEntersOnceAndWithNeitherFactItHasNoValueOfEitherKindAtAnyDate()
		{
			Cuenta c = new Cuenta();
			c.IncorporarFinca("F-1");
			Finca f = c.FincaNumero("F-1");

			Assert.Multiple(() =>
			{
				Assert.That(c.TieneFinca("F-1"), Is.True, "La finca esta");
				Assert.That(c.PuedeIncorporarFinca("F-1"), Is.False,
					"La misma finca dos veces no entra");

				Assert.That(f.TieneAvaluoAl(UnDiaPosterior), Is.False,
					"Sin avaluo publicado, no tiene avaluo a ninguna fecha");
				Assert.Throws<DomainException>(() => f.AvaluoVigenteAl(UnDiaPosterior),
					"Y pedirlo revienta en vez de inventar uno -es la leccion de T1-");

				Assert.That(f.TieneValorDeclaradoAl(UnDiaPosterior), Is.False,
					"Sin declaracion, tampoco tiene valor declarado");
				Assert.Throws<DomainException>(() => f.ValorDeclaradoVigenteAl(UnDiaPosterior),
					"Y esa lectura tambien esta guardada");
			});
		}

		// EL AVALUO DE LA MUNICIPALIDAD, con su propia identidad: uno por dia, y posterior al ultimo.
		[Test]
		public void TheMunicipalitysAssessedValueIsInForceFromItsDateOnwardAndOnePerDay()
		{
			Cuenta c = ConUnaFinca();
			c.FijarAvaluoDeFinca("F-1", ElDiaDelAvaluo, LoQueAvaluoLaMunicipalidad);
			Finca f = c.FincaNumero("F-1");

			Assert.Multiple(() =>
			{
				Assert.That(f.AvaluoVigenteAl(UnDiaPosterior), Is.EqualTo(LoQueAvaluoLaMunicipalidad),
					"Publicado en 2025, el avaluo vigente en 2026 es ese");
				Assert.That(f.TieneAvaluoAl(UnDiaAnterior), Is.False,
					"Pero en junio de 2024 todavia no habia");

				Assert.That(
					c.PuedeFijarAvaluoDeFinca("F-1", ElDiaDelAvaluo, LoQueAvaluoLaMunicipalidad), Is.False,
					"El mismo avaluo otra vez no entra: es el mismo hecho -regla 18, hueco 12-");
				Assert.That(
					c.PuedeFijarAvaluoDeFinca("F-1", UnDiaPosterior, 40000000m), Is.True,
					"Un avaluo posterior, si");
				Assert.That(
					c.PuedeFijarAvaluoDeFinca("F-1", UnDiaAnterior, 30000000m), Is.False,
					"Uno con fecha anterior al ultimo no entra");
				Assert.That(
					c.PuedeFijarAvaluoDeFinca("F-9", UnDiaPosterior, 40000000m), Is.False,
					"Ni uno de una finca que la municipalidad no mando");
			});
		}

		// LA DECLARACION DE LA PERSONA, con la suya, que es la misma ley sobre otra linea.
		[Test]
		public void ThePersonsDeclaredValueIsInForceFromItsDateOnwardAndOnePerDay()
		{
			Cuenta c = ConUnaFinca();
			c.DeclararValorDeFinca("F-1", ElDiaDeLaDeclaracion, LoQueDeclaroLaPersona);
			Finca f = c.FincaNumero("F-1");

			Assert.Multiple(() =>
			{
				Assert.That(
					f.ValorDeclaradoVigenteAl(UnDiaPosterior), Is.EqualTo(LoQueDeclaroLaPersona),
					"Declarado en 2025, el valor vigente en 2026 es el de 2025");
				Assert.That(
					c.PuedeDeclararValorDeFinca("F-1", ElDiaDeLaDeclaracion, LoQueDeclaroLaPersona),
					Is.False,
					"La misma declaracion otra vez no entra: es el mismo hecho -regla 18, hueco 12-");
				Assert.That(
					c.PuedeDeclararValorDeFinca("F-1", UnDiaPosterior, 65000000m), Is.True,
					"Una declaracion posterior, si");
				Assert.That(f.TieneValorDeclaradoAl(UnDiaAnterior), Is.False,
					"Pero en junio de 2024 todavia no habia");
				Assert.That(
					c.PuedeDeclararValorDeFinca("F-1", UnDiaAnterior, 50000000m), Is.False,
					"Una declaracion con fecha anterior a la ultima no entra");
			});
		}

		// LA AFIRMACION QUE JUSTIFICA LAS DOS LINEAS: ninguna contesta por la otra, en las dos
		// direcciones. Con una sola linea sin origen, la app podria decir que la municipalidad cobro con
		// un numero que escribio la persona -- que es exactamente lo que no se puede afirmar.
		[Test]
		public void NeitherTimelineEverAnswersForTheOther()
		{
			Cuenta soloDeclarado = ConUnaFinca();
			soloDeclarado.DeclararValorDeFinca("F-1", ElDiaDeLaDeclaracion, LoQueDeclaroLaPersona);

			Cuenta soloAvaluo = ConUnaFinca();
			soloAvaluo.FijarAvaluoDeFinca("F-1", ElDiaDelAvaluo, LoQueAvaluoLaMunicipalidad);

			Assert.Multiple(() =>
			{
				Assert.That(soloDeclarado.TieneValorDeclaradoDeFincaAl("F-1", UnDiaPosterior), Is.True,
					"La persona declaro");
				Assert.That(soloDeclarado.TieneAvaluoDeFincaAl("F-1", UnDiaPosterior), Is.False,
					"Y declarar NO publica un avaluo: la municipalidad no dijo nada");
				Assert.Throws<DomainException>(
					() => soloDeclarado.AvaluoDeFincaAl("F-1", UnDiaPosterior),
					"Y pedir el avaluo revienta en vez de contestar lo declarado");

				Assert.That(soloAvaluo.TieneAvaluoDeFincaAl("F-1", UnDiaPosterior), Is.True,
					"La municipalidad avaluo");
				Assert.That(soloAvaluo.TieneValorDeclaradoDeFincaAl("F-1", UnDiaPosterior), Is.False,
					"Y publicar un avaluo NO declara nada por la persona");
				Assert.Throws<DomainException>(
					() => soloAvaluo.ValorDeclaradoDeFincaAl("F-1", UnDiaPosterior),
					"Y pedir lo declarado revienta en vez de contestar el avaluo");
			});
		}

		// LOS DOS HECHOS DEL MISMO DIA ENTRAN LOS DOS, que es la consecuencia de que las identidades sean
		// por linea y no una compartida: la municipalidad puede avaluar el mismo dia en que la persona
		// declara, y ninguno de los dos es el mismo hecho que el otro.
		[Test]
		public void BothFactsOfTheSameDayEnterBecauseIdentityIsPerTimeline()
		{
			Cuenta c = ConUnaFinca();
			c.FijarAvaluoDeFinca("F-1", ElDiaDelAvaluo, LoQueAvaluoLaMunicipalidad);
			c.DeclararValorDeFinca("F-1", ElDiaDelAvaluo, LoQueDeclaroLaPersona);
			Finca f = c.FincaNumero("F-1");

			Assert.Multiple(() =>
			{
				Assert.That(f.CuantosAvaluos, Is.EqualTo(1));
				Assert.That(f.CuantosValoresDeclarados, Is.EqualTo(1));
				Assert.That(f.AvaluoVigenteAl(UnDiaPosterior), Is.EqualTo(LoQueAvaluoLaMunicipalidad),
					"y cada linea contesta lo suyo, anclado a lo que esta prueba escribio");
				Assert.That(
					f.ValorDeclaradoVigenteAl(UnDiaPosterior), Is.EqualTo(LoQueDeclaroLaPersona));
			});
		}

		private static Cuenta ConUnaFinca()
		{
			Cuenta c = new Cuenta();
			c.IncorporarFinca("F-1");
			return c;
		}
	}
}
