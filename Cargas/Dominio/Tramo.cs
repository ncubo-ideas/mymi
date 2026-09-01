using System;

namespace Cargas.Dominio
{
	/// <summary>
	/// El intervalo entre dos tanques llenos: LA UNIDAD DE CALCULO, no la carga. Es una
	/// PROYECCION sobre la serie de cargas, no una entidad journalizada: un kilometraje
	/// que Sonia agrega tarde redefine tramos ya vistos, y en un journal append-only eso
	/// solo puede ser un recalculo, nunca una mutacion de algo guardado.
	/// </summary>
	// Un tramo BLOQUEADO no es la ausencia de tramo: es un tipo con contenido. Dice que
	// carga esta trunca y que dato falta, y de ahi sale la tarjeta SIN TRAMO y la
	// consulta que motiva el ingreso. Las proyecciones son planas y totales -ceros con
	// su bandera- porque asi cruzan el print de a un nivel, igual que los carriers de
	// examenes.
	internal sealed class Tramo
	{
		internal bool EstaCalculado { get; private set; }
		internal DateTime Desde { get; private set; }
		internal DateTime Hasta { get; private set; }

		/// <summary>Cero cuando esta bloqueado: falta justamente el kilometraje.</summary>
		internal decimal Kilometros { get; private set; }

		/// <summary>Los litros que se consumieron: la suma de las cargas POSTERIORES a la
		/// que abre, incluida la que cierra. Los de la que abre ya se contaron en el tramo
		/// anterior. Se sabe aunque el tramo este bloqueado.</summary>
		internal decimal Litros { get; private set; }

		internal decimal RendimientoEnKmPorLitro { get; private set; }

		/// <summary>Si se puede leer contra la linea base sin reservas: mismo instrumento
		/// y mismo combustible que el resto de la serie.</summary>
		internal bool EsComparable { get; private set; }

		/// <summary>"otro surtidor" u "otro combustible"; vacio si es comparable.</summary>
		internal string MotivoDeReserva { get; private set; }

		/// <summary>Rendimiento implausible sobre la linea base: probable carga sin
		/// registrar. Lo marca la serie despues de conocer la base.</summary>
		internal bool EsSospechoso { get; private set; }

		/// <summary>Que dato falta, cuando esta bloqueado: "el kilometraje de tu carga
		/// del 25/07/2026". Vacio si esta calculado.</summary>
		internal string QueFalta { get; private set; }

		/// <summary>El instrumento que midio los litros consumidos; vacio si los midieron
		/// varios. Con el la serie decide la comparabilidad.</summary>
		internal string InstrumentoDeLosLitros { get; private set; }

		/// <summary>El combustible que se quemo en el tramo -el de la carga que abre y
		/// las que aportan-; vacio si se mezclo mas de uno.</summary>
		internal string CombustibleDelTramo { get; private set; }

		private Rendimiento rendimiento;

		private Tramo()
		{
			MotivoDeReserva = "";
			QueFalta = "";
			InstrumentoDeLosLitros = "";
			CombustibleDelTramo = "";
		}

		internal static Tramo Calculado(DateTime desde, DateTime hasta, Distancia recorrida,
			Volumen consumido, string instrumentoDeLosLitros, string combustibleDelTramo)
		{
			var tramo = new Tramo
			{
				EstaCalculado = true,
				Desde = desde,
				Hasta = hasta,
				Kilometros = recorrida.EnKilometros,
				Litros = consumido.EnLitros,
				InstrumentoDeLosLitros = instrumentoDeLosLitros,
				CombustibleDelTramo = combustibleDelTramo,
			};
			tramo.rendimiento = Rendimiento.DeTramo(recorrida, consumido);
			tramo.RendimientoEnKmPorLitro = tramo.rendimiento.KilometrosPorLitro;
			tramo.EsComparable = true;
			return tramo;
		}

		internal static Tramo Bloqueado(DateTime desde, DateTime hasta, decimal litros,
			string queFalta) =>
			new Tramo
			{
				EstaCalculado = false,
				Desde = desde,
				Hasta = hasta,
				Litros = litros,
				QueFalta = queFalta,
			};

		/// <summary>El rendimiento como magnitud, para el calculo. Leerlo en un tramo
		/// bloqueado es un bug del llamador: consultar antes EstaCalculado.</summary>
		internal Rendimiento Rendimiento =>
			rendimiento ?? throw new ErrorDeDominio(
				"Un tramo bloqueado no tiene rendimiento: consultar antes EstaCalculado. "
				+ $"Lo que falta: {QueFalta}");

		// Las marcas las pone la serie, que es quien conoce la base y el instrumento de
		// referencia; el tramo solo las porta.
		internal void MarcarReserva(string motivo)
		{
			EsComparable = false;
			MotivoDeReserva = motivo;
		}

		internal void MarcarSospechoso()
		{
			EsSospechoso = true;
		}

		internal string Expresado => EstaCalculado
			? $"{Kilometros:0} km / {Litros:0.#} L = {RendimientoEnKmPorLitro:0.#} km/L"
			: $"bloqueado: falta {QueFalta}";
	}
}
