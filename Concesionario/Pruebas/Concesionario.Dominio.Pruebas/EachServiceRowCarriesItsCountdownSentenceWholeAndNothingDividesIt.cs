using System.Collections.Generic;
using System.IO;
using NUnit.Framework;

namespace Concesionario.Dominio.Pruebas
{
	// LA CONDICION DE ADMISION DE LA PARTE A, Y ES LA UNICA PRUEBA DE MS-7 QUE ES UNA CONDICION Y NO UNA
	// AFIRMACION MAS. El censo de numeros de la puerta se amplio en MS-4 sobre la fuerza de que este
	// barrido existiera: "ninguna caratula divide un resto por el otro". Si esto falla, el argumento con
	// que la parte A se admitio fallo, y la parte A no debia haber salido.
	//
	// ESTA PANTALLA ES LA PRIMERA SUPERFICIE DEL MODULO QUE TIENE LOS DOS RESTOS JUNTOS. La consola los
	// imprime en un tablero de medicion y la caratula MCP los manda en una fila de JSON; una FILA DE
	// PANTALLA los pone lado a lado en la misma tarjeta, que es donde "unos 34 km por dia" esta a una
	// division de distancia.
	//
	// Y EL BARRIDO NO ATRAPA LA FORMA MAS FILOSA DE ESTA REBANADA, medido dos veces antes de esta tarea
	// -- en la caratula MCP y en un .razor. Una frase rival armada por CONCATENACION sobre los dos
	// numeros del dominio deja el barrido VERDE, porque un '+' con un operando de texto es concatenacion
	// y nunca es aritmetica. Lo que la atrapa es la afirmacion LETRA POR LETRA de que el markup pinta la
	// frase del dominio entera, y esta abajo. El barrido defiende los NUMEROS; la afirmacion letra por
	// letra defiende la FRASE.
	[TestFixture]
	public sealed class EachServiceRowCarriesItsCountdownSentenceWholeAndNothingDividesIt
	{
		private const string TheRow = "Servicios.razor";

		// LOS CINCO ARCHIVOS QUE MS-7 ESCRIBIO O TOCO, con el camino con que llegan a la raiz barrida.
		private static readonly string[] EveryFileThisTaskWrote =
		{
			"Sections/Servicios.razor",
			"Sections/Historial.razor",
			"Sections/Vehiculo.razor",
			"WhereEachOperationStands.cs",
			"WhatTheServicesCost.cs",
		};

		// ===== NINGUNA DE LAS LINEAS NUEVAS LLEVA UN OPERADOR =============================

		// LA LEY, ACOTADA A LO QUE ESTA TAREA ESCRIBIO. El barrido del modulo afirma el conjunto entero
		// contra la lista de permitidos; esto dice de quien es la culpa cuando ese conjunto crece, y se
		// pone rojo aunque alguien meta la linea nueva en la lista de permitidos.
		[Test]
		public void NoLineOfTheCountdownOrOfThePricesHoldsAnArithmeticOperator()
		{
			List<ArithmeticHit> found =
				TheCodeOfTheShellsAsData.TheArithmeticIn(TheCodeOfTheShellsAsData.Wasm);

			foreach (string file in EveryFileThisTaskWrote)
			{
				var mine = new List<ArithmeticHit>();

				foreach (ArithmeticHit hit in found)
					if (hit.File == file) mine.Add(hit);

				Assert.That(mine, Is.Empty,
					"'" + file + "' tiene una linea con un operador aritmetico: " + Printed(mine)
					+ "Todo agregado en pantalla lo compone el dominio. Los dos restos estan en la misma "
					+ "tarjeta, y dividir uno por el otro compone una tasa de uso que el dominio nunca "
					+ "dijo -- esa respuesta es de otra rebanada, que la compone adentro sobre la serie "
					+ "real con sus unidades revisadas.");
			}
		}

		// Y EL BARRIDO SI VE LA DIVISION QUE ESTA PRUEBA EXISTE PARA PROHIBIR. Sin este renglon, lo de
		// arriba estaria vacio por estar mirando mal: una lista vacia y un detector que no detecta nada
		// son el mismo verde.
		[Test]
		public void AndTheSweepDoesSeeThatDivisionWhenItIsWrittenInARazor()
		{
			foreach (string dividing in new[]
			{
				"<span>@(parada.DistanceRemaining / parada.DaysRemaining)</span>",
				"<span>@(parada.DistanceRemaining/parada.DaysRemaining)</span>",
				"<div>@(parada.DistanceRemaining - parada.DaysRemaining)</div>",
			})
				Assert.That(
					TheCodeOfTheShellsAsData.TheArithmeticInSource("probe", TheRow, dividing),
					Is.Not.Empty,
					"El detector no vio '" + dividing + "'.");

			// EL LIMITE DEL DETECTOR EN UN .razor, MEDIDO Y NO PROMETIDO: un guion PEGADO no se ve,
			// porque en markup un guion es parte de un nombre. Quien escriba el control negativo tiene
			// que ponerle espacio a un lado o el control pasa sin probar nada.
			Assert.That(
				TheCodeOfTheShellsAsData.TheArithmeticInSource(
					"probe", TheRow, "<div>@(parada.DistanceRemaining-parada.DaysRemaining)</div>"),
				Is.Empty,
				"El guion pegado en un .razor dejo de ser un punto ciego. Es una buena noticia, y hay "
				+ "que actualizar lo que las tareas siguientes leen sobre la forma del control negativo.");
		}

		// ===== LA FRASE, LETRA POR LETRA, QUE ES LO QUE EL BARRIDO NO VE ===================

		// LA FILA PINTA LA FRASE DEL DOMINIO ENTERA Y NO ARMA NINGUNA PROPIA. Es la mitad que el barrido
		// no puede defender, y la unica forma de defenderla es exigir que la frase aparezca pintada y
		// aparecer SOLA -- no adentro de una concatenacion.
		[Test]
		public void TheRowPaintsTheCountdownSentenceWholeAndComposesNoRivalOfItsOwn()
		{
			string markup = TheSectionAsItIsWritten(TheRow);

			Assert.That(markup, Does.Contain("@parada.Says"),
				"La fila dejo de pintar la cuenta atras del dominio. La frase la compone el dominio: una "
				+ "que la caratula arme con los dos numeros sale con la procedencia del registro encima "
				+ "de algo que el registro no dijo.");

			foreach (string forbidden in new[]
			{
				"Says +", "+ Says", "parada.Says +", "+ parada.Says",
				"DistanceRemaining +", "+ DistanceRemaining",
				"DaysRemaining +", "+ DaysRemaining",
			})
				Assert.That(markup, Does.Not.Contain(forbidden),
					"La fila concatena algo ('" + forbidden + "'). Lo que sale de la puerta se cita letra "
					+ "por letra o no se cita, y una frase armada por concatenacion sobre los dos numeros "
					+ "deja el barrido de aritmetica VERDE: esta afirmacion es la unica que la ve.");

			// Y NO HAY NINGUNA BARRA, NINGUN PORCENTAJE Y NINGUNA PROYECCION. La maqueta tenia una barra
			// de progreso y un grafico de barras, y las dos son una cuenta con un valor del dominio.
			foreach (string forbidden in new[]
			{
				"Porcentaje", "d-bar", "100.0", "percent", "%>",
			})
				Assert.That(markup, Does.Not.Contain(forbidden),
					"La fila gano una barra o un porcentaje ('" + forbidden + "'). Las filas y las frases "
					+ "son la pantalla.");
		}

		// LOS DOS MONTOS SE PINTAN CADA UNO ADENTRO DE SU PROPIA GUARDA, y son DOS guardas y no una: la
		// mitad de tiempo se mide sin ninguna lectura del odometro, asi que hay registros en que una se
		// puede decir y la otra no. Una sola guarda para las dos esconderia la que si se puede decir.
		[Test]
		public void EachAmountIsPaintedInsideItsOwnGuardAndTheyAreTwoGuardsAndNotOne()
		{
			string markup = TheSectionAsItIsWritten(TheRow);

			Assert.That(markup, Does.Contain("@if (parada.DistanceCanBeSaid)"),
				"El monto de distancia se pinta sin preguntar si se puede decir. Cuando no se puede, "
				+ "llega en cero -- y un cero en pantalla se lee como que hubo medicion.");

			Assert.That(markup, Does.Contain("@if (parada.DaysCanBeSaid)"),
				"El plazo se pinta sin su propia guarda.");

			Assert.That(markup, Does.Contain("parada.DistanceUnit"),
				"El monto de distancia se pinta sin su unidad. La distancia tiene un conjunto cerrado de "
				+ "DOS unidades, asi que un monto sin la suya es ambiguo.");

			// Y EL RECHAZO SE PREGUNTA ANTES QUE LA FRASE. Cuando la agenda rechaza, las nueve
			// respuestas llegan vacias: pintar la frase primero pondria una tarjeta en blanco donde hay
			// algo que decir.
			Assert.That(markup, Does.Contain("parada.CountdownRefused"),
				"La fila pinta la cuenta atras sin preguntar si la agenda rechazo la pregunta. Un plan "
				+ "anclado adelante de hoy deja las nueve respuestas vacias, y una tarjeta en blanco no "
				+ "dice nada.");
		}

		// NINGUNA FILA SE BUSCA CON UNA CLASE QUE LA CARATULA NOMBRO ELLA MISMA, y esta prueba se
		// escribio DESPUES de que la falsificacion encontrara que nada la veia. La lista de servicios
		// recorre las nueve y REORDENA por urgencia, asi que pinta la fila que la busqueda le da:
		// buscarla con un nombre escrito a mano pone la cuenta atras de una operacion bajo el nombre de
		// otra, en las nueve filas. MEDIDO: con Operations.Of("Repair") en el lugar de
		// Operations.Of(kind), la suite entera se quedo VERDE -- todas las afirmaciones de la cuenta
		// atras son sobre el PORTADOR, que la prueba arma ella misma, y las del markup miran que la
		// frase se pinte y no de cual fila sale.
		[Test]
		public void NoRowOfTheScreenIsLookedUpWithAKindTheShellNamedItself()
		{
			string markup = TheSectionAsItIsWritten(TheRow);

			Assert.That(markup, Does.Contain("Operations.Of(kind)"),
				"La fila ya no se busca con la clase que el recorrido esta pintando.");

			Assert.That(markup, Does.Not.Contain("Operations.Of(\""),
				"La fila se busca con una clase escrita a mano. Las nueve filas mostrarian la cuenta "
				+ "atras de esa, bajo nueve nombres distintos, y el registro no tiene nada de malo.");

			// Y NINGUNA DE LAS NUEVE APARECE COMO LITERAL EN LA SECCION. Es la mitad que ve la misma
			// falta escrita de otra forma -- una variable intermedia con el nombre adentro.
			foreach (string kind in Concesionario.Wasm.AppState.TheNineKinds)
				Assert.That(markup, Does.Not.Contain("\"" + kind + "\""),
					"La seccion nombra la clase '" + kind + "' como literal. Las nueve las nombra el "
					+ "dominio y la lista las recibe; escribir una aqui es que la caratula elija.");
		}

		// Y EL BARRIDO DE MARKUP DEL MODULO ESTA LEYENDO LOS TRES ARCHIVOS QUE ESTA TAREA TOCO. Ninguno
		// es nuevo -- los tres entraron por glob en tareas anteriores -- pero un barrido que no los lee
		// esta verde por no haber mirado, y eso no se hereda: se afirma.
		[Test]
		public void TheModuleWideMarkupSweepIsReadingTheThreeFilesThisTaskTouched()
		{
			var swept = new List<string>();

			foreach (string file in Directory.GetFiles(
				Path.Combine(System.AppContext.BaseDirectory, "MarcadoComoDato"), "*.razor",
				SearchOption.AllDirectories))
				swept.Add(Path.GetFileName(file));

			foreach (string mine in new[] { TheRow, "Historial.razor", "Vehiculo.razor" })
				Assert.That(swept, Contains.Item(mine),
					"'" + mine + "' no esta entre el markup que el barrido del modulo lee, asi que lo que "
					+ "MS-7 le escribio no lo mira nadie y NADA se pone rojo.");

			// EL PISO BAJO DE 16 A 15 PORQUE SE BORRO UNA SECCION, no porque la copia se rompiera. La
			// pestana "Mi registro" -- MiRegistro.razor -- mostraba entradas del diario, ultimo evento
			// aplicado y de donde se trae el feed, que es plumbing y no algo que la duena lea; se saco
			// del modulo y su boton de traer novedades se movio a Inicio. Un piso que no se baja al
			// borrar deja este barrido rojo diciendo que fallo la copia, que es lo contrario de lo que
			// paso.
			Assert.That(swept, Has.Count.GreaterThanOrEqualTo(15),
				"El barrido trajo menos markup del que hay. Lo que fallo es la copia.");
		}

		private static string Printed(List<ArithmeticHit> hits)
		{
			var said = new System.Text.StringBuilder("\n");

			foreach (ArithmeticHit hit in hits) said.Append("  ").Append(hit).Append('\n');

			return said.ToString();
		}

		private static string TheSectionAsItIsWritten(string name)
		{
			string folder = Path.Combine(System.AppContext.BaseDirectory, "SeccionesComoDato");

			Assert.That(Directory.Exists(folder), Is.True, "Las secciones no llegaron al output.");

			foreach (string file in Directory.GetFiles(folder, "*.razor"))
				if (Path.GetFileName(file) == name) return File.ReadAllText(file);

			Assert.Fail("'" + name + "' no llego al output.");
			return "";
		}
	}
}
