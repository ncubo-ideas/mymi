using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace DealershipCar.Dominio.Pruebas
{
	// E23, LA DOCTRINA DEL MAPA DE IDENTIDAD, ejecutable sobre TODO el ensamblado.
	//
	// "Cada objeto del dominio es una entidad mutable mapeada por identidad. Nunca Equals /
	// GetHashCode / IEquatable<T> / ==. Ningun record, ningun struct de igualdad de valor.
	// Iguales solo si son el MISMO objeto."
	//
	// Es una prueba sobre el ensamblado entero y no sobre una lista de tipos a proposito:
	// asi sigue protegiendo a T3, T4 y T5, donde entran los tipos que de verdad tientan --
	// un BasePoint (instante + lectura + unidad), un OdometerReading, un IntervalLengthFactor.
	// Los tres se ven como "value objects" desde tres metros y ninguno lo es.
	//
	// Por que muerde aca mas que en un dominio cualquiera: el journal reconstruye el
	// agregado por REPLAY de comandos, no por snapshot. Un hash sobre contenido mutable se
	// mueve cuando el contenido se mueve, asi que una coleccion con clave de valor se rompe
	// en silencio justo despues de una rehidratacion -- que es el momento en que nadie esta
	// mirando.
	//
	// §12 del slice pide esto como grep. Un grep hay que acordarse de correr; esto corre solo.
	[TestFixture]
	public sealed class ElDominioNoEscribeIgualdad
	{
		private const string EnsambladoDelDominio = "DealershipCar.Dominio";

		private List<Type> tipos;

		[SetUp]
		public void Setup()
		{
			tipos = Assembly.Load(new AssemblyName(EnsambladoDelDominio))
				.GetTypes()
				.Where(tipo => tipo.Namespace == EnsambladoDelDominio)
				.Where(tipo => !tipo.IsDefined(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute), false))
				.ToList();
		}

		[Test]
		public void NingunTipoSobreescribeEqualsNiGetHashCode()
		{
			List<string> culpables = new List<string>();

			foreach (Type tipo in tipos)
			{
				foreach (MethodInfo metodo in tipo.GetMethods(
					BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
					| BindingFlags.DeclaredOnly))
				{
					if (metodo.Name == "Equals" || metodo.Name == "GetHashCode")
						culpables.Add(tipo.Name + "." + metodo.Name);
				}
			}

			Assert.That(culpables, Is.Empty,
				"Igualdad escrita a mano: " + string.Join(", ", culpables) + ". "
				+ "E23: los objetos del dominio son iguales solo si son el mismo objeto. Si hacia "
				+ "falta preguntar 'son comparables estos dos', eso es una OPERACION CON NOMBRE "
				+ "(EsLaMisma, Overlaps, IsAtLeast), no un Equals.");
		}

		[Test]
		public void NingunTipoImplementaIEquatableNiDeclaraOperadoresDeIgualdad()
		{
			List<string> culpables = new List<string>();

			foreach (Type tipo in tipos)
			{
				bool equatable = tipo.GetInterfaces().Any(interfaz =>
					interfaz.IsGenericType
					&& interfaz.GetGenericTypeDefinition() == typeof(IEquatable<>));

				if (equatable) culpables.Add(tipo.Name + " : IEquatable<>");

				foreach (MethodInfo metodo in tipo.GetMethods(
					BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic
					| BindingFlags.DeclaredOnly))
				{
					if (metodo.Name == "op_Equality" || metodo.Name == "op_Inequality")
						culpables.Add(tipo.Name + "." + metodo.Name);
				}
			}

			Assert.That(culpables, Is.Empty,
				"Igualdad de valor por la puerta de atras: " + string.Join(", ", culpables) + ". "
				+ "El == que el dominio usa es el de referencia, el que viene de fabrica.");
		}

		// Un record, un record struct o un struct cualquiera trae igualdad de valor SIN que
		// nadie la escriba. Es la forma en que la doctrina se rompe sin dejar rastro en un
		// diff: la palabra 'record' entra en una linea y la igualdad viene de regalo.
		[Test]
		public void NingunTipoEsRecordNiStructDeValor()
		{
			List<string> culpables = tipos
				.Where(tipo => tipo.IsValueType && !tipo.IsEnum)
				.Select(tipo => tipo.Name + " (struct)")
				.ToList();

			culpables.AddRange(tipos
				.Where(tipo => tipo.GetMethod("<Clone>$",
					BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
					| BindingFlags.DeclaredOnly) != null)
				.Select(tipo => tipo.Name + " (record)"));

			Assert.That(culpables, Is.Empty,
				"Tipos con igualdad de valor implicita: " + string.Join(", ", culpables) + ". "
				+ "E23 los prohibe justamente porque no hace falta escribir nada para tenerla. "
				+ "Los enums quedan afuera: son los codigos del wire, y una clave primitiva es "
				+ "exactamente con lo que SI se puede indexar un diccionario.");
		}
	}
}
