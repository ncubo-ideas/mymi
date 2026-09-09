using System;

namespace Municipalidad.Dominio.Pruebas
{
	// LA HISTORIA INVENTADA, mudada desde Municipalidad.Consola en M-6. Tres recibos y una consulta que
	// reproducen: la primera linea sin historia, lineas que cubren varios periodos, el salto del 2025
	// por tarifa en basura y por avaluo en bienes inmuebles, y policia casi quieta.
	//
	// LOS DATOS SON INVENTADOS. Reproducen los fenomenos que los papeles reales mostraron -- los dos
	// alfabetos de Periodo, mensual contra trimestral, el salto del 2025, la mora recibida -- sin
	// ningun monto ni fecha de la cuenta real. Los datos del demo se orquestan como casos de prueba.
	internal static class ElExpedienteDeDemo
	{
		internal static readonly DateTime LaConsulta = new DateTime(2025, 9, 3);

		internal static Cuenta UnaCuentaCompleta()
		{
			Cuenta c = new Cuenta();
			c.IncorporarFinca("F-1");
			LosPrimerosDosRecibos(c);

			c.RegistrarPago("R-3", new DateTime(2025, 6, 30), 0m);
			c.AgregarLineaAlPago("R-3", "recoleccion-de-basura", 2025, 6, 96000m, 310m);
			c.AgregarLineaAlPago("R-3", "servicio-policia", 2025, 6, 73200m, 240m);
			c.AgregarLineaAlPago("R-3", "limpieza-de-calles", 2025, 6, 120000m, 390m);
			c.AgregarLineaAlPago("R-3", "bienes-inmuebles", 2025, 2, 144000m, 470m);
			c.AgregarLineaAlPago("R-3", "parques-y-zonas-verdes", 2025, 2, 21600m, 70m);

			LaConsultaDeSetiembre(c);
			return c;
		}

		// LA MISMA HISTORIA SIN EL TERCER RECIBO. La app conoce basura hasta junio de 2024; la consulta
		// de setiembre de 2025 debe 24.000 de basura, que a 5.000 por mes -- lo ultimo conocido -- son
		// unos cinco meses: mayo a setiembre de 2025. Julio de 2024 a abril de 2025 se pagaron en algun
		// lado que la app no vio.
		internal static Cuenta UnaCuentaSinElTercerRecibo()
		{
			Cuenta c = new Cuenta();
			LosPrimerosDosRecibos(c);
			LaConsultaDeSetiembre(c);
			return c;
		}

		internal static void LosPrimerosDosRecibos(Cuenta c)
		{
			c.RegistrarPago("R-1", new DateTime(2024, 3, 31), 0m);
			c.AgregarLineaAlPago("R-1", "recoleccion-de-basura", 2024, 3, 5000m, 0m);
			c.AgregarLineaAlPago("R-1", "servicio-policia", 2024, 3, 6000m, 0m);
			c.AgregarLineaAlPago("R-1", "limpieza-de-calles", 2024, 3, 6000m, 0m);
			c.AgregarLineaAlPago("R-1", "bienes-inmuebles", 2024, 1, 20000m, 0m);
			c.AgregarLineaAlPago("R-1", "parques-y-zonas-verdes", 2024, 1, 3000m, 0m);

			c.RegistrarPago("R-2", new DateTime(2024, 6, 30), 0m);
			c.AgregarLineaAlPago("R-2", "recoleccion-de-basura", 2024, 6, 15000m, 0m);
			c.AgregarLineaAlPago("R-2", "servicio-policia", 2024, 6, 18000m, 0m);
			c.AgregarLineaAlPago("R-2", "limpieza-de-calles", 2024, 6, 18000m, 0m);
			c.AgregarLineaAlPago("R-2", "bienes-inmuebles", 2024, 2, 20000m, 0m);
			c.AgregarLineaAlPago("R-2", "parques-y-zonas-verdes", 2024, 2, 3000m, 0m);
		}

		internal static void LaConsultaDeSetiembre(Cuenta c)
		{
			c.RegistrarConsulta(LaConsulta);
			c.AgregarPendiente(LaConsulta, "recoleccion-de-basura", 2025, 9, 24000m, 45m);
			c.AgregarPendiente(LaConsulta, "servicio-policia", 2025, 9, 18300m, 34m);
			c.AgregarPendiente(LaConsulta, "limpieza-de-calles", 2025, 9, 30000m, 56m);
			c.AgregarPendiente(LaConsulta, "bienes-inmuebles", 2025, 3, 36000m, 0m);
			c.AgregarPendiente(LaConsulta, "parques-y-zonas-verdes", 2025, 3, 5400m, 0m);
		}
	}
}
