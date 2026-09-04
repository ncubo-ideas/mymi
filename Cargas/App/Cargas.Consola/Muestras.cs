using Puppets.Portador;

namespace Cargas.Consola
{
	// EL GUION DE LA DEMO, en la MISMA forma de wire que emite el mock de la estacion.
	// Se usa para medir el dominio sin depender de que docker este arriba: entran por la
	// misma ruta (SenalDeEvento -> RutaDeCargas -> comando) que los reales.
	//
	// Las fechas son LITERALES, no "hoy menos ocho meses". Es obligatorio: el dominio no
	// lee el reloj, y si estas fechas se calcularan con DateTime.Now el mismo journal
	// reproducido manana daria otros dias transcurridos y otra distancia diaria. Un
	// guion de demo con fechas moviles no es reproducible.
	//
	// La historia que cuentan estas 28 cargas (§13 del brief, v2, realineada al CANON
	// de Sonia: el mismo carro que cuenta el modulo del concesionario):
	//
	//   el odometro:    49.850 (dic-25) -> 60.962 (ago-26), con el QUIEBRE DE ENERO -850
	//                   km/mes antes del ascenso, 1.400 despues-. Ese salto es el unico
	//                   hecho del que cuelga el demo entero.
	//   la cadencia:    y por eso las cargas se aprietan SOLAS, de una cada 17 dias a una
	//                   cada 8. El ascenso se ve aca sin que nadie lo explique, y es lo
	//                   que ningun otro modulo puede mostrar.
	//   tramos 1 a 13:  12.6 km/L - la LINEA BASE. Son mayoria A PROPOSITO: el habitual
	//                   es la MEDIANA de los comparables, asi que si la base deja de ser
	//                   mayoria el habitual se corre y la caida deja de dar -25%.
	//   carga 7:        PRESETEADA (20.000 L exactos) - la inferencia dice "no fue lleno"
	//                   y acierta; la parcial no rompe el tramo, lo aplaza, y el tramo 6
	//                   sale de 680 km con los litros de las DOS cargas
	//   tramos 14 a 18: 11.7 - empieza la caida
	//   carga 18:       en OTRO SURTIDOR (B-1) - el tramo sale no comparable. Son 10 L
	//                   porque ese tramo son 128 km: lleno desde 3/4 de tanque en una
	//                   bomba que no es la suya. Y por 6.520 colones NO da boleto, que es
	//                   el unico caso del guion donde una carga no llega al corte.
	//   tramos 19 a 21: 11.0 -> 10.4 -> 9.8, el escalon
	//   carga 27:       SIN kilometraje - rompe dos tramos finos y motiva el ingreso
	//   tramo 25:       el largo, 743 km / 79 L = 9.4 - la anomalia sostenida
	//   la lealtad:     998 puntos cruzan LOS DOS umbrales, 200 y 800: plata en febrero y
	//                   oro en julio. El guion de nueve cargas se quedaba en plata.
	//
	// Los totales cuadran EXACTO con precio por volumen a proposito: el veredicto
	// TotalNoCuadra se ejercita con una carga sintetica en Comprobaciones, no aca.
	//
	// ESTA TABLA NO SE EDITA A MANO, y ese es el punto: sale de resolver tres
	// restricciones en cascada -el odometro lo fija el canon, el rendimiento de cada
	// tramo lo fija el arco, y entonces los litros y la fecha YA NO SON LIBRES, porque
	// litros = distancia / rendimiento y la fecha sale de invertir la curva del canon-.
	// Tocar un numero suelto rompe el cuadre en silencio. La unica holgura real es el
	// tramo del B-1, que por diseno no tiene que dar ningun numero.
	internal static class Muestras
	{
		// La placa del demo, y es LA DEL CANON: el carro de Sonia es el mismo en los dos
		// modulos, asi que el numero no lo elige este guion. La llave del cruce quedo
		// acordada con Kenneth el 2026-09-03 -el concesionario indexa por VIN adentro,
		// pero la placa es lo que cruza la puerta- porque pedirle el VIN a alguien en una
		// gasolinera no lo hace nadie (§6.1.1).
		internal const string PlacaDelDemo = "BQR-482";

		internal const string EstacionDelDemo = "La Sabana";

		internal static SenalDeEvento[] LaHistoria() => new[]
		{
			Carga(201, "2025-12-15T08:10:00", 37m, "A-3", 615m, 22755m, "automatico", "T-0201"),
			Carga(202, "2026-01-01T09:20:00", 36m, "A-3", 620m, 22320m, "automatico", "T-0202"),
			Carga(203, "2026-01-17T08:45:00", 38m, "A-3", 620m, 23560m, "automatico", "T-0203"),
			Carga(204, "2026-01-27T07:55:00", 37m, "A-3", 620m, 22940m, "automatico", "T-0204"),
			Carga(205, "2026-02-06T08:15:00", 36m, "A-3", 628m, 22608m, "automatico", "T-0205"),
			Carga(206, "2026-02-16T09:05:00", 36m, "A-3", 628m, 22608m, "automatico", "T-0206"),
			Carga(207, "2026-02-21T17:30:00", 20m, "A-3", 628m, 12560m, "preseteado", "T-0207"),
			Carga(208, "2026-03-02T08:40:00", 34m, "A-3", 634m, 21556m, "automatico", "T-0208"),
			Carga(209, "2026-03-12T07:40:00", 37m, "A-3", 634m, 23458m, "automatico", "T-0209"),
			Carga(210, "2026-03-22T09:30:00", 36m, "A-3", 634m, 22824m, "automatico", "T-0210"),
			Carga(211, "2026-04-01T08:10:00", 38m, "A-3", 640m, 24320m, "automatico", "T-0211"),
			Carga(212, "2026-04-11T09:20:00", 36m, "A-3", 640m, 23040m, "automatico", "T-0212"),
			Carga(213, "2026-04-21T08:45:00", 37m, "A-3", 640m, 23680m, "automatico", "T-0213"),
			Carga(214, "2026-05-02T07:55:00", 38m, "A-3", 645m, 24510m, "automatico", "T-0214"),
			Carga(215, "2026-05-12T08:15:00", 36m, "A-3", 645m, 23220m, "automatico", "T-0215"),
			Carga(216, "2026-05-21T09:05:00", 38m, "A-3", 645m, 24510m, "automatico", "T-0216"),
			Carga(217, "2026-05-30T08:30:00", 36m, "A-3", 645m, 23220m, "automatico", "T-0217"),
			Carga(218, "2026-06-02T08:40:00", 10m, "B-1", 652m, 6520m, "automatico", "T-0218"),
			Carga(219, "2026-06-12T07:40:00", 39m, "A-3", 652m, 25428m, "automatico", "T-0219"),
			Carga(220, "2026-06-21T09:30:00", 37m, "A-3", 652m, 24124m, "automatico", "T-0220"),
			Carga(221, "2026-06-30T08:10:00", 38m, "A-3", 652m, 24776m, "automatico", "T-0221"),
			Carga(222, "2026-07-09T09:20:00", 37m, "A-3", 660m, 24420m, "automatico", "T-0222"),
			Carga(223, "2026-07-17T08:45:00", 39m, "A-3", 660m, 25740m, "automatico", "T-0223"),
			Carga(224, "2026-07-25T07:55:00", 38m, "A-3", 660m, 25080m, "automatico", "T-0224"),
			Carga(225, "2026-08-01T08:15:00", 36m, "A-3", 668m, 24048m, "automatico", "T-0225"),
			Carga(226, "2026-08-09T09:05:00", 39m, "A-3", 668m, 26052m, "automatico", "T-0226"),
			Carga(227, "2026-08-17T08:30:00", 39m, "A-3", 668m, 26052m, "automatico", "T-0227"),
			Carga(228, "2026-08-25T08:40:00", 40m, "A-3", 668m, 26720m, "automatico", "T-0228"),

			Puntos(229, "2025-12-15T08:10:05", 37),
			Puntos(230, "2026-01-01T09:20:05", 36),
			Puntos(231, "2026-01-17T08:45:05", 38),
			Puntos(232, "2026-01-27T07:55:05", 37),
			Puntos(233, "2026-02-06T08:15:05", 36),
			Puntos(234, "2026-02-16T09:05:05", 36),
			Nivel(235, "2026-02-16T09:05:07", "plata"),
			Puntos(236, "2026-02-21T17:30:05", 20),
			Puntos(237, "2026-03-02T08:40:05", 34),
			Puntos(238, "2026-03-12T07:40:05", 37),
			Puntos(239, "2026-03-22T09:30:05", 36),
			Puntos(240, "2026-04-01T08:10:05", 38),
			Puntos(241, "2026-04-11T09:20:05", 36),
			Puntos(242, "2026-04-21T08:45:05", 37),
			Puntos(243, "2026-05-02T07:55:05", 38),
			Puntos(244, "2026-05-12T08:15:05", 36),
			Puntos(245, "2026-05-21T09:05:05", 38),
			Puntos(246, "2026-05-30T08:30:05", 36),
			Puntos(247, "2026-06-02T08:40:05", 10),
			Puntos(248, "2026-06-12T07:40:05", 39),
			Puntos(249, "2026-06-21T09:30:05", 37),
			Puntos(250, "2026-06-30T08:10:05", 38),
			Puntos(251, "2026-07-09T09:20:05", 37),
			Puntos(252, "2026-07-17T08:45:05", 39),
			Nivel(253, "2026-07-17T08:45:07", "oro"),
			Puntos(254, "2026-07-25T07:55:05", 38),
			Puntos(255, "2026-08-01T08:15:05", 36),
			Puntos(256, "2026-08-09T09:05:05", 39),
			Puntos(257, "2026-08-17T08:30:05", 39),
			Puntos(258, "2026-08-25T08:40:05", 40),
		};

		// LOS APORTES DE SONIA. NO viajan por URL2 -la estacion no los sabe- asi que no
		// son senales: son los comandos locales que siguen al refresh. El kilometraje de
		// la carga 27 (T-0227) falta A PROPOSITO: es la historia del tramo bloqueado.
		//
		// El tanque no se declara: se CONFIRMA. Cada carga nace con la inferencia de su
		// forma de corte, y Sonia solo da el visto bueno (§6.3.3).
		internal static readonly (string transaccion, decimal kilometros)[] Kilometrajes =
		{
			("T-0201", 49850m),
			("T-0202", 50304m),
			("T-0203", 50783m),
			("T-0204", 51249m),
			("T-0205", 51703m),
			("T-0206", 52157m),
			("T-0207", 52409m),
			("T-0208", 52837m),
			("T-0209", 53303m),
			("T-0210", 53757m),
			("T-0211", 54236m),
			("T-0212", 54690m),
			("T-0213", 55156m),
			("T-0214", 55635m),
			("T-0215", 56089m),
			("T-0216", 56534m),
			("T-0217", 56955m),
			("T-0218", 57083m),
			("T-0219", 57539m),
			("T-0220", 57972m),
			("T-0221", 58390m),
			("T-0222", 58775m),
			("T-0223", 59157m),
			("T-0224", 59514m),
			("T-0225", 59852m),
			("T-0226", 60219m),
			("T-0228", 60962m),
		};

		internal static readonly string[] TanquesPorConfirmar =
		{
			"T-0201", "T-0202", "T-0203", "T-0204", "T-0205",
			"T-0206", "T-0207", "T-0208", "T-0209", "T-0210",
			"T-0211", "T-0212", "T-0213", "T-0214", "T-0215",
			"T-0216", "T-0217", "T-0218", "T-0219", "T-0220",
			"T-0221", "T-0222", "T-0223", "T-0224", "T-0225",
			"T-0226", "T-0227", "T-0228",
		};

		// LA FICHA: el tercer aporte, y el que el guion venia sin sembrar. CompletarFicha
		// existia desde el principio y nadie lo llamaba, asi que en una corrida limpia el
		// carro de Sonia no tenia mas nombre que su placa y el rendimiento de fabrica no
		// existia. Sin eso el CRUCE ESTRELLA -"el manual dice 18, tus tramos dan 9"- no
		// se puede ensenar sin que alguien lo escriba a mano en la pantalla primero.
		//
		// El carro es el del canon: Toyota Yaris 2019.
		//
		// LOS 18 km/L SON APORTE DE SONIA, no un dato del concesionario. Ella los lee del
		// manual de su carro; la ficha del fabricante que publica el otro modulo trae
		// fluidos, llantas y grados de combustible, y NO trae consumo. Que el cruce se
		// sostenga sin pedirle nada a ese modulo es la gracia, no una carencia.
		//
		// EL COLOR VA VACIO A PROPOSITO: el canon no lo dice, y un color inventado se
		// leeria igual que un dato. El alias si es nuestro y tiene que serlo -es como la
		// persona le dice a su carro, y eso no lo fija ningun canon-.
		internal static readonly (string alias, string marca, string modelo, int anio,
			string color, decimal rendimientoDeFabrica) FichaDelDemo =
			("el Yaris", "Toyota", "Yaris", 2019, "", 18m);

		private static SenalDeEvento Carga(long id, string fecha, decimal litros,
			string surtidor, decimal precio, decimal total, string corte, string transaccion) =>
			new SenalDeEvento(id, "CargaRegistrada", $@"{{
				""id"": {id}, ""tipo"": ""CargaRegistrada"",
				""fecha"": ""{fecha}"",
				""placa"": ""{PlacaDelDemo}"",
				""tipoDeCombustible"": ""regular"",
				""surtidorIdentificacion"": ""{surtidor}"",
				""surtidorEstacion"": ""{EstacionDelDemo}"",
				""precioMonto"": {precio}, ""precioMoneda"": ""CRC"",
				""volumenValor"": {litros}, ""volumenUnidad"": ""L"",
				""totalMonto"": {total}, ""totalMoneda"": ""CRC"",
				""formaDeCorte"": ""{corte}"",
				""numeroDeTransaccion"": ""{transaccion}""
			}}");

		private static SenalDeEvento Puntos(long id, string fecha, int puntos) =>
			new SenalDeEvento(id, "PuntosAcreditados", $@"{{
				""id"": {id}, ""tipo"": ""PuntosAcreditados"",
				""fecha"": ""{fecha}"",
				""puntos"": {puntos}
			}}");

		private static SenalDeEvento Nivel(long id, string fecha, string nivel) =>
			new SenalDeEvento(id, "NivelActualizado", $@"{{
				""id"": {id}, ""tipo"": ""NivelActualizado"",
				""fecha"": ""{fecha}"",
				""nivel"": ""{nivel}""
			}}");

		// Lo que la historia tiene que dar. Son los criterios de la fase: si esto no
		// sale asi, el dominio no esta contando la historia que la demo promete.
		internal const int CargasEsperadas = 28;
		internal const string VeredictoEsperado = "empeoro";
		internal const int PuntosEsperados = 998;

		// Una senal de un tipo que esta app no consume. La ruta debe devolver null y el
		// bucle descartarla, sin inventar un comando para tragarsela.
		internal static SenalDeEvento TipoDesconocido() =>
			new SenalDeEvento(301, "CanjeDePuntos", @"{ ""id"": 301, ""tipo"": ""CanjeDePuntos"" }");

		// Una CargaRegistrada con el galon ambiguo. Tambien se descarta: "gal" no dice
		// cual de los dos galones es, y la diferencia es un 20% del rendimiento.
		internal static SenalDeEvento GalonAmbiguo() =>
			new SenalDeEvento(302, "CargaRegistrada", $@"{{
				""id"": 302, ""tipo"": ""CargaRegistrada"",
				""fecha"": ""2026-08-25T10:00:00"",
				""placa"": ""{PlacaDelDemo}"",
				""tipoDeCombustible"": ""regular"",
				""surtidorIdentificacion"": ""A-3"",
				""surtidorEstacion"": ""{EstacionDelDemo}"",
				""precioMonto"": 2.5, ""precioMoneda"": ""USD"",
				""volumenValor"": 10, ""volumenUnidad"": ""gal"",
				""totalMonto"": 25, ""totalMoneda"": ""USD"",
				""formaDeCorte"": ""automatico"",
				""numeroDeTransaccion"": ""T-0302""
			}}");
	}
}
