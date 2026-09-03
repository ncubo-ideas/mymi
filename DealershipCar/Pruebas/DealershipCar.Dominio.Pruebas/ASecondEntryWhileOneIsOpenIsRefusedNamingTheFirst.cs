using System;
using NUnit.Framework;

namespace DealershipCar.Dominio.Pruebas
{
	// GAP G2 DEL BRIEF, resuelto como el brief lo propone, y test 2 de §11. "Un carro no puede estar
	// en dos sucursales a la vez, asi que se rechaza la segunda entrada y se NOMBRA la primera."
	//
	// POR QUE NOMBRAR LA PRIMERA ES LA MITAD QUE IMPORTA. G2 dice que dos sucursales registrando por
	// su cuenta podrian abrir las dos, y que si el rechazo alguna vez dispara en la practica eso es
	// un hallazgo sobre COMO se registran las estadias, no un bug de la regla. Un rechazo que no
	// dijera cual es la estadia abierta dejaria a quien registra sin nada con que ir a averiguarlo.
	//
	// Y ESTO CUESTA UNA CICATRIZ EN EL JOURNAL (C22): la guarda revienta ADENTRO del comando, y para
	// entonces la entrada ya se escribio. Por eso la puerta va a PRE-CHEQUEAR con
	// IsInsideTheShopAt(enteredAt) antes de performar (§4.1 del plan) -- eso es de SH-3/SH-4. Lo que
	// esta prueba fija es que la guarda del dominio es la duena de la invariante, que es la que
	// tiene que seguir ahi cuando el hecho entra por el feed y nadie pre-chequeo.
	//
	// LA TERCERA AFIRMACION ES LA QUE NADIE ESPERA: registrar una estadia CERRADA de un solo acto
	// mientras hay una abierta NO se rechaza. La tabla de §10 del brief pone la guarda de "una sola
	// abierta a la vez" en la fila de la ENTRADA y solo L6 en la fila del acto unico, y es
	// coherente: una estadia cerrada no deja una segunda abierta.
	[TestFixture]
	public sealed class ASecondEntryWhileOneIsOpenIsRefusedNamingTheFirst
	{
		private const string TheVin = "1FTFW1ET5DFC10312";
		private const string TheFirstBranch = "Taller Curridabat";
		private const string TheSecondBranch = "Taller Escazu";
		private const string TheSubject = "Cambio de aceite";
		private const string TheOtherSubject = "Llanta ponchada";

		private static readonly DateTime Entered = new DateTime(2026, 6, 10, 8, 30, 0);
		private static readonly DateTime EnteredAgain = new DateTime(2026, 6, 11, 9, 0, 0);
		private static readonly DateTime Collected = new DateTime(2026, 6, 12, 16, 45, 0);

		private ShopStay stays;

		[SetUp]
		public void Setup()
		{
			stays = new ShopStay(TheVin);
			stays.RecordEntry(TheFirstBranch, TheSubject, Entered);
		}

		[Test]
		public void TheSecondEntryIsRefusedAndTheRefusalNamesTheOpenOne()
		{
			DomainException refusal = Assert.Throws<DomainException>(() =>
				stays.RecordEntry(TheSecondBranch, TheOtherSubject, EnteredAgain));

			Assert.That(refusal.Message, Does.Contain(TheFirstBranch),
				"G2: el rechazo nombra DONDE esta la estadia abierta.");
			Assert.That(refusal.Message, Does.Contain("2026-06-10 08:30:00"),
				"y desde CUANDO, que es lo que quien registra necesita para ir a averiguarlo.");
		}

		// Y NO ENTRA A MEDIAS (E12, E22): despues del rechazo la abierta sigue siendo la primera y
		// no hay una segunda esperando.
		[Test]
		public void TheRefusedEntryLeavesTheFirstStayAsTheOnlyOpenOne()
		{
			Assert.Throws<DomainException>(() =>
				stays.RecordEntry(TheSecondBranch, TheOtherSubject, EnteredAgain));

			Assert.That(stays.DescribeTheStayAt(EnteredAgain), Does.Contain(TheFirstBranch));
			Assert.That(stays.DescribeTheStayAt(EnteredAgain), Does.Not.Contain(TheSecondBranch));

			stays.RecordCollection(Collected, new[] { "2026-06-10", "2026-06-11", "2026-06-12" });

			Assert.That(stays.StayCountBetween("2026-06-01", "2026-06-30"), Is.EqualTo(1),
				"una sola estadia quedo en el registro: la segunda entrada no dejo nada.");
		}

		// EL CONTROL: cerrada la primera, la segunda entrada se acepta. Sin el, una guarda que
		// rechazara toda segunda entrada pasaria las de arriba.
		[Test]
		public void OnceTheFirstIsClosedASecondEntryIsAccepted()
		{
			stays.RecordCollection(Collected, new[] { "2026-06-10", "2026-06-11", "2026-06-12" });

			stays.RecordEntry(TheSecondBranch, TheOtherSubject, new DateTime(2026, 6, 20, 7, 0, 0));

			Assert.That(stays.IsInsideTheShopAt(new DateTime(2026, 6, 21, 7, 0, 0)), Is.True);
			Assert.That(stays.DescribeTheStayAt(new DateTime(2026, 6, 21, 7, 0, 0)),
				Does.Contain(TheSecondBranch));
		}

		// LA TERCERA, y es la que la tabla de §10 dice y nadie espera: el acto unico NO lleva esta
		// guarda, porque una estadia cerrada no deja una segunda abierta.
		[Test]
		public void ACompletedStayRecordedInOneActWhileAnotherIsOpenIsNotRefused()
		{
			stays.RecordStay(TheSecondBranch, TheOtherSubject,
				new DateTime(2026, 5, 4, 8, 0, 0), new DateTime(2026, 5, 4, 15, 0, 0),
				new[] { "2026-05-04" });

			Assert.That(stays.DaysInTheShopCountBetween("2026-05-01", "2026-05-31"), Is.EqualTo(1));
			Assert.That(stays.IsInsideTheShopAt(EnteredAgain), Is.True,
				"y la abierta sigue abierta: las dos conviven porque solo una esta abierta.");
		}
	}
}
