using System;
using System.Collections.Generic;

namespace Examenes.Dominio
{
	// LA SERIE DEL ANTIGENO PROSTATICO, con los metodos que ninguna otra serie tiene.
	//
	// Es el ejemplo con el que Alvaro pidio §4: "hacer el plural antigeno prostatico,
	// examenes plural, y a ese hacerle metodos especificos de evaluar su tendencia".
	//
	// Lo que vive aca y en ninguna otra: la VELOCIDAD -que para la hemoglobina no
	// significa nada-, la LINEA BASE de esta persona, y la CONFIRMACION de un pico. Las
	// tres necesitan la serie entera: por eso son de la serie y no de una medicion.
	internal sealed class SerieDeAntigenoProstatico : SerieContinua
	{
		internal SerieDeAntigenoProstatico() : base(Analito.AntigenoProstatico) { }

		// La ultima medicion, que es la que se esta juzgando. Tipada, para poder pedirle
		// lo suyo sin castear en cada linea.
		private AntigenoProstatico Ultima => (AntigenoProstatico)Ultimo;

		private static readonly AnalitoContinuo Suyo = Analito.AntigenoProstatico;

		// LA capacidad de este analito. Es publica y con nombre porque es lo que la IA
		// pregunta y la pantalla no contesta.
		//
		// EL TRAMO ES EL MAS RECIENTE, no la serie entera, y esa es la regla nombrada:
		// los anos estables de antes no diluyen un salto de este ano, lo hacen mas
		// notable. Con los numeros del guion la diferencia decide el caso:
		//
		//   serie entera   3.0 (2023) -> 5.2 (2026)   2.2 ng/mL en 1096 dias = 0.73/ano
		//   tramo reciente 3.0 (2025) -> 5.2 (2026)   2.2 ng/mL en  365 dias = 2.20/ano
		//
		// Con el umbral en 0.75 la serie entera diria que NO exige atencion.
		internal Velocidad VelocidadDelTramo()
		{
			string unidad = Ultima.Valor.Unidad.Simbolo;
			int dias = DiasDelUltimoTramo;

			// Dos mediciones el MISMO dia no dan una velocidad: un cociente sobre cero
			// dias no es un ritmo anual, es una division por cero disfrazada. Ese caso ya
			// lo atrapa Evaluar() como salto inverosimil, que es la lectura correcta.
			if (dias <= 0)
				return Velocidad.NoAplica(Suyo, unidad, AntigenoProstatico.VelocidadQueExigeAtencionPorAno);

			decimal porAno = CambioDelUltimoTramo * DiasDeUnAno / dias;

			return Velocidad.Medida(Suyo, porAno, unidad, dias, AntigenoProstatico.VelocidadQueExigeAtencionPorAno);
		}

		// EL NIVEL HABITUAL DE ESTA PERSONA, que es contra lo que de verdad se lee este
		// analito. El techo de la poblacion dice mucho menos.
		//
		// Alvaro: "una persona puede dar que yo tengo 2, 2.1, 1.8, y entonces usted como
		// individuo anda alrededor de los 2, y otro tiene 3, 3.1, 3. Entonces la cosa esta
		// mal cuando eso pasa a uno, o de uno pasa a tres."
		//
		// El mismo 3.0 es normal para el segundo y es el doble de lo suyo para el primero.
		// Un rango de referencia no distingue esos dos casos; esto si.
		//
		// Es la MEDIANA de las mediciones previas, no el promedio: un pico arrastra el
		// promedio, y el pico es justamente lo que se quiere medir CONTRA el fondo. Se
		// excluye la ultima porque es la que se esta juzgando. Null si todavia no hay
		// fondo del que hablar.
		internal Medicion NivelHabitual()
		{
			if (Cantidad < 3) return null;

			IReadOnlyList<ExamenDeSangre> mediciones = Mediciones;

			var previos = new List<decimal>();
			for (int i = 0; i < mediciones.Count - 1; i++) previos.Add(mediciones[i].Valor.Valor);
			previos.Sort();

			int medio = previos.Count / 2;
			decimal mediana = previos.Count % 2 == 1
				? previos[medio]
				: (previos[medio - 1] + previos[medio]) / 2m;

			return new Medicion(mediana, Suyo.Unidad);
		}

		// Si el ultimo valor se salio de LO SUYO, que no es lo mismo que salirse del rango
		// de la poblacion. Un 3.0 en alguien que vive en 2.0 se salio; en alguien que vive
		// en 3.0 no, y los dos estan por debajo del techo de su edad.
		internal bool SeSalioDeLoSuyo()
		{
			Medicion habitual = NivelHabitual();
			if (habitual == null) return false;

			return Ultima.EsCambioReal(Ultima.Valor.Valor - habitual.Valor) && Ultima.Valor.Valor > habitual.Valor;
		}

		// UN PICO QUE SE REPITIO Y NO SE CONFIRMO.
		//
		// Alvaro, textual: "si a usted le da 3.9, uy que preocupante, paso de 3.1 a 3.9.
		// Entonces el medico le dice: hagaselo nuevamente, espere una semana. No va a pasar
		// un ano, van a pasar 7 dias. Paso de 3 a casi 4, y cuando lo vuelve a medir pasa
		// otra vez a 3. AHI AL MEDICO ESA SEGUNDA LECTURA LE SIRVE."
		//
		// La regla generica del intervalo corto no puede contar esto. Esa regla mira UN
		// paso y pregunta "cambio demasiado rapido"; aca hay que mirar TRES puntos y
		// preguntar otra cosa: si el ultimo VOLVIO. Sin esto, el dominio reporta la vuelta
		// a la normalidad como el error, que es la lectura exactamente opuesta.
		//
		// Un valor que APARECE de la nada en dias es sospechoso. Uno que VUELVE en dias es
		// la repeticion, y es buena noticia.
		internal bool PicoNoConfirmado()
		{
			if (Cantidad < 3) return false;

			IReadOnlyList<ExamenDeSangre> mediciones = Mediciones;

			ExamenDeSangre ultima = mediciones[mediciones.Count - 1];
			ExamenDeSangre pico = mediciones[mediciones.Count - 2];
			ExamenDeSangre antes = mediciones[mediciones.Count - 3];

			// La repeticion tiene que ser PRONTO. Si paso un ano, no es una confirmacion:
			// es el siguiente control, y ahi los dos valores cuentan.
			int diasDeLaRepeticion = (int)(ultima.Fecha.Date - pico.Fecha.Date).TotalDays;
			if (diasDeLaRepeticion <= 0 || diasDeLaRepeticion > DiasParaConfirmar) return false;

			// Tiene que haber HABIDO un pico: el intermedio subio de verdad respecto del
			// anterior.
			if (!Ultima.EsCambioReal(pico.Valor.Valor - antes.Valor.Valor)) return false;
			if (pico.Valor.Valor <= antes.Valor.Valor) return false;

			// Y el ultimo tiene que haber VUELTO: quedar a la altura del de antes del pico.
			return !Ultima.EsCambioReal(ultima.Valor.Valor - antes.Valor.Valor);
		}

		// La ventana dentro de la cual una medicion cuenta como repeticion del examen y no
		// como el control siguiente. Seis semanas cubre el "espere una semana" del ejemplo
		// con margen, y se queda muy lejos del control anual.
		private const int DiasParaConfirmar = 42;

		// Si el antigeno se mantuvo plano durante los ultimos anos. Es la otra pregunta
		// propia: la linea base es lo que le da sentido al ultimo valor.
		internal bool EsEstableDurante(int anos)
		{
			if (anos <= 0)
				throw new ArgumentOutOfRangeException(nameof(anos), anos,
					"Preguntar por la estabilidad de cero anos no tiene respuesta.");

			EvaluacionDeTendencia hechos = Evaluar();
			if (!hechos.HaySerie) return false;
			if (hechos.DiasTranscurridos < anos * DiasDeUnAno) return false;

			return !Ultima.EsCambioReal(hechos.Cambio);
		}

		// EL COMPARATIVO INTERANUAL. Un punto por ano, con el cambio contra el ano anterior
		// CON MEDICIONES —no contra el ano calendario previo, que puede estar vacio—.
		//
		// Alvaro: "tiene que hacer tal vez un comparativo con el ano pasado". Y lo que es:
		// "no son prints de la tabla, son computos". Esto no es la lista agrupada: puede
		// tener menos puntos que mediciones, si hubo dos en el mismo ano, y huecos, si hubo
		// un ano sin medir. Los huecos NO se rellenan.
		//
		// Se toma el ULTIMO valor de cada ano y no el promedio: es el estado en que quedo
		// la persona al cerrar ese ano, no una mezcla.
		internal IReadOnlyList<MedicionDelAno> PorAno()
		{
			var anos = new List<int>();
			var ultimaDelAno = new Dictionary<int, ExamenDeSangre>();
			var cuantasDelAno = new Dictionary<int, int>();

			foreach (ExamenDeSangre medicion in Mediciones)
			{
				int ano = medicion.Fecha.Year;
				if (!ultimaDelAno.ContainsKey(ano)) { anos.Add(ano); cuantasDelAno[ano] = 0; }

				// Las mediciones ya vienen ordenadas por fecha, asi que la ultima que se
				// ve de cada ano es la ultima del ano.
				ultimaDelAno[ano] = medicion;
				cuantasDelAno[ano]++;
			}

			anos.Sort();

			var comparativo = new List<MedicionDelAno>();
			decimal? anterior = null;

			foreach (int ano in anos)
			{
				Medicion valor = ultimaDelAno[ano].Valor;
				decimal? cambio = anterior.HasValue ? valor.Valor - anterior.Value : (decimal?)null;

				comparativo.Add(new MedicionDelAno(ano, valor, cuantasDelAno[ano], cambio));
				anterior = valor.Valor;
			}

			return comparativo;
		}

		// EL HISTOGRAMA: en que tramos de valor cayo esta persona, y cuantas veces.
		//
		// Es la linea base VISTA en vez de resumida. Si cuatro de cinco mediciones caen en
		// la misma banda, ese es su lugar, y la que se salio se ve sola. La mediana de
		// NivelHabitual() dice lo mismo en un numero; esto lo dice en una forma.
		//
		// Las bandas se arman sobre el rango REAL de esta persona, no sobre el de la
		// poblacion: alguien que vive entre 1.8 y 2.1 no necesita bandas que lleguen al
		// techo de su edad. Se devuelven TODAS, incluidas las vacias, para que la pantalla
		// pueda dibujar los huecos sin recalcular nada.
		internal IReadOnlyList<BandaDelHistograma> Histograma(int cuantasBandas)
		{
			if (cuantasBandas <= 0)
				throw new ArgumentOutOfRangeException(nameof(cuantasBandas), cuantasBandas,
					"Un histograma de cero bandas no es un histograma.");

			var bandas = new List<BandaDelHistograma>();
			if (Cantidad == 0) return bandas;

			decimal minimo = Ultima.Valor.Valor, maximo = Ultima.Valor.Valor;
			foreach (ExamenDeSangre medicion in Mediciones)
			{
				decimal v = medicion.Valor.Valor;
				if (v < minimo) minimo = v;
				if (v > maximo) maximo = v;
			}

			// Todas las mediciones en el mismo valor: una sola banda, y no se inventa
			// ancho donde no hay dispersion.
			if (maximo == minimo)
			{
				bandas.Add(new BandaDelHistograma(
					new Medicion(Borde(minimo), Suyo.Unidad),
					new Medicion(Borde(maximo), Suyo.Unidad),
					Cantidad, true));
				return bandas;
			}

			decimal ancho = (maximo - minimo) / cuantasBandas;

			for (int i = 0; i < cuantasBandas; i++)
			{
				// TODOS los bordes pasan por el mismo redondeo, incluidos el minimo y el
				// maximo. Si no, los calculados salen con la escala de la division -2.00-
				// y los extremos conservan la del dato -2.0-, y el mismo histograma
				// muestra dos precisiones distintas.
				//
				// El borde de una banda es el mismo numero que el de la siguiente por
				// construccion, asi que redondear los dos igual no abre huecos.
				decimal desde = Borde(minimo + ancho * i);
				decimal hasta = i == cuantasBandas - 1 ? Borde(maximo) : Borde(minimo + ancho * (i + 1));

				int cuantas = 0;
				bool caeLaUltima = false;
				foreach (ExamenDeSangre medicion in Mediciones)
				{
					decimal v = medicion.Valor.Valor;
					// La ultima banda cierra por arriba; las demas no, para que ningun
					// valor caiga en dos.
					bool adentro = i == cuantasBandas - 1 ? v >= desde && v <= hasta
						: v >= desde && v < hasta;
					if (!adentro) continue;

					cuantas++;
					if (ReferenceEquals(medicion, Ultimo)) caeLaUltima = true;
				}

				bandas.Add(new BandaDelHistograma(
					new Medicion(desde, Suyo.Unidad), new Medicion(hasta, Suyo.Unidad),
					cuantas, caeLaUltima));
			}

			return bandas;
		}

		// ---------------------------------------------------------------- como se dibuja

		internal override FormaDeLectura Forma => FormaDeLectura.TrayectoriaConLineaBase;

		// La linea base y el techo de la edad. La base solo aparece cuando hay fondo: sin
		// ella no se dibuja una banda inventada.
		internal override IReadOnlyList<ReferenciaDeLectura> Referencias(Paciente paciente)
		{
			var lineas = new List<ReferenciaDeLectura>();
			if (Ultimo == null) return lineas;

			Medicion habitual = NivelHabitual();
			if (habitual != null)
				lineas.Add(new ReferenciaDeLectura("habitual", habitual,
					"linea.antigeno-prostatico.habitual"));

			RangoDeReferencia rango = Ultima.RangoPara(paciente);
			lineas.Add(new ReferenciaDeLectura("techo", rango.Maximo,
				"linea.antigeno-prostatico.techo"));

			return lineas;
		}

		// ---------------------------------------------------------------- la lectura

		protected override Interpretacion Leer(Paciente paciente)
		{
			EvaluacionDeTendencia hechos = Evaluar();
			if (!hechos.HaySerie) return Interpretacion.SinSerie(hechos);

			// VA PRIMERO, antes que la regla del intervalo. Si no, el salto de vuelta del
			// pico a la linea base cae como "probable error de medicion" y el dominio
			// reporta la buena noticia como dato sospechoso.
			if (PicoNoConfirmado())
			{
				Medicion linea = NivelHabitual();
				decimal suyo = linea == null ? Ultima.Valor.Valor : linea.Valor;

				return new Interpretacion(Veredicto.PicoNoConfirmado, Severidad.Informativo,
					"lectura.antigeno-prostatico.pico-no-confirmado", hechos)
					.ConNivelPropio(suyo);
			}

			if (hechos.Tendencia.SugiereRepetirElExamen)
				return new Interpretacion(Veredicto.ProbableErrorDeMedicion, Severidad.Vigilar,
					"lectura.antigeno-prostatico.salto-inverosimil", hechos,
					// El "espere una semana" de Alvaro: la repeticion no espera al control.
					Accion.RepetirElExamen(Plazo.EstaSemana));

			// EL CASO DEL ANALITO: la velocidad sola alcanza, aunque el valor este en
			// rango. Va primero por eso.
			Velocidad velocidad = VelocidadDelTramo();
			if (velocidad.ExigeAtencion)
				return new Interpretacion(Veredicto.Empeorando, Severidad.ConsultePronto,
					"lectura.antigeno-prostatico.viene-subiendo", hechos,
					Accion.MostrarleEstoATuMedico(Plazo.EsteMes))
					.ConRitmo(velocidad.PorAno, AntigenoProstatico.VelocidadQueExigeAtencionPorAno);

			// SALIRSE DE LO SUYO, aunque el valor siga bajo el techo de la poblacion. Es la
			// lectura que este analito exige y que un rango de referencia no puede dar.
			if (SeSalioDeLoSuyo())
			{
				Medicion linea = NivelHabitual();
				return new Interpretacion(Veredicto.Empeorando, Severidad.ConsultePronto,
					"lectura.antigeno-prostatico.se-salio-de-lo-suyo", hechos,
					Accion.MostrarleEstoATuMedico(Plazo.EsteMes))
					.ConNivelPropio(linea.Valor);
			}

			if (!Ultima.EsCambioReal(hechos.Cambio))
			{
				// Estable pero por encima del techo de su edad sigue siendo algo que un
				// medico tiene que ver. Estable no significa bien — aunque, quieto, puede
				// esperar a la cita que ya ibas a tener.
				return Ultima.EsNormalPara(paciente)
					? new Interpretacion(Veredicto.Estable, Severidad.Informativo,
						"lectura.antigeno-prostatico.estable-dentro", hechos)
					: new Interpretacion(Veredicto.Estable, Severidad.ConsultePronto,
						"lectura.antigeno-prostatico.estable-fuera", hechos,
						Accion.MostrarleEstoATuMedico(Plazo.EnLaProximaCita));
			}

			return Ultima.Direccion.EsFavorable(hechos.Cambio)
				? new Interpretacion(Veredicto.Mejorando, Severidad.Informativo,
					"lectura.antigeno-prostatico.bajo", hechos)
				: new Interpretacion(Veredicto.Empeorando, Severidad.Vigilar,
					"lectura.antigeno-prostatico.subio", hechos,
					Accion.ConversarloConTuMedico(Plazo.EnLaProximaCita));
		}

		// Dos decimales para todos los bordes del histograma, ni mas ni menos.
		//
		// El "+ 0.00m" no es adorno y hace falta: decimal.Round REDUCE la escala pero no la
		// aumenta, asi que un 3.0 sigue saliendo "3.0" mientras un borde calculado sale
		// "2.00", y el mismo histograma muestra dos precisiones. Sumar cero con escala dos
		// la fuerza hacia arriba. Es la misma trampa de la escala del decimal que ya
		// aparecio en el journal, en otro lugar.
		private static decimal Borde(decimal valor) => decimal.Round(valor, 2) + 0.00m;
	}
}
