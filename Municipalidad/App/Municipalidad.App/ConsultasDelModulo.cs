using System;
using System.Collections.Generic;

namespace Municipalidad.App
{
	/// <summary>
	/// Las consultas que este modulo contesta cuando corre HOSPEDADO dentro de Gnothi: los cuerpos, en
	/// un solo sitio y sin actor propio.
	/// <para>
	/// Los dos hospedajes abren el journal de forma distinta -- el de escritorio abre un lector de solo
	/// lectura por consulta, porque la app instalada escribe mientras ese proceso vive, y en el telefono
	/// el actor del modulo ya esta vivo en el WebView -- y lo que NO puede cambiar entre los dos es la
	/// respuesta. Por eso los cuerpos viven aca, toman una puerta ya abierta y el instante con que se
	/// pregunta, y cada hospedaje pone los suyos.
	/// </para>
	/// <para>
	/// SON DOS Y NINGUNA PIDE UN HECHO DE LA CUENTA, igual que las seis del servidor del asistente: el
	/// instante es el alcance de la pregunta y los umbrales los pone esta capa, nunca quien pregunta.
	/// Cada respuesta viaja con el dia y con el numero con que se midio.
	/// </para>
	/// </summary>
	public static class ConsultasDelModulo
	{
		/// <summary>Los nombres con que las dos consultas viajan, tal como los declara el manifiesto.</summary>
		public const string Resumen = "resumen";

		public const string ListaDeVencimientos = "lista_de_vencimientos";

		/// <summary>
		/// Lo que este modulo puede atender de verdad. Gnothi compara esta lista contra la del manifiesto
		/// al dar de alta el modulo, asi que sale de aca y no se escribe dos veces.
		/// </summary>
		public static readonly string[] Declaradas = { Resumen, ListaDeVencimientos };

		/// <summary>
		/// EL IDIOMA DEL REGISTRO, y lo que esta cara contesta siempre. No sigue al dispositivo a
		/// proposito: quien lee esto es el asistente, que ya sabe en que idioma le hablo la persona y
		/// redacta en el suyo. Una frase traducida aca llegaria al modelo sin los nombres con que la
		/// municipalidad lleva la cuenta, que es lo que el modelo necesita para volver a preguntar.
		/// </summary>
		public const string IdiomaDelRegistro = "es";

		/// <summary>
		/// UNA LINEA DE RESUMEN: el bloque util mas chico, y el que una pantalla pone arriba. Cuenta
		/// periodos, no plata: cuanto debe la persona hoy lo dice el portal de la municipalidad, que es la
		/// autoridad sobre su deuda, y esta app existe para lo que el portal no contesta.
		/// </summary>
		public static object HacerResumen(MunicipalidadAPI api, DateTime preguntadoEn)
		{
			ArgumentNullException.ThrowIfNull(api);

			RegistroDeVencimientos porVencer =
				api.VencimientosAl(preguntadoEn, LosJuiciosSinFirmar.DiasDeAviso, IdiomaDelRegistro);

			RegistroDeAtrasos vencidos =
				api.AtrasosAl(preguntadoEn, LosJuiciosSinFirmar.TasaDeMora, IdiomaDelRegistro);

			return new
			{
				hayDatos = true,
				preguntadoEn = ElDiaDe(preguntadoEn),
				fincas = api.NumerosDeLasFincas().Length,
				rubrosConHistoria = api.RubrosConHistoria().Length,
				periodosPorVencer = porVencer.Rubro.Length,
				periodosVencidos = vencidos.Rubro.Length,
				diasDeAviso = LosJuiciosSinFirmar.DiasDeAviso,
				ultimoEventoLeido = api.UltimoEventoAplicado(),
			};
		}

		/// <summary>
		/// LO QUE SE VENCE Y LO QUE YA VENCIO, en filas planas: exactamente lo que una pantalla pinta
		/// como tabla, y la pregunta que ninguna pantalla del portal contesta.
		/// <para>
		/// SON DOS LISTAS Y NO SE SUMAN. Un periodo por vencer y uno vencido son dos situaciones y la
		/// persona hace cosas distintas con cada una. El costo de pagar tarde es una proyeccion de esta
		/// app y no un cobro de la municipalidad, y la frase de cada fila ya lo dice.
		/// </para>
		/// </summary>
		public static object HacerListaDeVencimientos(MunicipalidadAPI api, DateTime preguntadoEn)
		{
			ArgumentNullException.ThrowIfNull(api);

			RegistroDeVencimientos vencimientos =
				api.VencimientosAl(preguntadoEn, LosJuiciosSinFirmar.DiasDeAviso, IdiomaDelRegistro);

			RegistroDeAtrasos atrasos =
				api.AtrasosAl(preguntadoEn, LosJuiciosSinFirmar.TasaDeMora, IdiomaDelRegistro);

			var porVencer = new List<object>();

			for (int fila = 0; fila < vencimientos.Rubro.Length; fila++)
				porVencer.Add(new
				{
					rubro = vencimientos.Rubro[fila],
					periodo = vencimientos.Periodo[fila],
					vence = vencimientos.Vence[fila],
					monto = vencimientos.Monto[fila],
					faltan = vencimientos.Faltan[fila],
					urgencia = vencimientos.Urgencia[fila],
					dice = vencimientos.Dice[fila],
				});

			var vencidos = new List<object>();

			for (int fila = 0; fila < atrasos.Rubro.Length; fila++)
				vencidos.Add(new
				{
					rubro = atrasos.Rubro[fila],
					periodo = atrasos.Periodo[fila],
					vencio = atrasos.Vencio[fila],
					monto = atrasos.Monto[fila],
					dias = atrasos.Dias[fila],
					costoProyectado = atrasos.CostoProyectado[fila],
					urgencia = atrasos.Urgencia[fila],
					dice = atrasos.Dice[fila],
				});

			return new
			{
				hayDatos = true,
				preguntadoEn = ElDiaDe(preguntadoEn),
				diasDeAviso = LosJuiciosSinFirmar.DiasDeAviso,
				tasaConQueSeProyecto = LosJuiciosSinFirmar.TasaDeMora,
				porVencer,
				vencidos,
			};
		}

		/// <summary>
		/// Todavia no hay nada registrado. Se dice DONDE se busco, porque un "no hay datos" que no dice
		/// donde no se puede accionar, y se dice que no es un error de nadie.
		/// </summary>
		public static object SinDatos(string donde) => new
		{
			hayDatos = false,
			carpeta = donde ?? "",
			detalle = "La app todavía no se ha abierto en este dispositivo, así que no hay nada "
				+ "registrado en esta carpeta. Pídale a la persona que abra su app de la municipalidad "
				+ "una vez; no es un error, y no es algo que usted ni la municipalidad puedan arreglar "
				+ "desde aquí.",
		};

		/// <summary>El dia de un instante, en el formato con que el dominio escribe un dia.</summary>
		private static string ElDiaDe(DateTime instante) => instante.ToString("yyyy-MM-dd");
	}
}
