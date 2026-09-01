using System;
using System.Collections.Generic;

namespace Gimnasio.Dominio
{
	// LA RAIZ. Se llama Trayectoria y no Gimnasio porque la raiz NUNCA se llama como el
	// negocio: el namespace y el assembly ya se llaman asi. Los otros negocios usan
	// Expediente, Historial, Libro, Panorama.
	//
	// Guarda lo que el gimnasio manda -perfil, objetivo, planes, mediciones,
	// asistencias- y contesta las preguntas del Preguntas.md. Nada de lo que hay aca lo
	// aporto Sonia: por eso el journal es reconstruible.
	//
	// EL RELOJ NO ENTRA. Ninguna operacion mira el dia de hoy; cuando hace falta una
	// fecha de corte, entra por parametro.
	internal sealed class Trayectoria
	{
		private readonly List<Plan> planes = new List<Plan>();
		private readonly List<Medicion> mediciones = new List<Medicion>();
		private readonly List<DateTime> asistencias = new List<DateTime>();

		internal Perfil Perfil { get; private set; }
		internal Objetivo Objetivo { get; private set; }

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

		// ------------------------------------------------- la progresion de lo indicado

		internal bool SePuedeVerLaProgresionDe(string nombreDelEjercicio)
		{
			int cuantos = 0;
			foreach (Plan p in planes)
				if (p.Prescribe(nombreDelEjercicio)) cuantos++;
			return cuantos >= 2;
		}

		// Cuanto subio lo que el gimnasio le PIDE levantar. No es lo que levanto: eso no
		// lo registra nadie, y el archivo lo dice.
		internal decimal CuantoSubioLoIndicado(string nombreDelEjercicio)
		{
			if (!SePuedeVerLaProgresionDe(nombreDelEjercicio))
				throw new InvalidOperationException(
					"Ese ejercicio aparece en menos de dos planes, asi que no hay progresion. "
					+ "Preguntar SePuedeVerLaProgresionDe antes.");

			decimal primero = 0m, ultimo = 0m;
			bool hayPrimero = false;
			foreach (Plan p in planes)
			{
				if (!p.Prescribe(nombreDelEjercicio)) continue;
				if (!hayPrimero) { primero = p.PesoIndicadoDe(nombreDelEjercicio); hayPrimero = true; }
				ultimo = p.PesoIndicadoDe(nombreDelEjercicio);
			}
			return ultimo - primero;
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

			return new Incumplimiento(elPlan.DiasEsperadosEnTodoElPlan(), fue);
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
