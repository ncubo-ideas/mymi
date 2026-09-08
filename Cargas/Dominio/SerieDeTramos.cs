using System;
using System.Collections.Generic;

namespace Cargas.Dominio
{
	/// <summary>
	/// EL ALGORITMO DEL §7: parte la serie de cargas de un vehiculo en tramos entre
	/// tanques llenos. Es una proyeccion que se recalcula entera en cada consulta -un
	/// kilometraje agregado tarde redefine tramos ya vistos, y eso en un modelo
	/// event-sourced solo puede ser recalculo-.
	/// </summary>
	// LAS LEYES, con nombre:
	//
	// 1. UN TRAMO ABRE Y CIERRA EN UN LLENO QUE CUENTA. Lleno con procedencia observada
	//    o confirmada Y kilometraje que cuenta. Hacen falta DOS: la primera carga nunca
	//    da rendimiento, y esta bien decirlo.
	// 2. LOS LITROS DE LA CARGA QUE ABRE NO CUENTAN. Lo consumido en el tramo es lo que
	//    repusieron las cargas posteriores, incluida la que cierra: lo de la que abre ya
	//    se conto en el tramo anterior.
	// 3. UNA PARCIAL NO ROMPE EL TRAMO, LO APLAZA: se acumula adentro.
	// 4. UN LLENO SIN KILOMETRAJE NO CIERRA: el tramo sigue abierto y absorbe -por eso
	//    un kilometraje faltante rompe DOS tramos finos y produce igual el tramo largo-.
	// 5. UN ESTIMADO NO ABRE NI CIERRA NADA: la procedencia lo dice (OrigenDeLectura),
	//    no un if de este algoritmo.
	internal sealed class SerieDeTramos
	{
		private readonly List<Tramo> tramos = new List<Tramo>();

		internal LineaBaseDeRendimiento LineaBase { get; private set; }

		/// <summary>Las cargas deben venir ordenadas por fecha; el orden lo garantiza el
		/// Vehiculo, que es quien las guarda.</summary>
		internal SerieDeTramos(IReadOnlyList<Carga> cargasPorFecha)
		{
			ArgumentNullException.ThrowIfNull(cargasPorFecha);

			Partir(cargasPorFecha);
			MarcarReservas();
			LineaBase = CalcularLineaBase();
			MarcarSospechosos();
		}

		internal IReadOnlyList<Tramo> Tramos => tramos;

		private void Partir(IReadOnlyList<Carga> cargas)
		{
			Carga abridora = null;
			var pendientes = new List<Carga>();
			var llenosSinKilometraje = new List<Carga>();

			foreach (Carga carga in cargas)
			{
				if (abridora == null)
				{
					// Antes de la primera ancla no hay tramo que abrir.
					if (EsAncla(carga)) abridora = carga;
					continue;
				}

				pendientes.Add(carga);

				if (!carga.Tanque.EsLlenoQueCuenta) continue;   // parcial: se absorbe

				if (!carga.Odometro.CierraTramo)
				{
					// Ley 4: no cierra, pero deja su tramo fino bloqueado, que es lo que
					// alimenta la tarjeta SIN TRAMO con nombre y apellido.
					Carga anclaAnterior = llenosSinKilometraje.Count > 0
						? llenosSinKilometraje[llenosSinKilometraje.Count - 1] : abridora;
					tramos.Add(Tramo.Bloqueado(anclaAnterior.Fecha, carga.Fecha,
						LitrosEntre(pendientes, anclaAnterior, carga), QueFalta(carga)));
					llenosSinKilometraje.Add(carga);
					continue;
				}

				// Cierre real. Primero los finos que quedaron truncos hacia adelante...
				if (llenosSinKilometraje.Count > 0)
				{
					Carga ultimoTrunco = llenosSinKilometraje[llenosSinKilometraje.Count - 1];
					tramos.Add(Tramo.Bloqueado(ultimoTrunco.Fecha, carga.Fecha,
						LitrosEntre(pendientes, ultimoTrunco, carga), QueFalta(ultimoTrunco)));
				}

				// ...y el tramo que SI se puede calcular, absorbiendo lo trunco (ley 4).
				tramos.Add(Calcular(abridora, pendientes, carga));

				abridora = carga;
				pendientes.Clear();
				llenosSinKilometraje.Clear();
			}
		}

		private static bool EsAncla(Carga carga) =>
			carga.Tanque.EsLlenoQueCuenta && carga.Odometro.CierraTramo;

		private static Tramo Calcular(Carga abre, List<Carga> aportan, Carga cierra)
		{
			var recorrida = new Distancia(
				cierra.Odometro.Kilometraje.KilometrosDesde(abre.Odometro.Kilometraje),
				UnidadDeDistancia.Kilometro);

			Volumen consumido = null;
			foreach (Carga carga in aportan)
				consumido = consumido == null ? carga.Volumen : consumido.Mas(carga.Volumen);

			return Tramo.Calculado(abre.Fecha, cierra.Fecha, recorrida, consumido,
				InstrumentoUniforme(aportan), CombustibleUniforme(abre, aportan));
		}

		// Los litros repuestos DESPUES de un ancla y HASTA una carga (incluida), para los
		// tramos finos bloqueados. La ley 2 tambien aplica aca: la que abre no aporta.
		private static decimal LitrosEntre(List<Carga> pendientes, Carga despuesDe, Carga hasta)
		{
			// Si el ancla es la abridora del tramo largo, no esta en pendientes y se
			// cuenta desde el principio.
			bool adentro = !pendientes.Contains(despuesDe);

			decimal litros = 0m;
			foreach (Carga carga in pendientes)
			{
				if (!adentro)
				{
					if (ReferenceEquals(carga, despuesDe)) adentro = true;
					continue;
				}

				litros += carga.Volumen.EnLitros;
				if (ReferenceEquals(carga, hasta)) break;
			}
			return litros;
		}

		private static string QueFalta(Carga trunca) =>
			$"el kilometraje de tu carga del {trunca.Fecha:dd/MM/yyyy}";

		private static string InstrumentoUniforme(List<Carga> aportan)
		{
			Surtidor primero = aportan[0].Surtidor;
			foreach (Carga carga in aportan)
				if (!carga.Surtidor.EsComparableCon(primero)) return "";
			return primero.Identificacion;
		}

		private static string CombustibleUniforme(Carga abre, List<Carga> aportan)
		{
			TipoDeCombustible tipo = abre.Tipo;
			foreach (Carga carga in aportan)
				if (!carga.Tipo.EsElMismo(tipo)) return "";
			return tipo.Nombre;
		}

		// La comparabilidad es CONTRA LA SERIE, no solo interna: un tramo medido entero
		// por otro surtidor es consistente por dentro y aun asi no se puede leer contra
		// una linea base medida por el de siempre (§6.3.1). El de referencia es el mas
		// frecuente entre los calculados.
		private void MarcarReservas()
		{
			string instrumentoDeReferencia = MasFrecuente(t => t.InstrumentoDeLosLitros);
			string combustibleDeReferencia = MasFrecuente(t => t.CombustibleDelTramo);

			foreach (Tramo tramo in tramos)
			{
				if (!tramo.EstaCalculado) continue;

				if (tramo.CombustibleDelTramo.Length == 0)
					tramo.MarcarReserva("cambio de combustible dentro del tramo");
				else if (tramo.CombustibleDelTramo != combustibleDeReferencia)
					tramo.MarcarReserva("otro combustible");
				else if (tramo.InstrumentoDeLosLitros.Length == 0)
					tramo.MarcarReserva("varios surtidores dentro del tramo");
				else if (tramo.InstrumentoDeLosLitros != instrumentoDeReferencia)
					tramo.MarcarReserva("otro surtidor");
			}
		}

		private string MasFrecuente(Func<Tramo, string> etiqueta)
		{
			var conteo = new Dictionary<string, int>();
			foreach (Tramo tramo in tramos)
			{
				if (!tramo.EstaCalculado) continue;
				string valor = etiqueta(tramo);
				if (valor.Length == 0) continue;
				conteo[valor] = conteo.TryGetValue(valor, out int veces) ? veces + 1 : 1;
			}

			string masFrecuente = "";
			int maximo = 0;
			foreach (KeyValuePair<string, int> par in conteo)
				if (par.Value > maximo) { maximo = par.Value; masFrecuente = par.Key; }
			return masFrecuente;
		}

		private LineaBaseDeRendimiento CalcularLineaBase()
		{
			var comparables = new List<decimal>();
			foreach (Tramo tramo in tramos)
				if (tramo.EstaCalculado && tramo.EsComparable)
					comparables.Add(tramo.RendimientoEnKmPorLitro);

			if (comparables.Count < 2) return LineaBaseDeRendimiento.SinBase;

			comparables.Sort();
			decimal mediana = comparables.Count % 2 == 1
				? comparables[comparables.Count / 2]
				: (comparables[comparables.Count / 2 - 1] + comparables[comparables.Count / 2]) / 2m;

			return LineaBaseDeRendimiento.De(
				Rendimiento.EnKmPorLitro(mediana), comparables.Count);
		}

		// La regla del §7.2: un tramo MUY por encima de lo habitual es casi seguro una
		// carga sin registrar -quedaron los kilometros de dos tanques con los litros de
		// uno-. Se marca despues de conocer la base; la mediana aguanta al sospechoso
		// adentro sin torcerse, que es la razon de usarla y no un promedio.
		private void MarcarSospechosos()
		{
			if (!LineaBase.Hay) return;

			foreach (Tramo tramo in tramos)
				if (tramo.EstaCalculado && tramo.EsComparable
					&& tramo.Rendimiento.SuperaPorMultiploA(LineaBase.Rendimiento,
						UmbralesDeRendimiento.MultiploImplausible))
					tramo.MarcarSospechoso();
		}
	}
}
