using System;
using System.Collections.Generic;

namespace Cargas.Dominio
{
	/// <summary>
	/// La lealtad de Sonia con la estacion: saldo y nivel. Presente para que el negocio
	/// se sienta real, NO es el foco. Los puntos y el nivel LLEGAN como eventos de la
	/// institucion, nunca se calculan aca (§11 del brief): Puntos no es Dinero, y la
	/// tasa de canje es una regla del programa que puede cambiar.
	/// </summary>
	internal sealed class ProgramaDeLealtad
	{
		internal Puntos Saldo { get; private set; }
		internal NivelDeLealtad Nivel { get; private set; }
		internal Membresia Membresia { get; private set; }

		internal ProgramaDeLealtad()
		{
			Saldo = new Puntos(0);
			Nivel = NivelDeLealtad.Base;
			Membresia = Membresia.SinAfiliar;
		}

		/// <summary>La membresia LLEGA, igual que los puntos. Volver a emitirla es legitimo
		/// -la estacion puede reimprimir un carnet- y simplemente reemplaza a la anterior:
		/// el numero vigente es el ultimo que ellos dijeron.</summary>
		internal void Afiliar(string numeroDeSocio, string nombre, DateTime desde) =>
			Membresia = Membresia.Emitida(numeroDeSocio, nombre, desde);

		internal void Acreditar(Puntos puntos)
		{
			ArgumentNullException.ThrowIfNull(puntos);
			Saldo = Saldo.Mas(puntos);
		}

		/// <summary>El nivel lo decide la institucion; aca solo se registra. Bajar de
		/// nivel tambien es posible: es su programa.</summary>
		internal void ActualizarNivel(NivelDeLealtad nivel)
		{
			ArgumentNullException.ThrowIfNull(nivel);
			Nivel = nivel;
		}

		// ------------------------------------------------------- las tombolas

		// CUELGAN DE ACA Y NO DE UN GLOBAL NUEVO DEL DSL, y no es una preferencia: un
		// segundo upgrade('...') NO asienta en la misma corrida que siembra el journal, y
		// la vista de SoloLectura del MCP -que hidrata con OnHydrated vacio- rehidrataria
		// sin encontrarlo. Es un bug ya vivido en examenes, y solo aparece abriendo el
		// journal desde CERO con el MCP. ProgramaDeLealtad ya se construye en la semilla,
		// asi que el replay los reconstruye sin migracion.

		private readonly Dictionary<string, Sorteo> porCodigo =
			new Dictionary<string, Sorteo>(StringComparer.OrdinalIgnoreCase);
		private readonly List<Sorteo> sorteos = new List<Sorteo>();

		/// <summary>Obtiene el sorteo o lo CREA sin detalles. Aca es donde un BoletoEmitido
		/// que llega antes que su anuncio deja de ser un problema: el sorteo existe con sus
		/// boletos, y el anuncio lo completa cuando llegue.</summary>
		private Sorteo ObtenerOCrear(string codigo)
		{
			string llave = new CodigoDeSorteo(codigo).Texto;
			if (porCodigo.TryGetValue(llave, out Sorteo existente)) return existente;

			var sorteo = new Sorteo(llave);
			porCodigo[llave] = sorteo;
			sorteos.Add(sorteo);
			return sorteo;
		}

		internal void AnunciarSorteo(string codigo, string nombre, string premio,
			DateTime fechaDelSorteo, decimal montoPorBoleto, string moneda) =>
			ObtenerOCrear(codigo).Anunciar(nombre, premio, fechaDelSorteo, montoPorBoleto, moneda);

		internal void EmitirBoleto(string numeroDeBoleto, string codigoDelSorteo,
			DateTime fecha, string numeroDeTransaccion) =>
			ObtenerOCrear(codigoDelSorteo).Emitir(
				new Boleto(numeroDeBoleto, codigoDelSorteo, fecha, numeroDeTransaccion));

		internal bool ConoceSorteo(string codigo) =>
			!string.IsNullOrWhiteSpace(codigo)
			&& porCodigo.ContainsKey(new CodigoDeSorteo(codigo).Texto);

		internal Sorteo SorteoDe(string codigo)
		{
			string llave = new CodigoDeSorteo(codigo).Texto;
			if (porCodigo.TryGetValue(llave, out Sorteo sorteo)) return sorteo;

			throw new ErrorDeDominio($"No hay un sorteo {llave}: consultar antes ConoceSorteo.");
		}

		internal IReadOnlyList<Sorteo> Sorteos => sorteos;
		internal int CantidadDeSorteos => sorteos.Count;

		internal int CantidadDeBoletos
		{
			get
			{
				int cuantos = 0;
				foreach (Sorteo sorteo in sorteos) cuantos += sorteo.CantidadDeBoletos;
				return cuantos;
			}
		}

		/// <summary>El sorteo al que le toca una carga de esa fecha: el PRIMERO ANUNCIADO
		/// cuya fecha de rifa no haya pasado todavia. Los sin anunciar quedan afuera porque
		/// no tienen ventana — otra vez la ausencia haciendo trabajo.
		///
		/// No hace falta un campo de ventana: la fecha del sorteo ya la define.</summary>
		internal bool HaySorteoQueCubra(DateTime fecha) => BuscarQueCubra(fecha) != null;

		internal Sorteo SorteoQueCubre(DateTime fecha) =>
			BuscarQueCubra(fecha) ?? throw new ErrorDeDominio(
				$"Ningun sorteo anunciado cubre el {fecha:dd/MM/yyyy}: consultar antes "
				+ "HaySorteoQueCubra.");

		private Sorteo BuscarQueCubra(DateTime fecha)
		{
			Sorteo elMasCercano = null;
			foreach (Sorteo sorteo in sorteos)
			{
				if (!sorteo.FueAnunciado) continue;
				if (sorteo.FechaDelSorteo.Date < fecha.Date) continue;

				if (elMasCercano == null || sorteo.FechaDelSorteo < elMasCercano.FechaDelSorteo)
					elMasCercano = sorteo;
			}
			return elMasCercano;
		}

		// ------------------------------- proyecciones planas, para el print del actor

		internal int PuntosDelSaldo => Saldo.Cantidad;
		internal string NombreDelNivel => Nivel.Nombre;

		// La membresia se aplana con la ausencia YA RESUELTA: el print del actor no puede
		// preguntar, asi que sin afiliacion salen vacios y la caratula lee eso como el
		// carnet que todavia no llego.
		internal bool EstaAfiliada => Membresia.EstaAfiliada;
		internal string NumeroDeSocio => Membresia.EstaAfiliada ? Membresia.Numero : "";
		internal string NombreDelSocio => Membresia.EstaAfiliada ? Membresia.Nombre : "";
		internal DateTime SocioDesde =>
			Membresia.EstaAfiliada ? Membresia.Desde : default;

		/// <summary>Cuantos faltan para el siguiente nivel. Cero en el ultimo.</summary>
		internal int PuntosParaElSiguienteNivel =>
			Nivel.EsElUltimo ? 0 : Saldo.LosQueFaltanPara(Nivel.ParaElSiguiente).Cantidad;
	}
}
