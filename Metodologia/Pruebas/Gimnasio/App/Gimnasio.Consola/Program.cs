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
			LaProgresionDeLoIndicado();
			CuantoFalta();
			LaAusencia();

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

			Ok("El peso se compara como IMC, y su rango no depende del perfil",
				new Peso(70m).ClaveDeReferencia(sonia) == "imc");
			Ok("La grasa depende de genero y edad",
				new PorcentajeDeGrasa(30m).ClaveDeReferencia(sonia) == "grasa:femenino:42");
			Ok("La cintura depende solo del genero",
				new Cintura(88m).ClaveDeReferencia(sonia) == "cintura:femenino");
			Ok("La masa muscular no depende de nada del perfil",
				new MasaMuscular(30m).ClaveDeReferencia(sonia) == "masa-muscular");

			// Lo propio del peso: es la unica que no se compara consigo misma.
			decimal imc = new Peso(70m).ValorComparable(sonia);
			Ok("Y el peso no se compara consigo mismo: 70 kg con 1,65 m son 25,7 de IMC",
				Math.Round(imc, 1) == 25.7m);
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

		// ------------------------------------------------------- la progresion del plan

		private static void LaProgresionDeLoIndicado()
		{
			Titulo("La progresion es de lo PRESCRITO, no de lo levantado");

			Trayectoria t = new Trayectoria();

			Plan uno = new Plan(new DateTime(2026, 1, 5), 3);
			uno.Prescribir(new EjercicioPrescrito("prensa", 40m));
			uno.Terminar(new DateTime(2026, 4, 6));
			t.Agregar(uno);

			Ok("Con un solo plan no hay progresion", !t.SePuedeVerLaProgresionDe("prensa"));
			Revienta("Y pedirla revienta", () => t.CuantoSubioLoIndicado("prensa"));

			Plan dos = new Plan(new DateTime(2026, 4, 7), 3);
			dos.Prescribir(new EjercicioPrescrito("prensa", 55m));
			t.Agregar(dos);

			Ok("Con dos planes si", t.SePuedeVerLaProgresionDe("prensa"));
			Ok("Y subio 15 kilos lo que le piden", t.CuantoSubioLoIndicado("prensa") == 15m);

			Revienta("Un plan no puede prescribir dos veces el mismo ejercicio",
				() => dos.Prescribir(new EjercicioPrescrito("prensa", 60m)));
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
