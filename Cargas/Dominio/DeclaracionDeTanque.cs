using System;

namespace Cargas.Dominio
{
	/// <summary>
	/// Si el tanque quedo lleno, con su PROCEDENCIA. La ausencia es un tipo, no un null
	/// ni un bool?: Ninguna significa "nadie dijo nada", que es distinto de "no estaba
	/// lleno". Leer EsLleno sin declaracion es un bug del llamador y revienta; preguntar
	/// FueDeclarada decide sin reventar.
	/// </summary>
	// El flujo normal es: la carga nace con la inferencia de su forma de corte (origen
	// Estimado), Sonia la confirma o la corrige, y recien un lleno con procedencia que
	// cuenta puede abrir o cerrar un tramo. Un lleno Estimado prellena el switch y nada
	// mas: si cerrara tramos, la institucion estaria calculando el rendimiento sola, y
	// la tesis del dominio es que no puede.
	public abstract class DeclaracionDeTanque
	{
		/// <summary>La ausencia, como instancia unica.</summary>
		public static readonly DeclaracionDeTanque Ninguna = new SinDeclaracion();

		public static DeclaracionDeTanque De(EstadoDelTanque estado, OrigenDeLectura origen) =>
			new TanqueDeclarado(estado, origen);

		public abstract bool FueDeclarada { get; }

		/// <summary>Revienta si no hay declaracion: consultar antes FueDeclarada.</summary>
		public abstract bool EsLleno { get; }

		/// <summary>Revienta si no hay declaracion.</summary>
		public abstract OrigenDeLectura Origen { get; }

		/// <summary>Si esta declaracion habilita el calculo: un lleno con procedencia que
		/// cuenta. Total: sin declaracion, parcial o estimado dan false sin reventar.</summary>
		public abstract bool EsLlenoQueCuenta { get; }

		/// <summary>Para la lista y el switch prellenado: "lleno (confirmado)",
		/// "parcial (estimado)", o vacio si nadie dijo nada.</summary>
		public abstract string Expresada { get; }

		private sealed class SinDeclaracion : DeclaracionDeTanque
		{
			public override bool FueDeclarada => false;

			public override bool EsLleno =>
				throw new ErrorDeDominio(
					"Nadie declaro el tanque de esta carga: preguntar por EsLleno exige "
					+ "consultar antes FueDeclarada.");

			public override OrigenDeLectura Origen =>
				throw new ErrorDeDominio(
					"Una declaracion que no existe no tiene origen: consultar antes FueDeclarada.");

			public override bool EsLlenoQueCuenta => false;

			public override string Expresada => "";
		}

		private sealed class TanqueDeclarado : DeclaracionDeTanque
		{
			private readonly EstadoDelTanque estado;
			private readonly OrigenDeLectura origen;

			internal TanqueDeclarado(EstadoDelTanque estado, OrigenDeLectura origen)
			{
				ArgumentNullException.ThrowIfNull(estado);
				ArgumentNullException.ThrowIfNull(origen);

				this.estado = estado;
				this.origen = origen;
			}

			public override bool FueDeclarada => true;
			public override bool EsLleno => estado.EsLleno;
			public override OrigenDeLectura Origen => origen;
			public override bool EsLlenoQueCuenta => estado.EsLleno && origen.CuentaParaCalcular;
			public override string Expresada => $"{estado.Nombre} ({origen.Nombre})";
		}
	}
}
