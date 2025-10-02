using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

class Program {
    // Entry point
    static int Main(string[] args) {
        var (input, output, query) = ParseArgs(args);
        if (string.IsNullOrWhiteSpace(input) || string.IsNullOrWhiteSpace(output)) {
            Console.Error.WriteLine("ERROR: --input and --output are required.");
            return 2;
        }

        string raw = File.ReadAllText(input, Encoding.UTF8);
        string json = TryExtractJson(raw);
        JsonNode root;

        try {
            root = JsonNode.Parse(json) ?? new JsonObject();
        }
        catch (Exception ex) {
            Console.Error.WriteLine($"ERROR: Invalid JSON in {input}: {ex.Message}");
            return 3;
        }

        // 1) Clean all strings in the JSON tree
        root = CleanJsonStrings(root);

        // 2) Ensure metadata
        var metadata = root["metadata"] as JsonObject ?? new JsonObject();
        metadata["jurisdiction"] ??= "Orange County, Florida";
        metadata["fiscal_year"] ??= 2025;
        var source = metadata["source"] as JsonObject ?? new JsonObject();
        source["title"] ??= "Orange County FY 2024-25 Adopted Budget";
        source["file"] ??= "OrangeBudget2025.pdf";
        source["url"] ??= "";
        source["pages"] ??= 600;
        metadata["source"] = source;
        metadata["generated_at"] = DateTimeOffset.Now.ToString("O");
        root["metadata"] = metadata;

        // 3) Enrich categories (merge non-destructively)
        var categories = root["categories"] as JsonArray ?? new JsonArray();
        MergeCategory(categories, new() {
            ["id"] = "public_safety",
            ["label"] = "Public Safety",
            ["amount"] = 1200000000,
            ["currency"] = "USD",
            ["estimate"] = true,
            ["confidence"] = 0.7,
            ["synonyms"] = new JsonArray("police", "fire", "fire rescue", "sheriff", "EMS"),
            ["notes"] = "Approx. from initial chunk match during 1.4; verify later.",
            ["line_items"] = new JsonArray()
        });

        MergeCategory(categories, new() {
            ["id"] = "dei",
            ["label"] = "Diversity, Equity & Inclusion",
            ["amount"] = 15000000,
            ["currency"] = "USD",
            ["estimate"] = true,
            ["confidence"] = 0.6,
            ["synonyms"] = new JsonArray("DEI", "diversity", "equity", "inclusion", "DEIA"),
            ["notes"] = "Seeded estimate for testing; verify later against budget text.",
            ["line_items"] = new JsonArray()
        });

        root["categories"] = categories;

        // 4) Write pretty JSON back into .md file
        var pretty = JsonSerializer.Serialize(root, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(output, pretty + Environment.NewLine, Encoding.UTF8);

        Console.WriteLine($"✅ Cleaned & enriched: {output}");

        // 5) Optional: quick query to verify enrichment
        if (!string.IsNullOrWhiteSpace(query)) {
            var (match, amount, est) = QueryCategory(root, query);
            if (match != null) {
                Console.WriteLine($"🔎 Query '{query}' → {match} = {FormatUsd(amount)}{(est ? " (estimate)" : "")}");
                return 0;
            }
            Console.WriteLine($"🔎 Query '{query}' → no match.");
        }

        return 0;
    }

    // ---- helpers ----

    static (string input, string output, string query) ParseArgs(string[] args) {
        string input = "", output = "", query = "";
        for (int i = 0; i < args.Length; i++) {
            if (args[i] == "--input" && i + 1 < args.Length) input = args[++i];
            else if (args[i] == "--output" && i + 1 < args.Length) output = args[++i];
            else if (args[i] == "--query" && i + 1 < args.Length) query = args[++i];
        }
        return (input, output, query);
    }

    static string TryExtractJson(string content) {
        int firstBrace = content.IndexOf('{');
        int lastBrace = content.LastIndexOf('}');
        if (firstBrace >= 0 && lastBrace > firstBrace)
            return content.Substring(firstBrace, lastBrace - firstBrace + 1);
        return content;
    }

    static JsonNode? CleanJsonStrings(JsonNode? node) {
        if (node is null) return null;

        switch (node) {
            case JsonValue v:
                if (v.TryGetValue<string>(out var s)) {
                    // Clean strings
                    return JsonValue.Create(CleanText(s));
                }
                else if (v.TryGetValue<long>(out var l)) {
                    return JsonValue.Create(l);
                }
                else if (v.TryGetValue<double>(out var d)) {
                    return JsonValue.Create(d);
                }
                else if (v.TryGetValue<bool>(out var b)) {
                    return JsonValue.Create(b);
                }
                // fallback: clone as raw text
                return JsonValue.Create(v.ToJsonString());

            case JsonArray arr:
                var outArr = new JsonArray();
                foreach (var item in arr) {
                    outArr.Add(CleanJsonStrings(item));
                }
                return outArr;

            case JsonObject obj:
                var outObj = new JsonObject();
                foreach (var kv in obj) {
                    outObj[kv.Key] = CleanJsonStrings(kv.Value);
                }
                return outObj;

            default:
                // always return a new JsonValue
                return JsonValue.Create(node.ToJsonString());
        }
    }

    static string CleanText(string s) {
        if (string.IsNullOrEmpty(s)) return s;

        s = s.Replace("ﬁ", "fi").Replace("ﬂ", "fl");
        s = Regex.Replace(s, @"(\w)-\s*\r?\n\s*(\w)", "$1$2");
        s = Regex.Replace(s, @"(?mi)^\s*Page\s+\d+\s+of\s+\d+\s*$", "");
        s = Regex.Replace(s, @"[ \t]+", " ");
        s = Regex.Replace(s, @"\r?\n{2,}", "\n");
        s = s.Replace('“', '"').Replace('”', '"').Replace('’', '\'').Replace('—', '-');

        return s.Trim();
    }

    static void MergeCategory(JsonArray categories, JsonObject seed) {
        string id = seed["id"]?.ToString() ?? "";
        var existing = categories
            .OfType<JsonObject>()
            .FirstOrDefault(o => string.Equals(o["id"]?.ToString(), id, StringComparison.OrdinalIgnoreCase));

        if (existing is null) {
            categories.Add(seed);
            return;
        }

        foreach (var kv in seed) {
            if (!existing.ContainsKey(kv.Key) || existing[kv.Key] is null)
                existing[kv.Key] = kv.Value;
        }

        if (existing["synonyms"] is JsonArray es && seed["synonyms"] is JsonArray ss) {
            var set = es.Select(x => x?.ToString() ?? "").Where(x => x != "").ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var s in ss) {
                var val = s?.ToString() ?? "";
                if (val != "" && !set.Contains(val)) {
                    es.Add(val);
                    set.Add(val);
                }
            }
        }
    }

    static (string? label, long amount, bool estimate) QueryCategory(JsonNode root, string query) {
        var q = query.ToLowerInvariant();
        var cats = root["categories"] as JsonArray;
        if (cats is null) return (null, 0, false);

        foreach (var item in cats.OfType<JsonObject>()) {
            var label = item["label"]?.ToString() ?? item["id"]?.ToString() ?? "unknown";
            var id = item["id"]?.ToString()?.ToLowerInvariant() ?? "";
            var synonyms = (item["synonyms"] as JsonArray)?.Select(x => x?.ToString()?.ToLowerInvariant() ?? "").ToList() ?? new();

            if (q.Contains(id) || q.Contains((label ?? "").ToLowerInvariant()) || synonyms.Any(q.Contains)) {
                long amount = 0;
                bool est = false;
                if (item["amount"] != null && long.TryParse(item["amount"]!.ToString(), out var a)) amount = a;
                if (item["estimate"] != null && bool.TryParse(item["estimate"]!.ToString(), out var e)) est = e;
                return (label, amount, est);
            }
        }
        return (null, 0, false);
    }

    static string FormatUsd(long amount) {
        return string.Format(System.Globalization.CultureInfo.GetCultureInfo("en-US"), "${0:N0}", amount);
    }
}
