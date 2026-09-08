using System;

namespace Cargas.Dominio
{
	/// <summary>
	/// El kilometraje que el modulo le PROPONE a Sonia para que confirme en vez de
	/// recordar y teclear: distancia diaria promedio por dias desde la ultima lectura.
	/// Se muestra como sugerencia editable con el origen visible -"¿unos 42 900? es lo
	/// que da tu promedio de 38 km por dia"-, nunca como dato consumado.
	/// </summary>
	// LA TRAMPA DE LA CIRCULARIDAD (§7.3): esta proyeccion se alimenta SOLO de lecturas
	// observadas o confirmadas -lo garantiza quien la calcula- y lo que produce lleva
	// origen Estimado, que por tipo no puede cerrar un tramo. Si cerrara, el rendimiento
	// se calcularia con un odometro estimado desde el rendimiento: se confirmaria a si
	// mismo.
	internal sealed class ProyeccionDeOdometro
	{
		internal static readonly ProyeccionDeOdometro SinProyeccion =
			new ProyeccionDeOdometro(0m, 0m, 0);

		internal static ProyeccionDeOdometro De(decimal kilometros, decimal kmPorDia,
			int diasDesdeLaUltimaLectura) =>
			new ProyeccionDeOdometro(kilometros, kmPorDia, diasDesdeLaUltimaLectura);

		/// <summary>El valor propuesto, en kilometros. Cero si no hay proyeccion.</summary>
		internal decimal Kilometros { get; private set; }

		/// <summary>El promedio del que salio, para decirselo a Sonia.</summary>
		internal decimal KilometrosPorDia { get; private set; }

		internal int DiasDesdeLaUltimaLectura { get; private set; }

		private ProyeccionDeOdometro(decimal kilometros, decimal kmPorDia, int dias)
		{
			Kilometros = kilometros;
			KilometrosPorDia = kmPorDia;
			DiasDesdeLaUltimaLectura = dias;
		}

		internal bool Hay => Kilometros > 0m;

		/// <summary>El nombre del origen con el que entraria si Sonia la acepta sin
		/// mirar: estimado. Solo su confirmacion lo sube a confirmado.</summary>
		// LA MIRADA LA PONE LA BITACORA al entregarla, igual que en Tramo. Aca no es
		// cosmetico: el campo del kilometraje llega PRELLENADO con esta proyeccion y lo que
		// se teclea ahi se journaliza con la unidad del campo. Si la proyeccion viniera en
		// km y el campo dijera millas, cada aceptacion entraria 1.6 veces mas grande.
		private ComoLoMira mirada = ComoLoMira.Canonica;

		internal void MirarloComo(ComoLoMira como)
		{
			ArgumentNullException.ThrowIfNull(como);
			mirada = como;
		}

		internal decimal KilometrosComoSeVen => mirada.DistanciaComoSeVe(Kilometros);
		internal decimal PorDiaComoSeVe => mirada.DistanciaComoSeVe(KilometrosPorDia);
		internal string SimboloDeLaDistancia => mirada.SimboloDeLaDistancia;

		internal string Origen => OrigenDeLectura.Estimado.Nombre;
	}
}
