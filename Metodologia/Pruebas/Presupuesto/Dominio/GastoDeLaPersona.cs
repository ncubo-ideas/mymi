using System;

namespace Presupuesto.Dominio
{
	// EL GASTO QUE NO CAE EN NINGUNA DE LAS CLASES ANTERIORES.
	//
	// No es una clase de descarte ni un saco: es el caso HONESTO. Preguntas.md dice que la
	// reducibilidad se deduce para el consumo discrecional -el repertorio- y para todo lo
	// demas la marca la persona. Esta es esa segunda mitad.
	//
	// LO QUE SABE Y NINGUNA OTRA: que su reducibilidad la puso ALGUIEN, y quien. Un servicio
	// basico no sabe eso porque no le hace falta; este si, porque la respuesta que da no vale
	// por si misma sino por quien la firmo.
	//
	// La marca es un HECHO con procedencia Persona, no configuracion. Va al journal como
	// comando, y por eso este dominio no la puede inventar cuando falta: cuando nadie marco,
	// contesta PorDefinir, que es lo unico cierto.
	internal sealed class GastoDeLaPersona : GastoRecurrente
	{
		private readonly Periodicidad periodicidad;
		private Reducibilidad reducibilidadMarcada;

		internal GastoDeLaPersona(string nombre, Periodicidad periodicidad) : base(nombre)
		{
			this.periodicidad = periodicidad ?? throw new ArgumentNullException(nameof(periodicidad));
			reducibilidadMarcada = null;
		}

		internal override Periodicidad Periodicidad => periodicidad;

		// Sin saber que clase de compromiso es, no hay como afirmar que sea critico. Decir
		// "informativo" aca no es tibieza: es no inventar una urgencia que nadie declaro.
		internal override Severidad SeveridadSiFalta => Severidad.Informativo;

		// Mientras nadie marque, PorDefinir. Ese es el estado que la pregunta 3 necesita para
		// poder decir "sobre esto otro necesito que me digas" en vez de subestimar en silencio.
		internal override Reducibilidad Reducibilidad =>
			reducibilidadMarcada ?? Reducibilidad.PorDefinir;

		// ------------------------------------------------------------ lo suyo

		internal void LaPersonaLoMarcoComo(Reducibilidad reducibilidad)
		{
			if (reducibilidad == null) throw new ArgumentNullException(nameof(reducibilidad));
			if (reducibilidad == Reducibilidad.PorDefinir)
				throw new ArgumentException(
					"Marcar algo como 'por definir' no es marcarlo. Si la persona no sabe, no se marca.",
					nameof(reducibilidad));

			reducibilidadMarcada = reducibilidad;
		}

		internal bool AlguienLoMarco => reducibilidadMarcada != null;
	}
}
