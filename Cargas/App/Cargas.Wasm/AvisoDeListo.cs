namespace Cargas.Wasm;

// EL AVISO DE QUE SALIÓ BIEN, y se va solo. Existe porque el éxito no existía: confirmar un
// tanque cambiaba una versalita, exportar no decía nada, Actualizar sin novedades tampoco, y
// guardar la ficha dejaba un parte gris para siempre. Sin señal de éxito la persona repite la
// acción «por si acaso», y la segunda vez sí puede hacer daño.
//
// SE VA SOLO porque un éxito no es una alarma: leída una vez, ya cumplió. Lo que sí queda es
// lo que informa sobre el carro -«se desbloqueó»-, que no pasa por acá.
//
// Vive suelto porque lo usan las pantallas y también la cáscara, y son dos jerarquías
// distintas (ComponentBase y LayoutComponentBase), igual que Fallo.
public sealed class AvisoDeListo
{
	/// <summary>Cuánto se queda. El CSS lo desvanece en el último tramo (ver .parte--pasajero),
	/// así que cuando esto lo saca del DOM ya no se veía.</summary>
	public const int Milisegundos = 5000;

	public string Texto { get; private set; } = "";

	// Cada aviso nuevo invalida el apagado del anterior: dos confirmaciones seguidas no
	// pueden hacer que la segunda desaparezca a los dos segundos.
	private int vuelta;

	public void Mostrar(string texto, Func<Task> repintar)
	{
		ArgumentNullException.ThrowIfNull(repintar);
		Texto = texto;
		int esta = ++vuelta;
		_ = Apagar(esta, repintar);
	}

	public void Borrar()
	{
		Texto = "";
		vuelta++;
	}

	private async Task Apagar(int esta, Func<Task> repintar)
	{
		await Task.Delay(Milisegundos);
		if (esta != vuelta) return;
		Texto = "";
		await repintar();
	}
}
