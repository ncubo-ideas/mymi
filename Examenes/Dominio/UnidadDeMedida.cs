using System;

namespace Examenes.Dominio
{
	// La unidad en la que se midio un analito. Es el "que" de "200 que": sin ella un
	// 200 no significa nada, y dos numeros en unidades distintas no se pueden comparar
	// ni restar.
	//
	// Conjunto CERRADO: las unicas instancias que existen son las declaradas aca, y se
	// comparan por referencia. No hay Equals ni GetHashCode - dos unidades son la misma
	// unidad solo si son el mismo objeto, que es justamente lo que garantiza el conjunto
	// cerrado. Un string "mg/dL" suelto admitiria "mg/dl", "MG/DL" y "miligramos" como
	// tres unidades distintas para lo mismo.
	//
	// PorSimbolo es la frontera: el simbolo llega como texto en el evento del proveedor
	// y se resuelve UNA vez, aca. Un simbolo desconocido es un evento mal formado, no
	// una unidad nueva: revienta, no se inventa una instancia.
	internal sealed class UnidadDeMedida
	{
		internal static readonly UnidadDeMedida MiligramosPorDecilitro = new UnidadDeMedida("mg/dL");
		internal static readonly UnidadDeMedida NanogramosPorMililitro = new UnidadDeMedida("ng/mL");
		internal static readonly UnidadDeMedida GramosPorDecilitro = new UnidadDeMedida("g/dL");

		internal string Simbolo { get; private set; }

		private UnidadDeMedida(string simbolo)
		{
			Simbolo = simbolo;
		}

		// Funcion total con excepcion: el simbolo viene del wire y solo hay tres
		// unidades validas. Que no coincida ninguna es un defecto del emisor.
		internal static UnidadDeMedida PorSimbolo(string simbolo)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(simbolo);

			string normalizado = simbolo.Trim();
			if (normalizado == MiligramosPorDecilitro.Simbolo) return MiligramosPorDecilitro;
			if (normalizado == NanogramosPorMililitro.Simbolo) return NanogramosPorMililitro;
			if (normalizado == GramosPorDecilitro.Simbolo) return GramosPorDecilitro;

			throw new ArgumentException(
				$"'{simbolo}' no es una unidad de medida conocida. Las validas son: "
				+ $"{MiligramosPorDecilitro.Simbolo}, {NanogramosPorMililitro.Simbolo}, {GramosPorDecilitro.Simbolo}.",
				nameof(simbolo));
		}

		// Operacion con nombre en vez de sobrecargar ==. La pregunta del dominio no es
		// "son iguales" sino "puedo comparar estos dos valores".
		internal bool EsLaMisma(UnidadDeMedida otra)
		{
			ArgumentNullException.ThrowIfNull(otra);
			return ReferenceEquals(this, otra);
		}
	}
}
