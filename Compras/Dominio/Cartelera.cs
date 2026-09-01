using System;
using System.Collections.Generic;

namespace Compras.Dominio
{
	/// <summary>
	/// El corcho donde el súper cuelga sus promociones. Guarda todas las que llegaron;
	/// las vigentes se preguntan contra una fecha.
	/// </summary>
	// ES SU PROPIO PUPPET Y NO UNA LISTA EN Historial, porque son cosas de dueños
	// distintos: el historial es LO QUE ELLA COMPRO -sus hechos-, y la cartelera es LO QUE
	// EL SUPER OFRECE -contenido de la institucion-. Mezclarlos habria hecho que "cuantas
	// compras tenes" o la firma tuvieran que esquivar promociones adentro de su propia
	// coleccion.
	//
	// SE GUARDAN TAMBIEN LAS VENCIDAS. El journal es append-only y esta clase rehidrata de
	// el: borrar una promocion vencida seria fingir que el evento no llego. Ademas una
	// vencida no es basura — "que me ofrecieron el mes pasado" es una pregunta legitima
	// para el asistente, aunque ninguna pantalla la muestre.
	internal class Cartelera
	{
		private readonly List<Promocion> promociones = new List<Promocion>();

		internal IReadOnlyList<Promocion> Todas => promociones;

		internal int Cantidad => promociones.Count;

		internal void Colgar(Promocion promocion)
		{
			ArgumentNullException.ThrowIfNull(promocion);
			promociones.Add(promocion);
		}

		/// <summary>
		/// Las vigentes ese día, la que vence antes primero. Total: sin ninguna devuelve
		/// la lista vacía, que es la respuesta y no un error.
		/// </summary>
		// LA QUE VENCE ANTES PRIMERO, porque es la unica urgencia legitima de un cartel:
		// "esta se te va el viernes" importa mas que una rifa que cierra a fin de mes. A
		// igual vencimiento, la que llego primero — el orden del corcho. El desempate va
		// EXPLICITO en la comparacion porque List.Sort no es estable: sin el, dos carteles
		// que vencen el mismo dia saldrian en un orden que puede cambiar entre corridas.
		internal IReadOnlyList<Promocion> VigentesEl(DateTime fecha)
		{
			var vigentes = new List<(Promocion Promocion, int Llegada)>();
			for (int i = 0; i < promociones.Count; i++)
				if (promociones[i].VigenteEl(fecha)) vigentes.Add((promociones[i], i));

			vigentes.Sort((una, otra) =>
			{
				int porVencimiento = una.Promocion.ValidaHasta.Date.CompareTo(otra.Promocion.ValidaHasta.Date);
				return porVencimiento != 0 ? porVencimiento : una.Llegada.CompareTo(otra.Llegada);
			});

			var ordenadas = new List<Promocion>();
			foreach ((Promocion promocion, _) in vigentes) ordenadas.Add(promocion);
			return ordenadas;
		}
	}
}
