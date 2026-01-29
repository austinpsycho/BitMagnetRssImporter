using System.Security.Cryptography;
using MonoTorrent.BEncoding;

namespace BitMagnetRssImporter.Services;

public sealed class UrlInfoHashParser
{
    private static string ComputeInfoHashFromTorrentBytes(byte[] bytes)
    {
        // Parse encoded torrent
        var torrent = (BEncodedDictionary)BEncodedValue.Decode(bytes);

        var info = (BEncodedDictionary)torrent["info"];

        // infohash = SHA1(bencode(info))
        var infoBytes = info.Encode();

        var hash = SHA1.HashData(infoBytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
    internal static async Task<string?> TryComputeInfoHashFromTorrentUrlAsync(HttpClient http, string torrentUrl, CancellationToken ct)
    {
        try
        {
            var bytes = await http.GetByteArrayAsync(torrentUrl, ct);

            // Quick sanity: bencoded dictionaries start with 'd'
            if (bytes.Length == 0 || bytes[0] != (byte)'d')
                return null;

            return ComputeInfoHashFromTorrentBytes(bytes);
        }
        catch
        {
            return null;
        }
    }
}