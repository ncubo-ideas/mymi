using System.Text;

namespace Gnothi.Host;

// THE TABLE THAT REGISTERS GNOTHI IN CODEX -- and through Codex, in the ChatGPT desktop app.
//
// Since 2026-07-09 the ChatGPT desktop app carries Codex and Work inside, and both launch the
// local stdio MCP servers declared in ~/.codex/config.toml, the same file the Codex CLI reads.
// Measured 2026-09-09 on Windows (NotasGnothiHost/45): the Codex and Work tabs call Gnothi's
// tools; the Chat tab does not read this file at all.
//
// ═══ FOUR LINES, NOT TWO, AND THE TWO EXTRA ONES ARE THE WHOLE POINT ═══
//
// 1. required = true. Codex waits ONE SECOND for optional servers when it builds the initial
//    tool catalog (mcp_optional_startup_grace_ms). A .NET host carrying the engine takes longer,
//    so the first answer went out without Gnothi's tools and the model said "I can't find
//    Gnothi installed". Required servers wait startup_timeout_sec instead.
//
// 2. omit_tools_from = ["code_mode", "deferred"]. Codex hides MCP tools behind a tool_search
//    step the model does not take on its own. Without this line, asked "how much did I spend
//    at the supermarket", the agent ran sixteen shell commands reading the journal's .bin by
//    hand -- right number, wrong road, and past the person's consent switches. With it: one
//    direct call to the tool. Same option the app's own bundled plugin uses for its server.
//
// This class only renders and splices text. It knows nothing about files: HostInstaller
// reads, backs up and writes, exactly as it does for the JSON of Claude Desktop.
internal static class CodexTable
{
	internal const string Server = "gnothi";
	private const string Header = "[mcp_servers." + Server + "]";
	private const string SubtablePrefix = "[mcp_servers." + Server + ".";

	// The table, as it goes into the file, with the file's own line ending. Paths are TOML
	// LITERAL strings ('...'): no escaping, so a Windows path reads as a Windows path. A path
	// with a quote in it falls back to a basic string, escaped.
	internal static string Render(string executable, string modulesFolder, string newline = "\n") =>
		string.Join(newline,
		[
			Header,
			$"command = {Quote(executable)}",
			$"args = [{Quote(modulesFolder)}]",
			"# Codex waits only 1 s for optional servers; the Host takes longer to start.",
			"required = true",
			"startup_timeout_sec = 30",
			"# Without this Codex hides MCP tools behind tool_search and reads the journal by hand.",
			"omit_tools_from = [\"code_mode\", \"deferred\"]",
			"",
		]);

	internal static bool Has(string existing) =>
		Lines(existing).Any(l => l.Trim() == Header);

	// REPLACE OR APPEND, AND TOUCH NOTHING ELSE. The block to replace runs from our header to
	// the next table header that is not one of our own subtables ([mcp_servers.gnothi.env]
	// belongs to us). Everything before and after comes back as it was, line ending included.
	internal static string Upsert(string existing, string executable, string modulesFolder)
	{
		string nl = existing.Contains("\r\n") ? "\r\n" : "\n";
		string table = Render(executable, modulesFolder, nl);
		List<string> lines = Lines(existing);

		int start = lines.FindIndex(l => l.Trim() == Header);
		if (start < 0)
		{
			var sb = new StringBuilder(existing);
			if (existing.Length > 0 && !existing.EndsWith('\n')) sb.Append(nl);
			if (existing.Trim().Length > 0) sb.Append(nl);
			sb.Append(table);
			return sb.ToString();
		}

		int end = start + 1;
		while (end < lines.Count)
		{
			string t = lines[end].TrimStart();
			if (t.StartsWith('[') && !t.StartsWith(SubtablePrefix, StringComparison.Ordinal)) break;
			end++;
		}

		// Blank lines that closed the old block stay with what follows, not with us.
		while (end - 1 > start && lines[end - 1].Trim().Length == 0) end--;

		var result = new StringBuilder();
		for (int i = 0; i < start; i++) result.Append(lines[i]).Append(nl);
		result.Append(table);
		for (int i = end; i < lines.Count; i++)
		{
			result.Append(lines[i]);
			if (i < lines.Count - 1) result.Append(nl);
		}
		return result.ToString();
	}

	private static List<string> Lines(string text) =>
		text.Length == 0 ? [] : text.Replace("\r\n", "\n").Split('\n').ToList();

	private static string Quote(string value)
	{
		if (!value.Contains('\'') && !value.Contains('\n'))
			return "'" + value + "'";

		return "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
	}
}
