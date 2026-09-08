using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace Concesionario.Dominio.Pruebas
{
	// LEY L4 DEL SLICE, y G1 es su argumento: "Los dias de calendario que una estadia cubrio llegan
	// CON EL HECHO, dichos por la sucursal. No se calculan desde los instantes -- eso necesitaria
	// una zona horaria que este modulo no tiene (G1), y volveria APROXIMADO el cruce entre
	// instituciones justo donde tiene que ser exacto."
	//
	// LA MITAD ESTRUCTURAL ES LA QUE IMPORTA, y es por reflexion: StayDay NO DECLARA NINGUN MIEMBRO
	// QUE TOQUE UN DateTime. Un metodo que tomara un instante seria la derivacion prohibida con
	// otro nombre, y una prueba conductual no lo veria mientras nadie lo llamara.
	//
	// LAS DOS DIRECCIONES SE PROHIBEN, no solo una. Un StayDay construido desde un instante inventa
	// una zona; un StayDay que DEVOLVIERA un instante la inventa igual, en el otro sentido, y el que
	// lo reciba no tiene como saberlo. Por eso el barrido mira parametros Y retornos.
	//
	// Y EL BARRIDO SE DEFIENDE A SI MISMO (C61): si la enumeracion devolviera cero miembros, la
	// afirmacion de arriba quedaria verde sin haber mirado nada. Asi que primero se afirma QUE se
	// enumero, por nombre.
	[TestFixture]
	public sealed class ACalendarDayIsReceivedAndNeverDerived
	{
		private const BindingFlags Declarados = BindingFlags.DeclaredOnly | BindingFlags.Instance
			| BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

		// PRIMERO EL INSTRUMENTO. Estos son los miembros que el barrido de abajo recorre; si la
		// lista se vaciara, el barrido dejaria de proteger nada.
		[Test]
		public void TheSweepActuallyEnumeratesTheMembersOfStayDay()
		{
			List<string> metodos = typeof(StayDay).GetMethods(Declarados)
				.Where(metodo => !metodo.IsSpecialName)
				.Select(metodo => metodo.Name)
				.OrderBy(nombre => nombre, StringComparer.Ordinal)
				.ToList();

			Assert.That(metodos, Is.EqualTo(new[]
				{ "HowItIsSaid", "IsAfter", "IsBefore", "IsTheSameDayAs", "Next" }),
				"StayDay cambio de superficie. El barrido de L4 recorre estos miembros: si la "
				+ "lista se queda corta, el barrido queda verde sin mirar el miembro nuevo (C57).");

			Assert.That(typeof(StayDay).GetConstructors(Declarados), Is.Not.Empty);
		}

		// L4, LA MITAD ESTRUCTURAL. Ningun miembro declarado -- constructor, metodo o propiedad --
		// toma ni devuelve un DateTime.
		[Test]
		public void StayDayDeclaresNoMemberThatTakesOrReturnsAnInstant()
		{
			List<string> culpables = new List<string>();

			foreach (ConstructorInfo ctor in typeof(StayDay).GetConstructors(Declarados))
				foreach (ParameterInfo parametro in ctor.GetParameters())
					if (parametro.ParameterType == typeof(DateTime))
						culpables.Add("StayDay..ctor(" + parametro.Name + ")");

			foreach (MethodInfo metodo in typeof(StayDay).GetMethods(Declarados))
			{
				foreach (ParameterInfo parametro in metodo.GetParameters())
					if (parametro.ParameterType == typeof(DateTime))
						culpables.Add("StayDay." + metodo.Name + "(" + parametro.Name + ")");

				if (metodo.ReturnType == typeof(DateTime))
					culpables.Add("StayDay." + metodo.Name + " -> DateTime");
			}

			foreach (PropertyInfo propiedad in typeof(StayDay).GetProperties(Declarados))
				if (propiedad.PropertyType == typeof(DateTime))
					culpables.Add("StayDay." + propiedad.Name + " : DateTime");

			Assert.That(culpables, Is.Empty,
				"L4 y G1: un dia de calendario no sale de un instante sin una zona, y este modulo "
				+ "no tiene ninguna. Miembros que tocan un instante: "
				+ string.Join(", ", culpables));
		}

		// EL DIA LLEGA ESCRITO Y EN UN SOLO FORMATO. Un formato tolerante seria la puerta por la que
		// un dia mal dicho entra al journal de una persona y se rehidrata mal para siempre (S5).
		[TestCase("2026-6-10")]
		[TestCase("10/06/2026")]
		[TestCase("2026-06-10T00:00:00")]
		[TestCase("2026-13-01")]
		[TestCase("junio 10")]
		[TestCase("")]
		[TestCase("   ")]
		[TestCase(null)]
		public void ADayThatIsNotSaidInTheOneFormatIsACallerBug(string text)
		{
			Assert.Throws<DomainException>(() => new StayDay(text));
		}

		// Y EL DIA QUE SI LLEGA BIEN SE DICE DE VUELTA IGUAL. Es la mitad que hace falta para que la
		// de arriba no se cumpla con un constructor que rechace todo.
		[Test]
		public void ADayThatArrivesWellSaysItselfBackTheSameWay()
		{
			Assert.That(new StayDay("2026-06-10").HowItIsSaid(), Is.EqualTo("2026-06-10"));
			Assert.That(new StayDay("2025-12-31").HowItIsSaid(), Is.EqualTo("2025-12-31"));
		}

		// LAS OPERACIONES CON NOMBRE, contra dias y nunca contra instantes. Next() es la que L5 usa
		// para la contiguidad, y cruza el fin de mes sin que nadie le diga cuantos dias tiene.
		[Test]
		public void TheNamedOperationsCompareDaysWithDays()
		{
			StayDay theTenth = new StayDay("2026-06-10");
			StayDay theEleventh = new StayDay("2026-06-11");

			Assert.That(theTenth.IsBefore(theEleventh), Is.True);
			Assert.That(theEleventh.IsAfter(theTenth), Is.True);
			Assert.That(theTenth.IsTheSameDayAs(new StayDay("2026-06-10")), Is.True);
			Assert.That(theTenth.IsTheSameDayAs(theEleventh), Is.False);
			Assert.That(theTenth.Next().IsTheSameDayAs(theEleventh), Is.True);

			Assert.That(new StayDay("2026-06-30").Next().HowItIsSaid(), Is.EqualTo("2026-07-01"),
				"el fin de mes lo sabe el calendario, no una cuenta");
			Assert.That(new StayDay("2024-02-28").Next().HowItIsSaid(), Is.EqualTo("2024-02-29"),
				"y el ano bisiesto tambien");
		}
	}
}
