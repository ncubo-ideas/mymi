using NUnit.Framework;

namespace Municipalidad.Dominio.Pruebas
{
	// LO QUE M-2 PIDE LITERALMENTE: toda lectura GUARDADA, y nunca un texto vacio haciendo de
	// ausencia. Un token vacio se veria igual que un token, y quien lo recibiera creeria tener con
	// que leer los eventos.
	//
	// Es la leccion de T1 -- el Fin del Plan que devolvia 01/01/0001 en silencio -- aplicada a las
	// tres lecturas de la bitacora, y la forma que /puppeteer-domain-modeling E41 llama predicado
	// total mas accesor guardado: el predicado nunca revienta, el accesor niega.
	//
	// LA CARATULA NO PUEDE ATRAPAR ESA NEGATIVA, porque DomainException es internal en este modulo:
	// pre-pregunta con el predicado y proyecta vacio ella misma. Es la ruta de DEC-08, y la puerta
	// la paga con dos consultas por lectura.
	[TestFixture]
	public sealed class TheBitacoraNeverAnswersAnAbsenceWithAnEmptyString
	{
		[Test]
		public void AFreshBitacoraRefusesEveryGuardedReadAndItsPredicatesStillAnswer()
		{
			Bitacora bitacora = new Bitacora();

			Assert.Multiple(() =>
			{
				// Los tres predicados son TOTALES: contestan sobre una bitacora recien creada.
				Assert.That(bitacora.TieneDispositivo(), Is.False, "Bitacora.TieneDispositivo");
				Assert.That(bitacora.TieneCredencial(), Is.False, "Bitacora.TieneCredencial");
				Assert.That(bitacora.PuedeRotar(), Is.False, "Bitacora.PuedeRotar");

				// Y los tres accesores NIEGAN en vez de devolver "".
				Assert.Throws<DomainException>(
					() => bitacora.ClaveDeDispositivo(), "Bitacora.ClaveDeDispositivo");
				Assert.Throws<DomainException>(
					() => bitacora.Token(), "Bitacora.Token");
				Assert.Throws<DomainException>(
					() => bitacora.TokenDeRefresco(), "Bitacora.TokenDeRefresco");
			});
		}

		// LA MITAD QUE HACE FALSABLE A LA DE ARRIBA: los mismos tres accesores CONTESTAN cuando el
		// hecho esta. Sin este caso, un accesor que negara siempre dejaria la prueba anterior verde.
		[Test]
		public void TheSameThreeReadsAnswerOnceTheFactIsOnRecord()
		{
			Bitacora bitacora = new Bitacora();
			bitacora.RegistrarDispositivo("D-1");
			bitacora.GuardarCredencial("t-1", "r-1");

			Assert.Multiple(() =>
			{
				Assert.That(bitacora.TieneDispositivo(), Is.True, "Bitacora.TieneDispositivo");
				Assert.That(bitacora.TieneCredencial(), Is.True, "Bitacora.TieneCredencial");
				Assert.That(bitacora.PuedeRotar(), Is.True, "Bitacora.PuedeRotar");

				Assert.That(bitacora.ClaveDeDispositivo(), Is.EqualTo("D-1"),
					"Bitacora.ClaveDeDispositivo");
				Assert.That(bitacora.Token(), Is.EqualTo("t-1"), "Bitacora.Token");
				Assert.That(bitacora.TokenDeRefresco(), Is.EqualTo("r-1"), "Bitacora.TokenDeRefresco");
			});
		}

		// Y LA NEGATIVA ES LA DEL DOMINIO, no una excepcion del framework: es lo que M-1 dejo
		// medible, y lo que hace que un Apply que reciba un hecho ilegal cicatrice el journal en vez
		// de parar el bloque entero del refresh.
		[Test]
		public void EveryRefusalOfTheBitacoraIsTheDomainsOwnRefusal()
		{
			Bitacora bitacora = new Bitacora();

			Assert.Multiple(() =>
			{
				Assert.Throws<DomainException>(
					() => bitacora.Avanzar(0L), "Bitacora.Avanzar");
				Assert.Throws<DomainException>(
					() => bitacora.RegistrarDispositivo("  "), "Bitacora.RegistrarDispositivo");
				Assert.Throws<DomainException>(
					() => bitacora.GuardarCredencial("  ", ""), "Bitacora.GuardarCredencial");
			});
		}
	}
}
