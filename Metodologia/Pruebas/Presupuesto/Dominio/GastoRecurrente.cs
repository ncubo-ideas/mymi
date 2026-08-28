using System;
using System.Collections.Generic;

namespace Presupuesto.Dominio
{
	// UN GASTO QUE VUELVE. Es una IDENTIDAD, no una cadena.
	//
	// POR QUE NO ES UN STRING. Es el mismo problema que Examenes resolvio con el analito,
	// y por la misma razon: el emisor no nombra igual dos veces. El recibo electrico llega
	// como "CNFL", "CNFL S.A." o "PAGO SERV ELECTRICO", y con el texto como llave esos
	// tres meses no forman UNA serie de tres puntos: forman TRES series de un punto. La
	// pregunta 1 de Preguntas.md -"se incremento el pago de un gasto fijo?"- se vuelve
	// incontestable justo en el caso para el que existe.
	//
	// PERO NO SE PUEDE COPIAR LA SOLUCION DE EXAMENES. Alla el conjunto de analitos es
	// CERRADO y viaja con la app: son cuatro y se saben de antemano. Los gastos recurrentes
	// de una persona son SUYOS y ABIERTOS -ningun modulo los puede traer precargados-, asi
	// que esta base es abierta y el conjunto lo construye cada quien.
	//
	// HUECO 1 - Preguntas.md juicio 1, declarado PENDIENTE ahi mismo: no se decidio como se
	// establece la identidad. Aca se toma la decision MINIMA: el gasto RECIBE los textos con
	// los que responde, no los deduce. Es la unica opcion que no inventa un juicio. Si
	// manana se decide deducirlos de la historia, eso entra sin tocar esta clase: cambia
	// quien llama a Reconoce().
	//
	// Cada subclase carga lo que ESA clase de compromiso sabe de si misma: cada cuanto se
	// espera, que tan grave es que falte, y si se puede reducir. Nada de eso es un campo
	// suelto que todos arrastren.
	internal abstract class GastoRecurrente
	{
		private readonly List<string> textos = new List<string>();
		private readonly List<Cargo> cargos = new List<Cargo>();

		protected GastoRecurrente(string nombre)
		{
			if (string.IsNullOrWhiteSpace(nombre))
				throw new ArgumentException("Un gasto recurrente necesita un nombre para la persona.", nameof(nombre));

			Nombre = nombre;
		}

		// Como lo llama la persona. Con acentos: es el que ella ve.
		internal string Nombre { get; }

		// ------------------------------------------------------------ lo que cada clase sabe

		// Juicio 3. Cada cuanto se espera que vuelva a aparecer.
		internal abstract Periodicidad Periodicidad { get; }

		// Juicio 5. QUE TAN GRAVE es que este cargo no aparezca. La decide la clase de
		// compromiso y NO el monto: por eso es abstracta y no un calculo sobre Dinero.
		internal abstract Severidad SeveridadSiFalta { get; }

		// Juicio 6. Si se puede recortar. Tres estados y no un bool, porque "no se sabe" es
		// distinto de "no": lo que no se deduce hay que preguntarlo, no asumirlo.
		internal abstract Reducibilidad Reducibilidad { get; }

		// Juicio 4. Cuanto se espera despues de la fecha de pago antes de avisar. Tres dias
		// por defecto -por fin de semana o feriado-, y una subclase puede ser mas estricta.
		internal virtual int DiasDeGracia => 3;

		// .Este gasto CONSUME, o solo mueve plata de un lado a otro? Casi todos consumen; el
		// pago de la tarjeta no, y por eso la pregunta existe. Quien sume totales tiene que
		// hacerla antes de sumar, o cada compra con tarjeta se cuenta dos veces. Ver
		// PagoDeTarjetaDeCredito, donde ademas esta marcado que es una inferencia.
		internal virtual bool EsConsumo => true;

		// ------------------------------------------------------------ identidad

		// El gasto RESPONDE a varios textos y sigue siendo uno solo.
		internal void RespondeTambienA(string textoDelBanco)
		{
			if (string.IsNullOrWhiteSpace(textoDelBanco))
				throw new ArgumentException("Un texto vacio no identifica nada.", nameof(textoDelBanco));

			string normalizado = Normalizar(textoDelBanco);
			if (!textos.Contains(normalizado)) textos.Add(normalizado);
		}

		internal bool Reconoce(string textoDelBanco) =>
			!string.IsNullOrWhiteSpace(textoDelBanco) && textos.Contains(Normalizar(textoDelBanco));

		// Mayusculas, espacios y puntos son ruido del emisor, no identidad. "CNFL S.A." y
		// "cnfl sa" son el mismo. Esto NO adivina: solo evita que la misma marca falle por
		// como la escribio el banco esa vez.
		private static string Normalizar(string texto)
		{
			var limpio = new System.Text.StringBuilder(texto.Length);
			foreach (char c in texto)
				if (char.IsLetterOrDigit(c)) limpio.Append(char.ToUpperInvariant(c));
			return limpio.ToString();
		}

		// ------------------------------------------------------------ la serie

		internal void Registrar(Cargo cargo)
		{
			if (cargo == null) throw new ArgumentNullException(nameof(cargo));
			cargos.Add(cargo);
			cargos.Sort((a, b) => a.Fecha.CompareTo(b.Fecha));
		}

		internal IReadOnlyList<Cargo> Cargos => cargos;

		internal bool HaySerie => cargos.Count >= 2;

		internal Cargo UltimoCargo => cargos.Count == 0 ? null : cargos[cargos.Count - 1];

		// ------------------------------------------------------------ la ausencia

		// Pregunta 4: "que pago fijo no me cobraron este mes?".
		//
		// LA FECHA ENTRA POR PARAMETRO. Si el dominio leyera el reloj, el mismo journal
		// reproducido manana daria otra respuesta. Saber que dia es hoy es derecho de la
		// superficie.
		internal CargoAusente EvaluarAusencia(DateTime hoy)
		{
			if (UltimoCargo == null) return CargoAusente.SinHistoria(this);

			DateTime esperado = Periodicidad.ProximoDespuesDe(UltimoCargo.Fecha);
			int diasDeAtraso = (int)(hoy.Date - esperado.Date).TotalDays;

			return diasDeAtraso > DiasDeGracia
				? CargoAusente.Vencido(this, esperado, diasDeAtraso)
				: CargoAusente.AlDia(this, esperado, diasDeAtraso);
		}

		public override string ToString() => Nombre;
	}
}
