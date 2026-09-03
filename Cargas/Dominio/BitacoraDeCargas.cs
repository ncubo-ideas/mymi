using System;
using System.Collections.Generic;

namespace Cargas.Dominio
{
	/// <summary>
	/// LA RAIZ AGREGADA: todo lo que Sonia ha cargado, con sus vehiculos y su lealtad.
	/// Analoga al Expediente de examenes y al Facturario de facturas. La atribucion es
	/// automatica: cada carga trae su placa, y el vehiculo se crea solo -con la placa
	/// como unico dato- la primera vez que aparece.
	/// </summary>
	// Los indices se llavean por TEXTO normalizado (placa, transaccion), no por el
	// objeto: los objetos de este dominio se comparan por identidad y no son llave de
	// nada. Los conjuntos cerrados (Moneda) si pueden llavear, porque su identidad de
	// referencia es unica por miembro.
	internal sealed class BitacoraDeCargas
	{
		private readonly Dictionary<string, Vehiculo> vehiculosPorPlaca =
			new Dictionary<string, Vehiculo>();

		private readonly Dictionary<string, Carga> cargasPorTransaccion =
			new Dictionary<string, Carga>(StringComparer.OrdinalIgnoreCase);

		// El orden de llegada de las placas, para que la lista de vehiculos sea estable.
		private readonly List<Vehiculo> vehiculos = new List<Vehiculo>();

		internal ProgramaDeLealtad Lealtad { get; private set; }

		internal BitacoraDeCargas()
		{
			Lealtad = new ProgramaDeLealtad();
		}

		// ------------------------------------------------------ lo que publica la estacion

		internal void Agregar(Carga carga)
		{
			ArgumentNullException.ThrowIfNull(carga);

			// Dos hechos con la misma transaccion son un defecto del emisor: la guarda
			// del evento ya paro los reintentos, asi que esto no es un redelivery.
			if (cargasPorTransaccion.ContainsKey(carga.Transaccion.Texto))
				throw new ErrorDeDominio(
					$"Ya hay una carga con la transaccion {carga.Transaccion.Texto}: "
					+ "la institucion emitio dos hechos con la misma identidad.");

			Vehiculo vehiculo = ObtenerOCrear(carga.Placa);
			vehiculo.Agregar(carga);
			cargasPorTransaccion[carga.Transaccion.Texto] = carga;
		}

		private Vehiculo ObtenerOCrear(Placa placa)
		{
			if (vehiculosPorPlaca.TryGetValue(placa.Texto, out Vehiculo existente))
				return existente;

			var vehiculo = new Vehiculo(placa);
			vehiculosPorPlaca[placa.Texto] = vehiculo;
			vehiculos.Add(vehiculo);
			return vehiculo;
		}

		internal void AcreditarPuntos(int puntos)
		{
			Lealtad.Acreditar(new Puntos(puntos));
		}

		internal void ActualizarNivel(string nivel)
		{
			Lealtad.ActualizarNivel(NivelDeLealtad.PorNombre(nivel));
		}

		// ---------------------------------------------------------- los aportes de Sonia

		// Las guardas de decision son totales -deciden sin reventar- porque alimentan el
		// Check de un comando: un aporte rechazado no debe dejar entrada en el journal.
		// Las operaciones si revientan ante lo imposible: son la segunda linea.

		/// <summary>Si la transaccion existe. Total: la query de reconocimiento que
		/// acompana a la lectura guardada (CargaDe).</summary>
		internal bool ConoceTransaccion(string transaccion) =>
			!string.IsNullOrWhiteSpace(transaccion)
			&& cargasPorTransaccion.ContainsKey(transaccion.Trim());

		/// <summary>Revienta ante una transaccion desconocida: devolver "nada" mentiria,
		/// porque confundiria "no existe la carga" con "no tiene aportes".</summary>
		internal Carga CargaDe(string transaccion)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(transaccion);
			if (!cargasPorTransaccion.TryGetValue(transaccion.Trim(), out Carga carga))
				throw new ErrorDeDominio(
					$"No hay una carga con la transaccion {transaccion}: consultar antes "
					+ "ConoceTransaccion.");
			return carga;
		}

		/// <summary>La guarda del comando AnotarOdometro: la carga existe y el
		/// kilometraje es coherente con la serie (el odometro no retrocede).</summary>
		internal bool PuedeAnotarOdometro(string transaccion, decimal kilometros, string unidad)
		{
			if (!ConoceTransaccion(transaccion)) return false;
			if (kilometros <= 0m) return false;

			Carga carga = CargaDe(transaccion);
			decimal enKilometros = UnidadDeDistancia.PorSimbolo(unidad).EnKilometros(kilometros);
			return VehiculoDe(carga.Placa.Texto).EsKilometrajeCoherente(carga, enKilometros);
		}

		internal void AnotarOdometro(string transaccion, decimal kilometros, string unidad,
			string origen)
		{
			Carga carga = CargaDe(transaccion);
			VehiculoDe(carga.Placa.Texto).ExigirKilometrajeCoherente(carga,
				UnidadDeDistancia.PorSimbolo(unidad).EnKilometros(kilometros));
			carga.AnotarOdometro(kilometros, unidad, origen);
		}

		internal void DeclararTanque(string transaccion, string estado)
		{
			CargaDe(transaccion).DeclararTanque(estado);
		}

		/// <summary>La guarda de ConfirmarTanque: hay una inferencia esperando.</summary>
		internal bool PuedeConfirmarTanque(string transaccion) =>
			ConoceTransaccion(transaccion) && CargaDe(transaccion).TieneInferenciaSinConfirmar;

		internal void ConfirmarTanque(string transaccion)
		{
			CargaDe(transaccion).ConfirmarTanque();
		}

		internal void AdjuntarFoto(string transaccion, string nombreDeArchivo)
		{
			CargaDe(transaccion).AdjuntarFoto(nombreDeArchivo);
		}

		/// <summary>La ficha entra con primitivos del wire y escala aca: el cero en el
		/// rendimiento de fabrica es el "no lo se" del formulario, y se convierte en el
		/// tipo de ausencia antes de tocar el dominio.</summary>
		// ------------------------------------------------------ como lo mira ella

		/// <summary>En que unidades lo mira. Nace canonica y solo ella la cambia.</summary>
		internal ComoLoMira Mirada { get; private set; } = ComoLoMira.Canonica;

		/// <summary>Elegir en que se mira el rendimiento. SIN Check, a proposito: viene de
		/// un boton, y volver a elegir lo mismo es legitimo -no es un reintento del que
		/// haya que defenderse-. Es el mismo criterio que DeclararSexo en examenes.
		///
		/// RECONOCE en la frontera: un simbolo desconocido se rechaza aca y no se
		/// journaliza. Si entrara, el journal quedaria irreproducible para cualquier
		/// binario que no conozca ese simbolo.</summary>
		internal void ElegirPresentacionDeRendimiento(string simbolo)
		{
			if (!PresentacionDeRendimiento.EsConocida(simbolo))
				throw new ErrorDeDominio(
					$"'{simbolo}' no es una forma conocida de mirar el rendimiento.");

			Mirada = Mirada.ConRendimiento(PresentacionDeRendimiento.PorSimbolo(simbolo));
		}

		internal void ElegirUnidadDeDistancia(string simbolo)
		{
			if (!UnidadDeDistancia.EsConocida(simbolo))
				throw new ErrorDeDominio(
					$"'{simbolo}' no es una unidad de distancia conocida.");

			Mirada = Mirada.ConDistancia(UnidadDeDistancia.PorSimbolo(simbolo));
		}

		internal string SimboloDelRendimiento => Mirada.SimboloDelRendimiento;
		internal string SimboloDeLaDistancia => Mirada.SimboloDeLaDistancia;

		internal void CompletarFicha(string placa, string alias, string marca, string modelo,
			int anio, string color, decimal rendimientoDeFabricaKmPorLitro)
		{
			RendimientoDeFabrica deFabrica = rendimientoDeFabricaKmPorLitro > 0m
				? RendimientoDeFabrica.Declarado(rendimientoDeFabricaKmPorLitro)
				: RendimientoDeFabrica.SinDato;

			VehiculoDe(new Placa(placa).Texto).CompletarFicha(
				FichaDelVehiculo.Completada(alias, marca, modelo, anio, color, deFabrica));
		}

		// ------------------------------------------------------------------- consultas

		internal int Cantidad => cargasPorTransaccion.Count;
		internal int CantidadDeVehiculos => vehiculos.Count;

		internal IReadOnlyList<Vehiculo> Vehiculos => vehiculos;

		/// <summary>Total: la query de reconocimiento que acompana a VehiculoDe.</summary>
		internal bool ConocePlaca(string placa) =>
			!string.IsNullOrWhiteSpace(placa)
			&& vehiculosPorPlaca.ContainsKey(new Placa(placa).Texto);

		/// <summary>Revienta ante una placa desconocida: quien pregunta por un vehiculo
		/// concreto tiene un defecto si no existe.</summary>
		internal Vehiculo VehiculoDe(string placa)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(placa);
			string llave = new Placa(placa).Texto;
			if (!vehiculosPorPlaca.TryGetValue(llave, out Vehiculo vehiculo))
				throw new ErrorDeDominio(
					$"No hay un vehiculo con la placa {llave}: consultar antes ConocePlaca.");
			return vehiculo;
		}

		internal IReadOnlyList<Carga> CargasDe(string placa) => VehiculoDe(placa).Cargas;

		/// <summary>Los tramos, ya marcados con la mirada. La bitacora es quien conoce en
		/// que unidades lo mira ella, igual que la serie es quien conoce la linea base: cada
		/// una pone la marca que sabe y el tramo las porta.</summary>
		internal IReadOnlyList<Tramo> TramosDe(string placa)
		{
			IReadOnlyList<Tramo> tramos = VehiculoDe(placa).Serie().Tramos;
			foreach (Tramo tramo in tramos) tramo.MirarloComo(Mirada);
			return tramos;
		}

		internal EvaluacionDeRendimiento EvaluarRendimientoDe(string placa) =>
			VehiculoDe(placa).Evaluar(Mirada);

		internal LineaBaseDeRendimiento LineaBaseDe(string placa) =>
			VehiculoDe(placa).Serie().LineaBase;

		internal CapacidadEstimadaDeTanque CapacidadDe(string placa) =>
			VehiculoDe(placa).Capacidad();

		internal AutonomiaEstimada AutonomiaDe(string placa) => VehiculoDe(placa).Autonomia();

		internal CompletitudDeDatos CompletitudDe(string placa) =>
			VehiculoDe(placa).Completitud();

		internal IReadOnlyList<FaltanteDeDato> FaltantesDe(string placa) =>
			VehiculoDe(placa).Faltantes();

		internal ProyeccionDeOdometro OdometroEstimadoDe(string placa, DateTime hoy)
		{
			ProyeccionDeOdometro proyeccion = VehiculoDe(placa).OdometroEstimado(hoy);
			proyeccion.MirarloComo(Mirada);
			return proyeccion;
		}

		internal IReadOnlyList<CostoPorDistanciaEnMoneda> CostoPorDistanciaDe(string placa) =>
			VehiculoDe(placa).CostoPorDistancia();

		internal IReadOnlyList<PrecioEnElTiempo> PreciosDe(string placa) =>
			VehiculoDe(placa).Precios();

		/// <summary>El gasto de un mes, POR MONEDA y sobre TODOS los vehiculos: es la
		/// pregunta de la billetera, no la del carro.</summary>
		internal IReadOnlyList<GastoEnMoneda> GastoDelPeriodo(int anio, int mes) =>
			GastoEnMoneda.DelPeriodo(cargasPorTransaccion.Values, anio, mes);

		/// <summary>El gasto de un mes DE UN CARRO. Existe porque el tablero es de un
		/// vehiculo: ahi el gasto de todos convivia con la autonomia, el rendimiento y el
		/// costo por kilometro de UNO solo, y con dos carros el instrumento mentia por
		/// omision sin que nada lo dijera.</summary>
		internal IReadOnlyList<GastoEnMoneda> GastoDelPeriodoDe(string placa, int anio, int mes) =>
			GastoEnMoneda.DelPeriodo(VehiculoDe(placa).Cargas, anio, mes);
	}
}
