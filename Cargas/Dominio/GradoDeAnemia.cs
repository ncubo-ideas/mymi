using System;

namespace Cargas.Dominio
{
	// CUANTA anemia hay. Es una lectura que solo tiene sentido para la hemoglobina: no
	// existe el "grado de anemia" de un colesterol ni de un antigeno prostatico.
	//
	// Por eso vive en su propio conjunto cerrado y no como un ClasificacionDeValor mas.
	// "Bajo" dice que cruzo el piso del rango; esto dice CUANTO, que es lo que separa un
	// hallazgo para comentar en la proxima cita de uno que no espera.
	//
	// Los cortes son los de uso corriente en adultos y son ORIENTATIVOS: varian por sexo,
	// edad, embarazo y altitud de residencia. La altitud en particular no es hipotetica
	// para este demo -buena parte del Valle Central esta por encima de los 1.000 m- pero
	// el ajuste necesita un dato que aporta la persona y que el laboratorio no manda.
	// Queda nombrado y sin implementar, igual que las formas de Magnitud que no se
	// construyeron.
	public sealed class GradoDeAnemia
	{
		public static readonly GradoDeAnemia Ninguna = new GradoDeAnemia("ninguna", 0);
		public static readonly GradoDeAnemia Leve = new GradoDeAnemia("leve", 1);
		public static readonly GradoDeAnemia Moderada = new GradoDeAnemia("moderada", 2);
		public static readonly GradoDeAnemia Severa = new GradoDeAnemia("severa", 3);

		public string Nombre { get; private set; }

		private readonly int peso;

		private GradoDeAnemia(string nombre, int peso)
		{
			Nombre = nombre;
			this.peso = peso;
		}

		public bool HayAnemia => peso > 0;

		public bool EsPeorQue(GradoDeAnemia otro)
		{
			ArgumentNullException.ThrowIfNull(otro);
			return peso > otro.peso;
		}

		// La severidad que le corresponde a cada grado. Vive aca y no en la hemoglobina
		// para que agregar un grado obligue a decir que hacer con el.
		public Severidad Severidad
		{
			get
			{
				if (ReferenceEquals(this, Severa)) return Cargas.Dominio.Severidad.Urgente;
				if (ReferenceEquals(this, Moderada)) return Cargas.Dominio.Severidad.ConsultePronto;
				if (ReferenceEquals(this, Leve)) return Cargas.Dominio.Severidad.Vigilar;
				return Cargas.Dominio.Severidad.Informativo;
			}
		}

		// Y la accion con su plazo, por la misma razon: cada grado dice que hacer y
		// cuando, y agregar un grado obliga a decir las dos cosas.
		public Accion Accion
		{
			get
			{
				if (ReferenceEquals(this, Severa))
					return Cargas.Dominio.Accion.BuscarAtencionMedica(Plazo.Pronto);
				if (ReferenceEquals(this, Moderada))
					return Cargas.Dominio.Accion.MostrarleEstoATuMedico(Plazo.EsteMes);
				if (ReferenceEquals(this, Leve))
					return Cargas.Dominio.Accion.ConversarloConTuMedico(Plazo.EnLaProximaCita);
				return null;
			}
		}
	}
}
