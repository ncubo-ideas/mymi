using System;
using System.Collections.Generic;

namespace Compras.Dominio
{
	/// <summary>
	/// Lo que probablemente le va a faltar en la próxima compra, de lo más atrasado a lo
	/// que todavía no toca.
	/// </summary>
	// LA PIDIO EL EQUIPO ASI: "basado en las ultimas compras hazme una lista de las cosas que
	// necesito para las proximas compras, digamos de mis ultimas 2 compras".
	//
	// SE MIRA EL RITMO Y NO LAS ULTIMAS DOS COMPRAS, y vale explicar por que. Una lista de lo
	// que entro las ultimas dos veces son quince o veinte productos que la persona ACABA DE
	// COMPRAR: es lo contrario de lo que falta. Lo que contesta la pregunta es el ritmo —
	// "la leche vuelve cada 7 dias y hace 9 que no entra"—, y las compras viejas son de donde
	// sale ese ritmo, no la respuesta.
	//
	// ENTRAN LOS ANTOJOS, y esto es deliberado aunque parezca contradecir la regla de que
	// AbastecimientosPendientes NO reporta azucarados ni snacks. No es la misma situacion:
	//
	//   AQUELLA es un AVISO que la app levanta sola. Decirle a alguien "hace un mes que no
	//   comprás helado" sin que lo pida es opinar sobre su cuerpo por la puerta de atras, y
	//   por eso esas categorias tienen reabastecimiento cero.
	//
	//   ESTA es una lista que la persona ABRE. Si compra helado todas las semanas y hace
	//   nueve dias que no entra, sacarlo de SU lista seria la app editando su canasta por
	//   ella. Omitir es tambien una conclusion sobre el cuerpo, y del lado malo de la
	//   frontera: "de que a que" es nuestro, "que deberia comer" no.
	internal abstract class ListaParaLaProximaCompra
	{
		/// <summary>
		/// Cuántos ciclos puede pasar sin entrar antes de que deje de ser "te falta" y pase
		/// a ser "lo dejaste".
		/// </summary>
		// ESTA LISTA NO ES EL HUECO, y sin este corte se convertia en el hueco mal contado.
		// Medido contra los datos reales: la tilapia y la espinaca aparecian PRIMERAS, con
		// 266 dias sobre un ritmo mensual — nueve ciclos. Nadie "necesita" para su proxima
		// compra algo que dejo de llevar hace nueve meses; eso es un cambio de habito, y
		// tiene su propia lectura en AbastecimientosPendientes.
		//
		// TRES CICLOS: saltarse uno o dos es plausible -un viaje, quedo stock, no habia en
		// gondola-; saltarse cinco no es un olvido, es que cambio lo que compra. En semanal
		// son 21 dias y en mensual 90.
		//
		// El corte es POR CICLOS Y NO POR DIAS a proposito: 60 dias sin comprar leche es
		// abandono y 60 sin comprar comida de perro es normal. Un numero de dias fijo
		// trataria igual a los dos.
		internal const int CiclosDespuesDeLosCualesYaNoEsFalta = 3;

		/// <summary>Cuántas compras del producto hacen falta para saber su ritmo.</summary>
		// Dos compras dan UN intervalo, y un intervalo no es un habito: puede ser la semana
		// que se acabo antes. Con tres hay dos intervalos y ya se puede tomar una mediana.
		// Es el mismo numero que usa CambioDeRitmo, y a proposito: dos criterios distintos
		// de "ya se le puede medir el ritmo" es como se llega a que una pantalla muestre un
		// producto que la otra dice que no tiene ritmo.
		internal const int ComprasQueHacenFaltaParaSaberElRitmo = CambioDeRitmo.ComprasQueHacenFaltaDeCadaLado;

		/// <summary>Ausencia con nombre: sin historial suficiente porta esto, nunca null.</summary>
		internal static readonly ListaParaLaProximaCompra Ninguna = new SinLista();

		internal static ListaParaLaProximaCompra De(IReadOnlyList<ProductoQueVaAFaltar> productos) =>
			new Armada(productos);

		/// <summary>Consulta de presencia, total.</summary>
		internal abstract bool Existe();

		/// <summary>Todos los que tienen ritmo medido, del más atrasado al que menos falta.</summary>
		internal abstract IReadOnlyList<ProductoQueVaAFaltar> Productos { get; }

		/// <summary>Solo los que ya pasaron su tiempo. Es la lista corta, la de la pantalla.</summary>
		internal abstract IReadOnlyList<ProductoQueVaAFaltar> LosQueTocan { get; }

		/// <summary>Cuántos ya pasaron su tiempo. Cero cuando ninguno, y es verdad.</summary>
		internal abstract int CuantosTocan { get; }

		/// <summary>
		/// QUE FRASE dice esta lista, por su nombre: `sin-historial`, `al-dia`, `tocan.uno` o
		/// `tocan.varios`. Ninguno atrasado es una respuesta y tiene su propia clave, no una lista
		/// vacia; y el plural lo decide la clave, porque el espanol y el ingles lo concuerdan distinto.
		/// </summary>
		internal abstract string Clave { get; }

		private sealed class SinLista : ListaParaLaProximaCompra
		{
			internal override bool Existe() => false;

			internal override IReadOnlyList<ProductoQueVaAFaltar> Productos =>
				Array.Empty<ProductoQueVaAFaltar>();

			internal override IReadOnlyList<ProductoQueVaAFaltar> LosQueTocan =>
				Array.Empty<ProductoQueVaAFaltar>();

			internal override int CuantosTocan => 0;

			internal override string Clave => "sin-historial";
		}

		private sealed class Armada : ListaParaLaProximaCompra
		{
			private readonly IReadOnlyList<ProductoQueVaAFaltar> productos;
			private readonly List<ProductoQueVaAFaltar> tocan = new List<ProductoQueVaAFaltar>();

			internal Armada(IReadOnlyList<ProductoQueVaAFaltar> productos)
			{
				ArgumentNullException.ThrowIfNull(productos);
				if (productos.Count == 0)
					throw new ArgumentException(
						"Una lista sin productos no es una lista; eso es ListaParaLaProximaCompra.Ninguna.",
						nameof(productos));

				this.productos = productos;
				foreach (ProductoQueVaAFaltar producto in productos)
					if (producto.Toca) tocan.Add(producto);
			}

			internal override bool Existe() => true;

			internal override IReadOnlyList<ProductoQueVaAFaltar> Productos => productos;
			internal override IReadOnlyList<ProductoQueVaAFaltar> LosQueTocan => tocan;
			internal override int CuantosTocan => tocan.Count;

			internal override string Clave =>
				tocan.Count == 0 ? "al-dia" : tocan.Count == 1 ? "tocan.uno" : "tocan.varios";
		}
	}
}
