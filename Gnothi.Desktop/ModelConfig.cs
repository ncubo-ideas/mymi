using System.Net.Http.Headers;
using Gnothi.Host;
using System.Text.Json;

namespace Gnothi.Desktop;

// EL PROVEEDOR DEL MODELO Y LA LLAVE, EN EL ESCRITORIO. El espejo de lo que MainActivity guarda en
// el telefono, pero un paso mas alla: aqui la persona ELIGE proveedor -Claude, ChatGPT (OpenAI),
// Gemini, o cualquiera de la forma OpenAI- y, con la llave puesta, la app le PREGUNTA al proveedor
// que modelos tiene y ella escoge cual usar. Ese modelo es el que carga cada peticion.
//
// BYOK -- la llave es de la persona: vive SOLO en un area privada por-usuario, NUNCA se le devuelve
// a la UI (solo un booleano "hay una", y la LISTA de modelos que el proveedor responde -no la
// llave-), y no llega a ningun log.
//
// DOS FAMILIAS DE API, un solo asiento de configuracion:
//   - "anthropic": Claude. Header x-api-key, cuerpo propio -> lo habla ModelClaude.
//   - "openai":    ChatGPT, Gemini, Groq, Cerebras, OpenRouter, LM Studio local... todos hablan la
//                  forma OpenAI (Authorization: Bearer) -> lo habla ModelOpenAiCompatible.
// El "kind" del proveedor es lo unico que decide que adaptador se arma y como se listan sus modelos.
internal sealed class ModelConfig
{
    // Area privada por-usuario, la misma idea que el FilesDir del telefono.
    private static string Dir
    {
        get
        {
            string d = Path.Combine(
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

    // Kind: "anthropic" (Claude) u "openai" (todo lo que habla la forma OpenAI).
    public sealed record Preset(string Name, string Kind, string Url, string Model);

    // Presets para no tener que buscar. El ultimo es LOCAL -- sin llave, nada sale de la maquina.
    // Cada uno trae un modelo por defecto; con la llave puesta, "Cargar modelos" lo reemplaza por lo
    // que el proveedor realmente ofrezca.
    public static readonly IReadOnlyList<Preset> Presets =
    [
        new("Claude", "anthropic", "https://api.anthropic.com/v1", "claude-sonnet-5"),
        new("ChatGPT (OpenAI)", "openai", "https://api.openai.com/v1", "gpt-4o-mini"),
        new("Gemini (Google)", "openai", "https://generativelanguage.googleapis.com/v1beta/openai", "gemini-2.0-flash"),
        new("Groq", "openai", "https://api.groq.com/openai/v1", "openai/gpt-oss-120b"),
        new("Cerebras", "openai", "https://api.cerebras.ai/v1", "gpt-oss-120b"),
        new("OpenRouter", "openai", "https://openrouter.ai/api/v1", "openai/gpt-oss-20b:free"),
        new("LM Studio (local)", "openai", "http://localhost:1234/v1", "local-model"),
    ];

    // El proveedor elegido; si no hay archivo o no se puede leer, el default.
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

    // Elegir proveedor deja el modelo por defecto del preset; la persona lo afina con "Cargar
    // modelos". Escribe kind para que el asistente sepa que adaptador armar.
    public void SetProvider(string name, string kind, string url, string model) =>
        Write(name, kind, url, model);

    // Solo cambia el modelo, conservando el proveedor. Es lo que hace el desplegable de modelos.
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

    // Un modelo local (localhost) no necesita llave.
    public bool NeedsKey
    {
        get
        {
            string u = Provider().Url;
            return !u.Contains("localhost") && !u.Contains("127.0.0.1");
        }
    }

    // PER PROVIDER, a proposito: una llave de Groq no es una de OpenAI. Cambiar de proveedor cambia
    // que archivo de llave se lee, asi ir a otro y volver no pierde nada.
    public string KeyPath
    {
        get
        {
            string safe = new string(Provider().Name.Where(char.IsLetterOrDigit).ToArray());
            return Path.Combine(Dir, $"key-{safe}.key");
        }
    }

    public bool KeyPresent => File.Exists(KeyPath);

    // Tres reglas: vive SOLO aca (area privada), NUNCA vuelve a la UI, y no va a ningun log.
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

    // LE PREGUNTA AL PROVEEDOR QUE MODELOS TIENE. Usa la llave de la persona (leida del archivo
    // privado) SOLO para este GET; devuelve la lista de ids, nunca la llave. Ambas familias
    // responden { "data": [ { "id": "..." } ] }:
    //   - openai:    GET {base}/models        con Authorization: Bearer <llave>
    //   - anthropic: GET {base}/models        con x-api-key + anthropic-version
    public async Task<ModelsResult> FetchModels(CancellationToken ct = default)
    {
        (string _, string kind, string url, string _) = Provider();

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
