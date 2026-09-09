using System;
using System.Collections.Generic;

namespace Gimnasio.Dominio
{
	// LA RAIZ. Se llama Trayectoria y no Gimnasio porque la raiz NUNCA se llama como el
	// negocio: el namespace y el assembly ya se llaman asi. Los otros negocios usan
	// Expediente, Historial, Libro, Panorama.
	//
	// Guarda lo que el gimnasio manda -perfil, objetivo, planes, mediciones,
	// asistencias, y desde el v2 la membresia- y contesta las preguntas del
	// Preguntas.md. Nada de lo que hay aca lo aporto Sonia: por eso el journal es
	// reconstruible.
	//
	// EL RELOJ NO ENTRA. Ninguna operacion mira el dia de hoy; cuando hace falta una
	// fecha de corte, entra por parametro.
	internal sealed class Trayectoria
	{
		private readonly List<Plan> planes = new List<Plan>();
		private readonly List<Medicion> mediciones = new List<Medicion>();
		private readonly List<DateTime> asistencias = new List<DateTime>();

		// LA MEMBRESIA EXISTE DESDE QUE SONIA ES SOCIA, asi que nace con la trayectoria y
		// nunca es null: lo que puede faltar es su ESTADO, y eso lo guarda ella adentro
		// (TieneEstadoDeclarado). Es lo que el v1 no tenia -huecos 9 y 12-: la regla del
		// descarte mal escrita la dejo afuera del modelo entero.
		private readonly Membresia membresia = new Membresia();

		internal Perfil Perfil { get; private set; }
		internal Objetivo Objetivo { get; private set; }
		internal Membresia Membresia { get { return membresia; } }

		internal IReadOnlyList<Plan> Planes { get { return planes; } }
		internal IReadOnlyList<Medicion> Mediciones { get { return mediciones; } }
		internal int CuantasAsistencias { get { return asistencias.Count; } }

		// ------------------------------------------------------------------ lo que entra

		internal void DeclararPerfil(Perfil perfil)
		{
			ArgumentNullException.ThrowIfNull(perfil);
			Perfil = perfil;
		}

		internal void DeclararObjetivo(Objetivo objetivo)
		{
			ArgumentNullException.ThrowIfNull(objetivo);
			Objetivo = objetivo;
		}

		internal void Agregar(Plan elPlan)
		{
			ArgumentNullException.ThrowIfNull(elPlan);
			planes.Add(elPlan);
		}

		internal void Agregar(Medicion laMedicion)
		{
			ArgumentNullException.ThrowIfNull(laMedicion);
			mediciones.Add(laMedicion);
		}

		internal void Asistio(DateTime dia)
		{
			asistencias.Add(dia.Date);
		}

		// ------------------------------------------------------------------ predicados

		internal bool TienePerfil { get { return Perfil != null; } }
		internal bool TieneObjetivo { get { return Objetivo != null; } }

		// Cuantas mediciones traen esta medida. Total: contesta para cualquier nombre.
		internal int CuantasVecesSeMidio(string nombreDeLaMedida)
		{
			int cuantas = 0;
			foreach (Medicion m in mediciones)
				if (m.Trae(nombreDeLaMedida)) cuantas++;
			return cuantas;
		}

		internal bool SePuedeComparar(string nombreDeLaMedida)
		{
			return CuantasVecesSeMidio(nombreDeLaMedida) >= 2;
		}

		// ---------------------------------------- como estoy hoy, y como vengo desde el inicio

		// La lectura de la ultima medicion: bajo, normal o alto. Necesita el perfil, y
		// el compilador lo hace cumplir porque Leer lo exige.
		internal Lectura ComoEstoy(string nombreDeLaMedida, Referencia referencia)
		{
			ExigirPerfil();
			if (CuantasVecesSeMidio(nombreDeLaMedida) == 0)
				throw new InvalidOperationException(
					"Nunca te midieron esa medida. Preguntar CuantasVecesSeMidio antes.");

			return UltimaCon(nombreDeLaMedida).Traida(nombreDeLaMedida).Leer(Perfil, referencia);
		}

		// El movimiento desde la primera medicion hasta la ultima. Es el OTRO juicio, y
		// no se deduce de la lectura: se puede estar alto y mejorando.
		internal Tendencia ComoVengo(string nombreDeLaMedida, Referencia referencia)
		{
			ExigirPerfil();
			ExigirObjetivo();
			if (!SePuedeComparar(nombreDeLaMedida))
				throw new InvalidOperationException(
					"Hace falta mas de una medicion para decir como venis. "
					+ "Preguntar SePuedeComparar antes.");

			Medida primera = PrimeraCon(nombreDeLaMedida).Traida(nombreDeLaMedida);
			Medida ultima = UltimaCon(nombreDeLaMedida).Traida(nombreDeLaMedida);

			return Tendencia.Entre(primera, ultima, ultima.DireccionDeseable(Objetivo), referencia);
		}

		// ------------------------------------------------- la progresion de lo prescrito

		internal bool SePuedeVerLaProgresionDe(string nombreDelEjercicio)
		{
			int cuantos = 0;
			foreach (Plan p in planes)
				if (p.Prescribe(nombreDelEjercicio)) cuantos++;
			return cuantos >= 2;
		}

		// COMO CAMBIO LO QUE LE PRESCRIBEN (N8, v3): del primer plan que trae el ejercicio
		// al ultimo, sobre lo que el papel SI trae -volumen para fuerza, minutos para
		// cardio-. Total el predicado: dos planes, y la misma forma en los dos. Un
		// ejercicio que paso de fuerza a cardio no se compara, y se dice.
		internal bool SePuedeCompararLoPrescrito(string nombreDelEjercicio)
		{
			if (!SePuedeVerLaProgresionDe(nombreDelEjercicio)) return false;
			Prescripcion primera = PrimerPlanQuePrescribe(nombreDelEjercicio).Prescrito(nombreDelEjercicio).Prescripcion;
			Prescripcion ultima = UltimoPlanQuePrescribe(nombreDelEjercicio).Prescrito(nombreDelEjercicio).Prescripcion;
			return primera.EsDeLaMismaFormaQue(ultima);
		}

		internal Progresion ComoCambioLoPrescrito(string nombreDelEjercicio)
		{
			if (!SePuedeCompararLoPrescrito(nombreDelEjercicio))
				throw new InvalidOperationException(
					"Ese ejercicio aparece en menos de dos planes, o se prescribe de dos formas distintas. "
					+ "Preguntar SePuedeCompararLoPrescrito antes.");

			return new Progresion(nombreDelEjercicio,
				PrimerPlanQuePrescribe(nombreDelEjercicio).Prescrito(nombreDelEjercicio).Prescripcion,
				UltimoPlanQuePrescribe(nombreDelEjercicio).Prescrito(nombreDelEjercicio).Prescripcion);
		}

		// LA PREGUNTA DEL V2, GUARDADA: cuanto subio lo que le PIDEN levantar. Desde el v3
		// el peso es opcional -el papel no lo trae-, asi que solo se contesta cuando el
		// primer y el ultimo plan que traen el ejercicio lo indican. Si el detalle del
		// ejercicio algun dia trae peso (hueco 17), esta pregunta vuelve a vivir sola.
		internal bool SePuedeVerLaProgresionDelPeso(string nombreDelEjercicio)
		{
			return SePuedeVerLaProgresionDe(nombreDelEjercicio)
				&& PrimerPlanQuePrescribe(nombreDelEjercicio).TienePesoIndicadoPara(nombreDelEjercicio)
				&& UltimoPlanQuePrescribe(nombreDelEjercicio).TienePesoIndicadoPara(nombreDelEjercicio);
		}

		internal decimal CuantoSubioLoIndicado(string nombreDelEjercicio)
		{
			if (!SePuedeVerLaProgresionDelPeso(nombreDelEjercicio))
				throw new InvalidOperationException(
					"Ese ejercicio aparece en menos de dos planes, o alguno no indica peso -el papel no lo trae-. "
					+ "Preguntar SePuedeVerLaProgresionDelPeso antes.");

			return UltimoPlanQuePrescribe(nombreDelEjercicio).PesoIndicadoDe(nombreDelEjercicio)
				- PrimerPlanQuePrescribe(nombreDelEjercicio).PesoIndicadoDe(nombreDelEjercicio);
		}

		private Plan PrimerPlanQuePrescribe(string nombreDelEjercicio)
		{
			foreach (Plan p in planes)
				if (p.Prescribe(nombreDelEjercicio)) return p;
			throw new KeyNotFoundException("Ningun plan prescribe ese ejercicio.");
		}

		private Plan UltimoPlanQuePrescribe(string nombreDelEjercicio)
		{
			Plan encontrado = null;
			foreach (Plan p in planes)
				if (p.Prescribe(nombreDelEjercicio)) encontrado = p;
			if (encontrado == null)
				throw new KeyNotFoundException("Ningun plan prescribe ese ejercicio.");
			return encontrado;
		}

		// ------------------------------------------ lo que dijo el aparato (N7, v3)

		// Si la ultima medicion que trae esta medida vino con la palabra del aparato.
		// Total: contesta que no para una medida que nunca se midio o llego sin ella.
		internal bool ElAparatoEvaluo(string nombreDeLaMedida)
		{
			if (CuantasVecesSeMidio(nombreDeLaMedida) == 0) return false;
			return UltimaCon(nombreDeLaMedida).Traida(nombreDeLaMedida).TieneEvaluacionDelAparato;
		}

		// La evaluacion del aparato sobre la ultima medicion. Es la LECTURA de esa
		// medida, y no la calcula la app: la registro. Al lado va ComoVengo, que si es de
		// la app: el aparato dice "Increased" hoy; no dice "y viene bajando".
		internal EvaluacionDelAparato LoQueDijoElAparato(string nombreDeLaMedida)
		{
			if (!ElAparatoEvaluo(nombreDeLaMedida))
				throw new InvalidOperationException(
					"El aparato no evaluo esa medida, o nunca se midio. Preguntar ElAparatoEvaluo antes.");
			return UltimaCon(nombreDeLaMedida).Traida(nombreDeLaMedida).EvaluacionDelAparato;
		}

		// ------------------------------------------------------------ el incumplimiento

		internal Incumplimiento IncumplimientoDe(Plan elPlan)
		{
			ArgumentNullException.ThrowIfNull(elPlan);
			if (!elPlan.Termino)
				throw new InvalidOperationException(
					"Un plan que no termino no se puede juzgar todavia: le faltan dias por vivir. "
					+ "Preguntar Termino antes.");

			int fue = 0;
			foreach (DateTime dia in asistencias)
				if (dia >= elPlan.Inicio.Date && dia <= elPlan.Fin.Date) fue++;

			// EL HUECO 9, CERRADO (juicio N2 del v2): los dias en que la membresia estuvo
			// en mora dentro del plan no se esperan. La mora la dijo el gimnasio con su
			// bandera; la app no la deduce. Se descuenta en semanas enteras, la misma ley
			// con que el plan cuenta su duracion.
			//
			// Las asistencias que hubiera DURANTE la mora se cuentan igual: son hechos que
			// el gimnasio mando, y un hecho no se borra porque contradiga una regla. Lo
			// que cambia es lo esperado, no lo ocurrido.
			int diasInactiva = membresia.DiasInactivaEntre(elPlan.Inicio, elPlan.Fin);
			int semanasInactiva = diasInactiva / 7;
			int esperadosSinMora = elPlan.DiasEsperadosEnTodoElPlan();
			int esperados = elPlan.DiasEsperadosDescontando(semanasInactiva);

			return new Incumplimiento(esperados, fue, esperadosSinMora - esperados);
		}

		// -------------------------------------------------- que falta para la meta

		internal bool SePuedeDecirCuantoFalta(string nombreDeLaMedida)
		{
			if (!TieneObjetivo || CuantasVecesSeMidio(nombreDeLaMedida) == 0) return false;
			Medida ultima = UltimaCon(nombreDeLaMedida).Traida(nombreDeLaMedida);
			return Objetivo.TieneMetaPara(ultima) && Objetivo.MetaDe(ultima).TieneValorDeseado;
		}

		internal decimal CuantoFaltaPara(string nombreDeLaMedida)
		{
			if (!SePuedeDecirCuantoFalta(nombreDeLaMedida))
				throw new InvalidOperationException(
					"No se puede decir cuanto falta: hace falta una medicion y una meta CON valor. "
					+ "Preguntar SePuedeDecirCuantoFalta antes.");

			Medida ultima = UltimaCon(nombreDeLaMedida).Traida(nombreDeLaMedida);
			return Objetivo.MetaDe(ultima).CuantoFalta(ultima.Valor);
		}

		// ------------------------------------------ la ausencia: plan sin medicion

		// Planes que terminaron y a los que no siguio ninguna medicion dentro de los
		// dias de gracia. Es lo que ninguna pantalla puede mostrar, porque una pantalla
		// renderiza lo que tiene y esto es lo que falta.
		//
		// HUECO: los dias de gracia entran por parametro porque NADIE dijo cuantos son.
		// "Se toman medidas al final de cada plan" no dice si al dia siguiente o tres
		// semanas despues.
		internal IReadOnlyList<Plan> PlanesQueTerminaronSinMedicion(int diasDeGracia)
		{
			if (diasDeGracia < 0)
				throw new ArgumentOutOfRangeException(nameof(diasDeGracia), diasDeGracia,
					"Los dias de gracia no pueden ser negativos.");

			List<Plan> sinMedir = new List<Plan>();
			foreach (Plan p in planes)
			{
				if (!p.Termino) continue;
				bool loMidieron = false;
				foreach (Medicion m in mediciones)
					if (m.Fecha.Date >= p.Fin.Date && (m.Fecha.Date - p.Fin.Date).TotalDays <= diasDeGracia)
						loMidieron = true;
				if (!loMidieron) sinMedir.Add(p);
			}
			return sinMedir;
		}


		// ------------------------------------------- lo que pidio el perform y no estaba

		// El Check llega a la raiz, no al objetivo. Sin este paso intermedio el
		// predicado existe y es inalcanzable desde un comando.
		internal bool SePuedeAgregarMetaPara(string nombreDeLaMedida)
		{
			return TieneObjetivo && !Objetivo.YaTieneMetaPara(nombreDeLaMedida);
		}

		// EL PLAN NECESITA UN NOMBRE. En C# alcanzaba con la referencia al objeto; desde
		// un script hay que poder decir CUAL plan, y el gimnasio no le pone identificador
		// al papel. Se usa la fecha de inicio, que es lo unico que lo distingue hoy.
		// Ver el hueco 10: si dos planes empezaran el mismo dia, esto se cae.
		internal bool TienePlanQueEmpezo(DateTime inicio)
		{
			foreach (Plan p in planes)
				if (p.Inicio.Date == inicio.Date) return true;
			return false;
		}

		internal Plan PlanQueEmpezo(DateTime inicio)
		{
			foreach (Plan p in planes)
				if (p.Inicio.Date == inicio.Date) return p;
			throw new KeyNotFoundException(
				"No hay ningun plan que empiece ese dia. Preguntar TienePlanQueEmpezo antes.");
		}

		// Predicado total para el Check de terminar: junta las dos razones por las que
		// terminar fallaria, para que el comando no se escriba y despues reviente.
		internal bool SePuedeTerminarElPlanQueEmpezo(DateTime inicio)
		{
			foreach (Plan p in planes)
				if (p.Inicio.Date == inicio.Date) return !p.Termino;
			return false;
		}

		// LA HOJA SE ABRE AL LLEGAR SU PRIMERA MEDIDA. El gimnasio manda una hoja, pero
		// los eventos llegan medida por medida: la primera abre la hoja y las demas se
		// suman a la misma fecha. Sin esto haria falta un comando de "abrir hoja" que el
		// proveedor nunca va a mandar.
		internal Medicion AbrirMedicionDe(DateTime fecha)
		{
			foreach (Medicion m in mediciones)
				if (m.Fecha.Date == fecha.Date) return m;
			Medicion nueva = new Medicion(fecha);
			mediciones.Add(nueva);
			return nueva;
		}


		// ------------------------------------- UN PREDICADO POR COMANDO: "esto va a entrar?"
		//
		// Esta seccion entera la pidio el perform, y me costo tres journals envenenados
		// antes de ver el patron.
		//
		// La regla 1 del substrato dice "guarda con un Check todo comando que el dominio
		// pueda rechazar". Lo que no dice, y es lo que muerde, es que EL CHECK TIENE QUE
		// PREGUNTAR EXACTAMENTE LO QUE EL DOMINIO VA A RECHAZAR. Un Check que pregunta
		// otra cosa -aunque sea razonable- deja pasar el comando, el dominio revienta a
		// mitad de la escritura, y esa entrada se replaya en cada arranque para siempre.
		//
		// De ahi la simetria: por cada comando, un predicado TOTAL en la raiz que junta
		// todas las razones por las que ese comando fallaria.


		// EL COMANDO QUE NO PODIA FALLAR, Y ERA EL PEOR.
		//
		// Asistio no rechaza nada: agrega un dia a una lista. Por eso no le puse Check.
		// Y en la segunda corrida conto los mismos 30 dias otra vez: 60 asistencias,
		// incumplimiento cero, "cumplio: true". NO FALLO — dio una respuesta creible y
		// equivocada, que es peor que reventar.
		//
		// Un comando que no puede fallar igual necesita idempotencia si el mismo hecho
		// puede volver a llegar. Y siempre puede: un replay es exactamente eso.
		internal bool PuedeRegistrarAsistencia(DateTime dia)
		{
			foreach (DateTime ya in asistencias)
				if (ya.Date == dia.Date) return false;
			return true;
		}

		internal bool PuedeDeclararPerfil(string genero, int edad, decimal altura)
		{
			return !TienePerfil
				&& Dominio.Genero.EsUnNombreConocido(genero)
				&& edad > 0
				&& altura > 0;
		}

		internal bool PuedeRegistrarPlan(DateTime inicio, int diasPorSemana)
		{
			return !TienePlanQueEmpezo(inicio)
				&& diasPorSemana >= 1 && diasPorSemana <= 7;
		}

		// LAS DOS FORMAS DE PRESCRIBIR DEL PAPEL (v3), cada una con su predicado total que
		// junta todo lo que el constructor rechazaria (regla 17).
		internal bool PuedePrescribirFuerza(DateTime inicioDelPlan, string nombreDelEjercicio,
			int series, int repeticionesDesde, int repeticionesHasta, int descansoEnSegundos)
		{
			return PuedePrescribir(inicioDelPlan, nombreDelEjercicio)
				&& series > 0 && repeticionesDesde > 0 && repeticionesHasta >= repeticionesDesde
				&& descansoEnSegundos >= 0;
		}

		internal bool PuedePrescribirCardio(DateTime inicioDelPlan, string nombreDelEjercicio, int minutos)
		{
			return PuedePrescribir(inicioDelPlan, nombreDelEjercicio) && minutos > 0;
		}

		private bool PuedePrescribir(DateTime inicioDelPlan, string nombreDelEjercicio)
		{
			if (!TienePlanQueEmpezo(inicioDelPlan)) return false;
			if (string.IsNullOrWhiteSpace(nombreDelEjercicio)) return false;
			return !PlanQueEmpezo(inicioDelPlan).Prescribe(nombreDelEjercicio);
		}

		// El peso es un hecho aparte sobre un ejercicio ya prescrito -solo cuando la maquina
		// lo requiere-, y entra UNA vez (regla 18).
		internal bool PuedeIndicarPeso(DateTime inicioDelPlan, string nombreDelEjercicio, decimal peso)
		{
			if (peso <= 0) return false;
			if (!TienePlanQueEmpezo(inicioDelPlan)) return false;
			Plan p = PlanQueEmpezo(inicioDelPlan);
			return p.Prescribe(nombreDelEjercicio) && !p.Prescrito(nombreDelEjercicio).TienePesoIndicado;
		}

		internal bool PuedeTerminarPlan(DateTime inicioDelPlan, DateTime fin)
		{
			if (!TienePlanQueEmpezo(inicioDelPlan)) return false;
			Plan p = PlanQueEmpezo(inicioDelPlan);
			return !p.Termino && fin.Date >= p.Inicio.Date;
		}

		internal bool PuedeRegistrarMedida(DateTime fecha, string queSeMidio, decimal valor)
		{
			if (!Medida.EsUnNombreConocido(queSeMidio)) return false;
			if (valor <= 0) return false;
			if (Medida.EsUnPorcentaje(queSeMidio) && valor >= 100m) return false;
			foreach (Medicion m in mediciones)
				if (m.Fecha.Date == fecha.Date) return !m.Trae(queSeMidio);
			return true;
		}

		// La misma medida, con la palabra del aparato al lado (N7). Rechaza ademas una
		// palabra que el papel no mostro.
		internal bool PuedeRegistrarMedidaEvaluada(DateTime fecha, string queSeMidio, decimal valor, string evaluacion)
		{
			return PuedeRegistrarMedida(fecha, queSeMidio, valor)
				&& EvaluacionDelAparato.EsUnNombreConocido(evaluacion);
		}

		// ------------------------------------------ los tres comandos de la membresia (v2)
		//
		// El Check llega a la raiz, no a la membresia: igual que SePuedeAgregarMetaPara.
		// Cada uno delega en el predicado total de Membresia, que junta todas las razones
		// por las que ese comando fallaria (regla 17). El de la bandera rechaza ademas el
		// MISMO estado dos veces -es el mismo hecho otra vez, regla 18-: la leccion de
		// Asistio, que no podia fallar y conto 30 dias dos veces.

		internal bool PuedeCambiarEstadoDeMembresia(string nombreDelEstado, DateTime fecha)
		{
			return membresia.PuedeCambiarA(nombreDelEstado, fecha);
		}

		internal bool PuedeRegistrarPago(DateTime fecha, decimal monto)
		{
			return membresia.PuedeRegistrarPago(fecha, monto);
		}

		internal bool PuedeAnunciarCobro(DateTime vence, decimal monto, string etiqueta,
			DateTime cubreDesde, DateTime cubreHasta, string nombreDelEstado)
		{
			return membresia.PuedeAnunciarCobro(vence, monto, etiqueta, cubreDesde, cubreHasta, nombreDelEstado);
		}

		internal bool PuedeContratar(string producto, int diaDeVencimiento, DateTime fecha)
		{
			return membresia.PuedeContratar(producto, diaDeVencimiento, fecha);
		}

		internal bool PuedeCambiarEstadoDeCobro(DateTime vence, string nombreDelEstado)
		{
			return membresia.PuedeCambiarEstadoDeCobro(vence, nombreDelEstado);
		}

		// ------------------------------------------------------------------ internos

		private void ExigirPerfil()
		{
			if (!TienePerfil)
				throw new InvalidOperationException(
					"Sin perfil no se puede interpretar ningun numero: la lectura necesita genero, "
					+ "edad y altura.");
		}

		private void ExigirObjetivo()
		{
			if (!TieneObjetivo)
				throw new InvalidOperationException(
					"Sin objetivo declarado no hay direccion deseable, y sin direccion no se puede "
					+ "decir si un movimiento mejora o empeora.");
		}

		private Medicion PrimeraCon(string nombreDeLaMedida)
		{
			foreach (Medicion m in mediciones)
				if (m.Trae(nombreDeLaMedida)) return m;
			throw new KeyNotFoundException("Ninguna medicion trae esa medida.");
		}

		private Medicion UltimaCon(string nombreDeLaMedida)
		{
			Medicion encontrada = null;
			foreach (Medicion m in mediciones)
				if (m.Trae(nombreDeLaMedida)) encontrada = m;
			if (encontrada == null)
				throw new KeyNotFoundException("Ninguna medicion trae esa medida.");
			return encontrada;
		}
	}
}
