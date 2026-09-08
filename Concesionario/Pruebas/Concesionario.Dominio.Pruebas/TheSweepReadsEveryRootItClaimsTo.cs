using System.Collections.Generic;
using System.IO;
using NUnit.Framework;

namespace Concesionario.Dominio.Pruebas
{
	// UN BARRIDO VALE LO QUE ENUMERA (C57). TheShellPerformsNoArithmeticOnADomainValue afirma que en
	// las raices juzgadas no hay mas aritmetica que la escrita; esta prueba afirma la mitad que
	// aquella no puede afirmar sobre si misma -- que las raices existen, que llegaron con contenido, y
	// que las que el barrido DICE barrer son exactamente las que hay en el output.
	//
	// LA TERCERA ES LA QUE ATAJA EL DESCUIDO REAL: agregar un grupo <None> al csproj sin nombrar la
	// raiz en el barrido deja una carpeta entera sin mirar, en silencio; nombrarla en el barrido sin
	// agregar el grupo revienta ruidoso, que ya es mejor. Las dos se ponen rojas aca.
	[TestFixture]
	public sealed class TheSweepReadsEveryRootItClaimsTo
	{
		private static readonly string[] TheRoots = TheCodeOfTheShellsAsData.EveryRoot();

		[Test]
		[TestCaseSource(nameof(TheRoots))]
		public void ThisRootArrivedWithSomethingToRead(string root)
		{
			string[] files = TheCodeOfTheShellsAsData.EveryFileOf(root);

			Assert.That(files, Is.Not.Empty,
				"La raiz '" + root + "' llego vacia. El barrido la recorre igual y no encuentra nada, "
				+ "que se lee identico a una caratula limpia.");

			var read = new List<string>();

			foreach (string file in files)
				if (File.ReadAllText(file).Length > 0)
					read.Add(TheCodeOfTheShellsAsData.InsideItsRoot(root, file));

			Assert.That(read, Is.Not.Empty,
				"Los archivos de '" + root + "' llegaron y todos leyeron vacio. Un archivo de cero "
				+ "bytes no tiene aciertos: el barrido queda verde por no haber leido.");
		}

		// Y LAS RAICES QUE EL BARRIDO NOMBRA SON LAS QUE HAY. Sin este renglon, una carpeta copiada
		// por el csproj y no nombrada aca no la mira nadie y nada se pone rojo.
		[Test]
		public void TheRootsTheSweepNamesAreTheFoldersThatArrived()
		{
			string outputFolder = Path.GetDirectoryName(TheCodeOfTheShellsAsData.FolderOf(TheCodeOfTheShellsAsData.App));

			Assert.That(Directory.Exists(outputFolder), Is.True,
				"'CodigoComoDato' no llego al output. Los grupos <None> del csproj son lo que lo trae.");

			var arrived = new List<string>();

			foreach (string folder in Directory.GetDirectories(outputFolder))
				arrived.Add(Path.GetFileName(folder));

			Assert.That(arrived, Is.EquivalentTo(TheRoots),
				"Las carpetas que el csproj copio y las raices que el barrido nombra dejaron de ser "
				+ "las mismas. Una carpeta copiada y no nombrada queda sin juzgar en silencio, que es "
				+ "el unico de los dos descuidos que no se anuncia solo.");
		}
	}
}
