using System;

namespace Examenes.Dominio
{
	// UN ANALITO QUE SE MIDE CON UN NUMERO Y UNA UNIDAD. Forma 1 de la taxonomia de
	// Magnitud, y la unica que necesita las dos cosas que estaban en Analito.
	//
	// POR QUE BAJARON ACA. La unidad y el rango de lo posible no son de todo analito: son
	// de la forma continua. Mientras vivieron arriba, ser analito EXIGIA tener unidad, y
	// por eso el color de la orina y la sangre oculta no podian serlo aunque fueran, por
	// la propia definicion, cosas que se midieron.
	//
	// Es la misma correccion que ya hicimos en Magnitud: la raiz declara solo Expresado,
	// porque si declarara un decimal Valor, cuatro de las seis formas tendrian que
	// devolver cero y mentir.
	public sealed class AnalitoContinuo : Analito
	{
		public UnidadDeMedida Unidad { get; }

		// EL RANGO DE LO FISIOLOGICAMENTE POSIBLE, que NO es el rango de referencia.
		//
		// El de referencia dice que es NORMAL; este dice que es POSIBLE en una persona
		// viva. Entre los dos hay lugar para valores altos y reales -una hemoglobina de 9
		// es anemia, no un error- y afuera de este ya no hay hallazgo que interpretar: hay
		// un dato malformado.
		//
		// ⚠️ Los limites son deliberadamente ANCHOS y orientativos. La guarda existe para
		// atajar basura, no para opinar de clinica: apretarlos de mas descartaria hallazgos
		// reales, que es peor que dejar pasar uno raro.
		public decimal ValorMinimoPosible { get; }

		public decimal ValorMaximoPosible { get; }

		// El caso normal: un analito que se le mide a cualquier persona. La sobrecarga de
		// abajo es para el unico que no —el antigeno prostatico— y el requisito va ANTES
		// del params porque un params tiene que quedar ultimo.
		internal AnalitoContinuo(string nombre, string paraMostrar, UnidadDeMedida unidad,
			decimal minimoPosible, decimal maximoPosible, params string[] sinonimos)
			: this(nombre, paraMostrar, unidad, minimoPosible, maximoPosible,
				RequisitoAnatomico.Ninguno, sinonimos)
		{
		}

		internal AnalitoContinuo(string nombre, string paraMostrar, UnidadDeMedida unidad,
			decimal minimoPosible, decimal maximoPosible, RequisitoAnatomico requisito,
			params string[] sinonimos)
			: base(nombre, paraMostrar, requisito, sinonimos)
		{
			Unidad = unidad;
			ValorMinimoPosible = minimoPosible;
			ValorMaximoPosible = maximoPosible;
		}

		public bool EsValorPosible(decimal valor) =>
			valor >= ValorMinimoPosible && valor <= ValorMaximoPosible;

		// Se ordena y admite aritmetica: es la forma sobre la que la serie y la velocidad
		// significan algo.
		public override bool FormaSerie => true;
	}
}
