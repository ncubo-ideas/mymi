using System;

namespace Cargas.Dominio
{
	/// <summary>
	/// CUANTO SE MANEJA ESTE VEHICULO, en distancia por dia, leido de la serie del
	/// odometro. Es la respuesta que ninguna pantalla de la estacion puede dar y que el
	/// concesionario NO TIENE: su registro ve el odometro el dia del service, y esta
	/// bitacora lo ve cada vez que Sonia carga.
	///
	/// POR QUE ES UN TIPO Y NO UN DECIMAL. El ritmo se calculaba y devolvia cero cuando no
	/// se podia, y ese cero tapaba TRES respuestas que se arreglan distinto: no anotaste
	/// ninguno, anotaste uno solo, o los que anotaste son del mismo dia. La situacion viaja
	/// con el numero, no en lugar del numero.
	/// </summary>
	// SOLO ENTRAN LECTURAS QUE CUENTAN, y eso lo garantiza quien lo construye (Vehiculo):
	// observadas o confirmadas, nunca estimadas. Es la anti-circularidad del §7.3, y aca
	// ademas es lo que hace que este numero se pueda poner al lado del registro de otra
	// institucion: un ritmo derivado de un odometro estimado seria una medicion que nadie
	// hizo, que es justo lo que el MCP del concesionario prohibe aceptar.
	internal sealed class LecturaDeRitmo
	{
		internal static LecturaDeRitmo Medido(decimal kilometrosPorDia, decimal kilometros,
			int dias, DateTime desde, DateTime hasta, int lecturas) =>
			new LecturaDeRitmo(SituacionDelRitmo.Medido, kilometrosPorDia, kilometros,
				dias, desde, hasta, lecturas);

		internal static LecturaDeRitmo NoSePudo(SituacionDelRitmo situacion, int lecturas,
			DateTime desde, DateTime hasta)
		{
			ArgumentNullException.ThrowIfNull(situacion);
			if (situacion.SePudoMedir)
				throw new ErrorDeDominio(
					"NoSePudo con la situacion 'medido' es una contradiccion: usar Medido.");

			return new LecturaDeRitmo(situacion, 0m, 0m, 0, desde, hasta, lecturas);
		}

		internal SituacionDelRitmo Situacion { get; private set; }

		/// <summary>Cero cuando no se pudo medir, y ahi NO es un ritmo de cero: consultar
		/// antes Situacion.SePudoMedir.</summary>
		internal decimal KilometrosPorDia { get; private set; }

		/// <summary>Lo recorrido entre las dos puntas. Es de donde sale el ritmo, y va
		/// para que la frase sea falsable.</summary>
		internal decimal Kilometros { get; private set; }

		internal int Dias { get; private set; }

		/// <summary>Las fechas de la primera y la ultima lectura que cuentan. Con una
		/// sola lectura las dos son la misma; sin ninguna, default.</summary>
		internal DateTime Desde { get; private set; }
		internal DateTime Hasta { get; private set; }

		/// <summary>Cuantas lecturas alimentaron esto. Las estimadas NO se cuentan: no
		/// entran al calculo, asi que decir que participaron seria mentir.</summary>
		internal int Lecturas { get; private set; }

		private LecturaDeRitmo(SituacionDelRitmo situacion, decimal kilometrosPorDia,
			decimal kilometros, int dias, DateTime desde, DateTime hasta, int lecturas)
		{
			Situacion = situacion;
			KilometrosPorDia = kilometrosPorDia;
			Kilometros = kilometros;
			Dias = dias;
			Desde = desde;
			Hasta = hasta;
			Lecturas = lecturas;
		}

		internal bool SePudoMedir => Situacion.SePudoMedir;

		/// <summary>El nombre de la situacion, plano. Igual que Carga.NombreDelCorte: el
		/// print de la frontera lee valores, no navega objetos.</summary>
		internal string NombreDeLaSituacion => Situacion.Nombre;

		// LA MIRADA LA PONE LA BITACORA al entregarlo, igual que en ProyeccionDeOdometro y
		// en Tramo. Aca no es cosmetico: este numero se va a poner al lado de la distancia
		// que le falta al carro para un service, y esa distancia viene en la unidad del
		// concesionario. Un ritmo en km/dia leido como millas/dia da una fecha de service
		// 1.6 veces mas lejos.
		private ComoLoMira mirada = ComoLoMira.Canonica;

		internal void MirarloComo(ComoLoMira como)
		{
			ArgumentNullException.ThrowIfNull(como);
			mirada = como;
		}

		internal decimal PorDiaComoSeVe => mirada.DistanciaComoSeVe(KilometrosPorDia);
		internal decimal KilometrosComoSeVen => mirada.DistanciaComoSeVe(Kilometros);
		internal string SimboloDeLaDistancia => mirada.SimboloDeLaDistancia;

		/// <summary>LA RESPUESTA, compuesta por el dominio. Es lo que hay que decir; no se
		/// rearma dividiendo Kilometros entre Dias, que daria un ritmo que nadie midio
		/// -y con otro redondeo-.</summary>
		internal string Expresada
		{
			get
			{
				if (!SePudoMedir)
					return Situacion.Porque;

				// NO HAY RAMA PARA "EL CARRO NO SE MOVIO", y no es un olvido: el odometro
				// del vehiculo es ESTRICTAMENTE creciente -EsKilometrajeCoherente rechaza
				// con >= y <=-, asi que dos lecturas distintas no pueden marcar lo mismo y
				// un ritmo medido siempre es mayor que cero. Escribir esa rama seria
				// inventar un caso que el dominio no deja construir.
				string unidad = SimboloDeLaDistancia;
				return $"{Cifras.Aproximada(PorDiaComoSeVe)} {unidad} por dia: "
				     + $"{Cifras.Entera(KilometrosComoSeVen)} {unidad} entre el "
				     + $"{Desde:dd/MM/yyyy} y el {Hasta:dd/MM/yyyy}, sobre {Lecturas} "
				     + $"kilometrajes que anotaste vos";
			}
		}
	}
}
