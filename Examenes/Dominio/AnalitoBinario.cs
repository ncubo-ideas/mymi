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
	internal sealed class AnalitoBinario : Analito
	{
		internal AnalitoBinario(string nombre, string paraMostrar, params string[] sinonimos)
			: base(nombre, paraMostrar, RequisitoAnatomico.Ninguno, sinonimos)
		{
		}
	}
}
