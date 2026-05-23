using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace ItemDataConverter.Core
{
    /// <summary>
    /// アイテムデータを最終的な形式にレンダリングする
    /// render_rsp_items.py からの完全忠実な移植
    /// </summary>
    public static class ItemRenderer
    {
        // 型別マップ (TYPE_MAP)
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

        // HTMLテンプレート（/* ここに貼り付け */ をJSONデータで置換）
        private const string HtmlTemplate = @"<!DOCTYPE html>
<html lang=""ja"">
<head>
    <meta charset=""UTF-8"">
    <title>アイテムDB - 複合フィルタ</title>
    <style>
        body { font-family: sans-serif; background-color: #f0f2f5; margin: 20px; color: #333; }
        .controls { background: white; padding: 15px; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); margin-bottom: 20px; position: sticky; top: 10px; z-index: 100; }
        .filter-group { display: flex; gap: 10px; flex-wrap: wrap; align-items: flex-end; }
        .filter-item { display: flex; flex-direction: column; gap: 5px; }
        label { font-size: 11px; font-weight: bold; color: #666; }
        input, select, button { padding: 8px; border: 1px solid #ddd; border-radius: 4px; }
        button { cursor: pointer; background: #eee; }
        button.active { background: #4A90E2; color: white; border-color: #4A90E2; }

        /* 一覧表示 */
        table { width: 100%; border-collapse: collapse; background: white; table-layout: fixed; margin-top: 10px; }
        th { background: #4A90E2; color: white; padding: 10px; text-align: left; cursor: pointer; }
        td { padding: 8px; border-bottom: 1px solid #eee; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
        tr.item-row:hover { background: #f1f7ff; cursor: pointer; }
        .detail-row { display: none; background: #fafafa; }
        .detail-row td { white-space: normal; padding: 15px; border-bottom: 2px solid #4A90E2; }

        /* カード表示 */
        #cardView { display: none; grid-template-columns: repeat(auto-fill, minmax(280px, 1fr)); gap: 15px; margin-top: 10px; }
        .item-card { background: white; padding: 15px; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); border-top: 4px solid #4A90E2; }
        .card-title { font-weight: bold; font-size: 15px; margin-bottom: 8px; border-bottom: 1px solid #eee; padding-bottom: 5px; color: #4A90E2; }
        .card-body { font-size: 12px; line-height: 1.25; max-height: 400px; overflow-y: auto; }

        .pagination { margin-top: 20px; display: flex; justify-content: center; gap: 5px; }
        .pagination button { padding: 5px 12px; }
    </style>
</head>
<body>

<div class=""controls"">
    <div class=""filter-group"">
        <div class=""filter-item"">
            <label>キーワード</label>
            <input type=""text"" id=""qFull"" placeholder=""文字検索"" onkeyup=""runFilter()"">
        </div>
        <div class=""filter-item"">
            <label>種別</label>
            <select id=""qType"" style=""width:120px"" onchange=""runFilter()"">
                <option value="""">(全て)</option>
            </select>
        </div>
        <div class=""filter-item"">
            <label>要求Lv</label>
            <input type=""number"" id=""qReq"" style=""width:70px"" onkeyup=""runFilter()"" onchange=""runFilter()"">
        </div>
        <div class=""filter-item"">
            <label>DropLv</label>
            <input type=""number"" id=""qDrop"" style=""width:70px"" onkeyup=""runFilter()"" onchange=""runFilter()"">
        </div>
        <div class=""filter-item"">
            <label>表示数</label>
            <select id=""pageSize"" onchange=""runFilter()"">
                <option value=""20"">20</option>
                <option value=""50"">50</option>
                <option value=""100"">100</option>
            </select>
        </div>
        <div class=""filter-item"">
            <label>形式</label>
            <div>
                <button id=""btnList"" class=""active"" onclick=""setView('list')"">リスト</button>
                <button id=""btnCard"" onclick=""setView('card')"">カード</button>
            </div>
        </div>
        <div class=""filter-item"" style=""margin-left:8px"">
            <label>クイック絞り込み</label>
            <div style=""border:1px solid #ddd; padding:8px; border-radius:6px; background:#fff; display:flex; gap:6px; flex-wrap:wrap;"">
                <button id=""fBFU"" onclick=""setQuickFilter('BFU')"">BFU</button>
                <button id=""f800"" onclick=""setQuickFilter('800')"">800</button>
                <button id=""f900"" onclick=""setQuickFilter('900')"">900</button>
                <button id=""f1000"" onclick=""setQuickFilter('1000')"">1000</button>
                <button id=""f2100"" onclick=""setQuickFilter('2100')"">2100</button>
            </div>
        </div>
        <div class=""filter-item"" style=""margin-left:auto"">
            <label>&nbsp;</label>
            <button id=""btnCopyNames"" onclick=""copyNames()"" style=""background:#5cb85c; color:white; border-color:#5cb85c;"">名称のみコピー</button>
        </div>
    </div>
</div>

<div id=""listView"">
    <table>
        <thead>
            <tr>
                <th style=""width:60px"" onclick=""doSort('id')"">ID</th>
                <th onclick=""doSort('name')"">名称</th>
                <th style=""width:100px"" onclick=""doSort('type')"">種別</th>
                <th style=""width:80px"" onclick=""doSort('reqLv')"">要求Lv</th>
                <th style=""width:80px"" onclick=""doSort('dropLv')"">DropLv</th>
            </tr>
        </thead>
        <tbody id=""tableBody""></tbody>
    </table>
</div>

<div id=""cardView""></div>
<div id=""pagination"" class=""pagination""></div>

<script>

    const rawData = /* ここに貼り付け */;

    let allItems = [], filteredItems = [], currentPage = 1, currentView = 'list', currentSort = { key: 'id', asc: true };
    let activeQuickFilter = '';

    // カラータグ除去関数
    function stripColorTags(str) {
        return str.replace(/<c:\w+>|<n>/g, '');
    }

    function initialize() {
        if(!rawData) return;
        allItems = Object.keys(rawData).map(id => {
            const list = rawData[id].map(s => stripColorTags(s));
            let reqLv = 0, dropLv = 0;
            const rIdx = list.indexOf(""<要求能力値>"");
            if(rIdx !== -1 && list[rIdx+1]?.includes(""- レベル "")) reqLv = parseInt(list[rIdx+1].split(""レベル "")[1]) || 0;
            const dIdx = list.indexOf(""<DropLv/係数>"");
            if(dIdx !== -1 && list[dIdx+1]?.includes(""- ドロップレベル "")) dropLv = parseInt(list[dIdx+1].split(""レベル "")[1]) || 0;
            
            return { id: parseInt(id), name: list[0], type: (list[2] || """").replace(""- "", """"), reqLv, dropLv, fullList: list, searchIdx: list.join("" "").toLowerCase() };
        });
        // populate qType select with unique types found in data
        try {
            const sel = document.getElementById('qType');
            if (sel) {
                const types = Array.from(new Set(allItems.map(i => (i.type||'').trim()).filter(s => s)));
                types.sort((a,b) => a.localeCompare(b));
                sel.innerHTML = '<option value="""">(全て)</option>' + types.map(t => `<option value=""${t}"">${t}</option>`).join('');
                sel.addEventListener('change', runFilter);
            }
        } catch (e) {
            console.warn('qType select populate failed', e);
        }

        runFilter();
    }

    function runFilter() {
        const qFull = document.getElementById('qFull').value.toLowerCase();
        const qType = document.getElementById('qType').value.toLowerCase();
        const qReq = document.getElementById('qReq').value;
        const qDrop = document.getElementById('qDrop').value;

        const quick = activeQuickFilter;

        filteredItems = allItems.filter(item => {
            if (qFull && !item.searchIdx.includes(qFull)) return false;
            if (qType && !item.type.toLowerCase().includes(qType)) return false;
            if (qReq && item.reqLv != qReq) return false;
            if (qDrop && item.dropLv != qDrop) return false;

            // quick filters
            if (quick) {
                const name = item.name || '';
                const t = item.type || '';
                if (quick === 'BFU') {
                    if (!item.searchIdx.includes('★')) return false;
                } else if (quick === '800') {
                    if (item.reqLv != 800) return false;
                    if ((name||'').includes('★')) return false;
                } else if (quick === '900') {
                    if (item.reqLv != 900) return false;
                    if ((name||'').toUpperCase().includes('IF')) return false;
                } else if (quick === '1000') {
                    if (item.reqLv != 1000) return false;
                    if ((t||'').includes('お菓子')) return false;
                } else if (quick === '2100') {
                    if (item.reqLv != 2100) return false;
                }
            }

            return true;
        });
        currentPage = 1;
        render();
    }

    function clearQuickButtons() {
        ['fBFU','f800','f900','f1000','f2100'].forEach(id => {
            const el = document.getElementById(id);
            if (el) el.classList.remove('active');
        });
    }

    function setQuickFilter(mode) {
        // toggle
        if (activeQuickFilter === mode) {
            activeQuickFilter = '';
            clearQuickButtons();
        } else {
            activeQuickFilter = mode;
            clearQuickButtons();
            const btn = {
                'BFU':'fBFU','800':'f800','900':'f900','1000':'f1000','2100':'f2100'
            }[mode];
            const el = document.getElementById(btn);
            if (el) el.classList.add('active');
        }
        runFilter();
    }

    function setView(mode) {
        currentView = mode;
        document.getElementById('btnList').className = mode === 'list' ? 'active' : '';
        document.getElementById('btnCard').className = mode === 'card' ? 'active' : '';
        document.getElementById('listView').style.display = mode === 'list' ? 'block' : 'none';
        document.getElementById('cardView').style.display = mode === 'card' ? 'grid' : 'none';
        render();
    }

    function render() {
        const size = parseInt(document.getElementById('pageSize').value);
        const pageData = filteredItems.slice((currentPage - 1) * size, currentPage * size);
        
        if (currentView === 'list') {
            document.getElementById('tableBody').innerHTML = pageData.map(item => `
                <tr class=""item-row"" onclick=""toggleDetail(${item.id})"">
                    <td>${item.id}</td><td><strong>${item.name}</strong></td><td>${item.type}</td><td>${item.reqLv||""-""}</td><td>${item.dropLv||""-""}</td>
                </tr>
                <tr class=""detail-row"" id=""detail-${item.id}""><td colspan=""5""><div style=""line-height:1.5"">${item.fullList.join('<br>')}</div></td></tr>
            `).join('');
        } else {
            document.getElementById('cardView').innerHTML = pageData.map(item => `
                <div class=""item-card"">
                    <div class=""card-title"">${item.id}: ${item.name}</div>
                    <div class=""card-body"">${item.fullList.join('<br>')}</div>
                </div>
            `).join('');
        }
        renderPager(size);
    }

    function renderPager(size) {
        const total = Math.ceil(filteredItems.length / size);
        let html = """";
        for (let i = Math.max(1, currentPage-2); i <= Math.min(total, currentPage+2); i++) {
            html += `<button class=""${i===currentPage?'active':''}"" onclick=""currentPage=${i};render();window.scrollTo(0,0);"">${i}</button>`;
        }
        document.getElementById('pagination').innerHTML = html;
    }

    function toggleDetail(id) {
        const el = document.getElementById('detail-' + id);
        el.style.display = (el.style.display === 'table-row') ? 'none' : 'table-row';
    }

    function doSort(key) {
        currentSort.asc = (currentSort.key === key) ? !currentSort.asc : true;
        currentSort.key = key;
        filteredItems.sort((a, b) => {
            let vA = a[key], vB = b[key];
            if (typeof vA === 'string') return currentSort.asc ? vA.localeCompare(vB) : vB.localeCompare(vA);
            return currentSort.asc ? vA - vB : vB - vA;
        });
        render();
    }

    function copyNames() {
        const names = filteredItems.map(item => item.name).join('\n');
        navigator.clipboard.writeText(names).then(() => {
            const btn = document.getElementById('btnCopyNames');
            const original = btn.textContent;
            btn.textContent = 'コピー完了!';
            setTimeout(() => { btn.textContent = original; }, 1500);
        }).catch(err => {
            alert('コピーに失敗しました');
        });
    }

    window.onload = initialize;
</script>
</body>
</html>";

        /// <summary>
        /// 単一アイテムをテキスト行リストに変換 (Python make_entry)
        /// </summary>
        public static List<string> MakeEntry(Dictionary<string, object> item, Dictionary<string, Dictionary<string, string>> textdata)
        {
            var outList = new List<string>();

            // Name (カラータグを除去)
            string name = StripColorTags(GetString(item, "Name") ?? "");
            outList.Add(name);
            outList.Add("<基本情報>");

            // Type
            int? typeVal = GetInt(item, "Type");
            string typeStr = "";
            if (typeVal.HasValue && TypeMap.TryGetValue(typeVal.Value, out var tm))
                typeStr = tm;
            else if (typeVal.HasValue)
                typeStr = typeVal.Value.ToString();
            outList.Add($"- {typeStr}");

            // 攻撃力
            var atkStr = GetString(item, "攻撃力");
            if (!string.IsNullOrEmpty(atkStr))
            {
                outList.Add($"- 攻撃力 {atkStr}");
            }

            // 射程
            var rangeVal = item.GetValueOrDefault("射程");
            if (rangeVal != null)
            {
                outList.Add($"- 射程 {rangeVal}");
            }

            // unique_effects_text
            if (item.TryGetValue("unique_effects_text", out var uefsObj))
            {
                if (uefsObj is List<string> uefsList)
                {
                    foreach (var ue in uefsList)
                    {
                        string s = StripColorTags(ue);
                        if (!s.StartsWith("- ")) s = $"- {s}";
                        outList.Add(s);
                    }
                }
            }

            // unique_ops_text
            if (item.TryGetValue("unique_ops_text", out var uopsObj))
            {
                if (uopsObj is List<string> uopsList)
                {
                    foreach (var uo in uopsList)
                    {
                        string s = StripColorTags(uo);
                        if (!s.StartsWith("- ")) s = $"- {s}";
                        outList.Add(s);
                    }
                }
            }

            outList.Add("<要求能力値>");

            // RequiredLevel
            int? lvlVal = GetInt(item, "RequiredLevel");
            if (lvlVal.HasValue && lvlVal.Value != 0)
            {
                outList.Add($"- レベル {lvlVal.Value}");
            }

            // RequiredStatus
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

            // requirements
            string reqStr = GetString(item, "requirements") ?? "";
            if (!string.IsNullOrWhiteSpace(reqStr))
            {
                var parts = reqStr.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .Where(s => !string.IsNullOrEmpty(s));
                foreach (var p in parts)
                {
                    outList.Add($"- {p}");
                }
            }

            outList.Add("<DropLv/係数>");

            // DropLevel
            int? dlv = GetInt(item, "DropLevel");
            outList.Add($"- ドロップレベル {(dlv.HasValue ? dlv.Value.ToString() : "")}");

            // DropCoefficient - try extended_data if not at top level
            int? dcoef = GetInt(item, "DropCoefficient");
            if (!dcoef.HasValue && item.TryGetValue("extended_data", out var edObj) && edObj is Dictionary<string, object> ed)
            {
                dcoef = GetIntFromDict(ed, "DropCoefficient");
            }
            if (!dcoef.HasValue) dcoef = 1000;
            outList.Add($"- ドロップ係数 {dcoef.Value}");

            // StackableNum
            int? stack = GetInt(item, "StackableNum");
            if (stack.HasValue)
            {
                outList.Add($"- スタック数 {stack.Value}");
            }

            // リテラル文字列 \r\n で分割して別々の行にする
            var result = new List<string>();
            foreach (var line in outList)
            {
                if (line.Contains("\r\n"))
                {
                    var parts = line.Split(new[] { "\r\n" }, StringSplitOptions.None);
                    foreach (var part in parts)
                    {
                        if (!string.IsNullOrWhiteSpace(part))
                            result.Add(part.TrimStart());
                    }
                }
                else
                {
                    result.Add(line);
                }
            }
            return result;
        }

        /// <summary>
        /// 全アイテムをテキストリストに変換
        /// </summary>
        public static Dictionary<string, List<string>> RenderAll(
            Dictionary<string, Dictionary<string, object>> items,
            Dictionary<string, Dictionary<string, string>> textdata)
        {
            var rendered = new Dictionary<string, List<string>>();

            // sort by key (as int), exclude negative IDs
            var sortedKeys = items.Keys
                .Where(k => int.TryParse(k, out int v) && v >= 0)
                .OrderBy(k => int.Parse(k))
                .ToList();

            foreach (var k in sortedKeys)
            {
                rendered[k] = MakeEntry(items[k], textdata);
            }

            return rendered;
        }

        /// <summary>
        /// 全アイテムをJSON形式で出力（Python互換形式）
        /// </summary>
        public static string RenderAllJson(Dictionary<string, Dictionary<string, object>> items, bool indented = true)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = indented,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            return JsonSerializer.Serialize(items, options);
        }

        /// <summary>
        /// 全アイテムをテキストリスト形式JSONで出力
        /// </summary>
        public static string RenderAllJsonAsList(
            Dictionary<string, Dictionary<string, object>> items,
            Dictionary<string, Dictionary<string, string>> textdata,
            bool indented = true)
        {
            var rendered = RenderAll(items, textdata);
            var options = new JsonSerializerOptions
            {
                WriteIndented = indented,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            return JsonSerializer.Serialize(rendered, options);
        }

        /// <summary>
        /// アイテムをファイルに保存（JSON - Python互換形式）
        /// </summary>
        public static void SaveAsJson(
            Dictionary<string, Dictionary<string, object>> items,
            string outputPath,
            bool indented = true)
        {
            string json = RenderAllJson(items, indented);
            File.WriteAllText(outputPath, json, Encoding.UTF8);
        }

        /// <summary>
        /// アイテムをテキストリスト形式でJSONファイルに保存（Python out_rendered.json互換）
        /// </summary>
        public static void SaveAsRenderedJson(
            Dictionary<string, Dictionary<string, object>> items,
            Dictionary<string, Dictionary<string, string>> textdata,
            string outputPath,
            bool indented = true)
        {
            string json = RenderAllJsonAsList(items, textdata, indented);
            File.WriteAllText(outputPath, json, Encoding.UTF8);
        }

        /// <summary>
        /// アイテムをファイルに保存（HTML形式 - テンプレートにJSONを埋め込む）
        /// </summary>
        public static void SaveAsHtml(
            Dictionary<string, Dictionary<string, object>> items,
            Dictionary<string, Dictionary<string, string>> textdata,
            string outputPath,
            string title = "RSP アイテムデータ")
        {
            // テキストリスト形式のJSONを生成
            string jsonData = RenderAllJsonAsList(items, textdata, false);

            // テンプレート内の /* ここに貼り付け */ をJSONデータで置換
            string html = HtmlTemplate.Replace("/* ここに貼り付け */", jsonData);

            File.WriteAllText(outputPath, html, Encoding.UTF8);
        }

        /// <summary>
        /// 単一アイテムをHTML形式でレンダリング
        /// </summary>
        private static string RenderItemHtml(string itemId, Dictionary<string, object> item)
        {
            var sb = new StringBuilder();

            string name = GetString(item, "Name") ?? itemId;
            int? typeVal = GetInt(item, "Type");
            string typeStr = typeVal.HasValue && TypeMap.TryGetValue(typeVal.Value, out var tm) ? tm : "不明";

            sb.AppendLine($"<div class=\"item-card\" data-itemid=\"{itemId}\">");
            sb.AppendLine($"  <div class=\"item-header\">");
            sb.AppendLine($"    <span class=\"item-name\">{EscapeHtml(name)}</span>");
            sb.AppendLine($"    <span class=\"item-id\">ID: {itemId}</span>");
            sb.AppendLine($"  </div>");
            sb.AppendLine($"  <div class=\"item-meta\">");
            sb.AppendLine($"    <span class=\"item-type\">{EscapeHtml(typeStr)}</span>");

            int? lvl = GetInt(item, "RequiredLevel");
            if (lvl.HasValue && lvl.Value != 0)
            {
                sb.AppendLine($"    <span class=\"item-level\">Lv.{lvl.Value}</span>");
            }

            sb.AppendLine($"  </div>");

            // 攻撃力
            var atkStr = GetString(item, "攻撃力");
            if (!string.IsNullOrEmpty(atkStr))
            {
                sb.AppendLine($"  <div class=\"stat-line\">攻撃力: {EscapeHtml(atkStr)}</div>");
            }

            // 射程
            var rangeVal = item.GetValueOrDefault("射程");
            if (rangeVal != null)
            {
                sb.AppendLine($"  <div class=\"stat-line\">射程: {rangeVal}</div>");
            }

            // unique_effects_text
            if (item.TryGetValue("unique_effects_text", out var uefsObj) && uefsObj is List<string> uefsList && uefsList.Count > 0)
            {
                sb.AppendLine($"  <div class=\"effects-section\">");
                foreach (var ue in uefsList)
                {
                    sb.AppendLine($"    <div class=\"effect-line\">{EscapeHtml(ue)}</div>");
                }
                sb.AppendLine($"  </div>");
            }

            // unique_ops_text
            if (item.TryGetValue("unique_ops_text", out var uopsObj) && uopsObj is List<string> uopsList && uopsList.Count > 0)
            {
                sb.AppendLine($"  <div class=\"ops-section\">");
                foreach (var uo in uopsList)
                {
                    sb.AppendLine($"    <div class=\"op-line\">{EscapeHtml(uo)}</div>");
                }
                sb.AppendLine($"  </div>");
            }

            // requirements
            string reqStr = GetString(item, "requirements") ?? "";
            if (!string.IsNullOrWhiteSpace(reqStr))
            {
                sb.AppendLine($"  <div class=\"item-jobs\">装備可能: {EscapeHtml(reqStr)}</div>");
            }

            sb.AppendLine($"</div>");
            return sb.ToString();
        }

        /// <summary>
        /// テキストリスト形式のアイテムをHTML形式でレンダリング
        /// </summary>
        private static string RenderItemHtmlFromList(string itemId, List<string> lines)
        {
            var sb = new StringBuilder();

            // 最初の行はアイテム名
            string name = lines.Count > 0 ? lines[0] : itemId;

            sb.AppendLine($"<div class=\"item-card\" data-itemid=\"{itemId}\">");
            sb.AppendLine($"  <div class=\"item-header\">");
            sb.AppendLine($"    <span class=\"item-name\">{EscapeHtml(name)}</span>");
            sb.AppendLine($"    <span class=\"item-id\">ID: {itemId}</span>");
            sb.AppendLine($"  </div>");

            // 残りの行をそのまま表示
            for (int i = 1; i < lines.Count; i++)
            {
                string line = lines[i];
                if (line.StartsWith("<") && line.EndsWith(">"))
                {
                    // セクションヘッダー
                    sb.AppendLine($"  <div class=\"section-header\">{EscapeHtml(line)}</div>");
                }
                else if (line.StartsWith("- "))
                {
                    // 項目
                    sb.AppendLine($"  <div class=\"stat-line\">{EscapeHtml(line.Substring(2))}</div>");
                }
                else
                {
                    sb.AppendLine($"  <div class=\"info-line\">{EscapeHtml(line)}</div>");
                }
            }

            sb.AppendLine($"</div>");
            return sb.ToString();
        }

        // ヘルパーメソッド
        private static string? GetString(Dictionary<string, object> dict, string key)
        {
            if (dict.TryGetValue(key, out var val) && val != null)
                return val.ToString();
            return null;
        }

        /// <summary>
        /// カラータグを除去 (<c:COLOR> と <n> を削除)
        /// </summary>
        private static string StripColorTags(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            // <c:XXX> パターンを除去
            text = System.Text.RegularExpressions.Regex.Replace(text, @"<c:\w+>", "");
            // <n> を除去
            text = text.Replace("<n>", "");
            return text;
        }

        private static int? GetInt(Dictionary<string, object> dict, string key)
        {
            if (dict.TryGetValue(key, out var val) && val != null)
            {
                try { return Convert.ToInt32(val); }
                catch { }
            }
            return null;
        }

        private static int? GetIntFromDict(Dictionary<string, object> dict, string key)
        {
            return GetInt(dict, key);
        }

        private static List<T>? GetList<T>(Dictionary<string, object> dict, string key)
        {
            if (dict.TryGetValue(key, out var val))
            {
                if (val is List<T> list) return list;
                if (val is List<int> intList && typeof(T) == typeof(int))
                    return intList as List<T>;
                if (val is List<object> objList)
                {
                    try
                    {
                        var result = new List<T>();
                        foreach (var o in objList)
                        {
                            result.Add((T)Convert.ChangeType(o, typeof(T)));
                        }
                        return result;
                    }
                    catch { }
                }
            }
            return null;
        }

        private static string EscapeHtml(string text)
        {
            return text
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&#39;");
        }

        private static string GetCssStyles()
        {
            return @"
body {
    font-family: 'Meiryo', 'Yu Gothic', sans-serif;
    background-color: #1a1a2e;
    color: #eee;
    margin: 20px;
}
h1 {
    color: #fff;
    border-bottom: 2px solid #4a4a6a;
    padding-bottom: 10px;
}
.item-container {
    display: flex;
    flex-wrap: wrap;
    gap: 20px;
}
.item-card {
    background: #2a2a4a;
    border-radius: 8px;
    padding: 15px;
    width: 350px;
    box-shadow: 0 2px 8px rgba(0,0,0,0.3);
    border-left: 4px solid #5b9bd5;
}
.item-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 10px;
}
.item-name {
    font-size: 1.2em;
    font-weight: bold;
    color: #5bc0de;
}
.item-id {
    font-size: 0.8em;
    color: #888;
}
.item-meta {
    display: flex;
    gap: 10px;
    margin-bottom: 10px;
    flex-wrap: wrap;
}
.item-type, .item-level {
    font-size: 0.9em;
    color: #bbb;
}
.item-jobs {
    font-size: 0.85em;
    color: #999;
    margin-top: 10px;
    padding-top: 10px;
    border-top: 1px solid #3a3a5a;
}
.stat-line {
    font-size: 0.9em;
    color: #cfc;
    padding: 2px 0;
}
.effects-section, .ops-section {
    margin-top: 8px;
    padding-top: 8px;
    border-top: 1px dashed #3a3a5a;
}
.effect-line {
    color: #afd;
    font-size: 0.85em;
    padding: 2px 0;
}
.op-line {
    color: #fda;
    font-size: 0.85em;
    padding: 2px 0;
}
";
        }
    }
}
