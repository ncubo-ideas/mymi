using System;
using System.Collections.Generic;

namespace Cargas.Dominio
{
	/// <summary>
	/// A quien pertenecen las cargas. SIN ATRIBUCION A UN VEHICULO LA SERIE ES BASURA:
	/// mezclar el carro y la moto produce un rendimiento sin sentido. La identidad la
	/// resuelve la PLACA, que llega de la estacion con cada carga; que carro es ese
	/// -la ficha- lo aporta Sonia cuando quiere. Un vehiculo puede existir con solo la
	/// placa, y ese es el momento de pedirle los datos: cuando ya hay algo que nombrar.
	/// </summary>
	// El vehiculo es un concepto LOCAL y minimo: el objeto de negocio "vehiculo" es del
	// modulo del concesionario (lamina 5), y aca su unico proposito es atribuir las
	// cargas y prestar su serie. La placa es la llave con la que los dos modulos hablan
	// del mismo carro sin comunicarse entre si.
	internal sealed class Vehiculo
	{
		internal Placa Placa { get; private set; }
		internal FichaDelVehiculo Ficha { get; private set; }

		// Ordenadas por fecha SIEMPRE: los eventos pueden llegar atrasados y la serie
		// se lee cronologica. El orden se garantiza al insertar, no al consultar.
		private readonly List<Carga> cargas = new List<Carga>();

		internal Vehiculo(Placa placa)
		{
			ArgumentNullException.ThrowIfNull(placa);

			Placa = placa;
			Ficha = FichaDelVehiculo.Vacia;
		}

		/// <summary>La placa hasta que Sonia le ponga alias; el alias despues.</summary>
		internal string NombreParaMostrar => Ficha.Existe ? Ficha.Alias : Placa.ParaMostrar;

		internal bool TieneFicha => Ficha.Existe;

		// LA FICHA, PLANA Y TOTAL. Sus miembros revientan cuando no existe -devolver algo
		// mentiria- asi que la proyeccion para el print devuelve el vacio de cada tipo.
		// Sirve para PRELLENAR el formulario: editar una ficha es corregir la que hay, y
		// sin esto la pantalla tendria que pedirla en blanco cada vez.
		internal string AliasDeLaFicha => Ficha.Existe ? Ficha.Alias : "";
		internal string MarcaDeLaFicha => Ficha.Existe ? Ficha.Marca : "";
		internal string ModeloDeLaFicha => Ficha.Existe ? Ficha.Modelo : "";
		internal int AnioDeLaFicha => Ficha.Existe ? Ficha.Anio : FichaDelVehiculo.SinAnio;
		internal string ColorDeLaFicha => Ficha.Existe ? Ficha.Color : "";

		/// <summary>El rendimiento de fabrica en km/L, o 0 si no lo declaro. El cero es
		/// ausencia y no un valor: el tipo del dominio ya lo distingue.</summary>
		internal decimal RendimientoDeFabricaDeLaFicha =>
			Ficha.Existe && Ficha.RendimientoDeFabrica.Existe
				? Ficha.RendimientoDeFabrica.Valor.KilometrosPorLitro
				: 0m;
		internal string TextoDeLaPlaca => Placa.Texto;
		internal int CantidadDeCargas => cargas.Count;

		internal void Agregar(Carga carga)
		{
			ArgumentNullException.ThrowIfNull(carga);
			if (!carga.Placa.EsLaMisma(Placa))
				throw new ErrorDeDominio(
					$"La carga es de la placa {carga.Placa.Texto} y este vehiculo es {Placa.Texto}: "
					+ "la atribucion la decide la bitacora, no este metodo.");

			int posicion = cargas.Count;
			while (posicion > 0 && cargas[posicion - 1].Fecha > carga.Fecha) posicion--;
			cargas.Insert(posicion, carga);
		}

		internal void CompletarFicha(FichaDelVehiculo ficha)
		{
			ArgumentNullException.ThrowIfNull(ficha);
			Ficha = ficha;
		}

		internal IReadOnlyList<Carga> Cargas => cargas;

		// ------------------------------------------------------- la coherencia del odometro

		/// <summary>Si un kilometraje es coherente con la serie: el odometro es monotono
		/// creciente. Total, para la guarda del comando: decide sin reventar.</summary>
		internal bool EsKilometrajeCoherente(Carga cargaDestino, decimal enKilometros)
		{
			foreach (Carga otra in cargas)
			{
				if (ReferenceEquals(otra, cargaDestino)) continue;
				if (!otra.Odometro.Existe) continue;

				decimal kilometrajeDeLaOtra = otra.Odometro.Kilometraje.EnKilometros;
				if (otra.Fecha < cargaDestino.Fecha && kilometrajeDeLaOtra >= enKilometros) return false;
				if (otra.Fecha > cargaDestino.Fecha && kilometrajeDeLaOtra <= enKilometros) return false;
			}
			return true;
		}

		// Un retroceso es error de captura, vuelta a cero o cambio de tablero, y no
		// entra: envenenaria todos los tramos que lo toquen. Se exige aca porque la
		// coherencia necesita a las cargas vecinas, que solo este vehiculo tiene.
		internal void ExigirKilometrajeCoherente(Carga cargaDestino, decimal enKilometros)
		{
			if (!EsKilometrajeCoherente(cargaDestino, enKilometros))
				throw new ErrorDeDominio(
					$"Un kilometraje de {enKilometros:0} km en la carga del "
					+ $"{cargaDestino.Fecha:dd/MM/yyyy} no es coherente con la serie: el "
					+ "odometro es monotono creciente. Puede ser un error de captura.");
		}

		// -------------------------------------------------------------------- la serie

		internal SerieDeTramos Serie() => new SerieDeTramos(cargas);

		/// <summary>La lectura de la serie completa: el veredicto con sus hechos.</summary>
		/// <summary>La lectura de la serie EN CANONICO. Sigue existiendo porque las
		/// comprobaciones y cualquier lectura que no dependa de como ella lo mire no tienen
		/// por que arrastrar una preferencia de presentacion.</summary>
		internal EvaluacionDeRendimiento Evaluar() => Evaluar(ComoLoMira.Canonica);

		/// <summary>La misma lectura, dicha en las unidades que ella eligio.
		///
		/// EL VEREDICTO NO SE MUEVE: mejoro y empeoro se deciden con el cambio CANONICO,
		/// que es donde se juzgo. Lo que cambia con la unidad es la flecha, la palabra y la
		/// magnitud — y por eso el porcentaje sale de CambioComoSeVe y no del canonico: la
		/// inversa no es lineal, y "un 25% menos" al lado de 7.9 y 10.6 se contradice solo.</summary>
		internal EvaluacionDeRendimiento Evaluar(ComoLoMira mirada)
		{
			ArgumentNullException.ThrowIfNull(mirada);

			// Funcion local y no un helper estatico: asi CAPTURA la mirada y las siete
			// salidas de abajo no tienen que arrastrarla una por una.
			EvaluacionDeRendimiento Evaluacion(VeredictoDeRendimiento veredicto,
				Severidad severidad, Accion accion, decimal actual, decimal habitual,
				decimal cambio, int calculados, int bloqueados, bool haySerie,
				string paraSonia, string queFalta, string motivo) =>
				new EvaluacionDeRendimiento(veredicto, severidad, accion, actual, habitual,
					cambio, calculados, bloqueados, haySerie, paraSonia, queFalta, motivo,
					mirada);

			SerieDeTramos serie = Serie();
			var calculados = new List<Tramo>();
			int bloqueados = 0;
			foreach (Tramo tramo in serie.Tramos)
			{
				if (tramo.EstaCalculado) calculados.Add(tramo);
				else bloqueados++;
			}

			bool haySerie = serie.LineaBase.Hay;
			decimal habitual = serie.LineaBase.KilometrosPorLitro;

			// La calidad del dato va primero: un total que no cuadra en la carga mas
			// reciente es un problema a reportar antes que cualquier lectura.
			if (cargas.Count > 0 && !cargas[cargas.Count - 1].ElTotalCuadra)
				return Evaluacion(VeredictoDeRendimiento.TotalNoCuadra, Severidad.Vigilar,
					Accion.MostrarleEstoATuEstacion(Plazo.EsteMes), 0m, habitual, 0m,
					calculados.Count, bloqueados, haySerie,
					$"el total de tu carga del {cargas[cargas.Count - 1].Fecha:dd/MM/yyyy} "
					+ "no cuadra con precio por volumen", "", "");

			if (calculados.Count == 0)
			{
				string queFalta = PrimerFaltante(serie);
				return Evaluacion(VeredictoDeRendimiento.SinTramo, Severidad.Informativo,
					null, 0m, habitual, 0m, 0, bloqueados, haySerie,
					$"todavía no puedo calcular tu rendimiento: falta {queFalta}",
					queFalta, "");
			}

			Tramo ultimo = calculados[calculados.Count - 1];
			decimal actual = ultimo.RendimientoEnKmPorLitro;

			if (ultimo.EsSospechoso)
				return Evaluacion(VeredictoDeRendimiento.DatoSospechoso, Severidad.Vigilar,
					Accion.RegistrarLaCargaQueFalto(Plazo.EstaSemana), actual, habitual,
					0m, calculados.Count, bloqueados, haySerie,
					$"un tramo dio {mirada.Rendimiento.Formatear(ultimo.Rendimiento)} contra tu "
					+ $"nivel de siempre de {mirada.Rendimiento.Formatear(serie.LineaBase.Rendimiento)}: "
					+ "casi seguro cargaste y no quedó registrada", "", "");

			if (!ultimo.EsComparable)
				return Evaluacion(VeredictoDeRendimiento.NoComparable, Severidad.Informativo,
					null, actual, habitual, 0m, calculados.Count, bloqueados, haySerie,
					$"tu último tramo se midió con {ultimo.MotivoDeReserva}: no te lo "
					+ "puedo comparar con los anteriores", "", ultimo.MotivoDeReserva);

			if (!haySerie)
				return Evaluacion(VeredictoDeRendimiento.Indeterminado, Severidad.Informativo,
					null, actual, 0m, 0m, calculados.Count, bloqueados, false,
					$"un solo tramo calculado ({mirada.Rendimiento.Formatear(ultimo.Rendimiento)}): "
					+ "todavía no hay serie para comparar", "", "");

			decimal cambio = ultimo.Rendimiento
				.CambioProporcionalDesde(serie.LineaBase.Rendimiento);

			if (Math.Abs(cambio) < UmbralesDeRendimiento.CambioMinimoSignificativo)
				return Evaluacion(VeredictoDeRendimiento.Estable, Severidad.Informativo,
					null, actual, habitual, cambio, calculados.Count, bloqueados, true,
					$"tu carro rinde {mirada.Rendimiento.Formatear(ultimo.Rendimiento)}, "
					+ "tu nivel de siempre", "", "");

			// LA FRASE SE ARMA CON LO QUE SE VE, el veredicto con lo canonico. En km/L
			// 12.6 -> 9.4 es un 25% menos; los mismos dos tramos en L/100km son 7.9 -> 10.6,
			// que es un 33% MAS. El carro empeoro en los dos casos.
			decimal comoSeVe = mirada.Rendimiento.CambioComoSeVe(
				serie.LineaBase.Rendimiento, ultimo.Rendimiento);
			string frase = $"venías rindiendo {mirada.Rendimiento.Formatear(serie.LineaBase.Rendimiento)}"
				+ $" y el último tramo dio {mirada.Rendimiento.Convertir(ultimo.Rendimiento):0.#}: "
				+ $"un {Math.Round(Math.Abs(comoSeVe) * 100m):0}% {(comoSeVe > 0m ? "más" : "menos")}";

			if (cambio > 0m)
				return Evaluacion(VeredictoDeRendimiento.Mejoro, Severidad.Informativo,
					null, actual, habitual, cambio, calculados.Count, bloqueados, true,
					frase, "", "");

			return Evaluacion(VeredictoDeRendimiento.Empeoro, Severidad.Vigilar,
				Accion.RevisarloConTuTaller(Plazo.EsteMes), actual, habitual, cambio,
				calculados.Count, bloqueados, true, frase, "", "");
		}

		private string PrimerFaltante(SerieDeTramos serie)
		{
			foreach (Tramo tramo in serie.Tramos)
				if (!tramo.EstaCalculado) return tramo.QueFalta;

			return cargas.Count < 2
				? "una segunda carga con tanque lleno y kilometraje"
				: "un segundo tanque lleno con kilometraje";
		}

		// ---------------------------------------------------------------- lo derivado

		/// <summary>La carga mas grande observada con tanque lleno. Estimacion declarada:
		/// tercera procedencia, ni de Sonia ni de la estacion.</summary>
		internal CapacidadEstimadaDeTanque Capacidad()
		{
			decimal maximo = 0m;
			DateTime fecha = default;
			foreach (Carga carga in cargas)
			{
				if (!carga.Tanque.EsLlenoQueCuenta) continue;
				if (carga.Volumen.EnLitros <= maximo) continue;
				maximo = carga.Volumen.EnLitros;
				fecha = carga.Fecha;
			}
			return maximo > 0m
				? CapacidadEstimadaDeTanque.De(maximo, fecha)
				: CapacidadEstimadaDeTanque.SinEstimacion;
		}

		/// <summary>Capacidad estimada por rendimiento habitual: las dos derivaciones
		/// combinadas. Sin alguna de las dos, no hay autonomia y se dice.</summary>
		internal AutonomiaEstimada Autonomia()
		{
			CapacidadEstimadaDeTanque capacidad = Capacidad();
			LineaBaseDeRendimiento habitual = Serie().LineaBase;
			if (!capacidad.Hay || !habitual.Hay) return AutonomiaEstimada.SinEstimacion;

			return AutonomiaEstimada.De(
				Math.Round(capacidad.Litros * habitual.KilometrosPorLitro),
				capacidad.Litros, habitual.KilometrosPorLitro);
		}

		/// <summary>CUANTO SE MANEJA ESTE CARRO, de las lecturas que cuentan. Devuelve un
		/// tipo y no un decimal porque el cero tapaba cuatro respuestas: sin lecturas, con
		/// una sola, con todas del mismo dia, y el carro que no se movio -que es una
		/// medicion y no una ausencia-. Ver LecturaDeRitmo.</summary>
		internal LecturaDeRitmo Ritmo
		{
			get
			{
				// SOLO LAS QUE CUENTAN. CierraTramo es origen.CuentaParaCalcular, que es
				// false para Estimado POR TIPO: un ritmo derivado de un odometro que a su
				// vez se proyecto de un ritmo se confirmaria a si mismo (§7.3).
				Carga primera = null, ultima = null;
				int lecturas = 0;
				foreach (Carga carga in cargas)
				{
					if (!carga.Odometro.CierraTramo) continue;
					if (primera == null) primera = carga;
					ultima = carga;
					lecturas++;
				}

				if (primera == null)
					return LecturaDeRitmo.NoSePudo(SituacionDelRitmo.SinLecturas, 0,
						default, default);

				if (ReferenceEquals(primera, ultima))
					return LecturaDeRitmo.NoSePudo(SituacionDelRitmo.UnaSolaLectura, 1,
						primera.Fecha, primera.Fecha);

				// La serie esta ordenada por fecha al insertar, asi que ultima nunca es
				// anterior a primera: este caso es EL MISMO DIA y nada mas.
				int dias = (ultima.Fecha.Date - primera.Fecha.Date).Days;
				if (dias <= 0)
					return LecturaDeRitmo.NoSePudo(SituacionDelRitmo.LecturasDelMismoDia,
						lecturas, primera.Fecha, ultima.Fecha);

				decimal kilometros =
					ultima.Odometro.Kilometraje.KilometrosDesde(primera.Odometro.Kilometraje);

				return LecturaDeRitmo.Medido(kilometros / dias, kilometros, dias,
					primera.Fecha, ultima.Fecha, lecturas);
			}
		}

		internal int LecturasQueCuentan
		{
			get
			{
				int lecturas = 0;
				foreach (Carga carga in cargas)
					if (carga.Odometro.CierraTramo) lecturas++;
				return lecturas;
			}
		}

		/// <summary>El kilometraje esperado a una fecha, para el prellenado. Se alimenta
		/// SOLO de lecturas observadas o confirmadas (anti-circularidad del §7.3) y lo
		/// que produce lleva origen estimado. El "hoy" entra como parametro: el dominio
		/// no lee el reloj.</summary>
		internal ProyeccionDeOdometro OdometroEstimado(DateTime hoy)
		{
			// La guarda se mantiene EXACTA: sin ritmo, o con el carro quieto, no hay
			// nada que proyectar hacia adelante. Que ahora se pregunte por la situacion
			// en vez de por el signo del cero no cambia que casos entran.
			// La guarda se mantiene EXACTA. El odometro es estrictamente creciente, asi
			// que un ritmo medido siempre es mayor que cero: preguntar por la situacion
			// deja pasar los mismos casos que preguntaba el signo del decimal.
			LecturaDeRitmo ritmo = Ritmo;
			if (!ritmo.SePudoMedir) return ProyeccionDeOdometro.SinProyeccion;
			decimal kmPorDia = ritmo.KilometrosPorDia;

			Carga ultima = null;
			foreach (Carga carga in cargas)
				if (carga.Odometro.CierraTramo && carga.Fecha <= hoy) ultima = carga;
			if (ultima == null) return ProyeccionDeOdometro.SinProyeccion;

			int dias = (hoy.Date - ultima.Fecha.Date).Days;
			decimal esperado = ultima.Odometro.Kilometraje.EnKilometros + kmPorDia * dias;

			return ProyeccionDeOdometro.De(Math.Round(esperado), Math.Round(kmPorDia, 1), dias);
		}

		// ------------------------------------------------------- lo que falta y cuanto hay

		internal CompletitudDeDatos Completitud()
		{
			int conKilometraje = 0;
			foreach (Carga carga in cargas)
				if (carga.Odometro.Existe) conKilometraje++;

			SerieDeTramos serie = Serie();
			int calculados = 0, bloqueados = 0;
			foreach (Tramo tramo in serie.Tramos)
			{
				if (tramo.EstaCalculado) calculados++;
				else bloqueados++;
			}

			return new CompletitudDeDatos(conKilometraje, cargas.Count, calculados, bloqueados);
		}

		/// <summary>Los datos que faltan, cada uno con lo que desbloquea. El kilometraje
		/// faltante nombra los kilometros que YA se saben -salen de los odometros que
		/// Sonia si puso- porque el trabajo a medio hacer motiva mas que un formulario
		/// en blanco.</summary>
		internal IReadOnlyList<FaltanteDeDato> Faltantes()
		{
			var faltantes = new List<FaltanteDeDato>();
			foreach (Carga carga in cargas)
			{
				if (!carga.Odometro.Existe)
					faltantes.Add(new FaltanteDeDato(carga.Fecha, carga.Transaccion.Texto,
						"el kilometraje", DesbloqueoDelKilometraje(carga)));

				// LA SUPOSICION VIAJA CON EL FALTANTE. Sin ella la pantalla pedia
				// confirmar algo que no enunciaba, y "el tramo que esta carga abre o
				// cierra" era vocabulario del modelo filtrado a la pantalla: nadie que
				// no haya leido este codigo sabe que es un tramo.
				if (carga.TieneInferenciaSinConfirmar)
					faltantes.Add(new FaltanteDeDato(carga.Fecha, carga.Transaccion.Texto,
						"la confirmación del tanque",
						"cuánto rindió tu carro entre esta carga y la siguiente",
						carga.EstadoQueSeSupone, carga.ComoSeDetuvoElSurtidor));
			}
			return faltantes;
		}

		private string DesbloqueoDelKilometraje(Carga trunca)
		{
			Carga anterior = null, posterior = null;
			foreach (Carga carga in cargas)
			{
				if (!carga.Odometro.CierraTramo) continue;
				if (carga.Fecha < trunca.Fecha) anterior = carga;
				if (carga.Fecha > trunca.Fecha && posterior == null) posterior = carga;
			}

			if (anterior == null || posterior == null)
				return "el tramo que quedó abierto";

			decimal kilometros = posterior.Odometro.Kilometraje
				.KilometrosDesde(anterior.Odometro.Kilometraje);
			return $"cuánto rindió tu carro en estos {kilometros:0} km entre el "
				+ $"{anterior.Fecha:dd/MM} y el {posterior.Fecha:dd/MM}";
		}

		// ------------------------------------------------------------------ la billetera

		/// <summary>Cuanto cuesta el kilometro, POR MONEDA. La misma ley del tramo
		/// aplicada al dinero: lo gastado despues de la primera lectura valida, entre
		/// los kilometros recorridos desde ella.</summary>
		internal IReadOnlyList<CostoPorDistanciaEnMoneda> CostoPorDistancia()
		{
			var filas = new List<CostoPorDistanciaEnMoneda>();

			Carga primera = null, ultima = null;
			foreach (Carga carga in cargas)
			{
				if (!carga.Odometro.CierraTramo) continue;
				if (primera == null) primera = carga;
				ultima = carga;
			}
			if (primera == null || ReferenceEquals(primera, ultima)) return filas;

			decimal kilometros = ultima.Odometro.Kilometraje
				.KilometrosDesde(primera.Odometro.Kilometraje);
			if (kilometros <= 0m) return filas;

			var montos = new Dictionary<Moneda, decimal>();
			bool despuesDeLaPrimera = false;
			foreach (Carga carga in cargas)
			{
				if (ReferenceEquals(carga, primera)) { despuesDeLaPrimera = true; continue; }
				if (!despuesDeLaPrimera) continue;
				if (carga.Fecha > ultima.Fecha) break;

				montos[carga.Total.Moneda] = montos.TryGetValue(carga.Total.Moneda, out decimal m)
					? m + carga.Total.Monto : carga.Total.Monto;
			}

			foreach (KeyValuePair<Moneda, decimal> par in montos)
				filas.Add(new CostoPorDistanciaEnMoneda(par.Key,
					Math.Round(par.Value / kilometros, 2), kilometros));
			return filas;
		}

		/// <summary>La historia del precio por litro en los surtidores donde cargo.</summary>
		internal IReadOnlyList<PrecioEnElTiempo> Precios()
		{
			var precios = new List<PrecioEnElTiempo>();
			foreach (Carga carga in cargas)
			{
				decimal litrosPorUnidad = carga.Volumen.Unidad.EnLitros(1m);
				precios.Add(new PrecioEnElTiempo(carga.Fecha,
					Math.Round(carga.Surtidor.PrecioPorUnidad.Monto / litrosPorUnidad, 2),
					carga.Surtidor.PrecioPorUnidad.Moneda,
					carga.Surtidor.Identificacion));
			}
			return precios;
		}
	}
}
