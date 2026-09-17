using System;

namespace Cargas.Dominio
{
	/// <summary>
	/// El kilometraje que se anoto para una carga, con su procedencia. La ausencia es un
	/// tipo -Ninguna-, no un null: "no me dijiste" es informacion, y de los dos aportes
	/// de Sonia este es el unico que de verdad depende de ella (el tanque se infiere de
	/// la forma de corte).
	/// </summary>
	internal abstract class LecturaDeOdometro
	{
		/// <summary>La ausencia, como instancia unica.</summary>
		internal static readonly LecturaDeOdometro Ninguna = new SinLectura();

		internal static LecturaDeOdometro De(Distancia kilometraje, OrigenDeLectura origen) =>
			new LecturaTomada(kilometraje, origen);

		internal abstract bool Existe { get; }

		/// <summary>Revienta si no hay lectura: consultar antes Existe.</summary>
		internal abstract Distancia Kilometraje { get; }

		/// <summary>Revienta si no hay lectura.</summary>
		internal abstract OrigenDeLectura Origen { get; }

		/// <summary>Si esta lectura puede abrir o cerrar un tramo: existe y su origen
		/// cuenta. Total: la ausencia y el estimado dan false sin reventar.</summary>
		internal abstract bool CierraTramo { get; }

		private sealed class SinLectura : LecturaDeOdometro
		{
			internal override bool Existe => false;

			internal override Distancia Kilometraje =>
				throw new ErrorDeDominio(
					"Esta carga no tiene kilometraje anotado: preguntar por Kilometraje "
					+ "exige consultar antes Existe.");

			internal override OrigenDeLectura Origen =>
				throw new ErrorDeDominio(
					"Una lectura que no existe no tiene origen: consultar antes Existe.");

			internal override bool CierraTramo => false;
		}

		private sealed class LecturaTomada : LecturaDeOdometro
		{
			private readonly Distancia kilometraje;
			private readonly OrigenDeLectura origen;

			internal LecturaTomada(Distancia kilometraje, OrigenDeLectura origen)
			{
				ArgumentNullException.ThrowIfNull(kilometraje);
				ArgumentNullException.ThrowIfNull(origen);

				this.kilometraje = kilometraje;
				this.origen = origen;
			}

			internal override bool Existe => true;
			internal override Distancia Kilometraje => kilometraje;
			internal override OrigenDeLectura Origen => origen;
			internal override bool CierraTramo => origen.CuentaParaCalcular;
		}
	}
}
