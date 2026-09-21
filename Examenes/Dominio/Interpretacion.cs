using System;

namespace Examenes.Dominio
{
	// LO QUE EL ANALITO CONCLUYE SOBRE SU PROPIA SERIE. Es el tipo de retorno de
	// Interpretar(), la unica operacion que todos los analitos tienen en comun.
	//
	// Que sea el analito quien la produce, y no un algoritmo generico que recibe
	// umbrales, es la resolucion del feedback de Alvaro:
	//
	//   "No se intenta que la aplicacion responda todos los servicios como si fuera la
	//    misma tabla. Cada examen tiene tablas muy distintas."
	//
	// SOBRE EL TEXTO, Y POR QUE CAMBIA UNA POSICION ANTERIOR.
	//
	// EvaluacionDeTendencia dice, y decia bien para lo que hacia, que el dominio devuelve
	// HECHOS y no frases, y que quien redacta la oracion es la superficie. Interpretacion
	// se corre de ahi a proposito, porque el feedback pide justamente lo contrario:
	//
	//   "Ningun texto de consejo se genera en la UI. Todos vienen del analito."
	//
	// Las dos posiciones conviven porque el reparto es distinto del que parece:
	//
	//   Accion    NO se compone: es un que del conjunto cerrado de Accion mas un Plazo
	//             del conjunto cerrado de Plazo. La pantalla no puede inventar ni una
	//             accion nueva ni un consejo sin plazo.
	//   La frase  la elige el analito, con SUS hechos y SU vocabulario. Es una linea, no
	//             un consejo, y es lo unico que el analito puede decir mejor que nadie.
	//
	// Y EL ANALITO ELIGE LA FRASE, NO LA REDACTA. Elegir cual frase es suyo —sabe que
	// paso—; escribirla obligaria al dominio a tener un idioma. Lo que cruza es la CLAVE
	// de la frase entera mas los valores que van en sus huecos, y quien la escribe es una
	// capa de presentacion, en el idioma que le pidieron.
	//
	// Y siguen viajando los hechos al lado, para que la superficie o la IA puedan decir
	// otra cosa con ellos si hace falta. La interpretacion no los reemplaza.
	//
	// Carrier por identidad: sin Equals, sin GetHashCode, no es llave de nada.
	internal sealed class Interpretacion
	{
		internal Veredicto Veredicto { get; private set; }

		internal Severidad Severidad { get; private set; }

		/// <summary>
		/// LA FRASE QUE ESTE ANALITO ELIGIO, por su nombre. Es la clave de una frase ENTERA y nunca
		/// de media: el espanol concuerda en genero y el ingles no, y un catalogo que pegara
		/// fragmentos produciria concordancias rotas que ninguna comprobacion ve.
		/// </summary>
		internal string Clave { get; private set; }

		// Que hacer y cuando, elegidas por el analito del conjunto cerrado. Null cuando
		// no hay nada que hacer, que es la mayoria de las veces y es una respuesta.
		internal Accion Accion { get; private set; }

		// Los hechos con los que se llego a esto, para que la superficie pueda mostrarlos
		// sin volver a pedirlos y para que la conclusion sea auditable.
		// TIPADO A LA RAIZ, no a la evaluacion de tendencia. Una serie binaria concluye con
		// EvaluacionDePresencia y una nominal con EvaluacionDeNominal; ninguna de las dos
		// tiene valor inicial, ni final, ni cambio. Dejar el campo tipado a la continua las
		// habria obligado a mandar nulls y ceros.
		internal HechosDeLaSerie Hechos { get; private set; }

		// COMO SE DIBUJA esta lectura. Viaja con la interpretacion porque llega junta: la
		// superficie pide una vez y sabe que veredicto es Y que forma tiene.
		internal FormaDeLectura Forma { get; private set; }

		/// <summary>
		/// CONTRA QUE SE LEE el valor, por el nombre de la frase que lo dice. La escribe el analito
		/// —es quien sabe que depende de la edad, del sexo y, en la hemoglobina, de la altitud— y
		/// viaja como clave porque es texto de presentacion. Vacia cuando no hay valor que acompanar.
		/// </summary>
		internal string ClaveDeLaReferencia { get; private set; } = "";

		/// <summary>El piso que la referencia nombra, cero cuando no nombra ninguno.</summary>
		internal decimal MinimoDeLaReferencia { get; private set; }

		/// <summary>El techo que la referencia nombra, cero cuando no nombra ninguno.</summary>
		internal decimal MaximoDeLaReferencia { get; private set; }

		/// <summary>La unidad en que se dicen los dos extremos de la referencia.</summary>
		internal string UnidadDeLaReferencia { get; private set; } = "";

		// ------------------------------------------------- los valores que la frase nombra
		//
		// CUALES USA CADA FRASE LO DICE SU CLAVE, y los que no nombra quedan en cero o
		// vacios. Viven aca y no en los hechos a proposito: una serie binaria no tiene
		// "valor inicial" y una continua no tiene "positivas seguidas", asi que subirlos a
		// la raiz de los hechos obligaria a dos de las tres formas a mandar ceros — que es
		// la misma mentira que Magnitud evita al no declarar un decimal en su raiz.

		/// <summary>De cuanto venia, cero cuando la frase no lo nombra.</summary>
		internal decimal ValorInicial { get; private set; }

		/// <summary>En cuanto esta, cero cuando la frase no lo nombra.</summary>
		internal decimal ValorFinal { get; private set; }

		/// <summary>La unidad de los dos, vacia cuando la frase no nombra ningun valor.</summary>
		internal string Unidad { get; private set; } = "";

		/// <summary>Cuantos dias abarca lo que la frase cuenta, cero cuando no los nombra.</summary>
		internal int Dias { get; private set; }

		/// <summary>Cuanto cambio, cero cuando la frase no lo nombra.</summary>
		internal decimal Cambio { get; private set; }

		/// <summary>Cuantas veces se midio, cero cuando la frase no lo nombra.</summary>
		internal int Cuantas { get; private set; }

		/// <summary>Cuantas veces SEGUIDAS, que es el hecho que cambia la conducta. Cero cuando no.</summary>
		internal int CuantasSeguidas { get; private set; }

		/// <summary>
		/// LA CATEGORIA QUE LA FRASE NOMBRA, por su codigo: el color que salio. Vacia cuando no hay
		/// ninguna. El codigo es identidad; como se dice lo busca quien redacta.
		/// </summary>
		internal string ClaveDeLaCategoria { get; private set; } = "";

		/// <summary>
		/// EL NIVEL PROPIO que la frase cita junto al valor: el nivel de siempre del antigeno, el
		/// punto del que venia bajando el colesterol, el HDL que acompana al LDL. Cero cuando no.
		/// </summary>
		internal decimal NivelPropio { get; private set; }

		/// <summary>
		/// EL LIMITE que la frase cita: la meta del colesterol, el ritmo por ano desde el que conviene
		/// mirarlo. Cero cuando la frase no cita ninguno.
		/// </summary>
		internal decimal LimiteCitado { get; private set; }

		/// <summary>A que ritmo por ano viene cambiando, cuando la frase lo dice. Cero cuando no.</summary>
		internal decimal RitmoPorAno { get; private set; }

		/// <summary>
		/// LA CLAUSULA QUE LA FRASE AGREGA AL FINAL, por su nombre: el grado de anemia, como juega el
		/// HDL, si la meta ya se alcanzo. Vacia cuando la frase no agrega ninguna.
		/// </summary>
		internal string ClaveDeLaColetilla { get; private set; } = "";

		internal Interpretacion ConForma(FormaDeLectura forma)
		{
			Forma = forma;
			return this;
		}

		/// <summary>Contra que se lee el valor: la frase, y los extremos que nombra.</summary>
		internal Interpretacion ConReferencia(string clave, decimal minimo = 0m, decimal maximo = 0m,
			string unidad = "")
		{
			ClaveDeLaReferencia = clave ?? "";
			MinimoDeLaReferencia = minimo;
			MaximoDeLaReferencia = maximo;
			UnidadDeLaReferencia = unidad ?? "";
			return this;
		}

		/// <summary>Los valores del TRAMO que la frase nombra: de cuanto a cuanto, en cuantos dias.</summary>
		internal Interpretacion ConTramo(EvaluacionDeTendencia hechos)
		{
			ArgumentNullException.ThrowIfNull(hechos);

			ValorInicial = hechos.ValorInicial;
			ValorFinal = hechos.ValorFinal;
			Unidad = hechos.Unidad;
			Dias = hechos.DiasTranscurridos;
			Cambio = hechos.Cambio;
			Cuantas = hechos.CantidadDeMediciones;
			return this;
		}

		/// <summary>Los valores de la TIRA que la frase nombra: cuantas pruebas, cuantas seguidas.</summary>
		internal Interpretacion ConPruebas(EvaluacionDePresencia hechos)
		{
			ArgumentNullException.ThrowIfNull(hechos);

			Cuantas = hechos.CantidadDeMediciones;
			Dias = hechos.DiasTranscurridos;
			CuantasSeguidas = hechos.PositivasSeguidas;
			Cambio = hechos.Positivas;
			return this;
		}

		/// <summary>Los valores de las CATEGORIAS que la frase nombra: cuantas, cuantas seguidas, cual.</summary>
		internal Interpretacion ConCategorias(EvaluacionDeNominal hechos)
		{
			ArgumentNullException.ThrowIfNull(hechos);

			Cuantas = hechos.CantidadDeMediciones;
			Dias = hechos.DiasTranscurridos;
			CuantasSeguidas = hechos.NoEsperablesSeguidas;
			Cambio = hechos.NoEsperables;
			ClaveDeLaCategoria = hechos.UltimaCategoria;
			return this;
		}

		/// <summary>El nivel propio que la frase cita, y la clausula que le cuelga.</summary>
		internal Interpretacion ConNivelPropio(decimal nivel, string claveDeLaColetilla = "")
		{
			NivelPropio = nivel;
			if (!string.IsNullOrEmpty(claveDeLaColetilla)) ClaveDeLaColetilla = claveDeLaColetilla;
			return this;
		}

		/// <summary>El limite que la frase cita, y la clausula que le cuelga.</summary>
		internal Interpretacion ConLimite(decimal limite, string claveDeLaColetilla = "")
		{
			LimiteCitado = limite;
			if (!string.IsNullOrEmpty(claveDeLaColetilla)) ClaveDeLaColetilla = claveDeLaColetilla;
			return this;
		}

		/// <summary>El ritmo por ano que la frase dice, y desde cual empieza a importar.</summary>
		internal Interpretacion ConRitmo(decimal porAno, decimal desdeCual)
		{
			RitmoPorAno = porAno;
			LimiteCitado = desdeCual;
			return this;
		}

		/// <summary>La clausula que la frase agrega al final, por su nombre.</summary>
		internal Interpretacion ConColetilla(string clave)
		{
			ClaveDeLaColetilla = clave ?? "";
			return this;
		}

		internal Interpretacion(Veredicto veredicto, Severidad severidad, string clave,
			HechosDeLaSerie hechos, Accion accion = null)
		{
			ArgumentNullException.ThrowIfNull(veredicto);
			ArgumentNullException.ThrowIfNull(severidad);
			ArgumentNullException.ThrowIfNullOrWhiteSpace(clave);
			ArgumentNullException.ThrowIfNull(hechos);

			// TODO CONSEJO CON PLAZO, y esta guarda es la que lo hace ley: una severidad
			// que pide algo no puede viajar sin decir que y cuando. Es el criterio 8 de
			// la adopcion de marca, cumplido por construccion y no por disciplina.
			if (severidad.PideAlgo && accion == null)
				throw new ArgumentNullException(nameof(accion),
					$"Una severidad '{severidad.Nombre}' pide algo, y lo que pide "
					+ "tiene que decirse con su plazo: falta la Accion.");

			Veredicto = veredicto;
			Severidad = severidad;
			Clave = clave;
			Hechos = hechos;
			Accion = accion;

			// LOS VALORES LOS PONE LA FORMA DE SERIE, no un if sobre su tipo: cada
			// evaluacion sabe cuales tiene para nombrar, y aca no hay nada que preguntar.
			hechos.PonerSusValoresEn(this);
		}

		// El caso sin serie, que es identico para todos los analitos y por eso se arma una
		// sola vez: con una medicion no se afirma tendencia. Preservarlo es explicito en el
		// feedback.
		//
		// UN NOMINAL CON UNA OBSERVACION NO PASA POR ACA, y no es un olvido: un color ES su
		// categoria, asi que con una sola su lectura ya esta completa. Las dos frases de
		// abajo -"todavia no hay serie para decir para donde viene"- y la accion de repetir
		// el examen serian falsas ahi. Ver SerieDeColorDeOrina.Leer y Veredicto.Observado.
		//
		// CON UNA MEDICION HAY UNA ACCION, y tiene plazo aunque no corra: repetir el
		// examen es lo unico que convierte un punto en una serie, y cuando conviene
		// hacerlo lo dice el medico, no este dominio.
		internal static Interpretacion SinSerie(HechosDeLaSerie hechos)
		{
			ArgumentNullException.ThrowIfNull(hechos);

			bool hayUna = hechos.CantidadDeMediciones == 1;

			Accion accion = hayUna
				? Accion.RepetirElExamen(Plazo.CuandoElMedicoLoIndique)
				: null;

			return new Interpretacion(Veredicto.Indeterminado, Severidad.Informativo,
				hayUna ? "lectura.sin-serie.una" : "lectura.sin-serie.ninguna", hechos, accion);
		}

		// ESTE ANALITO NO LE CORRESPONDE A ESTA PERSONA. Severidad Informativo y sin
		// accion: no es un hallazgo clinico y no hay nada que hacer con el.
		//
		// La frase la elige el dominio, como todas: la superficie no tiene de donde sacar
		// por que un examen no corresponde, y elegirla afuera seria exactamente el filtro
		// de pantalla que esta correccion vino a evitar.
		internal static Interpretacion NoAplica(HechosDeLaSerie hechos)
		{
			ArgumentNullException.ThrowIfNull(hechos);

			return new Interpretacion(Veredicto.NoAplica, Severidad.Informativo,
				"lectura.no-aplica", hechos);
		}

		// NO SE PUEDE LEER HASTA SABER EL SEXO. El valor sigue a la vista —lo midio el
		// laboratorio— y lo que se retiene es la interpretacion.
		//
		// `clave` la elige cada analito con SU vocabulario, porque no falta lo mismo en
		// todos: en la hemoglobina falta contra que rango comparar; en el antigeno falta
		// saber si el examen le corresponde. Decir lo mismo en los dos seria la tabla plana
		// otra vez.
		//
		// Severidad Informativo, siempre: no es un hallazgo clinico y no debe pintarse de
		// ambar ni de rojo. El semaforo esta reservado para resultados.
		internal static Interpretacion RequiereSexo(HechosDeLaSerie hechos, string clave)
		{
			ArgumentNullException.ThrowIfNull(hechos);
			ArgumentNullException.ThrowIfNullOrWhiteSpace(clave);

			return new Interpretacion(Veredicto.RequiereSexo, Severidad.Informativo, clave, hechos);
		}

		// Proyecciones escalares, para que el print del DSL no tenga que navegar dos
		// niveles y la superficie no tenga que abrir los objetos.
		internal string Analito => Hechos.Analito;
		internal string ClaveDelAnalito => Hechos.ClaveDelAnalito;
		internal string NombreDelVeredicto => Veredicto.Nombre;
		internal string NombreDeLaForma => Forma == null ? "" : Forma.Nombre;
		internal string NombreDeLaSeveridad => Severidad.Nombre;
		internal bool PideAlgo => Severidad.PideAlgo;
		internal bool SugiereRepetirElExamen => Veredicto.SugiereRepetirElExamen;

		// Los dos de la aplicabilidad, proyectados para que la pantalla y la tool MCP no
		// tengan que comparar cadenas contra el nombre del veredicto.
		// Si hay una lectura de verdad detras de esto. Falso en los tres casos que no la
		// tienen —faltan mediciones, falta el sexo, o el analito no corresponde— y es lo que
		// una superficie pregunta cuando quiere saber si tiene algo que decir.
		internal bool HayLectura => Veredicto.HayLectura;

		internal bool NoLeCorresponde => Veredicto.NoLeCorresponde;

		internal bool RequiereElSexo => Veredicto.RequiereElSexo;

		/// <summary>El codigo de que hacer, vacio cuando no hay nada que hacer.</summary>
		internal string ClaveDeLaAccion => Accion == null ? "" : Accion.Clave;

		/// <summary>El codigo de cuando, vacio cuando no hay nada que hacer.</summary>
		internal string ClaveDelPlazo => Accion == null ? "" : Accion.ClaveDelPlazo;

		// COMO SE LLAMA LA LECTURA en una palabra, por su codigo: `en-ascenso` en una
		// continua, `aparecio` en un binario. Sale de los hechos, que es donde vive el
		// conjunto cerrado de cada forma, y es lo que la superficie puede pedir sin saber
		// cual tiene enfrente.
		internal string NombreDeLaLectura => Hechos.NombreDeLaLectura;
	}
}
