using System;

namespace Gimnasio.Dominio
{
	// UNA MEDIDA DEL PERFIL CORPORAL.
	//
	// Es abstracta, y cada medida concreta es su propia clase. NO hay una clase unica
	// con un `tipo` adentro: eso obligaria a un switch en cada lectura, que es la
	// porosidad que el capitulo 4 manda evitar.
	//
	// Lo que hace distinta a cada una -y es el juicio 7 del Preguntas.md- son dos
	// cosas, y ninguna es cosmetica:
	//
	//   1. COMO SE INDEXA SU REFERENCIA. El rango de la grasa se mueve con el genero y
	//      la edad; el de la cintura solo con el genero. Cada una lo sabe de si misma.
	//
	//   2. SI LA APP LA LEE O LA LEYO EL APARATO. Desde el v3 el peso ya no se convierte
	//      a IMC: el IMC llega del gimnasio, evaluado (hueco 19).
	//
	// El perfil es PARAMETRO OBLIGATORIO y no un campo opcional: sin el no se puede
	// pedir una lectura, y eso lo hace cumplir el compilador.
	//
	// DESDE EL V3, LA MEDIDA PUEDE TRAER LA EVALUACION DEL APARATO al lado del valor
	// -"Increased", "High"-. Es un HECHO que llega, no un juicio de la app (N7). Es
	// opcional porque no toda medida la trae: la cintura se mide con cinta. Y viaja como
	// bool mas valor, con el getter guardado -la leccion de T1-.
	internal abstract class Medida
	{
		private EvaluacionDelAparato evaluacionDelAparato;

		internal decimal Valor { get; }

		protected Medida(decimal valor)
		{
			if (valor <= 0)
				throw new ArgumentOutOfRangeException(nameof(valor), valor,
					"Una medida corporal no puede ser cero ni negativa.");
			Valor = valor;
		}

		internal abstract string Nombre { get; }

		// Con que llave se busca la referencia de ESTA medida para ESTE perfil.
		// Es lo que hace que el perfil cargue peso de verdad y no sea ceremonia.
		internal abstract string ClaveDeReferencia(Perfil perfil);

		// Que numero se compara contra el rango, y como se llama esa magnitud. Desde el
		// v3 es siempre el propio valor: la excepcion era el peso, y el peso ya no se lee.
		internal virtual decimal ValorComparable(Perfil perfil) { return Valor; }
		internal virtual string QueSeCompara { get { return Nombre; } }

		// SI EL APARATO YA LA EVALUA, LA APP NO LA LEE CONTRA UN RANGO PROPIO (N7). Las
		// cinco medidas nuevas del v3 y el peso lo fijan en true; grasa, cintura y masa
		// muscular lo dejan en false y siguen leyendose por parametro.
		internal virtual bool ElAparatoLaEvalua { get { return false; } }

		// HACIA DONDE ES DESEABLE QUE SE MUEVA. Por defecto lo decide el objetivo
		// -juicio 1-, y una subclase puede fijarlo. Es herencia cargando verdad de
		// dominio: MasaMuscular no consulta el objetivo porque no le hace falta.
		internal virtual Direccion DireccionDeseable(Objetivo objetivo)
		{
			ArgumentNullException.ThrowIfNull(objetivo);
			return objetivo.DireccionPara(this);
		}

		internal Lectura Leer(Perfil perfil, Referencia referencia)
		{
			ArgumentNullException.ThrowIfNull(perfil);
			ArgumentNullException.ThrowIfNull(referencia);
			if (ElAparatoLaEvalua)
				throw new InvalidOperationException(
					$"'{Nombre}' la evaluo el aparato: su lectura llega como hecho y la app no la recalcula "
					+ "contra un rango propio (N7). Preguntar ElAparatoLaEvalua antes, y pedir la evaluacion.");
			return Lectura.De(Valor, ValorComparable(perfil), QueSeCompara, referencia);
		}

		// ------------------------------------------------ la evaluacion del aparato (v3)

		internal bool TieneEvaluacionDelAparato { get { return evaluacionDelAparato != null; } }

		internal EvaluacionDelAparato EvaluacionDelAparato
		{
			get
			{
				if (evaluacionDelAparato == null)
					throw new InvalidOperationException(
						$"'{Nombre}' llego sin evaluacion del aparato. Preguntar TieneEvaluacionDelAparato antes.");
				return evaluacionDelAparato;
			}
		}

		// Se anota UNA vez: la misma medida con dos palabras del aparato no se puede
		// distinguir. Devuelve la medida para poder encadenarlo desde un script.
		internal Medida ConEvaluacion(EvaluacionDelAparato evaluacion)
		{
			ArgumentNullException.ThrowIfNull(evaluacion);
			if (evaluacionDelAparato != null)
				throw new InvalidOperationException(
					$"'{Nombre}' ya tenia la evaluacion del aparato ('{evaluacionDelAparato}').");
			evaluacionDelAparato = evaluacion;
			return this;
		}

		internal bool EsDeLaMismaClaseQue(Medida otra)
		{
			return otra != null && otra.Nombre == Nombre;
		}

		// RESOLUTOR TOTAL DEL CONJUNTO CERRADO DE MEDIDAS.
		//
		// No estaba, y lo pidio el perform. En C# se construye la subclase directo
		// -new Cintura(88)-; desde un script no se puede: un ternario no unifica dos
		// subclases a su base comun (regla 12) y un conjunto cerrado sin resolutor es
		// inalcanzable desde un comando (regla 11).
		//
		// El v3 agrega las cinco del aparato. Nueve medidas: las cuatro de la entrevista
		// y las cinco que el papel trajo.
		internal static Medida Crear(string nombre, decimal valor)
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(nombre);
			switch (nombre.Trim().ToLowerInvariant())
			{
				case "peso": return new Peso(valor);
				case "grasa": return new PorcentajeDeGrasa(valor);
				case "cintura": return new Cintura(valor);
				case "masa-muscular": return new MasaMuscular(valor);
				case "imc": return new IndiceDeMasaCorporal(valor);
				case "agua": return new Agua(valor);
				case "metabolismo-basal": return new MetabolismoBasal(valor);
				case "grasa-visceral": return new GrasaVisceral(valor);
				case "masa-osea": return new MasaOsea(valor);
				default:
					throw new ArgumentOutOfRangeException(nameof(nombre), nombre,
						"Medida desconocida. Las que existen son: peso, grasa, cintura, masa-muscular, "
						+ "imc, agua, metabolismo-basal, grasa-visceral, masa-osea.");
			}
		}

		// El predicado total que acompana al resolutor: es lo que permite preguntar
		// desde un Check antes de escribir, en vez de reventar a mitad del comando.
		internal static bool EsUnNombreConocido(string nombre)
		{
			if (string.IsNullOrWhiteSpace(nombre)) return false;
			switch (nombre.Trim().ToLowerInvariant())
			{
				case "peso":
				case "grasa":
				case "cintura":
				case "masa-muscular":
				case "imc":
				case "agua":
				case "metabolismo-basal":
				case "grasa-visceral":
				case "masa-osea": return true;
				default: return false;
			}
		}

		// Los porcentajes no llegan a 100. Es la regla que PuedeRegistrarMedida necesita
		// antes de construir, y que antes vivia repetida en la raiz con un string suelto.
		internal static bool EsUnPorcentaje(string nombre)
		{
			if (string.IsNullOrWhiteSpace(nombre)) return false;
			string n = nombre.Trim().ToLowerInvariant();
			return n == "grasa" || n == "agua";
		}
	}
}
