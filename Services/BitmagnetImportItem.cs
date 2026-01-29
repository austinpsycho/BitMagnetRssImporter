using System.Text;
using System.Text.Json;
using BitMagnetRssImporter.Models;

namespace BitMagnetRssImporter.Services;

internal class BitmagnetImporter
{
    internal static async Task PostToBitmagnetImportAsync(HttpClient http, Uri importEndpoint,
        IEnumerable<BitmagnetImportItem> items, CancellationToken ct)
    {
        // Newline-delimited JSON objects
        using var ms = new MemoryStream();
        await using (var writer = new StreamWriter(ms, new UTF8Encoding(false), leaveOpen: true))
        {
            foreach (var item in items)
            {
                var json = JsonSerializer.Serialize(item, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });
                await writer.WriteLineAsync(json);
            }

            await writer.FlushAsync();
        }

        ms.Position = 0;

        using var content = new StreamContent(ms);
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

        using var resp = await http.PostAsync(importEndpoint, content, ct);
        resp.EnsureSuccessStatusCode();
    }
}