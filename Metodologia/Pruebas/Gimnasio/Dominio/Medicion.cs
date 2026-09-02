using System;
using System.Collections.Generic;

namespace Gimnasio.Dominio
{
	// UNA HOJA DE MEDIDAS. Lo que el gimnasio le entrega, y que hoy nadie acumula: esa
	// es la razon de existir de la app.
	//
	// No es un evento de calendario. Ocurre AL FINAL DE UN PLAN -juicio 3-, y por eso
	// la trayectoria la asocia a un plan y no a un mes.
	internal sealed class Medicion
	{
		private readonly Dictionary<string, Medida> porMedida =
			new Dictionary<string, Medida>(StringComparer.OrdinalIgnoreCase);

		internal DateTime Fecha { get; }

		internal Medicion(DateTime fecha) { Fecha = fecha; }

		internal void Registrar(Medida medida)
		{
			ArgumentNullException.ThrowIfNull(medida);
			if (porMedida.ContainsKey(medida.Nombre))
				throw new InvalidOperationException(
					$"Esta medicion ya trae '{medida.Nombre}'. Dos valores de la misma medida en la "
					+ "misma hoja no se pueden distinguir.");
			porMedida[medida.Nombre] = medida;
		}

		internal bool Trae(string nombreDeLaMedida)
		{
			return !string.IsNullOrWhiteSpace(nombreDeLaMedida) && porMedida.ContainsKey(nombreDeLaMedida);
		}

		internal Medida Traida(string nombreDeLaMedida)
		{
			if (!porMedida.TryGetValue(nombreDeLaMedida ?? "", out Medida medida))
				throw new KeyNotFoundException(
					$"Esta medicion no trae '{nombreDeLaMedida}'. Preguntar Trae antes.");
			return medida;
		}

		internal int CuantasMedidas { get { return porMedida.Count; } }
	}
}
