using System.Net.Http.Headers;
using Gnothi.Host;
using System.Text.Json;

namespace Gnothi.Desktop;

// THE MODEL PROVIDER AND THE KEY, ON THE DESKTOP. The mirror of what MainActivity keeps on the
// phone, but one step further: here the person CHOOSES a provider -Claude, ChatGPT (OpenAI),
// Gemini, or anything in the OpenAI shape- and, with the key in place, the app ASKS the provider
// which models it has and she picks which one to use. That model is what every request carries.
//
// BYOK -- the key belongs to the person: it lives ONLY in a per-user private area, is NEVER handed
// back to the UI (only a boolean "there is one", and the LIST of models the provider answers with
// -not the key-), and reaches no log.
//
// TWO API FAMILIES, one single configuration seat:
//   - "anthropic": Claude. x-api-key header, its own body -> ModelClaude speaks it.
//   - "openai":    ChatGPT, Gemini, Groq, Cerebras, OpenRouter, local LM Studio... they all speak
//                  the OpenAI shape (Authorization: Bearer) -> ModelOpenAiCompatible speaks it.
// The provider "kind" alone decides which adapter is built and how its models are listed.
internal sealed class ModelConfig
{
    // A per-user private area, the same idea as the phone's FilesDir.
    private static string Dir
    {
        get
        {
            string d = Path.Combine(
                // "Gnothi" AND NOT "Mymi", even though the product is already called Mymi to the person.
                // This is a PATH ON DISK, not a text: renaming it leaves the model.cfg and the keys
                // of everyone who already has the app in a folder nobody reads again, and the symptom
                // would be «my provider was deleted» with no error at all. It is renamed the day
                // somebody writes the migration, not before.
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Gnothi");
            Directory.CreateDirectory(d);
            return d;
        }
    }

    private static string CfgPath => Path.Combine(Dir, "model.cfg");

    public const string DefaultName = "Claude";
    public const string DefaultKind = "anthropic";
    public const string DefaultUrl = "https://api.anthropic.com/v1";
    public const string DefaultModel = "claude-sonnet-5";

    // Kind: "anthropic" (Claude) or "openai" (everything that speaks the OpenAI shape).
    public sealed record Preset(string Name, string Kind, string Url, string Model);

    // Presets so nothing has to be looked up. The last is LOCAL -- no key, nothing leaves the machine.
    // Each brings a default model; with the key in place, "Load models" replaces it with whatever
    // the provider actually offers.
    public static readonly IReadOnlyList<Preset> Presets =
    [
        // THIS MACHINE'S OWN, first: it is the door that matters (on-device), and the network ones
        // are the fallback. No URL and no key: nothing leaves the machine. It is only offered if
        // the model really generates here (AppleModel.Works), see PresetsAvailable.
        new(AppleName, "apple", "", "on-device"),
        new("Claude", "anthropic", "https://api.anthropic.com/v1", "claude-sonnet-5"),
        new("ChatGPT (OpenAI)", "openai", "https://api.openai.com/v1", "gpt-4o-mini"),
        new("Gemini (Google)", "openai", "https://generativelanguage.googleapis.com/v1beta/openai", "gemini-2.0-flash"),
        new("Groq", "openai", "https://api.groq.com/openai/v1", "openai/gpt-oss-120b"),
        new("Cerebras", "openai", "https://api.cerebras.ai/v1", "gpt-oss-120b"),
        new("OpenRouter", "openai", "https://openrouter.ai/api/v1", "openai/gpt-oss-20b:free"),
        new("LM Studio (local)", "openai", "http://localhost:1234/v1", "local-model"),
    ];

    /// <summary>The provider that runs on this Mac.</summary>
    ///
    /// IT IS AN IDENTITY, NOT A LABEL: it is what is stored in model.cfg, what the dropdown
    /// compares, and what KeyPath comes from. The window SHOWS it through Display.
    public const string AppleName = "Apple Intelligence (en esta Mac)";

    /// <summary>What a provider is called ON THE SCREEN; what is stored and compared is still the
    /// preset name.</summary>
    public static string Display(string name) =>
        name == AppleName ? Textos.T("ajustes.proveedor.apple") : name;

    // The presets worth showing ON THIS MACHINE: Apple's only where the model generates. On
    // Windows, Linux or a Mac without the bridge, the list is the usual one.
    public IReadOnlyList<Preset> PresetsAvailable() =>
        AppleModel.Works() ? Presets : [.. Presets.Where(p => p.Kind != "apple")];

    public bool IsApple => Provider().Kind == "apple";

    // The chosen provider; if there is no file or it cannot be read, the default.
    public (string Name, string Kind, string Url, string Model) Provider()
    {
        try
        {
            if (File.Exists(CfgPath))
            {
                JsonElement e = JsonSerializer.Deserialize<JsonElement>(File.ReadAllText(CfgPath));
                return (e.GetProperty("name").GetString() ?? DefaultName,
                        e.TryGetProperty("kind", out JsonElement k) ? k.GetString() ?? DefaultKind : DefaultKind,
                        e.GetProperty("url").GetString() ?? DefaultUrl,
                        e.GetProperty("model").GetString() ?? DefaultModel);
            }
        }
        catch { /* eleccion ilegible -> default */ }
        return (DefaultName, DefaultKind, DefaultUrl, DefaultModel);
    }

    public bool IsAnthropic => Provider().Kind == "anthropic";

    // Choosing a provider leaves the preset's default model; the person refines it with "Load
    // models". It writes kind so the assistant knows which adapter to build.
    public void SetProvider(string name, string kind, string url, string model) =>
        Write(name, kind, url, model);

    // It changes only the model, keeping the provider. It is what the models dropdown does.
    public void SetModel(string model)
    {
        (string name, string kind, string url, _) = Provider();
        Write(name, kind, url, model);
    }

    private static void Write(string name, string kind, string url, string model) =>
        File.WriteAllText(CfgPath, JsonSerializer.Serialize(new Dictionary<string, string>
        {
            ["name"] = name, ["kind"] = kind, ["url"] = url, ["model"] = model,
        }));

    // A local model -Apple's, or one on localhost- needs no key.
    public bool NeedsKey
    {
        get
        {
            (_, string kind, string u, _) = Provider();
            if (kind == "apple") return false;
            return !u.Contains("localhost") && !u.Contains("127.0.0.1");
        }
    }

    // PER PROVIDER, on purpose: a Groq key is not an OpenAI one. Switching provider changes which
    // key file is read, so going to another and back loses nothing.
    public string KeyPath
    {
        get
        {
            string safe = new string(Provider().Name.Where(char.IsLetterOrDigit).ToArray());
            return Path.Combine(Dir, $"key-{safe}.key");
        }
    }

    public bool KeyPresent => File.Exists(KeyPath);

    // Three rules: it lives ONLY here (private area), NEVER goes back to the UI, and reaches no log.
    public void SetKey(string key)
    {
        key = key.Trim();
        if (key.Length > 0) File.WriteAllText(KeyPath, key);
    }

    public void ClearKey()
    {
        if (File.Exists(KeyPath)) File.Delete(KeyPath);
    }

    public sealed record ModelsResult(bool Ok, IReadOnlyList<string> Models, string? Error);

    // IT ASKS THE PROVIDER WHICH MODELS IT HAS. It uses the person's key (read from the private
    // file) ONLY for this GET; it returns the list of ids, never the key. Both families answer
    // { "data": [ { "id": "..." } ] }:
    //   - openai:    GET {base}/models        with Authorization: Bearer <key>
    //   - anthropic: GET {base}/models        with x-api-key + anthropic-version
    public async Task<ModelsResult> FetchModels(CancellationToken ct = default)
    {
        (string _, string kind, string url, string _) = Provider();

        // Apple lists no models: there is one, the system's, and it is not chosen.
        if (kind == "apple")
            return AppleModel.Works()
                ? new ModelsResult(true, ["on-device"], null)
                : new ModelsResult(false, [], AppleModel.Diagnosis());

        string? key = KeyPresent ? File.ReadAllText(KeyPath).Trim() : null;
        if (NeedsKey && string.IsNullOrWhiteSpace(key))
            return new ModelsResult(false, [], Textos.T("ajustes.error.sin-llave"));

        string endpoint = url.TrimEnd('/') + "/models";

        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            using var req = new HttpRequestMessage(HttpMethod.Get, endpoint);

            if (kind == "anthropic")
            {
                if (key is not null) req.Headers.Add("x-api-key", key);
                req.Headers.Add("anthropic-version", "2023-06-01");
            }
            else if (key is not null)
            {
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);
            }

            using HttpResponseMessage res = await http.SendAsync(req, ct);
            string text = await res.Content.ReadAsStringAsync(ct);

            if (!res.IsSuccessStatusCode)
                return new ModelsResult(false, [], Textos.T("ajustes.error.respondio", ("codigo", ((int)res.StatusCode).ToString())));

            JsonElement root = JsonSerializer.Deserialize<JsonElement>(text);
            if (!root.TryGetProperty("data", out JsonElement data) || data.ValueKind != JsonValueKind.Array)
                return new ModelsResult(false, [], Textos.T("ajustes.error.ilegible"));

            List<string> ids = [];
            foreach (JsonElement m in data.EnumerateArray())
                if (m.TryGetProperty("id", out JsonElement id) && id.GetString() is string s && s.Length > 0)
                    ids.Add(s.StartsWith("models/") ? s["models/".Length..] : s);  // Gemini prefija "models/"

            ids.Sort(StringComparer.OrdinalIgnoreCase);
            return ids.Count > 0
                ? new ModelsResult(true, ids, null)
                : new ModelsResult(false, [], Textos.T("ajustes.error.vacia"));
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception e)
        {
            return new ModelsResult(false, [], $"{e.GetType().Name}: {e.Message}");
        }
    }
}
