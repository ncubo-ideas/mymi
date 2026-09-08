using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace Concesionario.Dominio.Pruebas
{
	// LA CONSOLA GUIONA UNA SECUENCIA RECIBIDA, ASI QUE NO LEE NINGUN RELOJ (C27). Es la tercera de las
	// tres formas del instante recibido, y la unica cuya obligacion es la ausencia: la del COMANDO es
	// monotona, la de la LECTURA deliberadamente no lo es, y la de algo que GUIONA no existe. Una
	// corrida que leyera la hora no seria reproducible, y reproducirla es el proposito entero de
	// guionarla -- 'garantia' e 'historia' lo dicen en su propia salida.
	//
	// SE MIDE SOBRE EL TEXTO Y NO POR REFLEXION, por la misma razon que el barrido de aritmetica: lo
	// que hay que ver es una LLAMADA escrita, y un ensamblado no dice de donde salio el DateTime que
	// devuelve un miembro. El fuente de la consola ya viaja al output como dato -- el grupo <None> de
	// CodigoComoDato\Consola en el csproj -- asi que el instrumento es el mismo que ya existe.
	//
	// SE APAGAN COMENTARIOS Y LITERALES ANTES DE BUSCAR. Sin eso, una linea de Console.WriteLine que
	// EXPLIQUE que aca no se lee el reloj seria un acierto, y el barrido se pondria rojo por decir la
	// verdad.
	//
	// EL PISO VA ANTES QUE EL BARRIDO, y es la leccion que SH-0 se gano en rojo: un barrido que leyo
	// cero archivos tiene cero aciertos y una lista vacia lo declara limpio. Aca el piso son los
	// archivos de la raiz y el ARCHIVO CON NOMBRE es el de las estadias, que es lo que esta tarea
	// agrega.
	//
	// LO QUE ESTE BARRIDO NO VE, dicho y no disimulado: un reloj leido a traves de un alias -- un
	// using static, una variable que ya tenga DateTime.UtcNow adentro, un helper de otra capa. La
	// consola no tiene ninguno; queda escrito en vez de tapado con una lista de nombres que tendria
	// varios huecos mas.
	[TestFixture]
	public sealed class TheConsoleStagesFromLiteralsAndReadsNoClock
	{
		// LAS TRES FORMAS DE PREGUNTARLE LA HORA A LA MAQUINA EN .NET. Se buscan las tres: una consola
		// que use Today en vez de UtcNow no es menos irreproducible.
		private static readonly Regex AClockRead = new Regex(
			@"DateTime\s*\.\s*(UtcNow|Now|Today)|DateTimeOffset\s*\.\s*(UtcNow|Now)");

		// EL ARCHIVO QUE ESTA TAREA AGREGA, afirmado PRESENTE. El piso cuenta; esto nombra: un glob que
		// se rompio y trajo otra cosa pasa el piso y no pasa esto.
		private const string TheStagingOfTheStays = "ShopStays.cs";

		// Y EL DEL LIBRO DE COSTOS, que es el que agrega CL-8. Se nombra igual que el de las estadias
		// y por lo mismo: el subcomando 'costos' guiona tres gastos y tres lecturas desde literales, y
		// si su archivo no llega al barrido, lo de abajo esta verde por no haberlo leido.
		private const string TheStagingOfTheCosts = "Costs.cs";

		[Test]
		public void TheConsoleStagedTheStaysAndTheSweepIsReadingThatFile()
		{
			string[] files = TheCodeOfTheShellsAsData.EveryFileOf(TheCodeOfTheShellsAsData.Consola);

			Assert.That(files, Has.Length.GreaterThanOrEqualTo(6),
				"La raiz de la consola trajo menos archivos de los que tiene. Un barrido que leyo de "
				+ "menos esta verde por no haber leido.");

			var named = new List<string>();

			foreach (string each in files)
				named.Add(TheCodeOfTheShellsAsData.InsideItsRoot(
					TheCodeOfTheShellsAsData.Consola, each));

			Assert.That(named, Does.Contain(TheStagingOfTheStays),
				"El guion de las estadias de la consola no llego al barrido, asi que lo de abajo no "
				+ "dice nada sobre el.");
			Assert.That(named, Does.Contain(TheStagingOfTheCosts),
				"El guion de los gastos de la consola no llego al barrido, asi que lo de abajo no dice "
				+ "nada sobre el.");
			Assert.That(named, Does.Contain("Program.cs"));
			Assert.That(named, Does.Contain("Warranty.cs"));
		}

		[Test]
		public void TheConsoleStagesFromLiteralsAndReadsNoClockAnywhere()
		{
			var found = new List<string>();

			foreach (string file in TheCodeOfTheShellsAsData.EveryFileOf(
				TheCodeOfTheShellsAsData.Consola))
			{
				string inside = TheCodeOfTheShellsAsData.InsideItsRoot(
					TheCodeOfTheShellsAsData.Consola, file);

				string[] live = TheCodeOfTheShellsAsData
					.OnlyWhatTheCompilerReads(File.ReadAllText(file), "cs")
					.Split('\n');

				for (int line = 0; line < live.Length; line++)
					if (AClockRead.IsMatch(live[line]))
						found.Add(inside + ":" + (line + 1) + "  " + live[line].Trim());
			}

			Assert.That(found, Is.Empty,
				"La consola leyo un reloj. Todo instante de esta caratula esta ESCRITO -- los cuatro "
				+ "momentos de 'garantia', las fechas de 'historia', los instantes y los dias de las "
				+ "estadias -- para que dos corridas contesten lo mismo. Un reloj aca no hace la "
				+ "corrida mas viva: la hace irreproducible, y la reproducibilidad es lo unico que "
				+ "esta caratula existe para medir. Lo encontrado:\n  " + string.Join("\n  ", found));
		}

		// EL CONTROL, Y SIN EL LO DE ARRIBA ES UNA LISTA VACIA QUE CUMPLE CUALQUIER COSA. Prueba las dos
		// mitades del detector: que VE las tres formas cuando estan escritas, y que el apagado de
		// literales y comentarios no las esconde cuando estan vivas.
		[Test]
		public void TheDetectorSeesAClockReadWhenThereIsOneAndIgnoresOneThatIsOnlyTalkedAbout()
		{
			foreach (string each in new[]
			{
				"DateTime now = DateTime.UtcNow;",
				"var d = DateTime.Now;",
				"if (x < DateTime.Today) return;",
				"var o = DateTimeOffset.UtcNow;",
			})
				Assert.That(
					AClockRead.IsMatch(TheCodeOfTheShellsAsData.OnlyWhatTheCompilerReads(each, "cs")),
					Is.True, "El detector no vio: " + each);

			foreach (string each in new[]
			{
				"// esta caratula no lee DateTime.UtcNow y por eso es reproducible",
				"Console.WriteLine(\"ningun instante sale de DateTime.UtcNow\");",
				"/// <summary>No lee DateTime.Now.</summary>",
			})
				Assert.That(
					AClockRead.IsMatch(TheCodeOfTheShellsAsData.OnlyWhatTheCompilerReads(each, "cs")),
					Is.False, "El detector conto una mencion apagada: " + each);
		}

		// Y LOS INSTANTES DEL GUION SON LITERALES, comprobado sobre el propio fuente: cada uno de los
		// cuatro momentos de la estadia se escribe con un new DateTime(...) de argumentos constantes.
		// Es la otra mitad de 'no lee reloj': un guion sin reloj que armara sus fechas restandole dias
		// a algo tampoco seria reproducible.
		[Test]
		public void EveryInstantOfTheStagedStaysIsWrittenDownAsALiteral()
		{
			string source = File.ReadAllText(Path.Combine(
				TheCodeOfTheShellsAsData.FolderOf(TheCodeOfTheShellsAsData.Consola),
				TheStagingOfTheStays));

			string live = TheCodeOfTheShellsAsData.OnlyWhatTheCompilerReads(source, "cs");

			var written = new Regex(@"new DateTime\(\s*\d{4}\s*,\s*\d{1,2}\s*,\s*\d{1,2}\s*,");

			Assert.That(written.Matches(live), Has.Count.EqualTo(5),
				"Los CINCO instantes de las tres estadias -- la entrada y la entrega de la primera, "
				+ "las dos de la segunda y la entrada de la que queda abierta -- escritos con sus "
				+ "argumentos constantes. Uno menos quiere decir que alguno se esta derivando.");

			foreach (string each in new[] { "AddDays", "AddMonths", "AddYears", "AddHours" })
				Assert.That(live, Does.Not.Contain(each),
					"'" + each + "' en el guion de las estadias. Una fecha derivada de otra sigue "
					+ "siendo reproducible, pero deja de decir cual es: los dias de una estadia los "
					+ "DICE la sucursal y se escriben tal como los dijo.");
		}
	}
}
