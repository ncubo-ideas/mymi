using System;

namespace Cargas.Dominio
{
	/// <summary>
	/// QUE TAN SERIO ES LO QUE SE VIO. Es el semaforo de la marca, el mismo vocabulario
	/// de cuatro que fijo el documento de Delfos y que este negocio hereda: el nivel de
	/// accion lo decide el dominio y la UI pinta lo que el dominio indica.
	/// </summary>
	// LA REGLA DEL ROJO ES DE DOMINIO, no de diseno: Urgente esta RESERVADO para lo que
	// de verdad no espera, y ningun veredicto de rendimiento del demo lo produce -un
	// carro que rinde menos no es una emergencia-. Si el rojo se usara para una caida de
	// rendimiento, dejaria de significar algo.
	//
	// QUE HACER no vive aca: vive en Accion, que ademas del que lleva el PLAZO. Esta
	// clase se queda con lo unico comun a todos: el orden.
	internal sealed class Severidad
	{
		// Un dato: la serie estable, o algo sin accion. Decir que hay algo que hacer con
		// esto es ruido que entrena a la persona a ignorar los avisos que si importan.
		internal static readonly Severidad Informativo = new Severidad("informativo", 0);

		// Algo para mirar, sin urgencia: una caida sostenida, un dato sospechoso.
		internal static readonly Severidad Vigilar = new Severidad("vigilar", 1);

		// Amerita moverse, y la accion que acompana dice el plazo explicito.
		internal static readonly Severidad ConsultePronto = new Severidad("consulte pronto", 2);

		// RESERVADO para lo que de verdad es urgente. Es el unico que la UI puede pintar
		// de rojo, y en este negocio hoy no lo produce nadie.
		internal static readonly Severidad Urgente = new Severidad("urgente", 3);

		// La etiqueta interna, sin acentos: con este texto se compara y se serializa.
		internal string Nombre { get; private set; }

		private readonly int peso;

		private Severidad(string nombre, int peso)
		{
			Nombre = nombre;
			this.peso = peso;
		}

		internal bool PideAlgo => peso > 0;

		internal bool EsMasUrgenteQue(Severidad otra)
		{
			ArgumentNullException.ThrowIfNull(otra);
			return peso > otra.peso;
		}

		internal static Severidad LaMayor(Severidad una, Severidad otra)
		{
			ArgumentNullException.ThrowIfNull(una);
			ArgumentNullException.ThrowIfNull(otra);
			return una.EsMasUrgenteQue(otra) ? una : otra;
		}
	}
}
