namespace Cargas.Dominio
{
	/// <summary>
	/// COMO SE DIBUJA una lectura. Es dominio y no estilo: que el rendimiento se mire
	/// como trayectoria contra su linea base, y que un tramo bloqueado se muestre como
	/// tarjeta con lo que falta, son decisiones del modelo. La pantalla hace un switch
	/// sobre esto, y ese switch SI es presentacion legitima: traduce una forma del
	/// dominio a pixeles.
	/// </summary>
	internal sealed class FormaDeLectura
	{
		/// <summary>La serie de tramos contra la banda del nivel de siempre: el
		/// equivalente directo del grafico del antigeno de Delfos.</summary>
		internal static readonly FormaDeLectura TrayectoriaConLineaBase =
			new FormaDeLectura("trayectoria con linea base");

		/// <summary>El valor que existe pero esta bloqueado: la tarjeta SIN TRAMO con lo
		/// que falta y su boton.</summary>
		internal static readonly FormaDeLectura TarjetaBloqueada =
			new FormaDeLectura("tarjeta bloqueada");

		/// <summary>El historial de cargas, agrupado por fecha.</summary>
		internal static readonly FormaDeLectura CintaDeCargas =
			new FormaDeLectura("cinta de cargas");

		/// <summary>Un numero con su referencia, sin serie: la autonomia, el costo por
		/// kilometro, el saldo de puntos.</summary>
		internal static readonly FormaDeLectura MedidorDiscreto =
			new FormaDeLectura("medidor discreto");

		internal string Nombre { get; private set; }

		private FormaDeLectura(string nombre)
		{
			Nombre = nombre;
		}
	}
}
