using System;

namespace Examenes.Dominio
{
	// El resultado de evaluar la serie de un analito CONTINUO: la conclusion mas los
	// hechos con los que se llego a ella.
	//
	// Devuelve los HECHOS, no una frase armada. El dominio concluye "en ascenso, de 200
	// a 280 mg/dL en 365 dias"; quien redacta la oracion es la superficie - la UI o la
	// IA. Es lo que evita el consejo de plantilla: con estos campos se puede decir "tu
	// colesterol subio de 200 a 280 en un ano" y no se puede decir "baje las grasas",
	// porque el dominio no tiene de donde sacar eso.
	//
	// YA NO ES EL UNICO TIPO DE HECHOS. Cuelga de HechosDeLaSerie, y su hermano es
	// EvaluacionDePresencia. Esta clase quedo con lo que es de una magnitud continua
	// -primero, ultimo, dias, cambio- y de ahi para arriba solo subio lo que de verdad
	// tiene cualquier serie. Un binario no tiene "cambio de 80"; tiene "apareció".
	//
	// Carrier por identidad: sin Equals, sin GetHashCode, no es llave de nada.
	public class EvaluacionDeTendencia : HechosDeLaSerie
	{
		public Tendencia Tendencia { get; private set; }

		// Null cuando no hay ninguna medicion.
		public Medicion Primero { get; private set; }
		public Medicion Ultimo { get; private set; }

		public int DiasTranscurridos { get; private set; }

		// Con signo: positivo si subio.
		public decimal Cambio { get; private set; }

		// YA NO LLEVA "exigeAtencion". Era un juicio guardado adentro de un objeto de
		// HECHOS, y se calculaba con un bool -"la direccion preocupante es el ascenso"-
		// que no alcanzaba: a la hemoglobina lo que le conviene es quedarse quieta, y ese
		// bool la obligaba a elegir entre subir o bajar. La lectura vive ahora en
		// Interpretacion, que la produce el analito.
		public EvaluacionDeTendencia(Analito analito, Tendencia tendencia, int cantidadDeMediciones,
			Medicion primero, Medicion ultimo, int diasTranscurridos, decimal cambio)
			: base(analito, cantidadDeMediciones)
		{
			ArgumentNullException.ThrowIfNull(tendencia);

			Tendencia = tendencia;
			Primero = primero;
			Ultimo = ultimo;
			DiasTranscurridos = diasTranscurridos;
			Cambio = cambio;
		}

		// Cero o una medicion: no hay serie que evaluar.
		public static EvaluacionDeTendencia SinSerie(Analito analito, int cantidadDeMediciones, Medicion unico) =>
			new EvaluacionDeTendencia(analito, Tendencia.Indeterminada, cantidadDeMediciones,
				unico, unico, 0, 0m);

		// ESTE ANALITO NO SE LEE POR TENDENCIA. Es la respuesta para un binario, y no es
		// lo mismo que SinSerie: ahi faltan mediciones, aca la pregunta no le corresponde
		// a ese analito por mas mediciones que tenga.
		//
		// Existe porque EvaluarTendenciaDe es TOTAL -la superficie pregunta sin saber que
		// forma tiene enfrente- y contestar "indeterminada" seria decirle que le faltan
		// examenes. Es el mismo caso que Velocidad.NoAplica, que ya estaba.
		public static EvaluacionDeTendencia NoAplica(Analito analito, int cantidadDeMediciones) =>
			new EvaluacionDeTendencia(analito, Tendencia.NoAplica, cantidadDeMediciones,
				null, null, 0, 0m);

		// Proyecciones escalares, para que la superficie no tenga que abrir los objetos
		// ni el print tenga que navegar dos niveles.
		public string NombreDeLaTendencia => Tendencia.Nombre;
		public override string NombreDeLaLectura => Tendencia.Nombre;
		public decimal ValorInicial => Primero == null ? 0m : Primero.Valor;
		public decimal ValorFinal => Ultimo == null ? 0m : Ultimo.Valor;
		public string Unidad => Ultimo == null ? "" : Ultimo.Unidad.Simbolo;
		public override bool SugiereRepetirElExamen => Tendencia.SugiereRepetirElExamen;
	}
}
