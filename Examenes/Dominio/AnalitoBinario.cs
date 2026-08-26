using System;

namespace Examenes.Dominio
{
	// UN ANALITO QUE SE INFORMA COMO PRESENCIA O AUSENCIA. Forma 5: nitritos, sangre
	// oculta, serologias.
	//
	// Tampoco tiene unidad, y ademas tiene tres estados y no dos —positivo, negativo y no
	// realizado—, que es lo que la magnitud correspondiente ya modela.
	//
	// SIGUE SIENDO CADA UNO EL SUYO. Que la sangre en orina y la sangre en heces sean dos
	// analitos distintos y no uno con un parametro es el mismo argumento de siempre: la
	// forma es identica y la conducta es opuesta. Si fueran el mismo analito, la severidad
	// tendria que decidirse afuera.
	public sealed class AnalitoBinario : Analito
	{
		internal AnalitoBinario(string nombre, string paraMostrar, params string[] sinonimos)
			: base(nombre, paraMostrar, sinonimos)
		{
		}

		// SI forma serie, y esa es la correccion. "Negativo, negativo, positivo" es una
		// serie legitima —el brief la nombra en §6.5— y hasta ahora no se podia tener
		// porque la serie estaba tipada a los examenes de sangre. Ver SerieBinaria para
		// que aporta: una sangre oculta positiva DOS VECES SEGUIDAS no se lee como una.
		public override bool FormaSerie => true;
	}
}
