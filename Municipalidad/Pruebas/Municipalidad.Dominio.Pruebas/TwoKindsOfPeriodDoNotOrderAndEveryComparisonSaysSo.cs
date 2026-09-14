using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;

namespace Municipalidad.Dominio.Pruebas
{
	// E46, propuesta rio arriba el 2026-09-10 y PENDIENTE DE REVISION DEL EQUIPO. La guia escribe el
	// caso con los nombres de este modulo, porque salio de aca: un Periodo abstracto con Mes y
	// Trimestre sobre una misma Clave ordinal.
	//
	// LA REGLA. Un tipo abstracto con mas de una comparacion las guarda TODAS por la clase, y la
	// respuesta depende de la operacion: una igualdad entre dos clases contesta que NO, un orden entre
	// dos clases REVIENTA, porque no tiene respuesta. Guardar una y dejar la hermana abierta es el
	// defecto, y era lo que habia: EsElMismoQue miraba GetType y EsAnteriorA no.
	//
	// POR QUE NO SALIA EN NINGUNA PRUEBA. Los nueve sitios que llaman EsAnteriorA filtran por rubro
	// antes, y un rubro tiene una sola cadencia, asi que por el camino vivo los dos periodos ya son de
	// la misma clase. Variacion.Entre lo hace explicito: pregunta EsDe(actual.Rubro) ANTES de ordenar,
	// que es la guarda por el atributo del dominio que E46 prefiere. Es resistencia al mal uso y no una
	// conducta que el dominio prometa, asi que se ve leyendo las dos comparaciones una al lado de la
	// otra y no corriendo el dominio.
	//
	// EL TIPO QUE SE LANZA NO ES DomainException, Y ESO ES DEC-08 Y C4. Ordenar dos clases es un
	// defecto de quien llama y no una negativa que una caratula le diga a una persona. El vecino mas
	// cercano es la guarda de rubro de Variacion.Entre, que lanza ArgumentException. E46 pide
	// DomainException; C4 ya tiene registrado por que este modulo parte en dos ese vocabulario.
	[TestFixture]
	public sealed class TwoKindsOfPeriodDoNotOrderAndEveryComparisonSaysSo
	{
		private Periodo abril;
		private Periodo segundoTrimestre;

		// LOS DOS NUMEROS SON DISTINTOS A PROPOSITO. Con el mismo numero las dos Claves coinciden y un
		// EsAnteriorA sin guarda contestaria que no en las dos direcciones, que es lo mismo que
		// contestaria uno guardado: la prueba pasaria sin defender nada. Con 202604 contra 202602 el
		// orden sin guarda contesta que SI en una de las dos, que es el hallazgo.
		[SetUp]
		public void Setup()
		{
			abril = Rubro.Basura().LeerPeriodo(2026, 4);
			segundoTrimestre = Rubro.BienesInmuebles().LeerPeriodo(2026, 2);
		}

		[Test]
		public void TheOrderingRefusesTwoKindsInBothDirections()
		{
			Assert.Multiple(() =>
			{
				Assert.Throws<ArgumentException>(
					() => abril.EsAnteriorA(segundoTrimestre),
					"Un mes contra un trimestre no tiene un anterior");
				Assert.Throws<ArgumentException>(
					() => segundoTrimestre.EsAnteriorA(abril),
					"Y al reves tampoco: es la direccion que sin guarda contestaba que si");
			});
		}

		// LA POLARIDAD ES LA OTRA PARA LA IGUALDAD, y por eso las dos comparaciones no se escriben
		// igual. "Es el mismo periodo" entre un mes y un trimestre tiene una respuesta honesta, y es
		// que no. TheSameNumberIsAMonthForOneRubroAndAQuarterForAnother ya lo afirma en una direccion;
		// aca van las dos, que es lo que hace falta para que la guarda no dependa de cual se pregunta.
		[Test]
		public void AndTheSamenessAnswersNoInBothDirectionsInsteadOfThrowing()
		{
			Assert.Multiple(() =>
			{
				Assert.That(abril.EsElMismoQue(segundoTrimestre), Is.False);
				Assert.That(segundoTrimestre.EsElMismoQue(abril), Is.False);
			});
		}

		// EL CONTROL. Una guarda que refuse todo pasaria las dos pruebas de arriba sin que el orden
		// sirva para nada, asi que hay que mostrar que adentro de una cadencia sigue contestando. Y la
		// premisa se afirma tambien: si las dos Claves fueran iguales, la prueba de arriba no estaria
		// defendiendo lo que dice defender.
		[Test]
		public void ControlTheOrderingStillAnswersInsideOneCadence()
		{
			Periodo marzo = Rubro.Basura().LeerPeriodo(2026, 3);
			Periodo primerTrimestre = Rubro.BienesInmuebles().LeerPeriodo(2026, 1);

			Assert.Multiple(() =>
			{
				Assert.That(abril.Clave, Is.Not.EqualTo(segundoTrimestre.Clave),
					"Las dos Claves tienen que diferir, o el orden sin guarda habria contestado que no "
					+ "en las dos direcciones y la prueba hermana no probaria nada");
				Assert.That(marzo.EsAnteriorA(abril), Is.True, "Marzo es anterior a abril");
				Assert.That(abril.EsAnteriorA(marzo), Is.False, "Y abril no es anterior a marzo");
				Assert.That(primerTrimestre.EsAnteriorA(segundoTrimestre), Is.True,
					"El primer trimestre es anterior al segundo");
			});
		}

		// C57: un barrido vale lo que vale lo que enumera, y esta regla es sobre TODAS las
		// comparaciones y no sobre las dos que hoy se conocen. El censo enumera por reflexion y afirma
		// el conjunto EXACTO, asi que una comparacion nueva -- sobre estos tipos o sobre una jerarquia
		// que todavia no existe -- se pone roja aca y obliga a decidir su guarda y su polaridad.
		[Test]
		public void AndTheseAreEveryComparisonTheDomainHasBetweenTwoOfAKind()
		{
			var found = new List<string>();

			foreach (Type each in TheDomainsTypes())
				foreach (MethodInfo method in each.GetMethods(
					BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
					| BindingFlags.DeclaredOnly))
				{
					if (method.ReturnType != typeof(bool)) continue;

					ParameterInfo[] parameters = method.GetParameters();

					if (parameters.Length != 1) continue;
					if (!parameters[0].ParameterType.IsAssignableFrom(each)) continue;

					found.Add(each.Name + "." + method.Name);
				}

			Assert.That(found, Is.EquivalentTo(new[]
			{
				"Periodo.EsElMismoQue",
				"Periodo.EsAnteriorA",
				"Cadencia.Es",
				"LeyDeCalculo.Es",
				"Rubro.Es",
				"Urgencia.Es",
			}), "El conjunto de comparaciones del dominio cambio: " + string.Join(", ", found)
				+ ". Cada una necesita decidir su guarda por clase y su polaridad -- una igualdad "
				+ "contesta que no entre dos clases, un orden revienta.");
		}

		// El ensamblado se alcanza por un tipo que se nombra y no por su nombre en una cadena: sigue
		// siendo reflexion, porque tiene que cubrir lo que todavia no existe, pero no puede quedarse
		// vacio en silencio porque alguien renombro el ensamblado.
		private static List<Type> TheDomainsTypes()
		{
			var found = new List<Type>();

			foreach (Type each in typeof(Periodo).Assembly.GetTypes())
				if (each.Namespace == typeof(Periodo).Namespace) found.Add(each);

			return found;
		}
	}
}
