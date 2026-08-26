using System;
using System.Collections.Generic;

namespace Examenes.Dominio
{
	// LA SERIE DEL COLESTEROL FRACCIONADO. La llave es el LDL; el HDL viaja al lado y se
	// lee AL REVES, que es lo que ningun otro analito del demo hace.
	public sealed class SerieDeColesterolLdl : SerieContinua
	{
		public SerieDeColesterolLdl() : base(Analito.ColesterolLdl) { }

		private ColesterolDetallado Ultima => (ColesterolDetallado)Ultimo;

		// ---------------------------------------------------------------- lo suyo

		// Si el HDL acompana. Es propio de esta serie porque es la unica que trae una
		// fraccion que juega en contra del valor principal.
		public bool HdlAcompana() => Ultima.HdlEsProtector;

		// ---------------------------------------------------------------- como se dibuja

		public override FormaDeLectura Forma => FormaDeLectura.ProgresoHaciaMeta;

		public override IReadOnlyList<ReferenciaDeLectura> Referencias(int edad)
		{
			var lineas = new List<ReferenciaDeLectura>();
			if (Ultimo == null) return lineas;

			RangoDeReferencia rango = Ultima.RangoPara(edad);
			lineas.Add(new ReferenciaDeLectura("meta", rango.Maximo,
				$"tu meta de LDL es bajar de {rango.Maximo.Expresado}"));

			return lineas;
		}

		// ---------------------------------------------------------------- la lectura

		public override Interpretacion Interpretar(int edad)
		{
			EvaluacionDeTendencia hechos = Evaluar();
			if (!hechos.HaySerie) return Interpretacion.SinSerie(hechos);

			if (hechos.Tendencia.SugiereRepetirElExamen)
				return new Interpretacion(Veredicto.ProbableErrorDeMedicion, Severidad.Seguimiento,
					$"el LDL pasó de {hechos.ValorInicial} a {hechos.ValorFinal} {hechos.Unidad} "
					+ $"en {hechos.DiasTranscurridos} día{(hechos.DiasTranscurridos == 1 ? "" : "s")}",
					hechos);

			bool enRango = Ultima.EsNormalPara(edad);
			string sobreElHdl = Ultima.HdlEsProtector
				? $", y tu HDL de {Ultima.Hdl.Expresado} juega a favor"
				: $", y tu HDL de {Ultima.Hdl.Expresado} no alcanza para compensarlo";

			if (!Ultima.EsCambioReal(hechos.Cambio))
				return enRango
					? new Interpretacion(Veredicto.Estable, Severidad.Informativo,
						$"el LDL se mantiene en {Ultima.Ldl.Expresado}, dentro de lo deseable", hechos)
					: new Interpretacion(Veredicto.Estable, Severidad.ConsultaMedica,
						$"el LDL lleva tiempo en {Ultima.Ldl.Expresado}" + sobreElHdl, hechos);

			if (Ultima.Direccion.EsFavorable(hechos.Cambio))
				return new Interpretacion(Veredicto.Mejorando, Severidad.Informativo,
					$"el LDL bajó de {hechos.ValorInicial} a {hechos.ValorFinal} {hechos.Unidad}",
					hechos);

			return enRango
				? new Interpretacion(Veredicto.Empeorando, Severidad.Seguimiento,
					$"el LDL subió de {hechos.ValorInicial} a {hechos.ValorFinal} {hechos.Unidad}, "
					+ "todavía dentro de lo deseable", hechos)
				: new Interpretacion(Veredicto.Empeorando, Severidad.ConsultaMedica,
					$"el LDL subió de {hechos.ValorInicial} a {hechos.ValorFinal} {hechos.Unidad}"
					+ sobreElHdl, hechos);
		}
	}
}
