using System;
using System.Text.Json;
using Examenes.Dominio;
using Puppets.Portador;

namespace Examenes.App.Eventos
{
	// EL SEGUNDO EVENTO DE ESTE NEGOCIO: el laboratorio publico un dato del paciente.
	//
	// POR QUE EXISTE, y no alcanza con que la persona lo declare en su app. Un laboratorio
	// REAL sabe el sexo del paciente: los rangos de referencia van impresos en el reporte,
	// asi que su sistema tiene el dato para poder imprimirlos. Modelar solo la puerta de la
	// persona seria fingir que el laboratorio no lo sabe.
	//
	// Es la opcion C de la spec: la institucion publica, la persona puede corregir, y para
	// interpretar gana ella. Las dos afirmaciones conviven en el journal con su procedencia
	// (ver Expediente).
	//
	// Y DEJA VIVO EL CASO `Desconocido` SIN FINGIRLO: mientras este laboratorio no publique
	// el dato —hoy no lo hace, el guion del demo arranca sin este evento— el expediente
	// sigue sin saberlo y el bloque que insiste aparece. Es lo que Alvaro quiere ver, y
	// ocurre porque de verdad falta el dato, no porque se haya escondido.
	//
	// Forma de IMensajeDeEvento, igual que ExamenAgregado: un tag de routing compacto y un
	// Interpretar que entiende SU propio wire. De aca para adentro no entra nada del
	// payload del proveedor.
	public sealed class DatosDePacienteActualizados : IMensajeDeEvento
	{
		public const string TipoDeEvento = "DatosDePacienteActualizados";

		public long Evento { get; private set; }

		public string Tipo => TipoDeEvento;

		public DateTime Fecha { get; private set; }

		// YA RESUELTO A IDENTIDAD en la frontera -el laboratorio escribe "M", "masculino" o
		// "Hombre" segun de donde salga el dato- y transportado como el NOMBRE CANONICO.
		//
		// ES UN string Y NO UN Sexo, por lo mismo que el analito de un examen: este tipo es
		// un DTO de la Superficie, y un objeto del dominio cargado en un DTO viaja por
		// codigo C# ALREDEDOR del actor. La coercion la hace el call site del script,
		// adentro del actor y journalizada con el acto.
		public string Sexo { get; private set; }

		private DatosDePacienteActualizados() { }

		// Devuelve null si la senal no es de este tipo o no trae nada que esta app modele.
		// El bucle DESCARTA un null; no inventa un comando para tragarse la senal.
		public static DatosDePacienteActualizados Interpretar(SenalDeEvento senal)
		{
			if (senal.Tipo != TipoDeEvento) return null;
			if (senal.Contenido.Length == 0) return null;

			try
			{
				using JsonDocument documento = JsonDocument.Parse(senal.Contenido);
				JsonElement raiz = documento.RootElement;

				string sexo = Texto(raiz, "sexo");

				// UN EVENTO QUE NO TRAE EL SEXO NO ES UN EVENTO DE ESTA APP. El dia que el
				// laboratorio publique tambien la fecha de nacimiento, este mensaje crece;
				// hoy el unico dato de paciente que este dominio modela es el sexo, y un
				// evento sin el no tiene nada que aplicar.
				if (sexo.Length == 0) return null;

				// Y un valor que este dominio no conoce se DESCARTA, no revienta: un
				// laboratorio puede publicar categorias que esta app no modela, y eso no es
				// motivo para que la app deje de ponerse al dia con el resto.
				if (!Dominio.Sexo.EsConocidoElNombre(sexo)) return null;

				// Sin fecha se usa la del evento igual: a diferencia de un examen, este dato
				// no se ordena en una serie. Lo que ordena es el numero de evento.
				DateTime fecha = raiz.TryGetProperty("fecha", out JsonElement e)
					&& e.TryGetDateTime(out DateTime cuando) ? cuando : default;

				return new DatosDePacienteActualizados
				{
					Evento = senal.Numero,
					Fecha = fecha,
					Sexo = Dominio.Sexo.PorNombre(sexo).Nombre,
				};
			}
			catch (JsonException)
			{
				return null;
			}
		}

		private static string Texto(JsonElement raiz, string nombre) =>
			raiz.TryGetProperty(nombre, out JsonElement e) && e.ValueKind == JsonValueKind.String
				? e.GetString() : "";
	}
}
