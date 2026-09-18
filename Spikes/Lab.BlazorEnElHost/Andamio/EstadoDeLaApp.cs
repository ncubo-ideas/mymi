using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Choreography.Theater;
using Compras.App;
using Compras.App.Vistas;
using Puppeteer;
using Puppets.Portador;

namespace Compras.Wasm;

/// EL ANDAMIO, Y ESTE ARCHIVO **ES** EL RESULTADO DE LA SEGUNDA VUELTA DEL SPIKE.
///
/// `MisCompras.razor` se compila sin tocar una linea -- eso ya se sabia --, pero esta tipado contra
/// `EstadoDeLaApp`, que es una clase SELLADA del proyecto Wasm. Para montarlo nativo hay que
/// reponer esa clase, y lo que cuesta reponerla es la medida de cuanto andamio pide esta forma.
///
/// LOS 38 MIEMBROS QUE LA PANTALLA USA SE PARTEN EN DOS MITADES CASI EXACTAS:
///
///   19 son LECTURAS DEL DOMINIO -- Compras(), Firma(), Precios(), CanastaDelUltimoMes()... --
///      y aca son una linea cada una: reenvian a `ComprasAPI`, que es el mismo objeto que usa la
///      superficie hibrida. Nada que inventar.
///
///   19 son EL PROBLEMA DEL NAVEGADOR -- EstaSoportado, OpfsSoportado, RecuperarCarpeta,
///      PedirPermiso, MotivoSinAlmacenamiento, ReengancharModo, FaltaReotorgarPermiso,
///      EnAlmacenamientoDelNavegador, UsarAlmacenamientoDelNavegador, ConectarCarpeta... --
///      y aca son CONSTANTES, porque la carpeta la concede Gnothi. La misma razon por la que la
///      forma hibrida borro `ElArranque.razor` entero.
///
/// O SEA: la mitad de la API contra la que esta escrita la pantalla existe para resolver un
/// problema que esta forma no tiene, y la pantalla la nombra igual -- `@if (!estado.EstaArriba)`,
/// `estado.FaltaReotorgarPermiso`, `estado.OpfsSoportado` -- porque esta acoplada por TIPO.
///
/// NO SE PUEDE SUBCLASEAR: `EstadoDeLaApp` es `sealed`. Asi que el andamio no es «implementar una
/// interfaz», es REPONER LA CLASE en su propio namespace. Esto es lo que una cuarta forma tendria
/// que resolver de raiz -- probablemente pidiendole a la institucion que su pantalla dependa de una
/// interfaz y no de una clase --, y es una peticion que hoy no le hacemos a nadie.
internal sealed class EstadoDeLaApp
{
	private readonly ComprasApp actor;
	private readonly ComprasAPI api;

	public EstadoDeLaApp(string carpetaConcedida)
	{
		var arrancando = new ComprasApp("compras");
		arrancando.ConfigureStorage(DatabaseType.FileSystem, "path=" + carpetaConcedida);
		arrancando.Start();

		actor = arrancando;
		api = new ComprasAPI(actor);
	}

	// ---------------------------------------------------------------- las 19 del navegador
	//
	// Todas constantes. En la app suelta cada una es una vuelta por `IJSRuntime` al modulo de
	// almacenamiento; aca la respuesta la sabe el Host antes de preguntar.

	public bool EstaArriba => true;
	public bool HayCarpeta => true;
	public bool Hospedada => true;
	public bool Migrada => false;
	public bool FaltaReotorgarPermiso => false;
	public bool EnAlmacenamientoDelNavegador => false;
	public string NombreDeLaCarpeta => "la que te dio Gnothi";
	public string GuardadoFallando => "";
	public string DondeVive => "Gnothi";
	public string UltimoMensaje => "";
	public string Modo => "gnothi";

	public ValueTask<bool> EstaSoportado() => new(true);
	public ValueTask<bool> OpfsSoportado() => new(false);
	public ValueTask<bool> EstaInstalada() => new(true);
	public ValueTask<string> MotivoSinAlmacenamiento() => new("");
	public ValueTask<bool> ReengancharModo() => new(true);
	public ValueTask Arrancar() => ValueTask.CompletedTask;

	// LAS CUATRO QUE LA PANTALLA OFRECE Y ESTA FORMA NO NECESITA. Contestan «listo» para que los
	// botones de elegir carpeta y volver a dar permiso no se dibujen.
	public ValueTask<ResultadoDeCarpeta> RecuperarCarpeta() => CarpetaLista();
	public ValueTask<ResultadoDeCarpeta> ConectarCarpeta() => CarpetaLista();
	public ValueTask<ResultadoDeCarpeta> PedirPermiso() => CarpetaLista();
	public ValueTask<ResultadoDeCarpeta> UsarAlmacenamientoDelNavegador() => CarpetaLista();

	private static ValueTask<ResultadoDeCarpeta> CarpetaLista() =>
		new(new ResultadoDeCarpeta { Ok = true, Nombre = "la que te dio Gnothi", Permiso = "granted" });

	// ---------------------------------------------------------------- las 19 del dominio
	//
	// Una linea cada una, y todas contra el MISMO `ComprasAPI` que usa `ComprasSurface`. Esta
	// mitad no es andamio: es el reenvio que cualquier forma tiene que hacer.

	// INSTRUMENTADO: el spike necesita saber CUAL de las 18 lecturas devuelve null, porque la pila
	// del NullReferenceException llega sin numero de linea -- el razor viene de otro proyecto.
	private static T Ver<T>(string que, T v)
	{
		Android.Util.Log.Info("SPIKE", "andamio: " + que + " -> " + (v is null ? "NULL" : v.GetType().Name));
		return v;
	}

	public IReadOnlyList<CompraEnLista> Compras() => api.Compras();
	public int Cantidad() => api.CantidadDeCompras();
	public decimal Total() => api.TotalDeCompras();
	public long UltimoEvento() => api.UltimoEventoProcesado();
	public RangoDelHistorialVista Rango() => Ver("Rango", api.Rango());
	public FirmaDeLaSemana Firma() => Ver("Firma", api.Firma());
	public IReadOnlyList<PrecioQueCambio> Precios() => Ver("Precios", api.Precios());
	public RitmoDelSuperVista Ritmo() => Ver("Ritmo", api.Ritmo());
	public IReadOnlyList<RitmoQueCambio> RitmosQueCambiaron() => Ver("RitmosQueCambiaron", api.RitmosDelQuiebre());
	public RecorridoDeSucursalesVista Recorrido() => Ver("Recorrido", api.Recorrido());
	public IReadOnlyList<PromocionVista> Promociones() => Ver("Promociones", api.Promociones(DateTime.Now));
	public ListaDeLaProximaCompra Lista() => Ver("Lista", api.Lista(DateTime.Now));
	public IReadOnlyList<GarantiaDeProducto> GarantiasVigentes() => Ver("GarantiasVigentes", api.GarantiasVigentes(DateTime.Now));
	public IReadOnlyList<AbastecimientoEnEspera> AbastecimientosPendientes() => Ver("AbastecimientosPendientes", api.AbastecimientosPendientes(DateTime.Now));
	public CanastasComparadas ComparacionPorDefecto() => Ver("ComparacionPorDefecto", api.ComparacionConElAnioAnterior());
	public CanastasComparadas ComparacionConElMesAnterior() => Ver("ComparacionConElMesAnterior", api.ComparacionConElMesAnterior());
	public CambioDeLaCanasta CambioDelUltimoAnio() => Ver("CambioDelUltimoAnio", api.CambioDelQuiebre());

	public CanastaDelMesVista CanastaDelUltimoMes()
	{
		RangoDelHistorialVista r = api.Rango();
		if (r.MesesConCompras == 0) return api.Canasta(DateTime.Now.Year, DateTime.Now.Month);

		string[] partes = r.UltimoMes.Split('-');
		return Ver("CanastaDelUltimoMes", api.Canasta(int.Parse(partes[0]), int.Parse(partes[1])));
	}

	/// REFRESCAR NO SALE A LA RED, igual que en la forma hibrida: el cartero es Gnothi.
	public ValueTask<ResultadoDeRefresh> Refrescar(CancellationToken ct = default) =>
		new(new ResultadoDeRefresh());
}

/// LA OTRA CLASE QUE LA PANTALLA INYECTA, y es de las baratas: cuatro datos de contacto que la
/// institucion declara. En la app suelta se llena desde su configuracion; en esta forma la tendria
/// que llenar el Host desde el manifiesto.
internal sealed class ConfiguracionDeLaInstitucion
{
	public string Sitio { get; set; } = "";
	public string WhatsApp { get; set; } = "";
	public string TelefonoParaMostrar { get; set; } = "";

	public bool HaySitio => Sitio.Length > 0;
	public bool HayWhatsApp => WhatsApp.Length > 0;
	public bool HayAlguno => HaySitio || HayWhatsApp;
	public string EnlaceDeWhatsApp => HayWhatsApp ? $"https://wa.me/{WhatsApp}" : "";
}

/// LO QUE DEVUELVEN LAS CUATRO DEL ALMACENAMIENTO. Es la forma que la pantalla espera; el
/// contenido, en esta forma, es siempre el mismo.
internal sealed class ResultadoDeCarpeta
{
	public bool Ok { get; set; }
	public string Nombre { get; set; } = "";
	public string Permiso { get; set; } = "";
	public string Motivo { get; set; } = "";
}
