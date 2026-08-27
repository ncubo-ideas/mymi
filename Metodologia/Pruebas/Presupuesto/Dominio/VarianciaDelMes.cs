using System;
using System.Collections.Generic;

namespace Presupuesto.Dominio
{
	// LA RESPUESTA A LA PREGUNTA 1, y no de la forma en que la pregunta esta escrita.
	//
	// La pregunta dice "se incremento el pago de un gasto fijo?", pero el juicio 2 de
	// Preguntas.md corrigio el enfoque a mitad de la entrevista: NO se vigila cada gasto con
	// su propio umbral, se vigila el TOTAL, y el detalle aparece solo para explicar un
	// movimiento.
	//
	// La razon esta escrita en el archivo y vale repetirla: con doce gastos vigilandose a si
	// mismos, algo dispara todos los meses y a los tres nadie mira la app. Un umbral por
	// gasto suena mas fino y es peor.
	//
	// El umbral es 10%, y es del dominio: entra al construir, no vive quemado aca.
	internal sealed class VarianciaDelMes
	{
		private readonly List<Responsable> responsables = new List<Responsable>();

		internal VarianciaDelMes(int ano, int mes, decimal totalDelMes, decimal totalAnterior,
			decimal umbral)
		{
			Ano = ano;
			Mes = mes;
			TotalDelMes = totalDelMes;
			TotalAnterior = totalAnterior;
			Umbral = umbral;
		}

		internal int Ano { get; }
		internal int Mes { get; }
		internal decimal TotalDelMes { get; }
		internal decimal TotalAnterior { get; }
		internal decimal Umbral { get; }

		internal bool HayConQueComparar => TotalAnterior > 0;

		// Con signo: positivo si se gasto mas.
		internal decimal Proporcion =>
			HayConQueComparar ? (TotalDelMes - TotalAnterior) / TotalAnterior : 0m;

		internal bool ExigeAtencion => HayConQueComparar && Math.Abs(Proporcion) > Umbral;

		// Los gastos que explican el movimiento. Solo se llenan cuando ExigeAtencion: si el
		// total no se movio, no hay a quien buscar.
		internal IReadOnlyList<Responsable> Responsables => responsables;

		internal void Culpa(GastoRecurrente gasto, decimal antes, decimal ahora)
		{
			responsables.Add(new Responsable(gasto, antes, ahora));
		}

		internal string Lectura
		{
			get
			{
				if (!HayConQueComparar)
					return "Todavia no hay un mes anterior con el que comparar.";

				if (!ExigeAtencion)
					return $"El total de {Mes:00}/{Ano} se movio {Proporcion:P1}, dentro de lo normal.";

				string direccion = Proporcion > 0 ? "subio" : "bajo";
				return $"El total de {Mes:00}/{Ano} {direccion} {Math.Abs(Proporcion):P1} "
					+ $"contra el mes anterior.";
			}
		}

		// UN GASTO QUE EXPLICA PARTE DEL MOVIMIENTO.
		//
		// HUECO 5 - Preguntas.md no dice QUE convierte a un gasto en responsable. .El que mas
		// cambio en plata? .El que mas cambio en proporcion? .Todos los que se movieron? Con
		// tres gastos que subieron poco y uno que bajo mucho, la respuesta correcta no es
		// obvia y el archivo no la tiene. Aca se listan TODOS los que cambiaron y se ordenan
		// por monto absoluto, que es la decision menos inventiva: no elige por la persona,
		// solo pone primero lo que mas pesa.
		internal sealed class Responsable
		{
			internal Responsable(GastoRecurrente gasto, decimal antes, decimal ahora)
			{
				Gasto = gasto;
				Antes = antes;
				Ahora = ahora;
			}

			internal GastoRecurrente Gasto { get; }
			internal decimal Antes { get; }
			internal decimal Ahora { get; }
			internal decimal Cambio => Ahora - Antes;
			internal decimal Proporcion => Antes > 0 ? Cambio / Antes : 0m;

			// El servicio basico es el unico que sabe si su propio cambio es el clima. Si el
			// gasto no tiene esa nocion, no se le inventa una.
			internal bool EsVariacionNormalDeSuClase =>
				Gasto is ServicioBasico servicio && servicio.EsVariacionNormal(Proporcion);

			internal string Lectura =>
				$"{Gasto.Nombre}: de {Antes} a {Ahora} ({Proporcion:P1})"
				+ (EsVariacionNormalDeSuClase ? ", que es su variacion estacional normal." : ".");
		}
	}
}
