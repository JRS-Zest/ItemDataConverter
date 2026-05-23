using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using ItemDataConverter.Core;

namespace ItemDataConverter
{
    public partial class MainForm : Form
    {
        private bool _initialized = false;

        public MainForm()
        {
            InitializeComponent();
            // アプリケーションがアイドル状態になってから初期化
            Application.Idle += OnApplicationIdle;
        }

        /// <summary>
        /// アプリケーションがアイドル状態になったら初期化（GUI描画完了後）
        /// </summary>
        private void OnApplicationIdle(object? sender, EventArgs e)
        {
            if (_initialized) return;
            _initialized = true;
            
            // イベント解除
            Application.Idle -= OnApplicationIdle;
            
            // バックグラウンドで初期化
            Task.Run(() => LoadDefaultPaths());
        }

        /// <summary>
        /// レジストリからデフォルトパスを読み込み
        /// </summary>
        private void LoadDefaultPaths()
        {
            try
            {
                string? itemPath = RegistryHelper.GetItemDatPathFromRegistry();
                if (!string.IsNullOrEmpty(itemPath) && File.Exists(itemPath))
                {
                    SetTextSafe(txtItemDatPath, itemPath);
                }
            }
            catch { }

            try
            {
                string? textPath = RegistryHelper.GetTextDataDatPathFromRegistry();
                if (!string.IsNullOrEmpty(textPath) && File.Exists(textPath))
                {
                    SetTextSafe(txtTextDataPath, textPath);
                }
            }
            catch { }

            // 出力先のデフォルトはEXEと同じフォルダ
            try
            {
                SetTextSafe(txtOutputPath, Application.StartupPath);
            }
            catch { }
        }

        /// <summary>
        /// スレッドセーフにテキスト設定
        /// </summary>
        private void SetTextSafe(TextBox textBox, string text)
        {
            if (textBox.InvokeRequired)
            {
                textBox.Invoke(new Action(() => textBox.Text = text));
            }
            else
            {
                textBox.Text = text;
            }
        }

        /// <summary>
        /// item.dat参照ボタン
        /// </summary>
        private void btnBrowseItemDat_Click(object sender, EventArgs e)
        {
            using var ofd = new OpenFileDialog
            {
                Title = "item.datを選択",
                Filter = "DATファイル (*.dat)|*.dat|すべてのファイル (*.*)|*.*",
                FileName = "item.dat"
            };

            if (!string.IsNullOrEmpty(txtItemDatPath.Text))
            {
                ofd.InitialDirectory = Path.GetDirectoryName(txtItemDatPath.Text);
            }

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                txtItemDatPath.Text = ofd.FileName;
            }
        }

        /// <summary>
        /// textData.dat参照ボタン
        /// </summary>
        private void btnBrowseTextData_Click(object sender, EventArgs e)
        {
            using var ofd = new OpenFileDialog
            {
                Title = "textData.datを選択",
                Filter = "DATファイル (*.dat)|*.dat|すべてのファイル (*.*)|*.*",
                FileName = "textData.dat"
            };

            if (!string.IsNullOrEmpty(txtTextDataPath.Text))
            {
                ofd.InitialDirectory = Path.GetDirectoryName(txtTextDataPath.Text);
            }

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                txtTextDataPath.Text = ofd.FileName;
            }
        }

        /// <summary>
        /// 出力先参照ボタン
        /// </summary>
        private void btnBrowseOutput_Click(object sender, EventArgs e)
        {
            using var fbd = new FolderBrowserDialog
            {
                Description = "出力先フォルダを選択"
            };

            if (!string.IsNullOrEmpty(txtOutputPath.Text))
            {
                fbd.InitialDirectory = txtOutputPath.Text;
            }

            if (fbd.ShowDialog() == DialogResult.OK)
            {
                txtOutputPath.Text = fbd.SelectedPath;
            }
        }

        /// <summary>
        /// 変換実行ボタン
        /// </summary>
        private async void btnConvert_Click(object sender, EventArgs e)
        {
            // 入力チェック
            if (string.IsNullOrWhiteSpace(txtItemDatPath.Text))
            {
                MessageBox.Show("item.datのパスを指定してください。", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!File.Exists(txtItemDatPath.Text))
            {
                MessageBox.Show("指定されたitem.datが見つかりません。", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(txtTextDataPath.Text))
            {
                MessageBox.Show("textData.datのパスを指定してください。", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!File.Exists(txtTextDataPath.Text))
            {
                MessageBox.Show("指定されたtextData.datが見つかりません。", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(txtOutputPath.Text))
            {
                MessageBox.Show("出力先フォルダを指定してください。", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // UI無効化
            btnConvert.Enabled = false;
            progressBar.Visible = true;
            progressBar.Style = ProgressBarStyle.Marquee;
            lblStatus.Text = "処理中...";

            try
            {
                await Task.Run(() => ConvertProcess());

                lblStatus.Text = "変換完了";
                MessageBox.Show("変換が完了しました。", "完了", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                lblStatus.Text = "エラー発生";
                MessageBox.Show($"エラーが発生しました:\n{ex.Message}", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnConvert.Enabled = true;
                progressBar.Visible = false;
            }
        }

        /// <summary>
        /// 変換処理本体
        /// </summary>
        private void ConvertProcess()
        {
            string itemDatPath = txtItemDatPath.Text;
            string textDataPath = txtTextDataPath.Text;
            string outputDir = txtOutputPath.Text;

            // 出力ディレクトリ作成
            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            // Step 1: item.dat復号化
            UpdateStatus("item.datを復号化中...");
            byte[] decrypted = ItemDatDecryptor.Decrypt(itemDatPath);

            // Step 2: textData.dat読み込み
            UpdateStatus("textData.datを読み込み中...");
            var textdata = TextDataReader.ExtractFromPath(textDataPath);

            // Step 3: バイナリからアイテム抽出
            UpdateStatus("アイテムデータを抽出中...");
            var items = ItemParser.Parse(decrypted);

            // Step 4: ツールチップ情報追加
            UpdateStatus("ツールチップ情報を生成中...");
            var itemsWithTooltip = TooltipGenerator.AddTooltipAll(items, textdata);

            // Step 5: 出力ファイル生成
            UpdateStatus("ファイルを出力中...");

            // バージョン番号を取得（ファイル名用、4桁ゼロ埋め）
            int? version = RegistryHelper.GetVersionFromRegistry();
            string versionStr = version.HasValue ? version.Value.ToString("D4") : "0000";

            // JSON出力（テキストリスト形式 - Python互換）
            if (chkOutputJson.Checked)
            {
                string jsonPath = Path.Combine(outputDir, $"JRSitem_{versionStr}.json");
                ItemRenderer.SaveAsRenderedJson(itemsWithTooltip, textdata, jsonPath);
            }

            // HTML出力
            if (chkOutputHtml.Checked)
            {
                string htmlPath = Path.Combine(outputDir, $"JRSitem_{versionStr}.html");
                var ops = OpsExtractor.ExtractFromDecrypted(decrypted);
                var renderedOps = OpsRenderer.RenderAll(ops, textdata);
                CombinedHtmlRenderer.SaveCombinedHtml(itemsWithTooltip, renderedOps, htmlPath, "JRS", chkOutputJson.Checked);
            }

            // 復号化バイナリ出力（オプション）
            if (chkOutputDecrypted.Checked)
            {
                string binPath = Path.Combine(outputDir, "item_decrypted.bin");
                File.WriteAllBytes(binPath, decrypted);
            }

            UpdateStatus("完了");
        }

        /// <summary>
        /// ステータス更新（スレッドセーフ）
        /// </summary>
        private void UpdateStatus(string message)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string>(UpdateStatus), message);
                return;
            }
            lblStatus.Text = message;
        }
    }
}
