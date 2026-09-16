using System;

namespace Concesionario.Dominio.Pruebas
{
	// LA CLAUSULA QUE LLEVA UNA RESPUESTA ARMADA A MANO, para las fixtures cuya ley es sobre UNA
	// variante de EnvelopeAnswer y no sobre la composicion entera.
	//
	// ANTES ERA string.Empty Y YA NO PUEDE SERLO. Desde que la respuesta manda FORMA MAS VALORES en
	// vez de una frase, el limite publicado que el motivo de la suspension por unidades nombra sale
	// de la clausula y no de la variante: con una clausula vacia esa frase diria "esta en " y nadie
	// lo veria, porque ninguna prueba afirmaba de donde venia el limite.
	//
	// NO ES UN DOBLE DEL DOMINIO: son las mismas fabricas que VehicleProfile usa, con un sobre
	// publicado de verdad. Lo unico que vive aqui son los dos numeros del escenario.
	internal static class AClauseUnderTest
	{
		internal const int ThirtySixMonths = 36;
		internal const int SixtyThousand = 60000;

		private static readonly DateTime PublishedAt = new DateTime(2023, 12, 1);

		// Sin sobre publicado: la unica clausula que se puede citar es la constancia de que no hay
		// ninguno.
		internal static AnEnvelopeClause WithNoEnvelope()
		{
			return AnEnvelopeClause.OfTheEnvelopeAlone(WarrantyEnvelope.None);
		}

		// Con un sobre publicado en esa unidad, que es de donde toda respuesta saca el limite que
		// nombra.
		internal static AnEnvelopeClause WithAnEnvelopeIn(DistanceUnit unit)
		{
			return AnEnvelopeClause.OfThePublishedHalfAlone(
				WarrantyEnvelope.PublishedAs(ThirtySixMonths, SixtyThousand, unit, PublishedAt));
		}
	}
}
