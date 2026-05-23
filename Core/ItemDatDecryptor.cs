using System;
using System.IO;
using System.Text;
using RedStoneLib.Packets;

namespace ItemDataConverter.Core
{
    /// <summary>
    /// item.datファイルの復号化を行う
    /// 参考用ItemDatDecryptorGui MainForm.cs からの忠実な移植
    /// </summary>
    public static class ItemDatDecryptor
    {
        private const int ItemBlockSize = 426;
        private const int OpBlockSize = 160;
        private const int HeaderSize = 12;

        /// <summary>
        /// item.datを復号化してバイナリデータを返す
        /// </summary>
        /// <param name="filePath">item.datのパス</param>
        /// <returns>復号化されたバイナリデータ（ヘッダー + アイテム + OPカウント + OP）</returns>
        public static byte[] Decrypt(string filePath)
        {
            byte[] rawData = File.ReadAllBytes(filePath);

            if (rawData.Length < HeaderSize)
            {
                throw new InvalidDataException("Invalid item.dat file: too short for header");
            }

            // ヘッダー読み取り
            int rawKey = BitConverter.ToInt32(rawData, 0);

            // itemCount は暗号化されている（PacketReaderのEncryptionRead相当）
            // ここでは暗号化された4バイトを復号化する必要がある
            byte[] encItemCount = new byte[4];
            Array.Copy(rawData, 4, encItemCount, 0, 4);
            uint decodeKey = PacketCrypt.GenerateScenarioDecodeKey(rawKey);
            byte[] decItemCount = PacketCrypt.DecodeScenarioBuffer(encItemCount, decodeKey);
            int itemCount = BitConverter.ToInt32(decItemCount, 0);

            // unknown (4 bytes at offset 8) - 使用しない
            // uint unknown = BitConverter.ToUInt32(rawData, 8);

            // アイテムデータ復号化
            long itemDataLength = (long)itemCount * ItemBlockSize;
            int itemDataEnd = HeaderSize + (int)itemDataLength;
            if (rawData.Length < itemDataEnd)
            {
                throw new InvalidDataException($"Invalid item.dat file: expected {itemDataEnd} bytes, got {rawData.Length}");
            }

            byte[] encryptedItems = new byte[itemDataLength];
            Array.Copy(rawData, HeaderSize, encryptedItems, 0, itemDataLength);
            byte[] decryptedItems = PacketCrypt.DecodeScenarioBuffer(encryptedItems, decodeKey);

            int opCount = 0;
            byte[] decryptedOps = Array.Empty<byte>();

            if (rawData.Length >= itemDataEnd + 4)
            {
                byte[] encOpCount = new byte[4];
                Array.Copy(rawData, itemDataEnd, encOpCount, 0, 4);
                byte[] decOpCount = PacketCrypt.DecodeScenarioBuffer(encOpCount, decodeKey);
                opCount = BitConverter.ToInt32(decOpCount, 0);

                int opDataStart = itemDataEnd + 4;
                long opDataLength = (long)opCount * OpBlockSize;
                if (opCount > 0 && rawData.Length >= opDataStart + opDataLength)
                {
                    decryptedOps = new byte[opCount * OpBlockSize];
                    for (int index = 0; index < opCount; index++)
                    {
                        byte[] encryptedOp = new byte[OpBlockSize];
                        Array.Copy(rawData, opDataStart + (index * OpBlockSize), encryptedOp, 0, OpBlockSize);
                        byte[] decryptedOp = PacketCrypt.DecodeScenarioBuffer(encryptedOp, decodeKey);
                        Array.Copy(decryptedOp, 0, decryptedOps, index * OpBlockSize, OpBlockSize);
                    }
                }
            }

            byte[] result = new byte[HeaderSize + decryptedItems.Length + 4 + decryptedOps.Length];
            BitConverter.GetBytes(rawKey).CopyTo(result, 0);
            BitConverter.GetBytes(itemCount).CopyTo(result, 4);
            Array.Copy(decryptedItems, 0, result, HeaderSize, decryptedItems.Length);

            int opCountOffset = HeaderSize + decryptedItems.Length;
            BitConverter.GetBytes(opCount).CopyTo(result, opCountOffset);
            if (decryptedOps.Length > 0)
            {
                Array.Copy(decryptedOps, 0, result, opCountOffset + 4, decryptedOps.Length);
            }

            return result;
        }

        /// <summary>
        /// item.datを復号化してファイルに保存
        /// </summary>
        /// <param name="inputPath">item.datのパス</param>
        /// <param name="outputPath">復号化データの出力パス</param>
        public static void DecryptToFile(string inputPath, string outputPath)
        {
            byte[] decrypted = Decrypt(inputPath);
            File.WriteAllBytes(outputPath, decrypted);
        }
    }
}
