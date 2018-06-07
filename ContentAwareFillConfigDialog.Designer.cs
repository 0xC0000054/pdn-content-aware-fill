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
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (components != null)
                {
                    components.Dispose();
                    components = null;
                }
            }
            base.Dispose(disposing);
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
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            this.resetButton = new System.Windows.Forms.Button();
            this.sampleSizeHeader = new ContentAwareFill.HeaderLabel();
            this.sampleFromHeader = new ContentAwareFill.HeaderLabel();
            this.fillDirectionHeader = new ContentAwareFill.HeaderLabel();
            this.donateLabel = new System.Windows.Forms.LinkLabel();
            ((System.ComponentModel.ISupportInitialize)(this.sampleSizeTrackBar)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.sampleSizeUpDown)).BeginInit();
            this.SuspendLayout();
            // 
            // cancelButton
            // 
            this.cancelButton.Location = new System.Drawing.Point(250, 210);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(75, 23);
            this.cancelButton.TabIndex = 0;
            this.cancelButton.Text = "Cancel";
            this.cancelButton.UseVisualStyleBackColor = true;
            this.cancelButton.Click += new System.EventHandler(this.cancelButton_Click);
            // 
            // okButton
            // 
            this.okButton.Location = new System.Drawing.Point(167, 210);
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
            // progressBar1
            // 
            this.progressBar1.Location = new System.Drawing.Point(12, 171);
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(310, 23);
            this.progressBar1.TabIndex = 8;
            // 
            // resetButton
            // 
            this.resetButton.Image = global::ContentAwareFill.Properties.Resources.arrow_180_small;
            this.resetButton.Location = new System.Drawing.Point(300, 37);
            this.resetButton.Name = "resetButton";
            this.resetButton.Size = new System.Drawing.Size(22, 20);
            this.resetButton.TabIndex = 3;
            this.resetButton.UseVisualStyleBackColor = true;
            this.resetButton.Click += new System.EventHandler(this.resetButton_Click);
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
            // donateLabel
            // 
            this.donateLabel.AutoSize = true;
            this.donateLabel.Location = new System.Drawing.Point(9, 215);
            this.donateLabel.Name = "donateLabel";
            this.donateLabel.Size = new System.Drawing.Size(45, 13);
            this.donateLabel.TabIndex = 6;
            this.donateLabel.TabStop = true;
            this.donateLabel.Text = "Donate!";
            this.donateLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.donateLabel_LinkClicked);
            // 
            // ContentAwareFillConfigDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.ClientSize = new System.Drawing.Size(337, 244);
            this.Controls.Add(this.donateLabel);
            this.Controls.Add(this.fillDirectionHeader);
            this.Controls.Add(this.sampleFromHeader);
            this.Controls.Add(this.sampleSizeHeader);
            this.Controls.Add(this.progressBar1);
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
            this.Controls.SetChildIndex(this.progressBar1, 0);
            this.Controls.SetChildIndex(this.sampleSizeHeader, 0);
            this.Controls.SetChildIndex(this.sampleFromHeader, 0);
            this.Controls.SetChildIndex(this.fillDirectionHeader, 0);
            this.Controls.SetChildIndex(this.donateLabel, 0);
            ((System.ComponentModel.ISupportInitialize)(this.sampleSizeTrackBar)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.sampleSizeUpDown)).EndInit();
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
        private System.Windows.Forms.ProgressBar progressBar1;
        private System.Windows.Forms.Button resetButton;
        private HeaderLabel sampleSizeHeader;
        private HeaderLabel sampleFromHeader;
        private HeaderLabel fillDirectionHeader;
        private System.Windows.Forms.LinkLabel donateLabel;
    }
}
