using System.Text;

namespace AiLandscapeDiscovery.Cli.Output;

public static class CsvTableWriter
{
    public static async Task WriteAsync(
        string path,
        IReadOnlyList<string> headers,
        IEnumerable<IReadOnlyList<string?>> rows,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
        await using var stream = File.Create(path);
        await using var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));

        await writer.WriteLineAsync(string.Join(",", headers.Select(Escape)).AsMemory(), cancellationToken);
        foreach (IReadOnlyList<string?> row in rows)
        {
            await writer.WriteLineAsync(string.Join(",", row.Select(Escape)).AsMemory(), cancellationToken);
        }
    }

    public static string Escape(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        bool mustQuote = value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r');
        string escaped = value.Replace("\"", "\"\"", StringComparison.Ordinal);
        return mustQuote ? $"\"{escaped}\"" : escaped;
    }
}
