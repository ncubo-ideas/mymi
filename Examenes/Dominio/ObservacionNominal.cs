using System;

namespace Examenes.Dominio
{
	// UNA CATEGORIA OBSERVADA EN UNA FECHA: el punto de una serie nominal.
	//
	// Es la gemela de PruebaBinaria en la forma 6. Aquella dice si el hallazgo estaba o no
	// estaba; esta dice CUAL de las categorias salio, que es otra cosa: un color no es un
	// si o un no, y por eso su punto no puede ser el mismo tipo.
	//
	// POR QUE NO ES LecturaDeExamen. El argumento es el de PruebaBinaria, textual: aquella
	// dice QUE se midio dentro de UN examen, y no lleva fecha porque la fecha es del examen
	// que la contiene. Un punto de serie SI la necesita: la serie no tiene los examenes,
	// tiene los puntos.
	//
	// NO HAY GUARDA DE "REALIZADA", y no es un olvido: MagnitudNominal no tiene tercer
	// estado. Un urianalisis siempre informa un color —ExamenDeOrina.Lecturas lo emite
	// siempre—, asi que no existe el "no realizado" que en los binarios obliga a filtrar.
	// El dia en que un laboratorio informe "color: no informado", el tercer estado va en la
	// magnitud y la guarda viene aca.
	//
	// Carrier por identidad: sin Equals, sin GetHashCode, no es llave de nada.
	internal sealed class ObservacionNominal
	{
		internal DateTime Fecha { get; private set; }

		internal MagnitudNominal Resultado { get; private set; }

		internal ObservacionNominal(DateTime fecha, MagnitudNominal resultado)
		{
			ArgumentNullException.ThrowIfNull(resultado);

			Fecha = fecha;
			Resultado = resultado;
		}

		// Proyecciones planas, para que el print del DSL no navegue dos niveles y para que
		// el dibujo reciba el hecho y no el texto.
		//
		// La CATEGORIA canonica es la llave con la que se comparan dos observaciones; el
		// Expresado es el mismo dato escrito como se lee, con acentos.
		internal string Categoria => Resultado.Nombre;

		internal string Expresado => Resultado.Expresado;

		internal bool EsEsperable => Resultado.EsEsperable;

		// Para un nominal, "hay algo que no deberia estar" es que la categoria no sea de
		// las esperables. Lo contesta la magnitud, que es de quien es.
		internal bool EsHallazgo => !Resultado.EsEsperable;

		// Operacion con nombre y no un == sobrecargado, igual que Analito.EsElMismo. Las
		// categorias son un conjunto cerrado de instancias estaticas, asi que se comparan
		// por referencia.
		internal bool EsLaMisma(ObservacionNominal otra)
		{
			ArgumentNullException.ThrowIfNull(otra);

			return ReferenceEquals(Resultado, otra.Resultado);
		}
	}
}
