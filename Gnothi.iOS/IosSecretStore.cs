using Foundation;
using Gnothi.Movil;
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

		// UPDATE FIRST. Adding on top of an item that is already there returns `DuplicateItem`,
		// so the natural order -- try to add and fall back to update -- would leave an error
		// behind every time the token is renewed, which is the MOST common operation.
		using (SecRecord query = Query(alias))
		using (var change = new SecRecord(SecKind.GenericPassword) { ValueData = data })
		{
			SecStatusCode updated = SecKeyChain.Update(query, change);
			if (updated == SecStatusCode.Success) return;

			if (updated != SecStatusCode.ItemNotFound)
				throw new InvalidOperationException($"the Keychain could not update '{alias}': {updated}");
		}

		using var fresh = new SecRecord(SecKind.GenericPassword)
		{
			Service = Service,
			Account = alias,
			ValueData = data,
			Accessible = SecAccessible.WhenUnlockedThisDeviceOnly,
		};

		SecStatusCode added = SecKeyChain.Add(fresh);
		if (added != SecStatusCode.Success)
			throw new InvalidOperationException($"the Keychain could not save '{alias}': {added}");
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
}
