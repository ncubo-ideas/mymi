using System;

namespace Gimnasio.Dominio
{
	// LA BANDERA QUE EL GIMNASIO MANDA SOBRE LA MEMBRESIA.
	//
	// Es SU evaluacion de la cuenta de Sonia -contra sus reglas, en un instante-, y aca
	// llega como HECHO: la app la registra, no la recalcula. El gimnasio es la autoridad
	// sobre su propia mora (paper 8), y la app no cruza cobros contra pagos para
	// deducirla. Juicio N1 del v2, contestado por Jorge el 2026-09-07.
	//
	// CON LAS PALABRAS DEL GIMNASIO (N9, v3). El papel dice "Tu plan esta: Activo"; el
	// otro estado es "No Activa" -Jorge, 2026-09-08, hueco 18 cerrado-. El v2 decia
	// "al-dia" y "en-mora", que eran las palabras de la entrevista. Con el renombre, todo
	// lo que el modelo derivaba de la mora -el periodo, los dias descontados del plan-
	// paso a derivarse de la INACTIVIDAD: la bandera no dice por que la membresia no esta
	// activa, y el plan no espera dias mientras no lo este, sea la razon que sea.
	//
	// CONJUNTO CERRADO de dos, comparado por nombre, con resolutor total desde primitivo
	// (regla 11): desde un comando solo se puede nombrar. No es un bool: un bool no se
	// dice, y no deja lugar a un tercer estado si el gimnasio algun dia manda uno.
	//
	// Y NO ES UN SUBTIPO DE MEMBRESIA. Una membresia entra y sale de mora: eso es una
	// FASE, y una fase no puede ser una clase de la que la instancia tendria que
	// cambiar. Va como estado en un campo, cambiado por una transicion con nombre
	// (glosario nº 19, la pregunta de rigidez; guia de dominios, E36).
	internal sealed class EstadoDeMembresia
	{
		private readonly string nombre;

		private EstadoDeMembresia(string nombre) { this.nombre = nombre; }

		internal static EstadoDeMembresia Activo() { return new EstadoDeMembresia("activo"); }
		internal static EstadoDeMembresia NoActiva() { return new EstadoDeMembresia("no-activa"); }

		// RESOLUTOR TOTAL. Un nombre desconocido revienta: un conjunto cerrado no tiene
		// miembro "vacio" fuera de el, asi que "no encontrado" es un bug del llamador,
		// no un valor.
		internal static EstadoDeMembresia PorNombre(string nombre)
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(nombre);
			switch (nombre.Trim().ToLowerInvariant())
			{
				case "activo": return Activo();
				case "no-activa": return NoActiva();
				default:
					throw new ArgumentOutOfRangeException(nameof(nombre), nombre,
						"Estado de membresia desconocido. Los que existen son: activo, no-activa.");
			}
		}

		// El predicado total que acompana al resolutor: es lo que un Check puede
		// preguntar antes de escribir, en vez de reventar a mitad del comando.
		internal static bool EsUnNombreConocido(string nombre)
		{
			if (string.IsNullOrWhiteSpace(nombre)) return false;
			switch (nombre.Trim().ToLowerInvariant())
			{
				case "activo":
				case "no-activa": return true;
				default: return false;
			}
		}

		internal bool Es(EstadoDeMembresia otro)
		{
			return otro != null && otro.nombre == nombre;
		}

		internal bool EsNoActiva() { return nombre == "no-activa"; }
		internal bool EsActivo() { return nombre == "activo"; }

		// LA ETIQUETA ES UNA HOJA, no solo un ToString. Regla 13, corregida el
		// 2026-09-04: decidir que se ve del material es del actor, y para eso el
		// material tiene que exponer la hoja. Los tipos de juicio del v1 no lo hacian.
		internal string Etiqueta { get { return nombre; } }

		public override string ToString() { return nombre; }
	}
}
