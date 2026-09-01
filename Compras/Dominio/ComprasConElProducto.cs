using System;
using System.Collections.Generic;

namespace Compras.Dominio
{
	/// <summary>
	/// En cuáles compras apareció algo. Buscar algo que nunca se llevó porta Ninguna.
	/// </summary>
	// EXISTE PORQUE LA PANTALLA DE RECIBOS PROMETIA ALGO QUE NO HACIA. Su buscador dice
	// "leche, arroz, galletas" y solo miraba el NUMERO DEL TICKET, que es justo lo que nadie
	// recuerda. Escribir "leche" no devolvia nada y escribir "T-" devolvia las 87, porque
	// todos los numeros empiezan igual.
	//
	// Y NO ACOTA LA LISTA, que es lo que yo esperaba y resulto falso. Medido contra el
	// guion de 87 compras: "leche" devuelve LAS 87 -es un habitual semanal, esta en todas-,
	// "Azucarados" 64, "helado" 46, "Limpieza" 43. Una pregunta especifica no da una
	// respuesta corta cuando lo que se pregunta es justamente lo que nunca falta.
	//
	// O sea que la pantalla sigue necesitando un tope. Lo que cambia es DONDE se corta: 25
	// compras con leche es una respuesta a lo que se pregunto, y 25 tickets cualesquiera
	// no era ninguna.
	//
	// "PRODUCTO" INCLUYE UN DEPARTAMENTO, y no es un accidente: el mismo criterio que usa
	// el consumo -Alcanza- reconoce "Limpieza" como categoria y "leche" como texto. Buscar
	// un pasillo entero es una pregunta legitima, y salia gratis.
	//
	// ES UN PAR PRESENTE/AUSENTE QUE NO GUARDA NADA, y vale explicar por que: en los otros
	// pares hay lecturas que MIENTEN si se contestan vacias -el PrimerAnio de un historial
	// sin compras seria el año cero-, y por eso revientan. Aca no hay ninguna asi. De algo
	// que nunca se compro se llevaron CERO unidades y se gastaron CERO colones, y las dos
	// cosas son verdad, no rellenos. La primera version de esta clase hacia reventar los
	// totales por simetria con las otras, y era guardar de mas.
	//
	// El par se queda igual por las dos cosas que si separa: la frase -"no hay ninguna
	// compra con «X»" no se puede componer desde una lista vacia- y el invariante de que
	// un hallazgo con cero compras no existe.
	internal abstract class ComprasConElProducto
	{
		/// <summary>Ausencia con nombre: buscar algo que nunca se llevó porta esto, nunca null.</summary>
		internal static ComprasConElProducto Ninguna(string buscado) => new SinCoincidencias(buscado);

		internal static ComprasConElProducto De(
			string buscado,
			IReadOnlyList<CompraConElProducto> compras,
			IReadOnlyList<string> productos) => new Hallazgos(buscado, compras, productos);

		/// <summary>Consulta de presencia, total.</summary>
		internal abstract bool Existe();

		/// <summary>Lo que se escribió, tal cual. Siempre disponible: es lo que se preguntó.</summary>
		internal abstract string Buscado { get; }

		/// <summary>Las compras, de la más reciente a la más vieja. Vacía cuando no hay ninguna.</summary>
		internal abstract IReadOnlyList<CompraConElProducto> Compras { get; }

		/// <summary>
		/// Las descripciones distintas que coincidieron. Buscar "queso" puede alcanzar a dos
		/// productos, y quien pregunta tiene derecho a saber cuáles antes de leer los totales.
		/// </summary>
		internal abstract IReadOnlyList<string> Productos { get; }

		/// <summary>Cero cuando no apareció, y es verdad: no se llevó ninguna.</summary>
		internal abstract int TotalUnidades { get; }

		/// <summary>Cero cuando no apareció, y es verdad: no se gastó nada en eso.</summary>
		internal abstract decimal TotalGastado { get; }

		/// <summary>En cuántas compras apareció. Cero cuando no apareció en ninguna.</summary>
		// Existe aparte de Compras.Count porque una consulta del DSL imprime campos planos,
		// y un campo es mas barato de proyectar que contar una lista.
		internal abstract int CuantasCompras { get; }

		internal abstract string ParaElAsistente { get; }

		private sealed class SinCoincidencias : ComprasConElProducto
		{
			private readonly string buscado;

			internal SinCoincidencias(string buscado)
			{
				ArgumentNullException.ThrowIfNullOrWhiteSpace(buscado);
				this.buscado = buscado;
			}

			internal override bool Existe() => false;

			internal override string Buscado => buscado;

			internal override IReadOnlyList<CompraConElProducto> Compras => Array.Empty<CompraConElProducto>();
			internal override IReadOnlyList<string> Productos => Array.Empty<string>();

			internal override int TotalUnidades => 0;
			internal override decimal TotalGastado => 0m;

			internal override int CuantasCompras => 0;

			internal override string ParaElAsistente => $"No hay ninguna compra con «{buscado}».";
		}

		private sealed class Hallazgos : ComprasConElProducto
		{
			private readonly string buscado;
			private readonly IReadOnlyList<CompraConElProducto> compras;
			private readonly IReadOnlyList<string> productos;

			internal Hallazgos(
				string buscado,
				IReadOnlyList<CompraConElProducto> compras,
				IReadOnlyList<string> productos)
			{
				ArgumentNullException.ThrowIfNullOrWhiteSpace(buscado);
				ArgumentNullException.ThrowIfNull(compras);
				ArgumentNullException.ThrowIfNull(productos);
				if (compras.Count == 0)
					throw new ArgumentException(
						"Un hallazgo sin compras no es un hallazgo; eso es ComprasConElProducto.Ninguna.",
						nameof(compras));

				this.buscado = buscado;
				this.compras = compras;
				this.productos = productos;
			}

			internal override bool Existe() => true;

			internal override string Buscado => buscado;
			internal override IReadOnlyList<CompraConElProducto> Compras => compras;
			internal override IReadOnlyList<string> Productos => productos;

			internal override int TotalUnidades
			{
				get
				{
					int total = 0;
					foreach (CompraConElProducto compra in compras) total = total + compra.Unidades;
					return total;
				}
			}

			internal override decimal TotalGastado
			{
				get
				{
					decimal total = 0m;
					foreach (CompraConElProducto compra in compras) total = total + compra.Gasto;
					return total;
				}
			}

			internal override int CuantasCompras => compras.Count;

			// SE NOMBRA EL PRODUCTO ENCONTRADO Y NO LO ESCRITO cuando son distintos: quien
			// busco "leche" tiene que poder confirmar que la app entendio "Leche
			// semidescremada 1L" y no otra cosa. Con varios, se dice cuantos.
			internal override string ParaElAsistente
			{
				get
				{
					string que = productos.Count == 1
						? $"«{productos[0]}»"
						: $"{productos.Count} productos que coinciden con «{buscado}»";

					string veces = compras.Count == 1 ? "1 compra" : $"{compras.Count} compras";
					return $"{que}: {veces}, {TotalUnidades} en total, "
						+ $"{Plata.Colones(TotalGastado)}. La ultima, el {compras[0].FechaExpresada}.";
				}
			}
		}
	}
}
