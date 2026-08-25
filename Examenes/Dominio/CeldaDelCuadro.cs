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
	public sealed class CeldaDelCuadro
	{
		public int Fila { get; private set; }

		public int Columna { get; private set; }

		// La llave de la fila. Para los de sangre es el nombre canonico del analito, que
		// es con el que se pide la interpretacion; para los demas, el nombre del examen.
		public string Analito { get; private set; }

		public string ParaMostrar { get; private set; }

		// La fecha de la COLUMNA, ya sin hora. Se repite en todas las celdas de la misma
		// columna a proposito: es lo que le permite a la superficie reconstruir el
		// encabezado sin una segunda consulta.
		public DateTime Fecha { get; private set; }

		// El resultado ya dicho, con unidad. Es ResultadoExpresado y no un decimal por la
		// misma razon que la lista: un colesterol ("280 mg/dL") y una orina ("ambar")
		// conviven en el mismo cuadro sin que ninguno tenga que mentir sobre su forma.
		public string Expresado { get; private set; }

		// Si esta fila forma serie. La orina entra al cuadro -tiene un resultado por
		// fecha- pero no tiene veredicto que mostrar al final, y la superficie necesita
		// saberlo para no dejar un hueco donde no falta nada.
		public bool AportaSerie { get; private set; }

		// QUE PIDE este examen, para las filas que NO forman serie. Las que si la forman
		// lo dicen por su Interpretacion, que necesita la serie entera; una orina o unas
		// heces se leen de un solo punto, asi que su lectura viaja en la celda.
		//
		// Vacio en las filas de serie, y eso no es un faltante: es que ahi la respuesta la
		// da otro. Ver SangreEnOrina y SangreEnHeces para por que dos celdas identicas en
		// forma pueden traer severidades distintas.
		public string Severidad { get; private set; }

		public string ParaSonia { get; private set; }

		public CeldaDelCuadro(int fila, int columna, string analito, string paraMostrar,
			DateTime fecha, string expresado, bool aportaSerie,
			string severidad = "", string paraSonia = "")
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
			AportaSerie = aportaSerie;
			Severidad = severidad ?? "";
			ParaSonia = paraSonia ?? "";
		}
	}
}
