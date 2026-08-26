using System;

namespace Examenes.Dominio
{
	/// <summary>
	/// Lo que un examen INFORMA. Es la raiz de todo resultado de laboratorio.
	/// </summary>
	///
	/// Existe porque la dicotomia "cuantitativo vs cualitativo" es falsa. Un laboratorio
	/// reporta en SEIS formas distintas, y el urianalisis solo, por si mismo, usa cuatro:
	///
	///   1. Continua CON unidad   colesterol mg/dL, PSA ng/mL       ordena si, aritmetica si
	///   2. Continua SIN unidad   pH, densidad relativa             ordena si, aritmetica con cuidado
	///   3. Ordinal               cruces de la tira reactiva        ordena si, aritmetica NO
	///   4. Conteo                eritrocitos por campo             ordena si, aritmetica si
	///   5. Binaria               nitritos, serologias              no ordena, no aritmetica
	///   6. Nominal               color, aspecto, olor              no ordena, no aritmetica
	///
	/// ALCANCE de este demo (brief §6.7). Estan IMPLEMENTADAS la 1 (<see cref="Medicion"/>),
	/// la 5 (<see cref="Nitritos"/>) y la 6 (<see cref="ColorDeOrina"/>). Las formas 2, 3 y 4
	/// quedan IDENTIFICADAS y sin construir a proposito: el encargo fue modelar dos o tres
	/// analitos de verdad, no un sistema de laboratorio clinico. Que esten nombradas aca y
	/// no construidas es lo que hace que el modelo se lea rico sin inflar el alcance.
	///
	/// Lo que la raiz declara es deliberadamente POCO: solo <see cref="Expresado"/>. No
	/// declara un valor numerico, porque un color no tiene numero; no declara orden, porque
	/// un binario no se ordena. Cada capacidad aparece en la rama que de verdad la tiene, y
	/// esa es toda la razon de que la jerarquia exista. Si la raiz hubiera declarado
	/// <c>decimal Valor</c>, cuatro de las seis formas tendrian que devolver cero y mentir.
	///
	/// No es sealed: existe para que se herede (E43 — las hojas si se sellan).
	public abstract class Magnitud
	{
		/// <summary>
		/// El resultado ya dicho, completo. "280.0 mg/dL", "ambar", "negativo".
		/// </summary>
		/// No es un volcado de estado ni un contrato para parsear: es la forma en que el
		/// dominio DICE un resultado, que para un valor medido incluye la unidad y para un
		/// binario incluye el vocabulario de su analito.
		public abstract string Expresado { get; }
	}

	/// <summary>
	/// Forma 1 y 2 — una cantidad continua: se ordena y admite aritmetica.
	/// </summary>
	///
	/// Es la unica rama sobre la que tienen sentido una SERIE y una VELOCIDAD. Restar dos
	/// colesteroles significa algo; restar "3+" menos "trazas" no significa nada, y restar
	/// "ambar" menos "rojizo" menos todavia. Que <see cref="SerieDeAnalito"/> solo se
	/// construya a partir de examenes que informan una magnitud continua es lo que hace
	/// cumplir esa verdad clinica sin un solo if.
	///
	/// La forma 2 (continua SIN unidad: pH, densidad relativa) colgaria de aca y no esta
	/// construida. Trae ademas su propia trampa, anotada para quien la agregue: el pH es
	/// logaritmico, asi que promediarlo o restarlo NO significa lo que parece — de 5 a 6 no
	/// cambio "1", cambio diez veces la concentracion de iones.
	public abstract class MagnitudContinua : Magnitud
	{
		/// <summary>El numero. Sin la unidad no es un dato clinico — de ahi la rama entera.</summary>
		public abstract decimal Valor { get; }
	}

	/// <summary>
	/// Forma 5 — presencia o ausencia. Y NO es un <c>bool</c>, por tres razones del negocio.
	/// </summary>
	///
	/// **El vocabulario pertenece al analito.** Los nitritos son *positivo/negativo*; una
	/// serologia es *reactivo/no reactivo*; un cultivo es *detectado/no detectado*. No es
	/// tecnicismo vacio: "reactivo" separa *la prueba reacciono* de *el paciente tiene la
	/// enfermedad*, que es una distincion real. Con un bool ese vocabulario termina en la UI
	/// y deja de ser dominio.
	///
	/// **La polaridad no es obvia.** En Rh, positivo es lo mas comun y no es un hallazgo. En
	/// nitritos, positivo si lo es. Un bool no distingue PRESENCIA de ANORMALIDAD, y esa es
	/// justo la logica que evita el consejo ciego.
	///
	/// **Falta un tercer estado.** "No realizado" no es lo mismo que negativo, y en
	/// serologias "indeterminado" existe de verdad. Con un bool esos casos caen en null y
	/// ahi se perdio el significado.
	///
	/// No es un caso particular de la ordinal: las cruces tienen una escala detras, un
	/// binario no. No hay "mas positivo".
	public abstract class MagnitudBinaria : Magnitud
	{
		/// <summary>Si la prueba se llego a hacer. Un resultado ausente no es un resultado negativo.</summary>
		public abstract bool FueRealizada { get; }

		/// <summary>Si la prueba dio positiva. Solo tiene sentido preguntarlo cuando fue realizada.</summary>
		/// Lectura guardada, no total: preguntar el resultado de una prueba que no se hizo es
		/// un defecto de quien pregunta, no un "no" (E41). Consultar antes
		/// <see cref="FueRealizada"/>.
		public abstract bool EsPositivo { get; }

		/// <summary>Si este resultado es un HALLAZGO. Positivo y anormal no son lo mismo.</summary>
		/// En nitritos coinciden; en un factor Rh no. Por eso lo contesta cada analito y no
		/// se deduce de <see cref="EsPositivo"/> afuera.
		public abstract bool EsHallazgo { get; }

		/// <summary>QUE HACER si este hallazgo esta presente. Lo decide cada binario.</summary>
		///
		/// ES EL PEDIDO CENTRAL DEL FEEDBACK DE ALVARO, y el ejemplo con el que lo explico
		/// no deja lugar a dudas:
		///
		///   "Sangre en las heces ⟹ quizas estrenimiento. Sangre en la orina ⟹ quizas CORRA
		///    donde el medico, rapido."
		///
		/// Los dos hallazgos tienen LA MISMA FORMA: un binario positivo, fuera de lo normal.
		/// Un modelo generico les pone la misma etiqueta y la misma frase. La accion que
		/// exigen es radicalmente distinta, y esa distincion es conocimiento clinico DEL
		/// ANALITO, no una decision de pantalla.
		///
		/// Que sea abstracto y no derivado de EsHallazgo es la clave: si se dedujera, dos
		/// positivos darian siempre lo mismo y estariamos otra vez en la misma tabla.
		///
		/// Cuando no hay hallazgo la respuesta es Informativo, y eso es una respuesta: la
		/// mayoria de los resultados no piden nada.
		public abstract Severidad SeveridadDelHallazgo { get; }

		/// <summary>La lectura para la persona, escrita por el propio analito.</summary>
		/// Una linea, no un consejo: el consejo sale de la severidad. Ver Interpretacion.
		public abstract string ParaSonia { get; }

		/// <summary>QUE SERIE LEE A ESTE ANALITO. La fabrica de "el plural" de la forma 5.</summary>
		///
		/// Vive aca, y no en <see cref="Analito"/>, por la misma regla que la pone en
		/// <see cref="ExamenDeSangre"/> para la forma continua: <b>la declara la clase que
		/// carga el conocimiento clinico del analito</b>. En un continuo esa clase es el
		/// examen —ahi estan los rangos, los umbrales y la periodicidad—; en un binario es
		/// esta, donde estan el vocabulario, la polaridad y la severidad del hallazgo.
		///
		/// Con esto el Expediente arma la serie correcta sin un switch: le pregunta al
		/// resultado cual es la suya.
		public abstract SerieBinaria NuevaSerie();
	}

	/// <summary>
	/// Forma 6 — una categoria sin orden. Color, aspecto, olor.
	/// </summary>
	///
	/// No se ordena y no se le saca tendencia: preguntar si un color "viene subiendo" no es
	/// una pregunta. Es la forma del ejemplo favorito de Alvaro —el color de la orina— y la
	/// razon concreta de que <see cref="Examen"/> no declare un valor medido.
	public abstract class MagnitudNominal : Magnitud
	{
		/// <summary>Como se llama esta categoria para el paciente.</summary>
		public abstract string Nombre { get; }

		/// <summary>Si esta categoria es de las que se esperan. Un ámbar si, un rojizo no.</summary>
		public abstract bool EsEsperable { get; }

		/// <summary>QUE HACER si esta categoria no es la esperada. Lo decide cada nominal.</summary>
		///
		/// Sube a esta raiz por la misma razon que <see cref="MagnitudBinaria.SeveridadDelHallazgo"/>:
		/// es el nominal quien sabe leerse. Antes la severidad de un color la componia el
		/// EXAMEN, mezclada con la de sus binarios, y desde que los binarios tienen serie
		/// esa mezcla es incorrecta — la lectura de una sangre oculta ya no sale del examen
		/// donde aparecio, sale de su historia.
		public abstract Severidad SeveridadDelHallazgo { get; }

		/// <summary>La lectura para la persona, escrita por la propia categoria.</summary>
		public abstract string ParaSonia { get; }
	}
}
