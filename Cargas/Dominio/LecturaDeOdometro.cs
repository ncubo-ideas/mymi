using System;

namespace Cargas.Dominio
{
	/// <summary>
	/// El kilometraje que se anoto para una carga, con su procedencia. La ausencia es un
	/// tipo -Ninguna-, no un null: "no me dijiste" es informacion, y de los dos aportes
	/// de Sonia este es el unico que de verdad depende de ella (el tanque se infiere de
	/// la forma de corte).
	/// </summary>
	public abstract class LecturaDeOdometro
	{
		/// <summary>La ausencia, como instancia unica.</summary>
		public static readonly LecturaDeOdometro Ninguna = new SinLectura();

		public static LecturaDeOdometro De(Distancia kilometraje, OrigenDeLectura origen) =>
			new LecturaTomada(kilometraje, origen);

		public abstract bool Existe { get; }

		/// <summary>Revienta si no hay lectura: consultar antes Existe.</summary>
		public abstract Distancia Kilometraje { get; }

		/// <summary>Revienta si no hay lectura.</summary>
		public abstract OrigenDeLectura Origen { get; }

		/// <summary>Si esta lectura puede abrir o cerrar un tramo: existe y su origen
		/// cuenta. Total: la ausencia y el estimado dan false sin reventar.</summary>
		public abstract bool CierraTramo { get; }

		/// <summary>"42 380 km (observado)", o vacio si no hay lectura.</summary>
		public abstract string Expresada { get; }

		private sealed class SinLectura : LecturaDeOdometro
		{
			public override bool Existe => false;

			public override Distancia Kilometraje =>
				throw new ErrorDeDominio(
					"Esta carga no tiene kilometraje anotado: preguntar por Kilometraje "
					+ "exige consultar antes Existe.");

			public override OrigenDeLectura Origen =>
				throw new ErrorDeDominio(
					"Una lectura que no existe no tiene origen: consultar antes Existe.");

			public override bool CierraTramo => false;

			public override string Expresada => "";
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

			public override bool Existe => true;
			public override Distancia Kilometraje => kilometraje;
			public override OrigenDeLectura Origen => origen;
			public override bool CierraTramo => origen.CuentaParaCalcular;
			public override string Expresada => $"{kilometraje.Expresada} ({origen.Nombre})";
		}
	}
}
