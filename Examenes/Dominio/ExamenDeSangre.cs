using System;
using System.Collections.Generic;

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
	internal abstract class ExamenDeSangre : Examen
	{
		internal Medicion Valor { get; private set; }

		// QUE se midio, como identidad. Es la llave de la serie: dos examenes del mismo
		// analito forman serie aunque cada laboratorio lo haya nombrado distinto.
		//
		// Entra por constructor y no es abstracta. Antes cada subclase declaraba un
		// `override string Analito` con una constante, y la subclase repetia ademas su
		// UnidadDeMedida al armar la medicion: dos hechos sobre el mismo analito escritos
		// en dos lugares, que podian contradecirse. Ahora los dice Analito, una vez.
		// TIPADO COMO CONTINUO, no como Analito a secas. Un examen de sangre mide un numero
		// con unidad, y necesita la unidad para validar el evento y el rango posible para
		// rechazar basura. El color de la orina es un analito y no tiene ninguna de las
		// dos: por eso Analito dejo de exigirlas y ellas bajaron a la forma que si las usa.
		internal AnalitoContinuo Analito { get; private set; }

		protected ExamenDeSangre(DateTime fecha, AnalitoContinuo analito, Medicion valor) : base(fecha)
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
		protected static Medicion Medida(decimal valor, string simboloRecibido, AnalitoContinuo analito)
		{
			ArgumentNullException.ThrowIfNull(analito);

			UnidadDeMedida recibida = UnidadDeMedida.PorSimbolo(simboloRecibido);
			if (!recibida.EsLaMisma(analito.Unidad))
				throw new ArgumentException(
					$"{analito.Nombre} se mide en {analito.Unidad.Simbolo}, y el evento lo trajo en {recibida.Simbolo}.",
					nameof(simboloRecibido));

			// Y la otra guarda de la frontera: un valor que ninguna persona viva podria
			// tener no es un hallazgo, es un dato malformado. Revienta aca para que no
			// entre nunca al expediente; la ruta lo atrapa antes y descarta el evento.
			if (!analito.EsValorPosible(valor))
				throw new ArgumentOutOfRangeException(nameof(valor), valor,
					$"{valor} {analito.Unidad.Simbolo} no es un valor posible de "
					+ $"{analito.Nombre}: lo posible va de {analito.ValorMinimoPosible} a "
					+ $"{analito.ValorMaximoPosible}.");

			return new Medicion(valor, analito.Unidad);
		}

		internal override bool AportaSerieContinua => true;

		// Un examen de sangre mide UN analito. Es el caso simple, y es justamente por eso
		// que la forma singular parecia alcanzar para todos.
		internal override IReadOnlyList<LecturaDeExamen> Lecturas =>
			new[] { new LecturaDeExamen(Analito, Valor) };

		// El rango normal PARA ESTA PERSONA. Es una funcion de la persona, no una
		// constante: eso es lo que significa "los rangos van en el codigo del dominio".
		//
		// RECIBE AL PACIENTE Y NO LA EDAD, y ese es el segundo de los tres efectos del
		// sexo: hay analitos que aplican igual y se comparan contra otros umbrales —la
		// hemoglobina, el HDL—. Mientras la edad era el unico parametro clinico, un int
		// alcanzaba; con dos, pasar la lista de atributos seria arrastrar una tupla sin
		// nombre. Ver Paciente.
		//
		// DEVUELVE NULL cuando no se puede determinar, y eso es deliberado: devolver el
		// rango masculino "mientras tanto" es PEOR que no devolver nada, porque produce una
		// clasificacion que se ve igual de firme que las demas y esta mal. Quien lo llame
		// tiene que decidir que hacer con el hueco; lo que el dominio hace es decirlo.
		internal abstract RangoDeReferencia RangoPara(Paciente paciente);

		// Cuanto puede moverse este analito por dia sin que el cambio sea biologicamente
		// inverosimil. Es el umbral que convierte "subio 80" en "subio 80 EN UN DIA, o
		// sea que alguien midio mal".
		internal abstract decimal CambioMaximoVerosimilPorDia { get; }

		// El cambio minimo que este analito considera un cambio DE VERDAD. Por debajo de
		// esto la diferencia no se distingue de la variacion normal del propio analito
		// mas la imprecision del metodo, asi que llamarla tendencia seria inventar.
		//
		// Antes se llamaba CambioQueExigeAtencion, y el nombre conflacionaba dos cosas
		// que este cambio separa: que algo se haya movido de verdad, y que ese movimiento
		// exija algo. Un colesterol que baja 60 mg/dL se movio de verdad Y no exige nada.
		internal abstract decimal CambioMinimoSignificativo { get; }

		// HACIA DONDE conviene que se mueva este analito. Reemplaza al bool
		// DireccionPreocupanteEsAscenso, que solo sabia contestar "asciende o desciende"
		// y no tenia como decir que lo bueno es quedarse quieto.
		internal abstract DireccionDeseable Direccion { get; }

		// QUE SERIE LEE A ESTE ANALITO. Es la fabrica de "el plural": cada analito dice
		// cual es su serie, y con eso el Expediente arma la correcta sin un switch.
		//
		// Interpretar() ESTABA ACA y se fue a la serie. La razon la dio Alvaro: "eso no es
		// lo mismo que el conjunto de examenes de antigeno prostatico evaluando su
		// tendencia". Una medicion sola no puede interpretar una serie; lo hacia recibiendo
		// la serie por parametro, que es la senal de que el metodo no era suyo.
		// Devuelve la rama CONTINUA y no la raiz: un examen de sangre no puede producir
		// una serie binaria, y decirlo en la firma evita tener que comprobarlo despues.
		internal abstract SerieContinua NuevaSerie();

		// EL MULTIPLO QUE NINGUN INTERVALO JUSTIFICA. Es la otra mitad de lo que Alvaro
		// planteo, y es distinta del rango posible:
		//
		//   un valor imposible   -> "esto no es de una persona viva"
		//   un salto imposible   -> los dos valores pueden ser posibles, y aun asi uno de
		//                           los dos esta mal
		//
		// Alvaro: "si yo ahi pase de 1 a 10 — no senor. Ese brinco no es racional en este
		// examen." Un PSA de 10 existe y es real; llegar a 10 desde 1 sin que nada se
		// viera en el medio, no. Y esto la regla de la tasa no lo puede decir: con
		// suficientes dias entre mediciones, cualquier salto pasa.
		//
		// Se expresa como MULTIPLO y no como diferencia absoluta porque es proporcional:
		// duplicarse desde 1 y duplicarse desde 100 son el mismo hecho clinico.
		internal abstract decimal MultiploQueNingunIntervaloJustifica { get; }

		// CADA CUANTO conviene repetir ESTE analito, en meses.
		//
		// No es una constante: depende de la edad y, en varios analitos, de si el ultimo
		// valor salio en rango. Un colesterol normal se revisa cada varios anos; uno alto,
		// todos los anos. Por eso es un metodo y no un campo.
		//
		// Es lo que permite contestar QUE FALTA, que es distinto de todo lo demas que sabe
		// este dominio: el resto interpreta lo que hay, esto mira lo que NO esta.
		//
		// ⚠️ Las periodicidades son de uso corriente y ORIENTATIVAS. Varian por
		// antecedentes y criterio medico, y conviene que alguien del area las revise antes
		// de mostrarlas fuera del equipo.
		internal abstract int MesesHastaElProximo(Paciente paciente);

		// Proyecciones planas para el print del DSL, que no navega dos niveles. Es lo que
		// permite que un grafico reciba NUMEROS y no el texto "3.0 ng/mL".
		internal decimal ValorNumerico => Valor.Valor;

		internal string UnidadDelValor => Valor.Unidad.Simbolo;

		internal ClasificacionDeValor ClasificarPara(Paciente paciente) =>
			Exigido(paciente).Clasificar(Valor);

		// Si el valor se movio lo suficiente como para llamarlo cambio. Vive en la base
		// porque la comparacion es la misma para todos; el UMBRAL, que es lo que cambia
		// por analito, lo pone cada uno.
		internal bool EsCambioReal(decimal cambio) =>
			Math.Abs(cambio) >= CambioMinimoSignificativo;

		internal bool EsNormalPara(Paciente paciente) => Exigido(paciente).Contiene(Valor);

		// Si de esta persona se sabe lo suficiente como para que este analito tenga rango.
		// Lo pregunta quien no puede fallar —el conteo de hallazgos, la serie que decide si
		// hay veredicto— antes de comparar contra nada.
		internal bool HayRangoPara(Paciente paciente) => RangoPara(paciente) != null;

		// El rango, exigiendolo. Las dos operaciones que comparan contra el rango solo se
		// alcanzan DESPUES de que la serie descarto el caso "no se puede determinar" (ver
		// SerieDeAnalito.Interpretar), asi que llegar aca sin rango es un defecto de quien
		// pregunta y no un estado del paciente: revienta con la causa en vez de inventar un
		// rango por defecto.
		private RangoDeReferencia Exigido(Paciente paciente)
		{
			RangoDeReferencia rango = RangoPara(paciente);
			if (rango != null) return rango;

			throw new InvalidOperationException(
				$"El rango de {Analito.Nombre} depende del sexo, y de esta persona no se sabe. "
				+ "Hay que preguntar HayRangoPara antes de comparar contra el rango.");
		}
	}
}
