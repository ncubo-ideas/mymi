using System;

namespace Examenes.Dominio
{
	// CUANTO HACE que no se mide un analito, y si eso ya es demasiado para EL.
	//
	// Es la unica pregunta de este dominio que mira lo que NO ESTA. Todo lo demas
	// interpreta lo que hay: la serie, la tendencia, la velocidad, el veredicto. Esto
	// mira el hueco.
	//
	// POR QUE IMPORTA QUE SEA LO QUE NO ESTA. Una pantalla solo puede mostrar datos que
	// tiene. Puede ordenarlos, colorearlos, ponerlos en una matriz — pero no puede
	// renderizar una ausencia que nadie le paso. "Hace cuatro anos que no te medis el
	// colesterol" no es un dato del expediente: es una conclusion sobre el expediente
	// mas el calendario mas lo que cada analito considera su periodicidad.
	//
	// Ese es el criterio de Alvaro para las tools de la IA: si la IA contesta algo que ya
	// esta en una pantalla, el demo no probo nada. La tool que habia -revisar_mis_analitos-
	// recorria los analitos y devolvia la tendencia de cada uno, o sea exactamente lo que
	// la app renderiza. Su propia descripcion tenia que aclarar que "no es la lista que la
	// app ya le muestra", que es la senal de que si lo era.
	//
	// SOBRE LA FECHA. Llega como PARAMETRO y no se lee del reloj, y eso no es ceremonia:
	// un dominio que llama a DateTime.Now deja de ser reproducible, y el mismo journal
	// reproducido el ano que viene daria otra respuesta. Que sea un argumento explicito lo
	// mantiene como una funcion de sus entradas. Quien tiene derecho a saber que dia es
	// hoy es la superficie -el servidor MCP-, no el modelo.
	internal sealed class ControlPendiente
	{
		internal Analito Analito { get; private set; }

		// Cuando fue la ultima vez que se midio. Siempre hay una: un analito sin ninguna
		// medicion no tiene serie, y sin serie no hay control que este pendiente — no se
		// puede decir "te toca repetirlo" de algo que nunca se hizo.
		internal DateTime UltimaMedicion { get; private set; }

		internal int DiasDesdeLaUltima { get; private set; }

		// Lo que ESTE analito considera su periodicidad, ya resuelta contra su ultimo
		// valor y la edad. El colesterol contesta 60 o 12 segun donde quedo.
		internal int MesesRecomendados { get; private set; }

		internal ControlPendiente(Analito analito, DateTime ultimaMedicion, int diasDesdeLaUltima,
			int mesesRecomendados)
		{
			ArgumentNullException.ThrowIfNull(analito);
			if (mesesRecomendados <= 0)
				throw new ArgumentOutOfRangeException(nameof(mesesRecomendados), mesesRecomendados,
					"Un analito que se revisa cada cero meses no es una periodicidad.");

			Analito = analito;
			UltimaMedicion = ultimaMedicion;
			DiasDesdeLaUltima = diasDesdeLaUltima;
			MesesRecomendados = mesesRecomendados;
		}

		// Se usa el mes de 30 dias y no el calendario real a proposito: la periodicidad es
		// orientativa -"cada un ano", no "el 21 de agosto"- y un mes fijo hace que la
		// respuesta no dependa de en que mes cae la ultima medicion.
		private const int DiasDelMes = 30;

		internal int DiasRecomendados => MesesRecomendados * DiasDelMes;

		internal bool EstaVencido => DiasDesdeLaUltima > DiasRecomendados;

		// Cuanto falta, o cuanto hace que se paso. Con signo: negativo si ya vencio.
		internal int DiasQueFaltan => DiasRecomendados - DiasDesdeLaUltima;

		// Que tan urgente es ponerse al dia. Un control vencido no es un hallazgo clinico
		// -no dice que haya nada mal- pero tampoco es informativo: es algo que hacer.
		//
		// Al DOBLE del intervalo escala, porque ahi ya no es "se me paso", es que el
		// analito dejo de tener serie util: entre dos puntos demasiado separados no se
		// puede leer una tendencia.
		internal Severidad Severidad
		{
			get
			{
				if (!EstaVencido) return Severidad.Informativo;
				return DiasDesdeLaUltima > DiasRecomendados * 2
					? Severidad.ConsultePronto
					: Severidad.Vigilar;
			}
		}

		// Que hacer con el control vencido, con su plazo. Apenas vencido puede esperar
		// al mes; al doble del intervalo la serie dejo de ser util y ya no conviene
		// seguir empujando la fecha.
		internal Accion Accion
		{
			get
			{
				if (!EstaVencido) return null;
				return DiasDesdeLaUltima > DiasRecomendados * 2
					? Examenes.Dominio.Accion.RepetirElExamen(Plazo.EstaSemana)
					: Examenes.Dominio.Accion.RepetirElExamen(Plazo.EsteMes);
			}
		}

		// Proyecciones planas, para que el print del DSL no tenga que navegar dos niveles.
		internal string Nombre => Analito.Nombre;
		/// <summary>El codigo del analito dentro de una clave de frase.</summary>
		internal string ClaveDelAnalito => Analito.Clave;
		internal string NombreDeLaSeveridad => Severidad.Nombre;
		/// <summary>El codigo de que hacer, vacio cuando no hay nada que hacer.</summary>
		internal string ClaveDeLaAccion => Accion == null ? "" : Accion.Clave;

		/// <summary>El codigo de cuando, vacio cuando no hay nada que hacer.</summary>
		internal string ClaveDelPlazo => Accion == null ? "" : Accion.ClaveDelPlazo;

		/// <summary>LA FRASE QUE ESTE CONTROL ELIGE, por su nombre.</summary>
		internal string ClaveDeLaFrase => EstaVencido ? "control.vencido" : "control.al-dia";

		/// <summary>
		/// CUANTO HACE, por el nombre de su clausula: en anos cuando paso mas de uno, y en dias
		/// mientras no. Cual de las dos es lo decide el dominio; escribirla es de quien redacta.
		/// </summary>
		internal string ClaveDeLoQueHace =>
			AnosDesdeLaUltima >= 1 ? "control.hace.anos" : "control.hace.dias";

		/// <summary>Cuantos anos enteros hace, cero mientras no llegue a uno.</summary>
		internal int AnosDesdeLaUltima => DiasDesdeLaUltima / DiasDelAno;

		/// <summary>
		/// CADA CUANTO CONVIENE REPETIRLO, por el nombre de su clausula: en anos cuando la
		/// periodicidad es de anos enteros —"cada 5 anos" se lee mejor que "cada 60 meses"— y en
		/// meses mientras no. El dominio sabe cual de las dos es; escribirla es de quien redacta.
		/// </summary>
		internal string ClaveDeCadaCuanto =>
			MesesRecomendados % MesesDelAno == 0 ? "control.cada.anos" : "control.cada.meses";

		/// <summary>Cada cuantos anos enteros, cero cuando la periodicidad no son anos enteros.</summary>
		internal int AnosRecomendados =>
			MesesRecomendados % MesesDelAno == 0 ? MesesRecomendados / MesesDelAno : 0;

		private const int DiasDelAno = 365;

		private const int MesesDelAno = 12;
	}
}
