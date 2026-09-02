using System;

namespace Cargas.Dominio
{
	/// <summary>
	/// La lealtad de Sonia con la estacion: saldo y nivel. Presente para que el negocio
	/// se sienta real, NO es el foco. Los puntos y el nivel LLEGAN como eventos de la
	/// institucion, nunca se calculan aca (§11 del brief): Puntos no es Dinero, y la
	/// tasa de canje es una regla del programa que puede cambiar.
	/// </summary>
	public sealed class ProgramaDeLealtad
	{
		public Puntos Saldo { get; private set; }
		public NivelDeLealtad Nivel { get; private set; }

		public ProgramaDeLealtad()
		{
			Saldo = new Puntos(0);
			Nivel = NivelDeLealtad.Base;
		}

		public void Acreditar(Puntos puntos)
		{
			ArgumentNullException.ThrowIfNull(puntos);
			Saldo = Saldo.Mas(puntos);
		}

		/// <summary>El nivel lo decide la institucion; aca solo se registra. Bajar de
		/// nivel tambien es posible: es su programa.</summary>
		public void ActualizarNivel(NivelDeLealtad nivel)
		{
			ArgumentNullException.ThrowIfNull(nivel);
			Nivel = nivel;
		}

		// ------------------------------- proyecciones planas, para el print del actor

		public int PuntosDelSaldo => Saldo.Cantidad;
		public string NombreDelNivel => Nivel.Nombre;

		/// <summary>Cuantos faltan para el siguiente nivel. Cero en el ultimo.</summary>
		public int PuntosParaElSiguienteNivel =>
			Nivel.EsElUltimo ? 0 : Saldo.LosQueFaltanPara(Nivel.ParaElSiguiente).Cantidad;
	}
}
