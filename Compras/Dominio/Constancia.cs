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
	internal sealed class Constancia
	{
		internal static readonly Constancia NuncaFalla = new Constancia("nunca-falla", 1.0m, 4);

		internal static readonly Constancia CasiNuncaFalta = new Constancia("casi-nunca-falta", 0.75m, 3);

		internal static readonly Constancia VaYViene = new Constancia("va-y-viene", 0.4m, 2);

		// El ultimo del conjunto: cualquier proporcion mas baja cae aca.
		internal static readonly Constancia DeVezEnCuando = new Constancia("de-vez-en-cuando", 0m, 1);

		// De mas constante a menos: el orden en que se recorren para resolver.
		internal static readonly Constancia[] Todas =
		{
			NuncaFalla, CasiNuncaFalta, VaYViene, DeVezEnCuando,
		};

		/// <summary>
		/// EL CODIGO de esta constancia: identidad, para comparar y serializar. Es lo unico que cruza el
		/// wire, y con el una capa de presentacion busca como se dice en el idioma que le pidieron.
		/// </summary>
		internal string Clave { get; private set; }

		/// <summary>La proporción mínima que hace falta para llegar a esta constancia.</summary>
		internal decimal DesdeLaProporcion { get; private set; }

		private readonly int peso;

		private Constancia(string clave, decimal desdeLaProporcion, int peso)
		{
			Clave = clave;
			DesdeLaProporcion = desdeLaProporcion;
			this.peso = peso;
		}

		/// <summary>
		/// La constancia de una proporción de 0 a 1. TOTAL: cualquier proporción válida cae
		/// en una, porque la última arranca en cero.
		/// </summary>
		internal static Constancia Para(decimal proporcion)
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
		internal bool EsMasFielQue(Constancia otra)
		{
			ArgumentNullException.ThrowIfNull(otra);
			return peso > otra.peso;
		}

		public override string ToString() => Clave;
	}
}
