using System;

namespace Examenes.Dominio
{
	// Un examen de sangre: mide UN analito y da un numero con unidad.
	//
	// Aca vive todo lo que un examen cuantitativo sabe de si mismo, y es bastante mas
	// que el valor:
	//
	//   - su rango de referencia PARA UNA EDAD (RangoPara), porque lo normal a los 25
	//     no es lo normal a los 60;
	//   - cuanto puede cambiar por dia sin que el cambio sea inverosimil
	//     (CambioMaximoVerosimilPorDia), que es lo que distingue un error de laboratorio
	//     de un cambio real;
	//   - a partir de que cambio hay que prestar atencion (CambioQueExigeAtencion).
	//
	// Esas tres cosas son conocimiento clinico por analito, y por eso las declara cada
	// subclase en vez de vivir en una tabla o en el payload del proveedor. Es la razon
	// de que la herencia aca no sea decorativa: si fueran todos "un examen con un
	// numero", no habria donde poner estas reglas y la serie no se podria interpretar.
	//
	// No es sealed: es la rama de la que cuelgan los analitos.
	public abstract class ExamenDeSangre : Examen
	{
		public Medicion Valor { get; private set; }

		// QUE se midio, como identidad. Es la llave de la serie: dos examenes del mismo
		// analito forman serie aunque cada laboratorio lo haya nombrado distinto.
		//
		// Entra por constructor y no es abstracta. Antes cada subclase declaraba un
		// `override string Analito` con una constante, y la subclase repetia ademas su
		// UnidadDeMedida al armar la medicion: dos hechos sobre el mismo analito escritos
		// en dos lugares, que podian contradecirse. Ahora los dice Analito, una vez.
		public Analito Analito { get; private set; }

		protected ExamenDeSangre(DateTime fecha, Analito analito, Medicion valor) : base(fecha)
		{
			ArgumentNullException.ThrowIfNull(analito);
			ArgumentNullException.ThrowIfNull(valor);
			Analito = analito;
			Valor = valor;
		}

		// La unidad la valida la SUBCLASE, al armar el argumento del constructor base.
		// Se hace con un static y no con una propiedad abstracta consultada desde este
		// constructor a proposito: una llamada virtual desde el constructor de la base
		// corre antes de que la subclase este inicializada, y funciona solo mientras
		// nadie la haga depender de un campo. Este helper no tiene ese riesgo.
		protected static Medicion Medida(decimal valor, string simboloRecibido, Analito analito)
		{
			ArgumentNullException.ThrowIfNull(analito);

			UnidadDeMedida recibida = UnidadDeMedida.PorSimbolo(simboloRecibido);
			if (!recibida.EsLaMisma(analito.Unidad))
				throw new ArgumentException(
					$"{analito.Nombre} se mide en {analito.Unidad.Simbolo}, y el evento lo trajo en {recibida.Simbolo}.",
					nameof(simboloRecibido));

			return new Medicion(valor, analito.Unidad);
		}

		public override bool AportaSerie => true;

		public override string ResultadoExpresado => Valor.Expresado;

		// El rango normal para una edad. Es una funcion de la edad, no una constante:
		// eso es lo que significa "los rangos por edad van en el codigo del dominio".
		public abstract RangoDeReferencia RangoPara(int edad);

		// Cuanto puede moverse este analito por dia sin que el cambio sea biologicamente
		// inverosimil. Es el umbral que convierte "subio 80" en "subio 80 EN UN DIA, o
		// sea que alguien midio mal".
		public abstract decimal CambioMaximoVerosimilPorDia { get; }

		// El cambio minimo que este analito considera un cambio DE VERDAD. Por debajo de
		// esto la diferencia no se distingue de la variacion normal del propio analito
		// mas la imprecision del metodo, asi que llamarla tendencia seria inventar.
		//
		// Antes se llamaba CambioQueExigeAtencion, y el nombre conflacionaba dos cosas
		// que este cambio separa: que algo se haya movido de verdad, y que ese movimiento
		// exija algo. Un colesterol que baja 60 mg/dL se movio de verdad Y no exige nada.
		public abstract decimal CambioMinimoSignificativo { get; }

		// HACIA DONDE conviene que se mueva este analito. Reemplaza al bool
		// DireccionPreocupanteEsAscenso, que solo sabia contestar "asciende o desciende"
		// y no tenia como decir que lo bueno es quedarse quieto.
		public abstract DireccionDeseable Direccion { get; }

		// LA UNICA OPERACION QUE TODOS LOS ANALITOS TIENEN EN COMUN, y la razon de que la
		// herencia aca no sea decorativa.
		//
		// No hay un algoritmo generico que reciba umbrales y devuelva el mismo veredicto
		// para todos: cada analito lee SU serie con SU criterio. Un colesterol bajando
		// desde un pico y un antigeno subiendo tienen la misma forma y conclusiones
		// opuestas, y esa diferencia es conocimiento clinico, no configuracion.
		//
		// Todo lo demas que cada analito sabe hacer -la velocidad del antigeno, el nivel
		// habitual del colesterol, el grado de anemia- vive SOLO en el analito que lo
		// tiene, y no aparece aca. Si un metodo sirve para todos, esta en el nivel
		// equivocado.
		// Recibe la EDAD por la misma razon que RangoPara: un veredicto que mira el rango
		// depende de ella, y el dominio no lee el reloj ni guarda un estado mutable para
		// esquivarlo.
		public abstract Interpretacion Interpretar(SerieDeAnalito serie, int edad);

		public ClasificacionDeValor ClasificarPara(int edad) => RangoPara(edad).Clasificar(Valor);

		// Si el valor se movio lo suficiente como para llamarlo cambio. Vive en la base
		// porque la comparacion es la misma para todos; el UMBRAL, que es lo que cambia
		// por analito, lo pone cada uno.
		public bool EsCambioReal(decimal cambio) =>
			Math.Abs(cambio) >= CambioMinimoSignificativo;

		public bool EsNormalPara(int edad) => RangoPara(edad).Contiene(Valor);
	}
}
