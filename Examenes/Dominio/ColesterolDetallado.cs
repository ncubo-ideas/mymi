using System;

namespace Examenes.Dominio
{
	// Colesterol con fraccionamiento: HDL y LDL.
	//
	// Existe como subclase aparte de Colesterol, y no como un Colesterol con dos campos
	// mas, porque es OTRO analito: el LDL tiene su propio rango y su propia lectura, y
	// no forma serie con el colesterol total. Ponerlos juntos obligaria a que un
	// colesterol total cargara dos mediciones nulas.
	//
	// La serie la forma el LDL, que es la fraccion que decide la conducta clinica. El
	// HDL viaja como dato del examen y se muestra, pero no es la llave de la serie:
	// modelar dos series en un examen seria mezclar dos cosas distintas en una.
	public sealed class ColesterolDetallado : ExamenDeSangre
	{
		public const string NombreDelAnalito = "Colesterol LDL";

		public Medicion Hdl { get; private set; }

		public ColesterolDetallado(DateTime fecha, decimal ldl, decimal hdl, string unidad)
			: base(fecha, Medida(ldl, unidad, UnidadDeMedida.MiligramosPorDecilitro, NombreDelAnalito))
		{
			Hdl = Medida(hdl, unidad, UnidadDeMedida.MiligramosPorDecilitro, "Colesterol HDL");
		}

		public override string Nombre => "Colesterol HDL/LDL";

		public override string Analito => NombreDelAnalito;

		public Medicion Ldl => Valor;

		// Se muestran los dos, porque el examen son los dos.
		public override string ResultadoExpresado => $"LDL {Ldl.Expresado} / HDL {Hdl.Expresado}";

		public override RangoDeReferencia RangoPara(int edad) =>
			RangoDeReferencia.HastaUnMaximoDe(edad >= 50 ? 100m : 130m, UnidadDeMedida.MiligramosPorDecilitro);

		public override decimal CambioMaximoVerosimilPorDia => 0.8m;

		public override decimal CambioQueExigeAtencion => 40m;

		public override bool DireccionPreocupanteEsAscenso => true;

		public override decimal VelocidadQueExigeAtencionPorAno => 0m;

		// El HDL es el unico analito del demo donde ALTO es lo bueno, asi que su
		// clasificacion no se puede delegar al rango de la clase. Se dice aparte, con
		// nombre, en vez de invertir silenciosamente el significado de "alto".
		public bool HdlEsProtector => Hdl.Valor >= 40m;
	}
}
