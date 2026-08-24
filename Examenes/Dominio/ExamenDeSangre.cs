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

		protected ExamenDeSangre(DateTime fecha, Medicion valor) : base(fecha)
		{
			ArgumentNullException.ThrowIfNull(valor);
			Valor = valor;
		}

		// La unidad la valida la SUBCLASE, al armar el argumento del constructor base.
		// Se hace con un static y no con una propiedad abstracta consultada desde este
		// constructor a proposito: una llamada virtual desde el constructor de la base
		// corre antes de que la subclase este inicializada, y funciona solo mientras
		// nadie la haga depender de un campo. Este helper no tiene ese riesgo.
		protected static Medicion Medida(decimal valor, string simboloRecibido,
			UnidadDeMedida unidadDelAnalito, string analito)
		{
			ArgumentNullException.ThrowIfNull(unidadDelAnalito);

			UnidadDeMedida recibida = UnidadDeMedida.PorSimbolo(simboloRecibido);
			if (!recibida.EsLaMisma(unidadDelAnalito))
				throw new ArgumentException(
					$"{analito} se mide en {unidadDelAnalito.Simbolo}, y el evento lo trajo en {recibida.Simbolo}.",
					nameof(simboloRecibido));

			return new Medicion(valor, unidadDelAnalito);
		}

		public override bool AportaSerie => true;

		public override string ResultadoExpresado => Valor.Expresado;

		// El nombre del analito, que es la llave de la serie. Dos examenes del mismo
		// analito forman serie; uno de colesterol y uno de hemoglobina no.
		public abstract string Analito { get; }

		// El rango normal para una edad. Es una funcion de la edad, no una constante:
		// eso es lo que significa "los rangos por edad van en el codigo del dominio".
		public abstract RangoDeReferencia RangoPara(int edad);

		// Cuanto puede moverse este analito por dia sin que el cambio sea biologicamente
		// inverosimil. Es el umbral que convierte "subio 80" en "subio 80 EN UN DIA, o
		// sea que alguien midio mal".
		public abstract decimal CambioMaximoVerosimilPorDia { get; }

		// A partir de que cambio absoluto (en la unidad del analito) un ascenso real
		// deja de ser ruido y pasa a exigir atencion.
		public abstract decimal CambioQueExigeAtencion { get; }

		// Cual de las dos direcciones es la que preocupa en ESTE analito. Subir es malo
		// para el colesterol y para el antigeno; lo que preocupa en la hemoglobina es
		// que baje. Sin esto habria que meter un switch por analito afuera del analito,
		// que es exactamente lo que la herencia esta evitando.
		public abstract bool DireccionPreocupanteEsAscenso { get; }

		// Cuanto puede moverse este analito POR ANO antes de que la velocidad misma sea el
		// hallazgo, independientemente de si el valor absoluto quedo en rango.
		//
		// CERO significa "este analito no se interpreta por velocidad", y es una respuesta
		// legitima, no un valor faltante: la velocidad del PSA en ng/mL por ano es un
		// criterio clinico real, y "el colesterol subio 12 mg/dL por ano" no es nada que
		// nadie use. Un solo miembro y un predicado derivado de el, para que no puedan
		// contradecirse.
		public abstract decimal VelocidadQueExigeAtencionPorAno { get; }

		public bool SeLeePorVelocidad => VelocidadQueExigeAtencionPorAno > 0m;

		public ClasificacionDeValor ClasificarPara(int edad) => RangoPara(edad).Clasificar(Valor);

		public bool EsNormalPara(int edad) => RangoPara(edad).Contiene(Valor);
	}
}
