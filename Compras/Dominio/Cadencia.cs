using System;

namespace Compras.Dominio
{
	/// <summary>
	/// CADA CUÁNTO, dicho como lo dice una persona: "todas las semanas", "una vez al mes".
	/// </summary>
	// EXISTE PORQUE UN NUMERO DE DIAS NO ES UNA RESPUESTA. El ritmo se mide en dias -la
	// mediana de los intervalos- y eso esta bien para calcular; pero cuando hay que
	// DECIRLO, "cada 6.5 dias" no es como habla nadie. La pregunta que la persona se hace
	// es "¿cada cuanto compro yo esto?", y la respuesta que reconoce es "todas las
	// semanas".
	//
	// La wiki lo pide en esas palabras: la respuesta esperada de S10 dice que el helado
	// "paso de 1 vez al mes a casi todas las semanas". Y el diseño de la app lo dibuja
	// igual: "Antes 1 vez/mes -> Ahora 1 vez/semana".
	//
	// CONJUNTO CERRADO Y ORDENADO, comparado por referencia. Cerrado porque una cadencia
	// inventada en una pantalla es prosa libre otra vez; ordenado porque "todas las
	// semanas" es mas seguido que "una vez al mes" y alguna superficie va a querer decir
	// cual de las dos aprieta mas.
	//
	// LOS CORTES SON UN JUICIO NUESTRO, y conviene que se vea: nueve dias para que una
	// compra semanal que se corrio un par de dias siga leyendose "todas las semanas", y
	// dieciocho para que la quincenal aguante lo mismo. Un habito no es un reloj.
	public sealed class Cadencia
	{
		/// <summary>Hasta cuántos días de intervalo cuenta como "todas las semanas".</summary>
		public const int DiasDeLoSemanal = 9;

		public static readonly Cadencia TodasLasSemanas =
			new Cadencia("todas las semanas", "todas las semanas", "1 vez/semana", DiasDeLoSemanal, 5);

		public static readonly Cadencia CadaDosSemanas =
			new Cadencia("cada dos semanas", "cada dos semanas", "1 vez/quincena", 18, 4);

		public static readonly Cadencia UnaVezAlMes =
			new Cadencia("una vez al mes", "una vez al mes", "1 vez/mes", 45, 3);

		public static readonly Cadencia CadaDosMeses =
			new Cadencia("cada dos meses", "cada dos meses", "1 vez/2 meses", 75, 2);

		// El ultimo del conjunto: cualquier intervalo mas largo cae aca. No dice un numero
		// a proposito — a partir de dos meses y medio, "de vez en cuando" es mas honesto
		// que fingir una regularidad que no hay.
		public static readonly Cadencia DeVezEnCuando =
			new Cadencia("de vez en cuando", "de vez en cuando", "de vez en cuando", int.MaxValue, 1);

		public static readonly Cadencia[] Todas =
		{
			TodasLasSemanas, CadaDosSemanas, UnaVezAlMes, CadaDosMeses, DeVezEnCuando,
		};

		/// <summary>La etiqueta interna, para comparar y serializar.</summary>
		public string Nombre { get; private set; }

		/// <summary>Como se le dice a la persona, en una frase que encadena: "compra leche todas las semanas".</summary>
		public string ParaMostrar { get; private set; }

		/// <summary>La forma corta, para una pastilla: "1 vez/semana".</summary>
		public string Compacta { get; private set; }

		private readonly int hastaDias;
		private readonly int peso;

		private Cadencia(string nombre, string paraMostrar, string compacta, int hastaDias, int peso)
		{
			Nombre = nombre;
			ParaMostrar = paraMostrar;
			Compacta = compacta;
			this.hastaDias = hastaDias;
			this.peso = peso;
		}

		/// <summary>
		/// La cadencia de un intervalo típico en días. TOTAL: cualquier cantidad de días
		/// cae en una, porque la última no tiene techo.
		/// </summary>
		public static Cadencia Para(int dias)
		{
			if (dias <= 0)
				throw new ArgumentOutOfRangeException(nameof(dias), dias,
					"Un intervalo de cero dias o menos no es una cadencia.");

			foreach (Cadencia cadencia in Todas)
				if (dias <= cadencia.hastaDias) return cadencia;

			return DeVezEnCuando;   // inalcanzable: la ultima no tiene techo
		}

		/// <summary>Si esta cadencia es más seguida que la otra: semanal contra mensual.</summary>
		public bool EsMasSeguidaQue(Cadencia otra)
		{
			ArgumentNullException.ThrowIfNull(otra);
			return peso > otra.peso;
		}

		public override string ToString() => Nombre;
	}
}
