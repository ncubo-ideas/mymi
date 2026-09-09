using System;
using NUnit.Framework;

namespace Municipalidad.Dominio.Pruebas
{
	// G3: PonerAlCobro ES EL COMANDO QUE NUNCA SE HABIA EJERCIDO, y este es su nivel A.
	//
	// POR QUE SE ESCONDIO. AgregarPendiente acuna un Cobro como efecto -hueco 11- asi que
	// VencimientosAl y AtrasosAl tenian con que contestar y el lado de las consultas se veia
	// cubierto. El lado del comando no habia corrido nunca: ni el metodo, ni el script.
	//
	// LO QUE ESTE ARCHIVO MIDE es el predicado total de PuedePonerAlCobro, en el ORDEN en que
	// pregunta -- nombre, periodo, monto, duplicado -- y las dos consecuencias observables: que un
	// Cobro puesto se ve como vencimiento o como atraso segun la fecha de corte, y que los DOS
	// caminos por los que un Cobro entra comparten la misma guarda.
	[TestFixture]
	public sealed class ACobroEntersOnlyOnceAndOnlyForAPeriodItsRubroCanRead
	{
		private static readonly DateTime ElDiaQueSePuso = new DateTime(2025, 10, 1);

		// EL NOMBRE SE PREGUNTA PRIMERO, y por eso el resolutor no se alcanza nunca desde aca. Es
		// el argumento de DEC-01 -- que este modulo no necesita una Frontera -- hecho medible: la
		// negativa es la del dominio y NO la ArgumentOutOfRangeException de Rubro.PorNombre.
		[Test]
		public void AnUnknownRubroIsRefusedBeforeTheResolverIsEverReached()
		{
			Cuenta cuenta = new Cuenta();

			Assert.Multiple(() =>
			{
				Assert.That(cuenta.PuedePonerAlCobro("cementerio", 2025, 10, 8000m), Is.False,
					"Cuenta.PuedePonerAlCobro, con un rubro del menu del portal que este v1 no modela");

				Assert.That(cuenta.PuedePonerAlCobro("", 2025, 10, 8000m), Is.False, "un nombre vacio");

				DomainException negativa = Assert.Throws<DomainException>(
					() => cuenta.PonerAlCobro("cementerio", 2025, 10, 8000m, ElDiaQueSePuso),
					"Cuenta.PonerAlCobro");

				Assert.That(negativa.Message, Does.Contain("PuedePonerAlCobro"),
					"La negativa nombra el predicado que hay que preguntar antes.");

				Assert.That(cuenta.CuantosCobros, Is.EqualTo(0), "Y no entro nada.");
			});
		}

		// LOS DOS ALFABETOS DEL PERIODO, EN EL LADO DEL COMANDO. El mismo "2025.05" es mayo para un
		// servicio y NO EXISTE para bienes inmuebles, que tiene cuatro trimestres. Es la cadencia
		// del rubro la que lo decide, y el dominio no adivina.
		[Test]
		public void TheCadenceOfTheRubroDecidesWhetherTheNumberIsAPeriod()
		{
			Cuenta cuenta = new Cuenta();

			Assert.Multiple(() =>
			{
				Assert.That(cuenta.PuedePonerAlCobro("recoleccion-de-basura", 2025, 5, 8000m), Is.True,
					"mayo, para un rubro mensual");

				Assert.That(cuenta.PuedePonerAlCobro("bienes-inmuebles", 2025, 5, 36000m), Is.False,
					"el quinto trimestre, que no existe");

				Assert.That(cuenta.PuedePonerAlCobro("bienes-inmuebles", 2025, 4, 36000m), Is.True,
					"el cuarto trimestre, que si");

				Assert.That(cuenta.PuedePonerAlCobro("recoleccion-de-basura", 2025, 13, 8000m), Is.False,
					"el mes trece");

				Assert.That(cuenta.PuedePonerAlCobro("recoleccion-de-basura", 1999, 5, 8000m), Is.False,
					"un anio fuera de lo verosimil");

				Assert.Throws<DomainException>(
					() => cuenta.PonerAlCobro("bienes-inmuebles", 2025, 5, 36000m, ElDiaQueSePuso),
					"Cuenta.PonerAlCobro, con un trimestre que no existe");
			});
		}

		[Test]
		public void AnAmountThatIsNotPositiveIsRefused()
		{
			Cuenta cuenta = new Cuenta();

			Assert.Multiple(() =>
			{
				Assert.That(cuenta.PuedePonerAlCobro("recoleccion-de-basura", 2025, 10, 0m), Is.False,
					"cero");
				Assert.That(cuenta.PuedePonerAlCobro("recoleccion-de-basura", 2025, 10, -1m), Is.False,
					"negativo");

				Assert.Throws<DomainException>(
					() => cuenta.PonerAlCobro("recoleccion-de-basura", 2025, 10, 0m, ElDiaQueSePuso),
					"Cuenta.PonerAlCobro");

				Assert.That(cuenta.CuantosCobros, Is.EqualTo(0), "Y no entro nada.");
			});
		}

		// REGLA 18: la municipalidad puso ese periodo al cobro una vez. El mismo aviso otra vez es
		// el mismo hecho otra vez, y un replay lo repite siempre.
		[Test]
		public void TheSamePeriodIsNeverPutOnChargeTwice()
		{
			Cuenta cuenta = new Cuenta();
			cuenta.PonerAlCobro("recoleccion-de-basura", 2025, 10, 8000m, ElDiaQueSePuso);

			Assert.Multiple(() =>
			{
				Assert.That(cuenta.CuantosCobros, Is.EqualTo(1), "Cuenta.PonerAlCobro entro una vez");

				Assert.That(cuenta.PuedePonerAlCobro("recoleccion-de-basura", 2025, 10, 8000m), Is.False,
					"el mismo periodo, el mismo monto");
				Assert.That(cuenta.PuedePonerAlCobro("recoleccion-de-basura", 2025, 10, 9000m), Is.False,
					"el mismo periodo con OTRO monto: la identidad es rubro y periodo, no el monto");

				Assert.Throws<DomainException>(
					() => cuenta.PonerAlCobro("recoleccion-de-basura", 2025, 10, 9000m, ElDiaQueSePuso),
					"Cuenta.PonerAlCobro, repetido");

				Assert.That(cuenta.CuantosCobros, Is.EqualTo(1), "Y sigue habiendo uno.");

				Assert.That(cuenta.PuedePonerAlCobro("recoleccion-de-basura", 2025, 11, 8000m), Is.True,
					"El periodo siguiente del mismo rubro si entra.");
				Assert.That(cuenta.PuedePonerAlCobro("servicio-policia", 2025, 10, 6100m), Is.True,
					"Y el mismo periodo de otro rubro tambien.");
			});
		}

		// HUECO 11, QUE ES POR LO QUE G3 SE ESCONDIO: un Cobro entra por DOS caminos -- este comando,
		// y AgregarPendiente como efecto -- y los dos pasan por la misma guarda. Ninguna prueba veia
		// esto, y es justo la razon por la que el lado de las consultas se veia cubierto.
		[Test]
		public void ACobroReachesTheAccountByTwoPathsAndTheGuardIsShared()
		{
			DateTime laConsulta = new DateTime(2025, 10, 3);

			Cuenta porElPendiente = new Cuenta();
			porElPendiente.RegistrarConsulta(laConsulta);
			porElPendiente.AgregarPendiente(laConsulta, "recoleccion-de-basura", 2025, 10, 8000m, 45m);

			Cuenta porElComando = new Cuenta();
			porElComando.PonerAlCobro("recoleccion-de-basura", 2025, 10, 8000m, ElDiaQueSePuso);
			porElComando.RegistrarConsulta(laConsulta);

			Assert.Multiple(() =>
			{
				Assert.That(porElPendiente.CuantosCobros, Is.EqualTo(1),
					"AgregarPendiente acuno un Cobro: es el hueco 11.");

				Assert.That(porElPendiente.PuedePonerAlCobro("recoleccion-de-basura", 2025, 10, 8000m),
					Is.False,
					"Y por eso PonerAlCobro lo rechaza: el periodo ya estaba puesto, por el otro camino.");

				Assert.That(porElComando.CuantosCobros, Is.EqualTo(1), "Cuenta.PonerAlCobro");

				porElComando.AgregarPendiente(laConsulta, "recoleccion-de-basura", 2025, 10, 8000m, 45m);

				Assert.That(porElComando.CuantosCobros, Is.EqualTo(1),
					"Y al reves tambien: el pendiente NO acuna un segundo Cobro para un periodo que "
					+ "ya estaba puesto. Una sola lista, una sola guarda.");
			});
		}

		// LA CONSECUENCIA OBSERVABLE, que es lo unico que prueba que el Cobro entro de verdad y no
		// solo que la llamada no revento: lo puesto al cobro y no pagado se lee como vencimiento
		// antes de la fecha de corte y como atraso despues. La fecha de corte entra por parametro:
		// el dominio no lee el reloj.
		[Test]
		public void WhatEntersIsReadAsAVencimientoOrAnAtrasoDependingOnTheCutDate()
		{
			Cuenta cuenta = new Cuenta();
			cuenta.PonerAlCobro("recoleccion-de-basura", 2025, 10, 8000m, ElDiaQueSePuso);

			// Mes(2025, 10) vence el 31 de octubre.
			DateTime antesDeQueVenza = new DateTime(2025, 10, 15);
			DateTime despuesDeQueVenza = new DateTime(2025, 11, 5);

			Assert.Multiple(() =>
			{
				Assert.That(cuenta.VencimientosAl(antesDeQueVenza), Has.Count.EqualTo(1),
					"Cuenta.VencimientosAl, antes del ultimo dia del periodo");
				Assert.That(cuenta.AtrasosAl(antesDeQueVenza), Is.Empty,
					"Cuenta.AtrasosAl, antes del ultimo dia del periodo");

				Assert.That(cuenta.VencimientosAl(despuesDeQueVenza), Is.Empty,
					"Cuenta.VencimientosAl, despues");
				Assert.That(cuenta.AtrasosAl(despuesDeQueVenza), Has.Count.EqualTo(1),
					"Cuenta.AtrasosAl, despues");

				VencimientoProximo elVencimiento = cuenta.VencimientosAl(antesDeQueVenza)[0];

				Assert.That(elVencimiento.NombreDelRubro, Is.EqualTo("recoleccion-de-basura"),
					"y nombra SU propio rubro");
				Assert.That(elVencimiento.EtiquetaDelPeriodo, Is.EqualTo("2025-10"),
					"y SU propio periodo");
				Assert.That(elVencimiento.Monto, Is.EqualTo(8000m), "y el monto que se puso");
				Assert.That(elVencimiento.Vence, Is.EqualTo(new DateTime(2025, 10, 31)),
					"y vence el ultimo dia del mes que cierra el periodo");
			});
		}
	}
}
