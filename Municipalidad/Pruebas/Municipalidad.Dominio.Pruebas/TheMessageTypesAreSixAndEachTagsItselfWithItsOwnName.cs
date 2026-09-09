using System;
using System.Collections.Generic;
using System.Reflection;
using Municipalidad.App.Events;
using NUnit.Framework;
using Puppets.Portador;

namespace Municipalidad.Dominio.Pruebas
{
	// EL CENSO DE LOS MENSAJES. Enumera POR REFLEXION y no contra una lista escrita a mano, porque una
	// lista a mano no puede ver el tipo que nadie le agrego -- y ese es justo el que hay que ver.
	//
	// SEIS, Y LA CUENTA SE DERIVA DE LA PUERTA. Son 10 comandos, menos los DOS de la persona
	// -DeclararValorDeFinca y AdoptarReserva, que este dominio tiene por hibrido: los aporta ella y la
	// municipalidad no los sabe-, menos los DOS de la credencial -RegistrarDispositivo y
	// GuardarCredencial, que son del bucle de refresh y no del feed-. 10 - 2 - 2 = 6.
	//
	// EL NUMERO ESTA EN EL NOMBRE DEL METODO a proposito: subirlo obliga a renombrarlo, que es lo unico
	// que impide que un septimo tipo entre en silencio. Es la forma de TheOperationsAreThirtyFour, una
	// capa mas abajo.
	[TestFixture]
	public sealed class TheMessageTypesAreSixAndEachTagsItselfWithItsOwnName
	{
		[Test]
		public void TheMessageTypesAreSix()
		{
			IReadOnlyList<Type> tipos = UnaNarrativaDeLaMunicipalidad.LosTiposDeMensaje();

			Assert.That(tipos.Count, Is.EqualTo(6),
				"Son SEIS tipos de mensaje, uno por comando de la puerta que la MUNICIPALIDAD manda: "
				+ "10 comandos menos los 2 de la persona menos los 2 de la credencial. Si sube, hay un "
				+ "tipo nuevo y los barridos que recorren la narrativa no lo van a ver hasta que alguien "
				+ "lo agregue ahi.");
		}

		// EL TAG VIAJA POR SITIOS QUE NO TIENEN PRUEBA -- la ruta que escribe E-2, el mock del feed y el
		// inyector de E-3 --, asi que la unica forma de que no se despegue de su tipo es que sea derivable
		// del tipo. Por eso se afirma que el tag ES el nombre de la clase y no solo que existe.
		[Test]
		public void EveryMessageTypeTagsItselfWithItsOwnTypeNameAndDeclaresAnInterpret()
		{
			Assert.Multiple(() =>
			{
				foreach (Type tipo in UnaNarrativaDeLaMunicipalidad.LosTiposDeMensaje())
				{
					FieldInfo tag = tipo.GetField(
						"ElTipoDelEvento", BindingFlags.Public | BindingFlags.Static);

					Assert.That(tag, Is.Not.Null,
						tipo.Name + " no declara ElTipoDelEvento, asi que su tag no sale de su tipo.");

					Assert.That(tag.GetRawConstantValue(), Is.EqualTo(tipo.Name),
						"El tag de " + tipo.Name + " tiene que ser su propio nombre de tipo.");

					MethodInfo interpret = tipo.GetMethod(
						"Interpret", BindingFlags.Public | BindingFlags.Static);

					Assert.That(interpret, Is.Not.Null,
						tipo.Name + " no declara un Interpret estatico, asi que ninguna senal puede "
						+ "volverse ese hecho.");

					Assert.That(interpret.ReturnType, Is.EqualTo(tipo),
						"Y contesta SU tipo, no la interfaz: quien lo llama tiene que poder leer los "
						+ "campos del hecho sin castear. " + tipo.Name);

					Assert.That(typeof(IMensajeDeEvento).IsAssignableFrom(tipo));
				}
			});
		}

		// Y LOS SEIS ESTAN EN LA NARRATIVA, que es lo que hace que los otros barridos midan seis y no
		// cinco. Un tipo de mensaje sin senal es codigo que ninguna prueba visita.
		[Test]
		public void EveryMessageTypeHasASignalInTheNarrative()
		{
			HashSet<string> enLaNarrativa = new HashSet<string>();

			foreach (SenalDeEvento senal in UnaNarrativaDeLaMunicipalidad.LaNarrativaEntera())
				enLaNarrativa.Add(senal.Tipo);

			Assert.Multiple(() =>
			{
				foreach (Type tipo in UnaNarrativaDeLaMunicipalidad.LosTiposDeMensaje())
					Assert.That(enLaNarrativa, Does.Contain(tipo.Name),
						"La narrativa no trae ninguna senal de tipo " + tipo.Name + ", asi que ningun "
						+ "barrido de esta suite lo visita.");
			});
		}

		// LOS DOS COMANDOS DE LA PERSONA NO TIENEN EVENTO, y decirlo con una afirmacion es lo que impide
		// que alguien "complete" el juego mas adelante. La municipalidad no sabe cuanto vale la finca de
		// nadie ni cuanto decidio apartar nadie: son la mitad hibrida de este dominio, y un evento que los
		// trajera pondria a la institucion como fuente de un hecho de la persona.
		[Test]
		public void NeitherOfThePersonsTwoCommandsHasAnEventType()
		{
			HashSet<string> tags = new HashSet<string>();

			foreach (Type tipo in UnaNarrativaDeLaMunicipalidad.LosTiposDeMensaje())
				tags.Add(UnaNarrativaDeLaMunicipalidad.ElTagDe(tipo));

			Assert.Multiple(() =>
			{
				Assert.That(tags, Does.Not.Contain("ValorDeFincaDeclarado"));
				Assert.That(tags, Does.Not.Contain("ReservaAdoptada"));

				foreach (string tag in tags)
				{
					Assert.That(tag, Does.Not.Contain("Reserva"),
						"Ningun hecho de la municipalidad habla de la reserva: es de la persona.");
					Assert.That(tag, Does.Not.Contain("ValorDeFinca"),
						"Ni del valor de la finca, que es el hueco 2 y lo aporta la persona.");
				}
			});
		}

		// Y LOS DOS DE LA CREDENCIAL TAMPOCO, por otra razon: esos SI son del proveedor, pero los ejecuta
		// el bucle de refresh contra la bitacora antes de leer una sola senal. Un evento que los trajera
		// pediria una credencial para poder leer la credencial.
		[Test]
		public void NeitherOfTheTwoCredentialCommandsHasAnEventType()
		{
			HashSet<string> tags = new HashSet<string>();

			foreach (Type tipo in UnaNarrativaDeLaMunicipalidad.LosTiposDeMensaje())
				tags.Add(UnaNarrativaDeLaMunicipalidad.ElTagDe(tipo));

			Assert.Multiple(() =>
			{
				foreach (string tag in tags)
				{
					Assert.That(tag, Does.Not.Contain("Credencial"));
					Assert.That(tag, Does.Not.Contain("Dispositivo"));
					Assert.That(tag, Does.Not.Contain("Token"));
				}
			});
		}
	}
}
