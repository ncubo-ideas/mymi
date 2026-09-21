using System;

namespace Examenes.Dominio
{
	// Que le paso a un analito A LO LARGO DEL TIEMPO. Es la lectura que no se puede
	// hacer con el ultimo valor solo, y es la razon de ser de todo este dominio.
	//
	// La diferencia entre estos casos NO esta en el numero sino en el numero MAS el
	// tiempo transcurrido:
	//
	//   colesterol 200 -> 280 en un dia  = SaltoInverosimil (el laboratorio se equivoco)
	//   colesterol 200 -> 280 en un ano  = EnAscenso        (cambio la vida del paciente)
	//   antigeno 3.0 -> 3.1 en dos anos  = Estable          (normal)
	//   antigeno 3.0 -> 5.2 en un ano    = EnAscenso, y para este analito el ascenso es
	//                                      la direccion que preocupa
	//
	// Ninguna pantalla puede mostrar esto, porque no es un dato: es una conclusion
	// sobre una serie. Y no puede vivir en los datos del proveedor, porque depende de
	// reglas por analito que son del negocio.
	//
	// Esta clase dice DIRECCION y SIGNIFICANCIA, que son hechos neutros. Si esa
	// direccion es la preocupante NO se decide aca: subir es malo para el colesterol y
	// para el antigeno, pero lo que preocupa en la hemoglobina es que BAJE. Eso lo
	// contesta cada analito (ExamenDeSangre.DireccionPreocupanteEsAscenso) y lo resuelve
	// la serie al evaluar.
	internal sealed class Tendencia
	{
		// Menos de dos mediciones: no hay serie, y decir "estable" seria mentir.
		internal static readonly Tendencia Indeterminada = new Tendencia("indeterminada");

		// Hubo cambio, pero no llego al umbral que este analito considera significativo.
		internal static readonly Tendencia Estable = new Tendencia("estable");

		internal static readonly Tendencia EnAscenso = new Tendencia("en-ascenso");
		internal static readonly Tendencia EnDescenso = new Tendencia("en-descenso");

		// El cambio es demasiado grande para el tiempo transcurrido: biologicamente no
		// pasa. Lo que corresponde no es alarmarse, es repetir el examen.
		internal static readonly Tendencia SaltoInverosimil = new Tendencia("salto-inverosimil");

		// ESTE ANALITO NO ASCIENDE NI DESCIENDE. Es la respuesta para un binario: los
		// nitritos no vienen subiendo, aparecen o no aparecen, y eso lo dice Presencia.
		//
		// No es Indeterminada. Aquella significa "faltan mediciones" y se arregla
		// midiendo; esta significa que la pregunta no le corresponde a este analito, y no
		// se arregla con nada. Confundirlas le diria a la persona que le falta un examen
		// que no existe.
		internal static readonly Tendencia NoAplica = new Tendencia("no-aplica");

		/// <summary>
		/// EL CODIGO de esta tendencia: identidad, y lo unico que cruza el wire. Como se dice lo busca con este
		/// codigo la capa que redacta, en el idioma que le pidieron.
		/// </summary>
		internal string Nombre { get; private set; }

		private Tendencia(string nombre)
		{
			Nombre = nombre;
		}

		internal bool EsAscenso => ReferenceEquals(this, EnAscenso);

		internal bool EsDescenso => ReferenceEquals(this, EnDescenso);

		internal bool SugiereRepetirElExamen => ReferenceEquals(this, SaltoInverosimil);
	}
}
