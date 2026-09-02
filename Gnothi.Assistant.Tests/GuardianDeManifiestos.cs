using Gnothi.Host;

namespace Gnothi.Conformance;

// EL GUARDIAN DE MANIFIESTOS. Lo que un humano hizo a mano con gnothi_verify_module, aca corre
// solo sobre cada modulo instalado: arranca el modulo, le pide sus tools REALES, y las compara
// con lo que su manifiesto DECLARA. Si difieren, FALLA.
//
// POR QUE EXISTE. El 2026-09-02 compras y examenes se desincronizaron en silencio. Los
// companeros le agregaron tools al codigo y el manifiesto que Gnothi LEE -- Modulos/<x>/
// manifest.json -- quedo viejo. Y la Puerta 2 arma el catalogo del manifiesto, NO del modulo:
//
//   - una tool que el manifiesto declara y el modulo ya no tiene (un FANTASMA) se le ofrece al
//     modelo y falla al llamarse;
//   - una tool que el modulo expone y el manifiesto no lista NO le llega al modelo nunca.
//
// Nadie se entero hasta revisar a mano. Este guardian convierte esa revision en algo que corre
// en cada corrida de la suite, para que un manifiesto desincronizado no pase un CI en verde.
//
// LA RAIZ, que este guardian NO resuelve pero si expone: no hay un solo lugar donde vive la
// verdad de las tools de un modulo. compras tiene un manifiesto fuente (Compras/manifiesto.json)
// que alguien traduce a mano al esquema que Gnothi lee; examenes no tiene fuente y su manifiesto
// se edita directo. Mientras el manifiesto sea una copia y no una proyeccion del modulo, se va a
// desincronizar. El guardian es la red que atrapa la caida; el arreglo de fondo es generar el
// manifiesto desde el modulo, y ese es trabajo aparte.
//
// Corre ANTES de la conformance: no tiene sentido comparar las tres puertas sobre un catalogo
// que ni siquiera coincide con los modulos que lo respaldan.
internal static class GuardianDeManifiestos
{
	public static async Task Verificar(string modulesRoot, Action<string, bool, string> assert)
	{
		var catalog = new ModuleCatalog(modulesRoot);
		catalog.Load();

		if (catalog.Modules.Count == 0)
		{
			assert("el guardian encontro al menos un modulo que verificar", false,
				"el catalogo cargo cero modulos: nada que comparar, y eso oculta una regresion");
			return;
		}

		foreach (Module m in catalog.Modules)
		{
			string[] declared = [.. m.Manifest.Tools.Select(t => t.Name).Order()];

			string[] actual;
			try
			{
				// Arranca el modulo por su cascara y le pide la lista real. NO lee el journal:
				// solo enumera tools, asi que corre aunque la carpeta de datos este vacia.
				actual = [.. (await m.ActualToolNames(CancellationToken.None)).Order()];
			}
			catch (Exception e)
			{
				assert($"{m.Name}: el modulo arranca y publica sus tools",
					false, $"no se pudo consultar: {e.GetType().Name}: {e.Message}");
				continue;
			}

			string[] fantasmas = [.. declared.Except(actual)];    // el manifiesto las declara, el modulo NO las tiene
			string[] sinDeclarar = [.. actual.Except(declared)];  // el modulo las expone, el manifiesto NO las lista

			// Un fantasma es peor que una tool sin declarar: la Puerta 2 la OFRECE y explota al
			// llamarse. Por eso se afirma por separado y con su propio mensaje.
			assert($"{m.Name}: el manifiesto no declara tools que el modulo ya no tiene",
				fantasmas.Length == 0,
				fantasmas.Length == 0 ? "" : $"FANTASMAS (declaradas y ausentes en el modulo): {string.Join(", ", fantasmas)}");

			assert($"{m.Name}: el manifiesto declara todo lo que el modulo expone",
				sinDeclarar.Length == 0,
				sinDeclarar.Length == 0 ? "" : $"SIN DECLARAR (el modulo las expone, el manifiesto no): {string.Join(", ", sinDeclarar)}");
		}

		await catalog.DisposeAsync();
	}
}
