namespace Cargas.Dominio
{
	/// <summary>
	/// Los numeros con los que se juzga una serie de tramos. Son el analogo del
	/// CambioMinimoSignificativo de examenes, y como alla, la pregunta de si viven en
	/// codigo o en un repertorio sigue abierta (preguntas 4 y 5 del brief): por ahora
	/// viven aca, juntos, para que moverlos sea un solo cambio.
	/// </summary>
	// ⚠️ NINGUNO DE ESTOS NUMEROS ESTA REVISADO por alguien con criterio del negocio.
	// Sirven para modelar la estructura y contar la historia del demo; el manifiesto
	// los declara como advertencia, igual que los 81 umbrales clinicos de Delfos.
	public static class UmbralesDeRendimiento
	{
		/// <summary>El cambio proporcional minimo que se considera un cambio DE VERDAD.
		/// Por debajo, la serie esta Estable: un surtidor tiene tolerancia de calibracion
		/// y un tanque no se llena dos veces igual. ⚠️ 5% es criterio propio.</summary>
		public const decimal CambioMinimoSignificativo = 0.05m;

		/// <summary>El multiplo sobre la linea base que ningun carro alcanza de verdad.
		/// Un tramo que rinde esto por encima de lo habitual es casi seguro una carga sin
		/// registrar: quedaron los kilometros de dos tanques con los litros de uno. Es la
		/// regla del colesterol 200 a 280 en un dia: un cambio implausible es un problema
		/// de datos, no de la realidad. ⚠️ 1.5 es criterio propio.</summary>
		public const decimal MultiploImplausible = 1.5m;

		/// <summary>Cuanto puede desviarse el total de precio por volumen antes de
		/// reportarse: cubre el redondeo del surtidor y los precios a tres decimales.
		/// Proporcional al total, para que sirva igual en colones que en dolares.
		/// ⚠️ 1% es criterio propio.</summary>
		public const decimal ToleranciaProporcionalDelTotal = 0.01m;
	}
}
