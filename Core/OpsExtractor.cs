using System;
using System.Collections.Generic;
using System.Text;

namespace ItemDataConverter.Core
{
    /// <summary>
    /// 復号化済み item.dat 末尾のオプションブロックを抽出する。
    /// </summary>
    public static class OpsExtractor
    {
        public const int OpBlockSize = 160;
        public const int ItemBlockSize = 426;
        public const int HeaderSize = 12;

        public sealed class OpRecord
        {
            public int Index { get; set; }
            public int OpId { get; set; }
            public string Name1 { get; set; } = "";
            public string Name2 { get; set; } = "";
            public int Effect { get; set; }
            public int OpValue1Min { get; set; }
            public int OpValue1Max { get; set; }
            public int OpValue2Min { get; set; }
            public int OpValue2Max { get; set; }
            public int RequireLevel { get; set; }
            public int PriceBase { get; set; }
            public int PriceType { get; set; }
            public int WeaponColor { get; set; }
            public int DropCoefficient { get; set; }
        }

        public static List<OpRecord> ExtractFromDecrypted(byte[] decrypted)
        {
            if (decrypted.Length < HeaderSize)
            {
                throw new InvalidOperationException("復号化データが短すぎます。");
            }

            int itemCount = BitConverter.ToInt32(decrypted, 4);
            int opsCountOffset = HeaderSize + (itemCount * ItemBlockSize);
            if (opsCountOffset + 4 > decrypted.Length)
            {
                return new List<OpRecord>();
            }

            int opCount = BitConverter.ToInt32(decrypted, opsCountOffset);
            int opsOffset = opsCountOffset + 4;
            int opsByteLength = checked(opCount * OpBlockSize);
            if (opsOffset + opsByteLength > decrypted.Length)
            {
                return new List<OpRecord>();
            }

            var result = new List<OpRecord>(opCount);
            var encoding = Encoding.GetEncoding(932);

            for (int index = 0; index < opCount; index++)
            {
                int offset = opsOffset + (index * OpBlockSize);
                ushort value1A = BitConverter.ToUInt16(decrypted, offset + 6);
                ushort value1B = BitConverter.ToUInt16(decrypted, offset + 8);
                ushort value2A = BitConverter.ToUInt16(decrypted, offset + 10);
                ushort value2B = BitConverter.ToUInt16(decrypted, offset + 12);

                result.Add(new OpRecord
                {
                    Index = index,
                    OpId = BitConverter.ToUInt16(decrypted, offset),
                    Effect = decrypted[offset + 4],
                    OpValue1Min = Math.Min(value1A, value1B),
                    OpValue1Max = Math.Max(value1A, value1B),
                    OpValue2Min = Math.Min(value2A, value2B),
                    OpValue2Max = Math.Max(value2A, value2B),
                    Name1 = DecodeString(decrypted, offset + 16, 20, encoding),
                    Name2 = DecodeString(decrypted, offset + 36, 20, encoding),
                    RequireLevel = BitConverter.ToUInt16(decrypted, offset + 56),
                    PriceBase = BitConverter.ToUInt16(decrypted, offset + 60),
                    PriceType = BitConverter.ToUInt16(decrypted, offset + 64),
                    WeaponColor = decrypted[offset + 116],
                    DropCoefficient = BitConverter.ToUInt16(decrypted, offset + 120),
                });
            }

            return result;
        }

        private static string DecodeString(byte[] data, int offset, int length, Encoding encoding)
        {
            var buffer = new byte[length];
            Array.Copy(data, offset, buffer, 0, length);

            int nullIndex = Array.IndexOf(buffer, (byte)0);
            if (nullIndex >= 0)
            {
                return encoding.GetString(buffer, 0, nullIndex);
            }

            return encoding.GetString(buffer);
        }
    }
}