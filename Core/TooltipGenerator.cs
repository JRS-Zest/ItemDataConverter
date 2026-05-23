using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace ItemDataConverter.Core
{
    /// <summary>
    /// アイテムのツールチップ情報を生成する
    /// rsp_tooltip_generator.py からの完全忠実な移植
    /// </summary>
    public static class TooltipGenerator
    {
        /// <summary>
        /// テンプレートのプレースホルダを置換 (Pythonのreplace_placeholders)
        /// [0], [+0], [-0] などのパターンを values[idx] で置換
        /// </summary>
        private static string ReplacePlaceholders(string template, List<object> values)
        {
            if (string.IsNullOrEmpty(template)) return "";

            var pat = new Regex(@"\[([+-]?)(\d+)\]");

            string result = pat.Replace(template, m =>
            {
                string sign = m.Groups[1].Value;
                int idx = int.Parse(m.Groups[2].Value);

                object v;
                try { v = values[idx]; }
                catch { return ""; }

                // sentinel 65535 means skip
                if (v is int vi && vi == 65535) return "";
                if (v is long vl && vl == 65535) return "";

                string s = v?.ToString() ?? "";

                if (sign == "+")
                {
                    if (v is int || v is long || v is double || v is float)
                    {
                        try
                        {
                            double d = Convert.ToDouble(v);
                            if (d >= 0) return $"+{s}";
                        }
                        catch { return $"+{s}"; }
                    }
                    return s;
                }
                if (sign == "-")
                {
                    if (v is int || v is long || v is double || v is float)
                    {
                        return $"-{s}";
                    }
                    return s;
                }
                return s;
            });

            return result.Trim();
        }

        /// <summary>
        /// 有効な値かチェック（0やnullや65535でない）
        /// </summary>
        private static bool IsValidValue(object? v)
        {
            if (v == null) return false;
            try
            {
                int vi = Convert.ToInt32(v);
                return vi != 0 && vi != 65535;
            }
            catch { return true; }
        }

        /// <summary>
        /// 数値を文字列に変換
        /// </summary>
        private static string NumToStr(object? x)
        {
            if (x == null) return "";
            try { return Convert.ToInt32(x).ToString(); }
            catch
            {
                try { return Convert.ToDouble(x).ToString(); }
                catch { return x.ToString() ?? ""; }
            }
        }

        /// <summary>
        /// ペア値を評価してレンジまたは単一値を返す
        /// </summary>
        private static (object, object) PairValues(object? x, object? y)
        {
            if (x == null && y == null) return (0, 0);
            if (x != null && y != null)
            {
                try
                {
                    int xi = Convert.ToInt32(x);
                    int yi = Convert.ToInt32(y);
                    if (xi != yi)
                    {
                        string s = $"{Math.Min(xi, yi)}~{Math.Max(xi, yi)}";
                        return (s, s);
                    }
                }
                catch { }
                return (x, y);
            }
            object v = x ?? y!;
            return (v, v);
        }

        /// <summary>
        /// Pythonの_format_rangeと完全に同一の処理
        /// 0または65535をinvalidとして扱い、レンジ文字列を生成
        /// </summary>
        private static string FormatRange(object? x, object? y)
        {
            bool xValid = IsFormatRangeValid(x);
            bool yValid = IsFormatRangeValid(y);

            if (!xValid && !yValid)
                return "";

            int? xi = null;
            int? yi = null;

            if (xValid)
            {
                try { xi = Convert.ToInt32(x); }
                catch { }
            }
            if (yValid)
            {
                try { yi = Convert.ToInt32(y); }
                catch { }
            }

            if (xi.HasValue && yi.HasValue)
            {
                if (xi.Value == yi.Value)
                    return $"{xi.Value}";
                int mn = Math.Min(xi.Value, yi.Value);
                int mx = Math.Max(xi.Value, yi.Value);
                return $"{mn}~{mx}";
            }

            // only one present
            return (xi ?? yi)?.ToString() ?? "";
        }

        /// <summary>
        /// FormatRange用のvalid判定（Pythonの_format_range内の_validと同一）
        /// </summary>
        private static bool IsFormatRangeValid(object? v)
        {
            if (v == null) return false;
            try
            {
                int vi = Convert.ToInt32(v);
                if (vi == 0 || vi == 65535) return false;
                return true;
            }
            catch
            {
                // 変換できなければtrue（Pythonと同一）
                return true;
            }
        }

        /// <summary>
        /// Pythonのprocess関数と同一の処理
        /// </summary>
        public static Dictionary<string, Dictionary<string, object>> AddTooltipAll(
            Dictionary<string, Dictionary<string, object>> itemData,
            Dictionary<string, Dictionary<string, string>> textdata)
        {
            // textdata groups (mapped to section keys)
            var g374 = GetTextdataGroup(textdata, "section20");
            var g187 = GetTextdataGroup(textdata, "section21");

            var output = new Dictionary<string, Dictionary<string, object>>();

            foreach (var kvp in itemData)
            {
                string idStr = kvp.Key;
                var item = kvp.Value;

                Dictionary<string, object> fields;
                if (item.TryGetValue("fields", out var fieldsObj) && fieldsObj is Dictionary<string, object> f)
                {
                    fields = f;
                }
                else
                {
                    fields = new Dictionary<string, object>();
                }

                var entry = new Dictionary<string, object>();

                // Index, Name, Type
                if (fields.ContainsKey("Index")) entry["Index"] = fields["Index"];
                if (fields.ContainsKey("Name")) entry["Name"] = fields["Name"];
                if (fields.ContainsKey("Type")) entry["Type"] = fields["Type"];

                // 攻撃力・射程の計算
                bool lowPresent = IsValidValue(fields.GetValueOrDefault("LowAP"));
                bool highPresent = IsValidValue(fields.GetValueOrDefault("HighAP"));
                bool speedPresent = IsValidValue(fields.GetValueOrDefault("AttackSpeed_raw"));
                bool rangePresent = IsValidValue(fields.GetValueOrDefault("AttackRange"));

                if (lowPresent || highPresent || speedPresent || rangePresent)
                {
                    var low = fields.GetValueOrDefault("LowAP");
                    var high = fields.GetValueOrDefault("HighAP");

                    var attackParts = new List<string>();
                    if (lowPresent || highPresent)
                    {
                        if (lowPresent && highPresent)
                        {
                            try
                            {
                                int li = Convert.ToInt32(low);
                                int hi = Convert.ToInt32(high);
                                if (li == hi)
                                    attackParts.Add($"{li}");
                                else
                                {
                                    int mn = Math.Min(li, hi);
                                    int mx = Math.Max(li, hi);
                                    attackParts.Add($"{mn}~{mx}");
                                }
                            }
                            catch { attackParts.Add($"{NumToStr(low)}~{NumToStr(high)}"); }
                        }
                        else
                        {
                            object? v = lowPresent ? low : high;
                            attackParts.Add(NumToStr(v));
                        }
                    }

                    double? speedSec = null;
                    if (speedPresent)
                    {
                        try
                        {
                            speedSec = Convert.ToDouble(fields["AttackSpeed_raw"]) / 100.0;
                        }
                        catch { }
                    }

                    if (attackParts.Count > 0)
                    {
                        if (speedSec.HasValue)
                            entry["攻撃力"] = $"{attackParts[0]} ({speedSec.Value:F2} 秒)";
                        else
                            entry["攻撃力"] = attackParts[0];
                    }
                    else if (speedSec.HasValue)
                    {
                        entry["攻撃力"] = $"({speedSec.Value:F2} 秒)";
                    }

                    if (rangePresent)
                    {
                        try { entry["射程"] = Convert.ToInt32(fields["AttackRange"]); }
                        catch { entry["射程"] = fields["AttackRange"]; }
                    }
                }

                // パススルーキー
                foreach (var key in new[] { "RequiredLevel", "RequiredStatus", "StackableNum", "DropLevel" })
                {
                    if (fields.ContainsKey(key)) entry[key] = fields[key];
                }

                // DropCoefficient は fields または extended_data から取得 (render_rsp_items.py と同様)
                if (fields.ContainsKey("DropCoefficient"))
                {
                    entry["DropCoefficient"] = fields["DropCoefficient"];
                }
                else if (item.TryGetValue("extended_data", out var extDataObj) && extDataObj is Dictionary<string, object> extData)
                {
                    if (extData.ContainsKey("DropCoefficient"))
                    {
                        entry["DropCoefficient"] = extData["DropCoefficient"];
                    }
                }

                // requirements保持
                if (item.ContainsKey("requirements"))
                    entry["requirements"] = item["requirements"];
                else if (fields.ContainsKey("requirements"))
                    entry["requirements"] = fields["requirements"];

                // unique_effects → group 374
                var ueTexts = new List<string>();
                if (item.TryGetValue("unique_effects", out var uefsObj) && uefsObj is List<Dictionary<string, object>> uefs)
                {
                    for (int idx = 0; idx < uefs.Count; idx++)
                    {
                        var ue = uefs[idx];
                        var effObj = ue.GetValueOrDefault("EffectID");
                        if (effObj == null) continue;
                        int eff;
                        try { eff = Convert.ToInt32(effObj); }
                        catch { continue; }
                        if (eff == 65535) continue;

                        string? tmpl = g374.GetValueOrDefault(eff.ToString());
                        if (string.IsNullOrEmpty(tmpl)) continue;

                        // Need0/1, Need2/3 または LowValue/HighValue/LowValue2/HighValue2
                        object? a0 = ue.GetValueOrDefault("Need0") ?? ue.GetValueOrDefault("LowValue");
                        object? a1 = ue.GetValueOrDefault("Need1") ?? ue.GetValueOrDefault("HighValue");
                        object? b0 = ue.GetValueOrDefault("Need2") ?? ue.GetValueOrDefault("LowValue2");
                        object? b1 = ue.GetValueOrDefault("Need3") ?? ue.GetValueOrDefault("HighValue2");

                        var (n0, n1) = PairValues(a0, a1);
                        var (n2, n3) = PairValues(b0, b1);

                        // Pythonの_format_rangeと同一の処理
                        string range1 = FormatRange(a0, a1);
                        string range2 = FormatRange(b0, b1);

                        // 特殊ケース: 第1ペアが無く、第2ペアのみ存在する場合
                        if (string.IsNullOrEmpty(range1) && !string.IsNullOrEmpty(range2))
                        {
                            range1 = range2;
                        }

                        // tpl_values layout: [第1ペア範囲またはLow, 第1ペアHigh (n1), 第2ペア範囲, 第2ペアHigh(n3)]
                        var tplValues = new List<object> { range1, n1, range2, n3 };
                        string finalText = ReplacePlaceholders(tmpl, tplValues);

                        // 特殊例2のフォールバック処理：テンプレにプレースホルダがあり、置換後に
                        // 数字が含まれない（＝データ無し）場合について処理する
                        if (Regex.IsMatch(tmpl, @"\[[+-]?\d+\]") && !Regex.IsMatch(finalText ?? "", @"\d"))
                        {
                            // まず、第1エフェクトの "1番目の値" をフォールバック候補として取得する
                            string? fallback = null;
                            try
                            {
                                var firstUe = uefs[0];
                                object? cand = firstUe.GetValueOrDefault("Need0") ?? firstUe.GetValueOrDefault("LowValue");
                                if (cand != null)
                                {
                                    try
                                    {
                                        int ci = Convert.ToInt32(cand);
                                        if (ci != 0 && ci != 65535)
                                            fallback = ci.ToString();
                                    }
                                    catch
                                    {
                                        fallback = cand.ToString();
                                    }
                                }
                            }
                            catch { }

                            // フォールバックがある場合、tpl_values の空スロットを埋めて再置換する
                            if (fallback != null && idx > 0)
                            {
                                var newVals = new List<object>();
                                foreach (var v in tplValues)
                                {
                                    if (v == null || (v is string vs && string.IsNullOrWhiteSpace(vs)))
                                        newVals.Add(fallback);
                                    else
                                        newVals.Add(v);
                                }
                                tplValues = newVals;
                                finalText = ReplacePlaceholders(tmpl, tplValues);
                            }

                            // それでもデータがなければ明示的マーカー 'nullpo' を入れて再置換
                            if (!Regex.IsMatch(finalText ?? "", @"\d"))
                            {
                                var newVals = new List<object>();
                                foreach (var v in tplValues)
                                {
                                    if (v == null || (v is string vs && string.IsNullOrWhiteSpace(vs)))
                                        newVals.Add("nullpo");
                                    else
                                        newVals.Add(v);
                                }
                                tplValues = newVals;
                                finalText = ReplacePlaceholders(tmpl, tplValues);
                            }
                        }

                        // テンプレートにプレースホルダが無ければ、データがあるか確認して末尾に付加する
                        if (!Regex.IsMatch(tmpl, @"\[[+-]?\d+\]"))
                        {
                            string? suffix = MakeSuffixFromPair(n0, n1) ?? MakeSuffixFromPair(n2, n3);
                            if (!string.IsNullOrEmpty(suffix))
                            {
                                finalText = $"{finalText} {suffix}";
                            }
                        }

                        ueTexts.Add(finalText);
                    }
                }
                if (ueTexts.Count > 0) entry["unique_effects_text"] = ueTexts;

                // unique_ops → group 187
                var uopTexts = new List<string>();
                if (item.TryGetValue("unique_ops", out var uopsObj) && uopsObj is List<Dictionary<string, object>> uops)
                {
                    foreach (var uo in uops)
                    {
                        var effObj = uo.GetValueOrDefault("EffectID");
                        if (effObj == null) continue;
                        int eff;
                        try { eff = Convert.ToInt32(effObj); }
                        catch { continue; }
                        if (eff == 65535) continue;

                        string? tmpl = g187.GetValueOrDefault(eff.ToString());
                        if (string.IsNullOrEmpty(tmpl)) continue;

                        var valsObj = uo.GetValueOrDefault("Values");
                        var vals = new List<object>();
                        if (valsObj is List<int> intList)
                        {
                            foreach (var v in intList) vals.Add(v);
                        }
                        else if (valsObj is List<object> objList)
                        {
                            vals = objList;
                        }

                        uopTexts.Add(ReplacePlaceholders(tmpl, vals));
                    }
                }
                if (uopTexts.Count > 0) entry["unique_ops_text"] = uopTexts;

                output[idStr] = entry;
            }

            return output;
        }

        /// <summary>
        /// ペアから suffix を作成
        /// </summary>
        private static string? MakeSuffixFromPair(object? x, object? y)
        {
            bool xValid = IsValidNum(x);
            bool yValid = IsValidNum(y);

            if (!xValid && !yValid) return null;

            try
            {
                int xi = Convert.ToInt32(x);
                int yi = Convert.ToInt32(y);
                if (xi == yi) return $"[{xi}]";
                int mn = Math.Min(xi, yi);
                int mx = Math.Max(xi, yi);
                return $"[{mn}~{mx}]";
            }
            catch
            {
                string? sx = x?.ToString();
                if (!string.IsNullOrEmpty(sx) && sx.Contains('~') && sx.Any(char.IsDigit))
                {
                    return $"[{sx}]";
                }
            }
            return null;
        }

        private static bool IsValidNum(object? v)
        {
            if (v == null) return false;
            if (v is int vi) return vi != 0 && vi != 65535;
            if (v is long vl) return vl != 0 && vl != 65535;
            if (v is string vs && int.TryParse(vs, out int vsi)) return vsi != 0 && vsi != 65535;
            return false;
        }

        /// <summary>
        /// textdata からグループを取得
        /// </summary>
        private static Dictionary<string, string> GetTextdataGroup(Dictionary<string, Dictionary<string, string>> textdata, string key)
        {
            if (textdata.TryGetValue(key, out var grp)) return grp;
            return new Dictionary<string, string>();
        }
    }
}
