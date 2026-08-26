using System;

namespace Examenes.Dominio
{
	// UNA COSA MEDIDA dentro de un examen: que analito, y con que resultado.
	//
	// POR QUE HACE FALTA. Un examen no mide una cosa: mide las que mide. Uno de sangre
	// informa un analito; el urianalisis informa TRES -color, nitritos y sangre- y el de
	// heces DOS. Hasta ahora el dominio solo sabia contestar "el resultado del examen", en
	// singular, y por eso el historial mostraba esto:
	//
	//     Examen general de orina    ambar, nitritos negativo, sangre negativo
	//
	// Tres cosas medidas aplanadas en una fila y en un string, que es exactamente la
	// aplanadora que sacamos del dominio, sobreviviendo en la presentacion.
	//
	// Con esto, cada analito vuelve a ser lo que es: una fila propia, con su nombre y su
	// resultado. Es la forma "Examen -> Medicion[]" que el brief describia desde el
	// principio y que no habiamos construido.
	//
	// Carrier por identidad: sin Equals, sin GetHashCode, no es llave de nada.
	public sealed class LecturaDeExamen
	{
		public Analito Analito { get; private set; }

		// El resultado, como la magnitud que sea. Un numero con unidad, un color, un
		// binario: cada una sabe decirse, y por eso aca no hay un decimal ni un string
		// suelto.
		public Magnitud Resultado { get; private set; }

		public LecturaDeExamen(Analito analito, Magnitud resultado)
		{
			ArgumentNullException.ThrowIfNull(analito);
			ArgumentNullException.ThrowIfNull(resultado);

			Analito = analito;
			Resultado = resultado;
		}

		// Proyecciones planas, para que el print del DSL no navegue dos niveles.
		public string Nombre => Analito.Nombre;
		public string ParaMostrar => Analito.ParaMostrar;
		public string Expresado => Resultado.Expresado;
	}
}
