using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Gnothi.Assistant;

// THE MODEL, THROUGH ANY PROVIDER THAT SPEAKS THE OPENAI SHAPE.
//
// WHY A SECOND IMPLEMENTATION EXISTS, and it is not to have options for their own sake:
//
//   1. ANTHROPIC'S API HAS NO FREE TIER. It is prepaid credits. That makes finishing Gate 2
//      wait on a commercial decision -- BYOK vs our own key -- which is exactly the thing
//      that is NOT decided. Several other providers do have a permanent free tier, and they
//      all speak this shape, so one file unblocks the whole loop.
//
//   2. A LOCAL MODEL SPEAKS IT TOO. LM Studio and Ollama serve /chat/completions. So this
//      same class points at http://localhost:1234/v1 with no key at all -- and then NOTHING
//      crosses to any cloud, which is the strongest version of the architecture and the one
//      that empties NotasGnothiHost/12.
//
//   3. AND IT PROVES THE SEAM. IModel being an interface was a claim: that switching the
//      model is configuration, not code. Until there were two implementations it was
//      untested. Now AgentLoop runs against either one without changing a line, which is
//      also what makes "our own key" reversible if that is ever decided.
//
// WHERE IT DIFFERS FROM ANTHROPIC, and each difference cost a reading of the spec:
//
//   tools           { type: "function", function: { name, description, parameters } }
//                   instead of a flat { name, description, input_schema }
//   arguments       arrive as a JSON **string** to be parsed, not as an object
//   tool results    ONE message per result, role "tool" -- the exact OPPOSITE of Anthropic,
//                   which demands all of a round's results inside a single user message
//   instructions    a message with role "system", not a top-level field
public sealed class ModelOpenAiCompatible : IModel
{
	private readonly HttpClient http;
	private readonly string url;
	private readonly string model;
	private readonly string? key;

	// A few providers that work with this class, so nobody has to go looking:
	//
	//   Groq        https://api.groq.com/openai/v1   ~30 req/min free, no card asked
	//   Cerebras    https://api.cerebras.ai/v1       1M tokens/day free
	//   Mistral     https://api.mistral.ai/v1        1B tokens/month, but 2 req/min
	//   OpenRouter  https://openrouter.ai/api/v1     ~30 free models behind one key
	//   Gemini      https://generativelanguage.googleapis.com/v1beta/openai
	//   LM Studio   http://localhost:1234/v1         no key, nothing leaves the machine
	//   Ollama      http://localhost:11434/v1        idem
	//
	// Free tiers change month to month. If one stops working, the fix is a different base
	// URL, not different code.
	public ModelOpenAiCompatible(string baseUrl, string model, string? keyFile = null, HttpClient? http = null)
	{
		this.model = model;
		this.url = baseUrl.TrimEnd('/') + "/chat/completions";
		this.http = http ?? new HttpClient { Timeout = TimeSpan.FromSeconds(120) };

		// THE KEY IS OPTIONAL HERE, and that is the point: a local model does not have one.
		// Same order as ModelClaude -- environment first, then a file -- and never the code.
		string? k = Environment.GetEnvironmentVariable("MODEL_API_KEY");

		if (string.IsNullOrWhiteSpace(k) && keyFile is not null && File.Exists(keyFile))
			k = File.ReadAllText(keyFile).Trim();

		key = string.IsNullOrWhiteSpace(k) ? null : k;
	}

	// THE REQUEST BODY, in one place and public, for the same reason as in ModelClaude: the
	// cost per question can be MEASURED without sending anything.
	public static Dictionary<string, object?> Body(
		string model, IReadOnlyList<Exchange> conversation, IReadOnlyList<ToolForTheModel> tools) =>
		new()
		{
			["model"] = model,
			["max_tokens"] = 1024,
			["tools"] = tools.Select(t => new Dictionary<string, object?>
			{
				["type"] = "function",
				["function"] = new Dictionary<string, object?>
				{
					["name"] = t.Name,
					["description"] = t.Description,
					["parameters"] = JsonSerializer.Deserialize<JsonElement>(t.SchemaJson),
				},
			}).ToArray(),
			["messages"] = Messages(conversation),
		};

	// THE EXCHANGE, TRANSLATED. The instructions travel as the first message here, and each
	// tool result as its own message -- which is the opposite of Anthropic's grouping. Getting
	// this backwards is rejected by both APIs, each for the opposite reason.
	private static object[] Messages(IReadOnlyList<Exchange> conversation)
	{
		List<object> messages =
		[
			new Dictionary<string, object?> { ["role"] = "system", ["content"] = ModelClaude.Instructions },
		];

		foreach (Exchange e in conversation)
			switch (e)
			{
				case Asked a:
					messages.Add(new Dictionary<string, object?> { ["role"] = "user", ["content"] = a.Question });
					break;

				case Said s:
					var assistant = new Dictionary<string, object?>
					{
						["role"] = "assistant",
						["content"] = s.Text,
					};

					if (s.Calls.Count > 0)
						assistant["tool_calls"] = s.Calls.Select(c => new Dictionary<string, object?>
						{
							["id"] = c.Id,
							["type"] = "function",
							["function"] = new Dictionary<string, object?>
							{
								["name"] = c.PublicName,
								// The arguments go as a STRING, not as an object. It is the
								// shape the API defines, however odd it reads.
								["arguments"] = JsonSerializer.Serialize(c.Arguments),
							},
						}).ToArray();

					messages.Add(assistant);
					break;

				case Returned r:
					messages.Add(new Dictionary<string, object?>
					{
						["role"] = "tool",
						["tool_call_id"] = r.CallId,
						// There is no is_error field: a failure travels as the content of the
						// result, said in words, and the model reads it as one more fact.
						["content"] = r.Failed ? $"the query failed: {r.Json}" : r.Json,
					});
					break;
			}

		return [.. messages];
	}

	public async Task<Said> Next(
		IReadOnlyList<Exchange> conversation, IReadOnlyList<ToolForTheModel> tools, CancellationToken ct)
	{
		JsonElement answer = await Post(Body(model, conversation, tools), ct);

		if (!answer.TryGetProperty("choices", out JsonElement choices) || choices.GetArrayLength() == 0)
			return new Said("(the model answered nothing)", []);

		JsonElement message = choices[0].GetProperty("message");

		string? text = message.TryGetProperty("content", out JsonElement c) && c.ValueKind == JsonValueKind.String
			? c.GetString()
			: null;

		List<ToolCall> calls = [];

		// EVERY tool_call, for the same reason as in ModelClaude: taking only the first is
		// what makes a cross of two modules fail in silence.
		if (message.TryGetProperty("tool_calls", out JsonElement raw) && raw.ValueKind == JsonValueKind.Array)
			foreach (JsonElement call in raw.EnumerateArray())
			{
				JsonElement fn = call.GetProperty("function");

				Dictionary<string, object?> args = [];

				// The arguments arrive as a string with json inside. A model can produce
				// something that does not parse, and that is not a reason to lose the whole
				// answer: the call goes with no arguments and the module will complain, which
				// is the one who knows what it needed.
				if (fn.TryGetProperty("arguments", out JsonElement a) && a.ValueKind == JsonValueKind.String)
					try
					{
						JsonElement parsed = JsonSerializer.Deserialize<JsonElement>(a.GetString() ?? "{}");
						if (parsed.ValueKind == JsonValueKind.Object)
							foreach (JsonProperty p in parsed.EnumerateObject())
								args[p.Name] = p.Value.ValueKind == JsonValueKind.String
									? p.Value.GetString()
									: p.Value.GetRawText();
					}
					catch (JsonException) { /* left empty on purpose */ }

				calls.Add(new ToolCall(
					call.TryGetProperty("id", out JsonElement id) ? id.GetString() ?? "" : "",
					fn.GetProperty("name").GetString() ?? "",
					args));
			}

		return new Said(text, calls);
	}

	private async Task<JsonElement> Post(Dictionary<string, object?> body, CancellationToken ct)
	{
		using var request = new HttpRequestMessage(HttpMethod.Post, url);

		// No key means a local model. Sending an empty Authorization header would make some
		// servers reject a request that was fine.
		if (key is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);

		request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8);
		request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

		using HttpResponseMessage res = await http.SendAsync(request, ct);
		string text = await res.Content.ReadAsStringAsync(ct);

		if (!res.IsSuccessStatusCode)
			throw new InvalidOperationException($"the API answered {(int)res.StatusCode}: {text}");

		return JsonSerializer.Deserialize<JsonElement>(text);
	}
}
