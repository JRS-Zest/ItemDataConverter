using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

namespace ItemDataConverter.Core
{
    /// <summary>
    /// 復号化されたitem.datバイナリからJSONデータを抽出する
    /// 複合itemdatからjson抽出.py からの完全忠実な移植
    /// </summary>
    public static class ItemParser
    {
        private const int ItemBlockSize = 426;
        private const int HeaderSize = 12;

        // 職業データ (Pythonと同一順序)
        private static readonly (string name, int gender)[] JobData = new[]
        {
            ("剣士", 0), ("戦士", 0), ("ウィザード", 0), ("ウルフマン", 0),
            ("ビショップ", 0), ("天使", 0), ("シーフ", 0), ("武道家", 0),
            ("ランサー", 1), ("アーチャー", 1), ("ビーストテイマー", 1), ("サマナー", 1),
            ("プリンセス", 1), ("リトルウィッチ", 1), ("ネクロマンサー", 1),
            ("悪魔", 1), ("霊術師", 1), ("闘士", 1), ("光奏師", 0)
        };

        // 終端マーカー
        private static readonly byte[] EndMarkerA = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF, 0xFF };
        private static readonly byte[] EndMarkerB = new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF, 0xFF };
        private static readonly byte[] EndMarkerJrs = new byte[] { 0xDF, 0x02, 0x00, 0x00, 0x00, 0x00 };

        /// <summary>
        /// 復号化されたバイナリデータからアイテム辞書を抽出
        /// Pythonのextract_from_pathと完全に同一のロジック
        /// </summary>
        /// <param name="decrypted">復号化されたバイナリ（先頭12バイトはヘッダー）</param>
        /// <returns>アイテムID -> アイテムデータ の辞書</returns>
        public static Dictionary<string, Dictionary<string, object>> Parse(byte[] decrypted)
        {
            var output = new Dictionary<string, Dictionary<string, object>>();
            var encoding = Encoding.GetEncoding(932); // Shift_JIS

            int offset = HeaderSize; // f.seek(12) と同等

            while (offset + 6 <= decrypted.Length)
            {
                // 終端マーカーチェック
                if (MatchesEndMarkerJrs(decrypted, offset))
                {
                    break;
                }

                if (offset + ItemBlockSize > decrypted.Length)
                {
                    break;
                }

                var block = new byte[ItemBlockSize];
                Array.Copy(decrypted, offset, block, 0, ItemBlockSize);

                try
                {
                    var item = ParseItemBlock(block, encoding);
                    var fields = item["fields"] as Dictionary<string, object>;
                    if (fields != null && fields.ContainsKey("Index"))
                    {
                        string indexStr = fields["Index"].ToString()!;
                        output[indexStr] = item;
                    }
                }
                catch
                {
                    // パースエラーの場合はスキップ
                }

                offset += ItemBlockSize;
            }

            return output;
        }

        /// <summary>
        /// JRS終端マーカー判定
        /// </summary>
        private static bool MatchesEndMarkerJrs(byte[] data, int offset)
        {
            if (offset + 6 > data.Length) return false;
            for (int i = 0; i < EndMarkerJrs.Length; i++)
            {
                if (data[offset + i] != EndMarkerJrs[i]) return false;
            }
            return true;
        }

        /// <summary>
        /// 10バイトチャンクが終端マーカーか判定
        /// </summary>
        private static bool IsEndMarker(byte[] chunk)
        {
            if (chunk.Length < 10) return false;
            return MatchesPattern(chunk, EndMarkerA) || MatchesPattern(chunk, EndMarkerB);
        }

        private static bool MatchesPattern(byte[] data, byte[] pattern)
        {
            if (data.Length < pattern.Length) return false;
            for (int i = 0; i < pattern.Length; i++)
            {
                if (data[i] != pattern[i]) return false;
            }
            return true;
        }

        /// <summary>
        /// 1ブロック（426バイト）からアイテムデータを抽出
        /// Pythonと完全に同一のオフセット
        /// </summary>
        private static Dictionary<string, object> ParseItemBlock(byte[] block, Encoding encoding)
        {
            int typeId = (int)BitConverter.ToUInt32(block, 76);

            // --- 基本ステータス (0B - 149B) ---
            var fields = new Dictionary<string, object>
            {
                ["Index"] = (int)BitConverter.ToUInt32(block, 0),
                ["Name"] = ReadNullTerminatedString(block, 4, 64, encoding),
                ["Unknown_68_72"] = BytesToHex(block, 68, 4),
                ["Unknown_72_76"] = BytesToHex(block, 72, 4),
                ["Type"] = typeId,
                ["Unknown_80_96"] = BytesToHex(block, 80, 16),
                ["BasePrice"] = (int)BitConverter.ToUInt32(block, 96),
                ["SellingType"] = (int)BitConverter.ToUInt16(block, 100),
                ["AttackRange"] = (int)BitConverter.ToUInt16(block, 102),
                ["Unknown_104_106"] = BytesToHex(block, 104, 2),
                ["AttackSpeed_raw"] = (int)BitConverter.ToUInt16(block, 106),
            };

            // LowAP/HighAP: type_id == 0 の場合スワップ
            if (typeId == 0)
            {
                fields["LowAP"] = (int)BitConverter.ToUInt16(block, 110);
                fields["HighAP"] = (int)BitConverter.ToUInt16(block, 108);
            }
            else
            {
                fields["LowAP"] = (int)BitConverter.ToUInt16(block, 108);
                fields["HighAP"] = (int)BitConverter.ToUInt16(block, 110);
            }

            fields["Durable"] = (int)BitConverter.ToUInt16(block, 112);
            fields["Unknown_114_120"] = BytesToHex(block, 114, 6);
            fields["RequiredLevel"] = (int)BitConverter.ToUInt16(block, 120);
            fields["RequiredStatus"] = ReadUshortArray(block, 122, 7);
            fields["Shape"] = (int)BitConverter.ToUInt16(block, 136);
            fields["ImageShapesIndex"] = (int)BitConverter.ToUInt16(block, 138);
            fields["Unknown_140_142"] = BytesToHex(block, 140, 2);
            fields["QuestInfo"] = (int)BitConverter.ToUInt32(block, 142);
            fields["StackableNum"] = (int)BitConverter.ToUInt16(block, 146);
            fields["DropLevel"] = (int)BitConverter.ToUInt16(block, 148);

            // --- Effect 抽出（10バイト単位） ---
            var effects = new List<Dictionary<string, object>>();
            int effectBase = 150;
            int maxEffectBytes = 48;

            int i = 0;
            while (i < (maxEffectBytes / 10))
            {
                int chunkOffset = effectBase + (i * 10);
                if (chunkOffset + 10 > block.Length) break;

                var chunk = new byte[10];
                Array.Copy(block, chunkOffset, chunk, 0, 10);

                if (IsEndMarker(chunk)) break;

                ushort lVal = BitConverter.ToUInt16(chunk, 0);
                ushort hVal = BitConverter.ToUInt16(chunk, 2);
                ushort lVal2 = BitConverter.ToUInt16(chunk, 4);
                ushort hVal2 = BitConverter.ToUInt16(chunk, 6);
                ushort effectId = BitConverter.ToUInt16(chunk, 8);

                // 特殊パターンチェック: 次のブロックを先読み
                if (i + 1 < (maxEffectBytes / 10))
                {
                    int nextOffset = effectBase + ((i + 1) * 10);
                    if (nextOffset + 10 <= block.Length)
                    {
                        var nextChunk = new byte[10];
                        Array.Copy(block, nextOffset, nextChunk, 0, 10);

                        ushort nextLVal = BitConverter.ToUInt16(nextChunk, 0);
                        ushort nextHVal = BitConverter.ToUInt16(nextChunk, 2);
                        ushort nextLVal2 = BitConverter.ToUInt16(nextChunk, 4);
                        ushort nextHVal2 = BitConverter.ToUInt16(nextChunk, 6);
                        ushort nextEffectId = BitConverter.ToUInt16(nextChunk, 8);

                        // 特殊パターン: 次のブロックのIDが0xFFFFでなく、かつNeedが全て0
                        if (nextEffectId != 0xFFFF &&
                            nextLVal == 0 && nextHVal == 0 &&
                            nextLVal2 == 0 && nextHVal2 == 0)
                        {
                            effects.Add(new Dictionary<string, object>
                            {
                                ["LowValue"] = (int)lVal,
                                ["HighValue"] = (int)hVal,
                                ["LowValue2"] = 0,
                                ["HighValue2"] = 0,
                                ["EffectID"] = (int)effectId
                            });

                            effects.Add(new Dictionary<string, object>
                            {
                                ["LowValue"] = (int)lVal2,
                                ["HighValue"] = (int)hVal2,
                                ["LowValue2"] = 0,
                                ["HighValue2"] = 0,
                                ["EffectID"] = (int)nextEffectId
                            });

                            i += 2;
                            continue;
                        }
                    }
                }

                // 通常パターン: 全て0ならスキップ
                if (effectId == 0 && lVal == 0 && hVal == 0 && lVal2 == 0 && hVal2 == 0)
                {
                    i++;
                    continue;
                }

                effects.Add(new Dictionary<string, object>
                {
                    ["LowValue"] = (int)lVal,
                    ["HighValue"] = (int)hVal,
                    ["LowValue2"] = (int)lVal2,
                    ["HighValue2"] = (int)hVal2,
                    ["EffectID"] = (int)effectId
                });

                i++;
            }

            // --- Unique Option 6枠 (198B - 305B) ---
            var ops = new List<Dictionary<string, object>>();
            for (int j = 0; j < 6; j++)
            {
                int baseOff = 198 + (j * 18);
                ops.Add(new Dictionary<string, object>
                {
                    ["EffectID"] = (int)BitConverter.ToUInt16(block, baseOff),
                    ["Values"] = ReadUshortArray(block, baseOff + 2, 8)
                });
            }

            // --- 追加解析セクション (306B - 425B) ---
            string unknown306_330 = BytesToHex(block, 306, 24);
            int dropCoefficient = BitConverter.ToUInt16(block, 330);
            string unknown332_356 = BytesToHex(block, 332, 24);

            // 性別・職業制限 (356B - )
            byte genderByte = block[356];
            bool isFemaleAble = (genderByte & 0x20) != 0;
            bool isMaleAble = (genderByte & 0x10) != 0;

            var jobArea = new byte[4];
            Array.Copy(block, 358, jobArea, 0, 4);

            var tempEnabledJobs = new List<string>();
            int jobBitCount = 0;

            for (int k = 0; k < JobData.Length; k++)
            {
                int byteIdx = k / 8;
                int bitIdx = k % 8;
                if ((jobArea[byteIdx] & (1 << bitIdx)) != 0)
                {
                    jobBitCount++;
                    if ((JobData[k].gender == 0 && isMaleAble) || (JobData[k].gender == 1 && isFemaleAble))
                    {
                        tempEnabledJobs.Add(JobData[k].name);
                    }
                }
            }

            // 要件メッセージの生成
            string resStr = "";
            if (jobBitCount == JobData.Length)
            {
                if (isMaleAble && !isFemaleAble)
                {
                    resStr = "男性キャラ専用アイテム";
                }
                else if (isFemaleAble && !isMaleAble)
                {
                    resStr = "女性キャラ専用アイテム";
                }
            }
            else
            {
                resStr = string.Join(", ", tempEnabledJobs);
            }

            // JSON構造の組み立て（Pythonと同一）
            return new Dictionary<string, object>
            {
                ["fields"] = fields,
                ["unique_effects"] = effects,
                ["unique_ops"] = ops,
                ["extended_data"] = new Dictionary<string, object>
                {
                    ["Unknown_306_330"] = unknown306_330,
                    ["DropCoefficient"] = dropCoefficient,
                    ["Unknown_332_356"] = unknown332_356
                },
                ["requirements"] = resStr
            };
        }

        /// <summary>
        /// null終端文字列を読み取り
        /// </summary>
        private static string ReadNullTerminatedString(byte[] data, int offset, int maxLength, Encoding encoding)
        {
            int end = offset;
            int limit = Math.Min(offset + maxLength, data.Length);
            for (int idx = offset; idx < limit; idx++)
            {
                if (data[idx] == 0x00)
                {
                    end = idx;
                    break;
                }
                end = idx + 1;
            }

            int strLen = end - offset;
            if (strLen <= 0) return "";

            var bytes = new byte[strLen];
            Array.Copy(data, offset, bytes, 0, strLen);
            return encoding.GetString(bytes).Trim();
        }

        /// <summary>
        /// バイト列を16進文字列に変換
        /// </summary>
        private static string BytesToHex(byte[] data, int offset, int length)
        {
            var sb = new StringBuilder(length * 2);
            for (int i = 0; i < length && offset + i < data.Length; i++)
            {
                sb.Append(data[offset + i].ToString("x2"));
            }
            return sb.ToString();
        }

        /// <summary>
        /// ushort配列を読み取り
        /// </summary>
        private static List<int> ReadUshortArray(byte[] data, int offset, int count)
        {
            var result = new List<int>();
            for (int i = 0; i < count; i++)
            {
                int pos = offset + (i * 2);
                if (pos + 2 <= data.Length)
                {
                    result.Add(BitConverter.ToUInt16(data, pos));
                }
            }
            return result;
        }

        /// <summary>
        /// アイテム辞書をJSON文字列に変換（Pythonと同等の出力形式）
        /// </summary>
        public static string ToJson(Dictionary<string, Dictionary<string, object>> items, bool indented = true)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = indented,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            return JsonSerializer.Serialize(items, options);
        }
    }
}
