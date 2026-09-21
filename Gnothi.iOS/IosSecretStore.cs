using Foundation;
using Gnothi.MobileShared;
using Security;

namespace Gnothi.iOS;

// iOS'S SECRET STORE: the **Keychain**, which is the twin of the Android Keystore and not of
// the encrypted file. The difference in shape matters, and it is why this class is so short:
// on Android the bytes have to be encrypted by hand and written out; here the system keeps
// the secret itself and hands it back only to this app.
//
// THREE DECISIONS, AND ALL THREE ARE SECURITY DECISIONS:
//
//   `WhenUnlockedThisDeviceOnly` -- this is what makes it the real equivalent of the
//   Keystore. Without `ThisDeviceOnly` the credential TRAVELS in the iCloud backup and shows
//   up on another device: that would break the very property this class exists to hold, which
//   is that without THIS phone it is worthless. And `WhenUnlocked` because the courier runs
//   while the person is using the app, not in the background with the phone locked.
//
//   `GenericPassword` with a fixed Service and Account = alias -- one item per module,
//   isolated from each other, the same way Android has one key per module.
//
//   NOTHING IS SWALLOWED. If the Keychain fails, let it throw: GnothiCredential catches it,
//   says it, and asks for a new grant. Eating the error here would make a hardware problem
//   look like "there never was a credential".
//
// [NOT MEASURED ON DEVICE] Written against Apple's documentation and compiled, not run on an
// iPhone. What to check the first time: that `Get` right after `Save` returns the same thing,
// and that after reinstalling the app it returns nothing -- the Keychain survives uninstall on
// some iOS versions, and if that happened we would have to clear it on install.
internal sealed class IosSecretStore : ISecretStore
{
	// The same service for every module; what separates them is the account.
	private const string Service = "com.ncubo.gnothi.credentials";

	public string? Get(string alias)
	{
		using SecRecord query = Query(alias);

		SecRecord? found = SecKeyChain.QueryAsRecord(query, out SecStatusCode status);

		// Not being there is not an error: it is a module that has not asked for its grant yet.
		if (status == SecStatusCode.ItemNotFound || found is null) return null;

		if (status != SecStatusCode.Success)
			throw new InvalidOperationException($"the Keychain could not read '{alias}': {status}");

		using (found)
		{
			return found.ValueData is { } data ? NSString.FromData(data, NSStringEncoding.UTF8)?.ToString() : null;
		}
	}

	public void Save(string alias, string json)
	{
		using NSData data = NSData.FromString(json, NSStringEncoding.UTF8);

		// ADD, Y SI YA ESTABA, BORRAR Y AGREGAR. NO SecKeyChain.Update, y no es preferencia.
		//
		// El diccionario de atributos-a-actualizar de `SecItemUpdate` NO admite `kSecClass`, y
		// `SecRecord` no se puede construir sin una `SecKind`: la clase viaja adentro siempre.
		// Asi que todo Update falla con `NoSuchAttribute` (errSecNoSuchAttr), y como el unico
		// codigo tolerado antes de caer al Add era `ItemNotFound`, EL PRIMER GUARDADO SIEMPRE
		// TIRABA. MEDIDO en el simulador el 2026-09-07: los cuatro modulos del paquete cayeron
		// con "the Keychain could not update 'gnothi.credential.<modulo>': NoSuchAttribute", el
		// cartero no pudo pedir nada, y los cuatro journals quedaron en 0 B.
		//
		// Tolerar el NoSuchAttribute no alcanzaba: en un RENEWAL -el caso comun- el item existe,
		// el Update falla igual, y el Add de despues devuelve `DuplicateItem` y tira. Por eso se
		// invierte el orden y el duplicado deja de ser un error: es una rama esperada.
		//
		// Y HAY UNA GANANCIA QUE NO ES CASUAL: por aca TODA escritura pasa por el registro
		// completo, asi que `WhenUnlockedThisDeviceOnly` se reafirma en cada renovacion. El
		// Update no lo tocaba, asi que la accesibilidad quedaba fijada por el primer guardado.
		SecStatusCode added = Add(alias, data);
		if (added == SecStatusCode.Success) return;

		if (added != SecStatusCode.DuplicateItem)
			throw new InvalidOperationException($"the Keychain could not save '{alias}': {added}");

		// Ya habia una credencial de este modulo: se reemplaza. La ventana en que no existe es
		// de una llamada, dentro de este proceso y de un solo escritor.
		using (SecRecord query = Query(alias))
		{
			SecStatusCode removed = SecKeyChain.Remove(query);
			if (removed is not (SecStatusCode.Success or SecStatusCode.ItemNotFound))
				throw new InvalidOperationException($"the Keychain could not replace '{alias}': {removed}");
		}

		SecStatusCode again = Add(alias, data);
		if (again != SecStatusCode.Success)
			throw new InvalidOperationException($"the Keychain could not save '{alias}' after replacing it: {again}");
	}

	public void Forget(string alias)
	{
		using SecRecord query = Query(alias);

		SecStatusCode status = SecKeyChain.Remove(query);

		// Removing a module that never got a credential is normal.
		if (status is SecStatusCode.Success or SecStatusCode.ItemNotFound) return;

		throw new InvalidOperationException($"the Keychain could not delete '{alias}': {status}");
	}

	private static SecRecord Query(string alias) => new(SecKind.GenericPassword)
	{
		Service = Service,
		Account = alias,
	};

	// El registro COMPLETO, que es el unico que se escribe. La accesibilidad va aca y no en el
	// query, porque es un atributo del item que se guarda, no del que se busca.
	private static SecStatusCode Add(string alias, NSData data)
	{
		using var fresh = new SecRecord(SecKind.GenericPassword)
		{
			Service = Service,
			Account = alias,
			ValueData = data,
			Accessible = SecAccessible.WhenUnlockedThisDeviceOnly,
		};

		return SecKeyChain.Add(fresh);
	}
}
