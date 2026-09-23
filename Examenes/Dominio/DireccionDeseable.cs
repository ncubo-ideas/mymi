using System;

namespace Examenes.Dominio
{
	// HACIA DONDE ES BUENO que se mueva este analito.
	//
	// Es lo que faltaba para que dos analitos con el MISMO SIGNO DE DELTA puedan dar
	// veredictos distintos. La app etiquetaba "EN ASCENSO" al antigeno (3 -> 5.2) y al
	// colesterol (200 -> 280) con el mismo texto y el mismo consejo, que es la "misma
	// tabla" que Alvaro rechazo.
	//
	// El caso que lo obliga, textual:
	//
	//   "Por 20 anos he tenido el colesterol super bien. Un dia me sale altisimo... y lo
	//    que deseo ver es como va bajando."
	//
	// Un colesterol de 280 que viene BAJANDO desde 320 es buena noticia, aunque siga
	// fuera de rango. Fuera de rango y malo son dos ejes distintos, y sin este concepto
	// el dominio solo tiene uno.
	//
	// El signo NO alcanza por si solo: para la hemoglobina lo que preocupa es que baje,
	// para el antigeno que suba. Antes eso era un bool en ExamenDeSangre
	// (DireccionPreocupanteEsAscenso) que solo sabia contestar "asciende o desciende", y
	// no tenia como decir que a un analito le da lo mismo, ni que depende.
	internal sealed class DireccionDeseable
	{
		// Colesterol, LDL, antigeno prostatico: mas bajo es mejor.
		internal static readonly DireccionDeseable Bajar = new DireccionDeseable("bajar", -1);

		// HDL: mas alto es mejor. Es el unico del demo, y existe justamente para que
		// "alto" no signifique "malo" en todo el dominio.
		internal static readonly DireccionDeseable Subir = new DireccionDeseable("subir", 1);

		// Hemoglobina, sodio: lo bueno es quedarse adentro del rango. Alejarse es peor
		// en cualquiera de las dos direcciones, asi que ningun signo es favorable por si
		// mismo.
		internal static readonly DireccionDeseable Mantenerse = new DireccionDeseable("mantenerse", 0);

		// Existe de verdad y hay que poder decirlo: el mismo movimiento se lee distinto
		// segun algo que el numero no trae. Ningun analito del demo la usa todavia; esta
		// nombrada para que el dia que entre no obligue a reabrir el conjunto.
		internal static readonly DireccionDeseable DependeDelContexto =
			new DireccionDeseable("depende-del-contexto", 0);

		internal string Nombre { get; private set; }

		private readonly int favorable;

		private DireccionDeseable(string nombre, int favorable)
		{
			Nombre = nombre;
			this.favorable = favorable;
		}

		// Si esta direccion puede juzgar un cambio ella sola. Mantenerse necesita saber
		// donde esta el rango, y DependeDelContexto necesita el contexto: preguntarles
		// por el signo no tiene respuesta.
		internal bool JuzgaPorElSigno =>
			ReferenceEquals(this, Bajar) || ReferenceEquals(this, Subir);

		// Si un cambio CON SIGNO va hacia donde conviene.
		//
		// Revienta cuando no puede contestar, en vez de devolver false. Es la misma
		// decision que Nitritos.EsPositivo cuando la prueba no se corrio: un false ahi
		// diria "no es favorable", que es una afirmacion que esta direccion no puede
		// hacer, y el que pregunta no tendria como notar la diferencia.
		internal bool EsFavorable(decimal cambio)
		{
			if (!JuzgaPorElSigno)
				throw new InvalidOperationException(
					$"La direccion '{Nombre}' no puede juzgar un cambio por su signo. "
					+ "Preguntale al analito, que sabe contra que rango compararlo.");

			return cambio * favorable > 0m;
		}

		// Un cambio de cero no es favorable ni desfavorable en ninguna direccion, y en
		// Mantenerse es justamente lo mejor que puede pasar.
		internal bool EsQuedarseQuieto(decimal cambio) => cambio == 0m;
	}
}
