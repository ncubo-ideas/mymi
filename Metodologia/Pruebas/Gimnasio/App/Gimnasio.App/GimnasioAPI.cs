using System;
using Choreography.Theater;
using Puppeteer;

namespace Gimnasio.App
{
	// LA SUPERFICIE INTENCIONAL. Un metodo por intencion, con nombre de negocio.
	// La consola, el browser y el asistente entran todos por aca; nadie escribe scripts.
	//
	// OJO CON LOS NOMBRES DE PARAMETRO. Ninguno se llama como un tipo del dominio: un
	// @medida taparia a la clase Medida y el script dejaria de resolver (regla 10 del
	// substrato). Por eso son @nombreDeLaMedida, @diaQueFue, @inicioDelPlan.
	public sealed class GimnasioAPI
	{
		private readonly PerformanceV2 app;

		public GimnasioAPI(PerformanceV2 app)
		{
			ArgumentNullException.ThrowIfNull(app);
			this.app = app;
		}

		// ------------------------------------------------------------------ comandos

		public string DeclararPerfil(string genero, int edad, decimal alturaEnCentimetros)
		{
			return app.Using(
				scriptForChk: @"
					Check(trayectoria.PuedeDeclararPerfil(@generoDeclarado, @edad, @altura)) Error 'el perfil ya estaba declarado, o los datos no son validos';
				",
				@"
					{
						trayectoria.DeclararPerfil(Perfil(Genero.PorNombre(@generoDeclarado), @edad, @altura));
					}
				")
			.WithParameters(p => {
				p["generoDeclarado", typeof(string)] = genero;
				p["edad", typeof(int)] = edad;
				p["altura", typeof(decimal)] = alturaEnCentimetros;
			})
			.PerformCheckThenCommand();
		}

		// El objetivo se crea vacio y despues se le agregan metas. Dos comandos y no uno
		// porque una meta puede llegar meses despues de la otra.
		public string DeclararObjetivo()
		{
			return app.Using(
				scriptForChk: @"
					Check(!trayectoria.TieneObjetivo) Error 'el objetivo ya estaba declarado';
				",
				@"
					{
						trayectoria.DeclararObjetivo(Objetivo());
					}
				")
			.PerformCheckThenCommand();
		}

		public string AgregarMetaConValor(string nombreDeLaMedida, string haciaDonde, decimal valorDeseado)
		{
			return app.Using(
				scriptForChk: @"
					Check(trayectoria.SePuedeAgregarMetaPara(@nombreDeLaMedida)) Error 'no hay objetivo, o esa medida ya tiene meta';
				",
				@"
					{
						trayectoria.Objetivo.Agregar(
							Meta.ConValor(@nombreDeLaMedida, Direccion.PorNombre(@haciaDonde), @valorDeseado));
					}
				")
			.WithParameters(p => {
				p["nombreDeLaMedida", typeof(string)] = nombreDeLaMedida;
				p["haciaDonde", typeof(string)] = haciaDonde;
				p["valorDeseado", typeof(decimal)] = valorDeseado;
			})
			.PerformCheckThenCommand();
		}

		public string AgregarMetaSoloDireccion(string nombreDeLaMedida, string haciaDonde)
		{
			return app.Using(
				scriptForChk: @"
					Check(trayectoria.SePuedeAgregarMetaPara(@nombreDeLaMedida)) Error 'no hay objetivo, o esa medida ya tiene meta';
				",
				@"
					{
						trayectoria.Objetivo.Agregar(
							Meta.SoloDireccion(@nombreDeLaMedida, Direccion.PorNombre(@haciaDonde)));
					}
				")
			.WithParameters(p => {
				p["nombreDeLaMedida", typeof(string)] = nombreDeLaMedida;
				p["haciaDonde", typeof(string)] = haciaDonde;
			})
			.PerformCheckThenCommand();
		}

		// EL PLAN SE IDENTIFICA POR SU FECHA DE INICIO. En C# alcanzaba con la referencia
		// al objeto; desde un script hace falta un NOMBRE, y el gimnasio no le pone
		// ninguno al papel. Ver el hueco 10.
		public string RegistrarPlan(DateTime inicio, int diasPorSemana)
		{
			return app.Using(
				scriptForChk: @"
					Check(trayectoria.PuedeRegistrarPlan(@inicioDelPlan, @diasPorSemana)) Error 'ya hay un plan que empieza ese dia, o la frecuencia no es valida';
				",
				@"
					{
						trayectoria.Agregar(Plan(@inicioDelPlan, @diasPorSemana));
					}
				")
			.WithParameters(p => {
				p["inicioDelPlan", typeof(DateTime)] = inicio;
				p["diasPorSemana", typeof(int)] = diasPorSemana;
			})
			.PerformCheckThenCommand();
		}

		public string PrescribirEjercicio(DateTime inicioDelPlan, string nombreDelEjercicio, decimal pesoIndicado)
		{
			return app.Using(
				scriptForChk: @"
					Check(trayectoria.PuedePrescribir(@inicioDelPlan, @nombreDelEjercicio, @pesoIndicado)) Error 'no hay plan ese dia, o ya prescribe ese ejercicio';
				",
				@"
					{
						trayectoria.PlanQueEmpezo(@inicioDelPlan)
							.Prescribir(EjercicioPrescrito(@nombreDelEjercicio, @pesoIndicado));
					}
				")
			.WithParameters(p => {
				p["inicioDelPlan", typeof(DateTime)] = inicioDelPlan;
				p["nombreDelEjercicio", typeof(string)] = nombreDelEjercicio;
				p["pesoIndicado", typeof(decimal)] = pesoIndicado;
			})
			.PerformCheckThenCommand();
		}

		public string TerminarPlan(DateTime inicioDelPlan, DateTime fin)
		{
			return app.Using(
				scriptForChk: @"
					Check(trayectoria.PuedeTerminarPlan(@inicioDelPlan, @finDelPlan)) Error 'ese plan no existe, ya termino, o terminaria antes de empezar';
				",
				@"
					{
						trayectoria.PlanQueEmpezo(@inicioDelPlan).Terminar(@finDelPlan);
					}
				")
			.WithParameters(p => {
				p["inicioDelPlan", typeof(DateTime)] = inicioDelPlan;
				p["finDelPlan", typeof(DateTime)] = fin;
			})
			.PerformCheckThenCommand();
		}

		public string Asistio(DateTime dia)
		{
			return app.Using(
				scriptForChk: @"
					Check(trayectoria.PuedeRegistrarAsistencia(@diaQueFue)) Error 'ese dia ya estaba registrado';
				",
				@"
					{
						trayectoria.Asistio(@diaQueFue);
					}
				")
			.WithParameters(p => {
				p["diaQueFue", typeof(DateTime)] = dia;
			})
			.PerformCheckThenCommand();
		}

		// LA MEDIDA SE CREA POR NOMBRE. Las cuatro son un conjunto cerrado, y desde un
		// script no se puede elegir una subclase con un ternario -regla 12- ni nombrarla
		// sin un resolutor total -regla 11-. Ver el hueco 11.
		public string RegistrarMedida(DateTime fechaDeLaMedicion, string queSeMidio, decimal valor)
		{
			return app.Using(
				scriptForChk: @"
					Check(trayectoria.PuedeRegistrarMedida(@fechaDeLaMedicion, @queSeMidio, @valor)) Error 'esa medida no existe, el valor no sirve, o ya estaba en esa hoja';
				",
				@"
					{
						trayectoria.AbrirMedicionDe(@fechaDeLaMedicion).Registrar(Medida.Crear(@queSeMidio, @valor));
					}
				")
			.WithParameters(p => {
				p["fechaDeLaMedicion", typeof(DateTime)] = fechaDeLaMedicion;
				p["queSeMidio", typeof(string)] = queSeMidio;
				p["valor", typeof(decimal)] = valor;
			})
			.PerformCheckThenCommand();
		}

		// ----------------------------------------------------------------- consultas

		// Escalares TIPADOS por parametro Out, no parseando el print -regla 16-.
		public int CuantasVecesSeMidio(string nombreDeLaMedida)
		{
			using (RentedParameter alquilados = app.Actor.RentedParameters())
			{
				app.Actor.Using("@cuantas = trayectoria.CuantasVecesSeMidio(@nombreDeLaMedida);")
				.WithParameters(alquilados, p => {
					p["nombreDeLaMedida", typeof(string)] = nombreDeLaMedida;
					p[Parameter.Out, "cuantas", typeof(int)] = default;
				})
				.PerformQuery();

				return alquilados["cuantas"].GetValue<int>();
			}
		}

		public decimal CuantoSubioLoIndicado(string nombreDelEjercicio)
		{
			using (RentedParameter alquilados = app.Actor.RentedParameters())
			{
				app.Actor.Using("@cuanto = trayectoria.CuantoSubioLoIndicado(@nombreDelEjercicio);")
				.WithParameters(alquilados, p => {
					p["nombreDelEjercicio", typeof(string)] = nombreDelEjercicio;
					p[Parameter.Out, "cuanto", typeof(decimal)] = default;
				})
				.PerformQuery();

				return alquilados["cuanto"].GetValue<decimal>();
			}
		}

		// LOS DOS JUICIOS EN UNA SOLA VISTA, y ese es el punto: la lectura y la tendencia
		// no se deducen entre si, asi que la app tiene que poder decir las dos.
		//
		// La Referencia se construye DENTRO del bloque: en una consulta una asignacion de
		// primer nivel seria escritura de global, prohibida en contexto de solo lectura.
		public string ComoVoy(string nombreDeLaMedida,
			decimal bajoHasta, decimal altoDesde, decimal ruido, decimal noCreible)
		{
			return app.Using(@"
				{
					referencia = Referencia(@bajoHasta, @altoDesde, @ruido, @noCreible);
					lectura = trayectoria.ComoEstoy(@nombreDeLaMedida, referencia);
					tendencia = trayectoria.ComoVengo(@nombreDeLaMedida, referencia);
					print lectura.ValorMedido medido,
						lectura.QueSeComparo seCompara,
						lectura.ValorComparado comparado,
						lectura.ToString() estado,
						tendencia.ToString() movimiento,
						tendencia.Cambio cambio,
						tendencia.Urgencia().ToString() urgencia;
				}
			")
			.WithParameters(p => {
				p["nombreDeLaMedida", typeof(string)] = nombreDeLaMedida;
				p["bajoHasta", typeof(decimal)] = bajoHasta;
				p["altoDesde", typeof(decimal)] = altoDesde;
				p["ruido", typeof(decimal)] = ruido;
				p["noCreible", typeof(decimal)] = noCreible;
			})
			.PerformQuery();
		}

		public string IncumplimientoDelPlan(DateTime inicioDelPlan)
		{
			return app.Using(@"
				{
					incumplimiento = trayectoria.IncumplimientoDe(trayectoria.PlanQueEmpezo(@inicioDelPlan));
					print incumplimiento.DiasEsperados esperados,
						incumplimiento.DiasQueFue fue,
						incumplimiento.DiasQueFaltaron faltaron,
						incumplimiento.Cumplio cumplio;
				}
			")
			.WithParameters(p => {
				p["inicioDelPlan", typeof(DateTime)] = inicioDelPlan;
			})
			.PerformQuery();
		}

		// LA AUSENCIA. Proyectada hoja por hoja: una coleccion de objetos de dominio es
		// material, no un valor, y decidir que se ve de ella es del actor -regla 13-.
		public string PlanesSinMedicion(int diasDeGracia)
		{
			return app.Using(@"
				foreach (elPlan in trayectoria.PlanesQueTerminaronSinMedicion(@diasDeGracia)) {
					print elPlan.Inicio empezo,
						elPlan.Fin termino,
						elPlan.SemanasQueDuro() semanas;
				}
			")
			.WithParameters(p => {
				p["diasDeGracia", typeof(int)] = diasDeGracia;
			})
			.PerformQuery();
		}
	}
}
