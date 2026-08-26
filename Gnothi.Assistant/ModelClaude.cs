using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Gnothi.Assistant;

// THE MODEL, THROUGH THE CLAUDE API.
//
// This is the part that does not exist on desktop: there the tool-calling loop is run by
// Claude Desktop with the person's own subscription, and Gnothi only answers tool calls.
// Here the loop is OURS, so the call to the model is ours too -- and with it, the key.
//
// AND A DISTINCTION THAT CONFUSES EVERYONE: a Claude subscription is not an API key. They are
// two products. The subscription is consumed by Anthropic's own apps; a third-party app uses
// the API, which is billed separately.
//
// THE KEY IS NOT IN THE CODE AND NOT IN THE REPO. It is read, in this order:
//   1. the ANTHROPIC_API_KEY environment variable
//   2. a text file at whatever path is passed in (on Android, the app's own area)
//
// If there is neither, the constructor throws. That is deliberate: Gate 2 WITHOUT a key is
// not half a Gate 2, it is nothing. The diagram defines it as "agent loop + model key".
public sealed class ModelClaude : IModel
{
	private const string Url = "https://api.anthropic.com/v1/messages";
	private const string Version = "2023-06-01";

	private readonly HttpClient http;
	private readonly string key;
	private readonly string model;

	public ModelClaude(string? keyFile = null, string model = "claude-sonnet-5", HttpClient? http = null)
	{
		this.model = model;
		this.http = http ?? new HttpClient { Timeout = TimeSpan.FromSeconds(120) };

		string? k = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");

		if (string.IsNullOrWhiteSpace(k) && keyFile is not null && File.Exists(keyFile))
			k = File.ReadAllText(keyFile).Trim();

		if (string.IsNullOrWhiteSpace(k))
			throw new InvalidOperationException(
				"no model key. Set ANTHROPIC_API_KEY in the environment, or provide the file " +
				(keyFile ?? "(none given)") + ". Gate 2 is the loop PLUS the key.");

		key = k;
	}

	// THE REQUEST BODY, in one single place. Public and static so it can be MEASURED without
	// sending it: the cost per question is the catalog plus the instructions, and that can be
	// counted before having a key.
	public static Dictionary<string, object?> Body(string model, string question, IReadOnlyList<ToolForTheModel> tools) =>
		new()
		{
			["model"] = model,
			["max_tokens"] = 1024,
			["system"] = Instructions,
			["tools"] = tools.Select(t => new Dictionary<string, object?>
			{
				["name"] = t.Name,
				["description"] = t.Description,
				["input_schema"] = JsonSerializer.Deserialize<JsonElement>(t.SchemaJson),
			}).ToArray(),
			["messages"] = new[] { new Dictionary<string, object?> { ["role"] = "user", ["content"] = question } },
		};

	public async Task<Decision> Ask(string question, IReadOnlyList<ToolForTheModel> tools, CancellationToken ct)
	{
		JsonElement answer = await Post(Body(model, question, tools), ct);

		// The response comes in blocks. Look for tool_use; if there is none, take the text.
		string? text = null;
		foreach (JsonElement b in answer.GetProperty("content").EnumerateArray())
		{
			string kind = b.GetProperty("type").GetString() ?? "";

			if (kind == "tool_use")
			{
				string publicName = b.GetProperty("name").GetString() ?? "";
				(string module, string tool) = Split(publicName, tools);

				Dictionary<string, object?> args = [];
				if (b.TryGetProperty("input", out JsonElement input))
					foreach (JsonProperty p in input.EnumerateObject())
						args[p.Name] = p.Value.ValueKind == JsonValueKind.String ? p.Value.GetString() : p.Value.GetRawText();

				return Decision.CallTool(module, tool, args);
			}

			if (kind == "text") text ??= b.GetProperty("text").GetString();
		}

		return Decision.Answer(text ?? "(no answer)");
	}

	public async Task<string> Compose(string question, ToolCall call, string resultJson, CancellationToken ct)
	{
		var body = new Dictionary<string, object?>
		{
			["model"] = model,
			["max_tokens"] = 1024,
			["system"] = Instructions,
			["messages"] = new[]
			{
				new Dictionary<string, object?> { ["role"] = "user", ["content"] = question },
				new Dictionary<string, object?>
				{
					["role"] = "assistant",
					["content"] = $"I queried {call.Module}_{call.Tool} and the module returned these facts: {resultJson}",
				},
				new Dictionary<string, object?>
				{
					["role"] = "user",
					["content"] = "Answer me with those facts, with their numbers and units. Do not add advice.",
				},
			},
		};

		JsonElement r = await Post(body, ct);
		foreach (JsonElement b in r.GetProperty("content").EnumerateArray())
			if ((b.GetProperty("type").GetString() ?? "") == "text")
				return b.GetProperty("text").GetString() ?? "";

		return resultJson;
	}

	// The public name is prefixed by module, and it CANNOT be split on the first underscore:
	// 'total_de_facturas' has three. It is resolved against the catalog, which is what knows
	// which prefix is a module.
	private static (string module, string tool) Split(string publicName, IReadOnlyList<ToolForTheModel> tools)
	{
		foreach (ToolForTheModel t in tools)
			if (t.Name == publicName)
			{
				int i = publicName.IndexOf('_');
				return i < 0 ? (publicName, publicName) : (publicName[..i], publicName[(i + 1)..]);
			}

		int j = publicName.IndexOf('_');
		return j < 0 ? (publicName, publicName) : (publicName[..j], publicName[(j + 1)..]);
	}

	private async Task<JsonElement> Post(Dictionary<string, object?> body, CancellationToken ct)
	{
		using var request = new HttpRequestMessage(HttpMethod.Post, Url);
		request.Headers.Add("x-api-key", key);
		request.Headers.Add("anthropic-version", Version);
		request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8);
		request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

		using HttpResponseMessage res = await http.SendAsync(request, ct);
		string text = await res.Content.ReadAsStringAsync(ct);

		if (!res.IsSuccessStatusCode)
			throw new InvalidOperationException($"the API answered {(int)res.StatusCode}: {text}");

		return JsonSerializer.Deserialize<JsonElement>(text);
	}

	// The instructions are the same ones the Host hands over through MCP on desktop. They are
	// repeated here because in Gate 2 there is no handshake to carry them.
	public const string Instructions = """
		You are the personal AI of the person using this device.

		Gnothi is the Host she installed. The modules are what each institution handed her
		-her lab, her supermarket- and each one keeps her own record in a folder of hers, on
		this device. Gnothi asks nothing of any company: it reads what is already here.

		What you see is what she authorized. If you expect a query that does not appear, she
		most likely turned it off; do not insist and do not try to work around it.

		State the FACTS the tools give you, with their numbers and units. Do not invent
		categories or conclusions the module did not give you, and do not give boilerplate
		advice. If something needs attention, say so and recommend she see her doctor; do not
		diagnose.
		""";
}
