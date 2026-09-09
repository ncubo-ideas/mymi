using System;
using NUnit.Framework;

namespace Municipalidad.Dominio.Pruebas
{
	// JUICIOS 5 Y 6, FIRMADOS: la proyeccion del anio entre doce es una PROPUESTA de la app; lo que la
	// persona adopta es SUYO. Dos cosas, dos casillas, y no un `bool adoptada`. Se avisa si lo adoptado
	// queda mas de 10 % por debajo, y es urgente solo si el faltante vence en el mes.
	//
	// Mudado desde Municipalidad.Consola en M-6.
	[TestFixture]
	public sealed class TheAppProposesTheReserveAndThePersonAdoptsIt
	{
		[Test]
		public void WithNoPaymentThereIsNothingToProjectAndNeitherHalfIsInvented()
		{
			Cuenta vacia = new Cuenta();

			Assert.Multiple(() =>
			{
				Assert.That(vacia.SePuedeProyectar, Is.False, "Sin un pago no hay con que proyectar");
				Assert.Throws<DomainException>(
					() => vacia.ReservaPropuestaPara(2025),
					"Y pedir una propuesta revienta en vez de inventar un numero");
				Assert.Throws<DomainException>(
					() => { ReservaDeclarada vigente = vacia.ReservaVigente; },
					"Sin reserva adoptada no hay reserva vigente, y no se decide por la persona");
			});
		}

		[Test]
		public void TheProjectionAndTheProposalAreDerivedAndNeverStored()
		{
			Cuenta c = ElExpedienteDeDemo.UnaCuentaCompleta();

			Assert.Multiple(() =>
			{
				Assert.That(c.ProyeccionDelAnio(), Is.EqualTo(454800m),
					"La proyeccion del anio es 454.800");
				Assert.That(c.ReservaPropuestaPara(2025).MontoMensual, Is.EqualTo(37900m),
					"Y la propuesta mensual es 37.900");
			});
		}

		[Test]
		public void AdoptingIsOncePerDayAndTheSameAdoptionAgainIsTheSameFact()
		{
			Cuenta c = ElExpedienteDeDemo.UnaCuentaCompleta();

			Assert.That(c.SePuedeVerElFaltanteDeReserva, Is.False,
				"Sin reserva adoptada no se puede ver el faltante");

			c.AdoptarReserva(30000m, new DateTime(2025, 8, 1));

			Assert.Multiple(() =>
			{
				Assert.That(c.SePuedeVerElFaltanteDeReserva, Is.True, "Adoptada, si");
				Assert.That(c.PuedeAdoptarReserva(30000m, new DateTime(2025, 8, 1)), Is.False,
					"La misma adopcion el mismo dia no entra otra vez: es el mismo hecho -regla 18, hueco 13-");
				Assert.That(c.PuedeAdoptarReserva(31000m, new DateTime(2025, 8, 2)), Is.True,
					"Una adopcion nueva un dia despues, si");
				Assert.Throws<DomainException>(
					() => c.AdoptarReserva(0m, new DateTime(2025, 8, 1)),
					"Una reserva de cero no se adopta");
			});
		}

		[Test]
		public void TheShortfallWarnsOverTenPercentAndIsUrgentOnlyWhenItFallsDueThisMonth()
		{
			Cuenta c = ElExpedienteDeDemo.UnaCuentaCompleta();
			c.AdoptarReserva(30000m, new DateTime(2025, 8, 1));

			FaltanteDeReserva enAgosto = c.FaltanteDeReservaAl(new DateTime(2025, 8, 15));
			FaltanteDeReserva enSetiembre = c.FaltanteDeReservaAl(new DateTime(2025, 9, 15));

			Assert.Multiple(() =>
			{
				Assert.That(enAgosto.Faltante, Is.EqualTo(7900m), "Le faltan 7.900 por mes");
				Assert.That(enAgosto.Avisa(0.10m), Is.True, "Es mas del 10 %: avisa");
				Assert.That(enAgosto.VenceEsteMes == 0m && !enAgosto.EsUrgente, Is.True,
					"Pero en agosto no vence nada: no es urgente");

				Assert.That(enSetiembre.VenceEsteMes, Is.EqualTo(113700m),
					"En setiembre vencen 113.700");
				Assert.That(enSetiembre.Urgencia(0.10m).EsUrgente(), Is.True,
					"Y lo apartado no alcanza: urgente");
			});
		}
	}
}
