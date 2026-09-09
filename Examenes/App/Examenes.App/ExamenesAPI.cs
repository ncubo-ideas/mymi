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

		// LO QUE LA PERSONA DICE DE SI MISMA. Procedencia TU, y por eso es un comando local
		// y no un evento: nadie se lo publico, lo declaro ella.
		//
		// Es el comando que Alvaro pidio que la pantalla insista en conseguir, y con el
		// pasa la cosa mas linda de todo este cambio: EN EL MOMENTO EN QUE ENTRA, TODO EL
		// HISTORIAL SE VUELVE INTERPRETABLE. No hay que recalcular nada ni migrar eventos,
		// porque el brief ya establecia que el rango de referencia NUNCA se guarda con la
		// medicion —se calcula al consultar, porque depende de edad y sexo, que pueden
		// cambiar—. Ocho tarjetas pasan de "requiere sexo" a un veredicto real, hacia atras
		// en el tiempo, con un solo comando.
		//
		// Sin Check de idempotencia, y a proposito: esto NO viene de la narrativa del
		// proveedor, viene de un boton. Volver a declarar el mismo sexo es una operacion
		// legitima —la persona se corrige, cambia de opinion— y cada declaracion es un
		// hecho fechado que el journal conserva.
		public void DeclararSexo(string sexo)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(sexo);

			app.Using("expediente.DeclararSexo(@sexo);")
			.WithParameters(p => {
				p["sexo", typeof(string)] = sexo;
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

			if (evento.EsDeOrina) return Orina(evento);
			if (evento.EsDeHeces) return Heces(evento);
			if (evento.TraeFraccionamiento) return ColesterolDetallado(evento);

			return UnValor(evento, ClaseDelDominio(evento.Analito));
		}

		// La ULTIMA traduccion que queda: del nombre canonico del analito al nombre de la
		// clase que el comando construye. Vive aca y no en el dominio porque es un detalle
		// del DSL -el journal guarda texto y ese texto nombra una clase-, no conocimiento
		// clinico.
		//
		// Compara contra NombresDeAnalito, que es texto y no una referencia a Analito: desde
		// que el dominio es enteramente `internal`, esta Superficie no puede nombrar un tipo
		// suyo en C#, y no le hace falta porque la ruta ya resolvio el sinonimo del
		// laboratorio a su nombre canonico. Comprobaciones amarra esas constantes al dominio.
		private static string ClaseDelDominio(string analito)
		{
			if (analito == NombresDeAnalito.ColesterolTotal) return "Colesterol";
			if (analito == NombresDeAnalito.AntigenoProstatico) return "AntigenoProstatico";
			if (analito == NombresDeAnalito.Hemoglobina) return "Hemoglobina";

			// La quimica general. Los siete se construyen igual —fecha, valor y unidad— asi
			// que entran por el mismo comando UnValor; lo unico que cambia es el nombre de la
			// clase que el journal va a guardar.
			if (analito == NombresDeAnalito.Trigliceridos) return "Trigliceridos";
			if (analito == NombresDeAnalito.GlucosaEnAyunas) return "GlucosaEnAyunas";
			if (analito == NombresDeAnalito.NitrogenoUreico) return "NitrogenoUreico";
			if (analito == NombresDeAnalito.Sodio) return "Sodio";
			if (analito == NombresDeAnalito.Potasio) return "Potasio";
			if (analito == NombresDeAnalito.Cloro) return "Cloro";
			if (analito == NombresDeAnalito.ProteinasTotales) return "ProteinasTotales";

			// El conjunto de analitos es mas grande que el de examenes construibles: el
			// LDL entra fraccionado y el HDL no entra solo, y las dos cosas ya las filtro
			// la ruta. Llegar aca significa que se agrego un analito y no su comando.
			throw new ArgumentException(
				$"Esta app no tiene comando para construir un examen de '{analito}'. "
				+ "La ruta debio devolver null para descartarlo.", nameof(analito));
		}

		// Los analitos que se construyen igual: fecha, valor y unidad. El nombre de
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

		// LO QUE PUBLICA EL LABORATORIO sobre el paciente. Procedencia INSTITUCION, y es un
		// comando distinto de DeclararSexo porque es OTRO hecho: el journal guarda
		// "expediente.PublicarSexo(...)" y al reproducirlo se sabe quien lo dijo sin
		// desempaquetar nada. Los dos conviven y para interpretar gana la persona.
		//
		// Este SI lleva el Check de idempotencia y el checkpoint adentro del mismo comando,
		// igual que los examenes: viene de la narrativa del proveedor y un redelivered no
		// puede dejar entrada en el journal.
		public string AplicarDatosDePaciente(DatosDePacienteActualizados evento)
		{
			ArgumentNullException.ThrowIfNull(evento);
			if (evento.Evento <= 0)
				throw new ArgumentOutOfRangeException(nameof(evento), evento.Evento,
					"El numero de evento debe ser positivo.");

			return app.Using(
				scriptForChk: @"
					Check(@evento > bitacora.UltimoEvento) Error 'evento ya procesado';
				",
				@"
					{
						expediente.PublicarSexo(@sexo);
						bitacora.Avanzar(@evento);
					}
				")
			.WithParameters(p => {
				p["evento", typeof(long)]   = evento.Evento;
				p["sexo",   typeof(string)] = evento.Sexo;
			})
			.PerformCheckThenCommand();
		}

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

		// La orina no tiene valor ni unidad: informa un COLOR y cinco binarios, que son dos
		// formas de magnitud distintas y ninguna de las dos es un numero. El comando es
		// distinto porque el examen es distinto, y eso es exactamente lo que la jerarquia
		// del dominio esta diciendo.
		//
		// Los journals ya escritos tienen la forma de DOS, de TRES y de CUATRO argumentos. El
		// dominio conserva esos constructores a proposito (ver ExamenDeOrina), asi que un
		// expediente viejo sigue reproduciendo igual; lo que entra de ahora en adelante lleva
		// la tira entera, que son siete.
		private string Orina(ExamenAgregado evento) =>
			app.Using(
				scriptForChk: @"
					Check(@evento > bitacora.UltimoEvento) Error 'evento ya procesado';
				",
				@"
					{
						examen = ExamenDeOrina(@fecha, @color, @nitritos, @sangre,
							@proteina, @cetonas, @glucosa);
						expediente.Agregar(examen);
						bitacora.Avanzar(@evento);
					}
				")
			.WithParameters(p => {
				p["evento",   typeof(long)]     = evento.Evento;
				p["fecha",    typeof(DateTime)] = evento.Fecha;
				p["color",    typeof(string)]   = evento.Color;
				p["nitritos", typeof(string)]   = evento.Nitritos ?? "";
				p["sangre",   typeof(string)]   = evento.Sangre ?? "";
				p["proteina", typeof(string)]   = evento.Proteina ?? "";
				p["cetonas",  typeof(string)]   = evento.Cetonas ?? "";
				p["glucosa",  typeof(string)]   = evento.GlucosaEnOrina ?? "";
			})
			.PerformCheckThenCommand();

		// El examen de heces. Es su propio comando y no un parametro del de orina porque
		// construye OTRA clase del dominio: el journal guarda "ExamenDeHeces(...)" y al
		// reproducirlo arma esa, con su propia sangre que no se lee igual que la de orina.
		private string Heces(ExamenAgregado evento) =>
			app.Using(
				scriptForChk: @"
					Check(@evento > bitacora.UltimoEvento) Error 'evento ya procesado';
				",
				@"
					{
						examen = ExamenDeHeces(@fecha, @color, @sangre);
						expediente.Agregar(examen);
						bitacora.Avanzar(@evento);
					}
				")
			.WithParameters(p => {
				p["evento", typeof(long)]     = evento.Evento;
				p["fecha",  typeof(DateTime)] = evento.Fecha;
				p["color",  typeof(string)]   = evento.Color;
				p["sangre", typeof(string)]   = evento.Sangre ?? "";
			})
			.PerformCheckThenCommand();

		// ---------------------------------------------------------------- consultas

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

		// EL SEXO QUE GANA, y de donde salio cada afirmacion. Las tres viajan juntas porque
		// la superficie las necesita juntas: para saber si insistir, para mostrar el estado
		// actual, y para poder decir el conflicto en vez de esconderlo.
		public string SexoDelPaciente() => Texto("expediente.Sexo.Nombre");

		public string SexoDeclaradoPorLaPersona() => Texto("expediente.SexoDeclaradoPorLaPersona.Nombre");

		public string SexoPublicadoPorLaInstitucion() => Texto("expediente.SexoPublicadoPorLaInstitucion.Nombre");

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
						examen.AportaSerieContinua aportaSerieContinua;
				}
			")
			.PerformQuery();

		// LO MAS URGENTE DE TODO EL EXPEDIENTE. Es la unica consulta que mira el expediente
		// ENTERO y no un analito, y la unica que contesta lo que la persona viene a
		// preguntar cuando abre la app.
		//
		// Estaba en el dominio desde hace rato y no la consumia nadie. No se puede armar
		// afuera: exige recorrer cada serie, interpretarla con el criterio de SU analito y
		// quedarse con la mayor, y ordenar severidades es del dominio.
		public string LoMasUrgente() =>
			app.Using(@"
				{
					urgencia = expediente.LoMasUrgente;
					print urgencia.NombreDeLaSeveridad severidad,
						urgencia.AccionLegible accion,
						urgencia.PideAlgo pideAlgo;
				}
			")
			.PerformQuery();

		public LoMasUrgente Urgente() => ProyeccionDeVistas.Urgente(LoMasUrgente());

		// LO QUE ESTA BIEN, ya contado y ya dicho. Igual que el pedido de sexo, NO se puede
		// armar en la pantalla: la cuenta exige recorrer cada serie y saber cual se puede
		// interpretar, y la frase exige decidir si va la fraccion o el total.
		public string EstaEnRango() =>
			app.Using(@"
				{
					tranquilidad = expediente.LoQueEstaEnRango;
					print tranquilidad.EnSuRango enSuRango,
						tranquilidad.Analitos analitos,
						tranquilidad.HayAlgoEnRango hayAlgo,
						tranquilidad.ParaSonia paraSonia;
				}
			")
			.PerformQuery();

		public LoQueEstaBien LoQueEstaEnRango() => ProyeccionDeVistas.EstaEnRango(EstaEnRango());

		// EL DATO QUE FALTA, ya contado y ya dicho. Es lo que alimenta el bloque que
		// insiste, y NO se puede armar en la pantalla: el conteo sale de recorrer cada
		// serie e interpretarla, y la frase de saber por que falta cada una.
		public string FaltaElSexo() =>
			app.Using(@"
				{
					pedido = expediente.FaltaElSexo;
					print pedido.HaceFalta haceFalta,
						pedido.SinInterpretar sinInterpretar,
						pedido.Analitos analitos,
						pedido.NombreDeLaSeveridad severidad,
						pedido.ParaSonia paraSonia,
						pedido.ElPorQue porQue;
				}
			")
			.PerformQuery();

		public FaltaElSexoDelPaciente PedidoDeSexo() => ProyeccionDeVistas.PedidoDeSexo(FaltaElSexo());

		// LOS PUNTOS DE LA SERIE, con su numero. Es lo que un grafico necesita y una tabla
		// no: el cuadro historico los da ya convertidos a texto.
		public string PuntosDe(string analito)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(analito);

			return app.Using(@"
				foreach (punto in expediente.PuntosDe(@analito)) {
					print punto.Fecha fecha,
						punto.ValorNumerico valor,
						punto.UnidadDelValor unidad;
				}
			")
			.WithParameters(p => { p["analito", typeof(string)] = analito; })
			.PerformQuery();
		}

		// LAS PRUEBAS DE UNA SERIE BINARIA: fechas con un si o un no. Es el par de PuntosDe
		// para la forma 5, y va aparte porque no devuelve lo mismo — un nitrito no tiene
		// ValorNumerico, y pedirselo seria la mentira que este dominio evita en todos los
		// otros pisos.
		public string PruebasDe(string analito)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(analito);

			return app.Using(@"
				foreach (prueba in expediente.PruebasDe(@analito)) {
					print prueba.Fecha fecha,
						prueba.Expresado expresado,
						prueba.EsPositivo esPositivo,
						prueba.EsHallazgo esHallazgo;
				}
			")
			.WithParameters(p => { p["analito", typeof(string)] = analito; })
			.PerformQuery();
		}

		public IReadOnlyList<PruebaEnSerie> Pruebas(string analito) =>
			ProyeccionDeVistas.Pruebas(PruebasDe(analito));

		// LOS HECHOS DE UNA SERIE BINARIA. Es el par de TendenciaDe, y son dos consultas
		// porque no contestan lo mismo: aquella trae extremos y cambio, esta trae cuantas
		// veces aparecio y cuantas SEGUIDAS.
		public string PresenciaDe(string analito)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(analito);

			return app.Using(@"
				{
					presencia = expediente.EvaluarPresenciaDe(@analito);
					print presencia.Analito analito,
						presencia.AnalitoParaMostrar paraMostrar,
						presencia.NombreDeLaPresencia presencia,
						presencia.CantidadDeMediciones mediciones,
						presencia.Positivas positivas,
						presencia.PositivasSeguidas positivasSeguidas,
						presencia.DiasTranscurridos dias,
						presencia.UltimoResultado ultimoResultado,
						presencia.EstaPresente estaPresente,
						presencia.HaySerie haySerie;
				}
			")
			.WithParameters(p => { p["analito", typeof(string)] = analito; })
			.PerformQuery();
		}

		public PresenciaDeAnalito Presencia(string analito) =>
			ProyeccionDeVistas.Presencia(PresenciaDe(analito));

		// LAS OBSERVACIONES DE UNA SERIE NOMINAL: fechas con una categoria. Es el tercer par
		// de PuntosDe, y va aparte por lo mismo que las pruebas: un color no tiene
		// ValorNumerico ni un si o un no, tiene un nombre.
		public string ObservacionesDe(string analito)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(analito);

			return app.Using(@"
				foreach (observacion in expediente.ObservacionesDe(@analito)) {
					print observacion.Fecha fecha,
						observacion.Expresado expresado,
						observacion.EsEsperable esEsperable,
						observacion.EsHallazgo esHallazgo;
				}
			")
			.WithParameters(p => { p["analito", typeof(string)] = analito; })
			.PerformQuery();
		}

		public IReadOnlyList<ObservacionEnSerie> Observaciones(string analito) =>
			ProyeccionDeVistas.Observaciones(ObservacionesDe(analito));

		// LOS HECHOS DE UNA SERIE NOMINAL. Es el par de PresenciaDe para la forma 6: aquella
		// trae cuantas veces aparecio, esta trae CUAL salio y si volvio a lo esperable.
		public string ConstanciaDe(string analito)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(analito);

			return app.Using(@"
				{
					constancia = expediente.EvaluarConstanciaDe(@analito);
					print constancia.Analito analito,
						constancia.AnalitoParaMostrar paraMostrar,
						constancia.NombreDeLaConstancia constancia,
						constancia.CantidadDeMediciones mediciones,
						constancia.UltimaCategoria ultimaCategoria,
						constancia.UltimaEsEsperable ultimaEsEsperable,
						constancia.NoEsperables noEsperables,
						constancia.NoEsperablesSeguidas noEsperablesSeguidas,
						constancia.LaUltimaSeRepitio laUltimaSeRepitio,
						constancia.DiasTranscurridos dias,
						constancia.EstaFueraDeLoEsperable estaFueraDeLoEsperable,
						constancia.HaySerie haySerie;
				}
			")
			.WithParameters(p => { p["analito", typeof(string)] = analito; })
			.PerformQuery();
		}

		public ConstanciaDeAnalito Constancia(string analito) =>
			ProyeccionDeVistas.Constancia(ConstanciaDe(analito));

		// LAS LINEAS que se dibujan junto a los puntos, y COMO se dibujan. Cada analito
		// emite solo las que tiene; ninguno devuelve cero para rellenar.
		public string ReferenciasDe(string analito)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(analito);

			return app.Using(@"
				foreach (referencia in expediente.ReferenciasDe(@analito)) {
					print referencia.Nombre nombre,
						referencia.Numero valor,
						referencia.Unidad unidad,
						referencia.ParaSonia paraSonia;
				}
			")
			.WithParameters(p => { p["analito", typeof(string)] = analito; })
			.PerformQuery();
		}

		public IReadOnlyList<PuntoDeSerie> Puntos(string analito) =>
			ProyeccionDeVistas.Puntos(PuntosDe(analito));

		public IReadOnlyList<LineaDeReferencia> Referencias(string analito) =>
			ProyeccionDeVistas.Referencias(ReferenciasDe(analito));

		// QUE FALTA MEDIR. La consulta que ninguna pantalla puede contestar, porque una
		// pantalla solo renderiza datos que tiene y esto es una AUSENCIA.
		//
		// `hoy` viaja como parametro y no lo lee el dominio: el modelo no toca el reloj.
		public string ControlesPendientes(DateTime hoy) =>
			app.Using(@"
				foreach (control in expediente.ControlesPendientes(@hoy)) {
					print control.Nombre analito,
						control.ParaMostrar paraMostrar,
						control.UltimaMedicion ultimaMedicion,
						control.DiasDesdeLaUltima diasDesdeLaUltima,
						control.MesesRecomendados mesesRecomendados,
						control.DiasQueFaltan diasQueFaltan,
						control.EstaVencido estaVencido,
						control.NombreDeLaSeveridad severidad,
						control.AccionLegible accion,
						control.ParaSonia paraSonia;
				}
			")
			.WithParameters(p => {
				p["hoy", typeof(DateTime)] = hoy;
			})
			.PerformQuery();

		public IReadOnlyList<ControlEnEspera> Controles(DateTime hoy) =>
			ProyeccionDeVistas.Controles(ControlesPendientes(hoy));

		// EL CUADRO HISTORICO, plano. Ver CeldaDelCuadro en el dominio para por que viaja
		// asi: el print proyecta un solo nivel, y una coleccion de filas que a su vez
		// tienen celdas no cruza. Los indices son los que dejan que la superficie rearme
		// la grilla SIN volver a decidir que va donde.
		public string CuadroHistorico() =>
			app.Using(@"
				foreach (celda in expediente.CeldasDelCuadro) {
					print celda.Fila fila,
						celda.Columna columna,
						celda.Analito analito,
						celda.ParaMostrar paraMostrar,
						celda.Fecha fecha,
						celda.Expresado expresado;
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
						evaluacion.SugiereRepetirElExamen sugiereRepetirElExamen,
						evaluacion.HaySerie haySerie;
				}
			")
			.WithParameters(p => {
				p["analito", typeof(string)] = analito;
			})
			.PerformQuery();
		}

		// LO QUE EL ANALITO CONCLUYE de su serie: veredicto, severidad, la linea para la
		// persona y que hacer.
		//
		// Es la consulta que reemplaza al "exige atencion" generico. Aquella salia de un
		// bool por analito y le ponia la misma etiqueta al antigeno subiendo y al
		// colesterol bajando desde un pico; esta la produce cada analito con su criterio.
		public string InterpretacionDe(string analito)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(analito);

			return app.Using(@"
				{
					lectura = expediente.InterpretarSerieDe(@analito);
					print lectura.Analito analito,
						lectura.AnalitoParaMostrar paraMostrar,
						lectura.NombreDelVeredicto veredicto,
						lectura.NombreDeLaSeveridad severidad,
						lectura.NombreDeLaForma forma,
						lectura.ParaSonia paraSonia,
						lectura.AccionLegible accion,
						lectura.ReferenciaLegible referencia,
						lectura.PideAlgo pideAlgo,
						lectura.SugiereRepetirElExamen sugiereRepetirElExamen,
						lectura.NoLeCorresponde noLeCorresponde,
						lectura.RequiereElSexo requiereElSexo;
				}
			")
			.WithParameters(p => {
				p["analito", typeof(string)] = analito;
			})
			.PerformQuery();
		}

		public InterpretacionDeAnalito Interpretacion(string analito) =>
			ProyeccionDeVistas.Interpretacion(InterpretacionDe(analito));

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

		public IReadOnlyList<CeldaEnCuadro> Cuadro() =>
			ProyeccionDeVistas.Cuadro(CuadroHistorico());

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
			if (mensaje is DatosDePacienteActualizados datos) return AplicarDatosDePaciente(datos);

			throw new ArgumentException(
				$"Esta app no tiene comando para un mensaje de tipo '{mensaje.Tipo}'. La ruta debio devolver null "
				+ "para descartarlo.", nameof(mensaje));
		}
	}
}
