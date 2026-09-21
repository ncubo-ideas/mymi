using System;
using System.Collections.Generic;
using System.Reflection;
using Municipalidad.Wasm;
using NUnit.Framework;

namespace Municipalidad.Dominio.Pruebas
{
	// LAS SEIS PREGUNTAS DEL CANON SON LAS DEL SERVIDOR MCP, y hasta hoy la app contestaba CUATRO. Esa
	// diferencia no se veia en ninguna parte: DEC-61 la habia decidido en palabras -- "las cuatro
	// pantallas que Preguntas.md parte 3 permite, y ninguna quinta" -- y nada la sostenia, asi que el
	// compuesto de VS Code llego a llamarse "EL CANON ENTERO" sin que nada se pusiera rojo. DEC-94
	// renombro el compuesto; esta prueba es lo que hace que la cuenta no vuelva a divergir en silencio.
	//
	// LO QUE AFIRMA ES EL CONJUNTO EXACTO DE RUTAS, no un minimo: una septima pantalla se pone roja, y
	// al escribirla alguien tiene que decidir si el brief la permite. Preguntas.md parte 3 es quien lo
	// dice, y su enmienda del 2026-09-10 es la que abrio estas dos.
	//
	// POR QUE EL LADO DE LAS TOOLS VA COMO LITERAL Y NO POR REFLEXION: tomar los nombres de
	// Municipalidad.Mcp exigiria una referencia a esa caratula, que la metaria en el cierre de
	// compilacion de 'dotnet test'. Ahi su dll la tiene tomada cualquier servidor MCP que este
	// corriendo, y la suite entera empezaria a fallar por MSB3027 en maquinas donde el asistente esta
	// conectado. El costo de escribirlos a mano es que un rename de una tool no se ve aca; el costo del
	// otro camino es una suite que no corre.
	//
	// LO QUE ESTA PRUEBA NO PUEDE VER, dicho: que cada pantalla CONTESTE de verdad. Eso necesita
	// renderizarla, y renderizar necesita un EstadoDeLaApp vivo, que necesita el puente de archivos del
	// navegador. DEC-67 ya nombro ese hueco y sigue abierto.
	[TestFixture]
	public sealed class TheAppAnswersTheSixQuestionsOfTheCanonOnItsOwnScreens
	{
		private const string LaRutaEsUnAtributo = "Microsoft.AspNetCore.Components.RouteAttribute";

		// LA PUERTA MAS LAS SEIS PREGUNTAS, afirmadas como CONJUNTO EXACTO. La ruta "/" no es una
		// septima pregunta: es el unico sitio que sabe abrir la cuenta, y eso lo argumenta DEC-61.
		[Test]
		public void TheRoutesAreSevenTheDoorPlusOnePerQuestion()
		{
			List<string> rutas = LasRutasDeLaCaratula();

			Assert.That(rutas, Is.EquivalentTo(new[]
			{
				"/",
				"/por-que-pago-mas",
				"/que-se-me-vence",
				"/cuanto-apartar",
				"/sin-comprobante",
				"/cuanto-he-pagado",
				"/que-tan-al-dia-esta",
			}),
				"La app gano o perdio una pantalla. El conjunto se afirma exacto a proposito: quien "
				+ "agregue una tiene que decidir primero si el brief la permite, porque Preguntas.md "
				+ "parte 3 es quien dice cuales son. Halladas: " + string.Join(", ", rutas));
		}

		// LA CORRESPONDENCIA CON EL CANON, pregunta por pregunta. Es lo que hace falsable la frase "la
		// app contesta las seis": sin ella, contar rutas solo dice cuantas hay y no que contestan.
		[Test]
		public void EachOfTheSixToolsOfTheCanonHasItsOwnScreen()
		{
			var laPantallaDeCadaTool = new Dictionary<string, string>(StringComparer.Ordinal)
			{
				["por_que_pago_mas"] = "/por-que-pago-mas",
				["que_se_me_vence"] = "/que-se-me-vence",
				["cuanto_apartar"] = "/cuanto-apartar",
				["que_quedo_sin_comprobante"] = "/sin-comprobante",
				["cuanto_he_pagado"] = "/cuanto-he-pagado",
				["que_tan_al_dia_esta_esto"] = "/que-tan-al-dia-esta",
			};

			List<string> rutas = LasRutasDeLaCaratula();

			Assert.Multiple(() =>
			{
				foreach (KeyValuePair<string, string> cada in laPantallaDeCadaTool)
					Assert.That(rutas, Does.Contain(cada.Value),
						"La tool '" + cada.Key + "' del canon se quedo sin pantalla: no hay ninguna en "
						+ "'" + cada.Value + "'. Una pregunta que solo contesta el asistente es "
						+ "exactamente lo que DEC-94 encontro y DEC-96 cerro.");

				Assert.That(laPantallaDeCadaTool, Has.Count.EqualTo(6),
					"El canon dejo de tener seis preguntas. Esta tabla es el unico sitio donde el lado "
					+ "de las tools esta escrito, asi que se actualiza a mano y a proposito.");
			});
		}

		// LAS DOS LECTURAS NUEVAS EXISTEN EN LA PUERTA DE LA CARATULA, y ninguna de las dos pantallas
		// puede contestar sin ellas. Una pantalla que compile sin su lectura no existe: no hay de donde
		// sacar los datos.
		[Test]
		public void TheShellExposesAReadForEachOfTheTwoNewScreens()
		{
			Assert.Multiple(() =>
			{
				Assert.That(typeof(EstadoDeLaApp).GetMethod("LeerLoQuePague"), Is.Not.Null,
					"'Cuanto he pagado' se quedo sin su lectura en la caratula.");

				Assert.That(typeof(EstadoDeLaApp).GetMethod("LeerLoQueEntroYLoQueFalta"), Is.Not.Null,
					"'Que tan al dia esta esto' se quedo sin su lectura en la caratula.");
			});
		}

		// Las rutas se leen por el NOMBRE del atributo y no compilando contra el: el tipo vive en el
		// paquete de Blazor, y nombrarlo aca ataria la suite a esa referencia sin necesidad.
		private static List<string> LasRutasDeLaCaratula()
		{
			List<string> halladas = new List<string>();

			foreach (Type cada in typeof(EstadoDeLaApp).Assembly.GetTypes())
				foreach (Attribute atributo in cada.GetCustomAttributes())
				{
					if (atributo.GetType().FullName != LaRutaEsUnAtributo) continue;

					PropertyInfo plantilla = atributo.GetType().GetProperty("Template");

					Assert.That(plantilla, Is.Not.Null,
						"El atributo de ruta de Blazor dejo de tener 'Template', asi que esta prueba no "
						+ "mide nada. Se arregla aca, leyendo lo que traiga ahora.");

					halladas.Add((string)plantilla.GetValue(atributo));
				}

			return halladas;
		}
	}
}
