using Puppets.Portador;

namespace Cargas.App.Eventos
{
	// La ruta de esta app: de una senal cruda al mensaje tipado que un comando sabe
	// aplicar. Cinco tipos de evento, un Interpretar cada uno; null DESCARTA.
	public static class RutaDeCargas
	{
		public static IMensajeDeEvento Rutear(SenalDeEvento senal)
		{
			if (senal.Tipo == CargaRegistrada.TipoDeEvento) return CargaRegistrada.Interpretar(senal);
			if (senal.Tipo == PuntosAcreditados.TipoDeEvento) return PuntosAcreditados.Interpretar(senal);
			if (senal.Tipo == NivelActualizado.TipoDeEvento) return NivelActualizado.Interpretar(senal);
			if (senal.Tipo == SorteoAnunciado.TipoDeEvento) return SorteoAnunciado.Interpretar(senal);
			if (senal.Tipo == BoletoEmitido.TipoDeEvento) return BoletoEmitido.Interpretar(senal);

			return null;
		}
	}
}
