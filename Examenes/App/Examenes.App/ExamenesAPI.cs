using System;
using System.Collections.Generic;
using Choreography.Theater;
using Examenes.App.Eventos;
using Examenes.App.Vistas;
using Puppeteer;
using Puppets.Portador;

namespace Examenes.App
{
	// LA SUPERFICIE INTENCIONAL de la app. Todo el mundo entra por aca: la UI, el bucle
	// de refresh y el servidor MCP. Nadie habla con el actor crudo.
	//
	// Es tambien el IDiarioDeLaApp que el Portador necesita: el bucle no sabe hacer
	// performs, sabe pedirle a esto que aplique un mensaje.
	public sealed class ExamenesAPI : IDiarioDeLaApp
	{
		private readonly PerformanceV2 app;

		public ExamenesAPI(PerformanceV2 app)
		{
			ArgumentNullException.ThrowIfNull(app);
			this.app = app;
		}

		// ---------------------------------------------------------------- comandos

		// La clave de ESTA instalacion ante el proveedor. Se genera en C# y entra como
		// parametro, no dentro del DSL: asi el valor queda journalizado y el replay lo
		// reusa. Un Guid.NewGuid() en codigo de dominio daria una clave distinta en cada
		// replay y la app perderia su identidad ante el proveedor.
		public void RegistrarDevice(string deviceKey)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(deviceKey);

			app.Using("bitacora.RegistrarDevice(@device);")
			.WithParameters(p => {
				p["device", typeof(string)] = deviceKey;
			})
			.PerformCommand();
		}

		public void GuardarCredencial(string token, string refreshToken)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(token);

			app.Using("bitacora.GuardarCredencial(@token, @refresh);")
			.WithParameters(p => {
				p["token",   typeof(string)] = token;
				p["refresh", typeof(string)] = refreshToken ?? "";
			})
			.PerformCommand();
		}

		// Aplica UN evento como UN comando. Devuelve "" si entro, o el mensaje del Check
		// si se rechazo.
		//
		// El switch por tipo de examen vive ACA, en la frontera, y es lo unico que
		// traduce el string del wire a una clase del dominio. De este punto para adentro
		// no hay discriminadores: cada rama construye el tipo concreto y el dominio
		// trabaja con Colesterol, AntigenoProstatico o ExamenDeOrina, no con "examen".
		//
		// Cada rama es un comando distinto porque cada constructor del dominio es
		// distinto - y eso es una ventaja, no repeticion: el texto que queda en el
		// journal dice QUE examen entro, y se puede leer sin desempaquetar un JSON.
		public string AplicarExamenAgregado(ExamenAgregado evento)
		{
			ArgumentNullException.ThrowIfNull(evento);
			if (evento.Evento <= 0)
				throw new ArgumentOutOfRangeException(nameof(evento), evento.Evento,
					"El numero de evento debe ser positivo.");

			switch (evento.Examen)
			{
				case ExamenAgregado.Colesterol:
					return UnValor(evento, "Colesterol");
				case ExamenAgregado.AntigenoProstatico:
					return UnValor(evento, "AntigenoProstatico");
				case ExamenAgregado.Hemoglobina:
					return UnValor(evento, "Hemoglobina");
				case ExamenAgregado.ColesterolDetallado:
					return ColesterolDetallado(evento);
				case ExamenAgregado.Orina:
					return Orina(evento);
				default:
					throw new ArgumentException(
						$"Esta app no tiene comando para el examen '{evento.Examen}'. La ruta debio devolver null "
						+ "para descartarlo.", nameof(evento));
			}
		}

		// Los tres analitos que se construyen igual: fecha, valor y unidad. El nombre de
		// la clase entra en el TEXTO del comando, no como parametro, porque es parte de
		// que comando es: el journal guarda "AntigenoProstatico(...)" y al reproducirlo
		// construye esa clase.
		//
		// El checkpoint (bitacora.Avanzar) va DENTRO del mismo comando que inserta el
		// examen: eso es lo que hace exacto el resume tras una caida. O entraron los dos
		// o no entro ninguno; no existe el estado "guarde el examen pero no avance el
		// checkpoint", que seria el que duplica al reintentar.
		//
		// El Check es la idempotencia. Un evento redelivered se rechaza bajo el read lock
		// y NO deja entrada en el journal - por eso es un Check y no un if en el cuerpo,
		// que si persistiria una entrada no-op por cada reintento.
		private string UnValor(ExamenAgregado evento, string claseDelDominio) =>
			app.Using(
				scriptForChk: @"
					Check(@evento > bitacora.UltimoEvento) Error 'evento ya procesado';
				",
				@"
					{
						examen = " + claseDelDominio + @"(@fecha, @valor, @unidad);
						expediente.Agregar(examen);
						bitacora.Avanzar(@evento);
					}
				")
			.WithParameters(p => {
				p["evento", typeof(long)]     = evento.Evento;
				p["fecha",  typeof(DateTime)] = evento.Fecha;
				p["valor",  typeof(decimal)]  = evento.Valor;
				p["unidad", typeof(string)]   = evento.Unidad;
			})
			.PerformCheckThenCommand();

		// El colesterol fraccionado lleva DOS valores: LDL (el que forma la serie) y HDL.
		private string ColesterolDetallado(ExamenAgregado evento) =>
			app.Using(
				scriptForChk: @"
					Check(@evento > bitacora.UltimoEvento) Error 'evento ya procesado';
				",
				@"
					{
						examen = ColesterolDetallado(@fecha, @ldl, @hdl, @unidad);
						expediente.Agregar(examen);
						bitacora.Avanzar(@evento);
					}
				")
			.WithParameters(p => {
				p["evento", typeof(long)]     = evento.Evento;
				p["fecha",  typeof(DateTime)] = evento.Fecha;
				p["ldl",    typeof(decimal)]  = evento.Valor;
				p["hdl",    typeof(decimal)]  = evento.ValorSecundario;
				p["unidad", typeof(string)]   = evento.Unidad;
			})
			.PerformCheckThenCommand();

		// La orina no tiene valor ni unidad: informa un COLOR y unos NITRITOS, que son dos
		// formas de magnitud distintas y ninguna de las dos es un numero. El comando es
		// distinto porque el examen es distinto, y eso es exactamente lo que la jerarquia
		// del dominio esta diciendo.
		//
		// Los journals ya escritos tienen la forma de DOS argumentos. El dominio conserva
		// ese constructor a proposito (ver ExamenDeOrina), asi que un expediente viejo
		// sigue reproduciendo igual; lo que entra de ahora en adelante lleva los tres.
		private string Orina(ExamenAgregado evento) =>
			app.Using(
				scriptForChk: @"
					Check(@evento > bitacora.UltimoEvento) Error 'evento ya procesado';
				",
				@"
					{
						examen = ExamenDeOrina(@fecha, @color, @nitritos);
						expediente.Agregar(examen);
						bitacora.Avanzar(@evento);
					}
				")
			.WithParameters(p => {
				p["evento",   typeof(long)]     = evento.Evento;
				p["fecha",    typeof(DateTime)] = evento.Fecha;
				p["color",    typeof(string)]   = evento.Color;
				p["nitritos", typeof(string)]   = evento.Nitritos ?? "";
			})
			.PerformCheckThenCommand();

		// LO QUE LA PERSONA LE DEJA CONSULTAR A SU IA. Es un comando y no una preferencia
		// local a proposito: el servidor MCP es otro proceso y lo unico que comparte con
		// la app es el journal. Guardado afuera, el toggle no gobernaria nada.
		public void PermitirConsultaDeLaIA(string consulta, bool permitida)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(consulta);

			// El verbo entra en el TEXTO del comando y no como parametro: asi el journal
			// dice "permisos.Denegar('velocidad')" y se lee sin desempaquetar nada.
			string verbo = permitida ? "Permitir" : "Denegar";

			app.Using("permisos." + verbo + "(@consulta);")
			.WithParameters(p => {
				p["consulta", typeof(string)] = consulta;
			})
			.PerformCommand();
		}

		// ---------------------------------------------------------------- consultas

		// Si la persona habilito una consulta. La usan las dos superficies: la pantalla
		// para pintar el toggle, y el servidor MCP para negarse antes de contestar.
		public bool ConsultaDeLaIAPermitida(string consulta)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(consulta);

			using (RentedParameter alquilados = app.Actor.RentedParameters())
			{
				app.Actor.Using("@permitida = permisos.EstaPermitida(@consulta);")
				.WithParameters(alquilados, p => {
					p["consulta", typeof(string)] = consulta;
					p[Parameter.Out, "permitida", typeof(bool)] = default;
				})
				.PerformQuery();

				return alquilados["permitida"].GetValue<bool>();
			}
		}

		public int ConsultasDenegadas()
		{
			using (RentedParameter alquilados = app.Actor.RentedParameters())
			{
				app.Actor.Using("@cuantas = permisos.CantidadDenegada;")
				.WithParameters(alquilados, p => {
					p[Parameter.Out, "cuantas", typeof(int)] = default;
				})
				.PerformQuery();

				return alquilados["cuantas"].GetValue<int>();
			}
		}

		// Valores TIPADOS, no texto parseado: es lo que le permite a las tools MCP
		// devolver structuredContent con numeros de verdad, y a la UI hacer aritmetica
		// sin adivinar el formato del print.
		public int CantidadDeExamenes()
		{
			using (RentedParameter alquilados = app.Actor.RentedParameters())
			{
				app.Actor.Using("@cantidad = expediente.Cantidad;")
				.WithParameters(alquilados, p => {
					p[Parameter.Out, "cantidad", typeof(int)] = default;
				})
				.PerformQuery();

				return alquilados["cantidad"].GetValue<int>();
			}
		}

		public int AnalitosFueraDeRango()
		{
			using (RentedParameter alquilados = app.Actor.RentedParameters())
			{
				app.Actor.Using("@fuera = expediente.AnalitosFueraDeRango;")
				.WithParameters(alquilados, p => {
					p[Parameter.Out, "fuera", typeof(int)] = default;
				})
				.PerformQuery();

				return alquilados["fuera"].GetValue<int>();
			}
		}

		public int EdadDelPaciente()
		{
			using (RentedParameter alquilados = app.Actor.RentedParameters())
			{
				app.Actor.Using("@edad = expediente.Edad;")
				.WithParameters(alquilados, p => {
					p[Parameter.Out, "edad", typeof(int)] = default;
				})
				.PerformQuery();

				return alquilados["edad"].GetValue<int>();
			}
		}

		public long UltimoEventoProcesado()
		{
			using (RentedParameter alquilados = app.Actor.RentedParameters())
			{
				app.Actor.Using("@ultimo = bitacora.UltimoEvento;")
				.WithParameters(alquilados, p => {
					p[Parameter.Out, "ultimo", typeof(long)] = default;
				})
				.PerformQuery();

				return alquilados["ultimo"].GetValue<long>();
			}
		}

		public string TokenGuardado() => Texto("bitacora.Token");

		public string RefreshTokenGuardado() => Texto("bitacora.RefreshToken");

		public string DeviceKeyGuardado() => Texto("bitacora.DeviceKey");

		private string Texto(string expresion)
		{
			using (RentedParameter alquilados = app.Actor.RentedParameters())
			{
				app.Actor.Using($"@valor = {expresion};")
				.WithParameters(alquilados, p => {
					p[Parameter.Out, "valor", typeof(string)] = default;
				})
				.PerformQuery();

				return alquilados["valor"].GetValue<string>() ?? "";
			}
		}

		// La lista de examenes. Devuelve la emision del print tal cual - con el formatter
		// JSON eso YA es el JSON serializado, asi que no hay DTO ni re-serializacion en
		// el medio.
		//
		// La proyeccion se AUTORA aca, hoja por hoja. No se puede hacer
		// `print expediente.Lista`: el motor lo rechaza porque una coleccion de objetos
		// de dominio es material, no un valor, y decidir que se ve de ella es del actor,
		// no del material.
		//
		// Notar que la lista imprime ResultadoExpresado y no un decimal: es lo que
		// permite que un colesterol ("280 mg/dL") y una orina ("ambar") convivan en la
		// misma lista sin que ninguno tenga que mentir sobre su forma. La jerarquia del
		// dominio se paga aca.
		public string ListaDeExamenes() =>
			app.Using(@"
				foreach (examen in expediente.Lista) {
					print examen.Nombre nombre,
						examen.Fecha fecha,
						examen.ResultadoExpresado resultado,
						examen.AportaSerie aportaSerie;
				}
			")
			.PerformQuery();

		// LA consulta que justifica el dominio. Evalua la SERIE de un analito, no su
		// ultimo valor.
		//
		// El bloque mantiene `evaluacion` local: en una consulta una asignacion de primer
		// nivel seria escritura de global, prohibida en contexto read-only.
		public string TendenciaDe(string analito)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(analito);

			return app.Using(@"
				{
					evaluacion = expediente.EvaluarTendenciaDe(@analito);
					print evaluacion.Analito analito,
						evaluacion.AnalitoParaMostrar paraMostrar,
						evaluacion.NombreDeLaTendencia tendencia,
						evaluacion.CantidadDeMediciones mediciones,
						evaluacion.ValorInicial valorInicial,
						evaluacion.ValorFinal valorFinal,
						evaluacion.Unidad unidad,
						evaluacion.DiasTranscurridos dias,
						evaluacion.Cambio cambio,
						evaluacion.ExigeAtencion exigeAtencion,
						evaluacion.SugiereRepetirElExamen sugiereRepetirElExamen,
						evaluacion.HaySerie haySerie;
				}
			")
			.WithParameters(p => {
				p["analito", typeof(string)] = analito;
			})
			.PerformQuery();
		}

		// A QUE RITMO viene cambiando un analito. Es la segunda pregunta que exige la serie
		// completa, y para el antigeno prostatico es LA pregunta: la practica clinica no
		// decide por el valor absoluto sino por el cambio por ano.
		//
		// Va aparte de TendenciaDe y no como dos campos mas de la misma respuesta porque
		// son dos preguntas distintas sobre tramos distintos: la tendencia mira la serie
		// entera, la velocidad mira el tramo mas reciente. Fusionarlas obligaria a una de
		// las dos a mentir sobre que miro.
		public string VelocidadDe(string analito)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(analito);

			return app.Using(@"
				{
					velocidad = expediente.VelocidadDe(@analito);
					print velocidad.Analito analito,
						velocidad.AnalitoParaMostrar paraMostrar,
						velocidad.PorAno porAno,
						velocidad.Unidad unidad,
						velocidad.DiasDelTramo dias,
						velocidad.UmbralPorAno umbral,
						velocidad.EsAplicable esAplicable,
						velocidad.ExigeAtencion exigeAtencion,
						velocidad.Expresada expresada;
				}
			")
			.WithParameters(p => {
				p["analito", typeof(string)] = analito;
			})
			.PerformQuery();
		}

		// Los analitos de los que hay historia. La IA lo usa para saber por que puede
		// preguntar sin adivinar nombres.
		public string AnalitosConHistoria() =>
			app.Using(@"
				foreach (analito in expediente.Analitos) {
					print analito nombre;
				}
			")
			.PerformQuery();

		// Las mismas vistas, ya tipadas. Es lo que consumen la UI y las tools MCP; las
		// variantes que devuelven el string crudo quedan para diagnostico.
		public IReadOnlyList<ExamenEnLista> Examenes() =>
			ProyeccionDeVistas.Lista(ListaDeExamenes());

		public TendenciaDeAnalito Tendencia(string analito) =>
			ProyeccionDeVistas.Tendencia(TendenciaDe(analito));

		public VelocidadDeAnalito Velocidad(string analito) =>
			ProyeccionDeVistas.Velocidad(VelocidadDe(analito));

		public IReadOnlyList<string> Analitos() =>
			ProyeccionDeVistas.Analitos(AnalitosConHistoria());

		// ------------------------------------------------- IDiarioDeLaApp (Portador)

		string IDiarioDeLaApp.Aplicar(IMensajeDeEvento mensaje)
		{
			ArgumentNullException.ThrowIfNull(mensaje);

			if (mensaje is ExamenAgregado agregado) return AplicarExamenAgregado(agregado);

			throw new ArgumentException(
				$"Esta app no tiene comando para un mensaje de tipo '{mensaje.Tipo}'. La ruta debio devolver null "
				+ "para descartarlo.", nameof(mensaje));
		}
	}
}
