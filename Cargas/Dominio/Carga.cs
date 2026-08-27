using System;

namespace Cargas.Dominio
{
	/// <summary>
	/// Un evento de abastecimiento. Tiene DOS procedencias en el mismo hecho: lo que
	/// sabe la estacion (fecha, surtidor con su precio, tipo, volumen, total, forma de
	/// corte, placa, numero de transaccion) llega por evento y es inmutable; lo que solo
	/// sabe Sonia (el kilometraje, el tanque, la foto) se ADJUNTA despues, con su
	/// procedencia. Sin el aporte de ella esto es una factura; con el, es conocimiento
	/// sobre su carro.
	/// </summary>
	// EL TOTAL ES AUTORITATIVO, no derivado: precio por volumen no siempre cuadra (hay
	// redondeo de surtidor, descuentos, precios a tres decimales). Si no cuadra mas alla
	// de la tolerancia, el dominio lo reporta en vez de silenciarlo (TotalNoCuadra).
	//
	// El constructor recibe primitivos del wire y escala adentro: es la unica forma que
	// el comando del DSL puede journalizar, y deja el texto del journal legible.
	public sealed class Carga
	{
		public DateTime Fecha { get; private set; }
		public Surtidor Surtidor { get; private set; }
		public TipoDeCombustible Tipo { get; private set; }
		public Volumen Volumen { get; private set; }
		public Dinero Total { get; private set; }
		public FormaDeCorte Corte { get; private set; }
		public Placa Placa { get; private set; }
		public NumeroDeTransaccion Transaccion { get; private set; }

		/// <summary>El aporte de Sonia. Nace ausente; nunca es null.</summary>
		public LecturaDeOdometro Odometro { get; private set; }

		/// <summary>Nace con la INFERENCIA de la forma de corte (origen estimado), que es
		/// el hallazgo del §6.3.3: la app propone y Sonia solo confirma o corrige.</summary>
		public DeclaracionDeTanque Tanque { get; private set; }

		public FotoDelTablero Foto { get; private set; }

		public Carga(DateTime fecha, string estacion, string surtidor, string tipo,
			decimal volumenValor, string volumenUnidad, decimal precioMonto, string precioMoneda,
			decimal totalMonto, string totalMoneda, string formaDeCorte, string placa,
			string numeroDeTransaccion)
		{
			if (fecha == default)
				throw new ErrorDeDominio("Una carga sin fecha no se puede ordenar en la serie.");

			Fecha = fecha;
			Tipo = TipoDeCombustible.PorNombre(tipo);
			Volumen = new Volumen(volumenValor, UnidadDeVolumen.PorSimbolo(volumenUnidad));

			Moneda delPrecio = Moneda.PorCodigo(precioMoneda);
			Moneda delTotal = Moneda.PorCodigo(totalMoneda);
			if (!delPrecio.EsLaMisma(delTotal))
				throw new ErrorDeDominio(
					$"El precio vino en {delPrecio.Codigo} y el total en {delTotal.Codigo}: "
					+ "una carga se paga en UNA moneda.");

			Surtidor = new Surtidor(surtidor, new Estacion(estacion), Tipo,
				new Dinero(precioMonto, delPrecio));
			Total = new Dinero(totalMonto, delTotal);
			Corte = FormaDeCorte.PorNombre(formaDeCorte);
			Placa = new Placa(placa);
			Transaccion = new NumeroDeTransaccion(numeroDeTransaccion);

			Odometro = LecturaDeOdometro.Ninguna;
			Tanque = Corte.Inferencia();
			Foto = FotoDelTablero.Ninguna;
		}

		// ------------------------------------------------------- los aportes de Sonia

		// Anotar (o corregir) el kilometraje. La monotonia contra las cargas vecinas la
		// valida el Vehiculo, que es quien tiene la serie; aca solo se exige que la
		// lectura exista de verdad.
		public void AnotarOdometro(decimal kilometros, string unidad, string origen)
		{
			Odometro = LecturaDeOdometro.De(
				new Distancia(kilometros, UnidadDeDistancia.PorSimbolo(unidad)),
				OrigenDeLectura.PorNombre(origen));
		}

		/// <summary>Sonia declara el tanque directamente, sin pasar por la inferencia.</summary>
		public void DeclararTanque(string estado)
		{
			Tanque = DeclaracionDeTanque.De(EstadoDelTanque.PorNombre(estado),
				OrigenDeLectura.Observado);
		}

		/// <summary>Si hay una inferencia esperando el visto bueno. Es la guarda del
		/// Check de ConfirmarTanque: confirmar lo que nadie propuso es un bug.</summary>
		public bool TieneInferenciaSinConfirmar =>
			Tanque.FueDeclarada && !Tanque.Origen.CuentaParaCalcular;

		// El visto bueno de Sonia a lo que la inferencia propuso: el estimado pasa a
		// confirmado y recien ahi puede abrir o cerrar tramos.
		public void ConfirmarTanque()
		{
			if (!TieneInferenciaSinConfirmar)
				throw new ErrorDeDominio(
					"No hay una inferencia de tanque esperando confirmacion en esta carga.");

			Tanque = DeclaracionDeTanque.De(
				Tanque.EsLleno ? EstadoDelTanque.Lleno : EstadoDelTanque.Parcial,
				OrigenDeLectura.Confirmado);
		}

		public void AdjuntarFoto(string nombreDeArchivo)
		{
			Foto = FotoDelTablero.Guardada(nombreDeArchivo);
		}

		// ------------------------------------------------------------------ el total

		/// <summary>Si el total de la institucion cuadra con precio por volumen, dentro
		/// de la tolerancia de redondeo. Cuando no cuadra, el veredicto TotalNoCuadra lo
		/// dice; el dato igual se conserva, porque el total es el autoritativo.</summary>
		public bool ElTotalCuadra
		{
			get
			{
				var derivado = new Dinero(
					Surtidor.PrecioPorUnidad.Monto * Volumen.Valor, Total.Moneda);
				return Total.DiferenciaAbsolutaCon(derivado)
					<= Total.Monto * UmbralesDeRendimiento.ToleranciaProporcionalDelTotal;
			}
		}

		// ------------------------------- proyecciones planas, para el print del actor

		public string NombreDeLaEstacion => Surtidor.Estacion.Nombre;
		public string IdentificacionDelSurtidor => Surtidor.Identificacion;
		public string NombreDelTipo => Tipo.Nombre;
		public decimal Litros => Volumen.EnLitros;
		public string VolumenExpresado => Volumen.Expresado;
		public decimal PrecioPorUnidad => Surtidor.PrecioPorUnidad.Monto;
		public decimal MontoDelTotal => Total.Monto;
		public string MonedaDelTotal => Total.Moneda.Codigo;
		public string TotalExpresado => Total.Expresado;
		public string NombreDelCorte => Corte.Nombre;
		public string TextoDeLaPlaca => Placa.Texto;
		public string NumeroDeLaTransaccion => Transaccion.Texto;
		public string KilometrajeExpresado => Odometro.Expresada;
		public string TanqueExpresado => Tanque.Expresada;
		public bool TieneKilometraje => Odometro.Existe;
		public bool TieneFoto => Foto.Existe;
	}
}
