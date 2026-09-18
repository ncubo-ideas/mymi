using System;

namespace Cargas.Dominio
{
	/// <summary>
	/// LA EXPLICACION DE UNA CARGA: cuantos boletos dio y cuanto sobro.
	///
	/// Los boletos NO se derivan -el sorteo se rifa sobre numeros, y un boleto fabricado
	/// aca no sirve para reclamar nada-, pero la EXPLICACION si: "esta carga dio 2 boletos,
	/// sobraron 9 392 que no se acumulan". Eso es lo que una factura no dice.
	///
	/// Cuando la cuenta derivada no cuadra con lo emitido, el trato es el de EsSospechoso
	/// en un tramo: probablemente falta un dato por llegar, NUNCA que la estacion le debe
	/// algo. Un bug de nuestra aritmetica no puede convertirse en un reclamo.
	/// </summary>
	internal sealed class BoletosDeLaCarga
	{
		/// <summary>No hay ningun sorteo cuya regla alcance a esta carga.</summary>
		internal const string NoEntraEnNingunSorteo = "sin-sorteo";

		/// <summary>Hay sorteos, pero esta carga cae fuera de las fechas de todos.</summary>
		internal const string FueraDeLasFechas = "fuera-de-las-fechas";

		/// <summary>El sorteo da boletos en una moneda y esta carga se pago en otra.</summary>
		internal const string LaReglaEsEnOtraMoneda = "otra-moneda";

		/// <summary>La regla y lo emitido no coinciden: probablemente falta un dato por llegar.</summary>
		internal const string ProbablementeFaltaUnDatoPorLlegar = "falta-un-dato";

		/// <summary>La carga no llego al monto del primer boleto.</summary>
		internal const string NoAlcanzoParaUnBoleto = "no-alcanzo";

		/// <summary>Dio boletos y no sobro nada.</summary>
		internal const string DioBoletos = "dio-boletos";

		/// <summary>Dio boletos y sobro un resto, que no se acumula.</summary>
		internal const string DioBoletosYSobro = "dio-boletos-y-sobro";

		internal DateTime FechaDeLaCarga { get; private set; }
		internal string NumeroDeLaTransaccion { get; private set; }
		internal string CodigoDelSorteo { get; private set; }
		internal string NombreDelSorteo { get; private set; }

		/// <summary>Si hay un sorteo con regla aplicable a esta carga.</summary>
		internal bool HayRegla { get; private set; }

		/// <summary>EL CODIGO de por que no hay regla, o vacio cuando si la hay.</summary>
		internal string ClaveSinRegla { get; private set; }

		internal int BoletosQueDaLaRegla { get; private set; }
		internal int BoletosEmitidos { get; private set; }
		internal decimal Sobrante { get; private set; }

		/// <summary>La moneda en que se pago la carga, y la de la regla del sorteo. El dominio
		/// no convierte dinero: cuando no son la misma, la regla no aplica y se dice.</summary>
		internal string MonedaDeLaCarga { get; private set; }
		internal string MonedaDeLaRegla { get; private set; }

		internal bool LaCuentaCuadra => !HayRegla || BoletosQueDaLaRegla == BoletosEmitidos;
		internal bool ProbablementeFaltaUnDato => HayRegla && !LaCuentaCuadra;

		internal BoletosDeLaCarga(DateTime fecha, string transaccion, string codigo,
			string nombre, bool hayRegla, string claveSinRegla, int queDaLaRegla,
			int emitidos, decimal sobrante, string monedaDeLaCarga, string monedaDeLaRegla)
		{
			FechaDeLaCarga = fecha;
			NumeroDeLaTransaccion = transaccion ?? "";
			CodigoDelSorteo = codigo ?? "";
			NombreDelSorteo = nombre ?? "";
			HayRegla = hayRegla;
			ClaveSinRegla = claveSinRegla ?? "";
			BoletosQueDaLaRegla = queDaLaRegla;
			BoletosEmitidos = emitidos;
			Sobrante = sobrante;
			MonedaDeLaCarga = monedaDeLaCarga ?? "";
			MonedaDeLaRegla = monedaDeLaRegla ?? "";
		}

		/// <summary>
		/// LA CLAVE DE LA EXPLICACION, en pasado. RETROSPECTIVA A PROPOSITO: "sobraron X que no
		/// se acumulan" es un hecho de su registro; "carga X mas y te llevas otro boleto" seria
		/// la estacion vendiendo por la boca de su propia app.
		/// </summary>
		internal string ClaveDeLaExplicacion
		{
			get
			{
				if (!HayRegla)
					return ClaveSinRegla.Length > 0 ? ClaveSinRegla : NoEntraEnNingunSorteo;

				if (ProbablementeFaltaUnDato) return ProbablementeFaltaUnDatoPorLlegar;

				if (BoletosEmitidos == 0) return NoAlcanzoParaUnBoleto;

				return Sobrante > 0m ? DioBoletosYSobro : DioBoletos;
			}
		}
	}
}
