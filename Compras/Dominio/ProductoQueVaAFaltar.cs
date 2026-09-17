using System;

namespace Compras.Dominio
{
	/// <summary>
	/// Un producto que la persona compra con un ritmo conocido, y cuánto hace que no entra
	/// medido contra ese ritmo.
	/// </summary>
	// LO QUE HACE UTIL A ESTA FILA ES QUE TRAE SU RAZON. "Leche" sola es una adivinanza;
	// "leche, suele volver cada 7 dias y hace 9 que no entra" es un hecho que la persona
	// puede confirmar o descartar de un vistazo. Sin el porque, una lista sugerida es la app
	// diciendole a alguien lo que tiene que comprar.
	internal class ProductoQueVaAFaltar
	{
		internal string Sku { get; private set; }
		internal string Descripcion { get; private set; }

		/// <summary>Cada cuánto vuelve, dicho como habla la gente.</summary>
		internal Cadencia Cadencia { get; private set; }

		internal DateTime UltimaVez { get; private set; }

		internal int DiasDesdeLaUltima { get; private set; }

		/// <summary>La mediana de sus intervalos: cada cuántos días suele volver, en número.</summary>
		// SE GUARDA LA MEDIANA Y NO SOLO LA CADENCIA porque el corte de la cadencia tiene
		// HOLGURA A PROPOSITO -lo semanal llega hasta 9 dias, para que una semana corrida no
		// se anuncie como cambio de rutina-. Esa holgura es correcta para decir "cambiaste de
		// habito" y es tarde para decir "te va a faltar": quien compra cada 7 lo quiere en la
		// lista el dia 7, no el 10.
		internal int DiasTipicos { get; private set; }

		internal ProductoQueVaAFaltar(string sku, string descripcion, int diasTipicos,
			DateTime ultimaVez, int diasDesdeLaUltima)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(sku);
			ArgumentNullException.ThrowIfNullOrWhiteSpace(descripcion);
			if (diasTipicos <= 0)
				throw new ArgumentOutOfRangeException(nameof(diasTipicos), diasTipicos,
					"Un producto que vuelve cada cero dias no tiene ritmo.");
			if (diasDesdeLaUltima < 0)
				throw new ArgumentOutOfRangeException(nameof(diasDesdeLaUltima), diasDesdeLaUltima,
					"No se puede haber comprado en el futuro.");

			Sku = sku;
			Descripcion = descripcion;
			DiasTipicos = diasTipicos;
			UltimaVez = ultimaVez;
			DiasDesdeLaUltima = diasDesdeLaUltima;
			Cadencia = Cadencia.Para(diasTipicos);
		}

		/// <summary>Como aaaa-mm-dd, que es la forma en que la fecha viaja.</summary>
		internal string UltimaVezExpresada => UltimaVez.ToString("yyyy-MM-dd");

		/// <summary>Ya pasó su tiempo: si va al súper hoy, probablemente lo lleve.</summary>
		internal bool Toca => DiasDesdeLaUltima >= DiasTipicos;

		/// <summary>
		/// Días de más desde que tocaba. Negativo cuando todavía no toca, y ahí dice cuántos
		/// faltan.
		/// </summary>
		// UN SOLO NUMERO CON SIGNO en vez de dos lecturas guardadas. "Cuantos dias de atraso"
		// y "cuantos dias faltan" son la misma cuenta mirada desde los dos lados, y separarlas
		// obliga a preguntar Toca antes de leer cualquiera de las dos.
		internal int DiasDeMas => DiasDesdeLaUltima - DiasTipicos;

		/// <summary>
		/// QUE FRASE dice esta fila, por su nombre: `toca` o `no-toca`. Ninguna de las dos ORDENA
		/// comprar: dicen cada cuanto vuelve y cuanto hace que no entra, y la decision es de quien lee.
		/// </summary>
		internal string Clave => Toca ? "toca" : "no-toca";

		/// <summary>El codigo de la cadencia, aplanado para que el print lo alcance en un solo nivel.</summary>
		internal string ClaveDeLaCadencia => Cadencia.Clave;

		/// <summary>Cuantos dias faltan para que toque. Cero o menos cuando ya paso: lectura total.</summary>
		internal int DiasQueFaltan => -DiasDeMas;
	}
}
