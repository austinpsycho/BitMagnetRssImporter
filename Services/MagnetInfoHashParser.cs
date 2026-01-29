namespace BitMagnetRssImporter.Services;

public sealed class MagnetInfoHashParser
{
    internal static string? TryGetInfoHashFromMagnet(string magnetUri)
    {
        if (!magnetUri.StartsWith("magnet:?", StringComparison.OrdinalIgnoreCase))
            return null;

        var uri = new Uri(magnetUri);
        var query = uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries);

        // Look for xt=urn:btih:<hash>
        foreach (var kv in query)
        {
            var parts = kv.Split('=', 2);
            if (parts.Length != 2) continue;
            if (!parts[0].Equals("xt", StringComparison.OrdinalIgnoreCase)) continue;

            var value = Uri.UnescapeDataString(parts[1]);
            const string prefix = "urn:btih:";
            if (!value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) continue;

            var raw = value[prefix.Length..].Trim();

            // btih is typically either:
            // - 40 hex chars (SHA1)
            // - 32 base32 chars (SHA1)
            if (raw.Length == 40 && raw.All(Uri.IsHexDigit))
                return raw.ToLowerInvariant();

            if (raw.Length == 32)
            {
                var bytes = Base32Decode(raw);
                if (bytes.Length == 20) // SHA1 length
                    return Convert.ToHexString(bytes).ToLowerInvariant();
            }
        }

        return null;
    }

    static byte[] Base32Decode(string input)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var s = input.Trim().TrimEnd('=').ToUpperInvariant();

        var output = new List<byte>(s.Length * 5 / 8);
        int buffer = 0, bitsLeft = 0;

        foreach (var c in s)
        {
            var val = alphabet.IndexOf(c);
            if (val < 0) throw new FormatException($"Invalid base32 char '{c}'");

            buffer = (buffer << 5) | val;
            bitsLeft += 5;

            if (bitsLeft >= 8)
            {
                bitsLeft -= 8;
                output.Add((byte)((buffer >> bitsLeft) & 0xFF));
            }
        }

        return output.ToArray();
    }
}