using System;

namespace Examenes.Dominio
{
	// UNA BANDA DEL HISTOGRAMA: en cuantas mediciones cayo esta persona dentro de un
	// tramo de valores.
	//
	// Alvaro pidio el histograma junto con el comparativo interanual, y aclaro que es un
	// COMPUTO y no un dibujo: "no son prints de la tabla, son computos". El dominio
	// devuelve las bandas con su cuenta; que se dibujen como barras es de la pantalla.
	//
	// PARA QUE SIRVE en este analito. Un antigeno se lee contra la linea base de la
	// persona, y el histograma es esa linea base VISTA: si cuatro de cinco mediciones
	// caen en la misma banda, ese es su lugar, y el que se salio se ve solo. Es la misma
	// idea que la mediana de NivelHabitual, mostrada en vez de resumida.
	public sealed class BandaDelHistograma
	{
		public Medicion Desde { get; private set; }

		public Medicion Hasta { get; private set; }

		public int Cuantas { get; private set; }

		// Si en esta banda cae la ultima medicion. Es lo que le permite a la pantalla
		// senalar donde esta parada la persona hoy sin recalcular nada.
		public bool CaeLaUltima { get; private set; }

		public BandaDelHistograma(Medicion desde, Medicion hasta, int cuantas, bool caeLaUltima)
		{
			ArgumentNullException.ThrowIfNull(desde);
			ArgumentNullException.ThrowIfNull(hasta);

			Desde = desde;
			Hasta = hasta;
			Cuantas = cuantas;
			CaeLaUltima = caeLaUltima;
		}

		public bool EstaVacia => Cuantas == 0;

		public string Expresado => $"{Desde.Valor}–{Hasta.Valor} {Desde.Unidad.Simbolo}: {Cuantas}";
	}
}
