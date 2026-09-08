using System;
using System.Collections.Generic;
using System.Reflection;
using Concesionario.Wasm;
using NUnit.Framework;

namespace Concesionario.Dominio.Pruebas
{
	// EL BROWSER ES LA PRIMERA CARATULA DEL MODULO QUE LEE EL RELOJ DE LAS DOS FORMAS VIVAS, y las dos
	// tienen obligaciones OPUESTAS. Hasta T10 tenia una sola, y la regla que se venia repitiendo -- "el
	// reloj se lee en un solo sitio con nombre" -- era un accidente de que hubiera un solo llamador.
	//
	// LA REGLA QUE SI SE SOSTIENE es que un instante toma su obligacion DE AQUELLO DE LO QUE ES
	// ARGUMENTO:
	//
	//   argumento de un COMANDO -- MONOTONO dentro de la sesion. Un instante hacia atras falla ADENTRO
	//   del cuerpo, cuando la entrada ya esta escrita: queda una cicatriz permanente que loguea un
	//   fallo de rehidratacion en cada arranque futuro;
	//
	//   argumento de una LECTURA -- deliberadamente NO monotono. Una pregunta no escribe ninguna
	//   entrada, no hay nada que cicatrizar, y una guarda de monotonia solo rechazaria una segunda
	//   pregunta legitima un segundo despues;
	//
	//   y dentro de algo que GUIONA una secuencia recibida -- ningun reloj. Eso es la consola y el
	//   inyector, y no es esta caratula.
	//
	// LA TRAMPA ES QUE COPIAR LA EQUIVOCADA ES BARATO Y CARO DE NOTAR, porque una guarda de monotonia
	// sobre una consulta PARECE cuidadosa. Por eso las dos viven pegadas en el mismo archivo y con el
	// nombre diciendo cual es cual, y por eso esta prueba las cuenta.
	//
	// LO QUE SOSTIENE LA DIFERENCIA NO ES UN COMENTARIO SINO LA FORMA: la del comando es de INSTANCIA,
	// porque la monotonia necesita el estado de la sesion; la de la lectura es ESTATICA, y ademas
	// AppState no tiene NINGUN campo estatico de instante, asi que no hay donde guardar un punto de
	// control. Volverla monotona cuesta un campo nuevo, y ese campo se pone rojo aca.
	//
	// LO QUE ESTA PRUEBA NO PUEDE VER, dicho: que la lectura compuesta use de verdad la segunda y no la
	// primera. Eso necesitaria un AppState vivo, y un AppState vivo necesita el puente de archivos del
	// browser, que no existe fuera de el.
	[TestFixture]
	public sealed class TheScreenAsksAtAReadInstantAndStampsCommandsWithAnother
	{
		private const BindingFlags EveryMemberOfIt =
			BindingFlags.Instance | BindingFlags.Static
			| BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

		// LOS TRES MIEMBROS QUE DEVUELVEN UN INSTANTE SIN QUE NADIE SE LO DE, afirmados como CONJUNTO
		// EXACTO. Son la truncadura compartida y las dos formas que la usan; un cuarto se pone rojo, y
		// al escribirlo alguien tiene que decidir de que es argumento.
		[Test]
		public void TheShellHasExactlyTheseThreeMembersThatReadAClock()
		{
			var found = new List<string>();

			foreach (MethodInfo each in typeof(AppState).GetMethods(EveryMemberOfIt))
				if (each.ReturnType == typeof(DateTime) && each.GetParameters().Length == 0)
					found.Add(each.Name);

			Assert.That(found, Is.EquivalentTo(new[]
			{
				"TheWholeSecondItIsNow",
				"AnInstantThatDoesNotGoBackwards",
				"TheInstantTheScreenAsksAt",
			}),
				"La caratula gano o perdio un miembro que entrega un instante. El de arriba trunca al "
				+ "segundo entero y lo comparten los otros dos; el segundo es el que va a un COMANDO y "
				+ "es monotono; el tercero es el que va a una LECTURA y no lo es. Uno nuevo se escribe "
				+ "aca a proposito, diciendo de cual de las dos formas es.");
		}

		// LA FORMA ES LO QUE SOSTIENE LA DIFERENCIA, y no el comentario que la explica.
		[Test]
		public void TheCommandStampIsAnInstanceMemberAndTheReadInstantIsStatic()
		{
			Assert.That(TheMember("AnInstantThatDoesNotGoBackwards").IsStatic, Is.False,
				"El sello de comando dejo de ser de instancia. La monotonia necesita el estado de la "
				+ "sesion, asi que un miembro estatico no la puede sostener y la guarda se volveria "
				+ "una decoracion.");

			Assert.That(TheMember("TheInstantTheScreenAsksAt").IsStatic, Is.True,
				"El instante de la lectura dejo de ser estatico. Estatico es lo que hace que NO PUEDA "
				+ "volverse monotono por descuido: sin estado de sesion no hay punto de control que "
				+ "consultar.");
		}

		// Y NO HAY DONDE GUARDAR UNO. Es el renglon que cierra el hueco del anterior: un miembro
		// estatico si podria consultar un CAMPO estatico, y no hay ninguno que lleve un instante.
		[Test]
		public void NoStaticFieldOfTheShellCouldHoldACheckpointForTheReadInstant()
		{
			var found = new List<string>();

			foreach (FieldInfo each in typeof(AppState).GetFields(EveryMemberOfIt))
				if (each.IsStatic && each.FieldType == typeof(DateTime)) found.Add(each.Name);

			Assert.That(found, Is.Empty,
				"Aparecio un campo estatico de instante en la caratula. Es lo unico que le permitiria "
				+ "al instante de la LECTURA volverse monotono, que es exactamente lo que no debe ser: "
				+ "rechazaria una segunda pregunta legitima un segundo despues, sin nada que ganar, "
				+ "porque una consulta no escribe ninguna entrada.");

			// Y EL CONTROL: el campo del sello de COMANDO si existe, y es de instancia. Sin este
			// renglon, lo de arriba estaria vacio aunque la caratula no llevara ningun instante.
			var instance = new List<string>();

			foreach (FieldInfo each in typeof(AppState).GetFields(EveryMemberOfIt))
				if (!each.IsStatic && each.FieldType == typeof(DateTime)) instance.Add(each.Name);

			Assert.That(instance, Is.EquivalentTo(new[] { "lastStampSent" }));
		}

		// EL INSTANTE DE LA LECTURA SALE EN UTC Y TRUNCADO AL SEGUNDO ENTERO, que es lo que el journal
		// guarda: comparar mas fino que eso es comparar contra una precision que no sobrevive a un
		// reinicio.
		[Test]
		public void TheReadInstantIsUtcAndTruncatedToTheWholeSecond()
		{
			MethodInfo read = TheMember("TheInstantTheScreenAsksAt");

			var first = (DateTime)read.Invoke(null, new object[0]);
			var second = (DateTime)read.Invoke(null, new object[0]);

			foreach (DateTime each in new[] { first, second })
			{
				Assert.That(each.Kind, Is.EqualTo(DateTimeKind.Utc));
				Assert.That(each.Millisecond, Is.EqualTo(0));
				Assert.That(each.Ticks % TimeSpan.TicksPerSecond, Is.EqualTo(0),
					"Quedaron ticks por debajo del segundo. El journal trunca ahi, asi que un instante "
					+ "mas fino se compara distinto antes y despues de reabrir.");
			}

			Assert.That(second, Is.GreaterThanOrEqualTo(first),
				"Dos lecturas seguidas del reloj de la maquina. Que la segunda no sea anterior no es "
				+ "una guarda -- no hay ninguna -- es solo que el reloj no se movio para atras en el "
				+ "milisegundo que separa estas dos llamadas.");
		}

		// Y LA LECTURA COMPUESTA NO RECIBE NINGUN INSTANTE. Es la version de esta caratula de la
		// decision que T8 tomo en la suya: quien pregunta no elige el dia. La pantalla contesta sobre
		// HOY, y un parametro aca seria dejar que alguien pregunte por un dia que se invento.
		[Test]
		public void TheScreensCompositeReadTakesNoInstantAndNoOtherArgument()
		{
			MethodInfo read = typeof(AppState).GetMethod("ReadWhereThisVehicleStands");

			Assert.That(read, Is.Not.Null,
				"La caratula dejo de ofrecer la mitad del vehiculo y la seccion Garantia no tiene de "
				+ "donde sacarla.");

			Assert.That(read.GetParameters(), Is.Empty,
				"La lectura de la pantalla gano un argumento. Los tres hechos con que se mide salen "
				+ "del registro y el instante sale del reloj de la maquina: no queda ninguno que "
				+ "alguien tenga que traer.");

			Assert.That(read.ReturnType, Is.EqualTo(typeof(WhereThisVehicleStands)));
		}

		private static MethodInfo TheMember(string name)
		{
			var found = new List<MethodInfo>();

			foreach (MethodInfo each in typeof(AppState).GetMethods(EveryMemberOfIt))
				if (each.Name == name) found.Add(each);

			Assert.That(found, Has.Count.EqualTo(1),
				$"La caratula tiene {found.Count} miembros llamados '{name}'. Con dos, afirmar sobre "
				+ "'el' miembro es elegir uno y no mirar el otro.");

			return found[0];
		}
	}
}
