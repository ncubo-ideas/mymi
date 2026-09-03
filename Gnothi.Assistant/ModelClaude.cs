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
	public static Dictionary<string, object?> Body(
		string model, IReadOnlyList<Exchange> conversation, IReadOnlyList<ToolForTheModel> tools) =>
		new()
		{
			["model"] = model,
			["max_tokens"] = 1024,
			["system"] = Instructions,
			["tools"] = tools.Select(t => new Dictionary<string, object?>
			{
				["name"] = t.Name,
				["description"] = t.Description,
				["input_schema"] = Json.Parse(t.SchemaJson),
			}).ToArray(),
			["messages"] = Messages(conversation),
		};

	// THE EXCHANGE, TRANSLATED TO THE API'S SHAPE. One subtlety carries the whole cross: every
	// tool_result of a round has to travel in a SINGLE user message. One message per result is
	// rejected, and one message per result is exactly what a naive entry-by-entry translation
	// produces -- which is why the grouping is here and not left to the caller.
	private static object[] Messages(IReadOnlyList<Exchange> conversation)
	{
		List<object> messages = [];
		List<object> pending = [];

		void Flush()
		{
			if (pending.Count == 0) return;
			messages.Add(new Dictionary<string, object?> { ["role"] = "user", ["content"] = pending.ToArray() });
			pending = [];
		}

		foreach (Exchange e in conversation)
			switch (e)
			{
				case Asked a:
					Flush();
					messages.Add(new Dictionary<string, object?> { ["role"] = "user", ["content"] = a.Question });
					break;

				case Said s:
					Flush();
					List<object> blocks = [];
					if (!string.IsNullOrWhiteSpace(s.Text))
						blocks.Add(new Dictionary<string, object?> { ["type"] = "text", ["text"] = s.Text });
					foreach (ToolCall c in s.Calls)
						blocks.Add(new Dictionary<string, object?>
						{
							["type"] = "tool_use",
							["id"] = c.Id,
							["name"] = c.PublicName,
							["input"] = c.Arguments,
						});
					messages.Add(new Dictionary<string, object?> { ["role"] = "assistant", ["content"] = blocks.ToArray() });
					break;

				case Returned r:
					pending.Add(new Dictionary<string, object?>
					{
						["type"] = "tool_result",
						["tool_use_id"] = r.CallId,
						["content"] = r.Json,
						["is_error"] = r.Failed,
					});
					break;
			}

		Flush();
		return [.. messages];
	}

	public async Task<Said> Next(
		IReadOnlyList<Exchange> conversation, IReadOnlyList<ToolForTheModel> tools, CancellationToken ct)
	{
		JsonElement answer = await Post(Body(model, conversation, tools), ct);

		string? text = null;
		List<ToolCall> calls = [];

		foreach (JsonElement b in answer.GetProperty("content").EnumerateArray())
			switch (b.GetProperty("type").GetString() ?? "")
			{
				case "text":
					text ??= b.GetProperty("text").GetString();
					break;

				// EVERY tool_use block, not the first one. Returning on the first is what made
				// the cross fail in silence: asked about lab tests AND invoices, the model
				// answers with two, one got run, and the sentence read as if that were all the
				// facts there were.
				case "tool_use":
					Dictionary<string, object?> args = [];
					if (b.TryGetProperty("input", out JsonElement input))
						foreach (JsonProperty p in input.EnumerateObject())
							args[p.Name] = p.Value.ValueKind == JsonValueKind.String
								? p.Value.GetString()
								: p.Value.GetRawText();

					calls.Add(new ToolCall(
						b.GetProperty("id").GetString() ?? "",
						b.GetProperty("name").GetString() ?? "",
						args));
					break;
			}

		// The name is NOT taken apart here. Who contributes a public name is the Host's index's
		// business, and the loop is the one holding it -- this class only reports what the model
		// asked for.
		return new Said(text, calls);
	}


	private async Task<JsonElement> Post(Dictionary<string, object?> body, CancellationToken ct)
	{
		using var request = new HttpRequestMessage(HttpMethod.Post, Url);
		request.Headers.Add("x-api-key", key);
		request.Headers.Add("anthropic-version", Version);
		request.Content = new StringContent(Json.Write(body), Encoding.UTF8);
		request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

		using HttpResponseMessage res = await http.SendAsync(request, ct);
		string text = await res.Content.ReadAsStringAsync(ct);

		if (!res.IsSuccessStatusCode)
			throw new InvalidOperationException($"the API answered {(int)res.StatusCode}: {text}");

		return Json.Parse(text);
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
