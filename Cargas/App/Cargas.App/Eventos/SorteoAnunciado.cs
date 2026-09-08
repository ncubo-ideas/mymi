using System;
using System.Text.Json;
using Puppets.Portador;

namespace Cargas.App.Eventos
{
	// La institucion anuncia una tombola: su premio, su fecha y CUANTO HAY QUE CARGAR por
	// boleto. La regla llega como dato y no esta quemada en la app, por la misma razon que
	// los puntos: es una regla del programa y puede cambiar.
	//
	// Va aparte del boleto y no embebido en el. Sin este evento la app no podria mostrar un
	// sorteo en el que todavia no tenes boletos -"Sorteo de diciembre, todavia no llevas
	// ninguno"-, que es justo la pantalla que motiva.
	public sealed class SorteoAnunciado : IMensajeDeEvento
	{
		public const string TipoDeEvento = "SorteoAnunciado";

		public long Evento { get; private set; }
		public string Tipo => TipoDeEvento;

		public string Codigo { get; private set; }
		public string Nombre { get; private set; }
		public string Premio { get; private set; }
		public DateTime FechaDelSorteo { get; private set; }
		public decimal MontoPorBoleto { get; private set; }
		public string Moneda { get; private set; }

		private SorteoAnunciado() { }

		public static SorteoAnunciado Interpretar(SenalDeEvento senal)
		{
			if (senal.Tipo != TipoDeEvento) return null;
			if (senal.Contenido.Length == 0) return null;

			try
			{
				using JsonDocument documento = JsonDocument.Parse(senal.Contenido);
				JsonElement raiz = documento.RootElement;

				// Sin identidad no hay a que atar los boletos.
				string codigo = Texto(raiz, "codigo");
				if (codigo.Length == 0) return null;

				// Sin fecha no hay ventana ni vencimiento, que es la mitad de lo que un
				// sorteo es.
				if (!raiz.TryGetProperty("fechaDelSorteo", out JsonElement fechaElemento)
					|| !fechaElemento.TryGetDateTime(out DateTime fecha)) return null;

				// EL VALOR Y NO LA PRESENCIA, y no es cosmetico: con cero se divide por
				// cero, y con negativo una carga daria infinitos boletos.
				if (!raiz.TryGetProperty("montoPorBoleto", out JsonElement montoElemento)
					|| !montoElemento.TryGetDecimal(out decimal monto) || monto <= 0m) return null;

				// LA MONEDA NO SE RECONOCE ACA. El dominio es internal, asi que este
				// proyecto no puede nombrar Moneda en C#. El reconocimiento vive en el
				// Check de CargasAPI, dentro del script: el motor resuelve el tipo por
				// reflexion, que no tiene frontera de assembly, y rechazar por Check no
				// deja entrada en el journal.
				string moneda = Texto(raiz, "moneda");
				if (moneda.Length == 0) return null;

				return new SorteoAnunciado
				{
					Evento = senal.Numero,
					Codigo = codigo,
					// El nombre y el premio SI pueden venir vacios: son presentacion, no
					// la regla. Igual que la marca y el modelo en una ficha.
					Nombre = Texto(raiz, "nombre"),
					Premio = Texto(raiz, "premio"),
					FechaDelSorteo = fecha,
					MontoPorBoleto = monto,
					Moneda = moneda,
				};
			}
			catch (JsonException)
			{
				return null;
			}
		}

		private static string Texto(JsonElement raiz, string nombre) =>
			raiz.TryGetProperty(nombre, out JsonElement elemento)
			&& elemento.ValueKind == JsonValueKind.String
				? elemento.GetString().Trim()
				: "";
	}
}
