using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// QuickSheet Hacker News Top Stories Extension.
/// Prefix: "hntop". Usage: "hntop: 10" (show top 10 stories).
/// Uses HN Firebase API — free, no API key, no rate limits.
/// </summary>
class Program
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };

    private static readonly HttpClient Http = new()
    {
        Timeout = TimeSpan.FromSeconds(15)
    };

    // Cache: top story IDs + items
    private static int[]? _cachedTopIds;
    private static readonly Dictionary<int, HnItem> _itemCache = new();
    private static DateTime _lastFetch = DateTime.MinValue;
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    static void Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        string? line;
        while ((line = Console.ReadLine()) != null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            try
            {
                using var doc = JsonDocument.Parse(line);
                string? type = doc.RootElement.TryGetProperty("type", out var tp) ? tp.GetString() : null;

                switch (type)
                {
                    case "init":
                        HandleInit();
                        break;
                    case "activate":
                        HandleActivate(doc.RootElement);
                        break;
                    case "deactivate":
                        break;
                }
            }
            catch (Exception ex)
            {
                SendJson(new { type = "error", id = "", message = $"Parse error: {ex.Message}" });
            }
        }
    }

    static void HandleInit()
    {
        SendJson(new
        {
            type = "register",
            prefix = "hntop",
            name = "HN Top Stories",
            version = "1.0.0"
        });
        SendLog("HN Top Stories registered. Using HN Firebase API (no key needed).");
    }

    static void HandleActivate(JsonElement root)
    {
        string id = root.TryGetProperty("id", out var idProp) ? idProp.GetString() ?? "" : "";

        string[] extParams = [];
        if (root.TryGetProperty("params", out var paramsProp) && paramsProp.ValueKind == JsonValueKind.Array)
        {
            extParams = paramsProp.EnumerateArray()
                .Select(p => p.GetString()?.Trim() ?? "")
                .Where(p => p.Length > 0)
                .ToArray();
        }

        // Parse count (default 10, max 30)
        int count = 10;
        if (extParams.Length >= 1 && int.TryParse(extParams[0], out int parsed) && parsed > 0)
            count = Math.Min(parsed, 30);

        try
        {
            var stories = FetchTopStories(count);
            var cells = new List<(int r, int c, string v)>();

            // Header row
            cells.Add((0, 0, "🔶 Hacker News Top Stories"));
            cells.Add((0, 1, "Score"));
            cells.Add((0, 2, "Comments"));

            for (int i = 0; i < stories.Count; i++)
            {
                var s = stories[i];
                int row = i + 1;
                string rank = $"{i + 1}.";
                string title = s.Title ?? "(untitled)";

                // Truncate long titles
                if (title.Length > 60)
                    title = title[..57] + "...";

                cells.Add((row, 0, $"{rank} {title}"));
                cells.Add((row, 1, $"▲ {s.Score}"));
                cells.Add((row, 2, $"💬 {s.Descendants}"));
            }

            // Footer with cache info
            cells.Add((stories.Count + 1, 0, $"Updated {DateTime.Now:HH:mm} · hntop: {count}"));

            SendCells(id, cells);
        }
        catch (Exception ex)
        {
            SendCells(id, new List<(int r, int c, string v)> { (0, 0, $"⚠️ HN fetch failed: {ex.Message}"), (1, 0, "Retrying on next activation...") });
        }
    }

    static List<HnItem> FetchTopStories(int count)
    {
        bool needRefresh = _cachedTopIds == null ||
                           (DateTime.UtcNow - _lastFetch) > CacheTtl;

        if (needRefresh)
        {
            string json = Http.GetStringAsync("https://hacker-news.firebaseio.com/v0/topstories.json")
                .GetAwaiter().GetResult();
            _cachedTopIds = JsonSerializer.Deserialize<int[]>(json) ?? [];
            _lastFetch = DateTime.UtcNow;
            _itemCache.Clear(); // Refresh items too
        }

        var result = new List<HnItem>();
        var idsToFetch = _cachedTopIds!.Take(count).ToArray();

        foreach (int id in idsToFetch)
        {
            if (!_itemCache.TryGetValue(id, out var item))
            {
                string itemJson = Http.GetStringAsync(
                    $"https://hacker-news.firebaseio.com/v0/item/{id}.json")
                    .GetAwaiter().GetResult();
                item = JsonSerializer.Deserialize<HnItem>(itemJson, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                if (item != null)
                    _itemCache[id] = item;
            }

            if (item != null)
                result.Add(item);
        }

        return result;
    }

    static void SendCells(string id, List<(int r, int c, string v)> cells)
    {
        SendJson(new
        {
            type = "write",
            id,
            cells = cells.Select(c => new { r = c.r, c = c.c, v = c.v }).ToArray()
        });
    }

    static void SendCells(string id, (int r, int c, string v)[] cells)
    {
        SendCells(id, cells.ToList());
    }

    static void SendJson(object obj)
    {
        string json = JsonSerializer.Serialize(obj, JsonOpts);
        Console.WriteLine(json);
        Console.Out.Flush();
    }

    static void SendLog(string message)
    {
        SendJson(new { type = "log", message });
    }
}

class HnItem
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("score")]
    public int Score { get; set; }

    [JsonPropertyName("descendants")]
    public int Descendants { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("by")]
    public string? By { get; set; }

    [JsonPropertyName("time")]
    public long Time { get; set; }
}
