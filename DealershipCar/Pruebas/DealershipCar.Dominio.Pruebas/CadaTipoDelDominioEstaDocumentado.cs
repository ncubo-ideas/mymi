using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using NUnit.Framework;

namespace DealershipCar.Dominio.Pruebas
{
	// E21: "/// en cada tipo y cada miembro expuesto, diciendo su rol de dominio. Un tipo sin
	// /// es un defecto, como una invariante sin guarda."
	//
	// POR QUE ESTA PRUEBA EXISTE, y es un hallazgo de T2: el csproj del dominio prende
	// GenerateDocumentationFile creyendo que asi un /// que falta es un WARNING del
	// compilador. NO LO ES. CS1591 dice literalmente "falta el comentario XML para el tipo o
	// miembro VISIBLE PUBLICAMENTE", y este dominio es internal de punta a punta (E2, S1).
	// O sea que el guard-rail que T1 creyo poner esta INERTE: se puede escribir todo el
	// dominio sin un solo /// y compilar en verde.
	//
	// Se cierra el agujero donde se puede cerrar. El XML generado SI lleva a los miembros
	// internal que tienen ///, asi que se compara lo que el ensamblado declara contra lo que
	// el XML documento. Lo que falta sale con nombre y apellido.
	//
	// UNA EXCLUSION DELIBERADA: los miembros de los enums de frontera. Un /// por miembro de
	// enum seria decoracion -- repetiria el nombre -- y E21 dice que el /// no es adorno. El
	// significado de cada codigo vive en el /// del miembro HOMONIMO del conjunto cerrado que
	// ese enum alcanza, y el /// del enum apunta ahi. El tipo enum si necesita el suyo.
	[TestFixture]
	public sealed class CadaTipoDelDominioEstaDocumentado
	{
		private const string EnsambladoDelDominio = "DealershipCar.Dominio";
		private const string XmlDelDominio = "DealershipCar.Dominio.xml";

		private HashSet<string> documentados;
		private List<Type> tipos;

		[SetUp]
		public void Setup()
		{
			string ruta = Path.Combine(AppContext.BaseDirectory, XmlDelDominio);
			Assert.That(File.Exists(ruta), Is.True,
				$"No se encontro '{XmlDelDominio}' en el output. Lo genera "
				+ "GenerateDocumentationFile en el csproj del dominio; si dejo de viajar, esta "
				+ "prueba dejo de proteger nada.");

			documentados = new HashSet<string>(
				XDocument.Load(ruta)
					.Descendants("member")
					.Select(miembro => (string)miembro.Attribute("name"))
					.Where(nombre => !string.IsNullOrEmpty(nombre)),
				StringComparer.Ordinal);

			tipos = Assembly.Load(new AssemblyName(EnsambladoDelDominio))
				.GetTypes()
				.Where(tipo => tipo.Namespace == EnsambladoDelDominio)
				.Where(tipo => !tipo.IsDefined(typeof(CompilerGeneratedAttribute), false))
				.OrderBy(tipo => tipo.FullName, StringComparer.Ordinal)
				.ToList();
		}

		[Test]
		public void CadaTipoDeclaraSuResumen()
		{
			Assert.That(tipos, Is.Not.Empty, "El ensamblado del dominio no declara ni un tipo.");

			List<string> sinResumen = tipos
				.Where(tipo => !documentados.Contains("T:" + tipo.FullName))
				.Select(tipo => tipo.Name)
				.ToList();

			Assert.That(sinResumen, Is.Empty,
				"Tipos sin /// : " + string.Join(", ", sinResumen) + ". "
				+ "E21: el /// dice el ROL DE DOMINIO del tipo -- que ES en el negocio, no como "
				+ "esta hecho. Un tipo sin el obliga a cada lector, humano o IA, a deducirlo del "
				+ "codigo.");
		}

		[Test]
		public void CadaMiembroExpuestoDeclaraSuResumen()
		{
			List<string> sinResumen = new List<string>();

			foreach (Type tipo in tipos)
			{
				if (tipo.IsEnum) continue;   // sus miembros, por la exclusion de arriba

				const BindingFlags Declarados = BindingFlags.DeclaredOnly | BindingFlags.Static
					| BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

				foreach (FieldInfo campo in tipo.GetFields(Declarados))
				{
					if (!(campo.IsPublic || campo.IsAssembly)) continue;
					Anotar(sinResumen, tipo, campo.Name, "F:" + tipo.FullName + "." + campo.Name);
				}

				foreach (PropertyInfo propiedad in tipo.GetProperties(Declarados))
				{
					MethodInfo lector = propiedad.GetGetMethod(true);
					if (lector == null || !(lector.IsPublic || lector.IsAssembly)) continue;
					Anotar(sinResumen, tipo, propiedad.Name,
						"P:" + tipo.FullName + "." + propiedad.Name);
				}

				foreach (MethodInfo metodo in tipo.GetMethods(Declarados))
				{
					if (metodo.IsSpecialName) continue;   // get_/set_/op_: no se documentan solos
					if (!(metodo.IsPublic || metodo.IsAssembly)) continue;
					AnotarConFirma(sinResumen, tipo, metodo.Name,
						"M:" + tipo.FullName + "." + metodo.Name);
				}

				foreach (ConstructorInfo ctor in tipo.GetConstructors(Declarados))
				{
					if (!(ctor.IsPublic || ctor.IsAssembly)) continue;
					AnotarConFirma(sinResumen, tipo, ".ctor", "M:" + tipo.FullName + ".#ctor");
				}
			}

			Assert.That(sinResumen, Is.Empty,
				"Miembros expuestos sin /// : " + string.Join(", ", sinResumen) + ". "
				+ "Expuesto quiere decir public o internal: son exactamente los que el DSL puede "
				+ "nombrar por reflexion (E15), o sea los que otro lee para saber que hace el "
				+ "dominio.");
		}

		private void Anotar(List<string> sinResumen, Type tipo, string miembro, string id)
		{
			if (!documentados.Contains(id)) sinResumen.Add(tipo.Name + "." + miembro);
		}

		// Los metodos y constructores llevan la firma en el id del XML -- "M:T.M(System.Int32)"
		// -- asi que se compara por prefijo en vez de armar la firma a mano.
		private void AnotarConFirma(List<string> sinResumen, Type tipo, string miembro, string id)
		{
			bool documentado = documentados.Contains(id)
				|| documentados.Any(candidato => candidato.StartsWith(id + "(", StringComparison.Ordinal));

			if (!documentado) sinResumen.Add(tipo.Name + "." + miembro);
		}
	}
}
