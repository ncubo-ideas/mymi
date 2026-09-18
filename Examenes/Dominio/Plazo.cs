using System;

namespace Examenes.Dominio
{
	// CUANDO. Es la mitad de la accion que la app no decia: "conviene que lo vea tu
	// medico" sin un cuando es el consejo de plantilla que el feedback rechazo.
	//
	// Conjunto CERRADO y ORDENADO, comparado por referencia como Severidad. Cerrado
	// porque un plazo inventado en una pantalla o en una llamada es prosa libre otra
	// vez; ordenado porque "esta semana" es antes que "este mes" y alguna superficie
	// va a querer decir cual corre mas.
	//
	// EL PLAZO ES UN DATO DEL DOMINIO, no un adorno: depende del analito, de la
	// magnitud del cambio y de la severidad. Los nitritos y el antigeno pueden pedir
	// los dos ConsultePronto y no correr igual: una infeccion urinaria no espera un
	// mes, un PSA que se movio si puede esperar a que consigas cita este mes.
	//
	// ⚠️ Los plazos concretos reflejan criterio de practica corriente y sirven para
	// modelar la estructura. Antes de mostrarse fuera del equipo conviene que alguien
	// con criterio clinico los revise.
	internal sealed class Plazo
	{
		// Sin apuro: el proximo control que ya ibas a tener.
		internal static readonly Plazo EnLaProximaCita = new Plazo("en-la-proxima-cita", 1);

		internal static readonly Plazo EsteMes = new Plazo("este-mes", 2);

		// El "espere una semana" de la repeticion de un examen.
		internal static readonly Plazo EstaSemana = new Plazo("esta-semana", 3);

		// No espera a que consigas cita. Es el plazo del rojo reservado.
		internal static readonly Plazo Pronto = new Plazo("pronto", 4);

		// El plazo que no es del dominio decidir: repetir un examen suelto no corre, y
		// cuando corresponde lo dice el medico. Pesa cero porque no apura nada.
		internal static readonly Plazo CuandoElMedicoLoIndique =
			new Plazo("cuando-el-medico-lo-indique", 0);

		/// <summary>
		/// EL CODIGO de este plazo: identidad, para comparar y serializar, y lo unico que cruza el
		/// wire. Como se dice lo busca con este codigo la capa que redacta, en el idioma que le
		/// pidieron.
		/// </summary>
		internal string Clave { get; private set; }

		private readonly int peso;

		private Plazo(string clave, int peso)
		{
			Clave = clave;
			this.peso = peso;
		}

		internal bool EsMasApuradoQue(Plazo otro)
		{
			ArgumentNullException.ThrowIfNull(otro);
			return peso > otro.peso;
		}
	}
}
