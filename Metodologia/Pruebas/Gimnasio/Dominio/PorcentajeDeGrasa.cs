using System;

namespace Gimnasio.Dominio
{
	// EL PORCENTAJE DE GRASA CORPORAL.
	//
	// Lo propio: su rango saludable SE MUEVE CON LA EDAD, y es distinto entre hombres y
	// mujeres. Es la unica medida cuya referencia depende de dos atributos del perfil.
	//
	// HUECO 2: el Preguntas.md dice que el rango se mueve con la edad, pero nadie dijo
	// COMO SE AGRUPAN LAS EDADES. Hoy la clave lleva la edad exacta, lo que significa
	// que haria falta una referencia por cada ano de vida. Es impracticable y esta a la
	// vista a proposito: que la clave se vea fea es lo que obliga a decidir el tramo.
	internal sealed class PorcentajeDeGrasa : Medida
	{
		internal PorcentajeDeGrasa(decimal porcentaje) : base(porcentaje)
		{
			if (porcentaje >= 100m)
				throw new ArgumentOutOfRangeException(nameof(porcentaje), porcentaje,
					"Un porcentaje de grasa corporal de 100 o mas no existe.");
		}

		internal override string Nombre { get { return "grasa"; } }

		internal override string ClaveDeReferencia(Perfil perfil)
		{
			ArgumentNullException.ThrowIfNull(perfil);
			return $"grasa:{perfil.Genero}:{perfil.Edad}";
		}
	}
}
