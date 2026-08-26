using System;
using System.Collections.Generic;

namespace Presupuesto.Dominio
{
	// UN IMPREVISTO. Juicio 8 de Preguntas.md, y es el unico que salio de una nota al margen
	// de la entrevista en vez de una pregunta directa.
	//
	// NO ES UN CARGO: ES UN EVENTO QUE AGRUPA CARGOS. La entrevista lo dijo con sus ejemplos
	// sin decirlo con palabras: "podria ser un solo gasto muy grande -una hospitalizacion- o
	// varios gastos pequenos -repuesto auto, reparacion refrigeradora-". Una hospitalizacion
	// produce el hospital, la farmacia y el laboratorio: tres cargos, UN imprevisto.
	//
	// Y ESO DECIDE SI SE PUEDEN ANTICIPAR. No se puede predecir CUAL, pero si CUANTO POR ANO
	// -razonamiento de seguro-. Y la tasa hay que calcularla SOBRE EVENTOS: "tuve 47 cargos
	// no presupuestados este ano" no es informacion; "tuve 3 imprevistos que sumaron 900 mil"
	// si lo es, y de eso si sale cuanto apartar por mes.
	//
	// Las dos mitades hacen cosas distintas: "no estaba presupuestado" da los CANDIDATOS, y
	// la marca de la persona dice A QUE EVENTO pertenecen. La segunda es una AGRUPACION, no
	// una clasificacion, y es la cuarta forma que toma el patron de marcar.
	public sealed class Imprevisto
	{
		private readonly List<Cargo> cargos = new List<Cargo>();

		public Imprevisto(string quePaso, DateTime cuando)
		{
			if (string.IsNullOrWhiteSpace(quePaso))
				throw new ArgumentException(
					"Un imprevisto es algo que paso, y la persona tiene que poder reconocerlo despues.",
					nameof(quePaso));

			QuePaso = quePaso;
			Cuando = cuando.Date;
		}

		// "Hospitalizacion de mama", "se quemo la refrigeradora". Lo escribe la persona: el
		// dominio no tiene de donde sacarlo.
		public string QuePaso { get; }

		public DateTime Cuando { get; }

		public IReadOnlyList<Cargo> Cargos => cargos;

		public void LoCausoTambien(Cargo cargo)
		{
			if (cargo == null) throw new ArgumentNullException(nameof(cargo));
			cargos.Add(cargo);
		}

		public decimal Total
		{
			get
			{
				decimal total = 0m;
				foreach (Cargo c in cargos) total += c.Monto;
				return total;
			}
		}

		public bool OcurrioEntre(DateTime desde, DateTime hasta) =>
			Cuando >= desde.Date && Cuando <= hasta.Date;

		public string Lectura =>
			cargos.Count == 1
				? $"{QuePaso}: {Total}."
				: $"{QuePaso}: {Total} en {cargos.Count} cargos.";

		public override string ToString() => QuePaso;
	}
}
