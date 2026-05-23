namespace ItemDataConverter
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.lblItemDat = new System.Windows.Forms.Label();
            this.txtItemDatPath = new System.Windows.Forms.TextBox();
            this.btnBrowseItemDat = new System.Windows.Forms.Button();
            this.lblTextData = new System.Windows.Forms.Label();
            this.txtTextDataPath = new System.Windows.Forms.TextBox();
            this.btnBrowseTextData = new System.Windows.Forms.Button();
            this.lblOutput = new System.Windows.Forms.Label();
            this.txtOutputPath = new System.Windows.Forms.TextBox();
            this.btnBrowseOutput = new System.Windows.Forms.Button();
            this.grpOutputOptions = new System.Windows.Forms.GroupBox();
            this.chkOutputJson = new System.Windows.Forms.CheckBox();
            this.chkOutputHtml = new System.Windows.Forms.CheckBox();
            this.chkOutputDecrypted = new System.Windows.Forms.CheckBox();
            this.btnConvert = new System.Windows.Forms.Button();
            this.progressBar = new System.Windows.Forms.ProgressBar();
            this.lblStatus = new System.Windows.Forms.Label();
            this.grpOutputOptions.SuspendLayout();
            this.SuspendLayout();
            // 
            // lblItemDat
            // 
            this.lblItemDat.AutoSize = true;
            this.lblItemDat.Location = new System.Drawing.Point(12, 20);
            this.lblItemDat.Name = "lblItemDat";
            this.lblItemDat.Size = new System.Drawing.Size(65, 15);
            this.lblItemDat.TabIndex = 0;
            this.lblItemDat.Text = "item.dat:";
            // 
            // txtItemDatPath
            // 
            this.txtItemDatPath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtItemDatPath.Location = new System.Drawing.Point(100, 17);
            this.txtItemDatPath.Name = "txtItemDatPath";
            this.txtItemDatPath.Size = new System.Drawing.Size(400, 23);
            this.txtItemDatPath.TabIndex = 1;
            // 
            // btnBrowseItemDat
            // 
            this.btnBrowseItemDat.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnBrowseItemDat.Location = new System.Drawing.Point(506, 16);
            this.btnBrowseItemDat.Name = "btnBrowseItemDat";
            this.btnBrowseItemDat.Size = new System.Drawing.Size(75, 25);
            this.btnBrowseItemDat.TabIndex = 2;
            this.btnBrowseItemDat.Text = "参照...";
            this.btnBrowseItemDat.UseVisualStyleBackColor = true;
            this.btnBrowseItemDat.Click += new System.EventHandler(this.btnBrowseItemDat_Click);
            // 
            // lblTextData
            // 
            this.lblTextData.AutoSize = true;
            this.lblTextData.Location = new System.Drawing.Point(12, 55);
            this.lblTextData.Name = "lblTextData";
            this.lblTextData.Size = new System.Drawing.Size(82, 15);
            this.lblTextData.TabIndex = 3;
            this.lblTextData.Text = "textData.dat:";
            // 
            // txtTextDataPath
            // 
            this.txtTextDataPath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtTextDataPath.Location = new System.Drawing.Point(100, 52);
            this.txtTextDataPath.Name = "txtTextDataPath";
            this.txtTextDataPath.Size = new System.Drawing.Size(400, 23);
            this.txtTextDataPath.TabIndex = 4;
            // 
            // btnBrowseTextData
            // 
            this.btnBrowseTextData.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnBrowseTextData.Location = new System.Drawing.Point(506, 51);
            this.btnBrowseTextData.Name = "btnBrowseTextData";
            this.btnBrowseTextData.Size = new System.Drawing.Size(75, 25);
            this.btnBrowseTextData.TabIndex = 5;
            this.btnBrowseTextData.Text = "参照...";
            this.btnBrowseTextData.UseVisualStyleBackColor = true;
            this.btnBrowseTextData.Click += new System.EventHandler(this.btnBrowseTextData_Click);
            // 
            // lblOutput
            // 
            this.lblOutput.AutoSize = true;
            this.lblOutput.Location = new System.Drawing.Point(12, 90);
            this.lblOutput.Name = "lblOutput";
            this.lblOutput.Size = new System.Drawing.Size(50, 15);
            this.lblOutput.TabIndex = 6;
            this.lblOutput.Text = "出力先:";
            // 
            // txtOutputPath
            // 
            this.txtOutputPath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtOutputPath.Location = new System.Drawing.Point(100, 87);
            this.txtOutputPath.Name = "txtOutputPath";
            this.txtOutputPath.Size = new System.Drawing.Size(400, 23);
            this.txtOutputPath.TabIndex = 7;
            // 
            // btnBrowseOutput
            // 
            this.btnBrowseOutput.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnBrowseOutput.Location = new System.Drawing.Point(506, 86);
            this.btnBrowseOutput.Name = "btnBrowseOutput";
            this.btnBrowseOutput.Size = new System.Drawing.Size(75, 25);
            this.btnBrowseOutput.TabIndex = 8;
            this.btnBrowseOutput.Text = "参照...";
            this.btnBrowseOutput.UseVisualStyleBackColor = true;
            this.btnBrowseOutput.Click += new System.EventHandler(this.btnBrowseOutput_Click);
            // 
            // grpOutputOptions
            // 
            this.grpOutputOptions.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.grpOutputOptions.Controls.Add(this.chkOutputJson);
            this.grpOutputOptions.Controls.Add(this.chkOutputHtml);
            this.grpOutputOptions.Controls.Add(this.chkOutputDecrypted);
            this.grpOutputOptions.Location = new System.Drawing.Point(12, 125);
            this.grpOutputOptions.Name = "grpOutputOptions";
            this.grpOutputOptions.Size = new System.Drawing.Size(569, 60);
            this.grpOutputOptions.TabIndex = 9;
            this.grpOutputOptions.TabStop = false;
            this.grpOutputOptions.Text = "出力オプション";
            // 
            // chkOutputJson
            // 
            this.chkOutputJson.AutoSize = true;
            this.chkOutputJson.Checked = true;
            this.chkOutputJson.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkOutputJson.Location = new System.Drawing.Point(15, 28);
            this.chkOutputJson.Name = "chkOutputJson";
            this.chkOutputJson.Size = new System.Drawing.Size(104, 19);
            this.chkOutputJson.TabIndex = 0;
            this.chkOutputJson.Text = "JSON出力";
            this.chkOutputJson.UseVisualStyleBackColor = true;
            // 
            // chkOutputHtml
            // 
            this.chkOutputHtml.AutoSize = true;
            this.chkOutputHtml.Checked = true;
            this.chkOutputHtml.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkOutputHtml.Location = new System.Drawing.Point(140, 28);
            this.chkOutputHtml.Name = "chkOutputHtml";
            this.chkOutputHtml.Size = new System.Drawing.Size(100, 19);
            this.chkOutputHtml.TabIndex = 1;
            this.chkOutputHtml.Text = "HTML出力";
            this.chkOutputHtml.UseVisualStyleBackColor = true;
            // 
            // chkOutputDecrypted
            // 
            this.chkOutputDecrypted.AutoSize = true;
            this.chkOutputDecrypted.Location = new System.Drawing.Point(260, 28);
            this.chkOutputDecrypted.Name = "chkOutputDecrypted";
            this.chkOutputDecrypted.Size = new System.Drawing.Size(160, 19);
            this.chkOutputDecrypted.TabIndex = 2;
            this.chkOutputDecrypted.Text = "復号化バイナリ出力";
            this.chkOutputDecrypted.UseVisualStyleBackColor = true;
            // 
            // btnConvert
            // 
            this.btnConvert.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnConvert.Font = new System.Drawing.Font("Yu Gothic UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnConvert.Location = new System.Drawing.Point(456, 200);
            this.btnConvert.Name = "btnConvert";
            this.btnConvert.Size = new System.Drawing.Size(125, 40);
            this.btnConvert.TabIndex = 10;
            this.btnConvert.Text = "変換実行";
            this.btnConvert.UseVisualStyleBackColor = true;
            this.btnConvert.Click += new System.EventHandler(this.btnConvert_Click);
            // 
            // progressBar
            // 
            this.progressBar.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.progressBar.Location = new System.Drawing.Point(12, 250);
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(569, 23);
            this.progressBar.TabIndex = 11;
            this.progressBar.Visible = false;
            // 
            // lblStatus
            // 
            this.lblStatus.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.lblStatus.AutoSize = true;
            this.lblStatus.Location = new System.Drawing.Point(12, 215);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(60, 15);
            this.lblStatus.TabIndex = 12;
            this.lblStatus.Text = "待機中...";
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(593, 285);
            this.Controls.Add(this.lblStatus);
            this.Controls.Add(this.progressBar);
            this.Controls.Add(this.btnConvert);
            this.Controls.Add(this.grpOutputOptions);
            this.Controls.Add(this.btnBrowseOutput);
            this.Controls.Add(this.txtOutputPath);
            this.Controls.Add(this.lblOutput);
            this.Controls.Add(this.btnBrowseTextData);
            this.Controls.Add(this.txtTextDataPath);
            this.Controls.Add(this.lblTextData);
            this.Controls.Add(this.btnBrowseItemDat);
            this.Controls.Add(this.txtItemDatPath);
            this.Controls.Add(this.lblItemDat);
            this.MinimumSize = new System.Drawing.Size(500, 300);
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "ItemDataConverter - JRSアイテムデータ変換ツール";
            this.grpOutputOptions.ResumeLayout(false);
            this.grpOutputOptions.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Label lblItemDat;
        private System.Windows.Forms.TextBox txtItemDatPath;
        private System.Windows.Forms.Button btnBrowseItemDat;
        private System.Windows.Forms.Label lblTextData;
        private System.Windows.Forms.TextBox txtTextDataPath;
        private System.Windows.Forms.Button btnBrowseTextData;
        private System.Windows.Forms.Label lblOutput;
        private System.Windows.Forms.TextBox txtOutputPath;
        private System.Windows.Forms.Button btnBrowseOutput;
        private System.Windows.Forms.GroupBox grpOutputOptions;
        private System.Windows.Forms.CheckBox chkOutputJson;
        private System.Windows.Forms.CheckBox chkOutputHtml;
        private System.Windows.Forms.CheckBox chkOutputDecrypted;
        private System.Windows.Forms.Button btnConvert;
        private System.Windows.Forms.ProgressBar progressBar;
        private System.Windows.Forms.Label lblStatus;
    }
}
