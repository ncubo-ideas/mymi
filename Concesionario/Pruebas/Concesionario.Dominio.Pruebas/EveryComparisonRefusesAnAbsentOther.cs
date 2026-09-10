using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using NUnit.Framework;

namespace Concesionario.Dominio.Pruebas
{
	// E46 (propuesta rio arriba el 2026-09-10, PENDIENTE DE REVISION DEL EQUIPO). Un tipo que compara
	// contra otro de su propia familia lleva su guarda, y la lleva en TODAS sus comparaciones: guardar
	// una y dejar la hermana abierta es el defecto, no la mitad hecha.
	//
	// POR QUE ESTO NO SALE EN UNA PRUEBA DE COMPORTAMIENTO. El unico camino vivo hasta cada una de
	// estas comparaciones ya viene con el otro en la mano -- AdoptPlanRevision mira Count > 0 antes de
	// comparar, y los dias de una estadia salen de una lista que el propio agregado armo. Es
	// resistencia al mal uso y no una conducta que el dominio prometa, asi que se ve LEYENDO las
	// comparaciones una al lado de la otra, que es justo lo que un barrido puede hacer y una prueba
	// de caso no.
	//
	// LO QUE EL INSTRUMENTO EXIGE, Y ES MENOS DE LO QUE PARECE. No exige que toda comparacion reviente:
	// exige que ninguna se CAIGA. Contra un otro ausente valen dos respuestas -- la negativa del
	// dominio (DomainException, que es lo que hacen casi todas aca) o un false, que E46 sanciona para
	// una igualdad y es lo que contestan las dos monedas a proposito. Lo que no vale es un true, que
	// seria mentira, ni una excepcion del framework: una que no sea DomainException se escapa de Apply
	// y para el bloque entero del refresco (C22), asi que aca un NullReferenceException no es un
	// detalle de robustez sino la peor de las tres salidas.
	//
	// EL BARRIDO CORRE SOBRE EL ENSAMBLADO ENTERO y no sobre una lista de tipos, por la razon de
	// TheDomainWritesNoEquality: una comparacion nueva entra sola el dia que alguien la escriba.
	[TestFixture]
	public sealed class EveryComparisonRefusesAnAbsentOther
	{
		[Test]
		public void NoComparisonInTheDomainFallsOverAnAbsentOther()
		{
			List<string> guilty = TheOnesThatDoNotRefuseAnAbsentOther(TheDomainsTypes());

			Assert.That(guilty, Is.Empty,
				"Comparaciones que no contestan a un otro ausente: " + string.Join(", ", guilty)
				+ ". La ausencia del otro es un defecto de quien llama y se dice con la negativa del "
				+ "dominio; un true seria mentira y una excepcion del framework se escapa del bloque "
				+ "de refresco entero.");
		}

		// C57: un barrido vale lo que vale lo que enumera. Si el filtro deja de reconocer la forma
		// -- alguien renombra un parametro, o mete la comparacion en un tipo que este pase no visita
		// -- la prueba de arriba se pone verde por vacia, que es como se pierde un instrumento sin
		// que nadie lo borre. Esta lo impide, y nombra lo que encontro en vez de contarlo.
		[Test]
		public void AndTheCensusFindsTheComparisonsTheDomainActuallyHas()
		{
			List<MethodInfo> found = TheComparisonsOf(TheDomainsTypes());

			var named = new List<string>();

			foreach (MethodInfo each in found) named.Add(Named(each));

			Assert.That(found, Is.Not.Empty,
				"El censo no encontro ninguna comparacion, y este dominio tiene varias. El filtro "
				+ "dejo de reconocer la forma, asi que la prueba hermana esta verde por vacia.");

			Assert.That(named, Has.Some.Contains("Money.IsInTheSameCurrencyAs"),
				"El censo no vio la comparacion de dos montos, que es la que el dominio nombra en su "
				+ "propia doctrina: " + string.Join(", ", named));

			Assert.That(named, Has.Some.Contains("IsTheSameDayAs"),
				"El censo no vio la comparacion de dos dias de estadia: " + string.Join(", ", named));
		}

		// C61: la prueba que defiende un INSTRUMENTO necesita el mismo trato que el codigo. Los dos
		// controles corren el MISMO detector sobre un par local -- uno que se cae y uno que niega --
		// asi que un detector que dejara de morder, o que mordiera a cualquiera, se pone rojo aca y
		// no espera a que el dominio lo estrene.
		[Test]
		public void ControlTheInstrumentCatchesAComparisonWithNoGuard()
		{
			List<string> guilty = TheOnesThatDoNotRefuseAnAbsentOther(
				new List<Type> { typeof(AComparisonWithNoGuard) });

			Assert.That(guilty, Is.Not.Empty,
				"El detector dejo pasar una comparacion sin guarda, asi que el verde de la prueba de "
				+ "arriba no dice nada.");
		}

		[Test]
		public void ControlTheInstrumentPassesAComparisonThatRefuses()
		{
			List<string> guilty = TheOnesThatDoNotRefuseAnAbsentOther(
				new List<Type> { typeof(AComparisonThatRefuses) });

			Assert.That(guilty, Is.Empty,
				"El detector mordio una comparacion que si niega: " + string.Join(", ", guilty));
		}

		// EL OTRO SE PASA EN null Y LA INSTANCIA SE ARMA SIN CONSTRUCTOR. GetUninitializedObject deja
		// los campos en cero, que es exactamente lo que hace falta: la guarda es la PRIMERA sentencia
		// de una comparacion bien escrita, asi que niega antes de mirar ningun campo. Una que no la
		// tenga toca un campo vacio y se cae, que es el hallazgo y no un artefacto del rig.
		private static List<string> TheOnesThatDoNotRefuseAnAbsentOther(List<Type> family)
		{
			var guilty = new List<string>();

			foreach (MethodInfo each in TheComparisonsOf(family))
			{
				object one = RuntimeHelpers.GetUninitializedObject(each.ReflectedType);

				try
				{
					object answered = each.Invoke(one, new object[] { null });

					if (answered is bool && (bool)answered)
						guilty.Add(Named(each) + " contesto que si");
				}
				catch (TargetInvocationException thrown)
				{
					if (!(thrown.InnerException is DomainException))
						guilty.Add(Named(each) + " se cayo con "
							+ thrown.InnerException.GetType().Name);
				}
			}

			return guilty;
		}

		// LA FORMA QUE SE BUSCA: un miembro de instancia que toma UN parametro cuyo tipo admite al
		// propio tipo que lo declara. Eso es "comparar contra otro de mi familia" y deja afuera un
		// IsExpressedIn(DistanceUnit) o un Covers(StayDay), que toman otra cosa. El Equals(object) de
		// Object encaja en la forma y se va por el filtro de la familia, que es el mismo que le pone
		// el limite al barrido.
		private static List<MethodInfo> TheComparisonsOf(List<Type> family)
		{
			var found = new List<MethodInfo>();

			foreach (Type each in family)
			{
				if (each.IsAbstract || each.IsInterface || each.IsEnum || each.IsValueType) continue;

				foreach (MethodInfo method in each.GetMethods(
					BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
				{
					if (!family.Contains(method.DeclaringType)) continue;

					ParameterInfo[] parameters = method.GetParameters();

					if (parameters.Length != 1) continue;

					Type taken = parameters[0].ParameterType;

					if (taken.IsValueType || !taken.IsAssignableFrom(each)) continue;

					found.Add(method);
				}
			}

			return found;
		}

		// EL TIPO QUE SE NOMBRA ES EL QUE SE PREGUNTO, no el que declara el miembro: una variante que
		// hereda la comparacion de su base se nombra a si misma, porque es la que un llamador tiene
		// en la mano cuando la llama.
		private static string Named(MethodInfo comparison)
		{
			return comparison.ReflectedType.Name + "." + comparison.Name;
		}

		// EL ENSAMBLADO SE ALCANZA POR UN TIPO QUE SE NOMBRA y no por su nombre en una cadena
		// (DEC-101): sigue siendo reflexion, porque tiene que cubrir las comparaciones que todavia no
		// existen, pero ya no se queda vacio en silencio porque alguien renombro el ensamblado.
		private static List<Type> TheDomainsTypes()
		{
			var found = new List<Type>();

			foreach (Type each in typeof(VehicleProfile).Assembly.GetTypes())
				if (each.Namespace == typeof(VehicleProfile).Namespace) found.Add(each);

			return found;
		}

		// LOS DOS CONTROLES. Viven aca y no en el dominio a proposito: el detector se les aplica por
		// la lista de tipos que recibe, asi que prueban el instrumento sin agregarle un miembro
		// muerto al dominio para poder mirarlo.
		private sealed class AComparisonWithNoGuard
		{
			private readonly string _text = string.Empty;

			internal bool IsTheSameAs(AComparisonWithNoGuard other)
			{
				return _text == other._text;
			}
		}

		private sealed class AComparisonThatRefuses
		{
			private readonly string _text = string.Empty;

			internal bool IsTheSameAs(AComparisonThatRefuses other)
			{
				if (other == null)
					throw new DomainException("No hay con que comparar.");

				return _text == other._text;
			}
		}
	}
}
