using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ItemDataConverter.Core
{
    /// <summary>
    /// オプションブロックを HTML 用の表示辞書へ変換する。
    /// </summary>
    public static class OpsRenderer
    {
        private static readonly Regex PlaceholderPattern = new Regex(@"\[([+-]?)(\d+)\]", RegexOptions.Compiled);

        public static Dictionary<string, Dictionary<string, object>> RenderAll(
            List<OpsExtractor.OpRecord> ops,
            Dictionary<string, Dictionary<string, string>> textdata)
        {
            var result = new Dictionary<string, Dictionary<string, object>>();
            var templates = textdata.TryGetValue("section21", out var section21)
                ? section21
                : new Dictionary<string, string>();

            foreach (var op in ops)
            {
                templates.TryGetValue(op.Effect.ToString(), out string? template);

                result[op.OpId.ToString()] = new Dictionary<string, object>
                {
                    ["名前"] = StripBrackets(op.Name1),
                    ["効果"] = ReplacePlaceholders(template ?? "", op),
                    ["要求Lv上昇"] = op.RequireLevel,
                    ["付加係数"] = op.DropCoefficient,
                };
            }

            return result;
        }

        public static string ToJson(
            List<OpsExtractor.OpRecord> ops,
            Dictionary<string, Dictionary<string, string>> textdata,
            bool indented = true)
        {
            var rendered = RenderAll(ops, textdata);
            var options = new JsonSerializerOptions
            {
                WriteIndented = indented,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            return JsonSerializer.Serialize(rendered, options);
        }

        private static string StripBrackets(string text)
        {
            text = text.Trim();
            return text.StartsWith("[") && text.EndsWith("]") ? text[1..^1] : text;
        }

        private static string ReplacePlaceholders(string template, OpsExtractor.OpRecord op)
        {
            if (string.IsNullOrEmpty(template))
            {
                return "";
            }

            string result = template;
            foreach (Match match in PlaceholderPattern.Matches(template))
            {
                string sign = match.Groups[1].Value;
                int index = int.Parse(match.Groups[2].Value);

                int minValue = index == 0 ? op.OpValue1Min : op.OpValue2Min;
                int maxValue = index == 0 ? op.OpValue1Max : op.OpValue2Max;

                string replacement;
                if (minValue == maxValue || minValue == 0)
                {
                    replacement = FormatValue(maxValue, sign);
                }
                else
                {
                    replacement = $"{FormatValue(minValue, sign)}~{FormatValue(maxValue, "")}";
                }

                result = result.Replace(match.Value, replacement);
            }

            return result.Replace("[", "").Replace("]", "").Trim();
        }

        private static string FormatValue(int value, string sign)
        {
            if (sign == "+")
            {
                return value >= 0 ? $"+{value}" : value.ToString();
            }

            if (sign == "-")
            {
                return $"-{value}";
            }

            return value.ToString();
        }
    }
}