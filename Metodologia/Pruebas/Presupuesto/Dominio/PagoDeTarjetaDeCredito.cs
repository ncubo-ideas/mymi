using System;

namespace Presupuesto.Dominio
{
	// EL PAGO DE LA TARJETA. El otro caso critico del juicio 5.
	//
	// LO QUE ESTA CLASE SABE Y NINGUNA OTRA, y es lo mas importante de este archivo: NO ES
	// UN GASTO. Es una LIQUIDACION.
	//
	// Cuando la persona paga la tarjeta no esta consumiendo nada: esta saldando compras que
	// ya ocurrieron y que YA ESTAN en el estado de cuenta de la tarjeta como cargos propios.
	// Si el total del mes sumara las dos cosas, cada compra con tarjeta se contaria DOS
	// VECES, y el juicio 2 -"el presupuesto mensual vario mas de un 10%"- daria alarmas
	// falsas todos los meses sin que nadie entienda por que.
	//
	// ESTO ES UNA INFERENCIA, NO ALGO QUE Preguntas.md DIGA, y por eso queda marcada. Sale
	// de aplicar el mismo razonamiento que la persona ya uso al dejar fuera los estados de
	// tarjeta de DEBITO -"se duplicarian con la informacion que viene en los estados de
	// cuenta de las cuentas bancarias"-. La regla es la misma, un piso mas abajo. Pero es
	// una extension mia de su criterio, y conviene confirmarla antes de que alguien construya
	// un total encima.
	public sealed class PagoDeTarjetaDeCredito : GastoRecurrente
	{
		public PagoDeTarjetaDeCredito(string nombre) : base(nombre)
		{
		}

		public override Periodicidad Periodicidad => Periodicidad.Mensual;

		public override Severidad SeveridadSiFalta => Severidad.Critico;

		public override Reducibilidad Reducibilidad => Reducibilidad.Comprometido;

		public override int DiasDeGracia => 1;

		// ------------------------------------------------------------ lo suyo

		// La propiedad que lo distingue de todo lo demas del dominio. Quien sume gastos
		// TIENE que preguntar esto antes de sumar.
		public override bool EsConsumo => false;

		// Que no cuente como consumo NO lo vuelve invisible: si no aparece, es critico -es
		// mora- y por eso sigue siendo un gasto recurrente con todo lo demas intacto.
		public string PorQueNoSuma =>
			"Pagar la tarjeta no es consumir: salda compras que ya estan contadas una por una.";
	}
}
