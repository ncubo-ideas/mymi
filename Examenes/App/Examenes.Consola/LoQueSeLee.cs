using System;
using System.Collections.Generic;
using Examenes.App;
using Examenes.Dominio;

namespace Examenes.Consola
{
	// LAS FRASES QUE EL DOMINIO ELIGE, YA ESCRITAS. Es el puente entre lo que estas
	// comprobaciones miden -la frase entera, que es lo que una persona lee- y lo que el
	// dominio entrega desde que se localizo: una clave estable y sus valores.
	//
	// Corre en el IDIOMA DEL REGISTRO, que es el del laboratorio. Estas comprobaciones
	// fijan la redaccion de ese idioma; el otro catalogo lo cubre la comprobacion que
	// compara las dos listas de claves, que es la unica que ve una frase a medias.
	//
	// Son extensiones y no metodos sueltos para que el sitio de la comprobacion siga
	// leyendose como antes -`lectura.ParaSonia()`- y el cambio quede en un solo archivo.
	internal static class LoQueSeLee
	{
		private static readonly App.Frases.Frases enElRegistro =
			App.Frases.Frases.De(ExamenesAPI.IdiomaDelRegistro);

		internal static string ParaSonia(this Interpretacion lectura) =>
			App.Frases.LaRedaccion.Lectura(enElRegistro, lectura.Clave, lectura.ClaveDelAnalito,
				lectura.ValorInicial, lectura.ValorFinal, lectura.Unidad, lectura.Dias, lectura.Cambio,
				lectura.Cuantas, lectura.CuantasSeguidas, lectura.ClaveDeLaCategoria,
				lectura.NivelPropio, lectura.LimiteCitado, lectura.RitmoPorAno,
				lectura.ClaveDeLaColetilla);

		internal static string ReferenciaLegible(this Interpretacion lectura) =>
			App.Frases.LaRedaccion.Referencia(enElRegistro, lectura.ClaveDeLaReferencia,
				lectura.MinimoDeLaReferencia, lectura.MaximoDeLaReferencia,
				lectura.UnidadDeLaReferencia);

		internal static string AccionLegible(this Interpretacion lectura) =>
			App.Frases.LaRedaccion.Accion(enElRegistro, lectura.ClaveDeLaAccion, lectura.ClaveDelPlazo);

		internal static string AccionLegible(this UrgenciaDelExpediente urgencia) =>
			App.Frases.LaRedaccion.Accion(enElRegistro, urgencia.ClaveDeLaAccion,
				urgencia.ClaveDelPlazo);

		internal static string AccionLegible(this ControlPendiente control) =>
			App.Frases.LaRedaccion.Accion(enElRegistro, control.ClaveDeLaAccion, control.ClaveDelPlazo);

		internal static string Legible(this Accion accion) =>
			App.Frases.LaRedaccion.Accion(enElRegistro, accion.Clave, accion.ClaveDelPlazo);

		internal static string ParaSonia(this ControlPendiente control) =>
			App.Frases.LaRedaccion.Control(enElRegistro, control.ClaveDeLaFrase,
				control.ClaveDelAnalito, control.ClaveDeLoQueHace, control.AnosDesdeLaUltima,
				control.DiasDesdeLaUltima, control.ClaveDeCadaCuanto, control.AnosRecomendados,
				control.MesesRecomendados);

		internal static string ParaMostrar(this ControlPendiente control) =>
			App.Frases.LaRedaccion.Nombre(enElRegistro, control.ClaveDelAnalito);

		internal static string ParaSonia(this TranquilidadDelExpediente tranquilidad) =>
			App.Frases.LaRedaccion.EnRango(enElRegistro, tranquilidad.ClaveDeLaFrase,
				tranquilidad.EnSuRango, tranquilidad.Analitos);

		internal static string ParaSonia(this PedidoDeSexo pedido) =>
			App.Frases.LaRedaccion.PedidoDeSexo(enElRegistro, pedido.ClaveDeLaFrase,
				pedido.SinInterpretar, pedido.Analitos, pedido.ClavesPorRango,
				pedido.ClavesPorAplicabilidad);

		internal static string ElPorQue(this PedidoDeSexo pedido) =>
			enElRegistro.Diga(pedido.ClaveDelPorQue);

		internal static string ParaSonia(this ExamenDeOrina examen) =>
			enElRegistro.Diga(examen.ClaveDeLaFrase, ("categoria", Categoria(examen.Color.Nombre)));

		internal static string ParaSonia(this ExamenDeHeces examen) =>
			enElRegistro.Diga(examen.ClaveDeLaFrase, ("categoria", Categoria(examen.Color.Nombre)));

		internal static string ParaSonia(this MagnitudBinaria magnitud) =>
			enElRegistro.Diga(magnitud.ClaveDeLaFrase);

		internal static string ParaSonia(this MagnitudNominal magnitud) =>
			enElRegistro.Diga(magnitud.ClaveDeLaFrase, ("categoria", Categoria(magnitud.Nombre)));

		internal static string ResultadoExpresado(this Examen examen) =>
			App.Frases.LaRedaccion.Resultado(enElRegistro, examen.Clave, Lecturas(examen));

		internal static string Nombre(this Examen examen) =>
			App.Frases.LaRedaccion.Nombre(enElRegistro, examen.Clave);

		// SOLO LAS DOS FORMAS QUE SE DICEN CON UN CODIGO. Una Medicion conserva su propio
		// Expresado -- es un numero con su unidad, no una categoria -- y dejar esta extension
		// en la raiz se lo tapaba en silencio.
		internal static string Expresado(this MagnitudBinaria magnitud) => Categoria(magnitud.Clave);

		internal static string Expresado(this MagnitudNominal magnitud) => Categoria(magnitud.Clave);

		internal static string Expresado(this CeldaDelCuadro celda) =>
			celda.EsNumero ? enElRegistro.Medida(celda.Valor, celda.Unidad)
				: Categoria(celda.ClaveDelResultado);

		internal static string ParaMostrar(this CeldaDelCuadro celda) =>
			App.Frases.LaRedaccion.Nombre(enElRegistro, celda.ClaveDelAnalito);

		internal static string Expresado(this PruebaBinaria prueba) =>
			Categoria(prueba.ClaveDelResultado);

		internal static string Expresado(this ObservacionNominal observacion) =>
			Categoria(observacion.ClaveDelResultado);

		internal static string Expresada(this Velocidad velocidad) =>
			App.Frases.LaRedaccion.Velocidad(enElRegistro, velocidad.ClaveDeLaFrase,
				velocidad.PorAno, velocidad.Unidad);

		internal static string ParaMostrar(this Analito analito) =>
			App.Frases.LaRedaccion.Nombre(enElRegistro, analito.Clave);

		internal static string ParaMostrar(this Sexo sexo) =>
			App.Frases.LaRedaccion.Sexo(enElRegistro, sexo.Nombre);

		internal static string Expresado(this MedicionDelAno delAno) =>
			App.Frases.LaRedaccion.MedicionDelAnio(enElRegistro, delAno.ClaveDeLaFrase, delAno.Ano,
				delAno.Numero, delAno.Unidad, delAno.Cambio);

		internal static string Expresado(this BandaDelHistograma banda) =>
			App.Frases.LaRedaccion.Banda(enElRegistro, banda.Piso, banda.Techo, banda.Unidad,
				banda.Cuantas);

		private static string Categoria(string codigo) =>
			App.Frases.LaRedaccion.Categoria(enElRegistro, codigo);

		private static IReadOnlyList<(string, string, decimal, string, bool)> Lecturas(Examen examen)
		{
			var lecturas = new List<(string, string, decimal, string, bool)>();
			foreach (LecturaDeExamen lectura in examen.Lecturas)
				lecturas.Add((lectura.ClaveDelAnalito, lectura.ClaveDelResultado, lectura.Valor,
					lectura.Unidad, lectura.EsNumero));

			return lecturas;
		}
	}
}
