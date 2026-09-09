using System;
using System.Collections.Generic;
using System.Globalization;

namespace Compras.Dominio
{
	/// <summary>
	/// CUANTO Y CUANDO entro algo a la canasta, mes a mes. Es la serie, no el total: lo
	/// que permite decir que un consumo se disparo y DESDE QUE MES, que es lo que hace
	/// creible una relacion con cualquier otra cosa que le pase a la persona.
	/// </summary>
	// Es la contraparte de la serie de un analito del lado de las compras. Una pantalla
	// muestra tickets y totales por mes; esto mira la forma de la serie completa.
	//
	// Dice CUANTO se movio y DESDE QUE MES, nunca si eso esta bien: que el helado pase de
	// mensual a semanal es un dato, no una falta. Quien cruza esa serie con cualquier otra
	// cosa que le pase a la persona es el asistente, con permiso; el super no tiene con
	// que hacerlo.
	internal abstract class Consumo
	{
		/// <summary>Lo que no se encontro: nunca null.</summary>
		internal static Consumo Ninguno(string termino) => new SinConsumo(termino);

		internal static Consumo De(string termino, bool esCategoria, IReadOnlyList<MesDeConsumo> meses,
			DateTime ultimaVez, int comprasDelHistorial) =>
			new Serie(termino, esCategoria, meses, ultimaVez, comprasDelHistorial);

		internal string Termino { get; private set; }

		private Consumo(string termino)
		{
			ArgumentNullException.ThrowIfNull(termino);
			Termino = termino;
		}

		/// <summary>Consulta de presencia, total: si hay algo de esto en el historial.</summary>
		internal abstract bool Existe();

		/// <summary>Si el termino era una categoria del repertorio o un texto de producto.</summary>
		internal abstract bool EsCategoria { get; }

		internal abstract IReadOnlyList<MesDeConsumo> Meses { get; }

		/// <summary>
		/// La misma serie un escalón más arriba, un punto por año. Vacía cuando no hay
		/// consumo.
		/// </summary>
		// LA SERIE MENSUAL CRECE PARA SIEMPRE -doce filas por año- y la anual crece UNA. La
		// pantalla pinta el año mas reciente mes a mes y los anteriores de a una fila, asi
		// que pasa de crecer 12 por año a crecer 1. Las dos conviven a proposito: quien
		// pregunta por una fecha -una IA cruzando un resultado de laboratorio- necesita el
		// mes, y quien mira la pantalla necesita poder recorrerla.
		internal abstract IReadOnlyList<AnioDeConsumo> Anios { get; }

		internal abstract int MesesConConsumo { get; }
		internal abstract decimal TotalGastado { get; }
		internal abstract int TotalUnidades { get; }

		internal abstract string PrimerMesExpresado { get; }
		internal abstract string UltimoMesExpresado { get; }

		/// <summary>El mes de mayor gasto: donde el consumo pesa mas.</summary>
		internal abstract string MesDelPicoExpresado { get; }

		internal abstract decimal GastoEnElPrimerMes { get; }
		internal abstract decimal GastoEnElUltimoMes { get; }

		/// <summary>El HECHO, con signo: cuanto cambio el gasto entre el primer mes y el ultimo.</summary>
		internal abstract decimal CambioDeGasto { get; }

		/// <summary>Si hay serie con la que leer un cambio: hacen falta al menos dos meses.</summary>
		internal abstract bool HaySerie { get; }

		/// <summary>Si el gasto se movio lo bastante como para no ser variacion de despensa.</summary>
		internal abstract bool HayCambioReal { get; }

		/// <summary>La direccion del cambio, sin valorarla. Preguntar HayCambioReal primero.</summary>
		internal abstract bool Subio { get; }

		/// <summary>La linea, escrita por el dominio con SUS hechos. Nunca un consejo.</summary>
		internal abstract string ParaElAsistente { get; }

		// LO QUE LA FICHA DE UN PRODUCTO NECESITA, y que la serie por mes no contesta sola.
		//
		// EN CUANTAS COMPRAS APARECIO: se suma por mes, y sumarlo aca en vez de en cada
		// pantalla evita que dos superficies cuenten distinto -una contando lineas y otra
		// tickets- sobre el mismo producto.
		internal abstract int TotalCompras { get; }

		/// <summary>La última vez que lo llevó. Preguntar Existe primero.</summary>
		internal abstract DateTime UltimaVez { get; }

		/// <summary>La última vez, en aaaa-mm-dd. Vacío cuando no lo compró nunca.</summary>
		internal abstract string UltimaVezExpresada { get; }

		// QUE TAN FIEL ES, sobre TODO el historial y no sobre una ventana de ocho compras:
		// la ficha de un producto contesta "¿que tanto llevo esto?", y para eso la respuesta
		// honesta se mide contra todas sus compras. La firma de la portada usa una ventana
		// porque ahi la pregunta es otra -"que estoy llevando AHORA"-.
		internal abstract Constancia Constancia { get; }

		private sealed class SinConsumo : Consumo
		{
			internal SinConsumo(string termino) : base(termino) { }

			internal override bool Existe() => false;
			internal override bool EsCategoria => false;
			internal override IReadOnlyList<MesDeConsumo> Meses => Array.Empty<MesDeConsumo>();
			internal override IReadOnlyList<AnioDeConsumo> Anios => Array.Empty<AnioDeConsumo>();
			internal override int MesesConConsumo => 0;
			internal override decimal TotalGastado => 0m;
			internal override int TotalUnidades => 0;
			internal override int TotalCompras => 0;

			internal override DateTime UltimaVez =>
				throw new InvalidOperationException(
					$"'{Termino}' no aparece en ninguna compra, asi que no hay una ultima vez. "
					+ "Preguntar Existe antes.");

			internal override string UltimaVezExpresada => "";
			internal override Constancia Constancia => Constancia.DeVezEnCuando;

			internal override string PrimerMesExpresado => "";
			internal override string UltimoMesExpresado => "";
			internal override string MesDelPicoExpresado => "";
			internal override decimal GastoEnElPrimerMes => 0m;
			internal override decimal GastoEnElUltimoMes => 0m;
			internal override decimal CambioDeGasto => 0m;
			internal override bool HaySerie => false;
			internal override bool HayCambioReal => false;

			internal override bool Subio =>
				throw new InvalidOperationException(
					$"No hay compras de «{Termino}»: no hay direccion que dar. Preguntar HayCambioReal antes.");

			internal override string ParaElAsistente =>
				$"no hay compras de «{Termino}» en el historial";
		}

		private sealed class Serie : Consumo
		{
			// Cuanto tiene que moverse el gasto para llamarlo un cambio real. Un quinto
			// arriba o abajo es variacion de despensa; mas que eso es otro habito.
			private const decimal CambioQueCuentaComoReal = 0.20m;

			private readonly bool esCategoria;
			private readonly IReadOnlyList<MesDeConsumo> meses;
			private readonly DateTime ultimaVez;
			private readonly int comprasDelHistorial;

			internal Serie(string termino, bool esCategoria, IReadOnlyList<MesDeConsumo> meses,
				DateTime ultimaVez, int comprasDelHistorial)
				: base(termino)
			{
				ArgumentNullException.ThrowIfNull(meses);
				this.ultimaVez = ultimaVez;
				this.comprasDelHistorial = comprasDelHistorial;
				if (meses.Count == 0)
					throw new ArgumentException("Una serie sin meses no es una serie; eso es Consumo.Ninguno.",
						nameof(meses));

				this.esCategoria = esCategoria;
				this.meses = meses;
			}

			internal override bool Existe() => true;
			internal override bool EsCategoria => esCategoria;
			internal override IReadOnlyList<MesDeConsumo> Meses => meses;

			// SE ARMA AL LEERLA y no en el constructor, igual que los totales de esta clase:
			// la mayoria de las consultas no la pide, y recorrer veinte meses para armar dos
			// filas que nadie va a mirar se paga en cada consulta.
			//
			// LOS MESES YA VIENEN ORDENADOS -quien los arma los ordena por llave aaaamm-, asi
			// que basta con recorrerlos: el año cambia cuando cambia, y no hace falta ni
			// diccionario ni ordenar de nuevo.
			internal override IReadOnlyList<AnioDeConsumo> Anios
			{
				get
				{
					var anios = new List<AnioDeConsumo>();
					AnioDeConsumo actual = null;

					foreach (MesDeConsumo mes in meses)
					{
						if (actual is null || actual.Anio != mes.Anio)
						{
							actual = new AnioDeConsumo(mes.Anio);
							anios.Add(actual);
						}

						actual.Sumar(mes);
					}

					return anios;
				}
			}
			internal override int MesesConConsumo => meses.Count;

			internal override decimal TotalGastado
			{
				get
				{
					decimal total = 0m;
					foreach (MesDeConsumo mes in meses) total = total + mes.Gasto;
					return total;
				}
			}

			internal override int TotalUnidades
			{
				get
				{
					int total = 0;
					foreach (MesDeConsumo mes in meses) total = total + mes.Unidades;
					return total;
				}
			}

			internal override int TotalCompras
			{
				get
				{
					int cuantas = 0;
					foreach (MesDeConsumo mes in meses) cuantas += mes.Compras;
					return cuantas;
				}
			}

			internal override DateTime UltimaVez => ultimaVez;
			internal override string UltimaVezExpresada => ultimaVez.ToString("yyyy-MM-dd");

			// SOBRE TODAS SUS COMPRAS, no sobre una ventana. Si el historial viniera en cero
			// -imposible si hay meses, pero la division no se arriesga- cae en la constancia
			// mas baja, que es la respuesta honesta cuando no hay con que medir.
			internal override Constancia Constancia => comprasDelHistorial <= 0
				? Constancia.DeVezEnCuando
				: Constancia.Para(Math.Min(1m, (decimal)TotalCompras / comprasDelHistorial));

			internal override string PrimerMesExpresado => meses[0].MesExpresado;
			internal override string UltimoMesExpresado => meses[meses.Count - 1].MesExpresado;

			internal override string MesDelPicoExpresado
			{
				get
				{
					MesDeConsumo pico = meses[0];
					foreach (MesDeConsumo mes in meses) if (mes.Gasto > pico.Gasto) pico = mes;
					return pico.MesExpresado;
				}
			}

			internal override decimal GastoEnElPrimerMes => meses[0].Gasto;
			internal override decimal GastoEnElUltimoMes => meses[meses.Count - 1].Gasto;

			internal override decimal CambioDeGasto => GastoEnElUltimoMes - GastoEnElPrimerMes;

			// Un solo mes no es una serie: con una sola observacion no hay cambio que leer.
			internal override bool HaySerie => meses.Count >= 2;

			internal override bool HayCambioReal
			{
				get
				{
					if (!HaySerie) return false;

					decimal referencia = GastoEnElPrimerMes;
					if (referencia == 0m) return false;

					return Math.Abs(CambioDeGasto / referencia) >= CambioQueCuentaComoReal;
				}
			}

			internal override bool Subio
			{
				get
				{
					if (!HayCambioReal)
						throw new InvalidOperationException(
							$"El consumo de «{Termino}» no se movio lo suficiente como para darle una "
							+ "direccion. Preguntar HayCambioReal antes.");

					return CambioDeGasto > 0m;
				}
			}

			internal override string ParaElAsistente
			{
				get
				{
					string que = esCategoria ? Termino : $"«{Termino}»";

					if (meses.Count == 1)
						return $"{que} aparece una sola vez, en {PrimerMesExpresado}, por "
							+ $"{Colones(TotalGastado)}: todavía no hay serie para decir para dónde viene";

					string tramo = $"de {Colones(GastoEnElPrimerMes)} en {PrimerMesExpresado} a "
						+ $"{Colones(GastoEnElUltimoMes)} en {UltimoMesExpresado}";

					if (HayCambioReal)
						return $"{que} pasó {tramo}, sobre {MesesConConsumo} meses con compras "
							+ $"(el mes más alto fue {MesDelPicoExpresado})";

					return $"{que} se mantuvo {tramo}";
				}
			}

			private static string Colones(decimal monto) =>
				Plata.Colones(monto);
		}
	}
}
