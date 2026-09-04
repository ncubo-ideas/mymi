using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace DealershipCar.Dominio.Pruebas
{
	// L14 de SLICE-CostLedger.md: un codigo fuera del conjunto se contesta con una PREGUNTA DE
	// RECONOCIMIENTO TOTAL, nunca con una guarda que reviente por Apply y pare el bloque de refresco.
	// El nombre de esta clase es el que §4 le da a esa pregunta; la pregunta es un MIEMBRO y no un
	// tipo (§7.1 del plan lo dice), asi que el nombre del canon queda aca, en la clase que enuncia
	// la ley.
	//
	// LAS TRES POLARIDADES SON DISTINTAS Y LAS TRES ESTAN BIEN:
	//
	//   Currency.IsNamed(codigo)      CONTESTA. Es la que la frontera hace antes de que haya nada
	//                                 que performar, y es la que la ingesta hace para descartar un
	//                                 payload ajeno en vez de tumbar la lectura del feed.
	//
	//   Currency.Of(codigo)           TOTAL CON EXCEPCION. Quien pide una moneda NOMBRADA y da un
	//                                 codigo que no lo es tiene un defecto, y rechazarlo antes de
	//                                 performar es lo unico que lo rechaza sin dejar entrada.
	//
	//   Currency.OfPublished(codigo)  TOTAL, SIN EXCEPCION para un codigo no nombrado. Es el camino
	//                                 de un monto QUE EL REGISTRO YA GUARDO: rechazarlo ahi
	//                                 rechazaria un hecho que ya esta escrito, y eso deja dos
	//                                 entradas fallidas en cada arranque futuro. MEDIDO en
	//                                 SPIKES-BuildOrder.md PROBE 8, y es la mitad que DEC-85 decide.
	//
	// NO HAY CurrencyCode. Es la unica desviacion de la convencion de la casa en este modulo, y el
	// motivo esta medido: C52 midio que un enum cruza el wire como el NOMBRE de su miembro, cerrado
	// solo DENTRO de un conjunto, y los tres conjuntos de unidades ya colisionan ahi. Un cuarto enum
	// sumaria a esa colision sin comprar nada. §14 G1 lo resuelve asi.
	[TestFixture]
	public sealed class ACurrencyThisModuleNames
	{
		private const string LaQueNadieNombra = "XYZ";

		[TestCase("CRC")]
		[TestCase("USD")]
		public void EveryCodeTheCanonNamesIsRecognisedAndResolvedToItsOwnMember(string codigo)
		{
			Assert.That(Currency.IsNamed(codigo), Is.True,
				$"'{codigo}' es una de las dos que el canon nombra y el reconocedor no la conoce.");

			Assert.That(Currency.Of(codigo).Code, Is.EqualTo(codigo));
			Assert.That(Currency.Of(codigo).ReceivedButNotNamed(), Is.False);
		}

		// El resolutor NO fabrica una instancia por llamada: devuelve el miembro canonico. Si
		// fabricara, la comparacion por identidad -- que es la unica que estos tipos tienen (E23) --
		// diria "distintas" para la misma moneda.
		[Test]
		public void TheResolverAlwaysReturnsTheSameInstance()
		{
			Assert.That(Currency.Of("CRC"), Is.SameAs(Currency.CRC));
			Assert.That(Currency.Of("USD"), Is.SameAs(Currency.USD));
			Assert.That(Currency.Of("CRC"), Is.SameAs(Currency.Of("CRC")));
		}

		[Test]
		public void ACodeThisModuleDoesNotNameIsNotRecognisedAndTheResolverRefusesIt()
		{
			Assert.That(Currency.IsNamed(LaQueNadieNombra), Is.False);

			DomainException refused = Assert.Throws<DomainException>(
				() => Currency.Of(LaQueNadieNombra));

			Assert.That(refused.Message, Does.Contain(LaQueNadieNombra),
				"El rechazo nombra el codigo que llego.");
			Assert.That(refused.Message, Does.Contain("CRC").And.Contain("USD"),
				"Y dice contra que miro, que es lo unico que distingue un conjunto de otro cuando dos "
				+ "aceptan lo mismo (C52).");
		}

		// LAS DOS RAMAS DE LA GUARDA TIRAN LA MISMA EXCEPCION, y esa es la correccion sobre Cargas:
		// su Moneda.PorCodigo tira ArgumentNullException para un codigo en blanco y su propio error
		// de dominio para uno desconocido, en el MISMO miembro. Aca las dos son DomainException, o
		// C22/C36/C37 muerde: una guarda que tira otra cosa se escapa de Apply y para el bloque de
		// refresco entero. Dominio/ lleva cero ArgumentNullException y se queda en cero.
		[TestCase("")]
		[TestCase("   ")]
		[TestCase(null)]
		public void ABlankCodeNamesNothingAndItsRefusalIsTheDomainsOwnToo(string blanco)
		{
			Assert.That(Currency.IsNamed(blanco), Is.False,
				"Reconocer un blanco como moneda dejaria pasar un monto sin moneda.");

			Assert.That(Assert.Catch(() => Currency.Of(blanco)),
				Is.InstanceOf<DomainException>().And.Not.InstanceOf<ArgumentException>());

			Assert.That(Assert.Catch(() => Currency.OfPublished(blanco)),
				Is.InstanceOf<DomainException>().And.Not.InstanceOf<ArgumentException>(),
				"Un blanco no es una moneda que el registro pueda tener guardada: ServicePriceEntry "
				+ "lo rechaza desde siempre, asi que reventar aca no puede cicatrizar nada.");
		}

		// LA CAJA IMPORTA. El canon manda como se escribe un codigo, y "crc" en un journal para
		// siempre es una ortografia que nadie eligio.
		[TestCase("crc")]
		[TestCase("Usd")]
		[TestCase("CRc")]
		public void ACodeInTheWrongCaseIsNotThatCurrency(string malEscrito)
		{
			Assert.That(Currency.IsNamed(malEscrito), Is.False);
			Assert.That(Assert.Catch(() => Currency.Of(malEscrito)), Is.InstanceOf<DomainException>());
		}

		[Test]
		public void APublishedCodeTheCanonNamesResolvesToTheSameMemberAsTheStrictResolver()
		{
			Assert.That(Currency.OfPublished("CRC"), Is.SameAs(Currency.CRC));
			Assert.That(Currency.OfPublished("USD"), Is.SameAs(Currency.USD));
		}

		// LA MITAD QUE DEC-85 DECIDE: un codigo publicado que el conjunto no nombra CONTESTA, y lo que
		// contesta se sabe decir. Nada de esto esta clavado todavia contra un journal -- ninguna
		// entrada usa Money hasta CL-2 -- asi que lo que se afirma aca es el comportamiento del
		// miembro, no la rehidratacion.
		[Test]
		public void APublishedCodeThisModuleDoesNotNameIsAnsweredAndNotRefused()
		{
			Currency recibida = Currency.OfPublished(LaQueNadieNombra);

			Assert.That(recibida.ReceivedButNotNamed(), Is.True);
			Assert.That(recibida.Code, Is.EqualTo(LaQueNadieNombra));
			Assert.That(recibida.Says(85000.1m), Does.Contain(LaQueNadieNombra).And.Contain("85000.1"));
		}

		// LA DECISION DE CL-1 QUE PROBE 8 DEJO ABIERTA, y es ESTRUCTURAL y no un comentario: una
		// moneda que este modulo no nombra NO SE COMPARA CON NINGUNA, ni con otra del mismo codigo, ni
		// consigo misma. Dos precios en una moneda que nadie nombro no se SABE que esten en la misma
		// moneda, y eso es L2 siendo consistente y no una limitacion aceptada.
		//
		// La respuesta vive en el override de la variante y no en cuantas veces se llamo a
		// OfPublished: si dependiera de la identidad de la instancia, la respuesta dependeria de un
		// accidente del llamador.
		[Test]
		public void ACurrencyThisModuleDoesNotNameIsNotKnownToBeTheSameAsAnyOther()
		{
			Currency una = Currency.OfPublished(LaQueNadieNombra);
			Currency otra = Currency.OfPublished(LaQueNadieNombra);

			Assert.That(una.IsTheSameCurrencyAs(otra), Is.False);
			Assert.That(una.IsTheSameCurrencyAs(una), Is.False,
				"Ni consigo misma: la respuesta no puede depender de cuantas veces se resolvio el "
				+ "codigo.");
			Assert.That(una.IsTheSameCurrencyAs(Currency.CRC), Is.False);
			Assert.That(Currency.CRC.IsTheSameCurrencyAs(una), Is.False,
				"Y en las dos direcciones.");
		}

		[Test]
		public void SoTwoAmountsInACurrencyThisModuleDoesNotNameCannotBeAddedEither()
		{
			Money una = new Money(1000m, Currency.OfPublished(LaQueNadieNombra));
			Money otra = new Money(2000m, Currency.OfPublished(LaQueNadieNombra));

			Assert.That(una.IsInTheSameCurrencyAs(otra), Is.False);
			Assert.That(Assert.Catch(() => una.Plus(otra)), Is.InstanceOf<DomainException>());
		}

		[Test]
		public void ACurrencyTheCanonNamesIsTheSameAsItselfAndNotAsTheOther()
		{
			Assert.That(Currency.CRC.IsTheSameCurrencyAs(Currency.CRC), Is.True);
			Assert.That(Currency.CRC.IsTheSameCurrencyAs(Currency.USD), Is.False);
			Assert.That(Currency.USD.IsTheSameCurrencyAs(Currency.CRC), Is.False);
		}

		// LA LEY DEL NOMBRE PROYECTADO, afirmada AQUI y no en TheProjectedNameIsTheMembersOwnName.
		// Aquella fija Name == nombre del campo sobre ocho conjuntos; Currency proyecta Code y no
		// Name, porque Code es la palabra del canon y porque una moneda que este modulo NO nombra
		// tiene codigo y no tiene nombre -- llamarle Name seria una contradiccion en el termino. La
		// ley de fondo es la misma y no necesita la exclusion de C67: el Code de CRC es "CRC", el
		// nombre del miembro mismo, al contrario de UnitA que se dice "km".
		[Test]
		public void TheProjectedCodeIsTheMembersOwnName()
		{
			foreach (FieldInfo campo in MiembrosDe(typeof(Currency)))
			{
				Currency miembro = (Currency)campo.GetValue(null);

				Assert.That(miembro.Code, Is.EqualTo(campo.Name),
					$"El miembro {campo.Name} proyecta '{miembro.Code}'. El wire y el journal llevan "
					+ "el codigo, asi que despegarlo del nombre del miembro deja dos vocabularios.");
			}
		}

		[Test]
		public void TheTwoTheCanonNamesAreTheOnlyOnesTheResolverEverHandsOut()
		{
			List<string> nombrados = MiembrosDe(typeof(Currency))
				.Select(campo => campo.Name)
				.OrderBy(nombre => nombre, StringComparer.Ordinal)
				.ToList();

			Assert.That(nombrados, Is.EqualTo(new List<string> { "CRC", "USD" }),
				"P-8 nombra dos. Agregar una tercera cuesta un static readonly, que es el precio del "
				+ "conjunto cerrado y no una razon para abrirlo.");

			foreach (string codigo in nombrados)
				Assert.That(Currency.Of(codigo).ReceivedButNotNamed(), Is.False);
		}

		private static List<FieldInfo> MiembrosDe(Type conjunto)
		{
			return conjunto
				.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic
					| BindingFlags.DeclaredOnly)
				.Where(campo => campo.IsInitOnly && campo.FieldType == conjunto)
				.ToList();
		}
	}
}
