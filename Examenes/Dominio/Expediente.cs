using System;
using System.Collections.Generic;

namespace Examenes.Dominio
{
	// El agregado: el expediente del paciente. La puerta de acceso a sus examenes.
	//
	// Nadie construye un Examen y lo guarda por su cuenta; se agrega aca, y aca se
	// mantienen las series por analito al dia. Es el equivalente del Facturario en el
	// negocio de facturas, con una diferencia importante: ademas de la coleccion,
	// mantiene los INDICES que hacen posible la pregunta de la serie.
	//
	// EL PACIENTE vive en el expediente y entra por constructor. No se calcula con la
	// fecha de hoy a proposito: un dominio que lee el reloj deja de ser reproducible, y el
	// mismo journal reproducido el ano que viene daria otros rangos de referencia. Para
	// el demo la edad esta quemada al construir el expediente; el dia que el proveedor
	// mande la fecha de nacimiento, entra por el mismo lugar.
	//
	// EL SEXO ENTRA POR DOS PUERTAS, Y LAS DOS SE GUARDAN. Es la opcion C de la spec:
	//
	//   INSTITUCION  el laboratorio lo publica, porque lo tiene: los rangos de referencia
	//                van impresos en el reporte, asi que su sistema sabe el dato.
	//   TU           la persona lo declara o lo corrige desde su app.
	//
	// Conviven en el journal con su procedencia, y PARA INTERPRETAR GANA LO QUE DIJO LA
	// PERSONA SOBRE SI MISMA. Eso no es una preferencia de implementacion: es "tú eres la
	// autoridad final" demostrado en un caso concreto, y es la razon de que el conflicto
	// —la institucion dice una cosa y la persona otra— sea un caso modelado y no un bug.
	//
	// 🚩 PENDIENTE DE CONFIRMAR con Alvaro (§12.1 de la spec). El pedido original asume
	// que solo la persona lo aporta (opcion A), que haria que el bloque que insiste
	// aparezca SIEMPRE al inicio de la demo. La C lo incluye —mientras el laboratorio no
	// publique el dato, el estado sigue siendo Desconocido— asi que el guion de la demo
	// funciona igual y ademas queda el caso del conflicto para mostrar.
	internal class Expediente
	{
		private readonly List<Examen> examenes = new List<Examen>();

		// Serie por analito. Es lo que evita recorrer todos los examenes y filtrar en
		// cada consulta, y es la estructura que hace que "veni subiendo?" sea O(1) para
		// encontrar la serie.
		// La llave es la IDENTIDAD del analito, no su nombre. El diccionario compara por
		// referencia -Analito no define Equals- y eso es exactamente lo que se quiere: dos
		// examenes son de la misma serie si son del mismo analito, no si el emisor escribio
		// el nombre igual.
		//
		// Antes era un Dictionary<string,...> con OrdinalIgnoreCase, que resolvia las
		// mayusculas y NADA mas: "HGB" y "Hemoglobina" seguian siendo dos series.
		//
		// El valor es la RAIZ de las series, no la rama continua: el diccionario tiene las
		// tres formas —continuas, binarias y, desde que el color forma serie, nominales— y
		// el Expediente les pregunta lo mismo a todas sin saber cual tiene enfrente.
		private readonly Dictionary<Analito, SerieDeAnalito> series =
			new Dictionary<Analito, SerieDeAnalito>();

		// Lo que dijo cada uno, por separado. Se guardan LOS DOS y no se pisan: el journal
		// conserva las dos afirmaciones con su procedencia, y lo que sigue es solo quien
		// gana al interpretar.
		private Sexo declaradoPorLaPersona = Sexo.Desconocido;

		private Sexo publicadoPorLaInstitucion = Sexo.Desconocido;

		internal int Edad { get; private set; }

		// EL CONSTRUCTOR SIGUE RECIBIENDO SOLO LA EDAD, y eso no es pereza: la semilla del
		// actor esta JOURNALIZADA como `expediente = Expediente(60)`. Cambiarle la firma
		// haria que ningun expediente ya sembrado se pueda reproducir, que es exactamente
		// lo que un dominio event-sourced no puede permitirse.
		//
		// Y ademas es el arranque correcto: un expediente nuevo NO sabe el sexo. Que ese
		// sea el estado inicial —y no un default masculino— es la mitad del punto.
		internal Expediente(int edad)
		{
			if (edad <= 0 || edad > 120)
				throw new ArgumentOutOfRangeException(nameof(edad), edad,
					"La edad del paciente tiene que estar entre 1 y 120.");

			Edad = edad;
		}

		// DE QUIEN ES ESTE EXPEDIENTE. Es lo que todo el dominio recibe para interpretar, y
		// se arma cada vez en vez de guardarse: asi no puede quedar desincronizado de las
		// dos afirmaciones de las que sale.
		//
		// GANA LA PERSONA. Si declaro su sexo, ese es; si no, el que publico la institucion;
		// si ninguno, Desconocido.
		internal Paciente Paciente => new Paciente(Edad, SexoQueGana);

		internal Sexo Sexo => SexoQueGana;

		private Sexo SexoQueGana =>
			declaradoPorLaPersona.EsConocido ? declaradoPorLaPersona : publicadoPorLaInstitucion;

		// Lo que dijo cada uno, para que la superficie pueda mostrar el conflicto en vez de
		// esconderlo. Vacio -Desconocido- cuando esa fuente no dijo nada.
		internal Sexo SexoDeclaradoPorLaPersona => declaradoPorLaPersona;

		internal Sexo SexoPublicadoPorLaInstitucion => publicadoPorLaInstitucion;

		// HAY CONFLICTO cuando las dos fuentes dijeron algo y no dijeron lo mismo. No es un
		// error que haya que resolver: es un hecho del expediente, y el dominio ya sabe
		// cual gana. Se expone para poder DECIRLO.
		internal bool HayConflictoDeSexo =>
			declaradoPorLaPersona.EsConocido
			&& publicadoPorLaInstitucion.EsConocido
			&& !declaradoPorLaPersona.EsElMismo(publicadoPorLaInstitucion);

		// LO QUE LA PERSONA DICE DE SI MISMA. Es el comando local, procedencia TU.
		//
		// El texto entra como cadena porque asi llega —de un boton de la pantalla, del
		// journal al reproducir— y se resuelve a identidad en la frontera, igual que
		// Analito.PorNombre. De aca para adentro nadie vuelve a mirar un string.
		internal void DeclararSexo(string sexo)
		{
			declaradoPorLaPersona = Sexo.PorNombre(sexo);
		}

		// LO QUE PUBLICA EL LABORATORIO, procedencia INSTITUCION. Entra por el mismo lugar
		// y NO pisa lo que dijo la persona: los dos quedan, y para interpretar gana ella.
		internal void PublicarSexo(string sexo)
		{
			publicadoPorLaInstitucion = Sexo.PorNombre(sexo);
		}

		internal IReadOnlyList<Examen> Lista => examenes;

		internal int Cantidad => examenes.Count;

		// Los que TIENEN HISTORIA Y LE CORRESPONDEN a esta persona. Un antigeno prostatico
		// con serie completa no cuenta si el expediente es de una mujer: para ella ese
		// analito no existe, y contarlo aca lo dejaria asomando en el pie de la portada
		// aunque su tarjeta no estuviera.
		internal int CantidadDeAnalitosConSerie => Visibles().Count;

		// Indexador por posicion en la lista. Es lo que hace valido el DSL
		// `expediente[@indice]` para el detalle: los examenes no tienen un numero de
		// documento como las facturas, asi que la posicion es la unica llave estable
		// que el journal reproduce igual.
		internal Examen this[int indice]
		{
			get
			{
				if (indice < 0 || indice >= examenes.Count)
					throw new IndexOutOfRangeException(
						$"El expediente tiene {examenes.Count} examenes; no existe el indice {indice}.");
				return examenes[indice];
			}
		}

		internal void Agregar(Examen examen)
		{
			ArgumentNullException.ThrowIfNull(examen);

			examenes.Add(examen);

			// LOS CONTINUOS ENTRAN POR EL EXAMEN, y no por la lectura, porque su serie
			// necesita mas que el numero: los umbrales, el rango por edad y la
			// periodicidad viven en el examen, no en el resultado.
			//
			// La serie la crea EL ANALITO, no un switch aca. Cada uno sabe cual es "su
			// plural": el antigeno devuelve SerieDeAntigenoProstatico, con sus metodos, y
			// el colesterol devuelve la suya, con otros.
			if (examen is ExamenDeSangre deSangre)
				((SerieContinua)Nueva(deSangre.Analito, deSangre.NuevaSerie)).Agregar(deSangre);

			// LO QUE NO ES DE SANGRE ENTRA POR LA LECTURA, y cada lectura a la serie de SU
			// forma. Un urianalisis trae TRES cosas medidas y las TRES forman serie —cada
			// una con la suya—; el examen entero no es el punto de ninguna. Ahi el
			// conocimiento clinico esta en la magnitud —el vocabulario y la polaridad en el
			// binario, las categorias y lo esperable en el nominal— y el examen no aporta
			// mas que la fecha.
			//
			// Antes esto no ocurria en absoluto: un examen de orina entraba a la lista y a
			// ninguna serie, asi que "cómo viene mi sangre en orina" no se podia preguntar.
			// Y hasta el cambio de los nominales el color seguia afuera: se medía, se
			// clasificaba y no estaba en ningun plural.
			//
			// Un solo foreach con dos ramas y no dos foreach: una sola pasada, y la simetria
			// entre las dos formas queda a la vista.
			foreach (LecturaDeExamen lectura in examen.Lecturas)
			{
				if (lectura.Resultado is MagnitudBinaria binaria)
					((SerieBinaria)Nueva(lectura.Analito, binaria.NuevaSerie))
						.Agregar(new PruebaBinaria(examen.Fecha, binaria));
				else if (lectura.Resultado is MagnitudNominal nominal)
					((SerieNominal)Nueva(lectura.Analito, nominal.NuevaSerie))
						.Agregar(new ObservacionNominal(examen.Fecha, nominal));
			}
		}

		// La serie de ese analito, creandola con SU propia fabrica la primera vez. El
		// Expediente no sabe que clase es: se la pide a quien si lo sabe.
		private SerieDeAnalito Nueva(Analito analito, Func<SerieDeAnalito> fabrica)
		{
			if (!series.TryGetValue(analito, out SerieDeAnalito serie))
			{
				serie = fabrica();
				series[analito] = serie;
			}
			return serie;
		}

		// Las cuatro consultas de abajo reciben el analito COMO TEXTO porque asi llega: de
		// una pregunta de la IA, de un click en la pantalla, del DSL. El texto se resuelve
		// a identidad UNA vez, en la frontera, igual que UnidadDeMedida.PorSimbolo.
		//
		// Y ahi se separan dos cosas que antes se confundian en una sola respuesta:
		//
		//   nombre que no resuelve      -> DEFECTO de quien pregunta. Revienta.
		//   analito real, sin mediciones -> respuesta legitima. Contesta "no hay serie".
		//
		// Antes las dos daban "no hay datos de eso", asi que un nombre mal escrito se veia
		// igual que un analito que la persona todavia no se hizo. Eran indistinguibles
		// justo cuando importa distinguirlas: al agregar un sinonimo y equivocarse.
		// LAS SERIES QUE LE CORRESPONDEN A ESTA PERSONA. Es el filtro por el que pasan TODAS
		// las consultas de abajo, y por eso esta escrito una vez: si cada consulta decidiera
		// por su cuenta, la que se olvidara dejaria el analito asomando —en el historial, en
		// el titular, en el conteo— y la correccion volveria a ser un filtro de pantalla
		// puesto en varios lados.
		//
		// LOS INDETERMINADOS ENTRAN. Mientras no se sepa el sexo el analito sigue estando:
		// se muestra su valor y se retiene el veredicto. Esconderlo por las dudas seria
		// decidir con un dato que falta, que es justamente lo que este cambio no hace.
		private List<KeyValuePair<Analito, SerieDeAnalito>> Visibles()
		{
			Paciente paciente = Paciente;
			var visibles = new List<KeyValuePair<Analito, SerieDeAnalito>>();

			foreach (KeyValuePair<Analito, SerieDeAnalito> par in series)
				if (par.Key.AplicaA(paciente).HayAlgoQueDecir) visibles.Add(par);

			return visibles;
		}

		// La serie de ese analito SI le corresponde a esta persona; null si no. Es la
		// version de una sola llave del filtro de arriba.
		private SerieDeAnalito Visible(Analito analito)
		{
			if (!analito.AplicaA(Paciente).HayAlgoQueDecir) return null;
			return series.TryGetValue(analito, out SerieDeAnalito serie) ? serie : null;
		}

		internal bool TieneSerieDe(string analito) => Visible(Analito.PorNombre(analito)) != null;

		// EL ACCESO CRUDO A LA SERIE, sin el filtro de aplicabilidad. Existe para las
		// comprobaciones y para quien necesita los metodos propios de una serie concreta
		// -la velocidad del antigeno, el histograma-; las consultas que la superficie
		// consume son las de mas abajo, y esas si filtran.
		internal SerieDeAnalito SerieDe(string analito)
		{
			Analito cual = Analito.PorNombre(analito);
			if (!series.TryGetValue(cual, out SerieDeAnalito serie))
				throw new KeyNotFoundException(
					$"El expediente no tiene ninguna medicion de '{cual.Nombre}'.");

			return serie;
		}

		// La pregunta que ninguna pantalla contesta. Es total: un analito sin
		// mediciones devuelve Indeterminada en vez de reventar, porque "no tengo datos
		// de eso" es una respuesta legitima para quien pregunta.
		internal EvaluacionDeTendencia EvaluarTendenciaDe(string analito)
		{
			Analito cual = Analito.PorNombre(analito);

			// UN ANALITO QUE NO LE CORRESPONDE NO TIENE TENDENCIA, y la respuesta correcta
			// es NoAplica y no SinSerie: "todavia no hay mediciones" invitaria a hacerse un
			// examen que no corresponde.
			SerieDeAnalito serie = Visible(cual);
			if (serie == null)
				return cual.AplicaA(Paciente).EsNoAplica
					? EvaluacionDeTendencia.NoAplica(cual, 0)
					: EvaluacionDeTendencia.SinSerie(cual, 0, null);

			// LA TENDENCIA SOLO EXISTE EN LAS SERIES CONTINUAS. Un binario no asciende ni
			// desciende: aparece. Un nominal tampoco: se observa, y lo que se puede decir de
			// el es si salio de lo esperable. La consulta sigue siendo TOTAL —la superficie pregunta
			// sin saber que forma tiene enfrente— y por eso la respuesta es NoAplica y no
			// SinSerie: decirle "indeterminada" a un analito con cuatro pruebas seria
			// decirle a la persona que le faltan examenes.
			//
			// Es la misma frontera que VelocidadDe, que ya estaba: "esta pregunta es
			// general" contra "esta capacidad es de una forma sola". Para leer un binario
			// esta InterpretarSerieDe, que si contesta para todos.
			return serie is SerieContinua continua
				? continua.Evaluar()
				: EvaluacionDeTendencia.NoAplica(cual, serie.Cantidad);
		}

		// LO QUE EL ANALITO CONCLUYE de su propia serie: veredicto, severidad y la linea
		// para la persona. Es la consulta que reemplaza al "exige atencion" generico.
		//
		// Total por la misma razon que EvaluarTendenciaDe: preguntar por un analito real
		// del que todavia no hay nada es legitimo, y la respuesta es "no hay serie".
		internal Interpretacion InterpretarSerieDe(string analito)
		{
			Analito cual = Analito.PorNombre(analito);
			Paciente paciente = Paciente;

			// LA APLICABILIDAD SE CONTESTA AUNQUE NO HAYA SERIE, y eso es lo que distingue
			// esta correccion de un filtro de pantalla. La tool MCP pregunta por el antigeno
			// prostatico de una mujer y recibe "no corresponde a tu caso", no "todavia no
			// hay mediciones" y muchisimo menos un valor. La aplicabilidad es propiedad de
			// QUE SE MIDIO —vive en la identidad del analito— asi que se puede contestar sin
			// una sola medicion.
			if (cual.AplicaA(paciente).EsNoAplica)
				return Interpretacion.NoAplica(EvaluacionDeTendencia.NoAplica(cual, 0))
					.ConForma(FormaDeLectura.Afirmacion);

			// La REFERENCIA viaja con la lectura, igual que la forma: la superficie pide
			// una vez y sabe contra que se lee el valor. Se aplica aca —el unico punto de
			// consulta— para que ningun numero salga sin ella.
			return series.TryGetValue(cual, out SerieDeAnalito serie)
				? serie.Interpretar(paciente).ConForma(serie.Forma)
					.ConReferencia(serie.ReferenciaLegible(paciente))
				: Interpretacion.SinSerie(EvaluacionDeTendencia.SinSerie(cual, 0, null))
					.ConForma(FormaDeLectura.Afirmacion);
		}

		// LO MAS URGENTE de todo el expediente, con su accion. No se puede armar afuera:
		// exige recorrer cada serie, interpretarla con el criterio de SU analito y
		// quedarse con la mayor — y desde que la accion lleva plazo, quedarse tambien con
		// LA ACCION del que gano, porque la severidad sola ya no sabe decir que hacer.
		internal UrgenciaDelExpediente LoMasUrgente
		{
			get
			{
				Severidad mayor = Severidad.Informativo;
				Accion accion = null;

				Paciente paciente = Paciente;

				// SOLO LOS QUE LE CORRESPONDEN. Un antigeno prostatico que exige atencion
				// no puede poner "mostrále esto a tu médico" en el titular de una mujer:
				// para ella ese examen no existe, y el titular es lo primero y a veces lo
				// unico que alguien lee.
				foreach (KeyValuePair<Analito, SerieDeAnalito> par in Visibles())
				{
					Interpretacion lectura = par.Value.Interpretar(paciente);
					if (lectura.Severidad.EsMasUrgenteQue(mayor))
					{
						mayor = lectura.Severidad;
						accion = lectura.Accion;
					}
				}

				// UN SOLO RECORRIDO, y por eso este metodo ya no lee `examenes`. Habia un
				// segundo loop sobre las lecturas sueltas para los NOMINALES, que eran lo
				// unico medido sin serie; desde que el color forma la suya, su severidad
				// llega por Visibles() como la de todos.
				//
				// Sumar las dos cosas taparia el caso que mas importa, y es el mismo
				// argumento con el que las sangres ocultas y los nitritos habian salido de
				// aca antes: el hallazgo que no se confirmo, que la serie baja a informativo
				// mientras el examen suelto sigue diciendo lo maximo para siempre.
				return new UrgenciaDelExpediente(mayor, accion);
			}
		}

		// La severidad sola, para quien no necesita la accion: el conteo, las
		// comprobaciones, la IA que pregunta "que tan grave".
		internal Severidad SeveridadMayor => LoMasUrgente.Severidad;

		// A que ritmo viene cambiando un analito. Total por la misma razon que
		// EvaluarTendenciaDe: preguntar por un analito del que no hay nada es legitimo, y
		// la respuesta es "no hay velocidad que dar", no una excepcion.
		internal Velocidad VelocidadDe(string analito)
		{
			Analito cual = Analito.PorNombre(analito);

			// LA VELOCIDAD SOLO EXISTE EN LA SERIE DEL ANTIGENO, y esta consulta sigue
			// siendo TOTAL para que la superficie pueda preguntarle a cualquier analito sin
			// saber cual la tiene. La prueba de tipo es la frontera entre "esta es una
			// pregunta general" y "esta capacidad es de uno solo".
			// Y el antigeno es justamente el analito con requisito anatomico, asi que este
			// es el camino por el que su ritmo podria salir en el expediente de alguien a
			// quien no le corresponde. Visible() lo corta.
			return Visible(cual) is SerieDeAntigenoProstatico antigeno
				? antigeno.VelocidadDelTramo()
				: Examenes.Dominio.Velocidad.NoAplica(cual, "", 0m);
		}

		// Los analitos de los que hay historia, para que la superficie pueda recorrerlos
		// sin conocer la lista de clases del dominio.
		// Salen los nombres CANONICOS: es con esos que se vuelve a preguntar, y son los
		// que el enum del MCP conoce. El nombre para mostrar viaja en cada evaluacion.
		internal IReadOnlyList<string> Analitos
		{
			get
			{
				// LOS QUE LE CORRESPONDEN, y no todos los que tienen serie. Esta es la
				// lista que la pantalla recorre para armar una tarjeta por analito y la que
				// la IA usa para saber por que puede preguntar: si el antigeno saliera de
				// aca en el expediente de una mujer, la tarjeta volveria a existir por la
				// puerta de atras.
				var nombres = new List<string>();
				foreach (KeyValuePair<Analito, SerieDeAnalito> par in Visibles())
					nombres.Add(par.Key.Nombre);
				nombres.Sort(StringComparer.OrdinalIgnoreCase);
				return nombres;
			}
		}

		// QUE FALTA MEDIR. Es la unica consulta de este expediente que mira el hueco y no
		// el dato: cuanto hace que no se mide cada analito, y si eso ya es demasiado para
		// EL -cada uno tiene su periodicidad, y varios la resuelven contra su ultimo
		// valor-.
		//
		// LA FECHA ENTRA POR PARAMETRO. El dominio no lee el reloj: si lo hiciera, el
		// mismo journal reproducido manana daria otra respuesta y dejaria de ser
		// reproducible. Quien tiene derecho a saber que dia es hoy es la superficie.
		//
		// Solo aparecen los analitos CON SERIE. De uno que nunca se midio no se puede
		// decir "te toca repetirlo": no hay nada que repetir, y el dominio no inventa una
		// recomendacion sobre algo que nadie pidio.
		//
		// DESDE QUE LAS TRES FORMAS TIENEN SERIE, esto cubre todo lo que se mide: la sangre
		// oculta, los nitritos y los dos colores. Era el limite anotado en §7 bis:
		// "que_me_falta_medir solo cubre analitos con serie, o sea solo sangre". Y la
		// periodicidad la resuelve cada uno contra su ultimo resultado: una sangre oculta
		// positiva no espera al ano, y un color inesperado tampoco.
		internal IReadOnlyList<ControlPendiente> ControlesPendientes(DateTime hoy)
		{
			var pendientes = new List<ControlPendiente>();
			Paciente paciente = Paciente;

			// Y SOLO LOS QUE LE CORRESPONDEN: decirle a una mujer que le toca repetirse el
			// antigeno prostatico seria peor que mostrarle el valor viejo.
			foreach (KeyValuePair<Analito, SerieDeAnalito> par in Visibles())
			{
				SerieDeAnalito serie = par.Value;
				if (!serie.HayMediciones) continue;

				// Una medicion fechada DESPUES de la fecha de referencia cuenta como de
				// hoy, no como dias negativos. Pasa de verdad: el guion tiene un examen
				// del dia siguiente para demostrar el salto inverosimil, y en produccion
				// pasaria con una zona horaria o un laboratorio que fecha la validacion.
				// "Hace -1 dias que te lo hiciste" no es una respuesta.
				int dias = Math.Max(0, (int)(hoy.Date - serie.FechaDeLaUltima.Date).TotalDays);

				pendientes.Add(new ControlPendiente(
					par.Key, serie.FechaDeLaUltima.Date, dias, serie.MesesHastaElProximo(paciente)));
			}

			// Lo mas vencido primero: es el orden en que conviene leerlos, y evita que la
			// superficie tenga que decidirlo.
			pendientes.Sort((uno, otro) => uno.DiasQueFaltan.CompareTo(otro.DiasQueFaltan));
			return pendientes;
		}

		// LOS PUNTOS DE LA SERIE, con su valor NUMERICO. El cuadro los da ya convertidos a
		// texto -"3.0 ng/mL"-, que sirve para una tabla y no para dibujar. Un grafico
		// necesita numeros.
		internal IReadOnlyList<ExamenDeSangre> PuntosDe(string analito)
		{
			Analito cual = Analito.PorNombre(analito);

			// Solo las continuas tienen puntos con NUMERO. Los de un binario son fechas con
			// un si o un no y van por PruebasDe; los de un nominal son fechas con una
			// categoria y van por ObservacionesDe.
			//
			// Y solo las que le corresponden: un analito que no aplica no devuelve sus
			// puntos, porque un grafico con datos es una forma de mostrar el valor.
			return Visible(cual) is SerieContinua continua
				? continua.Mediciones
				: new List<ExamenDeSangre>();
		}

		// LOS PUNTOS DE UNA SERIE BINARIA: fechas con un si o un no. Es el par de PuntosDe
		// para la forma 5, y son dos consultas y no una porque no devuelven lo mismo — un
		// nitrito no tiene ValorNumerico, y pedirselo seria la mentira de siempre.
		internal IReadOnlyList<PruebaBinaria> PruebasDe(string analito)
		{
			Analito cual = Analito.PorNombre(analito);

			return Visible(cual) is SerieBinaria binaria
				? binaria.Pruebas
				: new List<PruebaBinaria>();
		}

		// LOS HECHOS DE UNA SERIE BINARIA: aparecio, persiste, cedio, y sobre cuantas
		// pruebas. Es el par de EvaluarTendenciaDe, y es total por la misma razon:
		// preguntar por un analito real del que todavia no hay nada es legitimo.
		internal EvaluacionDePresencia EvaluarPresenciaDe(string analito)
		{
			Analito cual = Analito.PorNombre(analito);

			return Visible(cual) is SerieBinaria binaria
				? binaria.Evaluar()
				: EvaluacionDePresencia.SinSerie(cual, 0, "");
		}

		// LOS PUNTOS DE UNA SERIE NOMINAL: fechas con una CATEGORIA. Es el tercer par de
		// PuntosDe, y son tres consultas y no una porque no devuelven lo mismo — un color no
		// tiene ValorNumerico ni un si o un no: tiene un nombre.
		internal IReadOnlyList<ObservacionNominal> ObservacionesDe(string analito)
		{
			Analito cual = Analito.PorNombre(analito);

			return Visible(cual) is SerieNominal nominal
				? nominal.Observaciones
				: new List<ObservacionNominal>();
		}

		// LOS HECHOS DE UNA SERIE NOMINAL: siempre lo esperable, aparecio, persiste, volvio,
		// y sobre cuantas observaciones. Es el par de EvaluarPresenciaDe para la forma 6, y
		// es total por la misma razon: preguntar por un analito real del que todavia no hay
		// nada es legitimo.
		internal EvaluacionDeNominal EvaluarConstanciaDe(string analito)
		{
			Analito cual = Analito.PorNombre(analito);

			return Visible(cual) is SerieNominal nominal
				? nominal.Evaluar()
				: EvaluacionDeNominal.SinSerie(cual, 0, "", true);
		}

		// COMO SE LEE ese analito. Total: de uno sin mediciones la respuesta es que no hay
		// nada que dibujar.
		internal string FormaDeLecturaDe(string analito)
		{
			Analito cual = Analito.PorNombre(analito);

			SerieDeAnalito serie = Visible(cual);
			return serie != null ? serie.Forma.Nombre : FormaDeLectura.Afirmacion.Nombre;
		}

		// LAS LINEAS que hay que dibujar junto a los puntos.
		internal IReadOnlyList<ReferenciaDeLectura> ReferenciasDe(string analito)
		{
			Analito cual = Analito.PorNombre(analito);

			SerieDeAnalito serie = Visible(cual);
			return serie != null
				? serie.Referencias(Paciente)
				: new List<ReferenciaDeLectura>();
		}

		// EL CUADRO HISTORICO, plano y con indices. Ver CeldaDelCuadro para por que esto
		// es una decision del dominio y no un acomodo de pantalla.
		//
		// COLUMNAS: los dias distintos en que hubo algun examen, en orden. Un dia es una
		// visita al laboratorio, aunque haya traido cinco analitos a horas distintas.
		//
		// FILAS: primero las de sangre y despues las de las otras muestras, y adentro de
		// cada grupo por nombre —ver OrdenadosParaElCuadro—. Es un orden ESTABLE a
		// proposito: si las filas se ordenaran por urgencia, el cuadro se reacomodaria solo
		// cada vez que entra un examen, y una tabla que se mueve deja de servir para
		// comparar contra la vez pasada.
		//
		// Dos mediciones del mismo analito el mismo dia colapsan en una celda y gana la
		// ULTIMA por hora: es una remedicion, no dos columnas.
		internal IReadOnlyList<CeldaDelCuadro> CeldasDelCuadro
		{
			get
			{
			var columnas = new List<DateTime>();
			foreach (Examen examen in examenes)
				if (!columnas.Contains(examen.Fecha.Date)) columnas.Add(examen.Fecha.Date);
			columnas.Sort();

			// UNA FILA POR ANALITO, no por examen. Un urianalisis trae tres cosas medidas y
			// antes las tres caian en la misma fila, aplanadas en un string. Ahora cada
			// lectura es su propia fila, con su nombre y su resultado —y las tres tienen
			// serie, asi que a las tres se les puede pedir la lectura por su analito.
			// EL HISTORIAL TAMBIEN FILTRA. Es el ultimo lugar donde un analito que no
			// corresponde podia seguir asomando: la tarjeta ya no existe, el titular ya no
			// lo nombra, y la fila del historial seguiria mostrando "Antígeno prostático
			// 5.2 ng/mL" con fecha. Criterio 1 de la spec: en NINGUNA parte.
			Paciente delExpediente = Paciente;

			var filas = new List<string>();
			var porFila = new Dictionary<string, LecturaEnFecha>();
			foreach (Examen examen in OrdenadosParaElCuadro())
				foreach (LecturaDeExamen lectura in examen.Lecturas)
				{
					if (!lectura.Analito.AplicaA(delExpediente).HayAlgoQueDecir) continue;

					string llave = lectura.Analito.Nombre;
					if (!filas.Contains(llave)) filas.Add(llave);
					porFila[llave + "|" + examen.Fecha.Date.Ticks] =
						new LecturaEnFecha(lectura, examen);
				}

			var celdas = new List<CeldaDelCuadro>();
			foreach (KeyValuePair<string, LecturaEnFecha> par in porFila)
			{
				Examen examen = par.Value.Examen;
				LecturaDeExamen lectura = par.Value.Lectura;
				string llave = lectura.Analito.Nombre;

				// LA CELDA ES EL RESULTADO Y NADA MAS: fecha, analito y lo que se midio.
				//
				// Antes viajaba tambien una lectura colgada —severidad y frase— para las
				// filas que no formaban serie, que al final eran unicamente los NOMINALES.
				// Desde que el color forma la suya no queda ninguna, y esa segunda fuente
				// de lectura seria peor que redundante: la magnitud sola contesta lo mismo
				// siempre, y la serie sabe bajarlo cuando el hallazgo no se repitio. Toda
				// lectura sale por UNA puerta, InterpretarSerieDe.
				celdas.Add(new CeldaDelCuadro(
					filas.IndexOf(llave),
					columnas.IndexOf(examen.Fecha.Date),
					llave,
					lectura.ParaMostrar,
					examen.Fecha.Date,
					lectura.Expresado));
			}

			celdas.Sort((una, otra) => una.Fila == otra.Fila
				? una.Columna.CompareTo(otra.Columna)
				: una.Fila.CompareTo(otra.Fila));

				return celdas;
			}
		}

		// Una lectura junto al examen del que salio. Hace falta porque la celda necesita
		// las dos cosas: el analito y el resultado vienen de la lectura, la fecha y la
		// severidad del examen.
		private sealed class LecturaEnFecha
		{
			internal LecturaDeExamen Lectura { get; }
			internal Examen Examen { get; }

			internal LecturaEnFecha(LecturaDeExamen lectura, Examen examen)
			{
				Lectura = lectura;
				Examen = examen;
			}
		}

		// Los de sangre primero —los que son punto de una serie continua— y adentro por
		// nombre. Estable e independiente del orden en que llegaron los eventos.
		private List<Examen> OrdenadosParaElCuadro()
		{
			var ordenados = new List<Examen>(examenes);
			ordenados.Sort((uno, otro) =>
			{
				if (uno.AportaSerieContinua != otro.AportaSerieContinua)
					return uno.AportaSerieContinua ? -1 : 1;

				int porNombre = string.Compare(uno.Nombre, otro.Nombre,
					StringComparison.OrdinalIgnoreCase);
				return porNombre != 0 ? porNombre : uno.Fecha.CompareTo(otro.Fecha);
			});
			return ordenados;
		}

		// CUANTOS RESULTADOS NO SE PUEDEN INTERPRETAR HASTA SABER EL SEXO.
		//
		// Es una consulta del DOMINIO y no una cuenta de la pantalla, y esa es la razon de
		// que exista: el bloque que insiste dice "no puedo interpretar 2 de tus 7
		// resultados", y ese 2 sale de recorrer cada serie e interpretarla, que es
		// exactamente lo que una pantalla no puede hacer.
		internal int AnalitosNoInterpretables
		{
			get
			{
				int cuantos = 0;
				Paciente paciente = Paciente;
				foreach (KeyValuePair<Analito, SerieDeAnalito> par in Visibles())
					if (par.Value.Interpretar(paciente).RequiereElSexo) cuantos++;
				return cuantos;
			}
		}

		// EL PEDIDO DEL DATO QUE FALTA, entero y ya dicho: cuantos resultados quedan sin
		// leer, cuales, y la frase que lo explica en terminos de la persona.
		//
		// Va junto y no en tres consultas sueltas porque la superficie lo necesita junto y
		// porque la FRASE la escribe el dominio: es el criterio 9 de la spec —ningun texto
		// sobre el sexo se compone en la vista— y componerla afuera obligaria a la pantalla
		// a saber por que cada analito no se puede leer.
		internal PedidoDeSexo FaltaElSexo
		{
			get
			{
				Paciente paciente = Paciente;
				var sinLeer = new List<Analito>();

				foreach (KeyValuePair<Analito, SerieDeAnalito> par in Visibles())
					if (par.Value.Interpretar(paciente).RequiereElSexo) sinLeer.Add(par.Key);

				sinLeer.Sort((uno, otro) => string.Compare(uno.ParaMostrar, otro.ParaMostrar,
					StringComparison.OrdinalIgnoreCase));

				return new PedidoDeSexo(paciente.Sexo, sinLeer, CantidadDeAnalitosConSerie);
			}
		}

		// CUANTOS ESTAN DONDE TIENEN QUE ESTAR, y la frase que lo dice. Es la contracara
		// de LoMasUrgente, y va por la misma puerta: la portada muestra las dos juntas.
		//
		// Un analito cuenta cuando le CORRESPONDE a esta persona, cuando su rango se puede
		// determinar —si depende del sexo y el sexo no se sabe, no se puede— y cuando su
		// ultima lectura no es un hallazgo. Las tres condiciones en una sola pasada: es
		// una cuenta, no una resta entre escalares publicados por separado.
		internal TranquilidadDelExpediente LoQueEstaEnRango
		{
			get
			{
				int enRango = 0;
				int conSerie = 0;
				Paciente paciente = Paciente;

				foreach (KeyValuePair<Analito, SerieDeAnalito> par in Visibles())
				{
					conSerie++;

					if (!par.Key.AplicaA(paciente).EsAplica) continue;
					if (par.Value.Interpretar(paciente).RequiereElSexo) continue;
					if (par.Value.UltimaEsHallazgo(paciente)) continue;

					enRango++;
				}

				return new TranquilidadDelExpediente(enRango, conSerie);
			}
		}

		// Cuantos analitos tienen HOY algo que no deberia estar. Es un conteo derivado, no
		// un campo que pueda quedar desincronizado.
		//
		// Se pregunta "es un hallazgo" y no "esta fuera de rango" porque un binario no
		// tiene rango y sin embargo la pregunta es la misma. Antes esto solo miraba
		// analitos de sangre, asi que una sangre en la orina no contaba para nada.
		internal int AnalitosFueraDeRango
		{
			get
			{
				// UN HALLAZGO EXIGE ESTAR SEGURO DE DOS COSAS: que el analito le corresponda
				// a esta persona —no que "quizas"— y que su rango se pueda determinar. Con
				// el sexo desconocido no se cumple ninguna de las dos en los analitos que
				// dependen de el, y contarlos igual seria afirmar un hallazgo con el dato
				// que falta. Lo que la persona ve en su lugar es el bloque que lo pide.
				int fuera = 0;
				Paciente paciente = Paciente;
				foreach (KeyValuePair<Analito, SerieDeAnalito> par in Visibles())
					if (par.Key.AplicaA(paciente).EsAplica && par.Value.UltimaEsHallazgo(paciente))
						fuera++;
				return fuera;
			}
		}
	}
}
