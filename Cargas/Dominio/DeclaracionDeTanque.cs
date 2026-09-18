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
	internal abstract class DeclaracionDeTanque
	{
		/// <summary>La ausencia, como instancia unica.</summary>
		internal static readonly DeclaracionDeTanque Ninguna = new SinDeclaracion();

		internal static DeclaracionDeTanque De(EstadoDelTanque estado, OrigenDeLectura origen) =>
			new TanqueDeclarado(estado, origen);

		internal abstract bool FueDeclarada { get; }

		/// <summary>Revienta si no hay declaracion: consultar antes FueDeclarada.</summary>
		internal abstract bool EsLleno { get; }

		/// <summary>Revienta si no hay declaracion.</summary>
		internal abstract OrigenDeLectura Origen { get; }

		/// <summary>Si esta declaracion habilita el calculo: un lleno con procedencia que
		/// cuenta. Total: sin declaracion, parcial o estimado dan false sin reventar.</summary>
		internal abstract bool EsLlenoQueCuenta { get; }

		/// <summary>
		/// EL CODIGO DEL ESTADO declarado, para la lista y el switch prellenado, o vacio si
		/// nadie dijo nada. Total: decide sin reventar, al reves que <c>EsLleno</c>.
		/// </summary>
		internal abstract string ClaveDelEstado { get; }

		private sealed class SinDeclaracion : DeclaracionDeTanque
		{
			internal override bool FueDeclarada => false;

			internal override bool EsLleno =>
				throw new ErrorDeDominio(
					"Nadie declaro el tanque de esta carga: preguntar por EsLleno exige "
					+ "consultar antes FueDeclarada.");

			internal override OrigenDeLectura Origen =>
				throw new ErrorDeDominio(
					"Una declaracion que no existe no tiene origen: consultar antes FueDeclarada.");

			internal override bool EsLlenoQueCuenta => false;

			internal override string ClaveDelEstado => "";
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

			internal override bool FueDeclarada => true;
			internal override bool EsLleno => estado.EsLleno;
			internal override OrigenDeLectura Origen => origen;
			internal override bool EsLlenoQueCuenta => estado.EsLleno && origen.CuentaParaCalcular;
			internal override string ClaveDelEstado => estado.Nombre;
		}
	}
}
