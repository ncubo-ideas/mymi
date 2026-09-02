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
	internal sealed class CargoAusente
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

		internal GastoRecurrente Gasto { get; }

		internal DateTime Esperado { get; }

		internal int DiasDeAtraso { get; }

		// De un gasto que nunca aparecio no hay nada que echar de menos. Es distinto de
		// "esta al dia", y confundirlos haria que la app avise de cosas que nunca existieron.
		internal bool HayHistoria { get; }

		internal bool EstaVencido { get; }

		// La severidad la decide el COMPROMISO, no el monto ni los dias. Juicio 5.
		internal Severidad Severidad =>
			EstaVencido ? Gasto.SeveridadSiFalta : Severidad.Informativo;

		internal string QueHacer => Severidad.QueHacer;

		// ------------------------------------------------------ lo que cruza el perform
		//
		// El print PROYECTA UN SOLO NIVEL: `control.Gasto.Nombre` no cruza. Estos tres
		// aplanan lo que arriba son objetos, para que el script pueda imprimirlos sin
		// que la caratula reciba un tipo del dominio. Es la misma solucion que Examenes
		// usa en CeldaDelCuadro con Fila y Columna.
		internal string NombreDelGasto => Gasto.Nombre;

		internal string NombreDeLaSeveridad => Severidad.Nombre;

		internal int OrdenDeLaSeveridad => Severidad.Orden;

		// EL DOMINIO NO FORMATEA FECHAS. Dice DURACIONES.
		//
		// No es una precaución de locale: es que una duración YA ES la lectura y una fecha
		// no. "Se esperaba el 05/07/2026" obliga a quien lo lea a hacer la resta; "lleva 15
		// días sin aparecer" es la conclusión, que es lo único que este objeto sabe decir
		// mejor que nadie.
		//
		// Y de paso evita un daño concreto que se midió: la fecha cruzaba el perform como
		// `07/05/2026` —el print serializa en formato US— mientras esta línea decía
		// `05/07/2026`. Mismo instante, dos lecturas, en el mismo payload. En LA pregunta
		// que existe para avisar de una mora, quien leyera mal el campo iba a reportar dos
		// meses de atraso donde hay quince días.
		//
		// La fecha sigue viajando en `Esperado`, como DateTime. Formatearla es derecho de
		// la superficie, que es la que sabe para quién escribe.
		internal string Lectura
		{
			get
			{
				if (!HayHistoria)
					return $"{Gasto.Nombre} no tiene cargos todavia, asi que no hay nada que esperar.";

				if (EstaVencido)
					return $"{Gasto.Nombre} lleva {EnPalabras(DiasDeAtraso)} sin aparecer.";

				return DiasDeAtraso >= 0
					? $"{Gasto.Nombre} esta al dia: se esperaba hace {EnPalabras(DiasDeAtraso)}, "
						+ "dentro de su margen."
					: $"{Gasto.Nombre} esta al dia. El proximo se espera en {EnPalabras(-DiasDeAtraso)}.";
			}
		}

		// Un día, quince días, dos meses. La persona no cuenta en días cuando pasan de un
		// par de meses, y una app que le diga "lleva 437 días" la obliga a dividir.
		private static string EnPalabras(int dias)
		{
			if (dias == 0) return "menos de un dia";
			if (dias == 1) return "un dia";
			if (dias < 60) return $"{dias} dias";

			int meses = dias / 30;
			return meses == 1 ? "un mes" : $"{meses} meses";
		}

		internal static CargoAusente SinHistoria(GastoRecurrente gasto) =>
			new CargoAusente(gasto, DateTime.MinValue, 0, hayHistoria: false, vencido: false);

		internal static CargoAusente AlDia(GastoRecurrente gasto, DateTime esperado, int dias) =>
			new CargoAusente(gasto, esperado, dias, hayHistoria: true, vencido: false);

		internal static CargoAusente Vencido(GastoRecurrente gasto, DateTime esperado, int dias) =>
			new CargoAusente(gasto, esperado, dias, hayHistoria: true, vencido: true);
	}
}
