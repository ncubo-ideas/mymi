using System;
using System.Collections.Generic;

namespace Gimnasio.Dominio
{
	// LO QUE SONIA QUIERE LOGRAR. Es lo que le da SIGNO a cada numero: sin objetivo
	// declarado la app no puede opinar sobre ninguna medida -juicio 1-.
	//
	// HUECO 5: el Preguntas.md ya anotaba como pendiente que pasa con objetivos que se
	// contradicen. Al modelar aparece la version chica y concreta del problema: aca se
	// guarda UNA meta por medida, asi que dos metas contradictorias sobre la misma
	// medida no se pueden ni expresar. Se rechaza al agregar, que es lo honesto -pero
	// que sea lo correcto lo decide el negocio-.
	internal sealed class Objetivo
	{
		private readonly Dictionary<string, Meta> porMedida =
			new Dictionary<string, Meta>(StringComparer.OrdinalIgnoreCase);

		internal void Agregar(Meta meta)
		{
			ArgumentNullException.ThrowIfNull(meta);
			if (porMedida.ContainsKey(meta.NombreDeLaMedida))
				throw new InvalidOperationException(
					$"Ya hay una meta para '{meta.NombreDeLaMedida}'. Dos metas sobre la misma medida "
					+ "se contradirian y el modelo no sabe cual gana.");
			porMedida[meta.NombreDeLaMedida] = meta;
		}

		// PREDICADO TOTAL: contesta para cualquier medida, sin reventar.
		internal bool TieneMetaPara(Medida medida)
		{
			return medida != null && porMedida.ContainsKey(medida.Nombre);
		}


		// PREDICADO TOTAL POR NOMBRE. El de arriba recibe una Medida, y un Check solo
		// tiene el nombre que vino como parametro. Sin esta sobrecarga el Check no puede
		// preguntar lo que el comando va a rechazar -y eso envenena el journal-.
		internal bool YaTieneMetaPara(string nombreDeLaMedida)
		{
			return !string.IsNullOrWhiteSpace(nombreDeLaMedida)
				&& porMedida.ContainsKey(nombreDeLaMedida);
		}

		internal Meta MetaDe(Medida medida)
		{
			ArgumentNullException.ThrowIfNull(medida);
			if (!porMedida.TryGetValue(medida.Nombre, out Meta meta))
				throw new KeyNotFoundException(
					$"No hay meta declarada para '{medida.Nombre}'. Preguntar TieneMetaPara antes.");
			return meta;
		}

		// De aca sale el signo. Y NO se inventa una direccion cuando no hay meta: sin
		// meta, la app no tiene con que juzgar el movimiento de esa medida.
		internal Direccion DireccionPara(Medida medida)
		{
			return MetaDe(medida).Direccion;
		}
	}
}
