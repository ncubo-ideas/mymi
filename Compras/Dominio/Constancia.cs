using System;

namespace Compras.Dominio
{
	/// <summary>
	/// QUÉ TAN FIEL es un producto: "nunca falla", "casi nunca falta", "va y viene".
	/// </summary>
	// EXISTE POR LA MISMA RAZON QUE Cadencia: una proporcion no es una respuesta. "0,875"
	// es correcto y no se lo dice nadie; "casi nunca falta" es lo que la persona reconoce.
	// La ficha de producto del diseño de la app lo pide con esas palabras.
	//
	// LA DIFERENCIA CON Cadencia importa y conviene tenerla escrita: la cadencia dice CADA
	// CUANTO -en el tiempo-, la constancia dice EN CUANTAS DE LAS VECES -sobre las compras
	// miradas-. Un producto puede ser muy constante y poco frecuente: si va al super una
	// vez al mes y el arroz esta en todas, el arroz nunca falla y se compra una vez al mes.
	// Son dos preguntas y por eso son dos conjuntos.
	//
	// CONJUNTO CERRADO Y ORDENADO, comparado por referencia. Y NO JUZGA: "va y viene" no es
	// un reproche, es una descripcion — hay cosas que uno compra cuando se le antojan y eso
	// no tiene nada de malo.
	public sealed class Constancia
	{
		public static readonly Constancia NuncaFalla =
			new Constancia("nunca falla", "nunca falla", 1.0m, 4);

		public static readonly Constancia CasiNuncaFalta =
			new Constancia("casi nunca falta", "casi nunca falta", 0.75m, 3);

		public static readonly Constancia VaYViene =
			new Constancia("va y viene", "va y viene", 0.4m, 2);

		// El ultimo del conjunto: cualquier proporcion mas baja cae aca.
		public static readonly Constancia DeVezEnCuando =
			new Constancia("de vez en cuando", "de vez en cuando", 0m, 1);

		// De mas constante a menos: el orden en que se recorren para resolver.
		public static readonly Constancia[] Todas =
		{
			NuncaFalla, CasiNuncaFalta, VaYViene, DeVezEnCuando,
		};

		/// <summary>La etiqueta interna, para comparar y serializar.</summary>
		public string Nombre { get; private set; }

		/// <summary>Como se le dice a la persona: "casi nunca falta".</summary>
		public string ParaMostrar { get; private set; }

		/// <summary>La proporción mínima que hace falta para llegar a esta constancia.</summary>
		public decimal DesdeLaProporcion { get; private set; }

		private readonly int peso;

		private Constancia(string nombre, string paraMostrar, decimal desdeLaProporcion, int peso)
		{
			Nombre = nombre;
			ParaMostrar = paraMostrar;
			DesdeLaProporcion = desdeLaProporcion;
			this.peso = peso;
		}

		/// <summary>
		/// La constancia de una proporción de 0 a 1. TOTAL: cualquier proporción válida cae
		/// en una, porque la última arranca en cero.
		/// </summary>
		public static Constancia Para(decimal proporcion)
		{
			if (proporcion < 0m || proporcion > 1m)
				throw new ArgumentOutOfRangeException(nameof(proporcion), proporcion,
					"Una proporcion va de 0 a 1: no hay un producto que aparezca en mas veces "
					+ "de las que se miraron.");

			foreach (Constancia constancia in Todas)
				if (proporcion >= constancia.DesdeLaProporcion) return constancia;

			return DeVezEnCuando;   // inalcanzable: la ultima arranca en cero
		}

		/// <summary>Si esta constancia es más fiel que la otra.</summary>
		public bool EsMasFielQue(Constancia otra)
		{
			ArgumentNullException.ThrowIfNull(otra);
			return peso > otra.peso;
		}

		public override string ToString() => Nombre;
	}
}
