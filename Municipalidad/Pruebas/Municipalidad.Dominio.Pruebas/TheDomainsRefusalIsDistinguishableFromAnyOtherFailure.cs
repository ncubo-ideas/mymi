using System;
using System.IO;
using NUnit.Framework;

namespace Municipalidad.Dominio.Pruebas
{
	// LO QUE G2 PIDE, MEDIDO Y NO RECORDADO. Antes de M-1 el dominio negaba con seis formas
	// distintas -- InvalidOperationException, KeyNotFoundException, ArgumentOutOfRangeException,
	// ArgumentNullException, ArgumentException y ArgumentException.ThrowIfNullOrWhiteSpace -- asi
	// que no habia un nombre con el que una caratula pudiera reconocer la negativa, y una guarda
	// que no es DomainException se escapa de Apply y para el bloque entero del refresh.
	//
	// LAS DOS MITADES SE MIDEN APARTE, y por eso este archivo tiene dos censos y no uno:
	//
	//   1. toda negativa POR EL ESTADO DEL REGISTRO lleva el tipo del dominio -- los dos censos;
	//   2. todo defecto de QUIEN LLAMA sigue sin llevarlo -- AProgrammerError...
	//
	// La segunda es la que hace que esto no sea una conversion mecanica: un barrido que hubiera
	// cambiado las seis formas a la vez la pone roja. La distincion misma es DEC-08.
	[TestFixture]
	public sealed class TheDomainsRefusalIsDistinguishableFromAnyOtherFailure
	{
		private static readonly DateTime LaConsulta = new DateTime(2025, 9, 3);
		private static readonly DateTime ElValorDeLaFinca = new DateTime(2025, 1, 15);
		private static readonly DateTime LaAdopcion = new DateTime(2025, 8, 1);

		// UNA CUENTA CON LOS OCHO COMANDOS YA EJERCIDOS, para que cada uno pueda ser negado por
		// el registro y no por un argumento. F-2 entra sin valor declarado a proposito: es lo que
		// niega a Finca.ValorVigenteAl.
		private static Cuenta UnaCuentaConHistoria()
		{
			Cuenta cuenta = new Cuenta();
			cuenta.IncorporarFinca("F-1");
			cuenta.IncorporarFinca("F-2");
			cuenta.DeclararValorDeFinca("F-1", ElValorDeLaFinca, 60000000m);
			cuenta.RegistrarPago("R-1", new DateTime(2024, 3, 31), 0m);
			cuenta.AgregarLineaAlPago("R-1", "recoleccion-de-basura", 2024, 3, 5000m, 0m);
			cuenta.PonerAlCobro("recoleccion-de-basura", 2024, 4, 5000m, new DateTime(2024, 4, 1));
			cuenta.RegistrarConsulta(LaConsulta);
			cuenta.AgregarPendiente(LaConsulta, "servicio-policia", 2025, 9, 18300m, 34m);
			cuenta.AdoptarReserva(30000m, LaAdopcion);
			return cuenta;
		}

		// EL CENSO DE LAS MUTACIONES: las once guardas que niegan una escritura. Ocho son los
		// comandos de Cuenta; las otras tres viven en las entidades hijas y solo se alcanzan
		// nombrandolas, que es lo que este proyecto puede hacer y una caratula no.
		[Test]
		public void EveryMutationTheRecordRefusesRaisesTheDomainsOwnRefusal()
		{
			Cuenta cuenta = UnaCuentaConHistoria();

			Assert.Multiple(() =>
			{
				Assert.Throws<DomainException>(
					() => cuenta.IncorporarFinca("F-1"),
					"Cuenta.IncorporarFinca");

				Assert.Throws<DomainException>(
					() => cuenta.DeclararValorDeFinca("F-1", ElValorDeLaFinca, 70000000m),
					"Cuenta.DeclararValorDeFinca");

				Assert.Throws<DomainException>(
					() => cuenta.PonerAlCobro("recoleccion-de-basura", 2024, 4, 5000m, new DateTime(2024, 4, 1)),
					"Cuenta.PonerAlCobro");

				Assert.Throws<DomainException>(
					() => cuenta.RegistrarPago("R-1", new DateTime(2024, 3, 31), 0m),
					"Cuenta.RegistrarPago");

				Assert.Throws<DomainException>(
					() => cuenta.AgregarLineaAlPago("R-1", "recoleccion-de-basura", 2024, 3, 5000m, 0m),
					"Cuenta.AgregarLineaAlPago");

				Assert.Throws<DomainException>(
					() => cuenta.RegistrarConsulta(LaConsulta),
					"Cuenta.RegistrarConsulta");

				Assert.Throws<DomainException>(
					() => cuenta.AgregarPendiente(LaConsulta, "servicio-policia", 2025, 9, 18300m, 34m),
					"Cuenta.AgregarPendiente");

				Assert.Throws<DomainException>(
					() => cuenta.AdoptarReserva(30000m, LaAdopcion),
					"Cuenta.AdoptarReserva");

				Assert.Throws<DomainException>(
					() => cuenta.FincaNumero("F-1").DeclararValor(new ValorDeFinca(new DateTime(2024, 6, 1), 50000000m)),
					"Finca.DeclararValor");

				Assert.Throws<DomainException>(
					() => cuenta.PagoNumero("R-1").AgregarLinea(
						new LineaPagada(Rubro.Basura(), new Mes(2024, 3), 5000m, 0m, 1, false)),
					"Pago.AgregarLinea");

				Assert.Throws<DomainException>(
					() => cuenta.ConsultaDel(LaConsulta).Agregar(
						new LineaPendiente(Rubro.Policia(), new Mes(2025, 9), 18300m, 34m)),
					"Consulta.Agregar");
			});
		}

		// EL CENSO DE LAS LECTURAS GUARDADAS: las nueve que niegan una respuesta porque el
		// registro no tiene con que darla. Cuatro de ellas necesitan una cuenta VACIA, porque
		// sobre una con historia contestarian.
		//
		// LOS TRES PRIMEROS SE MIDEN POR EL MENSAJE ADEMAS DEL TIPO (C60): son la misma forma
		// sobre tres registros distintos, asi que Throws<DomainException> por si solo pasaria con
		// la excepcion de cualquiera de los otros dos.
		[Test]
		public void EveryGuardedReadTheRecordCannotAnswerRaisesTheDomainsOwnRefusal()
		{
			Cuenta cuenta = UnaCuentaConHistoria();
			Cuenta vacia = new Cuenta();

			Assert.Multiple(() =>
			{
				Assert.That(
					Assert.Throws<DomainException>(() => cuenta.FincaNumero("F-9"), "Cuenta.FincaNumero").Message,
					Does.Contain("finca"), "Cuenta.FincaNumero nombra su propio asunto");

				Assert.That(
					Assert.Throws<DomainException>(() => cuenta.PagoNumero("R-9"), "Cuenta.PagoNumero").Message,
					Does.Contain("recibo"), "Cuenta.PagoNumero nombra su propio asunto");

				Assert.That(
					Assert.Throws<DomainException>(
						() => cuenta.ConsultaDel(LaConsulta.AddDays(1)), "Cuenta.ConsultaDel").Message,
					Does.Contain("consulta"), "Cuenta.ConsultaDel nombra su propio asunto");

				Assert.Throws<DomainException>(
					() => { _ = vacia.ReservaVigente; },
					"Cuenta.ReservaVigente");

				Assert.Throws<DomainException>(
					() => cuenta.VariacionDe("bienes-inmuebles", 0.25m, 0.02m),
					"Cuenta.VariacionDe");

				Assert.Throws<DomainException>(
					() => vacia.ReservaPropuestaPara(2025),
					"Cuenta.ReservaPropuestaPara");

				Assert.Throws<DomainException>(
					() => vacia.FaltanteDeReservaAl(new DateTime(2025, 9, 15)),
					"Cuenta.FaltanteDeReservaAl");

				Assert.Throws<DomainException>(
					() => cuenta.FincaNumero("F-2").ValorVigenteAl(new DateTime(2025, 9, 15)),
					"Finca.ValorVigenteAl");

				Assert.Throws<DomainException>(
					() => cuenta.ConsultaDel(LaConsulta).PendienteMasViejoDe(Rubro.Basura()),
					"Consulta.PendienteMasViejoDe");

				// LAS DOS QUE AGREGO M-5, y estan aca porque este censo es una LISTA DE SITIOS y no una
				// cuenta: una negativa nueva no lo pone rojo y no tiene que pedir permiso para entrar.
				// Es la misma falla C57 que el censo de la puerta evita con igualdad de conjuntos, una
				// capa mas abajo -- y por eso las dos se agregan a mano.
				Assert.Throws<DomainException>(
					() => { _ = vacia.ConsultaMasReciente; },
					"Cuenta.ConsultaMasReciente");

				Assert.Throws<DomainException>(
					() => cuenta.ValorDeFincaAl("F-2", new DateTime(2025, 9, 15)),
					"Cuenta.ValorDeFincaAl");
			});
		}

		// LA MITAD QUE HACE QUE ESTO NO SEA UNA CONVERSION MECANICA. Un defecto de quien llama no
		// es una negativa del registro y sigue con su excepcion del framework, una por familia:
		// el rango de un constructor, el resolutor de un conjunto cerrado, un colaborador
		// requerido en null, un texto en blanco, y los cinco parametros de LECTURA de DEC-08.
		//
		// Cada aserto pide el tipo EXACTO y ademas que no sea la negativa del dominio: pedir solo
		// el tipo dejaria pasar una DomainException que heredara de el. Y el nombre del sitio va
		// en las DOS mitades del aserto (C60), porque el que revienta es el de adentro: sin el, un
		// rojo dice que un tipo no era el esperado y no dice en cual de los nueve sitios.
		[Test]
		public void AProgrammerErrorDoesNotWearTheDomainsRefusal()
		{
			Cobro unCobro = new Cobro(Rubro.Basura(), new Mes(2024, 3), 5000m, new DateTime(2024, 3, 1));

			Assert.Multiple(() =>
			{
				Assert.That(
					Assert.Throws<ArgumentOutOfRangeException>(
						() => new Mes(2026, 13), "Mes(anio, mes) - el rango de un constructor"),
					Is.Not.InstanceOf<DomainException>(), "Mes(anio, mes) - el rango de un constructor");

				Assert.That(
					Assert.Throws<ArgumentOutOfRangeException>(
						() => Rubro.PorNombre("cementerio"), "Rubro.PorNombre - el resolutor de un conjunto cerrado"),
					Is.Not.InstanceOf<DomainException>(), "Rubro.PorNombre - el resolutor de un conjunto cerrado");

				Assert.That(
					Assert.Throws<ArgumentNullException>(
						() => new Cobro(null, new Mes(2024, 3), 5000m, new DateTime(2024, 3, 1)),
						"Cobro(rubro, ...) - un colaborador requerido en null"),
					Is.Not.InstanceOf<DomainException>(), "Cobro(rubro, ...) - un colaborador requerido en null");

				Assert.That(
					Assert.Throws<ArgumentException>(
						() => new Finca("   "), "Finca(numero) - un texto en blanco"),
					Is.Not.InstanceOf<DomainException>(), "Finca(numero) - un texto en blanco");

				// DEC-08: los cinco parametros de LECTURA. Ninguno puede cicatrizar un journal,
				// porque una consulta no escribe, y ninguno es un hecho del registro.
				Assert.That(
					Assert.Throws<ArgumentOutOfRangeException>(
						() => new Atraso(unCobro).CostoDePagarTarde(new DateTime(2025, 1, 1), -0.01m),
						"Atraso.CostoDePagarTarde(tasaAnual)"),
					Is.Not.InstanceOf<DomainException>(), "Atraso.CostoDePagarTarde(tasaAnual)");

				Assert.That(
					Assert.Throws<ArgumentOutOfRangeException>(
						() => UnaCuentaConHistoria().AhorroPorProntoPago(new DateTime(2025, 1, 1), -0.01m),
						"Cuenta.AhorroPorProntoPago(tasaAnual)"),
					Is.Not.InstanceOf<DomainException>(), "Cuenta.AhorroPorProntoPago(tasaAnual)");

				Assert.That(
					Assert.Throws<ArgumentOutOfRangeException>(
						() => new FaltanteDeReserva(1000m, 900m, 0m).Avisa(-0.10m),
						"FaltanteDeReserva.Avisa(umbral)"),
					Is.Not.InstanceOf<DomainException>(), "FaltanteDeReserva.Avisa(umbral)");

				Assert.That(
					Assert.Throws<ArgumentOutOfRangeException>(
						() => new VencimientoProximo(unCobro).Urgencia(new DateTime(2024, 3, 1), -1),
						"VencimientoProximo.Urgencia(diasDeAviso)"),
					Is.Not.InstanceOf<DomainException>(), "VencimientoProximo.Urgencia(diasDeAviso)");

				Assert.That(
					Assert.Throws<ArgumentException>(
						() => Variacion.Entre(
							new LineaPagada(Rubro.Basura(), new Mes(2024, 3), 5000m, 0m, 1, false),
							new LineaPagada(Rubro.Basura(), new Mes(2024, 4), 6000m, 0m, 1, false),
							umbralDeSalto: 0.01m, ruido: 0.02m),
						"Variacion.Entre(umbralDeSalto, ruido)"),
					Is.Not.InstanceOf<DomainException>(), "Variacion.Entre(umbralDeSalto, ruido)");
			});
		}

		// LA AFIRMACION DE G2, EN SU FORMA LITERAL: una caratula reconoce la negativa del dominio
		// POR EL NOMBRE DEL TIPO, y con eso no atrapa una falla de disco. Hereda de Exception
		// directamente para que ningun catch del framework se la lleve por el camino.
		[Test]
		public void TheRefusalIsDistinguishableFromANonDomainFailure()
		{
			Assert.Multiple(() =>
			{
				Assert.That(typeof(DomainException).BaseType, Is.EqualTo(typeof(Exception)),
					"si heredara de una excepcion del framework, el catch de esa se la llevaria");

				Assert.That(new IOException("el journal no se pudo leer"), Is.Not.InstanceOf<DomainException>(),
					"una falla de disco no es la negativa del dominio");

				Assert.That(new InvalidOperationException("cualquier otra cosa"), Is.Not.InstanceOf<DomainException>(),
					"la forma que el dominio usaba antes de M-1 tampoco lo es");
			});
		}

		// LA VISIBILIDAD, QUE ES UNA DECISION Y NO UN DETALLE. G2 cita "una caratula reconoce la
		// negativa del dominio por el nombre del tipo", y de ahi sale la tentacion de hacerlo
		// publico. Este dominio es enteramente internal y sigue siendolo: las caratulas
		// PRE-PREGUNTAN con los predicados totales, que es la ruta de DEC-08 y la de Concesionario.
		[Test]
		public void TheRefusalStaysInsideTheDomain()
		{
			Assert.Multiple(() =>
			{
				Assert.That(typeof(DomainException).IsPublic, Is.False,
					"hacerla publica rompe la regla de dominio internal para el modulo entero");

				Assert.That(typeof(DomainException).IsSealed, Is.True,
					"sellada, para que el conjunto de tipos de negativa sea exactamente uno");
			});
		}
	}
}
