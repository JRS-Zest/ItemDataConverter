using System;
using System.IO;
using Microsoft.Win32;

namespace ItemDataConverter.Core
{
    /// <summary>
    /// レジストリからRed Stoneのインストールパスを取得するヘルパー
    /// </summary>
    public static class RegistryHelper
    {
        /// <summary>
        /// レジストリからRedStoneインストールパスを取得
        /// </summary>
        public static string? GetRedStonePathFromRegistry()
        {
            try
            {
                // Red Stone for Japan（本番サーバー）
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\L&K Logic Korea\Red Stone for Japan");
                if (key != null)
                {
                    var path = key.GetValue("path") as string;
                    var excuteFolder = key.GetValue("Excute Folder") as string;
                    var basePath = !string.IsNullOrEmpty(path) ? path : excuteFolder;
                    if (!string.IsNullOrEmpty(basePath)) return basePath;
                }

                // フォールバック: Red Stone Portable for Japan
                using var keyOld = Registry.CurrentUser.OpenSubKey(@"Software\L&K Logic Korea\Red Stone Portable for Japan");
                if (keyOld != null)
                {
                    var path = keyOld.GetValue("path") as string;
                    var excuteFolder = keyOld.GetValue("Excute Folder") as string;
                    var basePath = !string.IsNullOrEmpty(path) ? path : excuteFolder;
                    return basePath;
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// レジストリからitem.datパスを取得
        /// </summary>
        public static string? GetItemDatPathFromRegistry()
        {
            var basePath = GetRedStonePathFromRegistry();
            if (string.IsNullOrEmpty(basePath)) return null;

            var itemDatPath = Path.Combine(basePath, "Data", "Scenario", "Red Stone", "item.dat");
            return File.Exists(itemDatPath) ? itemDatPath : null;
        }

        /// <summary>
        /// レジストリからtextData.datパスを取得
        /// </summary>
        public static string? GetTextDataDatPathFromRegistry()
        {
            var basePath = GetRedStonePathFromRegistry();
            if (string.IsNullOrEmpty(basePath)) return null;

            var textDataPath = Path.Combine(basePath, "Data", "textData.dat");
            return File.Exists(textDataPath) ? textDataPath : null;
        }

        /// <summary>
        /// レジストリからバージョン番号を取得
        /// </summary>
        public static int? GetVersionFromRegistry()
        {
            try
            {
                // Red Stone for Japan（本番サーバー）
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\L&K Logic Korea\Red Stone for Japan");
                if (key != null)
                {
                    var version = key.GetValue("Version");
                    if (version != null)
                    {
                        return Convert.ToInt32(version);
                    }
                }

                // フォールバック: Red Stone Portable for Japan
                using var keyOld = Registry.CurrentUser.OpenSubKey(@"Software\L&K Logic Korea\Red Stone Portable for Japan");
                if (keyOld != null)
                {
                    var version = keyOld.GetValue("Version");
                    if (version != null)
                    {
                        return Convert.ToInt32(version);
                    }
                }

                return null;
            }
            catch
            {
                return null;
            }
        }
    }
}
