using System;

namespace DealershipCar.Dominio
{
	// LOS UMBRALES DE POLITICA DE LA AGENCIA, con nombre (§6 del slice, E17).
	//
	// DOS CLASES DE NUMERO, DOS CASAS, y confundirlas es el error que este tipo evita:
	//
	//   Los limites POR OPERACION -- cuantos dias, cuanta distancia, en que unidad -- vienen
	//   del PLAN DEL FABRICANTE y son DATOS RECIBIDOS al sembrar la agenda. Viven en
	//   MaintenanceOperationDefinition, como reglas-en-forma-de-dato (E39). No estan aca, y
	//   ponerlos aca convertiria la linea base del fabricante en una constante nuestra.
	//
	//   Los umbrales de POLITICA son estos siete: cuanto se avisa antes, cuanta gracia hay
	//   despues, cuantos aplazamientos y de que largo, cuanto escala cada perfil de uso, y en
	//   que ventana conviene agrupar. Son de la agencia, iguales para todas las operaciones.
	//
	// Ninguno se escribe como literal en otro lado. E17: una cuenta correcta sin nombre es un
	// defecto en este slice, y "30" suelto adentro de una regla no dice si son dias de aviso,
	// dias de gracia o dias de ventana.
	//
	// ===== GAP G5 — ESTO ES UN SEAM, NO UNA CREENCIA ==================================
	//
	// Que sean constantes es una DECISION DE ESTE SLICE, tomada a ojos abiertos y con fecha de
	// vencimiento conocida. El documento de dominio trata estos numeros como CONFIGURACION con
	// fecha de vigencia; la guia (E17) los quiere nombrados en el dominio; E14 dice que lo
	// unico que persiste son los globales raiz del DSL. Los tres no se pueden cumplir a la vez,
	// asi que para OS2 ganan el nombre y la simplicidad.
	//
	// NO VA A SOBREVIVIR A OS5, donde los topes de dinero son POR OTORGAMIENTO y genuinamente
	// no pueden ser constantes: dos personas con dos permisos distintos tienen dos topes
	// distintos al mismo tiempo, y una constante no puede ser dos cosas.
	//
	// Lo que este tipo compra mientras tanto es que ese cambio sea UN cambio. Cuando la
	// configuracion entre, entra por aca -- el tipo pasa de catalogo estatico a algo que se
	// recibe (parametro de la evaluacion, o un segundo global raiz con su instante de
	// vigencia) -- y ninguna regla del dominio se toca, porque ninguna tiene el numero adentro.
	// Ese es todo el valor de tener una sola casa para estos numeros.
	//
	// ==================================================================================
	//
	// TRES DE LOS SIETE SON COMPUESTOS -- "30 dias O 1000 unidades de distancia" -- y se
	// declaran en dos mitades con sufijo Days / Distance. No es una licencia con el canon: dias
	// y unidades de distancia NO SE PUEDEN SUMAR NI COMPARAR, que es OINV-21 otra vez, un nivel
	// mas arriba. Un solo numero llamado DueSoonLead tendria que ser dos cosas incomparables.
	//
	// Y LAS DISTANCIAS DE POLITICA NO LLEVAN UNIDAD, a proposito. Como no hay conversion
	// (OINV-21), el numero se lee en la unidad del limite de la operacion contra la que se
	// compara: 1000 significa 1000 UnitA para una operacion con limites en UnitA, y 1000 UnitB
	// para una en UnitB. Ponerle unidad a la politica obligaria a convertir, que es exactamente
	// lo que no existe.

	/// <summary>
	/// Los umbrales de politica de la agencia (§6 del slice): el anticipo de cortesia, la
	/// gracia, el tope y la ventana de aplazamiento, los factores de perfil de uso y la ventana
	/// de agrupamiento. Iguales para todas las operaciones, a diferencia de los limites por
	/// operacion, que llegan del plan del fabricante como datos recibidos.
	/// <para>
	/// GAP G5: que sean CONSTANTES es una decision de este slice, no una creencia. Este tipo es
	/// EL SEAM POR DONDE VA A ENTRAR LA CONFIGURACION -- con fecha de vigencia, y en OS5 por
	/// otorgamiento, donde un tope de dinero genuinamente no puede ser constante porque dos
	/// permisos distintos tienen dos topes distintos al mismo tiempo. Cuando eso pase, cambia
	/// la forma de este tipo y no cambia ninguna regla, porque ninguna regla lleva el numero
	/// adentro.
	/// </para>
	/// </summary>
	internal static class SchedulePolicy
	{
		/// <summary>
		/// DueSoonLead, mitad de tiempo: cuantos dias antes del vencimiento se avisa. El aviso
		/// es una cortesia y nunca una parada obligatoria (ley L10).
		/// </summary>
		internal const int DueSoonLeadDays = 30;

		/// <summary>
		/// DueSoonLead, mitad de distancia: a cuantas unidades del limite se avisa. Se lee en la
		/// unidad del limite de la operacion, porque no hay conversion (OINV-21).
		/// </summary>
		internal const int DueSoonLeadDistance = 1000;

		/// <summary>
		/// OverdueGrace, mitad de tiempo: cuantos dias pasado el vencimiento se tolera antes de
		/// contar la operacion como no hecha.
		/// </summary>
		internal const int OverdueGraceDays = 14;

		/// <summary>
		/// OverdueGrace, mitad de distancia: cuantas unidades pasado el limite se toleran, en la
		/// unidad del limite de la operacion.
		/// </summary>
		internal const int OverdueGraceDistance = 500;

		/// <summary>
		/// Cuantas veces se puede aplazar UNA operacion (ley L6). Al llegar al tope, el
		/// siguiente intento se RECHAZA -- y eso es un resultado esperado del negocio, o sea un
		/// estado, no una excepcion (E32).
		/// </summary>
		internal const int MaxDeferralsPerOperation = 2;

		/// <summary>
		/// DeferralWindow: cuantos dias dura cada aplazamiento (ley L6). Cumplida la ventana, la
		/// operacion vuelve a Due y la cuenta de aplazamientos usados se conserva.
		/// </summary>
		internal const int DeferralWindowDays = 60;

		/// <summary>
		/// Cuanto del largo declarado queda cuando el perfil de uso es SevereDuty: 0.70, o sea
		/// el 70 por ciento. Escala LARGO y nada mas -- no toca la base ni quita operaciones
		/// (ley L8, OINV-22).
		/// </summary>
		internal const decimal SevereDutyIntervalFactor = 0.70m;

		/// <summary>
		/// Cuanto del largo declarado queda cuando el perfil es LowDistance: 1.00, o sea los
		/// limites de TIEMPO quedan intactos. La otra mitad de esa regla -- que los limites de
		/// distancia se ignoran -- es comportamiento y no un numero: vive con el factor de
		/// intervalo, no aca.
		/// </summary>
		internal const decimal LowDistanceTimeFactor = 1.00m;

		/// <summary>
		/// BundlingWindow, mitad de tiempo: dentro de cuantos dias conviene juntar operaciones
		/// en una sola visita. DECLARADO ACA Y USADO POR UN SLICE POSTERIOR (§6): esta puesto
		/// porque §6 lo nombra, y no porque OS2 lo lea.
		/// </summary>
		internal const int BundlingWindowDays = 60;

		/// <summary>
		/// BundlingWindow, mitad de distancia, en la unidad del limite de la operacion.
		/// Declarado aca y usado por un slice posterior, igual que su mitad de tiempo.
		/// </summary>
		internal const int BundlingWindowDistance = 2000;
	}
}
