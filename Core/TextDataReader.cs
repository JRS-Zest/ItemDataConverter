using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ItemDataConverter.Core
{
    /// <summary>
    /// textData.datファイルを解析してテキストデータを抽出する。
    /// </summary>
    public static class TextDataReader
    {
        private const int BlockSize = 260;

        public static Dictionary<string, Dictionary<string, string>> ExtractFromPath(string filePath)
        {
            var results = new Dictionary<string, Dictionary<string, string>>();

            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var br = new BinaryReader(fs, Encoding.GetEncoding(932));

            for (int sectionIndex = 1; sectionIndex <= 18; sectionIndex++)
            {
                if (fs.Position + 4 > fs.Length)
                {
                    throw new EndOfStreamException($"Section{sectionIndex}: Count を読み取れません (offset 0x{fs.Position:X8})");
                }

                uint count = br.ReadUInt32();
                if (count == 0 || count > 100000)
                {
                    throw new InvalidDataException($"Section{sectionIndex} の Count が不正です: {count} (offset 0x{fs.Position - 4:X8})");
                }

                var dict = new Dictionary<string, string>();
                for (uint index = 0; index < count; index++)
                {
                    byte[] header = br.ReadBytes(2);
                    if (header.Length < 2)
                    {
                        throw new EndOfStreamException($"Section{sectionIndex} のブロックがファイル末尾で切れています");
                    }

                    using var ms = new MemoryStream();
                    while (true)
                    {
                        int value = fs.ReadByte();
                        if (value == -1)
                        {
                            throw new EndOfStreamException($"Section{sectionIndex} ブロックの文字列終端が見つかりません (offset 0x{fs.Position:X8})");
                        }

                        if (value == 0x00)
                        {
                            break;
                        }

                        ms.WriteByte((byte)value);
                    }

                    dict[index.ToString()] = DecodeText(ms.ToArray());
                }

                results[$"section{sectionIndex}"] = dict;
            }

            long markerPos = FindPhysicalMarker(fs);
            fs.Seek(markerPos + 2, SeekOrigin.Begin);
            SkipZeroPadding(fs);

            long section20Pos = fs.Position;
            if (fs.Position + 4 > fs.Length)
            {
                throw new EndOfStreamException("Section20 の Count を読み取れませんでした");
            }

            uint count20 = br.ReadUInt32();
            if (count20 == 0 || count20 > 100000)
            {
                throw new InvalidDataException($"Section20 の Count が不正です: {count20} (offset 0x{section20Pos:X8})");
            }

            var section20 = ReadFixedBlockSection(br, count20);
            results["section20"] = PatchSection20(section20);

            long after20Offset = fs.Position;
            fs.Seek(after20Offset, SeekOrigin.Begin);
            SkipZeroPadding(fs);
            if (fs.Position + 4 > fs.Length)
            {
                throw new EndOfStreamException("Section21 の Count を読み取れませんでした");
            }

            uint count21 = br.ReadUInt32();
            if (count21 == 0 || count21 > 100000)
            {
                throw new InvalidDataException($"Section21 の Count が不正です: {count21} (offset 0x{fs.Position - 4:X8})");
            }

            results["section21"] = ReadFixedBlockSection(br, count21);
            return results;
        }

        private static Dictionary<string, string> ReadFixedBlockSection(BinaryReader br, uint count)
        {
            var section = new Dictionary<string, string>();
            for (uint index = 0; index < count; index++)
            {
                byte[] data = br.ReadBytes(BlockSize);
                if (data.Length < BlockSize)
                {
                    section[index.ToString()] = "";
                    continue;
                }

                byte[] textArea = new byte[BlockSize - 4];
                Array.Copy(data, 4, textArea, 0, BlockSize - 4);
                int nullIndex = Array.IndexOf(textArea, (byte)0);
                byte[] stringBytes;
                if (nullIndex >= 0)
                {
                    stringBytes = new byte[nullIndex];
                    Array.Copy(textArea, 0, stringBytes, 0, nullIndex);
                }
                else
                {
                    stringBytes = textArea;
                }

                section[index.ToString()] = DecodeText(stringBytes);
            }

            return section;
        }

        private static string DecodeText(byte[] bytes)
        {
            try
            {
                string text = Encoding.GetEncoding(932).GetString(bytes);
                text = text.Replace("<n>", "");
                text = System.Text.RegularExpressions.Regex.Replace(text, "<c:\\w+>", "");
                return text.Trim();
            }
            catch
            {
                return "[Decode Error]";
            }
        }

        private static long FindPhysicalMarker(FileStream fs)
        {
            int first = fs.ReadByte();
            int second = fs.ReadByte();
            if (first == -1 || second == -1)
            {
                throw new EndOfStreamException("物理境界マーカー (FF FF) が見つかりませんでした (EOF)");
            }

            while (true)
            {
                if (first == 0xFF && second == 0xFF)
                {
                    return fs.Position - 2;
                }

                first = second;
                second = fs.ReadByte();
                if (second == -1)
                {
                    break;
                }
            }

            throw new InvalidDataException("物理境界マーカー (FF FF) が見つかりませんでした");
        }

        private static void SkipZeroPadding(FileStream fs)
        {
            while (true)
            {
                int value = fs.ReadByte();
                if (value == -1)
                {
                    throw new EndOfStreamException("セクション開始位置を取得できませんでした（EOF）");
                }

                if (value != 0x00)
                {
                    fs.Seek(-1, SeekOrigin.Current);
                    return;
                }
            }
        }

        private static Dictionary<string, string> PatchSection20(Dictionary<string, string> source)
        {
            const int insertedFrom = 177;
            const int insertedTo = 178;
            const int shift = insertedTo - insertedFrom + 1;
            var patched = new Dictionary<string, string>(source.Count);

            foreach (var entry in source)
            {
                if (!int.TryParse(entry.Key, out int key))
                {
                    patched[entry.Key] = entry.Value;
                    continue;
                }

                if (key >= insertedFrom && key <= insertedTo)
                {
                    continue;
                }

                patched[(key > insertedTo ? key - shift : key).ToString()] = entry.Value;
            }

            return patched;
        }
    }
}
