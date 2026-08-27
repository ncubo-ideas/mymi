using System;
using Choreography.Theater;

namespace Presupuesto.App
{
	// LA SUPERFICIE INTENCIONAL de la app. Todo el mundo entra por acá: la consola, el
	// servidor MCP y la carátula del browser. Nadie habla con el actor crudo.
	//
	// NO NOMBRA UN SOLO TIPO DEL DOMINIO EN C#. Los tipos aparecen dentro de los strings
	// de los scripts y los resuelve el motor por reflexión. Ésa es la cerca del capítulo
	// 3: acá no se puede declarar una variable de dominio, así que la primera decisión
	// "chiquita" no tiene dónde escribirse y vuelve a donde pertenece.
	//
	// Las consultas devuelven STRING —el texto que produjo `print`— y no un objeto. Quien
	// lo necesite tipado lo proyecta del otro lado, con sus propios DTOs.
	public sealed class PresupuestoAPI
	{
		private readonly PerformanceV2 app;

		public PresupuestoAPI(PerformanceV2 app)
		{
			ArgumentNullException.ThrowIfNull(app);
			this.app = app;
		}

		// ================================================================ comandos
		//
		// EL OBJETO SE CONSTRUYE DENTRO DEL SCRIPT, desde primitivos. Eso es lo que hace
		// que el journal guarde `ServicioBasico('Electricidad', 0.15)` como texto legible:
		// al reproducirlo, el motor vuelve a construir esa clase con esos valores.
		//
		// Los identificadores van en un bloque `{ }` para que queden LOCALES. Un `g = ...`
		// suelto en el tope crearía un global del actor que nadie quiere: el gasto vive
		// adentro del panorama, no al lado.

		public void ReconocerServicioBasico(string nombre, decimal variacionTolerada, string textoDelBanco)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(nombre);

			app.Using(@"
				{
					g = ServicioBasico(@nombre, @variacion);
					g.RespondeTambienA(@texto);
					panorama.Reconoce(g);
				}
			")
			.WithParameters(p => {
				p["nombre",    typeof(string)]  = nombre;
				p["variacion", typeof(decimal)] = variacionTolerada;
				p["texto",     typeof(string)]  = textoDelBanco;
			})
			.PerformCommand();
		}

		public void ReconocerCuotaDePrestamo(string nombre, int cuotasTotales,
			int cuotasPagadas, string textoDelBanco)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(nombre);

			app.Using(@"
				{
					g = CuotaDePrestamo(@nombre, @totales, @pagadas);
					g.RespondeTambienA(@texto);
					panorama.Reconoce(g);
				}
			")
			.WithParameters(p => {
				p["nombre",  typeof(string)] = nombre;
				p["totales", typeof(int)]    = cuotasTotales;
				p["pagadas", typeof(int)]    = cuotasPagadas;
				p["texto",   typeof(string)] = textoDelBanco;
			})
			.PerformCommand();
		}

		public void ReconocerPagoDeTarjeta(string nombre, string textoDelBanco)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(nombre);

			app.Using(@"
				{
					g = PagoDeTarjetaDeCredito(@nombre);
					g.RespondeTambienA(@texto);
					panorama.Reconoce(g);
				}
			")
			.WithParameters(p => {
				p["nombre", typeof(string)] = nombre;
				p["texto",  typeof(string)] = textoDelBanco;
			})
			.PerformCommand();
		}

		// La periodicidad entra como TEXTO y la resuelve el conjunto cerrado adentro del
		// actor. Si el texto no nombra ninguna, revienta ahí: la interpretación ocurre
		// donde queda journalizada, no en esta clase.
		public void ReconocerGastoDeLaPersona(string nombre, string periodicidad, string textoDelBanco)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(nombre);

			app.Using(@"
				{
					g = GastoDeLaPersona(@nombre, Periodicidad.PorNombre(@cadaCuanto));
					g.RespondeTambienA(@texto);
					panorama.Reconoce(g);
				}
			")
			.WithParameters(p => {
				p["nombre",       typeof(string)] = nombre;
				p["cadaCuanto",   typeof(string)] = periodicidad;
				p["texto",        typeof(string)] = textoDelBanco;
			})
			.PerformCommand();
		}

		public void QueTambienResponda(string nombreDelGasto, string textoDelBanco)
		{
			app.Using("panorama.QueTambienResponda(@nombre, @texto);")
			.WithParameters(p => {
				p["nombre", typeof(string)] = nombreDelGasto;
				p["texto",  typeof(string)] = textoDelBanco;
			})
			.PerformCommand();
		}

		public void MarcarReducibilidad(string nombreDelGasto, string reducibilidad)
		{
			app.Using("panorama.MarcarComo(@nombre, @reducibilidad);")
			.WithParameters(p => {
				p["nombre",         typeof(string)] = nombreDelGasto;
				p["reducibilidad",  typeof(string)] = reducibilidad;
			})
			.PerformCommand();
		}

		// UN CARGO DEL BANCO. Procedencia institución: lo emitió el banco.
		public void EntraCargoDelBanco(DateTime fecha, decimal monto, string descripcion,
			string medioDePago)
		{
			app.Using(@"
				{
					c = Cargo(@fecha, @monto, @descripcion, @medio, Procedencia.PorNombre('institucion'));
					panorama.Entra(c);
				}
			")
			.WithParameters(p => {
				p["fecha",       typeof(DateTime)] = fecha;
				p["monto",       typeof(decimal)]  = monto;
				p["descripcion", typeof(string)]   = descripcion;
				p["medio",       typeof(string)]   = medioDePago ?? "";
			})
			.PerformCommand();
		}

		public void DeclararBien(string nombre, decimal valorEstimado)
		{
			app.Using(@"
				{
					b = Bien(@nombre, @valor);
					panorama.Tiene(b);
				}
			")
			.WithParameters(p => {
				p["nombre", typeof(string)]  = nombre;
				p["valor",  typeof(decimal)] = valorEstimado;
			})
			.PerformCommand();
		}

		public void AtribuirGastoAlBien(string nombreDelBien, DateTime fecha, decimal monto,
			string descripcion)
		{
			app.Using("panorama.SeLeAtribuyeAlBien(@bien, @fecha, @monto, @descripcion);")
			.WithParameters(p => {
				p["bien",        typeof(string)]   = nombreDelBien;
				p["fecha",       typeof(DateTime)] = fecha;
				p["monto",       typeof(decimal)]  = monto;
				p["descripcion", typeof(string)]   = descripcion ?? "";
			})
			.PerformCommand();
		}

		public void ElBienProduce(string nombreDelBien, DateTime fecha, decimal monto,
			string descripcion)
		{
			app.Using("panorama.ElBienProduce(@bien, @fecha, @monto, @descripcion);")
			.WithParameters(p => {
				p["bien",        typeof(string)]   = nombreDelBien;
				p["fecha",       typeof(DateTime)] = fecha;
				p["monto",       typeof(decimal)]  = monto;
				p["descripcion", typeof(string)]   = descripcion ?? "";
			})
			.PerformCommand();
		}

		public void RegistrarImprevisto(string quePaso, DateTime cuando)
		{
			app.Using(@"
				{
					i = Imprevisto(@quePaso, @cuando);
					panorama.Ocurrio(i);
				}
			")
			.WithParameters(p => {
				p["quePaso", typeof(string)]   = quePaso;
				p["cuando",  typeof(DateTime)] = cuando;
			})
			.PerformCommand();
		}

		public void ElImprevistoCausoTambien(string quePaso, DateTime fecha, decimal monto,
			string descripcion)
		{
			app.Using("panorama.LoCausoTambien(@quePaso, @fecha, @monto, @descripcion);")
			.WithParameters(p => {
				p["quePaso",     typeof(string)]   = quePaso;
				p["fecha",       typeof(DateTime)] = fecha;
				p["monto",       typeof(decimal)]  = monto;
				p["descripcion", typeof(string)]   = descripcion ?? "";
			})
			.PerformCommand();
		}

		// ================================================================ consultas
		//
		// EL CARRIER NO CRUZA. El script lo construye y lo consume ADENTRO del actor, y lo
		// que cruza son sus escalares por `print`. Ésa es E41 en la práctica, y es también
		// por qué el dominio tuvo que aplanar lo que tenía anidado: el print proyecta un
		// solo nivel.

		// Pregunta 1 — .se movio el total? Los escalares.
		public string VarianciaDe(int ano, int mes) =>
			app.Using(@"
				{
					v = panorama.VarianciaDe(@ano, @mes);
					print v.Ano ano,
						v.Mes mes,
						v.TotalDelMes total,
						v.TotalAnterior totalAnterior,
						v.HayConQueComparar hayConQueComparar,
						v.Proporcion proporcion,
						v.Umbral umbral,
						v.ExigeAtencion exigeAtencion,
						v.Lectura lectura;
				}
			")
			.WithParameters(p => {
				p["ano", typeof(int)] = ano;
				p["mes", typeof(int)] = mes;
			})
			.PerformQuery();

		// Pregunta 1 — y quienes lo explican. Consulta APARTE, porque una coleccion adentro
		// de un carrier es el segundo nivel que el print no proyecta.
		public string ResponsablesDe(int ano, int mes) =>
			app.Using(@"
				{
					v = panorama.VarianciaDe(@ano, @mes);
					foreach (responsable in v.Responsables) {
						print responsable.NombreDelGasto gasto,
							responsable.Antes antes,
							responsable.Ahora ahora,
							responsable.Cambio cambio,
							responsable.Proporcion proporcion,
							responsable.EsVariacionNormalDeSuClase esVariacionNormal,
							responsable.Lectura lectura;
					}
				}
			")
			.WithParameters(p => {
				p["ano", typeof(int)] = ano;
				p["mes", typeof(int)] = mes;
			})
			.PerformQuery();

		// Pregunta 2 — .vale la pena el bien?
		public string RendimientoDe(string nombreDelBien, DateTime desde, DateTime hasta,
			decimal tasaDeLaAlternativa, string nombreDeLaAlternativa)
		{
			bool hayAlternativa = !string.IsNullOrWhiteSpace(nombreDeLaAlternativa);

			// EL TERNARIO NO SIRVE ACÁ, y es una restricción del substrato que conviene
			// conocer antes de diseñar la fachada. MEDIDO: el DSL rechaza
			//
			//     contra = @hay ? ContraUnaTasaAnual(...) : ContraSuPropiaHistoria();
			//
			// con "Binary operation TernaryAstExpression cannot combine ContraUnaTasaAnual
			// and ContraSuPropiaHistoria". Sus dos ramas tienen que dar EL MISMO tipo: no
			// unifica dos subclases a su base común, aunque el parámetro reciba la base.
			//
			// Elegir el texto del script en C# es además más honesto: son dos consultas
			// distintas, y en un comando el journal guardaría cuál se ejecutó de verdad en
			// vez de una condición que hay que volver a evaluar al reproducir.
			string construirLaComparacion = hayAlternativa
				? "contra = ContraUnaTasaAnual(@alternativa, @tasa);"
				: "contra = ContraSuPropiaHistoria();";

			return app.Using(@"
				{
					" + construirLaComparacion + @"
					r = panorama.RendimientoDe(@bien, @desde, @hasta, contra);
					print r.NombreDelBien bien,
						r.Ingresos ingresos,
						r.Gastos gastos,
						r.Neto neto,
						r.SePagaSola sePagaSola,
						r.SeSabeElPorcentaje seSabeElPorcentaje,
						r.PorcentajeAnualODeCero porcentajeAnual,
						r.SePuedeComparar sePuedeComparar,
						r.AlternativaODeCero rendimientoAlternativa,
						r.NombreDeLaComparacion contra,
						r.Lectura lectura;
				}
			")
			.WithParameters(p => {
				p["bien",           typeof(string)]   = nombreDelBien;
				p["desde",          typeof(DateTime)] = desde;
				p["hasta",          typeof(DateTime)] = hasta;
				p["hayAlternativa", typeof(bool)]     = hayAlternativa;
				p["alternativa",    typeof(string)]   = nombreDeLaAlternativa ?? "";
				p["tasa",           typeof(decimal)]  = tasaDeLaAlternativa;
			})
			.PerformQuery();
		}

		// Pregunta 3 — .que puedo reducir? La unica hacia adelante y contrafactica.
		public string MargenPara(decimal mensualidadNueva, int ano, int mes) =>
			app.Using(@"
				{
					m = panorama.MargenPara(@mensualidad, @ano, @mes);
					print m.MensualidadNueva mensualidad,
						m.Discrecional discrecional,
						m.Comprometido comprometido,
						m.PorDefinir porDefinir,
						m.CuantosSinMarcar cuantosSinMarcar,
						m.TechoDeLoConocido techo,
						m.AlcanzaConLoConocido alcanza,
						m.LaRespuestaEstaCompleta respuestaCompleta,
						m.Lectura lectura;
				}
			")
			.WithParameters(p => {
				p["mensualidad", typeof(decimal)] = mensualidadNueva;
				p["ano",         typeof(int)]     = ano;
				p["mes",         typeof(int)]     = mes;
			})
			.PerformQuery();

		// Pregunta 4 — LA AUSENCIA. La que ninguna pantalla puede contestar.
		//
		// `hoy` viaja como parametro y no lo lee el dominio: el modelo no toca el reloj.
		public string QueNoMeCobraron(DateTime hoy) =>
			app.Using(@"
				foreach (pendiente in panorama.QueNoMeCobraron(@hoy)) {
					print pendiente.NombreDelGasto gasto,
						pendiente.HayHistoria hayHistoria,
						pendiente.EstaVencido estaVencido,
						pendiente.Esperado esperado,
						pendiente.DiasDeAtraso diasDeAtraso,
						pendiente.NombreDeLaSeveridad severidad,
						pendiente.OrdenDeLaSeveridad ordenDeLaSeveridad,
						pendiente.QueHacer accion,
						pendiente.Lectura lectura;
				}
			")
			.WithParameters(p => {
				p["hoy", typeof(DateTime)] = hoy;
			})
			.PerformQuery();

		// Pregunta 5 — .cuanto apartar? La tasa se cuenta sobre EVENTOS, no sobre cargos.
		public string CuantoApartarPorMes(DateTime desde, DateTime hasta) =>
			app.Using(@"
				{
					t = panorama.CuantoApartarPorMes(@desde, @hasta);
					print t.Cuantos cuantos,
						t.Total total,
						t.Meses meses,
						t.HayTasa hayTasa,
						t.PorMes porMes,
						t.CadaCuantosMeses cadaCuantosMeses,
						t.Lectura lectura;
				}
			")
			.WithParameters(p => {
				p["desde", typeof(DateTime)] = desde;
				p["hasta", typeof(DateTime)] = hasta;
			})
			.PerformQuery();

		public string TotalConsumidoEn(int ano, int mes) =>
			app.Using("print panorama.TotalConsumidoEn(@ano, @mes) total;")
			.WithParameters(p => {
				p["ano", typeof(int)] = ano;
				p["mes", typeof(int)] = mes;
			})
			.PerformQuery();
	}
}
