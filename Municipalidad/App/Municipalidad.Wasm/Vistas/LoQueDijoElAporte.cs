using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Municipalidad.App;
namespace Municipalidad.Wasm.Vistas
{
	/// <summary>
	/// Lo que pasó cuando la persona aportó algo a su cuenta, en TRES estados y nunca en dos: entró,
	/// el dominio lo rechazó y dijo por que, o entró y no se pudo bajar a la carpeta.
	/// <para>
	/// El tercero es el que no se puede callar: el hecho quedó en el journal en memoria y NO en la
	/// carpeta, asi que la pantalla y el respaldo dejaron de decir lo mismo.
	/// </para>
	/// </summary>
	public sealed class LoQueDijoElAporte
	{
		private LoQueDijoElAporte() { }

		/// <summary>Si el aporte quedó registrado Y guardado.</summary>
		public bool QuedoRegistrado { get; private init; }

		/// <summary>Si quedó registrado pero la carpeta rechazó la escritura.</summary>
		public bool QuedoSinRespaldo { get; private init; }

		/// <summary>La frase que explica lo que pasó, vacia solo cuando entró sin nada que decir.</summary>
		public string Dice { get; private init; } = "";

		internal static LoQueDijoElAporte Entro() =>
			new LoQueDijoElAporte { QuedoRegistrado = true };

		internal static LoQueDijoElAporte NoSePudo(string porque) =>
			new LoQueDijoElAporte { Dice = porque };

		internal static LoQueDijoElAporte QuedoSinGuardar(string porque) =>
			new LoQueDijoElAporte { QuedoRegistrado = true, QuedoSinRespaldo = true, Dice = porque };
	}
}
