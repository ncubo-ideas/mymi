using System;
using System.Collections.Generic;
using Municipalidad.App.Events;
using NUnit.Framework;
using Puppets.Portador;

namespace Municipalidad.Dominio.Pruebas
{
	// LA RUTA. Es la mitad de la frontera que E-1 no podia tener: los interpretes existian y nadie los
	// llamaba. Un interprete sin ruta es codigo que ningun feed alcanza.
	//
	// LO QUE ESTA PRUEBA IMPIDE. Una ruta que devolviera null a todo dejaria VERDE el fixture entero de
	// los descartes, porque ese solo afirma nulls. Aca se afirma lo contrario: que cada tipo que esta app
	// consume llega a su comando, y que llega COMO LO QUE ES y no como el tipo de al lado.
	//
	// Y EL ORDEN DE LA RUTA NO DECIDE NADA, que es una propiedad y no una casualidad: cada interprete
	// compara su propio tag antes de mirar el payload, y los seis tags son distintos. Eso se afirma
	// aparte, porque si dejara de ser cierto la ruta pasaria a depender del orden en que estan escritas
	// las seis lineas -- y nada mas en la suite lo veria.
	[TestFixture]
	public sealed class TheRouteAnswersWithTheFactTheSignalSaysItIs
	{
		[Test]
		public void EveryTypeThisAppConsumesReachesItsOwnFact()
		{
			HashSet<string> reconocidos = new HashSet<string>();

			Assert.Multiple(() =>
			{
				foreach (SenalDeEvento senal in UnaNarrativaDeLaMunicipalidad.LaNarrativaEntera())
				{
					IMensajeDeEvento hecho = MunicipalidadEventRoute.Route(senal);

					Assert.That(hecho, Is.Not.Null,
						"La ruta no reconoce " + senal.Tipo + ", asi que un feed que lo mande lo "
						+ "descartaria en silencio.");

					Assert.That(hecho.Tipo, Is.EqualTo(senal.Tipo),
						"Y lo reconoce como lo que es, no como el tipo de al lado.");

					Assert.That(hecho.Evento, Is.EqualTo(senal.Numero));

					reconocidos.Add(senal.Tipo);
				}
			});

			Assert.That(reconocidos.Count, Is.EqualTo(6),
				"Son SEIS tipos, uno por comando de la puerta que la municipalidad manda. Si esta cuenta "
				+ "baja, la narrativa dejo de traer uno de cada tipo y el barrido mide menos de lo que "
				+ "dice.");
		}

		// A LO SUMO UNO CONTESTA, y eso es lo que hace que el orden de las seis lineas de la ruta sea
		// indiferente. Se mide sobre los seis interpretes directamente y no a traves de la ruta: la ruta
		// devuelve el PRIMERO que contesta, asi que preguntarle a ella no puede ver un segundo.
		[Test]
		public void ExactlyOneInterpreterAnswersEachSignalSoTheRoutesOrderDecidesNothing()
		{
			Assert.Multiple(() =>
			{
				foreach (SenalDeEvento senal in UnaNarrativaDeLaMunicipalidad.LaNarrativaEntera())
				{
					int contestaron = 0;

					foreach (Type tipo in UnaNarrativaDeLaMunicipalidad.LosTiposDeMensaje())
						if (UnaNarrativaDeLaMunicipalidad.Interpretar(tipo, senal) != null)
							contestaron++;

					Assert.That(contestaron, Is.EqualTo(1),
						"Exactamente un interprete tiene que contestarle a " + senal.Tipo + ". Con dos, "
						+ "cual gana lo decidiria el orden de las lineas de la ruta.");
				}
			});
		}

		[Test]
		public void ATypeThisAppDoesNotConsumeIsDiscardedByTheRoute()
		{
			Assert.That(
				MunicipalidadEventRoute.Route(new SenalDeEvento(
					1, "AvisoDeCorteDeAgua", "{\"aviso\":\"corte del 12 al 13\"}")),
				Is.Null,
				"La municipalidad publica su narrativa entera y este modulo modela una rebanada: recibir "
				+ "un tipo que no consume es lo normal, no un defecto.");
		}

		// Y LOS TRES DESCARTES QUE TODO HECHO COMPARTE, VISTOS DESDE LA RUTA. El fixture de la frontera
		// ya los mide interprete por interprete; aca se mide que la ruta no los tape -- una ruta que
		// contestara algo para una senal sin payload seria una ruta que inventa un comando.
		[Test]
		public void TheRouteDiscardsWhatEveryInterpreterDiscards()
		{
			Assert.Multiple(() =>
			{
				foreach (SenalDeEvento senal in UnaNarrativaDeLaMunicipalidad.LaNarrativaEntera())
				{
					Assert.That(
						MunicipalidadEventRoute.Route(
							new SenalDeEvento(0, senal.Tipo, senal.Contenido)),
						Is.Null,
						"Un numero de evento que no avanza desde cero: " + senal.Tipo);

					Assert.That(
						MunicipalidadEventRoute.Route(new SenalDeEvento(senal.Numero, senal.Tipo, "")),
						Is.Null,
						"Sin payload no hay hecho: " + senal.Tipo);

					Assert.That(
						MunicipalidadEventRoute.Route(
							new SenalDeEvento(senal.Numero, senal.Tipo, "no soy json")),
						Is.Null,
						"Y un payload que no parsea se descarta en vez de reventar: " + senal.Tipo);
				}
			});
		}
	}
}
