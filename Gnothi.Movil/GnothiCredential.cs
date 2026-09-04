using System.Text.Json;

namespace Gnothi.Movil;

// GNOTHI'S OWN CREDENTIAL BEFORE A PUBLISHER. One per module, and it belongs to Gnothi, not
// to the module (specification 13 §3).
//
// THREE THINGS THIS CLASS EXISTS TO HOLD UP:
//
//   1. IT DOES NOT GO IN THE JOURNAL. A token is session context, not the person's history.
//      When it lived there it stayed in the clear, travelled with any copy of the journal,
//      and every process that could read the folder could read it. That was already taken
//      out on the PWA side; here it is held.
//   2. THE MODULE NEVER SEES IT. It lives on the other side of the bridge, and with
//      `connect-src 'self'` the module could not use it even if it had it.
//   3. IT DOES NOT LEAVE THE DEVICE IN THE CLEAR. That is the secret store's job on each
//      platform; here we only demand that one exists.
//
// WHY THIS CLASS IS NEUTRAL AND THE SECRET STORE IS NOT. What a platform decides is WHERE a
// few secret bytes live, not what they mean. The device key, the token, the refresh, and the
// rule "if it cannot be read, ask for a new grant" are the same on both sides -- and if they
// stopped being the same, Gnothi would have two behaviours under one name.
//
// Android backs it with the Keystore, which encrypts using a non-exportable key that lives in
// the TEE. iOS with the Keychain, which is the equivalent and survives differently: that is
// why the interface talks about saving and fetching, and not about encrypting.
internal sealed class GnothiCredential
{
	private readonly ISecretStore store;
	private readonly string alias;
	private readonly Action<string> log;

	private Stored? inMemory;

	// ONE KEY PER MODULE: one laboratory's credential has no business being able to decrypt
	// another's. The alias is the name the store keeps it under, and the Host picks it -- not
	// the platform -- precisely so that this isolation does not depend on who implements it.
	internal GnothiCredential(string module, ISecretStore store, Action<string> log)
	{
		this.store = store;
		this.log = log;
		alias = $"gnothi.credential.{module}";
	}

	internal string DeviceKey => Read().DeviceKey;

	internal string Token => Read().Token;

	internal string Refresh => Read().Refresh;

	// The identity of THIS installation of Gnothi before THIS publisher. Generated once and
	// kept; if it were lost, the publisher would see a different device.
	internal string EnsureDeviceKey()
	{
		Stored current = Read();
		if (current.DeviceKey.Length > 0) return current.DeviceKey;

		current.DeviceKey = "device-gnothi-" + Guid.NewGuid().ToString("N")[..16];
		Write(current);
		return current.DeviceKey;
	}

	internal void Save(string token, string refresh)
	{
		Stored current = Read();
		current.Token = token ?? "";
		current.Refresh = refresh ?? "";
		Write(current);
	}

	// When the module is removed. Against a publisher with revocation (RFC 7009) this goes
	// together with telling it, so the grant dies on the other side too.
	internal void Forget()
	{
		inMemory = null;
		store.Forget(alias);
	}

	private Stored Read()
	{
		if (inMemory is not null) return inMemory;

		try
		{
			string? json = store.Get(alias);
			if (string.IsNullOrEmpty(json)) return inMemory = new Stored();

			return inMemory = JsonSerializer.Deserialize<Stored>(json) ?? new Stored();
		}
		catch (Exception ex)
		{
			// A credential that cannot be read is a lost credential: it happens when the key
			// is gone -- restoring a backup on another device, clearing the app's data. It
			// does NOT break: Gnothi asks for a new grant, which is something it can always do.
			//
			// But IT IS SAID. A silent degradation is worse than an error: without this line,
			// Gnothi presenting itself as a brand new device to the publisher would look
			// exactly like never having had a credential at all.
			log($"credential unreadable ({ex.GetType().Name}): a new one will be requested");
			return inMemory = new Stored();
		}
	}

	private void Write(Stored value)
	{
		inMemory = value;
		store.Save(alias, JsonSerializer.Serialize(value));
	}

	private sealed class Stored
	{
		public string DeviceKey { get; set; } = "";

		public string Token { get; set; } = "";

		public string Refresh { get; set; } = "";
	}
}

// WHERE A FEW SECRET BYTES LIVE ON THIS DEVICE. Each platform implements it; the Host only
// names it, the same way it does with IWebModuleHost.
//
// The contract is deliberately poor -- three verbs, text inside -- because everything added
// here has to be held up twice. What is NOT here, on purpose: no "rotate" and no "expires
// at", because the publisher already takes care of that and duplicating it would give two
// truths.
//
// IT IS EXPECTED TO FAIL LOUDLY. If the store cannot read, let it throw: GnothiCredential
// catches it, says it, and asks for a new grant. Returning "" silently on a hardware error
// would make that look like "there never was a credential".
internal interface ISecretStore
{
	// The json kept under that alias, or null when there is nothing.
	string? Get(string alias);

	void Save(string alias, string json);

	// Does not fail when it was not there: removing a module that never got a credential is
	// normal.
	void Forget(string alias);
}
