using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace DealershipCar.Dominio.Pruebas
{
	// L2 de SLICE-CostLedger.md: no hay tasa, no hay moneda base, no hay normalizacion.
	//
	// LA LEY LA CARGA LA FORMA DEL TIPO Y NO ESTA PRUEBA, y esa es la mitad que importa: el conjunto
	// cerrado no lleva NINGUN factor de conversion, asi que no hay nada que olvidar. §14 G1 lo dice
	// con las palabras de Cargas -- "que el tipo no tenga factor hace imposible olvidarlo". Lo que se
	// afirma aca es la ausencia, que no se puede afirmar llamando.
	//
	// EL ARGUMENTO, de §1.2: reconvertir un historico es falsear el pasado, y en un journal
	// append-only ademas es contradictorio.
	//
	// QUE CUBRE YA OTRA PRUEBA, RE-DERIVADO EN VEZ DE SUPUESTO (C57, y el aviso de OD-0 §1(e) sobre
	// duplicar un verde). NoTypeInTheDomainDeclaresAMemberThatConvertsBetweenUnits recorre el
	// ENSAMBLADO ENTERO buscando cinco subcadenas: "Convert", "ToUnit", "InUnit", "Normalize" y
	// "Normalise". Leidas en su fuente, no citadas. O sea que de los cuatro nombres que la ley de
	// esta rebanada prohibe -- Convert, ToBaseCurrency, Normalize, Rate -- ese barrido ya agarra DOS,
	// Convert y Normalize, y los agarra en Money y en Currency sin que nadie lo ayude, porque enumera
	// el ensamblado. Aca se afirman SOLO LOS DOS QUE NO CUBRE, ToBaseCurrency y Rate, para no
	// reportar la misma cobertura dos veces.
	[TestFixture]
	public sealed class CurrenciesAreNeverConverted
	{
		private static readonly string[] LosDosQueElBarridoDelEnsambladoNoCubre =
		{
			"ToBaseCurrency", "Rate",
		};

		[Test]
		public void AddingTwoAmountsInDifferentCurrenciesIsRefused()
		{
			Money colones = new Money(85000m, Currency.CRC);
			Money dolares = new Money(120m, Currency.USD);

			DomainException refused = Assert.Throws<DomainException>(() => colones.Plus(dolares));

			Assert.That(refused.Message, Does.Contain("CRC").And.Contain("USD"),
				"El rechazo nombra las dos monedas: sin ellas, quien lo lea no sabe cual cruzo con "
				+ "cual.");
			Assert.That(refused.Message, Does.Contain("no se convierte"),
				"Y dice la regla, que es la que §1.1 encontro viviendo dentro de un mensaje.");
		}

		[Test]
		public void TheRefusalIsTheDomainsOwnAndNotAnArgumentException()
		{
			Money colones = new Money(85000m, Currency.CRC);
			Money dolares = new Money(120m, Currency.USD);

			Assert.That(Assert.Catch(() => colones.Plus(dolares)),
				Is.InstanceOf<DomainException>().And.Not.InstanceOf<ArgumentException>());
		}

		[Test]
		public void TwoAmountsInTheSameCurrencyAddUpAndStayInThatCurrency()
		{
			Money sumado = new Money(85000m, Currency.CRC).Plus(new Money(15000m, Currency.CRC));

			Assert.That(sumado.Expressed(), Is.EqualTo(new Money(100000m, Currency.CRC).Expressed()));
		}

		// La pregunta TOTAL que acompana al accesor guardado (E41): quien va a sumar pregunta primero,
		// y preguntar nunca revienta. Es la que CL-3 necesita para agrupar por moneda con un recorrido
		// lineal, que es el estilo de la casa (no hay LINQ en Dominio/).
		[Test]
		public void WhetherTwoAmountsShareACurrencyIsATotalQuestion()
		{
			Money colones = new Money(85000m, Currency.CRC);
			Money masColones = new Money(1m, Currency.CRC);
			Money dolares = new Money(120m, Currency.USD);

			Assert.That(colones.IsInTheSameCurrencyAs(masColones), Is.True);
			Assert.That(colones.IsInTheSameCurrencyAs(dolares), Is.False);
		}

		[Test]
		public void NeitherTypeDeclaresAMemberNamedForAConversionTheAssemblySweepDoesNotSee()
		{
			List<string> culpables = new List<string>();

			foreach (Type tipo in LaMonedaSusVariantesYElDinero())
				foreach (string miembro in LoQueExpone(tipo))
					if (LosDosQueElBarridoDelEnsambladoNoCubre.Any(
						sospechoso => miembro.IndexOf(sospechoso, StringComparison.Ordinal) >= 0))
						culpables.Add(tipo.Name + "." + miembro);

			Assert.That(culpables, Is.Empty,
				"Miembros que suenan a conversion de moneda: " + string.Join(", ", culpables)
				+ ". Una tasa, si alguna vez hace falta, es cosa de presentacion y tiene fecha: no es "
				+ "de este modulo tenerla.");
		}

		// LA MITAD ESTRUCTURAL, y es la que hace imposible olvidarse: ningun tipo de la moneda lleva
		// un numero fraccionario. Un factor de conversion no puede ser entero, asi que su ausencia
		// como CAMPO es la ausencia del factor, aunque nadie lo llame factor. Los decimales de la
		// moneda son un int y no entran por ahi.
		[Test]
		public void NoCurrencyTypeCarriesAFractionalFieldThatCouldOnlyBeAConversionFactor()
		{
			foreach (Type tipo in LaMonedaSusVariantesYElDinero())
			{
				List<string> fracciones = tipo
					.GetFields(BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public
						| BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
					.Where(campo => campo.FieldType == typeof(double)
						|| campo.FieldType == typeof(float))
					.Select(campo => campo.Name + " : " + campo.FieldType.Name)
					.ToList();

				Assert.That(fracciones, Is.Empty,
					$"{tipo.Name} lleva un numero fraccionario que no es el monto: "
					+ string.Join(", ", fracciones) + ". Un factor de conversion se veria asi.");
			}
		}

		// Y el conjunto tampoco lleva un factor DISFRAZADO DE decimal. Money si lleva un decimal --
		// es el monto -- asi que la afirmacion se hace sobre la moneda y sus variantes solas.
		[Test]
		public void TheClosedSetItselfCarriesNoDecimalAtAll()
		{
			foreach (Type tipo in LaMonedaYSusVariantes())
			{
				List<string> decimales = tipo
					.GetFields(BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public
						| BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
					.Where(campo => campo.FieldType == typeof(decimal))
					.Select(campo => campo.Name)
					.ToList();

				Assert.That(decimales, Is.Empty,
					$"{tipo.Name} lleva un decimal: " + string.Join(", ", decimales)
					+ ". La moneda no tiene monto ni tasa; lo unico numerico que le toca son sus "
					+ "decimales, que son un entero de ISO.");
			}
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

		private static IEnumerable<Type> LaMonedaSusVariantesYElDinero()
		{
			foreach (Type tipo in LaMonedaYSusVariantes()) yield return tipo;

			yield return typeof(Money);
		}

		private static List<string> LoQueExpone(Type tipo)
		{
			return tipo
				.GetMembers(BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public
					| BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
				.Where(miembro => EsExpuesto(miembro))
				.Select(miembro => miembro.Name)
				.Distinct()
				.OrderBy(nombre => nombre, StringComparer.Ordinal)
				.ToList();
		}

		private static bool EsExpuesto(MemberInfo miembro)
		{
			MethodInfo metodo = miembro as MethodInfo;
			if (metodo != null) return metodo.IsPublic || metodo.IsAssembly;

			FieldInfo campo = miembro as FieldInfo;
			if (campo != null) return campo.IsPublic || campo.IsAssembly;

			PropertyInfo propiedad = miembro as PropertyInfo;
			if (propiedad != null)
			{
				MethodInfo lector = propiedad.GetGetMethod(true);

				return lector != null && (lector.IsPublic || lector.IsAssembly);
			}

			return miembro is Type;
		}
	}
}
