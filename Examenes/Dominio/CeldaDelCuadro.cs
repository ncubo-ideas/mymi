using System;

namespace Examenes.Dominio
{
	// UNA CELDA del cuadro historico: una fila de analito por una columna de fecha.
	//
	// El cuadro es el pedido 1 del feedback de Alvaro. La lista plana de hoy repite el
	// nombre del analito en cada fila y obliga a leer salteado para reconstruir una serie:
	//
	//     Antigeno prostatico   21/08/2023   3 ng/mL
	//     Antigeno prostatico   21/08/2024   3.1 ng/mL
	//     Colesterol total      21/08/2025   200 mg/dL
	//
	// Lo que corresponde es que las FECHAS SEAN EL EJE y no un atributo de la fila, para
	// que la serie se lea de un barrido horizontal:
	//
	//                          21/08/2023  21/08/2024  21/08/2025  21/08/2026
	//     Antigeno prostatico     3 ng/mL   3.1 ng/mL   3.0 ng/mL   5.2 ng/mL
	//     Colesterol total              —           —   200 mg/dL   280 mg/dL
	//     Hemoglobina                   —           —           —    14.2 g/dL
	//
	// POR QUE ESTO ES DOMINIO Y NO ACOMODO DE PANTALLA. Armar el cuadro obliga a decidir
	// tres cosas que no son de presentacion:
	//
	//   - QUE ES UNA COLUMNA. Una columna es un DIA, no una marca de tiempo. En el guion
	//     el colesterol entra 08:30 y el antigeno 08:31 del mismo dia: son la misma visita
	//     al laboratorio. Sin colapsar por dia, ocho examenes dan ocho columnas y el
	//     cuadro sale en escalera, con una sola celda por columna, que es exactamente la
	//     lista plana disfrazada de tabla.
	//
	//   - QUE SIGNIFICA UNA CELDA VACIA. Que ese analito NO se midio ese dia. No se
	//     interpola ni se arrastra el valor anterior: inventar un punto que nadie midio es
	//     lo peor que puede hacer un dominio cuya razon de ser es leer series.
	//
	//   - EN QUE ORDEN VAN LAS FILAS. Se decide una vez, aca, y no en cada superficie.
	//
	// Viaja PLANA y con sus dos indices porque el print del motor proyecta un solo nivel:
	// una coleccion de filas que a su vez tienen celdas no cruza. Los indices son los que
	// permiten que la superficie rearme la grilla SIN volver a decidir nada.
	internal sealed class CeldaDelCuadro
	{
		internal int Fila { get; private set; }

		internal int Columna { get; private set; }

		// La llave de la fila. Para los de sangre es el nombre canonico del analito, que
		// es con el que se pide la interpretacion; para los demas, el nombre del examen.
		internal string Analito { get; private set; }

		internal string ParaMostrar { get; private set; }

		// La fecha de la COLUMNA, ya sin hora. Se repite en todas las celdas de la misma
		// columna a proposito: es lo que le permite a la superficie reconstruir el
		// encabezado sin una segunda consulta.
		internal DateTime Fecha { get; private set; }

		// El resultado ya dicho, con unidad. Es ResultadoExpresado y no un decimal por la
		// misma razon que la lista: un colesterol ("280 mg/dL") y una orina ("ambar")
		// conviven en el mismo cuadro sin que ninguno tenga que mentir sobre su forma.
		internal string Expresado { get; private set; }

		// NO HAY LECTURA COLGADA ACA, y es una decision. Hubo tres campos mas —si la fila
		// formaba serie, su severidad y su frase— que existian para las filas SIN serie, o
		// sea para los colores. Desde que los nominales forman la suya no queda ninguna
		// fila sin serie: TODA lectura se pide por el analito, a InterpretarSerieDe, y la
		// celda vuelve a ser lo que el cuadro dice que es —una fecha y un resultado—.
		//
		// Dejarlos habria sido peor que redundante: la magnitud sola contesta lo mismo
		// siempre y la serie sabe bajarlo cuando el hallazgo no se repitio, asi que la
		// pantalla tendria dos respuestas distintas para la misma pregunta.
		internal CeldaDelCuadro(int fila, int columna, string analito, string paraMostrar,
			DateTime fecha, string expresado)
		{
			if (fila < 0)
				throw new ArgumentOutOfRangeException(nameof(fila), fila, "La fila no puede ser negativa.");
			if (columna < 0)
				throw new ArgumentOutOfRangeException(nameof(columna), columna, "La columna no puede ser negativa.");
			ArgumentNullException.ThrowIfNullOrWhiteSpace(analito);

			Fila = fila;
			Columna = columna;
			Analito = analito;
			ParaMostrar = paraMostrar;
			Fecha = fecha;
			Expresado = expresado;
		}
	}
}
