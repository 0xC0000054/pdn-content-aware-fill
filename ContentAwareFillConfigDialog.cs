/*
*  This file is part of pdn-content-aware-fill, A Resynthesizer-based
*  content aware fill Effect plug-in for Paint.NET.
*
*  Copyright (C) 2018, 2020, 2021 Nicholas Hayes
*
*  This program is free software; you can redistribute it and/or modify
*  it under the terms of the GNU General Public License as published by
*  the Free Software Foundation; either version 2 of the License, or
*  (at your option) any later version.
*
*  This program is distributed in the hope that it will be useful,
*  but WITHOUT ANY WARRANTY; without even the implied warranty of
*  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
*  GNU General Public License for more details.
*
*  You should have received a copy of the GNU General Public License
*  along with this program; if not, write to the Free Software
*  Foundation, Inc., 675 Mass Ave, Cambridge, MA 02139, USA.
*
*/

using PaintDotNet;
using PaintDotNet.Effects;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace ContentAwareFill
{
    internal partial class ContentAwareFillConfigDialog : EffectConfigDialog
    {
        private int ignoreTokenChangedEventCount;
        private bool formClosePending;
        private bool restartBackgroundWorker;
        private bool selectionValid;
        private bool ranFirstAutoRender;
        private Surface output;
        private ResynthesizerRunner resynthesizer;

        public ContentAwareFillConfigDialog()
        {
            InitializeComponent();
            UI.InitScaling(this);
            this.ignoreTokenChangedEventCount = 0;
            this.formClosePending = false;
            this.selectionValid = false;
            this.ranFirstAutoRender = false;
            this.output = null;
            this.resynthesizer = null;
            PluginThemingUtil.EnableEffectDialogTheme(this);
        }

        protected override void OnBackColorChanged(EventArgs e)
        {
            base.OnBackColorChanged(e);

            PluginThemingUtil.UpdateControlBackColor(this);
        }

        protected override void OnForeColorChanged(EventArgs e)
        {
            base.OnForeColorChanged(e);

            PluginThemingUtil.UpdateControlForeColor(this);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (this.backgroundWorker.IsBusy)
            {
                e.Cancel = true;
                this.formClosePending = true;

                if (this.DialogResult == DialogResult.Cancel)
                {
                    this.backgroundWorker.CancelAsync();
                }
            }

            base.OnFormClosing(e);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            UpdateResetButtonIconForDpi();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            // This plugin does not support processing a selection of the whole image, it needs some
            // unselected pixels to replace the contents of the selected area.
            // When there is no active selection Paint.NET acts as if the whole image/layer is selected.
            if (ContentAwareFillEffect.IsWholeImageSelected(this.Selection, this.EffectSourceSurface.Bounds))
            {
                if (ShowMessage(Properties.Resources.WholeImageSelected, MessageBoxIcon.None) == DialogResult.OK)
                {
                    Close();
                }
            }
            else
            {
                this.selectionValid = true;

                if (this.ignoreTokenChangedEventCount == 0 && this.autoRenderCb.Checked && !this.ranFirstAutoRender)
                {
                    StartBackgroundWorker();
                }
            }
        }

        private void PushIgnoreTokenChangedEvents()
        {
            this.ignoreTokenChangedEventCount++;
        }

        private void PopIgnoreTokenChangedEvents()
        {
            this.ignoreTokenChangedEventCount--;

            if (this.ignoreTokenChangedEventCount == 0 && this.autoRenderCb.Checked && this.selectionValid)
            {
                this.ranFirstAutoRender = true;
                StartBackgroundWorker();
            }
        }

        private void UpdateConfigToken()
        {
            if (this.ignoreTokenChangedEventCount == 0 && this.autoRenderCb.Checked)
            {
                StartBackgroundWorker();
            }
        }

        protected override void InitialInitToken()
        {
            this.theEffectToken = new ContentAwareFillConfigToken(50,
                                                                  SampleSource.Sides,
                                                                  FillDirection.InwardToCenter,
                                                                  true,
                                                                  null);
        }

        protected override void InitDialogFromToken(EffectConfigToken effectTokenCopy)
        {
            ContentAwareFillConfigToken token = (ContentAwareFillConfigToken)effectTokenCopy;

            // Call FinishTokenUpdate after the controls are initialized.
            PushIgnoreTokenChangedEvents();

            this.sampleSizeTrackBar.Value = token.SampleSize;
            this.sampleFromCombo.SelectedIndex = (int)token.SampleFrom;
            this.fillDirectionCombo.SelectedIndex = (int)token.FillDirection;
            this.autoRenderCb.Checked = token.RenderAutomatically;

            PopIgnoreTokenChangedEvents();
        }

        protected override void InitTokenFromDialog()
        {
            ContentAwareFillConfigToken token = (ContentAwareFillConfigToken)this.theEffectToken;

            token.SampleSize = this.sampleSizeTrackBar.Value;
            token.SampleFrom =  (SampleSource)this.sampleFromCombo.SelectedIndex;
            token.FillDirection = (FillDirection)this.fillDirectionCombo.SelectedIndex;
            token.RenderAutomatically = this.autoRenderCb.Checked;
            token.Output = this.output;
        }

        private void UpdateResetButtonIconForDpi()
        {
            int dpi = 96;

            using (Graphics graphics = this.resetButton.CreateGraphics())
            {
                dpi = (int)graphics.DpiX;
            }

            if (dpi > 96)
            {
                if (dpi <= 120)
                {
                    this.resetButton.Image = new Bitmap(typeof(ContentAwareFillConfigDialog), "Resources.Icons.ResetIcon-120.png");
                }
                else if (dpi <= 144)
                {
                    this.resetButton.Image = new Bitmap(typeof(ContentAwareFillConfigDialog), "Resources.Icons.ResetIcon-144.png");
                }
                else if (dpi <= 192)
                {
                    this.resetButton.Image = new Bitmap(typeof(ContentAwareFillConfigDialog), "Resources.Icons.ResetIcon-192.png");
                }
                else
                {
                    this.resetButton.Image = new Bitmap(typeof(ContentAwareFillConfigDialog), "Resources.Icons.ResetIcon-384.png");
                }
            }
        }

        private DialogResult ShowMessage(string message, MessageBoxIcon icon)
        {
            MessageBoxOptions options = this.RightToLeft == RightToLeft.Yes ? MessageBoxOptions.RtlReading : 0;

            if (this.InvokeRequired)
            {
                return (DialogResult)Invoke(new Func<string, DialogResult>((string text) =>
                       MessageBox.Show(this, text, this.Text, MessageBoxButtons.OK, icon, MessageBoxDefaultButton.Button1, options)),
                       message);
            }
            else
            {
                return MessageBox.Show(this, message, this.Text, MessageBoxButtons.OK, icon, MessageBoxDefaultButton.Button1, options);
            }
        }

        private void okButton_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            Close();
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            Close();
        }

        private void sampleSizeTrackBar_ValueChanged(object sender, EventArgs e)
        {
            if (this.sampleSizeTrackBar.Value != (int)this.sampleSizeUpDown.Value)
            {
                this.sampleSizeUpDown.Value = this.sampleSizeTrackBar.Value;

                UpdateConfigToken();
            }
        }

        private void sampleSizeUpDown_ValueChanged(object sender, EventArgs e)
        {
            if (this.sampleSizeUpDown.Value != this.sampleSizeTrackBar.Value)
            {
                this.sampleSizeTrackBar.Value = (int)this.sampleSizeUpDown.Value;

                UpdateConfigToken();
            }
        }

        private void sampleFromCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateConfigToken();
        }

        private void fillDirectionCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateConfigToken();
        }

        private void resetButton_Click(object sender, EventArgs e)
        {
            this.sampleSizeTrackBar.Value = 50;
        }

        private void donateLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            this.Services.GetService<PaintDotNet.AppModel.IShellService>().LaunchUrl(this, "https://forums.getpaint.net/index.php?showtopic=112730");
        }

        private void autoRenderCb_CheckedChanged(object sender, EventArgs e)
        {
            this.applyButton.Enabled = !this.autoRenderCb.Checked;
        }

        private void applyButton_Click(object sender, EventArgs e)
        {
            StartBackgroundWorker();
        }

        private void StartBackgroundWorker()
        {
            if (this.backgroundWorker.IsBusy)
            {
                this.restartBackgroundWorker = true;
                this.backgroundWorker.CancelAsync();
            }
            else
            {
                if (this.resynthesizer is null)
                {
                    ContentAwareFillEffect effect = (ContentAwareFillEffect)this.Effect;

                    Surface source = effect.EnvironmentParameters.SourceSurface;
                    Rectangle sourceBounds = source.Bounds;
                    PdnRegion selection = effect.EnvironmentParameters.GetSelection(sourceBounds);

                    this.resynthesizer = new ResynthesizerRunner(source,
                                                                 sourceBounds,
                                                                 selection);
                }

                this.resynthesizer.SetParameters(this.sampleSizeTrackBar.Value,
                                                 (SampleSource)this.sampleFromCombo.SelectedIndex,
                                                 (FillDirection)this.fillDirectionCombo.SelectedIndex);

                this.backgroundWorker.RunWorkerAsync();
            }
        }

        private void backgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = (BackgroundWorker)sender;

            Surface output = this.resynthesizer.Run(() => worker.CancellationPending, worker.ReportProgress);

            if (output != null)
            {
                e.Result = output;
            }
            else
            {
                e.Cancel = true;
            }
        }

        private void backgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            this.progressBar1.Value = e.ProgressPercentage;
        }

        private void backgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            this.progressBar1.Value = 0;

            if (this.restartBackgroundWorker)
            {
                this.restartBackgroundWorker = false;
                this.resynthesizer.SetParameters(this.sampleSizeTrackBar.Value,
                                                 (SampleSource)this.sampleFromCombo.SelectedIndex,
                                                 (FillDirection)this.fillDirectionCombo.SelectedIndex);

                this.backgroundWorker.RunWorkerAsync();
            }
            else if (e.Error != null)
            {
                ShowMessage(e.Error.Message, MessageBoxIcon.Error);
            }
            else
            {
                if (this.output != null)
                {
                    this.output.Dispose();
                    this.output = null;
                }

                if (!e.Cancelled)
                {
                    this.output = (Surface)e.Result;
                    FinishTokenUpdate();
                }

                if (this.formClosePending)
                {
                    Close();
                }
            }
        }
    }
}
