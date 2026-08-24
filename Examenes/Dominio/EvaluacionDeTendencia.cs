using System;

namespace Examenes.Dominio
{
	// El resultado de evaluar la serie de un analito: la conclusion mas los hechos con
	// los que se llego a ella.
	//
	// Devuelve los HECHOS, no una frase armada. El dominio concluye "en ascenso, de 200
	// a 280 mg/dL en 365 dias"; quien redacta la oracion es la superficie - la UI o la
	// IA. Es lo que evita el consejo de plantilla: con estos campos se puede decir "tu
	// colesterol subio de 200 a 280 en un ano" y no se puede decir "baje las grasas",
	// porque el dominio no tiene de donde sacar eso.
	//
	// Carrier por identidad: sin Equals, sin GetHashCode, no es llave de nada.
	public class EvaluacionDeTendencia
	{
		public string Analito { get; private set; }
		public Tendencia Tendencia { get; private set; }
		public int CantidadDeMediciones { get; private set; }

		// Null cuando no hay ninguna medicion.
		public Medicion Primero { get; private set; }
		public Medicion Ultimo { get; private set; }

		public int DiasTranscurridos { get; private set; }

		// Con signo: positivo si subio.
		public decimal Cambio { get; private set; }

		// Si el cambio observado va en la direccion que preocupa PARA ESTE ANALITO y
		// alcanzo el umbral que el analito considera significativo. Lo calcula la serie,
		// que es la que tiene a mano el analito y sus umbrales.
		public bool ExigeAtencion { get; private set; }

		public EvaluacionDeTendencia(string analito, Tendencia tendencia, int cantidadDeMediciones,
			Medicion primero, Medicion ultimo, int diasTranscurridos, decimal cambio, bool exigeAtencion)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(analito);
			ArgumentNullException.ThrowIfNull(tendencia);

			Analito = analito;
			Tendencia = tendencia;
			CantidadDeMediciones = cantidadDeMediciones;
			Primero = primero;
			Ultimo = ultimo;
			DiasTranscurridos = diasTranscurridos;
			Cambio = cambio;
			ExigeAtencion = exigeAtencion;
		}

		// Cero o una medicion: no hay serie que evaluar.
		public static EvaluacionDeTendencia SinSerie(string analito, int cantidadDeMediciones, Medicion unico) =>
			new EvaluacionDeTendencia(analito, Tendencia.Indeterminada, cantidadDeMediciones,
				unico, unico, 0, 0m, false);

		// Proyecciones escalares, para que la superficie no tenga que abrir los objetos
		// ni el print tenga que navegar dos niveles.
		public string NombreDeLaTendencia => Tendencia.Nombre;
		public decimal ValorInicial => Primero == null ? 0m : Primero.Valor;
		public decimal ValorFinal => Ultimo == null ? 0m : Ultimo.Valor;
		public string Unidad => Ultimo == null ? "" : Ultimo.Unidad.Simbolo;
		public bool SugiereRepetirElExamen => Tendencia.SugiereRepetirElExamen;
		public bool HaySerie => CantidadDeMediciones >= 2;
	}
}
