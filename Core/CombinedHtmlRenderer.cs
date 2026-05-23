using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace ItemDataConverter.Core
{
    /// <summary>
    /// アイテム一覧とオプション一覧を統合した HTML を生成する。
    /// </summary>
    public static class CombinedHtmlRenderer
    {
        private const string TemplateResourceName = "ItemDataConverter.Templates.CombinedTemplate.html";

        private static readonly Dictionary<int, string> TypeMap = new Dictionary<int, string>
        {
            [0] = "帽子", [1] = "冠", [2] = "グローブ", [3] = "槍投", [4] = "クロー", [5] = "手首", [6] = "ベルト", [7] = "足",
            [8] = "首", [9] = "指", [10] = "耳", [11] = "背中", [12] = "ブロ", [13] = "腕刺青", [14] = "肩刺青", [15] = "十字架",
            [16] = "鎧", [17] = "職鎧", [18] = "片手剣", [19] = "盾", [20] = "両手剣", [21] = "杖", [22] = "牙", [23] = "棍棒",
            [24] = "翼", [25] = "短剣", [26] = "弓", [27] = "矢", [28] = "槍", [29] = "笛", [30] = "スリング", [31] = "ボトル",
            [32] = "棒", [33] = "鞭", [34] = "原石", [35] = "赤POT", [36] = "青POT", [37] = "水薬", [38] = "能力アップ", [39] = "異常回復",
            [40] = "復活系", [41] = "鍵", [42] = "帰還", [43] = "必殺技の巻物", [44] = "お菓子", [45] = "霊薬", [46] = "魔法液",
            [47] = "セッティング原石", [48] = "その他特殊アイテム", [49] = "クエストアイテム", [50] = "課金アイテム",
            [51] = "エンチャント系", [52] = "ロト系", [54] = "鎌", [55] = "闘士武器", [56] = "本"
        };

        private static readonly string[] ReqLabels = { "力", "敏捷", "健康", "知恵", "知識", "カリスマ", "幸運" };

        public static void SaveCombinedHtml(
            Dictionary<string, Dictionary<string, object>> items,
            Dictionary<string, Dictionary<string, object>> ops,
            string outputPath,
            string? platformPrefix,
            bool saveFinalJson = false)
        {
            var jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = false,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            var renderedItems = new Dictionary<string, List<string>>();
            if (items != null)
            {
                foreach (var key in items.Keys)
                {
                    renderedItems[key] = MakeEntry(items[key]);
                }
            }

            if (saveFinalJson)
            {
                try
                {
                    var finalOptions = new JsonSerializerOptions
                    {
                        WriteIndented = true,
                        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                    };
                    string finalJson = JsonSerializer.Serialize(renderedItems, finalOptions);
                    string jsonOutPath = Path.ChangeExtension(outputPath, ".json");
                    File.WriteAllText(jsonOutPath, finalJson, Encoding.UTF8);
                }
                catch
                {
                }
            }

            string itemJson = JsonSerializer.Serialize(renderedItems, jsonOptions);
            string opsJson = ops != null ? JsonSerializer.Serialize(ops, jsonOptions) : "{}";
            string title = string.IsNullOrEmpty(platformPrefix) ? "データ検索" : platformPrefix + " データ検索";

            string html = LoadTemplate()
                .Replace("__PLATFORM_TITLE__", title)
                .Replace("/* ITEM_DATA_PLACEHOLDER */", itemJson)
                .Replace("/* OPS_DATA_PLACEHOLDER */", opsJson)
                .Replace("/* ICON_MAP_PLACEHOLDER */", "{}")
                .Replace("/* ICON_BASE64_PLACEHOLDER */", "{}");

            File.WriteAllText(outputPath, html, Encoding.UTF8);
        }

        private static string LoadTemplate()
        {
            string path = Path.Combine(AppContext.BaseDirectory, "Templates", "CombinedTemplate.html");
            if (!File.Exists(path))
            {
                path = Path.Combine(AppContext.BaseDirectory, "CombinedTemplate.html");
            }
            if (File.Exists(path))
            {
                return File.ReadAllText(path, Encoding.UTF8);
            }

            var assembly = Assembly.GetExecutingAssembly();
            using Stream? stream = assembly.GetManifestResourceStream(TemplateResourceName);
            if (stream == null)
            {
                throw new InvalidOperationException("テンプレートファイルが見つかりません: CombinedTemplate.html");
            }

            using var reader = new StreamReader(stream, Encoding.UTF8);
            return reader.ReadToEnd();
        }

        private static List<string> MakeEntry(Dictionary<string, object> item)
        {
            var outList = new List<string>();

            string name = StripColorTags(GetString(item, "Name") ?? "");
            outList.Add(name);
            outList.Add("<基本情報>");

            int? typeVal = GetInt(item, "Type");
            string typeStr = "";
            if (typeVal.HasValue && TypeMap.TryGetValue(typeVal.Value, out var mappedType))
            {
                typeStr = mappedType;
            }
            else if (typeVal.HasValue)
            {
                typeStr = typeVal.Value.ToString();
            }
            outList.Add($"- {typeStr}");

            string? atk = GetString(item, "攻撃力");
            if (!string.IsNullOrEmpty(atk))
            {
                outList.Add($"- 攻撃力 {atk}");
            }

            if (item.TryGetValue("射程", out var rangeVal) && rangeVal != null)
            {
                outList.Add($"- 射程 {rangeVal}");
            }

            if (item.TryGetValue("unique_effects_text", out var uniqueEffects) && uniqueEffects is List<string> effectList)
            {
                foreach (string line in effectList)
                {
                    string normalized = StripColorTags(line);
                    if (!normalized.StartsWith("- "))
                    {
                        normalized = "- " + normalized;
                    }
                    outList.Add(normalized);
                }
            }

            if (item.TryGetValue("unique_ops_text", out var uniqueOps) && uniqueOps is List<string> opList)
            {
                foreach (string line in opList)
                {
                    string normalized = StripColorTags(line);
                    if (!normalized.StartsWith("- "))
                    {
                        normalized = "- " + normalized;
                    }
                    outList.Add(normalized);
                }
            }

            outList.Add("<要求能力値>");

            int? requiredLevel = GetInt(item, "RequiredLevel");
            if (requiredLevel.HasValue && requiredLevel.Value != 0)
            {
                outList.Add($"- レベル {requiredLevel.Value}");
            }

            var reqs = GetList<int>(item, "RequiredStatus");
            if (reqs != null)
            {
                for (int i = 0; i < Math.Min(reqs.Count, ReqLabels.Length); i++)
                {
                    if (reqs[i] != 0)
                    {
                        outList.Add($"- {ReqLabels[i]} {reqs[i]}");
                    }
                }
            }

            outList.Add("<着用可能な職業>");
            string reqStr = GetString(item, "requirements") ?? "";
            if (!string.IsNullOrWhiteSpace(reqStr))
            {
                string[] parts = reqStr.Split(',', StringSplitOptions.RemoveEmptyEntries);
                foreach (string part in parts)
                {
                    string trimmed = part.Trim();
                    if (!string.IsNullOrEmpty(trimmed))
                    {
                        outList.Add("- " + trimmed);
                    }
                }
            }

            outList.Add("<DropLv/係数>");

            int? dropLevel = GetInt(item, "DropLevel");
            outList.Add($"- ドロップレベル {(dropLevel.HasValue ? dropLevel.Value.ToString() : "")}");

            int? dropCoefficient = GetInt(item, "DropCoefficient");
            if (!dropCoefficient.HasValue)
            {
                dropCoefficient = 1000;
            }
            outList.Add($"- ドロップ係数 {dropCoefficient.Value}");

            int? stack = GetInt(item, "StackableNum");
            if (stack.HasValue)
            {
                outList.Add($"- スタック数 {stack.Value}");
            }

            return outList;
        }

        private static string StripColorTags(string s)
        {
            return System.Text.RegularExpressions.Regex.Replace(s ?? "", @"<c:\w+>|<n>", "");
        }

        private static string? GetString(Dictionary<string, object> d, string k)
        {
            return d.TryGetValue(k, out var v) ? v?.ToString() : null;
        }

        private static int? GetInt(Dictionary<string, object> d, string k)
        {
            if (!d.TryGetValue(k, out var v))
            {
                return null;
            }

            if (v is int i)
            {
                return i;
            }
            if (v is long l)
            {
                return (int)l;
            }
            if (v is JsonElement je && je.TryGetInt32(out int ji))
            {
                return ji;
            }
            if (int.TryParse(v?.ToString(), out int parsed))
            {
                return parsed;
            }
            return null;
        }

        private static List<T>? GetList<T>(Dictionary<string, object> d, string k)
        {
            if (!d.TryGetValue(k, out var v))
            {
                return null;
            }

            if (v is List<T> typed)
            {
                return typed;
            }
            if (v is List<object> objectList)
            {
                var result = new List<T>(objectList.Count);
                foreach (var entry in objectList)
                {
                    result.Add((T)Convert.ChangeType(entry, typeof(T)));
                }
                return result;
            }
            if (v is JsonElement je && je.ValueKind == JsonValueKind.Array)
            {
                var result = new List<T>();
                foreach (var element in je.EnumerateArray())
                {
                    if (typeof(T) == typeof(int) && element.TryGetInt32(out int iv))
                    {
                        result.Add((T)(object)iv);
                    }
                }
                return result;
            }

            return null;
        }
    }
}