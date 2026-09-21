using System.Text.Json;
using Gnothi.MobileShared;

namespace Gnothi.Desktop;

// THE COURIER'S CREDENTIAL, kept outside the journal and outside the WebView (13 §3). On the
// phones this is the Keystore / Keychain. On the desktop, for this proof, a json file under the
// person's local application data; a real desktop would use DPAPI / Keychain / libsecret.
internal sealed class FileSecretStore(string path) : ISecretStore
{
	private readonly Lock gate = new();

	public string? Get(string alias)
	{
		lock (gate) return Read().GetValueOrDefault(alias);
	}

	public void Save(string alias, string json)
	{
		lock (gate) { Dictionary<string, string> all = Read(); all[alias] = json; Write(all); }
	}

	public void Forget(string alias)
	{
		lock (gate) { Dictionary<string, string> all = Read(); all.Remove(alias); Write(all); }
	}

	private Dictionary<string, string> Read()
	{
		if (!File.Exists(path)) return [];
		try { return JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(path)) ?? []; }
		catch (JsonException) { return []; }
	}

	private void Write(Dictionary<string, string> all)
	{
		Directory.CreateDirectory(Path.GetDirectoryName(path)!);
		File.WriteAllText(path, JsonSerializer.Serialize(all));
	}
}
