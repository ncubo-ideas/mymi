using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace DealershipCar.Dominio.Pruebas
{
	// LAS DOS TRANSICIONES ILEGALES DE §10 del brief QUE HABLAN DE UNA REVISION SELLADA:
	//
	//   Sealed acepta un publish   -> revienta
	//   Sealed -> Draft            -> no existe camino
	//
	// Son dos formas de la misma ley: sellada quiere decir CERRADA. Una revision sellada ya
	// contesta (L12), y todo lo que contesto nombra esa revision (L1). Si despues de sellar
	// entrara contenido nuevo -- o si volviera a borrador y se sellara otra vez con contenido
	// distinto -- la revision que alguien tiene anotada como procedencia diria una cosa
	// distinta de la que dijo, sin haberse superseido. Eso es exactamente lo que la ley L11
	// prohibe: una revision no alcanza hacia atras.
	//
	// LA SEGUNDA MITAD SE AFIRMA COMO AUSENCIA, y por eso hay lista blanca. "Sealed -> Draft"
	// no se puede probar llamando a algo: se prueba mostrando que no hay NADA que llamar. Un
	// Reopen, un Unseal, un BackToDraft, un setter de estado -- cualquiera pone rojo esto y
	// obliga a leer L11 antes de seguir. Cuando entren los caminos de publicacion de verdad,
	// esta lista crece con ellos: cada miembro nuevo se lee contra esta ley y despues se
	// agrega, en ese orden y no al reves.
	[TestFixture]
	public sealed class ASealedRevisionIsClosedToPublicationAndCannotReopen
	{
		private const string TheRevision = "REV-2025-01";
		private const string TheNextRevision = "REV-2025-02";

		private static readonly DateTime PublishedAt = new DateTime(2025, 3, 14, 10, 0, 0);

		// La lista blanca de PublishedRevision: la identidad y el instante que recibio, la
		// proyeccion del estado, las tres preguntas totales, el accesor guardado del sucesor, y
		// las dos transiciones mas el camino por el que entra contenido. Nada que reabra.
		private static readonly string[] LoQueUnaRevisionPuedeExponer =
		{
			".ctor",
			"Code",
			"PublishedAt",
			"ReadState",
			"IsSealed",
			"HasContent",
			"HasBeenSuperseded",
			"SupersededByCode",
			"AcceptAPublication",
			"Seal",
			"SupersededBy",
		};

		private PublishedRevision revision;

		[SetUp]
		public void Setup()
		{
			revision = new PublishedRevision(TheRevision, PublishedAt);
			revision.AcceptAPublication();
			revision.Seal();
		}

		[Test]
		public void PublishingIntoASealedRevisionIsRefused()
		{
			DomainException error =
				Assert.Throws<DomainException>(() => revision.AcceptAPublication());

			Assert.That(error.Message, Does.Contain("Sealed"),
				"El mensaje nombra el estado que rechazo la publicacion.");
			Assert.That(revision.ReadState(), Is.EqualTo("Sealed"),
				"Y el rechazo es inerte: la revision sigue sellada, con el contenido que tenia.");
		}

		[Test]
		public void PublishingIntoASupersededRevisionIsRefused()
		{
			revision.SupersededBy(ASealedRevision(TheNextRevision));

			DomainException error =
				Assert.Throws<DomainException>(() => revision.AcceptAPublication());

			Assert.That(error.Message, Does.Contain("Superseded"));
			Assert.That(revision.ReadState(), Is.EqualTo("Superseded"));
		}

		[Test]
		public void NothingTakesASealedRevisionBackToADraft()
		{
			List<string> expuestos = typeof(PublishedRevision)
				.GetMembers(BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public
					| BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
				.Where(EsExpuesto)
				.Select(miembro => miembro.Name)
				.Distinct()
				.OrderBy(nombre => nombre, StringComparer.Ordinal)
				.ToList();

			List<string> intrusos = expuestos
				.Where(nombre => !LoQueUnaRevisionPuedeExponer.Contains(nombre))
				.ToList();

			Assert.That(intrusos, Is.Empty,
				"PublishedRevision gano miembros: " + string.Join(", ", intrusos) + ". "
				+ "§10 del brief no tiene fila de Sealed a Draft, y no tenerla es la ley: una "
				+ "revision que ya contesto no puede volver a borrador y cambiar lo que dijo. Si el "
				+ "miembro nuevo no reabre nada, agregarlo a la lista es correcto -- pero se lee "
				+ "contra esta ley primero.");
		}

		// El estado sale como NOMBRE y no como objeto (E24). Entregar el RevisionState dejaria
		// al de afuera ramificar sobre el interior en vez de preguntar; y como el conjunto se
		// elige adentro, no hay ninguna razon para que el objeto cruce.
		[Test]
		public void TheStateLeavesAsAScalarAndNotAsTheObject()
		{
			IEnumerable<MemberInfo> queEntreganElEstado = typeof(PublishedRevision)
				.GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
					| BindingFlags.DeclaredOnly)
				.Where(EsExpuesto)
				.Where(miembro => TipoQueDevuelve(miembro) == typeof(RevisionState));

			Assert.That(queEntreganElEstado, Is.Empty,
				"Algun miembro expuesto entrega el RevisionState. Sale ReadState() con el nombre, "
				+ "que es lo unico que cruza.");
		}

		private static PublishedRevision ASealedRevision(string code)
		{
			PublishedRevision sellada = new PublishedRevision(code, PublishedAt.AddDays(30));
			sellada.AcceptAPublication();
			sellada.Seal();

			return sellada;
		}

		private static Type TipoQueDevuelve(MemberInfo miembro)
		{
			if (miembro is FieldInfo campo) return campo.FieldType;
			if (miembro is PropertyInfo propiedad) return propiedad.PropertyType;
			if (miembro is MethodInfo metodo) return metodo.ReturnType;
			return null;
		}

		private static bool EsExpuesto(MemberInfo miembro)
		{
			if (miembro is FieldInfo campo) return campo.IsPublic || campo.IsAssembly;
			if (miembro is PropertyInfo propiedad)
			{
				MethodInfo lector = propiedad.GetGetMethod(true);
				return lector != null && (lector.IsPublic || lector.IsAssembly);
			}
			if (miembro is MethodInfo metodo)
				return !metodo.IsSpecialName && (metodo.IsPublic || metodo.IsAssembly);
			if (miembro is ConstructorInfo ctor) return ctor.IsPublic || ctor.IsAssembly;
			return false;
		}
	}
}
