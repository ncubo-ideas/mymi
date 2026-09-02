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

		// ------------------------------------------------------ ruteo desde primitivo
		//
		// Un comando NUEVO no puede tener en la mano un objeto que creó un comando
		// ANTERIOR: cada perform es una invocación aparte y lo único que persiste son los
		// globales. Los gastos, los bienes y los imprevistos viven ADENTRO de este
		// agregado, así que la única forma de volver a tocarlos es nombrarlos.
		//
		// Ésa es la razón de fondo de "la fachada entra por primitivo": no es estética, es
		// que del otro lado del journal no hay referencias, hay texto.
		//
		// Los cuatro REVIENTAN cuando el nombre no resuelve. Un comando que no encuentra a
		// quién aplicarse no es un no-op: es un comando mal escrito, y tragárselo dejaría
		// una entrada en el journal que al reproducirse no hace nada y nadie sabe por qué.

		internal void QueTambienResponda(string nombreDelGasto, string textoDelBanco)
		{
			Buscar(nombreDelGasto).RespondeTambienA(textoDelBanco);
		}

		internal void MarcarComo(string nombreDelGasto, string reducibilidad)
		{
			GastoRecurrente gasto = Buscar(nombreDelGasto);

			if (!(gasto is GastoDeLaPersona marcable))
				throw new InvalidOperationException(
					$"'{nombreDelGasto}' es un {gasto.GetType().Name}: su reducibilidad la decide su clase, "
					+ "no la persona. Marcarla acá sería contradecir al dominio.");

			marcable.LaPersonaLoMarcoComo(Reducibilidad.PorNombre(reducibilidad));
		}

		internal void SeLeAtribuyeAlBien(string nombreDelBien, DateTime fecha, decimal monto,
			string descripcion)
		{
			BuscarBien(nombreDelBien).SeLeAtribuye(
				new Cargo(fecha, monto, descripcion, "", Procedencia.Persona));
		}

		internal void ElBienProduce(string nombreDelBien, DateTime fecha, decimal monto,
			string descripcion)
		{
			BuscarBien(nombreDelBien).Produce(
				new Cargo(fecha, monto, descripcion, "", Procedencia.Institucion));
		}

		internal void LoCausoTambien(string quePaso, DateTime fecha, decimal monto,
			string descripcion)
		{
			Imprevisto evento = imprevistos.Find(i => i.QuePaso == quePaso);
			if (evento == null)
				throw new ArgumentException(
					$"No hay ningún imprevisto llamado '{quePaso}'. Se registra antes de agregarle cargos.",
					nameof(quePaso));

			evento.LoCausoTambien(new Cargo(fecha, monto, descripcion, "", Procedencia.Persona));
		}

		private GastoRecurrente Buscar(string nombre)
		{
			GastoRecurrente gasto = gastos.Find(g => g.Nombre == nombre);
			if (gasto == null)
				throw new ArgumentException(
					$"No hay ningún gasto recurrente llamado '{nombre}'.", nameof(nombre));
			return gasto;
		}

		private Bien BuscarBien(string nombre)
		{
			Bien bien = bienes.Find(b => b.Nombre == nombre);
			if (bien == null)
				throw new ArgumentException(
					$"No hay ningún bien llamado '{nombre}'.", nameof(nombre));
			return bien;
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

		// LOS CANDIDATOS A IMPREVISTO. Juicio 8.
		//
		// Esto NO es una aproximacion: es la definicion. Se pregunto, y la respuesta fue que
		// LO NO PRESUPUESTADO ES EL IMPREVISTO — en esta app no hay un presupuesto declarado,
		// y no hace falta.
		//
		// La definicion no es circular porque las dos mitades las aporta gente distinta: los
		// CANDIDATOS se deducen -un cargo que ningun gasto recurrente conocido reclama- y el
		// EVENTO lo marca la persona, agrupando cargos. Asi que "presupuestado" acaba
		// significando "atribuible a un gasto recurrente conocido".
		//
		// CUIDADO CON LA PALABRA, porque es de las que alguien "arregla" en seis meses: en
		// esta app PRESUPUESTO SIGNIFICA EL TOTAL OBSERVADO, NO UN PLAN. El juicio 2 ya la usa
		// asi. Declarar un presupuesto -"quiero gastar maximo 200 mil en comida"- es otra app,
		// y ninguna de las cinco preguntas la pidio.
		internal IReadOnlyList<Cargo> CargosQueNadieReconocio(int ano, int mes)
		{
			var lista = new List<Cargo>();
			foreach (Cargo c in sueltos)
				if (c.EstaEn(ano, mes)) lista.Add(c);
			return lista;
		}
	}
}
