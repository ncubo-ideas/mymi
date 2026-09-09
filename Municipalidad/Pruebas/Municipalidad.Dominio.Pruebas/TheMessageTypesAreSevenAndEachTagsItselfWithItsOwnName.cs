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
	// SIETE, Y LA CUENTA SE DERIVA DE LA PUERTA. Son 11 comandos, menos los DOS de la persona
	// -DeclararValorDeFinca y AdoptarReserva, que este dominio tiene por hibrido: los aporta ella y la
	// municipalidad no los sabe-, menos los DOS de la credencial -RegistrarDispositivo y
	// GuardarCredencial, que son del bucle de refresh y no del feed-. 11 - 2 - 2 = 7.
	//
	// ERAN SEIS, Y EL SEPTIMO ES AvaluoFijado. La version anterior de esta fixture afirmaba que ningun
	// tag de la municipalidad podia contener "ValorDeFinca", con este argumento: "La municipalidad no
	// sabe cuanto vale la finca de nadie". Eso resulto ser falso y la afirmacion se retiro a proposito:
	// el avaluo lo FIJA la institucion -- es el valor con que cobra los rubros sobre base imponible --,
	// y la persona se lo declaro a ella por ley antes de que existiera esta app. Lo que sigue siendo
	// cierto, y se afirma abajo, es que la DECLARACION que la persona hace EN ESTA APP no tiene evento:
	// es un hecho suyo, con otro dueno, sobre otra linea de tiempo.
	//
	// EL NUMERO ESTA EN EL NOMBRE DEL METODO a proposito: subirlo obliga a renombrarlo, que es lo unico
	// que impide que un octavo tipo entre en silencio. Es la forma de TheOperationsAreThirtyEight, una
	// capa mas abajo.
	[TestFixture]
	public sealed class TheMessageTypesAreSevenAndEachTagsItselfWithItsOwnName
	{
		[Test]
		public void TheMessageTypesAreSeven()
		{
			IReadOnlyList<Type> tipos = UnaNarrativaDeLaMunicipalidad.LosTiposDeMensaje();

			Assert.That(tipos.Count, Is.EqualTo(7),
				"Son SIETE tipos de mensaje, uno por comando de la puerta que la MUNICIPALIDAD manda: "
				+ "11 comandos menos los 2 de la persona menos los 2 de la credencial. Si sube, hay un "
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

		// Y LOS SIETE ESTAN EN LA NARRATIVA, que es lo que hace que los otros barridos midan siete y no
		// seis. Un tipo de mensaje sin senal es codigo que ninguna prueba visita.
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
		// que alguien "complete" el juego mas adelante. Cuanto decidio apartar la persona no lo sabe
		// nadie mas que ella, y el valor que ella DECLARA en esta app es suyo: son la mitad hibrida de
		// este dominio, y un evento que los trajera pondria a la institucion como fuente de un hecho de
		// la persona.
		//
		// SE AFIRMAN LOS NOMBRES DE LOS DOS COMANDOS Y NO UNA SUBCADENA. La version anterior prohibia la
		// subcadena "ValorDeFinca" en todo tag, que era una forma de decir "la municipalidad no sabe
		// cuanto vale una finca" -- y eso es falso: AvaluoFijado existe justamente porque lo sabe. La
		// afirmacion util es la de los DOS COMANDOS, que es la que sigue siendo cierta.
		[Test]
		public void NeitherOfThePersonsTwoCommandsHasAnEventType()
		{
			HashSet<string> tags = new HashSet<string>();

			foreach (Type tipo in UnaNarrativaDeLaMunicipalidad.LosTiposDeMensaje())
				tags.Add(UnaNarrativaDeLaMunicipalidad.ElTagDe(tipo));

			Assert.Multiple(() =>
			{
				Assert.That(tags, Does.Not.Contain("ValorDeFincaDeclarado"),
					"Lo que la PERSONA declara en esta app no llega por el feed de la institucion.");
				Assert.That(tags, Does.Not.Contain("ReservaAdoptada"));

				foreach (string tag in tags)
				{
					Assert.That(tag, Does.Not.Contain("Reserva"),
						"Ningun hecho de la municipalidad habla de la reserva: es de la persona.");
					Assert.That(tag, Does.Not.Contain("Declarad"),
						"Ni de una declaracion, que es el hecho de la persona sobre su propia finca.");
				}
			});
		}

		// Y EL SEPTIMO ES DE LA MUNICIPALIDAD, dicho en positivo para que quede una afirmacion que
		// nombra el hecho nuevo en vez de solo la ausencia de los otros dos. El avaluo es el valor con
		// que la institucion COBRA: si dejara de venir por el feed, la app volveria a no poder decir de
		// cuanto a cuanto paso, que era el hueco 2 en su mitad cerrada.
		[Test]
		public void TheAssessedValueIsAFactTheMunicipalityPublishesAndItIsNotThePersonsDeclaration()
		{
			HashSet<string> tags = new HashSet<string>();

			foreach (Type tipo in UnaNarrativaDeLaMunicipalidad.LosTiposDeMensaje())
				tags.Add(UnaNarrativaDeLaMunicipalidad.ElTagDe(tipo));

			Assert.That(tags, Does.Contain("AvaluoFijado"),
				"El avaluo lo fija la municipalidad y viaja por su feed.");
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
