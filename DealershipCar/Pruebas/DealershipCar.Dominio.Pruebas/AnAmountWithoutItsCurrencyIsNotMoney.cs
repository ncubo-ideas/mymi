using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace DealershipCar.Dominio.Pruebas
{
	// L1 de SLICE-CostLedger.md: el monto y la moneda son UN valor y no se pueden tener aparte.
	//
	// LA LEY TIENE DOS MITADES Y LA SEGUNDA ES LA QUE SOBREVIVE A UN REFACTOR. La primera es el
	// constructor: sin moneda no hay dinero. La segunda es que NINGUN MIEMBRO entrega el monto solo,
	// y esa no se puede afirmar llamando -- hay que enumerar los miembros por reflexion, porque el
	// miembro que la rompe es el que alguien AGREGUE manana. Un test que solo probara el ctor pasaria
	// en verde el dia que apareciera un `internal decimal Amount => _amount;`.
	//
	// Y POR QUE IMPORTA QUE NO SALGA SOLO: §1.1 encontro el monto y la moneda como dos campos
	// separados en ServicePriceEntry, con la regla de que las monedas no se convierten dicha
	// unicamente dentro del mensaje de una guarda. Quien tuviera el monto suelto podria sumarlo con
	// otro de otra moneda y decir un total que el dominio nunca dijo.
	//
	// EL CENSO ALCANZA TAMBIEN A Currency Y A SUS DOS VARIANTES, por E24: si Currency entregara sus
	// MinorUnits, una caratula podria redondear por su cuenta, que es la regla 3 de la convencion de
	// registro crudo con otra ropa. El redondeo vive en un solo sitio, adentro.
	[TestFixture]
	public sealed class AnAmountWithoutItsCurrencyIsNotMoney
	{
		[Test]
		public void MoneyWithNoCurrencyIsRefused()
		{
			DomainException refused = Assert.Throws<DomainException>(
				() => new Money(85000m, null));

			Assert.That(refused.Message, Does.Contain("moneda"),
				"El rechazo tiene que decir que falta la moneda, no solo que algo esta mal.");
		}

		// Y revienta con la excepcion DEL DOMINIO y no con una ArgumentException. No es estilo:
		// C22/C36/C37 midieron que una guarda que tira otra cosa se ESCAPA de Apply y para el bloque
		// de refresco entero, que es peor que una entrada cicatrizada por un hecho recibido.
		[Test]
		public void TheRefusalIsTheDomainsOwnAndNotAnArgumentException()
		{
			Assert.That(Assert.Catch(() => new Money(85000m, null)),
				Is.InstanceOf<DomainException>().And.Not.InstanceOf<ArgumentException>());
		}

		[Test]
		public void ANegativeAmountIsNotMoney()
		{
			DomainException refused = Assert.Throws<DomainException>(
				() => new Money(-1m, Currency.CRC));

			Assert.That(refused.Message, Does.Contain("-1"),
				"El rechazo nombra el monto que llego.");
		}

		// Cero SI es dinero: un servicio incluido es un precio publicado, y ServicePriceEntry ya lo
		// acepta con ese mismo argumento. Cero y "nada registrado" son dos hechos distintos (L8).
		[Test]
		public void ZeroIsMoneyAndIsAccepted()
		{
			Assert.That(new Money(0m, Currency.CRC).Expressed(), Does.Contain("0"));
		}

		[Test]
		public void NoMemberOfMoneyHandsOutTheAmountAlone()
		{
			Assert.That(LoQueDevuelveUnNumero(typeof(Money)), Is.Empty,
				"Miembros de Money que devuelven un numero suelto: "
				+ string.Join(", ", LoQueDevuelveUnNumero(typeof(Money)))
				+ ". Un monto sin su moneda no se puede leer, y quien lo tuviera podria sumarlo con "
				+ "otro de otra moneda.");
		}

		[Test]
		public void NoMemberOfTheCurrencyHandsOutANumberEither()
		{
			foreach (Type tipo in LaMonedaYSusVariantes())
				Assert.That(LoQueDevuelveUnNumero(tipo), Is.Empty,
					$"Miembros de {tipo.Name} que devuelven un numero suelto: "
					+ string.Join(", ", LoQueDevuelveUnNumero(tipo))
					+ ". Sus decimales son un hecho de ISO que se usa adentro para decir el monto; "
					+ "entregarlo dejaria que una caratula redondeara por su cuenta.");
		}

		// La otra mitad de la ley, por VALOR: el monto que entro sale dicho CON su moneda, en la
		// misma frase, y no hay forma de tener uno sin la otra.
		[Test]
		public void TheAmountOnlyEverLeavesInsideASentenceThatNamesItsCurrency()
		{
			Assert.That(new Money(85000m, Currency.CRC).Expressed(),
				Does.Contain("85000").And.Contain(Currency.CRC.Says(85000m)));
		}

		private static IEnumerable<Type> LaMonedaYSusVariantes()
		{
			yield return typeof(Currency);

			foreach (Type tipo in typeof(Currency).Assembly
				.GetTypes()
				.Where(cada => cada != typeof(Currency) && typeof(Currency).IsAssignableFrom(cada))
				.OrderBy(cada => cada.Name, StringComparer.Ordinal))
				yield return tipo;
		}

		// Se miran los miembros EXPUESTOS -- public o internal, que son los que el DSL puede nombrar
		// por reflexion (E15) -- y tambien los getters de propiedad, que de otro modo se colarian:
		// un `internal decimal Amount { get; }` es un metodo get_Amount marcado IsSpecialName.
		private static List<string> LoQueDevuelveUnNumero(Type tipo)
		{
			Type[] numeros =
			{
				typeof(decimal), typeof(double), typeof(float), typeof(int), typeof(long),
				typeof(short), typeof(byte), typeof(decimal[]), typeof(int[]), typeof(long[]),
			};

			const BindingFlags Declarados = BindingFlags.DeclaredOnly | BindingFlags.Static
				| BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

			List<string> culpables = new List<string>();

			foreach (MethodInfo metodo in tipo.GetMethods(Declarados))
				if ((metodo.IsPublic || metodo.IsAssembly)
					&& numeros.Contains(metodo.ReturnType))
					culpables.Add(tipo.Name + "." + metodo.Name);

			foreach (FieldInfo campo in tipo.GetFields(Declarados))
				if ((campo.IsPublic || campo.IsAssembly) && numeros.Contains(campo.FieldType))
					culpables.Add(tipo.Name + "." + campo.Name);

			return culpables;
		}
	}
}
