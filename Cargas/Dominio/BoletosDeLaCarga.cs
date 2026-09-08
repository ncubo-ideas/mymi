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
		internal DateTime FechaDeLaCarga { get; private set; }
		internal string NumeroDeLaTransaccion { get; private set; }
		internal string CodigoDelSorteo { get; private set; }
		internal string NombreDelSorteo { get; private set; }

		/// <summary>Si hay un sorteo con regla aplicable a esta carga.</summary>
		internal bool HayRegla { get; private set; }
		internal string MotivoSinRegla { get; private set; }

		internal int BoletosQueDaLaRegla { get; private set; }
		internal int BoletosEmitidos { get; private set; }
		internal decimal Sobrante { get; private set; }
		internal string SobranteExpresado { get; private set; }

		internal bool LaCuentaCuadra => !HayRegla || BoletosQueDaLaRegla == BoletosEmitidos;
		internal bool ProbablementeFaltaUnDato => HayRegla && !LaCuentaCuadra;

		internal BoletosDeLaCarga(DateTime fecha, string transaccion, string codigo,
			string nombre, bool hayRegla, string motivoSinRegla, int queDaLaRegla,
			int emitidos, decimal sobrante, string sobranteExpresado)
		{
			FechaDeLaCarga = fecha;
			NumeroDeLaTransaccion = transaccion ?? "";
			CodigoDelSorteo = codigo ?? "";
			NombreDelSorteo = nombre ?? "";
			HayRegla = hayRegla;
			MotivoSinRegla = motivoSinRegla ?? "";
			BoletosQueDaLaRegla = queDaLaRegla;
			BoletosEmitidos = emitidos;
			Sobrante = sobrante;
			SobranteExpresado = sobranteExpresado ?? "";
		}

		/// <summary>La frase, en pasado. RETROSPECTIVA A PROPOSITO: "sobraron X que no se
		/// acumulan" es un hecho de su registro; "carga X mas y te llevas otro boleto"
		/// seria la estacion vendiendo por la boca de su propia app.</summary>
		internal string ParaSonia
		{
			get
			{
				if (!HayRegla)
					return MotivoSinRegla.Length > 0
						? MotivoSinRegla
						: "esta carga no entra en ningún sorteo";

				if (ProbablementeFaltaUnDato)
					return $"por esta carga veo {BoletosEmitidos} "
						+ (BoletosEmitidos == 1 ? "boleto" : "boletos")
						+ $" y la regla del sorteo da {BoletosQueDaLaRegla}: probablemente "
						+ "falta un dato por llegar";

				if (BoletosEmitidos == 0)
					return $"esta carga no alcanzó para un boleto: faltaron "
						+ $"{SobranteExpresado} para el primero";

				return $"esta carga dio {BoletosEmitidos} "
					+ (BoletosEmitidos == 1 ? "boleto" : "boletos")
					+ (Sobrante > 0m ? $"; sobraron {SobranteExpresado}, que no se acumulan" : "");
			}
		}
	}
}
