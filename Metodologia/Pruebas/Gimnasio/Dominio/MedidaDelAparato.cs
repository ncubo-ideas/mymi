using System;

namespace Gimnasio.Dominio
{
	// LAS CINCO MEDIDAS QUE EL PAPEL TRAJO Y LA ENTREVISTA NO VIO: el IMC -que el v2
	// calculaba a partir del peso-, el agua, el metabolismo basal, la grasa visceral y la
	// masa osea. Cada una es su propia clase porque vive en su propia escala -indice con
	// un decimal, porcentaje, kilocalorias, indice entero, kilos- y porque el dominio es
	// un activo: una clase con nombre es donde el comportamiento futuro va a tener casa
	// (pasada 4, la barra de activo).
	//
	// LO QUE NINGUNA DE LAS CINCO HACE, A PROPOSITO: leerse contra un rango propio. El
	// aparato ya las evaluo y manda la palabra (N7); la app la registra y no la
	// recalcula. Por eso ElAparatoLaEvalua es true, Leer revienta, y ClaveDeReferencia
	// tambien: NADIE DIJO de que parte del perfil dependeria un rango propio -juicio 7,
	// sin firmar para estas cinco-, y un rango inventado es un juicio con mejores
	// modales.
	//
	// Y LA DIRECCION DESEABLE NO SE FIJA. "La masa osea sube y punto" y "la grasa
	// visceral baja y punto" son plausibles y estan sin firmar en la hoja del v3. Hasta
	// que alguien firme, siguen el juicio 1: la da el objetivo, y sin meta declarada la
	// app revienta en vez de opinar.
	internal abstract class MedidaDelAparato : Medida
	{
		protected MedidaDelAparato(decimal valor) : base(valor) { }

		internal override bool ElAparatoLaEvalua { get { return true; } }

		internal override string ClaveDeReferencia(Perfil perfil)
		{
			throw new InvalidOperationException(
				$"'{Nombre}' no tiene rango propio en la app: la evaluo el aparato (N7), y nadie dijo "
				+ "de que dependeria un rango nuestro. Preguntar ElAparatoLaEvalua antes.");
		}
	}

	// EL INDICE DE MASA CORPORAL, tal como el aparato lo manda. El v2 lo CALCULABA en
	// Peso a partir del peso y la altura del perfil; Jorge decidio que manda el del
	// gimnasio, "que tiene maquinas mas especializadas" (hueco 19, cerrado). Peso ya no
	// lo calcula.
	internal sealed class IndiceDeMasaCorporal : MedidaDelAparato
	{
		internal IndiceDeMasaCorporal(decimal indice) : base(indice) { }
		internal override string Nombre { get { return "imc"; } }
	}

	// EL AGUA CORPORAL, en porcentaje.
	internal sealed class Agua : MedidaDelAparato
	{
		internal Agua(decimal porcentaje) : base(porcentaje)
		{
			if (porcentaje >= 100m)
				throw new ArgumentOutOfRangeException(nameof(porcentaje), porcentaje,
					"Un porcentaje de agua corporal de 100 o mas no existe.");
		}
		internal override string Nombre { get { return "agua"; } }
	}

	// EL METABOLISMO BASAL, en kilocalorias. El aparato lo evaluaba contra una META
	// -"Didn't reach goals"-; Jorge saco esa evaluacion del modelo: "solo vamos a tener
	// la medida" (hueco 20). Entra como valor, sin palabra al lado.
	internal sealed class MetabolismoBasal : MedidaDelAparato
	{
		internal MetabolismoBasal(decimal kilocalorias) : base(kilocalorias) { }
		internal override string Nombre { get { return "metabolismo-basal"; } }
	}

	// LA GRASA VISCERAL, un indice entero sin unidad. Que sea un indice y no un
	// porcentaje es lo que la separa del porcentaje de grasa: otra escala, otra cosa.
	internal sealed class GrasaVisceral : MedidaDelAparato
	{
		internal GrasaVisceral(decimal indice) : base(indice) { }
		internal override string Nombre { get { return "grasa-visceral"; } }
	}

	// LA MASA OSEA, en kilos.
	internal sealed class MasaOsea : MedidaDelAparato
	{
		internal MasaOsea(decimal kilos) : base(kilos) { }
		internal override string Nombre { get { return "masa-osea"; } }
	}
}
