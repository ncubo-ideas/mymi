using System.Reflection;
using Concesionario.App;
using NUnit.Framework;

namespace Concesionario.Dominio.Pruebas
{
	// LA COPIA QUE LA CERCA OBLIGO, AMARRADA.
	//
	// Cuando el dominio rechaza un hecho, la Superficie deja que la narrativa avance igual: el
	// evento se marca como procesado y el rechazo se contesta como texto. Ver
	// AFactTheDomainRefusesDoesNotBlockTheNarrative, que prueba ese comportamiento.
	//
	// Para hacerlo, DealershipCarAPI escribia `catch (DomainException)`. Desde que la Superficie
	// salio de la lista de InternalsVisibleTo del dominio, no puede nombrar ese tipo -- y no le
	// hace falta: lo que necesita saber no es un tipo sino una PREGUNTA, "rechazo el dominio este
	// hecho?", cuya respuesta consume como cadena. Asi que lo reconoce por su nombre completo.
	//
	// Reconocer por nombre es una copia, y una copia sin prueba se desincroniza en silencio: el
	// catch dejaria de atrapar y un hecho rechazado tumbaria el avance de la narrativa en vez de
	// contestarse. Este fixture es el UNICO lugar del repo que ve los dos lados -- el dominio por
	// InternalsVisibleTo y la Superficie por referencia normal -- asi que es el unico que puede
	// obligarlos a coincidir.
	[TestFixture]
	public sealed class TheDoorNamesTheDomainsRefusalByItsFullName
	{
		// SE ENTRA POR REFLEXION, igual que a la puerta: Concesionario.App no declara ningun
		// InternalsVisibleTo, y no hay razon para abrirla por una constante.
		[Test]
		public void TheNameTheSurfaceWatchesForIsTheOneTheDomainThrows()
		{
			FieldInfo constante = typeof(DealershipCarAPI).GetField(
				"TheDomainRefusal", BindingFlags.Static | BindingFlags.NonPublic);

			Assert.That(constante, Is.Not.Null,
				"DealershipCarAPI.TheDomainRefusal no existe. Es el nombre completo de la "
				+ "excepcion con la que el dominio rechaza un hecho, y el catch de la Superficie "
				+ "lo necesita porque no puede nombrar el tipo.");

			Assert.That(
				constante.GetRawConstantValue(),
				Is.EqualTo(typeof(DomainException).FullName),
				"La Superficie esta esperando un nombre que el dominio ya no lanza. Si "
				+ "DomainException se renombro o cambio de namespace, hay que mover la constante "
				+ "con el -- si no, el catch deja de atrapar y el rechazo del dominio tumba el "
				+ "avance de la narrativa en vez de contestarse.");
		}
	}
}
