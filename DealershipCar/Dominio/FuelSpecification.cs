using System;
using System.Collections.Generic;

namespace DealershipCar.Dominio
{
	/// <summary>
	/// Que combustible acepta el modelo: los grados que el fabricante publico, cada uno con su
	/// postura. No es una lista de combustibles compatibles -- son tres posturas distintas, y la
	/// tercera es la que evita un motor danado.
	///
	/// Se ARMA por acumulacion dentro de una revision, un grado por publicacion, asi que nace vacia
	/// igual que la revision que la contiene: vacia quiere decir que todavia no entro ninguno, no
	/// que no exista.
	/// </summary>
	internal sealed class FuelSpecification
	{
		private readonly List<FuelGrade> _grades = new List<FuelGrade>();

		/// <summary>
		/// Nace vacia y sin nada que validar: los grados entran despues, uno por publicacion. Es la
		/// misma forma que la revision que la contiene, que tambien nace sin contenido.
		/// </summary>
		internal FuelSpecification()
		{
		}

		/// <summary>
		/// Entra un grado publicado. Si ese mismo grado ya habia entrado en esta revision, la ultima
		/// postura es la que queda y se queda en su sitio: es una correccion antes de sellar, y la
		/// revision todavia no contesto nada.
		/// </summary>
		internal void AcceptAGrade(FuelGrade grade)
		{
			if (grade == null)
				throw new DomainException(
					"Se pidio publicar un grado de combustible sin decir cual. Publicar nada le daria "
					+ "contenido a la revision sin darle nada que contestar.");

			for (int published = 0; published < _grades.Count; published++)
				if (_grades[published].IsTheSameGradeAs(grade))
				{
					_grades[published] = grade;
					return;
				}

			_grades.Add(grade);
		}

		/// <summary>
		/// Si entro alguno. TOTAL: una especificacion de combustible sin ningun grado no es una
		/// especificacion todavia, y la revision que la tiene no publico nada sobre combustible.
		/// </summary>
		internal bool HasAGrade()
		{
			return _grades.Count > 0;
		}

		/// <summary>
		/// El grado MINIMO que se publico, o vacio si ninguno de los que entraron lleva esa
		/// postura. El vacio no se puede confundir con un grado: uno en blanco se rechaza al
		/// publicarlo, asi que ningun grado publicado se dice asi.
		///
		/// Si se publicaron dos minimos contesta el primero. Elegir entre ellos seria decidir cual
		/// de los dos quiso decir el fabricante, y eso no se decide aca.
		/// </summary>
		internal string TheMinimumGradePublished()
		{
			foreach (FuelGrade grade in _grades)
				if (grade.ItStands(FuelGradeStanding.Minimum)) return grade.Text;

			return "";
		}

		/// <summary>
		/// Los textos de los grados que llevan esa postura, en el orden en que se publicaron. TOTAL:
		/// una lista vacia dice que ninguno la lleva, y una lista no se puede confundir con un
		/// grado.
		/// </summary>
		internal string[] TheGradesPublishedAs(FuelGradeStanding standing)
		{
			List<string> texts = new List<string>();

			foreach (FuelGrade grade in _grades)
				if (grade.ItStands(standing)) texts.Add(grade.Text);

			return texts.ToArray();
		}

		/// <summary>
		/// La clausula del combustible: todos los grados publicados, cada uno con su postura, en el
		/// orden en que se publicaron. Ninguna postura se omite y ninguna se resume.
		/// </summary>
		internal string Describe()
		{
			if (!HasAGrade())
				throw new DomainException(
					"Se pidio la frase de una especificacion de combustible sin ningun grado "
					+ "adentro. Hay que preguntar primero si entro alguno.");

			List<string> dichos = new List<string>();

			foreach (FuelGrade grade in _grades) dichos.Add(grade.Describe());

			return "Grados de combustible: " + string.Join(", ", dichos) + ".";
		}
	}
}
