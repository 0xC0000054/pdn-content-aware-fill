namespace ContentAwareFill
{
    partial class ContentAwareFillConfigDialog
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands")]
        protected override void OnDispose(bool disposing)
        {
            if (disposing)
            {
                if (components != null)
                {
                    components.Dispose();
                    components = null;
                }

                if (this.resynthesizer != null)
                {
                    this.resynthesizer.Dispose();
                    this.resynthesizer = null;
                }
            }
            base.OnDispose(disposing);
        }

        #region Component Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.cancelButton = new System.Windows.Forms.Button();
            this.okButton = new System.Windows.Forms.Button();
            this.sampleFromCombo = new System.Windows.Forms.ComboBox();
            this.fillDirectionCombo = new System.Windows.Forms.ComboBox();
            this.sampleSizeTrackBar = new System.Windows.Forms.TrackBar();
            this.sampleSizeUpDown = new System.Windows.Forms.NumericUpDown();
            this.resetButton = new System.Windows.Forms.Button();
            this.donateLabel = new System.Windows.Forms.LinkLabel();
            this.autoRenderCb = new System.Windows.Forms.CheckBox();
            this.applyButton = new System.Windows.Forms.Button();
            this.backgroundWorker = new System.ComponentModel.BackgroundWorker();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.toolStripStatusLabel1 = new System.Windows.Forms.ToolStripStatusLabel();
            this.toolStripProgressBar1 = new System.Windows.Forms.ToolStripProgressBar();
            this.fillDirectionHeader = new ContentAwareFill.HeaderLabel();
            this.sampleFromHeader = new ContentAwareFill.HeaderLabel();
            this.sampleSizeHeader = new ContentAwareFill.HeaderLabel();
            ((System.ComponentModel.ISupportInitialize)(this.sampleSizeTrackBar)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.sampleSizeUpDown)).BeginInit();
            this.statusStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // cancelButton
            // 
            this.cancelButton.Location = new System.Drawing.Point(250, 226);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(75, 23);
            this.cancelButton.TabIndex = 0;
            this.cancelButton.Text = "Cancel";
            this.cancelButton.UseVisualStyleBackColor = true;
            this.cancelButton.Click += new System.EventHandler(this.cancelButton_Click);
            // 
            // okButton
            // 
            this.okButton.Location = new System.Drawing.Point(169, 226);
            this.okButton.Name = "okButton";
            this.okButton.Size = new System.Drawing.Size(75, 23);
            this.okButton.TabIndex = 7;
            this.okButton.Text = "Ok";
            this.okButton.UseVisualStyleBackColor = true;
            this.okButton.Click += new System.EventHandler(this.okButton_Click);
            // 
            // sampleFromCombo
            // 
            this.sampleFromCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.sampleFromCombo.FormattingEnabled = true;
            this.sampleFromCombo.Items.AddRange(new object[] {
            "All Around",
            "Sides",
            "Top and Bottom"});
            this.sampleFromCombo.Location = new System.Drawing.Point(12, 88);
            this.sampleFromCombo.Name = "sampleFromCombo";
            this.sampleFromCombo.Size = new System.Drawing.Size(152, 21);
            this.sampleFromCombo.TabIndex = 4;
            this.sampleFromCombo.SelectedIndexChanged += new System.EventHandler(this.sampleFromCombo_SelectedIndexChanged);
            // 
            // fillDirectionCombo
            // 
            this.fillDirectionCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.fillDirectionCombo.FormattingEnabled = true;
            this.fillDirectionCombo.Items.AddRange(new object[] {
            "Random",
            "Inwards towards center",
            "Outwards from center"});
            this.fillDirectionCombo.Location = new System.Drawing.Point(12, 135);
            this.fillDirectionCombo.Name = "fillDirectionCombo";
            this.fillDirectionCombo.Size = new System.Drawing.Size(152, 21);
            this.fillDirectionCombo.TabIndex = 5;
            this.fillDirectionCombo.SelectedIndexChanged += new System.EventHandler(this.fillDirectionCombo_SelectedIndexChanged);
            // 
            // sampleSizeTrackBar
            // 
            this.sampleSizeTrackBar.Location = new System.Drawing.Point(12, 37);
            this.sampleSizeTrackBar.Maximum = 100;
            this.sampleSizeTrackBar.Minimum = 1;
            this.sampleSizeTrackBar.Name = "sampleSizeTrackBar";
            this.sampleSizeTrackBar.Size = new System.Drawing.Size(215, 45);
            this.sampleSizeTrackBar.TabIndex = 1;
            this.sampleSizeTrackBar.TickFrequency = 5;
            this.sampleSizeTrackBar.TickStyle = System.Windows.Forms.TickStyle.None;
            this.sampleSizeTrackBar.Value = 50;
            this.sampleSizeTrackBar.ValueChanged += new System.EventHandler(this.sampleSizeTrackBar_ValueChanged);
            // 
            // sampleSizeUpDown
            // 
            this.sampleSizeUpDown.Location = new System.Drawing.Point(233, 37);
            this.sampleSizeUpDown.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.sampleSizeUpDown.Name = "sampleSizeUpDown";
            this.sampleSizeUpDown.Size = new System.Drawing.Size(61, 20);
            this.sampleSizeUpDown.TabIndex = 2;
            this.sampleSizeUpDown.Value = new decimal(new int[] {
            50,
            0,
            0,
            0});
            this.sampleSizeUpDown.ValueChanged += new System.EventHandler(this.sampleSizeUpDown_ValueChanged);
            // 
            // resetButton
            // 
            this.resetButton.Image = global::ContentAwareFill.Properties.Resources.ResetIcon_96;
            this.resetButton.Location = new System.Drawing.Point(300, 37);
            this.resetButton.Name = "resetButton";
            this.resetButton.Size = new System.Drawing.Size(22, 20);
            this.resetButton.TabIndex = 3;
            this.resetButton.UseVisualStyleBackColor = true;
            this.resetButton.Click += new System.EventHandler(this.resetButton_Click);
            // 
            // donateLabel
            // 
            this.donateLabel.AutoSize = true;
            this.donateLabel.Location = new System.Drawing.Point(9, 231);
            this.donateLabel.Name = "donateLabel";
            this.donateLabel.Size = new System.Drawing.Size(45, 13);
            this.donateLabel.TabIndex = 6;
            this.donateLabel.TabStop = true;
            this.donateLabel.Text = "Donate!";
            this.donateLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.donateLabel_LinkClicked);
            // 
            // autoRenderCb
            // 
            this.autoRenderCb.AutoSize = true;
            this.autoRenderCb.Checked = true;
            this.autoRenderCb.CheckState = System.Windows.Forms.CheckState.Checked;
            this.autoRenderCb.Location = new System.Drawing.Point(12, 177);
            this.autoRenderCb.Name = "autoRenderCb";
            this.autoRenderCb.Size = new System.Drawing.Size(125, 17);
            this.autoRenderCb.TabIndex = 15;
            this.autoRenderCb.Text = "Render automatically";
            this.autoRenderCb.UseVisualStyleBackColor = true;
            this.autoRenderCb.CheckedChanged += new System.EventHandler(this.autoRenderCb_CheckedChanged);
            // 
            // applyButton
            // 
            this.applyButton.Enabled = false;
            this.applyButton.Location = new System.Drawing.Point(250, 173);
            this.applyButton.Name = "applyButton";
            this.applyButton.Size = new System.Drawing.Size(75, 23);
            this.applyButton.TabIndex = 16;
            this.applyButton.Text = "Apply";
            this.applyButton.UseVisualStyleBackColor = true;
            this.applyButton.Click += new System.EventHandler(this.applyButton_Click);
            // 
            // backgroundWorker
            // 
            this.backgroundWorker.WorkerReportsProgress = true;
            this.backgroundWorker.WorkerSupportsCancellation = true;
            this.backgroundWorker.DoWork += new System.ComponentModel.DoWorkEventHandler(this.backgroundWorker_DoWork);
            this.backgroundWorker.ProgressChanged += new System.ComponentModel.ProgressChangedEventHandler(this.backgroundWorker_ProgressChanged);
            this.backgroundWorker.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.backgroundWorker_RunWorkerCompleted);
            // 
            // statusStrip1
            // 
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusLabel1,
            this.toolStripProgressBar1});
            this.statusStrip1.Location = new System.Drawing.Point(0, 252);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(337, 22);
            this.statusStrip1.SizingGrip = false;
            this.statusStrip1.TabIndex = 17;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // toolStripStatusLabel1
            // 
            this.toolStripStatusLabel1.Name = "toolStripStatusLabel1";
            this.toolStripStatusLabel1.Size = new System.Drawing.Size(118, 17);
            this.toolStripStatusLabel1.Text = "toolStripStatusLabel1";
            // 
            // toolStripProgressBar1
            // 
            this.toolStripProgressBar1.Name = "toolStripProgressBar1";
            this.toolStripProgressBar1.Size = new System.Drawing.Size(100, 16);
            // 
            // fillDirectionHeader
            // 
            this.fillDirectionHeader.ForeColor = System.Drawing.SystemColors.Highlight;
            this.fillDirectionHeader.Location = new System.Drawing.Point(12, 115);
            this.fillDirectionHeader.Name = "fillDirectionHeader";
            this.fillDirectionHeader.Size = new System.Drawing.Size(310, 14);
            this.fillDirectionHeader.TabIndex = 14;
            this.fillDirectionHeader.TabStop = false;
            this.fillDirectionHeader.Text = "Fill direction:";
            // 
            // sampleFromHeader
            // 
            this.sampleFromHeader.ForeColor = System.Drawing.SystemColors.Highlight;
            this.sampleFromHeader.Location = new System.Drawing.Point(12, 63);
            this.sampleFromHeader.Name = "sampleFromHeader";
            this.sampleFromHeader.Size = new System.Drawing.Size(310, 14);
            this.sampleFromHeader.TabIndex = 12;
            this.sampleFromHeader.TabStop = false;
            this.sampleFromHeader.Text = "Sample from:";
            // 
            // sampleSizeHeader
            // 
            this.sampleSizeHeader.ForeColor = System.Drawing.SystemColors.Highlight;
            this.sampleSizeHeader.Location = new System.Drawing.Point(12, 17);
            this.sampleSizeHeader.Name = "sampleSizeHeader";
            this.sampleSizeHeader.Size = new System.Drawing.Size(310, 14);
            this.sampleSizeHeader.TabIndex = 11;
            this.sampleSizeHeader.TabStop = false;
            this.sampleSizeHeader.Text = "Sample area size (in pixels):";
            // 
            // ContentAwareFillConfigDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.ClientSize = new System.Drawing.Size(337, 274);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.applyButton);
            this.Controls.Add(this.autoRenderCb);
            this.Controls.Add(this.donateLabel);
            this.Controls.Add(this.fillDirectionHeader);
            this.Controls.Add(this.sampleFromHeader);
            this.Controls.Add(this.sampleSizeHeader);
            this.Controls.Add(this.okButton);
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.resetButton);
            this.Controls.Add(this.sampleSizeUpDown);
            this.Controls.Add(this.fillDirectionCombo);
            this.Controls.Add(this.sampleFromCombo);
            this.Controls.Add(this.sampleSizeTrackBar);
            this.Location = new System.Drawing.Point(0, 0);
            this.Name = "ContentAwareFillConfigDialog";
            this.Text = "Content Aware Fill";
            this.Controls.SetChildIndex(this.sampleSizeTrackBar, 0);
            this.Controls.SetChildIndex(this.sampleFromCombo, 0);
            this.Controls.SetChildIndex(this.fillDirectionCombo, 0);
            this.Controls.SetChildIndex(this.sampleSizeUpDown, 0);
            this.Controls.SetChildIndex(this.resetButton, 0);
            this.Controls.SetChildIndex(this.cancelButton, 0);
            this.Controls.SetChildIndex(this.okButton, 0);
            this.Controls.SetChildIndex(this.sampleSizeHeader, 0);
            this.Controls.SetChildIndex(this.sampleFromHeader, 0);
            this.Controls.SetChildIndex(this.fillDirectionHeader, 0);
            this.Controls.SetChildIndex(this.donateLabel, 0);
            this.Controls.SetChildIndex(this.autoRenderCb, 0);
            this.Controls.SetChildIndex(this.applyButton, 0);
            this.Controls.SetChildIndex(this.statusStrip1, 0);
            ((System.ComponentModel.ISupportInitialize)(this.sampleSizeTrackBar)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.sampleSizeUpDown)).EndInit();
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button cancelButton;
        private System.Windows.Forms.Button okButton;
        private System.Windows.Forms.ComboBox sampleFromCombo;
        private System.Windows.Forms.ComboBox fillDirectionCombo;
        private System.Windows.Forms.TrackBar sampleSizeTrackBar;
        private System.Windows.Forms.NumericUpDown sampleSizeUpDown;
        private System.Windows.Forms.Button resetButton;
        private HeaderLabel sampleSizeHeader;
        private HeaderLabel sampleFromHeader;
        private HeaderLabel fillDirectionHeader;
        private System.Windows.Forms.LinkLabel donateLabel;
        private System.Windows.Forms.CheckBox autoRenderCb;
        private System.Windows.Forms.Button applyButton;
        private System.ComponentModel.BackgroundWorker backgroundWorker;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel1;
        private System.Windows.Forms.ToolStripProgressBar toolStripProgressBar1;
    }
}
