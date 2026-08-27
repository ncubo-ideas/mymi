using System;
using System.Collections.Generic;

namespace Presupuesto.Dominio
{
	// LA RAIZ: todo el panorama de plata de la persona. Es tambien la fachada a la que se ata el perform: recibe primitivos y devuelve
	// escalares o proyecciones ya armadas, para que ninguna caratula tenga que conocer un
	// tipo del dominio.
	//
	// Construido UNICAMENTE con Preguntas.md en la mano, sin volver a preguntarle nada a
	// nadie. Los HUECOS que aparecieron estan marcados en cada clase y listados en HUECOS.md.
	//
	// La fecha SIEMPRE entra por parametro. Si el dominio leyera el reloj, el mismo journal
	// reproducido manana daria otra respuesta.
	internal sealed class Panorama
	{
		private readonly List<GastoRecurrente> gastos = new List<GastoRecurrente>();
		private readonly List<Cargo> sueltos = new List<Cargo>();
		private readonly List<Bien> bienes = new List<Bien>();
		private readonly List<Imprevisto> imprevistos = new List<Imprevisto>();
		private readonly decimal umbralDeVariacion;

		// El 10% del juicio 2. Entra al construir y no vive quemado: es del negocio, no del
		// codigo.
		internal Panorama(decimal umbralDeVariacion)
		{
			if (umbralDeVariacion <= 0 || umbralDeVariacion >= 1)
				throw new ArgumentOutOfRangeException(nameof(umbralDeVariacion),
					"Es una proporcion: 0,10 son diez por ciento.");

			this.umbralDeVariacion = umbralDeVariacion;
		}

		// ------------------------------------------------------------ lo que entra

		internal void Reconoce(GastoRecurrente gasto)
		{
			if (gasto == null) throw new ArgumentNullException(nameof(gasto));
			gastos.Add(gasto);
		}

		internal void Tiene(Bien bien)
		{
			if (bien == null) throw new ArgumentNullException(nameof(bien));
			bienes.Add(bien);
		}

		internal void Ocurrio(Imprevisto imprevisto)
		{
			if (imprevisto == null) throw new ArgumentNullException(nameof(imprevisto));
			imprevistos.Add(imprevisto);
		}

		// UN CARGO DEL BANCO. Se rutea al gasto recurrente que lo reconozca; si ninguno lo
		// reconoce, queda suelto. Quedar suelto NO es un error: es el estado honesto de un
		// cargo que la persona todavia no marco, y es lo que alimenta los candidatos a
		// imprevisto del juicio 8.
		internal bool Entra(Cargo cargo)
		{
			if (cargo == null) throw new ArgumentNullException(nameof(cargo));

			foreach (GastoRecurrente g in gastos)
				if (g.Reconoce(cargo.DescripcionDelBanco))
				{
					g.Registrar(cargo);
					return true;
				}

			sueltos.Add(cargo);
			return false;
		}

		// ------------------------------------------------------------ pregunta 1

		// El total se movio? Y si se movio, quienes lo explican.
		//
		// HUECO 2 - lo que NO es mensual entra entero en su mes. Un seguro anual dispara el
		// umbral en marzo sin que haya pasado nada. Preguntas.md no decidio si se prorratea.
		// Aca NO se prorratea, que es lo que el archivo literalmente dice -"el presupuesto
		// mensual"- y deja el falso positivo a la vista en vez de taparlo con una decision
		// que nadie tomo.
		internal VarianciaDelMes VarianciaDe(int ano, int mes)
		{
			DateTime anterior = new DateTime(ano, mes, 1).AddMonths(-1);

			decimal total = TotalConsumidoEn(ano, mes);
			decimal previo = TotalConsumidoEn(anterior.Year, anterior.Month);

			var variancia = new VarianciaDelMes(ano, mes, total, previo, umbralDeVariacion);

			if (!variancia.ExigeAtencion) return variancia;

			foreach (GastoRecurrente g in gastos)
			{
				if (!g.EsConsumo) continue;

				decimal ahora = SumarDe(g, ano, mes);
				decimal antes = SumarDe(g, anterior.Year, anterior.Month);
				if (ahora != antes) variancia.Culpa(g, antes, ahora);
			}

			return variancia;
		}

		// EsConsumo es la guarda que evita contar dos veces cada compra con tarjeta.
		internal decimal TotalConsumidoEn(int ano, int mes)
		{
			decimal total = 0m;

			foreach (GastoRecurrente g in gastos)
				if (g.EsConsumo) total += SumarDe(g, ano, mes);

			foreach (Cargo c in sueltos)
				if (c.EstaEn(ano, mes)) total += c.Monto;

			return total;
		}

		private static decimal SumarDe(GastoRecurrente gasto, int ano, int mes)
		{
			decimal total = 0m;
			foreach (Cargo c in gasto.Cargos)
				if (c.EstaEn(ano, mes)) total += c.Monto;
			return total;
		}

		// ------------------------------------------------------------ pregunta 2

		internal Rendimiento RendimientoDe(string nombreDelBien, DateTime desde, DateTime hasta,
			PuntoDeComparacion contra)
		{
			Bien bien = bienes.Find(b => b.Nombre == nombreDelBien);
			if (bien == null)
				throw new ArgumentException($"No hay ningun bien llamado '{nombreDelBien}'.",
					nameof(nombreDelBien));

			return Rendimiento.De(bien, desde, hasta, contra ?? new ContraSuPropiaHistoria());
		}

		// ------------------------------------------------------------ pregunta 3

		// "Si tomo este prestamo con esta mensualidad, que gastos puedo reducir?"
		//
		// Es la unica pregunta HACIA ADELANTE y CONTRAFACTICA de las cinco, y la doctrina
		// actual no la contempla: esta construida sobre leer la serie del pasado.
		//
		// HUECO 8 - Preguntas.md dice que un gasto es o no reducible, pero NO CUANTO se puede
		// reducir. .Se recorta el 100% del streaming? .El 30% de los restaurantes? Sin eso,
		// esto NO puede contestar "si te alcanza": solo puede poner sobre la mesa cuanto hay
		// en cada estado. Contestar mas seria inventar el juicio que falta.
		internal MargenParaRecortar MargenPara(decimal mensualidadNueva, int ano, int mes)
		{
			decimal discrecional = 0m, comprometido = 0m, porDefinir = 0m;
			var sinMarcar = new List<GastoRecurrente>();

			foreach (GastoRecurrente g in gastos)
			{
				if (!g.EsConsumo) continue;

				decimal monto = SumarDe(g, ano, mes);
				if (g.Reducibilidad == Reducibilidad.Discrecional) discrecional += monto;
				else if (g.Reducibilidad == Reducibilidad.Comprometido) comprometido += monto;
				else { porDefinir += monto; sinMarcar.Add(g); }
			}

			return new MargenParaRecortar(mensualidadNueva, discrecional, comprometido,
				porDefinir, sinMarcar);
		}

		// ------------------------------------------------------------ pregunta 4

		// La ausencia. Lo mas vencido primero; lo que esta al dia igual aparece, para poder
		// decirlo.
		internal IReadOnlyList<CargoAusente> QueNoMeCobraron(DateTime hoy)
		{
			var lista = new List<CargoAusente>();
			foreach (GastoRecurrente g in gastos) lista.Add(g.EvaluarAusencia(hoy));

			lista.Sort((a, b) =>
			{
				int porSeveridad = b.Severidad.Orden.CompareTo(a.Severidad.Orden);
				return porSeveridad != 0 ? porSeveridad : b.DiasDeAtraso.CompareTo(a.DiasDeAtraso);
			});

			return lista;
		}

		// ------------------------------------------------------------ pregunta 5

		// Cuanto apartar por mes. LA TASA SE CALCULA SOBRE EVENTOS, no sobre cargos: es lo
		// unico que hace util la respuesta.
		internal TasaDeImprevistos CuantoApartarPorMes(DateTime desde, DateTime hasta)
		{
			decimal total = 0m;
			int cuantos = 0;

			foreach (Imprevisto i in imprevistos)
				if (i.OcurrioEntre(desde, hasta)) { total += i.Total; cuantos++; }

			decimal meses = (decimal)((hasta.Date - desde.Date).TotalDays / 30.44);
			return new TasaDeImprevistos(cuantos, total, meses);
		}

		// ------------------------------------------------------------ lo que quedo sin marcar

		// Los candidatos a imprevisto del juicio 8: cargos que ningun gasto recurrente
		// reconocio. "No estaba presupuestado" da los candidatos; la marca de la persona los
		// agrupa en un evento.
		//
		// HUECO 7 - Y aca esta el hueco mas grande de todos. Preguntas.md usa "presupuestado"
		// como si estuviera definido y NUNCA DICE COMO SE FIJA UN PRESUPUESTO: quien lo pone,
		// por categoria o total, por mes o por ano. Sin eso, "lo no presupuestado" solo puede
		// aproximarse por "lo que no reconocio ningun gasto recurrente", que es otra cosa.
		// La app se llama Presupuesto y el archivo no define el presupuesto.
		internal IReadOnlyList<Cargo> CargosQueNadieReconocio(int ano, int mes)
		{
			var lista = new List<Cargo>();
			foreach (Cargo c in sueltos)
				if (c.EstaEn(ano, mes)) lista.Add(c);
			return lista;
		}
	}
}
