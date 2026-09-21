using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;

namespace Municipalidad.Dominio.Pruebas
{
	// E16, con la nota agregada rio arriba el 2026-09-10 y PENDIENTE DE REVISION DEL EQUIPO. La guia
	// escribe el caso con Rubro, su cadencia y su ley, porque salio de aca.
	//
	// LA REGLA. Un conjunto cerrado tiene UNA instancia por miembro, y quien lo resuelve entrega esa
	// instancia en vez de armar una nueva. DEC-13 ya decidio la mitad dificil -- los miembros son
	// metodos estaticos y no campos, porque el DSL alcanza un metodo y no un campo -- y esta es la
	// otra mitad: un metodo satisface "expone un resolutor" igual de bien armando el miembro cada vez,
	// y ahi se pierde la identidad por referencia.
	//
	// POR QUE NO HABIA UN ERROR VISIBLE. Este modulo compara por NOMBRE en las cuatro -- Rubro.Es,
	// Cadencia.Es, LeyDeCalculo.Es, Urgencia.Es -- y ninguna coleccion se llavea por un miembro, cosa
	// que se midio antes de tocar nada. Comparar por nombre es exactamente el rodeo que la nota
	// describe: funciona, y deja la ley de E16 sin cumplir.
	//
	// LO QUE SI COSTABA: PorNombre armaba tres objetos por llamada, porque el Rubro arma ademas su
	// cadencia y su ley; LosQueHay armaba quince para devolver cinco cadenas.
	[TestFixture]
	public sealed class EveryCatalogueMemberIsOneSharedInstance
	{
		// EL BARRIDO CORRE SOBRE EL ENSAMBLADO y no sobre una lista de cuatro tipos, por la razon de
		// siempre: un conjunto cerrado nuevo entra solo el dia que alguien lo escriba.
		[Test]
		public void EveryFactoryOfTheDomainHandsBackTheSameInstanceTwice()
		{
			List<string> rebuilt = TheOnesThatRebuildTheirMember(TheDomainsTypes());

			Assert.That(rebuilt, Is.Empty,
				"Miembros que se vuelven a armar en cada llamada: " + string.Join(", ", rebuilt)
				+ ". Un conjunto cerrado tiene una instancia por miembro, y un resolutor que la arma "
				+ "de nuevo deja dos miembros iguales sin ser el mismo objeto.");
		}

		// C57 otra vez: si el filtro deja de reconocer la forma, la prueba de arriba se pone verde por
		// vacia. Esta nombra lo que encontro en vez de contarlo, y afirma el conjunto EXACTO para que
		// un miembro nuevo del catalogo llegue aca a decidir si comparte instancia.
		[Test]
		public void AndTheseAreEveryMemberTheCatalogueHas()
		{
			var named = new List<string>();

			foreach (MethodInfo each in TheFactoriesOf(TheDomainsTypes()))
				named.Add(each.DeclaringType.Name + "." + each.Name);

			Assert.That(named, Is.EquivalentTo(new[]
			{
				"Rubro.Basura", "Rubro.LimpiezaDeCalles", "Rubro.Policia",
				"Rubro.BienesInmuebles", "Rubro.Parques",
				"Cadencia.Mensual", "Cadencia.Trimestral",
				"LeyDeCalculo.PorUnidad", "LeyDeCalculo.SobreBaseImponible",
				"Urgencia.Ninguna", "Urgencia.Aviso", "Urgencia.Urgente",
			}), "El catalogo cambio: " + string.Join(", ", named));
		}

		// Y EL RESOLUTOR DESDE EL PRIMITIVO ENTREGA ESA MISMA INSTANCIA, que es la mitad que la nota
		// nombra: es por PorNombre por donde el miembro entra desde el wire, asi que es ahi donde un
		// new se esconde mejor. Va escrito a mano y con los nombres literales del canon, porque
		// sacarlos del propio tipo haria que la prueba y el codigo estuvieran de acuerdo consigo
		// mismos y no con el canon.
		[Test]
		public void AndTheResolverFromTheWireHandsBackThatSameInstance()
		{
			Assert.Multiple(() =>
			{
				Assert.That(Rubro.PorNombre("recoleccion-de-basura"),
					Is.SameAs(Rubro.Basura()));
				Assert.That(Rubro.PorNombre("bienes-inmuebles"),
					Is.SameAs(Rubro.BienesInmuebles()));
				Assert.That(Cadencia.PorNombre("mensual"),
					Is.SameAs(Cadencia.Mensual()));
				Assert.That(LeyDeCalculo.PorNombre("sobre-base-imponible"),
					Is.SameAs(LeyDeCalculo.SobreBaseImponible()));
				Assert.That(Urgencia.PorNombre("urgente"),
					Is.SameAs(Urgencia.Urgente()));
			});
		}

		// EL CONTROL. Un detector que dejara de morder pondria verde la primera prueba sin mirar nada,
		// asi que corre sobre un par local: uno que comparte y uno que arma de nuevo.
		[Test]
		public void ControlTheInstrumentCatchesAFactoryThatRebuilds()
		{
			Assert.Multiple(() =>
			{
				Assert.That(
					TheOnesThatRebuildTheirMember(new List<Type> { typeof(ACatalogueThatRebuilds) }),
					Is.Not.Empty,
					"El detector dejo pasar una fabrica que arma de nuevo");
				Assert.That(
					TheOnesThatRebuildTheirMember(new List<Type> { typeof(ACatalogueThatShares) }),
					Is.Empty,
					"El detector mordio una fabrica que si comparte");
			});
		}

		private static List<string> TheOnesThatRebuildTheirMember(List<Type> family)
		{
			var rebuilt = new List<string>();

			foreach (MethodInfo each in TheFactoriesOf(family))
				if (!ReferenceEquals(each.Invoke(null, null), each.Invoke(null, null)))
					rebuilt.Add(each.DeclaringType.Name + "." + each.Name);

			return rebuilt;
		}

		// LA FORMA QUE SE BUSCA: un metodo estatico sin parametros que devuelve un tipo del propio
		// dominio. Eso es "un miembro del catalogo". LosQueHay queda afuera porque devuelve un arreglo
		// de cadenas, que es una lista de nombres y no un miembro.
		private static List<MethodInfo> TheFactoriesOf(List<Type> family)
		{
			var found = new List<MethodInfo>();

			foreach (Type each in family)
				foreach (MethodInfo method in each.GetMethods(
					BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic
					| BindingFlags.DeclaredOnly))
				{
					if (method.GetParameters().Length != 0) continue;
					if (!method.ReturnType.IsClass || method.ReturnType == typeof(string)) continue;
					if (!method.ReturnType.IsAssignableFrom(each)
						&& !each.IsAssignableFrom(method.ReturnType)) continue;

					found.Add(method);
				}

			return found;
		}

		private static List<Type> TheDomainsTypes()
		{
			var found = new List<Type>();

			foreach (Type each in typeof(Rubro).Assembly.GetTypes())
				if (each.Namespace == typeof(Rubro).Namespace) found.Add(each);

			return found;
		}

		// Los dos controles viven aca y no en el dominio: el detector se les aplica por la lista de
		// tipos que recibe, asi que prueban el instrumento sin agregarle un miembro muerto al catalogo.
		private sealed class ACatalogueThatRebuilds
		{
			internal static ACatalogueThatRebuilds Uno() { return new ACatalogueThatRebuilds(); }
		}

		private sealed class ACatalogueThatShares
		{
			private static readonly ACatalogueThatShares uno = new ACatalogueThatShares();

			internal static ACatalogueThatShares Uno() { return uno; }
		}
	}
}
