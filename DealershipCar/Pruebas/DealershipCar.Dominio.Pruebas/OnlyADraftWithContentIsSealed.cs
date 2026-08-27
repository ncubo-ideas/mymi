using System;
using NUnit.Framework;

namespace DealershipCar.Dominio.Pruebas
{
	// LA GUARDA DE SELLO DE §10 del brief, que es la ley L12 en su forma de comando:
	//
	//   Draft | revision sellada | Sealed | rechaza una revision vacia
	//
	// Y las dos transiciones ilegales que §10 manda probar y que terminan en el mismo sitio:
	// sellar dos veces, y sellar una revision ya sustituida.
	//
	// LAS TRES REVIENTAN (E32), y esa es la decision del plan: una revision se sella desde el
	// feed de publicaciones del fabricante, asi que un sello imposible no es un resultado del
	// negocio sino un defecto del que publica. C22 -- una guarda que revienta adentro de un
	// comando deja cicatriz en el journal -- no muerde aca: estos comandos llegan DETRAS del
	// checkpoint de eventos, asi que una re-entrega nunca alcanza la guarda.
	//
	// POR QUE UNA REVISION VACIA NO SE PUEDE SELLAR, dicho en negocio: sellada quiere decir
	// citable. Una revision vacia y sellada seria una hoja de especificaciones sin una sola
	// especificacion que, sin embargo, contesta -- y lo que contestaria es "no publicado" a
	// todo, firmado por una revision. Eso es peor que no tener revision: tiene procedencia.
	[TestFixture]
	public sealed class OnlyADraftWithContentIsSealed
	{
		private const string TheRevision = "REV-2025-01";
		private const string TheNextRevision = "REV-2025-02";

		private static readonly DateTime PublishedAt = new DateTime(2025, 3, 14, 10, 0, 0);

		private PublishedRevision revision;

		[SetUp]
		public void Setup()
		{
			revision = new PublishedRevision(TheRevision, PublishedAt);
		}

		[Test]
		public void AnEmptyRevisionCannotBeSealed()
		{
			DomainException error = Assert.Throws<DomainException>(() => revision.Seal());

			Assert.That(error.Message, Does.Not.Contain("L12"),
				"C45: el mensaje dice la REGLA en palabras, no cita un documento. Una excepcion que "
				+ "un programa puede imprimir no puede llevar un identificador que solo se entiende "
				+ "con el papel al lado.");
		}

		// Y el rechazo es INERTE: la revision se queda exactamente donde estaba, borrador y
		// vacia. Un sello a medias -- sellada pero sin contenido -- seria justo el estado que la
		// guarda existe para que no exista.
		[Test]
		public void ARefusedSealLeavesTheRevisionADraft()
		{
			Assert.Throws<DomainException>(() => revision.Seal());

			Assert.That(revision.ReadState(), Is.EqualTo("Draft"));
			Assert.That(revision.IsSealed(), Is.False);
			Assert.That(revision.HasContent(), Is.False);
		}

		[Test]
		public void ADraftWithContentIsSealed()
		{
			revision.AcceptAPublication();

			revision.Seal();

			Assert.That(revision.ReadState(), Is.EqualTo("Sealed"));
			Assert.That(revision.IsSealed(), Is.True,
				"Ley L12: sellada es la unica condicion en la que una revision contesta.");
		}

		// Sellar dos veces. No es idempotente a proposito: sellar es un hecho que pasa UNA vez,
		// y un segundo sello sobre lo mismo solo puede venir de un feed que perdio la cuenta.
		[Test]
		public void SealingTwiceIsRefused()
		{
			revision.AcceptAPublication();
			revision.Seal();

			DomainException error = Assert.Throws<DomainException>(() => revision.Seal());

			Assert.That(error.Message, Does.Contain("Sealed"),
				"El mensaje tiene que decir en que estado estaba. Un rechazo que no nombra el "
				+ "estado obliga a reconstruir la historia de la revision para entenderlo.");
			Assert.That(revision.ReadState(), Is.EqualTo("Sealed"),
				"Y el segundo sello no movio nada: sigue sellada, no volvio a Draft ni paso a otra "
				+ "cosa.");
		}

		// Superseded -> Sealed, la transicion ilegal que §10 nombra. Sustituida es TERMINAL: una
		// revision que ya fue reemplazada no vuelve a ser la que contesta, porque si volviera, la
		// respuesta que alguien recibio de su sucesora quedaria contradicha por su predecesora.
		[Test]
		public void ASupersededRevisionCannotBeSealedAgain()
		{
			PublishedRevision sucesora = ASealedRevision(TheNextRevision);
			revision.AcceptAPublication();
			revision.Seal();
			revision.SupersededBy(sucesora);

			DomainException error = Assert.Throws<DomainException>(() => revision.Seal());

			Assert.That(error.Message, Does.Contain("Superseded"));
			Assert.That(revision.ReadState(), Is.EqualTo("Superseded"),
				"Terminal quiere decir terminal: el intento de sellar no la resucito.");
		}

		private static PublishedRevision ASealedRevision(string code)
		{
			PublishedRevision sellada = new PublishedRevision(code, PublishedAt.AddDays(30));
			sellada.AcceptAPublication();
			sellada.Seal();

			return sellada;
		}
	}
}
