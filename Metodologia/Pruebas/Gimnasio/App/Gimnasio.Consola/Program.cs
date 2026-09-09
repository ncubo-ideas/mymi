using System;
using System.Collections.Generic;
using Gimnasio.Dominio;

namespace Gimnasio.Consola
{
	// EL ARNES DEL DOMINIO PURO. Sin actor, sin journal, sin disco.
	//
	// Cada comprobacion nombra la regla del Preguntas.md que fija, para que cuando se
	// rompa se sepa que decision se violo y no solo que fallo un assert.
	internal static class Program
	{
		private static int fallas;

		private static int Main(string[] args)
		{
			string comando = args.Length > 0 ? args[0] : "comprobar";
			if (comando == "medir")
				return Medir.Correr(args.Length > 1
					? args[1]
					: System.IO.Path.Combine(System.IO.Path.GetTempPath(), "gimnasio"));
			if (comando != "comprobar")
			{
				Console.WriteLine("Comandos:  comprobar  |  medir <carpeta>");
				return 1;
			}

			Console.WriteLine("El dominio del gimnasio, sin actor y sin disco:");
			Console.WriteLine();

			ElPerfilEsObligatorio();
			LaDireccionLaDaElObjetivo();
			LaMasaMuscularRompeLaRegla();
			LaLecturaYLaTendenciaSonDos();
			CadaMedidaSabeSuReferencia();
			ElRitmoEsDelPlan();
			LaUrgenciaTieneSigno();
			ElIncumplimiento();
			LaMembresiaYLaMora();
			LaProgresionDeLoIndicado();
			CuantoFalta();
			LaAusencia();
			LaEvaluacionDelAparatoEsHecho();
			LasDosFormasDePrescribir();
			ElCobroCubreUnPeriodo();
			ElProductoContratado();

			Console.WriteLine();
			Console.WriteLine(fallas == 0
				? "Todas las comprobaciones, bien."
				: fallas + " FALLARON.");
			return fallas == 0 ? 0 : 1;
		}

		// ------------------------------------------------------------------ el perfil

		private static void ElPerfilEsObligatorio()
		{
			Titulo("El perfil es entrada obligatoria");

			Trayectoria t = new Trayectoria();
			t.Agregar(MedicionCon(new DateTime(2026, 1, 10), new Cintura(88m)));

			Revienta("Sin perfil no se puede leer ningun numero",
				() => t.ComoEstoy("cintura", Referencias()));

			t.DeclararPerfil(PerfilDeSonia());
			Ok("Con perfil, la lectura sale", t.ComoEstoy("cintura", Referencias()) != null);
		}

		// ------------------------------------------------------- juicio 1 y su excepcion

		private static void LaDireccionLaDaElObjetivo()
		{
			Titulo("La direccion deseable la da el objetivo, no la medida");

			Objetivo bajar = new Objetivo();
			bajar.Agregar(Meta.SoloDireccion("peso", Direccion.Bajar()));
			Objetivo subir = new Objetivo();
			subir.Agregar(Meta.SoloDireccion("peso", Direccion.Subir()));

			Medida peso = new Peso(70m);
			Ok("El mismo peso quiere bajar para una persona...", peso.DireccionDeseable(bajar).EsBajar());
			Ok("...y subir para otra", peso.DireccionDeseable(subir).EsSubir());

			Objetivo vacio = new Objetivo();
			Revienta("Sin meta declarada no se inventa una direccion",
				() => peso.DireccionDeseable(vacio));
		}

		private static void LaMasaMuscularRompeLaRegla()
		{
			Titulo("La masa muscular es la excepcion: sube y punto");

			Objetivo bajar = new Objetivo();
			bajar.Agregar(Meta.SoloDireccion("masa-muscular", Direccion.Bajar()));

			Medida masa = new MasaMuscular(30m);
			Ok("Aunque el objetivo diga bajar, la direccion deseable es subir",
				masa.DireccionDeseable(bajar).EsSubir());

			Objetivo vacio = new Objetivo();
			Ok("Y ni siquiera necesita objetivo para saberlo",
				masa.DireccionDeseable(vacio).EsSubir());
		}

		// ------------------------------------------------------------- los dos juicios

		private static void LaLecturaYLaTendenciaSonDos()
		{
			Titulo("La lectura y la tendencia son dos, y no se deducen entre si");

			Perfil sonia = PerfilDeSonia();
			// Rango de grasa: bajo por debajo de 21, alto por encima de 33.
			Referencia grasa = new Referencia(21m, 33m, 1m, 5m);

			Objetivo bajarGrasa = new Objetivo();
			bajarGrasa.Agregar(Meta.SoloDireccion("grasa", Direccion.Bajar()));

			Trayectoria t = new Trayectoria();
			t.DeclararPerfil(sonia);
			t.DeclararObjetivo(bajarGrasa);
			t.Agregar(MedicionCon(new DateTime(2026, 1, 10), new PorcentajeDeGrasa(38m)));
			t.Agregar(MedicionCon(new DateTime(2026, 4, 10), new PorcentajeDeGrasa(35m)));

			Ok("El valor esta ALTO...", t.ComoEstoy("grasa", grasa).EsAlto());
			Ok("...y a la vez MEJORA", t.ComoVengo("grasa", grasa).Mejora());
			Ok("Y avisa en positivo", t.ComoVengo("grasa", grasa).Urgencia().EsPositiva());

			// El caso espejo: normal y empeorando.
			Trayectoria u = new Trayectoria();
			u.DeclararPerfil(sonia);
			u.DeclararObjetivo(bajarGrasa);
			u.Agregar(MedicionCon(new DateTime(2026, 1, 10), new PorcentajeDeGrasa(24m)));
			u.Agregar(MedicionCon(new DateTime(2026, 4, 10), new PorcentajeDeGrasa(28m)));

			Ok("Otro valor esta NORMAL...", u.ComoEstoy("grasa", grasa).EsNormal());
			Ok("...y sin embargo EMPEORA", u.ComoVengo("grasa", grasa).Empeora());
		}

		// ------------------------------------------------------------------ juicio 7

		private static void CadaMedidaSabeSuReferencia()
		{
			Titulo("Cada medida sabe con que llave se busca su referencia");

			Perfil sonia = PerfilDeSonia();

			// HUECO 19, CERRADO POR JORGE: el IMC lo manda el gimnasio; el peso ya no lo calcula.
			Ok("El peso ya no se lee contra un rango propio: el IMC llega del gimnasio", new Peso(70m).ElAparatoLaEvalua);
			Revienta("Y pedirle su clave de referencia revienta", () => new Peso(70m).ClaveDeReferencia(sonia));
			Ok("La grasa depende de genero y edad",
				new PorcentajeDeGrasa(30m).ClaveDeReferencia(sonia) == "grasa:femenino:42");
			Ok("La cintura depende solo del genero",
				new Cintura(88m).ClaveDeReferencia(sonia) == "cintura:femenino");
			Ok("La masa muscular no depende de nada del perfil",
				new MasaMuscular(30m).ClaveDeReferencia(sonia) == "masa-muscular");
		}

		// ------------------------------------------------------------------ juicio 3

		private static void ElRitmoEsDelPlan()
		{
			Titulo("El ritmo es del plan, no del calendario");

			Plan p = new Plan(new DateTime(2026, 1, 5), 3);
			Revienta("Un plan que no termino no duro nada todavia", () => p.SemanasQueDuro());

			p.Terminar(new DateTime(2026, 4, 6));
			Ok("Terminado, dice cuantas semanas duro", p.SemanasQueDuro() == 13);
			Ok("Y cuantos dias esperaba en total", p.DiasEsperadosEnTodoElPlan() == 39);

			Revienta("No se puede terminar dos veces", () => p.Terminar(new DateTime(2026, 5, 1)));
		}

		// ------------------------------------------------------------------ juicio 6

		private static void LaUrgenciaTieneSigno()
		{
			Titulo("La urgencia tiene signo, y existe la positiva");

			// Cintura: ruido por debajo de 1 cm, no creible por encima de 5.
			Referencia cintura = new Referencia(0m, 88m, 1m, 5m);
			Direccion bajar = Direccion.Bajar();

			Tendencia bajo = Tendencia.Entre(new Cintura(90m), new Cintura(88m), bajar, cintura);
			Ok("Dos centimetros menos avisan en positivo", bajo.Urgencia().EsPositiva());

			Tendencia subio = Tendencia.Entre(new Cintura(88m), new Cintura(90m), bajar, cintura);
			Ok("Dos centimetros mas avisan en negativo", subio.Urgencia().EsNegativa());

			Tendencia nada = Tendencia.Entre(new Cintura(88m), new Cintura(88.5m), bajar, cintura);
			Ok("Medio centimetro es ruido y no avisa", !nada.Urgencia().Avisa());

			Tendencia raro = Tendencia.Entre(new Cintura(88m), new Cintura(98m), bajar, cintura);
			Ok("Diez centimetros no son creibles", !raro.EsCreible());
			Ok("Y lo no creible NO avisa: no se sabe si paso", !raro.Urgencia().Avisa());
		}

		// ------------------------------------------------------------ el incumplimiento

		private static void ElIncumplimiento()
		{
			Titulo("El incumplimiento sale del plan y de la asistencia, ninguna es de Sonia");

			Plan p = new Plan(new DateTime(2026, 1, 5), 3);
			p.Terminar(new DateTime(2026, 2, 2));   // 4 semanas -> 12 dias esperados

			Trayectoria t = new Trayectoria();
			t.Agregar(p);
			for (int i = 0; i < 9; i++) t.Asistio(new DateTime(2026, 1, 5).AddDays(i * 2));

			Incumplimiento inc = t.IncumplimientoDe(p);
			Ok("Esperaba 12 dias", inc.DiasEsperados == 12);
			Ok("Fue 9", inc.DiasQueFue == 9);
			Ok("Le faltaron 3", inc.DiasQueFaltaron == 3);
			Ok("Y no cumplio", !inc.Cumplio);

			Incumplimiento demas = new Incumplimiento(12, 15);
			Ok("Ir de mas no da incumplimiento negativo", demas.DiasQueFaltaron == 0);

			Plan corriendo = new Plan(new DateTime(2026, 3, 1), 3);
			Revienta("Un plan sin terminar no se puede juzgar", () => t.IncumplimientoDe(corriendo));
		}

		// --------------------------------------------- la membresia y la mora (v2, N1 y N2)

		private static void LaMembresiaYLaMora()
		{
			Titulo("La membresia: la mora la dice el gimnasio, y el plan no la espera");

			// El plan del v1: 5-ene a 6-abr, 13 semanas, 3 dias por semana = 39 esperados.
			Plan p = new Plan(new DateTime(2026, 1, 5), 3);
			p.Terminar(new DateTime(2026, 4, 6));

			// SIN BANDERA NO SE SABE, Y SE DICE. Ni activo ni en mora por defecto.
			Trayectoria sinBandera = new Trayectoria();
			sinBandera.Agregar(p);
			Ok("Una membresia recien nacida no tiene estado declarado", !sinBandera.Membresia.TieneEstadoDeclarado);
			Revienta("Preguntar si esta en mora sin bandera revienta en vez de inventar 'activo'",
				() => sinBandera.Membresia.EstaInactiva());
			Ok("Pero el descuento es total: sin mora registrada se descuenta cero",
				sinBandera.IncumplimientoDe(p).DiasDescontadosPorInactividad == 0
				&& sinBandera.IncumplimientoDe(p).DiasEsperados == 39);

			// EL HUECO 9, CERRADO: dos semanas en mora dentro del plan esperan dos semanas menos.
			Trayectoria t = new Trayectoria();
			t.Agregar(p);
			t.Membresia.CambiarEstado(EstadoDeMembresia.Activo(), new DateTime(2026, 1, 5));
			t.Membresia.CambiarEstado(EstadoDeMembresia.NoActiva(), new DateTime(2026, 2, 2));
			t.Membresia.CambiarEstado(EstadoDeMembresia.Activo(), new DateTime(2026, 2, 16));

			Ok("Del 2 al 16 de febrero son 15 dias en mora", t.Membresia.DiasInactivaEntre(p.Inicio, p.Fin) == 15);
			Incumplimiento inc = t.IncumplimientoDe(p);
			Ok("Dos semanas en mora: el plan espera 33 dias, no 39", inc.DiasEsperados == 33);
			Ok("Y dice cuantos desconto: 6", inc.DiasDescontadosPorInactividad == 6);
			Ok("Hoy esta activa", !t.Membresia.EstaInactiva());
			Ok("Y el 10 de febrero estaba en mora", t.Membresia.EstabaInactivaEl(new DateTime(2026, 2, 10)));
			Ok("El periodo derivado es uno, y cerrado", t.Membresia.PeriodosInactivos().Count == 1
				&& t.Membresia.PeriodosInactivos()[0].TieneFin);

			// LA MISMA BANDERA DOS VECES ES EL MISMO HECHO OTRA VEZ (regla 18): se rechaza.
			Ok("Volver a decir 'activo' estando activa no entra",
				!t.PuedeCambiarEstadoDeMembresia("activo", new DateTime(2026, 3, 1)));
			Revienta("Y forzarlo revienta",
				() => t.Membresia.CambiarEstado(EstadoDeMembresia.Activo(), new DateTime(2026, 3, 1)));
			Ok("La palabra del v2, 'al-dia', ya no existe: la del papel es 'activo' (N9)",
				!EstadoDeMembresia.EsUnNombreConocido("al-dia"));
			Ok("Un cambio con fecha anterior al ultimo tampoco entra",
				!t.PuedeCambiarEstadoDeMembresia("no-activa", new DateTime(2026, 1, 20)));
			Ok("Un estado que no existe tampoco",
				!t.PuedeCambiarEstadoDeMembresia("suspendida", new DateTime(2026, 3, 1)));

			// UN PERIODO ABIERTO se descuenta hasta el fin del plan, y su Hasta esta guardado.
			Trayectoria u = new Trayectoria();
			u.Agregar(p);
			u.Membresia.CambiarEstado(EstadoDeMembresia.NoActiva(), new DateTime(2026, 3, 20));
			Ok("Sigue en mora", u.Membresia.EstaInactiva());
			PeriodoInactivo abierto = u.Membresia.PeriodosInactivos()[0];
			Ok("El periodo esta abierto", !abierto.TieneFin);
			Revienta("Preguntarle el fin a un periodo abierto revienta: es la leccion de T1",
				() => { DateTime fin = abierto.Hasta; });
			Ok("Del 20 de marzo al 6 de abril son 18 dias, dos semanas: espera 33",
				u.IncumplimientoDe(p).DiasEsperados == 33);

			// EL PAGO Y EL COBRO, por identidad (regla 18).
			Ok("Un pago entra", t.PuedeRegistrarPago(new DateTime(2026, 1, 5), 25000m));
			t.Membresia.RegistrarPago(new Pago(new DateTime(2026, 1, 5), 25000m));
			Ok("El mismo pago otra vez no entra", !t.PuedeRegistrarPago(new DateTime(2026, 1, 5), 25000m));
			Revienta("Y forzarlo revienta", () => t.Membresia.RegistrarPago(new Pago(new DateTime(2026, 1, 5), 25000m)));
			DateTime vence = new DateTime(2026, 2, 10), desde = new DateTime(2026, 1, 24), hasta = new DateTime(2026, 2, 23);
			Ok("Un cobro entra", t.PuedeAnunciarCobro(vence, 25000m, "Enero", desde, hasta, "abierto"));
			t.Membresia.AnunciarCobro(new Cobro(vence, 25000m, "Enero", desde, hasta, EstadoDeCobro.Abierto()));
			Ok("El mismo vencimiento no se anuncia dos veces", !t.PuedeAnunciarCobro(vence, 25000m, "Enero", desde, hasta, "abierto"));
			Revienta("Un pago de cero no es un pago", () => new Pago(new DateTime(2026, 1, 5), 0m));
		}

		// ------------------------------------------------------- la progresion del plan

		private static void LaProgresionDeLoIndicado()
		{
			Titulo("La progresion es de lo PRESCRITO, no de lo levantado -y desde el v3, sobre lo que el papel trae-");

			Trayectoria t = new Trayectoria();

			Plan uno = new Plan(new DateTime(2026, 1, 5), 3);
			uno.Prescribir(EjercicioPrescrito.DeFuerza("prensa", 3, 12, 15, 30));
			uno.Terminar(new DateTime(2026, 4, 6));
			t.Agregar(uno);

			Ok("Con un solo plan no hay progresion", !t.SePuedeVerLaProgresionDe("prensa"));
			Revienta("Y pedirla revienta", () => t.ComoCambioLoPrescrito("prensa"));

			Plan dos = new Plan(new DateTime(2026, 4, 7), 3);
			dos.Prescribir(EjercicioPrescrito.DeFuerza("prensa", 4, 12, 15, 30));
			t.Agregar(dos);

			Ok("Con dos planes si", t.SePuedeCompararLoPrescrito("prensa"));
			Progresion prog = t.ComoCambioLoPrescrito("prensa");
			Ok("De 3x15 a 4x15: el volumen subio 15 repeticiones", prog.EnQueSeMide == "volumen" && prog.Cambio == 15m && prog.Subio);

			Revienta("Un plan no puede prescribir dos veces el mismo ejercicio",
				() => dos.Prescribir(EjercicioPrescrito.DeFuerza("prensa", 4, 12, 15, 30)));

			// EL PESO YA NO ESTA EN EL PAPEL (hueco 17): la pregunta del v2 sigue, guardada.
			Ok("Sin peso indicado no hay 'cuanto subio lo que levanto'", !t.SePuedeVerLaProgresionDelPeso("prensa"));
			Revienta("Y pedirlo revienta en vez de inventar cero", () => t.CuantoSubioLoIndicado("prensa"));
			uno.Prescrito("prensa").IndicarPeso(40m);
			dos.Prescrito("prensa").IndicarPeso(55m);
			Ok("Si el detalle algun dia trae peso, la pregunta del v2 vuelve: subio 15 kilos",
				t.SePuedeVerLaProgresionDelPeso("prensa") && t.CuantoSubioLoIndicado("prensa") == 15m);
			Revienta("El peso se indica una vez (regla 18)", () => uno.Prescrito("prensa").IndicarPeso(45m));
		}

		// ----------------------------------------------------------------- que falta

		private static void CuantoFalta()
		{
			Titulo("Sin valor deseado no hay cuanto falta, y se dice");

			Trayectoria t = new Trayectoria();
			t.DeclararPerfil(PerfilDeSonia());
			t.Agregar(MedicionCon(new DateTime(2026, 4, 10), new Cintura(88m)));

			Objetivo soloDireccion = new Objetivo();
			soloDireccion.Agregar(Meta.SoloDireccion("cintura", Direccion.Bajar()));
			t.DeclararObjetivo(soloDireccion);
			Ok("Con meta sin valor, no se puede decir cuanto falta",
				!t.SePuedeDecirCuantoFalta("cintura"));
			Revienta("Y pedirlo revienta en vez de inventar un numero",
				() => t.CuantoFaltaPara("cintura"));

			Trayectoria u = new Trayectoria();
			u.DeclararPerfil(PerfilDeSonia());
			u.Agregar(MedicionCon(new DateTime(2026, 4, 10), new Cintura(88m)));
			Objetivo conValor = new Objetivo();
			conValor.Agregar(Meta.ConValor("cintura", Direccion.Bajar(), 80m));
			u.DeclararObjetivo(conValor);

			Ok("Con valor si", u.SePuedeDecirCuantoFalta("cintura"));
			Ok("Y faltan 8 centimetros", u.CuantoFaltaPara("cintura") == 8m);

			Objetivo repetido = new Objetivo();
			repetido.Agregar(Meta.SoloDireccion("peso", Direccion.Bajar()));
			Revienta("Dos metas sobre la misma medida se contradicen y se rechazan",
				() => repetido.Agregar(Meta.SoloDireccion("peso", Direccion.Subir())));
		}

		// ------------------------------------------------------------------ la ausencia

		private static void LaAusencia()
		{
			Titulo("La ausencia: tu plan termino y nadie te midio");

			Trayectoria t = new Trayectoria();

			Plan medido = new Plan(new DateTime(2026, 1, 5), 3);
			medido.Terminar(new DateTime(2026, 4, 6));
			t.Agregar(medido);
			t.Agregar(MedicionCon(new DateTime(2026, 4, 9), new Cintura(88m)));

			Plan sinMedir = new Plan(new DateTime(2026, 4, 7), 3);
			sinMedir.Terminar(new DateTime(2026, 7, 6));
			t.Agregar(sinMedir);

			Plan corriendo = new Plan(new DateTime(2026, 7, 7), 3);
			t.Agregar(corriendo);

			IReadOnlyList<Plan> huecos = t.PlanesQueTerminaronSinMedicion(14);
			Ok("Un plan sin medicion despues queda marcado", huecos.Count == 1);
			Ok("Y es el que corresponde", huecos.Count == 1 && huecos[0] == sinMedir);
			Ok("El plan que sigue corriendo no cuenta: todavia no le toca",
				huecos.Count == 1 && huecos[0] != corriendo);

			// El mismo caso con dos dias de gracia: la medicion del dia 9 ya no cuenta
			// para el plan que termino el 6.
			Ok("Con dos dias de gracia, el primero tambien queda sin medir",
				t.PlanesQueTerminaronSinMedicion(2).Count == 2);
		}

		// ----------------------------------------------- lo que el papel trajo (v3, N7)

		private static void LaEvaluacionDelAparatoEsHecho()
		{
			Titulo("La evaluacion del aparato llega como hecho, y la app no la recalcula (N7)");

			Perfil sonia = PerfilDeSonia();
			Referencia cualquiera = new Referencia(0m, 10m, 1m, 5m);

			Medida visceral = Medida.Crear("grasa-visceral", 12m);
			Ok("Las cinco medidas nuevas se crean por nombre", visceral is GrasaVisceral
				&& Medida.Crear("imc", 24m) is IndiceDeMasaCorporal
				&& Medida.Crear("agua", 50m) is Agua
				&& Medida.Crear("metabolismo-basal", 1400m) is MetabolismoBasal
				&& Medida.Crear("masa-osea", 2.5m) is MasaOsea);
			Ok("Y las cuatro del v2 siguen", Medida.EsUnNombreConocido("cintura") && Medida.EsUnNombreConocido("masa-muscular"));
			Revienta("Cien por ciento de agua no existe", () => Medida.Crear("agua", 100m));

			Ok("Una medida nueva sabe que el aparato la evalua", visceral.ElAparatoLaEvalua);
			Ok("Y una del v2 sabe que no", !new Cintura(88m).ElAparatoLaEvalua);
			Revienta("Leerla contra un rango propio revienta: el aparato ya la leyo",
				() => visceral.Leer(sonia, cualquiera));
			Revienta("Y no tiene clave de referencia: nadie dijo de que dependeria",
				() => visceral.ClaveDeReferencia(sonia));

			Ok("Recien creada no trae evaluacion", !visceral.TieneEvaluacionDelAparato);
			Revienta("Y pedirla revienta en vez de inventar 'normal'", () => { EvaluacionDelAparato e = visceral.EvaluacionDelAparato; });
			visceral.ConEvaluacion(EvaluacionDelAparato.PorNombre("high"));
			Ok("Con la palabra del aparato, la trae tal cual", visceral.EvaluacionDelAparato.Etiqueta == "high");
			Revienta("Y no se anota dos veces", () => visceral.ConEvaluacion(EvaluacionDelAparato.High()));
			Revienta("Una palabra que el papel no mostro revienta", () => EvaluacionDelAparato.PorNombre("normal"));
			Revienta("Y la meta del aparato -'Didn't reach goals'- no entra: Jorge la saco (hueco 20)",
				() => EvaluacionDelAparato.PorNombre("didnt-reach-goals"));

			// LOS DOS JUICIOS SIGUEN SIENDO DOS: el aparato dice el punto, la app dice la serie.
			Objetivo bajar = new Objetivo();
			bajar.Agregar(Meta.SoloDireccion("grasa-visceral", Direccion.Bajar()));
			Trayectoria t = new Trayectoria();
			t.DeclararPerfil(sonia);
			t.DeclararObjetivo(bajar);
			t.Agregar(MedicionCon(new DateTime(2026, 1, 10), Medida.Crear("grasa-visceral", 14m).ConEvaluacion(EvaluacionDelAparato.High())));
			t.Agregar(MedicionCon(new DateTime(2026, 4, 10), Medida.Crear("grasa-visceral", 12m).ConEvaluacion(EvaluacionDelAparato.High())));
			Ok("El aparato dice HIGH hoy...", t.LoQueDijoElAparato("grasa-visceral").Etiqueta == "high");
			Ok("...y la app dice que MEJORA", t.ComoVengo("grasa-visceral", cualquiera).Mejora());
			Revienta("ComoEstoy ya no existe para lo que el aparato evalua", () => t.ComoEstoy("grasa-visceral", cualquiera));
			Ok("Una medida sin evaluacion contesta que el aparato no la evaluo", !t.ElAparatoEvaluo("cintura"));

			// SIN META, NADA DE DIRECCION INVENTADA para las nuevas: la hoja del v3 no la firmo.
			Trayectoria sinMeta = new Trayectoria();
			sinMeta.DeclararPerfil(sonia);
			sinMeta.DeclararObjetivo(new Objetivo());
			sinMeta.Agregar(MedicionCon(new DateTime(2026, 1, 10), Medida.Crear("masa-osea", 2.5m)));
			sinMeta.Agregar(MedicionCon(new DateTime(2026, 4, 10), Medida.Crear("masa-osea", 2.6m)));
			Revienta("'La masa osea sube y punto' esta sin firmar: sin meta, revienta",
				() => sinMeta.ComoVengo("masa-osea", cualquiera));
		}

		// ----------------------------------------------- las dos formas del papel (v3, N8)

		private static void LasDosFormasDePrescribir()
		{
			Titulo("Fuerza y cardio se prescriben en escalas distintas, y no se comparan entre si (N8)");

			EjercicioPrescrito remo = EjercicioPrescrito.DeFuerza("remo", 3, 12, 15, 30);
			EjercicioPrescrito cardio = EjercicioPrescrito.DeCardio("caminadora", 15);

			Ok("La fuerza dice '3x 12 a 15' con las palabras del papel", remo.Prescripcion.Etiqueta == "3x 12 a 15");
			Ok("El cardio dice '15 minutos'", cardio.Prescripcion.Etiqueta == "15 minutos");
			Ok("Su magnitud es el volumen: 45", remo.Prescripcion.Magnitud == 45m && remo.Prescripcion.EnQueSeMide == "volumen");
			Ok("La del cardio son los minutos", cardio.Prescripcion.Magnitud == 15m && cardio.Prescripcion.EnQueSeMide == "minutos");
			Ok("No son de la misma forma", !remo.Prescripcion.EsDeLaMismaFormaQue(cardio.Prescripcion));
			Revienta("Un rango invertido no entra", () => new PrescripcionDeFuerza(3, 15, 12, 30));
			Revienta("Cero minutos no es cardio", () => new PrescripcionDeCardio(0));


			// Un ejercicio que cambia de forma entre planes no se compara, y se dice.
			Trayectoria t = new Trayectoria();
			Plan uno = new Plan(new DateTime(2026, 1, 5), 3);
			uno.Prescribir(EjercicioPrescrito.DeCardio("bici", 10));
			t.Agregar(uno);
			Plan dos = new Plan(new DateTime(2026, 4, 7), 3);
			dos.Prescribir(EjercicioPrescrito.DeFuerza("bici", 3, 12, 15, 30));
			t.Agregar(dos);
			Ok("Aparece en dos planes...", t.SePuedeVerLaProgresionDe("bici"));
			Ok("...pero cambio de forma, asi que no se compara", !t.SePuedeCompararLoPrescrito("bici"));
			Revienta("Y pedirlo revienta", () => t.ComoCambioLoPrescrito("bici"));
		}

		// -------------------------------------------------- el cobro con periodo (v3, N10)

		private static void ElCobroCubreUnPeriodo()
		{
			Titulo("El cobro cubre un periodo, y el vencimiento cae adentro (N10)");

			// Como en el papel: "Agosto" cubre del 24/08 al 23/09 y vence el 10/09.
			Cobro agosto = new Cobro(new DateTime(2026, 9, 10), 100m, "Agosto",
				new DateTime(2026, 8, 24), new DateTime(2026, 9, 23), EstadoDeCobro.Abierto());
			Ok("Cubre 31 dias", agosto.DiasQueCubre == 31);
			Ok("Cubre el 1 de septiembre", agosto.Cubre(new DateTime(2026, 9, 1)));
			Ok("No cubre el 23 de agosto", !agosto.Cubre(new DateTime(2026, 8, 23)));
			Ok("Y vence ADENTRO del periodo que cubre: se paga despues de empezar", agosto.VenceDentroDelPeriodo);
			Ok("Esta abierto, con la palabra del gimnasio", agosto.Estado.EstaAbierto() && agosto.Estado.Etiqueta == "abierto");
			Revienta("Un periodo invertido no entra", () => new Cobro(new DateTime(2026, 9, 10), 100m, "Agosto",
				new DateTime(2026, 9, 23), new DateTime(2026, 8, 24), EstadoDeCobro.Abierto()));
			Revienta("Un estado que nadie dijo revienta", () => EstadoDeCobro.PorNombre("vencido"));

			// LA FASE CAMBIA: de abierto a pagado, cuando el gimnasio lo dice (hueco 18, Jorge).
			Ok("Pasar a pagado entra", agosto.PuedeCambiarA(EstadoDeCobro.Pagado()));
			agosto.CambiarEstado(EstadoDeCobro.Pagado());
			Ok("Y ahora esta pagado", agosto.Estado.EstaPagado() && !agosto.Estado.EstaAbierto());
			Ok("Pagado otra vez es el mismo hecho otra vez (regla 18)", !agosto.PuedeCambiarA(EstadoDeCobro.Pagado()));
			Revienta("Y forzarlo revienta", () => agosto.CambiarEstado(EstadoDeCobro.Pagado()));

			Trayectoria t = new Trayectoria();
			Ok("Sin cobros, ningun dia esta cubierto", !t.Membresia.TieneCobroQueCubre(new DateTime(2026, 9, 1)));
			Revienta("Y pedir el cobro revienta", () => t.Membresia.CobroQueCubre(new DateTime(2026, 9, 1)));
			t.Membresia.AnunciarCobro(agosto);
			t.Membresia.AnunciarCobro(new Cobro(new DateTime(2026, 10, 10), 100m, "Septiembre",
				new DateTime(2026, 9, 24), new DateTime(2026, 10, 23), EstadoDeCobro.Abierto()));
			Ok("El 1 de septiembre lo cubre 'Agosto'", t.Membresia.CobroQueCubre(new DateTime(2026, 9, 1)).Etiqueta == "Agosto");
			Ok("El 24 de septiembre ya lo cubre 'Septiembre'", t.Membresia.CobroQueCubre(new DateTime(2026, 9, 24)).Etiqueta == "Septiembre");
			Ok("El Check rechaza un estado desconocido antes de construir",
				!t.PuedeAnunciarCobro(new DateTime(2026, 11, 10), 100m, "Octubre", new DateTime(2026, 10, 24), new DateTime(2026, 11, 23), "vencido"));
			Ok("Dos cobros sobre el mismo dia son un error (hueco 22): el que se solapa no entra",
				!t.PuedeAnunciarCobro(new DateTime(2026, 10, 15), 100m, "Otro", new DateTime(2026, 10, 1), new DateTime(2026, 10, 30), "abierto"));
			Ok("Uno que empieza justo despues del ultimo si entra",
				t.PuedeAnunciarCobro(new DateTime(2026, 11, 10), 100m, "Octubre", new DateTime(2026, 10, 24), new DateTime(2026, 11, 23), "abierto"));
			Ok("Cambiar el estado de un cobro que no existe no entra",
				!t.PuedeCambiarEstadoDeCobro(new DateTime(2026, 12, 10), "pagado"));
			Ok("El de septiembre puede pasar a pagado", t.PuedeCambiarEstadoDeCobro(new DateTime(2026, 10, 10), "pagado"));
		}

		// -------------------------------------------- el producto y el dia (v3, N11)

		private static void ElProductoContratado()
		{
			Titulo("El producto y el dia de vencimiento entran como contratacion (N11)");

			Trayectoria t = new Trayectoria();
			Ok("Sin contratacion no hay producto", !t.Membresia.TieneProductoContratado);
			Revienta("Y pedirlo revienta en vez de inventar uno", () => { string p = t.Membresia.Producto; });

			Ok("Contratar entra", t.PuedeContratar("Black", 10, new DateTime(2026, 8, 24)));
			t.Membresia.Contratar(new Contratacion(new DateTime(2026, 8, 24), "Black", 10));
			Ok("Y dice el producto y el dia", t.Membresia.Producto == "Black" && t.Membresia.DiaDeVencimiento == 10);
			Ok("El mismo producto otra vez no entra (regla 18)", !t.PuedeContratar("black", 10, new DateTime(2026, 9, 1)));
			Ok("Otro producto, despues, si", t.PuedeContratar("Otro", 10, new DateTime(2026, 9, 1)));
			Ok("Pero no hacia atras", !t.PuedeContratar("Otro", 10, new DateTime(2026, 8, 1)));
			Revienta("El dia 32 no es del mes", () => new Contratacion(new DateTime(2026, 8, 24), "Black", 32));
		}

		// ------------------------------------------------------------------ utilitarios

		private static Perfil PerfilDeSonia()
		{
			return new Perfil(Genero.PorNombre("femenino"), 42, 165m);
		}

		private static Referencia Referencias()
		{
			return new Referencia(0m, 88m, 1m, 5m);
		}

		private static Medicion MedicionCon(DateTime fecha, Medida medida)
		{
			Medicion m = new Medicion(fecha);
			m.Registrar(medida);
			return m;
		}

		private static void Titulo(string texto)
		{
			Console.WriteLine();
			Console.WriteLine("  " + texto);
		}

		private static void Ok(string dice, bool cierto)
		{
			Console.WriteLine((cierto ? "    ok    " : "    FALLA ") + dice);
			if (!cierto) fallas++;
		}

		private static void Revienta(string dice, Action accion)
		{
			try { accion(); Ok(dice, false); }
			catch (Exception) { Ok(dice, true); }
		}
	}
}
