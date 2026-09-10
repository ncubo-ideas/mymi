using System.Net;
using System.Text;
using System.Text.Json;

namespace Gnothi.Movil;

// THE COURIER. The only thing in the system that goes out to the network, and the reason
// Gnothi holds the internet permission while the module cannot use it (specification 11 §6,
// 13 §8).
//
//     module ── gnothi.pull(after=N) ──► COURIER ── HTTPS ──► publisher
//     module ◄────── the envelope ──────  COURIER ◄────────────┘
//
// THREE INVARIANTS, and each one is here for a reason:
//
//   THE CURSOR BELONGS TO THE ACTOR. The `after` arrives from the module, out of the replay
//   of its own journal. The courier validates it as an integer and relays it; it does NOT
//   store it. There are no two truths about how far it read, and therefore none that can go
//   stale.
//
//   THE ENDPOINT IS FIXED. It is registered when the module is installed and the module
//   neither chooses it nor can change it. "Fetch me from somewhere else" is not an operation
//   that exists.
//
//   THE CREDENTIAL BELONGS TO GNOTHI. It is obtained against Gnothi's own device key, kept in
//   the platform's secret store, and the module never sees it.
//
// THE ENVELOPE TRAVELS AS IS. The courier does not open it, does not reorder it and does not
// add anything to it: it is the module's arm, not a second author.
internal sealed class GnothiCourier
{
	private readonly HttpClient http;
	private readonly string resource;
	private readonly string id;
	private readonly GnothiCredential credential;
	private readonly string module;
	private readonly Action<string> log;

	// The endpoint arrives as `https://host/base|resource|id` in the installation manifest.
	// Three parts because they are three different things: where the publisher is, which
	// resource of the person, and which one.
	internal GnothiCourier(string module, string endpoint, GnothiCredential credential, Action<string> log)
	{
		this.module = module;
		this.credential = credential;
		this.log = log;

		string[] parts = endpoint.Split('|');
		string baseUrl = parts.Length > 0 && parts[0].Length > 0 ? parts[0] : "";
		resource = parts.Length > 1 && parts[1].Length > 0 ? parts[1] : "expediente";
		id = parts.Length > 2 && parts[2].Length > 0 ? parts[2] : "1";

		if (!baseUrl.EndsWith('/')) baseUrl += "/";
		http = new HttpClient { BaseAddress = new Uri(baseUrl), Timeout = TimeSpan.FromSeconds(20) };
	}

	// WHAT THE MODULE ASKS FOR. Returns the publisher's envelope, exactly as it came.
	//
	// If the publisher rejects the credential a new one is requested ONCE and the read is
	// retried. If the second attempt also fails the problem is not expiry, and retrying would
	// be punishing the publisher with requests we already know will fail.
	internal async Task<string> Pull(long from, CancellationToken ct = default)
	{
		if (from < 0) throw new ArgumentException($"invalid cursor: {from}");

		try
		{
			string token = await Credential(ct);

			HttpResponseMessage response = await Read(from, token, ct);
			if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
			{
				response.Dispose();
				log($"{module}: the publisher rejected the credential; asking for another");
				response = await Read(from, await Renew(ct), ct);
			}

			using (response)
			{
				response.EnsureSuccessStatusCode();
				string envelope = await response.Content.ReadAsStringAsync(ct);
				log($"{module}: pull after={from} -> {envelope.Length} bytes");
				return envelope;
			}
		}
		catch (Exception ex)
		{
			// A COURIER THAT FAILS CANNOT FAIL SILENTLY. The module still opens -- showing what
			// it already has, which is the whole point of owning the data -- and without this
			// nobody learns why the new facts stopped arriving. Same discipline as the
			// journal's flush: a mute degradation is worse than an error.
			log($"{module}: the courier could not fetch after={from}: {ex.GetType().Name}: {ex.Message}");
			throw;
		}
	}

	private Task<HttpResponseMessage> Read(long from, string token, CancellationToken ct)
	{
		var request = new HttpRequestMessage(HttpMethod.Get,
			$"{resource}/{Uri.EscapeDataString(id)}/events?after={from}");
		request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

		return http.SendAsync(request, ct);
	}

	private async Task<string> Credential(CancellationToken ct)
	{
		string kept = credential.Token;
		return kept.Length > 0 ? kept : await Renew(ct);
	}

	// GNOTHI INTRODUCES ITSELF AS ANOTHER CLIENT, with its own device key. The publisher issues
	// a grant scoped to reading the events of THAT resource: no profile, no other patients. And
	// at that very moment it can mark the account as migrated, which is what the standalone
	// installation reads on its next pull (13 §4).
	//
	// [ASSUMPTION] This is the "publisher enrolment" mechanism of 13 §3.2, which is the one the
	// mock implements. The other one -- OAuth with PKCE through the system browser, with Auth
	// Tab on Android -- requires the publisher to have an authorization server, and none has
	// one yet. When one does, THIS is the only function that changes.
	private async Task<string> Renew(CancellationToken ct)
	{
		string device = credential.EnsureDeviceKey();

		string body = JsonSerializer.Serialize(new { deviceKey = device, recurso = resource, id });
		using var content = new StringContent(body, Encoding.UTF8, "application/json");

		using HttpResponseMessage response = await http.PostAsync("device/token", content, ct);
		response.EnsureSuccessStatusCode();

		using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
		string token = Text(document.RootElement, "accessToken");
		credential.Save(token, Text(document.RootElement, "refreshToken"));

		log($"{module}: Gnothi obtained its credential from the publisher");
		return token;
	}

	private static string Text(JsonElement obj, string field) =>
		obj.TryGetProperty(field, out JsonElement value) && value.ValueKind == JsonValueKind.String
			? value.GetString() ?? ""
			: "";
}
