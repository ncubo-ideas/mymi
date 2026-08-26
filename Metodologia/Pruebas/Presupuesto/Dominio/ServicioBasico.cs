using System;

namespace Presupuesto.Dominio
{
	// LUZ, AGUA, INTERNET. El gasto que llega todos los meses y nunca es el mismo monto.
	//
	// LO QUE ESTA CLASE SABE Y NINGUNA OTRA: que su variacion NORMAL es alta. La entrevista
	// lo dijo con todas las letras -"normalmente si hay cambios entre verano e invierno"- y
	// eso no lo sabe una cuota de prestamo, que es identica todos los meses.
	//
	// Que exista esta clase es lo que permite que el juicio 2 no mienta. Un 10% de variacion
	// en una cuota de prestamo es imposible y por lo tanto sospechoso; un 10% en el recibo
	// electrico entre julio y diciembre es el clima. Sin la distincion, el mismo umbral
	// aplicado a los dos convierte a uno en falso positivo permanente.
	//
	// HUECO 5 - Preguntas.md fija el 10% sobre el TOTAL mensual y no dice cuanta variacion
	// propia tolera cada gasto antes de contar como "responsable" del movimiento. La
	// entrevista dijo que las diferencias estacionales "no son tan significativas", que no es
	// un numero. Aca queda como una propiedad DECLARADA por la clase, sin valor inventado:
	// quien construya el gasto la pone.
	public sealed class ServicioBasico : GastoRecurrente
	{
		private readonly decimal variacionEstacionalTolerada;

		public ServicioBasico(string nombre, decimal variacionEstacionalTolerada) : base(nombre)
		{
			if (variacionEstacionalTolerada < 0 || variacionEstacionalTolerada > 1)
				throw new ArgumentOutOfRangeException(nameof(variacionEstacionalTolerada),
					"Es una proporcion: 0,15 son quince por ciento.");

			this.variacionEstacionalTolerada = variacionEstacionalTolerada;
		}

		public override Periodicidad Periodicidad => Periodicidad.Mensual;

		// No es critico: que no llegue el recibo de la luz es un aviso, no una mora. Es
		// exactamente el contraste que la entrevista uso para explicar el juicio 5.
		public override Severidad SeveridadSiFalta => Severidad.Seguimiento;

		// No es discrecional -no se puede dejar de pagar la luz- pero tampoco es un
		// compromiso fijo: se puede consumir menos. Eso lo decide la persona, no el dominio.
		public override Reducibilidad Reducibilidad => Reducibilidad.PorDefinir;

		// ------------------------------------------------------------ lo suyo

		// .Este cambio es el clima, o paso algo?
		public bool EsVariacionNormal(decimal proporcionDeCambio) =>
			Math.Abs(proporcionDeCambio) <= variacionEstacionalTolerada;
	}
}
