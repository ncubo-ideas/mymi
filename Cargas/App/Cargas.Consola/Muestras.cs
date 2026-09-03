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
	// La historia que cuentan estas nueve cargas (§13 del brief, v2):
	//
	//   tramos 1 a 3:   12.6 km/L parejo - la LINEA BASE
	//   carga 4:        PRESETEADA (20.000 L exactos) - la inferencia dice "no fue
	//                   lleno" y acierta; la parcial no rompe el tramo, lo aplaza
	//   tramo con la parcial adentro: sigue en 12.6
	//   tramo 5 a 6:    11.7 - empieza la caida
	//   carga 7:        en OTRO SURTIDOR (B-1) - el tramo sale no comparable
	//   carga 8:        SIN kilometraje - rompe dos tramos finos y motiva el ingreso
	//   tramo 7 a 9:    el largo, 820 km / 87 L = 9.4 - la anomalia sostenida
	//
	// Los totales cuadran EXACTO con precio por volumen a proposito: el veredicto
	// TotalNoCuadra se ejercita con una carga sintetica en Comprobaciones, no aca.
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
			// --- la linea base: tres tramos parejos de ~12.6 km/L ---
			Carga(201, "2025-12-15T08:10:00", 40m, "A-3", 615m, 24600m, "automatico", "T-0201"),
			Carga(202, "2026-01-15T09:20:00", 38m, "A-3", 620m, 23560m, "automatico", "T-0202"),
			Carga(203, "2026-02-15T08:45:00", 39m, "A-3", 628m, 24492m, "automatico", "T-0203"),

			// --- la parcial PRESETEADA: 20.000 L exactos. La huella queda en el dato y
			// la inferencia propone "no fue lleno" antes de que Sonia diga nada ---
			Carga(204, "2026-03-20T17:30:00", 20m, "A-3", 634m, 12680m, "preseteado", "T-0204"),
			Carga(205, "2026-03-28T08:15:00", 19m, "A-3", 634m, 12046m, "automatico", "T-0205"),

			// --- empieza la caida ---
			Carga(206, "2026-05-20T08:50:00", 41m, "A-3", 645m, 26445m, "automatico", "T-0206"),

			// --- otro surtidor: consistente por dentro, no comparable contra la serie ---
			Carga(207, "2026-06-20T09:05:00", 42m, "B-1", 652m, 27384m, "automatico", "T-0207"),

			// --- la carga a la que le va a faltar el kilometraje ---
			Carga(208, "2026-07-25T08:30:00", 43m, "A-3", 660m, 28380m, "automatico", "T-0208"),

			// --- hoy: el tramo largo cierra la anomalia, y los 44 L fijan la capacidad ---
			Carga(209, "2026-08-25T08:40:00", 44m, "A-3", 668m, 29392m, "automatico", "T-0209"),

			// --- la lealtad, discreta: un punto por litro entero (⚠️ tasa del demo) ---
			Puntos(210, "2025-12-15T08:10:05", 40),
			Puntos(211, "2026-01-15T09:20:05", 38),
			Puntos(212, "2026-02-15T08:45:05", 39),
			Puntos(213, "2026-03-20T17:30:05", 20),
			Puntos(214, "2026-03-28T08:15:05", 19),
			Puntos(215, "2026-05-20T08:50:05", 41),
			Puntos(216, "2026-06-20T09:05:05", 42),
			Nivel(217, "2026-06-20T09:05:10", "plata"),
			Puntos(218, "2026-07-25T08:30:05", 43),
			Puntos(219, "2026-08-25T08:40:05", 44),
		};

		// LOS APORTES DE SONIA. NO viajan por URL2 -la estacion no los sabe- asi que no
		// son senales: son los comandos locales que siguen al refresh. El kilometraje de
		// la carga 8 falta A PROPOSITO: es la historia del tramo bloqueado.
		//
		// El tanque no se declara: se CONFIRMA. Cada carga nace con la inferencia de su
		// forma de corte, y Sonia solo da el visto bueno (§6.3.3).
		internal static readonly (string transaccion, decimal kilometros)[] Kilometrajes =
		{
			("T-0201", 40000m),
			("T-0202", 40480m),
			("T-0203", 40970m),
			("T-0204", 41200m),
			("T-0205", 41460m),
			("T-0206", 41940m),
			("T-0207", 42380m),
			// T-0208 sin kilometraje: rompe dos tramos finos y motiva el ingreso.
			("T-0209", 43200m),
		};

		internal static readonly string[] TanquesPorConfirmar =
		{
			"T-0201", "T-0202", "T-0203", "T-0204", "T-0205",
			"T-0206", "T-0207", "T-0208", "T-0209",
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
		internal const int CargasEsperadas = 9;
		internal const string VeredictoEsperado = "empeoro";
		internal const int PuntosEsperados = 326;

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
