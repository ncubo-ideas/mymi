using System;

namespace Presupuesto.Dominio
{
	// LA RESPUESTA A LA PREGUNTA 4: "que pago fijo no me cobraron este mes?".
	//
	// Es la unica de las cinco preguntas de Preguntas.md que ninguna pantalla puede mostrar.
	// Y no es una limitacion de la pantalla: es que esto NO ES UN DATO DEL EXPEDIENTE. Una
	// pantalla renderiza los movimientos que existen; una ausencia es una conclusion sobre
	// los movimientos MAS el calendario MAS la periodicidad que cada gasto declara para si.
	//
	// Un cargo ausente NO ES UN HALLAZGO FINANCIERO. No dice que haya algo mal con la plata:
	// dice que hay algo que verificar. Puede ser un debito que fallo -y corre interes-, un
	// seguro que se cayo sin que nadie se enterara, o un servicio que se creia cancelado y
	// sigue vivo al reves. Tres cosas muy distintas, todas caras.
	//
	// La LECTURA la arma este dominio; la ACCION no: sale de Severidad, que es un conjunto
	// cerrado. Capitulo 0 §8 - nombrar no es inventar.
	public sealed class CargoAusente
	{
		private CargoAusente(GastoRecurrente gasto, DateTime esperado, int diasDeAtraso,
			bool hayHistoria, bool vencido)
		{
			Gasto = gasto;
			Esperado = esperado;
			DiasDeAtraso = diasDeAtraso;
			HayHistoria = hayHistoria;
			EstaVencido = vencido;
		}

		public GastoRecurrente Gasto { get; }

		public DateTime Esperado { get; }

		public int DiasDeAtraso { get; }

		// De un gasto que nunca aparecio no hay nada que echar de menos. Es distinto de
		// "esta al dia", y confundirlos haria que la app avise de cosas que nunca existieron.
		public bool HayHistoria { get; }

		public bool EstaVencido { get; }

		// La severidad la decide el COMPROMISO, no el monto ni los dias. Juicio 5.
		public Severidad Severidad =>
			EstaVencido ? Gasto.SeveridadSiFalta : Severidad.Informativo;

		public string QueHacer => Severidad.QueHacer;

		public string Lectura
		{
			get
			{
				if (!HayHistoria)
					return $"{Gasto.Nombre} no tiene cargos todavia, asi que no hay nada que esperar.";

				if (!EstaVencido)
					return $"{Gasto.Nombre} esta al dia. El proximo se espera el {Esperado:dd/MM/yyyy}.";

				return $"{Gasto.Nombre} se esperaba el {Esperado:dd/MM/yyyy} y lleva "
					+ $"{DiasDeAtraso} dias sin aparecer.";
			}
		}

		internal static CargoAusente SinHistoria(GastoRecurrente gasto) =>
			new CargoAusente(gasto, DateTime.MinValue, 0, hayHistoria: false, vencido: false);

		internal static CargoAusente AlDia(GastoRecurrente gasto, DateTime esperado) =>
			new CargoAusente(gasto, esperado, 0, hayHistoria: true, vencido: false);

		internal static CargoAusente Vencido(GastoRecurrente gasto, DateTime esperado, int dias) =>
			new CargoAusente(gasto, esperado, dias, hayHistoria: true, vencido: true);
	}
}
