using System;
using System.Collections.Generic;

namespace Examenes.Dominio
{
	// Que le deja consultar la persona a su IA sobre este expediente.
	//
	// POR QUE ESTO VIVE EN EL DOMINIO Y NO EN UNA PREFERENCIA DEL BROWSER. La app corre en
	// el browser y el servidor MCP es OTRO proceso, afuera. Entre los dos no hay
	// protocolo: lo unico que comparten es el journal. Una preferencia guardada en
	// localStorage la veria la pantalla y no la veria la IA, o sea que el toggle no
	// gobernaria nada y seria decoracion.
	//
	// Journalizada, en cambio, viaja por el mismo camino que todo lo demas: la persona
	// decide en su app, queda como un comando en SU registro, y el servidor MCP la lee al
	// rehidratar. Es la misma idea que el senuelo de 403 — el permiso se hace cumplir, no
	// se promete.
	//
	// Y es un dato QUE APORTA LA PERSONA, no el laboratorio. Igual que la edad: no llega
	// por evento, se escribe en su app y se queda ahi.
	public class PermisosDeLaIA
	{
		// Se guardan las DENEGADAS y no las permitidas, y eso decide como se comporta el
		// dia que alguien agregue una consulta nueva: aparece permitida. Al reves —
		// guardar las permitidas— una consulta nueva nacería denegada para todo
		// expediente ya existente, y la persona veria una capacidad apagada que nunca
		// apago.
		private readonly HashSet<string> denegadas =
			new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		public void Denegar(string consulta)
		{
			// Resolver contra el conjunto cerrado ANTES de guardar: si el nombre no
			// existe revienta acá, y no queda una entrada muerta en el journal que nadie
			// va a poder interpretar despues.
			denegadas.Add(ConsultaDeLaIA.PorNombre(consulta).Nombre);
		}

		public void Permitir(string consulta)
		{
			denegadas.Remove(ConsultaDeLaIA.PorNombre(consulta).Nombre);
		}

		// Total: preguntar por una consulta que no existe es un defecto, pero preguntar
		// por una que existe siempre tiene respuesta. Por defecto todo esta permitido.
		public bool EstaPermitida(string consulta) =>
			!denegadas.Contains(ConsultaDeLaIA.PorNombre(consulta).Nombre);

		public int CantidadDenegada => denegadas.Count;

		public bool HayAlgunaDenegada => denegadas.Count > 0;

		// Los nombres de las denegadas, para que la superficie pueda proyectarlas sin
		// recorrer el conjunto cerrado por su cuenta.
		public IReadOnlyList<string> Denegadas
		{
			get
			{
				var nombres = new List<string>(denegadas);
				nombres.Sort(StringComparer.OrdinalIgnoreCase);
				return nombres;
			}
		}
	}
}
