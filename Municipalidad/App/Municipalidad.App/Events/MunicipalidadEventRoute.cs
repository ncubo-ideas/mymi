using Puppets.Portador;

namespace Municipalidad.App.Events
{
	/// <summary>
	/// De senal cruda a hecho tipado, o null para DESCARTARLA. La municipalidad publica su narrativa
	/// entera y esta app modela una rebanada, asi que recibir un tipo que no consume es lo normal y no
	/// un defecto: se descarta, y nunca se inventa un comando para tragarselo.
	/// </summary>
	public static class MunicipalidadEventRoute
	{
		/// <summary>
		/// Contesta el hecho que la senal dice ser, o null. Cada interprete compara su propio tag antes
		/// de mirar el payload y los seis tags son distintos, asi que a lo sumo uno puede contestar y el
		/// ORDEN de abajo no decide nada.
		/// </summary>
		public static IMensajeDeEvento Route(SenalDeEvento senal)
		{
			IMensajeDeEvento finca = FincaIncorporada.Interpret(senal);
			if (finca != null) return finca;

			IMensajeDeEvento cobro = CobroPuesto.Interpret(senal);
			if (cobro != null) return cobro;

			IMensajeDeEvento pago = PagoRegistrado.Interpret(senal);
			if (pago != null) return pago;

			IMensajeDeEvento linea = LineaDePagoRegistrada.Interpret(senal);
			if (linea != null) return linea;

			IMensajeDeEvento consulta = ConsultaTomada.Interpret(senal);
			if (consulta != null) return consulta;

			return PendienteRegistrado.Interpret(senal);
		}
	}
}
