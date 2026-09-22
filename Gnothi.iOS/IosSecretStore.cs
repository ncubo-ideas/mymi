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

		// ADD, AND IF IT WAS ALREADY THERE, DELETE AND ADD. NOT SecKeyChain.Update, and that is not a
		// preference.
		//
		// `SecItemUpdate`'s attributes-to-update dictionary does NOT admit `kSecClass`, and `SecRecord`
		// cannot be built without a `SecKind`: the class always travels inside. So every Update fails
		// with `NoSuchAttribute` (errSecNoSuchAttr), and since the only code tolerated before falling
		// through to the Add was `ItemNotFound`, THE FIRST SAVE ALWAYS THREW. MEASURED on the simulator
		// on 2026-09-07: all four modules in the package fell over with "the Keychain could not update
		// .gnothi.credential.<module>.: NoSuchAttribute", the postman could ask for nothing, and all four
		// journals stayed at 0 B.
		//
		// Tolerating the NoSuchAttribute was not enough: on a RENEWAL -- the common case -- the item
		// exists, the Update fails all the same, and the Add afterwards returns `DuplicateItem` and
		// throws. So the order is inverted and the duplicate stops being an error: it is an expected
		// branch.
		//
		// AND THERE IS A GAIN THAT IS NOT ACCIDENTAL: through here EVERY write goes through the complete
		// record, so `WhenUnlockedThisDeviceOnly` is reasserted on every renewal. The Update did not
		// touch it, so the accessibility stayed fixed by the first save.
		SecStatusCode added = Add(alias, data);
		if (added == SecStatusCode.Success) return;

		if (added != SecStatusCode.DuplicateItem)
			throw new InvalidOperationException($"the Keychain could not save '{alias}': {added}");

		// There was already a credential for this module: it gets replaced. The window in which it does
		// not exist is one call long, inside this process and with a single writer.
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

	// The COMPLETE record, which is the only one ever written. The accessibility goes here and not in
	// the query, because it is an attribute of the item being saved, not of the one being looked for.
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
