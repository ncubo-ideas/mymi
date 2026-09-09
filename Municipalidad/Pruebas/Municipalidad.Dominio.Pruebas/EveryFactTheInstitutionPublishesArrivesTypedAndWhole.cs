using System;
using Municipalidad.App.Events;
using NUnit.Framework;
using Puppets.Portador;

namespace Municipalidad.Dominio.Pruebas
{
	// LA OTRA MITAD DE LA FRONTERA: la senal que SI se puede decir se vuelve un hecho tipado, y no pierde
	// nada por el camino. Sin esto, un interprete que devolviera null a todo dejaria en verde el fixture
	// entero de los descartes.
	//
	// SE AFIRMA CAMPO POR CAMPO Y NO Is.Not.Null. Un hecho que llega con el rubro de otro, o con el monto
	// en el lugar de la mora, es exactamente lo que un Is.Not.Null no ve -- y es lo que E-2 va a mandar a
	// un comando que deja entrada permanente.
	//
	// EL PERIODO LLEGA COMO LO ESCRIBE EL RECIBO, sin partir. Los dos alfabetos los resuelve la puerta y
	// no el interprete: partirlo aca pondria la regla en dos sitios, y el que la publica no seria el que
	// Preguntas.md nombra.
	[TestFixture]
	public sealed class EveryFactTheInstitutionPublishesArrivesTypedAndWhole
	{
		[Test]
		public void AnIncorporatedFincaKeepsItsNumber()
		{
			FincaIncorporada hecho = FincaIncorporada.Interpret(
				UnaNarrativaDeLaMunicipalidad.Finca(7, "1-234-567"));

			Assert.Multiple(() =>
			{
				Assert.That(hecho.NumeroDelEvento, Is.EqualTo(7L));
				Assert.That(hecho.NumeroDeFinca, Is.EqualTo("1-234-567"));
			});
		}

		[Test]
		public void AChargePutOnTheAccountKeepsItsRubroItsPeriodItsAmountAndItsDay()
		{
			CobroPuesto hecho = CobroPuesto.Interpret(
				UnaNarrativaDeLaMunicipalidad.Cobro(
					8, "recoleccion-de-basura", "2026.01", "8000.50", "2026-01-05T00:00:00Z"));

			Assert.Multiple(() =>
			{
				Assert.That(hecho.NumeroDelEvento, Is.EqualTo(8L));
				Assert.That(hecho.NombreDelRubro, Is.EqualTo("recoleccion-de-basura"));
				Assert.That(hecho.PeriodoComoDiceElRecibo, Is.EqualTo("2026.01"),
					"Como lo escribe el recibo, sin partir: los dos alfabetos los resuelve la puerta.");
				Assert.That(hecho.MontoAlCobro, Is.EqualTo(8000.50m),
					"Con su fraccion. Un monto redondeado aca seria un cobro que nadie puso.");
				Assert.That(hecho.PuestoEl, Is.EqualTo(new DateTime(2026, 1, 5, 0, 0, 0, DateTimeKind.Utc)));
			});
		}

		[Test]
		public void AReceiptKeepsItsNumberItsDayAndItsDiscount()
		{
			PagoRegistrado hecho = PagoRegistrado.Interpret(
				UnaNarrativaDeLaMunicipalidad.Recibo(9, "R-9001", "2026-02-10T00:00:00Z", "450.25"));

			Assert.Multiple(() =>
			{
				Assert.That(hecho.NumeroDelEvento, Is.EqualTo(9L));
				Assert.That(hecho.NumeroDeRecibo, Is.EqualTo("R-9001"));
				Assert.That(hecho.FechaDelPago,
					Is.EqualTo(new DateTime(2026, 2, 10, 0, 0, 0, DateTimeKind.Utc)));
				Assert.That(hecho.DescuentoDelRecibo, Is.EqualTo(450.25m));
			});
		}

		// EL MONTO Y LA MORA SON DOS HECHOS Y NO UNO, y esta prueba les da valores DISTINTOS a proposito:
		// con los dos iguales, un interprete que leyera dos veces el mismo campo pasaria.
		[Test]
		public void AReceiptLineKeepsItsReceiptItsRubroItsPeriodItsAmountAndItsLateFee()
		{
			LineaDePagoRegistrada hecho = LineaDePagoRegistrada.Interpret(
				UnaNarrativaDeLaMunicipalidad.Linea(
					10, "R-9001", "recoleccion-de-basura", "2026.01", "8000", "1200"));

			Assert.Multiple(() =>
			{
				Assert.That(hecho.NumeroDelEvento, Is.EqualTo(10L));
				Assert.That(hecho.NumeroDeRecibo, Is.EqualTo("R-9001"));
				Assert.That(hecho.NombreDelRubro, Is.EqualTo("recoleccion-de-basura"));
				Assert.That(hecho.PeriodoComoDiceElRecibo, Is.EqualTo("2026.01"));
				Assert.That(hecho.MontoDeLaLinea, Is.EqualTo(8000m));
				Assert.That(hecho.InteresesDeLaLinea, Is.EqualTo(1200m),
					"La mora es la que la municipalidad COBRO, no la que esta app proyecta.");
			});
		}

		[Test]
		public void AConsultaKeepsItsDay()
		{
			ConsultaTomada hecho = ConsultaTomada.Interpret(
				UnaNarrativaDeLaMunicipalidad.Consulta(11, "2026-04-15T00:00:00Z"));

			Assert.Multiple(() =>
			{
				Assert.That(hecho.NumeroDelEvento, Is.EqualTo(11L));
				Assert.That(hecho.FechaDeLaConsulta,
					Is.EqualTo(new DateTime(2026, 4, 15, 0, 0, 0, DateTimeKind.Utc)));
			});
		}

		[Test]
		public void APendingItemKeepsTheConsultaThatUncoveredItAndAllOfItsOwnParts()
		{
			PendienteRegistrado hecho = PendienteRegistrado.Interpret(
				UnaNarrativaDeLaMunicipalidad.Pendiente(
					12, "2026-04-15T00:00:00Z", "bienes-inmuebles", "2026.01", "45000", "1200"));

			Assert.Multiple(() =>
			{
				Assert.That(hecho.NumeroDelEvento, Is.EqualTo(12L));
				Assert.That(hecho.FechaDeLaConsulta,
					Is.EqualTo(new DateTime(2026, 4, 15, 0, 0, 0, DateTimeKind.Utc)),
					"Es la consulta que lo destapo, y sin ella el pendiente no se puede colgar de nada.");
				Assert.That(hecho.NombreDelRubro, Is.EqualTo("bienes-inmuebles"));
				Assert.That(hecho.PeriodoComoDiceElRecibo, Is.EqualTo("2026.01"));
				Assert.That(hecho.MontoPendiente, Is.EqualTo(45000m));
				Assert.That(hecho.InteresesPendientes, Is.EqualTo(1200m));
			});
		}

		// Y LA NARRATIVA ENTERA SE INTERPRETA, que es lo que impide que las seis de arriba queden verdes
		// mientras un tipo nuevo entra sin testigo: la narrativa trae uno de cada tipo, y el censo afirma
		// aparte que los tipos son seis.
		[Test]
		public void EverySignalOfTheNarrativeBecomesTheFactItSaysItIs()
		{
			Assert.Multiple(() =>
			{
				foreach (SenalDeEvento senal in UnaNarrativaDeLaMunicipalidad.LaNarrativaEntera())
					foreach (Type tipo in UnaNarrativaDeLaMunicipalidad.LosTiposDeMensaje())
					{
						if (UnaNarrativaDeLaMunicipalidad.ElTagDe(tipo) != senal.Tipo) continue;

						object hecho = UnaNarrativaDeLaMunicipalidad.Interpretar(tipo, senal);

						Assert.That(hecho, Is.Not.Null,
							"La narrativa trae una senal de tipo " + senal.Tipo + " que su propio "
							+ "interprete descarta.");

						Assert.That(((IMensajeDeEvento)hecho).Tipo, Is.EqualTo(senal.Tipo),
							"Y se reconoce como lo que es, no como el tipo de al lado.");

						Assert.That(((IMensajeDeEvento)hecho).Evento, Is.EqualTo(senal.Numero),
							"El numero del evento es el punto de control, asi que el mensaje tiene que "
							+ "llevar el de SU senal: " + senal.Tipo);
					}
			});
		}
	}
}
