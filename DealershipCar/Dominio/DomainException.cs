using System;

namespace DealershipCar.Dominio
{
	// EL DOMINIO DEFINE SU PROPIA EXCEPCION (E32, nota de sustrato). No se hereda de una base
	// del framework y no se sale a buscar una: el motor no provee ninguna, y buscarla acopla
	// el puppet a su titiritero justo en el tipo que expresa "esto nunca deberia pasar".
	//
	// UNA SOLA, y para una sola cosa: la VIOLACION DE UN CONTRATO. E32 parte los fallos en dos
	// y este slice depende de ese corte:
	//
	//   defecto de QUIEN LLAMA        -> revienta.  Un codigo de operacion que no nombra
	//                                    ningun miembro; una base declarada que no concuerda
	//                                    con los limites que vinieron (OINV-20).
	//
	//   resultado ESPERADO del negocio -> es un ESTADO, no una excepcion. Un aplazamiento
	//                                    rechazado en el tope es Deferred que no avanza, no
	//                                    un throw; que la persona llegue al limite de
	//                                    aplazamientos es parte del negocio, no un error.
	//
	// Un solo tipo de excepcion del lado del dominio es lo que hace legible ese corte. Cuatro
	// tipos del BCL lo borran: ArgumentOutOfRange no dice si el que se equivoco fue el
	// programa o la persona.
	//
	// CONTRADICCION C3, resuelta a favor de la guia: los dos modulos que ya estan en el repo
	// tiran ArgumentNullException, ArgumentOutOfRangeException, IndexOutOfRangeException y
	// KeyNotFoundException desde el dominio. Aca no. Las excepciones de argumento del BCL se
	// quedan donde les toca: en la frontera de DealershipCarAPI, cuidando el WIRE.
	//
	// Sin codigos de error, sin base de framework, sin marcador. Solo un mensaje legible por
	// una persona, en los terminos del dominio.

	/// <summary>
	/// La violacion de un contrato del dominio: algo que el negocio dice que nunca puede
	/// pasar y que, si paso, es un defecto de quien llama y no un resultado posible.
	/// Un resultado esperado del negocio -- un aplazamiento rechazado en el tope -- es un
	/// estado y no viaja por aca.
	/// </summary>
	internal sealed class DomainException : Exception
	{
		/// <summary>
		/// Levanta la violacion con el mensaje que la explica en terminos del dominio:
		/// que regla se rompio y con que valor, no que linea de codigo fallo.
		/// </summary>
		internal DomainException(string message) : base(message)
		{
		}
	}
}
