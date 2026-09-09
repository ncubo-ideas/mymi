using System;
using System.Collections.Generic;
using System.Reflection;
using Municipalidad.Wasm;
using NUnit.Framework;

namespace Municipalidad.Dominio.Pruebas
{
	// EL NAVEGADOR ES LA PRIMERA CARATULA DE ESTE MODULO QUE LEE EL RELOJ DE LAS DOS FORMAS VIVAS, y
	// las dos tienen obligaciones OPUESTAS. Hasta W-2 leia una sola -- DEC-63 -- y la regla que se
	// venia repitiendo, "el reloj se lee en un solo sitio con nombre", era un accidente de que hubiera
	// un solo llamador.
	//
	// LA REGLA QUE SI SE SOSTIENE es que un instante toma su obligacion DE AQUELLO DE LO QUE ES
	// ARGUMENTO:
	//
	//   argumento de un COMANDO -- MONOTONO dentro de la sesion, porque un instante hacia atras falla
	//   ADENTRO del cuerpo cuando la entrada ya esta escrita;
	//
	//   argumento de una LECTURA -- deliberadamente NO monotono, porque una pregunta no escribe
	//   ninguna entrada y una guarda solo rechazaria una segunda pregunta legitima un segundo despues;
	//
	//   y dentro de algo que GUIONA una secuencia recibida -- ningun reloj. Eso es la consola y el
	//   inyector, y no es esta caratula.
	//
	// LO QUE SOSTIENE LA DIFERENCIA NO ES UN COMENTARIO SINO LA FORMA: la del comando es DE INSTANCIA,
	// porque la monotonia necesita el estado de la sesion; la de la lectura es ESTATICA, y ademas
	// EstadoDeLaApp no tiene NINGUN campo estatico de instante, asi que no hay donde guardar un punto
	// de control. Volverla monotona cuesta un campo nuevo, y ese campo se pone rojo aca.
	//
	// LO QUE ESTA PRUEBA NO PUEDE VER, dicho: que cada lectura compuesta use de verdad la de lectura y
	// no la de comando. Eso necesitaria un EstadoDeLaApp vivo, y uno vivo necesita el puente de
	// archivos del navegador, que no existe fuera de el.
	[TestFixture]
	public sealed class TheScreenAsksAtOneInstantAndStampsCommandsWithAnother
	{
		private const BindingFlags CadaMiembroSuyo =
			BindingFlags.Instance | BindingFlags.Static
			| BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

		// LOS TRES MIEMBROS QUE ENTREGAN UN INSTANTE SIN QUE NADIE SE LO DE, afirmados como CONJUNTO
		// EXACTO. Son la truncadura compartida y las dos formas que la usan; un cuarto se pone rojo, y
		// al escribirlo alguien tiene que decidir de que es argumento.
		[Test]
		public void TheShellHasExactlyTheseThreeMembersThatReadAClock()
		{
			List<string> hallados = new List<string>();

			foreach (MethodInfo cada in typeof(EstadoDeLaApp).GetMethods(CadaMiembroSuyo))
				if (cada.ReturnType == typeof(DateTime) && cada.GetParameters().Length == 0)
					hallados.Add(cada.Name);

			Assert.That(hallados, Is.EquivalentTo(new[]
			{
				"ElSegundoEnteroDeAhora",
				"UnSelloQueNoVaParaAtras",
				"ElInstanteEnQueLaPantallaPregunta",
			}),
				"La caratula gano o perdio un miembro que entrega un instante. El primero trunca al "
				+ "segundo entero y lo comparten los otros dos; el segundo es el que va a un COMANDO y "
				+ "es monotono; el tercero es el que va a una LECTURA y no lo es. Uno nuevo se escribe "
				+ "aca a proposito, diciendo de cual de las dos formas es.");
		}

		// LA FORMA ES LO QUE SOSTIENE LA DIFERENCIA, y no el comentario que la explica.
		[Test]
		public void TheCommandStampIsAnInstanceMemberAndTheReadInstantIsStatic()
		{
			Assert.Multiple(() =>
			{
				Assert.That(ElMiembro("UnSelloQueNoVaParaAtras").IsStatic, Is.False,
					"El sello de comando dejo de ser de instancia. La monotonia necesita el estado de "
					+ "la sesion, asi que un miembro estatico no la puede sostener y la guarda se "
					+ "volveria una decoracion.");

				Assert.That(ElMiembro("ElInstanteEnQueLaPantallaPregunta").IsStatic, Is.True,
					"El instante de la lectura dejo de ser estatico. Estatico es lo que hace que NO "
					+ "PUEDA volverse monotono por descuido: sin estado de sesion no hay punto de "
					+ "control que consultar.");
			});
		}

		// Y NO HAY DONDE GUARDAR UN PUNTO DE CONTROL PARA LA LECTURA. Cierra el hueco del anterior: un
		// miembro estatico si podria consultar un CAMPO estatico, y el unico campo de instante que
		// existe es de instancia y es el del sello.
		[Test]
		public void NoStaticFieldOfTheShellCouldHoldACheckpointForTheReadInstant()
		{
			List<string> estaticos = new List<string>();
			List<string> deInstancia = new List<string>();

			foreach (FieldInfo cada in typeof(EstadoDeLaApp).GetFields(CadaMiembroSuyo))
				if (cada.FieldType == typeof(DateTime))
					(cada.IsStatic ? estaticos : deInstancia).Add(cada.Name);

			Assert.Multiple(() =>
			{
				Assert.That(estaticos, Is.Empty,
					"Aparecio un campo estatico de instante en la caratula. Es el unico sitio donde el "
					+ "instante de una LECTURA podria guardar un punto de control, y con el la forma "
					+ "estatica dejaria de garantizar nada. Estaticos: " + string.Join(", ", estaticos));

				Assert.That(deInstancia, Is.EqualTo(new[] { "ultimoSelloMandado" }),
					"El unico campo de instante de la caratula es el punto de control del SELLO, y es "
					+ "de instancia. Si desaparece, la monotonia del comando no la sostiene nada.");
			});
		}

		private static MethodInfo ElMiembro(string nombre)
		{
			MethodInfo hallado = typeof(EstadoDeLaApp).GetMethod(nombre, CadaMiembroSuyo);

			Assert.That(hallado, Is.Not.Null,
				"No existe el miembro '" + nombre + "' en la caratula, asi que esta prueba no mide "
				+ "nada. Si se renombro, se renombra aca tambien y a proposito.");

			return hallado;
		}
	}
}
