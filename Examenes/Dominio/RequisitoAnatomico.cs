using System;

namespace Examenes.Dominio
{
	// QUE TIENE QUE TENER LA PERSONA para que este analito signifique algo.
	//
	// EL PREDICADO CORRECTO NO ES EL SEXO, ES LA ANATOMIA. El antigeno prostatico no se
	// pide "si es hombre": se pide SI TIENE PROSTATA. Las dos frases coinciden en el demo
	// y no son la misma cosa, y modelarlo asi no cuesta nada hoy:
	//
	//   - deja el dominio abierto a los casos que un demo no necesita —prostatectomia,
	//     terapias hormonales, condiciones intersexuales— SIN cambiar la forma del modelo
	//     despues: el dia que la anatomia deje de derivarse del sexo, cambia UNA linea de
	//     Paciente y ningun analito se entera;
	//   - y hace que el analito no pregunte por el sexo, que es lo que evita que la
	//     variable se filtre a lugares donde no es lo que importa.
	//
	// Es la misma jugada que dejar las cuatro formas de Magnitud documentadas y no
	// implementadas: el modelo demuestra que las contemplo.
	//
	// Conjunto CERRADO de dos, y el primero es el caso normal: casi ningun analito
	// requiere nada. Por eso Ninguno es el default de Analito y no hay que declararlo.
	public sealed class RequisitoAnatomico
	{
		// La mayoria. Una hemoglobina se mide en cualquier persona; lo que cambia con el
		// sexo es contra que se compara, y eso es el OTRO efecto.
		public static readonly RequisitoAnatomico Ninguno = new RequisitoAnatomico("ninguno");

		public static readonly RequisitoAnatomico Prostata = new RequisitoAnatomico("prostata");

		public string Nombre { get; private set; }

		private RequisitoAnatomico(string nombre)
		{
			Nombre = nombre;
		}

		// Si esta persona lo cumple. Devuelve los TRES estados y no un bool porque el dato
		// del que se deriva tiene tres: con el sexo desconocido, "tiene prostata" no es
		// falso, es indeterminado.
		public Aplicabilidad En(Paciente paciente)
		{
			ArgumentNullException.ThrowIfNull(paciente);

			if (ReferenceEquals(this, Ninguno)) return Aplicabilidad.Aplica;

			return paciente.TieneProstata;
		}

		public override string ToString() => Nombre;
	}
}
