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
		private static readonly Analito Suyo = Examenes.Dominio.Analito.ColesterolLdl;

		// El HDL es OTRO analito, y ahora se puede decir asi en vez de pasar su nombre
		// suelto como texto: viaja en el examen, se muestra, y no es la llave de la serie.
		private static readonly Analito ElDelHdl = Examenes.Dominio.Analito.ColesterolHdl;

		public Medicion Hdl { get; private set; }

		public ColesterolDetallado(DateTime fecha, decimal ldl, decimal hdl, string unidad)
			: base(fecha, Suyo, Medida(ldl, unidad, Suyo))
		{
			Hdl = Medida(hdl, unidad, ElDelHdl);
		}

		public override string Nombre => "Colesterol HDL/LDL";

		public Medicion Ldl => Valor;

		// Se muestran los dos, porque el examen son los dos.
		public override string ResultadoExpresado => $"LDL {Ldl.Expresado} / HDL {Hdl.Expresado}";

		public override RangoDeReferencia RangoPara(int edad) =>
			RangoDeReferencia.HastaUnMaximoDe(edad >= 50 ? 100m : 130m, Suyo.Unidad);

		public override decimal CambioMaximoVerosimilPorDia => 0.8m;

		public override decimal CambioMinimoSignificativo => 12m;

		public override DireccionDeseable Direccion => DireccionDeseable.Bajar;

		// Igual que el colesterol total: en rango cada cinco anos, fuera todos los anos.
		public override decimal MultiploQueNingunIntervaloJustifica => 4m;

		public override int MesesHastaElProximo(int edad) => EsNormalPara(edad) ? 60 : 12;

		// ---------------------------------------------------------------- lo suyo

		// El HDL es el unico analito del demo donde ALTO es lo bueno, asi que su
		// clasificacion no se puede delegar al rango de la clase. Se dice aparte, con
		// nombre, en vez de invertir silenciosamente el significado de "alto".
		//
		// Es la DireccionDeseable.Subir del dominio hecha caso concreto, y el metodo que
		// ningun otro analito tiene: nadie mas trae una fraccion acompanante que se lea
		// al reves que el valor principal.
		public bool HdlEsProtector => Hdl.Valor >= 40m;

		public override SerieDeAnalito NuevaSerie() => new SerieDeColesterolLdl();
	}
}
