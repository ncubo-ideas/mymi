using System;
using System.Collections.Generic;

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
		private static readonly AnalitoContinuo Suyo = Examenes.Dominio.Analito.ColesterolLdl;

		// El HDL es OTRO analito, y ahora se puede decir asi en vez de pasar su nombre
		// suelto como texto: viaja en el examen, se muestra, y no es la llave de la serie.
		private static readonly AnalitoContinuo ElDelHdl = Examenes.Dominio.Analito.ColesterolHdl;

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

		// DOS ANALITOS, no uno. Este examen trae el LDL y su HDL, y son dos cosas medidas
		// distintas: una se lee hacia abajo y la otra hacia arriba. Sin este override
		// heredaba el de ExamenDeSangre -que emite una sola lectura- y el HDL desaparecia
		// del historial. Es el mismo aplanamiento que este cambio vino a sacar, en el
		// unico examen de sangre que mide mas de una cosa.
		public override IReadOnlyList<LecturaDeExamen> Lecturas => new[]
		{
			new LecturaDeExamen(Examenes.Dominio.Analito.ColesterolLdl, Ldl),
			new LecturaDeExamen(Examenes.Dominio.Analito.ColesterolHdl, Hdl),
		};

		// EL RANGO DEL LDL NO DEPENDE DEL SEXO. El de su HDL acompanante SI, y las dos
		// cosas conviven en el mismo examen: ver PisoDelHdlPara, abajo. Es el ejemplo mas
		// claro de que la regla no es plana ni siquiera dentro de un examen.
		public override RangoDeReferencia RangoPara(Paciente paciente)
		{
			ArgumentNullException.ThrowIfNull(paciente);
			return RangoDeReferencia.HastaUnMaximoDe(paciente.Edad >= 50 ? 100m : 130m, Suyo.Unidad);
		}

		public override decimal CambioMaximoVerosimilPorDia => 0.8m;

		public override decimal CambioMinimoSignificativo => 12m;

		public override DireccionDeseable Direccion => DireccionDeseable.Bajar;

		// Igual que el colesterol total: en rango cada cinco anos, fuera todos los anos.
		public override decimal MultiploQueNingunIntervaloJustifica => 4m;

		public override int MesesHastaElProximo(Paciente paciente) =>
			EsNormalPara(paciente) ? 60 : 12;

		// ---------------------------------------------------------------- lo suyo

		// El HDL es el unico analito del demo donde ALTO es lo bueno, asi que su
		// clasificacion no se puede delegar al rango de la clase. Se dice aparte, con
		// nombre, en vez de invertir silenciosamente el significado de "alto".
		//
		// Es la DireccionDeseable.Subir del dominio hecha caso concreto, y el metodo que
		// ningun otro analito tiene: nadie mas trae una fraccion acompanante que se lea
		// al reves que el valor principal.
		//
		// Y SU PISO DEPENDE DEL SEXO. Es el SEGUNDO analito con ese efecto —el otro es la
		// hemoglobina— y el corte no es un detalle: un HDL de 45 protege en un hombre y no
		// alcanza en una mujer. Mismo numero, lectura opuesta.
		//
		// Null cuando no se sabe el sexo, por la misma razon que el rango de la
		// hemoglobina: dar el piso masculino "mientras tanto" le diria a la mitad de las
		// personas que estan protegidas cuando no lo estan.
		public decimal? PisoDelHdlPara(Paciente paciente)
		{
			ArgumentNullException.ThrowIfNull(paciente);

			if (!paciente.SeSabeElSexo) return null;
			return paciente.Sexo.EsElMismo(Sexo.Mujer) ? 50m : 40m;
		}

		// Si de esta persona se puede decir algo del HDL. Lo pregunta la serie antes de
		// nombrarlo en su lectura: sin sexo, el LDL se sigue leyendo igual y lo unico que
		// se calla es la coletilla sobre el acompanante.
		public bool SeSabeSiElHdlAcompana(Paciente paciente) => PisoDelHdlPara(paciente) != null;

		public bool HdlEsProtectorPara(Paciente paciente)
		{
			decimal? piso = PisoDelHdlPara(paciente);
			if (piso == null)
				throw new InvalidOperationException(
					"El piso del HDL depende del sexo, y de esta persona no se sabe. Hay que "
					+ "preguntar SeSabeSiElHdlAcompana antes de leer el acompanante.");

			return Hdl.Valor >= piso.Value;
		}

		public override SerieContinua NuevaSerie() => new SerieDeColesterolLdl();
	}
}
