using System;
using System.Collections.Generic;
using System.Globalization;
using Cargas.Dominio;

namespace Cargas.Consola
{
	// LAS REGLAS DEL DOMINIO PURO, comprobadas sin actor, sin journal y sin disco: corre
	// en memoria y por eso es el ciclo corto de trabajo. Cada grupo lleva el nombre de
	// la regla que comprueba, como una frase que el equipo diria en voz alta.
	//
	// La fecha es FIJA: el dominio no lee el reloj, y este arnes tampoco. Es la fecha
	// de la carga 9 del guion.
	internal static class Comprobaciones
	{
		private static int fallas;
		private static int corridas;

		private static readonly DateTime Hoy = new DateTime(2026, 8, 25);

		internal static int Correr()
		{
			fallas = 0; corridas = 0;

			ElGalonNoEsUno();
			ElDineroSeRegistraNoSeConvierte();
			LosPuntosNoSonDinero();
			LaPlacaEsUnaIdentidadNoUnString();
			ElSurtidorEsElInstrumento();
			LaFormaDeCorteInfiereElTanque();
			LaAusenciaEsUnTipoYReventarEsDelLlamador();
			UnEstimadoNoCierraTramo();
			ElGuionParteEnLosTramosDelBrief();
			LaParcialNoRompeElTramoLoAplaza();
			UnKilometrajeFaltanteRompeDosTramosYDejaElLargo();
			LaSuposicionDelTanqueSeEnunciaAntesDePreguntar();
			ElGastoDelCarroNoEsElGastoDeLaPersona();
			LaFichaSeLeePlanaParaPoderCorregirla();
			OtroSurtidorNoSeComparaConLaSerie();
			ElVeredictoDelGuionEsEmpeoro();
			CambiarLaUnidadInvierteLaFlechaPeroNoElVeredicto();
			MejorarTambienSeDice();
			UnCambioChicoEsEstable();
			UnSoloTramoEsIndeterminado();
			SinDosLlenosNoHayTramoYSeDiceQueFalta();
			ElRendimientoImplausibleEsDatoSospechoso();
			CambiarDeCombustibleNoEsComparable();
			ElTotalQueNoCuadraSeReporta();
			ElOdometroNoRetrocede();
			LaCapacidadYLaAutonomiaSonDerivadasDeclaradas();
			ElOdometroEstimadoProponeYNuncaEscribe();
			LaCompletitudYLoQueFaltaMotivanElIngreso();
			AgregarElKilometrajeParteElTramoLargo();
			ElTanqueSeConfirmaNoSeConvence();
			LaLealtadLlegaPorEventoYNoEsElFoco();
			ElOrdenDeLlegadaNoImporta();
			LasLecturasGuardadasRevientanYLasDeReconocimientoDeciden();
			LaTransaccionDuplicadaEsUnDefectoDelEmisor();
			TodoConsejoConPlazoYElRojoReservado();
			LaFichaLeDaNombreAlVehiculo();

			Console.WriteLine();
			Console.WriteLine(fallas == 0
				? $"TODO OK  ({corridas} comprobaciones)"
				: $"{fallas} FALLAS de {corridas} comprobaciones");
			return fallas == 0 ? 0 : 1;
		}

		// ------------------------------------------------------------- las cantidades

		private static void ElGalonNoEsUno()
		{
			Titulo("el galon no es uno");

			Igual("un galon US son 3.785412 litros", "3.785412",
				Formato(new Volumen(1m, UnidadDeVolumen.GalonUS).EnLitros, "0.######"));
			Igual("un galon imperial son 4.54609 litros", "4.54609",
				Formato(new Volumen(1m, UnidadDeVolumen.GalonImperial).EnLitros, "0.######"));

			decimal diferencia = (4.546090m - 3.785412m) / 3.785412m;
			Igual("difieren un 20%", "20.1", Formato(diferencia * 100m, "0.#"));

			Revienta<ErrorDeDominio>("'gal' pelado revienta: no dice cual es",
				() => UnidadDeVolumen.PorSimbolo("gal"));
			Igual("'gal' pelado tampoco se reconoce", "False",
				UnidadDeVolumen.EsConocida("gal").ToString());

			Igual("la suma cruza unidades porque es fisica", "7.785412",
				Formato(new Volumen(1m, UnidadDeVolumen.GalonUS)
					.Mas(new Volumen(4m, UnidadDeVolumen.Litro)).EnLitros, "0.######"));
		}

		private static void ElDineroSeRegistraNoSeConvierte()
		{
			Titulo("el dinero se registra, no se convierte");

			var colones = new Dinero(1000m, Moneda.Colon);
			var dolares = new Dinero(2m, Moneda.Dolar);

			Revienta<ErrorDeDominio>("sumar colones con dolares revienta",
				() => colones.Mas(dolares));
			Revienta<ErrorDeDominio>("compararlos tampoco se puede",
				() => colones.DiferenciaAbsolutaCon(dolares));
			Igual("sumar en la misma moneda si", "₡1500",
				colones.Mas(new Dinero(500m, Moneda.Colon)).Expresado);
		}

		private static void LosPuntosNoSonDinero()
		{
			Titulo("los puntos no son dinero");

			// Puntos.Mas(Dinero) no COMPILA, que es el diseno: la comprobacion es que el
			// tipo existe con su propia aritmetica y sus umbrales.
			var puntos = new Puntos(300);
			Igual("se suman entre ellos", "340", puntos.Mas(new Puntos(40)).Cantidad.ToString());
			Igual("y saben cuanto falta para un umbral", "500",
				puntos.LosQueFaltanPara(new Puntos(800)).Cantidad.ToString());
			Igual("si ya se alcanzo, faltan cero", "0",
				new Puntos(900).LosQueFaltanPara(new Puntos(800)).Cantidad.ToString());
		}

		// ------------------------------------------------------ la identidad y el hecho

		private static void LaPlacaEsUnaIdentidadNoUnString()
		{
			Titulo("la placa es una identidad, no un string");

			Igual("se normaliza al construir", "ABC123", new Placa("abc-123").Texto);
			Igual("con espacios da lo mismo", "True",
				new Placa("ABC 123").EsLaMisma(new Placa("abc-123")).ToString());
			Revienta<ErrorDeDominio>("una placa sin nada adentro revienta",
				() => new Placa("---"));
		}

		private static void ElSurtidorEsElInstrumento()
		{
			Titulo("el surtidor es el instrumento de medicion");

			var estacion = new Estacion("La Sabana");
			var a3DeAyer = new Surtidor("A-3", estacion, TipoDeCombustible.Regular,
				new Dinero(615m, Moneda.Colon));
			var a3DeHoy = new Surtidor("A-3", estacion, TipoDeCombustible.Regular,
				new Dinero(668m, Moneda.Colon));
			var b1 = new Surtidor("B-1", estacion, TipoDeCombustible.Regular,
				new Dinero(652m, Moneda.Colon));

			Igual("el mismo instrumento aunque el precio cambie: el precio es estado",
				"True", a3DeAyer.EsElMismo(a3DeHoy).ToString());
			Igual("otro instrumento no es comparable", "False",
				a3DeAyer.EsComparableCon(b1).ToString());
		}

		private static void LaFormaDeCorteInfiereElTanque()
		{
			Titulo("la forma de corte infiere el tanque (§6.3.3)");

			DeclaracionDeTanque porAutomatico = FormaDeCorte.Automatico.Inferencia();
			Igual("corte automatico propone lleno", "True", porAutomatico.EsLleno.ToString());
			Igual("  con origen estimado", "estimado", porAutomatico.Origen.Nombre);
			Igual("  y un estimado NO cuenta para el calculo", "False",
				porAutomatico.EsLlenoQueCuenta.ToString());

			DeclaracionDeTanque porPreseteado = FormaDeCorte.Preseteado.Inferencia();
			Igual("corte preseteado propone parcial", "False", porPreseteado.EsLleno.ToString());

			Igual("corte indeterminado no propone nada", "False",
				FormaDeCorte.Indeterminado.Inferencia().FueDeclarada.ToString());
		}

		private static void LaAusenciaEsUnTipoYReventarEsDelLlamador()
		{
			Titulo("la ausencia es un tipo, y leerla es un bug del llamador");

			Igual("la declaracion ausente decide sin reventar", "False",
				DeclaracionDeTanque.Ninguna.FueDeclarada.ToString());
			Revienta<ErrorDeDominio>("pero leerle EsLleno revienta",
				() => _ = DeclaracionDeTanque.Ninguna.EsLleno);

			Igual("la lectura ausente decide sin reventar", "False",
				LecturaDeOdometro.Ninguna.Existe.ToString());
			Revienta<ErrorDeDominio>("pero leerle el kilometraje revienta",
				() => _ = LecturaDeOdometro.Ninguna.Kilometraje);

			Revienta<ErrorDeDominio>("la foto ausente no tiene archivo",
				() => _ = FotoDelTablero.Ninguna.NombreDeArchivo);
			Revienta<ErrorDeDominio>("la ficha vacia no tiene alias",
				() => _ = FichaDelVehiculo.Vacia.Alias);
			Revienta<ErrorDeDominio>("el rendimiento de fabrica no aportado no tiene valor",
				() => _ = RendimientoDeFabrica.SinDato.Valor);
		}

		private static void UnEstimadoNoCierraTramo()
		{
			Titulo("un estimado no cierra tramo (la anti-circularidad del §7.3)");

			var kilometraje = new Distancia(42900m, UnidadDeDistancia.Kilometro);

			Igual("observado cierra", "True",
				LecturaDeOdometro.De(kilometraje, OrigenDeLectura.Observado).CierraTramo.ToString());
			Igual("confirmado cierra: Sonia miro el numero y dijo que si", "True",
				LecturaDeOdometro.De(kilometraje, OrigenDeLectura.Confirmado).CierraTramo.ToString());
			Igual("estimado NO cierra: se confirmaria a si mismo", "False",
				LecturaDeOdometro.De(kilometraje, OrigenDeLectura.Estimado).CierraTramo.ToString());
		}

		// ------------------------------------------------------------------- el guion

		private static void ElGuionParteEnLosTramosDelBrief()
		{
			Titulo("el guion parte en los tramos del brief (§7 y §13)");

			BitacoraDeCargas bitacora = ElGuion();
			IReadOnlyList<Tramo> tramos = bitacora.TramosDe("ABC-123");

			Igual("nueve cargas, un vehiculo", "9 1",
				$"{bitacora.Cantidad} {bitacora.CantidadDeVehiculos}");
			Igual("seis tramos calculados y dos bloqueados", "6 2", Conteo(tramos));

			IReadOnlyList<Tramo> calculados = Calculados(tramos);
			Igual("el primer tramo: 480 km / 38 L", "12.6",
				Formato(calculados[0].RendimientoEnKmPorLitro, "0.#"));
			Igual("el segundo confirma la base", "12.6",
				Formato(calculados[1].RendimientoEnKmPorLitro, "0.#"));
			Igual("la caida empieza en 11.7", "11.7",
				Formato(calculados[3].RendimientoEnKmPorLitro, "0.#"));
			Igual("los litros de la carga que abre NO cuentan", "38",
				Formato(calculados[0].Litros, "0.#"));
		}

		private static void LaParcialNoRompeElTramoLoAplaza()
		{
			Titulo("una parcial no rompe el tramo, lo aplaza");

			IReadOnlyList<Tramo> calculados = Calculados(ElGuion().TramosDe("ABC-123"));

			// El tramo 3 absorbe la carga preseteada: 490 km con los litros de las DOS
			// cargas que repusieron (20 + 19).
			Igual("el tramo con la parcial adentro sigue en 12.6", "12.6",
				Formato(calculados[2].RendimientoEnKmPorLitro, "0.#"));
			Igual("  con los litros de la parcial y el lleno", "39",
				Formato(calculados[2].Litros, "0.#"));
			Igual("  y sigue siendo un solo tramo", "490",
				Formato(calculados[2].Kilometros, "0"));
		}

		// LO QUE SE PIDE CONFIRMAR HAY QUE DECIRLO PRIMERO. La pantalla pedia "la
		// confirmacion del tanque" y ofrecia "si, quedo asi" sin decir nunca que era
		// "asi", con los mismos dos botones tanto si la inferencia decia lleno como
		// parcial. La suposicion es del dominio -la hace FormaDeCorte- asi que la frase
		// que la enuncia tambien.
		// EL TABLERO ES DE UN CARRO, asi que su gasto tambien. El instrumento mostraba el
		// gasto de TODOS los vehiculos al lado de la autonomia, el rendimiento y el costo
		// por kilometro de UNO: con dos carros mentia por omision y nada lo decia.
		private static void ElGastoDelCarroNoEsElGastoDeLaPersona()
		{
			Titulo("el gasto del carro no es el gasto de la persona");

			var bitacora = new BitacoraDeCargas();

			// Dos carros, el mismo mes, la misma moneda.
			bitacora.Agregar(new Carga(new DateTime(2026, 4, 3), "La Sabana", "A-3",
				"regular", 40m, "L", 600m, "CRC", 24000m, "CRC", "automatico",
				"ABC-123", "T-8001"));
			bitacora.Agregar(new Carga(new DateTime(2026, 4, 18), "La Sabana", "A-3",
				"regular", 30m, "L", 600m, "CRC", 18000m, "CRC", "automatico",
				"XYZ-789", "T-8002"));

			IReadOnlyList<GastoEnMoneda> deLaPersona = bitacora.GastoDelPeriodo(2026, 4);
			Igual("la persona gasto los dos", "42000", deLaPersona[0].Monto.ToString("0"));
			Igual("en dos cargas", "2", deLaPersona[0].CantidadDeCargas.ToString());

			IReadOnlyList<GastoEnMoneda> delPrimero = bitacora.GastoDelPeriodoDe("ABC-123", 2026, 4);
			Igual("el primer carro solo lo suyo", "24000", delPrimero[0].Monto.ToString("0"));
			Igual("y una sola carga", "1", delPrimero[0].CantidadDeCargas.ToString());

			IReadOnlyList<GastoEnMoneda> delSegundo = bitacora.GastoDelPeriodoDe("XYZ-789", 2026, 4);
			Igual("y el segundo lo suyo", "18000", delSegundo[0].Monto.ToString("0"));

			// La misma acumulacion para los dos: si se calcularan por separado, un dia
			// darian distinto y nadie sabria cual creer.
			Igual("los dos suman lo de la persona", "42000",
				(delPrimero[0].Monto + delSegundo[0].Monto).ToString("0"));

			// Un mes sin cargas de ese carro no es cero: es ninguna fila. El cero seria
			// una moneda inventada.
			Igual("un mes sin cargas no trae filas", "0",
				bitacora.GastoDelPeriodoDe("ABC-123", 2026, 5).Count.ToString());
		}

		// LA FICHA SE PUEDE EDITAR, y para eso hay que poder leerla. Sus miembros revientan
		// cuando no existe -y hacen bien-, asi que la proyeccion plana devuelve el vacio de
		// cada tipo en vez de reventar el print.
		private static void LaFichaSeLeePlanaParaPoderCorregirla()
		{
			Titulo("la ficha se lee plana para poder corregirla");

			var bitacora = new BitacoraDeCargas();
			bitacora.Agregar(new Carga(new DateTime(2026, 4, 3), "La Sabana", "A-3",
				"regular", 40m, "L", 600m, "CRC", 24000m, "CRC", "automatico",
				"ABC-123", "T-8101"));

			Vehiculo sinFicha = bitacora.VehiculoDe("ABC-123");
			Igual("sin ficha, el alias es vacio y no revienta", "", sinFicha.AliasDeLaFicha);
			Igual("sin ficha, el anio es cero", "0", sinFicha.AnioDeLaFicha.ToString());
			Igual("sin ficha, el rendimiento de fabrica es cero", "0",
				sinFicha.RendimientoDeFabricaDeLaFicha.ToString("0"));

			bitacora.CompletarFicha("ABC-123", "el Corolla", "Toyota", "Corolla", 2018,
				"gris", 18m);

			Vehiculo conFicha = bitacora.VehiculoDe("ABC-123");
			Igual("con ficha, el alias vuelve", "el Corolla", conFicha.AliasDeLaFicha);
			Igual("y la marca", "Toyota", conFicha.MarcaDeLaFicha);
			Igual("y el anio", "2018", conFicha.AnioDeLaFicha.ToString());
			Igual("y el rendimiento de fabrica", "18", conFicha.RendimientoDeFabricaDeLaFicha.ToString("0"));

			// Completar de nuevo CORRIGE: ponerle nombre a un carro casi siempre es
			// arreglar el que le pusiste antes.
			bitacora.CompletarFicha("ABC-123", "la nave", "Toyota", "Corolla", 2018, "gris", 0m);
			Igual("volver a completar corrige", "la nave",
				bitacora.VehiculoDe("ABC-123").AliasDeLaFicha);
			Igual("y sin rendimiento de fabrica vuelve a cero", "0",
				bitacora.VehiculoDe("ABC-123").RendimientoDeFabricaDeLaFicha.ToString("0"));
		}

		private static void LaSuposicionDelTanqueSeEnunciaAntesDePreguntar()
		{
			Titulo("lo que se pide confirmar se enuncia antes de preguntar");

			var bitacora = new BitacoraDeCargas();

			// Corte automatico: se supone que quedo lleno.
			bitacora.Agregar(new Carga(new DateTime(2026, 1, 10), "La Sabana", "A-3",
				"regular", 40m, "L", 615m, "CRC", 24600m, "CRC", "automatico",
				"ABC-123", "T-9001"));

			// Corte preseteado: se supone que NO quedo lleno.
			bitacora.Agregar(new Carga(new DateTime(2026, 2, 10), "La Sabana", "A-3",
				"regular", 20m, "L", 615m, "CRC", 12300m, "CRC", "preseteado",
				"ABC-123", "T-9002"));

			FaltanteDeDato delLleno = null, delParcial = null;
			foreach (FaltanteDeDato faltante in bitacora.FaltantesDe("ABC-123"))
			{
				if (faltante.QueFalta != "la confirmación del tanque") continue;
				if (faltante.NumeroDeLaTransaccion == "T-9001") delLleno = faltante;
				if (faltante.NumeroDeLaTransaccion == "T-9002") delParcial = faltante;
			}

			Igual("el faltante del tanque nace de una suposicion", "True",
				(delLleno != null && delLleno.NaceDeUnaSuposicion).ToString());

			Igual("y dice QUE se supone", "lleno", delLleno.LoQueSeSupone);
			Igual("y POR QUE lo supone",
				"el surtidor cortó solo, así que suponemos que el tanque quedó lleno",
				delLleno.LaSuposicion);

			// La simetrica: el mismo mecanismo tiene que decir lo contrario, o los dos
			// botones de la pantalla vuelven a ser genericos.
			Igual("con corte preseteado se supone lo contrario", "parcial",
				delParcial.LoQueSeSupone);
			Igual("y lo dice con sus palabras",
				"cargaste un monto fijo, así que suponemos que el tanque no quedó lleno",
				delParcial.LaSuposicion);

			// "tramo" es vocabulario del modelo y no tiene por que llegar a la pantalla.
			Igual("la frase no le habla de tramos", "False",
				delLleno.ParaSonia.Contains("tramo").ToString());

			// El kilometraje NO se infiere de nada: no hay suposicion que enunciar, y su
			// frase sigue siendo el pedido de siempre.
			FaltanteDeDato delKilometraje = null;
			foreach (FaltanteDeDato faltante in bitacora.FaltantesDe("ABC-123"))
				if (faltante.QueFalta == "el kilometraje") { delKilometraje = faltante; break; }

			Igual("el kilometraje no nace de una suposicion", "False",
				delKilometraje.NaceDeUnaSuposicion.ToString());
			Igual("y su frase sigue siendo el pedido", "True",
				delKilometraje.ParaSonia.StartsWith("necesito el kilometraje").ToString());
		}

		private static void UnKilometrajeFaltanteRompeDosTramosYDejaElLargo()
		{
			Titulo("un kilometraje faltante rompe dos tramos, y el largo se calcula igual");

			IReadOnlyList<Tramo> tramos = ElGuion().TramosDe("ABC-123");

			var bloqueados = new List<Tramo>();
			foreach (Tramo tramo in tramos)
				if (!tramo.EstaCalculado) bloqueados.Add(tramo);

			Igual("dos tramos finos bloqueados", "2", bloqueados.Count.ToString());
			Igual("los dos nombran la carga trunca", "True",
				(bloqueados[0].QueFalta.Contains("25/07/2026")
					&& bloqueados[1].QueFalta.Contains("25/07/2026")).ToString());

			IReadOnlyList<Tramo> calculados = Calculados(tramos);
			Tramo largo = calculados[calculados.Count - 1];
			Igual("el tramo largo absorbe: 820 km", "820", Formato(largo.Kilometros, "0"));
			Igual("  con los litros de las dos cargas siguientes", "87",
				Formato(largo.Litros, "0"));
			Igual("  y da la anomalia sostenida", "9.4",
				Formato(largo.RendimientoEnKmPorLitro, "0.#"));
			Igual("  comparable: sus litros los midio el surtidor de siempre", "True",
				largo.EsComparable.ToString());
		}

		private static void OtroSurtidorNoSeComparaConLaSerie()
		{
			Titulo("otro surtidor no se compara con la serie (§6.3.1)");

			IReadOnlyList<Tramo> calculados = Calculados(ElGuion().TramosDe("ABC-123"));
			Tramo enB1 = calculados[4];

			Igual("el tramo medido por B-1 no es comparable", "False",
				enB1.EsComparable.ToString());
			Igual("  y dice por que", "otro surtidor", enB1.MotivoDeReserva);
			Igual("  aunque por dentro sea consistente", "10.5",
				Formato(enB1.RendimientoEnKmPorLitro, "0.#"));
		}

		private static void ElVeredictoDelGuionEsEmpeoro()
		{
			Titulo("el veredicto del guion: empeoro contra la propia base");

			EvaluacionDeRendimiento evaluacion = ElGuion().EvaluarRendimientoDe("ABC-123");

			Igual("veredicto", "empeoro", evaluacion.NombreDelVeredicto);
			Igual("el actual es el tramo largo", "9.4",
				Formato(evaluacion.ActualKmPorLitro, "0.#"));
			Igual("el habitual es la mediana de los comparables", "12.6",
				Formato(evaluacion.HabitualKmPorLitro, "0.#"));
			Igual("un 25% menos: el titular del dashboard", "-25",
				evaluacion.CambioPorcentual.ToString());
			Igual("es semantico, no una flecha", "True", evaluacion.EstaEmpeorando.ToString());
			Igual("y pide algo con plazo", "revisalo con tu taller este mes",
				evaluacion.AccionLegible);
		}

		private static void CambiarLaUnidadInvierteLaFlechaPeroNoElVeredicto()
		{
			Titulo("cambiar la unidad invierte la flecha, pero no el veredicto (§5.3)");

			EvaluacionDeRendimiento evaluacion = ElGuion().EvaluarRendimientoDe("ABC-123");
			Rendimiento actual = Rendimiento.DeFabrica(evaluacion.ActualKmPorLitro);

			Igual("en km/L, mas es mejor", "mas es mejor",
				PresentacionDeRendimiento.KilometrosPorLitro.Direccion.Nombre);
			Igual("en L/100km, MENOS es mejor: la misma realidad al reves", "menos es mejor",
				PresentacionDeRendimiento.LitrosPorCienKilometros.Direccion.Nombre);

			Igual("9.4 km/L son 10.6 L/100km", "10.6 L/100km",
				PresentacionDeRendimiento.LitrosPorCienKilometros.Formatear(actual));
			Igual("y 29.6 mpg", "29.6 mpg",
				PresentacionDeRendimiento.MillasPorGalonUS.Formatear(
					Rendimiento.DeFabrica(evaluacion.HabitualKmPorLitro)));

			// La flecha se invierte: el mismo deterioro es "sube" en L/100km.
			Igual("bajar km/L es desfavorable", "False",
				PresentacionDeRendimiento.KilometrosPorLitro.Direccion.EsFavorable(-1m).ToString());
			Igual("bajar L/100km es favorable", "True",
				PresentacionDeRendimiento.LitrosPorCienKilometros.Direccion.EsFavorable(-1m).ToString());

			// El veredicto NO se invierte: se juzgo una sola vez, en canonico.
			Igual("el veredicto sigue siendo empeoro", "empeoro", evaluacion.NombreDelVeredicto);
		}

		// ------------------------------------------------- los veredictos sinteticos

		private static void MejorarTambienSeDice()
		{
			Titulo("mejorar tambien se dice");

			BitacoraDeCargas bitacora = SerieSintetica(10000m, 10400m, 10800m, 11280m);
			Igual("veredicto", "mejoro", bitacora.EvaluarRendimientoDe("ABC-123").NombreDelVeredicto);
		}

		private static void UnCambioChicoEsEstable()
		{
			Titulo("un cambio por debajo del minimo significativo es estable");

			BitacoraDeCargas bitacora = SerieSintetica(10000m, 10400m, 10800m, 11208m);
			EvaluacionDeRendimiento evaluacion = bitacora.EvaluarRendimientoDe("ABC-123");
			Igual("veredicto", "estable", evaluacion.NombreDelVeredicto);
			Igual("sin accion: decir algo aca es entrenar a ignorar avisos", "",
				evaluacion.AccionLegible);
		}

		private static void UnSoloTramoEsIndeterminado()
		{
			Titulo("un solo tramo es indeterminado: no hay serie");

			BitacoraDeCargas bitacora = SerieSintetica(10000m, 10400m);
			Igual("veredicto", "indeterminado",
				bitacora.EvaluarRendimientoDe("ABC-123").NombreDelVeredicto);
		}

		private static void SinDosLlenosNoHayTramoYSeDiceQueFalta()
		{
			Titulo("sin dos llenos no hay tramo, y se dice exactamente que falta");

			var bitacora = new BitacoraDeCargas();
			AgregarCarga(bitacora, "2026-08-01", 40m, "A-3", 615m, 24600m, "automatico", "T-1");
			bitacora.AnotarOdometro("T-1", 10000m, "km", "observado");
			bitacora.ConfirmarTanque("T-1");

			EvaluacionDeRendimiento evaluacion = bitacora.EvaluarRendimientoDe("ABC-123");
			Igual("veredicto", "sin tramo", evaluacion.NombreDelVeredicto);
			Igual("la primera carga nunca da rendimiento, y esta bien decirlo", "True",
				evaluacion.QueFalta.Contains("segunda carga").ToString());
		}

		private static void ElRendimientoImplausibleEsDatoSospechoso()
		{
			Titulo("el rendimiento implausible es un problema de datos, no de la realidad (§7.2)");

			// El ultimo tramo da 20 km/L contra una base de 10: quedaron los kilometros
			// de dos tanques con los litros de uno.
			BitacoraDeCargas bitacora = SerieSintetica(10000m, 10400m, 10800m, 11600m);
			EvaluacionDeRendimiento evaluacion = bitacora.EvaluarRendimientoDe("ABC-123");

			Igual("veredicto", "dato sospechoso", evaluacion.NombreDelVeredicto);
			Igual("sugiere registrar la carga que falto, NO alarma", "True",
				evaluacion.SugiereRegistrarLaCargaQueFalto.ToString());
			Igual("la accion lo dice", "registrá la carga que faltó esta semana",
				evaluacion.AccionLegible);
		}

		private static void CambiarDeCombustibleNoEsComparable()
		{
			Titulo("cambiar de combustible hace el tramo no comparable");

			BitacoraDeCargas bitacora = SerieSintetica(10000m, 10400m, 10800m);
			AgregarCarga(bitacora, "2026-04-01", 40m, "A-3", 615m, 24600m, "automatico",
				"T-4", tipo: "diesel");
			bitacora.AnotarOdometro("T-4", 11200m, "km", "observado");
			bitacora.ConfirmarTanque("T-4");

			EvaluacionDeRendimiento evaluacion = bitacora.EvaluarRendimientoDe("ABC-123");
			Igual("veredicto", "no comparable", evaluacion.NombreDelVeredicto);
			Igual("y dice por que", "cambio de combustible dentro del tramo",
				evaluacion.MotivoDeReserva);
		}

		private static void ElTotalQueNoCuadraSeReporta()
		{
			Titulo("el total es autoritativo, y cuando no cuadra se reporta");

			var bitacora = new BitacoraDeCargas();
			AgregarCarga(bitacora, "2026-08-01", 40m, "A-3", 615m, 24600m, "automatico", "T-1");
			// 40 L a 615 son 24 600; el total dice 30 000: mas alla de todo redondeo.
			AgregarCarga(bitacora, "2026-08-10", 40m, "A-3", 615m, 30000m, "automatico", "T-2");

			Igual("la carga lo sabe", "False",
				bitacora.CargaDe("T-2").ElTotalCuadra.ToString());
			Igual("y el veredicto lo dice antes que cualquier lectura", "total no cuadra",
				bitacora.EvaluarRendimientoDe("ABC-123").NombreDelVeredicto);

			// El redondeo del surtidor NO dispara el reporte: la tolerancia existe.
			Igual("un redondeo chico si cuadra", "True",
				bitacora.CargaDe("T-1").ElTotalCuadra.ToString());
		}

		private static void ElOdometroNoRetrocede()
		{
			Titulo("el odometro es monotono creciente");

			BitacoraDeCargas bitacora = ElGuion();

			Igual("la guarda decide sin reventar", "False",
				bitacora.PuedeAnotarOdometro("T-0209", 41000m, "km").ToString());
			Revienta<ErrorDeDominio>("y la operacion revienta: es la segunda linea",
				() => bitacora.AnotarOdometro("T-0209", 41000m, "km", "observado"));
			Igual("uno coherente si puede", "True",
				bitacora.PuedeAnotarOdometro("T-0208", 42800m, "km").ToString());
		}

		// ------------------------------------------------------------- lo derivado

		private static void LaCapacidadYLaAutonomiaSonDerivadasDeclaradas()
		{
			Titulo("la capacidad y la autonomia son derivadas, y se declaran");

			BitacoraDeCargas bitacora = ElGuion();

			CapacidadEstimadaDeTanque capacidad = bitacora.CapacidadDe("ABC-123");
			Igual("la capacidad sale de la carga maxima con lleno", "44",
				Formato(capacidad.Litros, "0"));
			Igual("  y dice que carga la fijo", "25/08/2026",
				capacidad.FechaDeLaCargaQueLaFijo.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture));

			AutonomiaEstimada autonomia = bitacora.AutonomiaDe("ABC-123");
			Igual("la autonomia combina capacidad y habitual", "553",
				Formato(autonomia.Kilometros, "0"));
			Igual("  y SIEMPRE es proyeccion", "True", autonomia.EsProyeccion.ToString());

			// Sin linea base no hay autonomia, y se dice.
			var sinBase = new BitacoraDeCargas();
			AgregarCarga(sinBase, "2026-08-01", 40m, "A-3", 615m, 24600m, "automatico", "T-1");
			sinBase.ConfirmarTanque("T-1");
			Igual("sin habitual no se inventa", "False",
				sinBase.AutonomiaDe("ABC-123").Hay.ToString());
		}

		private static void ElOdometroEstimadoProponeYNuncaEscribe()
		{
			Titulo("el odometro estimado propone; nunca escribe solo");

			BitacoraDeCargas bitacora = ElGuion();

			// El prellenado de la carga trunca: lo que la app le hubiera propuesto a
			// Sonia el dia de la carga 8.
			ProyeccionDeOdometro propuesta = bitacora.OdometroEstimadoDe("ABC-123",
				new DateTime(2026, 7, 25));
			Igual("propone ~42 823 km", "42823", Formato(propuesta.Kilometros, "0"));
			Igual("  con el promedio del que salio", "12.6",
				Formato(propuesta.KilometrosPorDia, "0.#"));
			Igual("  y su origen es estimado", "estimado", propuesta.Origen);

			// Hoy, con la lectura del dia, la propuesta ES la lectura.
			Igual("al dia de la ultima lectura propone esa lectura", "43200",
				Formato(bitacora.OdometroEstimadoDe("ABC-123", Hoy).Kilometros, "0"));
		}

		private static void LaCompletitudYLoQueFaltaMotivanElIngreso()
		{
			Titulo("la completitud y lo que falta: mostrar el valor bloqueado, no pedir el dato");

			BitacoraDeCargas bitacora = ElGuion();

			Igual("la linea discreta", "8 de 9 cargas con kilometraje · 6 tramos calculables",
				bitacora.CompletitudDe("ABC-123").Expresada);

			IReadOnlyList<FaltanteDeDato> faltantes = bitacora.FaltantesDe("ABC-123");
			Igual("un solo faltante", "1", faltantes.Count.ToString());
			Igual("nombra la carga y lo que se desbloquea", "True",
				(faltantes[0].ParaSonia.Contains("25/07/2026")
					&& faltantes[0].ParaSonia.Contains("820 km")).ToString());
		}

		private static void AgregarElKilometrajeParteElTramoLargo()
		{
			Titulo("agregar el kilometraje parte el tramo largo en dos: el refuerzo del ingreso");

			BitacoraDeCargas bitacora = ElGuion();
			bitacora.AnotarOdometro("T-0208", 42800m, "km", "confirmado");

			IReadOnlyList<Tramo> tramos = bitacora.TramosDe("ABC-123");
			Igual("siete calculados y cero bloqueados", "7 0", Conteo(tramos));

			IReadOnlyList<Tramo> calculados = Calculados(tramos);
			Igual("el tramo hasta la carga trunca aparece", "9.8",
				Formato(calculados[5].RendimientoEnKmPorLitro, "0.#"));
			Igual("y el que sigue tambien", "9.1",
				Formato(calculados[6].RendimientoEnKmPorLitro, "0.#"));
			Igual("la anomalia sostenida no cambia de historia", "empeoro",
				bitacora.EvaluarRendimientoDe("ABC-123").NombreDelVeredicto);
			Igual("y ya no falta nada", "0",
				bitacora.FaltantesDe("ABC-123").Count.ToString());
		}

		private static void ElTanqueSeConfirmaNoSeConvence()
		{
			Titulo("el tanque se confirma, no se convence: la inferencia hizo la mitad");

			var bitacora = new BitacoraDeCargas();
			AgregarCarga(bitacora, "2026-08-01", 40m, "A-3", 615m, 24600m, "automatico", "T-1");

			Carga carga = bitacora.CargaDe("T-1");
			Igual("la carga nace con la inferencia del corte", "lleno (estimado)",
				carga.TanqueExpresado);
			Igual("hay una inferencia esperando", "True",
				bitacora.PuedeConfirmarTanque("T-1").ToString());

			bitacora.ConfirmarTanque("T-1");
			Igual("el visto bueno la sube a confirmado", "lleno (confirmado)",
				carga.TanqueExpresado);
			Igual("y recien ahi cuenta para el calculo", "True",
				carga.Tanque.EsLlenoQueCuenta.ToString());

			Igual("ya no hay nada que confirmar", "False",
				bitacora.PuedeConfirmarTanque("T-1").ToString());
			Revienta<ErrorDeDominio>("confirmar dos veces es un bug del llamador",
				() => bitacora.ConfirmarTanque("T-1"));

			// Sonia tambien puede corregir: declara directo, con su palabra.
			bitacora.DeclararTanque("T-1", "parcial");
			Igual("la declaracion directa pisa la inferencia", "parcial (observado)",
				carga.TanqueExpresado);
		}

		private static void LaLealtadLlegaPorEventoYNoEsElFoco()
		{
			Titulo("la lealtad llega por evento y no es el foco");

			BitacoraDeCargas bitacora = ElGuion();

			Igual("el saldo es la suma de lo acreditado", "326",
				bitacora.Lealtad.PuntosDelSaldo.ToString());
			Igual("el nivel lo dijo la institucion", "plata", bitacora.Lealtad.NombreDelNivel);
			Igual("y se sabe cuanto falta para el siguiente", "474",
				bitacora.Lealtad.PuntosParaElSiguienteNivel.ToString());
		}

		// ------------------------------------------------------------- las fronteras

		private static void ElOrdenDeLlegadaNoImporta()
		{
			Titulo("el orden de llegada no importa: la serie ordena por fecha");

			var bitacora = new BitacoraDeCargas();
			// Las mismas tres anclas de la serie sintetica, agregadas al reves.
			AgregarCarga(bitacora, "2026-03-01", 40m, "A-3", 615m, 24600m, "automatico", "T-3");
			AgregarCarga(bitacora, "2026-01-01", 40m, "A-3", 615m, 24600m, "automatico", "T-1");
			AgregarCarga(bitacora, "2026-02-01", 40m, "A-3", 615m, 24600m, "automatico", "T-2");
			bitacora.AnotarOdometro("T-1", 10000m, "km", "observado");
			bitacora.AnotarOdometro("T-2", 10400m, "km", "observado");
			bitacora.AnotarOdometro("T-3", 10800m, "km", "observado");
			foreach (string transaccion in new[] { "T-1", "T-2", "T-3" })
				bitacora.ConfirmarTanque(transaccion);

			Igual("dos tramos como si hubieran llegado en orden", "2 0",
				Conteo(bitacora.TramosDe("ABC-123")));
			Igual("con los rendimientos correctos", "10",
				Formato(Calculados(bitacora.TramosDe("ABC-123"))[0].RendimientoEnKmPorLitro, "0.#"));
		}

		private static void LasLecturasGuardadasRevientanYLasDeReconocimientoDeciden()
		{
			Titulo("las lecturas guardadas revientan; las de reconocimiento deciden");

			BitacoraDeCargas bitacora = ElGuion();

			Igual("ConoceTransaccion decide", "False",
				bitacora.ConoceTransaccion("T-9999").ToString());
			Revienta<ErrorDeDominio>("CargaDe revienta: devolver nada mentiria",
				() => bitacora.CargaDe("T-9999"));

			Igual("ConocePlaca decide", "False", bitacora.ConocePlaca("ZZZ-999").ToString());
			Revienta<ErrorDeDominio>("VehiculoDe revienta",
				() => bitacora.VehiculoDe("ZZZ-999"));
		}

		private static void LaTransaccionDuplicadaEsUnDefectoDelEmisor()
		{
			Titulo("dos hechos con la misma transaccion son un defecto del emisor");

			var bitacora = new BitacoraDeCargas();
			AgregarCarga(bitacora, "2026-08-01", 40m, "A-3", 615m, 24600m, "automatico", "T-1");

			Revienta<ErrorDeDominio>("la segunda con la misma identidad revienta",
				() => AgregarCarga(bitacora, "2026-08-02", 38m, "A-3", 615m, 23370m,
					"automatico", "T-1"));
		}

		private static void TodoConsejoConPlazoYElRojoReservado()
		{
			Titulo("todo consejo con plazo, y el rojo esta reservado");

			Igual("la accion viene dicha entera", "anotá el kilometraje en tu próxima carga",
				Accion.AnotarElKilometraje(Plazo.EnTuProximaCarga).Legible);

			Revienta<ArgumentNullException>(
				"una severidad que pide algo sin accion no se puede construir",
				() => new EvaluacionDeRendimiento(VeredictoDeRendimiento.Empeoro,
					Severidad.Vigilar, null, 9m, 12m, -0.25m, 5, 2, true, "", "", ""));

			// Un carro que rinde menos no es una emergencia: el rojo no se gasta aca.
			Igual("el empeoro del guion pide vigilar, no urgente", "vigilar",
				ElGuion().EvaluarRendimientoDe("ABC-123").NombreDeLaSeveridad);
			Igual("urgente sigue siendo el mas alto, reservado", "True",
				Severidad.Urgente.EsMasUrgenteQue(Severidad.ConsultePronto).ToString());
		}

		private static void LaFichaLeDaNombreAlVehiculo()
		{
			Titulo("la ficha le da nombre al vehiculo, cuando Sonia quiere");

			BitacoraDeCargas bitacora = ElGuion();

			Igual("sin ficha, la placa", "ABC123",
				bitacora.VehiculoDe("ABC-123").NombreParaMostrar);

			bitacora.CompletarFicha("ABC-123", "el Corolla", "Toyota", "Corolla", 2019,
				"gris", 18m);
			Vehiculo vehiculo = bitacora.VehiculoDe("ABC-123");
			Igual("con ficha, el alias", "el Corolla", vehiculo.NombreParaMostrar);
			Igual("y el manual dice 18: la otra mitad del cruce con el concesionario",
				"18 km/L", vehiculo.Ficha.RendimientoDeFabrica.Valor.Expresado);

			// El cero del formulario es "no lo se", y se escala al tipo de ausencia.
			bitacora.CompletarFicha("ABC-123", "el Corolla", "Toyota", "Corolla", 2019,
				"gris", 0m);
			Igual("sin rendimiento de fabrica, el tipo lo dice", "False",
				bitacora.VehiculoDe("ABC-123").Ficha.RendimientoDeFabrica.Existe.ToString());
		}

		// -------------------------------------------------------------------- el guion

		// El guion del brief §13 en dominio puro: las nueve cargas de la institucion con
		// su forma de corte, y los aportes de Sonia. Es el MISMO guion de Muestras, sin
		// pasar por el wire: aca se mide el modelo, alla la frontera.
		private static BitacoraDeCargas ElGuion()
		{
			var bitacora = new BitacoraDeCargas();

			AgregarCarga(bitacora, "2025-12-15", 40m, "A-3", 615m, 24600m, "automatico", "T-0201");
			AgregarCarga(bitacora, "2026-01-15", 38m, "A-3", 620m, 23560m, "automatico", "T-0202");
			AgregarCarga(bitacora, "2026-02-15", 39m, "A-3", 628m, 24492m, "automatico", "T-0203");
			AgregarCarga(bitacora, "2026-03-20", 20m, "A-3", 634m, 12680m, "preseteado", "T-0204");
			AgregarCarga(bitacora, "2026-03-28", 19m, "A-3", 634m, 12046m, "automatico", "T-0205");
			AgregarCarga(bitacora, "2026-05-20", 41m, "A-3", 645m, 26445m, "automatico", "T-0206");
			AgregarCarga(bitacora, "2026-06-20", 42m, "B-1", 652m, 27384m, "automatico", "T-0207");
			AgregarCarga(bitacora, "2026-07-25", 43m, "A-3", 660m, 28380m, "automatico", "T-0208");
			AgregarCarga(bitacora, "2026-08-25", 44m, "A-3", 668m, 29392m, "automatico", "T-0209");

			foreach (int puntos in new[] { 40, 38, 39, 20, 19, 41, 42, 43, 44 })
				bitacora.AcreditarPuntos(puntos);
			bitacora.ActualizarNivel("plata");

			foreach ((string transaccion, decimal kilometros) in Muestras.Kilometrajes)
				bitacora.AnotarOdometro(transaccion, kilometros, "km", "observado");
			foreach (string transaccion in Muestras.TanquesPorConfirmar)
				bitacora.ConfirmarTanque(transaccion);

			return bitacora;
		}

		// Tres anclas (o mas) parejas para las series sinteticas: 40 L por tramo, y el
		// rendimiento lo deciden los kilometrajes que se pasen.
		private static BitacoraDeCargas SerieSintetica(params decimal[] kilometrajes)
		{
			var bitacora = new BitacoraDeCargas();
			for (int i = 0; i < kilometrajes.Length; i++)
			{
				string transaccion = $"T-{i + 1}";
				string fecha = new DateTime(2026, 1, 1).AddMonths(i)
					.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
				AgregarCarga(bitacora, fecha, 40m, "A-3", 615m, 24600m, "automatico", transaccion);
				bitacora.AnotarOdometro(transaccion, kilometrajes[i], "km", "observado");
				bitacora.ConfirmarTanque(transaccion);
			}
			return bitacora;
		}

		private static void AgregarCarga(BitacoraDeCargas bitacora, string fecha,
			decimal litros, string surtidor, decimal precio, decimal total, string corte,
			string transaccion, string tipo = "regular")
		{
			bitacora.Agregar(new Carga(
				DateTime.ParseExact(fecha, "yyyy-MM-dd", CultureInfo.InvariantCulture),
				"La Sabana", surtidor, tipo, litros, "L", precio, "CRC", total, "CRC",
				corte, "ABC-123", transaccion));
		}

		private static string Conteo(IReadOnlyList<Tramo> tramos)
		{
			int calculados = 0, bloqueados = 0;
			foreach (Tramo tramo in tramos)
			{
				if (tramo.EstaCalculado) calculados++;
				else bloqueados++;
			}
			return $"{calculados} {bloqueados}";
		}

		private static IReadOnlyList<Tramo> Calculados(IReadOnlyList<Tramo> tramos)
		{
			var calculados = new List<Tramo>();
			foreach (Tramo tramo in tramos)
				if (tramo.EstaCalculado) calculados.Add(tramo);
			return calculados;
		}

		private static string Formato(decimal valor, string formato) =>
			valor.ToString(formato, CultureInfo.InvariantCulture);

		// ------------------------------------------------------------------ el arnes

		private static void Titulo(string texto)
		{
			Console.WriteLine();
			Console.WriteLine($"-- {texto} --");
		}

		private static void Igual(string titulo, string esperado, string obtenido)
		{
			corridas++;
			bool ok = esperado == obtenido;
			if (!ok) fallas++;
			Console.WriteLine($"  [{(ok ? "OK  " : "FALLA")}] {titulo}");
			if (!ok) Console.WriteLine($"           esperado=[{esperado}] obtenido=[{obtenido}]");
		}

		private static void Revienta<T>(string titulo, Action accion) where T : Exception
		{
			corridas++;
			try
			{
				accion();
				fallas++;
				Console.WriteLine($"  [FALLA] {titulo}");
				Console.WriteLine($"           no revento (se esperaba {typeof(T).Name})");
			}
			catch (T)
			{
				Console.WriteLine($"  [OK  ] {titulo}");
			}
			catch (Exception ex)
			{
				fallas++;
				Console.WriteLine($"  [FALLA] {titulo}");
				Console.WriteLine($"           revento con {ex.GetType().Name}, se esperaba {typeof(T).Name}");
			}
		}
	}
}
