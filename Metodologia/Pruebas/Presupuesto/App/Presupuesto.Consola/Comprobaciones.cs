using System;
using System.Collections.Generic;
using Presupuesto.Dominio;

namespace Presupuesto.Consola
{
	// LAS COMPROBACIONES DEL DOMINIO PURO. Sin actor, sin journal y sin disco.
	//
	// Cada una corresponde a una pregunta o a un juicio de Preguntas.md, y el nombre lo dice.
	// Cuando una falla, lo que se rompio es una regla del negocio, no una tuberia.
	internal static class Comprobaciones
	{
		private static int corridas, fallidas;

		internal static int Correr()
		{
			corridas = fallidas = 0;

			LaIdentidadJuntaLosTresNombresDelBanco();
			ElPagoDeLaTarjetaNoSeCuentaComoConsumo();
			ElTotalSeVigilaYSoloDespuesSeBuscanResponsables();
			ElServicioBasicoSabeQueSuVariacionEsClima();
			LaCuotaVencidaEsCriticaYElReciboNoLoEs();
			DeUnGastoSinHistoriaNoHayNadaQueEcharDeMenos();
			LoQueNadieMarcoNoCuentaComoNoRecortable();
			LaPropiedadSePagaSolaAunqueNoSepamosCuantoVale();
			LaTasaDeImprevistosSeCuentaSobreEventosNoSobreCargos();
			ConUnSoloImprevistoNoHayTasa();

			Console.WriteLine();
			Console.WriteLine(fallidas == 0
				? $"{corridas} comprobaciones, todas bien."
				: $"{corridas} comprobaciones, {fallidas} FALLARON.");

			return fallidas == 0 ? 0 : 1;
		}

		// ---------------------------------------------------------------- identidad

		private static void LaIdentidadJuntaLosTresNombresDelBanco()
		{
			var luz = new ServicioBasico("Electricidad", 0.15m);
			luz.RespondeTambienA("CNFL");
			luz.RespondeTambienA("CNFL S.A.");
			luz.RespondeTambienA("PAGO SERV ELECTRICO");

			var p = new Panorama(0.10m);
			p.Reconoce(luz);

			p.Entra(Banco("CNFL", 2026, 5, 20000));
			p.Entra(Banco("cnfl s.a.", 2026, 6, 21000));
			p.Entra(Banco("PAGO SERV ELECTRICO", 2026, 7, 22000));

			Verificar("Tres textos distintos del banco forman UNA serie de tres, no tres de uno",
				luz.Cargos.Count == 3);
			Verificar("Con dos o mas cargos hay serie", luz.HaySerie);
		}

		// ---------------------------------------------------------------- doble conteo

		private static void ElPagoDeLaTarjetaNoSeCuentaComoConsumo()
		{
			var tarjeta = new PagoDeTarjetaDeCredito("Visa");
			tarjeta.RespondeTambienA("PAGO TARJETA VISA");

			var super = new GastoDeLaPersona("Supermercado", Periodicidad.Mensual);
			super.RespondeTambienA("SUPERMERCADO LA COSECHA");

			var p = new Panorama(0.10m);
			p.Reconoce(tarjeta);
			p.Reconoce(super);

			p.Entra(Banco("SUPERMERCADO LA COSECHA", 2026, 5, 43500));
			p.Entra(Banco("PAGO TARJETA VISA", 2026, 5, 43500));

			Verificar("Pagar la tarjeta no suma al consumo: la compra ya estaba contada",
				p.TotalConsumidoEn(2026, 5) == 43500m);
		}

		// ---------------------------------------------------------------- pregunta 1

		private static void ElTotalSeVigilaYSoloDespuesSeBuscanResponsables()
		{
			var alquiler = new GastoDeLaPersona("Alquiler oficina", Periodicidad.Mensual);
			alquiler.RespondeTambienA("ALQUILER");

			var p = new Panorama(0.10m);
			p.Reconoce(alquiler);

			p.Entra(Banco("ALQUILER", 2026, 5, 300000));
			p.Entra(Banco("ALQUILER", 2026, 6, 305000));

			VarianciaDelMes quieto = p.VarianciaDe(2026, 6);
			Verificar("Un movimiento del 1,7% no exige atencion con umbral de 10%",
				!quieto.ExigeAtencion);
			Verificar("Si el total no se movio, no se buscan responsables",
				quieto.Responsables.Count == 0);

			var p2 = new Panorama(0.10m);
			var alquiler2 = new GastoDeLaPersona("Alquiler oficina", Periodicidad.Mensual);
			alquiler2.RespondeTambienA("ALQUILER");
			p2.Reconoce(alquiler2);
			p2.Entra(Banco("ALQUILER", 2026, 5, 300000));
			p2.Entra(Banco("ALQUILER", 2026, 6, 400000));

			VarianciaDelMes salto = p2.VarianciaDe(2026, 6);
			Verificar("Un movimiento del 33% si exige atencion", salto.ExigeAtencion);
			Verificar("Y ahi si aparece quien lo explica", salto.Responsables.Count == 1);
		}

		private static void ElServicioBasicoSabeQueSuVariacionEsClima()
		{
			var luz = new ServicioBasico("Electricidad", 0.15m);
			Verificar("Un 12% en el recibo electrico es su variacion estacional normal",
				luz.EsVariacionNormal(0.12m));
			Verificar("Un 40% ya no lo es", !luz.EsVariacionNormal(0.40m));

			// Y el que NO es un servicio basico no recibe esa indulgencia prestada: un 12% en
			// una cuota de prestamo no es el clima, es que cambio algo.
			var p = new Panorama(0.10m);
			var cuota = new CuotaDePrestamo("Carro", 60, 12);
			cuota.RespondeTambienA("CUOTA CARRO");
			p.Reconoce(cuota);
			p.Entra(Banco("CUOTA CARRO", 2026, 5, 150000));
			p.Entra(Banco("CUOTA CARRO", 2026, 6, 200000));
			VarianciaDelMes v = p.VarianciaDe(2026, 6);
			Verificar("Un salto en una cuota de prestamo NO se excusa como variacion estacional",
				!v.Responsables[0].EsVariacionNormalDeSuClase);
		}

		// ---------------------------------------------------------------- pregunta 4

		private static void LaCuotaVencidaEsCriticaYElReciboNoLoEs()
		{
			var cuota = new CuotaDePrestamo("Carro", 60, 12);
			cuota.RespondeTambienA("CUOTA CARRO");
			var luz = new ServicioBasico("Electricidad", 0.15m);
			luz.RespondeTambienA("CNFL");

			var p = new Panorama(0.10m);
			p.Reconoce(cuota);
			p.Reconoce(luz);
			p.Entra(Banco("CUOTA CARRO", 2026, 5, 150000));
			p.Entra(Banco("CNFL", 2026, 5, 20000));

			// 5 de julio: los dos se esperaban el 5 de junio, o sea 30 dias de atraso.
			IReadOnlyList<CargoAusente> ausentes = p.QueNoMeCobraron(new DateTime(2026, 7, 5));

			Verificar("Lo mas grave primero: la cuota antes que el recibo",
				ausentes[0].Gasto.Nombre == "Carro");
			Verificar("Una cuota que no salio es CRITICA, y no por el monto",
				ausentes[0].Severidad == Severidad.Critico);
			Verificar("Un recibo que no llego es seguimiento, no mora",
				ausentes[1].Severidad == Severidad.Seguimiento);
			Verificar("La accion sale de la severidad, no la escribe cada gasto",
				ausentes[0].QueHacer == Severidad.Critico.QueHacer);
		}

		private static void DeUnGastoSinHistoriaNoHayNadaQueEcharDeMenos()
		{
			var nuevo = new GastoDeLaPersona("Gimnasio", Periodicidad.Mensual);
			CargoAusente a = nuevo.EvaluarAusencia(new DateTime(2026, 7, 5));

			Verificar("Sin historia no hay ausencia: es distinto de estar al dia",
				!a.HayHistoria && !a.EstaVencido);
		}

		// ---------------------------------------------------------------- pregunta 3

		private static void LoQueNadieMarcoNoCuentaComoNoRecortable()
		{
			var cine = new GastoDeLaPersona("Cine", Periodicidad.Mensual);
			cine.RespondeTambienA("CINEPOLIS");
			cine.LaPersonaLoMarcoComo(Reducibilidad.Discrecional);

			var cuota = new CuotaDePrestamo("Carro", 60, 12);
			cuota.RespondeTambienA("CUOTA CARRO");

			var raro = new GastoDeLaPersona("Algo sin marcar", Periodicidad.Mensual);
			raro.RespondeTambienA("MISTERIO");

			var p = new Panorama(0.10m);
			p.Reconoce(cine); p.Reconoce(cuota); p.Reconoce(raro);
			p.Entra(Banco("CINEPOLIS", 2026, 6, 20000));
			p.Entra(Banco("CUOTA CARRO", 2026, 6, 150000));
			p.Entra(Banco("MISTERIO", 2026, 6, 80000));

			MargenParaRecortar m = p.MargenPara(50000m, 2026, 6);

			Verificar("Lo discrecional se cuenta aparte", m.Discrecional == 20000m);
			Verificar("El compromiso se cuenta aparte", m.Comprometido == 150000m);
			Verificar("Lo que nadie marco NO se cuenta como no recortable: se cuenta como desconocido",
				m.PorDefinir == 80000m);
			Verificar("Con 20 mil recortables no alcanza para una mensualidad de 50 mil",
				!m.AlcanzaConLoConocido);
			Verificar("Y la respuesta se declara INCOMPLETA, porque hay plata sin marcar que podria cambiarla",
				!m.LaRespuestaEstaCompleta);
		}

		// ---------------------------------------------------------------- pregunta 2

		private static void LaPropiedadSePagaSolaAunqueNoSepamosCuantoVale()
		{
			var casa = new Bien("Apartamento en alquiler", valorEstimado: 0m);
			casa.Produce(Banco("ALQUILER RECIBIDO", 2026, 1, 400000));
			casa.Produce(Banco("ALQUILER RECIBIDO", 2026, 2, 400000));
			casa.SeLeAtribuye(Banco("FERRETERIA", 2026, 1, 150000));

			var p = new Panorama(0.10m);
			p.Tiene(casa);

			Rendimiento r = p.RendimientoDe("Apartamento en alquiler",
				new DateTime(2026, 1, 1), new DateTime(2026, 2, 28), new ContraSuPropiaHistoria());

			Verificar("El neto se contesta sin saber cuanto vale el bien", r.Neto == 650000m);
			Verificar("Y 'se paga sola' tambien", r.SePagaSola);
			Verificar("Pero SIN valor no hay porcentaje, y se dice en vez de inventarlo",
				!r.PorcentajeAnual.HasValue && !r.SePuedeComparar);

			var casa2 = new Bien("Apartamento", valorEstimado: 50000000m);
			casa2.Produce(Banco("ALQUILER", 2026, 1, 400000));
			var p2 = new Panorama(0.10m);
			p2.Tiene(casa2);
			Rendimiento r2 = p2.RendimientoDe("Apartamento",
				new DateTime(2026, 1, 1), new DateTime(2026, 12, 31),
				new ContraUnaTasaAnual("un certificado de deposito", 0.07m));

			Verificar("Con valor si hay porcentaje", r2.PorcentajeAnual.HasValue);
			Verificar("Y recien ahi se puede comparar contra la alternativa que la persona declaro",
				r2.SePuedeComparar);
		}

		// ---------------------------------------------------------------- pregunta 5

		private static void LaTasaDeImprevistosSeCuentaSobreEventosNoSobreCargos()
		{
			var hospital = new Imprevisto("Hospitalizacion", new DateTime(2026, 3, 10));
			hospital.LoCausoTambien(Banco("HOSPITAL", 2026, 3, 500000));
			hospital.LoCausoTambien(Banco("FARMACIA", 2026, 3, 80000));
			hospital.LoCausoTambien(Banco("LABORATORIO", 2026, 3, 45000));

			var refri = new Imprevisto("Se quemo la refrigeradora", new DateTime(2026, 9, 2));
			refri.LoCausoTambien(Banco("ELECTRODOMESTICOS", 2026, 9, 275000));

			var p = new Panorama(0.10m);
			p.Ocurrio(hospital);
			p.Ocurrio(refri);

			TasaDeImprevistos t = p.CuantoApartarPorMes(
				new DateTime(2026, 1, 1), new DateTime(2026, 12, 31));

			Verificar("Cuatro cargos, DOS imprevistos: la tasa se cuenta sobre eventos",
				t.Cuantos == 2);
			Verificar("El total suma los cuatro cargos", t.Total == 900000m);
			Verificar("Con dos eventos ya hay tasa", t.HayTasa);
			Verificar("Y se puede decir cada cuantos meses pasa uno", t.CadaCuantosMeses > 5m);
		}

		private static void ConUnSoloImprevistoNoHayTasa()
		{
			var uno = new Imprevisto("Se quemo la refrigeradora", new DateTime(2026, 9, 2));
			uno.LoCausoTambien(Banco("ELECTRODOMESTICOS", 2026, 9, 275000));

			var p = new Panorama(0.10m);
			p.Ocurrio(uno);

			TasaDeImprevistos t = p.CuantoApartarPorMes(
				new DateTime(2026, 1, 1), new DateTime(2026, 12, 31));

			Verificar("Con uno solo no hay tasa: hay una anecdota", !t.HayTasa);
			Verificar("Y no se inventa un promedio", t.PorMes == 0m);
		}

		// ---------------------------------------------------------------- utilitarios

		private static Cargo Banco(string texto, int ano, int mes, decimal monto) =>
			new Cargo(new DateTime(ano, mes, 5), monto, texto, "tarjeta", Procedencia.Institucion);

		private static void Verificar(string queSeEspera, bool paso)
		{
			corridas++;
			if (!paso) fallidas++;
			Console.WriteLine($"  {(paso ? "ok  " : "FALLA")}  {queSeEspera}");
		}
	}
}
