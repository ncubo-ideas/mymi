using System;

namespace Gimnasio.Dominio
{
	// EL HECHO: el gimnasio cambio la bandera el dia D. Es lo que se journaliza.
	//
	// EL PERIODO EN MORA NO SE GUARDA: se deriva de dos cambios consecutivos. Es la
	// figura 8.4 del capitulo 8 de Fowler -el estado de una accion tiene instancias en
	// el tiempo, y la suspension es el periodo entre dos-. Guardar el periodo ademas del
	// cambio seria dos fuentes de la misma verdad, que es el mismo defecto que las
	// celdas absolutas de una pieza que se mueve (guia de dominios, E25).
	//
	// La fecha entra por parametro: la trae el evento del gimnasio. El dominio no lee el
	// reloj. Si el feed algun dia mandara solo "estado actual" sin fecha, este tipo no
	// se puede construir honestamente -es el hueco descubierto del v2, dueno: el
	// artefacto del v3-.
	internal sealed class CambioDeEstado
	{
		internal DateTime Fecha { get; }
		internal EstadoDeMembresia Estado { get; }

		internal CambioDeEstado(DateTime fecha, EstadoDeMembresia estado)
		{
			ArgumentNullException.ThrowIfNull(estado);
			Fecha = fecha.Date;
			Estado = estado;
		}
	}
}
