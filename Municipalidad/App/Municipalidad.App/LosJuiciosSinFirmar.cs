namespace Municipalidad.App
{
	/// <summary>
	/// Los umbrales y las tasas con que se le pregunta al dominio. El dominio los toma por parametro y
	/// no guarda ninguno, asi que alguien de afuera tiene que ponerlos: aca hay UN sitio para las dos
	/// caratulas que preguntan, y cada respuesta dice el numero con que se midio.
	/// <para>
	/// CINCO DE LOS SEIS NO ESTAN FIRMADOS. Los propuso quien modelo, leyendo seis recibos, y esperan a
	/// la persona que conoce el tramite. Ni el asistente ni la persona los eligen.
	/// </para>
	/// </summary>
	public static class LosJuiciosSinFirmar
	{
		/// <summary>Cuanto tiene que subir un cobro para que se llame salto. SIN FIRMAR.</summary>
		public const decimal UmbralDeSalto = 0.25m;

		/// <summary>Por debajo de esto un cambio es ruido y no se avisa. SIN FIRMAR.</summary>
		public const decimal Ruido = 0.02m;

		/// <summary>Cuantos dias antes del vencimiento se avisa. SIN FIRMAR.</summary>
		public const int DiasDeAviso = 7;

		/// <summary>
		/// La tasa anual con que se PROYECTA lo que costaria pagar tarde. SIN FIRMAR, reconstruida de los
		/// recibos, y es del anio en curso: no es la mora que la municipalidad cobra.
		/// </summary>
		public const decimal TasaDeMora = 0.062m;

		/// <summary>
		/// La tasa anual del acuerdo municipal con que se calcula el ahorro por pronto pago. SIN FIRMAR,
		/// y es la del anio en curso.
		/// </summary>
		public const decimal TasaDeProntoPago = 0.0378m;

		/// <summary>
		/// Cuanto puede quedar lo adoptado por debajo de lo propuesto antes de avisar. FIRMADO por la
		/// persona el 2026-09-03, y es el unico de los seis que lo esta.
		/// </summary>
		public const decimal UmbralDeReserva = 0.10m;

		/// <summary>El dia en que la persona firmo el unico de los seis que esta firmado.</summary>
		public const string DiaEnQueSeFirmoElUmbralDeReserva = "2026-09-03";
	}
}
