using System;
using System.Collections.Generic;

namespace Examenes.Dominio
{
	// LA SERIE DEL COLESTEROL FRACCIONADO. La llave es el LDL; el HDL viaja al lado y se
	// lee AL REVES, que es lo que ningun otro analito del demo hace.
	internal sealed class SerieDeColesterolLdl : SerieContinua
	{
		internal SerieDeColesterolLdl() : base(Analito.ColesterolLdl) { }

		private ColesterolDetallado Ultima => (ColesterolDetallado)Ultimo;

		// ---------------------------------------------------------------- lo suyo

		// Si el HDL acompana. Es propio de esta serie porque es la unica que trae una
		// fraccion que juega en contra del valor principal.
		//
		// SU PISO DEPENDE DEL SEXO —40 en un hombre, 50 en una mujer— asi que hay que
		// preguntar antes si se sabe. Notar que el LDL, que es la llave de esta serie, NO
		// depende del sexo: por eso esta serie SI se puede leer sin el dato y lo unico que
		// se calla es la coletilla del acompanante. Es la diferencia entre "no puedo
		// interpretar esto" y "puedo interpretarlo, con una frase menos".
		internal bool SeSabeSiElHdlAcompana(Paciente paciente) => Ultima.SeSabeSiElHdlAcompana(paciente);

		internal bool HdlAcompana(Paciente paciente) => Ultima.HdlEsProtectorPara(paciente);

		// Las dos van juntas y en ese orden: preguntar la segunda sin la primera revienta,
		// que es lo correcto —devolver false "mientras tanto" diria que el HDL no protege
		// cuando lo que pasa es que no se sabe—.

		// ---------------------------------------------------------------- como se dibuja

		internal override FormaDeLectura Forma => FormaDeLectura.ProgresoHaciaMeta;

		internal override IReadOnlyList<ReferenciaDeLectura> Referencias(Paciente paciente)
		{
			var lineas = new List<ReferenciaDeLectura>();
			if (Ultimo == null) return lineas;

			RangoDeReferencia rango = Ultima.RangoPara(paciente);
			lineas.Add(new ReferenciaDeLectura("meta", rango.Maximo,
				"linea.colesterol-ldl.meta"));

			return lineas;
		}

		// ---------------------------------------------------------------- la lectura

		protected override Interpretacion Leer(Paciente paciente)
		{
			EvaluacionDeTendencia hechos = Evaluar();
			if (!hechos.HaySerie) return Interpretacion.SinSerie(hechos);

			if (hechos.Tendencia.SugiereRepetirElExamen)
				return new Interpretacion(Veredicto.ProbableErrorDeMedicion, Severidad.Vigilar,
					"lectura.colesterol-ldl.salto-inverosimil", hechos,
					Accion.RepetirElExamen(Plazo.EstaSemana));

			bool enRango = Ultima.EsNormalPara(paciente);

			// LA COLETILLA SE CALLA SI FALTA EL SEXO. No se reemplaza por una version
			// neutra ni se dice "tu HDL de 45": el numero sin su piso no significa nada, y
			// ponerlo al lado del LDL invitaria a leerlo contra el piso equivocado. El LDL
			// se sigue leyendo entero.
			string sobreElHdl = !Ultima.SeSabeSiElHdlAcompana(paciente)
				? ""
				: Ultima.HdlEsProtectorPara(paciente)
					? "hdl.juega-a-favor"
					: "hdl.no-alcanza";

			decimal hdl = Ultima.Hdl.Valor;

			if (!Ultima.EsCambioReal(hechos.Cambio))
				return enRango
					? new Interpretacion(Veredicto.Estable, Severidad.Informativo,
						"lectura.colesterol-ldl.estable-dentro", hechos)
					: new Interpretacion(Veredicto.Estable, Severidad.ConsultePronto,
							"lectura.colesterol-ldl.estable-fuera", hechos,
							Accion.MostrarleEstoATuMedico(Plazo.EsteMes))
						.ConNivelPropio(hdl, sobreElHdl);

			if (Ultima.Direccion.EsFavorable(hechos.Cambio))
				return new Interpretacion(Veredicto.Mejorando, Severidad.Informativo,
					"lectura.colesterol-ldl.bajo", hechos);

			return enRango
				? new Interpretacion(Veredicto.Empeorando, Severidad.Vigilar,
					"lectura.colesterol-ldl.subio-dentro", hechos,
					Accion.ConversarloConTuMedico(Plazo.EnLaProximaCita))
				: new Interpretacion(Veredicto.Empeorando, Severidad.ConsultePronto,
						"lectura.colesterol-ldl.subio-fuera", hechos,
						Accion.MostrarleEstoATuMedico(Plazo.EsteMes))
					.ConNivelPropio(hdl, sobreElHdl);
		}
	}
}
