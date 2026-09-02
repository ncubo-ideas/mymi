using System;
using System.IO;
using Choreography.Theater;
using Puppeteer;
using Presupuesto.App;

namespace Presupuesto.Consola
{
	// EL CICLO COMPLETO, sobre un journal de verdad.
	//
	// Las comprobaciones del dominio puro no tocan el actor: prueban las reglas del
	// negocio. Esto prueba lo otro — que el dominio SE PUEDA ATAR al perform. Son dos
	// preguntas distintas y una no cubre a la otra: un dominio con las 31 comprobaciones
	// en verde puede ser imposible de hospedar, y no se entera hasta acá.
	//
	// El par ConfigureStorage + Start es el mismo que hará la carátula del browser; ahí el
	// path apunta a MEMFS y no a una carpeta del disco.
	internal static class Medicion
	{
		private const string NombreDelActor = "presupuesto-de-prueba";

		internal static int Correr(string carpeta)
		{
			Directory.CreateDirectory(carpeta);

			var app = new PresupuestoApp(NombreDelActor);
			app.ConfigureStorage(DatabaseType.FileSystem, "path=" + carpeta);
			app.Start();

			var api = new PresupuestoAPI(app);

			Titulo("Comandos — se construye el panorama desde primitivos");
			Sembrar(api);
			Console.WriteLine("  ok · los comandos entraron y quedaron en el journal");

			Titulo("Pregunta 1 · ¿se movió el total de junio?");
			Console.WriteLine(api.VarianciaDe(2026, 6));

			Titulo("Pregunta 1b · ¿quiénes lo explican?");
			Console.WriteLine(api.ResponsablesDe(2026, 6));

			Titulo("Pregunta 2 · ¿vale la pena el apartamento?");
			Console.WriteLine(api.RendimientoDe("Apartamento", new DateTime(2026, 1, 1),
				new DateTime(2026, 12, 31), 0.07m, "un certificado de depósito"));

			Titulo("Pregunta 3 · con una mensualidad nueva de 50.000, ¿qué puedo reducir?");
			Console.WriteLine(api.MargenPara(50000m, 2026, 6));

			Titulo("Pregunta 4 · ¿qué pago fijo no me cobraron? (la ausencia)");
			Console.WriteLine(api.QueNoMeCobraron(new DateTime(2026, 7, 20)));

			Titulo("Pregunta 5 · ¿cuánto debería apartar para imprevistos?");
			Console.WriteLine(api.CuantoApartarPorMes(new DateTime(2026, 1, 1),
				new DateTime(2026, 12, 31)));

			Console.WriteLine();
			Console.WriteLine("Las cinco preguntas cruzaron el perform.");
			return 0;
		}

		// La historia está GUIONADA, no es aleatoria: cada valor está elegido para que una
		// de las cinco preguntas tenga algo que decir. Es lo mismo que hace
		// InyectorDeEventosDemo en los negocios del repo.
		private static void Sembrar(PresupuestoAPI api)
		{
			api.ReconocerServicioBasico("Electricidad", 0.15m, "CNFL");
			api.QueTambienResponda("Electricidad", "CNFL S.A.");
			api.QueTambienResponda("Electricidad", "PAGO SERV ELECTRICO");

			api.ReconocerCuotaDePrestamo("Carro", 60, 12, "CUOTA CARRO");
			api.ReconocerPagoDeTarjeta("Visa", "PAGO TARJETA VISA");

			api.ReconocerGastoDeLaPersona("Cine", "mensual", "CINEPOLIS");
			api.MarcarReducibilidad("Cine", "discrecional");

			api.ReconocerGastoDeLaPersona("Supermercado", "mensual", "SUPERMERCADO LA COSECHA");

			// Mayo: la línea base.
			api.EntraCargoDelBanco(new DateTime(2026, 5, 5), 20000m, "CNFL", "debito");
			api.EntraCargoDelBanco(new DateTime(2026, 5, 5), 150000m, "CUOTA CARRO", "debito");
			api.EntraCargoDelBanco(new DateTime(2026, 5, 8), 20000m, "CINEPOLIS", "tarjeta");
			api.EntraCargoDelBanco(new DateTime(2026, 5, 12), 90000m, "SUPERMERCADO LA COSECHA", "tarjeta");
			api.EntraCargoDelBanco(new DateTime(2026, 5, 20), 110000m, "PAGO TARJETA VISA", "debito");

			// Junio: el súper se dispara. El total tiene que moverse más del 10 %, y el
			// pago de la tarjeta NO debe sumar al consumo.
			api.EntraCargoDelBanco(new DateTime(2026, 6, 5), 21500m, "cnfl s.a.", "debito");
			api.EntraCargoDelBanco(new DateTime(2026, 6, 5), 150000m, "CUOTA CARRO", "debito");
			api.EntraCargoDelBanco(new DateTime(2026, 6, 8), 20000m, "CINEPOLIS", "tarjeta");
			api.EntraCargoDelBanco(new DateTime(2026, 6, 12), 165000m, "SUPERMERCADO LA COSECHA", "tarjeta");
			api.EntraCargoDelBanco(new DateTime(2026, 6, 20), 185000m, "PAGO TARJETA VISA", "debito");

			// El bien, con su valor: sin él no hay porcentaje y no se puede comparar.
			api.DeclararBien("Apartamento", 50000000m);
			api.ElBienProduce("Apartamento", new DateTime(2026, 1, 15), 400000m, "alquiler enero");
			api.ElBienProduce("Apartamento", new DateTime(2026, 2, 15), 400000m, "alquiler febrero");
			api.AtribuirGastoAlBien("Apartamento", new DateTime(2026, 1, 20), 150000m, "ferretería");

			// Dos imprevistos: uno de tres cargos y otro de uno. La tasa se cuenta sobre
			// EVENTOS, así que tienen que dar 2 y no 4.
			api.RegistrarImprevisto("Hospitalización", new DateTime(2026, 3, 10));
			api.ElImprevistoCausoTambien("Hospitalización", new DateTime(2026, 3, 10), 500000m, "hospital");
			api.ElImprevistoCausoTambien("Hospitalización", new DateTime(2026, 3, 11), 80000m, "farmacia");
			api.ElImprevistoCausoTambien("Hospitalización", new DateTime(2026, 3, 12), 45000m, "laboratorio");

			api.RegistrarImprevisto("Se quemó la refrigeradora", new DateTime(2026, 9, 2));
			api.ElImprevistoCausoTambien("Se quemó la refrigeradora", new DateTime(2026, 9, 2),
				275000m, "electrodomésticos");
		}

		private static void Titulo(string texto)
		{
			Console.WriteLine();
			Console.WriteLine("── " + texto);
		}
	}
}
